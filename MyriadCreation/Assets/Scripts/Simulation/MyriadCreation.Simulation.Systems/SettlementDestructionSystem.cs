using System.Collections.Generic;
using MyriadCreation.Core.Data;
using MyriadCreation.Simulation.Actors;
using MyriadCreation.Simulation.WorldState;
using MyriadCreation.World.Settlement;
using UnityEngine;
using MyriadCreation.Content.Definitions;
using MyriadCreation.Core.Enums;

namespace MyriadCreation.Simulation.Systems
{
public static class SettlementDestructionSystem
{
	private static readonly float[] PopulationLoss = new float[4] { 0f, 0.15f, 0.4f, 0.85f };

	private static readonly float[] EconomyLoss = new float[4] { 0f, 0.25f, 0.6f, 0.92f };

	private static readonly float[] FortLoss = new float[4] { 0f, 0.4f, 1f, 1f };

	private static readonly int[] RecoveryDays = new int[4] { 0, 730, 2555, 7300 };

	private const float FacilityRepairPerDay = 0.5f;

	public static string DestroySettlement(GameWorld world, int anchorId, DestructionMethod method, int attackerRealmId = -1)
	{
		if ((Object)(object)world == (Object)null || !world.settlements.TryGetValue(anchorId, out var value))
		{
			return null;
		}
		if (value.IsRuined)
		{
			return "聚落已是废墟";
		}
		DestructionSeverity destructionSeverity = MethodToSeverity(method, value);
		value.preRuinLevel = value.settlementLevel;
		value.preRuinForm = value.settlementForm;
		value.ruinedDay = world.currentDay;
		value.ruinLevel = (int)destructionSeverity;
		value.remnantCultureId = value.provinceId;
		value.recoveryProgress = 0f;
		float num = PopulationLoss[(int)destructionSeverity];
		int num2 = Mathf.RoundToInt(value.population * num);
		// D40：人口损失落到地块人口块，聚落快照由聚合重算。
		SettlementPopulationSystem.ApplyCasualties(world, value, num2);
		value.development *= 1f - EconomyLoss[(int)destructionSeverity];
		value.wealth *= 1f - EconomyLoss[(int)destructionSeverity];
		value.tradePower *= 1f - EconomyLoss[(int)destructionSeverity] * 0.7f;
		value.fortification *= 1f - FortLoss[(int)destructionSeverity];
		value.garrison = Mathf.Max(0, value.garrison - Mathf.RoundToInt((float)value.garrison * num));
		ApplyLevelDowngrade(value, destructionSeverity);
		string arg = ApplyMethodEffect(world, value, method, destructionSeverity, attackerRealmId, num2);
		int num3 = SpawnRefugeesFromDestruction(world, value, num2, destructionSeverity);
		if (destructionSeverity == DestructionSeverity.Raze)
		{
			AbandonTileAfterRaze(world, value);
		}
		ApplyFacilityDamage(value, destructionSeverity);
		if (destructionSeverity >= DestructionSeverity.Storm)
		{
			value.hasMarket = false;
			value.hasUniversity = false;
			if (destructionSeverity == DestructionSeverity.Raze)
			{
				value.hasTemple = false;
			}
		}
		Debug.Log((object)($"[Destruction] {value.settlementName} 遭到{method}（{destructionSeverity}），" + $"人口损失{num2}（流民{num3}），发展度降至{value.development:F1}"));
		world.settlements[anchorId] = value;
		return value.settlementName + "遭到" + GetMethodName(method) + "，" + GetSeverityDesc(destructionSeverity) + "，" + $"损失人口{num2}，{num3}人流离失所。{arg}";
	}

	private static DestructionSeverity MethodToSeverity(DestructionMethod method, SettlementData settlement)
	{
		switch (method)
		{
		case DestructionMethod.Plunder:
			return DestructionSeverity.Raid;
		case DestructionMethod.Arson:
			if (settlement.GetEffectiveFortificationLevel() > FortificationLevels.EarthenRampart)
			{
				return DestructionSeverity.Raid;
			}
			return DestructionSeverity.Storm;
		case DestructionMethod.Dismantle:
			return DestructionSeverity.Storm;
		case DestructionMethod.Flood:
			return DestructionSeverity.Raze;
		case DestructionMethod.Massacre:
			return DestructionSeverity.Raze;
		case DestructionMethod.Raze:
			return DestructionSeverity.Raze;
		default:
			return DestructionSeverity.Raid;
		}
	}

