using System.Collections.Generic;

using UnityEngine;
using MyriadCreation.Core;
using MyriadCreation.Core.Constants;
using MyriadCreation.Core.Data;
using MyriadCreation.Core.Dto;
using MyriadCreation.Core.Enums;


namespace MyriadCreation.World.Settlement
{
 /// 聚落类型学系统
 /// 负责：类型推导（地形/位置/资源→形态功能）、升级路线管理、形态约束、城形/堡型选择
 /// 综合：文档 + 文明引擎IN模块（港口层级/关隘瓶颈/要塞体系/城镇功能类型学）
    public static class SettlementTypologySystem
    {
 // ===== 类型推导：根据地块特征推导聚落初始形态 =====
 /// 根据地块特征推导聚落初始类型
        public static void DeriveInitialType(SettlementData settlement, TileData tile, int width, int height)
        {
            if (settlement == null) return;

 // 1. 判定是否为瓶颈节点
            settlement.bottleneckType = DetectBottleneck(tile, width, height);

 // 2. 判定港口层级
            settlement.portTier = DetectPortTier(tile);

 // 3. 推导主功能
            settlement.primaryFunction = DerivePrimaryFunction(tile, settlement);

 // 4. 推导形态（村镇/城/堡）
            settlement.settlementForm = DeriveSettlementForm(tile, settlement);

 // 5. 推导城形/堡型
            if (settlement.settlementForm == SettlementForm.City)
            {
                settlement.cityForm = DeriveCityForm(tile, settlement);
                settlement.fortSubtype = FortSubtype.RoyalCastle; // 默认
            }
            else if (settlement.siteMode == SiteMode.FortifiedOutpost)
            {
                settlement.fortSubtype = DeriveFortSubtype(tile, settlement);
                settlement.cityForm = CityForm.CitadelCity;
            }
            else
            {
                settlement.cityForm = CityForm.WalledTown;
                settlement.fortSubtype = FortSubtype.BorderFort;
            }

 // 6. 推导升级路线
            settlement.upgradePath = DeriveUpgradePath(tile, settlement);

 // 7. 初始等级
            settlement.settlementLevel = SettlementLevel.LevelI;
            settlement.evolutionStage = EvolutionStage.Stable;
            settlement.wallLevel = settlement.siteMode == SiteMode.FortifiedOutpost ? WallLevel.Palisade : WallLevel.None;

 // 7.5 聚居点分类推导
            if (settlement.siteMode != SiteMode.SedentarySettlement
                && settlement.siteMode != SiteMode.FortifiedOutpost)
                throw new System.InvalidOperationException(
                    $"Typology cannot classify site mode '{SiteModeKeys.ToKey(settlement.siteMode)}' as a settled site.");
            settlement.SetLegacyCategoryFromSiteMode();

 // 7.6 建设度初始化（每个等级内分五档，0~100，每20一档）
            settlement.constructionProgress = 0f;
            settlement.constructionTier = 1;

 // 8. 城市重心（低级聚落默认均衡，升级后再确定）
            settlement.cityFocus = CityFocus.Balanced;
        }

 /// <summary>建设度增长（每日调用，由发展度/人口/投资驱动）</summary>
        public static void UpdateConstructionProgress(SettlementData settlement, float dailyDevelopmentGain)
        {
            if (settlement == null) return;
            if (settlement.settlementLevel >= SettlementLevel.LevelV) return; // 最高等级不再增长建设度

            // 建设度增长（简化：发展度增益直接转化为建设度，后续可细化）
            settlement.constructionProgress = Mathf.Min(100f, settlement.constructionProgress + dailyDevelopmentGain);

            // 更新建设度档位（每20一档，1~5档）
            settlement.constructionTier = Mathf.Clamp(Mathf.FloorToInt(settlement.constructionProgress / 20f) + 1, 1, 5);
        }

 /// <summary>检查建设度是否达到升级条件（第5档即满100）</summary>
        public static bool IsConstructionReady(SettlementData settlement)
        {
            return settlement != null && settlement.constructionTier >= 5;
        }

