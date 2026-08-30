using System;
using System.Collections.Generic;
using UnityEngine;
using CivilizationEvolution.Core;
using CivilizationEvolution.Politics;
using CivilizationEvolution.Role;

namespace CivilizationEvolution.Culture
{
    /// <summary>文化数据（企划书 7.1 考古文化包七大板块）</summary>
    [Serializable]
    public class CultureData
    {
        public int cultureId;
        public string cultureName;
        /// <summary>地图文化着色（十六进制，如 "#4f8fbf"），由文化包 JSON 提供</summary>
        public string cultureColorHex = "#ffffff";
        public GameEnums.CultureStage stage;

        // ===== 七大板块（7.1.2） =====

        /// <summary>板块1 生计主轴（0-3：狩猎采集/渔猎/早期畜牧/管理型采集）</summary>
        public int livelihoodType;
        /// <summary>板块2 移动模式（0-2：高度流动/季节性营地/半定居）</summary>
        public int mobilityType;
        /// <summary>板块3 葬俗类型（兼容单选；多选用 burialTypes）</summary>
        public int burialType;
        /// <summary>板块3 葬俗类型（多选：竖穴墓0/二次葬1/瓮棺2/屈肢葬3/仰身葬4/赭石施用5）</summary>
        public List<int> burialTypes = new List<int>();
        /// <summary>板块4 原始崇拜权重向量（7维0-100相对配比：生殖/自然/祖先/死亡/图腾/形象/巫术；兼容旧4维）</summary>
        public float[] worshipVector;
        /// <summary>板块5 物质风格倾向（石器组合/陶器/骨器偏好）</summary>
        public int materialStyle;
        /// <summary>板块6 象征实践焦点（兼容单选；主次用 symbolicFoci）</summary>
        public int symbolicFocus;
        /// <summary>板块6 象征实践焦点（主+次双焦点：动物力量0/生殖1/山石2/死亡处理3）</summary>
        public List<int> symbolicFoci = new List<int>();
        /// <summary>板块7 环境适应标签（兼容单选；多选用 environmentAdapts）</summary>
        public int environmentAdapt;
        /// <summary>板块7 环境适应标签（多选：草原0/河谷1/森林边缘2/海岸3/山地4）</summary>
        public List<int> environmentAdapts = new List<int>();

        /// <summary>生产方式偏好（种族初始偏好传导，6.2.4）</summary>
        public int productionPreference;
        /// <summary>信仰倾向（种族初始偏好传导；-1=无）</summary>
        public int faithId = -1;
        /// <summary>默认语言（LanguageDef.languageId 引用）</summary>
        public string languageId = "";

        public float maturity = 0f;
        public float spreadPower = 1f;
        public int parentCultureId = -1;
        public List<int> childCultureIds = new List<int>();

        /// <summary>
        /// 文明默认继承法（inheritance_*_from_civilization 双轨模式：
        /// 国家未覆盖时按文化默认执行继承）
        /// </summary>
        public InheritanceLaw defaultSuccessionLaw = InheritanceLaw.Primogeniture();

        /// <summary>官职称号定制（文化覆盖默认表——OfficeTitleCatalog 查询）</summary>
        public List<OfficeTitleEntry> officialTitles = new List<OfficeTitleEntry>();

        /// <summary>民族特质概率表（EthnicTrait 枚举名 → 概率 0-100——AI 性格/行为基线）</summary>
        public List<EthnicTraitEntry> traitProbabilities = new List<EthnicTraitEntry>();

        /// <summary>
        /// 革新亲和（考古文化包联动 v3）：键列表（对应 InnovationField 枚举名，
        /// 如 "Metallurgy"/"Agriculture"）——文化在这些领域的研究速率获得加成
        /// （软加成，数据驱动；硬条件不做防卡死）
        /// </summary>
        public List<string> innovationAffinities = new List<string>();

        /// <summary>查询文化是否亲和某革新子类（field 名不区分大小写匹配）</summary>
        public bool HasInnovationAffinity(string fieldName)
        {
            if (string.IsNullOrEmpty(fieldName) || innovationAffinities == null) return false;
            foreach (var a in innovationAffinities)
                if (string.Equals(a, fieldName, System.StringComparison.OrdinalIgnoreCase)) return true;
            return false;
        }
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
