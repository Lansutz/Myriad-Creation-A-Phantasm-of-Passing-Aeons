using System;
using MyriadCreation.Core.Data;
using MyriadCreation.Core.Enums;
using MyriadCreation.Simulation.Definitions;
using UnityEngine;
using MyriadCreation.Simulation.Faith;
using Random = System.Random;
using MyriadCreation.Content.Definitions;

namespace MyriadCreation.Simulation.Systems
{
public static class MissionarySystem
{
	public static float CalculateSuccessChance(TileData tile, int missionaryFaithId, Func<int, ReligionDef> getReligion, Func<int, int> getRootFaith)
	{
		if (tile.populationBlocks == null)
		{
			return 0f;
		}
		int dominantFaith = PopulationStats.GetDominantFaith(tile);
		if (dominantFaith < 0)
		{
			return 0.6f;
		}
		if (dominantFaith == missionaryFaithId)
		{
			return 1f;
		}
		int num = getRootFaith?.Invoke(dominantFaith) ?? (-1);
		int num2 = getRootFaith?.Invoke(missionaryFaithId) ?? (-1);
		if (num >= 0 && num == num2)
		{
			return 0.6f;
		}
		return 0.2f;
	}

	public static bool ConvertTile(TileData tile, int missionaryFaithId, int cultureId, float successChance, Random rng)
	{
		if (tile.populationBlocks == null)
		{
			return false;
		}
		if (rng.NextDouble() > (double)successChance)
		{
			return false;
		}
		for (int i = 0; i < tile.populationBlocks.Count; i++)
		{
			if (tile.populationBlocks[i].faithId == missionaryFaithId)
			{
				PopulationBlock value = tile.populationBlocks[i];
				value.count *= 1.05f;
				tile.populationBlocks[i] = value;
				return true;
			}
		}
		int dominantFaith = PopulationStats.GetDominantFaith(tile);
		for (int j = 0; j < tile.populationBlocks.Count; j++)
		{
			PopulationBlock value2 = tile.populationBlocks[j];
			if (value2.faithId == dominantFaith && value2.count > 5f)
			{
				float num = value2.count * 0.03f;
				value2.count -= num;
				tile.populationBlocks[j] = value2;
				tile.populationBlocks.Add(new PopulationBlock
				{
					count = num,
					cultureId = ((cultureId >= 0) ? cultureId : value2.cultureId),
					faithId = missionaryFaithId,
					socialClass = value2.socialClass
				});
				return true;
			}
		}
		return false;
	}

	public static bool TaxPressureConversion(TileData tile, int stateFaithId, float taxRate)
	{
		if (tile.populationBlocks == null)
		{
			return false;
		}
		bool result = false;
		for (int i = 0; i < tile.populationBlocks.Count; i++)
		{
			PopulationBlock value = tile.populationBlocks[i];
			if (value.faithId != stateFaithId)
			{
				float num = taxRate * 0.01f;
				if (value.socialClass == GameEnums.SocialClass.Peasant || value.socialClass == GameEnums.SocialClass.Slave)
				{
					num *= 1.5f;
				}
				if (UnityEngine.Random.value < num)
				{
					value.faithId = stateFaithId;
					tile.populationBlocks[i] = value;
					result = true;
				}
			}
		}
		return result;
	}
}
}
