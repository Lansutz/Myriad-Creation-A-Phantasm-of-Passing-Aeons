using System;
using System.Collections.Generic;
using MyriadCreation.Core;
using MyriadCreation.Core.Data;
using MyriadCreation.Core.Enums;

namespace MyriadCreation.World.Settlement
{
    /// <summary>
    /// 城市区划系统
    /// 只有大型城市（Ⅳ级都会/Ⅴ级大都会）才会分区划。
    /// 区划是城市的内部分区，不是独立的聚居点类型。
    /// 区划的形成受经济成分系统影响，不同成分占比的城市区划结构不同。
    ///
    /// 核心思想：
    /// - 只有达到一定规模的城市才会有明确的功能分区
    /// - 区划的类型和比例由城市的经济成分决定
    /// - 区划影响城市的经济产出、驻军上限、税收等
    /// - 区划随城市发展自然形成，不是硬性分类
    ///
    /// 借鉴了 Azgaar's Fantasy Map Generator 的区划设计思路：
    /// 区划是城市的内部分区，由城市的功能和发展自然形成。
    /// </summary>
    public static class CityDistrictSystem
    {
        /// <summary>
        /// 生成城市区划（当城市达到Ⅳ级都会时调用）
        /// 区划的类型和比例由城市的经济成分决定。
        /// </summary>
        public static void GenerateDistricts(BurgData burg)
        {
            if (burg == null) return;
            if (burg.districtsGenerated) return;

            // 只有Ⅳ级都会/Ⅴ级大都会才会分区划
            if (burg.settlementLevel < SettlementLevel.LevelIV)
                return;

            var districts = new List<CityDistrict>();

            // 根据经济成分生成区划
            // 主要经济成分会形成对应的区划
            if (burg.economicComposition != null && burg.economicComposition.Count > 0)
            {
                // 按占比排序
                var sorted = new List<KeyValuePair<EconomicSector, float>>(burg.economicComposition);
                sorted.Sort((a, b) => b.Value.CompareTo(a.Value));

                // 前3个主要成分会形成明确的区划
                int districtCount = Math.Min(3, sorted.Count);
                float remainingRatio = 1.0f;

                for (int i = 0; i < districtCount; i++)
                {
                    var sector = sorted[i].Key;
                    var ratio = sorted[i].Value;

                    // 主要成分占比更高，次要成分占比更低
                    float districtRatio = ratio * (i == 0 ? 0.6f : 0.3f);
                    if (districtRatio > remainingRatio) districtRatio = remainingRatio;
                    remainingRatio -= districtRatio;

                    var districtType = MapSectorToDistrictType(sector);
                    if (districtType != CityDistrictType.None)
                    {
                        var district = new CityDistrict(
                            districtType,
                            GetDistrictName(districtType, i),
                            districtRatio,
                            ratio * 0.8f, // 人口比例略低于面积比例
                            50f + ratio * 30f, // 发展度
                            ratio * burg.wealth, // 财富
                            0f // 创建时间，后续可以设置为游戏内日期
                        );
                        districts.Add(district);
                    }
                }

                // 剩余面积为贫民区/普通居民区
                if (remainingRatio > 0.1f)
                {
                    var slum = new CityDistrict(
                        CityDistrictType.Slum,
                        "平民区",
                        remainingRatio,
                        remainingRatio * 1.2f, // 贫民区人口密度更高
                        30f,
                        remainingRatio * burg.wealth * 0.3f,
                        0f
                    );
                    districts.Add(slum);
                }
            }
            else
            {
                // 如果没有经济成分数据，生成默认区划
                districts.Add(new CityDistrict(CityDistrictType.Commercial, "商业区", 0.3f, 0.25f, 60f, burg.wealth * 0.3f, 0f));
                districts.Add(new CityDistrict(CityDistrictType.Administrative, "行政区", 0.2f, 0.15f, 70f, burg.wealth * 0.2f, 0f));
                districts.Add(new CityDistrict(CityDistrictType.Slum, "平民区", 0.5f, 0.6f, 30f, burg.wealth * 0.2f, 0f));
            }

            burg.districts = districts;
            burg.districtsGenerated = true;
        }

