using System.Collections.Generic;
using MyriadCreation.Content.Definitions;
using MyriadCreation.Core.Data;
using MyriadCreation.Core.Enums;
using MyriadCreation.Simulation.State;
using MyriadCreation.Simulation.WorldState;

namespace MyriadCreation.Simulation.Systems
{
/// <summary>
/// D25 四轴人口报告：每条人口块按四轴各归一次；身份/职业是位标志，按位展开并把人口均分，
/// 保证四轴人口合计与人口块总量相等（守恒）。
/// </summary>
public static class SocialAxisReportSystem
{
	public static List<SocialAxisReport> Build(GameWorld world, int realmId)
	{
		List<SocialAxisReport> reports = new List<SocialAxisReport>
		{
			new SocialAxisReport { axis = SocialAxis.Class },
			new SocialAxisReport { axis = SocialAxis.Stratum },
			new SocialAxisReport { axis = SocialAxis.Identity },
			new SocialAxisReport { axis = SocialAxis.Occupation }
		};
		Dictionary<string, SocialAxisBucket>[] buckets = new Dictionary<string, SocialAxisBucket>[4];
		for (int i = 0; i < buckets.Length; i++)
		{
			buckets[i] = new Dictionary<string, SocialAxisBucket>();
		}
		if (world?.tiles == null)
		{
			return reports;
		}
		float total = 0f;
		for (int i = 0; i < world.tiles.Length; i++)
		{
			TileData tile = world.tiles[i];
			if (!tile.exists || !tile.isLand || tile.ownerRealmId != realmId || tile.populationBlocks == null)
			{
				continue;
			}
			foreach (PopulationBlock block in tile.populationBlocks)
			{
				if (block.count <= 0f)
				{
					continue;
				}
				total += block.count;
				IdentityProfile identity = PopulationIdentity.Resolve(in block);
				Add(buckets[0], "class:" + identity.economicClass, block.count);
				Add(buckets[1], "stratum:" + identity.stratum, block.count);
				SocialIdentityFlags flags = LegacyIdentityMapping.FromLegacy(block.socialClass).identity;
				AddFlags(buckets[2], "identity:", (int)flags, block.count);
				AddFlags(buckets[3], "occupation:", (int)identity.occupations, block.count);
			}
		}
		for (int i = 0; i < reports.Count; i++)
		{
			reports[i].buckets.AddRange(buckets[i].Values);
			reports[i].totalPopulation = total;
		}
		return reports;
	}

	private static void AddFlags(Dictionary<string, SocialAxisBucket> sink, string prefix, int flags, float population)
	{
		if (flags == 0)
		{
			Add(sink, prefix + "None", population);
			return;
		}
		int bitCount = 0;
		for (int bit = 1; bit != 0 && bit <= flags; bit <<= 1)
		{
			if ((flags & bit) != 0) bitCount++;
		}
		if (bitCount <= 0)
		{
			Add(sink, prefix + "None", population);
			return;
		}
		float share = population / bitCount;
		for (int bit = 1; bit != 0 && bit <= flags; bit <<= 1)
		{
			if ((flags & bit) != 0)
			{
				Add(sink, prefix + bit, share);
			}
		}
	}

	private static void Add(Dictionary<string, SocialAxisBucket> sink, string key, float population)
	{
		if (!sink.TryGetValue(key, out SocialAxisBucket bucket))
		{
			bucket = new SocialAxisBucket { key = key };
			sink[key] = bucket;
		}
		bucket.population += population;
	}
}
}
