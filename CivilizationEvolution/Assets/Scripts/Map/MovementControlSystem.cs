using System.Collections.Generic;
using CivilizationEvolution.Core;
using CivilizationEvolution.Politics;
using UnityEngine;

namespace CivilizationEvolution.Map
{
    /// <summary>
    /// 通行管制系统（外交联动）
    /// 负责：4级通行管制、军事通行权授予/撤销、管制带来的通行成本加成
    ///
    /// 4级管制：
    /// - None（无管制）：军队可自由通过
    /// - Loose（松散管制）：军队可通过，但有关税/检查，速度略降
    /// - Limited（有限管制）：军队可通过，但需登记，速度明显下降，可能被监视
    /// - Strict（严格管制）：军队必须请求军事通行权，否则不可通过
    ///
    /// 军事通行权：
    /// - 严格管制下，只有被授予军事通行权的政权军队可通过
    /// - 通行权可通过外交条约授予/撤销
    /// - 战争状态下自动撤销敌对势力的通行权
    /// </summary>
    public static class MovementControlSystem
    {
        // ===== 管制带来的通行成本加成 =====
        /// <summary>无管制：通行成本不变</summary>
        public const float NoneCostMultiplier = 1.0f;
        /// <summary>松散管制：通行成本×1.1（检查/关税延迟）</summary>
        public const float LooseCostMultiplier = 1.1f;
        /// <summary>有限管制：通行成本×1.25（登记/监视）</summary>
        public const float LimitedCostMultiplier = 1.25f;
        /// <summary>严格管制（有通行权）：通行成本×1.15（通关检查）</summary>
        public const float StrictWithAccessCostMultiplier = 1.15f;

        // ===== 管制带来的其他影响 =====
        /// <summary>有限管制下的军队被监视概率（每Tick）</summary>
        public const float LimitedSurveillanceChance = 0.1f;
        /// <summary>严格管制（有通行权）下的军队被监视概率</summary>
        public const float StrictSurveillanceChance = 0.25f;

        /// <summary>松散管制下的关税比例（军队携带物资被征税）</summary>
        public const float LooseTariffRate = 0.02f;
        /// <summary>有限管制下的关税比例</summary>
        public const float LimitedTariffRate = 0.05f;
        /// <summary>严格管制（有通行权）下的关税比例</summary>
        public const float StrictTariffRate = 0.03f;

        // ===== 管制等级查询 =====

        /// <summary>
        /// 获取某地块的实际通行管制等级
        /// 优先使用地块单独覆盖，否则使用政权全国默认
        /// </summary>
        public static GameEnums.MovementControlLevel GetEffectiveControlLevel(
            TileData tile, RealmData ownerRealm)
        {
            if (ownerRealm == null) return GameEnums.MovementControlLevel.None;

            // 地块单独覆盖优先
            if (ownerRealm.tileMovementControlOverrides != null &&
                ownerRealm.tileMovementControlOverrides.TryGetValue(tile.tileIndex, out var tileLevel))
                return tileLevel;

            return ownerRealm.movementControl;
        }

        /// <summary>
        /// 获取某政权所有地块的最高管制等级（用于外交概览）
        /// </summary>
        public static GameEnums.MovementControlLevel GetMaxControlLevel(RealmData realm)
        {
            if (realm == null) return GameEnums.MovementControlLevel.None;

            var max = realm.movementControl;
            if (realm.tileMovementControlOverrides != null)
            {
                foreach (var level in realm.tileMovementControlOverrides.Values)
                {
                    if (level > max) max = level;
                }
            }
            return max;
        }

        // ===== 军事通行检查 =====

        /// <summary>
        /// 检查军队是否可通过某地块（考虑通行管制和军事通行权）
        /// </summary>
        /// <param name="tile">地块数据</param>
        /// <param name="armyRealmId">军队所属政权ID</param>
        /// <param name="ownerRealm">地块所属政权</param>
        /// <param name="isAtWar">是否与地块所有者处于战争状态</param>
        /// <returns>是否可通过</returns>
        public static bool CanMilitaryPass(TileData tile, int armyRealmId,
            RealmData ownerRealm, bool isAtWar)
        {
            // 无主土地：无管制
            if (ownerRealm == null || ownerRealm.realmId < 0) return true;

            // 己方领土：自由通行
            if (ownerRealm.realmId == armyRealmId) return true;

            // 战争状态：严格管制下不可通过，其他管制也可能被拒绝
            if (isAtWar)
            {
                var controlLevel = GetEffectiveControlLevel(tile, ownerRealm);
                // 战争状态下，有限/严格管制都不可通过（敌国军队）
                // 松散管制下也不可通过（战争状态）
                return false;
            }

            // 和平状态：检查管制等级
            var level = GetEffectiveControlLevel(tile, ownerRealm);

            switch (level)
            {
                case GameEnums.MovementControlLevel.None:
                case GameEnums.MovementControlLevel.Loose:
                case GameEnums.MovementControlLevel.Limited:
                    return true; // 非严格管制，和平状态下可通过

                case GameEnums.MovementControlLevel.Strict:
                    // 严格管制：需要军事通行权
                    return HasMilitaryAccess(ownerRealm, armyRealmId);

                default:
                    return true;
            }
        }

