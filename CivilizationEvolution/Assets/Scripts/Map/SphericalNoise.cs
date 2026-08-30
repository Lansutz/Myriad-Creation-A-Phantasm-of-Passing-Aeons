using System;

namespace CivilizationEvolution.Map
{
    /// <summary>
    /// 3D 球面 Simplex 噪声生成器
    /// 输入单位球面上的 3D 坐标，避免等矩形投影两极拉伸
    /// 支持：Simplex 噪声 / fBm 分形布朗运动 / 域扭曲 Domain Warping / Smootherstep
    /// </summary>
    public class SphericalNoise
    {
        // ===== 置换表（Perlin 经典 256 -entry，种子化洗牌）=====
        private readonly byte[] _perm = new byte[512];
        private readonly byte[] _permMod12 = new byte[512];

        // 3D Simplex 梯度向量：12 条立方体边的中点方向（归一化）
        private static readonly float[][] Grad3 =
        {
            new[] { 1f, 1f, 0f }, new[] { -1f, 1f, 0f }, new[] { 1f, -1f, 0f }, new[] { -1f, -1f, 0f },
            new[] { 1f, 0f, 1f }, new[] { -1f, 0f, 1f }, new[] { 1f, 0f, -1f }, new[] { -1f, 0f, -1f },
            new[] { 0f, 1f, 1f }, new[] { 0f, -1f, 1f }, new[] { 0f, 1f, -1f }, new[] { 0f, -1f, -1f }
        };

        // 3D Simplex 斜切/反斜切常数
        private const float F3 = 1f / 3f;  // 斜切：将笛卡尔坐标转为单纯形坐标
        private const float G3 = 1f / 6f;  // 反斜切：单纯形坐标转回笛卡尔

        /// <summary>使用指定种子初始化置换表</summary>
        public SphericalNoise(int seed = 1337)
        {
            // 基于种子的确定性洗牌（Fisher-Yates）
            var rng = new Random(seed);
            var p = new byte[256];
            for (int i = 0; i < 256; i++) p[i] = (byte)i;
            for (int i = 255; i > 0; i--)
            {
                int j = rng.Next(i + 1);
                (p[i], p[j]) = (p[j], p[i]);
            }
            // 翻倍避免越界取模
            for (int i = 0; i < 512; i++)
            {
                _perm[i] = p[i & 255];
                _permMod12[i] = (byte)(_perm[i] % 12);
            }
        }

        // ===== 经纬度 → 单位球面 3D 坐标 =====
        /// <summary>
        /// 经纬度转单位球面 3D 坐标
        /// </summary>
        /// <param name="lat">纬度，-90~90 度</param>
        /// <param name="lon">经度，-180~180 度</param>
        /// <returns>单位球面上的 (x,y,z)</returns>
        public static (float x, float y, float z) LatLonToSphere(float lat, float lon)
        {
            float latRad = lat * MathF.PI / 180f;
            float lonRad = lon * MathF.PI / 180f;
            float cosLat = MathF.Cos(latRad);
            return (
                cosLat * MathF.Cos(lonRad),
                MathF.Sin(latRad),
                cosLat * MathF.Sin(lonRad)
            );
        }

        /// <summary>
        /// 网格坐标（等矩形投影）转单位球面 3D 坐标
        /// </summary>
        /// <param name="x">列索引 0~width-1</param>
        /// <param name="y">行索引 0~height-1</param>
        /// <param name="width">地图宽度（应为 height*2）</param>
        /// <param name="height">地图高度</param>
        public static (float x, float y, float z) GridToSphere(int x, int y, int width, int height)
        {
            float lon = (x / (float)width) * 360f - 180f;
            float lat = 90f - (y / (float)height) * 180f;
            return LatLonToSphere(lat, lon);
        }

