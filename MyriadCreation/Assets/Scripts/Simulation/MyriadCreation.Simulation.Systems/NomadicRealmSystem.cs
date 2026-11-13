using System.Collections.Generic;
using MyriadCreation.Core.Data;
using MyriadCreation.Simulation.State;
using MyriadCreation.Simulation.WorldState;
using MyriadCreation.World.Settlement;
using UnityEngine;
using MyriadCreation.Content.Definitions;

namespace MyriadCreation.Simulation.Systems
{
public static class NomadicRealmSystem
{
	public enum Season
	{
		Spring,
		Summer,
		Autumn,
		Winter
	}

	public static RealmForm FormFromMobility(int mobilityType)
	{
		return mobilityType switch
		{
			0 => RealmForm.Nomadic, 
			1 => RealmForm.SemiSedentary, 
			_ => RealmForm.Sedentary, 
		};
	}

	public static void SeasonalMigration(GameWorld world, int realmId, Season season)
	{
		if ((Object)(object)world == (Object)null || !world.realms.TryGetValue(realmId, out var value))
		{
			return;
		}
		NomadicRange nomadicRange = value.nomadicRange;
		if (nomadicRange != null)
		{
			int num = ((season == Season.Winter) ? nomadicRange.winterCampTile : nomadicRange.summerCampTile);
			if (num < 0)
			{
				num = nomadicRange.centerTile;
			}
			if (num >= 0)
			{
				nomadicRange.centerTile = num;
				Debug.Log((object)$"[NomadicRealm] {value.realmName} 王庭迁移至地块{num}（{season}）");
			}
		}
	}

	public static float CollectTribute(GameWorld world, int realmId)
	{
		if ((Object)(object)world == (Object)null)
		{
			return 0f;
		}
		if (!world.realms.TryGetValue(realmId, out var value))
		{
			return 0f;
		}
		if (value.realmForm != RealmForm.Nomadic || value.nomadicRange == null)
		{
			return 0f;
		}
		float num = 0f;
		foreach (KeyValuePair<int, SettlementData> settlement in world.settlements)
		{
			SettlementData value2 = settlement.Value;
			if (!value2.IsRuined && value.nomadicRange.IsInRange(value2.tileIndex, world.mapWidth, world.mapHeight) && world.tiles[value2.tileIndex].ownerRealmId != realmId)
			{
				float influenceAt = value.nomadicRange.GetInfluenceAt(value2.tileIndex, world.mapWidth, world.mapHeight);
				float num2 = value2.wealth * 0.05f * influenceAt;
				value2.wealth -= num2;
				num += num2;
				world.settlements[settlement.Key] = value2;
			}
		}
		RealmTreasury.Add(value, num);
		return num;
	}

	public static bool SettleRealm(GameWorld world, int realmId, int capitalTile)
	{
		if ((Object)(object)world == (Object)null)
		{
			return false;
		}
		if (!world.realms.TryGetValue(realmId, out var value))
		{
			return false;
		}
		if (value.realmForm == RealmForm.Sedentary)
		{
			return false;
		}
		value.realmForm = RealmForm.Sedentary;
		if (value.nomadicRange != null)
		{
			for (int i = 0; i < world.tiles.Length; i++)
			{
				if (value.nomadicRange.IsInCore(i, world.mapWidth, world.mapHeight))
				{
					TileData tileData = world.tiles[i];
					if (tileData.exists && tileData.isLand && tileData.ownerRealmId < 0)
					{
						tileData.ownerRealmId = realmId;
						value.coreTiles.Add(i);
						world.tiles[i] = tileData;
					}
				}
			}
			value.nomadicRange = null;
		}
		Debug.Log((object)$"[NomadicRealm] {value.realmName} 完成定居化改革，定都地块{capitalTile}");
		return true;
	}

	public static void NomadicAbandon(GameWorld world, int realmId, int tileIndex)
	{
		LandAbandonmentSystem.AbandonTile(world, tileIndex, AbandonmentType.Nomadic, realmId);
	}

	public static void GetNomadicMilitaryBonus(RealmData realm, out float manpowerRatio, out float cavalryBonus, out float mobilityBonus)
	{
		manpowerRatio = 0f;
		cavalryBonus = 0f;
		mobilityBonus = 0f;
		if (realm.realmForm == RealmForm.Nomadic)
		{
			manpowerRatio = 0.25f;
			switch (realm.nomadicSubtype)
			{
			case NomadicSubtype.Steppe:
				cavalryBonus = 0.3f;
				mobilityBonus = 0.4f;
				break;
			case NomadicSubtype.Highland:
				cavalryBonus = 0.1f;
				mobilityBonus = 0.1f;
				break;
			case NomadicSubtype.Desert:
				cavalryBonus = 0.2f;
				mobilityBonus = 0.3f;
				break;
			case NomadicSubtype.Pastoral:
				cavalryBonus = 0.15f;
				mobilityBonus = 0.2f;
				break;
			}
		}
	}
}
}
