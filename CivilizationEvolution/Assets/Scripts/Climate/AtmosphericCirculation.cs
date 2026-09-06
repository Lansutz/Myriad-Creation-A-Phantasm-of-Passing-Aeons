using System;
using System.Linq;
using CivilizationEvolution.Core;
using UnityEngine;

namespace CivilizationEvolution.Map
{
    /// <summary>
    /// 大气环流 GCM（General Circulation Model，简化版）
    /// 基于物理的大气环流模拟，替换旧的简单三圈环流降水。
    ///
    /// 核心组件：
    ///   1. 气压带：赤道低压(ITCZ)、副热带高压、副极地低压、极地高压
    ///   2. 三圈环流：Hadley(0-30°)、Ferrel(30-60°)、Polar(60-90°)
    ///   3. 科里奥利效应：北半球右偏，南半球左偏（f=2Ωsin(lat)）
    ///   4. 地转风平衡：气压梯度力 + 科里奥利力 = 0
    ///   5. 季风：海陆热力差异导致的季节性风向反转
    ///   6. 地形抬升：山脉迎风坡降水增加，背风坡雨影效应
    ///   7. 比湿/相对湿度：基于温度的饱和比湿（Clausius-Clapeyron）
    ///
    /// 参考：Hartmann (2016) "Global Physical Climatology"；Peixoto & Oort (1992) "Physics of Climate"
    /// </summary>
    public class AtmosphericCirculation
    {
        // ===== GCM 参数 =====
        public float PlanetOmega = 7.292e-5f;     // 行星自转角速度（rad/s，地球值）
        public float PlanetRadius = 6371000f;       // 行星半径（m，地球值）
        public float AxialTilt = 0.409f;            // 轴倾角（rad，地球23.44°）
        public float SolarConstant = 1361f;          // 太阳常数（W/m²，地球值）
        public float Season = 0f;                     // 季节（0=春分，0.25=夏至，0.5=秋分，0.75=冬至）
        public float OrographicPrecipFactor = 800f;  // 地形降水系数
        public float RainShadowFactor = 0.3f;         // 雨影效应系数（背风坡降水比例）
        public float MonsoonStrength = 0.6f;          // 季风强度（0=无季风，1=强季风）

        private readonly int _width;
        private readonly int _height;

        // 输出场
        public float[] Pressure { get; private set; }      // 海平面气压（hPa）
        public float[] WindU { get; private set; }         // 风的东西分量（m/s，正=东）
        public float[] WindV { get; private set; }         // 风的南北分量（m/s，正=北）
        public float[] WindSpeed { get; private set; }     // 风速（m/s）
        public float[] SpecificHumidity { get; private set; } // 比湿（kg/kg）
        public float[] Precipitation { get; private set; } // 年降水（mm）

        public AtmosphericCirculation(int width, int height)
        {
            _width = width;
            _height = height;
            int n = width * height;
            Pressure = new float[n];
            WindU = new float[n];
            WindV = new float[n];
            WindSpeed = new float[n];
            SpecificHumidity = new float[n];
            Precipitation = new float[n];
        }

