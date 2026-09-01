using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using CivilizationEvolution.Core;
using CivilizationEvolution.Culture;
using CivilizationEvolution.Role;
using CivilizationEvolution.Thought;
using CivilizationEvolution.Politics;

namespace CivilizationEvolution.Tests
{
    /// <summary>
    /// 宗教系统数据层测试（ReligionDef 扩展/教义池/热忱/美德罪行/人口三维占比）
    /// </summary>
    public class ReligionSystemTests
    {
        [SetUp]
        public void Setup()
        {
            ContentRegistry.Reset();
            ContentRegistry.Initialize();
        }

        [Test]
        public void ReligionDef_ExtendedFields()
        {
            var catholic = ReligionCatalog.Get(104);
            Assert.IsNotNull(catholic, "罗马公教会存在");

            // 领袖三分离
            Assert.IsNotEmpty(catholic.headName, "教统领袖（教宗）");
            Assert.AreEqual("罗马共融", catholic.communionName, "共融归属");

            // 世界观（默认未配——数据待补——字段存在即可）
            Assert.IsNotNull(catholic.worldview, "世界观字段存在");
            Assert.IsNotNull(catholic.virtues, "美德列表存在");
            Assert.IsNotNull(catholic.sins, "罪行列表存在");
        }

        [Test]
        public void DoctrinePool_LoadsByPillar()
        {
            Assert.GreaterOrEqual(ContentRegistry.Doctrines.Count, 30, "教义池已加载");

            // 神观组
            var mono = DoctrinePool.Get("doctrine_monotheism");
            Assert.IsNotNull(mono, "一神论在池中");
            Assert.AreEqual("doctrine", mono.pillar, "神观归教义支柱");

            // 伦理态度组（议题×档位）
            var homoCrime = DoctrinePool.Get("doctrine_homosexuality_crime");
            var homoAccepted = DoctrinePool.Get("doctrine_homosexuality_accepted");
            Assert.IsNotNull(homoCrime, "同性恋·罪行");
            Assert.IsNotNull(homoAccepted, "同性恋·接纳");
            Assert.AreEqual("ethics", homoCrime.pillar, "态度归伦理教法支柱");

            // 天葬归仪式支柱（用户修正）
            var skyBurial = DoctrinePool.Get("doctrine_funeral_sky_burial");
            Assert.IsNotNull(skyBurial, "天葬在池中");
            Assert.AreEqual("ritual", skyBurial.pillar, "天葬=仪式支柱");
        }

        [Test]
        public void DoctrinePool_ExclusiveAndFilter()
        {
            // 专属风味化：无念=佛教专属（300）
            var noMind = DoctrinePool.Get("doctrine_no_mind");
            Assert.IsNotNull(noMind, "无念存在");
            Assert.IsTrue(noMind.exclusiveReligionIds.Contains(300), "无念=佛教专属");

            // 过滤：佛教（300）能看到无念；基督教（100）看不到
            var buddhist = DoctrinePool.GetOptions("doctrine", 300);
            Assert.IsTrue(buddhist.Exists(o => o.optionId == "doctrine_no_mind"), "佛教可见无念");
            var christian = DoctrinePool.GetOptions("doctrine", 100);
            Assert.IsFalse(christian.Exists(o => o.optionId == "doctrine_no_mind"), "基督教不可见无念");

            // 专属升级来源：伊玛目无误=君权神授升级（enhancedFrom）
            var imamate = DoctrinePool.Get("doctrine_imamate");
            Assert.AreEqual("doctrine_sacred_kingship", imamate.enhancedFrom, "伊玛目无误升级自君权神授");
        }

