using System;
using System.Collections.Generic;
using UnityEngine;

namespace MyriadCreation.Simulation.Units
{
[Serializable]
public class TreasuryVault
{
	public List<TreasuryGoodsStack> goods = new List<TreasuryGoodsStack>();

	public bool legacyCurrencyMigrated;

	public float Get(int goodsId)
	{
		// 0 号是粮食（合法物产），"无效"用 -1
		if (goodsId < 0 || goods == null)
		{
			return 0f;
		}
		float num = 0f;
		for (int i = 0; i < goods.Count; i++)
		{
			if (goods[i].goodsId == goodsId)
			{
				num += goods[i].amount;
			}
		}
		return num;
	}

	public bool Has(int goodsId, float amount)
	{
		if (amount <= 0f)
		{
			return true;
		}
		return Get(goodsId) + 0.0001f >= amount;
	}

	public void Add(int goodsId, float amount)
	{
		// 0 号是粮食（合法物产），"无效"用 -1
		if (goodsId < 0 || Mathf.Approximately(amount, 0f))
		{
			return;
		}
		if (goods == null)
		{
			goods = new List<TreasuryGoodsStack>();
		}
		for (int i = 0; i < goods.Count; i++)
		{
			TreasuryGoodsStack value = goods[i];
			if (value.goodsId == goodsId)
			{
				value.amount = Mathf.Max(0f, value.amount + amount);
				goods[i] = value;
				return;
			}
		}
		if (amount > 0f)
		{
			goods.Add(new TreasuryGoodsStack(goodsId, amount));
		}
	}

	public bool TrySpend(int goodsId, float amount)
	{
		if (amount <= 0f)
		{
			return true;
		}
		if (goodsId < 0)
		{
			return false;
		}
		if (Get(goodsId) + 0.0001f < amount)
		{
			return false;
		}
		float num = amount;
		int num2 = goods.Count - 1;
		while (num2 >= 0 && num > 1E-05f)
		{
			TreasuryGoodsStack value = goods[num2];
			if (value.goodsId == goodsId)
			{
				float num3 = Mathf.Min(value.amount, num);
				value.amount -= num3;
				num -= num3;
				if (value.amount <= 0.0001f)
				{
					goods.RemoveAt(num2);
				}
				else
				{
					goods[num2] = value;
				}
			}
			num2--;
		}
		return true;
	}

	public float Total()
	{
		float num = 0f;
		if (goods == null)
		{
			return num;
		}
		for (int i = 0; i < goods.Count; i++)
		{
			num += goods[i].amount;
		}
		return num;
	}

	public float CurrencyAmount(int currencyGoodsId)
	{
		return Get(currencyGoodsId);
	}

	public void Clear()
	{
		goods?.Clear();
		legacyCurrencyMigrated = false;
	}
}
}
