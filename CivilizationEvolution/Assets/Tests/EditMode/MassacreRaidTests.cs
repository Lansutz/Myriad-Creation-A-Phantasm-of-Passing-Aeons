using System.Collections.Generic;
using NUnit.Framework;
using CivilizationEvolution.Core;
using CivilizationEvolution.Diplomacy;
using CivilizationEvolution.Politics;

namespace CivilizationEvolution.Tests
{
    /// <summary>
    /// 屠城劫掠测试（大规模屠杀：人口削减+高掠夺+最重报复借口）
    /// </summary>
    public class MassacreRaidTests
    {
        private DiplomacyManager _dm;
        private TileData[] _tiles;

        [SetUp]
        public void Setup()
        {
            ContentRegistry.Reset();
            ContentRegistry.Initialize();

            var realms = new Dictionary<int, RealmData>();
            for (int i = 0; i < 2; i++)
                realms[i] = new RealmData { realmId = i, realmName = "国" + i };
            _dm = new DiplomacyManager(realms);

            _tiles = new TileData[4];
            for (int i = 0; i < 4; i++)
                _tiles[i] = new TileData
                {
                    tileIndex = i, regionId = 0, isLand = true,
                    buildingLevels = new int[6],
                    populationBlocks = new List<PopulationBlock>
                    {
                        new PopulationBlock { count = 100f, socialClass = GameEnums.SocialClass.Peasant }
                    }
                };
        }

        [Test]
        public void Massacre_ReducesPopulationAndLoots()
        {
            // 敌对关系（hostility ≥50 不升级战争）
            _dm.IncreaseHostility(1, 0, 60f, "测试");

            var result = _dm.RaidSettlement(0, 1, 0, GameEnums.RaidType.Massacre, _tiles);

            Assert.IsTrue(result.success, "屠城应成功");
            Assert.Greater(result.lootValue, 300f, "屠城掠夺远超普通劫掠");
            Assert.That(_tiles[0].populationBlocks[0].count, Is.EqualTo(70f).Within(0.1f), "人口削减 30%");
        }

        [Test]
        public void Massacre_GeneratesStrongestRaidCB()
        {
            var cb = WarJustificationSystem.GenerateRaidReprisalCB(1, 0, 0, 1000, GameEnums.RaidType.Massacre);
            Assert.Greater(cb.justificationStrength, 85f, "屠城报复借口强度最高");

            var village = WarJustificationSystem.GenerateRaidReprisalCB(1, 0, 0, 1000, GameEnums.RaidType.VillageRaid);
            Assert.Greater(cb.justificationStrength, village.justificationStrength, "屠城强于普通劫掠");
        }

        [Test]
        public void NormalRaid_NoPopulationLoss()
        {
            _dm.IncreaseHostility(1, 0, 60f, "测试");
            _dm.RaidSettlement(0, 1, 0, GameEnums.RaidType.VillageRaid, _tiles);
            Assert.That(_tiles[0].populationBlocks[0].count, Is.EqualTo(100f).Within(0.1f), "普通劫掠不削减人口");
        }
    }
}