        [Test]
        public void FaithSystem_FervorAndGreatHolyWar()
        {
            var faith = new FaithSystem { faithId = 100 };

            // 默认 50——不可大圣战（<60）
            Assert.IsFalse(faith.CanDeclareGreatHolyWar(), "热忱 50 不可大圣战");
            faith.highPriestCharacterId = 1;
            Assert.IsFalse(faith.CanDeclareGreatHolyWar(), "有领袖但热忱不足");

            // 圣地丢失 +50 → 100
            faith.AddFervor(50f);
            Assert.AreEqual(100f, faith.fervor, "热忱增长");
            Assert.IsTrue(faith.CanDeclareGreatHolyWar(), "热忱 100 可大圣战");

            // clamp 不越界
            faith.AddFervor(200f);
            Assert.AreEqual(100f, faith.fervor, "热忱上限 100");
            faith.AddFervor(-500f);
            Assert.AreEqual(0f, faith.fervor, "热忱下限 0");
        }

        [Test]
        public void FaithSystem_VirtueSinScores()
        {
            var faith = new FaithSystem { faithId = 100 };
            faith.virtues = new List<string> { "forgiving", "compassionate" };
            faith.sins = new List<string> { "lustful", "greedy" };

            var cm = new CharacterManager();
            var saint = cm.CreateCharacter("圣", "人", 40, true, 0, 0, 0, CharacterRole.Commoner);
            saint.traits = new List<PersonalityTrait>
            {
                new PersonalityTrait { traitId = "forgiving_3", traitName = "宽恕" },
                new PersonalityTrait { traitId = "compassionate_2", traitName = "慈悲" }
            };
            var sinner = cm.CreateCharacter("罪", "人", 40, true, 0, 0, 0, CharacterRole.Commoner);
            sinner.traits = new List<PersonalityTrait>
            {
                new PersonalityTrait { traitId = "lustful_2", traitName = "好色" },
                new PersonalityTrait { traitId = "greedy_1", traitName = "贪念" }
            };

            Assert.AreEqual(2, faith.GetVirtueScore(saint), "持两美德");
            Assert.AreEqual(0, faith.GetSinScore(saint), "圣人无罪");
            Assert.AreEqual(2, faith.GetSinScore(sinner), "罪人持两罪");
            Assert.AreEqual(0, faith.GetVirtueScore(sinner), "罪人无美德");
        }

        [Test]
        public void PopulationStats_ThreeDimensionShares()
        {
            var tile = new TileData
            {
                tileIndex = 0,
                populationBlocks = new List<PopulationBlock>
                {
                    new PopulationBlock { count = 70f, cultureId = 1, faithId = 100, socialClass = GameEnums.SocialClass.Peasant },
                    new PopulationBlock { count = 30f, cultureId = 2, faithId = 200, socialClass = GameEnums.SocialClass.Slave }
                }
            };

            // 文化占比
            Assert.That(PopulationStats.GetCultureShare(tile, 1), Is.EqualTo(0.7f).Within(0.001f), "文化1 占 70%");
            Assert.That(PopulationStats.GetCultureShare(tile, 2), Is.EqualTo(0.3f).Within(0.001f), "文化2 占 30%");

            // 信仰占比
            Assert.That(PopulationStats.GetFaithShare(tile, 100), Is.EqualTo(0.7f).Within(0.001f), "信仰100 占 70%");

            // 主流（count 最大块）
            Assert.AreEqual(1, PopulationStats.GetDominantCulture(tile), "主流文化=文化1");
            Assert.AreEqual(100, PopulationStats.GetDominantFaith(tile), "主流信仰=信仰100");
        }

        [Test]
        public void Evolution_CreateSuccession_Schism()
        {
            // 裂教：迦克墩（101）裂出东正教——新教统节点（同宗教内——根不变）
            var orthodox = ReligionCatalog.CreateSuccession(101, "正统大公教会", "牧首", "拜占庭礼", "拜占庭传统");
            Assert.IsNotNull(orthodox, "裂教创建新教统");
            Assert.AreEqual(ReligionNodeType.Succession, orthodox.nodeType, "裂教产物=教统");
            Assert.AreEqual(101, orthodox.parentReligionId, "组织父=迦克墩派");
            Assert.AreEqual("牧首", orthodox.headName, "新教统领袖");
            Assert.IsTrue(orthodox.rites.Contains("拜占庭礼"), "新教统分家礼仪");

            // 根不变（仍是基督教体系）
            var root = ReligionCatalog.GetRoot(orthodox.religionId);
            Assert.AreEqual(100, root.religionId, "裂教=同宗教内分裂（根不变）——不是创教");
        }

