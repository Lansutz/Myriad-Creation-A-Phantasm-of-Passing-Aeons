using System;
using System.Collections.Generic;
using UnityEngine;
using CivilizationEvolution.Core;

namespace CivilizationEvolution.Role
{
    /// <summary>
    /// 精神疾病（简单版）
    /// 角色级状态机，与传染病系统（人口级）分层：
    /// - 触发：长期高压（压力>80 持续 90 天）、深度恐惧（>80）、高龄心智衰退、重病后谵妄
    /// - 效果：属性修正 + 压力恢复速度 + 好感衰减速度
    /// - 缓解：压力长期低于 30 可恢复（失智不可逆）
    /// 企划书第九篇无此模块，2026-08-29 用户新增需求
    /// </summary>
    public enum MentalDisorderType
    {
        None,
        Depression,   // 抑郁
        Anxiety,      // 焦虑
        Paranoia,     // 偏执
        Delirium,     // 谵妄
        Dementia      // 失智（不可逆）
    }

    /// <summary>精神疾病定义</summary>
    [Serializable]
    public class MentalDisorderDef
    {
        public MentalDisorderType type;
        public string name;
        public string description;
        public bool reversible = true;   // 失智不可逆

        // 属性修正
        public float martialMod;
        public float diplomacyMod;
        public float warfareMod;
        public float stewardshipMod;
        public float intrigueMod;
        public float learningMod;
        public float charmMod;

        /// <summary>压力恢复倍率（&lt;1 恢复慢：抑郁/焦虑使压力缠绵不去）</summary>
        public float stressDecayMult = 1f;
        /// <summary>好感自然衰减倍率（&gt;1 关系恶化快：偏执）</summary>
        public float opinionDecayMult = 1f;
    }

    /// <summary>精神疾病系统核心（静态定义表 + 判定辅助）</summary>
    public static class MentalHealthSystem
    {
        // 触发阈值（简单版）
        public const int HighStressTriggerDays = 90;   // 压力>80 持续天数 → 抑郁/焦虑
        public const float DreadParanoiaThreshold = 80f; // 恐惧>80 → 偏执风险
        public const int LowStressRecoveryDays = 120;  // 压力<30 持续天数 → 恢复
        public const int DementiaAge = 70;             // 高龄失智起点
        public const float DementiaLearningGate = 45f; // 学识低于此值失智风险上升

        public static readonly Dictionary<MentalDisorderType, MentalDisorderDef> Defs =
            new Dictionary<MentalDisorderType, MentalDisorderDef>
            {
                [MentalDisorderType.Depression] = new MentalDisorderDef
                {
                    type = MentalDisorderType.Depression,
                    name = "抑郁",
                    reversible = true,
                    description = "长期高压下的意志消沉，对政务与人事提不起兴致，日渐寡言",
                    learningMod = -5f,
                    diplomacyMod = -5f,
                    charmMod = -5f,
                    stressDecayMult = 0.4f
                },
                [MentalDisorderType.Anxiety] = new MentalDisorderDef
                {
                    type = MentalDisorderType.Anxiety,
                    name = "焦虑",
                    reversible = true,
                    description = "终日忧惧不安，寝食难安，决断迟疑，杯弓蛇影",
                    stewardshipMod = -5f,
                    warfareMod = -3f,
                    charmMod = -10f,
                    stressDecayMult = 0.3f
                },
                [MentalDisorderType.Paranoia] = new MentalDisorderDef
                {
                    type = MentalDisorderType.Paranoia,
                    name = "偏执",
                    reversible = true,
                    description = "怀疑身边的一切人，把善意当作阴谋，睚眦必报",
                    diplomacyMod = -8f,
                    intrigueMod = 5f,
                    opinionDecayMult = 2f
                },
                [MentalDisorderType.Delirium] = new MentalDisorderDef
                {
                    type = MentalDisorderType.Delirium,
                    name = "谵妄",
                    reversible = true,
                    description = "神志昏乱，言语无状，时而清醒时而糊涂，不识昼夜",
                    martialMod = -3f,
                    diplomacyMod = -3f,
                    warfareMod = -3f,
                    stewardshipMod = -3f,
                    intrigueMod = -3f,
                    learningMod = -3f
                },
                [MentalDisorderType.Dementia] = new MentalDisorderDef
                {
                    type = MentalDisorderType.Dementia,
                    name = "失智",
                    reversible = false,
                    description = "年迈带来的心智衰退，往事如烟，认不清亲人，性情大变",
                    learningMod = -15f,
                    warfareMod = -5f,
                    diplomacyMod = -5f,
                    charmMod = -5f
                }
            };

        public static MentalDisorderDef GetDef(MentalDisorderType type)
        {
            return Defs.TryGetValue(type, out var def) ? def : null;
        }

        /// <summary>获取角色精神疾病的名称（无则空串）</summary>
        public static string GetDisorderName(CharacterData c)
        {
            var def = GetDef(c.mentalDisorder);
            return def != null ? def.name : "";
        }

        /// <summary>精神疾病属性修正（六维 + 魅力，作用于显示/判定层）</summary>
        public static void ApplyDisorderMods(CharacterData c, out float martial, out float diplomacy,
            out float warfare, out float stewardship, out float intrigue, out float learning, out float charm)
        {
            martial = c.martial;
            diplomacy = c.diplomacy;
            warfare = c.warfare;
            stewardship = c.stewardship;
            intrigue = c.intrigue;
            learning = c.learning;
            charm = c.charm;

            var def = GetDef(c.mentalDisorder);
            if (def == null) return;
            martial += def.martialMod;
            diplomacy += def.diplomacyMod;
            warfare += def.warfareMod;
            stewardship += def.stewardshipMod;
            intrigue += def.intrigueMod;
            learning += def.learningMod;
            charm += def.charmMod;
        }
    }
}
