using System;
using UnityEngine;
using MyriadCreation.Core.Enums;

namespace MyriadCreation.World.Settlement
{
/// <summary>
/// 城防评估：聚合一个聚落的所有城墙块（而不是只取第一块），
/// 每个城墙块再聚合其横向段（WallSegment），并按材质/工艺/城防等级/层数/护城河/守军计算。
/// </summary>
public static class WallDefense
{
	public const float MetersPerDefender = 4f;

	public const float TowerSpacingMeters = 60f;

	public const int DefendersPerTower = 4;

	public const float OvercrowdRatio = 1.5f;

	private const int MaxWallLayers = 3;

	public static WallDefenseSnapshot Evaluate(SettlementData settlement)
	{
		WallDefenseSnapshot result = default;
		if (settlement == null)
		{
			return result;
		}
		var walls = settlement.GetBlocks(SettlementBlockKind.Wall);
		if (walls == null || walls.Count == 0)
		{
			return result;
		}

		float totalLength = 0f;
		int totalTowers = 0;
		int maxLayers = 1;
		int maxMoat = 0;
		int maxLevel = 0;
		int blockCount = 0;
		int segmentCount = 0;
		float weightedCondition = 0f;
		float conditionWeight = 0f;
		WallMaterial primaryMaterial = WallMaterial.Earth;
		int primaryGrade = 1;
		float primaryWeight = -1f;

		foreach (var wall in walls)
		{
			if (wall == null || !wall.enabled)
			{
				continue;
			}
			blockCount++;
			int layers = Mathf.Clamp(wall.wallLayerCount, 1, MaxWallLayers);
			maxLayers = Math.Max(maxLayers, layers);

			float blockLength = GetBlockLength(settlement, wall);
			float layerLength = blockLength * (1f + (layers - 1) * 0.5f);
			totalLength += layerLength;

			int towerCount = CountTowers(wall, blockLength);
			totalTowers += towerCount;

			int moat = Math.Max(0, wall.wallMoatLevel);
			if (wall.wallSegments != null)
			{
				segmentCount += wall.wallSegments.Count;
				foreach (var segment in wall.wallSegments)
				{
					if (segment != null)
					{
						moat = Math.Max(moat, segment.moatLevel);
					}
				}
			}
			maxMoat = Math.Max(maxMoat, moat);

			int level = FortificationLevels.Clamp(wall.level);
			if (level <= 0)
			{
				level = FortificationLevels.Clamp(settlement.fortificationLevel);
			}
			maxLevel = Math.Max(maxLevel, level);

			float condition = AverageCondition(wall);
			weightedCondition += condition * layerLength;
			conditionWeight += layerLength;

			if (layerLength >= primaryWeight)
			{
				primaryWeight = layerLength;
				primaryMaterial = wall.wallMaterial;
				primaryGrade = FortificationLevels.ClampMaterialGrade(wall.wallMaterialGrade);
			}
		}

		if (blockCount == 0)
		{
			return default(WallDefenseSnapshot);
		}
		result.hasWall = true;
		result.wallLengthMeters = totalLength;
		result.wallLayerCount = maxLayers;
		result.wallBlockCount = blockCount;
		result.wallSegmentCount = segmentCount;
		result.towerCount = Math.Max(1, totalTowers);
		result.fortificationLevel = maxLevel;
		result.wallMaterial = primaryMaterial;
		result.wallMaterialGrade = primaryGrade;
		result.averageCondition = conditionWeight > 0f ? Mathf.Clamp01(weightedCondition / conditionWeight) : 1f;

		result.garrison = Math.Max(0, settlement.garrison);
		result.requiredGarrison = Math.Max(10, Mathf.CeilToInt(totalLength / MetersPerDefender) + result.towerCount * DefendersPerTower);
		result.coverage = (float)result.garrison / (float)result.requiredGarrison;
		result.unguardedLengthMeters = Math.Max(0f, totalLength * (1f - Mathf.Clamp01(result.coverage)));

		float coverageFactor = Mathf.Clamp(0.25f + 0.75f * Mathf.Sqrt(Mathf.Clamp01(result.coverage)), 0.2f, 1.1f);
		float layerFactor = 1f + (maxLayers - 1) * 0.08f;
		float moatFactor = 1f + Mathf.Clamp(maxMoat, 0, 5) * 0.04f;
		float materialFactor = GetMaterialFactor(primaryMaterial) * (1f + (primaryGrade - 1) * 0.08f);
		float levelFactor = 1f + maxLevel * 0.02f;
		float depthFactor = 1f + Math.Max(0, blockCount - 1) * 0.10f;
		float conditionFactor = Mathf.Clamp(result.averageCondition, 0.2f, 1f);

		result.effectiveFortification = Mathf.Max(0f,
			settlement.fortification * coverageFactor * layerFactor * moatFactor *
			materialFactor * levelFactor * depthFactor * conditionFactor);
		result.overcrowded = result.coverage > OvercrowdRatio;
		return result;
	}

