using System;
using CivilizationEvolution.Core;
using UnityEngine;

namespace CivilizationEvolution.Map
{
    /// <summary>
    /// 水力侵蚀模拟（Hydraulic Erosion）
    /// 粒子基算法：模拟大量雨滴粒子从高处落下，沿坡度流动，携带沉积物，
    /// 流速降低时沉积，形成河谷、峡谷、冲积扇、三角洲等地貌。
    ///
    /// 参考：Hans Theobald (2007) "Hydraulic Erosion"；Jákó et al. (2018) 粒子基侵蚀优化
    ///
    /// 参数可调节：
    ///   - 粒子数量：越多效果越细腻，性能开销越大
    ///   - 侵蚀率：粒子携带沉积物的能力
    ///   - 沉积率：粒子沉积沉积物的速度
    ///   - 蒸发率：水量减少速度
    ///   - 惯性：粒子保持方向的能力（0=完全沿坡度，1=完全惯性）
    ///   - 最小坡度：低于此坡度不侵蚀（避免平坦地区被侵蚀）
    /// </summary>
    public class HydraulicErosion
    {
        // ===== 侵蚀参数 =====
        public int ParticleCount = 60000;      // 粒子数量（百万级地图建议10万+）
        public int MaxLifetime = 64;             // 单个粒子最大生命周期（步数）
        public float ErosionRate = 0.3f;         // 侵蚀率（0-1，粒子携带沉积物能力）
        public float DepositionRate = 0.3f;      // 沉积率（0-1，粒子沉积速度）
        public float EvaporationRate = 0.02f;    // 蒸发率（每步水量减少比例）
        public float Inertia = 0.05f;             // 惯性（0=完全沿坡度，1=完全保持方向）
        public float MinSlope = 0.01f;            // 最小坡度（低于此不侵蚀）
        public float CapacityFactor = 4f;          // 携沙能力系数（坡度×水量×流速）
        public float InitialWater = 1f;            // 初始水量
        public float InitialSpeed = 1f;            // 初始速度
        public bool WrapX = true;                  // 左右环绕

        private readonly int _width;
        private readonly int _height;
        private readonly System.Random _rng;

        public HydraulicErosion(int width, int height, int seed = 42)
        {
            _width = width;
            _height = height;
            _rng = new System.Random(seed);
        }

        /// <summary>
        /// 运行水力侵蚀模拟
        /// </summary>
        /// <param name="elevation">高程数组（0-1，会被修改）</param>
        /// <param name="isLand">是否陆地（只侵蚀陆地）</param>
        public void Run(float[] elevation, bool[] isLand)
        {
            int n = _width * _height;
            // 沉积物层（每个点的沉积物量）
            var sediment = new float[n];

            Debug.Log($"[HydraulicErosion] 开始侵蚀模拟：{ParticleCount}粒子，最大寿命{MaxLifetime}步");

            // 模拟每个粒子
            for (int p = 0; p < ParticleCount; p++)
            {
                // 随机在陆地上生成粒子
                int startX, startY, startIdx;
                int attempts = 0;
                do
                {
                    startX = _rng.Next(_width);
                    startY = _rng.Next(_height / 4, _height); // 偏向上半部分（降水多在中高纬）
                    startIdx = startY * _width + startX;
                    attempts++;
                } while ((!isLand[startIdx] || elevation[startIdx] < 0.5f) && attempts < 50);

                if (attempts >= 50) continue; // 找不到合适起点，跳过

                // 粒子状态
                float posX = startX;
                float posY = startY;
                float dirX = 0f, dirY = 0f;
                float water = InitialWater;
                float speed = InitialSpeed;
                float sedimentLoad = 0f;

                for (int step = 0; step < MaxLifetime; step++)
                {
                    int nodeX = (int)posX;
                    int nodeY = (int)posY;
                    int nodeIdx = nodeY * _width + nodeX;

                    // 粒子偏移（双线性插值用）
                    float u = posX - nodeX;
                    float v = posY - nodeY;

                    // 计算当前点的高度和梯度（双线性插值）
                    float height = BilinearInterpolate(elevation, nodeX, nodeY, u, v);
                    var (gradX, gradY) = BilinearGradient(elevation, nodeX, nodeY, u, v);

                    // 更新方向：惯性+坡度
                    dirX = dirX * Inertia - gradX * (1f - Inertia);
                    dirY = dirY * Inertia - gradY * (1f - Inertia);

                    // 方向归一化（如果为0则随机）
                    float dirLen = Mathf.Sqrt(dirX * dirX + dirY * dirY);
                    if (dirLen > 0.0001f)
                    {
                        dirX /= dirLen;
                        dirY /= dirLen;
                    }
                    else
                    {
                        // 随机方向
                        float angle = (float)_rng.NextDouble() * Mathf.PI * 2f;
                        dirX = Mathf.Cos(angle);
                        dirY = Mathf.Sin(angle);
                    }

                    // 移动到新位置
                    float newX = posX + dirX;
                    float newY = posY + dirY;

                    // 边界检查（上下不环绕，左右环绕）
                    if (newY < 0 || newY >= _height - 1) break;
                    if (WrapX)
                    {
                        if (newX < 0) newX += _width;
                        if (newX >= _width) newX -= _width;
                    }
                    else if (newX < 0 || newX >= _width - 1) break;

                    int newNodeX = (int)newX;
                    int newNodeY = (int)newY;
                    int newNodeIdx = newNodeY * _width + newNodeX;

                    // 只在陆地上侵蚀
                    if (!isLand[newNodeIdx]) break;

                    // 新位置高度
                    float newHeight = BilinearInterpolate(elevation, newNodeX, newNodeY, newX - newNodeX, newY - newNodeY);
                    float heightDiff = newHeight - height;

                    // 计算携沙能力（坡度越大、水量越多、速度越快，携沙能力越强）
                    float slopeAngle = Mathf.Abs(heightDiff);
                    float capacity = Mathf.Max(slopeAngle, MinSlope) * water * speed * CapacityFactor;

                    // 侵蚀或沉积
                    if (sedimentLoad > capacity || heightDiff > 0f)
                    {
                        // 沉积：粒子携带的沉积物超过能力，或上坡，沉积
                        float depositAmount;
                        if (heightDiff > 0f)
                        {
                            // 上坡：沉积一部分填补高度差
                            depositAmount = Mathf.Min(heightDiff, sedimentLoad);
                        }
                        else
                        {
                            // 超量沉积
                            depositAmount = (sedimentLoad - capacity) * DepositionRate;
                        }
                        sedimentLoad -= depositAmount;

                        // 沉积到当前节点（双线性分布）
                        DepositAt(elevation, sediment, nodeX, nodeY, u, v, depositAmount);
                    }
                    else
                    {
                        // 侵蚀：粒子携沙能力未饱和，侵蚀地面
                        float erodeAmount = Mathf.Min((capacity - sedimentLoad) * ErosionRate, -heightDiff);
                        erodeAmount = Mathf.Min(erodeAmount, elevation[nodeIdx] * 0.5f); // 不侵蚀超过当前高度的50%

                        // 从当前节点侵蚀（双线性分布）
                        ErodeAt(elevation, sediment, nodeX, nodeY, u, v, erodeAmount);
                        sedimentLoad += erodeAmount;
                    }

                    // 更新速度：上坡减速，下坡加速
                    speed = Mathf.Sqrt(Mathf.Max(0f, speed * speed + heightDiff * 10f));
                    // 水量蒸发
                    water *= (1f - EvaporationRate);

                    if (water < 0.01f) break;

                    posX = newX;
                    posY = newY;
                }
            }

            Debug.Log($"[HydraulicErosion] 侵蚀模拟完成");
        }

