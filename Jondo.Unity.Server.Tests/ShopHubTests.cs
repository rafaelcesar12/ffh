using Jondo.Protocol;
using Jondo.Unity.Server.Handlers;
using Jondo.Unity.Server.Managers;
using Jondo.Unity.Server.Network;
using Xunit;

namespace Jondo.Unity.Server.Tests;

public sealed class ShopHubTests
{
    [Fact]
    public void One_kama_override_is_opt_in_and_does_not_discount_existing_merchants()
    {
        Assert.Null(ShopHub.PriceFor(false, ShopHub.RuneSeller));
        Assert.Equal(1, ShopHub.PriceFor(true, ShopHub.RuneSeller));
        Assert.Equal(1, ShopHub.PriceFor(true, ShopHub.PotionSeller));
        Assert.Null(ShopHub.PriceFor(true, 5548));
        Assert.Null(ShopHub.PriceFor(true, ShopHub.DungeonGuide));
    }

    [Fact]
    public void Resource_sellers_sell_at_one_kama_only_while_the_hub_is_on()
    {
        var resources = new HashSet<int> { 313 };
        Assert.Equal(1, ShopHub.PriceFor(true, 313, resources));
        Assert.Null(ShopHub.PriceFor(false, 313, resources));
        Assert.Null(ShopHub.PriceFor(true, 314, resources));
        Assert.Null(ShopHub.PriceFor(true, 313));
    }

    [Fact]
    public void Resource_shop_catalogues_fit_the_client_and_never_overlap()
    {
        string content = Path.Combine(RepoRoot(), "content", "npcs");
        using var hub = System.Text.Json.JsonDocument.Parse(File.ReadAllText(Path.Combine(content, "shop_hub.json")));
        var sellers = hub.RootElement.GetProperty("resourceSellers").EnumerateArray().Select(e => e.GetInt32()).ToHashSet();
        long map = hub.RootElement.GetProperty("resourceMap").GetInt64();
        Assert.Equal(11, sellers.Count);

        using var shops = System.Text.Json.JsonDocument.Parse(File.ReadAllText(Path.Combine(content, "shops.json")));
        var seen = new HashSet<int>();
        int written = 0;
        foreach (var shop in shops.RootElement.GetProperty("shops").EnumerateArray())
        {
            if (!sellers.Contains(shop.GetProperty("npc").GetInt32())) continue;
            written++;
            var items = shop.GetProperty("items").EnumerateArray().ToArray();
            Assert.InRange(items.Length, 1, 444);
            foreach (var item in items)
            {
                Assert.False(item.TryGetProperty("price", out _), "a written price would change it everywhere");
                Assert.True(seen.Add(item.GetProperty("gid").GetInt32()));
            }
        }
        Assert.Equal(sellers.Count, written);

        using var spawns = System.Text.Json.JsonDocument.Parse(File.ReadAllText(Path.Combine(content, "spawns.json")));
        var placed = spawns.RootElement.GetProperty("spawns").EnumerateArray()
            .Where(s => s.GetProperty("map").GetInt64() == map).Select(s => s.GetProperty("npc").GetInt32()).ToHashSet();
        Assert.Subset(placed, sellers);
    }