	private static float GetBlockLength(SettlementData settlement, SettlementBlock wall)
	{
		if (wall.wallSegments != null && wall.wallSegments.Count > 0)
		{
			float sum = 0f;
			foreach (var segment in wall.wallSegments)
			{
				if (segment != null && segment.length > 0f)
				{
					sum += segment.length;
				}
			}
			if (sum > 0f)
			{
				return sum;
			}
		}
		return wall.length > 0f ? wall.length : EstimateLength(settlement, wall);
	}

	private static int CountTowers(SettlementBlock wall, float blockLength)
	{
		int fromSegments = 0;
		if (wall.wallSegments != null)
		{
			foreach (var segment in wall.wallSegments)
			{
				if (segment != null)
				{
					fromSegments += Math.Max(0, segment.towerCount);
				}
			}
		}
		int spacingTowers = Mathf.Max(1, Mathf.CeilToInt(blockLength / TowerSpacingMeters));
		return Math.Max(fromSegments, spacingTowers);
	}

	private static float AverageCondition(SettlementBlock wall)
	{
		if (wall.wallSegments == null || wall.wallSegments.Count == 0)
		{
			return Mathf.Clamp01(wall.condition);
		}
		float sum = 0f;
		float weight = 0f;
		foreach (var segment in wall.wallSegments)
		{
			if (segment != null && segment.length > 0f)
			{
				sum += Mathf.Clamp01(segment.condition) * segment.length;
				weight += segment.length;
			}
		}
		return weight > 0f ? sum / weight : Mathf.Clamp01(wall.condition);
	}

	private static float GetMaterialFactor(WallMaterial material)
	{
		switch (material)
		{
			case WallMaterial.Palisade: return 0.55f;
			case WallMaterial.Earth: return 0.75f;
			case WallMaterial.Stone: return 1.0f;
			case WallMaterial.Brick: return 1.08f;
			case WallMaterial.Composite: return 1.15f;
			default: return 1.0f;
		}
	}

	public static float EstimateLength(SettlementData settlement, SettlementBlock wall)
	{
		float num = settlement.settlementLevel switch
		{
			SettlementLevel.LevelI => 300f, 
			SettlementLevel.LevelII => 800f, 
			SettlementLevel.LevelIII => 2000f, 
			SettlementLevel.LevelIV => 4500f, 
			SettlementLevel.LevelV => 8000f, 
			_ => 2000f, 
		};
		float num2 = Mathf.Sqrt(Mathf.Max(0f, settlement.population)) * 3f;
		float num3 = settlement.cityForm switch
		{
			CityForm.Circular => 0.9f, 
			CityForm.Square => 1f, 
			CityForm.Irregular => 1.1f, 
			CityForm.TwinCity => 1.25f, 
			CityForm.MountainCity => 1.2f, 
			CityForm.StarCity => 1.3f, 
			CityForm.WalledTown => 0.9f, 
			CityForm.CitadelCity => 1.1f, 
			_ => 1f, 
		};
		float num4;
		if (wall.wallLayout == WallLayout.Straight)
		{
			num4 = 0.85f;
		}
		else if (wall.wallLayout == WallLayout.Polygonal)
		{
			num4 = 1.05f;
		}
		else
		{
			num4 = ((wall.wallLayout == WallLayout.MultiCircuit) ? 1.1f : 1f);
		}
		return Mathf.Clamp((num + num2) * num3 * num4, 120f, 40000f);
	}
}
}
