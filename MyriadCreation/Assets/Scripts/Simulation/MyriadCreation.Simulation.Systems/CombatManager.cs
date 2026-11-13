using System.Collections.Generic;
using MyriadCreation.Core.Data;
using MyriadCreation.Core.Enums;
using MyriadCreation.Core.Events;
using MyriadCreation.Simulation.State;
using MyriadCreation.World.Terrain;
using UnityEngine;
using MyriadCreation.Content.Definitions;

namespace MyriadCreation.Simulation.Systems
{
public class CombatManager
{
	private readonly TileData[] _tiles;

	private readonly Dictionary<int, UnitDef> _unitDefs;

	private readonly SeaLandGenerator _seaLand;

	private readonly SimulationEventBus _events;

	public const float VictoryScore = 100f;

	public CombatManager(TileData[] tiles, Dictionary<int, UnitDef> unitDefs, SeaLandGenerator seaLand, SimulationEventBus events = null)
	{
		_tiles = tiles;
		_unitDefs = unitDefs;
		_seaLand = seaLand;
		_events = events;
	}

	public BattleResult ResolveBattle(Army attacker, Army defender)
	{
		BattleResult result = default;
		attacker.RecordBattleStartManpower(_unitDefs);
		defender.RecordBattleStartManpower(_unitDefs);
		float num = attacker.CalculateCombatPower(_unitDefs, _tiles[attacker.currentTileIndex]);
		float num2 = defender.CalculateCombatPower(_unitDefs, _tiles[defender.currentTileIndex]);
		num2 *= 1.2f;
		float num3 = num * (1f + Random.Range(-0.1f, 0.2f));
		num2 *= 1f + Random.Range(-0.1f, 0.2f);
		float num4 = num3 / Mathf.Max(1f, num2);
		result.attackerWins = num4 > 1.1f;
		bool flag = false;
		Army army = (result.attackerWins ? defender : attacker);
		_ = result.attackerWins;
		for (int i = 0; i < 5; i++)
		{
			if (flag)
			{
				break;
			}
			float num5 = 1f / (float)(5 - i);
			float lossRate = (result.attackerWins ? (Mathf.Clamp(0.1f / num4, 0.05f, 0.3f) * num5) : (Mathf.Clamp(0.2f * num4, 0.1f, 0.5f) * num5));
			float lossRate2 = (result.attackerWins ? (Mathf.Clamp(0.2f * num4, 0.1f, 0.5f) * num5) : (Mathf.Clamp(0.1f / num4, 0.05f, 0.3f) * num5));
			float num6 = ApplyLosses(attacker, lossRate);
			float num7 = ApplyLosses(defender, lossRate2);
			result.attackerLosses += num6;
			result.defenderLosses += num7;
			attacker.UpdateCasualtyRates(_unitDefs, num6);
			defender.UpdateCasualtyRates(_unitDefs, num7);
			attacker.ApplyInstantCasualtyOrgShock();
			defender.ApplyInstantCasualtyOrgShock();
			float num8 = 1f - attacker.training / 100f * 0.5f;
			float num9 = 1f - defender.training / 100f * 0.5f;
			attacker.organization = Mathf.Max(0f, attacker.organization - 8f * num8);
			defender.organization = Mathf.Max(0f, defender.organization - 8f * num9);
			attacker.CheckCombatIneffective();
			defender.CheckCombatIneffective();
			if (defender.isCombatIneffective)
			{
				float num10 = attacker.CalculatePursuitEfficiency(defender);
				float num11 = ApplyLosses(defender, 0.1f * num10);
				result.defenderLosses += num11;
				defender.UpdateCasualtyRates(_unitDefs, num11);
			}
			if (attacker.isCombatIneffective)
			{
				float num12 = defender.CalculatePursuitEfficiency(attacker);
				float num13 = ApplyLosses(attacker, 0.1f * num12);
				result.attackerLosses += num13;
				attacker.UpdateCasualtyRates(_unitDefs, num13);
			}
			if (attacker.CheckBattleEnd(_unitDefs) || defender.CheckBattleEnd(_unitDefs))
			{
				flag = true;
				result.battleEndedByManpower = true;
			}
			if (army.isCombatIneffective && army.CheckBattleEnd(_unitDefs))
			{
				flag = true;
			}
		}
		if (result.attackerWins)
		{
			attacker.organization = Mathf.Max(0f, attacker.organization - 5f);
			attacker.morale = Mathf.Min(100f, attacker.morale + 5f);
			defender.organization = Mathf.Max(0f, defender.organization - 20f);
			defender.morale = Mathf.Max(0f, defender.morale - 20f);
			result.defenderRetreated = defender.ShouldRetreat();
			if (result.defenderRetreated)
			{
				defender.state = GameEnums.CombatState.Retreating;
				StampedeEvent stampedeEvent = StampedeSystem.CheckMilitaryStampede(defender, _tiles, _unitDefs);
				if (stampedeEvent != null)
				{
					result.defenderStampede = stampedeEvent;
					result.defenderLosses += stampedeEvent.casualties;
				}
			}
			else
			{
				defender.state = GameEnums.CombatState.Dead;
			}
		}
		else
		{
			attacker.organization = Mathf.Max(0f, attacker.organization - 20f);
			attacker.morale = Mathf.Max(0f, attacker.morale - 20f);
			defender.organization = Mathf.Max(0f, defender.organization - 5f);
			defender.morale = Mathf.Min(100f, defender.morale + 5f);
			result.attackerRetreated = attacker.ShouldRetreat();
			if (result.attackerRetreated)
			{
				attacker.state = GameEnums.CombatState.Retreating;
				StampedeEvent stampedeEvent2 = StampedeSystem.CheckMilitaryStampede(attacker, _tiles, _unitDefs);
				if (stampedeEvent2 != null)
				{
					result.attackerStampede = stampedeEvent2;
					result.attackerLosses += stampedeEvent2.casualties;
				}
			}
			else
			{
				attacker.state = GameEnums.CombatState.Dead;
			}
		}
		return result;
	}

