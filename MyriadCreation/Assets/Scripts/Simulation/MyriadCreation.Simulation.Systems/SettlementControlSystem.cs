using System.Collections.Generic;
using MyriadCreation.Core.Data;
using MyriadCreation.Core.Enums;
using MyriadCreation.Simulation.State;
using MyriadCreation.World.Settlement;
using UnityEngine;
using MyriadCreation.Content.Definitions;

namespace MyriadCreation.Simulation.Systems
{
public static class SettlementControlSystem
{
	public const float BaseControlSpeed = 0.5f;

	public const float ControlCompleteThreshold = 100f;

	public const float NoGarrisonDecaySpeed = 1f;

	public const float SuzerainOccupiedDecayMultiplier = 3f;

	public const float StableDecayMultiplier = 0.5f;

	public static void DailyTick(Dictionary<int, SettlementData> settlements, TileData[] tiles, int mapWidth, int mapHeight, Dictionary<int, Army> armies = null, Dictionary<int, MyriadCreation.Content.Definitions.UnitDef> unitDefs = null)
	{
		if (settlements != null && settlements.Count != 0)
		{
			UpdateGarrisonInfo(settlements, tiles, armies, unitDefs);
			ScanControlRelationships(settlements, tiles, mapWidth, mapHeight);
			UpdateControlProgress(settlements, tiles);
			CheckControlCompletion(settlements);
		}
	}

	private static void UpdateGarrisonInfo(Dictionary<int, SettlementData> settlements, TileData[] tiles, Dictionary<int, Army> armies, Dictionary<int, MyriadCreation.Content.Definitions.UnitDef> unitDefs)
	{
		foreach (SettlementData value in settlements.Values)
		{
			value.garrisonQuantity = value.garrison;
			value.garrisonQuality = 50f;
		}
		if (armies == null)
		{
			return;
		}
		foreach (Army value2 in armies.Values)
		{
			if (value2.state == GameEnums.CombatState.Dead)
			{
				continue;
			}
			foreach (SettlementData value3 in settlements.Values)
			{
				if (value3.tileIndex == value2.currentTileIndex && tiles[value3.tileIndex].ownerRealmId == value2.ownerRealmId)
				{
					value3.garrisonQuantity += (int)value2.GetTotalManpower(unitDefs);
					value3.garrisonQuality = Mathf.Max(value3.garrisonQuality, value2.training);
				}
			}
		}
	}

	private static void ScanControlRelationships(Dictionary<int, SettlementData> settlements, TileData[] tiles, int mapWidth, int mapHeight)
	{
		List<SettlementData> list = new List<SettlementData>(settlements.Values);
		list.Sort((SettlementData a, SettlementData b) => b.settlementLevel.CompareTo(a.settlementLevel));
		foreach (SettlementData item in list)
		{
			if (!item.IsMajorSettlement || item.controllerAnchorId >= 0)
			{
				continue;
			}
			int influenceRadius = GetInfluenceRadius(item);
			int num = item.tileIndex % mapWidth;
			int num2 = item.tileIndex / mapWidth;
			for (int num3 = -influenceRadius; num3 <= influenceRadius; num3++)
			{
				for (int num4 = -influenceRadius; num4 <= influenceRadius; num4++)
				{
					if (num4 * num4 + num3 * num3 > influenceRadius * influenceRadius)
					{
						continue;
					}
					int num5 = num + num4;
					int num6 = num2 + num3;
					if (num5 < 0 || num5 >= mapWidth || num6 < 0 || num6 >= mapHeight)
					{
						continue;
					}
					int num7 = num6 * mapWidth + num5;
					foreach (SettlementData value in settlements.Values)
					{
						if (value.anchorId != item.anchorId && value.tileIndex == num7 && value.controllerAnchorId < 0 && tiles[value.tileIndex].ownerRealmId == tiles[item.tileIndex].ownerRealmId && value.settlementLevel < item.settlementLevel && !item.controlProgress.ContainsKey(value.anchorId))
						{
							item.controlProgress[value.anchorId] = 0f;
							if (!item.controlledAnchorIds.Contains(value.anchorId))
							{
								item.controlledAnchorIds.Add(value.anchorId);
							}
						}
					}
				}
			}
		}
	}