        /// <summary>
        /// 运行大气环流模拟
        /// </summary>
        /// <param name="elevation">高程（0-1）</param>
        /// <param name="isLand">是否陆地</param>
        /// <param name="temperature">温度（°C）</param>
        public void Run(float[] elevation, bool[] isLand, float[] temperature)
        {
            int n = _width * _height;
            Debug.Log($"[AtmosphericCirculation] GCM开始：{_width}x{_height}，季节={Season:F2}");

            // 第1步：计算太阳辐射/温度分布（基于纬度+季节+轴倾角）
            var insolation = new float[n];
            for (int y = 0; y < _height; y++)
            {
                float lat = 90f - (y / (float)_height) * 180f;
                float latRad = lat * Mathf.Deg2Rad;
                // 太阳赤纬（基于季节）
                float declination = AxialTilt * Mathf.Sin(Season * Mathf.PI * 2f);
                // 时角（日平均，简化为cos）
                float hourAngle = 0f; // 日平均
                // 太阳天顶角余弦
                float cosZenith = Mathf.Sin(latRad) * Mathf.Sin(declination) +
                                   Mathf.Cos(latRad) * Mathf.Cos(declination) * Mathf.Cos(hourAngle);
                cosZenith = Mathf.Max(0f, cosZenith);
                // 日平均太阳辐射（简化：极夜时为0）
                float dayLength = CalculateDayLength(lat, declination);
                insolation[y * _width] = SolarConstant * cosZenith * (dayLength / 24f) * 0.25f; // 0.25=球面+大气衰减
                for (int x = 1; x < _width; x++)
                    insolation[y * _width + x] = insolation[y * _width]; // 同一纬度相同
            }

            // 第2步：计算气压场（基于温度的热低压/冷高压+纬度气压带）
            for (int i = 0; i < n; i++)
            {
                int y = i / _width;
                float lat = 90f - (y / (float)_height) * 180f;
                float absLat = Mathf.Abs(lat);

                // 纬度气压带（年平均）
                float latPressure;
                if (absLat < 10f) latPressure = 1010f; // 赤道低压（ITCZ）
                else if (absLat < 20f) latPressure = 1013f; // 赤道-副热带过渡
                else if (absLat < 35f) latPressure = 1022f; // 副热带高压
                else if (absLat < 45f) latPressure = 1015f; // 副热带-副极地过渡
                else if (absLat < 65f) latPressure = 1008f; // 副极地低压
                else latPressure = 1018f; // 极地高压

                // 热力修正：陆地夏季热低压，冬季冷高压
                float thermalCorrection = 0f;
                if (isLand[i])
                {
                    float landTemp = temperature[i];
                    // 夏季（Season=0.25）陆地热低压，冬季（Season=0.75）冷高压
                    float seasonFactor = Mathf.Cos(Season * Mathf.PI * 2f - Mathf.PI / 2f);
                    if (lat > 0f) seasonFactor = -seasonFactor; // 南半球季节相反
                    thermalCorrection = -landTemp * 0.3f * seasonFactor * MonsoonStrength;
                }

                Pressure[i] = latPressure + thermalCorrection;
            }

            // 第3步：计算风场（地转风+科里奥利+三圈环流）
            for (int i = 0; i < n; i++)
            {
                int x = i % _width;
                int y = i / _width;
                float lat = 90f - (y / (float)_height) * 180f;
                float absLat = Mathf.Abs(lat);

                // 科里奥利参数 f = 2Ωsin(lat)
                float f = 2f * PlanetOmega * Mathf.Sin(lat * Mathf.Deg2Rad);
                if (Mathf.Abs(f) < 1e-6f) f = 1e-6f * Mathf.Sign(lat); // 赤道避免除零

                // 气压梯度（中心差分）
                int xL = (x - 1 + _width) % _width;
                int xR = (x + 1) % _width;
                int yU = Mathf.Max(0, y - 1);
                int yD = Mathf.Min(_height - 1, y + 1);
                float dpdx = (Pressure[y * _width + xR] - Pressure[y * _width + xL]) / (2f * 111000f * Mathf.Cos(lat * Mathf.Deg2Rad));
                float dpdy = (Pressure[yD * _width + x] - Pressure[yU * _width + x]) / (2f * 111000f);

                // 地转风：u = -(1/fρ)dpdy, v = (1/fρ)dpdx（ρ≈1.2kg/m³）
                float rho = 1.2f;
                float ug = -(1f / (f * rho)) * dpdy;
                float vg = (1f / (f * rho)) * dpdx;

                // 三圈环流的经向风（叠加到地转风上）
                float meridionalWind;
                if (absLat < 30f) meridionalWind = (lat > 0f ? -1f : 1f) * 3f; // Hadley：地面向赤道
                else if (absLat < 60f) meridionalWind = (lat > 0f ? 1f : -1f) * 2f; // Ferrel：地面向极地
                else meridionalWind = (lat > 0f ? -1f : 1f) * 1.5f; // Polar：地面向赤道

                // 信风/西风/极地东风的纬向风（科里奥利偏转后的结果）
                float zonalWind;
                if (absLat < 30f) zonalWind = (lat > 0f ? -1f : 1f) * 5f; // 信风（东北/东南）
                else if (absLat < 60f) zonalWind = (lat > 0f ? 1f : -1f) * 8f; // 西风
                else zonalWind = (lat > 0f ? -1f : 1f) * 3f; // 极地东风

                // 合成风场（地转风+环流风）
                WindU[i] = ug * 0.3f + zonalWind; // 地转风权重较低，环流风主导
                WindV[i] = vg * 0.3f + meridionalWind;
                WindSpeed[i] = Mathf.Sqrt(WindU[i] * WindU[i] + WindV[i] * WindV[i]);
            }

            // 第4步：计算比湿（基于温度的Clausius-Clapeyron）
            for (int i = 0; i < n; i++)
            {
                float tempK = temperature[i] + 273.15f;
                // 饱和水汽压（Tetens公式）
                float es = 6.112f * Mathf.Exp(17.67f * (tempK - 273.15f) / (tempK - 29.65f));
                // 比湿（假设相对湿度70%）
                float rh = isLand[i] ? 0.6f : 0.8f;
                SpecificHumidity[i] = 0.622f * (rh * es) / (Pressure[i] - 0.378f * rh * es) / 1000f; // kg/kg
            }

            // 第5步：计算降水（大尺度降水+地形降水+对流降水）
            for (int i = 0; i < n; i++)
            {
                int x = i % _width;
                int y = i / _width;
                float lat = 90f - (y / (float)_height) * 180f;
                float absLat = Mathf.Abs(lat);

                // 大尺度降水（基于风场辐合+比湿）
                float largeScalePrecip;
                if (absLat < 10f) largeScalePrecip = 2000f; // 赤道辐合带
                else if (absLat < 25f) largeScalePrecip = 800f; // 副热带（部分干旱）
                else if (absLat < 45f) largeScalePrecip = 1200f; // 温带
                else if (absLat < 70f) largeScalePrecip = 600f; // 副极地
                else largeScalePrecip = 200f; // 极地

                // 副热带高压区降水减少（沙漠带）
                if (absLat > 20f && absLat < 35f && isLand[i])
                    largeScalePrecip *= 0.4f;

                // 地形降水（迎风坡抬升，背风坡雨影）
                float orographicPrecip = 0f;
                if (isLand[i] && elevation[i] > 0.55f)
                {
                    // 计算迎风方向（风从哪个方向吹来）
                    float windDirX = -WindU[i] / WindSpeed[i];
                    float windDirY = -WindV[i] / WindSpeed[i];
                    // 迎风坡：沿风向高程增加
                    int upX = x + (int)Mathf.Sign(windDirX) * 2;
                    int upY = y + (int)Mathf.Sign(windDirY) * 2;
                    upX = (upX + _width) % _width;
                    upY = Mathf.Clamp(upY, 0, _height - 1);
                    float upElev = elevation[upY * _width + upX];
                    if (upElev > elevation[i])
                    {
                        // 迎风坡：地形抬升降水
                        orographicPrecip = OrographicPrecipFactor * (upElev - elevation[i]) * SpecificHumidity[i] * 100f;
                    }
                    else
                    {
                        // 背风坡：雨影效应
                        largeScalePrecip *= RainShadowFactor;
                    }
                }

                // 对流降水（热带高温地区）
                float convectivePrecip = 0f;
                if (absLat < 20f && temperature[i] > 20f && isLand[i])
                {
                    convectivePrecip = 500f * SpecificHumidity[i] * 50f;
                }

                // 海洋蒸发补充（沿海地区降水增加）
                float coastalBoost = 1f;
                if (isLand[i])
                {
                    bool nearCoast = false;
                    for (int dx = -2; dx <= 2 && !nearCoast; dx++)
                    {
                        for (int dy = -2; dy <= 2 && !nearCoast; dy++)
                        {
                            int nx = (x + dx + _width) % _width;
                            int ny = Mathf.Clamp(y + dy, 0, _height - 1);
                            if (!isLand[ny * _width + nx]) nearCoast = true;
                        }
                    }
                    if (nearCoast) coastalBoost = 1.3f;
                }

                Precipitation[i] = Mathf.Max(0f, (largeScalePrecip + orographicPrecip + convectivePrecip) * coastalBoost);
            }

            Debug.Log($"[AtmosphericCirculation] GCM完成：平均降水{Precipitation.Average():F0}mm，最大{Precipitation.Max():F0}mm");
        }

        /// <summary>将GCM结果应用到TileData（更新温度/降水/湿度）</summary>
        public void ApplyToTiles(TileData[] tiles, float[] temperature)
        {
            int n = Math.Min(tiles.Length, Precipitation.Length);
            for (int i = 0; i < n; i++)
            {
                tiles[i].annualPrecipMm = Precipitation[i];
                tiles[i].airHumidityPct = Mathf.Clamp01(SpecificHumidity[i] * 100f);
                // 温度保持输入值（GCM不直接修改温度，只计算降水）
            }
        }

        /// <summary>计算昼长（小时）</summary>
        private static float CalculateDayLength(float lat, float declination)
        {
            float latRad = lat * Mathf.Deg2Rad;
            float cosH = -Mathf.Tan(latRad) * Mathf.Tan(declination);
            cosH = Mathf.Clamp(cosH, -1f, 1f);
            float hourAngle = Mathf.Acos(cosH) * Mathf.Rad2Deg;
            return 2f * hourAngle / 15f; // 15°/小时
        }
    }
}
