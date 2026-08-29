using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using CivilizationEvolution.Core;
using CivilizationEvolution.Economy;
using CivilizationEvolution.Politics;
using CivilizationEvolution.Role;
using CivilizationEvolution.Tech;

namespace CivilizationEvolution.Tests
{
    /// <summary>
    /// 角色数值体系 EditMode 测试（企划书第九篇：容量型/上限型数值、人格七维、精神疾病、饮食联动）
    /// </summary>
    public class CharacterSystemTests
    {
        [SetUp]
        public void Setup()
        {
            // 真实加载 StreamingAssets（模板/家族传统/种族基因频率依赖注册表）
            ContentRegistry.Reset();
            Localization.Reset();
            Localization.Initialize("zh-Hans");
            ContentRegistry.Initialize();
        }

        // ===== 统治类型判定（企划书 9.1：威望/恶名 → 明君/暴君/昏暴/平庸） =====

        [Test]
        public void RulerType_Benevolent_HighPrestigeLowNotoriety()
        {
            var c = MakeRuler();
            c.prestige = 80f;   // 容量 100 的 80%
            c.notoriety = 10f;
            Assert.AreEqual(RulerType.Benevolent, c.GetRulerType());
        }

        [Test]
        public void RulerType_Tyrant_HighNotorietyLowPrestige()
        {
            var c = MakeRuler();
            c.prestige = 10f;
            c.notoriety = 80f;
            Assert.AreEqual(RulerType.Tyrant, c.GetRulerType());
        }

        [Test]
        public void RulerType_TyrantFool_BothHigh()
        {
            var c = MakeRuler();
            c.prestige = 80f;
            c.notoriety = 80f;
            Assert.AreEqual(RulerType.TyrantFool, c.GetRulerType());
        }

        [Test]
        public void RulerType_Mediocre_BothLow()
        {
            var c = MakeRuler();
            c.prestige = 10f;
            c.notoriety = 10f;
            Assert.AreEqual(RulerType.Mediocre, c.GetRulerType());
        }

        // ===== 威望容量等级（当前值 + 容量等级 + 上限） =====

        [Test]
        public void PrestigeCapacity_UpgradeOnCap()
        {
            var c = MakeRuler();
            Assert.AreEqual(100f, c.GetPrestigeCapacity(), 0.001f);
            c.ModifyPrestige(150f);
            Assert.AreEqual(2, c.prestigeCapacityLevel, "达上限应升 2 级");
            Assert.AreEqual(300f, c.GetPrestigeCapacity(), 0.001f);
            Assert.AreEqual(100f, c.prestige, 0.001f, "升级时当前值保持旧上限");
        }

        [Test]
        public void PrestigeCapacity_DowngradeBelowThirtyPercent()
        {
            var c = MakeRuler();
            c.prestigeCapacityLevel = 3; // 上限 600
            c.prestige = 100f;            // < 600×0.3=180
            c.ModifyPrestige(0f);
            Assert.AreEqual(2, c.prestigeCapacityLevel, "低于 30% 应降级");
        }

        // ===== 人格七维家族遗传基线（企划书 9.3） =====

        [Test]
        public void Personality_InheritedFromParents_WithinTolerance()
        {
            var manager = new CharacterManager();
            var father = manager.CreateCharacter("父", "氏", 30, true, 0, 0, 0, CharacterRole.Noble);
            var mother = manager.CreateCharacter("母", "氏", 28, false, 0, 0, 0, CharacterRole.Noble);
            father.honor = 80f; mother.honor = 60f;      // 均值 70
            father.greed = -40f; mother.greed = -20f;    // 均值 -30

            var child = manager.CreateCharacter("子", "氏", 0, true, 0, 0, 0, CharacterRole.Commoner,
                null, father.characterId, mother.characterId);

            Assert.That(child.honor, Is.InRange(59f, 81f), "荣誉应接近双亲均值 70 ±10");
            Assert.That(child.greed, Is.InRange(-41f, -19f), "贪婪应接近双亲均值 -30 ±10");
        }

        [Test]
        public void Personality_Description_NonEmptyAndSceneBased()
        {
            var c = MakeRuler();
            c.honor = 90f;
            c.rationality = 70f;
            string desc = c.GetPersonalityDescription();
            Assert.IsFalse(string.IsNullOrEmpty(desc));
            StringAssert.Contains("为人", desc, "应为写实场景化描述而非标签");
            StringAssert.DoesNotContain("四字", desc);
        }

        // ===== 精神疾病（简单版：长期高压触发） =====

