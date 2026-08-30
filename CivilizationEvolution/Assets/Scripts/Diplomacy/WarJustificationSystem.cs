using System;
using System.Collections.Generic;
using CivilizationEvolution.Core;
using UnityEngine;

namespace CivilizationEvolution.Diplomacy
{
    /// <summary>
    /// 战争借口（Casus Belli）——为什么开战
    /// 由敌对活动、领土争端、宗教冲突等事件生成
    /// 有战争借口时宣战无惩罚（或惩罚小），无借口宣战有高惩罚
    /// </summary>
    [Serializable]
    public class CasusBelli
    {
        public int cbId;
        public GameEnums.CasusBelliType type;
        public int holderRealmId;      // 借口持有方（可以用这个借口宣战的一方）
        public int targetRealmId;      // 借口针对方
        public int generatedDay;       // 生成日期
        public int expiryDay;           // 过期日期（-1=永不过期）
        public string description;       // 借口描述
        public float justificationStrength; // 正当性强度（0-100，影响宣战惩罚和战争目标选择）
        public int relatedTileIndex;    // 相关地块（领土争端/劫掠地点等，-1=无）
        public bool isUsed;             // 是否已被使用（宣战后标记为已用）

        /// <summary>是否有效（未过期、未使用）</summary>
        public bool IsValid(int currentDay)
        {
            return !isUsed && (expiryDay < 0 || currentDay < expiryDay);
        }
    }

    /// <summary>
    /// 战争目标（War Goal）——开战想要达到什么目的
    /// 决定战争分数的计算方式和和平条约的可选条款
    /// 与战争借口关联：不同借口支持不同的战争目标
    /// </summary>
    [Serializable]
    public class WarGoal
    {
        public int goalId;
        public GameEnums.WarGoalType type;
        public int attackerRealmId;
        public int defenderRealmId;
        public int targetTileIndex;     // 目标地块（夺取领土/边境调整等，-1=无）
        public int targetRegionId;      // 目标地区（-1=无）
        public float targetWarScore;    // 达成目标所需战争分数（0-100）
        public string description;
        public bool isPrimaryGoal;      // 是否为主要战争目标（一场战争可有多个目标）

        /// <summary>获取该战争目标支持的条约条款类型</summary>
        public List<TreatyClauseType> GetSupportedClauses()
        {
            return type switch
            {
                GameEnums.WarGoalType.ConquerTerritory => new List<TreatyClauseType>
                    { TreatyClauseType.TerritoryCession, TreatyClauseType.WarReparations, TreatyClauseType.Truce },
                GameEnums.WarGoalType.ConquerRegion => new List<TreatyClauseType>
                    { TreatyClauseType.TerritoryCession, TreatyClauseType.WarReparations, TreatyClauseType.Humiliation, TreatyClauseType.Truce },
                GameEnums.WarGoalType.Vassalization => new List<TreatyClauseType>
                    { TreatyClauseType.Vassalage, TreatyClauseType.WarReparations, TreatyClauseType.Truce },
                GameEnums.WarGoalType.Indemnity => new List<TreatyClauseType>
                    { TreatyClauseType.WarReparations, TreatyClauseType.TradePrivileges, TreatyClauseType.Truce },
                GameEnums.WarGoalType.Humiliation => new List<TreatyClauseType>
                    { TreatyClauseType.Humiliation, TreatyClauseType.WarReparations, TreatyClauseType.Truce },
                GameEnums.WarGoalType.Annihilation => new List<TreatyClauseType>
                    { TreatyClauseType.Annexation, TreatyClauseType.TerritoryCession, TreatyClauseType.WarCrimesTrial },
                GameEnums.WarGoalType.BorderAdjustment => new List<TreatyClauseType>
                    { TreatyClauseType.TerritoryCession, TreatyClauseType.BorderDemilitarization, TreatyClauseType.Truce },
                GameEnums.WarGoalType.ConvertReligion => new List<TreatyClauseType>
                    { TreatyClauseType.ReligiousFreedom, TreatyClauseType.WarReparations, TreatyClauseType.Truce },
                _ => new List<TreatyClauseType> { TreatyClauseType.WarReparations, TreatyClauseType.Truce }
            };
        }
    }

    /// <summary>
    /// 和平条约条款——实际得到什么
    /// 战争结束后根据战争分数和战争目标生成具体条款
    /// </summary>




