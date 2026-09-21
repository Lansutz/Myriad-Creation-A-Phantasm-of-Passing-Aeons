using System;
using System.Collections.Generic;

namespace CivilizationEvolution.Simulation.Innovation
{
    /// <summary>
    /// 单个革新的研究进度。
    /// 前现代技术进步 = 生产实践中积累经验，不是投入抽象研究点数。
    /// 进度增长来源：基础经验积累 + 资源实践（拥有相关资源且有产量）+ 规模效应 + 技术人员 + 建筑。
    /// </summary>
    [Serializable]
    public class InnovationProgress
    {
        public int innovationId;

        /// <summary>研究进度（0-100）</summary>
        public float progress;

        /// <summary>本月增长（用于UI显示）</summary>
        public float monthlyGain;

        /// <summary>是否可研究（满足前置革新+前置物产）</summary>
        public bool isAvailable;

        /// <summary>锁定原因（UI显示，如"缺少野马资源"/"需要前置革新：冶金"）</summary>
        public string lockedReason;

        /// <summary>增长来源明细（UI显示用）</summary>
        public Dictionary<string, float> gainBreakdown = new Dictionary<string, float>();

        /// <summary>累计相关产物产量（实践量；超过当前品质上限后不再提供经验）</summary>
        public float cumulativeOutput;

        /// <summary>相关产物的平均质量（1-10，决定产量经验上限与品质系数）</summary>
        public float averageQuality;

        /// <summary>沿用旧方法的实践次数（达到阈值后角色更易获得灵感）</summary>
        public int oldMethodPracticeCount;
    }

    /// <summary>
    /// 革新研究进度的配置参数（可调，数据驱动）。
    /// </summary>
    public static class InnovationProgressConfig
    {
        /// <summary>基础每月增长（没有资源/人员时的缓慢经验积累）</summary>
        public const float BaseMonthlyGain = 0.1f;

        /// <summary>拥有相关资源且月产量≥10单位时的加成</summary>
        public const float ResourcePracticeBonus = 0.5f;

        /// <summary>资源月产量≥100单位时的规模加成</summary>
        public const float ScaleBonus = 1.0f;

        /// <summary>有工匠/学者阶层时的技术人员加成</summary>
        public const float SpecialistBonus = 0.3f;

        /// <summary>有相关建筑（工坊/学校）时的设施加成</summary>
        public const float FacilityBonus = 0.2f;

        /// <summary>每月增长上限</summary>
        public const float MaxMonthlyGain = 2.0f;

        /// <summary>资源实践入门阈值（月产量≥此值触发ResourcePracticeBonus）</summary>
        public const float PracticeThreshold = 10f;

        /// <summary>规模效应阈值（月产量≥此值触发ScaleBonus）</summary>
        public const float ScaleThreshold = 100f;

        /// <summary>每级品质对应的累计产量经验上限（品质1=50，品质5=250，品质10=500）</summary>
        public const float OutputCapPerQuality = 50f;

        /// <summary>品质系数：相对标准品质(5)每级的经验增减（+/-10%）</summary>
        public const float QualityCoefficientPerLevel = 0.1f;

        /// <summary>产量经验基础系数（log(1+有效产量) 的乘子）</summary>
        public const float OutputExperienceBase = 0.4f;

        /// <summary>每个研究角色（工匠/学者/商人）每月提供的基础研究进度</summary>
        public const float CharacterResearchBase = 0.15f;

        /// <summary>旧方法实践次数阈值：达到后角色获得灵感、研究效率翻倍</summary>
        public const int OldMethodPracticeThreshold = 30;

        /// <summary>边际递减基数：递减系数=1/(1+progress/此值)，progress 等于此值时增长减半</summary>
        public const float DiminishingBase = 60f;
    }
}