 /// <summary>检测瓶颈节点类型</summary>
        private static BottleneckType DetectBottleneck(TileData tile, int width, int height)
        {
            int x = tile.tileIndex % width;
            int y = tile.tileIndex / width;

 // 海峡/渡口：沿海且两侧有陆地
            if (tile.isCoast || tile.oceanTier == GameEnums.OceanTier.Coast)
            {
 // 检查是否为狭窄水道
                bool hasLandLeft = HasLandInDirection(tile, width, height, -1, 0, 3);
                bool hasLandRight = HasLandInDirection(tile, width, height, 1, 0, 3);
                if (hasLandLeft && hasLandRight) return BottleneckType.StraitCrossing;
            }

 // 山地隘口：高海拔且周围有低海拔通道
            if (tile.elevation01 > 0.6f && tile.isLand)
            {
                for (int dy = -1; dy <= 1; dy++)
                {
                    for (int dx = -1; dx <= 1; dx++)
                    {
                        if (dx == 0 && dy == 0) continue;
                        int nx = x + dx, ny = y + dy;
                        if (nx < 0 || nx >= width || ny < 0 || ny >= height) continue;
                        int ni = ny * width + nx;
 // 假设tiles可访问，这里简化
                    }
                }
                if (tile.slopeDegree > 20f) return BottleneckType.MountainPass;
            }

 // 峡谷：河流+高坡度
            if (tile.isRiver && tile.slopeDegree > 15f)
                return BottleneckType.CanyonPass;

 // 河流浅滩
            if (tile.isRiver && tile.elevation01 < 0.4f)
                return BottleneckType.RiverFording;

 // 沙漠走廊：干旱+有水源
            if (tile.annualPrecipMm < 300f && tile.isRiver)
                return BottleneckType.DesertCorridor;

            return BottleneckType.None;
        }

        private static bool HasLandInDirection(TileData tile, int width, int height, int dx, int dy, int dist)
        {
 // 简化：假设周围有陆地
            return true;
        }

 /// <summary>检测港口层级</summary>
        private static PortTier DetectPortTier(TileData tile)
        {
            if (!tile.isCoast && !tile.isRiver) return PortTier.None;

 // 沿海+深水（高坡度海岸）= 深水港潜力
            if (tile.isCoast && tile.slopeDegree > 10f)
                return PortTier.DeepWaterPort;

 // 沿海+平缓 = 中转港
            if (tile.isCoast)
                return PortTier.IntermediatePort;

 // 河流+通航 = 内河港
            if (tile.isRiver)
                return PortTier.RiverPort;

            return PortTier.Anchorage;
        }

 /// <summary>推导主功能</summary>
        private static SettlementFunction DerivePrimaryFunction(TileData tile, SettlementData settlement)
        {
 // 瓶颈节点 → 渡口/关税/军事
            if (settlement.bottleneckType != BottleneckType.None)
            {
                if (settlement.bottleneckType == BottleneckType.StraitCrossing ||
                    settlement.bottleneckType == BottleneckType.RiverFording)
                    return SettlementFunction.Crossing;
                return SettlementFunction.Military;
            }

 // 港口 → 商业
            if (settlement.portTier >= PortTier.IntermediatePort)
                return SettlementFunction.Commercial;

 // 高坡度+山地 → 矿业
            if (tile.elevation01 > 0.65f && tile.slopeDegree > 15f)
                return SettlementFunction.Mining;

 // 干旱+绿洲 → 农业（绿洲农业）
            if (tile.annualPrecipMm < 300f && tile.isRiver)
                return SettlementFunction.Agricultural;

 // 默认 → 农业
            return SettlementFunction.Agricultural;
        }

 /// <summary>推导聚落形态</summary>
        private static SettlementForm DeriveSettlementForm(TileData tile, SettlementData settlement)
        {
 // 瓶颈+军事功能 → 堡
            if (settlement.bottleneckType != BottleneckType.None &&
                settlement.primaryFunction == SettlementFunction.Military)
            {
                if (!HabitationSiteTransitionSystem.TryTransition(
                    settlement, SiteMode.FortifiedOutpost, out var failure))
                    throw new System.InvalidOperationException(
                        $"Cannot promote site to outpost during typology: {failure}.");
                return SettlementForm.Village;
            }

 // 港口+商业 → 城
            if (settlement.portTier >= PortTier.IntermediatePort &&
                settlement.primaryFunction == SettlementFunction.Commercial)
                return SettlementForm.City;

 // 矿业 → 村镇（矿业村镇，后期可升级为城）
            if (settlement.primaryFunction == SettlementFunction.Mining)
                return SettlementForm.Village;

 // 默认 → 村镇
            return SettlementForm.Village;
        }