    /// <summary>
    /// 战争正当性系统（War Justification System）
    /// 管理战争借口的生成、检查、使用，以及战争目标和和平条约的生成
    ///
    /// 三层分离：
    /// 1. 战争借口（Casus Belli）——为什么开战（理由）
    /// 2. 战争目标（War Goal）——开战想要达到什么目的
    /// 3. 实际条约（Peace Treaty）——战争结束后实际得到什么
    /// </summary>
    public static class WarJustificationSystem
    {
        private static int _nextCbId = 1;
        private static int _nextGoalId = 1;
        private static int _nextTreatyId = 1;

        // ===== 战争借口生成 =====

        /// <summary>
        /// 劫掠事件生成战争借口（被劫掠方获得劫掠报复借口）
        /// </summary>
        public static CasusBelli GenerateRaidReprisalCB(int raidedRealmId, int raiderRealmId,
            int raidTileIndex, int currentDay, GameEnums.RaidType raidType)
        {
            var cb = new CasusBelli
            {
                cbId = _nextCbId++,
                type = GameEnums.CasusBelliType.RaidReprisal,
                holderRealmId = raidedRealmId,
                targetRealmId = raiderRealmId,
                generatedDay = currentDay,
                expiryDay = currentDay + 365 * 3, // 3年有效期
                description = $"报复{raidType}劫掠（地块 #{raidTileIndex}）",
                justificationStrength = raidType switch
                {
                    GameEnums.RaidType.TownAttack => 80f,
                    GameEnums.RaidType.SlaveRaiding => 75f,
                    GameEnums.RaidType.VillageRaid => 60f,
                    GameEnums.RaidType.SupplyRaiding => 40f,
                    GameEnums.RaidType.BorderSkirmish => 30f,
                    _ => 50f
                },
                relatedTileIndex = raidTileIndex
            };
            return cb;
        }

        /// <summary>
        /// 边境摩擦生成战争借口（边境事件借口，可升级为边境战争）
        /// </summary>
        public static CasusBelli GenerateBorderIncidentCB(int realmA, int realmB, int currentDay)
        {
            return new CasusBelli
            {
                cbId = _nextCbId++,
                type = GameEnums.CasusBelliType.BorderIncident,
                holderRealmId = realmA,
                targetRealmId = realmB,
                generatedDay = currentDay,
                expiryDay = currentDay + 365 * 2, // 2年有效期
                description = "边境摩擦升级事件",
                justificationStrength = 45f,
                relatedTileIndex = -1
            };
        }

        /// <summary>
        /// 领土争端生成战争借口
        /// </summary>
        public static CasusBelli GenerateTerritorialDisputeCB(int claimantRealmId, int targetRealmId,
            int disputedTileIndex, int currentDay, float strength = 70f)
        {
            return new CasusBelli
            {
                cbId = _nextCbId++,
                type = GameEnums.CasusBelliType.TerritorialDispute,
                holderRealmId = claimantRealmId,
                targetRealmId = targetRealmId,
                generatedDay = currentDay,
                expiryDay = -1, // 领土争端永不过期
                description = $"对地块 #{disputedTileIndex} 的领土争端",
                justificationStrength = strength,
                relatedTileIndex = disputedTileIndex
            };
        }

        /// <summary>
        /// 检查某政权对另一政权是否有有效战争借口
        /// </summary>
        public static bool HasValidCasusBelli(List<CasusBelli> allCBs, int holderRealmId,
            int targetRealmId, int currentDay)
        {
            return GetValidCasusBelli(allCBs, holderRealmId, targetRealmId, currentDay).Count > 0;
        }

        /// <summary>
        /// 获取某政权对另一政权的所有有效战争借口
        /// </summary>
        public static List<CasusBelli> GetValidCasusBelli(List<CasusBelli> allCBs, int holderRealmId,
            int targetRealmId, int currentDay)
        {
            var result = new List<CasusBelli>();
            foreach (var cb in allCBs)
            {
                if (cb.holderRealmId == holderRealmId && cb.targetRealmId == targetRealmId &&
                    cb.IsValid(currentDay))
                    result.Add(cb);
            }
            return result;
        }

        /// <summary>
        /// 获取最强的战争借口（用于宣战惩罚计算）
        /// </summary>
        public static CasusBelli GetStrongestCB(List<CasusBelli> allCBs, int holderRealmId,
            int targetRealmId, int currentDay)
        {
            var valid = GetValidCasusBelli(allCBs, holderRealmId, targetRealmId, currentDay);
            if (valid.Count == 0) return null;
            valid.Sort((a, b) => b.justificationStrength.CompareTo(a.justificationStrength));
            return valid[0];
        }

        // ===== 宣战惩罚计算 =====

