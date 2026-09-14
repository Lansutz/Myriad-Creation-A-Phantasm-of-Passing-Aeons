using System;
using System.Collections.Generic;
using CivilizationEvolution.Core;
using CivilizationEvolution.Core.Data;
using CivilizationEvolution.Core.Enums;

namespace CivilizationEvolution.World.Settlement
{
    /// <summary>
    /// 经济成分系统
    /// 城市内部的经济构成，由地理条件和物产自然决定，不需要硬性分类。
    ///
    /// 核心思想：
    /// - 城市有多种经济成分（农业、渔业、矿业、商业、手工业、行政、军事、宗教、文化、交通）
    /// - 每种成分有一个比例（0~1），总和为1
    /// - 地理条件和物产自然决定了初始成分比例
    /// - 成分比例随时间变化，受贸易、政策、人口、技术等影响
    /// - 城市的名称、功能、建筑、区划等根据主要成分来描述
    ///
    /// 这替代了原来的硬性经济起源分类（农业起源/渔业起源/矿业起源/牧业起源），
    /// 因为历史上的城市很少是单一经济起源的，大多数都是多种经济成分的混合。
    /// </summary>
    public static class EconomicCompositionSystem
    {
        /// <summary>
        /// 根据地理条件和物产初始化城市的经济成分
        /// 这是成分系统的核心：地理条件和物产自然决定了城市的经济成分比例。
        /// </summary>
        public static void InitializeComposition(BurgData burg, TileData tile)
        {
            if (burg == null) return;
            if (burg.economicInitialized) return;

            var composition = new Dictionary<EconomicSector, float>();

            // 基础权重（所有城市都有一定的基础成分）
            float agriculture = 10f;    // 农业基础
            float commerce = 5f;        // 商业基础
            float artisanal = 3f;       // 手工业基础
            float fishery = 0f;
            float mining = 0f;
            float administrative = 0f;
            float military = 0f;
            float religious = 1f;
            float cultural = 0.5f;
            float transportation = 0f;

            // 根据地理条件调整
            // 沿海/沿河 → 渔业和交通
            if (tile.isCoast || tile.isRiver)
            {
                fishery += 8f;
                transportation += 5f;
                commerce += 3f;
            }

            // 山地/高海拔 → 矿业
            if (tile.elevation01 > 0.6f)
            {
                mining += 10f;
            }

            // 干旱/少雨 → 牧业（归入农业）
            if (tile.annualPrecipMm < 300f)
            {
                agriculture += 5f; // 牧业也算农业的一种
            }

            // 肥沃土地 → 农业
            if (tile.fertility > 0.6f)
            {
                agriculture += 8f;
            }

            // 根据聚居点类型调整
            if (burg.settlementType == SettlementType.Fort)
            {
                military += 15f;
                administrative += 3f;
            }

            if (burg.isCapital)
            {
                administrative += 10f;
                cultural += 5f;
                religious += 3f;
                commerce += 5f;
            }

            if (burg.isPort)
            {
                transportation += 10f;
                commerce += 8f;
                fishery += 5f;
            }

            if (burg.hasMarket)
            {
                commerce += 5f;
            }

            if (burg.hasTemple)
            {
                religious += 5f;
            }

            if (burg.hasUniversity)
            {
                cultural += 8f;
            }

            // 计算总和并归一化
            float total = agriculture + fishery + mining + commerce + artisanal +
                         administrative + military + religious + cultural + transportation;

            if (total > 0)
            {
                composition[EconomicSector.Agriculture] = agriculture / total;
                composition[EconomicSector.Fishery] = fishery / total;
                composition[EconomicSector.Mining] = mining / total;
                composition[EconomicSector.Commerce] = commerce / total;
                composition[EconomicSector.Artisanal] = artisanal / total;
                composition[EconomicSector.Administrative] = administrative / total;
                composition[EconomicSector.Military] = military / total;
                composition[EconomicSector.Religious] = religious / total;
                composition[EconomicSector.Cultural] = cultural / total;
                composition[EconomicSector.Transportation] = transportation / total;
            }
            else
            {
                // 默认：以农业为主
                composition[EconomicSector.Agriculture] = 0.6f;
                composition[EconomicSector.Commerce] = 0.2f;
                composition[EconomicSector.Artisanal] = 0.1f;
                composition[EconomicSector.Religious] = 0.1f;
            }

            burg.economicComposition = composition;
            burg.primarySector = GetPrimarySector(composition);
            burg.economicInitialized = true;
        }

