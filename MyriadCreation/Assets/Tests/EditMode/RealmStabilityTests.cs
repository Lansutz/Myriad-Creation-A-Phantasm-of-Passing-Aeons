using NUnit.Framework;
using MyriadCreation.Simulation.State;

namespace MyriadCreation.Tests.EditMode
{
    /// <summary>P2.5 C：稳定度统一钳制在 0–100，Drift 只按比例靠拢。</summary>
    public class RealmStabilityTests
    {
        [Test]
        public void Adjust_ClampsBothEnds()
        {
            var realm = new RealmData { stability = 95f };
            RealmStability.Adjust(realm, 20f);
            Assert.AreEqual(100f, realm.stability, 0.001f);

            realm.stability = 5f;
            RealmStability.Adjust(realm, -20f);
            Assert.AreEqual(0f, realm.stability, 0.001f);
        }

        [Test]
        public void Set_Clamps()
        {
            var realm = new RealmData();
            RealmStability.Set(realm, 130f);
            Assert.AreEqual(100f, realm.stability, 0.001f);
            RealmStability.Set(realm, -5f);
            Assert.AreEqual(0f, realm.stability, 0.001f);
        }

        [Test]
        public void Drift_MovesTowardBaseline()
        {
            var realm = new RealmData { stability = 80f };
            RealmStability.Drift(realm, 50f, 0.5f);
            Assert.AreEqual(65f, realm.stability, 0.01f);
        }
    }
}
