using System;
using System.Collections.Generic;
using UnityEngine;
using CivilizationEvolution.Core;

namespace CivilizationEvolution.Diplomacy
{
    /// <summary>
    /// 条约谈判系统（简化版）
    /// 双方各有"让步"和"索取"两个清单，总价值受战争分数限制
    /// 领地索取后续完善接壤检查
    /// </summary>
    public static class PeaceNegotiationSystem
    {
        // ===== 谈判条款 =====

        [Serializable]
        public class NegotiationClause
        {
            public TreatyClauseType type;
            public string description;
            public int cost;
            public float value = 0f;
            public int durationDays = 365 * 5;
            public bool isDemanded = true;
        }

        [Serializable]
        public class NegotiationState
        {
            public int attackerId;
            public int defenderId;
            public float attackerWarScore;
            public float defenderWarScore;
            public List<NegotiationClause> attackerDemands = new List<NegotiationClause>();
            public List<NegotiationClause> attackerConcessions = new List<NegotiationClause>();

            public int AttackerTotalCost
            {
                get
                {
                    int total = 0;
                    foreach (var c in attackerDemands) total += c.cost;
                    foreach (var c in attackerConcessions) total -= c.cost;
                    return total;
                }
            }

            public bool IsAttackerWithinLimit => AttackerTotalCost <= Mathf.CeilToInt(attackerWarScore);
        }

        // ===== 可用条款生成 =====

        public static List<NegotiationClause> GetAvailableDemands(float warScore)
        {
            var demands = new List<NegotiationClause>();

            if (warScore >= 10f)
                demands.Add(new NegotiationClause { type = TreatyClauseType.WarReparations, description = $"战争赔款 {Mathf.FloorToInt(warScore * 10f)}", cost = 10, value = Mathf.FloorToInt(warScore * 10f), isDemanded = true });

            if (warScore >= 15f)
                demands.Add(new NegotiationClause { type = TreatyClauseType.ReleasePrisoners, description = "释放所有战俘", cost = 10, isDemanded = true });

            if (warScore >= 20f)
                demands.Add(new NegotiationClause { type = TreatyClauseType.TradePrivileges, description = "贸易特权（5年）", cost = 15, durationDays = 365 * 5, isDemanded = true });

            if (warScore >= 25f)
                demands.Add(new NegotiationClause { type = TreatyClauseType.Humiliation, description = "羞辱（降低稳定度和名声）", cost = 15, isDemanded = true });

            if (warScore >= 30f)
                demands.Add(new NegotiationClause { type = TreatyClauseType.TerritoryCession, description = "割让边境领土（后续完善地块选择）", cost = 25, isDemanded = true });

            if (warScore >= 50f)
                demands.Add(new NegotiationClause { type = TreatyClauseType.Disarmament, description = "裁军（限制军队规模，10年）", cost = 30, durationDays = 365 * 10, isDemanded = true });

            if (warScore >= 50f)
                demands.Add(new NegotiationClause { type = TreatyClauseType.DemilitarizedZone, description = "边境非军事化（5年）", cost = 25, durationDays = 365 * 5, isDemanded = true });

            if (warScore >= 60f)
                demands.Add(new NegotiationClause { type = TreatyClauseType.NavigationRights, description = "航行权（5年）", cost = 20, durationDays = 365 * 5, isDemanded = true });

            if (warScore >= 80f)
                demands.Add(new NegotiationClause { type = TreatyClauseType.Vassalage, description = "附庸化（对方成为附庸国）", cost = 80, isDemanded = true });

            if (warScore >= 90f)
                demands.Add(new NegotiationClause { type = TreatyClauseType.Annexation, description = "吞并（彻底吞并对方）", cost = 100, isDemanded = true });

            return demands;
        }

