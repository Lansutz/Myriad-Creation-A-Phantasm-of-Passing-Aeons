using System;
using System.Collections.Generic;
using MyriadCreation.Core.Data;
using UnityEngine;
using MyriadCreation.Content.Definitions;

namespace MyriadCreation.Simulation.State
{
public class TradeCenter
{
	public int regionId;

	public string centerName;

	public int centerTileIndex;

	[NonSerialized]
	public Dictionary<int, float> inventory = new Dictionary<int, float>();

	public float inventoryCapacity = 10000f;

	public List<TradeRoute> tradeRoutes = new List<TradeRoute>();

	[NonSerialized]
	public Dictionary<int, float> localDemand = new Dictionary<int, float>();

	[NonSerialized]
	public Dictionary<int, float> localSupply = new Dictionary<int, float>();

	public float GetGoodsPrice(int goodsId, Dictionary<int, GoodsDef> goodsDefs)
	{
		if (!goodsDefs.TryGetValue(goodsId, out var value))
		{
			return 1f;
		}
		float valueOrDefault = inventory.GetValueOrDefault(goodsId, 0f);
		float valueOrDefault2 = localDemand.GetValueOrDefault(goodsId, 0f);
		float valueOrDefault3 = localSupply.GetValueOrDefault(goodsId, 0f);
		float num = ((valueOrDefault3 > 0f) ? (valueOrDefault2 / Mathf.Max(0.01f, valueOrDefault3)) : 2f);
		num = Mathf.Clamp(num, 0.2f, 5f);
		float num2 = Mathf.Clamp(1.5f - valueOrDefault / Mathf.Max(1f, inventoryCapacity), 0.3f, 1.5f);
		return value.baseValue * num * num2;
	}

	/// <summary>整单入库判定容差：实际入库量与申请量相差不超过该值即视为整单成功。</summary>
	public const float AcceptedEpsilon = 0.001f;

	/// <summary>
	/// 把 goodsId 的 amount 入库，返回**实际入库量**（容量不足时返回剩余容量，即部分入账）。
	/// 调用方用返回值结算，超出的部分由调用方决定去向，不再出现"人以为失败了、货其实已部分进库"的歧义。
	/// </summary>
	public float AddGoodsReturningAccepted(int goodsId, float amount)
	{
		if (goodsId < 0 || amount <= 0f)
		{
			return 0f;
		}
		float capacityLeft = Mathf.Max(0f, inventoryCapacity - GetTotalInventory());
		float accepted = Mathf.Min(amount, capacityLeft);
		if (accepted <= 0f)
		{
			return 0f;
		}
		inventory[goodsId] = inventory.GetValueOrDefault(goodsId, 0f) + accepted;
		return accepted;
	}

	/// <summary>
	/// 向后兼容的布尔版本：只有整单入库才返回 true；部分入库返回 false 但已收下的部分仍在库里
	/// （与旧实现一致），需要区分"部分入库 / 完全拒绝"时请用 AddGoodsReturningAccepted。
	/// </summary>
	public bool AddGoods(int goodsId, float amount)
	{
		if (amount <= 0f)
		{
			return amount >= 0f;
		}
		return AddGoodsReturningAccepted(goodsId, amount) >= amount - AcceptedEpsilon;
	}

	public bool RemoveGoods(int goodsId, float amount)
	{
		float valueOrDefault = inventory.GetValueOrDefault(goodsId, 0f);
		if (valueOrDefault < amount)
		{
			return false;
		}
		inventory[goodsId] = valueOrDefault - amount;
		return true;
	}

	public float GetTotalInventory()
	{
		float num = 0f;
		foreach (KeyValuePair<int, float> item in inventory)
		{
			num += item.Value;
		}
		return num;
	}

	public void UpdateSupplyDemand(TileData[] tiles, Dictionary<int, GoodsDef> goodsDefs, int regionTileStart, int regionTileEnd)
	{
		localSupply.Clear();
		localDemand.Clear();
		for (int i = regionTileStart; i < regionTileEnd && i < tiles.Length; i++)
		{
			if (tiles[i].isLand)
			{
				float num = tiles[i].fertility * tiles[i].development * 0.5f;
				AddToDict(localSupply, 0, num);
				AddToDict(localSupply, 10, num * 0.8f);
				float regionPopulation = GetRegionPopulation(tiles, i);
				AddToDict(localDemand, 0, regionPopulation * 0.01f);
				AddToDict(localDemand, 3, regionPopulation * 0.002f);
				if (tiles[i].buildingLevels[1] > 0)
				{
					AddToDict(localSupply, 70, (float)tiles[i].buildingLevels[1] * 0.1f);
					AddToDict(localSupply, 31, (float)tiles[i].buildingLevels[1] * 0.2f);
				}
			}
		}
	}

	private float GetRegionPopulation(TileData[] tiles, int tileIndex)
	{
		float num = 0f;
		if (tiles[tileIndex].populationBlocks != null)
		{
			foreach (PopulationBlock populationBlock in tiles[tileIndex].populationBlocks)
			{
				num += populationBlock.count;
			}
		}
		return num;
	}

	private void AddToDict(Dictionary<int, float> dict, int key, float value)
	{
		dict[key] = dict.GetValueOrDefault(key, 0f) + value;
	}
}
}
