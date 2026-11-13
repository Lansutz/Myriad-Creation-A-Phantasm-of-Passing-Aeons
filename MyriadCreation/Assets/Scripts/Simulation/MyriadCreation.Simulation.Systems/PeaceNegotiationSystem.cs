using System.Collections.Generic;
using MyriadCreation.Simulation.State;
using UnityEngine;
using MyriadCreation.Content.Definitions;

namespace MyriadCreation.Simulation.Systems
{
public static class PeaceNegotiationSystem
{
	private static int _nextTreatyId = 1;

	public static List<NegotiationClause> GetAvailableDemands(float warScore)
	{
		List<NegotiationClause> list = new List<NegotiationClause>();
		if (warScore >= 10f)
		{
			list.Add(new NegotiationClause
			{
				type = TreatyClauseType.WarReparations,
				description = $"战争赔款 {Mathf.FloorToInt(warScore * 10f)}",
				cost = 10,
				value = Mathf.FloorToInt(warScore * 10f),
				isDemanded = true
			});
		}
		if (warScore >= 15f)
		{
			list.Add(new NegotiationClause
			{
				type = TreatyClauseType.ReleasePrisoners,
				description = "释放所有战俘",
				cost = 10,
				isDemanded = true
			});
		}
		if (warScore >= 20f)
		{
			list.Add(new NegotiationClause
			{
				type = TreatyClauseType.TradePrivileges,
				description = "贸易特权（5年）",
				cost = 15,
				durationDays = 1825,
				isDemanded = true
			});
		}
		if (warScore >= 25f)
		{
			list.Add(new NegotiationClause
			{
				type = TreatyClauseType.Humiliation,
				description = "羞辱（降低稳定度和名声）",
				cost = 15,
				isDemanded = true
			});
		}
		if (warScore >= 30f)
		{
			list.Add(new NegotiationClause
			{
				type = TreatyClauseType.TerritoryCession,
				description = "割让边境领土（后续完善地块选择）",
				cost = 25,
				isDemanded = true
			});
		}
		if (warScore >= 50f)
		{
			list.Add(new NegotiationClause
			{
				type = TreatyClauseType.Disarmament,
				description = "裁军（限制军队规模，10年）",
				cost = 30,
				durationDays = 3650,
				isDemanded = true
			});
		}
		if (warScore >= 50f)
		{
			list.Add(new NegotiationClause
			{
				type = TreatyClauseType.DemilitarizedZone,
				description = "边境非军事化（5年）",
				cost = 25,
				durationDays = 1825,
				isDemanded = true
			});
		}
		if (warScore >= 60f)
		{
			list.Add(new NegotiationClause
			{
				type = TreatyClauseType.NavigationRights,
				description = "航行权（5年）",
				cost = 20,
				durationDays = 1825,
				isDemanded = true
			});
		}
		if (warScore >= 80f)
		{
			list.Add(new NegotiationClause
			{
				type = TreatyClauseType.Vassalage,
				description = "附庸化（对方成为附庸国）",
				cost = 80,
				isDemanded = true
			});
		}
		if (warScore >= 90f)
		{
			list.Add(new NegotiationClause
			{
				type = TreatyClauseType.Annexation,
				description = "吞并（彻底吞并对方）",
				cost = 100,
				isDemanded = true
			});
		}
		return list;
	}

	public static List<NegotiationClause> GetAvailableConcessions()
	{
		return new List<NegotiationClause>
		{
			new NegotiationClause
			{
				type = TreatyClauseType.NonInterference,
				description = "承诺互不侵犯（5年）",
				cost = 10,
				durationDays = 1825,
				isDemanded = false
			},
			new NegotiationClause
			{
				type = TreatyClauseType.TradeRights,
				description = "给予贸易互惠（5年）",
				cost = 10,
				durationDays = 1825,
				isDemanded = false
			},
			new NegotiationClause
			{
				type = TreatyClauseType.AllianceCommitment,
				description = "缔结防御同盟（5年）",
				cost = 20,
				durationDays = 1825,
				isDemanded = false
			},
			new NegotiationClause
			{
				type = TreatyClauseType.RoyalMarriage,
				description = "王室联姻（永久）",
				cost = 15,
				durationDays = -1,
				isDemanded = false
			},
			new NegotiationClause
			{
				type = TreatyClauseType.WarReparations,
				description = "支付和解金 100",
				cost = 20,
				value = 100f,
				isDemanded = false
			},
			new NegotiationClause
			{
				type = TreatyClauseType.ArbitrationAgreement,
				description = "仲裁协定（5年）",
				cost = 10,
				durationDays = 1825,
				isDemanded = false
			}
		};
	}

