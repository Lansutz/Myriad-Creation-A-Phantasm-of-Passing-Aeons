using System;
using System.Collections.Generic;
using UnityEngine;

namespace CivilizationEvolution.Map
{
    /// <summary>
    /// 球面 Voronoi 图（Spherical Voronoi Diagram）
    /// 在单位球面上生成 N 个种子点，将球面划分为 N 个 Voronoi 单元
    /// 用于：板块构造边界、省份生成、生物地理区域划分
    ///
    /// 算法：
    ///   1. 最远点采样（Farthest Point Sampling）生成均匀分布的种子点
    ///   2. 球面大圆弧距离（haversine / 3D点积）计算最近种子
    ///   3. Lloyd 松弛迭代优化单元形状（可选）
    ///   4. 边界检测：与邻域种子不同的点即为边界
    /// </summary>
    public class SphericalVoronoi
    {
        /// <summary>Voronoi 种子点（单位球面 3D 坐标）</summary>
        public struct Seed
        {
            public float x, y, z;
            public int id;
            public Seed(float x, float y, float z, int id) { this.x = x; this.y = y; this.z = z; this.id = id; }
        }

        public Seed[] Seeds { get; private set; }
        public int SeedCount => Seeds?.Length ?? 0;

        private readonly System.Random _rng;

        public SphericalVoronoi(int seed = 42)
        {
            _rng = new System.Random(seed);
        }

        /// <summary>
        /// 生成球面 Voronoi 图
        /// </summary>
        /// <param name="seedCount">种子点数量（板块数/省份数）</param>
        /// <param name="lloydIterations">Lloyd 松弛迭代次数（0=不松弛，3-5次效果好）</param>
        public void Generate(int seedCount, int lloydIterations = 3)
        {
            // 1. 最远点采样生成初始种子
            Seeds = FarthestPointSampling(seedCount);

            // 2. Lloyd 松弛迭代
            for (int iter = 0; iter < lloydIterations; iter++)
            {
                LloydRelaxation();
            }
        }

        /// <summary>
        /// 最远点采样（Farthest Point Sampling / Blue Noise）
        /// 在球面上生成均匀分布的种子点，避免聚集
        /// </summary>
        private Seed[] FarthestPointSampling(int count)
        {
            var seeds = new List<Seed>(count);
            // 第一个种子随机
            var first = RandomSpherePoint();
            seeds.Add(new Seed(first.x, first.y, first.z, 0));

            // 每个后续种子选离已有种子最远的点
            // 简化：采样 M 个候选点，选最小距离最大的
            int candidateCount = Math.Max(100, count * 20);
            for (int i = 1; i < count; i++)
            {
                float bestDist = -1f;
                (float x, float y, float z) bestPoint = (0, 0, 0);

                for (int c = 0; c < candidateCount; c++)
                {
                    var p = RandomSpherePoint();
                    float minDist = float.MaxValue;
                    foreach (var s in seeds)
                    {
                        float d = SphereDistanceSq(p.x, p.y, p.z, s.x, s.y, s.z);
                        if (d < minDist) minDist = d;
                    }
                    if (minDist > bestDist)
                    {
                        bestDist = minDist;
                        bestPoint = p;
                    }
                }
                seeds.Add(new Seed(bestPoint.x, bestPoint.y, bestPoint.z, i));
            }
            return seeds.ToArray();
        }

        /// <summary>
        /// Lloyd 松弛：将每个种子移动到其 Voronoi 单元的质心
        /// 使单元形状更规则（接近六边形）
        /// </summary>
        private void LloydRelaxation()
        {
            // 采样球面上的点，分配到最近种子，计算每个单元的质心
            int sampleCount = Seeds.Length * 500; // 每个单元500个采样点
            var sums = new (float x, float y, float z, int count)[Seeds.Length];

            for (int i = 0; i < sampleCount; i++)
            {
                var p = RandomSpherePoint();
                int nearest = FindNearestSeed(p.x, p.y, p.z);
                sums[nearest].x += p.x;
                sums[nearest].y += p.y;
                sums[nearest].z += p.z;
                sums[nearest].count++;
            }

            // 更新种子位置为单元质心（归一化到单位球面）
            for (int i = 0; i < Seeds.Length; i++)
            {
                if (sums[i].count > 0)
                {
                    float len = Mathf.Sqrt(sums[i].x * sums[i].x + sums[i].y * sums[i].y + sums[i].z * sums[i].z);
                    if (len > 0.001f)
                    {
                        Seeds[i].x = sums[i].x / len;
                        Seeds[i].y = sums[i].y / len;
                        Seeds[i].z = sums[i].z / len;
                    }
                }
            }
        }

