using System.Collections.Generic;
using NUnit.Framework;
using CivilizationEvolution.Core;
using CivilizationEvolution.Tech;

namespace CivilizationEvolution.Tests
{
    /// <summary>
    /// 革新系统 EditMode 测试（2026-08-30 重构：两级分类 + 数据驱动）
    /// 真实加载 Assets/StreamingAssets/Base/Innovation/Innovations.json
    /// </summary>
    public class InnovationTreeTests
    {
        private InnovationTree _tree;

        [SetUp]
        public void Setup()
        {
            ContentRegistry.Reset();
            Localization.Reset();
            Localization.Initialize("zh-Hans");
            ContentRegistry.Initialize();
            _tree = new InnovationTree();
        }

        // ===== 加载与分类完整性 =====

        [Test]
        public void InnovationTree_LoadsAllDefinitions()
        {
            Assert.That(_tree.GetAllInnovations().Count, Is.GreaterThanOrEqualTo(60), "革新定义应加载（66 条）");
        }

        [Test]
        public void InnovationTree_AllFields_MapToValidDomain()
        {
            // 每个革新的子类都能推导出合法大类（映射表完整性）
            foreach (var kv in _tree.GetAllInnovations())
            {
                var def = kv.Value;
                var domain = def.Domain;
                Assert.IsTrue(domain == InnovationDomain.Technology || domain == InnovationDomain.Thought
                    || domain == InnovationDomain.Institution || domain == InnovationDomain.Tradition,
                    $"革新 {def.innovationId} 的子类应映射到合法大类");
            }
        }

        [Test]
        public void InnovationTree_FourDomains_AllPopulated()
        {
            // 四分类每类都有内容（重构后传统类已补全）
            Assert.That(_tree.GetInnovationsByDomain(InnovationDomain.Technology).Count, Is.GreaterThan(20), "技术类应最丰富");
            Assert.That(_tree.GetInnovationsByDomain(InnovationDomain.Thought).Count, Is.GreaterThan(0), "思维类应有内容");
            Assert.That(_tree.GetInnovationsByDomain(InnovationDomain.Institution).Count, Is.GreaterThan(5), "制度类应有内容");
            Assert.That(_tree.GetInnovationsByDomain(InnovationDomain.Tradition).Count, Is.GreaterThan(5), "传统类应已补全");
        }

        [Test]
        public void InnovationTree_ReclassifiedContent_InPlace()
        {
            // 重构归位抽查：原农业→技术-农耕、原军事武器→技术-器械、骑兵战术→思维-兵学、
            // 封建制度→制度-政制、文字→思维-文字、大学→制度-教育
            Assert.AreEqual(InnovationField.Agriculture, _tree.GetInnovation(100).field, "刀耕火种→农耕");
            Assert.AreEqual(InnovationDomain.Technology, _tree.GetInnovation(100).Domain);
            Assert.AreEqual(InnovationField.Machinery, _tree.GetInnovation(300).field, "青铜武器→器械");
            Assert.AreEqual(InnovationField.MilitaryThought, _tree.GetInnovation(302).field, "骑兵战术→兵学（思维）");
            Assert.AreEqual(InnovationDomain.Thought, _tree.GetInnovation(302).Domain);
            Assert.AreEqual(InnovationField.Governance, _tree.GetInnovation(501).field, "封建制度→政制");
            Assert.AreEqual(InnovationField.Script, _tree.GetInnovation(600).field, "文字→思维-文字");
            Assert.AreEqual(InnovationField.Education, _tree.GetInnovation(603).field, "大学→制度-教育");
        }

        [Test]
        public void InnovationTree_TraditionSupplemented_WithNewContent()
        {
            // 补全内容在位：传统类 8 个 + 技术空缺子类
            Assert.IsNotNull(_tree.GetInnovation(809), "宗族祭祀（传统-仪礼）应存在");
            Assert.IsNotNull(_tree.GetInnovation(811), "岁时节庆（传统-岁时）应存在");
            Assert.IsNotNull(_tree.GetInnovation(815), "结绳纪事（传统-传承）应存在");
            Assert.IsNotNull(_tree.GetInnovation(802), "露天采掘（技术-采掘）应存在");
            Assert.IsNotNull(_tree.GetInnovation(804), "草药学（技术-医疗）应存在");
            Assert.IsNotNull(_tree.GetInnovation(807), "轮车（技术-交通）应存在");
        }

        // ===== 机制：前置链 / 研究流程 =====

        [Test]
        public void StartResearch_PrerequisiteChain_Enforced()
        {
            // 轮作制前置休耕制→刀耕火种：未研究前置时不可开始
            Assert.IsFalse(_tree.StartResearch(1, 102), "无前置革新时不应可研究");
            Assert.IsTrue(_tree.StartResearch(1, 100), "无前置的刀耕火种应可研究");

            _tree.DailyTick(1, 1000f); // 研究点充足，立即完成
            Assert.IsTrue(_tree.HasInnovation(1, 100), "刀耕火种应完成");

            Assert.IsTrue(_tree.StartResearch(1, 101), "前置满足后休耕制应可研究");
            _tree.DailyTick(1, 1000f);
            Assert.IsTrue(_tree.HasInnovation(1, 101));

            Assert.IsTrue(_tree.StartResearch(1, 102), "链上前置满足后轮作制应可研究");
        }

        [Test]
        public void DailyTick_ResearchProgress_Accumulates()
        {
            _tree.StartResearch(1, 100); // 刀耕火种 cost 100
            _tree.DailyTick(1, 50f);
            Assert.That(_tree.GetResearchProgress(1), Is.EqualTo(0.5f).Within(0.001f), "研究点应累积到 50%");
            _tree.DailyTick(1, 50f);
            Assert.IsTrue(_tree.HasInnovation(1, 100), "研究点达标应完成");
        }

        [Test]
        public void GetAvailableInnovations_ExcludesResearched()
        {
            _tree.StartResearch(1, 100);
            _tree.DailyTick(1, 1000f);
            var available = _tree.GetAvailableInnovations(1);
            Assert.IsFalse(available.Exists(i => i.innovationId == 100), "已研究革新不应出现在可研究列表");
            Assert.IsTrue(available.Exists(i => i.innovationId == 101), "前置满足的休耕制应可研究");
        }

        [Test]
        public void InnovationDef_LocalizedOrEmbedded()
        {
            var def = _tree.GetInnovation(100);
            Assert.IsNotNull(def, "刀耕火种定义应存在");
            Assert.AreEqual("刀耕火种", def.GetName(), "内嵌名称应可用");
            Assert.IsFalse(string.IsNullOrEmpty(def.GetDescription()), "描述应非空");
        }
    }
}
