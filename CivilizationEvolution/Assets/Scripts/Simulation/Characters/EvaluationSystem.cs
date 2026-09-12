using System.Collections.Generic;
using System.Text;
using System;
using UnityEngine;


namespace CivilizationEvolution.Simulation.Characters
{
 /// 评价分级（一生成就标尺——从高到低）：
 /// 传奇 &gt; 卓越 &gt; 杰出 &gt; 优秀 &gt; 平平 &gt; 平庸 &gt; 无名 &gt; 遗臭
 /// 等级名与绰号系统分离（不用"伟大"——伟大者是绰号非等级）
 /// 成就分（0-1000）映射等级；绰号按等级+行为发放

 /// 评价系统（成就分→等级→绰号发放标尺）
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
