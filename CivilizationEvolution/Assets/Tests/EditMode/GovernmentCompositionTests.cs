using System.Collections.Generic;
using NUnit.Framework;
using CivilizationEvolution.Core;
using CivilizationEvolution.Politics;
using CivilizationEvolution.Role;

namespace CivilizationEvolution.Tests
{
    /// <summary>
    /// 政体系统 EditMode 测试（用户定稿 v3：七维——三权力层级×交接/分配 + 央地结构）
    /// 政体 = 七维成分自由组合；成分是模组化接口；继承法=最高权力·交接=世袭子选项
    /// </summary>
    public class GovernmentCompositionTests
    {
        [SetUp]
        public void Setup()
        {
            ContentRegistry.Reset();
            ContentRegistry.Initialize();
        }

        // ===== 继承法双轨（文明默认 × 国家覆盖——借鉴《地图上发生的事》） =====

        [Test]
        public void SuccessionLaw_TwoTier_CultureDefaultThenOverride()
        {
            // 第一轨：跟随文化默认（Laethis=宗祧析产：头衔唯一+领地均分）
            var realm = new RealmData { realmId = 1, primaryCultureId = 1 };
            realm.successionLawFromCulture = true;
            Assert.AreEqual(LandInheritanceMode.Partible, realm.SuccessionLaw.landMode,
                "跟随文化默认：领地均分（析产）");

            // 第二轨：国家覆盖（改自定继承法）
            realm.successionLawFromCulture = false;
            realm.composition.successionLaw = InheritanceLaw.Tanistry();
            Assert.AreEqual(InheritanceGender.MaleOnly, realm.SuccessionLaw.gender,
                "国家覆盖：兄终弟及男子专属");
        }

        [Test]
        public void SupremeRuler_MonarchConsulDualTrack()
        {
            // 君主制：monarch 生效
            var realm = new RealmData { realmId = 1 };
            realm.monarchId = 10;
            realm.consulId = 20;
            Assert.AreEqual(10, realm.GetSupremeRulerId(), "君主制取君主");

            // 共和制（君主空缺）：执政官生效
            realm.monarchId = -1;
            Assert.AreEqual(20, realm.GetSupremeRulerId(), "共和制取执政官");

            // 无人：-1
            realm.consulId = -1;
            Assert.AreEqual(-1, realm.GetSupremeRulerId());
        }

        [Test]
        public void SupremeRuler_RuntimeFields()
        {
            var realm = new RealmData { realmId = 1 };
            realm.heirId = 30;
            realm.senateSeats = 300;
            Assert.AreEqual(30, realm.heirId, "继承人指针");
            Assert.AreEqual(300, realm.senateSeats, "元老院席位");
        }

        // ===== 七维结构完整性 =====

        [Test]
        public void SevenDimensions_AllPresent()
        {
            var comp = new GovernmentComposition();
            // A. 最高权力
            Assert.AreEqual((int)SupremeSuccession.Hereditary, comp.supremeSuccession.primary);
            Assert.AreEqual((int)SupremeScope.Absolute, comp.supremeScope.primary);
            // B. 中央权力
            Assert.AreEqual((int)CentralSuccession.Appointed, comp.centralSuccession.primary);
            Assert.AreEqual((int)CentralInstitution.BureaucraticCore, comp.centralInstitution.primary);
            // C. 地方权力
            Assert.AreEqual((int)LocalSuccession.Appointed, comp.localSuccession.primary);
            Assert.AreEqual((int)LocalScope.FiscalJudicial, comp.localScope.primary);
            // D. 央地结构（第七维）
            Assert.AreEqual((int)SpatialStructure.Unitary, comp.spatialStructure.primary, "央地结构独立维度");

            // 世袭子选项=继承法（默认长子继承）
            Assert.AreEqual(InheritanceGender.MalePreference, comp.successionLaw.gender);
        }

