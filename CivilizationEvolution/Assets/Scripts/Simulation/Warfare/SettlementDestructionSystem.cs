using System.Collections.Generic;



using UnityEngine;
using CivilizationEvolution.Core;
using CivilizationEvolution.Core.Constants;
using CivilizationEvolution.Core.Data;
using CivilizationEvolution.Core.Dto;
using CivilizationEvolution.Core.Enums;
using CivilizationEvolution.Simulation.Actors;
using CivilizationEvolution.Simulation.Economy;
using CivilizationEvolution.Simulation.Events;
using CivilizationEvolution.Simulation.Generation;
using CivilizationEvolution.Simulation.Modding;
using CivilizationEvolution.Simulation.Politics;
using CivilizationEvolution.Simulation.Settlement;
using CivilizationEvolution.Simulation.Society;
using CivilizationEvolution.Simulation.WorldState;
using CivilizationEvolution.World.Generation;
using CivilizationEvolution.World.Hydrology;
using CivilizationEvolution.World.Settlement;
using CivilizationEvolution.World.Terrain;


namespace CivilizationEvolution.Simulation.Warfare
{
    /// <summary>
    /// 聚落摧毁程度（三级）。
    /// </summary>
    public enum DestructionSeverity
    {
        Raid = 1,       // 轻度·劫掠：掠夺物资、零星纵火，等级不变，1-3年恢复
        Storm = 2,      // 中度·破城：拆城墙、烧主要建筑，降1级，5-10年恢复
        Raze = 3        // 重度·夷平：系统性摧毁/水淹/屠城，降为村镇或废墟，20年+或永不
    }

    /// <summary>
    /// 摧毁手段（六种）。手段决定适用场景和特殊效果。
    /// </summary>
    public enum DestructionMethod
    {
        Plunder,    // 劫掠：占领后掠夺，轻度，获取物资+士气
        Arson,      // 纵火：烧毁城区，轻~中度，可能蔓延相邻地块
        Dismantle,  // 拆城：系统拆除城墙建筑，中度，获得建材
        Flood,      // 水淹：决堤灌城，重度，仅临河/沿海，地块变沼泽
        Massacre,   // 屠城：系统屠杀，重度，人口锐减+大量流民+名声暴跌
        Raze        // 夷平：彻底摧毁，重度，聚落可能消失变废墟
    }

    /// <summary>
    /// 聚落摧毁系统。
    /// 统一处理：劫掠/破城/夷平三级程度，六种手段，废墟状态，连锁反应（流民/弃地/经济崩溃），废墟恢复。
    /// 历史案例：汉谟拉比毁玛睿（拆城+劫掠=中度）、杨坚毁建康（拆城+移民+焚毁=中~重度）、赵炅毁晋阳（火烧+水淹+移民=重度夷平）。
    /// </summary>
    public static class SettlementDestructionSystem
    {
        // ===== 摧毁效果参数表（severity → 各项损失比例）=====
        // 人口损失、经济损失、防御损失
        private static readonly float[] PopulationLoss = { 0f, 0.15f, 0.40f, 0.85f };
        private static readonly float[] EconomyLoss = { 0f, 0.25f, 0.60f, 0.92f };
        private static readonly float[] FortLoss = { 0f, 0.40f, 1.00f, 1.00f };
        // 恢复时间（天）：轻度1-3年，中度5-10年，重度20年+
        private static readonly int[] RecoveryDays = { 0, 730, 2555, 7300 };

