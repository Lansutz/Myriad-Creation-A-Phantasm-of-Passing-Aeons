using NUnit.Framework;
using CivilizationEvolution.Core;
using CivilizationEvolution.Politics;
using CivilizationEvolution.Role;

namespace CivilizationEvolution.Tests
{
    /// <summary>
    /// 继位扶正系统测试（统治者死亡→继承人扶正→争议判定）
    /// </summary>
    public class SuccessionSystemTests
    {
        private CharacterManager _chars;
        private RealmData _realm;

        [SetUp]
        public void Setup()
        {
            ContentRegistry.Reset();
            ContentRegistry.Initialize();
            _chars = new CharacterManager();
            _realm = new RealmData { realmId = 1, realmName = "测试国" };
        }

        private CharacterData MakeRuler(string name, int age, int birthYear)
        {
            var c = _chars.CreateCharacter(name, "氏", age, true, birthYear, 0, 1, CharacterRole.Ruler);
            return c;
        }

        private CharacterData AddFamilyMember(string name, int age, int birthYear, int familyId)
        {
            var c = _chars.CreateCharacter(name, "氏", age, true, birthYear, 0, 1, CharacterRole.Commoner);
            c.familyId = familyId;
            _chars.GetFamily(familyId).memberIds.Add(c.characterId);
            return c;
        }

        private int SetupFamilyWithRuler(string rulerName, int rulerAge)
        {
            var ruler = MakeRuler(rulerName, rulerAge, 1);
            var family = _chars.CreateFamily("氏", ruler.characterId, 1, realmId: 1);
            ruler.familyId = family.familyId;
            _realm.monarchId = ruler.characterId;
            return ruler.characterId;
        }

        [Test]
        public void MonarchDeath_EldestSonSucceeds()
        {
            int rulerId = SetupFamilyWithRuler("先君", 60);
            AddFamilyMember("长子", 35, 10, _chars.GetCharacter(rulerId).familyId);
            AddFamilyMember("次子", 25, 20, _chars.GetCharacter(rulerId).familyId);

            // 统治者死亡
            _chars.GetCharacter(rulerId).deathDay = 1000;

            var result = SuccessionSystem.ExecuteSuccession(_realm, _chars, 1000);

            Assert.IsTrue(result.triggered, "统治者死亡应触发");
            Assert.IsTrue(result.succeeded, "应有继承人");
            Assert.IsFalse(result.disputed, "有成年继承人无争议");
            Assert.AreEqual("长子", _chars.GetCharacter(result.newRulerId).firstName, "长子继承（年长优先）");
            Assert.AreEqual(result.newRulerId, _realm.monarchId, "君主已扶正");
        }

        [Test]
        public void MonarchDeath_NoHeirs_Disputed()
        {
            int rulerId = SetupFamilyWithRuler("孤君", 60);
            _chars.GetCharacter(rulerId).deathDay = 1000;

            var result = SuccessionSystem.ExecuteSuccession(_realm, _chars, 1000);

            Assert.IsTrue(result.triggered);
            Assert.IsTrue(result.disputed, "绝嗣应争议");
            Assert.AreEqual("绝嗣", result.reason);
            Assert.IsFalse(result.succeeded);
        }

        [Test]
        public void MonarchDeath_UnderageHeir_Disputed()
        {
            int rulerId = SetupFamilyWithRuler("先君", 60);
            AddFamilyMember("幼主", 10, 35, _chars.GetCharacter(rulerId).familyId);
            _chars.GetCharacter(rulerId).deathDay = 1000;

            var result = SuccessionSystem.ExecuteSuccession(_realm, _chars, 1000);

            Assert.IsTrue(result.disputed, "幼主应争议");
            Assert.AreEqual("幼主", result.reason);
            // 仍扶正（幼主危机）
            Assert.IsTrue(result.succeeded);
            Assert.AreEqual("幼主", _chars.GetCharacter(result.newRulerId).firstName);
        }

        [Test]
        public void AliveRuler_NotTriggered()
        {
            int rulerId = SetupFamilyWithRuler("健在君", 40);
            var result = SuccessionSystem.ExecuteSuccession(_realm, _chars, 1000);
            Assert.IsFalse(result.triggered, "统治者未死不触发");
        }

        [Test]
        public void Republic_ConsulDeath_EligibilityAndPrestige()
        {
            // 共和制：A1 选举 + A2 共议
            _realm.composition = GovernmentComposition.AthenianDemocracy();
            _realm.composition.supremeEligibility = new EligibilityRules
            {
                gender = InheritanceGender.MaleOnly,
                scope = EligibilityScope.Citizens
            };

            var consul = MakeRuler("执政", 60, 1);
            var family = _chars.CreateFamily("氏", consul.characterId, 1, realmId: 1);
            consul.familyId = family.familyId;
            _realm.consulId = consul.characterId;

            var maleLow = AddFamilyMember("低望男", 40, 5, family.familyId);
            maleLow.prestige = 10f;
            var maleHigh = AddFamilyMember("高望男", 45, 0, family.familyId);
            maleHigh.prestige = 90f;
            var femaleHigh = AddFamilyMember("高望女", 42, 3, family.familyId);
            femaleHigh.isMale = false;
            femaleHigh.prestige = 95f;

            consul.deathDay = 1000;
            var result = SuccessionSystem.ExecuteSuccession(_realm, _chars, 1000);

            Assert.IsTrue(result.succeeded);
            Assert.AreEqual("高望男", _chars.GetCharacter(result.newRulerId).firstName,
                "男子专属资格过滤后威望最高者当选");
            Assert.AreEqual(result.newRulerId, _realm.consulId, "执政官已扶正");
        }

        [Test]
        public void SuccessionLaw_TwoTier()
        {
            // 双轨：文化默认（Laethis 宗祧析产）vs 国家自定（兄终弟及）
            int rulerId = SetupFamilyWithRuler("先君", 60);
            var familyId = _chars.GetCharacter(rulerId).familyId;
            AddFamilyMember("长子", 35, 10, familyId);
            AddFamilyMember("次子", 30, 15, familyId);
            var ruler = _chars.GetCharacter(rulerId);
            ruler.deathDay = 1000;

            // 文化默认：男子优先+年长 → 长子（无叔父时）
            _realm.primaryCultureId = 1;
            _realm.successionLawFromCulture = true;
            var resultCulture = SuccessionSystem.ExecuteSuccession(_realm, _chars, 1000);
            Assert.AreEqual("长子", _chars.GetCharacter(resultCulture.newRulerId).firstName, "文化默认长子");

            // 加入"先君的弟弟"（叔父——同父=真兄弟，兄终弟及时优先于子辈）
            ruler.fatherId = 900;
            var uncle = AddFamilyMember("叔父", 55, 1, familyId);
            uncle.fatherId = 900;

            // 国家自定：兄终弟及（支系=Collateral——同辈兄弟优先于子辈）
            _realm.successionLawFromCulture = false;
            _realm.composition.successionLaw = InheritanceLaw.Tanistry();
            _realm.monarchId = rulerId; // 重置为已死的先君（重新触发继位）
            var resultCustom = SuccessionSystem.ExecuteSuccession(_realm, _chars, 1000);
            Assert.AreEqual("叔父", _chars.GetCharacter(resultCustom.newRulerId).firstName,
                "兄终弟及：同辈兄弟优先于子辈");
        }
    }
}