 /// <summary>推导城的形态</summary>
        private static CityForm DeriveCityForm(TileData tile, SettlementData settlement)
        {
 // 港口城市 → 不规则形态（沿河/沿海城市通常受地形约束，有机生长）
            if (settlement.portTier >= PortTier.RiverPort)
                return CityForm.Irregular;

 // 山地城市 → 山城
            if (tile.elevation01 > 0.6f)
                return CityForm.MountainCity;

 // 平原+行政 → 方城
            if (settlement.primaryFunction == SettlementFunction.Administrative &&
                tile.elevation01 < 0.4f)
                return CityForm.Square;

 // 默认 → 圆城
            return CityForm.Circular;
        }

 /// <summary>推导堡垒亚型</summary>
        private static FortSubtype DeriveFortSubtype(TileData tile, SettlementData settlement)
        {
 // 关口堡
            if (settlement.bottleneckType == BottleneckType.MountainPass ||
                settlement.bottleneckType == BottleneckType.CanyonPass)
                return FortSubtype.PassFort;

 // 河口堡
            if (settlement.bottleneckType == BottleneckType.StraitCrossing ||
                (settlement.portTier >= PortTier.RiverPort && tile.isCoast))
                return FortSubtype.EstuaryFort;

 // 高地堡
            if (tile.elevation01 > 0.6f && tile.isLand)
                return FortSubtype.HighlandKeep;

 // 平原屯堡
            if (tile.elevation01 < 0.4f && settlement.primaryFunction == SettlementFunction.Military)
                return FortSubtype.PlainGarrison;

 // 坞堡庄园（默认内陆）
            return FortSubtype.ManorFort;
        }

 /// <summary>推导升级路线（使用进化树系统，根据起源确定发展路径）</summary>
        private static UpgradePath DeriveUpgradePath(TileData tile, SettlementData settlement)
        {
            return SettlementEvolutionTree.DeriveEvolutionPath(tile, settlement);
        }

 // ===== 升级路线：等级提升检查 =====
 /// 检查聚落是否可以升级到下一等级
        public static bool CanLevelUp(SettlementData settlement, out string reason)
        {
            reason = "";
            SettlementLevel nextLevel = settlement.settlementLevel + 1;
            if (nextLevel > SettlementLevel.LevelV)
            {
                reason = "已达最高等级";
                return false;
            }

 // 进化路径最大等级限制（如牧业通常最高Ⅱ级）
            SettlementLevel pathMaxLevel = SettlementEvolutionTree.GetMaxLevel(settlement.upgradePath);
            if (nextLevel > pathMaxLevel)
            {
                reason = $"此发展路径最高只能达到 {pathMaxLevel}";
                return false;
            }

 // 形态约束：村镇最高到Ⅱ级（集镇），极少数交通要道可到Ⅲ级
            if (settlement.settlementForm == SettlementForm.Village && nextLevel >= SettlementLevel.LevelIII)
            {
                bool isTransportHub = settlement.bottleneckType != BottleneckType.None ||
                                       settlement.portTier >= PortTier.IntermediatePort;
                if (!isTransportHub)
                {
                    reason = "村镇需先转型为城才能升到Ⅲ级以上";
                    return false;
                }
            }

 // 建设度要求（必须达到当前等级第5档才能升级）
            if (!IsConstructionReady(settlement))
            {
                reason = $"建设度不足（需第5档/100，当前第{settlement.constructionTier}档/{settlement.constructionProgress:0}）";
                return false;
            }

 // 人口要求
            float[] popRequirements = { 0f, 500f, 2000f, 8000f, 20000f };
            if (settlement.population < popRequirements[(int)nextLevel])
            {
                reason = $"人口不足（需{popRequirements[(int)nextLevel]:0}，当前{settlement.population:0}）";
                return false;
            }

 // Ⅲ级以上需要城墙
            if (nextLevel >= SettlementLevel.LevelIII && settlement.wallLevel < WallLevel.EarthenRampart)
            {
                reason = "Ⅲ级以上需要城墙防御";
                return false;
            }

 // Ⅳ级（都会）需要确定城市重心
            if (nextLevel == SettlementLevel.LevelIV && settlement.cityFocus == CityFocus.Balanced)
            {
 // 自动推导重心
                settlement.cityFocus = DeriveCityFocus(settlement);
            }

            return true;
        }