        public static List<NegotiationClause> GetAvailableConcessions()
        {
            return new List<NegotiationClause>
            {
                new NegotiationClause { type = TreatyClauseType.NonInterference, description = "承诺互不侵犯（5年）", cost = 10, durationDays = 365 * 5, isDemanded = false },
                new NegotiationClause { type = TreatyClauseType.TradeRights, description = "给予贸易互惠（5年）", cost = 10, durationDays = 365 * 5, isDemanded = false },
                new NegotiationClause { type = TreatyClauseType.AllianceCommitment, description = "缔结防御同盟（5年）", cost = 20, durationDays = 365 * 5, isDemanded = false },
                new NegotiationClause { type = TreatyClauseType.RoyalMarriage, description = "王室联姻（永久）", cost = 15, durationDays = -1, isDemanded = false },
                new NegotiationClause { type = TreatyClauseType.WarReparations, description = "支付和解金 100", cost = 20, value = 100f, isDemanded = false },
                new NegotiationClause { type = TreatyClauseType.ArbitrationAgreement, description = "仲裁协定（5年）", cost = 10, durationDays = 365 * 5, isDemanded = false }
            };
        }

        // ===== 条约生成 =====

        public static Treaty GenerateTreatyFromNegotiation(NegotiationState negotiation, int currentDay)
        {
            var treaty = new Treaty
            {
                treatyId = GetNextTreatyId(),
                treatyName = $"和平条约（第{currentDay}日）",
                signerAId = negotiation.attackerId,
                signerBId = negotiation.defenderId,
                signedDay = currentDay,
                expiryDay = currentDay + 365 * 5,
                isActive = true,
                isPeaceTreaty = true,
                warScoreAtSigning = negotiation.attackerWarScore,
                truceUntilDay = currentDay + 365 * 5
            };

            foreach (var demand in negotiation.attackerDemands)
            {
                treaty.clauses.Add(new TreatyClause
                {
                    type = demand.type,
                    description = demand.description,
                    value = demand.value,
                    fromRealmId = negotiation.defenderId,
                    toRealmId = negotiation.attackerId,
                    durationDays = demand.durationDays
                });
            }

            foreach (var concession in negotiation.attackerConcessions)
            {
                treaty.clauses.Add(new TreatyClause
                {
                    type = concession.type,
                    description = concession.description,
                    value = concession.value,
                    fromRealmId = negotiation.attackerId,
                    toRealmId = negotiation.defenderId,
                    durationDays = concession.durationDays
                });
            }

            treaty.clauses.Add(new TreatyClause
            {
                type = TreatyClauseType.Truce,
                description = "停战 5 年",
                fromRealmId = negotiation.defenderId,
                toRealmId = negotiation.attackerId,
                durationDays = 365 * 5
            });

            return treaty;
        }

        private static int _nextTreatyId = 1;
        private static int GetNextTreatyId() => _nextTreatyId++;

        // ===== AI 谈判 =====

        public static NegotiationState GenerateAINegotiation(
            int attackerId, int defenderId, float attackerWarScore, float defenderWarScore)
        {
            var negotiation = new NegotiationState
            {
                attackerId = attackerId,
                defenderId = defenderId,
                attackerWarScore = attackerWarScore,
                defenderWarScore = defenderWarScore
            };

            var availableDemands = GetAvailableDemands(attackerWarScore);
            float demandRatio = attackerWarScore >= 80f ? 0.9f :
                                 attackerWarScore >= 50f ? 0.7f :
                                 attackerWarScore >= 30f ? 0.5f : 0.3f;

            int targetCost = Mathf.FloorToInt(attackerWarScore * demandRatio);
            int currentCost = 0;

            availableDemands.Sort((a, b) => b.cost.CompareTo(a.cost));
            foreach (var demand in availableDemands)
            {
                if (currentCost + demand.cost <= targetCost)
                {
                    negotiation.attackerDemands.Add(demand);
                    currentCost += demand.cost;
                }
            }

            if (attackerWarScore < 30f)
            {
                var concessions = GetAvailableConcessions();
                if (concessions.Count > 0)
                    negotiation.attackerConcessions.Add(concessions[0]);
            }

            return negotiation;
        }

        public static bool WillDefenderAccept(NegotiationState negotiation, float defenderWarScore)
        {
            float maxAcceptable = Mathf.Max(10f, 100f - defenderWarScore);
            return negotiation.AttackerTotalCost <= maxAcceptable;
        }
    }
}
