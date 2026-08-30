using System;
using System.Linq;
using CivilizationEvolution.Core;
using UnityEngine;

namespace CivilizationEvolution.Map
{
    /// <summary>
    /// 洋流模拟（Ocean Current Simulation）
    /// 简化版海洋环流模型，包含：
    ///   1. 风生环流（Wind-driven circulation）：表层洋流由盛行风驱动
    ///   2. 科里奥利效应（Coriolis effect）：北半球右偏（顺时针环流），南半球左偏（逆时针环流）
    ///   3. 大陆阻挡与边界反射：大陆形状影响洋流路径（如美洲阻挡形成墨西哥湾流）
    ///   4. 暖流/寒流分类：从低纬流向高纬为暖流，反之为寒流
    ///   5. 沿海温度调节：暖流增温增湿，寒流降温减湿
    ///   6. 温盐环流（简化）：深层洋流由温度盐度差异驱动（简化为回流）
    ///
    /// 参考：Tomczak & Godfrey (2003) "Regional Oceanography: An Introduction"；
    ///       Pedlosky (1998) "Ocean Circulation Theory"
    /// </summary>
    public class OceanCurrentSimulator
    {
        // ===== 洋流参数 =====
        public float WindCoupling = 0.3f;          // 风-海耦合系数（风速→洋流速度比例）
        public float CoriolisStrength = 1.0f;       // 科里奥利效应强度
        public float BoundaryReflection = 0.8f;      // 边界反射系数（大陆阻挡后的反射强度）
        public float Diffusion = 0.1f;                // 洋流扩散系数
        public int Iterations = 20;                   // 迭代次数（洋流稳定化）
        public float WarmCurrentTempBoost = 3f;       // 暖流对沿海温度的提升（°C）
        public float ColdCurrentTempReduction = 2f;   // 寒流对沿海温度的降低（°C）
        public float WarmCurrentPrecipBoost = 200f;   // 暖流对沿海降水的提升（mm）
        public float ColdCurrentPrecipReduction = 100f; // 寒流对沿海降水的降低（mm）

        private readonly int _width;
        private readonly int _height;

        // 输出场
        public float[] CurrentU { get; private set; }    // 洋流东西分量（m/s，正=东）
        public float[] CurrentV { get; private set; }    // 洋流南北分量（m/s，正=北）
        public float[] CurrentSpeed { get; private set; } // 洋流速度（m/s）
        public bool[] IsWarmCurrent { get; private set; }  // 是否暖流
        public bool[] IsColdCurrent { get; private set; }  // 是否寒流

        public OceanCurrentSimulator(int width, int height)
        {
            _width = width;
            _height = height;
            int n = width * height;
            CurrentU = new float[n];
            CurrentV = new float[n];
            CurrentSpeed = new float[n];
            IsWarmCurrent = new bool[n];
            IsColdCurrent = new bool[n];
        }

