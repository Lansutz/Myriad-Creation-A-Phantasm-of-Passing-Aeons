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
        public void InnovationTree_MetallurgyChain_ResearchBased()
        {
            // 批 B：冶金链（研究实证）——鼓风链/钢链/贵金属
            Assert.IsNotNull(_tree.GetInnovation(927), "灰吹法应存在（Tepe Sialk 实证）");
            Assert.AreEqual(2, _tree.GetInnovation(927).era, "灰吹法 era2");
            Assert.IsTrue(_tree.GetInnovation(927).prerequisites.Contains(925), "灰吹法前置皮囊风箱");

            Assert.IsNotNull(_tree.GetInnovation(928), "失蜡法应存在（六中心独立发明）");
            Assert.IsNotNull(_tree.GetInnovation(929), "坩埚钢应存在（Kodumanal 实证）");
            Assert.IsTrue(_tree.GetInnovation(929).prerequisites.Contains(203), "坩埚钢前置炼钢");

            Assert.IsNotNull(_tree.GetInnovation(931), "生铁高炉应存在（中国最早实证）");
            Assert.IsTrue(_tree.GetInnovation(926).prerequisites.Contains(931), "活塞风箱前置高炉");

            Assert.IsNotNull(_tree.GetInnovation(933), "井盐深钻应存在（四川）");
            Assert.IsNotNull(_tree.GetInnovation(936), "瓷器应存在（小仙坛实证）");
            Assert.AreEqual(4, _tree.GetInnovation(936).era, "瓷器 era4");
        }

        [Test]
        public void InnovationTree_BureaucracyEightNodes_Complete()
        {
            // 批 C：官僚 8 节点（研究 N0-N7）——文书→档案→统计→驿传→选拔→监察→俸禄
            Assert.IsNotNull(_tree.GetInnovation(940), "档案典藏（N2）应存在");
            Assert.IsNotNull(_tree.GetInnovation(941), "上计制度（N3）应存在");
            Assert.IsNotNull(_tree.GetInnovation(942), "考课制度（N6）应存在");
            Assert.IsNotNull(_tree.GetInnovation(943), "御史监察（N6）应存在");
            Assert.IsNotNull(_tree.GetInnovation(944), "俸禄制度（N7）应存在");
            Assert.IsNotNull(_tree.GetInnovation(945), "九品中正（N5）应存在");

            // 科举链：官僚制度→九品中正→科举
            Assert.IsTrue(_tree.GetInnovation(945).prerequisites.Contains(503), "九品中正前置官僚制度");
            Assert.IsTrue(_tree.GetInnovation(504).prerequisites.Contains(945), "科举前置九品中正");
            Assert.AreEqual(4, _tree.GetInnovation(504).era, "科举 era4（隋唐）");

            // 监察链：上计→考课→御史
            Assert.IsTrue(_tree.GetInnovation(942).prerequisites.Contains(941), "考课前置上计");
            Assert.IsTrue(_tree.GetInnovation(943).prerequisites.Contains(942), "御史前置考课");
        }

        [Test]
        public void CultureAffinity_Laethis_Resolves()
        {
            // 文化包联动：Laethis 文化亲和 Agriculture/Craft/Script
            Assert.IsTrue(ContentRegistry.TryGetCulture(1, out var pack), "Laethis 文化应加载");
            var c = pack.data;
            Assert.IsTrue(c.HasInnovationAffinity("Agriculture"), "农耕亲和");
            Assert.IsTrue(c.HasInnovationAffinity("craft"), "工艺亲和（不区分大小写）");
            Assert.IsFalse(c.HasInnovationAffinity("Metallurgy"), "冶炼非亲和");
        }

        [Test]
        public void InnovationTree_AdministrativeDiversity_AlternativeForms()
        {
            // 差异化路径：记录载体三形态 + 无文字统计链 + 选拔多形态
            Assert.IsNotNull(_tree.GetInnovation(946), "泥板文书（两河）应存在");
            Assert.IsNotNull(_tree.GetInnovation(947), "莎草纸文书（埃及/地中海）应存在");
            Assert.IsNotNull(_tree.GetInnovation(948), "奇普结绳（安第斯，无文字）应存在");
            Assert.IsNotNull(_tree.GetInnovation(949), "奇普统计应存在");
            Assert.IsNotNull(_tree.GetInnovation(950), "巴里德驿站（阿拉伯）应存在");
            Assert.IsNotNull(_tree.GetInnovation(951), "书吏学校（埃及选拔）应存在");

            // 节点级文化标签
            Assert.IsTrue(_tree.GetInnovation(946).affinityTags.Contains("Clay"), "泥板文书带 Clay 标签");
            Assert.IsTrue(_tree.GetInnovation(948).affinityTags.Contains("Quipu"), "奇普带 Quipu 标签");

            // OR 前置：簿籍=简牍/泥板/莎草 任选；官僚=文书行政/奇普统计 任选
            Assert.IsTrue(_tree.GetInnovation(821).prerequisitesAny.Contains(946), "簿籍 OR 含泥板");
            Assert.IsTrue(_tree.GetInnovation(503).prerequisitesAny.Contains(949), "官僚 OR 含奇普统计");
        }

        [Test]
        public void InnovationTree_OrPrerequisites_QuipuPathReachesBureaucracy()
        {
            // 差异化验证：无文字路径（奇普链）也能到达官僚制度——仅完成奇普链不碰简牍链
            var tree = new InnovationTree();
            // 政制链 + 驿传的硬质道路链（807 轮车→808 硬质道路）
            CompleteTree(tree, 500);  // 部落联盟
            CompleteTree(tree, 501);  // 封建
            CompleteTree(tree, 807);  // 轮车
            CompleteTree(tree, 808);  // 硬质道路
            // 奇普统计链（无文字！）
            CompleteTree(tree, 915);  // 刻痕计数
            CompleteTree(tree, 948);  // 奇普结绳
            CompleteTree(tree, 949);  // 奇普统计
            // 集权链（无文字版本）：思想经奇普传承→郡县制→中央集权
            CompleteTree(tree, 958);  // 中央集权思想（OR [600 文字, 948 奇普]——奇普满足）
            CompleteTree(tree, 959);  // 郡县制
            // 驿传：OR [822 文书行政, 949 奇普统计]——奇普满足（印加 chasqui）
            CompleteTree(tree, 823);
            // 中央集权 → 官僚制度：OR [822, 949]——奇普满足
            CompleteTree(tree, 502);
            Assert.IsTrue(tree.StartResearch(1, 503), "奇普统计满足官僚制度 OR 前置，应可研究");
            // 全程未触碰简牍/文字链
            Assert.IsFalse(tree.HasInnovation(1, 600), "奇普路径不应需要文字");
            Assert.IsFalse(tree.HasInnovation(1, 820), "奇普路径不应需要简牍");
            Assert.IsFalse(tree.HasInnovation(1, 822), "奇普路径不应需要文书行政");
        }

        [Test]
        public void InnovationTree_GovernanceForms_ConceptLayers()
        {
            // 行政制度 v2：概念层次——思想（思维）vs 治理形态（制度）vs 结构（中央集权）
            Assert.IsNotNull(_tree.GetInnovation(958), "中央集权思想应存在（思维层）");
            Assert.AreEqual(InnovationDomain.Thought, _tree.GetInnovation(958).Domain, "集权思想→思维");

            Assert.IsNotNull(_tree.GetInnovation(959), "郡县制应存在（政制）");
            Assert.IsNotNull(_tree.GetInnovation(960), "行省制应存在（政制）");
            Assert.IsNotNull(_tree.GetInnovation(961), "包税制应存在（经制）");
            Assert.AreEqual(InnovationDomain.Institution, _tree.GetInnovation(959).Domain, "郡县制→制度");
            Assert.AreEqual(InnovationField.Economic, _tree.GetInnovation(961).field, "包税制→制度-经制");

            // 中央集权=思想+治理形态（OR：郡县/行省）
            Assert.IsTrue(_tree.GetInnovation(502).prerequisites.Contains(958), "中央集权需集权思想");
            Assert.IsTrue(_tree.GetInnovation(502).prerequisitesAny.Contains(959), "中央集权 OR 含郡县制");
            Assert.IsTrue(_tree.GetInnovation(502).prerequisitesAny.Contains(960), "中央集权 OR 含行省制");

            // 包税制=思想+铸币
            Assert.IsTrue(_tree.GetInnovation(961).prerequisites.Contains(701), "包税制前置铸币");
            Assert.IsTrue(_tree.GetInnovation(961).affinityTags.Contains("TaxFarming"), "包税制带 TaxFarming 标签");
        }

        [Test]
        public void InnovationTree_ManorLine_FeudalEconomicBase()
        {
            // 庄园线（Manorialism，Bloch《封建社会》）：封建制度→庄园制度→{法庭/公地/磨坊/劳役}
            Assert.IsNotNull(_tree.GetInnovation(952), "庄园制度应存在");
            Assert.IsTrue(_tree.GetInnovation(952).prerequisites.Contains(501), "庄园制度前置封建制度");
            Assert.IsTrue(_tree.GetInnovation(952).affinityTags.Contains("Manor"), "庄园带 Manor 标签");

            Assert.IsTrue(_tree.GetInnovation(953).prerequisites.Contains(952), "庄园法庭前置庄园");
            Assert.IsTrue(_tree.GetInnovation(954).prerequisites.Contains(952), "公地制度前置庄园");
            Assert.IsTrue(_tree.GetInnovation(955).prerequisites.Contains(952), "磨坊垄断前置庄园");
            Assert.IsTrue(_tree.GetInnovation(955).prerequisites.Contains(106), "磨坊垄断前置水磨");
            Assert.IsTrue(_tree.GetInnovation(956).prerequisites.Contains(952), "劳役地租前置庄园");

            Assert.IsNotNull(_tree.GetInnovation(957), "末日审判书应存在（1086 全英清丈）");
            Assert.IsTrue(_tree.GetInnovation(957).prerequisites.Contains(941), "末日审判书前置上计制度");
        }

        [Test]
        public void InnovationTree_StoneToolSpectrum_DeepChain()
        {
            // 旧石器技术谱系（研究 P2-P4）：打制→预制石核→石叶→细石器
            Assert.IsNotNull(_tree.GetInnovation(962), "预制石核（勒瓦娄哇）应存在");
            Assert.IsTrue(_tree.GetInnovation(962).prerequisites.Contains(902), "预制石核前置石器打制");
            Assert.IsTrue(_tree.GetInnovation(963).prerequisites.Contains(962), "石叶前置预制石核");
            Assert.IsTrue(_tree.GetInnovation(964).prerequisites.Contains(963), "细石器前置石叶");
            Assert.IsTrue(_tree.GetInnovation(965).prerequisites.Contains(906), "投矛器前置标枪");
            Assert.IsNotNull(_tree.GetInnovation(967), "历法计时应存在（Ishango 骨）");
            Assert.IsNotNull(_tree.GetInnovation(966), "岩画艺术应存在（Chauvet）");
        }

        [Test]
        public void InnovationTree_AdministrationForms_Worldwide()
        {
            // 行政新形态（全球比较）：民主/政事论/拜占庭/站赤/封泥/神庙档案/迪万/契约
            Assert.IsTrue(_tree.GetInnovation(980).affinityTags.Contains("Democracy"), "雅典议事带 Democracy 标签");
            Assert.IsTrue(_tree.GetInnovation(981).prerequisites.Contains(980), "陶片放逐前置雅典议事");
            Assert.IsTrue(_tree.GetInnovation(982).prerequisites.Contains(503), "政事论前置官僚制度");
            Assert.IsNotNull(_tree.GetInnovation(983), "拜占庭官僚应存在");
            Assert.IsTrue(_tree.GetInnovation(984).prerequisites.Contains(960), "蒙古站赤前置行省制");
            Assert.IsTrue(_tree.GetInnovation(985).prerequisitesAny.Contains(820), "封泥符券 OR 含简牍");
            Assert.IsTrue(_tree.GetInnovation(987).affinityTags.Contains("Diwan"), "迪万财政带 Diwan 标签");
            Assert.IsNotNull(_tree.GetInnovation(993), "契约文书应存在");
        }

        [Test]
        public void InnovationTree_Era5_PreModernExists()
        {
            // 前近代（era5）：机械钟首次出现
            Assert.IsNotNull(_tree.GetInnovation(988), "机械钟应存在（era5）");
            Assert.AreEqual(5, _tree.GetInnovation(988).era, "机械钟 era5");
            Assert.IsNotNull(_tree.GetInnovation(989), "风车应存在");
            Assert.IsNotNull(_tree.GetInnovation(990), "星盘导航应存在");
        }

        private static void CompleteTree(InnovationTree tree, int id)
        {
            Assert.IsTrue(tree.StartResearch(1, id), $"革新 {id} 应可开始研究");
            tree.DailyTick(1, 100000f);
            Assert.IsTrue(tree.HasInnovation(1, id), $"革新 {id} 应已完成");
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