        /// <summary>
        /// 检查是否需要军事通行权才能通过
        /// </summary>
        public static bool RequiresMilitaryAccess(TileData tile, RealmData ownerRealm,
            int armyRealmId, bool isAtWar)
        {
            if (ownerRealm == null || ownerRealm.realmId == armyRealmId) return false;
            if (isAtWar) return true; // 战争状态总是需要"通行权"（实际上是不可通过）

            var level = GetEffectiveControlLevel(tile, ownerRealm);
            return level == GameEnums.MovementControlLevel.Strict &&
                   !HasMilitaryAccess(ownerRealm, armyRealmId);
        }

        // ===== 军事通行权管理 =====

        /// <summary>
        /// 检查某政权是否被授予了军事通行权
        /// </summary>
        public static bool HasMilitaryAccess(RealmData ownerRealm, int armyRealmId)
        {
            if (ownerRealm == null || ownerRealm.militaryAccessGranted == null) return false;
            return ownerRealm.militaryAccessGranted.Contains(armyRealmId);
        }

        /// <summary>
        /// 授予军事通行权（外交操作）
        /// </summary>
        public static bool GrantMilitaryAccess(RealmData ownerRealm, int targetRealmId)
        {
            if (ownerRealm == null || ownerRealm.militaryAccessGranted == null) return false;
            if (ownerRealm.realmId == targetRealmId) return false; // 不需要给自己授权
            return ownerRealm.militaryAccessGranted.Add(targetRealmId);
        }

        /// <summary>
        /// 撤销军事通行权（外交操作）
        /// </summary>
        public static bool RevokeMilitaryAccess(RealmData ownerRealm, int targetRealmId)
        {
            if (ownerRealm == null || ownerRealm.militaryAccessGranted == null) return false;
            return ownerRealm.militaryAccessGranted.Remove(targetRealmId);
        }

        /// <summary>
        /// 战争爆发时自动撤销敌对势力的军事通行权
        /// </summary>
        public static void OnWarDeclared(RealmData ownerRealm, int enemyRealmId)
        {
            RevokeMilitaryAccess(ownerRealm, enemyRealmId);
        }

        // ===== 管制带来的通行成本 =====

        /// <summary>
        /// 计算管制带来的通行成本倍率
        /// </summary>
        public static float CalculateControlCostMultiplier(TileData tile, int armyRealmId,
            RealmData ownerRealm, bool isAtWar)
        {
            if (ownerRealm == null || ownerRealm.realmId == armyRealmId)
                return NoneCostMultiplier;

            var level = GetEffectiveControlLevel(tile, ownerRealm);

            // 战争状态：成本大幅增加（如果强行通过）
            if (isAtWar)
            {
                return level switch
                {
                    GameEnums.MovementControlLevel.None => 1.5f,
                    GameEnums.MovementControlLevel.Loose => 1.8f,
                    GameEnums.MovementControlLevel.Limited => 2.5f,
                    GameEnums.MovementControlLevel.Strict => 999f, // 严格管制战争状态不可通过
                    _ => 1.5f
                };
            }

            // 和平状态
            return level switch
            {
                GameEnums.MovementControlLevel.None => NoneCostMultiplier,
                GameEnums.MovementControlLevel.Loose => LooseCostMultiplier,
                GameEnums.MovementControlLevel.Limited => LimitedCostMultiplier,
                GameEnums.MovementControlLevel.Strict => HasMilitaryAccess(ownerRealm, armyRealmId)
                    ? StrictWithAccessCostMultiplier
                    : 999f, // 无通行权不可通过
                _ => NoneCostMultiplier
            };
        }

        // ===== 管制带来的其他影响 =====

        /// <summary>
        /// 计算军队通过时被征收的关税
        /// </summary>
        /// <param name="tile">地块</param>
        /// <param name="armyRealmId">军队所属政权</param>
        /// <param name="ownerRealm">地块所有者</param>
        /// <param name="cargoValue">军队携带物资价值</param>
        /// <returns>关税金额</returns>
        public static float CalculateTariff(TileData tile, int armyRealmId,
            RealmData ownerRealm, float cargoValue)
        {
            if (ownerRealm == null || ownerRealm.realmId == armyRealmId) return 0f;

            var level = GetEffectiveControlLevel(tile, ownerRealm);
            float rate = level switch
            {
                GameEnums.MovementControlLevel.Loose => LooseTariffRate,
                GameEnums.MovementControlLevel.Limited => LimitedTariffRate,
                GameEnums.MovementControlLevel.Strict => HasMilitaryAccess(ownerRealm, armyRealmId)
                    ? StrictTariffRate : 0f,
                _ => 0f
            };

            return cargoValue * rate;
        }