        /// <summary>
        /// 更新城市区划（每日调用）
        /// 区划随城市发展缓慢变化
        /// </summary>
        public static void UpdateDistricts(BurgData burg, float deltaTime)
        {
            if (burg == null || burg.districts == null || burg.districts.Count == 0)
                return;

            // 简化：区划变化非常缓慢，目前只做微小的发展度增长
            foreach (var district in burg.districts)
            {
                // 区划发展度缓慢增长
                district.development = Math.Min(100f, district.development + 0.01f * deltaTime);
                district.wealth += district.areaRatio * burg.wealth * 0.001f * deltaTime;
            }
        }

        /// <summary>
        /// 当城市升级时，检查是否需要生成区划
        /// </summary>
        public static void CheckDistrictGeneration(BurgData burg)
        {
            if (burg == null) return;
            if (burg.districtsGenerated) return;

            // 达到Ⅳ级都会时生成区划
            if (burg.settlementLevel >= SettlementLevel.LevelIV)
            {
                GenerateDistricts(burg);
            }
        }

        /// <summary>
        /// 将经济成分映射为区划类型
        /// </summary>
        private static CityDistrictType MapSectorToDistrictType(EconomicSector sector)
        {
            return sector switch
            {
                EconomicSector.Commerce => CityDistrictType.Commercial,
                EconomicSector.Artisanal => CityDistrictType.Artisanal,
                EconomicSector.Military => CityDistrictType.Military,
                EconomicSector.Administrative => CityDistrictType.Administrative,
                EconomicSector.Religious => CityDistrictType.Religious,
                EconomicSector.Cultural => CityDistrictType.Cultural,
                EconomicSector.Transportation => CityDistrictType.Port,
                EconomicSector.Agriculture => CityDistrictType.Agricultural,
                EconomicSector.Fishery => CityDistrictType.Port,
                EconomicSector.Mining => CityDistrictType.Artisanal,
                _ => CityDistrictType.None
            };
        }

        /// <summary>
        /// 获取区划名称
        /// </summary>
        private static string GetDistrictName(CityDistrictType type, int index)
        {
            string baseName = type switch
            {
                CityDistrictType.Commercial => "商业区",
                CityDistrictType.Artisanal => "手工业区",
                CityDistrictType.Military => "军事区",
                CityDistrictType.Noble => "贵族区",
                CityDistrictType.Administrative => "行政区",
                CityDistrictType.Religious => "宗教区",
                CityDistrictType.Port => "港口区",
                CityDistrictType.Slum => "平民区",
                CityDistrictType.Cultural => "文教区",
                CityDistrictType.Agricultural => "农业区",
                _ => "未知区"
            };

            // 如果有多个同类型区划，添加序号
            if (index > 0)
                return $"{baseName}{index + 1}";

            return baseName;
        }

        /// <summary>
        /// 获取区划对聚居点的影响（经济产出、驻军上限、税收、行政效率等）
        /// 分级计算：
        /// - 村落（Ⅰ级）和集镇（Ⅱ级）：极简计算，主要看自然资源和简单加工
        /// - 城邑（Ⅲ级）：中等复杂度计算，包括正常的贸易和税收
        /// - 都会（Ⅳ级）和大都会（Ⅴ级）：使用区划系统的复杂计算
        /// </summary>
        public static DistrictEffects GetDistrictEffects(BurgData burg)
        {
            var effects = new DistrictEffects();

            if (burg == null)
                return effects;

            // 都会（Ⅳ级）和大都会（Ⅴ级）：使用区划系统的复杂计算
            if (burg.districtsGenerated && burg.districts != null && burg.districts.Count > 0)
            {
                GetDistrictEconomicEffects(burg, effects);
                return effects;
            }

            // 城邑（Ⅲ级）：中等复杂度计算，包括正常的贸易和税收
            if (burg.settlementLevel == SettlementLevel.LevelIII)
            {
                GetCityEconomicEffects(burg, effects);
                return effects;
            }

            // 村落（Ⅰ级）和集镇（Ⅱ级）：极简计算，主要看自然资源和简单加工
            GetVillageEconomicEffects(burg, effects);
            return effects;
        }

