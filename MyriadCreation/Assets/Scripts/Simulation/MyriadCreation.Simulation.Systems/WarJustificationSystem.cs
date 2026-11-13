using System.Collections.Generic;
using MyriadCreation.Core.Enums;
using MyriadCreation.Simulation.State;
using UnityEngine;
using MyriadCreation.Content.Definitions;

namespace MyriadCreation.Simulation.Systems
{
public static class WarJustificationSystem
{
	private static int _nextCbId = 1;

	private static int _nextGoalId = 1;

	private static int _nextTreatyId = 1;

	public static CasusBelli GenerateRaidReprisalCB(int raidedRealmId, int raiderRealmId, int raidTileIndex, int currentDay, GameEnums.RaidType raidType)
	{
		CasusBelli casusBelli = new CasusBelli();
		casusBelli.cbId = _nextCbId++;
		casusBelli.type = GameEnums.CasusBelliType.RaidReprisal;
		casusBelli.holderRealmId = raidedRealmId;
		casusBelli.targetRealmId = raiderRealmId;
		casusBelli.generatedDay = currentDay;
		casusBelli.expiryDay = currentDay + 1095;
		casusBelli.description = $"报复{raidType}劫掠（地块 #{raidTileIndex}）";
		CasusBelli casusBelli2 = casusBelli;
		casusBelli2.justificationStrength = raidType switch
		{
			GameEnums.RaidType.TownAttack => 80f, 
			GameEnums.RaidType.SlaveRaiding => 75f, 
			GameEnums.RaidType.Massacre => 95f, 
			GameEnums.RaidType.VillageRaid => 60f, 
			GameEnums.RaidType.SupplyRaiding => 40f, 
			GameEnums.RaidType.BorderSkirmish => 30f, 
			_ => 50f, 
		};
		casusBelli.relatedTileIndex = raidTileIndex;
		return casusBelli;
	}

	public static CasusBelli GenerateBorderIncidentCB(int realmA, int realmB, int currentDay)
	{
		return new CasusBelli
		{
			cbId = _nextCbId++,
			type = GameEnums.CasusBelliType.BorderIncident,
			holderRealmId = realmA,
			targetRealmId = realmB,
			generatedDay = currentDay,
			expiryDay = currentDay + 730,
			description = "边境摩擦升级事件",
			justificationStrength = 45f,
			relatedTileIndex = -1
		};
	}

	public static CasusBelli GenerateTerritorialDisputeCB(int claimantRealmId, int targetRealmId, int disputedTileIndex, int currentDay, float strength = 70f)
	{
		return new CasusBelli
		{
			cbId = _nextCbId++,
			type = GameEnums.CasusBelliType.TerritorialDispute,
			holderRealmId = claimantRealmId,
			targetRealmId = targetRealmId,
			generatedDay = currentDay,
			expiryDay = -1,
			description = $"对地块 #{disputedTileIndex} 的领土争端",
			justificationStrength = strength,
			relatedTileIndex = disputedTileIndex
		};
	}

	public static bool HasValidCasusBelli(List<CasusBelli> allCBs, int holderRealmId, int targetRealmId, int currentDay)
	{
		return GetValidCasusBelli(allCBs, holderRealmId, targetRealmId, currentDay).Count > 0;
	}

	public static List<CasusBelli> GetValidCasusBelli(List<CasusBelli> allCBs, int holderRealmId, int targetRealmId, int currentDay)
	{
		List<CasusBelli> list = new List<CasusBelli>();
		foreach (CasusBelli allCB in allCBs)
		{
			if (allCB.holderRealmId == holderRealmId && allCB.targetRealmId == targetRealmId && allCB.IsValid(currentDay))
			{
				list.Add(allCB);
			}
		}
		return list;
	}

	public static CasusBelli GetStrongestCB(List<CasusBelli> allCBs, int holderRealmId, int targetRealmId, int currentDay)
	{
		List<CasusBelli> validCasusBelli = GetValidCasusBelli(allCBs, holderRealmId, targetRealmId, currentDay);
		if (validCasusBelli.Count == 0)
		{
			return null;
		}
		validCasusBelli.Sort((CasusBelli a, CasusBelli b) => b.justificationStrength.CompareTo(a.justificationStrength));
		return validCasusBelli[0];
	}

