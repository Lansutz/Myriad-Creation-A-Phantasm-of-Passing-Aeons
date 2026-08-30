using NUnit.Framework;
using UnityEngine;
using CivilizationEvolution.Core;
using CivilizationEvolution.Map;

namespace CivilizationEvolution.Tests
{
    /// <summary>
    /// 大陆形态测试（多倍频高度场 + 山脉脊线 + 河流追踪）
    /// </summary>
    public class TerrainGeneratorTests
    {
        private TileData[] MakeTiles(int w, int h)
        {
            var tiles = new TileData[w * h];
            for (int i = 0; i < tiles.Length; i++)
            {
                tiles[i] = new TileData
                {
                    tileIndex = i,
                    regionId = i / 16,
                    buildingLevels = new int[6],
                    exists = true,
                    isLand = true
                };
            }
            return tiles;
        }

        [Test]
        public void HeightField_DeterministicBySeed()
        {
            int w = 64, h = 48;
            var t1 = MakeTiles(w, h);
            var t2 = MakeTiles(w, h);

            new TerrainGenerator(42).Generate(t1, w, h, 1f, 0.6f);
            new TerrainGenerator(42).Generate(t2, w, h, 1f, 0.6f);

            for (int i = 0; i < t1.Length; i++)
                Assert.AreEqual(t1[i].elevation01, t2[i].elevation01, 0.0001f, "同种子高度场一致");
        }

        [Test]
        public void HeightField_DifferentSeeds_Differ()
        {
            int w = 64, h = 48;
            var t1 = MakeTiles(w, h);
            var t2 = MakeTiles(w, h);

            new TerrainGenerator(1).Generate(t1, w, h, 1f, 0.6f);
            new TerrainGenerator(2).Generate(t2, w, h, 1f, 0.6f);

            int diff = 0;
            for (int i = 0; i < t1.Length; i++)
                if (Mathf.Abs(t1[i].elevation01 - t2[i].elevation01) > 0.01f) diff++;
            Assert.Greater(diff, t1.Length / 10, "不同种子应有显著差异");
        }

        [Test]
        public void Mountains_HighElevationExist()
        {
            int w = 128, h = 64;
            var tiles = MakeTiles(w, h);

            new TerrainGenerator(7).Generate(tiles, w, h, 1f, 0.9f);

            // 强山脉参数下应有高地（>0.75）与坡度
            int high = 0, steep = 0;
            foreach (var t in tiles)
            {
                if (t.elevation01 > 0.75f) high++;
                if (t.slopeDegree > 30f) steep++;
            }
            Assert.Greater(high, 10, "山脉高地应存在");
            Assert.Greater(steep, 10, "陡坡应存在");
        }

        [Test]
        public void Rivers_FlowOnLand_Downhill()
        {
            int w = 128, h = 64;
            var tiles = MakeTiles(w, h);

            var gen = new TerrainGenerator(7);
            gen.Generate(tiles, w, h, 1f, 0.9f);
            gen.TrackRivers(tiles);

            int riverCount = 0;
            float avgRiverElev = 0f, avgLandElev = 0f;
            int landCount = 0;
            foreach (var t in tiles)
            {
                if (t.isRiver)
                {
                    riverCount++;
                    avgRiverElev += t.elevation01;
                }
                if (t.isLand) { landCount++; avgLandElev += t.elevation01; }
            }

            Assert.Greater(riverCount, 10, "应有河流");
            Assert.Less(avgRiverElev / riverCount, avgLandElev / landCount + 0.05f,
                "河流平均海拔不高于陆地平均（流向低处）");
        }
    }
}