        /// <summary>
        /// 执行聚落摧毁。
        /// </summary>
        /// <param name="world">游戏世界</param>
        /// <param name="burgId">聚落ID</param>
        /// <param name="method">摧毁手段</param>
        /// <param name="attackerRealmId">攻击者政权ID</param>
        /// <returns>摧毁结果描述（用于事件文本）</returns>
        public static string DestroySettlement(GameWorld world, int burgId,
            DestructionMethod method, int attackerRealmId = -1)
        {
            if (world == null || !world.burgs.TryGetValue(burgId, out var burg)) return null;
            if (burg.IsRuined) return "聚落已是废墟";

            // 手段 → 程度映射
            DestructionSeverity severity = MethodToSeverity(method, burg);

            // 记录摧毁前状态
            burg.preRuinLevel = burg.settlementLevel;
            burg.preRuinType = burg.settlementType;
            burg.ruinedDay = world.currentDay;
            burg.ruinLevel = (int)severity;
            burg.remnantCultureId = burg.provinceId; // 简化：残留标记
            burg.recoveryProgress = 0f;

            // 1. 人口损失
            float popLossRatio = PopulationLoss[(int)severity];
            int lostPop = Mathf.RoundToInt(burg.population * popLossRatio);
            burg.population -= lostPop;

            // 2. 经济损失
            burg.development *= (1f - EconomyLoss[(int)severity]);
            burg.wealth *= (1f - EconomyLoss[(int)severity]);
            burg.tradePower *= (1f - EconomyLoss[(int)severity] * 0.7f);

            // 3. 防御损失
            burg.fortification *= (1f - FortLoss[(int)severity]);
            burg.garrison = Mathf.Max(0, burg.garrison - Mathf.RoundToInt(burg.garrison * popLossRatio));

            // 4. 等级变化
            ApplyLevelDowngrade(burg, severity);

            // 5. 特殊手段效果
            string specialEffect = ApplyMethodEffect(world, burg, method, severity, attackerRealmId, lostPop);

            // 6. 连锁反应：幸存者变为流民
            int refugeeCount = SpawnRefugeesFromDestruction(world, burg, lostPop, severity);

            // 7. 重度夷平：地块可能进入弃地状态
            if (severity == DestructionSeverity.Raze)
            {
                AbandonTileAfterRaze(world, burg);
            }

            // 8. 市场/大学等设施损毁
            if (severity >= DestructionSeverity.Storm)
            {
                burg.hasMarket = false;
                burg.hasUniversity = false;
                if (severity == DestructionSeverity.Raze) burg.hasTemple = false;
            }

            Debug.Log($"[Destruction] {burg.burgName} 遭到{method}（{severity}），" +
                      $"人口损失{lostPop}（流民{refugeeCount}），发展度降至{burg.development:F1}");

            world.burgs[burgId] = burg;
            return $"{burg.burgName}遭到{GetMethodName(method)}，{GetSeverityDesc(severity)}，" +
                   $"损失人口{lostPop}，{refugeeCount}人流离失所。{specialEffect}";
        }

        /// <summary>手段映射到程度（部分手段受场景影响）</summary>
        private static DestructionSeverity MethodToSeverity(DestructionMethod method, BurgData burg)
        {
            switch (method)
            {
                case DestructionMethod.Plunder: return DestructionSeverity.Raid;
                case DestructionMethod.Arson:
                    // 纵火：木建筑为主的聚落破坏更重
                    return burg.wallLevel <= WallLevel.EarthenRampart
                        ? DestructionSeverity.Storm : DestructionSeverity.Raid;
                case DestructionMethod.Dismantle: return DestructionSeverity.Storm;
                case DestructionMethod.Flood: return DestructionSeverity.Raze;
                case DestructionMethod.Massacre: return DestructionSeverity.Raze;
                case DestructionMethod.Raze: return DestructionSeverity.Raze;
                default: return DestructionSeverity.Raid;
            }
        }

        /// <summary>等级降级</summary>
        private static void ApplyLevelDowngrade(BurgData burg, DestructionSeverity severity)
        {
            if (severity == DestructionSeverity.Raid) return; // 轻度不变

            if (severity == DestructionSeverity.Storm)
            {
                // 中度降1级
                if (burg.settlementLevel > SettlementLevel.LevelI)
                    burg.settlementLevel--;
            }
            else // Raze
            {
                // 重度：降为Ⅰ级聚落或直接变废墟
                if (burg.population < 50)
                {
                    // 人口几乎灭绝，聚落消失（保留废墟数据）
                    burg.settlementLevel = SettlementLevel.LevelI;
                    burg.buildLevel = 0;
                }
                else
                {
                    burg.settlementLevel = SettlementLevel.LevelI;
                    burg.buildLevel = Mathf.Max(0, burg.buildLevel - 2);
                }
            }
        }

        /// <summary>特殊手段效果</summary>
        private static string ApplyMethodEffect(GameWorld world, BurgData burg,
            DestructionMethod method, DestructionSeverity severity, int attackerId, int lostPop)
        {
            switch (method)
            {
                case DestructionMethod.Plunder:
                    // 劫掠获取物资（简化：攻击者财富增加）
                    float loot = burg.wealth * 0.3f;
                    return $"劫掠者获得了价值{loot:F0}的物资。";

                case DestructionMethod.Arson:
                    // 纵火可能蔓延到相邻地块（简化：相邻地块发展度略降）
                    DamageAdjacentTiles(world, burg.tileIndex, 0.05f);
                    return "大火蔓延到了周边地区。";

                case DestructionMethod.Dismantle:
                    // 拆城获得建材，城防永久降低
                    burg.wallLevel = WallLevel.None;
                    return "城墙被系统性拆除，获得大量建材。";

                case DestructionMethod.Flood:
                    // 水淹：地块湿度大增，可能变沼泽
                    if (burg.tileIndex >= 0 && burg.tileIndex < world.tiles.Length)
                    {
                        var tile = world.tiles[burg.tileIndex];
                        tile.soilHumidityPct = Mathf.Min(100f, tile.soilHumidityPct + 40f);
                        tile.development *= 0.5f;
                        world.tiles[burg.tileIndex] = tile;
                    }
                    return "大水淹没了城区，土地沦为沼泽。";

                case DestructionMethod.Massacre:
                    // 屠城：名声暴跌（由外交系统处理），流民更多
                    return "屠城的消息传开，远近震恐。";

                case DestructionMethod.Raze:
                    burg.wallLevel = WallLevel.None;
                    return "城市被彻底夷为平地。";

                default: return "";
            }
        }