        /// <summary>
        /// 计算宣战惩罚（根据战争借口强度）
        /// </summary>
        /// <returns>名声惩罚、稳定度惩罚、其他政权关系惩罚</returns>
        public static (float prestigePenalty, float stabilityPenalty, float relationPenalty)
            CalculateDeclarationPenalties(CasusBelli strongestCB, bool isSurpriseAttack)
        {
            if (strongestCB == null)
            {
                // 无战争借口 = 不宣而战，高惩罚
                return isSurpriseAttack ?
                    (40f, 20f, 15f) : // 不宣而战
                    (30f, 15f, 10f);   // 无借口宣战
            }

            // 有战争借口，根据正当性强度计算惩罚
            float strength = strongestCB.justificationStrength;
            float prestigePenalty = Mathf.Max(0f, 20f - strength * 0.2f);
            float stabilityPenalty = Mathf.Max(0f, 10f - strength * 0.1f);
            float relationPenalty = Mathf.Max(0f, 8f - strength * 0.08f);

            return (prestigePenalty, stabilityPenalty, relationPenalty);
        }

        // ===== 战争目标生成 =====

        /// <summary>
        /// 根据战争借口生成可选战争目标
        /// </summary>
        public static List<GameEnums.WarGoalType> GetSupportedWarGoals(GameEnums.CasusBelliType cbType)
        {
            return cbType switch
            {
                GameEnums.CasusBelliType.RaidReprisal => new List<GameEnums.WarGoalType>
                    { GameEnums.WarGoalType.Indemnity, GameEnums.WarGoalType.Humiliation, GameEnums.WarGoalType.BorderAdjustment },
                GameEnums.CasusBelliType.BorderIncident => new List<GameEnums.WarGoalType>
                    { GameEnums.WarGoalType.BorderAdjustment, GameEnums.WarGoalType.Humiliation, GameEnums.WarGoalType.Indemnity },
                GameEnums.CasusBelliType.TerritorialDispute => new List<GameEnums.WarGoalType>
                    { GameEnums.WarGoalType.ConquerTerritory, GameEnums.WarGoalType.BorderAdjustment, GameEnums.WarGoalType.Indemnity },
                GameEnums.CasusBelliType.ReligiousConflict => new List<GameEnums.WarGoalType>
                    { GameEnums.WarGoalType.ConvertReligion, GameEnums.WarGoalType.Humiliation, GameEnums.WarGoalType.Indemnity },
                GameEnums.CasusBelliType.HegemonyExpansion => new List<GameEnums.WarGoalType>
                    { GameEnums.WarGoalType.ConquerRegion, GameEnums.WarGoalType.Vassalization, GameEnums.WarGoalType.Humiliation },
                GameEnums.CasusBelliType.ImperialConquest => new List<GameEnums.WarGoalType>
                    { GameEnums.WarGoalType.ConquerRegion, GameEnums.WarGoalType.Annihilation, GameEnums.WarGoalType.Vassalization },
                GameEnums.CasusBelliType.IndependenceWar => new List<GameEnums.WarGoalType>
                    { GameEnums.WarGoalType.Independence, GameEnums.WarGoalType.Humiliation },
                GameEnums.CasusBelliType.Reconquest => new List<GameEnums.WarGoalType>
                    { GameEnums.WarGoalType.ConquerTerritory, GameEnums.WarGoalType.ConquerRegion },
                _ => new List<GameEnums.WarGoalType>
                    { GameEnums.WarGoalType.Indemnity, GameEnums.WarGoalType.Humiliation, GameEnums.WarGoalType.None }
            };
        }

        /// <summary>
        /// 创建战争目标
        /// </summary>
        public static WarGoal CreateWarGoal(GameEnums.WarGoalType type, int attackerId, int defenderId,
            int targetTile = -1, int targetRegion = -1)
        {
            return new WarGoal
            {
                goalId = _nextGoalId++,
                type = type,
                attackerRealmId = attackerId,
                defenderRealmId = defenderId,
                targetTileIndex = targetTile,
                targetRegionId = targetRegion,
                targetWarScore = type switch
                {
                    GameEnums.WarGoalType.ConquerTerritory => 50f,
                    GameEnums.WarGoalType.ConquerRegion => 75f,
                    GameEnums.WarGoalType.Vassalization => 80f,
                    GameEnums.WarGoalType.Indemnity => 30f,
                    GameEnums.WarGoalType.Humiliation => 25f,
                    GameEnums.WarGoalType.Annihilation => 100f,
                    GameEnums.WarGoalType.BorderAdjustment => 40f,
                    GameEnums.WarGoalType.ConvertReligion => 60f,
                    GameEnums.WarGoalType.Independence => 70f,
                    _ => 30f
                },
                description = type.ToString(),
                isPrimaryGoal = true
            };
        }