        [Test]
        public void Evolution_CreateReligion_NewRoot()
        {
            // 创教：异端升格→新宗教根（跨宗教）
            var newRel = ReligionCatalog.CreateReligion("新教", "一神", "先知甲");
            Assert.IsNotNull(newRel, "创教创建新宗教");
            Assert.AreEqual(ReligionNodeType.Religion, newRel.nodeType, "创教产物=宗教根");
            Assert.AreEqual(-1, newRel.parentReligionId, "新宗教独立于原宗教树");

            // 与裂教区别：裂教根不变/创教新根
            var schism = ReligionCatalog.CreateSuccession(100, "裂出派", "领袖", "", "");
            Assert.AreEqual(100, ReligionCatalog.GetRoot(schism.religionId).religionId, "裂教=同根");
            Assert.AreEqual(newRel.religionId, ReligionCatalog.GetRoot(newRel.religionId).religionId, "创教=新根");
        }

        [Test]
        public void Divergence_DoctrineOptionDifferences()
        {
            // 同选项=0 偏离
            var a = new ReligionDef { religionId = 9001, religionName = "A" };
            a.selectedDoctrines.Add("doctrine_monotheism");
            var b = new ReligionDef { religionId = 9002, religionName = "B" };
            b.selectedDoctrines.Add("doctrine_monotheism");
            Assert.AreEqual(0f, ReligionCatalog.GetDivergence(a, b), "同选项=0 偏离");

            // 对立选项（一神 vs 多神——教义支柱权重 0.30×30×1.0）→ 偏离 > 0
            var c = new ReligionDef { religionId = 9003, religionName = "C" };
            c.selectedDoctrines.Add("doctrine_polytheism");
            float d1 = ReligionCatalog.GetDivergence(a, c);
            Assert.Greater(d1, 0f, "对立选项有偏离");
            Assert.LessOrEqual(d1, 100f, "偏离上限 100");

            // 无选择=0（未配支柱的节点无偏离）
            var empty = new ReligionDef { religionId = 9004, religionName = "空" };
            Assert.AreEqual(0f, ReligionCatalog.GetDivergence(a, empty), "空选择=0");
        }

        [Test]
        public void Divergence_Threshold_Heresy()
        {
            // 传统偏离 80+ = 异端条件（裂教门槛）
            var orthodoxStandard = new ReligionDef { religionId = 9100, religionName = "正统" };
            orthodoxStandard.selectedDoctrines.Add("doctrine_monotheism");
            orthodoxStandard.selectedDoctrines.Add("doctrine_icon_veneration");

            var heretic = new ReligionDef { religionId = 9101, religionName = "异端" };
            heretic.selectedDoctrines.Add("doctrine_polytheism");
            heretic.selectedDoctrines.Add("doctrine_iconoclasm");
            heretic.selectedDoctrines.Add("doctrine_human_sacrifice");

            // 多选项冲突累积——异端偏离应显著高于多样性
            var moderate = new ReligionDef { religionId = 9102, religionName = "温和" };
            moderate.selectedDoctrines.Add("doctrine_monotheism");
            moderate.selectedDoctrines.Add("doctrine_icon_veneration");

            float dHeretic = ReligionCatalog.GetDivergence(orthodoxStandard, heretic);
            float dModerate = ReligionCatalog.GetDivergence(orthodoxStandard, moderate);
            Assert.Greater(dHeretic, dModerate, "异端偏离 > 温和偏离");
    }
}
}
