using System;
using System.Collections.Generic;
using UnityEngine;
using CivilizationEvolution.Core;

namespace CivilizationEvolution.Tech
{
    /// <summary>
    /// 革新分类体系（2026-08-30 重构定稿）
    /// 两级分类：大类（性质维度）× 子类（领域维度）
    /// 大类四分类 = 文化学经典四分法：物质文化（技术）/ 精神文化（思维）/
    /// 制度文化（制度）/ 习俗文化（传统）
    /// 边界归位规则：武器装备器械→技术；战术兵法→思维；军制→制度；
    /// 医术实践→技术-医疗；医理→思维；教义→思维-神学；仪式→传统-仪礼；教团→制度-教制
    /// </summary>

    /// <summary>革新大类（性质维度）</summary>
    public enum InnovationDomain
    {
        Technology,   // 技术：人与自然互动的物质手段
        Thought,      // 思维：认知与观念体系
        Institution,  // 制度：组织与规则
        Tradition     // 传统：习俗与文化传承
    }

    /// <summary>革新子类（领域维度；每个子类唯一归属一个大类）</summary>
    public enum InnovationField
    {
        // ===== 技术 Technology =====
        Mining,             // 采掘
        Agriculture,        // 农耕
        Metallurgy,         // 冶炼
        Construction,       // 营建
        Medicine,           // 医疗
        Machinery,          // 器械
        Craft,              // 工艺
        Transport,          // 交通
        // ===== 思维 Thought =====
        Script,             // 文字
        Mathematics,        // 数理
        Philosophy,         // 哲思
        NaturalCognition,   // 格致（自然认知）
        Theology,           // 神学
        MilitaryThought,    // 兵学（战术兵法）
        // ===== 制度 Institution =====
        Governance,         // 政制
        Law,                // 法制
        Bureaucracy,        // 官制
        Economic,           // 经制
        MilitaryInstitution,// 军制
        ReligiousInstitution,// 教制
        Education,          // 教育
        // ===== 传统 Tradition =====
        Ritual,             // 仪礼
        Festival,           // 岁时
        Folkways,           // 民俗
        Heritage            // 传承
    }

    /// <summary>子类→大类归属映射（子类唯一归属，推导 domain 用）</summary>
    public static class InnovationDomainMap
    {
        public static readonly Dictionary<InnovationField, InnovationDomain> FieldToDomain =
            new Dictionary<InnovationField, InnovationDomain>
            {
                // 技术
                [InnovationField.Mining] = InnovationDomain.Technology,
                [InnovationField.Agriculture] = InnovationDomain.Technology,
                [InnovationField.Metallurgy] = InnovationDomain.Technology,
                [InnovationField.Construction] = InnovationDomain.Technology,
                [InnovationField.Medicine] = InnovationDomain.Technology,
                [InnovationField.Machinery] = InnovationDomain.Technology,
                [InnovationField.Craft] = InnovationDomain.Technology,
                [InnovationField.Transport] = InnovationDomain.Technology,
                // 思维
                [InnovationField.Script] = InnovationDomain.Thought,
                [InnovationField.Mathematics] = InnovationDomain.Thought,
                [InnovationField.Philosophy] = InnovationDomain.Thought,
                [InnovationField.NaturalCognition] = InnovationDomain.Thought,
                [InnovationField.Theology] = InnovationDomain.Thought,
                [InnovationField.MilitaryThought] = InnovationDomain.Thought,
                // 制度
                [InnovationField.Governance] = InnovationDomain.Institution,
                [InnovationField.Law] = InnovationDomain.Institution,
                [InnovationField.Bureaucracy] = InnovationDomain.Institution,
                [InnovationField.Economic] = InnovationDomain.Institution,
                [InnovationField.MilitaryInstitution] = InnovationDomain.Institution,
                [InnovationField.ReligiousInstitution] = InnovationDomain.Institution,
                [InnovationField.Education] = InnovationDomain.Institution,
                // 传统
                [InnovationField.Ritual] = InnovationDomain.Tradition,
                [InnovationField.Festival] = InnovationDomain.Tradition,
                [InnovationField.Folkways] = InnovationDomain.Tradition,
                [InnovationField.Heritage] = InnovationDomain.Tradition
            };

        /// <summary>子类推导大类（未注册返回 Technology 兜底，测试可校验）</summary>
        public static InnovationDomain GetDomain(InnovationField field)
        {
            return FieldToDomain.TryGetValue(field, out var domain) ? domain : InnovationDomain.Technology;
        }
    }

    /// <summary>革新定义（数据驱动：Innovation/Innovations.json，Base/Mods 可覆盖）</summary>
    [Serializable]
    public class InnovationDef
    {
        public int innovationId;
        /// <summary>内置名称（本地化表有 &lt;id&gt;_name 键时优先）</summary>
        public string innovationName;
        /// <summary>子类（domain 由映射表推导）</summary>
        public InnovationField field;
        public int era; // 时代 0-5
        public float researchCost;
        /// <summary>前置（AND：全部满足）</summary>
        public List<int> prerequisites = new List<int>();
        /// <summary>或前置（OR：满足任一即可；差异化路径——同节点多形态）</summary>
        public List<int> prerequisitesAny = new List<int>();
        /// <summary>内置描述（本地化表有 &lt;id&gt;_desc 键时优先）</summary>
        public string description;
        /// <summary>
        /// 节点级文化亲和标签（如 "Clay"/"Papyrus"/"Quipu"）：
        /// 文化 innovationAffinities 含该标签时研究速率加成——引导不同文明
        /// 走向不同行政/技术形态（差异化路径，软引导非硬锁）
        /// </summary>
        public List<string> affinityTags = new List<string>();

        /// <summary>所属大类（由子类映射推导）</summary>
        public InnovationDomain Domain => InnovationDomainMap.GetDomain(field);

        /// <summary>显示名：本地化表优先（&lt;id&gt;_name），回退内嵌字段</summary>
        public string GetName() => Localization.Has(innovationId + "_name") ? Localization.Get(innovationId + "_name") : innovationName;

        /// <summary>写实描述：本地化表优先（&lt;id&gt;_desc），回退内嵌字段</summary>
        public string GetDescription() => Localization.Has(innovationId + "_desc") ? Localization.Get(innovationId + "_desc") : description;
    }
}
