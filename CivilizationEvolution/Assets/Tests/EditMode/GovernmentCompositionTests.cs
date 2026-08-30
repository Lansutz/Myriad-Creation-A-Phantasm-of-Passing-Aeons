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
            Assert.AreEqual((int)LocalSuccession.CentralAppointed, comp.localSuccession.primary);
            Assert.AreEqual((int)LocalScope.FiscalJudicial, comp.localScope.primary);
            // D. 央地结构（第七维）
            Assert.AreEqual((int)SpatialStructure.Unitary, comp.spatialStructure.primary, "央地结构独立维度");

            // 世袭子选项=继承法（默认长子继承）
            Assert.AreEqual(InheritanceGender.MalePreference, comp.successionLaw.gender);
        }

        // ===== 细分成分（A1 九选/议会三形态/领主两分/央地结构） =====

        [Test]
        public void SupremeSuccession_RefinedOptions()
        {
            // 选举三分：公民大会直接/代议/委员会
            Assert.AreEqual((int)SupremeSuccession.DirectAssembly, GovernmentComposition.AthenianDemocracy().supremeSuccession.primary);
            Assert.AreEqual((int)SupremeSuccession.CollegialElection, GovernmentComposition.SenatorialRepublic().supremeSuccession.primary);
            Assert.AreEqual((int)SupremeSuccession.RepresentativeElection, GovernmentComposition.ConstitutionalMonarchy().supremeSuccession.primary);
            // 指定储君（非世袭非选举——中国预立太子）
            Assert.AreEqual((int)SupremeSuccession.SuccessorDesignation, GovernmentComposition.SuccessorDesignationEmpire().supremeSuccession.primary);
            // 贵族推举（选帝侯）
            Assert.AreEqual((int)SupremeSuccession.NobleDesignation, GovernmentComposition.HolyRomanEmpire().supremeSuccession.primary);
        }

        [Test]
        public void CentralInstitution_AssemblyThreeForms()
        {
            // 议会三形态：一院（雅典公民大会/罗马元老院）/两院（英国上下院）/等级会议（三级会议/帝国议会）
            Assert.AreEqual((int)CentralInstitution.UnicameralAssembly, GovernmentComposition.AthenianDemocracy().centralInstitution.primary);
            Assert.AreEqual((int)CentralInstitution.BicameralAssembly, GovernmentComposition.ConstitutionalMonarchy().centralInstitution.primary);
            Assert.AreEqual((int)CentralInstitution.EstateAssembly, GovernmentComposition.HolyRomanEmpire().centralInstitution.primary);
        }

        [Test]
        public void LocalSuccession_LordTwoForms()
        {
            // 领主两分：世袭封臣（封建契约）vs 宗室采邑（分封宗亲）
            Assert.AreEqual((int)LocalSuccession.HereditaryVassal, GovernmentComposition.MongolHorde().localSuccession.primary);
            Assert.AreEqual((int)LocalSuccession.FeudalAppanage, GovernmentComposition.FeudalFiefdom().localSuccession.primary);
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
            Assert.AreEqual((int)SupremeSuccession.RepresentativeElection, comp.supremeSuccession.primary,
                "最高权力在议会，不在君主");
            Assert.AreEqual((int)CentralInstitution.BicameralAssembly, comp.centralInstitution.primary);
        }

        [Test]
        public void EldersCouncil_NotRepublican()
        {
            // 长老议事会（长老资格制）≠ 议会/元老院（共和制）
            var comp = new GovernmentComposition
            {
                supremeSuccession = new ComponentChoice((int)SupremeSuccession.NobleDesignation),
                centralInstitution = new ComponentChoice((int)CentralInstitution.EldersCouncil)
            };
            Assert.AreEqual((int)CentralInstitution.EldersCouncil, comp.centralInstitution.primary);
            var republic = GovernmentComposition.SenatorialRepublic();
            Assert.AreEqual((int)CentralInstitution.UnicameralAssembly, republic.centralInstitution.primary,
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
            Assert.AreEqual((int)SupremeSuccession.DivineMandate, theo.supremeSuccession.primary);
            Assert.AreEqual((int)CentralInstitution.ReligiousCouncil, theo.centralInstitution.primary);

            // 罗马帝国：皇帝+元老院（与共和元老院层级对等）
            var empire = GovernmentComposition.ImperialSenate();
            var republic = GovernmentComposition.SenatorialRepublic();
            Assert.AreEqual(empire.centralInstitution.primary, republic.centralInstitution.primary,
                "皇帝制与共和制元老院同层级");
            Assert.AreNotEqual(empire.supremeSuccession.primary, republic.supremeSuccession.primary);

            // 威尼斯：委员会选举+两院+城市特许
            var venice = GovernmentComposition.VenetianRepublic();
            Assert.AreEqual((int)SupremeSuccession.CollegialElection, venice.supremeSuccession.primary);
            Assert.AreEqual((int)CentralInstitution.BicameralAssembly, venice.centralInstitution.primary);
            Assert.AreEqual((int)LocalSuccession.CityCharter, venice.localSuccession.primary);

            // 储君制帝国：生前指定+考课晋升
            var tang = GovernmentComposition.SuccessorDesignationEmpire();
            Assert.AreEqual((int)SupremeSuccession.SuccessorDesignation, tang.supremeSuccession.primary);
            Assert.AreEqual((int)CentralSuccession.MeritPromotion, tang.centralSuccession.primary);
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
                localSuccession = new ComponentChoice((int)LocalSuccession.ReligiousAppointed),
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
            comp.supremeSuccession = new ComponentChoice((int)SupremeSuccession.Hereditary, (int)SupremeSuccession.SuccessorDesignation);
            Assert.IsTrue(comp.supremeSuccession.Contains((int)SupremeSuccession.Hereditary));
            Assert.IsTrue(comp.supremeSuccession.Contains((int)SupremeSuccession.SuccessorDesignation));
            Assert.AreEqual(1, comp.supremeSuccession.secondary.Count, "0~2 个次要");
        }

        [Test]
        public void Eligibility_GenderCrosscutsAllSuccession()
        {
            // 用户定稿：性别等要素横切所有交接方式——选举/推举/轮座/神命同样有资格问题
            // 雅典（选举）：男子专属·本城邦公民
            var athens = GovernmentComposition.AthenianDemocracy();
            Assert.AreEqual(InheritanceGender.MaleOnly, athens.eligibility.gender, "雅典选举：男子专属被选举权");
            Assert.AreEqual(EligibilityScope.Citizens, athens.eligibility.scope, "雅典：限本城邦公民");

            // 罗马共和（推举）：男子专属·公民
            var rome = GovernmentComposition.SenatorialRepublic();
            Assert.AreEqual(InheritanceGender.MaleOnly, rome.eligibility.gender, "罗马推举：男子专属");

            // 神权（神命）：男子专属·教阶
            var theo = GovernmentComposition.Theocracy();
            Assert.AreEqual(EligibilityScope.Clergy, theo.eligibility.scope, "神权：限教阶");

            // 蒙古（轮座/世袭）：男子专属·贵族（忽里台）
            var mongol = GovernmentComposition.MongolHorde();
            Assert.AreEqual(EligibilityScope.Nobility, mongol.eligibility.scope, "蒙古：限贵族");

            // 资格过滤验证：男子专属时女性被过滤
            var filtered = rome.eligibility.Filter(new List<CharacterData>
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
            comp.eligibility = new EligibilityRules { gender = InheritanceGender.MaleOnly, scope = EligibilityScope.ClanOnly };
            comp.successionLaw = InheritanceLaw.Salic();

            Assert.AreEqual(InheritanceGender.MaleOnly, comp.eligibility.gender, "资格门槛：男子专属");
            Assert.AreEqual(InheritanceGender.MaleOnly, comp.successionLaw.gender, "继承排序：男子专属（萨利克）");
            Assert.IsTrue(comp.eligibility.IsGenderEligible(true), "男性合格");
            Assert.IsFalse(comp.eligibility.IsGenderEligible(false), "女性不合格");
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