        /// <summary>
        /// 计算军队被监视的概率（可能泄露军事信息）
        /// </summary>
        public static float CalculateSurveillanceChance(TileData tile, int armyRealmId,
            RealmData ownerRealm)
        {
            if (ownerRealm == null || ownerRealm.realmId == armyRealmId) return 0f;

            var level = GetEffectiveControlLevel(tile, ownerRealm);
            return level switch
            {
                GameEnums.MovementControlLevel.Limited => LimitedSurveillanceChance,
                GameEnums.MovementControlLevel.Strict => HasMilitaryAccess(ownerRealm, armyRealmId)
                    ? StrictSurveillanceChance : 0f,
                _ => 0f
            };
        }

        // ===== 地块管制覆盖管理 =====

        /// <summary>
        /// 设置某地块的单独管制等级覆盖（用于关键城镇/关隘）
        /// </summary>
        public static void SetTileControlOverride(RealmData realm, int tileIndex,
            GameEnums.MovementControlLevel level)
        {
            if (realm == null || realm.tileMovementControlOverrides == null) return;
            realm.tileMovementControlOverrides[tileIndex] = level;
        }

        /// <summary>
        /// 清除某地块的管制覆盖（恢复全国默认）
        /// </summary>
        public static void ClearTileControlOverride(RealmData realm, int tileIndex)
        {
            if (realm == null || realm.tileMovementControlOverrides == null) return;
            realm.tileMovementControlOverrides.Remove(tileIndex);
        }

        // ===== AI 决策辅助 =====

        /// <summary>
        /// AI评估是否应该授予某政权军事通行权
        /// </summary>
        /// <param name="ownerRealm">己方政权</param>
        /// <param name="requesterRealm">请求通行权的政权</param>
        /// <param name="relation">双方关系值（-100到100）</param>
        /// <param name="threatLevel">请求方对己方的威胁等级（0-1）</param>
        /// <returns>是否应该授予</returns>
        public static bool ShouldGrantMilitaryAccess(RealmData ownerRealm, RealmData requesterRealm,
            float relation, float threatLevel)
        {
            if (ownerRealm == null || requesterRealm == null) return false;

            // 关系>=50且威胁<0.3：授予
            if (relation >= 50f && threatLevel < 0.3f) return true;

            // 关系>=30且威胁<0.5：可能授予（50%概率）
            if (relation >= 30f && threatLevel < 0.5f)
                return Random.value > 0.5f;

            // 关系<0或威胁>0.7：拒绝
            if (relation < 0f || threatLevel > 0.7f) return false;

            return false;
        }

        /// <summary>
        /// 获取管制等级描述（用于UI显示）
        /// </summary>
        public static string GetControlLevelDescription(GameEnums.MovementControlLevel level)
        {
            return level switch
            {
                GameEnums.MovementControlLevel.None => "无管制：军队可自由通过",
                GameEnums.MovementControlLevel.Loose => "松散管制：可通过，有关税和检查，速度略降",
                GameEnums.MovementControlLevel.Limited => "有限管制：可通过，需登记，速度明显下降，可能被监视",
                GameEnums.MovementControlLevel.Strict => "严格管制：需军事通行权，否则不可通过",
                _ => "未知"
            };
        }

        /// <summary>
        /// 获取通行状态描述（用于UI显示）
        /// </summary>
        public static string GetPassStatus(TileData tile, int armyRealmId,
            RealmData ownerRealm, bool isAtWar)
        {
            if (ownerRealm == null) return "无主土地：自由通行";
            if (ownerRealm.realmId == armyRealmId) return "己方领土：自由通行";

            var level = GetEffectiveControlLevel(tile, ownerRealm);

            if (isAtWar)
                return $"战争状态：敌国领土，不可通过（管制等级：{level}）";

            if (level == GameEnums.MovementControlLevel.Strict)
            {
                return HasMilitaryAccess(ownerRealm, armyRealmId)
                    ? "严格管制：已获军事通行权，可通过（有通关检查）"
                    : "严格管制：无军事通行权，不可通过";
            }

            return level switch
            {
                GameEnums.MovementControlLevel.None => "无管制：自由通行",
                GameEnums.MovementControlLevel.Loose => "松散管制：可通过（关税+检查）",
                GameEnums.MovementControlLevel.Limited => "有限管制：可通过（登记+监视）",
                _ => "未知"
            };
        }
    }
}