        // ===== 和平条约生成 =====

        /// <summary>
        /// 根据战争分数和战争目标生成和平条约
        /// 区分战争目标（想要的）和实际条约（得到的）
        /// </summary>
        public static Treaty GeneratePeaceTreaty(int winnerId, int loserId, float warScore,
            List<WarGoal> attackerGoals, int currentDay, int truceYears = 5)
        {
            var treaty = new Treaty
            {
                treatyId = _nextTreatyId++,
                treatyName = $"和平条约（第{currentDay}日）",
                signerAId = winnerId,
                signerBId = loserId,
                signedDay = currentDay,
                warScoreAtSigning = warScore,
                truceUntilDay = currentDay + truceYears * 365
            };

            // 记录原战争目标
            foreach (var goal in attackerGoals)
                treaty.originalWarGoals.Add(goal.type);

            // 根据战争分数生成条约条款
            float remainingScore = warScore;
            bool goalsFullyAchieved = true;

            // 检查主要战争目标是否达成
            foreach (var goal in attackerGoals)
            {
                if (goal.isPrimaryGoal && warScore < goal.targetWarScore)
                {
                    goalsFullyAchieved = false;
                    break;
                }
            }
            treaty.goalsFullyAchieved = goalsFullyAchieved;

            // 生成赔款条款（基础条款）
            if (remainingScore >= 10f)
            {
                float indemnity = Mathf.Min(remainingScore * 10f, 500f);
                treaty.clauses.Add(new TreatyClause
                {
                    type = TreatyClauseType.WarReparations,
                    fromRealmId = loserId,
                    toRealmId = winnerId,
                    value = indemnity,
                    durationDays = 365,
                    description = $"战争赔款 {indemnity:F0}"
                });
                remainingScore -= 10f;
            }

            // 根据战争目标生成对应条款
            foreach (var goal in attackerGoals)
            {
                if (remainingScore < goal.targetWarScore * 0.5f) continue;

                var supportedClauses = goal.GetSupportedClauses();
                foreach (var clauseType in supportedClauses)
                {
                    if (remainingScore < 15f) break;

                    var clause = GenerateClauseByType(clauseType, winnerId, loserId,
                        goal.targetTileIndex, goal.targetRegionId, remainingScore);
                    if (clause.HasValue)
                    {
                        treaty.clauses.Add(clause.Value);
                        remainingScore -= 15f;
                    }
                }
            }

            // 停战协定（必有）
            treaty.clauses.Add(new TreatyClause
            {
                type = TreatyClauseType.Truce,
                fromRealmId = loserId,
                toRealmId = winnerId,
                durationDays = truceYears * 365,
                description = $"停战 {truceYears} 年"
            });

            return treaty;
        }

        /// <summary>
        /// 根据条款类型生成具体条款
        /// </summary>
        private static TreatyClause? GenerateClauseByType(TreatyClauseType type,
            int winnerId, int loserId, int targetTile, int targetRegion, float warScore)
        {
            return type switch
            {
                TreatyClauseType.TerritoryCession => new TreatyClause
                {
                    type = type, fromRealmId = loserId, toRealmId = winnerId,
                    tileIndex = targetTile, regionId = targetRegion,
                    description = targetTile >= 0 ? $"割让地块 #{targetTile}" : "领土割让"
                },
                TreatyClauseType.Humiliation => new TreatyClause
                {
                    type = type, fromRealmId = loserId, toRealmId = winnerId,
                    value = Mathf.Min(warScore * 0.5f, 30f),
                    description = $"羞辱（威望 -{Mathf.Min(warScore * 0.5f, 30f):F0}）"
                },
                TreatyClauseType.Vassalage => new TreatyClause
                {
                    type = type, fromRealmId = loserId, toRealmId = winnerId,
                    durationDays = -1, description = "成为附庸"
                },
                TreatyClauseType.Disarmament => new TreatyClause
                {
                    type = type, fromRealmId = loserId, toRealmId = winnerId,
                    durationDays = 365 * 5, description = "裁军5年"
                },
                TreatyClauseType.TradePrivileges => new TreatyClause
                {
                    type = type, fromRealmId = loserId, toRealmId = winnerId,
                    durationDays = 365 * 10, description = "贸易特权10年"
                },
                _ => null
            };
        }

        // ===== 边境摩擦 vs 边境战争区分 =====

