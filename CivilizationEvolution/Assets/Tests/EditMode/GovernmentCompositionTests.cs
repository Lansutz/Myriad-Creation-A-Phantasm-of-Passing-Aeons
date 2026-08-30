using System.Collections.Generic;
using NUnit.Framework;
using CivilizationEvolution.Politics;

namespace CivilizationEvolution.Tests
{
    /// <summary>
    /// 政体系统 EditMode 测试（用户定稿：三权力层级 × 交接/分配 = 六维结构）
    /// 政体 = 六维成分组合；成分是模组化接口；继承法=最高权力·交接=世袭子选项
    /// </summary>
    public class GovernmentCompositionTests
    {
        // ===== 六维结构完整性 =====

        [Test]
        public void SixDimensions_AllPresent()
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

            // 世袭子选项=继承法（默认长子继承）
            Assert.AreEqual(InheritanceGender.MalePreference, comp.successionLaw.gender);
        }

        // ===== 经典政体组合（学术示例） =====

        [Test]
        public void BureaucraticMonarchy_QinStyle()
        {
            // 秦式：世袭+全能+任命+官僚中枢+中央任官+完全直辖（宗祧+析产）
            var comp = GovernmentComposition.BureaucraticMonarchy();
            Assert.AreEqual((int)SupremeSuccession.Hereditary, comp.supremeSuccession.primary);
            Assert.AreEqual((int)CentralInstitution.BureaucraticCore, comp.centralInstitution.primary);
            Assert.AreEqual((int)LocalSuccession.CentralAppointed, comp.localSuccession.primary);
            Assert.AreEqual((int)LocalScope.None, comp.localScope.primary);
            Assert.AreEqual(LandInheritanceMode.Partible, comp.successionLaw.landMode, "析产：领地诸子均分");
            StringAssert.Contains("官僚中枢", comp.GetName());
        }

        [Test]
        public void FeudalFiefdom_ZhouStyle()
        {
            // 西周式：世袭领主+全权自治（封国）
            var comp = GovernmentComposition.FeudalFiefdom();
            Assert.AreEqual((int)CentralSuccession.HereditaryOffice, comp.centralSuccession.primary);
            Assert.AreEqual((int)CentralInstitution.Court, comp.centralInstitution.primary);
            Assert.AreEqual((int)LocalSuccession.HereditaryLord, comp.localSuccession.primary);
            Assert.AreEqual((int)LocalScope.FullAutonomy, comp.localScope.primary);
        }

        [Test]
        public void AthenianDemocracy_AssemblyRule()
        {
            // 雅典式：选举+共议+选举+议会+地方推举+全权自治
            var comp = GovernmentComposition.AthenianDemocracy();
            Assert.AreEqual((int)SupremeSuccession.Election, comp.supremeSuccession.primary);
            Assert.AreEqual((int)SupremeScope.Consensual, comp.supremeScope.primary);
            Assert.AreEqual((int)CentralInstitution.Assembly, comp.centralInstitution.primary);
        }

        [Test]
        public void SenatorialRepublic_RomeStyle()
        {
            // 罗马共和式：推举+法理受限+恩庇+元老院+中央任命+征税司法
            var comp = GovernmentComposition.SenatorialRepublic();
            Assert.AreEqual((int)SupremeSuccession.Designation, comp.supremeSuccession.primary);
            Assert.AreEqual((int)SupremeScope.LegallyBound, comp.supremeScope.primary);
            Assert.AreEqual((int)CentralSuccession.Patronage, comp.centralSuccession.primary);
            Assert.AreEqual((int)LocalScope.FiscalJudicial, comp.localScope.primary);
        }

        [Test]
        public void Theocracy_DivineRule()
        {
            // 神权式：神命+神意约束+宗教会议+教区委任
            var comp = GovernmentComposition.Theocracy();
            Assert.AreEqual((int)SupremeSuccession.DivineMandate, comp.supremeSuccession.primary);
            Assert.AreEqual((int)SupremeScope.DivinelyBound, comp.supremeScope.primary);
            Assert.AreEqual((int)CentralInstitution.ReligiousCouncil, comp.centralInstitution.primary);
            Assert.AreEqual((int)LocalSuccession.ReligiousAppointed, comp.localSuccession.primary);
        }

        [Test]
        public void MongolHorde_TanistrySuccession()
        {
            // 蒙古式：军事委员会+世袭领主+全权自治+兄终弟及继承法
            var comp = GovernmentComposition.MongolHorde();
            Assert.AreEqual((int)CentralInstitution.MilitaryCouncil, comp.centralInstitution.primary);
            Assert.AreEqual((int)LocalScope.FullAutonomy, comp.localScope.primary);
            Assert.AreEqual(InheritanceGender.MaleOnly, comp.successionLaw.gender, "兄终弟及男子专属");
            Assert.AreEqual(InheritanceBranch.Collateral, comp.successionLaw.branch, "横向继承");
        }

        // ===== 正交组合（最高权力主体 ≠ 中央机构——自由组合） =====

        [Test]
        public void ConstitutionalMonarchy_SovereigntyInParliament()
        {
            // 君主立宪：最高权力在议会（parliamentary sovereignty）——非世袭君主！
            // 君主仅为虚位元首（外交头衔，不占成分）
            var comp = GovernmentComposition.ConstitutionalMonarchy();
            Assert.AreEqual((int)SupremeSuccession.Election, comp.supremeSuccession.primary,
                "最高权力在议会（选举产生），不在君主");
            Assert.AreEqual((int)SupremeScope.Consensual, comp.supremeScope.primary, "共议制约");
            Assert.AreEqual((int)CentralInstitution.Assembly, comp.centralInstitution.primary, "议会机构");
        }

