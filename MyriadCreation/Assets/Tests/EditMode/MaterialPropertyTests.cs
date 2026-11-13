using System.Collections.Generic;
using NUnit.Framework;
using MyriadCreation.Content.Modding;
using MyriadCreation.Simulation.ContentHost;
using MyriadCreation.Simulation.State;
using MyriadCreation.Content.Definitions;

namespace MyriadCreation.Tests.EditMode
{
    /// <summary>
    /// 数值型工艺能力（如最大炉温）：加法 + 乘法修正、门槛阻塞、内容闭包可满足。
    /// </summary>
    public class MaterialPropertyTests
    {
        private InnovationTree _tree;

        [SetUp]
        public void Setup()
        {
            ContentRegistry.Reset();
            ContentRegistry.Initialize();
            _tree = new InnovationTree();
        }

        private InnovationDef Register(int id, System.Action<InnovationDef> configure)
        {
            var def = new InnovationDef
            {
                innovationId = id,
                innovationName = "测试能力" + id,
                field = InnovationField.Craft,
                era = 1,
                researchCost = 10f,
            };
            configure?.Invoke(def);
            _tree.RegisterInnovation(def);
            return def;
        }

        private static void Complete(InnovationTree tree, int id)
        {
            var def = tree.GetInnovation(id);
            Assert.IsNotNull(def, $"革新 {id} 不存在");
            foreach (int pre in def.GetPrerequisiteIds())
                if (!tree.HasInnovation(1, pre)) Complete(tree, pre);
            Assert.IsTrue(tree.StartResearch(1, id), $"革新 {id} 应可开始");
            tree.DailyTick(1, 100000f);
            Assert.IsTrue(tree.HasInnovation(1, id), $"革新 {id} 应完成");
        }

        [Test]
        public void NumericCapability_AddAndMultiply()
        {
            Register(9300, d => d.capabilityModifiers = new List<CapabilityModifier>
            {
                new CapabilityModifier { channel = CapabilityChannels.HeatMaxTemperature, add = 700f }
            });
            Register(9301, d => d.capabilityModifiers = new List<CapabilityModifier>
            {
                new CapabilityModifier { channel = CapabilityChannels.HeatMaxTemperature, multiply = 1.25f }
            });
            Register(9302, d => d.requiredCapabilityValues = new List<CapabilityRequirement>
            {
                new CapabilityRequirement { channel = CapabilityChannels.HeatMaxTemperature, minValue = 800f }
            });

            Assert.AreEqual(0f, _tree.GetCapabilityValue(1, CapabilityChannels.HeatMaxTemperature), 0.01f);
            Complete(_tree, 9300);
            Assert.AreEqual(700f, _tree.GetCapabilityValue(1, CapabilityChannels.HeatMaxTemperature), 0.01f);
            Complete(_tree, 9301);
            Assert.AreEqual(875f, _tree.GetCapabilityValue(1, CapabilityChannels.HeatMaxTemperature), 0.01f);
            Assert.IsTrue(_tree.StartResearch(1, 9302), "(700+0)*1.25=875 ≥ 800 应可研究");
        }

        [Test]
        public void NumericCapability_BlocksBelowThreshold()
        {
            Register(9310, d => d.requiredCapabilityValues = new List<CapabilityRequirement>
            {
                new CapabilityRequirement { channel = CapabilityChannels.HeatMaxTemperature, minValue = 1000f }
            });
            Register(9311, d => d.capabilityModifiers = new List<CapabilityModifier>
            {
                new CapabilityModifier { channel = CapabilityChannels.HeatMaxTemperature, add = 700f }
            });
            Complete(_tree, 9311);
            Assert.AreEqual(700f, _tree.GetCapabilityValue(1, CapabilityChannels.HeatMaxTemperature), 0.01f);
            Assert.IsFalse(_tree.StartResearch(1, 9310), "700 < 1000 不应可研究");
        }

        [Test]
        public void IronChain_ReachesIronRequirement()
        {
            Complete(_tree, 202);
            float heat = _tree.GetCapabilityValue(1, CapabilityChannels.HeatMaxTemperature);
            Assert.GreaterOrEqual(heat, 1400f, "冶铁链应达到铁的冶炼温度需求");
        }

        [Test]
        public void Content_NumericRequirements_AreSatisfiableByPrerequisiteClosure()
        {
            var innovations = ContentRegistry.Innovations;
            HashSet<int> Closure(int id)
            {
                var result = new HashSet<int>();
                void Walk(int current)
                {
                    foreach (int pre in innovations[current].GetPrerequisiteIds())
                        if (result.Add(pre)) Walk(pre);
                }
                Walk(id);
                return result;
            }
            float ValueOf(IEnumerable<int> ids)
            {
                float add = 0f, multiply = 1f;
                foreach (int id in ids)
                {
                    if (innovations[id].capabilityModifiers == null) continue;
                    foreach (var mod in innovations[id].capabilityModifiers)
                    {
                        if (mod.channel != CapabilityChannels.HeatMaxTemperature) continue;
                        add += mod.add;
                        if (mod.multiply > 0f) multiply *= mod.multiply;
                    }
                }
                return add * multiply;
            }
            foreach (var pair in innovations)
            {
                if (pair.Value.requiredCapabilityValues == null
                    || pair.Value.requiredCapabilityValues.Count == 0) continue;
                float available = ValueOf(Closure(pair.Key));
                foreach (var requirement in pair.Value.requiredCapabilityValues)
                {
                    if (requirement.channel != CapabilityChannels.HeatMaxTemperature) continue;
                    Assert.GreaterOrEqual(available, requirement.minValue,
                        $"革新 {pair.Key} 的数值门槛无法由前置链满足");
                }
            }
        }

        [Test]
        public void Content_InnovationAssociatedGoods_HaveProperties()
        {
            foreach (var pair in ContentRegistry.Innovations)
            {
                if (pair.Value.associatedGoods == null) continue;
                foreach (int goodsId in pair.Value.associatedGoods)
                {
                    Assert.IsTrue(GoodsPropertyCatalog.TryGet(goodsId, out var properties) && properties.Count > 0,
                        $"革新 {pair.Key} 关联的物产 {goodsId} 没有物理属性");
                }
            }
        }
    }
}
