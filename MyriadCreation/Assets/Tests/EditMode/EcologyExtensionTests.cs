using System.Collections.Generic;
using NUnit.Framework;
using MyriadCreation.Core.Data;
using MyriadCreation.Core.Enums;
using MyriadCreation.Simulation.Systems;
using UnityEngine;

namespace MyriadCreation.Tests.EditMode
{
    /// <summary>
    /// P4.3 生态扩展：沿海捕鱼扣鱼群、放牧压低植被、火灾确定性触发且可恢复。
    /// </summary>
    public class EcologyExtensionTests
    {
        private static TileData Land(float npp, float vegetation = 0.8f, int people = 0, bool coast = false, float livestock = 0f)
        {
            var blocks = new List<PopulationBlock>();
            if (people > 0) blocks.Add(new PopulationBlock { count = people, socialClass = GameEnums.SocialClass.Peasant });
            return new TileData
            {
                exists = true,
                isLand = true,
                isCoast = coast,
                npp = npp,
                vegetation = vegetation,
                wildlife = vegetation * 0.4f,
                fishStock = coast ? 0.5f : 0f,
                livestock = livestock,
                annualTemp = 28f,
                annualPrecipMm = 300f,
                populationBlocks = blocks
            };
        }

        [Test]
        public void HarvestFish_OnlyCoastalAndConsumesStock()
        {
            TileData[] tiles = { Land(0.8f, coast: true, people: 100) };
            float before = tiles[0].fishStock;
            float harvested = EcosystemSystem.HarvestFish(ref tiles[0], 1f);

            Assert.Greater(harvested, 0f);
            Assert.Less(tiles[0].fishStock, before, "捕捞必须扣鱼群");

            TileData[] inland = { Land(0.8f, people: 100) };
            Assert.AreEqual(0f, EcosystemSystem.HarvestFish(ref inland[0], 1f), 0.0001f, "内陆不产鱼");
        }

        [Test]
        public void FishRecovers_WhenNotFished()
        {
            TileData[] tiles = { Land(0.8f, coast: true) };
            tiles[0].fishStock = 0.1f;

            EcosystemSystem.Tick(tiles, 30f * 12f, 5);

            Assert.Greater(tiles[0].fishStock, 0.1f, "无人捕捞时鱼群应向承载恢复");
        }

        [Test]
        public void Grazing_LowersVegetationTarget()
        {
            TileData[] tiles =
            {
                Land(0.8f, vegetation: 0.8f),
                Land(0.8f, vegetation: 0.8f, livestock: 1f)
            };

            EcosystemSystem.Tick(tiles, 30f * 24f, 5);

            Assert.Less(tiles[1].vegetation, tiles[0].vegetation, "放牧压力应压低植被");
        }

        [Test]
        public void Fire_IsDeterministicAndRecovers()
        {
            TileData wet = Land(0.8f);
            wet.annualPrecipMm = 1500f;
            Assert.IsFalse(EcosystemSystem.ShouldBurn(in wet, 1), "湿润地块不应起火");

            TileData dry = Land(0.9f, vegetation: 0.9f);
            dry.tileIndex = 7;
            int fireYear = -1;
            for (int year = 0; year < 100000; year++)
            {
                if (EcosystemSystem.ShouldBurn(in dry, year))
                {
                    fireYear = year;
                    break;
                }
            }
            Assert.GreaterOrEqual(fireYear, 0, "干燥高温高植被地块在足够年份内应出现火灾");

            bool first = EcosystemSystem.ShouldBurn(in dry, fireYear);
            bool second = EcosystemSystem.ShouldBurn(in dry, fireYear);
            Assert.AreEqual(first, second, "同一 tile/year 必须同一结果");

            EcosystemSystem.Burn(ref dry);
            Assert.Less(dry.vegetation, 0.9f, "火灾应烧掉植被");
            TileData[] tiles = { dry };
            EcosystemSystem.Tick(tiles, 30f * 12f, 999999);
            Assert.Greater(tiles[0].vegetation, dry.vegetation, "火灾后植被应逐步恢复（非火灾年）");
        }

        [Test]
        public void NewFields_SurviveJsonRoundTrip()
        {
            TileData tile = Land(0.8f, coast: true);
            tile.livestock = 0.4f;
            tile.fishStock = 0.6f;

            string json = JsonUtility.ToJson(tile);
            TileData restored = JsonUtility.FromJson<TileData>(json);

            Assert.AreEqual(0.6f, restored.fishStock, 0.001f);
            Assert.AreEqual(0.4f, restored.livestock, 0.001f);
        }
    }
}