	public static (float prestigePenalty, float stabilityPenalty, float relationPenalty) CalculateDeclarationPenalties(CasusBelli strongestCB, bool isSurpriseAttack)
	{
		if (strongestCB == null)
		{
			if (!isSurpriseAttack)
			{
				return (prestigePenalty: 30f, stabilityPenalty: 15f, relationPenalty: 10f);
			}
			return (prestigePenalty: 40f, stabilityPenalty: 20f, relationPenalty: 15f);
		}
		float justificationStrength = strongestCB.justificationStrength;
		float item = Mathf.Max(0f, 20f - justificationStrength * 0.2f);
		float item2 = Mathf.Max(0f, 10f - justificationStrength * 0.1f);
		float item3 = Mathf.Max(0f, 8f - justificationStrength * 0.08f);
		return (prestigePenalty: item, stabilityPenalty: item2, relationPenalty: item3);
	}

	public static List<GameEnums.WarGoalType> GetSupportedWarGoals(GameEnums.CasusBelliType cbType)
	{
		return cbType switch
		{
			GameEnums.CasusBelliType.RaidReprisal => new List<GameEnums.WarGoalType>
			{
				GameEnums.WarGoalType.Indemnity,
				GameEnums.WarGoalType.Humiliation,
				GameEnums.WarGoalType.BorderAdjustment
			}, 
			GameEnums.CasusBelliType.BorderIncident => new List<GameEnums.WarGoalType>
			{
				GameEnums.WarGoalType.BorderAdjustment,
				GameEnums.WarGoalType.Humiliation,
				GameEnums.WarGoalType.Indemnity
			}, 
			GameEnums.CasusBelliType.TerritorialDispute => new List<GameEnums.WarGoalType>
			{
				GameEnums.WarGoalType.ConquerTerritory,
				GameEnums.WarGoalType.BorderAdjustment,
				GameEnums.WarGoalType.Indemnity
			}, 
			GameEnums.CasusBelliType.ReligiousConflict => new List<GameEnums.WarGoalType>
			{
				GameEnums.WarGoalType.ConvertReligion,
				GameEnums.WarGoalType.Humiliation,
				GameEnums.WarGoalType.Indemnity
			}, 
			GameEnums.CasusBelliType.HegemonyExpansion => new List<GameEnums.WarGoalType>
			{
				GameEnums.WarGoalType.ConquerRegion,
				GameEnums.WarGoalType.Vassalization,
				GameEnums.WarGoalType.Humiliation
			}, 
			GameEnums.CasusBelliType.ImperialConquest => new List<GameEnums.WarGoalType>
			{
				GameEnums.WarGoalType.ConquerRegion,
				GameEnums.WarGoalType.Annihilation,
				GameEnums.WarGoalType.Vassalization
			}, 
			GameEnums.CasusBelliType.IndependenceWar => new List<GameEnums.WarGoalType>
			{
				GameEnums.WarGoalType.Independence,
				GameEnums.WarGoalType.Humiliation
			}, 
			GameEnums.CasusBelliType.Reconquest => new List<GameEnums.WarGoalType>
			{
				GameEnums.WarGoalType.ConquerTerritory,
				GameEnums.WarGoalType.ConquerRegion
			}, 
			_ => new List<GameEnums.WarGoalType>
			{
				GameEnums.WarGoalType.Indemnity,
				GameEnums.WarGoalType.Humiliation,
				GameEnums.WarGoalType.None
			}, 
		};
	}

	public static WarGoal CreateWarGoal(GameEnums.WarGoalType type, int attackerId, int defenderId, int targetTile = -1, int targetRegion = -1)
	{
		WarGoal warGoal = new WarGoal();
		warGoal.goalId = _nextGoalId++;
		warGoal.type = type;
		warGoal.attackerRealmId = attackerId;
		warGoal.defenderRealmId = defenderId;
		warGoal.targetTileIndex = targetTile;
		warGoal.targetRegionId = targetRegion;
		warGoal.targetWarScore = GetDefaultTargetWarScore(type);
		warGoal.description = type.ToString();
		warGoal.isPrimaryGoal = true;
		return warGoal;
	}

