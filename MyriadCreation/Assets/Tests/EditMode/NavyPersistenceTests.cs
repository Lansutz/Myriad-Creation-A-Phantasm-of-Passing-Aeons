using NUnit.Framework;
using MyriadCreation.Infrastructure.Save;
using MyriadCreation.Simulation.State;

namespace MyriadCreation.Tests.EditMode
{
    /// <summary>海军 DTO 往返：舰船数量、母港、位置、士气/补给。</summary>
    public class NavyPersistenceTests
    {
        [Test]
        public void NavySaveData_RoundTrips()
        {
            var navy = new Navy
            {
                navyId = 7,
                navyName = "第七舰队",
                ownerRealmId = 3,
                commanderId = 42,
                homePortTile = 100,
                currentTileIndex = 101,
                organization = 80f,
                morale = 70f,
                supply = 60f,
                training = 50f,
            };
            navy.AddShips(107, 3f);
            navy.AddShips(102, 2f);

            var dto = NavySaveData.FromNavy(navy);
            var restored = dto.ToNavy();

            Assert.AreEqual(7, restored.navyId);
            Assert.AreEqual("第七舰队", restored.navyName);
            Assert.AreEqual(3, restored.ownerRealmId);
            Assert.AreEqual(100, restored.homePortTile);
            Assert.AreEqual(101, restored.currentTileIndex);
            Assert.AreEqual(5f, restored.TotalShips, 0.001f);
            Assert.AreEqual(3f, restored.shipCounts[107], 0.001f);
            Assert.AreEqual(80f, restored.organization, 0.001f);
        }
    }
}