        [Test]
        public void MentalDisorder_HighStressNinetyDays_Triggers()
        {
            var manager = new CharacterManager();
            var c = manager.CreateCharacter("高压", "氏", 30, true, 0, 0, 0, CharacterRole.Ruler);
            c.stress = 90f;

            for (int day = 1; day <= 90; day++)
                manager.DailyTick(day, 1);

            Assert.IsFalse(string.IsNullOrEmpty(c.mentalDisorderId), "压力>80 持续 90 天应触发精神疾病");
            Assert.IsTrue(c.mentalDisorderId == MentalDisorderIds.Depression || c.mentalDisorderId == MentalDisorderIds.Anxiety,
                "高压触发应为抑郁或焦虑");
        }

        [Test]
        public void MentalDisorder_RecoversWhenStressLow()
        {
            var manager = new CharacterManager();
            var c = manager.CreateCharacter("康复", "氏", 30, true, 0, 0, 0, CharacterRole.Ruler);
            c.mentalDisorderId = MentalDisorderIds.Depression;
            c.stress = 10f;

            for (int day = 1; day <= 130; day++)
                manager.DailyTick(day, 1);

            Assert.AreEqual(MentalDisorderIds.None, c.mentalDisorderId, "压力<30 持续 120 天应康复");
        }

        [Test]
        public void MentalDisorder_DementiaIrreversible()
        {
            var manager = new CharacterManager();
            var c = manager.CreateCharacter("老朽", "氏", 80, true, 0, 0, 0, CharacterRole.Ruler);
            c.mentalDisorderId = MentalDisorderIds.Dementia;
            c.stress = 10f;

            for (int day = 1; day <= 200; day++)
                manager.DailyTick(day, 1);

            Assert.AreEqual(MentalDisorderIds.Dementia, c.mentalDisorderId, "失智不可逆");
        }

        // ===== 角色模板（第九篇角色生成参数模板：年龄/六维/人格倾向） =====

        [Test]
        public void CharacterTemplate_Ruler_AppliesStatMinAndAgeRange()
        {
            var manager = new CharacterManager();
            Assert.IsTrue(ContentRegistry.TryGetCharacterTemplate("tmpl_ruler", out var tpl), "统治者模板应已加载");

            var c = manager.CreateCharacter("君", "氏", 0, true, 0, 0, 0, CharacterRole.Ruler, template: tpl);

            Assert.That(c.age, Is.InRange(26, 60), "统治者模板年龄范围 26-60");
            Assert.That(c.martial, Is.GreaterThanOrEqualTo(30f), "统治者勇武下限 30");
            Assert.That(c.diplomacy, Is.GreaterThanOrEqualTo(30f), "统治者外交下限 30");
            Assert.That(c.stewardship, Is.GreaterThanOrEqualTo(30f), "统治者管理下限 30");
            Assert.That(c.learning, Is.GreaterThanOrEqualTo(30f), "统治者学识下限 30");
        }

        [Test]
        public void CharacterTemplate_Ruler_RationalityBiasShiftsMeanPositive()
        {
            var manager = new CharacterManager();
            Assert.IsTrue(ContentRegistry.TryGetCharacterTemplate("tmpl_ruler", out var tpl));

            float sum = 0f;
            const int N = 60;
            for (int i = 0; i < N; i++)
            {
                var c = manager.CreateCharacter("君" + i, "氏", 0, true, 0, 0, 0, CharacterRole.Ruler, template: tpl);
                sum += c.rationality;
            }
            Assert.That(sum / N, Is.GreaterThan(2f), $"理性倾向 +10 应使均值偏正，实际 {sum / N:F2}");
        }

        [Test]
        public void CharacterTemplate_ExplicitAge_NotOverridden()
        {
            var manager = new CharacterManager();
            Assert.IsTrue(ContentRegistry.TryGetCharacterTemplate("tmpl_military", out var tpl));

            var c = manager.CreateCharacter("将", "氏", 45, true, 0, 0, 0, CharacterRole.Military, template: tpl);
            Assert.AreEqual(45, c.age, "调用方显式指定年龄时模板不应覆盖");
            Assert.That(c.martial, Is.GreaterThanOrEqualTo(40f), "武将勇武下限 40");
            Assert.That(c.warfare, Is.GreaterThanOrEqualTo(40f), "武将军事经略下限 40");
        }

        // ===== 家族传统（企划书 9.4 家族文化偏移；定义表解释键，见 FamilyTraditionDef） =====

