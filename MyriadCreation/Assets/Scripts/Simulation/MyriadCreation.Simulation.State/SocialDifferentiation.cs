using System;
using System.Collections.Generic;
using MyriadCreation.Core.Data;
using MyriadCreation.Core.Enums;
using UnityEngine;
using MyriadCreation.Content.Definitions;

namespace MyriadCreation.Simulation.State
{
public static class SocialDifferentiation
{
	private const float MerchantFloor = 0.02f;

	private const float MerchantDevWeight = 0.15f;

	private const float MerchantTradeWeight = 0.05f;

	private const float MerchantCeil = 0.28f;

	private const float NobleFloor = 0.02f;

	private const float NobleCentralWeight = 0.04f;

	private const float NobleCeil = 0.09f;

	private const float SlaveFloor = 0.005f;

	private const float SlaveWarBoost = 0.045f;

	private const float SlaveDevWeight = 0.01f;

	private const float SlaveCeil = 0.18f;

	private const float NonPeasantCeil = 0.42f;

	private const float ConvergeAlpha = 0.1f;

	private const float MinBlockCount = 0.02f;

	public static void DifferentiateRealm(RealmData realm, TileData[] tiles, RealmSituation sit, IReadOnlyList<int> realmTiles = null)
	{
		if (realm == null || tiles == null || sit == null)
		{
			return;
		}
		float num = 0f;
		int num2 = 0;
		List<int> list = new List<int>();
		foreach (int item in EnumerateRealmTiles(realm, tiles, realmTiles))
		{
			ref TileData reference = ref tiles[item];
			if (reference.isLand && reference.populationBlocks != null && reference.populationBlocks.Count != 0)
			{
				list.Add(item);
				list.Add(item);
				num += Mathf.Clamp01(reference.development);
				num2++;
			}
		}
		if (list.Count == 0)
		{
			return;
		}
		float num3 = ((num2 > 0) ? (num / (float)num2) : 0f);
		Dictionary<GameEnums.SocialClass, float> realmGoal = ComputeGoalShares(realm, sit, num3);
		foreach (int item2 in list)
		{
			ref TileData reference2 = ref tiles[item2];
			RebalanceTile(ref reference2, realmGoal, Mathf.Clamp01(reference2.development), Mathf.Max(0.05f, num3));
			tiles[item2] = reference2;
		}
	}

	private static Dictionary<GameEnums.SocialClass, float> ComputeGoalShares(RealmData realm, RealmSituation sit, float avgDev)
	{
		Dictionary<GameEnums.SocialClass, float> dictionary = new Dictionary<GameEnums.SocialClass, float>
		{
			[GameEnums.SocialClass.Royalty] = 0f,
			[GameEnums.SocialClass.Peasant] = 1f,
			[GameEnums.SocialClass.MerchantFreeman] = 0f,
			[GameEnums.SocialClass.NobilityClergy] = 0f,
			[GameEnums.SocialClass.Slave] = 0f
		};
		if (sit.IsRecognized(GameEnums.SocialClass.MerchantFreeman))
		{
			dictionary[GameEnums.SocialClass.MerchantFreeman] = Mathf.Clamp(0.02f + avgDev * 0.15f + Mathf.Clamp01(sit.tradeFlow) * 0.05f, 0f, 0.28f);
		}
		if (sit.IsRecognized(GameEnums.SocialClass.NobilityClergy))
		{
			dictionary[GameEnums.SocialClass.NobilityClergy] = Mathf.Clamp(0.02f + Mathf.Clamp01(realm.centralization) * 0.04f, 0f, 0.09f);
		}
		if (sit.IsRecognized(GameEnums.SocialClass.Slave))
		{
			dictionary[GameEnums.SocialClass.Slave] = Mathf.Clamp(0.005f + (sit.atWar ? 0.045f : 0f) + avgDev * 0.01f, 0f, 0.18f);
		}
		float num = dictionary[GameEnums.SocialClass.MerchantFreeman] + dictionary[GameEnums.SocialClass.NobilityClergy] + dictionary[GameEnums.SocialClass.Slave];
		if (num > 0.42f && num > 0f)
		{
			float num2 = 0.42f / num;
			dictionary[GameEnums.SocialClass.MerchantFreeman] *= num2;
			dictionary[GameEnums.SocialClass.NobilityClergy] *= num2;
			dictionary[GameEnums.SocialClass.Slave] *= num2;
			num = 0.42f;
		}
		dictionary[GameEnums.SocialClass.Peasant] = 1f - num;
		return dictionary;
	}

