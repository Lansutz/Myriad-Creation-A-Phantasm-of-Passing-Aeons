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

	private readonly int _mapWidth;

	private readonly List<DetectionContact> _lastContacts = new List<DetectionContact>();

	public const float VictoryScore = 100f;

	/// <summary>当日探测接触结果（观察者视角，可能双向重复）；供 AI 选目标/UI 展示，不落盘。</summary>
	public IReadOnlyList<DetectionContact> LastContacts => _lastContacts;

	/// <summary>最近一场战斗的遭遇变量（形态/地形/设防/侦察先手）。</summary>
	public EncounterContext LastEncounter { get; private set; }

	public CombatManager(TileData[] tiles, Dictionary<int, UnitDef> unitDefs, SeaLandGenerator seaLand, SimulationEventBus events = null, int mapWidth = 0)
	{
		_tiles = tiles;
		_unitDefs = unitDefs;
		_seaLand = seaLand;
		_events = events;
		_mapWidth = mapWidth;
	}

	public BattleResult ResolveBattle(Army attacker, Army defender)
	{
		BattleResult result = default;
		attacker.RecordBattleStartManpower(_unitDefs);
		defender.RecordBattleStartManpower(_unitDefs);
		EncounterContext encounter = DetectionContactService.BuildEncounter(attacker, defender, _tiles, 0);
		LastEncounter = encounter;
		float num = attacker.CalculateCombatPower(_unitDefs, _tiles[attacker.currentTileIndex]) * encounter.attackerPowerMultiplier;
		float num2 = defender.CalculateCombatPower(_unitDefs, _tiles[defender.currentTileIndex]) * encounter.defenderPowerMultiplier;
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

	/// <summary>
	/// P11.1/P11.2：夜间突袭——相邻地块才可发动，需将领解锁 night_raid。
	/// 这是"接触≠会战"之外的独立操作：不进入 ResolveBattle，只打乱对方组织与士气。
	/// </summary>
	public bool TryNightRaid(Army raider, Army target, int mapWidth)
	{
		if (raider == null || target == null) return false;
		if (raider.state == GameEnums.CombatState.Dead || target.state == GameEnums.CombatState.Dead) return false;
		CharacterData commander = DetectionContactService.CommanderResolver != null && raider.commanderId >= 0
			? DetectionContactService.CommanderResolver(raider.commanderId)
			: null;
		if (commander == null || !commander.HasPerk("night_raid")) return false;
		if (DetectionContactService.TileDistance(raider.currentTileIndex, target.currentTileIndex, mapWidth) != 1)
		{
			return false;
		}

		float raiderPower = raider.GetTotalManpower(_unitDefs);
		float targetPower = target.GetTotalManpower(_unitDefs);
		float shock = Mathf.Clamp(4f + raiderPower / Mathf.Max(1f, targetPower) * 4f, 4f, 12f);
		target.organization = Mathf.Max(0f, target.organization - shock);
		target.morale = Mathf.Max(0f, target.morale - shock * 0.5f);
		return true;
	}

	public void DailyTick(Dictionary<int, Army> armies, List<WarState> wars, WarRules rules, int day)
	{
		if (wars == null || armies == null)
		{
			return;
		}
		foreach (KeyValuePair<int, Army> army3 in armies)
		{
			DetectionContactService.RefreshScoutRange(army3.Value, _tiles, _unitDefs);
			DetectionContactService.TickScoutDetachment(army3.Value, _mapWidth);
		}
		_lastContacts.Clear();
		var reportedContacts = new HashSet<long>();
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
					if ((ownerRealmId2 != item.attackerId && ownerRealmId2 != item.defenderId) || ownerRealmId2 == ownerRealmId || value2.state == GameEnums.CombatState.Dead)
					{
						continue;
					}
					// P11.2：探测是单向的，各自由自己的斥候决定；双方都看不见才没有接触。
					bool observerSees = DetectionContactService.CanDetect(value, value2, _mapWidth);
					bool targetSees = DetectionContactService.CanDetect(value2, value, _mapWidth);
					if (!observerSees && !targetSees)
					{
						continue;
					}
					int distance = DetectionContactService.TileDistance(value.currentTileIndex, value2.currentTileIndex, _mapWidth);
					long pairKey = ((long)value.armyId << 32) ^ (uint)value2.armyId;
					if (reportedContacts.Add(pairKey))
					{
						_lastContacts.Add(new DetectionContact
						{
							observerArmyId = value.armyId,
							targetArmyId = value2.armyId,
							observerRealmId = ownerRealmId,
							targetRealmId = ownerRealmId2,
							distance = distance,
							mutual = targetSees
						});
					}
					if (distance == 0)
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
				else if (rules != null && rules.allowWhitePeace && war.attackerScore < rules.peaceWhiteScore * 100f && war.defenderScore < rules.peaceWhiteScore * 100f && day - war.startDay >= rules.peaceMinYears * MyriadCreation.Core.Data.GameCalendar.DefaultDaysPerYear)
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
