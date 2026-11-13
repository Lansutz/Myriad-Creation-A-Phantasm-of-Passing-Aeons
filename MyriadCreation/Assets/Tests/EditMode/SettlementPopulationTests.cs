using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using MyriadCreation.Core.Data;
using MyriadCreation.Core.Enums;
using MyriadCreation.Infrastructure.Save;
using MyriadCreation.Simulation.Systems;
using MyriadCreation.Simulation.WorldState;
using MyriadCreation.World;
using MyriadCreation.World.Anchor;
using MyriadCreation.World.Generation;
using MyriadCreation.World.Settlement;

namespace MyriadCreation.Tests.EditMode
{
    /// <summary>
    /// D40 人口单源：地块人口块是唯一权威，聚落人口只是聚合快照；
    /// 伤亡必须改块，不能只改聚落标量；没有块时不回落到旧标量。
    /// </summary>
    public class SettlementPopulationTests
    {
        private const string SaveName = "__settlement_population_test";
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
            var go = new GameObject("SettlementPopulationWorld");
            _objects.Add(go);
            var world = go.AddComponent<GameWorld>();
            world.mapWidth = 48;
            world.mapHeight = 24;
            world.SetupOptions = WorldSetupOptions.CreateStandard();
            world.InitializeWorld();
            world.GenerateTerrain(43);
            return world;
        }

        private static SettlementData FirstSettlement(GameWorld world)
        {
            foreach (var settlement in world.settlements.Values) return settlement;
            return null;
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

        [Test]
        public void DerivedPopulation_SumsOwnAndControlledTiles()
        {
            var world = NewWorld();
            SettlementData settlement = FirstSettlement(world);
            Assert.IsNotNull(settlement, "前置：应有聚落");
            settlement.anchor ??= new AnchorData { anchorId = settlement.anchorId, tileIndex = settlement.tileIndex };
            settlement.anchor.controlledAnchorIds.Clear();

            SettlementData controlled = null;
            foreach (var other in world.settlements.Values)
            {
                if (other != settlement && other.tileIndex != settlement.tileIndex) { controlled = other; break; }
            }
            Assert.IsNotNull(controlled, "前置：应有第二个聚落");
            settlement.anchor.controlledAnchorIds.Add(controlled.anchorId);
            SetTilePopulation(world, settlement.tileIndex, 100f);
            SetTilePopulation(world, controlled.tileIndex, 250f);

            float total = SettlementPopulationSystem.Recompute(world, settlement);

            Assert.AreEqual(350f, total, 0.01f, "聚落人口 = 自身地块 + 受控锚点地块");
            Assert.AreEqual(350f, settlement.population, 0.01f);
        }

        [Test]
        public void TilePopulationChange_FlowsIntoSnapshot()
        {
            var world = NewWorld();
            SettlementData settlement = FirstSettlement(world);
            Assert.IsNotNull(settlement);
            settlement.anchor ??= new AnchorData { anchorId = settlement.anchorId, tileIndex = settlement.tileIndex };
            settlement.anchor.controlledAnchorIds.Clear();

            SetTilePopulation(world, settlement.tileIndex, 120f);
            SettlementPopulationSystem.Recompute(world, settlement);
            Assert.AreEqual(120f, settlement.population, 0.01f);

            SetTilePopulation(world, settlement.tileIndex, 480f);
            SettlementPopulationSystem.Recompute(world, settlement);

            Assert.AreEqual(480f, settlement.population, 0.01f, "地块人口变化必须反映到聚落快照");
        }

        [Test]
        public void ApplyCasualties_ReducesBlocksAndSnapshot()
        {
            var world = NewWorld();
            SettlementData settlement = FirstSettlement(world);
            Assert.IsNotNull(settlement);
            settlement.anchor ??= new AnchorData { anchorId = settlement.anchorId, tileIndex = settlement.tileIndex };
            settlement.anchor.controlledAnchorIds.Clear();
            SetTilePopulation(world, settlement.tileIndex, 200f);
            SettlementPopulationSystem.Recompute(world, settlement);

            SettlementPopulationSystem.ApplyCasualties(world, settlement, 50f);

            float blockTotal = 0f;
            foreach (PopulationBlock block in world.tiles[settlement.tileIndex].populationBlocks) blockTotal += block.count;
            Assert.AreEqual(150f, blockTotal, 0.01f, "伤亡应落到人口块");
            Assert.AreEqual(150f, settlement.population, 0.01f, "快照应随块重算");
        }

        [Test]
        public void NoBlocks_DoesNotFallBackToLegacyScalar()
        {
            var world = NewWorld();
            SettlementData settlement = FirstSettlement(world);
            Assert.IsNotNull(settlement);
            settlement.anchor ??= new AnchorData { anchorId = settlement.anchorId, tileIndex = settlement.tileIndex };
            settlement.anchor.controlledAnchorIds.Clear();
            TileData tile = world.tiles[settlement.tileIndex];
            tile.populationBlocks = new List<PopulationBlock>();
            world.tiles[settlement.tileIndex] = tile;
            ((HabitationSite)settlement).population = 9999f;

            float total = SettlementPopulationSystem.Recompute(world, settlement);

            Assert.AreEqual(0f, total, "没有人口块时快照为 0，不回落旧标量");
            Assert.AreEqual(0f, settlement.population, 0.01f);
        }

        [Test]
        public void DerivedPopulation_RoundTripsThroughSave()
        {
            var world = NewWorld();
            SettlementData settlement = FirstSettlement(world);
            Assert.IsNotNull(settlement);
            settlement.anchor ??= new AnchorData { anchorId = settlement.anchorId, tileIndex = settlement.tileIndex };
            settlement.anchor.controlledAnchorIds.Clear();
            SetTilePopulation(world, settlement.tileIndex, 321f);
            SettlementPopulationSystem.Recompute(world, settlement);

            Assert.IsTrue(SaveSystem.SaveGame(world, SaveName), "存盘应成功");
            var loaded = SaveSystem.LoadGame(SaveName);
            Assert.IsNotNull(loaded, "读档应返回世界");
            _objects.Add(loaded.gameObject);
            Assert.IsTrue(loaded.settlements.TryGetValue(settlement.anchorId, out SettlementData loadedSettlement));

            float expected = 0f;
            foreach (PopulationBlock block in loaded.tiles[loadedSettlement.tileIndex].populationBlocks) expected += block.count;
            Assert.AreEqual(expected, loadedSettlement.population, 0.01f, "读档后聚落人口应等于地块人口块聚合");
        }
    }
}
