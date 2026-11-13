using NUnit.Framework;
using MyriadCreation.Core.Data;
using MyriadCreation.Core.Enums;
using MyriadCreation.Simulation.Systems;

namespace MyriadCreation.Tests.EditMode
{
    /// <summary>
    /// P7：土地肥力必须有恢复端。灾害每天扣肥力而没有恢复，会让全图肥力在一两百年内归零，
    /// 城市随后集体饿死——这是 220 年自然长跑失败的直接原因。
    /// </summary>
    public class SoilRecoveryTests
    {
        private static TileData NewTile(float fertility)
        {
            return new TileData
            {
                exists = true,
                isLand = true,
                biome = GameEnums.BiomeType.TemperateGrassland,
                annualTemp = 14f,
                annualPrecipMm = 900f,
                elevation01 = 0.2f,
                slopeDegree = 5f,
                npp = 0.8f,
                fertility = fertility,
                development = 0.2f,
                stability = 50f,
                order = 40f
            };
        }

        [Test]
        public void DegradedSoil_RecoversTowardClimatePotential()
        {
            var tile = NewTile(0.05f);
            float potential = EcosystemSystem.PotentialFertility(tile);
            Assert.Greater(potential, 0.3f, "温带草原的潜在肥力不应是荒漠量级");

            var tiles = new[] { tile };
            for (int month = 0; month < 600; month++) EcosystemSystem.Tick(tiles, 30f);

            Assert.Greater(tiles[0].fertility, potential - 0.02f, "50 年休耕应把肥力恢复到接近潜在值");
        }

        [Test]
        public void SoilRecovery_NeverExceedsPotential()
        {
            var tile = NewTile(0.05f);
            float potential = EcosystemSystem.PotentialFertility(tile);
            var tiles = new[] { tile };
            for (int month = 0; month < 1200; month++) EcosystemSystem.Tick(tiles, 30f);

            Assert.LessOrEqual(tiles[0].fertility, potential + 0.0001f, "恢复只能向潜在肥力靠拢，不能凭空超过");
        }

        [Test]
        public void FertilityAbovePotential_IsNotForcedDown()
        {
            var tile = NewTile(0.99f);
            var tiles = new[] { tile };
            for (int month = 0; month < 240; month++) EcosystemSystem.Tick(tiles, 30f);

            Assert.AreEqual(0.99f, tiles[0].fertility, 0.001f, "火山灰等一次性增肥不应被恢复逻辑抹掉");
        }

        [Test]
        public void RecoveredSoil_ProducesMoreFood()
        {
            var degraded = NewTile(0.1f);
            degraded.populationBlocks = new System.Collections.Generic.List<PopulationBlock>
            {
                new PopulationBlock { count = 200f, socialClass = GameEnums.SocialClass.Peasant, satisfaction = 60f }
            };
            float before = AgricultureYield.Compute(degraded);

            var tiles = new[] { degraded };
            for (int month = 0; month < 600; month++) EcosystemSystem.Tick(tiles, 30f);
            float after = AgricultureYield.Compute(tiles[0]);

            Assert.Greater(after, before * 1.5f, "肥力恢复后同样劳力的产出应显著提高");
        }
    }
}