        // ===== 细分成分（A1 九选/议会三形态/领主两分/央地结构） =====

        [Test]
        public void SupremeSuccession_LeveledOptions()
        {
            // 层级1 推导（用户定稿：君主/共和=交接方式×权力分配 推导，非独立枚举）
            // 个人传承系 → 君主
            Assert.IsTrue(SupremeSuccessionLevel.IsMonarchy(SupremeSuccession.Hereditary, SupremeScope.Absolute));
            Assert.IsTrue(SupremeSuccessionLevel.IsMonarchy(SupremeSuccession.Usurpation, SupremeScope.Absolute));
            Assert.IsTrue(SupremeSuccessionLevel.IsMonarchy(SupremeSuccession.Divine, SupremeScope.DivinelyBound), "神命君主");

            // 选举系：A2=全能 → 选举君主（教宗/大汗）；A2=共议 → 共和（雅典/罗马/威尼斯）
            Assert.IsTrue(SupremeSuccessionLevel.IsMonarchy(SupremeSuccession.ElectiveDirect, SupremeScope.Absolute),
                "选举+全能=选举君主");
            Assert.IsTrue(SupremeSuccessionLevel.IsRepublic(SupremeSuccession.ElectiveDirect, SupremeScope.Consensual),
                "选举+共议=共和——共和本身也是选举！");
            Assert.IsTrue(SupremeSuccessionLevel.IsRepublic(SupremeSuccession.ElectiveRepresentative, SupremeScope.LegallyBound),
                "选举·代议+受限=共和");
            Assert.IsTrue(SupremeSuccessionLevel.IsRepublic(SupremeSuccession.Rotation, SupremeScope.Consensual),
                "轮座+共议=共和（原始形态）");

            // 经典政体层级抽查（实例推导）
            Assert.IsTrue(SupremeSuccessionLevel.IsRepublic(GovernmentComposition.AthenianDemocracy()), "雅典=共和");
            Assert.IsTrue(SupremeSuccessionLevel.IsRepublic(GovernmentComposition.SenatorialRepublic()), "罗马=共和（贵族共和——选举+贵族范围）");
            Assert.IsTrue(SupremeSuccessionLevel.IsRepublic(GovernmentComposition.VenetianRepublic()), "威尼斯=共和");
            Assert.IsTrue(SupremeSuccessionLevel.IsMonarchy(GovernmentComposition.HolyRomanEmpire()), "神罗=君主（选举君主：选帝侯选出+个人全能）");
            Assert.IsTrue(SupremeSuccessionLevel.IsMonarchy(GovernmentComposition.MongolHorde()), "蒙古=君主（选举君主：忽里台+个人全能）");
            Assert.IsTrue(SupremeSuccessionLevel.IsRepublic(GovernmentComposition.ConstitutionalMonarchy()), "君主立宪=共和实质（主权在议会）");
        }

