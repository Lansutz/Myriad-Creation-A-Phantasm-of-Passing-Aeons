using System;
using System.Collections.Generic;
using MyriadCreation.Core.Data;
using MyriadCreation.Core.Enums;

namespace MyriadCreation.Simulation.Systems
{
public static class PopulationStats
{
	public static float GetCultureShare(TileData tile, int cultureId)
	{
		float num = 0f;
		float num2 = 0f;
		if (tile.populationBlocks == null)
		{
			return 0f;
		}
		foreach (PopulationBlock populationBlock in tile.populationBlocks)
		{
			num += populationBlock.count;
			if (populationBlock.cultureId == cultureId)
			{
				num2 += populationBlock.count;
			}
		}
		if (!(num > 0f))
		{
			return 0f;
		}
		return num2 / num;
	}

	public static float GetFaithShare(TileData tile, int faithId)
	{
		float num = 0f;
		float num2 = 0f;
		if (tile.populationBlocks == null)
		{
			return 0f;
		}
		foreach (PopulationBlock populationBlock in tile.populationBlocks)
		{
			num += populationBlock.count;
			if (populationBlock.faithId == faithId)
			{
				num2 += populationBlock.count;
			}
		}
		if (!(num > 0f))
		{
			return 0f;
		}
		return num2 / num;
	}

	public static float GetClassShare(TileData tile, GameEnums.SocialClass socialClass)
	{
		float num = 0f;
		float num2 = 0f;
		if (tile.populationBlocks == null)
		{
			return 0f;
		}
		foreach (PopulationBlock populationBlock in tile.populationBlocks)
		{
			num += populationBlock.count;
			if (populationBlock.socialClass == socialClass)
			{
				num2 += populationBlock.count;
			}
		}
		if (!(num > 0f))
		{
			return 0f;
		}
		return num2 / num;
	}

	public static int GetDominantCulture(TileData tile)
	{
		if (tile.populationBlocks == null || tile.populationBlocks.Count == 0)
		{
			return -1;
		}
		PopulationBlock populationBlock = tile.populationBlocks[0];
		foreach (PopulationBlock populationBlock2 in tile.populationBlocks)
		{
			if (populationBlock2.count > populationBlock.count)
			{
				populationBlock = populationBlock2;
			}
		}
		return populationBlock.cultureId;
	}

	public static int GetDominantFaith(TileData tile)
	{
		if (tile.populationBlocks == null || tile.populationBlocks.Count == 0)
		{
			return -1;
		}
		PopulationBlock populationBlock = tile.populationBlocks[0];
		foreach (PopulationBlock populationBlock2 in tile.populationBlocks)
		{
			if (populationBlock2.count > populationBlock.count)
			{
				populationBlock = populationBlock2;
			}
		}
		return populationBlock.faithId;
	}

	public static string BuildShareText(TileData tile, Func<int, string> cultureName, Func<int, string> faithName)
	{
		if (tile.populationBlocks == null || tile.populationBlocks.Count == 0)
		{
			return "";
		}
		List<string> list = new List<string>();
		HashSet<int> hashSet = new HashSet<int>();
		HashSet<int> hashSet2 = new HashSet<int>();
		foreach (PopulationBlock populationBlock in tile.populationBlocks)
		{
			if (populationBlock.cultureId >= 0 && hashSet.Add(populationBlock.cultureId))
			{
				list.Add($"{cultureName(populationBlock.cultureId)} {GetCultureShare(tile, populationBlock.cultureId) * 100f:F0}%");
			}
		}
		List<string> list2 = new List<string>();
		foreach (PopulationBlock populationBlock2 in tile.populationBlocks)
		{
			if (populationBlock2.faithId >= 0 && hashSet2.Add(populationBlock2.faithId))
			{
				list2.Add($"{faithName(populationBlock2.faithId)} {GetFaithShare(tile, populationBlock2.faithId) * 100f:F0}%");
			}
		}
		return "文化：" + string.Join(" · ", list) + "｜信仰：" + string.Join(" · ", list2);
	}
}
}