        [Test]
        public void EldersCouncil_NotRepublican()
        {
            // 长老议事会（部落/长老资格制）≠ 议会/元老院（共和制）——独立机构形态
            var comp = new GovernmentComposition
            {
                supremeSuccession = new ComponentChoice((int)SupremeSuccession.Designation),
                centralInstitution = new ComponentChoice((int)CentralInstitution.EldersCouncil)
            };
            Assert.AreEqual((int)CentralInstitution.EldersCouncil, comp.centralInstitution.primary,
                "长老议事会为独立选项（非共和制）");

            var republic = GovernmentComposition.SenatorialRepublic();
            Assert.AreNotEqual((int)CentralInstitution.EldersCouncil, republic.centralInstitution.primary,
                "共和制（罗马）中央机构为元老院/议会，非长老会");
            Assert.AreEqual((int)CentralInstitution.Assembly, republic.centralInstitution.primary);
        }

        [Test]
        public void ImperialSenate_EmperorWithSenate()
        {
            // 罗马帝国：皇帝（世袭/僭夺）+ 元老院——与共和元老院层级对等
            var comp = GovernmentComposition.ImperialSenate();
            Assert.AreEqual((int)CentralInstitution.Assembly, comp.centralInstitution.primary, "元老院");
            Assert.IsTrue(comp.supremeSuccession.Contains((int)SupremeSuccession.Usurpation), "皇帝可僭夺产生");

            // 与共和制元老院（SenatorialRepublic）对比：中央机构同层级
            var republic = GovernmentComposition.SenatorialRepublic();
            Assert.AreEqual(comp.centralInstitution.primary, republic.centralInstitution.primary,
                "皇帝制与共和制的元老院在层级上对等");
            Assert.AreNotEqual(comp.supremeSuccession.primary, republic.supremeSuccession.primary,
                "最高权力主体不同（皇帝 vs 推举执政）");
        }

        [Test]
        public void HolyRomanEmpire_ElectedEmperorWithImperialDiet()
        {
            // 神罗：选帝侯选举（非世袭）+ 帝国议会
            var comp = GovernmentComposition.HolyRomanEmpire();
            Assert.AreEqual((int)SupremeSuccession.Designation, comp.supremeSuccession.primary, "选帝侯推举");
            Assert.AreEqual((int)CentralInstitution.Assembly, comp.centralInstitution.primary, "帝国议会");
            Assert.AreEqual((int)LocalScope.FullAutonomy, comp.localScope.primary, "诸侯全权自治");
        }

        // ===== 成分组合自由性（模组化接口：任意组合合法） =====

        [Test]
        public void FreeCombination_AnySixComponents()
        {
            // 成分自由组合：如 僭主+全能+考试选任+王庭+教区委任+仅军事——非法治但合法组合
            var comp = new GovernmentComposition
            {
                supremeSuccession = new ComponentChoice((int)SupremeSuccession.Usurpation),
                supremeScope = new ComponentChoice((int)SupremeScope.Absolute),
                centralSuccession = new ComponentChoice((int)CentralSuccession.Examination),
                centralInstitution = new ComponentChoice((int)CentralInstitution.Court),
                localSuccession = new ComponentChoice((int)LocalSuccession.ReligiousAppointed),
                localScope = new ComponentChoice((int)LocalScope.MilitaryOnly)
            };
            Assert.AreEqual((int)SupremeSuccession.Usurpation, comp.supremeSuccession.primary);
            Assert.AreEqual((int)CentralSuccession.Examination, comp.centralSuccession.primary);
            Assert.AreEqual((int)LocalScope.MilitaryOnly, comp.localScope.primary);
            StringAssert.Contains("王庭", comp.GetName());
        }

        // ===== 次要成分（○）支持 =====

        [Test]
        public void SecondaryComponents_Supported()
        {
            var comp = new GovernmentComposition();
            comp.supremeScope = new ComponentChoice((int)SupremeScope.Absolute, (int)SupremeScope.Consensual);
            Assert.IsTrue(comp.supremeScope.Contains((int)SupremeScope.Absolute), "主导成分");
            Assert.IsTrue(comp.supremeScope.Contains((int)SupremeScope.Consensual), "次要成分");
            Assert.AreEqual(1, comp.supremeScope.secondary.Count, "0~2 个次要");
        }

        // ===== RealmData 挂载 =====

        [Test]
        public void RealmData_Composition_Integrated()
        {
            var realm = new RealmData { realmId = 1 };
            Assert.IsNotNull(realm.composition, "政权应带六维政体成分");
            Assert.AreEqual((int)SupremeSuccession.Hereditary, realm.composition.supremeSuccession.primary);
            // 便捷访问继承法（世袭子选项）
            Assert.AreEqual(InheritanceGender.MalePreference, realm.SuccessionLaw.gender, "默认长子继承");
            // 可整体替换为蒙古汗国式
            realm.composition = GovernmentComposition.MongolHorde();
            Assert.AreEqual(InheritanceGender.MaleOnly, realm.SuccessionLaw.gender);
            Assert.AreEqual((int)CentralInstitution.MilitaryCouncil, realm.composition.centralInstitution.primary);
        }
    }
}