        [Test]
        public void SupremeSuccession_RefinedOptions()
        {
            // 选举君主 ≠ 民主共和：教宗/神罗有君主，雅典无君主——分界在 A2 权力分配
            Assert.AreEqual((int)SupremeSuccession.ElectiveRepresentative, GovernmentComposition.HolyRomanEmpire().supremeSuccession.primary,
                "神罗=选举·代议（选帝侯选举人团）");
            Assert.AreEqual(SupremeScope.Absolute, (SupremeScope)GovernmentComposition.HolyRomanEmpire().supremeScope.primary,
                "神罗皇帝个人全能 → 选举君主");
            Assert.AreEqual((int)SupremeSuccession.ElectiveDirect, GovernmentComposition.AthenianDemocracy().supremeSuccession.primary,
                "雅典=选举·直接（公民大会）");
            Assert.AreEqual(SupremeScope.Consensual, (SupremeScope)GovernmentComposition.AthenianDemocracy().supremeScope.primary,
                "雅典共议制约 → 共和");

            // 贵族共和=选举+贵族范围（scope 要素——不是独立形态！）
            var rome = GovernmentComposition.SenatorialRepublic();
            Assert.AreEqual((int)SupremeSuccession.ElectiveDirect, rome.supremeSuccession.primary, "罗马=选举");
            Assert.AreEqual(EligibilityScope.Citizens, rome.supremeEligibility.scope, "罗马选举范围=公民（百人团）");
            var venice = GovernmentComposition.VenetianRepublic();
            Assert.AreEqual(EligibilityScope.Nobility, venice.supremeEligibility.scope, "威尼斯选举范围=贵族（大议会）——贵族共和的'贵族'是范围！");

            // 蒙古：选举·直接（忽里台）+ 世袭次要
            var mongol = GovernmentComposition.MongolHorde();
            Assert.AreEqual((int)SupremeSuccession.ElectiveDirect, mongol.supremeSuccession.primary);
            Assert.IsTrue(mongol.supremeSuccession.Contains((int)SupremeSuccession.Hereditary), "黄金家族世袭候选为次要成分");
        }

        [Test]
        public void CentralInstitution_AssemblyThreeForms()
        {
            // 议会三形态：一院（雅典公民大会/罗马元老院）/两院（英国上下院）/等级会议（三级会议/帝国议会）
            Assert.AreEqual((int)CentralInstitution.Assembly, GovernmentComposition.AthenianDemocracy().centralInstitution.primary);
            Assert.AreEqual((int)CentralInstitution.Assembly, GovernmentComposition.ConstitutionalMonarchy().centralInstitution.primary);
            Assert.AreEqual((int)CentralInstitution.Assembly, GovernmentComposition.HolyRomanEmpire().centralInstitution.primary);
        }

        [Test]
        public void LocalSuccession_LordTwoForms()
        {
            // 领主两分：世袭封臣（封建契约）vs 宗室采邑（分封宗亲）
            Assert.AreEqual((int)LocalSuccession.Hereditary, GovernmentComposition.MongolHorde().localSuccession.primary);
            Assert.AreEqual((int)LocalSuccession.Hereditary, GovernmentComposition.FeudalFiefdom().localSuccession.primary);
            // 城市特许自治（自由城市）
            Assert.AreEqual((int)LocalSuccession.CityCharter, GovernmentComposition.VenetianRepublic().localSuccession.primary);
        }

        [Test]
        public void SpatialStructure_SeventhDimension()
        {
            // 央地结构独立：秦=单一/罗马共和=联邦/西周神罗蒙古=邦联
            Assert.AreEqual((int)SpatialStructure.Unitary, GovernmentComposition.BureaucraticMonarchy().spatialStructure.primary);
            Assert.AreEqual((int)SpatialStructure.Federal, GovernmentComposition.SenatorialRepublic().spatialStructure.primary);
            Assert.AreEqual((int)SpatialStructure.Confederal, GovernmentComposition.FeudalFiefdom().spatialStructure.primary);
            Assert.AreEqual((int)SpatialStructure.Confederal, GovernmentComposition.HolyRomanEmpire().spatialStructure.primary);
        }

        // ===== 概念边界（用户定稿修正） =====

        [Test]
        public void ConstitutionalMonarchy_SovereigntyInParliament()
        {
            // 君主立宪：最高权力在议会（代议）——非世袭君主！
            var comp = GovernmentComposition.ConstitutionalMonarchy();
            Assert.AreEqual((int)SupremeSuccession.ElectiveRepresentative, comp.supremeSuccession.primary,
                "最高权力在议会，不在君主");
            Assert.AreEqual((int)CentralInstitution.Assembly, comp.centralInstitution.primary);
        }