	private static void UpdateControlProgress(Dictionary<int, SettlementData> settlements, TileData[] tiles)
	{
		foreach (SettlementData value in settlements.Values)
		{
			if (value.controlledAnchorIds.Count == 0)
			{
				continue;
			}
			bool flag = false;
			foreach (int controlledAnchorId in value.controlledAnchorIds)
			{
				if (!settlements.ContainsKey(controlledAnchorId) || !value.controlProgress.ContainsKey(controlledAnchorId))
				{
					continue;
				}
				SettlementData settlementData = settlements[controlledAnchorId];
				float num = value.controlProgress[controlledAnchorId];
				float num2 = CalculateControlSpeed(value, settlementData, tiles);
				if (flag)
				{
					float num3 = 3f;
					if (settlementData.isControlStable)
					{
						num3 *= 0.5f;
					}
					num = Mathf.Max(0f, num - num3);
				}
				else if (value.garrisonQuantity <= 0)
				{
					float num4 = 1f;
					if (settlementData.isControlStable)
					{
						num4 *= 0.5f;
					}
					num = Mathf.Max(0f, num - num4);
				}
				else
				{
					num = Mathf.Min(100f, num + num2);
				}
				value.controlProgress[controlledAnchorId] = num;
				if (num <= 0f)
				{
					if (settlementData.anchor != null)
					{
						settlementData.anchor.controllerAnchorId = -1;
					}
					if (settlementData.anchor != null)
					{
						settlementData.anchor.isControlStable = false;
					}
					value.controlProgress.Remove(controlledAnchorId);
				}
			}
		}
	}

	private static float CalculateControlSpeed(SettlementData controller, SettlementData target, TileData[] tiles)
	{
		float num = Mathf.Clamp(1f + (float)controller.garrisonQuantity / 500f, 1f, 3f);
		float num2 = 0.5f * num;
		float num3 = 0.7f + controller.garrisonQuality / 100f * 0.8f;
		float num4 = num2 * num3;
		int num5 = controller.settlementLevel - target.settlementLevel;
		float num6 = num4 * (1f + (float)num5 * 0.2f);
		int num7 = ((target.anchor != null) ? target.anchor.influenceTier : 0);
		return num6 * ((num7 >= 3) ? 1.2f : (num7 switch
		{
			1 => 0.45f, 
			2 => 0.8f, 
			_ => 0.15f, 
		}));
	}

	private static void CheckControlCompletion(Dictionary<int, SettlementData> settlements)
	{
		foreach (SettlementData value in settlements.Values)
		{
			foreach (int controlledAnchorId in value.controlledAnchorIds)
			{
				if (settlements.ContainsKey(controlledAnchorId) && value.controlProgress.ContainsKey(controlledAnchorId))
				{
					SettlementData settlementData = settlements[controlledAnchorId];
					int num = ((settlementData.anchor != null) ? settlementData.anchor.influenceTier : 0);
					float num2;
					if (num >= 3)
					{
						num2 = 100f;
					}
					else
					{
						num2 = ((num == 2) ? 150f : 1000f);
					}
					if (value.controlProgress[controlledAnchorId] >= num2 && settlementData.anchor != null)
					{
						settlementData.anchor.controllerAnchorId = value.anchorId;
						settlementData.anchor.isControlStable = true;
					}
				}
			}
		}
	}

	public static int GetInfluenceRadius(SettlementData settlement)
	{
		return Mathf.Clamp((int)settlement.settlementLevel, 1, 5);
	}

	public static SettlementData GetSuzerain(SettlementData settlement, Dictionary<int, SettlementData> settlements)
	{
		if (settlement.controllerAnchorId < 0)
		{
			return null;
		}
		return settlements.GetValueOrDefault(settlement.controllerAnchorId);
	}

	public static float GetTaxSiphonModifier(SettlementData settlement, Dictionary<int, SettlementData> settlements)
	{
		if (GetSuzerain(settlement, settlements) == null)
		{
			return 1f;
		}
		return 0.9f;
	}

	public static float GetTradeSiphonModifier(SettlementData settlement, Dictionary<int, SettlementData> settlements)
	{
		if (GetSuzerain(settlement, settlements) == null)
		{
			return 1f;
		}
		return 0.8f;
	}
}
}
