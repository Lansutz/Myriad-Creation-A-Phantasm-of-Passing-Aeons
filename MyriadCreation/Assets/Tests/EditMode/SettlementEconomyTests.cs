using NUnit.Framework;
using MyriadCreation.World.Settlement;

namespace MyriadCreation.Tests.EditMode
{
    /// <summary>C2 第一片：栖居点经济读数全部从块树推导（不依赖地区 TradeCenter）。</summary>
    public class SettlementEconomyTests
    {
        [Test]
        public void EmptySettlement_HasNoEconomyReadings()
        {
            var settlement = new SettlementData();
            var snapshot = SettlementEconomy.Read(settlement);
            Assert.IsFalse(snapshot.hasMarket);
            Assert.AreEqual(0f, snapshot.marketSize);
            Assert.AreEqual(0f, snapshot.warehouseCapacity);
            Assert.AreEqual(0, snapshot.productionSlots);
            Assert.AreEqual(0f, snapshot.tradeConnectivity);
        }

        [Test]
        public void MarketBlock_DrivesMarketSize()
        {
            var settlement = new SettlementData
            {
                population = 1000f,
                development = 20f,
                tradePower = 30f,
            };
            var market = settlement.AddBlock(SettlementBlockKind.Market, null, 2);
            market.AddChild(SettlementBlockKind.Building, 1, SettlementBlockSubtype.Stall);
            market.AddChild(SettlementBlockKind.Building, 1, SettlementBlockSubtype.Stall);
            market.AddChild(SettlementBlockKind.Building, 1, SettlementBlockSubtype.Exchange);

            var snapshot = SettlementEconomy.Read(settlement);
            Assert.IsTrue(snapshot.hasMarket);
            // 2*120 + 2*30 + 1*80 + 1000*0.05 + 20*2 + 30*0.5 = 240+60+80+50+40+15
            Assert.AreEqual(485f, snapshot.marketSize, 0.01f);
        }

        [Test]
        public void WarehouseAndWorkshopAndRoad_AreReadFromBlocks()
        {
            var settlement = new SettlementData();
            var warehouse = settlement.AddBlock(SettlementBlockKind.Warehouse, null, 2);
            warehouse.AddChild(SettlementBlockKind.Building, 1, SettlementBlockSubtype.Granary);
            warehouse.AddChild(SettlementBlockKind.Building, 1, SettlementBlockSubtype.Depot);

            var workshop = settlement.AddBlock(SettlementBlockKind.Workshop, null, 1);
            workshop.AddChild(SettlementBlockKind.Building, 1, SettlementBlockSubtype.Forge);
            workshop.AddChild(SettlementBlockKind.Building, 1, SettlementBlockSubtype.Kiln);

            var road = settlement.AddBlock(SettlementBlockKind.RoadNetwork, null, 2);
            road.AddChild(SettlementBlockKind.Building, 1, SettlementBlockSubtype.Bridge);

            var port = settlement.AddBlock(SettlementBlockKind.Port, null, 1);

            var snapshot = SettlementEconomy.Read(settlement);
            // 800*2 + 400 + 600 = 2600
            Assert.AreEqual(2600f, snapshot.warehouseCapacity, 0.01f);
            // 1 + forge + kiln = 3
            Assert.AreEqual(3, snapshot.productionSlots);
            // road 2*.2 + bridge .1 + port 1*.25 = .75
            Assert.AreEqual(0.75f, snapshot.tradeConnectivity, 0.01f);
        }

        [Test]
        public void MarketInsideDistrict_IsFoundRecursively()
        {
            var settlement = new SettlementData();
            var district = settlement.AddBlock(SettlementBlockKind.District, null, 1);
            district.districtType = CityDistrictType.Commercial;
            var market = settlement.AddBlock(SettlementBlockKind.Market, district, 1);
            market.AddChild(SettlementBlockKind.Building, 1, SettlementBlockSubtype.Stall);

            Assert.IsTrue(SettlementEconomy.Read(settlement).hasMarket);
            // 1*120 + stall 30 = 150（无人口/发展/贸易加成）
            Assert.AreEqual(150f, SettlementEconomy.GetMarketSize(settlement), 0.01f);
        }

        [Test]
        public void TradeConnectivity_IsClamped()
        {
            var settlement = new SettlementData();
            var road = settlement.AddBlock(SettlementBlockKind.RoadNetwork, null, 10);
            for (int i = 0; i < 20; i++)
                road.AddChild(SettlementBlockKind.Building, 1, SettlementBlockSubtype.Bridge);

            Assert.AreEqual(1.5f, SettlementEconomy.GetTradeConnectivity(settlement), 0.01f);
        }
    }
}
