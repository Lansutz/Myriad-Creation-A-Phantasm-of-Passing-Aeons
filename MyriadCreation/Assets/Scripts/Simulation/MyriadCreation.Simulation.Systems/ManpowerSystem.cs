using System.Collections.Generic;
using MyriadCreation.Core.Data;
using MyriadCreation.Core.Enums;
using MyriadCreation.Simulation.Laws;
using MyriadCreation.Simulation.State;
using MyriadCreation.Content.Definitions;
using MyriadCreation.Simulation.LawHost;

namespace MyriadCreation.Simulation.Systems
{
public static class ManpowerSystem
{
	public static float GetClassRecruitRate(GameEnums.SocialClass c)
	{
		return c switch
		{
			GameEnums.SocialClass.Royalty => 0.05f, 
			GameEnums.SocialClass.NobilityClergy => 0.08f, 
			GameEnums.SocialClass.MerchantFreeman => 0.15f, 
			GameEnums.SocialClass.Peasant => 0.1f, 
			GameEnums.SocialClass.Slave => 0f, 
			_ => 0f, 
		};
	}

	public static float GetTerrainRecruitModifier(TileData tile)
	{
		return CarryingCapacitySystem.GetTerrainMultiplier(tile) * 0.5f + 0.5f;
	}

	public static Dictionary<GameEnums.SocialClass, float> GetRealmManpowerPool(int realmId, TileData[] tiles, IReadOnlyDictionary<int, RealmData> realms)
	{
		Dictionary<GameEnums.SocialClass, float> dictionary = new Dictionary<GameEnums.SocialClass, float>();
		if (tiles == null || realms == null || !realms.TryGetValue(realmId, out var value))
		{
			return dictionary;
		}
		foreach (int coreTile in value.coreTiles)
		{
			if (coreTile < 0 || coreTile >= tiles.Length)
			{
				continue;
			}
			TileData tile = tiles[coreTile];
			if (tile.populationBlocks == null)
			{
				continue;
			}
			float terrainRecruitModifier = GetTerrainRecruitModifier(tile);
			foreach (PopulationBlock populationBlock in tile.populationBlocks)
			{
				float classRecruitRate = GetClassRecruitRate(populationBlock.socialClass);
				if (!(classRecruitRate <= 0f))
				{
					float num = populationBlock.count * 100f * classRecruitRate * terrainRecruitModifier;
					dictionary[populationBlock.socialClass] = dictionary.GetValueOrDefault(populationBlock.socialClass) + num;
				}
			}
		}
		return dictionary;
	}

	public static float GetRealmTotalManpower(int realmId, TileData[] tiles, IReadOnlyDictionary<int, RealmData> realms)
	{
		float num = 0f;
		foreach (float value in GetRealmManpowerPool(realmId, tiles, realms).Values)
		{
			num += value;
		}
		return num;
	}

	/// <summary>
	/// 人力实际入口（法律/法令 manpowerMultiplier 消费点）：在总人力池上乘
	/// LawEffectRuntime.GetManpowerMultiplier。生产侧计算"可征召/可补员人力"应使用本重载；
	/// 旧无 day 重载保持默认乘子 1（兼容既有调用）。
	/// </summary>
	public static float GetRealmTotalManpower(int realmId, TileData[] tiles, IReadOnlyDictionary<int, RealmData> realms, int currentDay)
	{
		float num = GetRealmTotalManpower(realmId, tiles, realms);
		if (realms != null && realms.TryGetValue(realmId, out var value) && value != null)
		{
			num *= LawEffectRuntime.GetManpowerMultiplier(value, currentDay);
		}
		return num;
	}
}
}
