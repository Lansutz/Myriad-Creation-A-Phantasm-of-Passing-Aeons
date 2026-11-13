using NUnit.Framework;
using MyriadCreation.Simulation.LawHost;
using MyriadCreation.Simulation.Laws;
using MyriadCreation.Simulation.State;

namespace MyriadCreation.Tests.EditMode
{
    /// <summary>LawState → LawComposition 回写：现行法律必须改变治理组件。</summary>
    public class LawCompositionDeriverTests
    {
        [Test]
        public void ForceSetLaw_DerivesPolityComponents()
        {
            var realm = new RealmData { realmId = 1 };
            realm.lawState = new RealmLawState();

            Assert.IsTrue(LawRealmHost.ForceSetLaw(realm, "succession", "succ_elective", 10, out _));
            Assert.IsTrue(LawRealmHost.ForceSetLaw(realm, "supreme_scope", "scope_consensual", 10, out _));
            Assert.IsTrue(LawRealmHost.ForceSetLaw(realm, "central_succession", "cent_succ_examination", 10, out _));
            Assert.IsTrue(LawRealmHost.ForceSetLaw(realm, "local_succession", "local_succ_charter", 10, out _));
            Assert.IsTrue(LawRealmHost.ForceSetLaw(realm, "local_scope", "local_scope_military_only", 10, out _));
            Assert.IsTrue(LawRealmHost.ForceSetLaw(realm, "spatial", "spatial_federal", 10, out _));

            Assert.AreEqual(1, realm.laws.supremeSuccession.primary);
            Assert.AreEqual(3, realm.laws.supremeScope.primary);
            Assert.AreEqual(2, realm.laws.centralSuccession.primary);
            Assert.AreEqual(4, realm.laws.localSuccession.primary);
            Assert.AreEqual(2, realm.laws.localScope.primary);
            Assert.AreEqual(1, realm.laws.spatialStructure.primary);
        }

        [Test]
        public void UnknownCatalogLaw_DoesNotGuessComponent()
        {
            var realm = new RealmData { realmId = 2 };
            realm.lawState = new RealmLawState();
            int before = realm.laws.supremeSuccession.primary;

            LawCompositionDeriver.Apply(realm.lawState, realm.laws);

            Assert.AreEqual(before, realm.laws.supremeSuccession.primary);
        }
    }
}
