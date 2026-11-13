using System.Linq;
using NUnit.Framework;
using MyriadCreation.Core.Data;
using MyriadCreation.World;

namespace MyriadCreation.Tests.EditMode
{
    /// <summary>省份生成：空间加速后仍需保证全覆盖、确定性、省数合理。</summary>
    public class ProvinceGeneratorTests
    {
        private static TileData[] Grid(int w, int h, int oceanEvery = 0)
        {
            var tiles = new TileData[w * h];
            for (int i = 0; i < tiles.Length; i++)
            {
                tiles[i] = new TileData
                {
                    exists = true,
                    isLand = oceanEvery <= 0 || i % oceanEvery != 0,
                    elevation01 = 0.3f,
                };
            }
            return tiles;
        }

        [Test]
        public void CellsPerProvinceFor_TargetsProvinceCount()
        {
            Assert.AreEqual(25, ProvinceGenerator.CellsPerProvinceFor(150000, 6000));
            Assert.AreEqual(8, ProvinceGenerator.CellsPerProvinceFor(150000, 100000), "最小 8 格");
            Assert.AreEqual(1, ProvinceGenerator.CellsPerProvinceFor(0, 6000));
        }

        [Test]
        public void Generate_AssignsEveryLandTile()
        {
            int w = 32, h = 32;
            var tiles = Grid(w, h);
            var gen = new ProvinceGenerator(tiles, w, h, false);
            var provinces = gen.Generate(12345, cellsPerProvince: 32, lloydIterations: 1);

            Assert.Greater(provinces.Count, 1);
            int assigned = 0;
            for (int i = 0; i < tiles.Length; i++)
            {
                if (!tiles[i].isLand) continue;
                assigned++;
                Assert.GreaterOrEqual(tiles[i].provinceId, 0, "每个陆地格都必须归省");
                Assert.IsTrue(provinces.ContainsKey(tiles[i].provinceId));
            }
            Assert.AreEqual(assigned, provinces.Values.Sum(p => p.memberTiles.Count));
            foreach (var province in provinces.Values)
            {
                Assert.Greater(province.memberTiles.Count, 0);
                Assert.IsTrue(tiles[province.centerTileIndex].isLand);
            }
        }

        [Test]
        public void GenerateAsync_CompletesOnWorker()
        {
            int w = 32, h = 32;
            var tiles = Grid(w, h);
            var task = ProvinceGenerator.GenerateAsync(tiles, w, h, false, 42, 32, 1);
            Assert.IsTrue(task.Wait(10000), "后台生成应在超时前完成");
            var provinces = task.GetAwaiter().GetResult();
            Assert.Greater(provinces.Count, 1);
            int assigned = 0;
            for (int i = 0; i < tiles.Length; i++)
                if (tiles[i].isLand) { assigned++; Assert.GreaterOrEqual(tiles[i].provinceId, 0); }
            Assert.Greater(assigned, 0);
        }

        [Test]
        public void GenerateAsync_ReportsProgressToCompletion()
        {
            int w = 32, h = 32;
            var tiles = Grid(w, h);
            float last = -1f;
            var task = ProvinceGenerator.GenerateAsync(tiles, w, h, false, 42, 32, 1, p => last = p);
            Assert.IsTrue(task.Wait(10000));
            Assert.AreEqual(1f, last, 0.001f, "最后进度必须是 1");
        }

        [Test]
        public void Generate_OceanTilesStayUnassigned()
        {
            int w = 24, h = 24;
            var tiles = Grid(w, h, oceanEvery: 3);
            var gen = new ProvinceGenerator(tiles, w, h, false);
            var provinces = gen.Generate(7, cellsPerProvince: 24, lloydIterations: 1);
            for (int i = 0; i < tiles.Length; i++)
            {
                if (!tiles[i].isLand)
                    Assert.AreEqual(-1, tiles[i].provinceId);
            }
            Assert.Greater(provinces.Count, 0);
        }

        [Test]
        public void Generate_IsDeterministic()
        {
            int w = 32, h = 32;
            var a = Grid(w, h);
            var b = Grid(w, h);
            var first = new ProvinceGenerator(a, w, h, false).Generate(999, 40, 1);
            var second = new ProvinceGenerator(b, w, h, false).Generate(999, 40, 1);

            Assert.AreEqual(first.Count, second.Count);
            for (int i = 0; i < a.Length; i++)
                Assert.AreEqual(a[i].provinceId, b[i].provinceId, "同种子必须同归属");
        }
    }
}
