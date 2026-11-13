using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using MyriadCreation.Core.Data;
using MyriadCreation.Core.Enums;
using MyriadCreation.Infrastructure.Save;
using MyriadCreation.Simulation.State;
using MyriadCreation.Simulation.WorldState;
using MyriadCreation.World;
using MyriadCreation.World.Generation;
using MyriadCreation.World.Settlement;

namespace MyriadCreation.Tests.EditMode
{
    /// <summary>
    /// P1.1 聚落生命周期：长期低人口 + 无守军 + 无驻军 → 废弃压力累积 → 降为营地；
    /// 人口/守军恢复则压力清零；压力随存档往返。
    /// </summary>
    public class SettlementLifecycleTests
    {
        private const string SaveName = "__settlement_lifecycle_test";
        private readonly List<GameObject> _objects = new List<GameObject>();

        [TearDown]
        public void TearDown()
        {
            foreach (var go in _objects)
            {
                if (go != null) Object.DestroyImmediate(go);
            }
            _objects.Clear();
            SaveSystem.DeleteSave(SaveName);
        }

        private GameWorld NewWorld()
        {
            var go = new GameObject("SettlementLifecycleWorld");
            _objects.Add(go);
            var world = go.AddComponent<GameWorld>();
            world.mapWidth = 48;
            world.mapHeight = 24;
            world.SetupOptions = WorldSetupOptions.CreateStandard();
            world.InitializeWorld();
            world.GenerateTerrain(59);
            return world;
        }

        private static SettlementData FirstSettlement(GameWorld world)
        {
            foreach (var settlement in world.settlements.Values)
            {
                if (settlement != null && settlement.siteMode == SiteMode.SedentarySettlement) return settlement;
            }
            return null;
        }

        private static void ClearTilePopulation(GameWorld world, int tileIndex)
        {
            TileData tile = world.tiles[tileIndex];
            tile.populationBlocks = new List<PopulationBlock>();
            world.tiles[tileIndex] = tile;
        }

        private static void SetTilePopulation(GameWorld world, int tileIndex, float amount)
        {
            TileData tile = world.tiles[tileIndex];
            tile.populationBlocks = new List<PopulationBlock>
            {
                new PopulationBlock
                {
                    count = amount,
                    raceId = 0,
                    cultureId = -1,
                    faithId = -1,
                    socialClass = GameEnums.SocialClass.Peasant,
                    satisfaction = 60f
                }
            };
            world.tiles[tileIndex] = tile;
        }

        private static void ClearArmiesFrom(GameWorld world, int tileIndex)
        {
            foreach (Army army in world.armies.Values)
            {
                if (army != null && army.currentTileIndex == tileIndex)
                {
                    army.currentTileIndex = (tileIndex + 1) % world.tiles.Length;
                    army.state = GameEnums.CombatState.Idle;
                }
            }
        }

        [Test]
        public void LowPopulationWithoutGarrison_AbandonsAfterPressure()
        {
            var world = NewWorld();
            SettlementData settlement = FirstSettlement(world);
            Assert.IsNotNull(settlement, "前置：应有聚落");
            settlement.garrison = 0;
            settlement.abandonmentPressureDays = 179;
            foreach (int tileIndex in Simulation.Systems.SettlementPopulationSystem.CollectTiles(world, settlement))
            {
                ClearTilePopulation(world, tileIndex);
            }
            Simulation.Systems.SettlementPopulationSystem.Recompute(world, settlement);
            ClearArmiesFrom(world, settlement.tileIndex);
            int anchorId = settlement.anchorId;
            string siteId = settlement.siteId;

            world.RunSettlementTypologyNow();

            Assert.IsFalse(world.settlements.ContainsKey(anchorId), "达到压力阈值应废弃聚落");
            bool campFound = false;
            foreach (var camp in world.Camps.AllCamps)
            {
                if (camp != null && camp.siteId == siteId) { campFound = true; break; }
            }
            Assert.IsTrue(campFound, "废弃后应留下营地");
        }

        [Test]
        public void HealthySettlement_ResetsPressure()
        {
            var world = NewWorld();
            SettlementData settlement = FirstSettlement(world);
            Assert.IsNotNull(settlement);
            settlement.garrison = 50;
            settlement.abandonmentPressureDays = 50;
            SetTilePopulation(world, settlement.tileIndex, 1000f);

            world.RunSettlementTypologyNow();
            Assert.Greater(world.SettlementTypologyTickCount, 0, "类型学日结应被调用");
            Assert.Greater(world.SettlementTypologySettlementCount, 0, "系统应持有聚落表");
            Assert.Greater(world.SettlementTypologySedentaryCount, 0, "系统内应有定居聚落");
            Assert.IsTrue(world.SettlementTypologyHasSettlement(settlement.anchorId), "系统与 world.settlements 必须是同一张表");
            Assert.IsTrue(world.settlements.ContainsKey(settlement.anchorId), "健康聚落不应被废弃");
            Assert.AreEqual(0, settlement.abandonmentPressureDays, "人口/守军恢复应清零压力");
        }

        [Test]
        public void AbandonmentPressure_RoundTripsThroughSave()
        {
            var world = NewWorld();
            SettlementData settlement = FirstSettlement(world);
            Assert.IsNotNull(settlement);
            settlement.abandonmentPressureDays = 42;

            Assert.IsTrue(SaveSystem.SaveGame(world, SaveName), "存盘应成功");
            var loaded = SaveSystem.LoadGame(SaveName);
            Assert.IsNotNull(loaded, "读档应返回世界");
            _objects.Add(loaded.gameObject);

            Assert.IsTrue(loaded.settlements.TryGetValue(settlement.anchorId, out SettlementData loadedSettlement));
            Assert.AreEqual(42, loadedSettlement.abandonmentPressureDays, "废弃压力应往返");
        }
    }
}