        // ===== 核心：3D Simplex 噪声 =====
        /// <summary>
        /// 3D Simplex 噪声，输出范围约 [-1, 1]
        /// 输入单位球面上的 3D 坐标（或任意 3D 点）
        /// </summary>
        public float Noise3D(float xin, float yin, float zin)
        {
            float n0, n1, n2, n3; // 四个顶点的噪声贡献

            // 斜切到单纯形空间
            float s = (xin + yin + zin) * F3;
            int i = (int)MathF.Floor(xin + s);
            int j = (int)MathF.Floor(yin + s);
            int k = (int)MathF.Floor(zin + s);

            // 反斜切回笛卡尔空间，计算原点距离
            float t = (i + j + k) * G3;
            float X0 = i - t;
            float Y0 = j - t;
            float Z0 = k - t;
            float x0 = xin - X0;
            float y0 = yin - Y0;
            float z0 = zin - Z0;

            // 确定当前点所在四面体的另外三个顶点（单纯形内的排序）
            int i1, j1, k1; // 第二顶点偏移
            int i2, j2, k2; // 第三顶点偏移

            if (x0 >= y0)
            {
                if (y0 >= z0) { i1 = 1; j1 = 0; k1 = 0; i2 = 1; j2 = 1; k2 = 0; } // x y z
                else if (x0 >= z0) { i1 = 1; j1 = 0; k1 = 0; i2 = 1; j2 = 0; k2 = 1; } // x z y
                else { i1 = 0; j1 = 0; k1 = 1; i2 = 1; j2 = 0; k2 = 1; } // z x y
            }
            else
            {
                if (y0 < z0) { i1 = 0; j1 = 0; k1 = 1; i2 = 0; j2 = 1; k2 = 1; } // z y x
                else if (x0 < z0) { i1 = 0; j1 = 1; k1 = 0; i2 = 0; j2 = 1; k2 = 1; } // y z x
                else { i1 = 0; j1 = 1; k1 = 0; i2 = 1; j2 = 1; k2 = 0; } // y x z
            }

            // 四个顶点的偏移（步长 G3）
            float x1 = x0 - i1 + G3;
            float y1 = y0 - j1 + G3;
            float z1 = z0 - k1 + G3;
            float x2 = x0 - i2 + 2f * G3;
            float y2 = y0 - j2 + 2f * G3;
            float z2 = z0 - k2 + 2f * G3;
            float x3 = x0 - 1f + 3f * G3;
            float y3 = y0 - 1f + 3f * G3;
            float z3 = z0 - 1f + 3f * G3;

            // 置换表索引（&255 取模）
            int ii = i & 255;
            int jj = j & 255;
            int kk = k & 255;

            // 顶点0
            float t0 = 0.6f - x0 * x0 - y0 * y0 - z0 * z0;
            if (t0 < 0) n0 = 0f;
            else
            {
                t0 *= t0;
                int gi0 = _permMod12[ii + _perm[jj + _perm[kk]]];
                n0 = t0 * t0 * (Grad3[gi0][0] * x0 + Grad3[gi0][1] * y0 + Grad3[gi0][2] * z0);
            }

            // 顶点1
            float t1 = 0.6f - x1 * x1 - y1 * y1 - z1 * z1;
            if (t1 < 0) n1 = 0f;
            else
            {
                t1 *= t1;
                int gi1 = _permMod12[ii + i1 + _perm[jj + j1 + _perm[kk + k1]]];
                n1 = t1 * t1 * (Grad3[gi1][0] * x1 + Grad3[gi1][1] * y1 + Grad3[gi1][2] * z1);
            }

            // 顶点2
            float t2 = 0.6f - x2 * x2 - y2 * y2 - z2 * z2;
            if (t2 < 0) n2 = 0f;
            else
            {
                t2 *= t2;
                int gi2 = _permMod12[ii + i2 + _perm[jj + j2 + _perm[kk + k2]]];
                n2 = t2 * t2 * (Grad3[gi2][0] * x2 + Grad3[gi2][1] * y2 + Grad3[gi2][2] * z2);
            }

            // 顶点3
            float t3 = 0.6f - x3 * x3 - y3 * y3 - z3 * z3;
            if (t3 < 0) n3 = 0f;
            else
            {
                t3 *= t3;
                int gi3 = _permMod12[ii + 1 + _perm[jj + 1 + _perm[kk + 1]]];
                n3 = t3 * t3 * (Grad3[gi3][0] * x3 + Grad3[gi3][1] * y3 + Grad3[gi3][2] * z3);
            }

            // 求和并放大到 [-1, 1]（32 是经验缩放系数）
            return 32f * (n0 + n1 + n2 + n3);
        }

