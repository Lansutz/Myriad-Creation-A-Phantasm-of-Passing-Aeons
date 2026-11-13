using System.Collections.Generic;
using NUnit.Framework;
using MyriadCreation.Core.Data;
using MyriadCreation.Infrastructure.Save;
using MyriadCreation.Simulation.State;
using MyriadCreation.Simulation.WorldState;
using MyriadCreation.World.Anchor;
using MyriadCreation.World.Settlement;
using MyriadCreation.Save.Contracts;

namespace MyriadCreation.Tests.EditMode
{
    /// <summary>荒野建点与栖居点市场存档。</summary>
    public class SettlementFoundingAndMarketSaveTests
    {
        [Test]
        public void CreateWildernessSettlement_ClaimsTile_AndRegistersSite()
        {
            var world = new GameWorld
            {
                mapWidth = 1,
                mapHeight = 1,
                tiles = new[]
                {
                    new TileData { exists = true, isLand = true, ownerRealmId = -1, provinceId = 0 }
                },
                realms = new Dictionary<int, RealmData>(),
                settlements = new Dictionary<int, SettlementData>(),
                anchors = new Dictionary<int, AnchorData>(),
            };
            var realm = new RealmData { realmId = 1 };
            world.realms[1] = realm;

            var settlement = world.CreateWildernessSettlement(1, 0, -1);
            Assert.IsNotNull(settlement, "无主陆地应可开拓");
            Assert.AreEqual(1, world.tiles[0].ownerRealmId, "开拓后地块归该政权");
            Assert.IsTrue(realm.coreTiles.Contains(0), "地块加入核心领地");
            Assert.IsTrue(world.settlements.ContainsKey(settlement.anchorId));
            Assert.IsTrue(world.anchors.ContainsKey(settlement.anchorId));
        }

        [Test]
        public void WildernessSettlement_FailsOnOwnedTile()
        {
            var world = new GameWorld
            {
                mapWidth = 1, mapHeight = 1,
                tiles = new[] { new TileData { exists = true, isLand = true, ownerRealmId = 2 } },
                realms = new Dictionary<int, RealmData>(),
                settlements = new Dictionary<int, SettlementData>(),
                anchors = new Dictionary<int, AnchorData>(),
            };
            world.realms[1] = new RealmData { realmId = 1 };
            Assert.IsNull(world.CreateWildernessSettlement(1, 0, -1), "有主地块不能按无主开拓");
        }

        [Test]
        public void Market_SaveRoundTrip_PreservesStocksAndCapacity()
        {
            var market = new SettlementMarket(capacity: 500f);
            market.AddStock(53, 10f);
            market.AddStock(55, 7f);
            market.demand[53] = 4f;
            market.supply[53] = 6f;

            var save = SettlementMarketSaveMapping.Capture(market);
            Assert.IsNotNull(save);
            var restored = SettlementMarketSaveMapping.ToMarket(save);

            Assert.AreEqual(500f, restored.capacity, 0.01f);
            Assert.AreEqual(10f, restored.GetStock(53), 0.01f);
            Assert.AreEqual(7f, restored.GetStock(55), 0.01f);
            Assert.AreEqual(4f, restored.demand[53], 0.01f);
            Assert.AreEqual(6f, restored.supply[53], 0.01f);
        }

        [Test]
        public void Settlement_GetOrCreateMarket_UsesWarehouseCapacity()
        {
            var settlement = new SettlementData();
            var warehouse = settlement.AddBlock(SettlementBlockKind.Warehouse, null, 2);
            warehouse.AddChild(SettlementBlockKind.Building, 1, SettlementBlockSubtype.Granary);

            var market = settlement.GetOrCreateMarket();
            Assert.IsNotNull(market);
            Assert.AreEqual(800f * 2 + 400f, market.capacity, 0.01f);
            Assert.AreSame(market, settlement.GetOrCreateMarket(), "市场实例应复用");
        }
    }
}