	public static float GetDefaultTargetWarScore(GameEnums.WarGoalType type)
	{
		return type switch
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
			_ => 30f, 
		};
	}

	/// <summary>
	/// 战争目标可变更，但理由（CB）不可变：新目标必须落在原 CB 授权范围内；无 CB 的突袭没有扩张依据。
	/// 冷却、升级成本与威望门槛由世界层执行，本方法只做纯合法性判定。
	/// </summary>
	public static bool CanChangeWarGoal(CasusBelli casusBelli, WarGoal currentGoal, WarGoal proposedGoal, out string reason)
	{
		if (casusBelli == null)
		{
			reason = "no_casus_belli";
			return false;
		}
		if (proposedGoal == null)
		{
			reason = "no_goal";
			return false;
		}
		if (currentGoal != null && currentGoal.type == proposedGoal.type && currentGoal.targetTileIndex == proposedGoal.targetTileIndex && currentGoal.targetRegionId == proposedGoal.targetRegionId)
		{
			reason = "same_goal";
			return false;
		}
		List<GameEnums.WarGoalType> supported = GetSupportedWarGoals(casusBelli.type);
		if (!supported.Contains(proposedGoal.type))
		{
			reason = "goal_not_supported";
			return false;
		}
		reason = "ok";
		return true;
	}

	public static Treaty GenerateWhitePeace(int realmA, int realmB, int currentDay, int truceYears = 5)
	{
		Treaty treaty = new Treaty
		{
			treatyId = _nextTreatyId++,
			treatyName = $"白和条约（第{currentDay}日）",
			signerAId = realmA,
			signerBId = realmB,
			signedDay = currentDay,
			truceUntilDay = currentDay + truceYears * MyriadCreation.Core.Data.GameCalendar.DefaultDaysPerYear,
			goalsFullyAchieved = false,
			isPeaceTreaty = true
		};
		treaty.clauses.Add(new TreatyClause
		{
			type = TreatyClauseType.Truce,
			fromRealmId = realmA,
			toRealmId = realmB,
			durationDays = truceYears * MyriadCreation.Core.Data.GameCalendar.DefaultDaysPerYear,
			description = $"停战 {truceYears} 年"
		});
		return treaty;
	}

	public static Treaty GeneratePeaceTreaty(int winnerId, int loserId, float warScore, List<WarGoal> attackerGoals, int currentDay, int truceYears = 5)
	{
		Treaty treaty = new Treaty
		{
			treatyId = _nextTreatyId++,
			treatyName = $"和平条约（第{currentDay}日）",
			signerAId = winnerId,
			signerBId = loserId,
			signedDay = currentDay,
			warScoreAtSigning = warScore,
			truceUntilDay = currentDay + truceYears * MyriadCreation.Core.Data.GameCalendar.DefaultDaysPerYear
		};
		foreach (WarGoal attackerGoal in attackerGoals)
		{
			treaty.originalWarGoals.Add(attackerGoal.type);
		}
		float num = warScore;
		bool goalsFullyAchieved = true;
		foreach (WarGoal attackerGoal2 in attackerGoals)
		{
			if (attackerGoal2.isPrimaryGoal && warScore < attackerGoal2.targetWarScore)
			{
				goalsFullyAchieved = false;
				break;
			}
		}
		treaty.goalsFullyAchieved = goalsFullyAchieved;
		if (num >= 10f)
		{
			float num2 = Mathf.Min(num * 10f, 500f);
			treaty.clauses.Add(new TreatyClause
			{
				type = TreatyClauseType.WarReparations,
				fromRealmId = loserId,
				toRealmId = winnerId,
				value = num2,
				durationDays = MyriadCreation.Core.Data.GameCalendar.DefaultDaysPerYear,
				description = $"战争赔款 {num2:F0}"
			});
			num -= 10f;
		}
		foreach (WarGoal attackerGoal3 in attackerGoals)
		{
			if (num < attackerGoal3.targetWarScore * 0.5f)
			{
				continue;
			}
			foreach (TreatyClauseType supportedClause in attackerGoal3.GetSupportedClauses())
			{
				if (num < 15f)
				{
					break;
				}
				TreatyClause? treatyClause = GenerateClauseByType(supportedClause, winnerId, loserId, attackerGoal3.targetTileIndex, attackerGoal3.targetRegionId, num);
				if (treatyClause.HasValue)
				{
					treaty.clauses.Add(treatyClause.Value);
					num -= 15f;
				}
			}
		}
		treaty.clauses.Add(new TreatyClause
		{
			type = TreatyClauseType.Truce,
			fromRealmId = loserId,
			toRealmId = winnerId,
			durationDays = truceYears * MyriadCreation.Core.Data.GameCalendar.DefaultDaysPerYear,
			description = $"停战 {truceYears} 年"
		});
		return treaty;
	}

