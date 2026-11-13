using System;
using System.Collections.Generic;
using MyriadCreation.Core.Data;
using MyriadCreation.Core.Enums;
using MyriadCreation.Simulation.Units;
using MyriadCreation.World.Terrain;
using UnityEngine;
using MyriadCreation.Content.Definitions;
using MyriadCreation.Simulation.LawHost;
using Random = UnityEngine.Random;

namespace MyriadCreation.Simulation.State
{
public class Army
{
	public int armyId;

	public string armyName;

	public int ownerRealmId;

	public int commanderId;

	[NonSerialized]
	public TransportUnit transport;

	public int currentTileIndex;

	[NonSerialized]
	public Dictionary<int, float> unitCounts = new Dictionary<int, float>();

	public const int ManpowerPerBlock = 100;

	public const float ReinforceBatchRatio = 0.1f;

	public const float TrainingOrgLossReductionMax = 0.5f;

	public const float IneffectiveOrgThreshold = 20f;

	public const float IneffectiveTotalCasualtyThreshold = 0.5f;

	public const float InstantCasualtyOrgShockThreshold = 0.2f;

	public const float BattleEndManpowerRatio = 0.3f;

	public const float InstantCasualtyOrgShock = 15f;

	public GameEnums.CombatState state;

	public float organization = 100f;

	public float morale = 100f;

	public float supply = 100f;

	public float training = 50f;

	/// <summary>斥候可达范围（地块，切比雪夫距离）；每日按兵力构成/状态/地形重算，D37。</summary>
	public int scoutRange = 1;

	[NonSerialized]
	public float originalManpower;

	[NonSerialized]
	public float totalCasualtyRate;

	[NonSerialized]
	public float instantCasualtyRate;

	[NonSerialized]
	public bool isCombatIneffective;

	[NonSerialized]
	public float pursuitBonus;

	public List<int> movePath = new List<int>();

	public int moveTargetTile = -1;

	public float moveProgress;

	public float GetTotalManpower(Dictionary<int, UnitDef> unitDefs)
	{
		// unitDefs 在场景/测试/读档早期可能还没初始化：宁可返回 0，也不要在每日结算里 NRE。
		if (unitDefs == null)
		{
			return 0f;
		}
		float num = 0f;
		foreach (KeyValuePair<int, float> unitCount in unitCounts)
		{
			if (unitDefs.TryGetValue(unitCount.Key, out var value))
			{
				num += unitCount.Value * value.manpowerCost;
			}
		}
		return num;
	}

	public float CalculateCombatPower(Dictionary<int, UnitDef> unitDefs, TileData tile, int raceId = -1)
	{
		if (unitDefs == null)
		{
			return 0f;
		}
		float num = 0f;
		GameEnums.TerrainTacticType terrainTacticType = GetTerrainTacticType(tile);
		foreach (KeyValuePair<int, float> unitCount in unitCounts)
		{
			if (unitDefs.TryGetValue(unitCount.Key, out var value))
			{
				float num2 = (value.meleeAttack + value.rangedAttack + value.defense) * unitCount.Value;
				float valueOrDefault = value.terrainModifiers.GetValueOrDefault(terrainTacticType, 1f);
				num += num2 * valueOrDefault;
			}
		}
		float num3 = 0.85f + training / 100f * 0.3f;
		num *= (0.5f + organization / 200f) * (0.5f + morale / 200f) * num3;
		if (supply < 30f)
		{
			num *= 0.5f + supply / 60f;
		}
		return num;
	}

	public void DailyTick(Dictionary<int, UnitDef> unitDefs, TileData[] tiles, SeaLandGenerator seaLand)
	{
		if (unitDefs == null)
		{
			return;
		}
		float num = 0f;
		foreach (KeyValuePair<int, float> unitCount in unitCounts)
		{
			if (unitDefs.TryGetValue(unitCount.Key, out var value))
			{
				num += unitCount.Value * value.supplyConsumption;
			}
		}
		supply = Mathf.Max(0f, supply - num);
		if (supply <= 0f)
		{
			float num2 = 1f - training / 100f * 0.5f;
			organization = Mathf.Max(0f, organization - 5f * num2);
			morale = Mathf.Max(0f, morale - 10f);
			if (tiles[currentTileIndex].ownerRealmId != ownerRealmId)
			{
				tiles[currentTileIndex].stability = Mathf.Max(0f, tiles[currentTileIndex].stability - 3f);
				tiles[currentTileIndex].development = Mathf.Max(0f, tiles[currentTileIndex].development - 0.01f);
			}
		}
		if (state == GameEnums.CombatState.Marching && movePath.Count > 0)
		{
			MoveTick(unitDefs, tiles, seaLand);
		}
		if (state == GameEnums.CombatState.Idle && supply > 30f)
		{
			training = Mathf.Min(100f, training + 0.1f);
		}
	}