        /// <summary>
        /// 区划系统的复杂影响计算（都会/大都会）
        /// 每种区划类型有不同的加成效果
        /// </summary>
        private static void GetDistrictEconomicEffects(BurgData burg, DistrictEffects effects)
        {

            foreach (var district in burg.districts)
            {
                float devFactor = district.development / 100f;
                float areaFactor = district.areaRatio;
                float effectiveFactor = devFactor * areaFactor;

                switch (district.districtType)
                {
                    case CityDistrictType.Commercial:
                        // 商业区：增加贸易收入、商业税收、市场吸引力
                        effects.tradeIncome += effectiveFactor * 0.5f;
                        effects.commerceTax += effectiveFactor * 0.4f;
                        effects.marketAttraction += effectiveFactor * 0.3f;
                        effects.populationCapacity += effectiveFactor * 0.2f;
                        break;

                    case CityDistrictType.Artisanal:
                        // 手工业区：增加手工业产出、手工业税收、就业
                        effects.artisanalOutput += effectiveFactor * 0.5f;
                        effects.artisanalTax += effectiveFactor * 0.3f;
                        effects.employment += effectiveFactor * 0.4f;
                        effects.populationCapacity += effectiveFactor * 0.3f;
                        break;

                    case CityDistrictType.Military:
                        // 军事区：增加驻军上限、防御加成、训练效率
                        effects.garrisonCapacity += effectiveFactor * 0.6f;
                        effects.defenseBonus += effectiveFactor * 0.4f;
                        effects.trainingEfficiency += effectiveFactor * 0.3f;
                        effects.unrest += effectiveFactor * 0.1f; // 军事区可能增加不安
                        break;

                    case CityDistrictType.Noble:
                        // 贵族区：增加贵族满意度、税收、文化产出
                        effects.nobleApproval += effectiveFactor * 0.5f;
                        effects.nobleTax += effectiveFactor * 0.4f;
                        effects.culturalOutput += effectiveFactor * 0.2f;
                        effects.populationCapacity += effectiveFactor * 0.1f;
                        break;

                    case CityDistrictType.Administrative:
                        // 行政区：增加行政效率、税收效率、秩序
                        effects.administrativeEfficiency += effectiveFactor * 0.5f;
                        effects.taxEfficiency += effectiveFactor * 0.4f;
                        effects.order += effectiveFactor * 0.3f;
                        effects.unrest -= effectiveFactor * 0.2f;
                        break;

                    case CityDistrictType.Religious:
                        // 宗教区：增加宗教满意度、宗教税收、稳定
                        effects.religiousApproval += effectiveFactor * 0.5f;
                        effects.religiousTax += effectiveFactor * 0.3f;
                        effects.stability += effectiveFactor * 0.3f;
                        effects.unrest -= effectiveFactor * 0.15f;
                        break;

                    case CityDistrictType.Port:
                        // 港口区：增加贸易收入、交通效率、渔业产出
                        effects.tradeIncome += effectiveFactor * 0.6f;
                        effects.transportEfficiency += effectiveFactor * 0.5f;
                        effects.fisheryOutput += effectiveFactor * 0.3f;
                        effects.populationCapacity += effectiveFactor * 0.2f;
                        break;

                    case CityDistrictType.Slum:
                        // 贫民区：增加人口容量，但降低满意度和税收
                        effects.populationCapacity += effectiveFactor * 0.8f;
                        effects.unrest += effectiveFactor * 0.3f;
                        effects.publicHealth -= effectiveFactor * 0.2f;
                        effects.taxEfficiency -= effectiveFactor * 0.1f;
                        break;

                    case CityDistrictType.Cultural:
                        // 文教区：增加文化产出、人才培养、创新
                        effects.culturalOutput += effectiveFactor * 0.6f;
                        effects.innovationBonus += effectiveFactor * 0.4f;
                        effects.education += effectiveFactor * 0.5f;
                        effects.populationCapacity += effectiveFactor * 0.15f;
                        break;

                    case CityDistrictType.Agricultural:
                        // 农业区：增加农业产出、粮食储备
                        effects.agriculturalOutput += effectiveFactor * 0.6f;
                        effects.foodStorage += effectiveFactor * 0.4f;
                        effects.populationCapacity += effectiveFactor * 0.2f;
                        break;
                }
            }
        }

