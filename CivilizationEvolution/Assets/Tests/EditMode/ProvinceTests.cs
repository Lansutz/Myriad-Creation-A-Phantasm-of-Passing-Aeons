using System.Collections.Generic;
using NUnit.Framework;
using CivilizationEvolution.Core;
using CivilizationEvolution.Map;

namespace CivilizationEvolution.Tests
{
    /// <summary>
    /// </summary>
    public class ProvinceTests
    {
        private TileData[] MakeTiles(int width, int height, System.Func<int, int, bool> isLand)
        {
            var tiles = new TileData[width * height];
            for (int y = 0; y < height; y++)
                for (int x = 0; x < width; x++)
                {
                    int i = y * width + x;
                    tiles[i] = new TileData
                    {
                        tileIndex = i,
                        regionId = i / 16,
                        provinceId = -1,
                        buildingLevels = new int[6],
                        isLand = isLand(x, y),
                        exists = isLand(x, y),
                        elevation01 = 0.5f,
                        annualPrecipMm = 600f
                    };
                }
            return tiles;
        }

        [Test]
        public void Provinces_CoverAllLandTiles()
        {
            // 64×32 全陆地（简单矩形大陆）
            int w = 64, h = 32;
            var tiles = MakeTiles(w, h, (x, y) => true);

            var generator = new ProvinceGenerator(tiles, w, h, wrapX: true);
            var provinces = generator.Generate(seed: 42);

            // 每省有成员+中心
            Assert.Greater(provinces.Count, 0, "应有省份");
            int expectedCount = (w * h) / ProvinceGenerator.DefaultCellsPerProvince;
            Assert.That(provinces.Count, Is.InRange(expectedCount - 2, expectedCount + 2), "省份数≈陆地/cellsPerProvince");

            // 陆地全部归属
            int landTotal = 0, assigned = 0;
            foreach (var tile in tiles)
            {
                if (!tile.isLand) continue;
                landTotal++;
                if (tile.provinceId >= 0) assigned++;
            }
            Assert.AreEqual(landTotal, assigned, "全部陆地应归属省份");

            // 每省成员非空且含中心
            foreach (var province in provinces.Values)
            {
                Assert.Greater(province.memberTiles.Count, 0, "省应有成员");
                Assert.IsTrue(province.memberTiles.Contains(province.centerTileIndex), "中心应在成员内");
                Assert.IsFalse(string.IsNullOrEmpty(province.provinceName), "省应有名称");
            }
        }

        [Test]
        public void Provinces_IslandsGetTheirOwn()
        {
            // 两个分离大陆（左 20 列 + 右 20 列，中间海）——各大陆都应产生省份
            int w = 64, h = 32;
            var tiles = MakeTiles(w, h, (x, y) => x < 20 || x >= 44);

            var generator = new ProvinceGenerator(tiles, w, h, wrapX: true);
            var provinces = generator.Generate(seed: 7);

            // 左大陆与右大陆的 tile 都应归属（无省份跨越海洋——种子距离保证）
            bool leftAssigned = false, rightAssigned = false;
            foreach (var province in provinces.Values)
            {
                foreach (int t in province.memberTiles)
                {
                    int x = t % w;
                    if (x < 20) leftAssigned = true;
                    if (x >= 44) rightAssigned = true;
                }
            }
            Assert.IsTrue(leftAssigned, "左大陆应有省份");
            Assert.IsTrue(rightAssigned, "右大陆应有省份");
        }

        [Test]
        public void Provinces_DeterministicBySeed()
        {
            int w = 32, h = 32;
            var tiles1 = MakeTiles(w, h, (x, y) => true);
            var tiles2 = MakeTiles(w, h, (x, y) => true);

            var g1 = new ProvinceGenerator(tiles1, w, h, wrapX: false);
            var g2 = new ProvinceGenerator(tiles2, w, h, wrapX: false);
            var p1 = g1.Generate(seed: 99);
            var p2 = g2.Generate(seed: 99);

            Assert.AreEqual(p1.Count, p2.Count, "同种子省份数一致");
            bool allSame = true;
            for (int i = 0; i < tiles1.Length; i++)
                if (tiles1[i].provinceId != tiles2[i].provinceId) { allSame = false; break; }
            Assert.IsTrue(allSame, "同种子归属完全一致（确定性）");
        }
    }
}
