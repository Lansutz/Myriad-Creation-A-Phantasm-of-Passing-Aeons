using System.Collections.Generic;
using NUnit.Framework;
using CivilizationEvolution.Role;
using CivilizationEvolution.Culture;

namespace CivilizationEvolution.Tests
{
    /// <summary>
    /// 原型学术画像与称号系统测试
    /// </summary>
    public class PersonalityArchetypeTests
    {
        private CharacterManager _cm;

        [SetUp]
        public void Setup()
        {
            _cm = new CharacterManager();
        }

        [Test]
        public void Archetype_NeutralDescription_NoIdentityWords()
        {
            // 中性描述——不挂钩身份/职业（农人也是同一描述）
            var conquerorLike = _cm.CreateCharacter("烈", "性", 50, true, 0, 0, 0, CharacterRole.Commoner);
            conquerorLike.boldness = 80f;
            conquerorLike.greed = 70f;
            conquerorLike.vengefulness = 60f;
            conquerorLike.compassion = -50f;
            string desc = PersonalityArchetype.Describe(conquerorLike);

            Assert.IsNotEmpty(desc, "描述非空");
            // 不得含身份挂钩词（君/主/王/者/之人后缀）
            foreach (var w in new[] { "之君", "之主", "之王", "之士" })
                Assert.IsFalse(desc.Contains(w), $"描述不含身份词 {w}");

            // 任意七维组合都能生成（108 组合全覆盖——不遗漏）
            var rng = new System.Random(7);
            for (int i = 0; i < 20; i++)
            {
                var c = _cm.CreateCharacter("测", i.ToString(), 40, true, 0, 0, 0, CharacterRole.Commoner);
                c.boldness = (float)(rng.NextDouble() * 200 - 100);
                c.compassion = (float)(rng.NextDouble() * 200 - 100);
                c.greed = (float)(rng.NextDouble() * 200 - 100);
                c.honor = (float)(rng.NextDouble() * 200 - 100);
                c.rationality = (float)(rng.NextDouble() * 200 - 100);
                c.vengefulness = (float)(rng.NextDouble() * 200 - 100);
                c.piety = (float)(rng.NextDouble() * 200 - 100);
                Assert.IsNotEmpty(PersonalityArchetype.Describe(c), $"随机组合 {i} 有描述");
                Assert.IsNotEmpty(PersonalityArchetype.TypeName(c), $"随机组合 {i} 有类型名");
            }
        }

        [Test]
        public void Archetype_TypeName_Categories()
        {
            // 类型名（学术画像简名——内部归类）
            var dev = _cm.CreateCharacter("虔", "徒", 50, true, 0, 0, 0, CharacterRole.Commoner);
            dev.piety = 80f;
            Assert.IsTrue(PersonalityArchetype.TypeName(dev).Contains("虔信"), "高虔信→虔信型");

            var gentle = _cm.CreateCharacter("温", "良", 50, true, 0, 0, 0, CharacterRole.Commoner);
            gentle.boldness = -60f;
            gentle.compassion = 70f;
            gentle.honor = 60f;
            Assert.IsTrue(PersonalityArchetype.TypeName(gentle).Contains("温"), "低进取高仁厚→温厚型");
        }

        [Test]
        public void EvaluationLevels_OrderAndScores()
        {
            // 8 级评价（传奇>卓越>杰出>优秀>平平>平庸>无名>遗臭）
            Assert.AreEqual(EvaluationLevel.Legendary, EvaluationSystem.LevelFromScore(950f), "950→传奇");
            Assert.AreEqual(EvaluationLevel.Preeminent, EvaluationSystem.LevelFromScore(800f), "800→卓越");
            Assert.AreEqual(EvaluationLevel.Distinguished, EvaluationSystem.LevelFromScore(600f), "600→杰出");
            Assert.AreEqual(EvaluationLevel.Excellent, EvaluationSystem.LevelFromScore(400f), "400→优秀");
            Assert.AreEqual(EvaluationLevel.Mediocre, EvaluationSystem.LevelFromScore(250f), "250→平平");
            Assert.AreEqual(EvaluationLevel.Ordinary, EvaluationSystem.LevelFromScore(150f), "150→平庸");
            Assert.AreEqual(EvaluationLevel.Obscure, EvaluationSystem.LevelFromScore(50f), "50→无名");
            Assert.AreEqual(EvaluationLevel.Infamous, EvaluationSystem.LevelFromScore(-50f), "负分→遗臭");

            // 等级名（评价词非绰号——无"伟大"）
            Assert.AreEqual("卓越", EvaluationSystem.LevelName(EvaluationLevel.Preeminent), "卓越级名");
            Assert.AreEqual("传奇", EvaluationSystem.LevelName(EvaluationLevel.Legendary), "传奇级名");
        }

