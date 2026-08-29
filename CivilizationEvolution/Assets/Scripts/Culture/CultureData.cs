using System.Collections.Generic;
using UnityEngine;
using CivilizationEvolution.Core;

namespace CivilizationEvolution.Culture
{
    /// <summary>文化数据</summary>
    [System.Serializable]
    public class CultureData
    {
        public int cultureId;
        public string cultureName;
        /// <summary>地图文化着色（十六进制，如 "#4f8fbf"），由文化包 JSON 提供</summary>
        public string cultureColorHex = "#ffffff";
        public GameEnums.CultureStage stage;

        // 7维文化基因
        public int livelihoodType;
        public int mobilityType;
        public int burialType;
        public float[] worshipVector;
        public int materialStyle;
        public int symbolicFocus;
        public int environmentAdapt;

        public float maturity = 0f;
        public float spreadPower = 1f;
        public int parentCultureId = -1;
        public List<int> childCultureIds = new List<int>();
    }

    /// <summary>
    /// 文化相似度计算
    /// 7维度加权：生计0.25 / 移动0.15 / 葬俗0.15 / 崇拜向量余弦0.20 / 物质风格0.10 / 象征焦点0.10 / 环境适配0.05
    /// </summary>
    public static class CultureSimilarity
    {
        public const float W_LIVELIHOOD = 0.25f;
        public const float W_MOBILITY = 0.15f;
        public const float W_BURIAL = 0.15f;
        public const float W_WORSHIP = 0.20f;
        public const float W_MATERIAL = 0.10f;
        public const float W_SYMBOLIC = 0.10f;
        public const float W_ENVIRONMENT = 0.05f;

        /// <summary>计算两个文化的相似度 0~1</summary>
        public static float CalculateSim(CultureData a, CultureData b)
        {
            float sim = 0f;
            sim += W_LIVELIHOOD * CategoricalSim(a.livelihoodType, b.livelihoodType);
            sim += W_MOBILITY * CategoricalSim(a.mobilityType, b.mobilityType);
            sim += W_BURIAL * CategoricalSim(a.burialType, b.burialType);
            sim += W_WORSHIP * CosineSimilarity(a.worshipVector, b.worshipVector);
            sim += W_MATERIAL * CategoricalSim(a.materialStyle, b.materialStyle);
            sim += W_SYMBOLIC * CategoricalSim(a.symbolicFocus, b.symbolicFocus);
            sim += W_ENVIRONMENT * CategoricalSim(a.environmentAdapt, b.environmentAdapt);
            return Mathf.Clamp01(sim);
        }

        private static float CategoricalSim(int a, int b)
        {
            if (a == b) return 1f;
            if (Mathf.Abs(a - b) == 1) return 0.5f;
            return 0f;
        }

        private static float CosineSimilarity(float[] a, float[] b)
        {
            if (a == null || b == null || a.Length != b.Length) return 0.5f;
            float dot = 0f, normA = 0f, normB = 0f;
            for (int i = 0; i < a.Length; i++)
            {
                dot += a[i] * b[i];
                normA += a[i] * a[i];
                normB += b[i] * b[i];
            }
            float denom = Mathf.Sqrt(normA) * Mathf.Sqrt(normB);
            return denom > 0 ? dot / denom : 0.5f;
        }

        /// <summary>计算文化分离阻力（含变革性修正）</summary>
        public static float CalculateSeparationResistance(CultureData culture, float transformativity)
        {
            float baseResistance = 0.3f + culture.maturity / 100f * 0.5f;
            float transMod = 1f - transformativity / 200f;
            return Mathf.Clamp(baseResistance * transMod, 0.1f, 1f);
        }

        /// <summary>计算文化融合阻力（含变革性修正）</summary>
        public static float CalculateFusionResistance(CultureData dominant, CultureData minority, float transDominant, float transMinority)
        {
            float sim = CalculateSim(dominant, minority);
            float baseResistance = 1f - sim * 0.7f;
            float avgTrans = (transDominant + transMinority) / 2f;
            float transMod = 1f - avgTrans / 150f;
            return Mathf.Clamp(baseResistance * transMod, 0.1f, 1f);
        }

        /// <summary>计算文化传播速率</summary>
        public static float CalculateSpreadRate(CultureData culture, float transformativity, float contactIntensity)
        {
            float maturityMod = 0.5f + culture.maturity / 100f;
            float transMod = 0.5f + transformativity / 200f;
            return culture.spreadPower * maturityMod * transMod * contactIntensity * 0.01f;
        }
    }
}
