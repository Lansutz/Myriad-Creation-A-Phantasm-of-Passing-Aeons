using NUnit.Framework;
using MyriadCreation.Core.Data;
using MyriadCreation.Core.Enums;
using MyriadCreation.Simulation.Units;
using MyriadCreation.World.Hydrology;

namespace MyriadCreation.Tests.EditMode
{
    /// <summary>海洋 4 分区、河网通航、舟船按海区变化。</summary>
    public class WaterNavigabilityTests
    {
        [Test]
        public void OceanZoning_HasFourWaterZones()
        {
            Assert.AreEqual(OceanZoning.OceanZone.Coast, OceanZoning.GetZone(0));
            Assert.AreEqual(OceanZoning.OceanZone.Coast, OceanZoning.GetZone(1));
            Assert.AreEqual(OceanZoning.OceanZone.Neritic, OceanZoning.GetZone(2));
            Assert.AreEqual(OceanZoning.OceanZone.Neritic, OceanZoning.GetZone(4));
            Assert.AreEqual(OceanZoning.OceanZone.Oceanic, OceanZoning.GetZone(5));
            Assert.AreEqual(OceanZoning.OceanZone.DeepSea, OceanZoning.GetZone(9));
            Assert.AreEqual(OceanZoning.OceanZone.DeepSea, OceanZoning.GetZone(-1));
        }

        [Test]
        public void RiverNavigability_UsesExistingNetworkFields()
        {
            var small = new TileData { isRiver = true, riverStreamOrder = 1, riverWidthMeters = 5f };
            var large = new TileData { isRiver = true, riverStreamOrder = 2, riverWidthMeters = 30f, riverDepthMeters = 1f };
            var wide = new TileData { isRiver = true, riverStreamOrder = 1, riverWidthMeters = 25f, riverDepthMeters = 0.8f };
            Assert.IsFalse(RiverNavigability.IsNavigable(small), "小支流不通航");
            Assert.IsTrue(RiverNavigability.IsNavigable(large), "2 级河流通航");
            Assert.IsTrue(RiverNavigability.IsNavigable(wide), "宽深河段通航");
        }

        [Test]
        public void TerrainConditions_BoatByOceanZone()
        {
            var coast = new TerrainConditions
            {
                IsLand = false, OceanTier = GameEnums.OceanTier.Coast, OceanDepth01 = 0.1f
            };
            var deep = new TerrainConditions
            {
                IsLand = false, OceanTier = GameEnums.OceanTier.DeepSea, OceanDepth01 = 0.8f
            };
            Assert.IsFalse(coast.RequiresBoat, "近海可涉水/沿岸");
            Assert.IsTrue(deep.RequiresBoat, "深海必须有船");
            Assert.Less(coast.CrossingMultiplier(false), coast.CrossingMultiplier(true));
            Assert.Less(deep.CrossingMultiplier(false), deep.CrossingMultiplier(true));
            Assert.Less(deep.CrossingMultiplier(true), coast.CrossingMultiplier(true), "深海更慢");
        }
    }
}