        [Test]
        public void Epithet_EvaluateAndGrant_Tiers()
        {
            // 普通绰号（征服者——行为阈值）
            var conqueror = _cm.CreateCharacter("征", "者", 60, true, 0, 0, 0, CharacterRole.Ruler);
            var rec = new EvaluationSystem.AchievementRecord { warsWon = 4, conquests = 6 };
            Assert.AreEqual("征服者", EpithetSystem.EvaluateAndGrant(conqueror, rec), "征服 6 块→征服者");

            // 狐狸（诈术——不覆盖已有）
            var fox = _cm.CreateCharacter("狡", "狐", 60, true, 0, 0, 0, CharacterRole.Ruler);
            var recFox = new EvaluationSystem.AchievementRecord { schemesSucceeded = 6 };
            Assert.AreEqual("狐狸", EpithetSystem.EvaluateAndGrant(fox, recFox), "诈术 6 次→狐狸");

            // 伟大者（区域影响力≥0.6——独立于成就分——阿尔弗雷德式：
            // 未控全英格兰但区域内影响力大——发放较多——不是严苛评价）
            var great = _cm.CreateCharacter("中", "兴", 60, true, 0, 0, 0, CharacterRole.Ruler);
            var recGreat = new EvaluationSystem.AchievementRecord
            {
                warsWon = 2, conquests = 1, reignYears = 25f, // 成就一般——评价不高
                regionalInfluence = 0.7f                       // 但区域内影响力前列
            };
            Assert.AreEqual("伟大者", EpithetSystem.EvaluateAndGrant(great, recGreat), "区域影响力前列→伟大者");

            // 区域影响力不足（0.4）→ 不给伟大者（普通绰号照常）
            var minor = _cm.CreateCharacter("小", "邦", 60, true, 0, 0, 0, CharacterRole.Ruler);
            var recMinor = new EvaluationSystem.AchievementRecord
            {
                warsWon = 4, conquests = 6, regionalInfluence = 0.3f
            };
            Assert.AreEqual("征服者", EpithetSystem.EvaluateAndGrant(minor, recMinor), "影响力不足→征服者非伟大者");

            // 传奇升格（征服王——传奇线 900+ 跨文化大征服——覆盖普通绰号）
            var alexander = _cm.CreateCharacter("亚", "历山大", 60, true, 0, 0, 0, CharacterRole.Ruler);
            var recLeg = new EvaluationSystem.AchievementRecord
            {
                warsWon = 20, conquests = 15, expeditions = 5, cultureActs = 3, religionActs = 1,
                reignYears = 13f
            }; // 20*30+15*40+5*45+50 = 1475 → 传奇
            Assert.AreEqual("征服王", EpithetSystem.EvaluateAndGrant(alexander, recLeg), "跨文化大征服→征服王（覆盖）");

            // 无地者（反讽中性）
            var landless = _cm.CreateCharacter("无", "地", 60, true, 0, 0, 0, CharacterRole.Ruler);
            var recLand = new EvaluationSystem.AchievementRecord { lostAllLands = 1 };
            Assert.AreEqual("无地者", EpithetSystem.EvaluateAndGrant(landless, recLand), "失地→无地者");
        }

