using System.Collections.Generic;
using NUnit.Framework;
using CivilizationEvolution.Core;
using CivilizationEvolution.Culture;
using CivilizationEvolution.Economy;
using CivilizationEvolution.Politics;
using CivilizationEvolution.Tech;

namespace CivilizationEvolution.Tests
{
    /// <summary>
    /// 阶层×革新×文化传统 与 政体成分×革新 对接测试（用户定稿）
    /// 不是所有社会都有全部阶层；政体每个部分都有支撑革新
    /// </summary>
    public class SocialInnovationLinkTests
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
            Assert.IsTrue(tree.StartResearch(1, id), $"革新 {id} 应可研究");
            tree.DailyTick(1, 100000f);
        }

        // ===== 阶层存在性 × 革新 =====

        [Test]
        public void Serf_RequiresManorialism()
        {
            // 农奴需庄园制度（952）——未持有庄园时无农奴
            Assert.IsFalse(SocialClassAvailability.IsSubclassAvailable(
                GameEnums.SocialSubclass.Serf, null, _tree, 1), "无庄园制度时农奴不可用");

            // 完成庄园链：部落联盟→封建→庄园
            Complete(_tree, 500);
            Complete(_tree, 501);
            Complete(_tree, 952);
            Assert.IsTrue(SocialClassAvailability.IsSubclassAvailable(
                GameEnums.SocialSubclass.Serf, null, _tree, 1), "庄园制度后农奴可用");
        }

        [Test]
        public void Scholar_RequiresScriptAndBureaucracy()
        {
            // 士人需文字（600）+官僚/科举
            Assert.IsFalse(SocialClassAvailability.IsSubclassAvailable(
                GameEnums.SocialSubclass.Scholar, null, _tree, 1), "无文字时士人不可用");

            Complete(_tree, 200); // 陶器
            Complete(_tree, 600); // 文字
            Complete(_tree, 820); // 简牍
            Complete(_tree, 821); // 簿籍
            Complete(_tree, 505); // 成文法
            Complete(_tree, 822); // 文书行政
            Complete(_tree, 807); // 轮车
            Complete(_tree, 808); // 硬质道路
            Complete(_tree, 823); // 驿传
            Complete(_tree, 958); // 集权思想
            Complete(_tree, 959); // 郡县
            Complete(_tree, 502); // 中央集权
            Complete(_tree, 503); // 官僚
            Assert.IsTrue(SocialClassAvailability.IsSubclassAvailable(
                GameEnums.SocialSubclass.Scholar, null, _tree, 1), "文字+官僚后士人可用");
        }

        [Test]
        public void Merchant_RequiresCoinage()
        {
            Assert.IsFalse(SocialClassAvailability.IsSubclassAvailable(
                GameEnums.SocialSubclass.Merchant, null, _tree, 1), "无铸币时商人不可用");
            Complete(_tree, 200);
            Complete(_tree, 201); // 青铜冶炼
            Complete(_tree, 700); // 以物易物
            Complete(_tree, 701); // 铸币
            Assert.IsTrue(SocialClassAvailability.IsSubclassAvailable(
                GameEnums.SocialSubclass.Merchant, null, _tree, 1), "铸币后商人可用");
        }

        [Test]
        public void Artisan_CultureTraditionFallback()
        {
            // 工匠：工艺革新 或 工匠行会文化传统
            Assert.IsFalse(SocialClassAvailability.IsSubclassAvailable(
                GameEnums.SocialSubclass.Artisan, null, _tree, 1), "无工艺无传统时工匠不可用");

            // 文化传统兜底（Laethis 有 trad_craft_guild 工匠行会）
            Assert.IsTrue(ContentRegistry.TryGetCulture(1, out var pack), "Laethis 文化");
            Assert.IsTrue(SocialClassAvailability.IsSubclassAvailable(
                GameEnums.SocialSubclass.Artisan, pack.data, _tree, 1), "工匠行会传统→工匠可用");
        }

        [Test]
        public void DebtSlave_RequiresWrittenLaw()
        {
            Assert.IsFalse(SocialClassAvailability.IsSubclassAvailable(
                GameEnums.SocialSubclass.DebtSlave, null, _tree, 1), "无成文法时债务奴不可用");
            Complete(_tree, 200);
            Complete(_tree, 505); // 成文法
            Assert.IsTrue(SocialClassAvailability.IsSubclassAvailable(
                GameEnums.SocialSubclass.DebtSlave, null, _tree, 1), "成文法后债务奴可用");
        }

        // ===== 政体成分 × 支撑革新 =====

        [Test]
        public void PolityComponent_Examination_RequiresKeju()
        {
            // B1.Examination（客观标准/科举）需科举制度（504）
            Assert.IsFalse(PolityComponentInnovations.IsComponentAvailable(
                PolityComponentInnovations.PolityDimension.CentralSuccession, (int)CentralSuccession.Examination, _tree, 1), "无科举时考试选任不可用");

            Complete(_tree, 200); // 陶器
            Complete(_tree, 600); // 文字
            Complete(_tree, 204); // 纺织机
            Complete(_tree, 205); // 造纸
            Complete(_tree, 820); Complete(_tree, 821); Complete(_tree, 505);
            Complete(_tree, 822); Complete(_tree, 807); Complete(_tree, 808);
            Complete(_tree, 823); Complete(_tree, 958); Complete(_tree, 959);
            Complete(_tree, 502); Complete(_tree, 503); Complete(_tree, 945);
            Complete(_tree, 504); // 科举
            Assert.IsTrue(PolityComponentInnovations.IsComponentAvailable(
                PolityComponentInnovations.PolityDimension.CentralSuccession, (int)CentralSuccession.Examination, _tree, 1), "科举后考试选任可用");
        }

        [Test]
        public void PolityComponent_CentralAppointed_RequiresCountyOrProvince()
        {
            // C1.Appointed 需郡县（959）或行省（960）
            Assert.IsFalse(PolityComponentInnovations.IsComponentAvailable(
                PolityComponentInnovations.PolityDimension.LocalSuccession, (int)LocalSuccession.Appointed, _tree, 1), "无郡县/行省时中央任命不可用");
            Complete(_tree, 200); // 陶器（文字前置）
            Complete(_tree, 600);
            Complete(_tree, 958); // 集权思想
            Complete(_tree, 959); // 郡县
            Assert.IsTrue(PolityComponentInnovations.IsComponentAvailable(
                PolityComponentInnovations.PolityDimension.LocalSuccession, (int)LocalSuccession.Appointed, _tree, 1), "郡县制后中央任命可用");
        }

        [Test]
        public void PolityComponent_Unitary_RequiresCentralization()
        {
            // D.Unitary 需中央集权（502）
            Assert.IsFalse(PolityComponentInnovations.IsComponentAvailable(
                PolityComponentInnovations.PolityDimension.SpatialStructure, (int)SpatialStructure.Unitary, _tree, 1), "无中央集权时单一制不可用");

            // 基础成分无要求
            Assert.IsTrue(PolityComponentInnovations.IsComponentAvailable(
                PolityComponentInnovations.PolityDimension.SupremeScope, (int)SupremeScope.Absolute, _tree, 1), "全能为基础可用");
            Assert.IsTrue(PolityComponentInnovations.IsComponentAvailable(
                PolityComponentInnovations.PolityDimension.CentralInstitution, (int)CentralInstitution.Court, _tree, 1), "王庭为基础可用");

            // 无革新树=宽松
            Assert.IsTrue(PolityComponentInnovations.IsComponentAvailable(
                PolityComponentInnovations.PolityDimension.CentralSuccession, (int)CentralSuccession.Examination, null, 1), "未注入革新树时宽松可用");
        }

        // ===== 文化传统 × 革新前置 + 族群革新挂载 + 城市特许 =====

        [Test]
        public void Tradition_RequiresInnovations()
        {
            // 用户定稿：革新与文化传统互相约束——必须持有革新才能持有传统
            Assert.IsTrue(ContentRegistry.TryGetTradition("trad_agrarian_rites", out var agrarian));
            Assert.IsTrue(agrarian.requiredInnovations.Contains(100), "农耕礼俗需刀耕火种");

            Assert.IsTrue(ContentRegistry.TryGetTradition("trad_ancestor_cult", out var ancestor));
            Assert.IsTrue(ancestor.requiredInnovations.Contains(809), "祖先崇拜需宗族祭祀");

            Assert.IsTrue(ContentRegistry.TryGetTradition("trad_pastoral_riding", out var riding));
            Assert.IsTrue(riding.requiredInnovations.Contains(922), "游牧骑术需马的驯化");

            // 全部 11 条传统都有前置（互相约束全覆盖）
            foreach (var trad in ContentRegistry.Traditions.Values)
            {
                Assert.That(trad.requiredInnovations.Count, Is.GreaterThan(0),
                    $"传统 {trad.traditionId} 应有革新前置");
            }
        }

        [Test]
        public void EthnicGroup_InnovationsMounted_NotPillar()
        {
            // 革新挂载在族群上但非支柱（支柱=族群精神/语言/文化传统）
            Assert.IsTrue(ContentRegistry.TryGetEthnicGroup("ethnos_laethis", out var group));
            Assert.IsTrue(group.innovationIds.Contains(600), "族群拥有文字革新");
            Assert.IsTrue(group.innovationIds.Contains(100), "族群拥有刀耕火种");

            // 支柱字段仍为原三件套
            Assert.AreEqual("ethos_scholarly", group.ethosId, "族群精神=支柱");
            Assert.AreEqual("laethis_lang", group.languageId, "语言=支柱");
            Assert.That(group.traditionIds.Count, Is.GreaterThan(0), "文化传统=支柱");
        }

        [Test]
        public void LearningDifficulty_PrerequisiteCompletion()
        {
            // 无前置革新=1.0（没有困难）
            Assert.That(_tree.GetLearningDifficulty(1, 902), Is.EqualTo(1f).Within(0.001f), "石器打制无前置");

            // 缺前置：0.4 + 0.6×完成比例（轮作制 102 前置仅休耕制 101：0 完成=0.4）
            Assert.That(_tree.GetLearningDifficulty(1, 102), Is.EqualTo(0.4f).Within(0.001f), "缺前置学习慢");

            // 完成前置链后=1.0（速度快）
            Complete(_tree, 100);
            Complete(_tree, 101);
            Assert.That(_tree.GetLearningDifficulty(1, 102), Is.EqualTo(1f).Within(0.001f), "前置全完成速度快");
        }

        [Test]
        public void EffectiveRate_DifficultyAndAffinity()
        {
            // Laethis 文化亲和 Agriculture/Craft/Script（field 级）
            Assert.IsTrue(ContentRegistry.TryGetCulture(1, out var pack));
            var culture = pack.data;

            // 农耕革新（field=Agriculture）亲和 → ×1.25；无前置难度 1.0
            float rate = _tree.GetEffectiveResearchRate(1, 100, 10f, culture);
            Assert.That(rate, Is.EqualTo(12.5f).Within(0.001f), "亲和加成 1.25");

            // 无亲和文化（临时造一个空亲和）→ 无加成
            var noAffinity = new CultureData { cultureId = 99, cultureName = "无亲和", innovationAffinities = new List<string>() };
            float rate2 = _tree.GetEffectiveResearchRate(1, 100, 10f, noAffinity);
            Assert.That(rate2, Is.EqualTo(10f).Within(0.001f), "无亲和无加成");
        }

        [Test]
        public void CityCharter_IsInstitutionInnovation()
        {
            // 城市特许=独立制度革新（用户定稿）
            var def = _tree.GetInnovation(994);
            Assert.IsNotNull(def, "城市特许状应存在");
            Assert.AreEqual(InnovationField.Governance, def.field, "城市特许=制度-政制");
            Assert.IsTrue(def.prerequisites.Contains(701), "前置铸币");
            Assert.IsTrue(def.prerequisites.Contains(505), "前置成文法（特许状=法律文书）");

            // C1.CityCharter 支撑革新=994
            Assert.IsTrue(PolityComponentInnovations.GetRequiredInnovations(
                PolityComponentInnovations.PolityDimension.LocalSuccession,
                (int)LocalSuccession.CityCharter).Contains(994), "城市特许成分由特许状革新支撑");
        }
    }
}
