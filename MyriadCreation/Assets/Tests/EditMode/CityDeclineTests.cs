using NUnit.Framework;
using UnityEngine;
using MyriadCreation.Core.Data;
using MyriadCreation.Core.Enums;
using MyriadCreation.World.Anchor;
using MyriadCreation.Simulation.Systems;
using MyriadCreation.Simulation.WorldState;
using MyriadCreation.World.Hydrology;
using MyriadCreation.World.Settlement;

namespace MyriadCreation.Tests.EditMode
{
    /// <summary>P9/M5：河流改道与多年干旱能把依赖水源的聚落推向衰退。</summary>
    public class CityDeclineTests
    {
        private GameObject _go;

        [TearDown]
        public void TearDown()
        {
            if (_go != null) Object.DestroyImmediate(_go);
        }

        private GameWorld FlatWorld(int waterTile = -1)
        {
            _go = new GameObject("DeclineWorld");
            var world = _go.AddComponent<GameWorld>();
            world.mapWidth = 5;
            world.mapHeight = 5;
            world.tiles = new TileData[25];
            for (int i = 0; i < world.tiles.Length; i++)
            {
                world.tiles[i] = new TileData { exists = true, isLand = true, elevation01 = 0.4f, annualPrecipMm = 800f, soilHumidityPct = 60f };
            }
            if (waterTile >= 0)
            {
                world.tiles[waterTile].isRiver = true;
                world.tiles[waterTile].riverStreamOrder = 2;
                world.tiles[waterTile].riverWidthMeters = 20f;
                world.tiles[waterTile].riverDepthMeters = 1f;
            }
            return world;
        }

        [Test]
        public void Avulsion_AbandonsOldChannelAndOpensNewOne()
        {
            var tiles = new TileData[9];
            for (int i = 0; i < tiles.Length; i++)
            {
                tiles[i] = new TileData { exists = true, isLand = true, elevation01 = 0.5f };
            }
            tiles[4].isRiver = true;
            tiles[4].riverStreamOrder = 2;
            tiles[4].riverWidthMeters = 30f;
            tiles[4].riverDepthMeters = 1.5f;
            tiles[4].riverCurrentMps = 1f;
            tiles[1].elevation01 = 0.2f;

            Assert.IsTrue(RiverAvulsion.TryAvulse(tiles, 3, 3, 4, out int fresh));
            Assert.IsFalse(tiles[4].isRiver, "旧河道应断流");
            Assert.IsTrue(tiles[fresh].isRiver, "新河道应出现");
            Assert.Greater(tiles[fresh].riverWidthMeters, 0f);
        }

        [Test]
        public void WaterLoss_PushesRiverDependentCityPastThreshold()
        {
            var world = FlatWorld();
            var settlement = new SettlementData
            {
                anchorId = 1,
                anchor = new AnchorData { anchorId = 1, tileIndex = 12 },
                settlementForm = SettlementForm.City,
                population = 1200f
            };
            settlement.environmentalPressureDays = SettlementEvolutionSystem.EnvironmentalCollapseDays - 1;

            Assert.IsTrue(SettlementEvolutionSystem.ApplyEnvironmentalPressure(world, settlement, 1));
            Assert.AreEqual("water_loss", settlement.lastDeclineCause);
        }

        [Test]
        public void NearbyWater_RelievesPressure()
        {
            var world = FlatWorld(waterTile: 11);
            var settlement = new SettlementData
            {
                anchorId = 2,
                anchor = new AnchorData { anchorId = 2, tileIndex = 12 },
                settlementForm = SettlementForm.City,
                population = 1200f,
                environmentalPressureDays = 100
            };

            Assert.IsFalse(SettlementEvolutionSystem.ApplyEnvironmentalPressure(world, settlement, 30));
            Assert.Less(settlement.environmentalPressureDays, 100, "有水时压力应回退");
        }

        [Test]
        public void DroughtStress_PushesDeclineEvenWithRiver()
        {
            var world = FlatWorld(waterTile: 11);
            var site = world.tiles[12];
            site.soilHumidityPct = 5f;
            site.annualPrecipMm = 200f;
            world.tiles[12] = site;

            var settlement = new SettlementData
            {
                anchorId = 3,
                anchor = new AnchorData { anchorId = 3, tileIndex = 12 },
                settlementForm = SettlementForm.City,
                population = 1200f,
                environmentalPressureDays = SettlementEvolutionSystem.EnvironmentalCollapseDays - 1
            };

            Assert.IsTrue(SettlementEvolutionSystem.ApplyEnvironmentalPressure(world, settlement, 1));
        }
    }
}
