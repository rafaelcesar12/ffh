using System.Text.Json;
using Jondo.Unity.Launcher;

namespace Jondo.Unity.Server.Managers;

/// <summary>Opt-in services for the local test server's existing merchant map.</summary>
internal static class ShopHub
{
    internal const long MapId = 88212759;
    internal const int RuneSeller = 7379;
    internal const int PotionSeller = 166; // Uba Tesse: the stock client's alchemist merchant.
    internal const int DungeonGuide = 7575; // Stock dungeon-teleport-rune merchant.
    internal static bool Enabled { get; private set; }

    internal static void Load()
    {
        Enabled = false;
        string path = Paths.ContentFile("npcs/shop_hub.json");
        if (!File.Exists(path)) return;
        using var doc = JsonDocument.Parse(File.ReadAllText(path));
        var root = doc.RootElement;
        if (!root.GetProperty("enabled").GetBoolean()) return;
        if (root.GetProperty("map").GetInt64() != MapId
            || root.GetProperty("runeSeller").GetInt32() != RuneSeller
            || root.GetProperty("potionSeller").GetInt32() != PotionSeller
            || root.GetProperty("dungeonGuide").GetInt32() != DungeonGuide
            || root.GetProperty("unitPrice").GetInt32() != 1)
            throw new InvalidDataException("Invalid local shop hub configuration.");
        if (NpcShops.CatalogueOf(RuneSeller).Count == 0 || NpcShops.CatalogueOf(PotionSeller).Count == 0)
            throw new InvalidDataException("Local shop hub catalogues are missing.");
        foreach (int npc in new[] { RuneSeller, PotionSeller, DungeonGuide })
            if (!Npcs.Of(MapId).Any(spawn => spawn.NpcId == npc))
                throw new InvalidDataException($"Local shop hub NPC {npc} is missing.");
        Enabled = true;
        Console.WriteLine($"[ShopHub] {NpcShops.CatalogueOf(RuneSeller).Count} runes, "
            + $"{NpcShops.CatalogueOf(PotionSeller).Count} potions, one kama per item.");
    }

    internal static long? UnitPrice(int npcId)
        => PriceFor(Enabled, npcId);

    internal static long? PriceFor(bool enabled, int npcId)
        => enabled && (npcId == RuneSeller || npcId == PotionSeller) ? 1L : null;

    internal static bool CanOpenGuide(bool enabled, long currentMap, long requestedMap, int npcId, bool inFight)
        => enabled && !inFight && currentMap == MapId && requestedMap == MapId && npcId == DungeonGuide;

    /// <summary>Last combat room, never a post-boss exit; duplicate expedition destinations collapse.</summary>
    internal static IReadOnlyList<DungeonManager.Dungeon> LastRooms(
        IEnumerable<DungeonManager.Dungeon> dungeons, Func<long, bool> mapExists)
        => dungeons.Where(d => d.Rooms.Count > 0 && mapExists(d.Rooms[^1]))
            .OrderBy(d => d.Id).DistinctBy(d => d.Rooms[^1]).ToArray();
}
