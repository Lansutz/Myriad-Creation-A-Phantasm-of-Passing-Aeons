using NUnit.Framework;
using MyriadCreation.Core.Data;
using MyriadCreation.World.Generation;
using MyriadCreation.World.Terrain;

namespace MyriadCreation.Tests.EditMode
{
    /// <summary>局部模式边界塑形：西海/岛屿在边缘生成海洋，内部保留陆地。</summary>
    public class RegionalTerrainShaperTests
    {
        private static TileData[] Land(int w, int h)
        {
            var tiles = new TileData[w * h];
            for (int i = 0; i < tiles.Length; i++)
                tiles[i] = new TileData { exists = true, isLand = true, elevation01 = 0.5f };
            return tiles;
        }

        [Test]
        public void SeaWest_MakesWestEdgeOcean()
        {
            int w = 20, h = 10;
            var tiles = Land(w, h);
            RegionalTerrainShaper.Apply(tiles, w, h,
                MapGenerationConfig.RegionalEdgeMode.SeaWest, 0.2f, 0.05f);
            Assert.IsFalse(tiles[0].isLand, "西边缘应入海");
            Assert.IsTrue(tiles[5 * w + 5].isLand, "内部应保留陆地");
            Assert.IsTrue(tiles[5 * w + (w - 1)].isLand, "东侧应保留陆地");
        }

        [Test]
        public void Island_MakesEdgesOcean()
        {
            int w = 20, h = 20;
            var tiles = Land(w, h);
            RegionalTerrainShaper.Apply(tiles, w, h,
                MapGenerationConfig.RegionalEdgeMode.Island, 0.2f, 0.05f);
            Assert.IsFalse(tiles[0].isLand);
            Assert.IsFalse(tiles[w * (h - 1)].isLand);
            Assert.IsTrue(tiles[10 * w + 10].isLand);
        }
    }
}