        /// <summary>
        /// 城邑（Ⅲ级）的中等复杂度经济影响计算
        /// 包括正常的贸易和税收，经济结构已经比较复杂
        /// </summary>
        private static void GetCityEconomicEffects(BurgData burg, DistrictEffects effects)
        {
            if (burg.economicComposition == null || burg.economicComposition.Count == 0)
                return;

            var composition = burg.economicComposition;

            // 农业：农业产出和粮食储备
            if (composition.ContainsKey(EconomicSector.Agriculture))
            {
                float ratio = composition[EconomicSector.Agriculture];
                effects.agriculturalOutput += ratio * 0.5f;
                effects.foodStorage += ratio * 0.3f;
                effects.populationCapacity += ratio * 0.2f;
            }

            // 渔业：渔业产出
            if (composition.ContainsKey(EconomicSector.Fishery))
            {
                float ratio = composition[EconomicSector.Fishery];
                effects.fisheryOutput += ratio * 0.4f;
                effects.foodStorage += ratio * 0.2f;
            }

            // 矿业：矿业产出和就业
            if (composition.ContainsKey(EconomicSector.Mining))
            {
                float ratio = composition[EconomicSector.Mining];
                effects.artisanalOutput += ratio * 0.3f;
                effects.employment += ratio * 0.3f;
            }

            // 手工业：手工业产出和税收（加工品，城市的重要经济支柱）
            if (composition.ContainsKey(EconomicSector.Artisanal))
            {
                float ratio = composition[EconomicSector.Artisanal];
                effects.artisanalOutput += ratio * 0.5f;
                effects.artisanalTax += ratio * 0.35f;
                effects.employment += ratio * 0.4f;
                effects.populationCapacity += ratio * 0.25f;
            }

            // 商业：贸易收入和商业税收（城市的核心功能）
            if (composition.ContainsKey(EconomicSector.Commerce))
            {
                float ratio = composition[EconomicSector.Commerce];
                effects.tradeIncome += ratio * 0.5f;
                effects.commerceTax += ratio * 0.4f;
                effects.marketAttraction += ratio * 0.3f;
                effects.populationCapacity += ratio * 0.2f;
            }

            // 交通：交通效率
            if (composition.ContainsKey(EconomicSector.Transportation))
            {
                float ratio = composition[EconomicSector.Transportation];
                effects.transportEfficiency += ratio * 0.4f;
                effects.tradeIncome += ratio * 0.1f; // 交通促进贸易
            }

            // 行政：行政效率和税收效率
            if (composition.ContainsKey(EconomicSector.Administrative))
            {
                float ratio = composition[EconomicSector.Administrative];
                effects.administrativeEfficiency += ratio * 0.35f;
                effects.taxEfficiency += ratio * 0.3f;
                effects.order += ratio * 0.2f;
            }

            // 军事：驻军上限和防御
            if (composition.ContainsKey(EconomicSector.Military))
            {
                float ratio = composition[EconomicSector.Military];
                effects.garrisonCapacity += ratio * 0.4f;
                effects.defenseBonus += ratio * 0.3f;
                effects.trainingEfficiency += ratio * 0.2f;
            }

            // 宗教：宗教满意度和税收
            if (composition.ContainsKey(EconomicSector.Religious))
            {
                float ratio = composition[EconomicSector.Religious];
                effects.religiousApproval += ratio * 0.3f;
                effects.religiousTax += ratio * 0.25f;
                effects.stability += ratio * 0.2f;
            }

            // 文化：文化产出和教育
            if (composition.ContainsKey(EconomicSector.Cultural))
            {
                float ratio = composition[EconomicSector.Cultural];
                effects.culturalOutput += ratio * 0.35f;
                effects.education += ratio * 0.3f;
                effects.innovationBonus += ratio * 0.2f;
            }
        }

