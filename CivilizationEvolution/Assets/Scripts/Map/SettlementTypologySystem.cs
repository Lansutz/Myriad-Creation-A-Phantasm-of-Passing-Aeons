using System.Collections.Generic;
using CivilizationEvolution.Core;
using UnityEngine;

namespace CivilizationEvolution.Map
{
    /// <summary>
    /// 聚落类型学系统
    /// 负责：类型推导（地形/位置/资源→形态功能）、升级路线管理、形态约束、城形/堡型选择
    /// 综合：用户设计文档 + 文明引擎IN模块（港口层级/关隘瓶颈/要塞体系/城镇功能类型学）
    /// </summary>
    public static class SettlementTypologySystem
    {
        // ===== 类型推导：根据地块特征推导聚落初始形态 =====

        /// <summary>
        /// 根据地块特征推导聚落初始类型
        /// </summary>
        public static void DeriveInitialType(BurgData burg, TileData tile, int width, int height)
        {
            if (burg == null || tile == null) return;

            // 1. 判定是否为瓶颈节点
            burg.bottleneckType = DetectBottleneck(tile, width, height);

            // 2. 判定港口层级
            burg.portTier = DetectPortTier(tile);

            // 3. 推导主功能
            burg.primaryFunction = DerivePrimaryFunction(tile, burg);

            // 4. 推导形态（村镇/城/堡）
            burg.settlementType = DeriveSettlementType(tile, burg);

            // 5. 推导城形/堡型
            if (burg.settlementType == SettlementType.City)
            {
                burg.cityForm = DeriveCityForm(tile, burg);
                burg.fortSubtype = FortSubtype.RoyalCastle; // 默认
            }
            else if (burg.settlementType == SettlementType.Fort)
            {
                burg.fortSubtype = DeriveFortSubtype(tile, burg);
                burg.cityForm = CityForm.CitadelCity;
            }
            else
            {
                burg.cityForm = CityForm.WalledTown;
                burg.fortSubtype = FortSubtype.BorderFort;
            }

            // 6. 推导升级路线
            burg.upgradePath = DeriveUpgradePath(tile, burg);

            // 7. 初始等级
            burg.settlementLevel = SettlementLevel.LevelI;
            burg.evolutionStage = EvolutionStage.Stable;
            burg.wallLevel = burg.settlementType == SettlementType.Fort ? WallLevel.Palisade : WallLevel.None;

            // 8. 城市重心（低级聚落默认均衡，升级后再确定）
            burg.cityFocus = CityFocus.Balanced;
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
                bool lowNeighbor = false;
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
        private static SettlementFunction DerivePrimaryFunction(TileData tile, BurgData burg)
        {
            // 瓶颈节点 → 渡口/关税/军事
            if (burg.bottleneckType != BottleneckType.None)
            {
                if (burg.bottleneckType == BottleneckType.StraitCrossing ||
                    burg.bottleneckType == BottleneckType.RiverFording)
                    return SettlementFunction.Crossing;
                return SettlementFunction.Military;
            }

            // 港口 → 商业
            if (burg.portTier >= PortTier.IntermediatePort)
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
        private static SettlementType DeriveSettlementType(TileData tile, BurgData burg)
        {
            // 瓶颈+军事功能 → 堡
            if (burg.bottleneckType != BottleneckType.None &&
                burg.primaryFunction == SettlementFunction.Military)
                return SettlementType.Fort;

            // 港口+商业 → 城
            if (burg.portTier >= PortTier.IntermediatePort &&
                burg.primaryFunction == SettlementFunction.Commercial)
                return SettlementType.City;

            // 矿业 → 村镇（矿业村镇，后期可升级为城）
            if (burg.primaryFunction == SettlementFunction.Mining)
                return SettlementType.Village;

            // 默认 → 村镇
            return SettlementType.Village;
        }

        /// <summary>推导城的形态</summary>
        private static CityForm DeriveCityForm(TileData tile, BurgData burg)
        {
            // 港口城市 → 水城或不规则
            if (burg.portTier >= PortTier.RiverPort)
                return tile.isRiver ? CityForm.WaterCity : CityForm.Irregular;

            // 山地城市 → 山城
            if (tile.elevation01 > 0.6f)
                return CityForm.MountainCity;

            // 平原+行政 → 方城
            if (burg.primaryFunction == SettlementFunction.Administrative &&
                tile.elevation01 < 0.4f)
                return CityForm.Square;

            // 默认 → 圆城
            return CityForm.Circular;
        }

        /// <summary>推导堡垒亚型</summary>
        private static FortSubtype DeriveFortSubtype(TileData tile, BurgData burg)
        {
            // 关口堡
            if (burg.bottleneckType == BottleneckType.MountainPass ||
                burg.bottleneckType == BottleneckType.CanyonPass)
                return FortSubtype.PassFort;

            // 河口堡
            if (burg.bottleneckType == BottleneckType.StraitCrossing ||
                (burg.portTier >= PortTier.RiverPort && tile.isCoast))
                return FortSubtype.EstuaryFort;

            // 高地堡
            if (tile.elevation01 > 0.6f && tile.isLand)
                return FortSubtype.HighlandKeep;

            // 平原屯堡
            if (tile.elevation01 < 0.4f && burg.primaryFunction == SettlementFunction.Military)
                return FortSubtype.PlainGarrison;

            // 坞堡庄园（默认内陆）
            return FortSubtype.ManorFort;
        }

        /// <summary>推导升级路线</summary>
        private static UpgradePath DeriveUpgradePath(TileData tile, BurgData burg)
        {
            return burg.primaryFunction switch
            {
                SettlementFunction.Commercial => burg.portTier != PortTier.None
                    ? UpgradePath.PortDevelopment
                    : UpgradePath.CommercialGrowth,
                SettlementFunction.Military => UpgradePath.MilitaryGrowth,
                SettlementFunction.Mining => UpgradePath.MiningGrowth,
                SettlementFunction.Religious => UpgradePath.ReligiousGrowth,
                SettlementFunction.Administrative => UpgradePath.AdministrativeGrowth,
                SettlementFunction.Crossing => UpgradePath.StrategicGrowth,
                _ => UpgradePath.NaturalGrowth
            };
        }

        // ===== 升级路线：等级提升检查 =====

        /// <summary>
        /// 检查聚落是否可以升级到下一等级
        /// </summary>
        public static bool CanLevelUp(BurgData burg, out string reason)
        {
            reason = "";
            SettlementLevel nextLevel = burg.settlementLevel + 1;
            if (nextLevel > SettlementLevel.LevelV)
            {
                reason = "已达最高等级";
                return false;
            }

            // 形态约束：村镇最高到Ⅱ级（集镇），极少数交通要道可到Ⅲ级
            if (burg.settlementType == SettlementType.Village && nextLevel >= SettlementLevel.LevelIII)
            {
                bool isTransportHub = burg.bottleneckType != BottleneckType.None ||
                                       burg.portTier >= PortTier.IntermediatePort;
                if (!isTransportHub)
                {
                    reason = "村镇需先转型为城才能升到Ⅲ级以上";
                    return false;
                }
            }

            // 发展度要求
            float[] devRequirements = { 0f, 15f, 35f, 60f, 85f };
            if (burg.development < devRequirements[(int)nextLevel])
            {
                reason = $"发展度不足（需{devRequirements[(int)nextLevel]:0}，当前{burg.development:0}）";
                return false;
            }

            // 人口要求
            float[] popRequirements = { 0f, 500f, 2000f, 8000f, 20000f };
            if (burg.population < popRequirements[(int)nextLevel])
            {
                reason = $"人口不足（需{popRequirements[(int)nextLevel]:0}，当前{burg.population:0}）";
                return false;
            }

            // Ⅲ级以上需要城墙
            if (nextLevel >= SettlementLevel.LevelIII && burg.wallLevel < WallLevel.EarthenRampart)
            {
                reason = "Ⅲ级以上需要城墙防御";
                return false;
            }

            // Ⅳ级（都会）需要确定城市重心
            if (nextLevel == SettlementLevel.LevelIV && burg.cityFocus == CityFocus.Balanced)
            {
                // 自动推导重心
                burg.cityFocus = DeriveCityFocus(burg);
            }

            return true;
        }

        /// <summary>推导城市重心（根据功能组合）</summary>
        public static CityFocus DeriveCityFocus(BurgData burg)
        {
            if (burg.primaryFunction.HasFlag(SettlementFunction.Military) &&
                burg.fortSubtype != FortSubtype.ManorFort)
                return CityFocus.Defense;

            if (burg.bottleneckType != BottleneckType.None ||
                burg.primaryFunction.HasFlag(SettlementFunction.Crossing))
                return CityFocus.Hub;

            if (burg.portTier >= PortTier.DeepWaterPort ||
                burg.primaryFunction.HasFlag(SettlementFunction.Commercial))
                return CityFocus.Trade;

            if (burg.isCapital || burg.primaryFunction.HasFlag(SettlementFunction.Administrative))
                return CityFocus.Administrative;

            if (burg.hasTemple)
                return CityFocus.Religious;

            if (burg.hasUniversity)
                return CityFocus.Cultural;

            if (burg.primaryFunction.HasFlag(SettlementFunction.Mining))
                return CityFocus.Mining;

            return CityFocus.Balanced;
        }

        /// <summary>执行升级</summary>
        public static void PerformLevelUp(BurgData burg)
        {
            burg.settlementLevel++;
            burg.evolutionStage = EvolutionStage.Transformed;

            // 升级时提升城墙
            if (burg.settlementLevel >= SettlementLevel.LevelIII &&
                burg.wallLevel < WallLevel.StoneWall)
            {
                burg.wallLevel = burg.settlementType == SettlementType.Fort
                    ? WallLevel.FortifiedWall
                    : WallLevel.StoneWall;
            }

            // Ⅳ级以上确定城形
            if (burg.settlementLevel >= SettlementLevel.LevelIV &&
                burg.cityForm == CityForm.WalledTown)
            {
                burg.cityForm = burg.settlementType == SettlementType.Fort
                    ? CityForm.StarCity
                    : CityForm.Circular;
            }

            Debug.Log($"[SettlementTypology] {burg.burgName} 升级到 {burg.settlementLevel}");
        }

        // ===== 形态约束检查 =====

        /// <summary>
        /// 检查形态-等级约束（软性规则，AI遵循，玩家可突破）
        /// </summary>
        public static bool CheckFormLevelConstraint(BurgData burg)
        {
            return burg.settlementLevel switch
            {
                SettlementLevel.LevelIV or SettlementLevel.LevelV =>
                    burg.settlementType != SettlementType.Village, // 都会不能是村镇形态
                _ => true
            };
        }

        /// <summary>获取形态描述</summary>
        public static string GetFullDescription(BurgData burg)
        {
            string level = burg.settlementLevel switch
            {
                SettlementLevel.LevelI => "村落",
                SettlementLevel.LevelII => "集镇",
                SettlementLevel.LevelIII => "城邑",
                SettlementLevel.LevelIV => "都会",
                SettlementLevel.LevelV => "大都会",
                _ => "未知"
            };

            string form = burg.settlementType switch
            {
                SettlementType.Village => "村镇",
                SettlementType.City => "城",
                SettlementType.Fort => "堡",
                _ => "未知"
            };

            return $"{level}·{form}·{burg.cityFocus}";
        }
    }
}
