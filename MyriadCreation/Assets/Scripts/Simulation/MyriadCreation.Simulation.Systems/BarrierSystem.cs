using System.Collections.Generic;
using MyriadCreation.Core.Data;
using MyriadCreation.Core.Enums;
using MyriadCreation.World.Settlement;
using UnityEngine;
using MyriadCreation.Content.Definitions;

namespace MyriadCreation.Simulation.Systems
{
public static class BarrierSystem
{
	public const float ImpassableElevation = 0.85f;

	public const float ImpassableSlope = 50f;

	public const float HighCostSlope = 30f;

	public const float MediumCostSlope = 15f;

	public const float HighElevation = 0.7f;

	public const float MediumElevation = 0.5f;

	public const int MinNarrownessForBarrier = 2;

	public const int BarrierRadius = 1;

	public const int FortBaseRadius = 2;

	public static void CalculatePassability(TileData[] tiles, int width, int height)
	{
		for (int i = 0; i < tiles.Length; i++)
		{
			ref TileData reference = ref tiles[i];
			if (!reference.exists || !reference.isLand)
			{
				reference.passable = reference.exists;
				reference.movementCost = (reference.isLand ? 50f : 2f);
			}
			else
			{
				reference.passable = !(reference.elevation01 >= 0.85f) || !(reference.slopeDegree >= 50f);
				reference.movementCost = CalculateBaseMovementCost(reference);
			}
		}
	}

	public static float CalculateBaseMovementCost(TileData tile)
	{
		if (!tile.passable)
		{
			return 50f;
		}
		float num = 1f;
		if (tile.slopeDegree >= 30f)
		{
			num *= 3f;
		}
		else if (tile.slopeDegree >= 15f)
		{
			num *= 1.8f;
		}
		else if (tile.slopeDegree >= 8f)
		{
			num *= 1.3f;
		}
		if (tile.elevation01 >= 0.7f)
		{
			num *= 2f;
		}
		else if (tile.elevation01 >= 0.5f)
		{
			num *= 1.4f;
		}
		if (tile.elevation01 >= 0.7f && tile.slopeDegree < 15f)
		{
			num *= 0.7f;
		}
		if (tile.elevation01 >= 0.5f && tile.elevation01 < 0.7f && tile.slopeDegree >= 30f)
		{
			num *= 0.85f;
		}
		float num2 = num;
		num = num2 * tile.roadLevel switch
		{
			GameEnums.RoadLevel.ImperialHighway => 0.5f, 
			GameEnums.RoadLevel.OfficialRoad => 0.7f, 
			GameEnums.RoadLevel.DirtRoad => 0.9f, 
			_ => 1f, 
		};
		if (tile.isRiver && tile.roadLevel == GameEnums.RoadLevel.None)
		{
			num *= 1.5f;
		}
		if (tile.isCoast && tile.elevation01 < 0.3f)
		{
			num *= 1.2f;
		}
		return Mathf.Max(0.3f, num);
	}

	public static List<int> DetectNarrowPassages(TileData[] tiles, int width, int height)
	{
		List<int> list = new List<int>();
		for (int i = 0; i < tiles.Length; i++)
		{
			ref TileData reference = ref tiles[i];
			if (reference.exists && reference.isLand && reference.passable && IsNarrowPassage(tiles, width, height, i))
			{
				list.Add(i);
			}
		}
		return list;
	}

	public static bool IsNarrowPassage(TileData[] tiles, int width, int height, int index)
	{
		int startX = index % width;
		int startY = index / width;
		int num = 0;
		int[] array = new int[4] { 0, 0, -1, 1 };
		int[] array2 = new int[4] { -1, 1, 0, 0 };
		for (int i = 0; i < 4; i++)
		{
			if (IsDirectionBlocked(tiles, width, height, startX, startY, array[i], array2[i]))
			{
				num++;
			}
		}
		if (num >= 2)
		{
			return num < 4;
		}
		return false;
	}

