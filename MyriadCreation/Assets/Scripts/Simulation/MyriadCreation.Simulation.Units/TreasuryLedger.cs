using System.Collections.Generic;
using MyriadCreation.Core.Enums;
using MyriadCreation.Simulation.State;
using MyriadCreation.Simulation.Systems;
using MyriadCreation.Simulation.WorldState;
using UnityEngine;
using MyriadCreation.Content.Definitions;

namespace MyriadCreation.Simulation.Units
{
public static class TreasuryLedger
{
	public static TreasuryVault NationalVault(RealmData realm)
	{
		if (realm == null)
		{
			return null;
		}
		if (realm.currencySystem == null)
		{
			realm.currencySystem = new CurrencySystem();
		}
		if (realm.currencySystem.vault == null)
		{
			realm.currencySystem.vault = new TreasuryVault();
		}
		return realm.currencySystem.vault;
	}

	public static int SelectCurrencyGoodsId(IReadOnlyDictionary<int, GoodsDef> goodsDefs)
	{
		if (goodsDefs == null)
		{
			return -1;
		}
		int num = -1;
		float metalValue = -1f;
		int num2 = -1;
		foreach (KeyValuePair<int, GoodsDef> goodsDef in goodsDefs)
		{
			GoodsDef value = goodsDef.Value;
			if (value.category == GameEnums.GoodsCategory.Metal && value.baseValue > metalValue)
			{
				metalValue = value.baseValue;
				num = goodsDef.Key;
			}
			if (value.category == GameEnums.GoodsCategory.Gem && num2 < 0)
			{
				num2 = goodsDef.Key;
			}
		}
		if (num < 0)
		{
			return num2;
		}
		return num;
	}

	public static int SelectFoodGoodsId(IReadOnlyDictionary<int, GoodsDef> goodsDefs)
	{
		if (goodsDefs == null)
		{
			return -1;
		}
		int num = -1;
		int num2 = -1;
		foreach (KeyValuePair<int, GoodsDef> goodsDef in goodsDefs)
		{
			GoodsDef value = goodsDef.Value;
			if (value.category == GameEnums.GoodsCategory.Food)
			{
				return goodsDef.Key;
			}
			if (value.category == GameEnums.GoodsCategory.Crop && num < 0)
			{
				num = goodsDef.Key;
			}
			if (value.category == GameEnums.GoodsCategory.Livestock && num2 < 0)
			{
				num2 = goodsDef.Key;
			}
		}
		if (num < 0)
		{
			return num2;
		}
		return num;
	}

	public static void ReconcileNationalCurrency(RealmData realm, int currencyGoodsId)
	{
		if (realm == null || currencyGoodsId < 0)
		{
			return;
		}
		TreasuryVault treasuryVault = NationalVault(realm);
		float num = treasuryVault.CurrencyAmount(currencyGoodsId);
		float treasury = realm.treasury;
		if (treasury < 0f)
		{
			return;
		}
		if (!treasuryVault.legacyCurrencyMigrated)
		{
			if (treasury > 0f)
			{
				treasuryVault.Add(currencyGoodsId, treasury);
			}
			treasuryVault.legacyCurrencyMigrated = true;
		}
		else
		{
			float num2 = treasury - num;
			if (Mathf.Abs(num2) > 0.001f)
			{
				treasuryVault.Add(currencyGoodsId, num2);
			}
		}
	}

	public static TradeCenter GetRegionalVault(GameWorld world, int regionId)
	{
		if ((Object)(object)world == (Object)null || world.tradeCenters == null || regionId < 0)
		{
			return null;
		}
		if (!world.tradeCenters.TryGetValue(regionId, out var value))
		{
			return null;
		}
		return value;
	}

