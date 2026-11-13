using System.Collections.Generic;
using MyriadCreation.Core.Data;
using MyriadCreation.Core.Enums;
using MyriadCreation.Simulation.State;
using MyriadCreation.World.Settlement;
using UnityEngine;
using MyriadCreation.Content.Definitions;

namespace MyriadCreation.Simulation.Systems
{
public static class StampedeSystem
{
	public const float MilitaryBaseChance = 0.3f;

	public const float TrainingStampedeReductionMax = 0.6f;

	public const float InstantCasualtyChanceBonus = 1.5f;

	public const float MilitaryBaseCasualtyRate = 0.05f;

	public const float StampedeOrgLoss = 10f;

	public const float StampedeMoraleLoss = 15f;

	public const float UrbanBaseChance = 0.15f;

	public const float UrbanDensityThreshold = 5000f;

	public const float UrbanBaseCasualtyRate = 0.02f;

	public static StampedeEvent CheckMilitaryStampede(Army army, TileData[] tiles, Dictionary<int, UnitDef> unitDefs, float commanderAbility = 50f, int currentDay = 0)
	{
		if (army == null)
		{
			return null;
		}
		if (army.state != GameEnums.CombatState.Retreating)
		{
			return null;
		}
		if (army.organization > 20f)
		{
			return null;
		}
		float num = 0.3f;
		float num2 = army.training / 100f * 0.6f;
		num *= 1f - num2;
		if (army.instantCasualtyRate > 0.1f)
		{
			num *= 1f + (army.instantCasualtyRate - 0.1f) * 1.5f;
		}
		num *= 1f - commanderAbility / 100f * 0.3f;
		if (tiles[army.currentTileIndex].slopeDegree > 30f)
		{
			num *= 1.3f;
		}
		num = Mathf.Clamp01(num);
		if (Random.value > num)
		{
			return null;
		}
		StampedeEvent stampedeEvent = new StampedeEvent
		{
			type = StampedeType.MilitaryRout,
			tileIndex = army.currentTileIndex,
			realmId = army.ownerRealmId,
			armyId = army.armyId,
			severity = Mathf.Clamp01(0.3f + army.instantCasualtyRate + (20f - army.organization) / 100f),
			startDay = currentDay,
			durationDays = 1,
			description = "军队溃败踩踏：组织度崩溃导致混乱，士兵互相践踏"
		};
		float totalManpower = army.GetTotalManpower(unitDefs);
		float num3 = 0.05f * stampedeEvent.severity * (1f + army.instantCasualtyRate);
		stampedeEvent.casualties = Mathf.RoundToInt(totalManpower * num3);
		ApplyMilitaryStampedeEffects(army, stampedeEvent, unitDefs);
		return stampedeEvent;
	}

	private static void ApplyMilitaryStampedeEffects(Army army, StampedeEvent stampede, Dictionary<int, UnitDef> unitDefs)
	{
		float totalManpower = army.GetTotalManpower(unitDefs);
		if (totalManpower <= 0f)
		{
			return;
		}
		float num = (float)stampede.casualties / totalManpower;
		foreach (int item in new List<int>(army.unitCounts.Keys))
		{
			army.unitCounts[item] = Mathf.Max(0f, army.unitCounts[item] * (1f - num));
			if (army.unitCounts[item] <= 0f)
			{
				army.unitCounts.Remove(item);
			}
		}
		army.organization = Mathf.Max(0f, army.organization - 10f * stampede.severity);
		army.morale = Mathf.Max(0f, army.morale - 15f * stampede.severity);
		army.UpdateCasualtyRates(unitDefs, stampede.casualties);
	}

	public static StampedeEvent CheckUrbanStampede(SettlementData settlement, TileData[] tiles, bool isUnderSiege, bool hasDisaster, float cityStability, int currentDay = 0)
	{
		if (settlement == null)
		{
			return null;
		}
		if (!settlement.IsMajorSettlement)
		{
			return null;
		}
		if (settlement.population < 5000f)
		{
			return null;
		}
		if (!isUnderSiege && !hasDisaster && cityStability > 50f)
		{
			return null;
		}
		float num = 0.15f;
		if (isUnderSiege)
		{
			num *= 2f;
		}
		if (hasDisaster)
		{
			num *= 1.5f;
		}
		if (cityStability < 30f)
		{
			num *= 1.5f;
		}
		num *= 0.5f + settlement.population / 5000f * 0.5f;
		num = Mathf.Clamp01(num);
		if (Random.value > num)
		{
			return null;
		}
		StampedeEvent stampedeEvent = new StampedeEvent
		{
			type = StampedeType.UrbanPanic,
			tileIndex = settlement.tileIndex,
			realmId = tiles[settlement.tileIndex].ownerRealmId,
			anchorId = settlement.anchorId,
			severity = Mathf.Clamp01(0.2f + (isUnderSiege ? 0.3f : 0f) + (hasDisaster ? 0.2f : 0f) + (100f - cityStability) / 200f),
			startDay = currentDay,
			durationDays = 1,
			description = "城市恐慌踩踏：" + settlement.settlementName + "人群拥挤导致混乱"
		};
		float num2 = 0.02f * stampedeEvent.severity;
		stampedeEvent.casualties = Mathf.RoundToInt(settlement.population * num2);
		ApplyUrbanStampedeEffects(settlement, tiles, stampedeEvent);
		return stampedeEvent;
	}

	private static void ApplyUrbanStampedeEffects(SettlementData settlement, TileData[] tiles, StampedeEvent stampede)
	{
		settlement.population = Mathf.Max(0f, settlement.population - (float)stampede.casualties);
		if (settlement.tileIndex >= 0 && settlement.tileIndex < tiles.Length)
		{
			tiles[settlement.tileIndex].stability = Mathf.Max(0f, tiles[settlement.tileIndex].stability - 5f * stampede.severity);
		}
		settlement.development = Mathf.Max(0f, settlement.development - 1f * stampede.severity);
	}

	public static string GetStampedeDescription(StampedeEvent stampede)
	{
		return stampede.type switch
		{
			StampedeType.MilitaryRout => $"【军队溃败踩踏】伤亡{stampede.casualties}人，组织度和士气大幅下降", 
			StampedeType.UrbanPanic => $"【城市恐慌踩踏】{stampede.casualties}人伤亡，城市稳定度下降", 
			StampedeType.Gathering => $"【集会踩踏】{stampede.casualties}人伤亡", 
			_ => $"【踩踏事件】伤亡{stampede.casualties}人", 
		};
	}
}
}