	/// <summary>
	/// 补员；manpowerMultiplier 为法律/法令人力乘子（LawEffectRuntime.GetManpowerMultiplier），
	/// 默认 1 保持旧行为，只放大"可用补员人力"，不改变单位成本。
	/// </summary>
	public int ReinforceUnit(int unitId, float targetCount, int availableManpower, Dictionary<int, UnitDef> unitDefs, float manpowerMultiplier = 1f)
	{
		if (!unitCounts.ContainsKey(unitId))
		{
			return 0;
		}
		if (!unitDefs.TryGetValue(unitId, out var value))
		{
			return 0;
		}
		float num = unitCounts[unitId];
		if (num >= targetCount)
		{
			return 0;
		}
		float num2 = Mathf.Min(targetCount * 0.1f, targetCount - num);
		int num3 = Mathf.CeilToInt(num2 * value.manpowerCost);
		// 法律/法令人力乘子：按乘子放大可用补员人力（不改单位成本；乘子=1 时与旧行为一致）。
		int usableManpower = Mathf.Max(0, Mathf.FloorToInt((float)availableManpower * Mathf.Max(0f, manpowerMultiplier)));
		if (num3 > usableManpower)
		{
			num2 = ((value.manpowerCost > 0f) ? ((float)usableManpower / value.manpowerCost) : 0f);
			num3 = usableManpower;
		}
		unitCounts[unitId] = num + num2;
		float totalManpower = GetTotalManpower(unitDefs);
		if (totalManpower > 0f)
		{
			float num4 = num2 * value.manpowerCost / totalManpower;
			training = Mathf.Max(0f, training - num4 * 20f);
		}
		return num3;
	}

	public string GetTrainingLevel()
	{
		if (training >= 80f)
		{
			return "精锐";
		}
		if (training >= 60f)
		{
			return "老兵";
		}
		if (training >= 40f)
		{
			return "常备";
		}
		if (training >= 20f)
		{
			return "新兵";
		}
		return "民兵";
	}

	public void RecordBattleStartManpower(Dictionary<int, UnitDef> unitDefs)
	{
		originalManpower = GetTotalManpower(unitDefs);
		totalCasualtyRate = 0f;
		instantCasualtyRate = 0f;
		isCombatIneffective = false;
		pursuitBonus = 0f;
	}

	public void UpdateCasualtyRates(Dictionary<int, UnitDef> unitDefs, float lossesThisPhase)
	{
		if (!(originalManpower <= 0f))
		{
			float totalManpower = GetTotalManpower(unitDefs);
			totalCasualtyRate = Mathf.Clamp01(1f - totalManpower / originalManpower);
			instantCasualtyRate = Mathf.Clamp01(lossesThisPhase / originalManpower);
		}
	}

	public bool CheckCombatIneffective()
	{
		isCombatIneffective = organization <= 20f || totalCasualtyRate >= 0.5f;
		return isCombatIneffective;
	}

	public void ApplyInstantCasualtyOrgShock()
	{
		if (instantCasualtyRate >= 0.2f)
		{
			float num = 15f * (instantCasualtyRate / 0.2f);
			organization = Mathf.Max(0f, organization - num);
		}
	}

	public bool CheckBattleEnd(Dictionary<int, UnitDef> unitDefs)
	{
		if (originalManpower <= 0f)
		{
			return true;
		}
		return GetTotalManpower(unitDefs) / originalManpower <= 0.3f;
	}

	public bool ShouldRetreat(float commanderAbility = 50f)
	{
		float num = 0.5f;
		num += (50f - morale) / 100f * 0.4f;
		num += (50f - training) / 100f * 0.2f;
		float num2 = 1f - Mathf.Abs(commanderAbility - 50f) / 50f;
		num *= 0.6f + num2 * 0.4f;
		if (organization <= 5f)
		{
			num = 1f;
		}
		return Random.value < Mathf.Clamp01(num);
	}

	public float CalculatePursuitEfficiency(Army enemy)
	{
		if (enemy == null || !enemy.isCombatIneffective)
		{
			return 1f;
		}
		float num = 1f + (20f - enemy.organization) / 20f * 0.5f;
		float num2 = 0.8f + training / 100f * 0.4f;
		return num * num2;
	}

