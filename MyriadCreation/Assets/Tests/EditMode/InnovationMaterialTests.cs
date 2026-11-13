using System.Collections.Generic;
using NUnit.Framework;
using MyriadCreation.Simulation.ContentHost;
using MyriadCreation.Simulation.State;
using MyriadCreation.Content.Definitions;

namespace MyriadCreation.Tests.EditMode
{
    /// <summary>
    /// 革新系统第二批：工艺能力硬门槛、材料属性软加成、时代软权重。
    /// </summary>
    public class InnovationMaterialTests
    {
        private InnovationTree _tree;

        [SetUp]
        public void Setup()
        {
            ContentRegistry.Reset();
            ContentRegistry.Initialize();
            _tree = new InnovationTree();
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

        private InnovationDef Register(int id, System.Action<InnovationDef> configure)
        {
            var def = new InnovationDef
            {
                innovationId = id,
                innovationName = "测试革新" + id,
                field = InnovationField.Craft,
                era = 1,
                researchCost = 10f,
            };
            configure?.Invoke(def);
            _tree.RegisterInnovation(def);
            return def;
        }

        [Test]
        public void CapabilityGate_BlocksUntilProviderKnown()
        {
            Register(9100, d => d.providesCapabilities = new List<string> { "test.cap" });
            Register(9101, d => d.requiredCapabilities = new List<string> { "test.cap" });

            Assert.IsFalse(_tree.StartResearch(1, 9101), "没有能力提供者时不应可研究");
            Complete(_tree, 9100);
            Assert.IsTrue(_tree.GetRealmCapabilities(1).Contains("test.cap"), "提供者应授予能力");
            Assert.IsTrue(_tree.StartResearch(1, 9101), "掌握能力提供者后应可研究");
        }

        [Test]
        public void Content_CapabilityProviders_ExistInPrerequisiteClosure()
        {
            var innovations = ContentRegistry.Innovations;
            var providers = new Dictionary<string, List<int>>();
            foreach (var pair in innovations)
            {
                if (pair.Value.providesCapabilities == null) continue;
                foreach (var cap in pair.Value.providesCapabilities)
                {
                    if (string.IsNullOrEmpty(cap)) continue;
                    if (!providers.TryGetValue(cap, out var list))
                        providers[cap] = list = new List<int>();
                    list.Add(pair.Key);
                }
            }
            Assert.Greater(providers.Count, 0, "内容应至少提供一项工艺能力");

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

            foreach (var pair in innovations)
            {
                var def = pair.Value;
                if (def.requiredCapabilities == null || def.requiredCapabilities.Count == 0) continue;
                var closure = Closure(pair.Key);
                foreach (var cap in def.requiredCapabilities)
                {
                    bool satisfied = providers.TryGetValue(cap, out var list)
                        && list.Exists(provider => provider != pair.Key && closure.Contains(provider));
                    Assert.IsTrue(satisfied,
                        $"革新 {pair.Key}({def.innovationName}) 需要能力 {cap}，但前置链里没有提供者");
                }
            }
        }

        [Test]
        public void SoftPrerequisite_IsCapabilityThreshold_NotSpecificTech()
        {
            Register(9200, d => d.capabilityModifiers = new List<CapabilityModifier>
            {
                new CapabilityModifier { channel = CapabilityChannels.HeatMaxTemperature, add = 700f }
            });
            Register(9202, d => d.capabilityModifiers = new List<CapabilityModifier>
            {
                new CapabilityModifier { channel = CapabilityChannels.HeatMaxTemperature, add = 700f }
            });
            var target = Register(9201, d =>
            {
                d.capabilityModifiers = new List<CapabilityModifier>
                {
                    new CapabilityModifier { channel = CapabilityChannels.HeatMaxTemperature, add = 700f }
                };
                d.softPrerequisites = new List<InnovationSoftPrerequisite>
                {
                    new InnovationSoftPrerequisite
                    {
                        goodsId = 50,
                        propertyId = "meltingPoint",
                        capabilityChannel = CapabilityChannels.HeatMaxTemperature,
                        propertyFactor = 0.9f,
                    }
                };
            });

            Complete(_tree, 9200);
            Assert.IsFalse(_tree.StartResearch(1, 9201), "只有 700 度时，铁熔点软门槛不应满足");

            Complete(_tree, 9202);
            Assert.AreEqual(1400f, _tree.GetCapabilityValue(1, CapabilityChannels.HeatMaxTemperature), 0.01f);
            Assert.IsTrue(_tree.StartResearch(1, 9201), "总最大温度达到铁熔点阈值后应可研究；不需要特定前置科技");
        }

        [Test]
        public void Era_SoftWeight_PrefersCurrentEra_WithoutLocking()
        {
            Assert.AreEqual(1.2f, InnovationEra.GetPreferenceWeight(3, 3), 0.001f);
            Assert.AreEqual(1.1f, InnovationEra.GetPreferenceWeight(2, 3), 0.001f);
            Assert.AreEqual(1.0f, InnovationEra.GetPreferenceWeight(1, 3), 0.001f);
            Assert.AreEqual(0.8f, InnovationEra.GetPreferenceWeight(4, 3), 0.001f);
            Assert.AreEqual(0.6f, InnovationEra.GetPreferenceWeight(5, 3), 0.001f);

            Assert.AreEqual(1, InnovationEra.GetEraForYear(-5000));
            Assert.AreEqual(2, InnovationEra.GetEraForYear(-3000));
            Assert.AreEqual(3, InnovationEra.GetEraForYear(0));
            Assert.AreEqual(4, InnovationEra.GetEraForYear(1000));
            Assert.AreEqual(5, InnovationEra.GetEraForYear(1900));
        }

        [Test]
        public void IronChain_Completes_WithCapabilityGate()
        {
            Complete(_tree, 202);
            Assert.IsTrue(_tree.HasInnovation(1, 202), "冶铁术应能沿自身前置链完成（无青铜硬前置）");
            Assert.IsTrue(_tree.GetRealmCapabilities(1).Contains("heat.high"), "冶铁术后应获得高温能力");
        }
    }
}