        [Test]
        public void FamilyTradition_InnovationPrerequisite_Enforced()
        {
            // 簪缨世家需官僚制度(503)：家族未持有革新 → 拒绝；持有 → 接受
            var manager = new CharacterManager();
            var tree = new InnovationTree();
            manager.Innovations = tree; // 先注入（CreateFamily 时传递给家族）

            var ruler = manager.CreateCharacter("祖", "氏", 30, true, 0, 0, 0, CharacterRole.Ruler);
            var family = manager.CreateFamily("簪缨氏", ruler.characterId, 1, realmId: 1);

            Assert.IsFalse(family.AddFamilyTradition("famtrad_dignitary_legacy"),
                "未持有官僚制度革新时簪缨世家应拒绝添加");

            // 完成官僚制度前置链（部落联盟→封建→驿传→中央集权→文书行政→官僚制度）
            CompleteChain(tree, 1, 500);
            CompleteChain(tree, 1, 200); // 陶器（文字前置）
            CompleteChain(tree, 1, 600);
            CompleteChain(tree, 1, 820);
            CompleteChain(tree, 1, 821);
            CompleteChain(tree, 1, 505);
            CompleteChain(tree, 1, 822);
            CompleteChain(tree, 1, 807);
            CompleteChain(tree, 1, 808);
            CompleteChain(tree, 1, 823);
            CompleteChain(tree, 1, 501);
            CompleteChain(tree, 1, 502);
            CompleteChain(tree, 1, 503);

            Assert.IsTrue(family.AddFamilyTradition("famtrad_dignitary_legacy"),
                "持有官僚制度革新后簪缨世家应可添加");
        }

        [Test]
        public void FamilyTradition_NoTreeInjected_SkipsCheck()
        {
            // 革新树未注入（宽松模式）：前置检查跳过，传统直接可添加
            var manager = new CharacterManager();
            var ruler = manager.CreateCharacter("祖", "氏", 30, true, 0, 0, 0, CharacterRole.Ruler);
            var family = manager.CreateFamily("宽松氏", ruler.characterId, 1, realmId: 1);

            Assert.IsTrue(family.AddFamilyTradition("famtrad_dignitary_legacy"),
                "未注入革新树时不应做前置检查");
        }

        [Test]
        public void FamilyTradition_ExtendedLines_RequireSpecificInnovations()
        {
            // 批 C：家族传统扩展线——翰墨传家需[造纸+文字]/盐铁世家需[井盐+冶铁]/弓马世家需[马镫+骑兵战术]
            var manager = new CharacterManager();
            var tree = new InnovationTree();
            manager.Innovations = tree;

            var ruler = manager.CreateCharacter("祖", "氏", 30, true, 0, 0, 0, CharacterRole.Ruler);
            var family = manager.CreateFamily("扩展氏", ruler.characterId, 1, realmId: 1);

            Assert.IsFalse(family.AddFamilyTradition("famtrad_ink_legacy"), "未持有时翰墨传家应拒绝");

            // 完成翰墨链：陶器→文字→简牍→簿籍→文书行政→(造纸：纺织+文字)→印刷前置链
            CompleteChain(tree, 1, 200);
            CompleteChain(tree, 1, 600);
            CompleteChain(tree, 1, 204);
            CompleteChain(tree, 1, 205); // 造纸

            Assert.IsTrue(family.AddFamilyTradition("famtrad_ink_legacy"), "持有造纸+文字后翰墨传家应可添加");
            Assert.IsFalse(family.AddFamilyTradition("famtrad_salt_iron"), "盐铁世家前置（井盐+冶铁）未满足应拒绝");
        }

        private static void CompleteChain(InnovationTree tree, int realmId, int innovationId)
        {
            Assert.IsTrue(tree.StartResearch(realmId, innovationId), $"革新 {innovationId} 应可开始研究");
            tree.DailyTick(realmId, 100000f);
            Assert.IsTrue(tree.HasInnovation(realmId, innovationId), $"革新 {innovationId} 应已完成");
        }

        // ===== 家族传统（企划书 9.4：互斥/注册表校验/效果查询） =====

        [Test]
        public void FamilyTradition_MutualExclusion_BlocksOpposite()
        {
            var manager = new CharacterManager();
            var ruler = manager.CreateCharacter("祖", "氏", 30, true, 0, 0, 0, CharacterRole.Ruler);
            var family = manager.CreateFamily("奢华氏", ruler.characterId, 1);

            Assert.IsTrue(family.AddFamilyTradition("famtrad_luxury_style"), "奢华门风应可传承");
            Assert.IsFalse(family.AddFamilyTradition("famtrad_thrifty_style"), "节俭家风与奢华门风互斥，应拒绝");
            Assert.IsTrue(family.familyTraditions.ContainsKey("famtrad_luxury_style"));

            Assert.IsTrue(family.RemoveFamilyTradition("famtrad_luxury_style"), "移除后应可腾出互斥位");
            Assert.IsTrue(family.AddFamilyTradition("famtrad_thrifty_style"), "互斥传统移除后应可添加");
        }

        [Test]
        public void FamilyTradition_UnknownId_RejectedWithWarning()
        {
            var manager = new CharacterManager();
            var ruler = manager.CreateCharacter("祖", "氏", 30, true, 0, 0, 0, CharacterRole.Ruler);
            var family = manager.CreateFamily("无名氏", ruler.characterId, 1);

            Assert.IsFalse(family.AddFamilyTradition("famtrad_does_not_exist"), "未注册传统应拒绝");
            Assert.AreEqual(0, family.familyTraditions.Count);
        }