	public static void ApplyFacilityDamage(SettlementData settlement, DestructionSeverity severity)
	{
		foreach (FacilityData facility in settlement.facilities)
		{
			facility.repairProgress = 0f;
			switch (severity)
			{
			case DestructionSeverity.Raid:
				if (facility.damageState == FacilityDamageState.Intact)
				{
					facility.damageState = FacilityDamageState.Damaged;
				}
				break;
			case DestructionSeverity.Storm:
				if (facility.facilityType == FacilityType.Wall || facility.facilityType == FacilityType.Gate)
				{
					facility.damageState = FacilityDamageState.Disabled;
				}
				else if (facility.damageState == FacilityDamageState.Intact)
				{
					facility.damageState = FacilityDamageState.Damaged;
				}
				break;
			case DestructionSeverity.Raze:
				facility.damageState = FacilityDamageState.Destroyed;
				facility.constructionState = ConstructionState.Ruined;
				break;
			}
		}
	}

	public static void RepairFacilities(SettlementData settlement)
	{
		foreach (FacilityData facility in settlement.facilities)
		{
			if (facility.damageState == FacilityDamageState.Intact || facility.damageState == FacilityDamageState.Destroyed)
			{
				continue;
			}
			facility.repairProgress += 0.5f;
			if (facility.repairProgress >= 100f)
			{
				facility.repairProgress = 0f;
				facility.damageState--;
				if (facility.damageState == FacilityDamageState.Intact && facility.constructionState == ConstructionState.Ruined)
				{
					facility.constructionState = ConstructionState.Complete;
				}
			}
		}
	}

	private static void ApplyLevelDowngrade(SettlementData settlement, DestructionSeverity severity)
	{
		switch (severity)
		{
		case DestructionSeverity.Raid:
			return;
		case DestructionSeverity.Storm:
			if (settlement.settlementLevel > SettlementLevel.LevelI)
			{
				settlement.settlementLevel--;
			}
			return;
		}
		if (settlement.population < 50f)
		{
			settlement.settlementLevel = SettlementLevel.LevelI;
			settlement.constructionProgress = 0f;
			settlement.constructionTier = 1;
		}
		else
		{
			settlement.settlementLevel = SettlementLevel.LevelI;
			settlement.constructionProgress = Mathf.Max(0f, settlement.constructionProgress - 40f);
			settlement.constructionTier = Mathf.Clamp(Mathf.FloorToInt(settlement.constructionProgress / 20f) + 1, 1, 5);
		}
	}

	private static string ApplyMethodEffect(GameWorld world, SettlementData settlement, DestructionMethod method, DestructionSeverity severity, int attackerId, int lostPop)
	{
		switch (method)
		{
		case DestructionMethod.Plunder:
		{
			float num = settlement.wealth * 0.3f;
			return $"劫掠者获得了价值{num:F0}的物资。";
		}
		case DestructionMethod.Arson:
			DamageAdjacentTiles(world, settlement.tileIndex, 0.05f);
			return "大火蔓延到了周边地区。";
		case DestructionMethod.Dismantle:
			settlement.DisableAllWalls();
			return "城墙被系统性拆除，获得大量建材。";
		case DestructionMethod.Flood:
			if (settlement.tileIndex >= 0 && settlement.tileIndex < world.tiles.Length)
			{
				TileData tileData = world.tiles[settlement.tileIndex];
				tileData.soilHumidityPct = Mathf.Min(100f, tileData.soilHumidityPct + 40f);
				tileData.development *= 0.5f;
				world.tiles[settlement.tileIndex] = tileData;
			}
			return "大水淹没了城区，土地沦为沼泽。";
		case DestructionMethod.Massacre:
			return "屠城的消息传开，远近震恐。";
		case DestructionMethod.Raze:
			settlement.DisableAllWalls();
			return "城市被彻底夷为平地。";
		default:
			return "";
		}
	}

