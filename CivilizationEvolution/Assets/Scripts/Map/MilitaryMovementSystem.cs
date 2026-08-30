using System.Collections.Generic;
using CivilizationEvolution.Core;
using UnityEngine;

namespace CivilizationEvolution.Map
{
    /// <summary>
    /// 军事通行系统
    /// 负责：军队可通行检查、实际通行成本计算、敌对堡垒损耗、己方堡垒补给支援
    ///
    /// 核心规则：
    /// - 不可通行地区（坡度>45°）：军队无法通过
    /// - 关隘（Barrier）：敌对势力的关隘直接阻挡通行，除非攻破；己方/中立关隘正常通行
    /// - 堡垒（Fort）区域：不直接阻挡通行，但敌对堡垒区域内军队损耗增加、行进速度下降；己方堡垒区域给补给和支援加成
    /// </summary>
    public static class MilitaryMovementSystem
    {
        // ===== 堡垒区域影响参数 =====
        /// <summary>敌对堡垒区域通行成本倍率（1级影响）</summary>
        public const float EnemyFortCostMultiplier_L1 = 1.3f;
        /// <summary>敌对堡垒区域通行成本倍率（2级影响）</summary>
        public const float EnemyFortCostMultiplier_L2 = 1.6f;
        /// <summary>敌对堡垒区域通行成本倍率（3级影响）</summary>
        public const float EnemyFortCostMultiplier_L3 = 2.0f;

        /// <summary>敌对堡垒区域每Tick损耗率（占军队总人数比例，1级影响）</summary>
        public const float EnemyFortLossRate_L1 = 0.005f; // 0.5%
        /// <summary>敌对堡垒区域每Tick损耗率（2级影响）</summary>
        public const float EnemyFortLossRate_L2 = 0.012f; // 1.2%
        /// <summary>敌对堡垒区域每Tick损耗率（3级影响）</summary>
        public const float EnemyFortLossRate_L3 = 0.025f; // 2.5%

        /// <summary>己方堡垒区域通行成本减成（1级影响）</summary>
        public const float FriendlyFortCostReduction_L1 = 0.9f;
        /// <summary>己方堡垒区域通行成本减成（2级影响）</summary>
        public const float FriendlyFortCostReduction_L2 = 0.8f;
        /// <summary>己方堡垒区域通行成本减成（3级影响）</summary>
        public const float FriendlyFortCostReduction_L3 = 0.7f;

        /// <summary>己方堡垒区域补给率（每Tick恢复比例，1级影响）</summary>
        public const float FriendlyFortSupplyRate_L1 = 0.003f; // 0.3%
        /// <summary>己方堡垒区域补给率（2级影响）</summary>
        public const float FriendlyFortSupplyRate_L2 = 0.008f; // 0.8%
        /// <summary>己方堡垒区域补给率（3级影响）</summary>
        public const float FriendlyFortSupplyRate_L3 = 0.015f; // 1.5%

        /// <summary>己方堡垒区域士气/组织度恢复加成</summary>
        public const float FriendlyFortMoraleBonus = 0.1f;

        /// <summary>关隘防御加成（守方战斗加成）</summary>
        public const float BarrierDefenseBonus = 0.3f; // 30%防御加成

        // ===== 可通行检查 =====

        /// <summary>
        /// 检查军队是否可通行某地块
        /// 考虑：不可通行地区、关隘封锁
        /// </summary>
        /// <param name="tile">地块数据</param>
        /// <param name="armyRealmId">军队所属政权ID</param>
        /// <param name="isAtWarWithBarrierOwner">是否与关隘所有者处于战争状态</param>
        /// <returns>是否可通行</returns>
        public static bool IsPassable(TileData tile, int armyRealmId, bool isAtWarWithBarrierOwner = true)
        {
            if (!tile.exists) return false;

            // 不可通行地区（坡度>45°）
            if (!tile.passable) return false;

            // 关隘封锁
            if (tile.hasBarrier && tile.barrierOwnerRealmId >= 0)
            {
                // 己方关隘：可通行
                if (tile.barrierOwnerRealmId == armyRealmId) return true;

                // 中立/无主关隘：可通行
                if (tile.barrierOwnerRealmId == -1) return true;

                // 敌对关隘且处于战争状态：不可通行（除非攻破）
                if (isAtWarWithBarrierOwner) return false;

                // 非战争状态的敌对关隘：可通行（但有关税/检查）
                return true;
            }

            return true;
        }

