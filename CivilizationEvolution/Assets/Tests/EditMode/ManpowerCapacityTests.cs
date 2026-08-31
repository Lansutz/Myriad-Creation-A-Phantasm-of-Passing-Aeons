using System.Collections.Generic;
using NUnit.Framework;
using CivilizationEvolution.Core;
using CivilizationEvolution.Economy;
using CivilizationEvolution.Politics;

namespace CivilizationEvolution.Tests
{
    /// <summary>
    /// 人口承载与军事人力测试（两独立系统：承载=人口上限，人力=征募池）
    /// </summary>
    public class ManpowerCapacityTests
    {
        private TileData MakeTile(GameEnums.BiomeType biome, bool coast = false, bool river = false)
        {
            return new TileData
            {
                tileIndex = 0,
                regionId = 0,
                isLand = true,
                biome = biome,
                isCoast = coast,
                isRiver = river,
                buildingLevels = new int[6],
                populationBlocks = new List<PopulationBlock>()
            };
        }

        [Test]
        public void CarryingCapacity_TerrainVariation()
        {
            var plain = MakeTile(GameEnums.BiomeType.TemperateGrassland);
            var desert = MakeTile(GameEnums.BiomeType.HotDesert);
            var river = MakeTile(GameEnums.BiomeType.TemperateGrassland, river: true);
            var coast = MakeTile(GameEnums.BiomeType.TemperateGrassland, coast: true);

            float p = CarryingCapacitySystem.CalculateCarryingCapacity(plain, null);
            float d = CarryingCapacitySystem.CalculateCarryingCapacity(desert, null);
            float r = CarryingCapacitySystem.CalculateCarryingCapacity(river, null);
            float c = CarryingCapacitySystem.CalculateCarryingCapacity(coast, null);

            Assert.Greater(p, d, "平原承载高于沙漠");
            Assert.Greater(r, p, "河流承载加成");
            Assert.Greater(c, p, "海岸承载加成");
        }

        [Test]
        public void CarryingCapacity_GranaryAndTradeBoost()
        {
            var tile = MakeTile(GameEnums.BiomeType.TemperateGrassland);
            var centers = new Dictionary<int, TradeCenter> { [0] = new TradeCenter { regionId = 0 } };

            float noTrade = CarryingCapacitySystem.CalculateCarryingCapacity(tile, null);
            float withTrade = CarryingCapacitySystem.CalculateCarryingCapacity(tile, centers);

            Assert.Greater(withTrade, noTrade, "贸易中心（地区仓储）加成");
        }

        [Test]
        public void CarryingCapacity_FoodStockSupportsPopulation()
        {
            var tile = MakeTile(GameEnums.BiomeType.HotDesert); // 低地理系数 0.4
            tile.populationBlocks.Add(new PopulationBlock { count = 50f, socialClass = GameEnums.SocialClass.Peasant });

            var centers = new Dictionary<int, TradeCenter>
            {
                [0] = new TradeCenter { regionId = 0 }
            };

            // 无存粮：承载被压缩
            float hungry = CarryingCapacitySystem.CalculateCarryingCapacity(tile, centers);

            // 大量存粮（粮食 0 号 5000 单位）：承载提升
            centers[0].inventory[0] = 5000f;
            float fed = CarryingCapacitySystem.CalculateCarryingCapacity(tile, centers);

            Assert.Greater(fed, hungry, "仓储存粮支撑人口承载");
        }

        [Test]
        public void StorageCapacity_GranaryBuilding()
        {
            // 粮仓建筑（Agriculture 槽）→ 地区仓储容量提升
            var tiles = new TileData[2];
            tiles[0] = MakeTile(GameEnums.BiomeType.TemperateGrassland);
            tiles[0].regionId = 0;
            tiles[0].buildingLevels[0] = 2; // 二级农业建筑（粮仓 tier2）
            tiles[1] = MakeTile(GameEnums.BiomeType.TemperateGrassland);
            tiles[1].regionId = 1;

            var centers = new Dictionary<int, TradeCenter>
            {
                [0] = new TradeCenter { regionId = 0, centerTileIndex = 0 },
                [1] = new TradeCenter { regionId = 1, centerTileIndex = 1 }
            };
            var manager = new EconomyManager(tiles, centers, new Dictionary<int, GoodsDef>(), null, null);
            manager.UpdateStorageCapacities();

            Assert.Greater(centers[0].inventoryCapacity, 10000f, "粮仓提升仓储容量");
            Assert.AreEqual(10000f, centers[1].inventoryCapacity, "无粮仓地区保持基础容量");
        }

        [Test]
        public void CarryingCapacity_OverloadSuppressesGrowth()
        {
            // 超载 >1.1 起抑制（PopulationTick 逻辑——此处验证超载率计算）
            var tile = MakeTile(GameEnums.BiomeType.HotDesert);
            tile.populationBlocks.Add(new PopulationBlock { count = 200f, socialClass = GameEnums.SocialClass.Peasant });

            float capacity = CarryingCapacitySystem.CalculateCarryingCapacity(tile, null);
            float overload = CarryingCapacitySystem.GetOverloadRatio(tile, capacity);

            Assert.Greater(overload, 1.1f, "沙漠高人口应超载");
            Assert.Less(overload, 6f, "超载率量级合理");
        }

        [Test]
        public void Manpower_PoolByClass()
        {
            var realm = new RealmData { realmId = 1 };
            realm.coreTiles.Add(0);
            var tiles = new TileData[2];
            tiles[0] = MakeTile(GameEnums.BiomeType.TemperateGrassland);
            tiles[0].populationBlocks.Add(new PopulationBlock { count = 100f, socialClass = GameEnums.SocialClass.Peasant });
            tiles[0].populationBlocks.Add(new PopulationBlock { count = 10f, socialClass = GameEnums.SocialClass.NobilityClergy });
            tiles[0].populationBlocks.Add(new PopulationBlock { count = 50f, socialClass = GameEnums.SocialClass.Slave });
            tiles[1] = MakeTile(GameEnums.BiomeType.TemperateGrassland);

            var pool = ManpowerSystem.GetRealmManpowerPool(1, tiles, new Dictionary<int, RealmData> { [1] = realm });

            // 农民 100块×50人×0.10 = 500
            Assert.That(pool[GameEnums.SocialClass.Peasant], Is.EqualTo(500f).Within(1f), "农民人力");
            // 贵族 10块×50×0.08 = 40
            Assert.That(pool[GameEnums.SocialClass.NobilityClergy], Is.EqualTo(40f).Within(1f), "贵族人力");
            // 奴隶不征募
            Assert.IsFalse(pool.ContainsKey(GameEnums.SocialClass.Slave), "奴隶不征募");

            // 总人力 = 540
            Assert.That(ManpowerSystem.GetRealmTotalManpower(1, tiles, new Dictionary<int, RealmData> { [1] = realm }),
                Is.EqualTo(540f).Within(1f), "总人力");
        }
    }
}
