using System.Collections.Generic;
using UnityEngine;
using CivilizationEvolution.Core;

namespace CivilizationEvolution.Race
{
    /// <summary>
    /// 种族数据
    /// 社会层面差异全部由【变革性】单一数值管控，不设其他社会类细分参数
    /// </summary>
    [System.Serializable]
    public class RaceData
    {
        public int raceId;
        public string raceName;
        public string description;

        [Header("生理参数")]
        [Range(0.5f, 2.5f)] public float baseLifespan = 1.0f;
        [Range(0.5f, 2.0f)] public float growthRate = 1.0f;
        [Range(0.5f, 2.0f)] public float reproductionRate = 1.0f;
        [Range(0.5f, 2.0f)] public float physicalStrength = 1.0f;
        [Range(0.5f, 2.0f)] public float diseaseResistance = 1.0f;
        [Range(0.5f, 2.0f)] public float environmentalTolerance = 1.0f;

        [Header("感官认知")]
        [Range(0.5f, 2.0f)] public float visualAcuity = 1.0f;
        [Range(0.5f, 2.0f)] public float auditoryRange = 1.0f;
        [Range(0.5f, 2.0f)] public float olfactorySensitivity = 1.0f;
        [Range(0.5f, 2.0f)] public float cognitiveCapacity = 1.0f;

        /// <summary>
        /// 变革性 0-100（唯一社会维度）
        /// 所有社会层面种族差异由此单一数值管控：
        /// - 社会结构倾向（高→松散平等，低→森严等级）
        /// - 阶层容忍度（高→阶层流动大，低→阶层固化）
        /// - 暴力倾向基线（高→冲突频繁但易和解，低→隐忍但爆发剧烈）
        /// - 群居度（高→个体主义，低→集体主义）
        /// - 文明适配性（高→适应新环境快，低→坚守传统）
        /// - 社会发展速率（高→文化成熟快，低→稳定但缓慢）
        /// - 文化演化偏向（高→创新分支多，低→正统传承强）
        /// </summary>
        [Header("变革性（唯一社会维度）")]
        [Range(0f, 100f)] public float transformativity = 50f;

        // ===== 变革性派生计算 =====
        public float CultureMaturityRate => 0.5f + transformativity / 100f;
        public float SeparationResistanceMod => 1f - transformativity / 200f;
        public float FusionResistanceMod => 1f - transformativity / 150f;
        public float ClassRelationBaseline => 30f + transformativity * 0.4f;
        public float InnovationGainMultiplier => 0.5f + transformativity / 100f;
        public float RebellionThreshold => 25f + transformativity * 0.3f;
        public float SocialMobility => transformativity / 100f;
        public float ViolenceBaseline => 30f + Mathf.Abs(transformativity - 50f) * 0.4f;

        public string GetTransformativityTier()
        {
            if (transformativity >= 75f) return "高变革性：个体主义、创新驱动、阶层流动、文化多元";
            if (transformativity >= 40f) return "中变革性：平衡型，传统与创新并存";
            return "低变革性：集体主义、传统坚守、阶层固化、文化正统";
        }

        [Header("经济偏好")]
        public Dictionary<GameEnums.GoodsCategory, float> productionModifiers = new Dictionary<GameEnums.GoodsCategory, float>();
        public Dictionary<GameEnums.GoodsCategory, float> consumptionModifiers = new Dictionary<GameEnums.GoodsCategory, float>();

        [Header("军事偏好")]
        [Range(0.5f, 2.0f)] public float infantryBonus = 1.0f;
        [Range(0.5f, 2.0f)] public float cavalryBonus = 1.0f;
        [Range(0.5f, 2.0f)] public float navyBonus = 1.0f;
        [Range(0.5f, 2.0f)] public float moraleBase = 1.0f;

        [Header("环境适配")]
        public List<GameEnums.BiomeType> preferredBiomes = new List<GameEnums.BiomeType>();
        [Range(0f, 1f)] public float coldTolerance = 0.5f;
        [Range(0f, 1f)] public float heatTolerance = 0.5f;
        [Range(0f, 1f)] public float aridityTolerance = 0.5f;
        [Range(0f, 1f)] public float humidityTolerance = 0.5f;
        [Range(0f, 1f)] public float altitudeTolerance = 0.5f;

        /// <summary>计算在特定地块的环境适配度 0~1</summary>
        public float CalculateEnvironmentFitness(TileData tile)
        {
            float tempScore = tile.annualTemp < 10f
                ? Mathf.Lerp(0.3f, 1f, coldTolerance)
                : Mathf.Lerp(0.3f, 1f, heatTolerance);

            float humidScore = tile.airHumidityPct < 40f
                ? Mathf.Lerp(0.3f, 1f, aridityTolerance)
                : Mathf.Lerp(0.3f, 1f, humidityTolerance);

            float altScore = tile.elevation01 > 0.4f
                ? Mathf.Lerp(0.3f, 1f, altitudeTolerance)
                : 1f;

            float biomeScore = preferredBiomes.Contains(tile.biome) ? 1.2f : 1.0f;

            return Mathf.Clamp(tempScore * 0.35f + humidScore * 0.35f + altScore * 0.3f, 0.1f, 1f) * biomeScore;
        }

        /// <summary>计算人口自然增长率</summary>
        public float CalculatePopulationGrowthRate(TileData tile, float satisfaction)
        {
            float envFitness = CalculateEnvironmentFitness(tile);
            float baseRate = 0.002f * reproductionRate; // 基础日增长率0.2%
            float satisfactionMod = 0.5f + satisfaction / 100f;
            float envMod = 0.3f + envFitness * 0.7f;
            return baseRate * satisfactionMod * envMod;
        }

        /// <summary>计算疾病感染概率修正</summary>
        public float GetDiseaseInfectionMod()
        {
            return 1f / diseaseResistance;
        }
    }
}