	private void RecordBattlePractice(Army army)
	{
		if (army == null || _events == null || army.commanderId < 0)
		{
			return;
		}
		foreach (KeyValuePair<int, float> unitCount in army.unitCounts)
		{
			if (!_unitDefs.TryGetValue(unitCount.Key, out var value) || value.requiredInnovations == null || value.requiredInnovations.Count == 0 || unitCount.Value <= 0f)
			{
				continue;
			}
			float num = Mathf.Sqrt(unitCount.Value) * 0.05f;
			if (num <= 0f)
			{
				continue;
			}
			for (int i = 0; i < value.requiredInnovations.Count; i++)
			{
				int num2 = value.requiredInnovations[i];
				if (num2 >= 0)
				{
					_events.Publish(new PracticeRecordedEvent(army.commanderId, num2, num));
				}
			}
		}
	}

	private float ApplyLosses(Army army, float lossRate)
	{
		float num = 0f;
		foreach (int item in new List<int>(army.unitCounts.Keys))
		{
			float num2 = army.unitCounts[item] * lossRate;
			army.unitCounts[item] = Mathf.Max(0f, army.unitCounts[item] - num2);
			num += num2;
			if (army.unitCounts[item] <= 0f)
			{
				army.unitCounts.Remove(item);
			}
		}
		return num;
	}

	public void DailyTick(Dictionary<int, Army> armies, List<WarState> wars, WarRules rules, int day)
	{
		if (wars == null || armies == null)
		{
			return;
		}
		foreach (WarState item in new List<WarState>(wars.FindAll((WarState w) => !w.ended)))
		{
			foreach (KeyValuePair<int, Army> army in armies)
			{
				Army value = army.Value;
				int ownerRealmId = value.ownerRealmId;
				if ((ownerRealmId != item.attackerId && ownerRealmId != item.defenderId) || value.state == GameEnums.CombatState.Dead)
				{
					continue;
				}
				foreach (KeyValuePair<int, Army> army2 in armies)
				{
					if (army2.Key == army.Key)
					{
						continue;
					}
					Army value2 = army2.Value;
					int ownerRealmId2 = value2.ownerRealmId;
					if ((ownerRealmId2 == item.attackerId || ownerRealmId2 == item.defenderId) && ownerRealmId2 != ownerRealmId && value2.state != GameEnums.CombatState.Dead && value2.currentTileIndex == value.currentTileIndex)
					{
						BattleResult battleResult = ResolveBattle(value, value2);
						RecordBattlePractice(value);
						RecordBattlePractice(value2);
						item.lastBattleDay = day;
						if (battleResult.attackerWins)
						{
							float num = Mathf.Clamp(value2.GetTotalManpower(_unitDefs) / Mathf.Max(1f, value.GetTotalManpower(_unitDefs)), 0.5f, 2f);
							item.AddScore(ownerRealmId, rules.scoreBattle * num);
						}
						else
						{
							float num2 = Mathf.Clamp(value.GetTotalManpower(_unitDefs) / Mathf.Max(1f, value2.GetTotalManpower(_unitDefs)), 0.5f, 2f);
							item.AddScore(ownerRealmId2, rules.scoreBattle * num2);
						}
						break;
					}
				}
			}
		}
	}

	public static List<WarState> UpdateWarOutcomes(List<WarState> wars, WarRules rules, int day)
	{
		List<WarState> list = new List<WarState>();
		if (wars == null)
		{
			return list;
		}
		foreach (WarState war in wars)
		{
			if (!war.ended)
			{
				if (war.attackerScore >= 100f)
				{
					war.ended = true;
					war.winnerId = war.attackerId;
					war.outcome = "victory";
					list.Add(war);
				}
				else if (war.defenderScore >= 100f)
				{
					war.ended = true;
					war.winnerId = war.defenderId;
					war.outcome = "victory";
					list.Add(war);
				}
				else if (rules != null && rules.allowWhitePeace && war.attackerScore < rules.peaceWhiteScore * 100f && war.defenderScore < rules.peaceWhiteScore * 100f && day - war.startDay >= rules.peaceMinYears * 365)
				{
					war.ended = true;
					war.winnerId = -1;
					war.outcome = "white_peace";
					list.Add(war);
				}
			}
		}
		return list;
	}
}
}
