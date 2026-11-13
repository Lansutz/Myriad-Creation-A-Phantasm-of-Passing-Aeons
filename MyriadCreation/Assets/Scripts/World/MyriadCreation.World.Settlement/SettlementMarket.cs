using System;
using System.Collections.Generic;

namespace MyriadCreation.World.Settlement;

public sealed class SettlementMarket
{
	public readonly Dictionary<int, float> inventory = new Dictionary<int, float>();

	public readonly Dictionary<int, float> demand = new Dictionary<int, float>();

	public readonly Dictionary<int, float> supply = new Dictionary<int, float>();

	public float capacity;

	public SettlementMarket(float capacity)
	{
		this.capacity = Math.Max(0f, capacity);
	}

	public float TotalStock()
	{
		float num = 0f;
		foreach (KeyValuePair<int, float> item in inventory)
		{
			num += item.Value;
		}
		return num;
	}

	public float GetStock(int goodsId)
	{
		return inventory.GetValueOrDefault(goodsId, 0f);
	}

	public bool AddStock(int goodsId, float amount)
	{
		if (goodsId < 0 || amount <= 0f)
		{
			return false;
		}
		if (TotalStock() + amount > capacity + 0.0001f)
		{
			return false;
		}
		inventory[goodsId] = GetStock(goodsId) + amount;
		return true;
	}

	public bool TryTake(int goodsId, float amount)
	{
		if (goodsId < 0 || amount <= 0f)
		{
			return false;
		}
		float stock = GetStock(goodsId);
		if (stock + 0.0001f < amount)
		{
			return false;
		}
		inventory[goodsId] = stock - amount;
		if (inventory[goodsId] <= 1E-05f)
		{
			inventory.Remove(goodsId);
		}
		return true;
	}

	public float GetPrice(int goodsId, float baseValue)
	{
		float valueOrDefault = supply.GetValueOrDefault(goodsId, 0f);
		float valueOrDefault2 = demand.GetValueOrDefault(goodsId, 0f);
		float num = 0f;
		foreach (KeyValuePair<int, float> item in supply)
		{
			if (item.Key != goodsId)
			{
				num += item.Value * 0.5f;
			}
		}
		float num2 = DerivedEconomicProperties.Scarcity(valueOrDefault, valueOrDefault2, num);
		float num3 = valueOrDefault2 / (valueOrDefault + 1f);
		return Math.Max(0.01f, baseValue * (1f + num2 + 0.5f * num3));
	}
}
