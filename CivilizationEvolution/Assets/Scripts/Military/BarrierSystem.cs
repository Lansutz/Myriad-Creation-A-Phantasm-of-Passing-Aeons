using System.Collections.Generic;
using CivilizationEvolution.Core;
using UnityEngine;

namespace CivilizationEvolution.Map
{
    /// <summary>
    /// 关隘与通行地理系统
    /// 负责：不可通行地区计算（坡度）、通行成本计算、狭窄通道检测、关隘建造/攻破、堡垒区域影响
    ///
    /// 核心区分：
    /// - 关隘（Barrier）：建在狭窄通道，直接阻挡敌对势力通行（除非攻破）
    /// - 堡垒（Fort）：区域控制，不直接阻挡通行，但敌对时经过有损耗和速度影响，己方给补给支援
    /// </summary>
    public static class BarrierSystem
    {
        // ===== 坡度/海拔阈值（综合判定可通行性）=====
        /// <summary>不可通行海拔阈值（高于此海拔且坡度足够陡峭才不可通行）</summary>
        public const float ImpassableElevation = 0.85f;

        /// <summary>不可通行坡度阈值（度）——需同时满足高海拔</summary>
        public const float ImpassableSlope = 50f;

        /// <summary>高通行成本坡度阈值（度）</summary>
        public const float HighCostSlope = 30f;

        /// <summary>中等通行成本坡度阈值（度）</summary>
        public const float MediumCostSlope = 15f;

        /// <summary>高海拔阈值（雪线/高原，通行成本增加）</summary>
        public const float HighElevation = 0.7f;

        /// <summary>中海拔阈值（山地，通行成本增加）</summary>
        public const float MediumElevation = 0.5f;

        // ===== 关隘建造条件 =====
        /// <summary>关隘所需的通道狭窄度（两侧不可通行/高成本地块的最小数量）</summary>
        public const int MinNarrownessForBarrier = 2;

        /// <summary>关隘影响半径（格）</summary>
        public const int BarrierRadius = 1;

        /// <summary>堡垒基础影响半径（格）</summary>
        public const int FortBaseRadius = 2;

        // ===== 不可通行地区计算 =====

        /// <summary>
        /// 根据坡度计算所有地块的可通行性和基础通行成本
        /// 生成地块时调用，智能生成不可通行地区
        /// </summary>
        public static void CalculatePassability(TileData[] tiles, int width, int height)
        {
            for (int i = 0; i < tiles.Length; i++)
            {
                ref TileData tile = ref tiles[i];
                if (!tile.exists || !tile.isLand)
                {
                    tile.passable = tile.exists; // 海洋可通行（船只），虚空不可
                    tile.movementCost = tile.isLand ? 999f : 2.0f; // 海洋通行成本
                    continue;
                }

                // 综合海拔+坡度决定可通行性
                // 只有高海拔(>0.85)且极陡峭(>50°)才不可通行（高山绝壁/雪线以上陡峭山峰）
                // 山脉中间的山口（高海拔但坡度较低）可以通行
                // 低海拔的陡坡（峡谷峭壁）一般也有小径可通行
                tile.passable = !(tile.elevation01 >= ImpassableElevation &&
                                  tile.slopeDegree >= ImpassableSlope);

                // 基础通行成本（平原=1.0）
                tile.movementCost = CalculateBaseMovementCost(tile);
            }
        }

