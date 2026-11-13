using NUnit.Framework;
using MyriadCreation.Core.Data;
using MyriadCreation.Simulation.Systems;
using MyriadCreation.World.Terrain;

namespace MyriadCreation.Tests.EditMode
{
    /// <summary>Earth Simulator 借用的 Miami NPP：温度/降水双限制，高温封顶。</summary>
    public class EcologyModelTests
    {
        [Test]
        public void Npp_IsLimitedByBothTemperatureAndRain()
        {
            float temperate = EcologyModel.ComputeNpp(22f, 1000f);
            float coldDry = EcologyModel.ComputeNpp(-5f, 50f);
            float hot = EcologyModel.ComputeNpp(54f, 5000f);

            Assert.Greater(temperate, 0.4f);
            Assert.Less(coldDry, 0.2f);
            Assert.Less(hot, 0.2f);
            Assert.LessOrEqual(EcologyModel.ComputeNpp(80f, 1000f), 0.001f);
        }

        [Test]
        public void CarryingCapacity_UsesNpp_WhenPresent()
        {
            var rich = new TileData { exists = true, isLand = true, biome = MyriadCreation.Core.Enums.GameEnums.BiomeType.TemperateGrassland, npp = 1f };
            var poor = rich;
            poor.npp = 0.2f;

            float richCapacity = CarryingCapacitySystem.CalculateCarryingCapacity(rich, null);
            float poorCapacity = CarryingCapacitySystem.CalculateCarryingCapacity(poor, null);
            Assert.Greater(richCapacity, poorCapacity);
        }
    }
}
