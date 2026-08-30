using System.Collections.Generic;
using CivilizationEvolution.Core;
using UnityEngine;

namespace CivilizationEvolution.Map
{
    /// <summary>
    /// 聚落形态演化系统
    /// 管理村镇/城/堡三种形态的渐进式演化，不突变
    /// 形态-等级-倾向三者软性约束（AI遵循，玩家可手动突破）
    /// </summary>
    public static class SettlementEvolutionSystem
    {
        // ===== 演化阈值 =====
        /// <summary>形态切换所需演化进度阈值</summary>
        public const float EvolutionThreshold = 80f;

        /// <summary>形态切换冷却期（Tick数）</summary>
        public const int TransitionCooldown = 365; // 约1年

        /// <summary>每Tick最大演化进度变化</summary>
        public const float MaxEvolutionDeltaPerTick = 0.5f;

        // ===== 倾向阈值 =====
        /// <summary>军政倾向高阈值（超过则堡形态稳定）</summary>
        public const float HighMilitaryThreshold = 0.65f;

        /// <summary>军政倾向低阈值（低于则堡形态向村镇/城偏移）</summary>
        public const float LowMilitaryThreshold = 0.35f;

        /// <summary>经贸倾向高阈值（超过则城形态稳定）</summary>
        public const float HighEconomyThreshold = 0.6f;

        /// <summary>发展度阈值（村镇升城所需最低发展度）</summary>
        public const float VillageToCityDevelopment = 40f;

        /// <summary>人口阈值（村镇升城所需最低人口）</summary>
        public const float VillageToCityPopulation = 2000f;

        /// <summary>
        /// 每Tick更新单个聚落的形态演化进度
        /// </summary>
        /// <param name="burg">聚落数据</param>
        /// <param name="militaryWeight">当前军政倾向权重（0~1）</param>
        /// <param name="economyWeight">当前经贸倾向权重（0~1）</param>
        /// <param name="cultureWeight">当前文教倾向权重（0~1）</param>
        /// <param name="isStrategicLocation">是否为战略要地（隘口/海峡/边境）</param>
        /// <param name="deltaTime">时间增量（Tick比例）</param>
        /// <returns>是否发生了形态切换</returns>
        public static bool UpdateEvolution(BurgData burg,
            float militaryWeight, float economyWeight, float cultureWeight,
            bool isStrategicLocation, float deltaTime = 1f)
        {
            if (burg == null) return false;

            burg.ticksSinceLastTransition++;

            // 冷却期内不演化
            if (burg.ticksSinceLastTransition < TransitionCooldown)
                return false;

            // 计算演化方向和速度
            float evolutionDelta = CalculateEvolutionDelta(burg,
                militaryWeight, economyWeight, cultureWeight, isStrategicLocation);

            // 应用演化进度（向目标方向累积）
            burg.settlementEvolution = Mathf.Clamp(
                burg.settlementEvolution + evolutionDelta * deltaTime,
                -100f, 100f);

            // 检查是否达到切换阈值
            if (Mathf.Abs(burg.settlementEvolution) >= EvolutionThreshold)
            {
                return TryTransition(burg, burg.settlementEvolution > 0);
            }

            return false;
        }