        /// <summary>
        /// 计算单地块基础通行成本（综合海拔+坡度+道路+水文）
        /// 平原=1.0，山脉山口=2-4，高山绝壁=不可通行
        /// </summary>
        public static float CalculateBaseMovementCost(TileData tile)
        {
            if (!tile.passable) return 999f; // 不可通行

            float cost = 1.0f;

            // ===== 坡度加成 =====
            if (tile.slopeDegree >= HighCostSlope)
                cost *= 3.0f;
            else if (tile.slopeDegree >= MediumCostSlope)
                cost *= 1.8f;
            else if (tile.slopeDegree >= 8f)
                cost *= 1.3f;

            // ===== 海拔加成（高海拔缺氧/严寒，通行成本增加）=====
            if (tile.elevation01 >= HighElevation)
                cost *= 2.0f; // 雪线/高原
            else if (tile.elevation01 >= MediumElevation)
                cost *= 1.4f; // 山地

            // ===== 海拔×坡度交互（山脉山口：高海拔但坡度适中，成本低于陡峭低海拔）=====
            // 高海拔+低坡度 = 高原/山口，比高海拔+高坡度容易通行
            if (tile.elevation01 >= HighElevation && tile.slopeDegree < MediumCostSlope)
                cost *= 0.7f; // 高原/山口减免

            // 中海拔+高坡度 = 陡峭山地，但比高海拔+高坡度容易
            if (tile.elevation01 >= MediumElevation && tile.elevation01 < HighElevation &&
                tile.slopeDegree >= HighCostSlope)
                cost *= 0.85f;

            // ===== 道路减成 =====
            cost *= tile.roadLevel switch
            {
                GameEnums.RoadLevel.ImperialHighway => 0.5f,
                GameEnums.RoadLevel.OfficialRoad => 0.7f,
                GameEnums.RoadLevel.DirtRoad => 0.9f,
                _ => 1.0f
            };

            // ===== 水文加成 =====
            // 河流渡口（无桥的河流需要渡河）
            if (tile.isRiver && tile.roadLevel == GameEnums.RoadLevel.None)
                cost *= 1.5f;

            // 海岸滩涂
            if (tile.isCoast && tile.elevation01 < 0.3f)
                cost *= 1.2f;

            return Mathf.Max(0.3f, cost);
        }

        // ===== 狭窄通道检测 =====

        /// <summary>
        /// 检测地图上的狭窄通道（可建关隘的位置）
        /// 狭窄通道定义：可通行地块两侧被不可通行/高成本地块包围
        /// </summary>
        public static List<int> DetectNarrowPassages(TileData[] tiles, int width, int height)
        {
            var passages = new List<int>();

            for (int i = 0; i < tiles.Length; i++)
            {
                ref TileData tile = ref tiles[i];
                if (!tile.exists || !tile.isLand || !tile.passable) continue;

                if (IsNarrowPassage(tiles, width, height, i))
                    passages.Add(i);
            }

            return passages;
        }

        /// <summary>
        /// 检查某地块是否为狭窄通道
        /// 判定：4方向邻居中，至少2个方向的连续3格内有不可通行/高成本地块
        /// </summary>
        public static bool IsNarrowPassage(TileData[] tiles, int width, int height, int index)
        {
            int x = index % width;
            int y = index / width;

            int blockedDirections = 0;

            // 检查4个方向
            int[] dx = { 0, 0, -1, 1 };
            int[] dy = { -1, 1, 0, 0 };

            for (int d = 0; d < 4; d++)
            {
                if (IsDirectionBlocked(tiles, width, height, x, y, dx[d], dy[d]))
                    blockedDirections++;
            }

            // 至少2个方向被阻挡（形成通道），且不是4面都被阻挡（不是孤立点）
            return blockedDirections >= MinNarrownessForBarrier && blockedDirections < 4;
        }

        /// <summary>
        /// 检查某方向是否被阻挡（连续3格内有不可通行/高成本地块）
        /// </summary>
        private static bool IsDirectionBlocked(TileData[] tiles, int width, int height,
            int startX, int startY, int dx, int dy)
        {
            for (int step = 1; step <= 3; step++)
            {
                int nx = startX + dx * step;
                int ny = startY + dy * step;

                // 左右环绕
                nx = ((nx % width) + width) % width;

                if (ny < 0 || ny >= height) return true; // 地图边界视为阻挡

                int ni = ny * width + nx;
                if (!tiles[ni].exists || !tiles[ni].passable ||
                    tiles[ni].slopeDegree >= HighCostSlope)
                    return true;
            }
            return false;
        }

        // ===== 关隘建造 =====

        /// <summary>
        /// 检查是否可在某地块建关隘
        /// 条件：必须是狭窄通道+可通行+陆地
        /// </summary>
        public static bool CanBuildBarrier(TileData[] tiles, int width, int height, int index)
        {
            ref TileData tile = ref tiles[index];
            if (!tile.exists || !tile.isLand || !tile.passable) return false;
            if (tile.hasBarrier) return false; // 已有建筑
            return IsNarrowPassage(tiles, width, height, index);
        }