        /// <summary>
        /// 检查是否需要攻破关隘才能通过
        /// </summary>
        public static bool RequiresBarrierBreach(TileData tile, int armyRealmId)
        {
            return tile.hasBarrier &&
                   tile.barrierOwnerRealmId >= 0 &&
                   tile.barrierOwnerRealmId != armyRealmId;
        }

        // ===== 实际通行成本计算 =====

        /// <summary>
        /// 计算军队通过某地块的实际通行成本
        /// 考虑：基础地形成本、敌对堡垒区域加成、己方堡垒区域减成、关隘
        /// </summary>
        public static float CalculateActualMovementCost(TileData tile, int armyRealmId,
            int fortOwnerRealmId = -1)
        {
            if (!tile.passable) return 999f;

            float cost = tile.movementCost;

            // 堡垒区域影响
            if (tile.fortInfluenceLevel > 0 && fortOwnerRealmId >= 0)
            {
                if (fortOwnerRealmId == armyRealmId)
                {
                    // 己方堡垒：通行成本减成
                    cost *= tile.fortInfluenceLevel switch
                    {
                        1 => FriendlyFortCostReduction_L1,
                        2 => FriendlyFortCostReduction_L2,
                        3 => FriendlyFortCostReduction_L3,
                        _ => 1.0f
                    };
                }
                else
                {
                    // 敌对堡垒：通行成本加成
                    cost *= tile.fortInfluenceLevel switch
                    {
                        1 => EnemyFortCostMultiplier_L1,
                        2 => EnemyFortCostMultiplier_L2,
                        3 => EnemyFortCostMultiplier_L3,
                        _ => 1.0f
                    };
                }
            }

            // 关隘：己方关隘略微降低成本（有维护的通道）
            if (tile.hasBarrier && tile.barrierOwnerRealmId == armyRealmId)
                cost *= 0.95f;

            return Mathf.Max(0.2f, cost);
        }

        // ===== 军队损耗计算 =====

        /// <summary>
        /// 计算军队经过某地块时的人员损耗
        /// 主要来自敌对堡垒区域的骚扰/劫掠/箭雨
        /// </summary>
        /// <param name="tile">地块数据</param>
        /// <param name="armySize">军队总人数</param>
        /// <param name="armyRealmId">军队所属政权ID</param>
        /// <param name="fortOwnerRealmId">堡垒所有者政权ID</param>
        /// <param name="ticks">经过该地块的Tick数</param>
        /// <returns>损耗人数</returns>
        public static float CalculateArmyLosses(TileData tile, float armySize,
            int armyRealmId, int fortOwnerRealmId, int ticks = 1)
        {
            if (tile.fortInfluenceLevel <= 0 || fortOwnerRealmId < 0) return 0f;
            if (fortOwnerRealmId == armyRealmId) return 0f; // 己方堡垒无损耗

            float lossRate = tile.fortInfluenceLevel switch
            {
                1 => EnemyFortLossRate_L1,
                2 => EnemyFortLossRate_L2,
                3 => EnemyFortLossRate_L3,
                _ => 0f
            };

            return armySize * lossRate * ticks;
        }

        /// <summary>
        /// 计算军队经过敌对堡垒区域的组织度/士气损耗
        /// </summary>
        public static float CalculateMoraleLoss(TileData tile, int armyRealmId, int fortOwnerRealmId)
        {
            if (tile.fortInfluenceLevel <= 0 || fortOwnerRealmId < 0) return 0f;
            if (fortOwnerRealmId == armyRealmId) return 0f;

            return tile.fortInfluenceLevel * 0.05f; // 每级5%组织度损耗
        }

        // ===== 己方堡垒补给支援 =====