        [Test]
        public void Epithet_GrantAndPosthumous()
        {
            // 绰号：直接授予（一次不覆盖）
            var c = _cm.CreateCharacter("征", "者", 60, true, 0, 0, 0, CharacterRole.Ruler);
            Assert.IsTrue(EpithetSystem.GrantEpithet(c, "征服者"), "授予绰号");
            Assert.AreEqual("征服者", c.epithet, "绰号记录");
            Assert.IsFalse(EpithetSystem.GrantEpithet(c, "大冒险家"), "已有绰号不覆盖");

            // 谥号：华夏式（行为定谥——显式设全七维防隐性随机）
            var martial = _cm.CreateCharacter("武", "功", 60, true, 0, 0, 0, CharacterRole.Ruler);
            martial.boldness = 50f; martial.greed = 0f; martial.honor = 0f;
            martial.rationality = 0f; martial.vengefulness = 0f; martial.piety = 0f;
            martial.compassion = 30f;
            Assert.AreEqual(30f, martial.compassion, "设值生效");
            Assert.AreEqual("武", EpithetSystem.DeterminePosthumousTitle(martial, warsWon: 6, conquests: 4, false), "多战功→武");

            var benevolent = _cm.CreateCharacter("仁", "德", 60, true, 0, 0, 0, CharacterRole.Ruler);
            benevolent.boldness = 0f; benevolent.greed = 0f; benevolent.honor = 0f;
            benevolent.rationality = 0f; benevolent.vengefulness = 0f; benevolent.piety = 0f;
            benevolent.compassion = 70f;
            Assert.AreEqual("仁", EpithetSystem.DeterminePosthumousTitle(benevolent, 0, 0, false), "仁德→仁");

            var cruel = _cm.CreateCharacter("暴", "虐", 60, true, 0, 0, 0, CharacterRole.Ruler);
            cruel.boldness = 0f; cruel.greed = 0f; cruel.honor = 0f;
            cruel.rationality = 0f; cruel.piety = 0f;
            cruel.compassion = -70f;
            cruel.vengefulness = 70f;
            Assert.AreEqual("暴", EpithetSystem.DeterminePosthumousTitle(cruel, 2, 1, false), "暴虐→暴");

            var negligent = _cm.CreateCharacter("荒", "政", 60, true, 0, 0, 0, CharacterRole.Ruler);
            negligent.boldness = 0f; negligent.greed = 0f; negligent.honor = 0f;
            negligent.rationality = 0f; negligent.vengefulness = 0f; negligent.piety = 0f;
            negligent.compassion = 0f;
            Assert.AreEqual("荒", EpithetSystem.DeterminePosthumousTitle(negligent, 0, 0, true), "饥荒→荒");
        }

        [Test]
        public void Epithet_MadKing_NPD_Style()
        {
            // 疯王=NPD 式统治风格（未必有病）：傲慢(2+)+多疑+喜怒无常(高报复+低理性)
            // +任性妄为(高大胆+低荣誉)+在位 10 年——卡利古拉式
            var _cm2 = new CharacterManager();
            var caligula = _cm2.CreateCharacter("卡", "利古拉", 40, true, 0, 0, 0, CharacterRole.Ruler);
            caligula.traits = new List<PersonalityTrait>
            {
                new PersonalityTrait { traitId = "arrogant_3", traitName = "目中无人" },
                new PersonalityTrait { traitId = "paranoid_2", traitName = "多疑" }
            };
            caligula.boldness = 60f; caligula.rationality = -40f;
            caligula.vengefulness = 60f; caligula.honor = -30f; caligula.compassion = -50f;
            var rec = new EvaluationSystem.AchievementRecord { reignYears = 15f };
            Assert.AreEqual("疯王", EpithetSystem.EvaluateAndGrant(caligula, rec), "NPD 组合+在位→疯王");

            // 有精神疾病≠疯王（疯子=临床——查理六世式）
            var madman = _cm2.CreateCharacter("查", "理", 40, true, 0, 0, 0, CharacterRole.Commoner);
            madman.mentalDisorderId = "delirium";
            var rec2 = new EvaluationSystem.AchievementRecord();
            Assert.AreEqual("疯子", EpithetSystem.EvaluateAndGrant(madman, rec2), "精神疾病→疯子（非疯王）");
        }