	private void MoveTick(Dictionary<int, UnitDef> unitDefs, TileData[] tiles, SeaLandGenerator seaLand)
	{
		if (movePath.Count == 0)
		{
			return;
		}
		float num = float.MaxValue;
		foreach (KeyValuePair<int, float> unitCount in unitCounts)
		{
			if (unitDefs.TryGetValue(unitCount.Key, out var value))
			{
				num = Mathf.Min(num, value.speed);
			}
		}
		if (num == float.MaxValue)
		{
			num = 1f;
		}
		float num2 = 1f - tiles[currentTileIndex].slopeDegree / 90f * 0.5f;
		if (tiles[currentTileIndex].roadLevel == GameEnums.RoadLevel.None)
		{
			num2 *= 0.7f;
		}
		float num3 = ((transport != null) ? transport.MoveSpeedMultiplier(TerrainConditions.FromTile(tiles[currentTileIndex])) : 1f);
		moveProgress += num * num2 * num3;
		if (moveProgress >= 1f)
		{
			moveProgress = 0f;
			currentTileIndex = movePath[0];
			movePath.RemoveAt(0);
			if (movePath.Count == 0)
			{
				state = GameEnums.CombatState.Idle;
			}
		}
	}

	public void SetMoveTarget(int targetTile, TileData[] tiles, SeaLandGenerator seaLand, int mapWidth)
	{
		movePath = FindPath(currentTileIndex, targetTile, tiles, seaLand, mapWidth);
		moveTargetTile = targetTile;
		state = GameEnums.CombatState.Marching;
		moveProgress = 0f;
	}

	private List<int> FindPath(int start, int end, TileData[] tiles, SeaLandGenerator seaLand, int mapWidth)
	{
		List<int> list = new List<int>();
		List<int> list2 = new List<int>();
		Dictionary<int, int> dictionary = new Dictionary<int, int>();
		Dictionary<int, float> dictionary2 = new Dictionary<int, float>();
		Dictionary<int, float> dictionary3 = new Dictionary<int, float>();
		list2.Add(start);
		dictionary2[start] = 0f;
		dictionary3[start] = Heuristic(start, end, mapWidth);
		while (list2.Count > 0)
		{
			int num = list2[0];
			float num2 = dictionary3.GetValueOrDefault(num, float.MaxValue);
			for (int i = 1; i < list2.Count; i++)
			{
				float valueOrDefault = dictionary3.GetValueOrDefault(list2[i], float.MaxValue);
				if (valueOrDefault < num2)
				{
					num2 = valueOrDefault;
					num = list2[i];
				}
			}
			list2.Remove(num);
			if (num == end)
			{
				while (dictionary.ContainsKey(num))
				{
					list.Insert(0, num);
					num = dictionary[num];
				}
				return list;
			}
			foreach (int neighbourIndex in seaLand.GetNeighbourIndices(num))
			{
				if (!tiles[neighbourIndex].isLand && tiles[neighbourIndex].oceanTier == GameEnums.OceanTier.DeepSea)
				{
					continue;
				}
				float num3 = 1f + tiles[neighbourIndex].slopeDegree / 45f;
				float num4 = dictionary2.GetValueOrDefault(num, float.MaxValue) + num3;
				if (num4 < dictionary2.GetValueOrDefault(neighbourIndex, float.MaxValue))
				{
					dictionary[neighbourIndex] = num;
					dictionary2[neighbourIndex] = num4;
					dictionary3[neighbourIndex] = num4 + Heuristic(neighbourIndex, end, mapWidth);
					if (!list2.Contains(neighbourIndex))
					{
						list2.Add(neighbourIndex);
					}
				}
			}
		}
		return list;
	}

	private float Heuristic(int a, int b, int mapWidth)
	{
		int num = a % mapWidth;
		int num2 = a / mapWidth;
		int num3 = b % mapWidth;
		int num4 = b / mapWidth;
		return Mathf.Abs(num - num3) + Mathf.Abs(num2 - num4);
	}

	private GameEnums.TerrainTacticType GetTerrainTacticType(TileData tile)
	{
		if (tile.elevation01 > 0.5f)
		{
			return GameEnums.TerrainTacticType.Mountain;
		}
		if (tile.biome == GameEnums.BiomeType.BorealForest || tile.biome == GameEnums.BiomeType.DeciduousForest || tile.biome == GameEnums.BiomeType.TropicalRainforest)
		{
			return GameEnums.TerrainTacticType.Forest;
		}
		if (tile.biome == GameEnums.BiomeType.WetMarshPlain)
		{
			return GameEnums.TerrainTacticType.Wetland;
		}
		if (tile.biome == GameEnums.BiomeType.HotDesert)
		{
			return GameEnums.TerrainTacticType.Desert;
		}
		if (tile.buildingLevels != null && tile.buildingLevels.Length > 3 && tile.buildingLevels[3] > 0)
		{
			return GameEnums.TerrainTacticType.Fortress;
		}
		return GameEnums.TerrainTacticType.Plain;
	}
}
}