        [Test]
        public void EldersCouncil_NotRepublican()
        {
            // 长老议事会（长老资格制）≠ 议会/元老院（共和制）
            var comp = new GovernmentComposition
            {
                supremeSuccession = new ComponentChoice((int)SupremeSuccession.ElectiveDirect),
                centralInstitution = new ComponentChoice((int)CentralInstitution.EldersCouncil)
            };
            Assert.AreEqual((int)CentralInstitution.EldersCouncil, comp.centralInstitution.primary);
            var republic = GovernmentComposition.SenatorialRepublic();
            Assert.AreEqual((int)CentralInstitution.Assembly, republic.centralInstitution.primary,
                "共和制（罗马）中央机构为元老院，非长老会");
        }

        // ===== 经典政体（11 个）抽查 =====

        [Test]
        public void ClassicalPolities_Sample()
        {
            // 秦式：官僚中枢+完全直辖
            var qin = GovernmentComposition.BureaucraticMonarchy();
            Assert.AreEqual((int)CentralInstitution.BureaucraticCore, qin.centralInstitution.primary);
            Assert.AreEqual((int)LocalScope.None, qin.localScope.primary);
            Assert.AreEqual(LandInheritanceMode.Partible, qin.successionLaw.landMode, "析产");

            // 蒙古：军事委员会+兄终弟及
            var mongol = GovernmentComposition.MongolHorde();
            Assert.AreEqual((int)CentralInstitution.MilitaryCouncil, mongol.centralInstitution.primary);
            Assert.AreEqual(InheritanceGender.MaleOnly, mongol.successionLaw.gender);

            // 神权：神命+宗教会议
            var theo = GovernmentComposition.Theocracy();
            Assert.AreEqual((int)SupremeSuccession.Divine, theo.supremeSuccession.primary);
            Assert.AreEqual((int)CentralInstitution.ReligiousCouncil, theo.centralInstitution.primary);

            // 罗马帝国：皇帝+元老院（与共和元老院层级对等）
            var empire = GovernmentComposition.ImperialSenate();
            var republic = GovernmentComposition.SenatorialRepublic();
            Assert.AreEqual(empire.centralInstitution.primary, republic.centralInstitution.primary,
                "皇帝制与共和制元老院同层级");
            Assert.AreNotEqual(empire.supremeSuccession.primary, republic.supremeSuccession.primary);

            // 威尼斯：委员会选举+两院+城市特许
            var venice = GovernmentComposition.VenetianRepublic();
            Assert.AreEqual((int)SupremeSuccession.ElectiveDirect, venice.supremeSuccession.primary);
            Assert.AreEqual((int)CentralInstitution.Assembly, venice.centralInstitution.primary);
            Assert.AreEqual((int)LocalSuccession.CityCharter, venice.localSuccession.primary);

            // 储君制帝国：生前指定+考课晋升
            // 储君预立=世袭内部预案（非独立制度——已并入世袭）
            Assert.AreEqual((int)SupremeSuccession.Hereditary, GovernmentComposition.BureaucraticMonarchy().supremeSuccession.primary,
                "秦=世袭（预立太子为内部预案）");
            Assert.AreEqual((int)CentralSuccession.Appointed, GovernmentComposition.BureaucraticMonarchy().centralSuccession.primary,
                "秦中央=君主任命");
        }

        // ===== 组合自由性（模组化接口：任意组合合法） =====

        [Test]
        public void FreeCombination_AnySevenComponents()
        {
            // 任意组合：僭主+惯例约束+考试选任+王庭+教区委任+仅军事+联邦制
            var comp = new GovernmentComposition
            {
                supremeSuccession = new ComponentChoice((int)SupremeSuccession.Usurpation),
                supremeScope = new ComponentChoice((int)SupremeScope.CustomBound),
                centralSuccession = new ComponentChoice((int)CentralSuccession.Examination),
                centralInstitution = new ComponentChoice((int)CentralInstitution.Court),
                localSuccession = new ComponentChoice((int)LocalSuccession.Appointed),
                localScope = new ComponentChoice((int)LocalScope.MilitaryOnly),
                spatialStructure = new ComponentChoice((int)SpatialStructure.Federal)
            };
            Assert.AreEqual((int)SupremeSuccession.Usurpation, comp.supremeSuccession.primary);
            Assert.AreEqual((int)SupremeScope.CustomBound, comp.supremeScope.primary);
            Assert.AreEqual((int)SpatialStructure.Federal, comp.spatialStructure.primary);
            StringAssert.Contains("王庭", comp.GetName());
            StringAssert.Contains("联邦制", comp.GetName());
        }