	private static bool IsDirectionBlocked(TileData[] tiles, int width, int height, int startX, int startY, int dx, int dy)
	{
		for (int i = 1; i <= 3; i++)
		{
			int num = startX + dx * i;
			int num2 = startY + dy * i;
			num = (num % width + width) % width;
			if (num2 < 0 || num2 >= height)
			{
				return true;
			}
			int num3 = num2 * width + num;
			if (!tiles[num3].exists || !tiles[num3].passable || tiles[num3].slopeDegree >= 30f)
			{
				return true;
			}
		}
		return false;
	}

	public static bool CanBuildBarrier(TileData[] tiles, int width, int height, int index)
	{
		ref TileData reference = ref tiles[index];
		if (!reference.exists || !reference.isLand || !reference.passable)
		{
			return false;
		}
		if (reference.hasBarrier)
		{
			return false;
		}
		return IsNarrowPassage(tiles, width, height, index);
	}

	public static bool BuildBarrier(TileData[] tiles, int index, int realmId, float strength = 5f)
	{
		ref TileData reference = ref tiles[index];
		if (reference.hasBarrier)
		{
			return false;
		}
		reference.hasBarrier = true;
		reference.barrierOwnerRealmId = realmId;
		reference.barrierStrength = Mathf.Clamp(strength, 1f, 10f);
		reference.isGate = true;
		reference.movementCost *= 0.9f;
		return true;
	}

	public static bool BreachBarrier(TileData[] tiles, int index, int attackerRealmId)
	{
		ref TileData reference = ref tiles[index];
		if (!reference.hasBarrier)
		{
			return false;
		}
		reference.hasBarrier = false;
		reference.barrierOwnerRealmId = -1;
		reference.barrierStrength = 0f;
		reference.isGate = false;
		reference.movementCost *= 1.3f;
		return true;
	}

	public static bool CaptureBarrier(TileData[] tiles, int index, int newRealmId)
	{
		ref TileData reference = ref tiles[index];
		if (!reference.hasBarrier)
		{
			return false;
		}
		reference.barrierOwnerRealmId = newRealmId;
		return true;
	}

	public static void CalculateFortInfluence(TileData[] tiles, int width, int height, Dictionary<int, SettlementData> settlements)
	{
		for (int i = 0; i < tiles.Length; i++)
		{
			tiles[i].nearbyFortId = -1;
			tiles[i].fortInfluenceLevel = 0;
		}
		foreach (SettlementData value in settlements.Values)
		{
			if (value.siteMode != SiteMode.FortifiedOutpost)
			{
				continue;
			}
			int tileIndex = value.tileIndex;
			if (tileIndex < 0 || tileIndex >= tiles.Length)
			{
				continue;
			}
			int num = (int)(2 + value.settlementLevel);
			int num2 = Mathf.Clamp(1 + (int)value.settlementLevel / 2, 1, 3);
			for (int j = -num; j <= num; j++)
			{
				for (int k = -num; k <= num; k++)
				{
					if (k * k + j * j > num * num)
					{
						continue;
					}
					int num3 = tileIndex % width + k;
					int num4 = tileIndex / width + j;
					num3 = (num3 % width + width) % width;
					if (num4 < 0 || num4 >= height)
					{
						continue;
					}
					int num5 = num4 * width + num3;
					if (tiles[num5].exists && tiles[num5].isLand)
					{
						float num6 = Mathf.Sqrt((float)(k * k + j * j));
						int num7 = ((num6 <= (float)num * 0.5f) ? num2 : ((!(num6 <= (float)num * 0.8f)) ? 1 : Mathf.Max(1, num2 - 1)));
						if (tiles[num5].fortInfluenceLevel < num7 || tiles[num5].nearbyFortId == -1)
						{
							tiles[num5].nearbyFortId = value.anchorId;
							tiles[num5].fortInfluenceLevel = num7;
						}
					}
				}
			}
		}
	}

	public static int GetFortOwnerRealmId(TileData[] tiles, Dictionary<int, SettlementData> settlements, int tileIndex)
	{
		if (tiles[tileIndex].nearbyFortId < 0)
		{
			return -1;
		}
		if (settlements.TryGetValue(tiles[tileIndex].nearbyFortId, out var value))
		{
			return tiles[value.tileIndex].ownerRealmId;
		}
		return -1;
	}
}
}
