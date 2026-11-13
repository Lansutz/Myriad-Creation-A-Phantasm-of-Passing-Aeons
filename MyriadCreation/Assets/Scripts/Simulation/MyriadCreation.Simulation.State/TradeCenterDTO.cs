using System;
using System.Collections.Generic;
using MyriadCreation.Core.Dto;

namespace MyriadCreation.Simulation.State
{
[Serializable]
public class TradeCenterDTO
{
	public int regionId;

	public string centerName;

	public int centerTileIndex;

	public List<IntFloatEntry> inventory = new List<IntFloatEntry>();

	public float inventoryCapacity;

	public List<TradeRoute> tradeRoutes = new List<TradeRoute>();

	public List<IntFloatEntry> localDemand = new List<IntFloatEntry>();

	public List<IntFloatEntry> localSupply = new List<IntFloatEntry>();

	public static TradeCenterDTO FromTradeCenter(TradeCenter tc)
	{
		TradeCenterDTO tradeCenterDTO = new TradeCenterDTO
		{
			regionId = tc.regionId,
			centerName = tc.centerName,
			centerTileIndex = tc.centerTileIndex,
			inventoryCapacity = tc.inventoryCapacity,
			tradeRoutes = tc.tradeRoutes
		};
		if (tc.inventory != null)
		{
			foreach (KeyValuePair<int, float> item in tc.inventory)
			{
				tradeCenterDTO.inventory.Add(new IntFloatEntry(item.Key, item.Value));
			}
		}
		if (tc.localDemand != null)
		{
			foreach (KeyValuePair<int, float> item2 in tc.localDemand)
			{
				tradeCenterDTO.localDemand.Add(new IntFloatEntry(item2.Key, item2.Value));
			}
		}
		if (tc.localSupply != null)
		{
			foreach (KeyValuePair<int, float> item3 in tc.localSupply)
			{
				tradeCenterDTO.localSupply.Add(new IntFloatEntry(item3.Key, item3.Value));
			}
		}
		return tradeCenterDTO;
	}

	public TradeCenter ToTradeCenter()
	{
		TradeCenter tradeCenter = new TradeCenter
		{
			regionId = regionId,
			centerName = centerName,
			centerTileIndex = centerTileIndex,
			inventoryCapacity = inventoryCapacity,
			tradeRoutes = tradeRoutes
		};
		foreach (IntFloatEntry item in inventory)
		{
			tradeCenter.inventory[item.key] = item.value;
		}
		foreach (IntFloatEntry item2 in localDemand)
		{
			tradeCenter.localDemand[item2.key] = item2.value;
		}
		foreach (IntFloatEntry item3 in localSupply)
		{
			tradeCenter.localSupply[item3.key] = item3.value;
		}
		return tradeCenter;
	}
}
}