        // ===== fBm 分形布朗运动（多倍频叠加）=====
        /// <summary>
        /// 分形布朗运动：多倍频 Simplex 噪声叠加
        /// </summary>
        /// <param name="x">球面 x</param>
        /// <param name="y">球面 y</param>
        /// <param name="z">球面 z</param>
        /// <param name="octaves">倍频数（建议 4~8）</param>
        /// <param name="lacunarity">频率倍增（默认 2.0）</param>
        /// <param name="gain">振幅衰减（默认 0.5）</param>
        /// <param name="frequency">基础频率</param>
        /// <returns>约 [-1, 1]（实际范围随倍频数略有扩展）</returns>
        public float Fbm(float x, float y, float z, int octaves = 6,
            float lacunarity = 2.0f, float gain = 0.5f, float frequency = 1.0f)
        {
            float amplitude = 1f;
            float sum = 0f;
            float norm = 0f;
            float fx = x * frequency;
            float fy = y * frequency;
            float fz = z * frequency;

            for (int i = 0; i < octaves; i++)
            {
                sum += amplitude * Noise3D(fx, fy, fz);
                norm += amplitude;
                fx *= lacunarity;
                fy *= lacunarity;
                fz *= lacunarity;
                amplitude *= gain;
            }
            return sum / norm; // 归一化到约 [-1, 1]
        }

        // ===== 域扭曲 Domain Warping =====
        /// <summary>
        /// 域扭曲：用噪声场偏移输入坐标，再采样主噪声
        /// 产生更复杂、更自然的地形（山脉走向、海岸线弯曲）
        /// </summary>
        /// <param name="x">球面 x</param>
        /// <param name="y">球面 y</param>
        /// <param name="z">球面 z</param>
        /// <param name="warpStrength">扭曲强度（建议 0.3~1.5）</param>
        /// <param name="warpFrequency">扭曲噪声频率（建议 0.5~2.0）</param>
        /// <param name="octaves">主噪声倍频数</param>
        /// <returns>扭曲后的噪声值</returns>
        public float DomainWarpFbm(float x, float y, float z,
            float warpStrength = 0.8f, float warpFrequency = 1.2f, int octaves = 6)
        {
            // 用三个不同种子的噪声场偏移坐标（这里用同一噪声的不同频率分量近似）
            float qx = Fbm(x * warpFrequency + 0.0f, y * warpFrequency + 5.2f, z * warpFrequency + 1.3f, 4);
            float qy = Fbm(x * warpFrequency + 5.2f, y * warpFrequency + 1.3f, z * warpFrequency + 0.0f, 4);
            float qz = Fbm(x * warpFrequency + 1.3f, y * warpFrequency + 0.0f, z * warpFrequency + 5.2f, 4);

            // 偏移后的坐标采样主噪声
            return Fbm(
                x + warpStrength * qx,
                y + warpStrength * qy,
                z + warpStrength * qz,
                octaves
            );
        }

        // ===== 工具函数 =====
        /// <summary>Smootherstep 平滑插值（比 smoothstep 更平滑，二阶导数连续）</summary>
        public static float Smootherstep(float t)
        {
            t = Math.Clamp(t, 0f, 1f);
            return t * t * t * (t * (t * 6f - 15f) + 10f);
        }

        /// <summary>将 [-1,1] 映射到 [0,1]</summary>
        public static float Normalize01(float v) => (v + 1f) * 0.5f;

        /// <summary>反锐化掩蔽：增加地形对比度（山脊更明显）</summary>
        public float RidgedFbm(float x, float y, float z, int octaves = 6,
            float lacunarity = 2.0f, float gain = 0.5f, float frequency = 1.0f)
        {
            float amplitude = 0.5f;
            float sum = 0f;
            float norm = 0f;
            float fx = x * frequency;
            float fy = y * frequency;
            float fz = z * frequency;

            for (int i = 0; i < octaves; i++)
            {
                // 山脊噪声：1 - |noise|，产生尖锐山脊线
                float n = 1f - MathF.Abs(Noise3D(fx, fy, fz));
                n *= n; // 平方让山脊更尖锐
                sum += amplitude * n;
                norm += amplitude;
                fx *= lacunarity;
                fy *= lacunarity;
                fz *= lacunarity;
                amplitude *= gain;
            }
            return sum / norm; // [0, 1]
        }
    }
}