	private static TreatyClause? GenerateClauseByType(TreatyClauseType type, int winnerId, int loserId, int targetTile, int targetRegion, float warScore)
	{
		return type switch
		{
			TreatyClauseType.TerritoryCession => (TreatyClause?)new TreatyClause
			{
				type = type,
				fromRealmId = loserId,
				toRealmId = winnerId,
				tileIndex = targetTile,
				regionId = targetRegion,
				description = ((targetTile >= 0) ? $"割让地块 #{targetTile}" : "领土割让")
			}, 
			TreatyClauseType.Humiliation => new TreatyClause
			{
				type = type,
				fromRealmId = loserId,
				toRealmId = winnerId,
				value = Mathf.Min(warScore * 0.5f, 30f),
				description = $"羞辱（威望 -{Mathf.Min(warScore * 0.5f, 30f):F0}）"
			}, 
			TreatyClauseType.Vassalage => new TreatyClause
			{
				type = type,
				fromRealmId = loserId,
				toRealmId = winnerId,
				durationDays = -1,
				description = "成为附庸"
			}, 
			TreatyClauseType.Disarmament => new TreatyClause
			{
				type = type,
				fromRealmId = loserId,
				toRealmId = winnerId,
				durationDays = 1825,
				description = "裁军5年"
			}, 
			TreatyClauseType.TradePrivileges => new TreatyClause
			{
				type = type,
				fromRealmId = loserId,
				toRealmId = winnerId,
				durationDays = 3650,
				description = "贸易特权10年"
			}, 
			_ => null, 
		};
	}

	public static bool ShouldBorderSkirmishEscalate(float hostilityLevel, int raidCount, float relationValue, float randomRoll)
	{
		float num = 0f;
		if (hostilityLevel >= 70f)
		{
			num += 0.3f;
		}
		if (hostilityLevel >= 90f)
		{
			num += 0.2f;
		}
		if (raidCount >= 3)
		{
			num += 0.2f;
		}
		if (raidCount >= 5)
		{
			num += 0.15f;
		}
		if (relationValue <= -50f)
		{
			num += 0.15f;
		}
		return randomRoll < num;
	}

	public static void ExecutePeaceTreaty(Treaty treaty, DiplomaticRelation rel, int currentDay)
	{
		if (treaty == null || rel == null)
		{
			return;
		}
		foreach (TreatyClause clause in treaty.clauses)
		{
			switch (clause.type)
			{
			case TreatyClauseType.AllianceCommitment:
				rel.activeAlliances.Add(new Alliance
				{
					type = AllianceType.DefensiveAlliance,
					realmAId = treaty.signerAId,
					realmBId = treaty.signerBId,
					signedDay = currentDay,
					durationDays = 1825,
					mutualDefense = true,
					relationRequirement = -20f
				});
				break;
			case TreatyClauseType.NonInterference:
				rel.activeAlliances.Add(new Alliance
				{
					type = AllianceType.NonAggressionPact,
					realmAId = treaty.signerAId,
					realmBId = treaty.signerBId,
					signedDay = currentDay,
					durationDays = 1825,
					relationRequirement = -50f
				});
				break;
			case TreatyClauseType.PersonalUnion:
				rel.SetSpecialBond(SpecialBondType.PersonalUnion);
				break;
			case TreatyClauseType.Independence:
				rel.ClearSpecialBond();
				break;
			}
		}
		rel.truceUntilDay = ((treaty.truceUntilDay > 0) ? treaty.truceUntilDay : (currentDay + 1825));
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
			_ => "未知", 
		};
	}
}
}