 /// <summary>推导城市重心（根据功能组合）</summary>
        public static CityFocus DeriveCityFocus(SettlementData settlement)
        {
            if (settlement.primaryFunction.HasFlag(SettlementFunction.Military) &&
                settlement.fortSubtype != FortSubtype.ManorFort)
                return CityFocus.Defense;

            if (settlement.bottleneckType != BottleneckType.None ||
                settlement.primaryFunction.HasFlag(SettlementFunction.Crossing))
                return CityFocus.Hub;

            if (settlement.portTier >= PortTier.DeepWaterPort ||
                settlement.primaryFunction.HasFlag(SettlementFunction.Commercial))
                return CityFocus.Trade;

            if (settlement.isCapital || settlement.primaryFunction.HasFlag(SettlementFunction.Administrative))
                return CityFocus.Administrative;

            if (settlement.hasTemple)
                return CityFocus.Religious;

            if (settlement.hasUniversity)
                return CityFocus.Cultural;

            if (settlement.primaryFunction.HasFlag(SettlementFunction.Mining))
                return CityFocus.Mining;

            return CityFocus.Balanced;
        }

 /// <summary>执行升级</summary>
        public static void PerformLevelUp(SettlementData settlement)
        {
            settlement.settlementLevel++;
            settlement.evolutionStage = EvolutionStage.Transformed;

 // 升级后重置建设度（新等级从第1档开始）
            settlement.constructionProgress = 0f;
            settlement.constructionTier = 1;

 // 升级时提升城墙
            if (settlement.settlementLevel >= SettlementLevel.LevelIII &&
                settlement.wallLevel < WallLevel.StoneWall)
            {
                settlement.wallLevel = settlement.siteMode == SiteMode.FortifiedOutpost
                    ? WallLevel.FortifiedWall
                    : WallLevel.StoneWall;
            }

 // Ⅳ级以上确定城形
            if (settlement.settlementLevel >= SettlementLevel.LevelIV &&
                settlement.cityForm == CityForm.WalledTown)
            {
                settlement.cityForm = settlement.siteMode == SiteMode.FortifiedOutpost
                    ? CityForm.StarCity
                    : CityForm.Circular;
            }

            Debug.Log($"[SettlementTypology] {settlement.settlementName} 升级到 {settlement.settlementLevel}");
        }

 // ===== 形态约束检查 =====
 /// 检查形态-等级约束（软性规则，AI遵循，玩家可突破）
        public static bool CheckFormLevelConstraint(SettlementData settlement)
        {
            return settlement.settlementLevel switch
            {
                SettlementLevel.LevelIV or SettlementLevel.LevelV =>
                    settlement.settlementForm != SettlementForm.Village, // 都会不能是村镇形态
                _ => true
            };
        }

 /// <summary>获取形态描述（使用进化树的阶段名称）</summary>
        public static string GetFullDescription(SettlementData settlement)
        {
            string stageName = SettlementEvolutionTree.GetStageName(settlement);
            string form = settlement.siteMode switch
            {
                SiteMode.SedentarySettlement when settlement.settlementForm == SettlementForm.Village => "村镇",
                SiteMode.SedentarySettlement when settlement.settlementForm == SettlementForm.City => "城",
                SiteMode.FortifiedOutpost => "据点",
                SiteMode.TemporaryEncampment => "营地",
                _ => "未知"
            };

            return $"{stageName}·{form}·{settlement.cityFocus}";
        }
    }
}
