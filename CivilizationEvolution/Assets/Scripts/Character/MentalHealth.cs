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
    /// 数据驱动（2026-08-29 模组化）：MentalDisorderDef 由 MentalHealth/MentalHealthDefs.json
    /// 定义（Base/Mods 可覆盖、模组可新增），id 为字符串；MentalHealthSystem 查询
    /// 注册表优先、未初始化回退内置。企划书第九篇无此模块，用户新增需求。
    /// </summary>

    /// <summary>原版内置精神疾病 id 常量（字符串键，模组可新增任意 id）</summary>
    public static class MentalDisorderIds
    {
        public const string None = "";
        public const string Depression = "depression";   // 抑郁
        public const string Anxiety = "anxiety";         // 焦虑
        public const string Paranoia = "paranoia";       // 偏执
        public const string Delirium = "delirium";       // 谵妄
        public const string Dementia = "dementia";       // 失智（不可逆）
    }

    /// <summary>精神疾病定义（数据驱动：定义文件只存键，显示走本地化表）</summary>
    [Serializable]
    public class MentalDisorderDef
    {
        public string id;
        /// <summary>内置回退名（未加载本地化表时用）</summary>
        public string name;
        /// <summary>内置回退描述</summary>
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

        /// <summary>显示名：本地化表优先（&lt;id&gt;_name），回退内置字段</summary>
        public string GetName() => Localization.Has(id + "_name") ? Localization.Get(id + "_name") : name;
        /// <summary>写实描述：本地化表优先（&lt;id&gt;_desc），回退内置字段</summary>
        public string GetDescription() => Localization.Has(id + "_desc") ? Localization.Get(id + "_desc") : description;
    }

    /// <summary>精神疾病系统核心（注册表优先 + 内置回退 + 判定辅助）</summary>
    public static class MentalHealthSystem
    {
        // 触发阈值（简单版）
        public const int HighStressTriggerDays = 90;   // 压力>80 持续天数 → 抑郁/焦虑
        public const float DreadParanoiaThreshold = 80f; // 恐惧>80 → 偏执风险
        public const int LowStressRecoveryDays = 120;  // 压力<30 持续天数 → 恢复
        public const int DementiaAge = 70;             // 高龄失智起点
        public const float DementiaLearningGate = 45f; // 学识低于此值失智风险上升

        /// <summary>原版内置定义表（注册表未初始化/未定义时的回退）</summary>
        public static readonly Dictionary<string, MentalDisorderDef> BuiltinDefs =
            new Dictionary<string, MentalDisorderDef>
            {
                [MentalDisorderIds.Depression] = new MentalDisorderDef
                {
                    id = MentalDisorderIds.Depression,
                    name = "抑郁",
                    reversible = true,
                    description = "长期高压下的意志消沉，对政务与人事提不起兴致，日渐寡言",
                    learningMod = -5f,
                    diplomacyMod = -5f,
                    charmMod = -5f,
                    stressDecayMult = 0.4f
                },
                [MentalDisorderIds.Anxiety] = new MentalDisorderDef
                {
                    id = MentalDisorderIds.Anxiety,
                    name = "焦虑",
                    reversible = true,
                    description = "终日忧惧不安，寝食难安，决断迟疑，杯弓蛇影",
                    stewardshipMod = -5f,
                    warfareMod = -3f,
                    charmMod = -10f,
                    stressDecayMult = 0.3f
                },
                [MentalDisorderIds.Paranoia] = new MentalDisorderDef
                {
                    id = MentalDisorderIds.Paranoia,
                    name = "偏执",
                    reversible = true,
                    description = "怀疑身边的一切人，把善意当作阴谋，睚眦必报",
                    diplomacyMod = -8f,
                    intrigueMod = 5f,
                    opinionDecayMult = 2f
                },
                [MentalDisorderIds.Delirium] = new MentalDisorderDef
                {
                    id = MentalDisorderIds.Delirium,
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
                [MentalDisorderIds.Dementia] = new MentalDisorderDef
                {
                    id = MentalDisorderIds.Dementia,
                    name = "失智",
                    reversible = false,
                    description = "年迈带来的心智衰退，往事如烟，认不清亲人，性情大变",
                    learningMod = -15f,
                    warfareMod = -5f,
                    diplomacyMod = -5f,
                    charmMod = -5f
                }
            };

        /// <summary>按 id 取定义（注册表优先——模组可覆盖/新增；未初始化回退内置）</summary>
        public static MentalDisorderDef GetDef(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            if (ContentRegistry.IsInitialized && ContentRegistry.TryGetMentalDisorder(id, out var reg))
                return reg;
            return BuiltinDefs.TryGetValue(id, out var builtin) ? builtin : null;
        }

        /// <summary>获取角色精神疾病的显示名（无病返回空串）</summary>
        public static string GetDisorderName(CharacterData c)
        {
            var def = GetDef(c.mentalDisorderId);
            return def != null ? def.GetName() : "";
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

            var def = GetDef(c.mentalDisorderId);
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