        /// <summary>
        /// 检查边境摩擦是否升级为边境战争
        /// 边境摩擦是低烈度冲突（不触发战争），边境战争是有限战争（有战争借口和战争目标）
        /// </summary>
        public static bool ShouldBorderSkirmishEscalate(float hostilityLevel, int raidCount,
            float relationValue, float randomRoll)
        {
            // 升级条件：敌对程度高 + 累计摩擦/劫掠次数多 + 关系差
            float escalationChance = 0f;
            if (hostilityLevel >= 70f) escalationChance += 0.3f;
            if (hostilityLevel >= 90f) escalationChance += 0.2f;
            if (raidCount >= 3) escalationChance += 0.2f;
            if (raidCount >= 5) escalationChance += 0.15f;
            if (relationValue <= -50f) escalationChance += 0.15f;

            return randomRoll < escalationChance;
        }

        /// <summary>
        /// 获取战争借口描述（用于UI显示）
        /// </summary>
        /// <summary>
        /// 执行和平条约条款（对接现有外交系统）
        /// 将条约条款转化为Alliance/Subordination/SpecialBond等现有结构
        /// </summary>
        public static void ExecutePeaceTreaty(Treaty treaty, DiplomaticRelation rel, int currentDay)
        {
            if (treaty == null || rel == null) return;
            foreach (var clause in treaty.clauses)
            {
                switch (clause.type)
                {
                    case TreatyClauseType.AllianceCommitment:
                        rel.activeAlliances.Add(new Alliance { type = AllianceType.DefensiveAlliance, realmAId = treaty.signerAId, realmBId = treaty.signerBId, signedDay = currentDay, durationDays = 365 * 5, mutualDefense = true, relationRequirement = -20f });
                        break;
                    case TreatyClauseType.NonInterference:
                        rel.activeAlliances.Add(new Alliance { type = AllianceType.NonAggressionPact, realmAId = treaty.signerAId, realmBId = treaty.signerBId, signedDay = currentDay, durationDays = 365 * 5, relationRequirement = -50f });
                        break;
                    case TreatyClauseType.TradeRights:
                    case TreatyClauseType.TradePrivileges:
                    case TreatyClauseType.NavigationRights:
                    case TreatyClauseType.ResourceConcession:
                        // 经济类条款：记录在条约中，由经济系统执行（贸易权/航行权/资源特许权）
                        break;
                    case TreatyClauseType.PersonalUnion:
                        rel.SetSpecialBond(SpecialBondType.PersonalUnion);
                        break;
                    case TreatyClauseType.Independence:
                        rel.ClearSpecialBond();
                        break;
                    case TreatyClauseType.RoyalMarriage:
                        // 王室联姻：记录在条约中，由人物/王朝系统执行
                        break;
                    case TreatyClauseType.ArbitrationAgreement:
                    case TreatyClauseType.ReligiousFreedom:
                    case TreatyClauseType.MinorityProtection:
                    case TreatyClauseType.CulturalAssimilation:
                        // 人文类条款：记录在条约中，由文化/宗教系统执行
                        break;
                    default:
                        // 其他条款（领土/赔款/裁军等）记录在条约中，由对应系统执行
                        break;
                }
            }
            rel.truceUntilDay = treaty.truceUntilDay > 0 ? treaty.truceUntilDay : currentDay + 5 * 365;
        }

        public static string GetCBDescription(GameEnums.CasusBelliType type)
        {
            return type switch
            {
                GameEnums.CasusBelliType.None => "无借口（不宣而战）",
                GameEnums.CasusBelliType.RaidReprisal => "劫掠报复",
                GameEnums.CasusBelliType.BorderIncident => "边境事件",
                GameEnums.CasusBelliType.TerritorialDispute => "领土争端",
                GameEnums.CasusBelliType.ReligiousConflict => "宗教冲突",
                GameEnums.CasusBelliType.AllianceObligation => "联盟义务",
                GameEnums.CasusBelliType.HegemonyExpansion => "霸权扩张",
                GameEnums.CasusBelliType.DynasticClaim => "王朝宣称",
                GameEnums.CasusBelliType.TradeDispute => "贸易争端",
                GameEnums.CasusBelliType.IndependenceWar => "独立战争",
                GameEnums.CasusBelliType.Reconquest => "收复失地",
                GameEnums.CasusBelliType.Crusade => "圣战",
                GameEnums.CasusBelliType.ImperialConquest => "帝国征服",
                GameEnums.CasusBelliType.CivilWar => "内战",
                GameEnums.CasusBelliType.Intervention => "武装干涉",
                _ => "未知"
            };
        }
    }
}