        /// <summary>
        /// 运行洋流模拟
        /// </summary>
        /// <param name="isLand">是否陆地</param>
        /// <param name="windU">风的东西分量</param>
        /// <param name="windV">风的南北分量</param>
        /// <param name="temperature">温度（°C）</param>
        public void Run(bool[] isLand, float[] windU, float[] windV, float[] temperature)
        {
            int n = _width * _height;
            Debug.Log($"[OceanCurrentSimulator] 洋流模拟开始：{_width}x{_height}，{Iterations}次迭代");

            // 第1步：初始化洋流场（风生环流）
            for (int i = 0; i < n; i++)
            {
                if (isLand[i]) continue;
                int y = i / _width;
                float lat = 90f - (y / (float)_height) * 180f;

                // 风生洋流：表层洋流速度约为风速的2-3%
                CurrentU[i] = windU[i] * WindCoupling;
                CurrentV[i] = windV[i] * WindCoupling;

                // 科里奥利偏转：北半球右偏（顺时针），南半球左偏（逆时针）
                float f = Mathf.Sin(lat * Mathf.Deg2Rad) * CoriolisStrength;
                float rotatedU = CurrentU[i] - f * CurrentV[i] * 0.5f;
                float rotatedV = CurrentV[i] + f * CurrentU[i] * 0.5f;
                CurrentU[i] = rotatedU;
                CurrentV[i] = rotatedV;
            }

            // 第2步：迭代稳定化（大陆阻挡+边界反射+扩散）
            for (int iter = 0; iter < Iterations; iter++)
            {
                var newU = new float[n];
                var newV = new float[n];
                Array.Copy(CurrentU, newU, n);
                Array.Copy(CurrentV, newV, n);

                for (int y = 1; y < _height - 1; y++)
                {
                    for (int x = 0; x < _width; x++)
                    {
                        int i = y * _width + x;
                        if (isLand[i]) continue;

                        // 左右环绕
                        int xL = (x - 1 + _width) % _width;
                        int xR = (x + 1) % _width;
                        int iL = y * _width + xL;
                        int iR = y * _width + xR;
                        int iU = (y - 1) * _width + x;
                        int iD = (y + 1) * _width + x;

                        // 大陆阻挡：如果洋流方向指向陆地，反射
                        float reflectU = 0f, reflectV = 0f;
                        if (CurrentU[i] > 0 && isLand[iR]) reflectU = -CurrentU[i] * BoundaryReflection;
                        if (CurrentU[i] < 0 && isLand[iL]) reflectU = -CurrentU[i] * BoundaryReflection;
                        if (CurrentV[i] > 0 && isLand[iD]) reflectV = -CurrentV[i] * BoundaryReflection;
                        if (CurrentV[i] < 0 && isLand[iU]) reflectV = -CurrentV[i] * BoundaryReflection;

                        // 西边界强化（西边界流，如墨西哥湾流、黑潮）
                        if (CurrentU[i] < 0 && !isLand[iL] && isLand[(y * _width + (xL - 2 + _width) % _width)])
                        {
                            // 西边界流加速
                            newU[i] *= 1.5f;
                            newV[i] *= 1.5f;
                        }

                        // 扩散（拉普拉斯算子）
                        float lapU = (CurrentU[iL] + CurrentU[iR] + CurrentU[iU] + CurrentU[iD] - 4f * CurrentU[i]) * Diffusion;
                        float lapV = (CurrentV[iL] + CurrentV[iR] + CurrentV[iU] + CurrentV[iD] - 4f * CurrentV[i]) * Diffusion;

                        newU[i] = CurrentU[i] + reflectU + lapU;
                        newV[i] = CurrentV[i] + reflectV + lapV;
                    }
                }

                CurrentU = newU;
                CurrentV = newV;
            }

            // 第3步：计算洋流速度和暖流/寒流分类
            for (int i = 0; i < n; i++)
            {
                if (isLand[i]) continue;
                int y = i / _width;
                float lat = 90f - (y / (float)_height) * 180f;

                CurrentSpeed[i] = Mathf.Sqrt(CurrentU[i] * CurrentU[i] + CurrentV[i] * CurrentV[i]);

                // 暖流/寒流判断：向极地方向流动为暖流，向赤道方向为寒流
                bool movingPoleward = (lat > 0f && CurrentV[i] > 0f) || (lat < 0f && CurrentV[i] < 0f);
                bool movingEquatorward = (lat > 0f && CurrentV[i] < 0f) || (lat < 0f && CurrentV[i] > 0f);

                IsWarmCurrent[i] = movingPoleward && CurrentSpeed[i] > 0.1f && Mathf.Abs(lat) > 15f;
                IsColdCurrent[i] = movingEquatorward && CurrentSpeed[i] > 0.1f && Mathf.Abs(lat) > 15f;
            }

            int warmCount = 0, coldCount = 0;
            for (int i = 0; i < n; i++) { if (IsWarmCurrent[i]) warmCount++; if (IsColdCurrent[i]) coldCount++; }
            Debug.Log($"[OceanCurrentSimulator] 洋流模拟完成：暖流{warmCount}地块，寒流{coldCount}地块，最大速度{CurrentSpeed.Max():F2}m/s");
        }

        /// <summary>
        /// 将洋流影响应用到沿海陆地（温度调节+降水调节）
        /// </summary>
        public void ApplyCoastalEffects(TileData[] tiles, bool[] isLand)
        {
            int n = Math.Min(tiles.Length, CurrentU.Length);

            for (int i = 0; i < n; i++)
            {
                if (!isLand[i] || !tiles[i].isCoast) continue;

                int x = i % _width;
                int y = i / _width;

                // 检查沿海相邻海洋是否有暖流/寒流
                bool adjacentWarm = false;
                bool adjacentCold = false;
                float maxWarmSpeed = 0f;
                float maxColdSpeed = 0f;

                for (int dx = -1; dx <= 1; dx++)
                {
                    for (int dy = -1; dy <= 1; dy++)
                    {
                        if (dx == 0 && dy == 0) continue;
                        int nx = (x + dx + _width) % _width;
                        int ny = Mathf.Clamp(y + dy, 0, _height - 1);
                        int ni = ny * _width + nx;
                        if (ni >= n) continue;

                        if (IsWarmCurrent[ni])
                        {
                            adjacentWarm = true;
                            maxWarmSpeed = Mathf.Max(maxWarmSpeed, CurrentSpeed[ni]);
                        }
                        if (IsColdCurrent[ni])
                        {
                            adjacentCold = true;
                            maxColdSpeed = Mathf.Max(maxColdSpeed, CurrentSpeed[ni]);
                        }
                    }
                }

                // 暖流：增温增湿
                if (adjacentWarm)
                {
                    float strength = Mathf.Clamp01(maxWarmSpeed / 1.0f);
                    tiles[i].annualTemp += WarmCurrentTempBoost * strength;
                    tiles[i].annualPrecipMm += WarmCurrentPrecipBoost * strength;
                }

                // 寒流：降温减湿
                if (adjacentCold)
                {
                    float strength = Mathf.Clamp01(maxColdSpeed / 1.0f);
                    tiles[i].annualTemp -= ColdCurrentTempReduction * strength;
                    tiles[i].annualPrecipMm = Mathf.Max(0f, tiles[i].annualPrecipMm - ColdCurrentPrecipReduction * strength);
                }
            }
        }
    }
}
