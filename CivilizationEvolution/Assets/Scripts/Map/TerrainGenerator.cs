using UnityEngine;
using CivilizationEvolution.Core;

namespace CivilizationEvolution.Map
{
    /// <summary>
    /// 大陆形态生成器：多倍频值噪声（fBm）高度场 + 山脉脊线 + 河流追踪
    /// 替代单一正弦波叠加，产生大陆轮廓/山脉链/水系
    /// </summary>
    public class TerrainGenerator
    {
        private readonly int _seed;
        private int _width;
        private int _height;

        public TerrainGenerator(int seed)
        {
            _seed = seed;
        }

        // ===== 值噪声（value noise：网格随机 + 双线性插值） =====

        private float Hash(int x, int y)
        {
            uint h = (uint)(x * 73856093 ^ y * 19349663 ^ _seed * 83492791);
            h = (h ^ (h >> 16)) * 0x7feb352d;
            h = (h ^ (h >> 15)) * 0x846ca68b;
            h = (h ^ (h >> 16));
            return (h & 0xffffff) / (float)0xffffff;
        }

        private float SmoothNoise(float x, float y)
        {
            int x0 = Mathf.FloorToInt(x);
            int y0 = Mathf.FloorToInt(y);
            float fx = x - x0;
            float fy = y - y0;
            fx = fx * fx * (3f - 2f * fx); // smoothstep
            fy = fy * fy * (3f - 2f * fy);

            float v00 = Hash(x0, y0);
            float v10 = Hash(x0 + 1, y0);
            float v01 = Hash(x0, y0 + 1);
            float v11 = Hash(x0 + 1, y0 + 1);

            return Mathf.Lerp(Mathf.Lerp(v00, v10, fx), Mathf.Lerp(v01, v11, fx), fy);
        }

        /// <summary>分形叠加（fBm）：低频大陆轮廓 + 中频起伏 + 高频细节</summary>
        private float Fbm(float x, float y, int octaves = 5, float lacunarity = 2f, float gain = 0.5f)
        {
            float total = 0f;
            float amplitude = 1f;
            float frequency = 1f;
            float max = 0f;
            for (int o = 0; o < octaves; o++)
            {
                total += SmoothNoise(x * frequency, y * frequency) * amplitude;
                max += amplitude;
                amplitude *= gain;
                frequency *= lacunarity;
            }
            return total / max;
        }

        /// <summary>脊线变换（ridge noise）：在噪声中段产生线性山脊，用于山脉链</summary>
        private float Ridge(float x, float y, int octaves = 4, float lacunarity = 2.2f, float gain = 0.5f)
        {
            float total = 0f;
            float amplitude = 1f;
            float frequency = 1f;
            float max = 0f;
            for (int o = 0; o < octaves; o++)
            {
                float n = 1f - Mathf.Abs(2f * SmoothNoise(x * frequency, y * frequency) - 1f);
                total += n * n * amplitude;
                max += amplitude;
                amplitude *= gain;
                frequency *= lacunarity;
            }
            return total / max;
        }

        // ===== 主流程 =====

        /// <summary>
        /// 生成大陆形态：高度场（fBm 大陆轮廓 + 脊线山脉）
        /// 河流追踪须在 isLand 判定完成后单独调用（TrackRivers）
        /// </summary>
        public void Generate(TileData[] tiles, int width, int height,
            float continentScale, float mountainStrength)
        {
            _width = width;
            _height = height;

            float baseFreq = 1.2f / Mathf.Max(8f, Mathf.Min(width, height) / 16f);

            // 1. 高度场：大陆轮廓（低频）+ 起伏（中频）+ 山脉脊线叠加
            for (int i = 0; i < tiles.Length; i++)
            {
                int x = i % width;
                int y = i / width;
                float nx = (float)x / width;
                float ny = (float)y / height;

                float continent = Fbm(nx * 1.5f, ny * 1.5f, 3, 2f, 0.5f);          // 低频大陆轮廓
                float terrain = Fbm(nx * 3f + 100f, ny * 3f + 100f, 3, 2f, 0.5f);  // 中频地形起伏
                float ridge = Ridge(nx * baseFreq * 2f + 50f, ny * baseFreq * 2f + 50f); // 山脊线

                float height01 = continent * 0.65f + terrain * 0.25f;
                height01 += ridge * ridge * mountainStrength * 0.5f; // 脊线→山
                tiles[i].elevation01 = Mathf.Clamp01(height01);
                tiles[i].slopeDegree = Mathf.Clamp(ridge * 60f * mountainStrength
                    + Mathf.Abs(height01 - 0.5f) * 20f, 0f, 60f);
            }
        }

        /// <summary>D8 河流追踪：山脊起始点沿最低邻域流向海/洼地（须在 isLand 判定后调用）</summary>
        public void TrackRivers(TileData[] tiles)
        {
            // 清除旧标记
            for (int i = 0; i < tiles.Length; i++)
                tiles[i].isRiver = false;

            // 起始点：高地（elevation > 0.65）且坡度大——每隔几步采样一个
            for (int y = 0; y < _height; y += 4)
            {
                for (int x = 0; x < _width; x += 4)
                {
                    int i = y * _width + x;
                    if (tiles[i].elevation01 < 0.65f) continue;

                    // 沿最低邻域下降，直到海或回到已标记河道
                    int current = i;
                    int guard = 0;
                    while (guard++ < 500)
                    {
                        if (!tiles[current].isLand) break; // 入海
                        if (tiles[current].isRiver) break; // 汇入已有河
                        tiles[current].isRiver = true;

                        int lowest = LowestNeighbour(current, tiles);
                        if (lowest < 0 || tiles[lowest].elevation01 >= tiles[current].elevation01)
                            break; // 洼地终止
                        current = lowest;
                    }
                }
            }
        }

        private int LowestNeighbour(int index, TileData[] tiles)
        {
            int x = index % _width;
            int y = index / _width;
            int best = -1;
            float bestElev = float.MaxValue;
            for (int dy = -1; dy <= 1; dy++)
            {
                for (int dx = -1; dx <= 1; dx++)
                {
                    if (dx == 0 && dy == 0) continue;
                    int nx = x + dx;
                    int ny = y + dy;
                    if (nx < 0 || nx >= _width || ny < 0 || ny >= _height) continue;
                    int ni = ny * _width + nx;
                    if (tiles[ni].elevation01 < bestElev)
                    {
                        bestElev = tiles[ni].elevation01;
                        best = ni;
                    }
                }
            }
            return best;
        }
    }
}
