using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using CivilizationEvolution.Core;
using CivilizationEvolution.Culture;
using CivilizationEvolution.Role;
using CivilizationEvolution.Thought;
using CivilizationEvolution.Politics;
using CivilizationEvolution.War;

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

        [Test]
        public void ThreeFaithForms_PersonalTenets()
        {
            var cm = new CharacterManager();
            var c = cm.CreateCharacter("信", "徒", 40, true, 0, 0, 0, CharacterRole.Commoner);

            // 创建时私人信仰=社会信仰
            cm.InitPrivateFaith(c);
            Assert.AreEqual(c.faithId, c.privateFaithId, "私人信仰初始=社会信仰");
            Assert.IsFalse(c.isSecretBeliever, "初始非秘密信徒");

            // 添加个人信条（借其他信仰——个人融合）
            Assert.IsTrue(cm.AddPersonalTenet(c, "doctrine_icon_veneration"), "添加个人信条");
            Assert.IsFalse(cm.AddPersonalTenet(c, "doctrine_icon_veneration"), "去重");
            Assert.AreEqual(1, c.personalTenets.Count, "仅一条");
            Assert.IsTrue(cm.HasFaithDivergence(c), "有信条→偏离社会信仰");

            // 移除后无偏离
            Assert.IsTrue(cm.RemovePersonalTenet(c, "doctrine_icon_veneration"), "移除");
            Assert.IsFalse(cm.HasFaithDivergence(c), "移除后无偏离");

            // 私人信仰≠社会信仰 → 秘密信仰
            c.privateFaithId = 200; // 私下信伊斯兰
            cm.UpdateSecretBelief(c);
            Assert.IsTrue(c.isSecretBeliever, "私人≠社会→秘密信仰标记");
        }

        [Test]
        public void ThreeFaithForms_InitKeepsExisting()
        {
            var cm = new CharacterManager();
            var c = cm.CreateCharacter("已", "信", 40, true, 0, 0, 0, CharacterRole.Commoner);
            c.privateFaithId = 300; // 已私下信佛
            cm.InitPrivateFaith(c);
            Assert.AreEqual(300, c.privateFaithId, "已有私人信仰不被覆盖");
        }

        [Test]
        public void Beneficiary_InheritanceLine_SoleHeir()
        {
            // 长子继承：长子线内（不能受益）——次子线外（可受益）
            var cm = new CharacterManager();
            var father = cm.CreateCharacter("父", "王", 60, true, 0, 0, 0, CharacterRole.Ruler);
            var eldest = cm.CreateCharacter("长", "子", 30, true, 0, 0, 0, CharacterRole.Commoner);
            var second = cm.CreateCharacter("次", "子", 25, true, 0, 0, 0, CharacterRole.Commoner);
            eldest.familyId = father.familyId;
            second.familyId = father.familyId;
            eldest.realmId = 1;
            second.realmId = 1;

            var realm = new RealmData { realmId = 1 };
            realm.monarchId = father.characterId;
            var law = InheritanceLaw.Primogeniture();

            // 领地 1 块（SoleHeir 线宽 1）
            Assert.IsTrue(SuccessionSystem.IsInInheritanceLine(realm, cm, law, eldest.characterId, 1), "长子在线内");
            Assert.IsFalse(SuccessionSystem.IsInInheritanceLine(realm, cm, law, second.characterId, 1), "次子线外——可受益");

            // 领地 2 块（均分场景线宽 2）——长子次子都在线内
            Assert.IsTrue(SuccessionSystem.IsInInheritanceLine(realm, cm, law, second.characterId, 2), "领地够分次子线内");
        }

        [Test]
        public void Beneficiary_DaughterSalicLaw()
        {
            // 萨利克（男子专属）：女儿完全无继承权——可当受益人（即便直系血脉）
            var cm = new CharacterManager();
            var father = cm.CreateCharacter("王", "父", 60, true, 0, 0, 0, CharacterRole.Ruler);
            var son = cm.CreateCharacter("儿", "子", 30, true, 0, 0, 0, CharacterRole.Commoner);
            var daughter = cm.CreateCharacter("女", "儿", 20, false, 0, 0, 0, CharacterRole.Commoner);
            son.familyId = father.familyId;
            daughter.familyId = father.familyId;
            son.realmId = 2;
            daughter.realmId = 2;

            var realm = new RealmData { realmId = 2 };
            realm.monarchId = father.characterId;
            var salic = InheritanceLaw.Salic();

            Assert.IsTrue(SuccessionSystem.IsInInheritanceLine(realm, cm, salic, son.characterId, 1), "儿子在线内");
            Assert.IsFalse(SuccessionSystem.IsInInheritanceLine(realm, cm, salic, daughter.characterId, 1), "女儿线外——可受益");
        }

        [Test]
        public void Fervor_WarBetweenFaiths()
        {
            // 异教冲突：双方信仰不同 → 热忱各 +25
            var faithA = new FaithSystem { faithId = 100, fervor = 40f };
            var faithB = new FaithSystem { faithId = 200, fervor = 40f };

            // 直接测 OnWarBetweenFaiths 逻辑（模拟 GameWorld 调用）
            faithA.AddFervor(25f);
            faithB.AddFervor(25f);
            Assert.AreEqual(65f, faithA.fervor, "异教冲突 A 热忱+25");
            Assert.AreEqual(65f, faithB.fervor, "异教冲突 B 热忱+25");
        }

        [Test]
        public void StateReligion_AndPatronSaint()
        {
            // 政权国教+主保圣人（用户设计：国教教统内的圣人）
            var realm = new RealmData { realmId = 1 };
            Assert.AreEqual(-1, realm.stateReligionId, "初始无国教");
            realm.stateReligionId = 104; // 选罗马公教会为国教
            Assert.AreEqual(104, realm.stateReligionId, "国教已设");

            // 政权主保圣人（简化：字段可设——校验由上层做）
            realm.statePatronSaintId = 5001;
            Assert.AreEqual(5001, realm.statePatronSaintId, "政权主保圣人可设");
        }

        [Test]
        public void SixSystems_CompleteData()
        {
            // 六大宗教体系全齐（44 节点）
            Assert.GreaterOrEqual(ContentRegistry.Religions.Count, 40, "六大体系数据完整");

            // 华夏：祭祀体系根 → 儒教（祭祀型）→ 谶纬（宗教学派）
            var huaxia = ReligionCatalog.Get(400);
            Assert.IsTrue(huaxia.IsRoot, "华夏祭祀体系=根");
            Assert.AreEqual("祭祀型", huaxia.worldview, "祭祀型世界观");

            var confucian = ReligionCatalog.Get(401);
            Assert.AreEqual(400, confucian.parentReligionId, "儒教←华夏祭祀体系");
            Assert.IsTrue(confucian.rites.Contains("郊祀礼"), "儒教=郊祀礼");

            var chenwei = ReligionCatalog.Get(402);
            Assert.AreEqual(ReligionNodeType.ReligiousSchool, chenwei.nodeType, "谶纬=宗教学派（儒教内）");

            // 道教：仙教 → 正一/全真（传统）
            var taoism = ReligionCatalog.Get(403);
            Assert.AreEqual(400, taoism.parentReligionId, "仙教←华夏祭祀体系");
            Assert.AreEqual(ReligionNodeType.Tradition, ReligionCatalog.Get(404).nodeType, "正一道=传统");
            Assert.AreEqual(ReligionNodeType.Tradition, ReligionCatalog.Get(405).nodeType, "全真道=传统");

            // 祆教：二元论 → 正统/祖尔万（异端）/马兹达克（异端）——帕西（迁徙）
            var zoroaster = ReligionCatalog.Get(500);
            Assert.AreEqual("二元论", zoroaster.worldview, "祆教=二元论");
            Assert.AreEqual("琐罗亚斯德", zoroaster.founder, "创教者");
            Assert.IsFalse(ReligionCatalog.Get(503).orthodoxy, "祖尔万=异端（被正统驱逐）");
            Assert.IsFalse(ReligionCatalog.Get(504).orthodoxy, "马兹达克=异端（被镇压）");
            Assert.AreEqual(501, ReligionCatalog.Get(502).parentReligionId, "帕西←正统派（迁徙支派）");

            // 摩尼教=独立宗教根（组织父=-1——学派父=祆教——聚合创生）
            var mani = ReligionCatalog.Get(505);
            Assert.IsTrue(mani.IsRoot, "摩尼教=独立根（创教）");
            Assert.AreEqual(500, mani.schoolParentId, "摩尼教学派父=祆教（聚合来源）");

            // 原始崇拜=era0 起点（mana——无教统）
            var animatism = ReligionCatalog.Get(600);
            Assert.AreEqual("mana", animatism.worldview, "原始崇拜=mana（无人格化）");
            Assert.IsFalse(animatism.hasSuccession, "原始崇拜无教统");
        }

        [Test]
        public void HolySite_CreateAndLoss()
        {
            // 创建圣地（动态——封圣/圣迹/朝圣传统）
            var world = new GameWorld();
            typeof(GameWorld).GetMethod("InitializeUnitDefs",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                .Invoke(world, null);

            // 反射初始化 _faithSystems（宗教运行时）
            var faithsField = typeof(GameWorld).GetField("_faithSystems",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var faith = new FaithSystem { faithId = 401, faithName = "儒教", fervor = 40f };
            var list = new List<FaithSystem> { faith };
            faithsField.SetValue(world, list);

            // 初始化 tiles（圣地地块）
            var tilesField = typeof(GameWorld).GetField("tiles",
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
            var tiles = new TileData[100];
            for (int i = 0; i < 100; i++)
            {
                tiles[i] = new TileData { tileIndex = i, ownerRealmId = -1 };
            }
            tilesField.SetValue(world, tiles);

            // 创建圣地
            Assert.IsTrue(world.CreateHolySite(401, 10), "创建圣地成功");
            Assert.IsFalse(world.CreateHolySite(401, 10), "去重——不重复创建");
            Assert.AreEqual(1, faith.holySiteTileIndices.Count, "圣地列表 1 个");
        }

        [Test]
        public void ReligionInnovation_Linkage()
        {
            // 华夏祭祀体系/儒教/祆教←文字（901）；原始崇拜=era0 无前置
            Assert.IsFalse(ReligionCatalog.IsAvailable(400, id => false), "无文字→华夏祭祀体系不可用");
            Assert.IsTrue(ReligionCatalog.IsAvailable(400, id => id == 901), "有文字→可用");
            Assert.IsTrue(ReligionCatalog.IsAvailable(600, id => false), "原始崇拜无前置——era0 可用");
            Assert.IsTrue(ReligionCatalog.IsAvailable(600, null), "宽松模式");
            Assert.IsTrue(ReligionCatalog.IsAvailable(999999, id => false), "未知 id 宽松");
        }

        [Test]
        public void SuccessionDoctrines_RealData()
        {
            // 教统支柱选择（数据——偏离度计算基础）
            var catholic = ReligionCatalog.Get(104);
            Assert.IsTrue(catholic.selectedDoctrines.Contains("doctrine_monotheism"), "天主教=一神论");
            Assert.IsTrue(catholic.selectedDoctrines.Contains("doctrine_clerical_appointment_spiritual"),
                "天主教=神职灵性任命（叙任权在教）");
            Assert.IsTrue(catholic.selectedDoctrines.Contains("doctrine_clerical_marriage_celibate"),
                "天主教=神职独身");

            var orthodox = ReligionCatalog.Get(105);
            Assert.IsTrue(orthodox.selectedDoctrines.Contains("doctrine_clerical_marriage_allowed"),
                "东正教=神职可婚（与天主教偏离——东西方差异教义化）");

            // 儒教=祖先崇拜+君权神授（祭祀型——天命）
            var confucian = ReligionCatalog.Get(401);
            Assert.IsTrue(confucian.selectedDoctrines.Contains("doctrine_ancestor_veneration"), "儒教=祖先崇拜");
            Assert.IsTrue(confucian.selectedDoctrines.Contains("doctrine_sacred_kingship"), "儒教=君权神授（天命）");

            // 什叶=伊玛目无误（专属教义）
            var shia = ReligionCatalog.Get(202);
            Assert.IsTrue(shia.selectedDoctrines.Contains("doctrine_imamate"), "什叶=伊玛目无误");

            // 祆教=二元论（善恶二神）
            var zoroaster = ReligionCatalog.Get(500);
            Assert.IsTrue(zoroaster.selectedDoctrines.Contains("doctrine_dualism"), "祆教=二元论");

            // 天主教 vs 东正教偏离（神职婚姻差异——伦理/制度支柱）
            float div = ReligionCatalog.GetDivergence(catholic, orthodox);
            Assert.Greater(div, 0f, "天主教/东正教有偏离（神职婚姻差异）");
        }

        [Test]
        public void Piety_SpiritualFulfillment()
        {
            // 灵性满足：美德增长/罪行下降/秘密信徒煎熬——clamp 0-100
            var cm = new CharacterManager();
            var faith = new FaithSystem { faithId = 104 };
            faith.virtues = new List<string> { "forgiving" };
            faith.sins = new List<string> { "lustful" };

            var saint = cm.CreateCharacter("圣", "徒", 40, true, 0, 0, 0, CharacterRole.Commoner);
            saint.traits = new List<PersonalityTrait>
            {
                new PersonalityTrait { traitId = "forgiving_3", traitName = "宽恕" },
                new PersonalityTrait { traitId = "forgiving_1", traitName = "宽恕" }
            };
            saint.spiritualFulfillment = 50f;
            for (int i = 0; i < 100; i++) cm.UpdatePiety(saint, faith);
            Assert.Greater(saint.spiritualFulfillment, 50f, "美德增长虔诚");
            Assert.LessOrEqual(saint.spiritualFulfillment, 100f, "虔诚上限");

            var sinner = cm.CreateCharacter("罪", "人", 40, true, 0, 0, 0, CharacterRole.Commoner);
            sinner.traits = new List<PersonalityTrait>
            {
                new PersonalityTrait { traitId = "lustful_2", traitName = "好色" }
            };
            sinner.spiritualFulfillment = 50f;
            for (int i = 0; i < 100; i++) cm.UpdatePiety(sinner, faith);
            Assert.Less(sinner.spiritualFulfillment, 50f, "罪行下降虔诚");
            Assert.GreaterOrEqual(sinner.spiritualFulfillment, 0f, "虔诚下限");

            // 秘密信徒煎熬（私人≠社会）
            var secret = cm.CreateCharacter("秘", "密", 40, true, 0, 0, 0, CharacterRole.Commoner);
            secret.spiritualFulfillment = 50f;
            secret.isSecretBeliever = true;
            cm.UpdatePiety(secret, null);
            Assert.Less(secret.spiritualFulfillment, 50f, "秘密信徒灵性煎熬");
        }

        [Test]
        public void GreatHolyWar_FullFlow()
        {
            GreatHolyWarSystem.ActiveWars.Clear();
            CanonizationSystem.Reset();

            // 发起条件：热忱≥60+有领袖+目标
            var war1 = GreatHolyWarSystem.Declare(104, 1, 2, 10, 100, hasLeader: false, fervor: 80f);
            Assert.IsNull(war1, "无领袖不可发起");
            var war2 = GreatHolyWarSystem.Declare(104, 1, 2, 10, 100, hasLeader: true, fervor: 50f);
            Assert.IsNull(war2, "热忱不足不可发起");
            var war3 = GreatHolyWarSystem.Declare(104, 1, 2, 10, 100, hasLeader: true, fervor: 80f);
            Assert.IsNotNull(war3, "条件齐→发起成功");
            Assert.AreEqual(1, war3.callerRealmId, "号召者=教统领袖政权");
            Assert.IsTrue(war3.participants.Contains(1), "号召者加入参战");

            // 号召：同教统政权强制加入（信仰义务）
            var realms = new List<RealmData>
            {
                new RealmData { realmId = 1, stateReligionId = 104 },
                new RealmData { realmId = 3, stateReligionId = 104 },
                new RealmData { realmId = 4, stateReligionId = 200 }
            };
            GreatHolyWarSystem.Rally(war3, realms);
            Assert.IsTrue(war3.participants.Contains(3), "同教统政权响应号召");
            Assert.IsFalse(war3.participants.Contains(4), "异教政权不响应");

            // 结算：圣战方胜→受益人=继承线外者
            var cm = new CharacterManager();
            var king = cm.CreateCharacter("王", "者", 60, true, 0, 0, 0, CharacterRole.Ruler);
            var eldest = cm.CreateCharacter("长", "子", 30, true, 0, 0, 0, CharacterRole.Commoner);
            var second = cm.CreateCharacter("次", "子", 25, true, 0, 0, 0, CharacterRole.Commoner);
            eldest.familyId = king.familyId; second.familyId = king.familyId;
            eldest.realmId = 1; second.realmId = 1;
            var callerRealm = new RealmData { realmId = 1 };
            callerRealm.monarchId = king.characterId;

            war3.holySideWon = true;
            int beneficiary = GreatHolyWarSystem.Resolve(war3, cm, callerRealm, 1,
                InheritanceLaw.Primogeniture());
            Assert.AreEqual(second.characterId, beneficiary, "次子=继承线外→受益人（长子在线内）");
            Assert.AreEqual(beneficiary, war3.beneficiaryId, "受益人记录");

            // 防御方胜利→无受益人
            var war4 = GreatHolyWarSystem.Declare(104, 1, 2, 10, 100, true, 80f);
            war4.holySideWon = false;
            Assert.AreEqual(-1, GreatHolyWarSystem.Resolve(war4, cm, callerRealm, 1,
                InheritanceLaw.Primogeniture()), "防御方胜→无受益人");
        }

        [Test]
        public void Canonization_SaintFromVirtuousDead()
        {
            CanonizationSystem.Reset();
            var faith = new FaithSystem { faithId = 104, highPriestCharacterId = 1 };
            faith.virtues = new List<string> { "forgiving", "compassionate", "chaste" };

            var cm = new CharacterManager();
            var alive = cm.CreateCharacter("活", "圣", 40, true, 0, 0, 0, CharacterRole.Commoner);
            alive.traits = new List<PersonalityTrait>
            {
                new PersonalityTrait { traitId = "forgiving_2", traitName = "宽恕" },
                new PersonalityTrait { traitId = "compassionate_3", traitName = "慈悲" }
            };
            alive.spiritualFulfillment = 90f;
            Assert.IsFalse(CanonizationSystem.IsCanonizationCandidate(faith, alive), "活着不能封圣");

            alive.deathDay = 1; // 死亡（isAlive=deathDay<0 只读）
            Assert.IsTrue(CanonizationSystem.IsCanonizationCandidate(faith, alive), "死后+虔诚90+美德2→候选");

            // 无领袖不批准
            var faithNoHead = new FaithSystem { faithId = 105, highPriestCharacterId = -1 };
            faithNoHead.virtues = faith.virtues;
            Assert.IsNull(CanonizationSystem.Canonize(faithNoHead, alive, "战争"), "无教会领袖不封圣");

            // 有领袖→圣人
            var saint = CanonizationSystem.Canonize(faith, alive, "战争");
            Assert.IsNotNull(saint, "封圣成功");
            Assert.AreEqual(alive.characterId, saint.linkedCharacterId, "圣人=角色升格");
            Assert.AreEqual("战争", saint.domain, "庇护领域");
            Assert.AreEqual(1, CanonizationSystem.GetSaints(104).Count, "圣人入池");

            // 防重复
            var again = CanonizationSystem.Canonize(faith, alive, "战争");
            Assert.AreEqual(saint.saintId, again.saintId, "防重复封圣");
        }

        [Test]
        public void Hierarchy_EcclesiasticalTitles()
        {
            var faith = new FaithSystem { faithId = 104 };
            Assert.AreEqual(0, faith.hierarchyLevel, "初始无教阶");

            faith.SetHierarchyLevel(2);
            Assert.AreEqual(2, faith.hierarchyLevel, "主教区级");

            // 叙任权：俗人任命（世俗君主任命——政教冲突基础）
            faith.AddHierarchyTitle("科隆大主教", 2, temporalAppointment: false);
            faith.AddHierarchyTitle("美因茨大主教", 3, temporalAppointment: true);
            Assert.AreEqual(3, faith.hierarchyLevel, "头衔升级到 大主教区");
            Assert.AreEqual(2, faith.hierarchyTitles.Count, "两个教阶头衔");
            Assert.IsTrue(faith.hierarchyTitles[1].temporalAppointment, "俗人任命=叙任权在君");
        }

        [Test]
        public void Missionary_ConversionAndTax()
        {
            // 传教成功率：同宗教易传/异教难
            var tile = new TileData
            {
                tileIndex = 0,
                populationBlocks = new List<PopulationBlock>
                {
                    new PopulationBlock { count = 100f, cultureId = 1, faithId = 105, socialClass = GameEnums.SocialClass.Peasant }
                }
            };
            // 同宗教（东正 105 → 天主教 104——同根 100）
            float same = MissionarySystem.CalculateSuccessChance(tile, 104,
                id => ReligionCatalog.Get(id), id => ReligionCatalog.GetRoot(id)?.religionId ?? -1);
            Assert.Greater(same, 0.4f, "同宗教不同教统——较易传");

            // 异教（105 → 伊斯兰 201——不同根）
            float diff = MissionarySystem.CalculateSuccessChance(tile, 201,
                id => ReligionCatalog.Get(id), id => ReligionCatalog.GetRoot(id)?.religionId ?? -1);
            Assert.Less(diff, 0.4f, "异教——难传");

            // ConvertTile：新建传教块（从主流分出）
            var rng = new System.Random(42);
            bool converted = MissionarySystem.ConvertTile(tile, 201, 1, 1f, rng);
            Assert.IsTrue(converted, "高成功率→转信成功");
            Assert.AreEqual(2, tile.populationBlocks.Count, "新建传教块");
            Assert.AreEqual(201, tile.populationBlocks[1].faithId, "新块=传教信仰");

            // 宗教税压力改宗（吉兹亚——农民先改——阶层参与）
            var tile2 = new TileData
            {
                tileIndex = 1,
                populationBlocks = new List<PopulationBlock>
                {
                    new PopulationBlock { count = 100f, cultureId = 1, faithId = 105, socialClass = GameEnums.SocialClass.Peasant },
                    new PopulationBlock { count = 100f, cultureId = 1, faithId = 105, socialClass = GameEnums.SocialClass.NobilityClergy }
                }
            };
            // 高税率多次尝试（农民 1.5× 概率——必然先改）
            bool taxChanged = false;
            for (int i = 0; i < 200 && !taxChanged; i++)
                taxChanged = MissionarySystem.TaxPressureConversion(tile2, 201, 40f);
            Assert.IsTrue(taxChanged, "高税率诱导改宗");
        }

        [Test]
        public void LiturgicalVsScripturalLanguage()
        {
            // 仪式语言（口头）× 经典语言（圣典）两维分离
            var catholic = ReligionCatalog.Get(104);
            Assert.AreEqual("拉丁语", catholic.liturgicalLanguage, "天主教弥撒=拉丁语（仪式）");
            Assert.IsTrue(catholic.scripturalLanguage.Contains("武加大"), "经典=武加大译本");

            var islam = ReligionCatalog.Get(201);
            Assert.AreEqual("古典阿拉伯语", islam.liturgicalLanguage, "伊斯兰礼拜=阿拉伯语（仪式必用）");
            Assert.IsTrue(islam.scripturalLanguage.Contains("不可译"), "古兰经不可译（经典神圣性）");

            // 佛教：仪式用本地语言——经典保留原文
            var theravada = ReligionCatalog.Get(301);
            Assert.IsTrue(theravada.scripturalLanguage.Contains("巴利语"), "上座部经典=巴利语（三藏）");
            Assert.IsTrue(theravada.liturgicalLanguage.Contains("本地语言"), "仪式用本地语言");

            // 儒教：文言=仪式+经典（祭祀祝辞+五经）
            var confucian = ReligionCatalog.Get(401);
            Assert.AreEqual("文言（祭祀祝辞）", confucian.liturgicalLanguage, "儒教仪式=文言祝辞");
            Assert.IsTrue(confucian.scripturalLanguage.Contains("五经"), "儒教经典=五经文言");

            // 祆教：阿维斯陀语=仪式+经典（火祭+经文）
            var zoroaster = ReligionCatalog.Get(500);
            Assert.AreEqual("阿维斯陀语", zoroaster.liturgicalLanguage, "祆教仪式=阿维斯陀语");
            Assert.AreEqual("阿维斯陀语（《阿维斯陀》）", zoroaster.scripturalLanguage, "祆教经典=阿维斯陀");

            // 原始崇拜=无文字（口语）
            var animatism = ReligionCatalog.Get(600);
            Assert.AreEqual("", animatism.liturgicalLanguage, "原始崇拜仪式=无（口语）");
            Assert.AreEqual("", animatism.scripturalLanguage, "原始崇拜无经典");
        }

        [Test]
        public void ReligionPanelText_Build()
        {
            // 面板文本（教统信息+两维语言+支柱+热忱+圣人）
            var succession = ReligionCatalog.Get(104);
            var faith = new FaithSystem { faithId = 104, fervor = 70f, highPriestCharacterId = 1 };
            var text = ReligionPanelText.Build(succession, faith, -1);

            Assert.IsTrue(text.Contains("罗马公教会"), "教统名");
            Assert.IsTrue(text.Contains("教宗"), "领袖");
            Assert.IsTrue(text.Contains("仪式语言：拉丁语"), "仪式语言显示");
            Assert.IsTrue(text.Contains("经典语言"), "经典语言显示");
            Assert.IsTrue(text.Contains("信仰热忱：70"), "热忱显示");
            Assert.IsTrue(text.Contains("大圣战可用"), "热忱≥60+领袖→可用提示");
            Assert.IsTrue(text.Contains("支柱选择"), "支柱区");
            Assert.IsTrue(text.Contains("一神论"), "支柱选项显示");

            // 无国教
            var none = ReligionPanelText.Build(null, null, -1);
            Assert.IsTrue(none.Contains("未确立国教"), "无国教提示");
        }
    }
}