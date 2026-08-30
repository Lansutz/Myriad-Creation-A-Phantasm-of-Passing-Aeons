using System.Collections.Generic;
using CivilizationEvolution.Core;
using UnityEngine;

namespace CivilizationEvolution.Map
{
    /// <summary>
    /// 聚落AI建造决策系统
    /// 负责AI政权的聚落建造决策：民用聚落自然生长、关口堡战略投资、4种区域堡寨、港口渡口附属设施
    /// 综合：用户设计文档（AI建造逻辑）+ 文明引擎IN模块（关隘瓶颈/要塞体系/城镇功能）
    /// </summary>
    public static class SettlementAISystem
    {
        // ===== 地区城镇容量硬上限 =====

        /// <summary>每地区最大聚落数（按地区面积和承载力计算）</summary>
        public const int BaseSettlementCapacityPerRegion = 8;

        /// <summary>每万人口最大聚落数</summary>
        public const float SettlementPer10kPopulation = 0.5f;

        /// <summary>
        /// 计算地区城镇容量硬上限
        /// </summary>
        public static int CalculateRegionCapacity(int regionTileCount, float regionPopulation, float foodSurplus)
        {
            int byArea = Mathf.CeilToInt(regionTileCount / 500f * BaseSettlementCapacityPerRegion);
            int byPopulation = Mathf.CeilToInt(regionPopulation / 10000f * SettlementPer10kPopulation);
            int byFood = foodSurplus > 0 ? Mathf.CeilToInt(foodSurplus / 100f) : 0;

            return Mathf.Max(2, Mathf.Min(byArea, byPopulation + byFood + 2));
        }

        // ===== 民用聚落（村镇→城）AI建造 =====

        /// <summary>
        /// AI决策：是否新建民用聚落（村镇）
        /// 触发条件：地块肥力高、商路流量大、人口压力大、区域粮食承载力充足
        /// </summary>
        public static AIBuildDecision ShouldBuildVillage(TileData tile, float regionPopulationPressure,
            float tradeRouteFlow, float foodCapacity, int currentSettlementCount, int regionCapacity)
        {
            var decision = new AIBuildDecision { shouldBuild = false, priority = 0f, reason = "" };

            // 容量硬上限
            if (currentSettlementCount >= regionCapacity)
            {
                decision.reason = "地区城镇容量已满";
                return decision;
            }

            // 必须是陆地且非山地
            if (!tile.isLand || tile.elevation01 > 0.7f)
            {
                decision.reason = "地形不适宜建村";
                return decision;
            }

            // 地块肥力评分（降水+温度+地形）
            float fertilityScore = CalculateFertilityScore(tile);
            if (fertilityScore < 0.3f)
            {
                decision.reason = "地块肥力不足";
                return decision;
            }

            // 人口压力评分
            float pressureScore = Mathf.Clamp01(regionPopulationPressure / 100f);

            // 商路流量评分
            float tradeScore = Mathf.Clamp01(tradeRouteFlow / 100f);

            // 粮食承载力评分
            float foodScore = Mathf.Clamp01(foodCapacity / 100f);

            // 综合优先级
            decision.priority = fertilityScore * 0.4f + pressureScore * 0.25f +
                                tradeScore * 0.2f + foodScore * 0.15f;

            // 决策阈值
            if (decision.priority > 0.5f)
            {
                decision.shouldBuild = true;
                decision.reason = $"肥力{fertilityScore:0.00} 人口压力{pressureScore:0.00} 商路{tradeScore:0.00}";
            }

            return decision;
        }

        /// <summary>计算地块肥力评分</summary>
        public static float CalculateFertilityScore(TileData tile)
        {
            // 降水评分（300-1500mm最佳）
            float precipScore = tile.annualPrecipMm switch
            {
                < 200f => 0.1f,
                < 400f => 0.4f,
                < 800f => 0.8f,
                < 1500f => 1.0f,
                < 2500f => 0.7f,
                _ => 0.5f
            };

            // 温度评分（10-25°C最佳）
            float tempScore = Mathf.Clamp01(1f - Mathf.Abs(tile.annualTemp - 18f) / 20f);

            // 地形评分（平原最佳，山地最差）
            float terrainScore = tile.elevation01 switch
            {
                < 0.3f => 1.0f,
                < 0.5f => 0.8f,
                < 0.65f => 0.5f,
                < 0.8f => 0.2f,
                _ => 0.05f
            };

            // 河流加成
            float riverBonus = tile.isRiver ? 0.15f : 0f;

            // 海岸加成（渔业）
            float coastBonus = tile.isCoast ? 0.1f : 0f;

            return Mathf.Clamp01(precipScore * 0.4f + tempScore * 0.25f +
                                  terrainScore * 0.25f + riverBonus + coastBonus);
        }

        // ===== 关口堡（Pass）AI建造 =====

