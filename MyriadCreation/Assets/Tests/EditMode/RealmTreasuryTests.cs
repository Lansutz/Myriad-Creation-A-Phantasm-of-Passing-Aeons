using NUnit.Framework;
using MyriadCreation.Simulation.State;

namespace MyriadCreation.Tests.EditMode
{
    /// <summary>P2.5 C：政权货币余额走 RealmTreasury 单一入口，预检不产生部分扣款。</summary>
    public class RealmTreasuryTests
    {
        [Test]
        public void TrySpend_Insufficient_DoesNotChangeBalance()
        {
            var realm = new RealmData { treasury = 50f };

            Assert.IsFalse(RealmTreasury.TrySpend(realm, 60f));
            Assert.AreEqual(50f, realm.treasury, 0.001f);
        }

        [Test]
        public void TrySpend_Sufficient_Deducts()
        {
            var realm = new RealmData { treasury = 50f };

            Assert.IsTrue(RealmTreasury.TrySpend(realm, 20f));
            Assert.AreEqual(30f, realm.treasury, 0.001f);
        }

        [Test]
        public void Spend_AllowsNegative_ForLegacyPaths()
        {
            var realm = new RealmData { treasury = 10f };

            RealmTreasury.Spend(realm, 25f);
            Assert.AreEqual(-15f, realm.treasury, 0.001f);
        }

        [Test]
        public void AddAndSet_Work()
        {
            var realm = new RealmData { treasury = 10f };

            RealmTreasury.Add(realm, 5f);
            Assert.AreEqual(15f, realm.treasury, 0.001f);

            RealmTreasury.Set(realm, 100f);
            Assert.AreEqual(100f, realm.treasury, 0.001f);
        }
    }
}