    private static string RepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null && !Directory.Exists(Path.Combine(dir.FullName, "content", "npcs"))) dir = dir.Parent;
        return dir?.FullName ?? throw new DirectoryNotFoundException("content/npcs");
    }

    [Theory]
    [InlineData(true, 88212759, 88212759, 7575, false, true)]
    [InlineData(false, 88212759, 88212759, 7575, false, false)]
    [InlineData(true, 1, 88212759, 7575, false, false)]
    [InlineData(true, 88212759, 1, 7575, false, false)]
    [InlineData(true, 88212759, 88212759, 7379, false, false)]
    [InlineData(true, 88212759, 88212759, 7575, true, false)]
    public void Guide_requires_its_real_map_and_template_outside_combat(bool enabled,
        long current, long requested, int npc, bool fighting, bool expected)
        => Assert.Equal(expected, ShopHub.CanOpenGuide(enabled, current, requested, npc, fighting));

    [Fact]
    public void Destinations_are_last_rooms_not_entrances_and_duplicate_expeditions_collapse()
    {
        var first = Dungeon(1, 100, 101, 102);
        var expedition = Dungeon(2, 100, 101, 102);
        var missing = Dungeon(3, 200, 201);
        var empty = Dungeon(4, 300);
        var result = ShopHub.LastRooms(new[] { expedition, missing, empty, first }, map => map == 102);
        Assert.Same(first, Assert.Single(result));
        Assert.Equal(102, result[0].Rooms[^1]);
    }

    [Fact]
    public void Destination_authorization_is_session_local_and_requires_the_offered_map()
    {
        var first = new SessionState();
        var other = new SessionState();
        first.OpenHubDungeonMaps.Add(169349120);
        Assert.True(Allowed(first.OpenHubDungeonMaps));
        Assert.False(Allowed(other.OpenHubDungeonMaps));
        Assert.False(ShopHubTravelHandler.MayTravel(true, ShopHub.MapId, ShopHub.MapId,
            false, 0, 999, first.OpenHubDungeonMaps));
        Assert.False(ShopHubTravelHandler.MayTravel(true, ShopHub.MapId, ShopHub.MapId,
            false, 1, 169349120, first.OpenHubDungeonMaps));
        Assert.False(ShopHubTravelHandler.MayTravel(true, 1, ShopHub.MapId,
            false, 0, 169349120, first.OpenHubDungeonMaps));
        Assert.False(ShopHubTravelHandler.MayTravel(true, ShopHub.MapId, 0,
            false, 0, 169349120, first.OpenHubDungeonMaps));
        Assert.False(ShopHubTravelHandler.MayTravel(true, ShopHub.MapId, ShopHub.MapId,
            true, 0, 169349120, first.OpenHubDungeonMaps));
        first.OpenHubDungeonMaps.Clear();
        Assert.False(Allowed(first.OpenHubDungeonMaps));
    }

    [Fact]
    public void Advertised_shop_price_uses_the_override_without_changing_normal_prices()
    {
        const int item = 2147483000;
        byte[] normal = ConnectionProtocol.BuildShop(-20001, new[] { item });
        byte[] overridden = ConnectionProtocol.BuildShop(-20001, new[] { item }, unitPrice: 7);
        Assert.Equal(1, Price(normal));
        Assert.Equal(7, Price(overridden));
        Assert.Equal(1, Price(ConnectionProtocol.BuildShop(-20001, new[] { item })));
    }

    [Fact]
    public void Hub_items_never_sell_back_for_more_than_they_cost_while_the_hub_is_on()
    {
        var hub = new HashSet<int> { 20155 };
        // A potion bought for one kama whose template would pay 200 a unit.
        Assert.Equal(1, ShopHub.CapBuyBack(true, hub, 20155, 1, 200));
        Assert.Equal(5, ShopHub.CapBuyBack(true, hub, 20155, 5, 5 * 200));
        // Never raised: a cheaper resale stays as it was (zero stays zero).
        Assert.Equal(0, ShopHub.CapBuyBack(true, hub, 20155, 3, 0));
        // Hub off, or an item the hub does not sell: the normal tenth of the template price.
        Assert.Equal(200, ShopHub.CapBuyBack(false, hub, 20155, 1, 200));
        Assert.Equal(1290, ShopHub.CapBuyBack(true, hub, 12900, 1, 1290));
    }

    private static long Price(byte[] payload)
    {
        var entry = ProtoMessage.Parse(ProtoMessage.Parse(payload).Fields.Single(f => f.FieldNumber == 1).BytesValue);
        var price = ProtoMessage.Parse(entry.Fields.Single(f => f.FieldNumber == 3).BytesValue);
        return price.Fields.Single(f => f.FieldNumber == 2).VarIntValue;
    }

    private static bool Allowed(IReadOnlySet<long> maps)
        => ShopHubTravelHandler.MayTravel(true, ShopHub.MapId, ShopHub.MapId, false, 0, 169349120, maps);

    private static DungeonManager.Dungeon Dungeon(int id, long entrance, params long[] rooms)
    {
        var dungeon = new DungeonManager.Dungeon { Id = id, EntranceMapId = entrance };
        dungeon.Rooms.AddRange(rooms);
        return dungeon;
    }
}
