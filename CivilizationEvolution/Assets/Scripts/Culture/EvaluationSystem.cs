using System.Collections.Generic;
using System.Text;
using UnityEngine;
using CivilizationEvolution.Role;

namespace CivilizationEvolution.Culture
{
    /// <summary>
    /// 评价分级（一生成就标尺——从高到低）：
    /// 传奇 &gt; 卓越 &gt; 杰出 &gt; 优秀 &gt; 平平 &gt; 平庸 &gt; 无名 &gt; 遗臭
    /// 等级名与绰号系统分离（不用"伟大"——伟大者是绰号非等级）
    /// 成就分（0-1000）映射等级；绰号按等级+行为发放
    /// </summary>
    public enum EvaluationLevel
    {
        Legendary,      // 传奇——神话级跨时代（亚历山大）
        Preeminent,     // 卓越——超群绝伦，一世代罕见（传奇之下）
        Distinguished,  // 杰出——时代顶尖，领域大师
        Excellent,      // 优秀——出类拔萃，超出一般
        Mediocre,       // 平平——中游，无突出建树亦无大过
        Ordinary,       // 平庸——偏低，碌碌无为
        Obscure,        // 无名——几乎无痕迹，史书不载
        Infamous        // 遗臭——恶名，败政乱国
    }

    /// <summary>
    /// 评价系统（成就分→等级→绰号发放标尺）
    /// </summary>
    public static class EvaluationSystem
    {
        /// <summary>等级名（中文——评价词非绰号）</summary>
        public static string LevelName(EvaluationLevel level)
        {
            switch (level)
            {
                case EvaluationLevel.Legendary: return "传奇";
                case EvaluationLevel.Preeminent: return "卓越";
                case EvaluationLevel.Distinguished: return "杰出";
                case EvaluationLevel.Excellent: return "优秀";
                case EvaluationLevel.Mediocre: return "平平";
                case EvaluationLevel.Ordinary: return "平庸";
                case EvaluationLevel.Obscure: return "无名";
                case EvaluationLevel.Infamous: return "遗臭";
                default: return "未知";
            }
        }

        /// <summary>成就分→等级（0-1000——阈值）</summary>
        public static EvaluationLevel LevelFromScore(float score)
        {
            if (score >= 900f) return EvaluationLevel.Legendary;
            if (score >= 750f) return EvaluationLevel.Preeminent;
            if (score >= 550f) return EvaluationLevel.Distinguished;
            if (score >= 350f) return EvaluationLevel.Excellent;
            if (score >= 200f) return EvaluationLevel.Mediocre;
            if (score >= 100f) return EvaluationLevel.Ordinary;
            if (score >= 0f) return EvaluationLevel.Obscure;
            return EvaluationLevel.Infamous; // 负分=恶名
        }

        /// <summary>行为统计（角色一生——绰号/评价的输入——[Serializable] 供 Unity 序列化分析器）</summary>
        [System.Serializable]
        public struct AchievementRecord
        {
            public int warsWon;          // 胜仗
            public int conquests;        // 征服领地
            public int cultureActs;      // 文治（学院/法典/艺术）
            public int poetryActs;       // 诗作/文学创作（诗人/诗人王判定——文治细分）
            public int religionActs;     // 宗教（建寺/朝圣/护教）
            public int faithChanges;     // 改宗次数（叛教者判定）
            public int defeatedBattles;  // 败仗（常胜者判定——无败仗）
            public int massacres;        // 屠城（屠夫判定——负评价）
            public int rebellions;       // 叛乱次数（受爱戴/被憎恨判定）
            public int defensiveWins;    // 防御大捷（铁锤判定——打退入侵）
            public bool usurpedThrone;   // 篡位上位（篡位者判定）
            public bool canonized;       // 死后封圣（圣者判定——死亡结算传入）
            public bool youngAccession;     // 幼年即位（年轻者判定——即位时<16——
                                            // bool 默认 false 安全——struct 无初始化器）
            public bool ruledUnderRegency;  // 摄政掌权（被架空/护国公/年轻者判定）
            public int schemesSucceeded; // 诈术成功（外交欺诈/密谋）
            public int threatsResolved;  // 化解危机（叛乱/密谋/边境）
            public int expeditions;      // 远征/探险
            public int lostAllLands;     // 失地（反讽绰号）
            public bool famineUnderRule; // 治下大饥荒（负评价）
            public float reignYears;
            /// <summary>区域影响力 0-1（该角色在其所在地区[政治圈/文化圈/地理区]
            /// 的相对影响力——领土规模/声望/周边承认综合——由调用方计算——
            /// 伟大者的判定依据：≥0.6=区域内前列[中等偏上]即可——阿尔弗雷德式：
            /// 未控制整个英格兰但区域内影响力大）</summary>
            public float regionalInfluence;
        }

        /// <summary>成就评分（各行为加权——负项扣分）</summary>
        public static float CalculateScore(AchievementRecord r)
        {
            float s = 50f; // 基础（平平起点）
            s += r.warsWon * 30f;
            s += r.conquests * 40f;
            s += r.cultureActs * 35f;
            s += r.poetryActs * 40f; // 诗作传世权重高（诗人王/诗人的成就源）
            s += r.religionActs * 25f;
            s -= r.defeatedBattles * 20f; // 败仗扣分
            s -= r.massacres * 60f;      // 屠城重扣（暴行——屠夫绰号负评价）
            s -= r.rebellions * 15f;     // 叛乱扣分（治理不稳）
            s += r.defensiveWins * 25f;  // 卫国大捷加分
            if (r.usurpedThrone) s -= 30f; // 篡位减分（合法性）
            if (r.canonized) s += 80f;   // 封圣大加分
            s += r.schemesSucceeded * 20f;
            s += r.threatsResolved * 30f;
            s += r.expeditions * 45f;
            if (r.famineUnderRule) s -= 150f;
            if (r.lostAllLands > 0) s -= 100f;
            return Mathf.Clamp(s, -100f, 1000f);
        }
    }
}