        [Test]
        public void Epithet_PoetKing_JourneymanKing()
        {
            // 诗人王=经历型传奇：行吟远行(远征 5+)+诗作(3+)+贤君(评价 550+——杰出)
            var _cm3 = new CharacterManager();
            var harald = _cm3.CreateCharacter("哈", "拉尔德", 60, true, 0, 0, 0, CharacterRole.Ruler);
            harald.boldness = 0f; harald.greed = 0f; harald.honor = 40f;
            harald.rationality = 0f; harald.vengefulness = 0f; harald.piety = 0f;
            harald.compassion = 40f;
            var rec = new EvaluationSystem.AchievementRecord
            {
                expeditions = 7, poetryActs = 5, warsWon = 8, conquests = 4,
                religionActs = 2, threatsResolved = 3, reignYears = 20f
            }; // 50+8*30+4*40+5*40+2*25+3*30 = 880 → 杰出 550+ ✓
            Assert.AreEqual("诗人王", EpithetSystem.EvaluateAndGrant(harald, rec), "行吟经历+贤君→诗人王");

            // 诗人（普通——双向）——诗作多但非统治者/未达贤君
            var poet = _cm3.CreateCharacter("游", "吟", 40, true, 0, 0, 0, CharacterRole.Commoner);
            var rec2 = new EvaluationSystem.AchievementRecord { poetryActs = 5, expeditions = 2 };
            Assert.AreEqual("诗人", EpithetSystem.EvaluateAndGrant(poet, rec2), "诗作→诗人（普通双向）");
        }

        [Test]
        public void Epithet_BodyMarks_AndBlackWhite()
        {
            // 外貌绰号（bodyMarks——事件写入）
            var _cm4 = new CharacterManager();
            var bald = _cm4.CreateCharacter("秃", "头", 50, true, 0, 0, 0, CharacterRole.Ruler);
            bald.bodyMarks.Add("秃顶");
            var rec = new EvaluationSystem.AchievementRecord { reignYears = 5f };
            Assert.AreEqual("秃头", EpithetSystem.EvaluateAndGrant(bald, rec), "秃顶标记→秃头");

            // 成对体系：黑王子（继承人）/黑王（在位君主）
            var blackPrince = _cm4.CreateCharacter("黑", "王子", 30, true, 0, 0, 0, CharacterRole.Heir);
            blackPrince.bodyMarks.Add("黑色");
            Assert.AreEqual("黑王子", EpithetSystem.EvaluateAndGrant(blackPrince, rec), "黑色+继承人→黑王子");

            var whiteKing = _cm4.CreateCharacter("白", "王", 50, true, 0, 0, 0, CharacterRole.Ruler);
            whiteKing.bodyMarks.Add("白色");
            Assert.AreEqual("白王", EpithetSystem.EvaluateAndGrant(whiteKing, rec), "白色+君主→白王");

            // 骑士级成对（军事身份——黑甲→黑骑士）
            var blackKnight = _cm4.CreateCharacter("黑", "骑", 30, true, 0, 0, 0, CharacterRole.Military);
            blackKnight.bodyMarks.Add("黑色");
            Assert.AreEqual("黑骑士", EpithetSystem.EvaluateAndGrant(blackKnight, rec), "黑甲+骑士→黑骑士");

            // 平民黑色标记→无黑白绰号（甲色系与平民无关——发色自然系另给）
            var peasant = _cm4.CreateCharacter("农", "夫", 40, true, 0, 0, 0, CharacterRole.Commoner);
            peasant.bodyMarks.Add("黑色");
            Assert.AreEqual("", EpithetSystem.EvaluateAndGrant(peasant, rec), "平民黑甲标记→无黑白绰号");
            peasant.bodyMarks.Clear();
            peasant.bodyMarks.Add("金发");
            Assert.AreEqual("美发者", EpithetSystem.EvaluateAndGrant(peasant, rec), "金发→美发者（任何身份）");

            // catalog 查询（语义色彩/成对完整）
            Assert.IsNotNull(EpithetCatalog.Get("epithet_black_king"), "黑王在册");
            Assert.IsNotNull(EpithetCatalog.Get("epithet_white_prince"), "白王子在册（成对）");
            Assert.AreEqual(EpithetConnotation.Dual, EpithetCatalog.Get("epithet_poet").connotation, "诗人=双向语义");
            Assert.AreEqual(EpithetConnotation.Negative, EpithetCatalog.Get("epithet_mad_king").connotation, "疯王=贬");
        }

