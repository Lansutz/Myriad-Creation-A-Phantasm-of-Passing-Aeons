using NUnit.Framework;
using MyriadCreation.Simulation.LawHost;
using MyriadCreation.Simulation.Laws;
using MyriadCreation.Simulation.State;

namespace MyriadCreation.Tests.EditMode
{
    /// <summary>
    /// P1.3 法律生命周期：换法记废除并重置；Active→Consolidated→Fundamentalized；
    /// 有推行中法律时不得根本化。
    /// </summary>
    public class LawLifecycleTests
    {
        private static RealmData NewRealm()
        {
            return new RealmData { realmId = 1, realmName = "测试国" };
        }

        [Test]
        public void ReplacingLaw_RecordsRepealAndResetsYears()
        {
            RealmData realm = NewRealm();
            realm.lawState.SetActiveLaw("succession", "succ_hereditary", 10);
            LawGroupState group = realm.lawState.groups[0];
            group.yearsActive = 12;
            group.stage = LegalNormStage.Consolidated;

            realm.lawState.SetActiveLaw("succession", "succ_elective", 20);

            CollectionAssert.Contains(group.repealedLawIds, "succ_hereditary");
            Assert.AreEqual("succ_elective", group.activeLawId);
            Assert.AreEqual(0, group.yearsActive, "换法后年限应清零");
            Assert.AreEqual(LegalNormStage.Active, group.stage);
        }

        [Test]
        public void YearlyTick_ConsolidatesThenFundamentalizes()
        {
            RealmData realm = NewRealm();
            realm.lawState.legitimacy = 80f;
            realm.lawState.SetActiveLaw("succession", "succ_hereditary", 1);
            LawGroupState group = realm.lawState.groups[0];

            for (int i = 0; i < LawLifecycleSystem.ConsolidationYears; i++)
            {
                LawLifecycleSystem.YearlyTick(new[] { realm });
            }
            Assert.AreEqual(LegalNormStage.Consolidated, group.stage, "20 年成习应固化");

            int remaining = LawLifecycleSystem.FundamentalizationYears - LawLifecycleSystem.ConsolidationYears;
            for (int i = 0; i < remaining; i++)
            {
                LawLifecycleSystem.YearlyTick(new[] { realm });
            }

            Assert.AreEqual(LegalNormStage.Fundamentalized, group.stage, "50 年 + 合法性应根本化");
            CollectionAssert.Contains(realm.lawState.fundamentalizedLawIds, "succ_hereditary");
        }

        [Test]
        public void PendingLaw_BlocksFundamentalization()
        {
            RealmData realm = NewRealm();
            realm.lawState.legitimacy = 90f;
            realm.lawState.SetActiveLaw("succession", "succ_hereditary", 1);
            LawGroupState group = realm.lawState.groups[0];
            group.stage = LegalNormStage.Consolidated;
            group.yearsActive = LawLifecycleSystem.FundamentalizationYears - 1;
            realm.lawState.pendingLaws.Add(new PendingLaw
            {
                groupId = "succession",
                lawId = "succ_elective",
                status = "enacting"
            });

            LawLifecycleSystem.YearlyTick(new[] { realm });

            Assert.AreEqual(LegalNormStage.Consolidated, group.stage, "有推行中法律时不得根本化");
            CollectionAssert.DoesNotContain(realm.lawState.fundamentalizedLawIds, "succ_hereditary");
        }
    }
}