        /// <summary>
        /// 建造关隘
        /// </summary>
        public static bool BuildBarrier(TileData[] tiles, int index, int realmId, float strength = 5f)
        {
            ref TileData tile = ref tiles[index];
            if (tile.hasBarrier) return false;

            tile.hasBarrier = true;
            tile.barrierOwnerRealmId = realmId;
            tile.barrierStrength = Mathf.Clamp(strength, 1f, 10f);
            tile.isGate = true;

            // 关隘会略微降低通行成本（有关隘=有维护的通道）
            tile.movementCost *= 0.9f;

            return true;
        }

        /// <summary>
        /// 攻破关隘
        /// </summary>
        public static bool BreachBarrier(TileData[] tiles, int index, int attackerRealmId)
        {
            ref TileData tile = ref tiles[index];
            if (!tile.hasBarrier) return false;

            tile.hasBarrier = false;
            tile.barrierOwnerRealmId = -1;
            tile.barrierStrength = 0f;
            tile.isGate = false;

            // 攻破后通行成本上升（关隘被破坏）
            tile.movementCost *= 1.3f;

            return true;
        }

        /// <summary>
        /// 占领关隘（不破坏，直接变更所有者）
        /// </summary>
        public static bool CaptureBarrier(TileData[] tiles, int index, int newRealmId)
        {
            ref TileData tile = ref tiles[index];
            if (!tile.hasBarrier) return false;

            tile.barrierOwnerRealmId = newRealmId;
            return true;
        }

        // ===== 堡垒区域影响计算 =====

        /// <summary>
        /// 计算所有堡垒的区域影响
        /// 为每个地块设置nearbyFortId和fortInfluenceLevel
        /// 敌对堡垒：经过时损耗+速度下降
        /// 己方堡垒：补给+支援加成
        /// </summary>
        public static void CalculateFortInfluence(TileData[] tiles, int width, int height,
            Dictionary<int, BurgData> burgs)
        {
            // 先重置所有地块的堡垒影响
            for (int i = 0; i < tiles.Length; i++)
            {
                tiles[i].nearbyFortId = -1;
                tiles[i].fortInfluenceLevel = 0;
            }

            // 遍历所有堡垒类型的Burg
            foreach (var burg in burgs.Values)
            {
                if (burg.settlementType != SettlementType.Fort &&
                    burg.type != BurgType.Fortress) continue;

                int fortTile = burg.tileIndex;
                if (fortTile < 0 || fortTile >= tiles.Length) continue;

                // 堡垒等级决定影响半径和强度
                int radius = FortBaseRadius + (int)burg.settlementLevel;
                int influenceLevel = Mathf.Clamp(1 + (int)burg.settlementLevel / 2, 1, 3);

                // 为半径内的地块设置影响（取最近的堡垒）
                for (int dy = -radius; dy <= radius; dy++)
                {
                    for (int dx = -radius; dx <= radius; dx++)
                    {
                        if (dx * dx + dy * dy > radius * radius) continue;

                        int tx = (fortTile % width) + dx;
                        int ty = (fortTile / width) + dy;
                        tx = ((tx % width) + width) % width;
                        if (ty < 0 || ty >= height) continue;

                        int ti = ty * width + tx;
                        if (!tiles[ti].exists || !tiles[ti].isLand) continue;

                        // 距离衰减
                        float dist = Mathf.Sqrt(dx * dx + dy * dy);
                        int level = dist <= radius * 0.5f ? influenceLevel :
                                    dist <= radius * 0.8f ? Mathf.Max(1, influenceLevel - 1) : 1;

                        // 只设置最近的堡垒（或更强的堡垒）
                        if (tiles[ti].fortInfluenceLevel < level || tiles[ti].nearbyFortId == -1)
                        {
                            tiles[ti].nearbyFortId = burg.burgId;
                            tiles[ti].fortInfluenceLevel = level;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// 获取某地块的堡垒所有者政权ID
        /// </summary>
        public static int GetFortOwnerRealmId(TileData[] tiles, Dictionary<int, BurgData> burgs, int tileIndex)
        {
            if (tiles[tileIndex].nearbyFortId < 0) return -1;
            if (burgs.TryGetValue(tiles[tileIndex].nearbyFortId, out var burg))
                return tiles[burg.tileIndex].ownerRealmId;
            return -1;
        }
    }
}