	private static void RebalanceTile(ref TileData tile, Dictionary<GameEnums.SocialClass, float> realmGoal, float tileDev, float avgDev)
	{
		List<PopulationBlock> populationBlocks = tile.populationBlocks;
		float num = 0f;
		foreach (PopulationBlock item in populationBlocks)
		{
			num += item.count;
		}
		if (num < 0.1f)
		{
			return;
		}
		float num2 = Mathf.Clamp(tileDev / Mathf.Max(0.05f, avgDev), 0.3f, 2.2f);
		Dictionary<GameEnums.SocialClass, float> dictionary = new Dictionary<GameEnums.SocialClass, float>(realmGoal) { [GameEnums.SocialClass.MerchantFreeman] = Mathf.Clamp01(realmGoal[GameEnums.SocialClass.MerchantFreeman] * num2) };
		float num3 = dictionary[GameEnums.SocialClass.MerchantFreeman] + dictionary[GameEnums.SocialClass.NobilityClergy] + dictionary[GameEnums.SocialClass.Slave];
		dictionary[GameEnums.SocialClass.Peasant] = Mathf.Max(0f, 1f - num3);
		Dictionary<GameEnums.SocialClass, float> dictionary2 = new Dictionary<GameEnums.SocialClass, float>();
		foreach (GameEnums.SocialClass value2 in Enum.GetValues(typeof(GameEnums.SocialClass)))
		{
			dictionary2[value2] = 0f;
		}
		foreach (PopulationBlock item2 in populationBlocks)
		{
			dictionary2[item2.socialClass] += item2.count;
		}
		Dictionary<GameEnums.SocialClass, float> dictionary3 = new Dictionary<GameEnums.SocialClass, float>();
		float num4 = 0f;
		foreach (GameEnums.SocialClass value3 in Enum.GetValues(typeof(GameEnums.SocialClass)))
		{
			float num5 = (dictionary3[value3] = num * dictionary.GetValueOrDefault(value3, 0f) - dictionary2.GetValueOrDefault(value3, 0f));
			if (num5 < 0f)
			{
				num4 += (0f - num5) * 0.1f;
			}
		}
		if (num4 < 0.0001f)
		{
			return;
		}
		float num7 = 0f;
		foreach (KeyValuePair<GameEnums.SocialClass, float> item3 in dictionary3)
		{
			if (item3.Value > 0f)
			{
				num7 += item3.Value * 0.1f;
			}
		}
		if (num7 < 0.0001f)
		{
			return;
		}
		Dictionary<GameEnums.SocialClass, float> dictionary4 = new Dictionary<GameEnums.SocialClass, float>();
		foreach (KeyValuePair<GameEnums.SocialClass, float> item4 in dictionary3)
		{
			if (!(item4.Value >= 0f))
			{
				float value = Mathf.Min((0f - item4.Value) * 0.1f, dictionary2[item4.Key]);
				dictionary4[item4.Key] = value;
			}
		}
		Dictionary<GameEnums.SocialClass, float> dictionary5 = new Dictionary<GameEnums.SocialClass, float>();
		foreach (KeyValuePair<GameEnums.SocialClass, float> item5 in dictionary3)
		{
			if (!(item5.Value <= 0f))
			{
				float num8 = item5.Value * 0.1f;
				dictionary5[item5.Key] = ((num7 > 0f) ? (num8 * (num4 / num7)) : 0f);
			}
		}
		foreach (KeyValuePair<GameEnums.SocialClass, float> item6 in dictionary4)
		{
			Withdraw(populationBlocks, item6.Key, item6.Value);
		}
		foreach (KeyValuePair<GameEnums.SocialClass, float> item7 in dictionary5)
		{
			if (item7.Value > 0.0001f)
			{
				Deposit(populationBlocks, item7.Key, item7.Value, populationBlocks);
			}
		}
		for (int num9 = populationBlocks.Count - 1; num9 >= 0; num9--)
		{
			if (populationBlocks[num9].count < 0.02f)
			{
				populationBlocks.RemoveAt(num9);
			}
		}
	}

	private static void Withdraw(List<PopulationBlock> blocks, GameEnums.SocialClass cls, float amount)
	{
		for (int i = 0; i < blocks.Count; i++)
		{
			if (!(amount > 1E-05f))
			{
				break;
			}
			if (blocks[i].socialClass == cls)
			{
				PopulationBlock value = blocks[i];
				float num = Mathf.Min(value.count, amount);
				value.count -= num;
				amount -= num;
				blocks[i] = value;
			}
		}
	}

	private static void Deposit(List<PopulationBlock> blocks, GameEnums.SocialClass cls, float amount, List<PopulationBlock> templateSource)
	{
		IdentityProfile identityProfile = LegacyIdentityMapping.FromLegacy(cls);
		for (int i = 0; i < blocks.Count; i++)
		{
			if (blocks[i].socialClass == cls)
			{
				PopulationBlock value = blocks[i];
				value.count += amount;
				value.economicClass = identityProfile.economicClass;
				value.stratum = identityProfile.stratum;
				value.occupations = identityProfile.occupations;
				blocks[i] = value;
				return;
			}
		}
		PopulationBlock populationBlock = ((templateSource.Count > 0) ? templateSource[0] : default(PopulationBlock));
		blocks.Add(new PopulationBlock
		{
			socialClass = cls,
			count = amount,
			raceId = populationBlock.raceId,
			cultureId = populationBlock.cultureId,
			faithId = populationBlock.faithId,
			satisfaction = ((populationBlock.satisfaction > 0f) ? populationBlock.satisfaction : 55f),
			culturePenetration = populationBlock.culturePenetration,
			profession = 0,
			economicClass = identityProfile.economicClass,
			stratum = identityProfile.stratum,
			occupations = identityProfile.occupations
		});
	}

	private static IEnumerable<int> EnumerateRealmTiles(RealmData realm, TileData[] tiles, IReadOnlyList<int> realmTiles = null)
	{
		if (realmTiles != null)
		{
			foreach (int realmTile in realmTiles)
			{
				if (realmTile >= 0 && realmTile < tiles.Length)
				{
					yield return realmTile;
				}
			}
			yield break;
		}
		HashSet<int> seen = new HashSet<int>();
		foreach (int coreTile in realm.coreTiles)
		{
			if (coreTile >= 0 && coreTile < tiles.Length && seen.Add(coreTile))
			{
				yield return coreTile;
			}
		}
		for (int i = 0; i < tiles.Length; i++)
		{
			if (tiles[i].ownerRealmId == realm.realmId && seen.Add(i))
			{
				yield return i;
			}
		}
	}
}
}