        /// <summary>
        /// 村落（Ⅰ级）和集镇（Ⅱ级）的极简经济影响计算
        /// 主要看自然资源（农业/渔业/矿业）和简单加工（手工业）
        /// 贸易和税收很少，因为还没有形成真正的城市经济
        /// </summary>
        private static void GetVillageEconomicEffects(BurgData burg, DistrictEffects effects)
        {
            if (burg.economicComposition == null || burg.economicComposition.Count == 0)
                return;

            var composition = burg.economicComposition;

            // 农业：农业产出和粮食储备（村落的核心经济）
            if (composition.ContainsKey(EconomicSector.Agriculture))
            {
                float ratio = composition[EconomicSector.Agriculture];
                effects.agriculturalOutput += ratio * 0.7f;
                effects.foodStorage += ratio * 0.5f;
                effects.populationCapacity += ratio * 0.3f;
            }

            // 渔业：渔业产出
            if (composition.ContainsKey(EconomicSector.Fishery))
            {
                float ratio = composition[EconomicSector.Fishery];
                effects.fisheryOutput += ratio * 0.6f;
                effects.foodStorage += ratio * 0.3f;
            }

            // 矿业：矿业产出（简单开采，没有深加工）
            if (composition.ContainsKey(EconomicSector.Mining))
            {
                float ratio = composition[EconomicSector.Mining];
                effects.artisanalOutput += ratio * 0.2f; // 简单的初级加工
                effects.employment += ratio * 0.2f;
            }

            // 手工业：简单加工品（村落级别的手工业很初级）
            if (composition.ContainsKey(EconomicSector.Artisanal))
            {
                float ratio = composition[EconomicSector.Artisanal];
                effects.artisanalOutput += ratio * 0.3f;
                effects.employment += ratio * 0.2f;
                // 村落的手工业税收很少，不计算
            }

            // 商业：少量的本地交换（村落没有真正的商业和贸易）
            if (composition.ContainsKey(EconomicSector.Commerce))
            {
                float ratio = composition[EconomicSector.Commerce];
                effects.tradeIncome += ratio * 0.15f; // 只有少量的本地贸易
                // 村落的商业税收很少，不计算
            }

            // 其他成分（交通、行政、军事、宗教、文化）在村落级别影响很小，不计算
        }
    }

    /// <summary>
    /// 区划对城市的影响数据
    /// 各种区划类型对城市有不同的加成效果
    /// </summary>
    public class DistrictEffects
    {
        // 经济产出
        public float tradeIncome = 0f;           // 贸易收入加成
        public float artisanalOutput = 0f;       // 手工业产出加成
        public float agriculturalOutput = 0f;    // 农业产出加成
        public float fisheryOutput = 0f;         // 渔业产出加成
        public float culturalOutput = 0f;        // 文化产出加成

        // 税收
        public float commerceTax = 0f;           // 商业税收加成
        public float artisanalTax = 0f;          // 手工业税收加成
        public float nobleTax = 0f;               // 贵族税收加成
        public float religiousTax = 0f;           // 宗教税收加成
        public float taxEfficiency = 0f;          // 税收效率加成

        // 军事
        public float garrisonCapacity = 0f;       // 驻军上限加成
        public float defenseBonus = 0f;           // 防御加成
        public float trainingEfficiency = 0f;     // 训练效率加成

        // 行政
        public float administrativeEfficiency = 0f; // 行政效率加成
        public float order = 0f;                  // 秩序加成
        public float stability = 0f;              // 稳定加成

        // 社会
        public float nobleApproval = 0f;          // 贵族满意度
        public float religiousApproval = 0f;      // 宗教满意度
        public float unrest = 0f;                 // 不安（负值为减少不安）
        public float publicHealth = 0f;           // 公共卫生
        public float employment = 0f;             // 就业
        public float education = 0f;              // 教育

        // 其他
        public float populationCapacity = 0f;     // 人口容量加成
        public float marketAttraction = 0f;       // 市场吸引力
        public float transportEfficiency = 0f;    // 交通效率
        public float foodStorage = 0f;            // 粮食储备
        public float innovationBonus = 0f;        // 创新加成
    }
}
