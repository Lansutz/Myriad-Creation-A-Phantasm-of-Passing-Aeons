using System;
using CivilizationEvolution.Core;
using UnityEngine;

namespace CivilizationEvolution.Map
{
    /// <summary>
    /// Holdridge 生命地带分类系统（Holdridge 1967, 1987）
    /// 三个核心变量（对数刻度三角形坐标系）：
    ///   1. 年生物温度 ABT（Annual Biotemperature）：0~30°C，低于0°C的月份按0计（植物休眠），高于30°C的月份排除
    ///   2. 年降水 AP（Annual Precipitation）：mm，对数刻度 62.5~4000+
    ///   3. 潜在蒸散比 PER（Potential Evapotranspiration Ratio）= PET / P，<0.5 极湿润 / 0.5-1 湿润 / 1-2 半湿润 / 2-4 半干旱 / >4 干旱
    ///
    /// 分类结果映射到游戏的 BiomeType 枚举（55+生态区）
    /// 参考：Holdridge, L.R. (1967) Life Zone Ecology; Watson et al. (1971) Holdridge Life Zones of the World
    /// </summary>
    public static class HoldridgeBiomeClassifier
    {
        /// <summary>
        /// 计算年生物温度（ABT）
        /// 简化版：基于年均温，低于0°C按0计，高于30°C按30°C计
        /// 精确版需要月均温数据，这里用年均温近似
        /// </summary>
        public static float CalculateBiotemperature(float meanAnnualTemp)
        {
            // 生物温度：植物生长季的有效温度
            // 简化：年均温低于0°C时生物温度趋近0，高于30°C时饱和
            float bt = Mathf.Clamp(meanAnnualTemp, 0f, 30f);
            // 冬季休眠修正：寒冷地区实际生物温度低于年均温
            if (meanAnnualTemp < 10f)
                bt *= 0.6f + 0.4f * (meanAnnualTemp / 10f);
            return bt;
        }

        /// <summary>
        /// 计算潜在蒸散量（PET），mm/年
        /// 基于Thornthwaite方程简化：PET ≈ 1.6 * (10 * ABT / I)^a
        /// 简化版用经验公式：PET ≈ 50 * ABT（粗略近似，ABT=10时PET≈500mm）
        /// </summary>
        public static float CalculatePET(float biotemperature)
        {
            // Thornthwaite简化：PET = 1.6 * (10 * T / I)^a
            // 这里用更简单的线性近似，ABT=0→PET=0, ABT=30→PET≈1500mm
            return Mathf.Max(0f, biotemperature * 50f);
        }

        /// <summary>
        /// 计算潜在蒸散比 PER = PET / P
        /// PER < 0.5: 极湿润（rain forest）
        /// PER 0.5-1: 湿润（wet forest）
        /// PER 1-2: 半湿润（moist forest）
        /// PER 2-4: 半干旱（thorn steppe）
        /// PER >4: 干旱（desert）
        /// </summary>
        public static float CalculatePER(float pet, float precipitation)
        {
            if (precipitation < 1f) return 100f; // 几乎无降水→极端干旱
            return pet / precipitation;
        }

        /// <summary>
        /// Holdridge 湿度等级（基于PER）
        /// </summary>
        public enum HumidityProvince
        {
            SuperArid,      // 超干旱 PER > 16
            PerArid,        // 极干旱 PER 8-16
            Arid,           // 干旱 PER 4-8
            SemiArid,       // 半干旱 PER 2-4
            SubHumid,       // 半湿润 PER 1-2
            Humid,          // 湿润 PER 0.5-1
            PerHumid,       // 极湿润 PER 0.25-0.5
            SuperHumid      // 超湿润 PER < 0.25
        }

        /// <summary>
        /// Holdridge 温度带（基于ABT）
        /// </summary>
        public enum ThermalBelt
        {
            Nival,          // 冰雪带 ABT < 1.5°C（冰盖/冰川）
            Tundra,         // 冻原带 ABT 1.5-3°C
            Boreal,         // 北方带 ABT 3-6°C（寒温带/泰加林）
            CoolTemperate,  // 凉温带 ABT 6-12°C
            WarmTemperate,  // 暖温带 ABT 12-18°C
            Subtropical,    // 亚热带 ABT 18-24°C
            Tropical        // 热带 ABT >24°C
        }

        /// <summary>获取湿度等级</summary>
        public static HumidityProvince GetHumidityProvince(float per)
        {
            if (per > 16f) return HumidityProvince.SuperArid;
            if (per > 8f) return HumidityProvince.PerArid;
            if (per > 4f) return HumidityProvince.Arid;
            if (per > 2f) return HumidityProvince.SemiArid;
            if (per > 1f) return HumidityProvince.SubHumid;
            if (per > 0.5f) return HumidityProvince.Humid;
            if (per > 0.25f) return HumidityProvince.PerHumid;
            return HumidityProvince.SuperHumid;
        }

