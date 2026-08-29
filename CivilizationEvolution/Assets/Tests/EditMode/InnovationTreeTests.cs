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
        public void InnovationTree_OriginalEra0_FromPrimitiveStart()
        {
            // 批 A：era 0 原始起点（从最原始的技术开始）
            var fire = _tree.GetInnovation(900);
            Assert.IsNotNull(fire, "用火应存在");
            Assert.AreEqual(0, fire.era, "用火应为 era0");
            Assert.AreEqual(InnovationDomain.Technology, fire.Domain, "用火→技术");

            Assert.AreEqual(0, _tree.GetInnovation(902).era, "石器打制 era0");
            Assert.AreEqual(0, _tree.GetInnovation(911).era, "狗的驯化 era0");
            Assert.AreEqual(0, _tree.GetInnovation(914).era, "赭石珠饰 era0");

            // 陶器/独木舟降入 era0（全球最早实证：仙人洞 2 万年 / Pesse 前 8000 年）
            Assert.AreEqual(0, _tree.GetInnovation(200).era, "陶器 era0（东亚最早）");
            Assert.AreEqual(0, _tree.GetInnovation(400).era, "独木舟 era0");

            // 人类学分期起点数
            int era0Count = 0;
            foreach (var kv in _tree.GetAllInnovations())
                if (kv.Value.era == 0) era0Count++;
            Assert.That(era0Count, Is.GreaterThanOrEqualTo(15), "era0 原始起点应 ≥15 项");
        }

        [Test]
        public void InnovationTree_HorseChain_Complete()
        {
            // 骑兵链：马的驯化→骑乘术→马镫→重装骑兵（Botai 实证链）
            Assert.AreEqual(1, _tree.GetInnovation(922).era, "马的驯化 era1");
            Assert.IsTrue(_tree.GetInnovation(923).prerequisites.Contains(922), "骑乘术前置马驯化");
            Assert.IsTrue(_tree.GetInnovation(924).prerequisites.Contains(923), "马镫前置骑乘");
            Assert.IsTrue(_tree.GetInnovation(924).prerequisites.Contains(301), "马镫前置铁制武器");
            Assert.IsTrue(_tree.GetInnovation(303).prerequisites.Contains(924), "重装骑兵前置马镫");
            Assert.IsTrue(_tree.GetInnovation(302).prerequisites.Contains(923), "骑兵战术前置骑乘");
        }

        [Test]
        public void InnovationTree_GlobalAgriculture_ThreeCrops()
        {
            // 全球驯化中心分列：粟黍（华北）/稻作（长江）/麦类（新月沃地）
            Assert.IsNotNull(_tree.GetInnovation(916), "粟黍旱作应存在");
            Assert.IsNotNull(_tree.GetInnovation(917), "稻作水田应存在");
            Assert.IsNotNull(_tree.GetInnovation(918), "麦类耕作应存在");
            Assert.AreEqual(1, _tree.GetInnovation(916).era, "农业 era1");
            Assert.IsTrue(_tree.GetInnovation(916).prerequisites.Contains(100), "粟黍前置刀耕火种");
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