	public static bool TryPay(RealmData realm, GameWorld world, UnitFundingSource source, int regionId, IReadOnlyDictionary<int, GoodsDef> goodsDefs, UnitProvisioningCost cost, int payerCharacterId, out UnitFunding funding, out string reason)
	{
		reason = "";
		funding = new UnitFunding
		{
			source = source,
			payerCharacterId = payerCharacterId,
			foodGoodsId = cost.foodGoodsId,
			currencyGoodsId = cost.currencyGoodsId
		};
		if (cost.money <= 0f && cost.food <= 0f)
		{
			return true;
		}
		if (realm == null)
		{
			reason = "没有政权金库";
			return false;
		}
		if (funding.currencyGoodsId < 0)
		{
			funding.currencyGoodsId = SelectCurrencyGoodsId(goodsDefs);
		}
		if (funding.foodGoodsId < 0)
		{
			funding.foodGoodsId = SelectFoodGoodsId(goodsDefs);
		}
		// 结算前必须拿到真实物产 id：粮食没有别的兜底（没有"国库粮食账本"），拿不到就不能假成功；
		// 货币则允许在没有任何货币物产时回退到 realm.treasury 旧账本（见 TryPayNational）。
		if (cost.food > 0f && funding.foodGoodsId < 0)
		{
			reason = "缺少粮食物产";
			return false;
		}
		switch (source)
		{
		case UnitFundingSource.Personal:
		case UnitFundingSource.Household:
			// 个人/家族钱包尚未接入（CharacterData 无可用账户结算），不能凭空记账。
			// 先由国家金库垫付；国库没有实物粮草时回退区域库（生成局的城/省库有粮），
			// 并把 funding.source 回写成真实付款方——否则传令官/队伍会"根本发不出"。
			{
				funding.source = UnitFundingSource.NationalTreasury;
				if (TryPayNational(realm, funding, cost, out reason))
				{
					reason = "";
					return true;
				}
				TradeCenter fallbackVault = GetRegionalVault(world, regionId);
				if (fallbackVault != null && CanPayFromRegional(fallbackVault, funding, cost))
				{
					SpendRegional(fallbackVault, funding, cost);
					funding.source = UnitFundingSource.CityTreasury;
					funding.moneyPaid = cost.money;
					funding.foodPaid = cost.food;
					reason = "";
					return true;
				}
				return false;
			}
		case UnitFundingSource.CityTreasury:
		case UnitFundingSource.ProvinceTreasury:
		{
			TradeCenter regionalVault = GetRegionalVault(world, regionId);
			if (regionalVault != null && CanPayFromRegional(regionalVault, funding, cost))
			{
				SpendRegional(regionalVault, funding, cost);
				funding.moneyPaid = cost.money;
				funding.foodPaid = cost.food;
				return true;
			}
			if (TryPayNational(realm, funding, cost, out reason))
			{
				// 实际是国库付的，来源必须回写，避免 UI/统计把账记在城库/省库上。
				funding.source = UnitFundingSource.NationalTreasury;
				reason = "";
				return true;
			}
			if (string.IsNullOrEmpty(reason))
			{
				reason = ((source == UnitFundingSource.CityTreasury) ? "城库货物不足" : "省库货物不足");
			}
			return false;
		}
		default:
			return TryPayNational(realm, funding, cost, out reason);
		}
	}

	private static bool CanPayFromRegional(TradeCenter tc, UnitFunding funding, UnitProvisioningCost cost)
	{
		if (tc == null || tc.inventory == null)
		{
			return false;
		}
		if (cost.money > 0f)
		{
			if (funding.currencyGoodsId < 0)
			{
				return false;
			}
			if (!tc.inventory.TryGetValue(funding.currencyGoodsId, out var value))
			{
				return false;
			}
			if (value + 0.0001f < cost.money)
			{
				return false;
			}
		}
		if (cost.food > 0f)
		{
			if (funding.foodGoodsId < 0)
			{
				return false;
			}
			if (!tc.inventory.TryGetValue(funding.foodGoodsId, out var value2))
			{
				return false;
			}
			if (value2 + 0.0001f < cost.food)
			{
				return false;
			}
		}
		return true;
	}

	private static void SpendRegional(TradeCenter tc, UnitFunding funding, UnitProvisioningCost cost)
	{
		if (cost.money > 0f)
		{
			tc.inventory[funding.currencyGoodsId] = tc.inventory[funding.currencyGoodsId] - cost.money;
			if (tc.inventory[funding.currencyGoodsId] <= 0.0001f)
			{
				tc.inventory.Remove(funding.currencyGoodsId);
			}
		}
		if (cost.food > 0f)
		{
			tc.inventory[funding.foodGoodsId] = tc.inventory[funding.foodGoodsId] - cost.food;
			if (tc.inventory[funding.foodGoodsId] <= 0.0001f)
			{
				tc.inventory.Remove(funding.foodGoodsId);
			}
		}
	}

	private static bool TryPayNational(RealmData realm, UnitFunding funding, UnitProvisioningCost cost, out string reason)
	{
		reason = "";
		TreasuryVault treasuryVault = NationalVault(realm);
		int currencyGoodsId = funding.currencyGoodsId;
		bool flag = realm.treasury < 0f;
		if (currencyGoodsId >= 0)
		{
			ReconcileNationalCurrency(realm, currencyGoodsId);
		}
		bool flag2 = false;
		bool flag3 = false;
		if (cost.money > 0f)
		{
			if (currencyGoodsId >= 0)
			{
				if (!treasuryVault.TrySpend(currencyGoodsId, cost.money))
				{
					reason = "国库货币货物不足";
					return false;
				}
			}
			else
			{
				if (realm.treasury + 0.0001f < cost.money)
				{
					reason = "国库不足";
					return false;
				}
				realm.treasury -= cost.money;
				flag3 = true;
			}
			flag2 = true;
		}
		if (cost.food > 0f && funding.foodGoodsId >= 0 && !treasuryVault.TrySpend(funding.foodGoodsId, cost.food))
		{
			if (flag2)
			{
				if (currencyGoodsId >= 0)
				{
					treasuryVault.Add(currencyGoodsId, cost.money);
				}
				else if (flag3)
				{
					realm.treasury += cost.money;
				}
			}
			reason = "国库粮草货物不足";
			return false;
		}
		if (currencyGoodsId >= 0 && !flag)
		{
			realm.treasury = treasuryVault.CurrencyAmount(currencyGoodsId);
		}
		funding.moneyPaid = cost.money;
		funding.foodPaid = ((cost.food > 0f && funding.foodGoodsId >= 0) ? cost.food : 0f);
		return true;
	}
}
}
