using System.Collections.Generic;
using MyriadCreation.Simulation.State;
using MyriadCreation.Simulation.WorldState;
using UnityEngine;
using MyriadCreation.Simulation.Laws;
using MyriadCreation.Content.Definitions;

namespace MyriadCreation.Simulation.Units
{
public static class UnitProvisioning
{
	public static UnitFundingSource ResolveFundingSource(RealmData realm)
	{
		if (realm == null)
		{
			return UnitFundingSource.NationalTreasury;
		}
		LawComposition laws = realm.laws;
		if (laws == null)
		{
			return UnitFundingSource.Personal;
		}
		int num = ((laws.localSuccession != null) ? laws.localSuccession.primary : (-1));
		bool flag = num == 3;
		bool num2 = num == 4 || num == 1;
		bool flag2 = laws.nationalTax != null && laws.nationalTax.primary >= 0;
		bool flag3 = laws.localTax != null && laws.localTax.primary >= 0;
		bool flag4 = laws.militaryOrganization != null && laws.militaryOrganization.primary >= 0;
		if (num2)
		{
			return UnitFundingSource.CityTreasury;
		}
		if (flag && !flag2)
		{
			return UnitFundingSource.ProvinceTreasury;
		}
		if (flag2 | flag4)
		{
			return UnitFundingSource.NationalTreasury;
		}
		if (flag3)
		{
			return UnitFundingSource.ProvinceTreasury;
		}
		return UnitFundingSource.Personal;
	}

	public static bool TryFund(RealmData realm, CharacterData payer, UnitFundingSource source, float money, out UnitFunding funding, out string reason)
	{
		UnitProvisioningCost cost = new UnitProvisioningCost
		{
			money = money,
			foodGoodsId = UnitProvisioningCost.InvalidGoodsId,
			currencyGoodsId = UnitProvisioningCost.InvalidGoodsId
		};
		return TryFund(null, null, realm, payer, source, -1, cost, out funding, out reason);
	}

	public static bool TryFund(RealmData realm, CharacterData payer, UnitFundingSource source, UnitProvisioningCost cost, out UnitFunding funding, out string reason)
	{
		return TryFund(null, null, realm, payer, source, -1, cost, out funding, out reason);
	}

	public static bool TryFund(GameWorld world, RealmData realm, CharacterData payer, UnitFundingSource source, int regionId, UnitProvisioningCost cost, out UnitFunding funding, out string reason)
	{
		Dictionary<int, GoodsDef> goodsDefs = (((Object)(object)world != (Object)null) ? world.goodsDefs : null);
		return TryFund(world, goodsDefs, realm, payer, source, regionId, cost, out funding, out reason);
	}

	public static bool TryFund(GameWorld world, IReadOnlyDictionary<int, GoodsDef> goodsDefs, RealmData realm, CharacterData payer, UnitFundingSource source, int regionId, UnitProvisioningCost cost, out UnitFunding funding, out string reason)
	{
		// 0 号是粮食（合法物产），"未指定"用 -1。
		if (cost.money > 0f && cost.currencyGoodsId < 0)
		{
			cost.currencyGoodsId = TreasuryLedger.SelectCurrencyGoodsId(goodsDefs);
		}
		if (cost.food > 0f && cost.foodGoodsId < 0)
		{
			cost.foodGoodsId = TreasuryLedger.SelectFoodGoodsId(goodsDefs);
		}
		return TreasuryLedger.TryPay(realm, world, source, regionId, goodsDefs, cost, payer?.characterId ?? (-1), out funding, out reason);
	}

	public static void Equip(TransportUnit unit, UnitProvisioningCost cost, UnitFunding funding, IReadOnlyDictionary<int, GoodsDef> goodsDefs = null)
	{
		if (unit == null)
		{
			return;
		}
		// 发货以 funding 为唯一事实源（TryPay 已在其中记录实际选中的物产 id），
		// 不能用调用方原始 cost 反向覆盖，否则"钱扣了、货没发"。
		int foodGoodsId = funding != null ? funding.foodGoodsId : cost.foodGoodsId;
		int currencyGoodsId = funding != null ? funding.currencyGoodsId : cost.currencyGoodsId;
		if (foodGoodsId < 0) foodGoodsId = cost.foodGoodsId;
		if (currencyGoodsId < 0) currencyGoodsId = cost.currencyGoodsId;

		float unitWeight = 0.1f;
		float unitWeight2 = 0.01f;
		if (goodsDefs != null)
		{
			if (foodGoodsId >= 0 && goodsDefs.TryGetValue(foodGoodsId, out var value))
			{
				unitWeight = Mathf.Max(0.001f, value.weight);
			}
			if (currencyGoodsId >= 0 && goodsDefs.TryGetValue(currencyGoodsId, out var value2))
			{
				unitWeight2 = Mathf.Max(0.001f, value2.weight);
			}
		}
		if (cost.food > 0f && foodGoodsId >= 0)
		{
			unit.AddCargo(foodGoodsId, cost.food, unitWeight);
		}
		if (cost.money > 0f && currencyGoodsId >= 0)
		{
			unit.AddCargo(currencyGoodsId, cost.money, unitWeight2);
		}
		for (int i = 0; i < cost.horses; i++)
		{
			unit.animals.Add(PackAnimalKind.Horse);
		}
		for (int j = 0; j < cost.carts; j++)
		{
			unit.carts.Add(CartKind.Handcart);
		}
		unit.funding = funding;
	}
}
}