        // ===== 次要成分（○）支持 =====

        [Test]
        public void SecondaryComponents_Supported()
        {
            var comp = new GovernmentComposition();
            comp.supremeSuccession = new ComponentChoice((int)SupremeSuccession.Hereditary, (int)SupremeSuccession.ElectiveDirect);
            Assert.IsTrue(comp.supremeSuccession.Contains((int)SupremeSuccession.Hereditary));
            Assert.IsTrue(comp.supremeSuccession.Contains((int)SupremeSuccession.ElectiveDirect));
            Assert.AreEqual(1, comp.supremeSuccession.secondary.Count, "0~2 个次要");
        }

        [Test]
        public void Eligibility_GenderCrosscutsAllSuccession()
        {
            // 用户定稿：性别等要素横切所有交接方式——选举/推举/轮座/神命同样有资格问题
            // 雅典（选举）：男子专属·本城邦公民
            var athens = GovernmentComposition.AthenianDemocracy();
            Assert.AreEqual(InheritanceGender.MaleOnly, athens.supremeEligibility.gender, "雅典选举：男子专属被选举权");
            Assert.AreEqual(EligibilityScope.Citizens, athens.supremeEligibility.scope, "雅典：限本城邦公民");

            // 罗马共和（推举）：男子专属·公民
            var rome = GovernmentComposition.SenatorialRepublic();
            Assert.AreEqual(InheritanceGender.MaleOnly, rome.supremeEligibility.gender, "罗马推举：男子专属");

            // 神权（神命）：男子专属·教阶
            var theo = GovernmentComposition.Theocracy();
            Assert.AreEqual(EligibilityScope.Clergy, theo.supremeEligibility.scope, "神权：限教阶");

            // 蒙古（轮座/世袭）：男子专属·贵族（忽里台）
            var mongol = GovernmentComposition.MongolHorde();
            Assert.AreEqual(EligibilityScope.Nobility, mongol.supremeEligibility.scope, "蒙古：限贵族");

            // 资格过滤验证：男子专属时女性被过滤
            var filtered = rome.supremeEligibility.Filter(new List<CharacterData>
            {
                MakeEligibleChar(1, true), MakeEligibleChar(2, false)
            });
            Assert.AreEqual(1, filtered.Count, "男子专属：女性被过滤");
            Assert.IsTrue(filtered[0].isMale);
        }

        [Test]
        public void Eligibility_Salic_TwoTier()
        {
            // 萨利克双轨：资格=男子专属（门槛）+ 继承排序=男子优先（偏好）
            // 资格规则在 composition.eligibility，排序在 successionLaw.gender
            var comp = new GovernmentComposition();
            comp.supremeEligibility = new EligibilityRules { gender = InheritanceGender.MaleOnly, scope = EligibilityScope.ClanOnly };
            comp.successionLaw = InheritanceLaw.Salic();

            Assert.AreEqual(InheritanceGender.MaleOnly, comp.supremeEligibility.gender, "资格门槛：男子专属");
            Assert.AreEqual(InheritanceGender.MaleOnly, comp.successionLaw.gender, "继承排序：男子专属（萨利克）");
            Assert.IsTrue(comp.supremeEligibility.IsGenderEligible(true), "男性合格");
            Assert.IsFalse(comp.supremeEligibility.IsGenderEligible(false), "女性不合格");
        }

