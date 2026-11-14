using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using MyriadCreation.Core.Data;
using MyriadCreation.Core.Enums;
using MyriadCreation.Simulation.State;
using MyriadCreation.Simulation.Systems;
using MyriadCreation.Simulation.WorldState;
using MyriadCreation.World.Generation;
using MyriadCreation.World.Settlement;

namespace MyriadCreation.Tests.EditMode
{
    /// <summary>P8.1：城市由机制涌现，不读世界/聚落年龄；政权要求城市形态。</summary>
    public class SettlementUrbanizationTests
    {
        private GameObject _go;

        [TearDown]
        public void TearDown()
        {
            if (_go != null) Object.DestroyImmediate(_go);
        }

        private GameWorld NewWorld()
        {
            _go = new GameObject("UrbanizationWorld");
            var world = _go.AddComponent<GameWorld>();
            world.randomSeed = 131;
            world.mapWidth = 48;
            world.mapHeight = 24;
            world.SetupOptions = WorldSetupOptions.CreateDefault();
            world.InitializeWorld();
            world.GenerateTerrain(131);
            return world;
        }

        private static SettlementData FirstVillage(GameWorld world)
        {
            foreach (SettlementData settlement in world.settlements.Values)
            {
                if (!settlement.IsRuined && settlement.settlementCategory != SettlementCategory.Outpost
                    && settlement.settlementForm == SettlementForm.Village)
                {
                    return settlement;
                }
            }
            return null;
        }

        private static void MakeCityReady(GameWorld world, SettlementData settlement, float population = 1500f)
        {
            TileData tile = world.tiles[settlement.tileIndex];
            tile.populationBlocks = new List<PopulationBlock>
            {
                new PopulationBlock { count = population, raceId = 0, socialClass = GameEnums.SocialClass.Peasant, satisfaction = 60f }
            };
            world.tiles[settlement.tileIndex] = tile;
            SettlementPopulationSystem.Recompute(world, settlement);
            settlement.development = 25f;
            settlement.hasMarket = true;
            settlement.constructionTier = 5;
            settlement.constructionProgress = 100f;
            if (world.tradeCenters.TryGetValue(tile.regionId, out TradeCenter center))
            {
                center.inventory.Clear();
                center.inventory[0] = population * 3f;
            }
        }

        [Test]
        public void Village_WithPopulationDevelopmentMarketAndFood_BecomesCity()
        {
            var world = NewWorld();
            SettlementData village = FirstVillage(world);
            Assert.IsNotNull(village, "生成世界应有村镇");
            MakeCityReady(world, village);

            bool urbanized = SettlementEvolutionSystem.TryUrbanize(world, village);

            Assert.IsTrue(urbanized, "条件齐备的村镇应转为城市");
            Assert.AreEqual(SettlementForm.City, village.settlementForm);
            Assert.AreEqual(world.currentYear, world.FirstCityYear, "第一座城市年份应是观测值");
        }

        [Test]
        public void Village_WithoutMarket_StaysVillage()
        {
            var world = NewWorld();
            SettlementData village = FirstVillage(world);
            Assert.IsNotNull(village, "生成世界应有村镇");
            MakeCityReady(world, village);
            village.hasMarket = false;
            village.market = null;

            Assert.IsFalse(SettlementEvolutionSystem.TryUrbanize(world, village), "没有市场的村镇不转城市");
            Assert.AreEqual(SettlementForm.Village, village.settlementForm);
        }

        [Test]
        public void YoungCity_IsEligibleWithoutAnyAgeGate()
        {
            var world = NewWorld();
            SettlementData village = FirstVillage(world);
            Assert.IsNotNull(village, "生成世界应有村镇");
            MakeCityReady(world, village, population: 1500f);
            Assert.IsTrue(SettlementEvolutionSystem.TryUrbanize(world, village));
            // 刚成立的城市：age=0，仍应满足建国条件（P8.1 取消年龄门）。
            village.foundingTick = world.calendar.AbsoluteDay(world.currentYear, world.currentDay);

            Assert.IsTrue(StateFormationSystem.IsEligible(world, village), "城市不应因“年龄不足”被挡在建国之外");
        }

        [Test]
        public void Village_IsNotEligibleForRealm()
        {
            var world = NewWorld();
            SettlementData village = FirstVillage(world);
            Assert.IsNotNull(village, "生成世界应有村镇");
            MakeCityReady(world, village);

            Assert.IsFalse(StateFormationSystem.IsEligible(world, village), "村镇不是城市，不能建国");
        }
    }
}