        /// <summary>查找给定点最近的种子ID</summary>
        public int FindNearestSeed(float x, float y, float z)
        {
            int nearest = 0;
            float bestDist = float.MaxValue;
            for (int i = 0; i < Seeds.Length; i++)
            {
                float d = SphereDistanceSq(x, y, z, Seeds[i].x, Seeds[i].y, Seeds[i].z);
                if (d < bestDist)
                {
                    bestDist = d;
                    nearest = i;
                }
            }
            return nearest;
        }

        /// <summary>
        /// 对网格地图分配 Voronoi 单元 ID
        /// </summary>
        /// <param name="width">地图宽度</param>
        /// <param name="height">地图高度</param>
        /// <returns>每个网格点的种子ID数组</returns>
        public int[] AssignToGrid(int width, int height)
        {
            var result = new int[width * height];
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    var (sx, sy, sz) = SphericalNoise.GridToSphere(x, y, width, height);
                    result[y * width + x] = FindNearestSeed(sx, sy, sz);
                }
            }
            return result;
        }

        /// <summary>
        /// 检测 Voronoi 边界点（与任一邻域种子不同即为边界）
        /// </summary>
        /// <param name="grid">AssignToGrid的结果</param>
        /// <param name="width">宽度</param>
        /// <param name="height">高度</param>
        /// <param name="wrapX">是否左右环绕</param>
        /// <returns>边界点布尔数组</returns>
        public bool[] DetectBoundaries(int[] grid, int width, int height, bool wrapX = true)
        {
            var boundary = new bool[width * height];
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    int idx = y * width + x;
                    int id = grid[idx];
                    // 检查4邻域
                    if (x > 0 && grid[idx - 1] != id) boundary[idx] = true;
                    else if (x < width - 1 && grid[idx + 1] != id) boundary[idx] = true;
                    else if (y > 0 && grid[idx - width] != id) boundary[idx] = true;
                    else if (y < height - 1 && grid[idx + width] != id) boundary[idx] = true;
                    // 左右环绕
                    else if (wrapX && x == 0 && grid[y * width + width - 1] != id) boundary[idx] = true;
                    else if (wrapX && x == width - 1 && grid[y * width] != id) boundary[idx] = true;
                }
            }
            return boundary;
        }

        // ===== 工具函数 =====

        /// <summary>球面上的随机点（均匀分布）</summary>
        private (float x, float y, float z) RandomSpherePoint()
        {
            // 均匀球面采样：u=cos(theta), phi=2*pi*v
            float u = (float)_rng.NextDouble() * 2f - 1f; // -1~1
            float theta = Mathf.Acos(u);
            float phi = (float)_rng.NextDouble() * Mathf.PI * 2f;
            return (
                Mathf.Sin(theta) * Mathf.Cos(phi),
                Mathf.Cos(theta),
                Mathf.Sin(theta) * Mathf.Sin(phi)
            );
        }

        /// <summary>
        /// 球面距离平方（用3D点积近似，避免acos计算）
        /// 实际大圆弧距离 = R * acos(dot)，这里用 (1-dot) 作为距离度量（单调递增）
        /// </summary>
        private static float SphereDistanceSq(float x1, float y1, float z1, float x2, float y2, float z2)
        {
            float dot = x1 * x2 + y1 * y2 + z1 * z2;
            return 1f - dot; // 0（同点）~2（对跖点）
        }
    }
}
