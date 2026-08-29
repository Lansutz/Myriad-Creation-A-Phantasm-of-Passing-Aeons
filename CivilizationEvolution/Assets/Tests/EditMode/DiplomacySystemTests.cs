using System.Collections.Generic;
using NUnit.Framework;
using CivilizationEvolution.Politics;
using CivilizationEvolution.Diplomacy;

namespace CivilizationEvolution.Tests
{
    /// <summary>
    /// 外交层三槽位 EditMode 测试（用户定稿：主权状态/条约义务/特殊纽带 彻底解耦）
    /// 谱系二 5 级（朝贡→保护→附属→附庸→傀儡）+ 谱系一邦联 + 谱系三君合国
    /// </summary>
    public class DiplomacySystemTests
    {
        private DiplomacyManager _dm;

        [SetUp]
        public void Setup()
        {
            var realms = new Dictionary<int, RealmData>();
            for (int i = 0; i < 4; i++)
                realms[i] = new RealmData { realmId = i, realmName = "国" + i };
            _dm = new DiplomacyManager(realms);
        }

        // ===== 槽位1：主权状态（谱系二 5 级） =====

        [Test]
        public void SovereigntySpectrum_AutonomyDescending()
        {
            // 自治度 朝贡0.9 > 保护0.7 > 附属0.5 > 附庸0.35 > 傀儡0.1（内政自主递减）
            var tributary = _dm.EstablishSubordination(0, 1, SubordinationType.Tributary);
            var protectorate = _dm.EstablishSubordination(0, 2, SubordinationType.Protectorate);
            var associate = _dm.EstablishSubordination(0, 3, SubordinationType.Associate);

            Assert.That(tributary.autonomy, Is.EqualTo(0.9f).Within(0.001f), "朝贡国内政完全自主");
            Assert.That(protectorate.autonomy, Is.EqualTo(0.7f).Within(0.001f), "保护国内政自主");
            Assert.That(associate.autonomy, Is.EqualTo(0.5f).Within(0.001f), "附属国内政受监督");
            Assert.IsTrue(associate.foreignPolicyControl, "附属国外交权全权代理");
            Assert.IsTrue(associate.militaryObligation, "附属国国防代理");

            var vassal = _dm.EstablishSubordination(1, 0, SubordinationType.Vassal);
            var puppet = _dm.EstablishSubordination(2, 0, SubordinationType.Puppet);
            Assert.That(vassal.autonomy, Is.EqualTo(0.35f).Within(0.001f), "附庸国总督控制");
            Assert.That(puppet.autonomy, Is.EqualTo(0.1f).Within(0.001f), "傀儡国首脑指定");
            Assert.IsTrue(puppet.successionControl, "傀儡国继承/更替由宗主控制");
        }

        [Test]
        public void SovereigntySlot_Query_FromBothPerspectives()
        {
            _dm.EstablishSubordination(0, 1, SubordinationType.Associate);
            var rel = _dm.GetRelation(0, 1);

            Assert.IsNotNull(rel.subordination, "从属应挂载到关系槽位1");
            Assert.AreEqual(SubordinationType.Associate, rel.GetSovereigntyStatus(1), "从属国视角应返回附属国");
            Assert.IsNull(rel.GetSovereigntyStatus(0), "宗主视角应视为独立（自身非从属）");
        }

        [Test]
        public void SovereigntySlot_Independence_Clears()
        {
            _dm.EstablishSubordination(0, 1, SubordinationType.Tributary);
            Assert.IsTrue(_dm.GrantIndependence(0, 1), "应可独立");

            var rel = _dm.GetRelation(0, 1);
            Assert.IsNull(rel.subordination, "独立后槽位1应清空");
            Assert.IsNull(rel.GetSovereigntyStatus(1), "独立后从属国视角应返回独立");
        }

        // ===== 槽位3：特殊纽带（谱系三：君合国/共主邦联） =====

        [Test]
        public void SpecialBond_PersonalUnion_IndependentOfSubordination()
        {
            Assert.IsTrue(_dm.EstablishPersonalUnion(0, 1, SpecialBondType.PersonalUnion), "君合国应可建立");
            var rel = _dm.GetRelation(0, 1);
            Assert.AreEqual(SpecialBondType.PersonalUnion, rel.specialBond, "特殊纽带槽位应设置");
            Assert.IsNull(rel.subordination, "君合国不应产生从属关系（双方各自保留主权）");

            // 共主邦联可切换（同一对政权仅一个活跃纽带）
            rel.SetSpecialBond(SpecialBondType.CompositeMonarchy);
            Assert.AreEqual(SpecialBondType.CompositeMonarchy, rel.specialBond, "纽带可切换为共主邦联");
        }

        [Test]
        public void SpecialBond_MutuallyExclusiveWithSubordination()
        {
            _dm.EstablishSubordination(0, 1, SubordinationType.Tributary);
            Assert.IsFalse(_dm.EstablishPersonalUnion(0, 1, SpecialBondType.PersonalUnion),
                "存在从属关系时不可建立君合国（主权状态与特殊纽带互斥）");
        }

        [Test]
        public void SpecialBond_PersonalUnionLegacy_Rejected()
        {
            // PersonalUnion 已移出从属枚举：走旧路径应拒绝并提示
            Assert.IsNull(_dm.EstablishSubordination(0, 1, SubordinationType.PersonalUnion),
                "PersonalUnion 不再作为从属关系建立");
        }

        // ===== 槽位2：条约义务（谱系一：邦联） =====

        [Test]
        public void TreatyObligation_Confederation_Established()
        {
            _dm.ModifyRelation(0, 1, 60f, "测试好感");
            var alliance = _dm.ProposeAlliance(0, 1, AllianceType.Confederation);
            Assert.IsNotNull(alliance, "邦联盟约应可建立");
            Assert.AreEqual(AllianceType.Confederation, alliance.type);

            var rel = _dm.GetRelation(0, 1);
            Assert.IsTrue(rel.HasTreatyObligation(AllianceType.Confederation), "槽位2应含邦联义务");
            Assert.IsFalse(rel.HasTreatyObligation(AllianceType.OffensiveAlliance), "不应误报其他盟约");
        }
    }
}