        /// <summary>
        /// 计算己方堡垒区域的补给恢复量
        /// </summary>
        public static float CalculateSupplyRecovery(TileData tile, float armySize,
            int armyRealmId, int fortOwnerRealmId, int ticks = 1)
        {
            if (tile.fortInfluenceLevel <= 0 || fortOwnerRealmId < 0) return 0f;
            if (fortOwnerRealmId != armyRealmId) return 0f; // 必须是己方堡垒

            float supplyRate = tile.fortInfluenceLevel switch
            {
                1 => FriendlyFortSupplyRate_L1,
                2 => FriendlyFortSupplyRate_L2,
                3 => FriendlyFortSupplyRate_L3,
                _ => 0f
            };

            return armySize * supplyRate * ticks;
        }

        /// <summary>
        /// 计算己方堡垒区域的组织度/士气恢复加成
        /// </summary>
        public static float CalculateMoraleRecovery(TileData tile, int armyRealmId, int fortOwnerRealmId)
        {
            if (tile.fortInfluenceLevel <= 0 || fortOwnerRealmId < 0) return 0f;
            if (fortOwnerRealmId != armyRealmId) return 0f;

            return tile.fortInfluenceLevel * FriendlyFortMoraleBonus;
        }

        /// <summary>
        /// 计算己方堡垒区域的战斗支援加成（防御时）
        /// </summary>
        public static float CalculateCombatSupportBonus(TileData tile, int armyRealmId, int fortOwnerRealmId)
        {
            if (tile.fortInfluenceLevel <= 0 || fortOwnerRealmId < 0) return 0f;
            if (fortOwnerRealmId != armyRealmId) return 0f;

            return tile.fortInfluenceLevel * 0.08f; // 每级8%战斗加成
        }

        // ===== 关隘战斗 =====

        /// <summary>
        /// 计算关隘守方防御加成
        /// </summary>
        public static float GetBarrierDefenseBonus(TileData tile)
        {
            if (!tile.hasBarrier) return 0f;
            return BarrierDefenseBonus * (tile.barrierStrength / 5f); // 强度5=30%加成
        }

        /// <summary>
        /// 计算攻关隘的难度（所需兵力/时间）
        /// </summary>
        public static float CalculateBarrierBreachDifficulty(TileData tile)
        {
            if (!tile.hasBarrier) return 0f;
            return tile.barrierStrength * tile.movementCost;
        }

        // ===== 军队行进速度 =====

        /// <summary>
        /// 计算军队在某地块的行进速度（相对于正常速度的比例）
        /// </summary>
        public static float CalculateMovementSpeed(TileData tile, int armyRealmId, int fortOwnerRealmId)
        {
            float cost = CalculateActualMovementCost(tile, armyRealmId, fortOwnerRealmId);
            return Mathf.Clamp(1f / cost, 0.1f, 2f);
        }

        /// <summary>
        /// 获取通行状态描述（用于UI显示）
        /// </summary>
        public static string GetMovementStatus(TileData tile, int armyRealmId, int fortOwnerRealmId)
        {
            if (!tile.exists) return "虚空";
            if (!tile.passable) return "不可通行";

            var status = new List<string>();

            if (tile.hasBarrier)
            {
                if (tile.barrierOwnerRealmId == armyRealmId)
                    status.Add("己方关隘");
                else if (tile.barrierOwnerRealmId >= 0)
                    status.Add("敌对关隘");
                else
                    status.Add("中立关隘");
            }

            if (tile.fortInfluenceLevel > 0 && fortOwnerRealmId >= 0)
            {
                if (fortOwnerRealmId == armyRealmId)
                    status.Add($"己方堡垒区域(Lv{tile.fortInfluenceLevel})");
                else
                    status.Add($"敌对堡垒区域(Lv{tile.fortInfluenceLevel})");
            }

            if (tile.slopeDegree >= BarrierSystem.HighCostSlope)
                status.Add("陡坡");
            else if (tile.slopeDegree >= BarrierSystem.MediumCostSlope)
                status.Add("缓坡");

            if (tile.roadLevel > GameEnums.RoadLevel.None)
                status.Add(tile.roadLevel.ToString());

            return status.Count > 0 ? string.Join(" / ", status) : "正常通行";
        }
    }
}