        /// <summary>
        /// 更新城市的经济成分（每日调用）
        /// 成分比例随时间缓慢变化，受贸易、政策、人口、技术等影响
        /// </summary>
        public static void UpdateComposition(BurgData burg, float deltaTime)
        {
            if (burg == null || burg.economicComposition == null || burg.economicComposition.Count == 0)
                return;

            // 简化：成分比例变化非常缓慢，目前只做微小的随机波动
            // 后续可以加入贸易、政策、人口、技术等因素的影响
            float changeRate = 0.001f * deltaTime; // 每天变化0.1%

            var composition = burg.economicComposition;
            var keys = new List<EconomicSector>(composition.Keys);

            // 随机波动（模拟经济的自然变化）
            foreach (var key in keys)
            {
                float randomChange = (UnityEngine.Random.value - 0.5f) * 2f * changeRate;
                composition[key] = Math.Max(0f, composition[key] + randomChange);
            }

            // 重新归一化
            NormalizeComposition(composition);
            burg.primarySector = GetPrimarySector(composition);
        }

        /// <summary>
        /// 获取主要经济成分（占比最高的成分）
        /// </summary>
        public static EconomicSector GetPrimarySector(Dictionary<EconomicSector, float> composition)
        {
            if (composition == null || composition.Count == 0)
                return EconomicSector.None;

            EconomicSector primary = EconomicSector.None;
            float maxValue = 0f;

            foreach (var kvp in composition)
            {
                if (kvp.Value > maxValue)
                {
                    maxValue = kvp.Value;
                    primary = kvp.Key;
                }
            }

            return primary;
        }

        /// <summary>
        /// 归一化成分比例（确保总和为1）
        /// </summary>
        private static void NormalizeComposition(Dictionary<EconomicSector, float> composition)
        {
            if (composition == null || composition.Count == 0) return;

            float total = 0f;
            foreach (var kvp in composition)
            {
                total += kvp.Value;
            }

            if (total > 0)
            {
                var keys = new List<EconomicSector>(composition.Keys);
                foreach (var key in keys)
                {
                    composition[key] = composition[key] / total;
                }
            }
        }

        /// <summary>
        /// 获取经济成分的描述（用于UI显示）
        /// </summary>
        public static string GetSectorDescription(EconomicSector sector)
        {
            return sector switch
            {
                EconomicSector.Agriculture => "农业",
                EconomicSector.Fishery => "渔业",
                EconomicSector.Mining => "矿业",
                EconomicSector.Commerce => "商业",
                EconomicSector.Artisanal => "手工业",
                EconomicSector.Administrative => "行政",
                EconomicSector.Military => "军事",
                EconomicSector.Religious => "宗教",
                EconomicSector.Cultural => "文化",
                EconomicSector.Transportation => "交通",
                _ => "未知"
            };
        }

        /// <summary>
        /// 获取城市经济成分的综合描述（用于Tooltip和UI显示）
        /// </summary>
        public static string GetCompositionDescription(BurgData burg)
        {
            if (burg == null || burg.economicComposition == null || burg.economicComposition.Count == 0)
                return "经济成分未知";

            // 按占比排序，取前3个
            var sorted = new List<KeyValuePair<EconomicSector, float>>(burg.economicComposition);
            sorted.Sort((a, b) => b.Value.CompareTo(a.Value));

            string description = "";
            for (int i = 0; i < Math.Min(3, sorted.Count); i++)
            {
                if (i > 0) description += "、";
                description += $"{GetSectorDescription(sorted[i].Key)}({sorted[i].Value * 100f:F0}%)";
            }

            return description;
        }
    }
}