        /// <summary>
        /// 计算演化进度变化量
        /// 正值表示向"更高阶"演化（村镇→城，城→堡）
        /// 负值表示向"更低阶"退化（堡→城/村镇，城→村镇）
        /// </summary>
        private static float CalculateEvolutionDelta(BurgData burg,
            float militaryWeight, float economyWeight, float cultureWeight,
            bool isStrategicLocation)
        {
            float delta = 0f;

            switch (burg.settlementType)
            {
                case SettlementType.Village:
                    // 村镇→城：发展度、人口、经贸达到阈值
                    if (burg.development >= VillageToCityDevelopment &&
                        burg.population >= VillageToCityPopulation &&
                        economyWeight >= 0.4f)
                    {
                        delta += 0.3f * (economyWeight + burg.development / 100f);
                    }
                    // 村镇→堡：战略要地且军政倾向上升
                    if (isStrategicLocation && militaryWeight >= HighMilitaryThreshold)
                    {
                        delta += 0.4f * militaryWeight;
                    }
                    // 稳定度抵抗演化
                    delta -= burg.settlementStability * 0.002f;
                    break;

                case SettlementType.City:
                    // 城→堡：军政倾向极高且地处战略要地
                    if (isStrategicLocation && militaryWeight >= HighMilitaryThreshold)
                    {
                        delta += 0.35f * (militaryWeight - 0.5f);
                    }
                    // 城→村镇：军政经贸均低，发展度下降
                    if (militaryWeight < LowMilitaryThreshold &&
                        economyWeight < 0.3f &&
                        burg.development < 20f)
                    {
                        delta -= 0.25f * (0.5f - economyWeight);
                    }
                    delta -= burg.settlementStability * 0.002f;
                    break;

                case SettlementType.Fort:
                    // 堡→城：经贸文教持续增长，军政倾向下降
                    if (militaryWeight < LowMilitaryThreshold &&
                        (economyWeight >= 0.5f || cultureWeight >= 0.4f))
                    {
                        delta -= 0.3f * (0.5f - militaryWeight + economyWeight * 0.5f);
                    }
                    // 堡→村镇：军政持续极低，人口以农耕商贸为主
                    if (militaryWeight < 0.2f && economyWeight < 0.3f &&
                        burg.fortification < 2f)
                    {
                        delta -= 0.4f * (0.3f - militaryWeight);
                    }
                    // 战略要地的堡垒更稳定
                    if (isStrategicLocation) delta += 0.1f;
                    delta -= burg.settlementStability * 0.002f;
                    break;
            }

            // 玩家指定演化目标时加速
            if (burg.evolutionTarget.HasValue &&
                burg.evolutionTarget.Value != burg.settlementType)
            {
                delta += 0.2f; // 玩家推动加速
            }

            return Mathf.Clamp(delta, -MaxEvolutionDeltaPerTick, MaxEvolutionDeltaPerTick);
        }

        /// <summary>
        /// 尝试形态切换
        /// </summary>
        private static bool TryTransition(BurgData burg, bool upward)
        {
            SettlementType oldType = burg.settlementType;
            SettlementType newType = oldType;

            switch (oldType)
            {
                case SettlementType.Village:
                    newType = upward ? SettlementType.City : SettlementType.Village;
                    // 村镇不能向下退化
                    if (!upward) return false;
                    break;

                case SettlementType.City:
                    newType = upward ? SettlementType.Fort : SettlementType.Village;
                    break;

                case SettlementType.Fort:
                    // 堡不能向上演化，只能向下
                    if (upward) return false;
                    newType = burg.development > 30f ? SettlementType.City : SettlementType.Village;
                    break;
            }

            if (newType == oldType) return false;

            // 执行切换
            burg.settlementType = newType;
            burg.settlementEvolution = 0f;
            burg.settlementStability = Mathf.Max(0f, burg.settlementStability - 20f);
            burg.ticksSinceLastTransition = 0;

            Debug.Log($"[SettlementEvolution] {burg.burgName}: {oldType} → {newType}");
            return true;
        }

        /// <summary>
        /// 检查形态-等级约束（软性规则）
        /// 返回是否违反约束（AI演化时应避免，玩家可突破）
        /// </summary>
        public static bool CheckLevelConstraint(BurgData burg)
        {
            return burg.buildLevel <= burg.MaxBuildLevelForType;
        }

        /// <summary>
        /// 获取形态描述
        /// </summary>
        public static string GetSettlementDescription(SettlementType type)
        {
            return type switch
            {
                SettlementType.Village => "村镇：村落、集镇，生产功能为主，防御薄弱，辐射范围小",
                SettlementType.City => "城：城邑、都会、大都会，区域综合型中心，功能复合",
                SettlementType.Fort => "堡：堡垒、要塞、堡寨，军事防御为核心，等级跨度完整",
                _ => "未知"
            };
        }

        /// <summary>
        /// 根据BurgType推断初始SettlementType
        /// </summary>
        public static SettlementType InferFromBurgType(BurgType burgType)
        {
            return burgType switch
            {
                BurgType.Village => SettlementType.Village,
                BurgType.Town => SettlementType.Village,
                BurgType.City => SettlementType.City,
                BurgType.Port => SettlementType.City,
                BurgType.Capital => SettlementType.City,
                BurgType.Fortress => SettlementType.Fort,
                _ => SettlementType.Village
            };
        }
    }
}