        /// <summary>获取温度带</summary>
        public static ThermalBelt GetThermalBelt(float abt)
        {
            if (abt < 1.5f) return ThermalBelt.Nival;
            if (abt < 3f) return ThermalBelt.Tundra;
            if (abt < 6f) return ThermalBelt.Boreal;
            if (abt < 12f) return ThermalBelt.CoolTemperate;
            if (abt < 18f) return ThermalBelt.WarmTemperate;
            if (abt < 24f) return ThermalBelt.Subtropical;
            return ThermalBelt.Tropical;
        }

        /// <summary>
        /// 核心分类：Holdridge 三变量 → BiomeType
        /// 综合温度带、湿度等级、高程、海陆、水文特征，映射到55+生态区
        /// </summary>
        /// <param name="meanAnnualTemp">年均温（°C）</param>
        /// <param name="precipitation">年降水（mm）</param>
        /// <param name="elevation01">归一化高程（0-1）</param>
        /// <param name="isLand">是否陆地</param>
        /// <param name="isCoast">是否海岸</param>
        /// <param name="isRiver">是否河流</param>
        /// <param name="slopeDegree">坡度（度）</param>
        /// <param name="latAbs">绝对纬度（0-90）</param>
        public static GameEnums.BiomeType Classify(
            float meanAnnualTemp, float precipitation, float elevation01,
            bool isLand, bool isCoast, bool isRiver, float slopeDegree, float latAbs)
        {
            if (!isLand)
            {
                // 海洋群系（简化）
                if (isCoast) return GameEnums.BiomeType.Mangrove; // 沿海红树林
                return GameEnums.BiomeType.EndorheicLake; // 占位
            }

            // 计算Holdridge三变量
            float abt = CalculateBiotemperature(meanAnnualTemp);
            float pet = CalculatePET(abt);
            float per = CalculatePER(pet, precipitation);

            var thermal = GetThermalBelt(abt);
            var humidity = GetHumidityProvince(per);

            // ===== 高海拔特化（优先于气候带分类）=====
            if (elevation01 > 0.9f)
            {
                if (meanAnnualTemp < -5f) return GameEnums.BiomeType.MountainGlacier;
                return GameEnums.BiomeType.HighMountains;
            }
            if (elevation01 > 0.75f && slopeDegree > 20f)
            {
                if (thermal <= ThermalBelt.Boreal) return GameEnums.BiomeType.AlpineMeadow;
                return GameEnums.BiomeType.FoldMountains;
            }

            // ===== 水文特化 =====
            if (isRiver && elevation01 < 0.4f && humidity >= HumidityProvince.Humid)
                return GameEnums.BiomeType.Swamp;
            if (isCoast && precipitation > 1500f && thermal >= ThermalBelt.Subtropical)
                return GameEnums.BiomeType.Mangrove;
            if (isCoast && elevation01 < 0.3f)
                return GameEnums.BiomeType.CoastalLowland;

            // ===== Holdridge 温度带 × 湿度等级 主分类 =====
            switch (thermal)
            {
                case ThermalBelt.Nival:
                    return GameEnums.BiomeType.IceSheet;

                case ThermalBelt.Tundra:
                    return humidity <= HumidityProvince.Arid ? GameEnums.BiomeType.ColdDesert : GameEnums.BiomeType.Tundra;

                case ThermalBelt.Boreal:
                    if (humidity <= HumidityProvince.SemiArid) return GameEnums.BiomeType.Tundra;
                    return GameEnums.BiomeType.BorealForest;

                case ThermalBelt.CoolTemperate:
                    if (humidity <= HumidityProvince.Arid) return GameEnums.BiomeType.ColdDesert;
                    if (humidity <= HumidityProvince.SemiArid) return GameEnums.BiomeType.TemperateGrassland;
                    if (humidity <= HumidityProvince.SubHumid)
                        return elevation01 > 0.6f ? GameEnums.BiomeType.LowHills : GameEnums.BiomeType.DeciduousForest;
                    return GameEnums.BiomeType.DeciduousForest;

                case ThermalBelt.WarmTemperate:
                    if (humidity <= HumidityProvince.Arid)
                        return isCoast ? GameEnums.BiomeType.CoastalDesert : GameEnums.BiomeType.InlandDesert;
                    if (humidity <= HumidityProvince.SemiArid)
                        return GameEnums.BiomeType.SemiAridShrubland;
                    if (humidity <= HumidityProvince.SubHumid)
                        return elevation01 > 0.55f ? GameEnums.BiomeType.LowHills : GameEnums.BiomeType.DeciduousForest;
                    // 暖温带湿润区：地中海气候（夏干冬雨）→ 常绿硬叶林，否则落叶阔叶
                    if (latAbs > 30f && latAbs < 45f && per > 0.8f)
                        return GameEnums.BiomeType.EvergreenForest; // 地中海型常绿硬叶林
                    return GameEnums.BiomeType.DeciduousForest;

                case ThermalBelt.Subtropical:
                    if (humidity <= HumidityProvince.Arid)
                        return isCoast ? GameEnums.BiomeType.CoastalDesert : GameEnums.BiomeType.HotDesert;
                    if (humidity <= HumidityProvince.SemiArid)
                        return GameEnums.BiomeType.Savanna;
                    if (humidity <= HumidityProvince.SubHumid)
                        return GameEnums.BiomeType.TropicalMonsoon; // 季雨林/季风林
                    if (humidity <= HumidityProvince.Humid)
                        return GameEnums.BiomeType.MonsoonForest; // 季风干湿林
                    return GameEnums.BiomeType.EvergreenForest; // 常绿阔叶林

                case ThermalBelt.Tropical:
                    if (humidity <= HumidityProvince.Arid)
                        return isCoast ? GameEnums.BiomeType.CoastalDesert : GameEnums.BiomeType.HotDesert;
                    if (humidity <= HumidityProvince.SemiArid)
                        return GameEnums.BiomeType.Savanna; // 稀树草原
                    if (humidity <= HumidityProvince.SubHumid)
                        return GameEnums.BiomeType.TropicalMonsoon; // 季雨林
                    return GameEnums.BiomeType.TropicalRainforest; // 热带雨林

                default:
                    return GameEnums.BiomeType.TemperateGrassland;
            }
        }

