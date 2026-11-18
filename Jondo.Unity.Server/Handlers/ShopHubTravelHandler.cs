using System.Net.Sockets;
using Jondo.Unity.Protocol;
using Jondo.Unity.Server.Managers;
using Jondo.Unity.Server.Network;

namespace Jondo.Unity.Server.Handlers;

/// <summary>Uses the client's existing destination picker; no client patches or new text IDs.</summary>
internal static class ShopHubTravelHandler
{
    internal static async Task<bool> OpenAsync(NetworkStream stream, Npcs.Spawn npc, long requestedMap)
    {
        if (!ShopHub.CanOpenGuide(ShopHub.Enabled, SessionContext.State.MapId,
            requestedMap, npc.NpcId, SessionContext.State.IsInFight)) return false;
        if (await Jail.KeepsInAsync(stream)) return true;
        if (await ConquestHandler.HoldsInAsync(stream, ConquestHandler.NoTeleportInAWar)) return true;

        var destinations = new List<ConnectionProtocol.ZaapDestination>();
        var allowed = new HashSet<long>();
        foreach (var dungeon in ShopHub.LastRooms(DungeonManager.All.Values, map => MapManager.GetMapInfo(map) != null))
        {
            long map = dungeon.Rooms[^1];
            var info = MapManager.GetMapInfo(map)!;
            if (!MapManager.WalkableCells.TryGetValue(map, out var cells) || cells.Count == 0) continue;
            allowed.Add(map);
            destinations.Add(new ConnectionProtocol.ZaapDestination(map, info.SubAreaId, dungeon.OptimalLevel, 0));
        }
        SessionContext.State.OpenHubDungeonMaps = allowed;
        SessionContext.State.OpenZaapMapId = ShopHub.MapId;
        await Jondo.Protocol.NetworkMessage.WriteFrameAsync(stream,
            ConnectionProtocol.Push(Op.Hjj, ConnectionProtocol.BuildZaapList(0, destinations)));
        Console.WriteLine($"[ShopHub] Opened {allowed.Count} last-room destinations for {SessionContext.State.CharacterId}.");
        return true;
    }

    internal static void Forget() => SessionContext.State.OpenHubDungeonMaps.Clear();

    internal static bool MayTravel(bool enabled, long currentMap, long openMap, bool inFight,
        int kind, long chosen, IReadOnlySet<long> offered)
        => enabled && !inFight && currentMap == ShopHub.MapId && openMap == ShopHub.MapId
            && kind == 0 && offered.Contains(chosen);

    internal static async Task<bool> TryTravelAsync(NetworkStream stream, int kind, long chosen)
    {
        var state = SessionContext.State;
        if (state.OpenHubDungeonMaps.Count == 0) return false;
        bool accepted = MayTravel(ShopHub.Enabled, state.MapId, state.OpenZaapMapId,
            state.IsInFight, kind, chosen, state.OpenHubDungeonMaps);
        Forget(); // A selection is single-use; stale or forged choices never become a normal zaap trip.
        state.OpenZaapMapId = 0;
        await Jondo.Protocol.NetworkMessage.WriteFrameAsync(stream,
            ConnectionProtocol.Push(Op.Kld, ConnectionProtocol.BuildDialogClosed()));
        if (!accepted) return true;
        int cell = await TeleportHandler.ToMapAsync(stream, chosen);
        if (cell >= 0)
            Console.WriteLine($"[ShopHub] {state.CharacterId} travelled to last room {chosen}, cell {cell}, free.");
        return true;
    }
}
