using NUnit.Framework;
using MyriadCreation.Simulation.State;

namespace MyriadCreation.Tests.EditMode
{
    /// <summary>海军是特殊实体：有舰船数量、驻港、士气/补给，而不是陆军单位列表。</summary>
    public class NavyEntityTests
    {
        [Test]
        public void Navy_AddsAndRemovesShips()
        {
            var navy = new Navy { navyId = 1, navyName = "第一舰队", ownerRealmId = 1 };
            navy.AddShips(106, 3f);
            navy.AddShips(102, 2f);
            Assert.AreEqual(5f, navy.TotalShips, 0.001f);
            Assert.IsTrue(navy.RemoveShips(102, 1f));
            Assert.AreEqual(1f, navy.shipCounts[102], 0.001f);
            Assert.IsFalse(navy.RemoveShips(999, 1f));
        }

        [Test]
        public void Navy_AtSeaAndSupplyConsumption()
        {
            var navy = new Navy { navyId = 2, homePortTile = 10, currentTileIndex = 11, supply = 10f };
            navy.AddShips(106, 2f);
            Assert.IsTrue(navy.IsAtSea);
            navy.DailyTick(1f);
            Assert.AreEqual(8f, navy.supply, 0.001f);
        }
    }
}
