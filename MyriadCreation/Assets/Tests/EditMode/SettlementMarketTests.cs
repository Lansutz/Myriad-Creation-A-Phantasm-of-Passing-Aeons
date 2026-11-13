using NUnit.Framework;
using MyriadCreation.Simulation.ContentHost;
using MyriadCreation.Simulation.Systems;
using MyriadCreation.World.Settlement;

namespace MyriadCreation.Tests.EditMode
{
    /// <summary>栖居点市场库存与生产：青铜多输入配方、容量、动态价格。</summary>
    public class SettlementMarketTests
    {
        [SetUp]
        public void Setup()
        {
            ContentRegistry.Reset();
            ContentRegistry.Initialize();
        }

        [TearDown]
        public void TearDown() => ContentRegistry.Reset();

        [Test]
        public void AddStock_RespectsCapacity()
        {
            var market = new SettlementMarket(capacity: 10f);
            Assert.IsTrue(market.AddStock(53, 6f));
            Assert.IsFalse(market.AddStock(55, 5f), "超出容量应拒绝");
            Assert.AreEqual(6f, market.TotalStock());
            Assert.AreEqual(6f, market.GetStock(53), 0.001f);
        }

        [Test]
        public void TryTake_DoesNotGoNegative()
        {
            var market = new SettlementMarket(capacity: 10f);
            market.AddStock(53, 5f);
            Assert.IsTrue(market.TryTake(53, 3f));
            Assert.AreEqual(2f, market.GetStock(53), 0.001f);
            Assert.IsFalse(market.TryTake(53, 3f), "库存不足应拒绝");
            Assert.AreEqual(2f, market.GetStock(53), 0.001f);
        }

        [Test]
        public void Price_RisesWithScarcity_FallsWithAbundance()
        {
            var scarce = new SettlementMarket(100f);
            scarce.AddStock(53, 10f);
            scarce.supply[53] = 10f;
            scarce.demand[53] = 10f;
            float scarcePrice = scarce.GetPrice(53, baseValue: 10f);

            var abundant = new SettlementMarket(100f);
            abundant.AddStock(53, 100f);
            abundant.supply[53] = 100f;
            abundant.demand[53] = 10f;
            float abundantPrice = abundant.GetPrice(53, baseValue: 10f);

            Assert.Greater(scarcePrice, abundantPrice, "本地越稀缺，价格越高");
            Assert.Greater(scarcePrice, 10f, "价格应围绕 baseValue 波动，不为固定值");
        }

        [Test]
        public void CraftBronze_ConsumesCopperAndTin()
        {
            var market = new SettlementMarket(capacity: 100f);
            market.AddStock(53, 10f); // 铜
            market.AddStock(55, 10f); // 锡

            Assert.IsTrue(SettlementProduction.TryCraft(market, 56, out float produced));
            Assert.AreEqual(1f, produced, 0.001f);
            Assert.AreEqual(9.2f, market.GetStock(53), 0.001f);
            Assert.AreEqual(9.8f, market.GetStock(55), 0.001f);
            Assert.AreEqual(1f, market.GetStock(56), 0.001f);
        }

        [Test]
        public void CraftBronze_WithoutTin_FailsAndConsumesNothing()
        {
            var market = new SettlementMarket(capacity: 100f);
            market.AddStock(53, 10f);

            Assert.IsFalse(SettlementProduction.TryCraft(market, 56, out _));
            Assert.AreEqual(10f, market.GetStock(53), 0.001f);
            Assert.AreEqual(0f, market.GetStock(56), 0.001f);
        }

        [Test]
        public void Craft_RespectsCapacity_WithoutConsumingInputs()
        {
            var market = new SettlementMarket(capacity: 20f);
            market.AddStock(53, 10f);
            market.AddStock(55, 10f);
            Assert.AreEqual(20f, market.TotalStock(), 0.001f);

            Assert.IsFalse(SettlementProduction.TryCraft(market, 56, out _), "成品会超容量，应拒绝");
            Assert.AreEqual(10f, market.GetStock(53), 0.001f);
            Assert.AreEqual(10f, market.GetStock(55), 0.001f);
            Assert.AreEqual(0f, market.GetStock(56), 0.001f);
        }
    }
}
