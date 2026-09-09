using System;
using CivilizationEvolution.Core;
using System.Collections.Generic;

namespace CivilizationEvolution.Map
{
    public class ProvinceGenerator
    {
        private readonly TileData[] _tiles;
        private readonly int _width;
        private readonly int _height;
        private readonly bool _wrapX;

        public ProvinceGenerator(TileData[] tiles, int width, int height, bool wrapX)
        {
            _tiles = tiles;
            _width = width;
            _height = height;
            _wrapX = wrapX;
        }

 /// <summary>每省目标地块数（cells_per_province——默认 64 地块≈省份规模）</summary>
        public const int DefaultCellsPerProvince = 64;

 /// <summary>Lloyd 松弛迭代次数（形状优化）</summary>
        public const int DefaultLloydIterations = 3;

 /// 生成省份
        public Dictionary<int, Province> Generate(int seed, int cellsPerProvince = DefaultCellsPerProvince,
            int lloydIterations = DefaultLloydIterations)
        {
            var rng = new System.Random(seed);

 // 0. 重置全部地块归属（-1=未归属；struct 默认 0，必须显式重置）
            for (int i = 0; i < _tiles.Length; i++)
                _tiles[i].provinceId = -1;

 // 1. 收集陆地 tile 并采样种子点
            var landTiles = new List<int>();
            for (int i = 0; i < _tiles.Length; i++)
                if (_tiles[i].isLand) landTiles.Add(i);

            int provinceCount = Math.Max(1, landTiles.Count / Math.Max(1, cellsPerProvince));
            var seeds = new List<int>();
            var seedSet = new HashSet<int>();
            while (seeds.Count < provinceCount && seedSet.Count < landTiles.Count)
            {
                int candidate = landTiles[rng.Next(landTiles.Count)];
                if (seedSet.Add(candidate)) seeds.Add(candidate);
            }

 // 2. Lloyd 松弛迭代
            int[] assignment = new int[_tiles.Length];
            Array.Fill(assignment, -1);
            for (int iter = 0; iter < lloydIterations; iter++)
            {
 // 每 tile 归最近种子
                for (int i = 0; i < _tiles.Length; i++)
                {
                    if (!_tiles[i].isLand) continue;
                    assignment[i] = NearestSeed(i, seeds);
                }

 // 种子移到所属集合质心
                var centroidX = new float[seeds.Count];
                var centroidY = new float[seeds.Count];
                var counts = new int[seeds.Count];
                for (int i = 0; i < _tiles.Length; i++)
                {
                    if (!_tiles[i].isLand || assignment[i] < 0) continue;
                    int x = i % _width;
                    int y = i / _width;
                    centroidX[assignment[i]] += x;
                    centroidY[assignment[i]] += y;
                    counts[assignment[i]]++;
                }

                for (int s = 0; s < seeds.Count; s++)
                {
                    if (counts[s] == 0) continue;
                    int cx = (int)(centroidX[s] / counts[s]);
                    int cy = (int)(centroidY[s] / counts[s]);
                    int newSeed = cy * _width + cx;
                    if (newSeed >= 0 && newSeed < _tiles.Length)
                        seeds[s] = newSeed;
                }
            }

 // 3. 最终归属
            for (int i = 0; i < _tiles.Length; i++)
            {
                if (!_tiles[i].isLand) continue;
                assignment[i] = NearestSeed(i, seeds);
            }

 // 4. 组装省份
            var provinces = new Dictionary<int, Province>();
            for (int s = 0; s < seeds.Count; s++)
            {
                provinces[s] = new Province
                {
                    provinceId = s,
                    provinceName = GenerateProvinceName(_tiles[seeds[s]]),
                    centerTileIndex = seeds[s]
                };
            }
            for (int i = 0; i < _tiles.Length; i++)
            {
                if (!_tiles[i].isLand || assignment[i] < 0) continue;
                provinces[assignment[i]].memberTiles.Add(i);
                _tiles[i].provinceId = assignment[i];
            }

            return provinces;
        }

 /// <summary>最近种子（欧氏距离，wrapX 感知环绕）</summary>
        private int NearestSeed(int tileIndex, List<int> seeds)
        {
            int x = tileIndex % _width;
            int y = tileIndex / _width;
            int best = 0;
            float bestDist = float.MaxValue;
            for (int s = 0; s < seeds.Count; s++)
            {
                int sx = seeds[s] % _width;
                int sy = seeds[s] / _width;
                float dx = Math.Abs(x - sx);
                if (_wrapX) dx = Math.Min(dx, _width - dx); // 环绕
                float dy = y - sy;
                float dist = dx * dx + dy * dy;
                if (dist < bestDist)
                {
                    bestDist = dist;
                    best = s;
                }
            }
            return best;
        }


 /// <summary>省名生成（地形特征词组合——中文地名池；世界构建词干体系可后续接入）</summary>
        private static string GenerateProvinceName(TileData center)
        {
            string terrainWord = center.elevation01 > 0.55f ? "山地" : "平原";
            if (center.isCoast) terrainWord = "滨海" + terrainWord;
            string feature = center.annualPrecipMm > 900f ? "青" : center.annualPrecipMm < 300f ? "赤" : "沃";
            return feature + terrainWord + "原";
        }
    }
}