        [Test]
        public void Eligibility_LayeredByPowerLevel()
        {
            // 用户定稿：性别等要素横切所有交接方式，且分层应用——
            // 最高/中央/地方各层资格可不同（中世纪欧洲：最高男子优先、地方可有女领主）
            var comp = new GovernmentComposition();
            comp.supremeEligibility = new EligibilityRules { gender = InheritanceGender.MalePreference };
            comp.centralEligibility = new EligibilityRules { gender = InheritanceGender.MaleOnly };
            comp.localEligibility = new EligibilityRules { gender = InheritanceGender.Equal };

            // 各层独立判定
            Assert.IsTrue(comp.supremeEligibility.IsGenderEligible(false), "最高层男子优先：女性可继承（排序靠后）");
            Assert.IsFalse(comp.centralEligibility.IsGenderEligible(false), "中央层男子专属：女性不可为官");
            Assert.IsTrue(comp.localEligibility.IsGenderEligible(false), "地方层平等：女领主可领有");
        }

        [Test]
        public void Eligibility_ScopeMapsToSocialClass()
        {
            // 用户定稿：资格范围与经济系统阶层（SocialClass）对接
            // 限贵族=王室+贵族教士；限自由民=不含奴隶；全体=含奴隶
            var nobility = new EligibilityRules { scope = EligibilityScope.Nobility };
            Assert.IsTrue(nobility.IsScopeEligible(GameEnums.SocialClass.Royalty), "王室在贵族范围");
            Assert.IsTrue(nobility.IsScopeEligible(GameEnums.SocialClass.NobilityClergy), "贵族教士在贵族范围");
            Assert.IsFalse(nobility.IsScopeEligible(GameEnums.SocialClass.Peasant), "农民不在贵族范围");
            Assert.IsFalse(nobility.IsScopeEligible(GameEnums.SocialClass.Slave), "奴隶不在贵族范围");

            var free = new EligibilityRules { scope = EligibilityScope.FreePeople };
            Assert.IsTrue(free.IsScopeEligible(GameEnums.SocialClass.Peasant), "农民是自由民");
            Assert.IsFalse(free.IsScopeEligible(GameEnums.SocialClass.Slave), "奴隶非自由民");

            var all = new EligibilityRules { scope = EligibilityScope.All };
            Assert.IsTrue(all.IsScopeEligible(GameEnums.SocialClass.Slave), "全体含奴隶");

            var clergy = new EligibilityRules { scope = EligibilityScope.Clergy };
            Assert.IsTrue(clergy.IsScopeEligible(GameEnums.SocialClass.NobilityClergy), "教士阶层");
            Assert.IsFalse(clergy.IsScopeEligible(GameEnums.SocialClass.Peasant), "农民非教阶");
        }

        private static CharacterData MakeEligibleChar(int id, bool isMale)
        {
            return new CharacterData
            {
                characterId = id,
                firstName = "C" + id,
                lastName = "氏",
                age = 30,
                isMale = isMale,
                familyId = 1
                // isAlive 只读（deathDay<0 推导），默认存活
            };
        }

        // ===== RealmData 挂载 =====

        [Test]
        public void RealmData_Composition_Integrated()
        {
            var realm = new RealmData { realmId = 1 };
            Assert.IsNotNull(realm.composition, "政权应带七维政体成分");
            Assert.AreEqual((int)SpatialStructure.Unitary, realm.composition.spatialStructure.primary);
            Assert.AreEqual(InheritanceGender.MalePreference, realm.SuccessionLaw.gender, "默认长子继承");
            // 可整体替换为蒙古汗国式
            realm.composition = GovernmentComposition.MongolHorde();
            Assert.AreEqual(InheritanceGender.MaleOnly, realm.SuccessionLaw.gender);
            Assert.AreEqual((int)CentralInstitution.MilitaryCouncil, realm.composition.centralInstitution.primary);
        }
    }
}