        /// <summary>将侵蚀结果应用到TileData（更新高程）</summary>
        public void ApplyToTiles(TileData[] tiles, float[] elevation)
        {
            int n = Math.Min(tiles.Length, elevation.Length);
            for (int i = 0; i < n; i++)
            {
                if (tiles[i].isLand)
                {
                    tiles[i].elevation01 = Mathf.Clamp01(elevation[i]);
                }
            }
        }

        // ===== 内部工具 =====

        /// <summary>双线性插值获取高度</summary>
        private float BilinearInterpolate(float[] elevation, int x, int y, float u, float v)
        {
            int x1 = WrapX ? ((x + 1) % _width) : Mathf.Min(x + 1, _width - 1);
            int y1 = Mathf.Min(y + 1, _height - 1);
            int idx00 = y * _width + x;
            int idx10 = y * _width + x1;
            int idx01 = y1 * _width + x;
            int idx11 = y1 * _width + x1;

            float h00 = elevation[idx00];
            float h10 = elevation[idx10];
            float h01 = elevation[idx01];
            float h11 = elevation[idx11];

            return h00 * (1 - u) * (1 - v) + h10 * u * (1 - v) + h01 * (1 - u) * v + h11 * u * v;
        }

        /// <summary>双线性插值获取梯度</summary>
        private (float gradX, float gradY) BilinearGradient(float[] elevation, int x, int y, float u, float v)
        {
            int x1 = WrapX ? ((x + 1) % _width) : Mathf.Min(x + 1, _width - 1);
            int y1 = Mathf.Min(y + 1, _height - 1);
            int idx00 = y * _width + x;
            int idx10 = y * _width + x1;
            int idx01 = y1 * _width + x;
            int idx11 = y1 * _width + x1;

            float h00 = elevation[idx00];
            float h10 = elevation[idx10];
            float h01 = elevation[idx01];
            float h11 = elevation[idx11];

            // x方向梯度
            float gradX = (h10 - h00) * (1 - v) + (h11 - h01) * v;
            // y方向梯度
            float gradY = (h01 - h00) * (1 - u) + (h11 - h10) * u;

            return (gradX, gradY);
        }

        /// <summary>在节点双线性分布沉积</summary>
        private void DepositAt(float[] elevation, float[] sediment, int x, int y, float u, float v, float amount)
        {
            int x1 = WrapX ? ((x + 1) % _width) : Mathf.Min(x + 1, _width - 1);
            int y1 = Mathf.Min(y + 1, _height - 1);
            int idx00 = y * _width + x;
            int idx10 = y * _width + x1;
            int idx01 = y1 * _width + x;
            int idx11 = y1 * _width + x1;

            elevation[idx00] += amount * (1 - u) * (1 - v);
            elevation[idx10] += amount * u * (1 - v);
            elevation[idx01] += amount * (1 - u) * v;
            elevation[idx11] += amount * u * v;
        }

        /// <summary>在节点双线性分布侵蚀</summary>
        private void ErodeAt(float[] elevation, float[] sediment, int x, int y, float u, float v, float amount)
        {
            int x1 = WrapX ? ((x + 1) % _width) : Mathf.Min(x + 1, _width - 1);
            int y1 = Mathf.Min(y + 1, _height - 1);
            int idx00 = y * _width + x;
            int idx10 = y * _width + x1;
            int idx01 = y1 * _width + x;
            int idx11 = y1 * _width + x1;

            elevation[idx00] -= amount * (1 - u) * (1 - v);
            elevation[idx10] -= amount * u * (1 - v);
            elevation[idx01] -= amount * (1 - u) * v;
            elevation[idx11] -= amount * u * v;
        }
    }
}
