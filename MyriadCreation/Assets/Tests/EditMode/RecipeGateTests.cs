using System.Collections.Generic;
using NUnit.Framework;
using MyriadCreation.Content.Definitions;
using MyriadCreation.Simulation.ContentHost;
using MyriadCreation.Simulation.State;
using MyriadCreation.Simulation.Systems;
using MyriadCreation.World.Settlement;

namespace MyriadCreation.Tests.EditMode
{
    /// <summary>配方门禁：需科技/设施时，缺了不能合成；旧三参调用不被误锁。</summary>
    public class RecipeGateTests
    {
        private InnovationTree _tree;
        private SettlementMarket _market;

        [SetUp]
        public void Setup()
        {
            ContentRegistry.Reset();
            ContentRegistry.Initialize();
            _tree = new InnovationTree();
            _market = new SettlementMarket(100f);
            _market.inventory[50] = 10f; // 铁矿
        }

        private void Complete(int id)
        {
            var def = _tree.GetInnovation(id);
            Assert.IsNotNull(def, "革新 " + id + " 不存在");
            foreach (int pre in def.GetPrerequisiteIds())
                if (!_tree.HasInnovation(1, pre)) Complete(pre);
            Assert.IsTrue(_tree.StartResearch(1, id), "应可开始 " + id);
            _tree.DailyTick(1, 100000f);
            Assert.IsTrue(_tree.HasInnovation(1, id));
        }

        [Test]
        public void IronRecipe_BlockedUntil202()
        {
            Assert.IsFalse(SettlementProduction.TryCraft(_market, 52, out _, _tree, 1, null),
                "未掌握块炼铁时不能炼铁");
            Complete(202);
            Assert.IsTrue(SettlementProduction.TryCraft(_market, 52, out float produced, _tree, 1, null));
            Assert.AreEqual(1f, produced, 0.001f);
        }

        [Test]
        public void OldThreeArgOverload_StillWorks()
        {
            Assert.IsTrue(SettlementProduction.TryCraft(_market, 52, out var produced));
            Assert.AreEqual(1f, produced, 0.001f);
        }

        [Test]
        public void RequiredBuilding_CanBlock()
        {
            Assert.IsFalse(SettlementProduction.TryCraft(_market, 52, out _, _tree, 1, id => false),
                "设施不满足应阻断");
        }
    }
}
