using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using MyriadCreation.Content.Definitions;
using MyriadCreation.Core.Data;
using MyriadCreation.Core.Enums;
using MyriadCreation.Simulation.Actors;
using MyriadCreation.Simulation.State;
using MyriadCreation.Simulation.Systems;
using MyriadCreation.Simulation.WorldState;
using MyriadCreation.World.Settlement;

namespace MyriadCreation.Tests.EditMode
{
    /// <summary>
    /// P6：营地补给来自本地劳动/狩猎/捕鱼；人口迁移从过载地块来，不再随机造人。
    /// </summary>
    public class SettlementLifecycleTests
    {
        private readonly List<GameObject> _objects = new List<GameObject>();

        [TearDown]
        public void TearDown()
        {
            foreach (var go in _objects)
            {
                if (go != null) Object.DestroyImmediate(go);
            }
            _objects.Clear();
        }

        private GameWorld NewFlatWorld(int size = 64)
        {
            var go = new GameObject("LifecycleWorld");
            _objects.Add(go);
            var world = go.AddComponent<GameWorld>();
            world.mapWidth = 8;
            world.mapHeight = size / 8;
            world.tiles = new TileData[size];
            world.settlements = new Dictionary<int, SettlementData>();
            world.tradeCenters = new Dictionary<int, TradeCenter>();
            for (int i = 0; i < size; i++) world.tiles[i] = Land(i, 1f, 1f);
            return world;
        }

        private static TileData Land(int tileIndex, float fertility, float npp, float wildlife = 1f, bool coast = false)
        {
            return new TileData
            {
                exists = true,
                isLand = true,
                tileIndex = tileIndex,
                regionId = 0,
                biome = GameEnums.BiomeType.TemperateGrassland,
                elevation01 = 0.2f,
                fertility = fertility,
                npp = npp,
                vegetation = 1f,
                wildlife = wildlife,
                fishStock = coast ? 1f : 0f,
                isCoast = coast
            };
        }

        [Test]
        public void Camp_WithLocalFood_SurvivesAndCanEvolve()
        {
            var world = NewFlatWorld();
            var camp = new CampData
            {
                tileIndex = 0,
                population = 50f,
                supplies = 50f,
                morale = 80f,
                type = CampType.Refugee
            };

            for (int day = 0; day < 2200; day++) camp.Tick(world, 1f);

            Assert.IsFalse(camp.isAbandoned, "有本地农业产出的营地不应饿死");
            Assert.GreaterOrEqual(camp.permanence, 100f, "营地应能积累到可演化永久度");
            Assert.IsTrue(camp.CanEvolveToSettlement, "永久度与人口达标后应可演化为聚落");
        }

        [Test]
        public void Camp_WithoutLocalFood_IsAbandoned()
        {
            var world = NewFlatWorld();
            world.tiles[0] = Land(0, 0f, 0f, 0f, coast: false);
            var camp = new CampData
            {
                tileIndex = 0,
                population = 50f,
                supplies = 0f,
                morale = 80f,
                type = CampType.Refugee
            };

            for (int day = 0; day < 80 && !camp.isAbandoned; day++) camp.Tick(world, 1f);

            Assert.IsTrue(camp.isAbandoned, "无农业/野生/鱼群的营地应因补给耗尽废弃");
        }

        [Test]
        public void Migration_MovesPeopleFromOverloadedTile()
        {
            var world = NewFlatWorld();
            var source = Land(0, 1f, 1f);
            source.populationBlocks = new List<PopulationBlock>
            {
                // 无开发（dev=0）的温带草地承载力约 1350；1800 人明确过载，迁移才有意义。
                new PopulationBlock { count = 1800f, raceId = 0, cultureId = 0, faithId = 0, socialClass = GameEnums.SocialClass.Peasant, satisfaction = 60f }
            };
            world.tiles[0] = source;
            var spawn = new SandboxSpawnSystem(world, 7);

            float before = PopulationLedger.Total(world.tiles[0]);
            bool moved = spawn.TrySeedPopulation();
            float sourceAfter = PopulationLedger.Total(world.tiles[0]);
            float targetAfter = 0f;
            for (int i = 1; i < world.tiles.Length; i++) targetAfter += PopulationLedger.Total(world.tiles[i]);

            Assert.IsTrue(moved, "过载地块应发生迁移");
            Assert.Less(sourceAfter, before, "源地块人口应下降");
            Assert.Greater(targetAfter, 0f, "目标地块人口应上升");
            Assert.AreEqual(before, sourceAfter + targetAfter, 0.01f, "迁移前后总人口必须守恒");
        }

        [Test]
        public void Migration_NoOverload_DoesNotCreatePeople()
        {
            var world = NewFlatWorld();
            for (int i = 0; i < world.tiles.Length; i++)
            {
                TileData tile = Land(i, 1f, 1f);
                tile.populationBlocks = new List<PopulationBlock>
                {
                    new PopulationBlock { count = 50f, raceId = 0, cultureId = 0, faithId = 0, socialClass = GameEnums.SocialClass.Peasant, satisfaction = 60f }
                };
                world.tiles[i] = tile;
            }
            float before = 0f;
            foreach (TileData tile in world.tiles) before += PopulationLedger.Total(tile);
            var spawn = new SandboxSpawnSystem(world, 7);

            bool moved = spawn.TrySeedPopulation();
            float after = 0f;
            foreach (TileData tile in world.tiles) after += PopulationLedger.Total(tile);

            Assert.IsFalse(moved, "没有过载源时不应迁移");
            Assert.AreEqual(before, after, 0.01f, "没有过载源时不得凭空新增人口");
        }

        [Test]
        public void MarketDelivery_AddsSettlementWealthFromDeliveredGoods()
        {
            var settlement = new SettlementData
            {
                anchorId = 0,
                anchor = new MyriadCreation.World.Anchor.AnchorData { anchorId = 0, tileIndex = 16 },
                wealth = 0f
            };
            var settlements = new Dictionary<int, SettlementData> { [0] = settlement };
            var origin = new TradeCenter { regionId = 0, centerTileIndex = 0 };
            var destination = new TradeCenter { regionId = 1, centerTileIndex = 16 };
            origin.tradeRoutes.Add(new TradeRoute { fromRegionId = 0, toRegionId = 1, nodeTileIndices = new List<int> { 0, 16 } });
            var centers = new Dictionary<int, TradeCenter> { [0] = origin, [1] = destination };
            var goods = new Dictionary<int, GoodsDef>
            {
                [0] = new GoodsDef { goodsId = 0, baseValue = 1f, weight = 1f }
            };
            var economy = new EconomyManager(
                new TileData[32], centers, goods, new CurrencySystem(), new TaxSystem(),
                realms: null, mapWidth: 8, settlements: settlements);
            var caravan = new Caravan
            {
                caravanId = 1,
                fromRegionId = 0,
                toRegionId = 1,
                isMoving = false,
                cargo = new Dictionary<int, float> { [0] = 100f }
            };
            economy.RegisterCaravan(caravan);

            economy.MoveCaravans();

            Assert.Greater(settlement.wealth, 0f, "商队实际入库的货物价值应留成到目的地聚落财富");
            Assert.AreEqual(100f, destination.inventory[0], 0.01f, "货物仍应真实入库");
        }
    }
}
