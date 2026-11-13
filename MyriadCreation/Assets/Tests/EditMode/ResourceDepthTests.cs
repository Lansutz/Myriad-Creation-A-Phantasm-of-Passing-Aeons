using System.Collections.Generic;
using NUnit.Framework;
using MyriadCreation.Core.Data;
using MyriadCreation.Simulation.Generation;
using MyriadCreation.Simulation.ContentHost;
using MyriadCreation.Simulation.State;
using MyriadCreation.Simulation.Systems;
using MyriadCreation.Content.Definitions;

namespace MyriadCreation.Tests.EditMode
{
    /// <summary>资源点深度（露天/坑采/深井）与“存在但不可采”判定。</summary>
    public class ResourceDepthTests
    {
        private InnovationTree _tree;

        [SetUp]
        public void Setup()
        {
            ContentRegistry.Reset();
            ContentRegistry.Initialize();
            _tree = new InnovationTree();
        }

        private static GoodsDef MineralGoods(float surface, float shaft)
        {
            return new GoodsDef
            {
                goodsId = 500,
                goodsName = "测试矿",
                resourceType = ResourceType.Mineral,
                isNaturalResource = true,
                requiredInnovation = -1,
                surfaceDepthShare = surface,
                shaftDepthShare = shaft,
            };
        }

        private void Complete(int id)
        {
            var def = _tree.GetInnovation(id);
            Assert.IsNotNull(def, $"革新 {id} 不存在");
            foreach (int pre in def.GetPrerequisiteIds())
                if (!_tree.HasInnovation(1, pre)) Complete(pre);
            Assert.IsTrue(_tree.StartResearch(1, id), $"革新 {id} 应可开始");
            _tree.DailyTick(1, 100000f);
        }

        private InnovationDef RegisterCapabilityProvider(int id, string capability)
        {
            var def = new InnovationDef
            {
                innovationId = id,
                innovationName = "测试能力" + id,
                field = InnovationField.Mining,
                researchCost = 10f,
                providesCapabilities = new List<string> { capability },
            };
            _tree.RegisterInnovation(def);
            return def;
        }

        [Test]
        public void RollDepth_RespectsShares()
        {
            var goods = MineralGoods(0.6f, 0.3f);
            Assert.AreEqual(ResourceAccessDepth.Surface, ResourceGenerator.RollDepth(goods, 0.1f));
            Assert.AreEqual(ResourceAccessDepth.Surface, ResourceGenerator.RollDepth(goods, 0.59f));
            Assert.AreEqual(ResourceAccessDepth.Shaft, ResourceGenerator.RollDepth(goods, 0.6f));
            Assert.AreEqual(ResourceAccessDepth.Shaft, ResourceGenerator.RollDepth(goods, 0.89f));
            Assert.AreEqual(ResourceAccessDepth.Deep, ResourceGenerator.RollDepth(goods, 0.95f));

            var forest = new GoodsDef { resourceType = ResourceType.Forest, surfaceDepthShare = 0f };
            Assert.AreEqual(ResourceAccessDepth.Surface, ResourceGenerator.RollDepth(forest, 0.99f),
                "非矿物固定露天");
        }

        [Test]
        public void DepthAccessibility_UsesMiningCapabilities()
        {
            Assert.IsTrue(ResourceAccess.IsDepthAccessible(ResourceAccessDepth.Surface, new HashSet<string>()));
            Assert.IsFalse(ResourceAccess.IsDepthAccessible(ResourceAccessDepth.Shaft, new HashSet<string>()));
            Assert.IsFalse(ResourceAccess.IsDepthAccessible(ResourceAccessDepth.Deep, new HashSet<string>()));

            var shaftCaps = new HashSet<string> { ResourceAccess.MiningShaftCapability };
            Assert.IsTrue(ResourceAccess.IsDepthAccessible(ResourceAccessDepth.Shaft, shaftCaps));
            Assert.IsFalse(ResourceAccess.IsDepthAccessible(ResourceAccessDepth.Deep, shaftCaps));

            var deepCaps = new HashSet<string> { ResourceAccess.MiningDeepCapability };
            Assert.IsTrue(ResourceAccess.IsDepthAccessible(ResourceAccessDepth.Deep, deepCaps));
        }

        [Test]
        public void CanDevelop_RequiresDiscoveryTechAndDepth()
        {
            RegisterCapabilityProvider(9401, ResourceAccess.MiningShaftCapability);
            RegisterCapabilityProvider(9402, ResourceAccess.MiningDeepCapability);
            _tree.RegisterInnovation(new InnovationDef
            {
                innovationId = 9400,
                innovationName = "测试采矿前置",
                field = InnovationField.Mining,
                researchCost = 10f,
            });
            var goods = MineralGoods(0.5f, 0.3f);
            goods.requiredInnovation = 9400;
            var resource = TileResource.Create(goods.goodsId, 0.5f, ResourceAccessDepth.Surface);

            Assert.IsFalse(ResourceAccess.CanDevelop(goods, resource, _tree, 1), "未发现不可开发");
            resource.discovered = true;
            Assert.IsFalse(ResourceAccess.CanDevelop(goods, resource, _tree, 1), "缺前置革新不可开发");

            Complete(9400);
            Assert.IsTrue(ResourceAccess.CanDevelop(goods, resource, _tree, 1), "露天+技术满足可开发");

            resource.accessDepth = ResourceAccessDepth.Shaft;
            Assert.IsFalse(ResourceAccess.CanDevelop(goods, resource, _tree, 1), "坑采需要坑采能力");
            Complete(9401);
            Assert.IsTrue(ResourceAccess.CanDevelop(goods, resource, _tree, 1), "掌握坑采后可开发");

            resource.accessDepth = ResourceAccessDepth.Deep;
            Assert.IsFalse(ResourceAccess.CanDevelop(goods, resource, _tree, 1), "深井需要深井能力");
            Complete(9402);
            Assert.IsTrue(ResourceAccess.CanDevelop(goods, resource, _tree, 1), "掌握深井后可开发");
        }

        [Test]
        public void DescribeStatus_ShowsLockedReason()
        {
            var goods = MineralGoods(0.5f, 0.3f);
            var resource = TileResource.Create(goods.goodsId, 0.5f, ResourceAccessDepth.Deep);
            Assert.AreEqual("未发现", ResourceAccess.DescribeStatus(goods, resource, _tree, 1));

            resource.discovered = true;
            string status = ResourceAccess.DescribeStatus(goods, resource, _tree, 1);
            StringAssert.Contains("存在但不可采", status);
            StringAssert.Contains("深井", status);
        }

        [Test]
        public void Content_MiningCapabilitiesProvidedByMiningInnovations()
        {
            Assert.IsTrue(ContentRegistry.Innovations.TryGetValue(802, out var openPit));
            CollectionAssert.Contains(openPit.providesCapabilities, "mining.surface");
            Assert.IsTrue(ContentRegistry.Innovations.TryGetValue(803, out var deepShaft));
            CollectionAssert.Contains(deepShaft.providesCapabilities, ResourceAccess.MiningShaftCapability);
            CollectionAssert.Contains(deepShaft.providesCapabilities, ResourceAccess.MiningDeepCapability);
        }
    }
}