	private static void DamageAdjacentTiles(GameWorld world, int centerTile, float ratio)
	{
		int mapWidth = world.mapWidth;
		int mapHeight = world.mapHeight;
		int num = centerTile % mapWidth;
		int num2 = centerTile / mapWidth;
		for (int i = -1; i <= 1; i++)
		{
			for (int j = -1; j <= 1; j++)
			{
				if (j == 0 && i == 0)
				{
					continue;
				}
				int num3 = num + j;
				int num4 = num2 + i;
				if (num3 < 0 || num3 >= mapWidth || num4 < 0 || num4 >= mapHeight)
				{
					continue;
				}
				int num5 = num4 * mapWidth + num3;
				if (num5 >= 0 && num5 < world.tiles.Length)
				{
					TileData tileData = world.tiles[num5];
					if (tileData.exists)
					{
						tileData.development *= 1f - ratio;
						world.tiles[num5] = tileData;
					}
				}
			}
		}
	}

	private static int SpawnRefugeesFromDestruction(GameWorld world, SettlementData settlement, int lostPop, DestructionSeverity severity)
	{
		if (world.MapActors == null || lostPop <= 0)
		{
			return 0;
		}
		int num = Mathf.RoundToInt((float)lostPop * severity switch
		{
			DestructionSeverity.Storm => 0.6f, 
			DestructionSeverity.Raid => 0.3f, 
			_ => 0.9f, 
		});
		if (num < 5)
		{
			return 0;
		}
		MapActor mapActor = world.MapActors.SpawnActor(MapActorType.Refugee, settlement.tileIndex, num, new RefugeeAI());
		mapActor.actorName = "来自" + settlement.settlementName + "的流民";
		mapActor.morale = 20f;
		mapActor.supplies = 30f;
		return num;
	}

	private static void AbandonTileAfterRaze(GameWorld world, SettlementData settlement)
	{
		if (settlement.tileIndex >= 0 && settlement.tileIndex < world.tiles.Length)
		{
			TileData tileData = world.tiles[settlement.tileIndex];
			tileData.ownerRealmId = -1;
			tileData.order = Mathf.Max(0f, tileData.order - 30f);
			world.tiles[settlement.tileIndex] = tileData;
		}
	}

	public static void DailyTickRecovery(GameWorld world)
	{
		if ((Object)(object)world == (Object)null || world.settlements == null)
		{
			return;
		}
		foreach (KeyValuePair<int, SettlementData> settlement in world.settlements)
		{
			SettlementData value = settlement.Value;
			if (value.ruinLevel <= 0 || !(value.population > 10f))
			{
				continue;
			}
			RepairFacilities(value);
			float num = 0.01f * Mathf.Log10(value.population + 1f);
			value.recoveryProgress += num;
			int num2 = RecoveryDays[value.ruinLevel];
			float num3 = 100f / (float)Mathf.Max(1, num2);
			value.recoveryProgress += num3;
			if (value.recoveryProgress >= 100f)
			{
				if (value.ruinLevel >= 3)
				{
					value.settlementLevel = value.preRuinLevel;
					value.settlementForm = value.preRuinForm;
				}
				value.ruinLevel = 0;
				value.ruinedDay = -1;
				value.recoveryProgress = 0f;
				value.fortification = Mathf.Max(value.fortification, 1f);
				Debug.Log((object)("[Destruction] " + value.settlementName + " 已从废墟中恢复"));
			}
			world.settlements[settlement.Key] = value;
		}
	}

	public static string GetMethodName(DestructionMethod method)
	{
		return method switch
		{
			DestructionMethod.Plunder => "劫掠", 
			DestructionMethod.Arson => "纵火", 
			DestructionMethod.Dismantle => "拆城", 
			DestructionMethod.Flood => "水淹", 
			DestructionMethod.Massacre => "屠城", 
			DestructionMethod.Raze => "夷平", 
			_ => "破坏", 
		};
	}

	public static string GetSeverityDesc(DestructionSeverity severity)
	{
		return severity switch
		{
			DestructionSeverity.Raid => "轻度劫掠", 
			DestructionSeverity.Storm => "城池残破", 
			DestructionSeverity.Raze => "满目疮痍", 
			_ => "", 
		};
	}
}
}