	public static Treaty GenerateTreatyFromNegotiation(NegotiationState negotiation, int currentDay)
	{
		Treaty treaty = new Treaty
		{
			treatyId = GetNextTreatyId(),
			treatyName = $"和平条约（第{currentDay}日）",
			signerAId = negotiation.attackerId,
			signerBId = negotiation.defenderId,
			signedDay = currentDay,
			expiryDay = currentDay + 1825,
			isActive = true,
			isPeaceTreaty = true,
			warScoreAtSigning = negotiation.attackerWarScore,
			truceUntilDay = currentDay + 1825
		};
		foreach (NegotiationClause attackerDemand in negotiation.attackerDemands)
		{
			treaty.clauses.Add(new TreatyClause
			{
				type = attackerDemand.type,
				description = attackerDemand.description,
				value = attackerDemand.value,
				fromRealmId = negotiation.defenderId,
				toRealmId = negotiation.attackerId,
				durationDays = attackerDemand.durationDays
			});
		}
		foreach (NegotiationClause attackerConcession in negotiation.attackerConcessions)
		{
			treaty.clauses.Add(new TreatyClause
			{
				type = attackerConcession.type,
				description = attackerConcession.description,
				value = attackerConcession.value,
				fromRealmId = negotiation.attackerId,
				toRealmId = negotiation.defenderId,
				durationDays = attackerConcession.durationDays
			});
		}
		treaty.clauses.Add(new TreatyClause
		{
			type = TreatyClauseType.Truce,
			description = "停战 5 年",
			fromRealmId = negotiation.defenderId,
			toRealmId = negotiation.attackerId,
			durationDays = 1825
		});
		return treaty;
	}

	private static int GetNextTreatyId()
	{
		return _nextTreatyId++;
	}

	public static NegotiationState GenerateAINegotiation(int attackerId, int defenderId, float attackerWarScore, float defenderWarScore)
	{
		NegotiationState negotiationState = new NegotiationState
		{
			attackerId = attackerId,
			defenderId = defenderId,
			attackerWarScore = attackerWarScore,
			defenderWarScore = defenderWarScore
		};
		List<NegotiationClause> availableDemands = GetAvailableDemands(attackerWarScore);
		float num;
		if (attackerWarScore >= 80f)
		{
			num = 0.9f;
		}
		else if (attackerWarScore >= 50f)
		{
			num = 0.7f;
		}
		else
		{
			num = ((attackerWarScore >= 30f) ? 0.5f : 0.3f);
		}
		int num2 = Mathf.FloorToInt(attackerWarScore * num);
		int num3 = 0;
		availableDemands.Sort((NegotiationClause a, NegotiationClause b) => b.cost.CompareTo(a.cost));
		foreach (NegotiationClause item in availableDemands)
		{
			if (num3 + item.cost <= num2)
			{
				negotiationState.attackerDemands.Add(item);
				num3 += item.cost;
			}
		}
		if (attackerWarScore < 30f)
		{
			List<NegotiationClause> availableConcessions = GetAvailableConcessions();
			if (availableConcessions.Count > 0)
			{
				negotiationState.attackerConcessions.Add(availableConcessions[0]);
			}
		}
		return negotiationState;
	}

	public static bool WillDefenderAccept(NegotiationState negotiation, float defenderWarScore)
	{
		float num = Mathf.Max(10f, 100f - defenderWarScore);
		return (float)negotiation.AttackerTotalCost <= num;
	}
}
}
