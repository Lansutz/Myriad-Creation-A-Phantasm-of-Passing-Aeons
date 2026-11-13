using System.Collections.Generic;
using NUnit.Framework;
using MyriadCreation.Core.Enums;
using MyriadCreation.Simulation.Economy;
using MyriadCreation.Simulation.ContentHost;
using MyriadCreation.Simulation.Systems;
using MyriadCreation.World.Settlement;
using MyriadCreation.Content.Definitions;

namespace MyriadCreation.Tests.EditMode
{
    /// <summary>
    /// 运行时自然产生的新产品：合金/合成物自动注册、带属性与配方、可进入市场、可存档。
    /// </summary>
    public class DynamicGoodsTests
    {
        [SetUp]
        public void Setup()
        {
            ContentRegistry.Reset();
            ContentRegistry.Initialize();
            DynamicGoodsRegistry.Clear();
        }

        [TearDown]
        public void TearDown()
        {
            DynamicGoodsRegistry.Clear();
            ContentRegistry.Reset();
        }

        private static GoodsDef Metal(int id, string name, float baseValue, float weight,
            float melt, float hardness, float toughness, float ductility)
        {
            return new GoodsDef
            {
                goodsId = id,
                goodsName = name,
                category = GameEnums.GoodsCategory.Metal,
                baseValue = baseValue,
                weight = weight,
                properties = new List<GoodsPropertyValue>
                {
                    new GoodsPropertyValue { id = "meltingPoint", value = melt },
                    new GoodsPropertyValue { id = "hardness", value = hardness },
                    new GoodsPropertyValue { id = "toughness", value = toughness },
                    new GoodsPropertyValue { id = "ductility", value = ductility },
                },
            };
        }

        private static (GoodsDef copper, GoodsDef tin) BronzeInputs()
            => (Metal(53, "铜", 5f, 0.15f, 1085f, 3f, 6f, 7.5f),
                Metal(55, "锡", 8f, 0.1f, 232f, 1.5f, 2f, 6f));

        [Test]
        public void Alloy_IsAutoRegistered_WithWeightedPropertiesAndName()
        {
            var (copper, tin) = BronzeInputs();
            Assert.IsTrue(DynamicGoodsRegistry.TryCreateAlloy(
                new[] { copper, tin }, new[] { 0.8f, 0.2f }, hasAlloyTech: true, out int id));

            Assert.GreaterOrEqual(id, DynamicGoodsRegistry.FirstDynamicGoodsId);
            Assert.IsTrue(DynamicGoodsRegistry.TryGet(id, out var created));
            StringAssert.Contains("铜", created.goodsName);
            StringAssert.Contains("锡", created.goodsName);
            StringAssert.Contains("合金", created.goodsName);

            Assert.IsTrue(created.TryGetProperty("meltingPoint", out float melt));
            Assert.AreEqual(1085f * 0.8f + 232f * 0.2f, melt, 0.01f);
            Assert.AreEqual(5f * 0.8f + 8f * 0.2f, created.baseValue, 0.01f);
            Assert.AreEqual(0.15f * 0.8f + 0.1f * 0.2f, created.weight, 0.001f);

            Assert.IsTrue(DynamicGoodsRegistry.TryGetRecipe(id, out var recipe));
            Assert.AreEqual(2, recipe.inputs.Count);
        }

        [Test]
        public void Alloy_RequiresTech_AndAtLeastTwoMetals()
        {
            var (copper, tin) = BronzeInputs();
            Assert.IsFalse(DynamicGoodsRegistry.TryCreateAlloy(
                new[] { copper, tin }, new[] { 0.8f, 0.2f }, hasAlloyTech: false, out _));

            Assert.IsFalse(DynamicGoodsRegistry.TryCreateAlloy(
                new[] { copper }, new[] { 1f }, hasAlloyTech: true, out _));

            var food = new GoodsDef
            {
                goodsId = 0, goodsName = "粮食", category = GameEnums.GoodsCategory.Food,
                baseValue = 1f, weight = 0.1f,
            };
            Assert.IsFalse(DynamicGoodsRegistry.TryCreateAlloy(
                new[] { copper, food }, new[] { 0.5f, 0.5f }, hasAlloyTech: true, out _),
                "非金属不能算合金");
        }

        [Test]
        public void Alloy_CanBeNamed()
        {
            var (copper, tin) = BronzeInputs();
            Assert.IsTrue(DynamicGoodsRegistry.TryCreateAlloy(
                new[] { copper, tin }, new[] { 0.8f, 0.2f }, true, out int id, nameOverride: "星辉合金"));
            Assert.IsTrue(DynamicGoodsRegistry.TryGet(id, out var created));
            Assert.AreEqual("星辉合金", created.goodsName);
        }

        [Test]
        public void Alloy_CanBeCraftedFromMarketStocks()
        {
            var (copper, tin) = BronzeInputs();
            DynamicGoodsRegistry.TryCreateAlloy(new[] { copper, tin }, new[] { 0.8f, 0.2f }, true, out int id);

            var market = new SettlementMarket(capacity: 100f);
            market.AddStock(53, 10f);
            market.AddStock(55, 10f);

            Assert.IsTrue(SettlementProduction.TryCraft(market, id, out float produced));
            Assert.AreEqual(1f, produced, 0.001f);
            Assert.AreEqual(9.2f, market.GetStock(53), 0.001f);
            Assert.AreEqual(9.8f, market.GetStock(55), 0.001f);
            Assert.AreEqual(1f, market.GetStock(id), 0.001f);
        }

        [Test]
        public void DynamicGoods_RoundTripSaveData()
        {
            var (copper, tin) = BronzeInputs();
            DynamicGoodsRegistry.TryCreateAlloy(new[] { copper, tin }, new[] { 0.8f, 0.2f }, true, out int id);
            var captured = DynamicGoodsRegistry.Capture();
            Assert.AreEqual(1, captured.Count);

            DynamicGoodsRegistry.Clear();
            Assert.IsFalse(DynamicGoodsRegistry.TryGet(id, out _));

            DynamicGoodsRegistry.Restore(captured);
            Assert.IsTrue(DynamicGoodsRegistry.TryGet(id, out var restored));
            Assert.AreEqual("铜-锡合金", restored.goodsName);
            Assert.IsTrue(DynamicGoodsRegistry.TryGetRecipe(id, out var recipe));
            Assert.AreEqual(id, recipe.outputGoodsId);
        }
    }
}
