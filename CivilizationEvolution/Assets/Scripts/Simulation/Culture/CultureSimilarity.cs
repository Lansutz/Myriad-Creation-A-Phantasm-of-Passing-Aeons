using System.Collections.Generic;
using UnityEngine;

namespace CivilizationEvolution.Culture
{
    public static class CultureSimilarity
    {
        public const float W_LIVELIHOOD = 0.25f;
        public const float W_MOBILITY = 0.15f;
        public const float W_BURIAL = 0.15f;
        public const float W_WORSHIP = 0.20f;
        public const float W_MATERIAL = 0.10f;
        public const float W_SYMBOLIC = 0.10f;
        public const float W_ENVIRONMENT = 0.05f;

 /// <summary>计算两个文化的相似度 0~1（企划书 7.4.6 权重公式）</summary>
        public static float CalculateSim(CultureData a, CultureData b)
        {
            float sim = 0f;
            sim += W_LIVELIHOOD * CategoricalSim(a.livelihoodType, b.livelihoodType);
            sim += W_MOBILITY * CategoricalSim(a.mobilityType, b.mobilityType);
 // 葬俗重合度：多选集合优先（Jaccard），空集回退单值
            sim += W_BURIAL * SetOrSingleSim(a.burialTypes, b.burialTypes, a.burialType, b.burialType);
            sim += W_WORSHIP * CosineSimilarity(a.worshipVector, b.worshipVector);
            sim += W_MATERIAL * CategoricalSim(a.materialStyle, b.materialStyle);
 // 象征焦点重合度：多选集合优先
            sim += W_SYMBOLIC * SetOrSingleSim(a.symbolicFoci, b.symbolicFoci, a.symbolicFocus, b.symbolicFocus);
 // 环境适配标签重合度：多选集合优先
            sim += W_ENVIRONMENT * SetOrSingleSim(a.environmentAdapts, b.environmentAdapts, a.environmentAdapt, b.environmentAdapt);
            return Mathf.Clamp01(sim);
        }

        private static float CategoricalSim(int a, int b)
        {
            if (a == b) return 1f;
            if (Mathf.Abs(a - b) == 1) return 0.5f;
            return 0f;
        }

 /// <summary>多选集合相似（Jaccard 重合度）；双方集合均空时回退单值比较</summary>
        private static float SetOrSingleSim(List<int> setA, List<int> setB, int singleA, int singleB)
        {
            bool aEmpty = setA == null || setA.Count == 0;
            bool bEmpty = setB == null || setB.Count == 0;
            if (aEmpty && bEmpty) return CategoricalSim(singleA, singleB);
            if (aEmpty || bEmpty) return 0.5f; // 一方未定义：中性

            int inter = 0;
            foreach (int v in setA)
                if (setB.Contains(v)) inter++;
            int union = setA.Count + setB.Count - inter;
            return union > 0 ? (float)inter / union : 1f;
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
