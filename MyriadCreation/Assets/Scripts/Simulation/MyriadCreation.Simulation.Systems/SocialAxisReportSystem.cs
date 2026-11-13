using System.Collections.Generic;
using MyriadCreation.Content.Definitions;
using MyriadCreation.Core.Data;
using MyriadCreation.Core.Enums;
using MyriadCreation.Simulation.State;
using MyriadCreation.Simulation.WorldState;
using UnityEngine;

namespace MyriadCreation.Simulation.Systems
{
/// <summary>
/// D25 四轴人口/需求报告：每条人口块按四轴各归一次；身份/职业是位标志，按位展开并把人口均分，
/// 保证四轴人口合计与人口块总量相等（守恒）。需求满足度沿用阶级需求评分，再叠加阶层/身份/职业位置修正。
/// </summary>
public static class SocialAxisReportSystem
{
	public static List<SocialAxisReport> Build(GameWorld world, int realmId, RealmSituation situation = null)
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
		Dictionary<GameEnums.SocialClass, ClassNeedReport> classNeeds = (situation != null) ? new ClassNeedsSystem().EvaluateRealm(situation) : null;
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
				float score = 50f;
				if (classNeeds != null && classNeeds.TryGetValue(block.socialClass, out ClassNeedReport needReport) && needReport != null)
				{
					score = needReport.overallSatisfaction;
				}
				Add(buckets[0], "class:" + identity.economicClass, block.count, score);
				Add(buckets[1], "stratum:" + identity.stratum, block.count, score + StratumModifier(identity.stratum));
				SocialIdentityFlags flags = LegacyIdentityMapping.FromLegacy(block.socialClass).identity;
				AddFlags(buckets[2], "identity:", (int)flags, block.count, score, true);
				AddFlags(buckets[3], "occupation:", (int)identity.occupations, block.count, score, false);
			}
		}
		for (int i = 0; i < reports.Count; i++)
		{
			float weighted = 0f;
			foreach (SocialAxisBucket bucket in buckets[i].Values)
			{
				bucket.needsSatisfaction = (bucket.population > 0f) ? (bucket.needsSatisfaction / bucket.population) : 50f;
				weighted += bucket.needsSatisfaction * bucket.population;
			}
			reports[i].buckets.AddRange(buckets[i].Values);
			reports[i].totalPopulation = total;
			reports[i].axisSatisfaction = (total > 0f) ? (weighted / total) : 50f;
		}
		return reports;
	}

	/// <summary>四轴加权 unrest（0–100）：轴满足度按可配权重合成，再取补。</summary>
	public static float ComputeAxisUnrest(List<SocialAxisReport> reports, SocialAxisWeights weights)
	{
		if (reports == null || reports.Count == 0)
		{
			return 50f;
		}
		float weighted = 0f;
		float weightSum = 0f;
		foreach (SocialAxisReport report in reports)
		{
			float weight = (weights != null) ? weights.WeightOf(report.axis) : 0.25f;
			if (weight <= 0f)
			{
				continue;
			}
			weighted += report.axisSatisfaction * weight;
			weightSum += weight;
		}
		float satisfaction = (weightSum > 0f) ? (weighted / weightSum) : 50f;
		return Mathf.Clamp(100f - satisfaction, 0f, 100f);
	}

	private static void AddFlags(Dictionary<string, SocialAxisBucket> sink, string prefix, int flags, float population, float classScore, bool identity)
	{
		if (flags == 0)
		{
			Add(sink, prefix + "None", population, classScore - 2f);
			return;
		}
		int bitCount = CountBits(flags);
		if (bitCount <= 0)
		{
			Add(sink, prefix + "None", population, classScore - 2f);
			return;
		}
		float share = population / bitCount;
		for (int bit = 1; bit != 0 && bit <= flags; bit <<= 1)
		{
			if ((flags & bit) != 0)
			{
				float modifier = identity ? IdentityModifier(bit) : OccupationModifier(bit);
				Add(sink, prefix + bit, share, classScore + modifier);
			}
		}
	}

	private static int CountBits(int flags)
	{
		int count = 0;
		for (int bit = 1; bit != 0 && bit <= flags; bit <<= 1)
		{
			if ((flags & bit) != 0)
			{
				count++;
			}
		}
		return count;
	}

	private static float StratumModifier(SocialStratum stratum)
	{
		return stratum switch
		{
			SocialStratum.Upper => 8f,
			SocialStratum.UpperMiddle => 4f,
			SocialStratum.Middle => 0f,
			SocialStratum.LowerMiddle => -4f,
			SocialStratum.Lower => -8f,
			_ => 0f
		};
	}

	private static float IdentityModifier(int flag)
	{
		return (SocialIdentityFlags)flag switch
		{
			SocialIdentityFlags.Royalty => 10f,
			SocialIdentityFlags.Noble => 6f,
			SocialIdentityFlags.Clergy => 4f,
			SocialIdentityFlags.Citizen => 2f,
			SocialIdentityFlags.Freedman => 0f,
			SocialIdentityFlags.Foreigner => -3f,
			SocialIdentityFlags.Outlaw => -12f,
			_ => -2f
		};
	}

	private static float OccupationModifier(int flag)
	{
		return (OccupationFlags)flag switch
		{
			OccupationFlags.Farmer => 0f,
			OccupationFlags.Artisan => 2f,
			OccupationFlags.Merchant => 4f,
			OccupationFlags.Scholar => 4f,
			OccupationFlags.Soldier => -2f,
			OccupationFlags.Official => 5f,
			OccupationFlags.Priest => 3f,
			OccupationFlags.Miner => -4f,
			OccupationFlags.Mariner => -1f,
			OccupationFlags.Pastoralist => 0f,
			OccupationFlags.Physician => 4f,
			OccupationFlags.Teacher => 3f,
			OccupationFlags.Laborer => -5f,
			_ => -2f
		};
	}

	private static void Add(Dictionary<string, SocialAxisBucket> sink, string key, float population, float score)
	{
		if (!sink.TryGetValue(key, out SocialAxisBucket bucket))
		{
			bucket = new SocialAxisBucket { key = key };
			sink[key] = bucket;
		}
		bucket.population += population;
		bucket.needsSatisfaction += Mathf.Clamp(score, 0f, 100f) * population;
	}
}
}