        /// <summary>
        /// 计算肥力（Holdridge体系下的农业潜力）
        /// 基于温度带×湿度等级×地形修正
        /// </summary>
        public static float CalculateFertility(
            float meanAnnualTemp, float precipitation, float elevation01,
            float slopeDegree, GameEnums.BiomeType biome)
        {
            float abt = CalculateBiotemperature(meanAnnualTemp);
            float pet = CalculatePET(abt);
            float per = CalculatePER(pet, precipitation);

            // 基础肥力：温度适中+降水适中最高
            float tempFactor = Mathf.Exp(-Mathf.Pow((abt - 15f) / 12f, 2)); // ABT=15°C最优
            float moistureFactor = Mathf.Exp(-Mathf.Pow((per - 0.75f) / 1.5f, 2)); // PER=0.75最优（湿润）
            float baseFert = tempFactor * moistureFactor * 0.8f + 0.1f;

            // 群系修正
            switch (biome)
            {
                case GameEnums.BiomeType.AlluvialPlain:
                case GameEnums.BiomeType.VolcanicAshPlain:
                case GameEnums.BiomeType.Delta:
                case GameEnums.BiomeType.GreatRiverPlain:
                    baseFert = Mathf.Max(baseFert, 0.85f); break;
                case GameEnums.BiomeType.Interfluvial:
                case GameEnums.BiomeType.EnclosedBasin:
                case GameEnums.BiomeType.DesertOasis:
                    baseFert = Mathf.Max(baseFert, 0.7f); break;
                case GameEnums.BiomeType.DeciduousForest:
                case GameEnums.BiomeType.EvergreenForest:
                case GameEnums.BiomeType.TropicalRainforest:
                    baseFert *= 0.85f; break; // 雨林土壤贫瘠（淋溶强）
                case GameEnums.BiomeType.TemperateGrassland:
                case GameEnums.BiomeType.Savanna:
                    baseFert *= 0.9f; break; // 草原土壤肥沃（黑钙土/栗钙土）
                case GameEnums.BiomeType.HotDesert:
                case GameEnums.BiomeType.ColdDesert:
                case GameEnums.BiomeType.InlandDesert:
                case GameEnums.BiomeType.IceSheet:
                case GameEnums.BiomeType.Tundra:
                case GameEnums.BiomeType.MountainGlacier:
                case GameEnums.BiomeType.HighMountains:
                    baseFert *= 0.15f; break;
            }

            // 坡度惩罚（>15°显著降低）
            baseFert *= Mathf.Clamp01(1f - Mathf.Max(0, slopeDegree - 10f) / 50f);
            // 高程惩罚（>0.6显著降低）
            baseFert *= Mathf.Clamp01(1f - Mathf.Max(0, elevation01 - 0.5f) * 1.2f);

            return Mathf.Clamp01(baseFert);
        }
    }
}