        [Test]
        public void FamilyTradition_EffectSummation_ScalesWithStrength()
        {
            var manager = new CharacterManager();
            var ruler = manager.CreateCharacter("祖", "氏", 30, true, 0, 0, 0, CharacterRole.Ruler);
            var family = manager.CreateFamily("商贾氏", ruler.characterId, 1);

            family.AddFamilyTradition("famtrad_merchant_legacy");
            Assert.That(family.GetTraditionEffect("gold"), Is.EqualTo(0.08f).Within(0.001f), "传承强度 1 时 gold 效果 = 0.08");

            family.familyTraditions["famtrad_merchant_legacy"] = 3f; // 三代传承
            Assert.That(family.GetTraditionEffect("gold"), Is.EqualTo(0.24f).Within(0.001f), "传承强度随代际线性累积");
        }

        // ===== 饮食联动（缺粮 → 压力上升 + 肥胖下降） =====

        [Test]
        public void Diet_FamineRaisesStressAndLowersObesity()
        {
            var manager = new CharacterManager();
            var c = manager.CreateCharacter("饥民", "氏", 30, true, 0, 0, 0, CharacterRole.Commoner);
            c.realmId = 0;
            c.stress = 0f;
            c.obesity = 50f;

            // 最小经济注入：政权 0 核心地块 0（region 0）的贸易中心无粮
            var tiles = new TileData[64];
            for (int i = 0; i < tiles.Length; i++)
                tiles[i] = new TileData { tileIndex = i, regionId = i / 16, exists = true };
            var tcs = new Dictionary<int, TradeCenter> { [0] = new TradeCenter { regionId = 0, inventory = new Dictionary<int, float>() } };
            var economy = new EconomyManager(tiles, tcs, new Dictionary<int, GoodsDef>(), new CurrencySystem(), new TaxSystem());
            var realm = new RealmData { realmId = 0 };
            realm.coreTiles.Add(0);
            var realms = new Dictionary<int, RealmData> { [0] = realm };

            manager.Economy = economy;
            manager.Tiles = tiles;
            manager.Realms = realms;

            manager.DailyTick(1, 1); // 一天：缺粮 → 压力+2，肥胖-0.03

            Assert.That(c.stress, Is.GreaterThan(1f), "缺粮应显著提高压力");
            Assert.That(c.obesity, Is.LessThan(50f), "缺粮应降低肥胖");
        }

        // ===== 人格亲和与 AI 同步（借鉴 CK3 More Personality Depth） =====

        [Test]
        public void PersonalityAffinity_SameDirection_Positive()
        {
            var a = MakeRuler();
            var b = MakeRuler();
            a.honor = 80f; b.honor = 70f;   // 同向高荣誉
            Assert.That(a.GetPersonalityAffinity(b), Is.GreaterThan(0f), "同向人格应互喜");
        }

        [Test]
        public void PersonalityAffinity_OppositeDirection_Negative()
        {
            var a = MakeRuler();
            var b = MakeRuler();
            a.honor = 80f; b.honor = -80f;  // 反向
            Assert.That(a.GetPersonalityAffinity(b), Is.LessThan(0f), "反向人格应互厌");
        }

        [Test]
        public void PersonalityAffinity_WeakTraits_Neutral()
        {
            var a = MakeRuler();
            var b = MakeRuler();
            Assert.AreEqual(0f, a.GetPersonalityAffinity(b), "无倾向（|v|<15）不参与亲和");
        }

        [Test]
        public void AIController_SyncPersonality_GreedyRuler_EconomicBias()
        {
            var ruler = MakeRuler();
            ruler.greed = 90f;
            var controller = new CivilizationEvolution.AI.AIController(0, CivilizationEvolution.AI.AIPersonality.RandomPersonality());
            controller.SyncPersonality(ruler);
            Assert.That(controller.personality.economicBias, Is.GreaterThan(0.6f), "高贪婪统治者应显著偏好经济");
        }

        [Test]
        public void AIController_SyncPersonality_VengefulRuler_Aggression()
        {
            var ruler = MakeRuler();
            ruler.vengefulness = 90f;
            ruler.boldness = 80f;
            var controller = new CivilizationEvolution.AI.AIController(0, CivilizationEvolution.AI.AIPersonality.RandomPersonality());
            controller.SyncPersonality(ruler);
            Assert.That(controller.personality.aggression, Is.GreaterThan(0.6f), "高报复统治者应显著好战");
        }

        // ===== 辅助 =====

        private static CharacterData MakeRuler()
        {
            return new CharacterData
            {
                characterId = 1,
                firstName = "T",
                lastName = "C",
                role = CharacterRole.Ruler
            };
        }
    }
}
