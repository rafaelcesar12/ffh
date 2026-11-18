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
        HubItems = new HashSet<int>();
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
        HubItems = NpcShops.CatalogueOf(RuneSeller).Concat(NpcShops.CatalogueOf(PotionSeller)).ToHashSet();
        Enabled = true;
        Console.WriteLine($"[ShopHub] {NpcShops.CatalogueOf(RuneSeller).Count} runes, "
            + $"{NpcShops.CatalogueOf(PotionSeller).Count} potions, one kama per item; "
            + $"resale of those {HubItems.Count} items capped at one kama.");
    }

    /// <summary>Every item the two one-kama sellers offer, whichever vendor it is later sold to.</summary>
    internal static IReadOnlySet<int> HubItems { get; private set; } = new HashSet<int>();

    /// <summary>
    /// What a vendor pays for a sale, closing the loop «buy at one kama, sell back at a tenth of the
    /// template price»: while the hub is on, its items never sell back for more than they cost here.
    /// Items are not marked with where they came from, so a dropped or crafted copy is capped too;
    /// that is the price of a test server where they can be bought for one kama anyway.
    /// </summary>
    internal static long BuyBack(int gid, int quantity)
        => CapBuyBack(Enabled, HubItems, gid, quantity, NpcShops.BuyBackOf(gid, quantity));

    internal static long CapBuyBack(bool enabled, IReadOnlySet<int> hubItems, int gid, int quantity, long normal)
        => enabled && hubItems.Contains(gid) ? Math.Min(normal, HubUnitPrice * quantity) : normal;

    private const long HubUnitPrice = 1;

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
