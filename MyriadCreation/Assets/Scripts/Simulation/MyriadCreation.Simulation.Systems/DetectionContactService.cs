using System.Collections.Generic;
using MyriadCreation.Content.Definitions;
using MyriadCreation.Core.Data;
using MyriadCreation.Core.Enums;
using MyriadCreation.Simulation.Genetics;
using MyriadCreation.Simulation.State;
using UnityEngine;

namespace MyriadCreation.Simulation.Systems
{
/// <summary>遭遇形态：不强制硬类型，只给结算提供变量。</summary>
public enum EncounterKind
{
	None,
	/// <summary>斥候/前卫接触，规模小、双方战力都打折。</summary>
	Skirmish,
	/// <summary>主力会战。</summary>
	Battle,
	/// <summary>对设防地块的攻坚。</summary>
	Siege
}

/// <summary>一次探测接触：谁先看到谁、距离多远。仅当日有效，不落盘。</summary>
public sealed class DetectionContact
{
	public int observerArmyId;
	public int targetArmyId;
	public int observerRealmId;
	public int targetRealmId;
	public int distance;
	public bool mutual;
}

/// <summary>遭遇结算变量：地形、设防、侦察先手、遭遇形态。</summary>
public struct EncounterContext
{
	public EncounterKind kind;
	public float attackerPowerMultiplier;
	public float defenderPowerMultiplier;
}

/// <summary>
/// D37 探测/接触层：先有斥候可达范围，才有接敌；不做全知视野。
/// 范围由机动兵力占比、行军状态、补给/训练与所在地形共同决定，纯函数、可复现。
/// </summary>
public static class DetectionContactService
{
	public const int BaseScoutRange = 1;

	public const int MaxScoutRange = 6;

	/// <summary>指挥官查询（由 GameWorld 注入）；未注入时视为无指挥官表型加成。</summary>
	public static System.Func<int, CharacterData> CommanderResolver;

	public static void RefreshScoutRange(Army army, TileData[] tiles, Dictionary<int, UnitDef> unitDefs)
	{
		if (army != null)
		{
			army.scoutRange = ComputeScoutRange(army, tiles, unitDefs);
		}
	}

	public static int ComputeScoutRange(Army army, TileData[] tiles, Dictionary<int, UnitDef> unitDefs)
	{
		if (army == null || unitDefs == null)
		{
			return 0;
		}
		int range = BaseScoutRange;
		float total = 0f;
		float mobile = 0f;
		foreach (KeyValuePair<int, float> unitCount in army.unitCounts)
		{
			if (!unitDefs.TryGetValue(unitCount.Key, out UnitDef def))
			{
				continue;
			}
			total += unitCount.Value;
			if (def.category == GameEnums.UnitArmType.Cavalry)
			{
				mobile += unitCount.Value;
			}
		}
		if (total > 0f)
		{
			range += Mathf.FloorToInt(Mathf.Clamp01(mobile / total) * 3f);
		}
		if (army.state == GameEnums.CombatState.Marching)
		{
			range += 1;
		}
		if (army.supply >= 50f && army.training >= 50f)
		{
			range += 1;
		}
		if (army.supply < 20f)
		{
			range -= 1;
		}
		if (tiles != null && army.currentTileIndex >= 0 && army.currentTileIndex < tiles.Length)
		{
			TileData tile = tiles[army.currentTileIndex];
			if (tile.elevation01 > 0.5f || IsDenseTerrain(tile.biome))
			{
				range -= 1;
			}
			if (tile.roadLevel != GameEnums.RoadLevel.None)
			{
				range += 1;
			}
		}
		if (CommanderResolver != null && army.commanderId >= 0)
		{
			CharacterData commander = CommanderResolver(army.commanderId);
			if (commander != null)
			{
				range += PhenotypeMechanics.ScoutRangeBonus(PhenotypeMechanics.PhenotypeOf(commander));
			}
		}
		return Mathf.Clamp(range, 0, MaxScoutRange);
	}

	private static bool IsDenseTerrain(GameEnums.BiomeType biome)
	{
		return biome == GameEnums.BiomeType.BorealForest || biome == GameEnums.BiomeType.DeciduousForest || biome == GameEnums.BiomeType.TropicalRainforest || biome == GameEnums.BiomeType.WetMarshPlain;
	}

	/// <summary>切比雪夫距离（八向邻接），与地图寻路邻接口径一致。</summary>
	public static int TileDistance(int a, int b, int mapWidth)
	{
		if (mapWidth <= 0)
		{
			return (a == b) ? 0 : int.MaxValue;
		}
		int ax = a % mapWidth;
		int ay = a / mapWidth;
		int bx = b % mapWidth;
		int by = b / mapWidth;
		return Mathf.Max(Mathf.Abs(ax - bx), Mathf.Abs(ay - by));
	}

	/// <summary>双方任一侧的斥候可达范围覆盖对方，才算接触。</summary>
	public static bool CanContact(Army observer, Army target, int mapWidth)
	{
		if (observer == null || target == null)
		{
			return false;
		}
		int distance = TileDistance(observer.currentTileIndex, target.currentTileIndex, mapWidth);
		int reach = Mathf.Max(observer.scoutRange, target.scoutRange);
		return distance <= reach;
	}

	/// <summary>接触先手与地形/设防修正；不改变谁是攻方，只改结算权重。</summary>
	public static EncounterContext BuildEncounter(Army attacker, Army defender, TileData[] tiles, int distance)
	{
		EncounterContext context = new EncounterContext
		{
			kind = EncounterKind.Battle,
			attackerPowerMultiplier = 1f,
			defenderPowerMultiplier = 1f
		};
		if (attacker == null || defender == null || tiles == null)
		{
			return context;
		}
		TileData tile = tiles[defender.currentTileIndex];
		bool fortified = tile.buildingLevels != null && tile.buildingLevels.Length > 3 && tile.buildingLevels[3] > 0;
		context.kind = (distance <= 0) ? (fortified ? EncounterKind.Siege : EncounterKind.Battle) : EncounterKind.Skirmish;
		if (tile.elevation01 > 0.5f || IsDenseTerrain(tile.biome))
		{
			context.attackerPowerMultiplier *= 0.92f;
		}
		if (fortified)
		{
			context.defenderPowerMultiplier *= 1.25f;
		}
		if (distance > 0)
		{
			bool attackerSees = attacker.scoutRange >= distance;
			bool defenderSees = defender.scoutRange >= distance;
			if (attackerSees && !defenderSees)
			{
				context.attackerPowerMultiplier *= 1.1f;
			}
			else if (defenderSees && !attackerSees)
			{
				context.defenderPowerMultiplier *= 1.1f;
			}
			context.attackerPowerMultiplier *= 0.9f;
			context.defenderPowerMultiplier *= 0.9f;
		}
		return context;
	}
}
}