        /// <summary>相邻地块受损</summary>
        private static void DamageAdjacentTiles(GameWorld world, int centerTile, float ratio)
        {
            int w = world.mapWidth, h = world.mapHeight;
            int cx = centerTile % w, cy = centerTile / w;
            for (int dy = -1; dy <= 1; dy++)
            {
                for (int dx = -1; dx <= 1; dx++)
                {
                    if (dx == 0 && dy == 0) continue;
                    int nx = cx + dx, ny = cy + dy;
                    if (nx < 0 || nx >= w || ny < 0 || ny >= h) continue;
                    int idx = ny * w + nx;
                    if (idx < 0 || idx >= world.tiles.Length) continue;
                    var tile = world.tiles[idx];
                    if (!tile.exists) continue;
                    tile.development *= (1f - ratio);
                    world.tiles[idx] = tile;
                }
            }
        }

        /// <summary>摧毁后产生流民MapActor</summary>
        private static int SpawnRefugeesFromDestruction(GameWorld world, BurgData burg,
            int lostPop, DestructionSeverity severity)
        {
            if (world.MapActors == null || lostPop <= 0) return 0;
            // 轻度：30%幸存者逃亡；中度：60%；重度：90%
            float fleeRatio = severity == DestructionSeverity.Raid ? 0.3f
                : severity == DestructionSeverity.Storm ? 0.6f : 0.9f;
            int refugeeCount = Mathf.RoundToInt(lostPop * fleeRatio);
            if (refugeeCount < 5) return 0;

            var refugee = world.MapActors.SpawnActor(
                MapActorType.Refugee, burg.tileIndex, refugeeCount, new RefugeeAI());
            refugee.actorName = $"来自{burg.burgName}的流民";
            refugee.morale = 20f;
            refugee.supplies = 30f;
            return refugeeCount;
        }

        /// <summary>夷平后地块进入弃地状态</summary>
        private static void AbandonTileAfterRaze(GameWorld world, BurgData burg)
        {
            if (burg.tileIndex < 0 || burg.tileIndex >= world.tiles.Length) return;
            var tile = world.tiles[burg.tileIndex];
            tile.ownerRealmId = -1;
            tile.order = Mathf.Max(0f, tile.order - 30f);
            world.tiles[burg.tileIndex] = tile;
        }

        // ===== 废墟恢复 =====

        /// <summary>每日更新废墟恢复（在GameWorld主循环调用）</summary>
        public static void DailyTickRecovery(GameWorld world)
        {
            if (world == null || world.burgs == null) return;
            foreach (var kvp in world.burgs)
            {
                var burg = kvp.Value;
                if (burg.ruinLevel <= 0) continue;

                // 有人口回流才恢复
                if (burg.population > 10)
                {
                    // 恢复速度受人口、发展度影响
                    float recoveryRate = 0.01f * Mathf.Log10(burg.population + 1);
                    burg.recoveryProgress += recoveryRate;

                    // 轻度恢复最快
                    int requiredDays = RecoveryDays[burg.ruinLevel];
                    float progressPerDay = 100f / Mathf.Max(1, requiredDays);
                    burg.recoveryProgress += progressPerDay;

                    if (burg.recoveryProgress >= 100f)
                    {
                        // 恢复完成
                        if (burg.ruinLevel >= 3)
                        {
                            // 废墟重建：恢复到摧毁前等级的下一级（不会完全恢复到巅峰）
                            burg.settlementLevel = burg.preRuinLevel;
                            burg.settlementType = burg.preRuinType;
                        }
                        burg.ruinLevel = 0;
                        burg.ruinedDay = -1;
                        burg.recoveryProgress = 0f;
                        burg.fortification = Mathf.Max(burg.fortification, 1f);
                        Debug.Log($"[Destruction] {burg.burgName} 已从废墟中恢复");
                    }
                    world.burgs[kvp.Key] = burg;
                }
            }
        }

        // ===== 显示辅助 =====

        public static string GetMethodName(DestructionMethod method)
        {
            switch (method)
            {
                case DestructionMethod.Plunder: return "劫掠";
                case DestructionMethod.Arson: return "纵火";
                case DestructionMethod.Dismantle: return "拆城";
                case DestructionMethod.Flood: return "水淹";
                case DestructionMethod.Massacre: return "屠城";
                case DestructionMethod.Raze: return "夷平";
                default: return "破坏";
            }
        }

        public static string GetSeverityDesc(DestructionSeverity severity)
        {
            switch (severity)
            {
                case DestructionSeverity.Raid: return "轻度劫掠";
                case DestructionSeverity.Storm: return "城池残破";
                case DestructionSeverity.Raze: return "满目疮痍";
                default: return "";
            }
        }
    }
}
