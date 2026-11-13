using System;
using System.Collections.Generic;
using MyriadCreation.Core.Data;
using MyriadCreation.Core.Enums;
using MyriadCreation.Simulation.State;
using UnityEngine;
using MyriadCreation.Content.Definitions;

namespace MyriadCreation.Simulation.Systems
{
public class SocietyManager
{
	public ClassNeedsSystem Needs { get; } = new ClassNeedsSystem();

	private static float BaseOrganization(GameEnums.SocialClass cls)
	{
		return cls switch
		{
			GameEnums.SocialClass.Royalty => 0.95f, 
			GameEnums.SocialClass.NobilityClergy => 0.75f, 
			GameEnums.SocialClass.MerchantFreeman => 0.55f, 
			GameEnums.SocialClass.Peasant => 0.25f, 
			GameEnums.SocialClass.Slave => 0.1f, 
			_ => 0.3f, 
		};
	}

	private static float InstitutionalBase(GameEnums.SocialClass cls, RealmSituation sit)
	{
		return cls switch
		{
			GameEnums.SocialClass.Royalty => 42f, 
			GameEnums.SocialClass.NobilityClergy => 14f, 
			_ => 0f, 
		} + sit.GetPoliticalAccess(cls) * 12f;
	}

	public RealmSociety EvaluateRealm(RealmData realm, TileData[] tiles, RealmSituation sit, IReadOnlyList<int> realmTiles = null)
	{
		RealmSociety realmSociety = new RealmSociety
		{
			realmId = realm.realmId
		};
		Dictionary<GameEnums.SocialClass, float> dictionary = new Dictionary<GameEnums.SocialClass, float>();
		Dictionary<EconomicClass, float> dictionary2 = new Dictionary<EconomicClass, float>();
		Dictionary<SocialStratum, float> dictionary3 = new Dictionary<SocialStratum, float>();
		float total = 0f;
		if (realmTiles != null)
		{
			foreach (int realmTile in realmTiles)
			{
				if (realmTile >= 0 && realmTile < tiles.Length)
				{
					Accumulate(tiles[realmTile], dictionary, ref total, dictionary2, dictionary3);
				}
			}
		}
		else
		{
			foreach (int coreTile in realm.coreTiles)
			{
				if (coreTile >= 0 && coreTile < tiles.Length)
				{
					Accumulate(tiles[coreTile], dictionary, ref total, dictionary2, dictionary3);
				}
			}
			for (int i = 0; i < tiles.Length; i++)
			{
				if (tiles[i].ownerRealmId == realm.realmId && !realm.coreTiles.Contains(i))
				{
					Accumulate(tiles[i], dictionary, ref total, dictionary2, dictionary3);
				}
			}
		}
		realmSociety.totalPopulation = total;
		realmSociety.populationByEconomicClass = dictionary2;
		realmSociety.populationByStratum = dictionary3;
		float num = -1f;
		foreach (KeyValuePair<EconomicClass, float> item in dictionary2)
		{
			if (item.Value > num)
			{
				num = item.Value;
				realmSociety.dominantEconomicClass = item.Key;
			}
		}
		Dictionary<GameEnums.SocialClass, ClassNeedReport> dictionary4 = Needs.EvaluateRealm(sit);
		float num2 = 0f;
		float num3 = 0f;
		float num4 = -1f;
		float num5 = -1f;
		foreach (GameEnums.SocialClass value2 in Enum.GetValues(typeof(GameEnums.SocialClass)))
		{
			float valueOrDefault = dictionary.GetValueOrDefault(value2, 0f);
			ClassNeedReport classNeedReport = dictionary4[value2];
			float num6 = ((total > 0f) ? Mathf.Clamp01(valueOrDefault / total) : 0f);
			float num7 = BaseOrganization(value2);
			if (value2 != GameEnums.SocialClass.Slave)
			{
				num7 = Mathf.Clamp01(num7 + (sit.GetPoliticalAccess(value2) - 0.3f) * 0.15f);
			}
			float num8 = Mathf.Clamp(num6 * num7 * 100f + InstitutionalBase(value2, sit), 0f, 100f);
			float num9 = Mathf.Clamp01((100f - classNeedReport.overallSatisfaction) / 100f);
			float num10 = num8 * num9;
			float support = num8 * (1f - num9);
			IdentityProfile identityProfile = LegacyIdentityMapping.FromLegacy(value2);
			ClassProfile value = new ClassProfile
			{
				socialClass = value2,
				economicClass = identityProfile.economicClass,
				stratum = identityProfile.stratum,
				population = valueOrDefault,
				populationShare = num6,
				satisfaction = classNeedReport.overallSatisfaction,
				organization = num7,
				influence = num8,
				unrest = num10,
				support = support,
				loyalty = realm.classRelations.GetValueOrDefault(value2, 50f),
				chiefGrievance = classNeedReport.worstDimension,
				chiefGrievanceReason = classNeedReport.Get(classNeedReport.worstDimension).reason,
				needReport = classNeedReport
			};
			realmSociety.classes[value2] = value;
			if (valueOrDefault > num4)
			{
				num4 = valueOrDefault;
				realmSociety.dominantClass = value2;
			}
			if (num10 > num5)
			{
				num5 = num10;
				realmSociety.mostRestlessClass = value2;
			}
			num2 += num10;
			num3 += num8;
		}
		realmSociety.unrestScore = ((num3 > 0f) ? Mathf.Clamp(num2 / num3 * 100f, 0f, 100f) : 0f);
		return realmSociety;
	}

	private static void Accumulate(TileData tile, Dictionary<GameEnums.SocialClass, float> acc, ref float total, Dictionary<EconomicClass, float> byEconomicClass = null, Dictionary<SocialStratum, float> byStratum = null)
	{
		if (tile.populationBlocks == null)
		{
			return;
		}
		foreach (PopulationBlock populationBlock in tile.populationBlocks)
		{
			PopulationBlock block = populationBlock;
			acc[block.socialClass] = acc.GetValueOrDefault(block.socialClass, 0f) + block.count;
			total += block.count;
			PopulationIdentity.Add(in block, byEconomicClass, byStratum);
		}
	}

	public void ApplyClassRelations(RealmData realm, RealmSociety society, float daysPerTick)
	{
		foreach (KeyValuePair<GameEnums.SocialClass, ClassProfile> @class in society.classes)
		{
			GameEnums.SocialClass key = @class.Key;
			ClassProfile value = @class.Value;
			float valueOrDefault = realm.classRelations.GetValueOrDefault(key, 50f);
			float num = Mathf.Clamp01(((valueOrDefault > value.satisfaction) ? 0.02f : 0.012f) * daysPerTick);
			float num2 = Mathf.Lerp(valueOrDefault, value.satisfaction, num);
			realm.classRelations[key] = Mathf.Clamp(num2, 0f, 100f);
			value.loyalty = realm.classRelations[key];
		}
	}
}
}