        /// <summary>
        /// AI决策：是否修建关口堡
        /// 触发条件：地形瓶颈+商路必经+敌方压力
        /// </summary>
        public static AIBuildDecision ShouldBuildPassFort(TileData tile, BottleneckType bottleneck,
            float tradeRouteImportance, float enemyPressure, bool hasEnemyOnOtherSide,
            int currentFortCount, int maxForts)
        {
            var decision = new AIBuildDecision { shouldBuild = false, priority = 0f, reason = "" };

            // 必须是瓶颈节点
            if (bottleneck == BottleneckType.None)
            {
                decision.reason = "非瓶颈节点";
                return decision;
            }

            // 堡垒数量上限
            if (currentFortCount >= maxForts)
            {
                decision.reason = "堡垒数量已达上限";
                return decision;
            }

            // 瓶颈战略价值评分
            float bottleneckScore = bottleneck switch
            {
                BottleneckType.StraitCrossing => 0.9f,
                BottleneckType.MountainPass => 0.85f,
                BottleneckType.CanyonPass => 0.8f,
                BottleneckType.RiverFording => 0.6f,
                BottleneckType.DesertCorridor => 0.5f,
                BottleneckType.IsthmusPass => 0.9f,
                _ => 0.3f
            };

            // 商路重要性
            float tradeScore = Mathf.Clamp01(tradeRouteImportance / 100f);

            // 敌方压力
            float enemyScore = hasEnemyOnOtherSide ? Mathf.Clamp01(enemyPressure / 100f) : 0.1f;

            // 综合优先级
            decision.priority = bottleneckScore * 0.5f + tradeScore * 0.25f + enemyScore * 0.25f;

            if (decision.priority > 0.55f)
            {
                decision.shouldBuild = true;
                decision.fortSubtype = FortSubtype.PassFort;
                decision.reason = $"瓶颈{bottleneck} 商路{tradeScore:0.00} 敌方压力{enemyScore:0.00}";
            }

            return decision;
        }

        // ===== 4种区域堡寨AI建造 =====

        /// <summary>
        /// AI决策：是否修建高地堡（HighlandKeep）
        /// 触发：边境或易遭袭扰的丘陵高地；构建区域压制网
        /// </summary>
        public static AIBuildDecision ShouldBuildHighlandKeep(TileData tile, bool isBorderRegion,
            float raidRisk, float existingFortCoverage, int currentFortCount, int maxForts)
        {
            var decision = new AIBuildDecision { shouldBuild = false, priority = 0f, reason = "" };

            if (currentFortCount >= maxForts) { decision.reason = "堡垒上限"; return decision; }

            // 必须是丘陵高地
            if (tile.elevation01 < 0.5f || tile.elevation01 > 0.8f)
            {
                decision.reason = "非丘陵高地";
                return decision;
            }

            float heightScore = Mathf.Clamp01((tile.elevation01 - 0.5f) / 0.3f);
            float borderScore = isBorderRegion ? 0.8f : 0.3f;
            float raidScore = Mathf.Clamp01(raidRisk / 100f);
            float coverageGap = 1f - Mathf.Clamp01(existingFortCoverage);

            decision.priority = heightScore * 0.3f + borderScore * 0.3f +
                                raidScore * 0.25f + coverageGap * 0.15f;

            if (decision.priority > 0.5f)
            {
                decision.shouldBuild = true;
                decision.fortSubtype = FortSubtype.HighlandKeep;
                decision.reason = $"高地{heightScore:0.00} 边境{borderScore:0.00} 袭扰{raidScore:0.00}";
            }

            return decision;
        }

        /// <summary>
        /// AI决策：是否修建坞堡庄园（ManorFort）
        /// 触发：内地农耕区域，人口扩散，地方庄园势力兴起
        /// </summary>
        public static AIBuildDecision ShouldBuildManorFort(TileData tile, float regionPopulation,
            float localPower, float centralControl, int currentManorCount, int maxManors)
        {
            var decision = new AIBuildDecision { shouldBuild = false, priority = 0f, reason = "" };

            if (currentManorCount >= maxManors) { decision.reason = "坞堡上限"; return decision; }

            // 必须是肥沃农耕区
            float fertility = CalculateFertilityScore(tile);
            if (fertility < 0.5f) { decision.reason = "非农耕区"; return decision; }

            float popScore = Mathf.Clamp01(regionPopulation / 50000f);
            float powerScore = Mathf.Clamp01(localPower / 100f);
            float controlGap = 1f - Mathf.Clamp01(centralControl / 100f); // 中央控制弱→地方势力强

            decision.priority = fertility * 0.35f + popScore * 0.25f +
                                powerScore * 0.2f + controlGap * 0.2f;

            if (decision.priority > 0.55f)
            {
                decision.shouldBuild = true;
                decision.fortSubtype = FortSubtype.ManorFort;
                decision.reason = $"肥力{fertility:0.00} 人口{popScore:0.00} 地方势力{powerScore:0.00}";
            }

            return decision;
        }