        [Test]
        public void Epithet_SecondBatch_Judgments()
        {
            var cm2 = new CharacterManager();
            var rec = new EvaluationSystem.AchievementRecord { reignYears = 10f };

            // 屠夫（屠城）
            var butcher = cm2.CreateCharacter("屠", "夫", 50, true, 0, 0, 0, CharacterRole.Ruler);
            butcher.compassion = -80f;
            var recB = new EvaluationSystem.AchievementRecord { massacres = 2 };
            Assert.AreEqual("屠夫", EpithetSystem.EvaluateAndGrant(butcher, recB), "屠城→屠夫");

            // 胖子（肥胖系统）
            var fatty = cm2.CreateCharacter("胖", "子", 50, true, 0, 0, 0, CharacterRole.Ruler);
            fatty.obesity = 80f;
            Assert.AreEqual("胖子", EpithetSystem.EvaluateAndGrant(fatty, rec), "肥胖 80→胖子");

            // 篡位者
            var usurper = cm2.CreateCharacter("篡", "位", 50, true, 0, 0, 0, CharacterRole.Ruler);
            var recU = new EvaluationSystem.AchievementRecord { usurpedThrone = true };
            Assert.AreEqual("篡位者", EpithetSystem.EvaluateAndGrant(usurper, recU), "篡位→篡位者");

            // 和平者（在位久无战）
            var pacifist = cm2.CreateCharacter("和", "平", 50, true, 0, 0, 0, CharacterRole.Ruler);
            pacifist.boldness = -40f; pacifist.compassion = 30f;
            var recP = new EvaluationSystem.AchievementRecord { reignYears = 25f, rebellions = 2 };
            // 有内乱但对外无战→和平者（公正者需无叛乱——区分）
            Assert.AreEqual("和平者", EpithetSystem.EvaluateAndGrant(pacifist, recP), "对外无战在位久→和平者");

            // 铁锤（防御大捷）
            var hammer = cm2.CreateCharacter("铁", "锤", 50, true, 0, 0, 0, CharacterRole.Ruler);
            hammer.compassion = 0f;
            var recH = new EvaluationSystem.AchievementRecord { defensiveWins = 4, reignYears = 8f };
            Assert.AreEqual("铁锤", EpithetSystem.EvaluateAndGrant(hammer, recH), "防御大捷 4→铁锤");

            // 圣者（死后封圣——与圣君区分）
            var saint = cm2.CreateCharacter("圣", "者", 70, true, 0, 0, 0, CharacterRole.Commoner);
            saint.deathDay = 1; // 已死
            var recS = new EvaluationSystem.AchievementRecord { canonized = true };
            Assert.AreEqual("圣者", EpithetSystem.EvaluateAndGrant(saint, recS), "死后封圣→圣者");

            // 受爱戴者 vs 被憎恨者
            var beloved = cm2.CreateCharacter("爱", "民", 50, true, 0, 0, 0, CharacterRole.Ruler);
            beloved.compassion = 60f; beloved.greed = -20f; beloved.honor = 40f;
            var recL = new EvaluationSystem.AchievementRecord { reignYears = 20f, rebellions = 0 };
            Assert.AreEqual("受爱戴者", EpithetSystem.EvaluateAndGrant(beloved, recL), "无叛乱宽仁→受爱戴者");

            var hated = cm2.CreateCharacter("苛", "政", 50, true, 0, 0, 0, CharacterRole.Ruler);
            hated.compassion = -50f;
            var recT = new EvaluationSystem.AchievementRecord { reignYears = 12f, rebellions = 4 };
            Assert.AreEqual("被憎恨者", EpithetSystem.EvaluateAndGrant(hated, recT), "叛乱多苛政→被憎恨者");
        }
    }
}