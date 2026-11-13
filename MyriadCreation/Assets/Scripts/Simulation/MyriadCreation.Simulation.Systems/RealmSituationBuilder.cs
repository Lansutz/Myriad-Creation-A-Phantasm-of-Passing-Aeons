using System;
using System.Collections.Generic;
using MyriadCreation.Core.Data;
using MyriadCreation.Core.Enums;
using MyriadCreation.Simulation.ContentHost;
using MyriadCreation.Simulation.State;
using UnityEngine;
using MyriadCreation.Simulation.Laws;
using MyriadCreation.Simulation.Culture;

namespace MyriadCreation.Simulation.Systems
{
public static class RealmSituationBuilder
{
	private const int FoodGoodsId = 0;

	private const float FoodReserveTargetDays = 30f;

	public static RealmSituation Build(RealmData realm, TileData[] tiles, EconomyManager economy = null, List<WarState> wars = null, Dictionary<int, Army> armies = null, DisasterSystem disasters = null, InnovationTree innovations = null, IReadOnlyList<int> realmTiles = null)
	{
		RealmSituation realmSituation = new RealmSituation
		{
			realmId = realm.realmId
		};
		float num = 0f;
		float num2 = 0f;
		float num3 = 0f;
		int num4 = 0;
		HashSet<int> hashSet = new HashSet<int>();
		float num5 = 0f;
		bool flag = false;
		foreach (int item in EnumerateRealmTiles(realm, tiles, realmTiles))
		{
			ref TileData reference = ref tiles[item];
			hashSet.Add(reference.regionId);
			num2 += reference.order;
			num3 += reference.stability;
			num4++;
			if (reference.populationBlocks != null)
			{
				foreach (PopulationBlock populationBlock in reference.populationBlocks)
				{
					num += populationBlock.count;
				}
			}
			if (disasters != null)
			{
				foreach (ActiveDisaster item2 in disasters.GetDisastersAtTile(item))
				{
					num5 += item2.severity;
				}
			}
			if (reference.occupyingRealmId != -1 && reference.occupyingRealmId != reference.ownerRealmId)
			{
				flag = true;
			}
		}
		realmSituation.publicOrder = ((num4 > 0) ? (num2 / (float)num4) : 50f);
		if (num4 > 0)
		{
			_ = num3 / (float)num4;
		}
		realmSituation.disasterSeverity = Mathf.Clamp(num5 * 2f, 0f, 100f);
		float num6 = Mathf.Max(0.01f, num * 0.01f);
		float num7 = 0f;
		float num8 = 0f;
		int num9 = 0;
		if (economy != null)
		{
			foreach (int item3 in hashSet)
			{
				TradeCenter tradeCenter = economy.GetTradeCenter(item3);
				if (tradeCenter == null)
				{
					continue;
				}
				num7 += tradeCenter.inventory.GetValueOrDefault(0, 0f);
				if (tradeCenter.tradeRoutes == null)
				{
					continue;
				}
				foreach (TradeRoute tradeRoute in tradeCenter.tradeRoutes)
				{
					if (tradeRoute.isBlocked)
					{
						num8 += 0f;
						num9++;
					}
					else
					{
						num8 += Mathf.Clamp01(tradeRoute.currentEfficiency);
						num9++;
					}
				}
			}
		}
		realmSituation.foodSecurity = Mathf.Clamp(num7 / (num6 * 30f), 0f, 1.2f);
		realmSituation.tradeFlow = ((num9 > 0) ? (num8 / (float)num9) : 0.8f);
		bool atWar = false;
		bool warOnHomeSoil = flag;
		if (wars != null)
		{
			foreach (WarState war in wars)
			{
				if (war.ended || (war.attackerId != realm.realmId && war.defenderId != realm.realmId))
				{
					continue;
				}
				atWar = true;
				int num10 = ((war.attackerId == realm.realmId) ? war.defenderId : war.attackerId);
				if (armies == null)
				{
					continue;
				}
				foreach (Army value2 in armies.Values)
				{
					if (value2.ownerRealmId == num10 && realm.coreTiles.Contains(value2.currentTileIndex))
					{
						warOnHomeSoil = true;
						break;
					}
				}
			}
		}
		realmSituation.atWar = atWar;
		realmSituation.warOnHomeSoil = warOnHomeSoil;
		foreach (GameEnums.SocialClass value3 in Enum.GetValues(typeof(GameEnums.SocialClass)))
		{
			float num11 = ((realm.taxSystem != null) ? realm.taxSystem.GetTaxSatisfactionImpact(value3) : 0f);
			realmSituation.taxPain[value3] = Mathf.Clamp(0f - num11, 0f, 100f);
		}
		realmSituation.politicalAccess = PoliticalAccessAnalyzer.GetAllAccess(realm.laws);
		if (realm.currencySystem != null)
		{
			float currencyValue = realm.currencySystem.GetCurrencyValue();
			realmSituation.monetaryStability = Mathf.Clamp01(1f - realm.currencySystem.inflationRate * 2f) * Mathf.Clamp01(currencyValue);
		}
		else
		{
			realmSituation.monetaryStability = 1f;
		}
		CultureData culture = null;
		if (realm.primaryCultureId >= 0 && ContentResolvers.Cultures.TryGet(realm.primaryCultureId, out var value))
		{
			culture = value.data;
		}
		foreach (GameEnums.SocialClass value4 in Enum.GetValues(typeof(GameEnums.SocialClass)))
		{
			realmSituation.classRecognized[value4] = SocialClassAvailability.IsClassAvailable(value4, culture, innovations, realm.realmId);
		}
		realmSituation.stability = realm.stability;
		realmSituation.legitimacy = Mathf.Clamp(realm.stability * 0.6f + realm.prestige * 0.4f, 0f, 100f);
		LawComposition laws = realm.laws;
		float num12 = 0.3f;
		if (laws.supremeSuccession.primary == 0)
		{
			num12 += 0.3f;
		}
		if (laws.localSuccession.primary == 3)
		{
			num12 += 0.25f;
		}
		if (laws.centralInstitution.primary == 3)
		{
			num12 += 0.15f;
		}
		if (realm.taxSystem != null && realm.taxSystem.taxExemptions.GetValueOrDefault(GameEnums.SocialClass.NobilityClergy, defaultValue: false))
		{
			num12 += 0.1f;
		}
		realmSituation.privilegeSecurity = Mathf.Clamp01(num12);
		return realmSituation;
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