        /// <summary>
        /// AI决策：是否修建平原屯堡（PlainGarrison）
        /// 触发：开阔平原边境，无天然山地隘口，沿边境线构筑防御带
        /// </summary>
        public static AIBuildDecision ShouldBuildPlainGarrison(TileData tile, bool isBorder,
            float enemyThreat, float terrainOpenness, int currentGarrisonCount, int maxGarrisons)
        {
            var decision = new AIBuildDecision { shouldBuild = false, priority = 0f, reason = "" };

            if (currentGarrisonCount >= maxGarrisons) { decision.reason = "屯堡上限"; return decision; }

            // 必须是开阔平原
            if (tile.elevation01 > 0.4f || tile.slopeDegree > 5f)
            {
                decision.reason = "非开阔平原";
                return decision;
            }

            float openScore = Mathf.Clamp01(terrainOpenness / 100f);
            float borderScore = isBorder ? 0.9f : 0.1f;
            float threatScore = Mathf.Clamp01(enemyThreat / 100f);

            decision.priority = openScore * 0.3f + borderScore * 0.4f + threatScore * 0.3f;

            if (decision.priority > 0.6f)
            {
                decision.shouldBuild = true;
                decision.fortSubtype = FortSubtype.PlainGarrison;
                decision.reason = $"平原{openScore:0.00} 边境{borderScore:0.00} 威胁{threatScore:0.00}";
            }

            return decision;
        }

        /// <summary>
        /// AI决策：是否修建河口堡（EstuaryFort）
        /// 触发：大河河口、干流要道，管控内河与近海航线
        /// </summary>
        public static AIBuildDecision ShouldBuildEstuaryFort(TileData tile, bool isRiverMouth,
            float navalThreat, float tradeValue, int currentEstuaryCount, int maxEstuaries)
        {
            var decision = new AIBuildDecision { shouldBuild = false, priority = 0f, reason = "" };

            if (currentEstuaryCount >= maxEstuaries) { decision.reason = "河口堡上限"; return decision; }

            // 必须是河口（河流+海岸）
            if (!isRiverMouth || !tile.isCoast || !tile.isRiver)
            {
                decision.reason = "非河口位置";
                return decision;
            }

            float riverScore = tile.isRiver ? 0.8f : 0.3f;
            float coastScore = tile.isCoast ? 0.8f : 0.2f;
            float navalScore = Mathf.Clamp01(navalThreat / 100f);
            float tradeScore = Mathf.Clamp01(tradeValue / 100f);

            decision.priority = (riverScore + coastScore) * 0.25f +
                                navalScore * 0.3f + tradeScore * 0.2f;

            if (decision.priority > 0.55f)
            {
                decision.shouldBuild = true;
                decision.fortSubtype = FortSubtype.EstuaryFort;
                decision.reason = $"河口 海军威胁{navalScore:0.00} 贸易{tradeScore:0.00}";
            }

            return decision;
        }

        // ===== 港口/渡口（附属设施）AI建造 =====

        /// <summary>
        /// AI决策：是否在已有聚落升级港口
        /// </summary>
        public static AIBuildDecision ShouldUpgradePort(BurgData burg, float tradeDemand,
            float navalNeed, PortTier currentTier)
        {
            var decision = new AIBuildDecision { shouldBuild = false, priority = 0f, reason = "" };

            // 必须沿海或沿河
            if (!burg.isCoastal && !burg.isPort)
            {
                decision.reason = "非沿海/沿河聚落";
                return decision;
            }

            // 已达最高级
            if (currentTier >= PortTier.ImperialPort)
            {
                decision.reason = "已达最高港口等级";
                return decision;
            }

            float demandScore = Mathf.Clamp01(tradeDemand / 100f);
            float navalScore = Mathf.Clamp01(navalNeed / 100f);
            float existingBonus = burg.portTier != PortTier.None ? 0.2f : 0f;

            decision.priority = demandScore * 0.5f + navalScore * 0.3f + existingBonus;

            if (decision.priority > 0.5f)
            {
                decision.shouldBuild = true;
                decision.reason = $"贸易需求{demandScore:0.00} 海军需求{navalScore:0.00}";
            }

            return decision;
        }

        /// <summary>
        /// AI决策：是否在空白地块修建简易码头/渡口
        /// </summary>
        public static AIBuildDecision ShouldBuildFerry(TileData tile, float crossingDemand,
            bool hasSettlementNearby, int currentFerryCount, int maxFerries)
        {
            var decision = new AIBuildDecision { shouldBuild = false, priority = 0f, reason = "" };

            if (currentFerryCount >= maxFerries) { decision.reason = "渡口中限"; return decision; }

            // 必须是河流或海岸
            if (!tile.isRiver && !tile.isCoast)
            {
                decision.reason = "非水域边缘";
                return decision;
            }

            float demandScore = Mathf.Clamp01(crossingDemand / 100f);
            float settlementBonus = hasSettlementNearby ? 0.3f : 0f;

            decision.priority = demandScore * 0.6f + settlementBonus;

            if (decision.priority > 0.45f)
            {
                decision.shouldBuild = true;
                decision.reason = $"渡河需求{demandScore:0.00}";
            }

            return decision;
        }
    }

    /// <summary>
    /// AI建造决策结果
    /// </summary>
    public struct AIBuildDecision
    {
        public bool shouldBuild;
        public float priority;
        public string reason;
        public FortSubtype fortSubtype;
        public PortTier targetPortTier;
    }
}
