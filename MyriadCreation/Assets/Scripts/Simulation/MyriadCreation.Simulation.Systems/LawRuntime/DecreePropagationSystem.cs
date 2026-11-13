using System;
using System.Collections.Generic;
using MyriadCreation.Core.Data;
using MyriadCreation.Simulation.State;
using MyriadCreation.Simulation.Systems;
using MyriadCreation.World.Settlement;
using MyriadCreation.Content.Definitions;
using MyriadCreation.Simulation.Laws;

namespace MyriadCreation.Simulation.LawHost
{
public static class DecreePropagationSystem
{
	private static int _nextOrderId = 1;

	public static List<DecreeOrder> IssueDownward(string decreeId, int realmId, IReadOnlyList<AdminDivision> divisions, int currentDay, int mapWidth, int originDivisionId = -1, Func<int, int> divisionCenterTile = null)
	{
		List<DecreeOrder> list = new List<DecreeOrder>();
		if (divisions == null || divisions.Count == 0)
		{
			return list;
		}
		Dictionary<int, AdminDivision> dictionary = new Dictionary<int, AdminDivision>();
		foreach (AdminDivision division in divisions)
		{
			dictionary[division.divisionId] = division;
		}
		foreach (AdminDivision division2 in divisions)
		{
			if ((originDivisionId >= 0 && division2.divisionId != originDivisionId) || (originDivisionId < 0 && division2.parentDivisionId >= 0))
			{
				continue;
			}
			foreach (int childId in division2.childIds)
			{
				if (dictionary.TryGetValue(childId, out var value))
				{
					DecreeOrder item = new DecreeOrder
					{
						orderId = _nextOrderId++,
						decreeId = decreeId,
						realmId = realmId,
						fromDivisionId = division2.divisionId,
						toDivisionId = value.divisionId,
						officialCharacterId = value.holderCharacterId,
						targetTile = (divisionCenterTile?.Invoke(value.divisionId) ?? (-1)),
						issuedDay = currentDay,
						totalTravelDays = TravelDays(division2, value, mapWidth),
						state = DecreeOrderState.Pending
					};
					list.Add(item);
				}
			}
		}
		return list;
	}

	public static List<DecreeOrder> IssueToSubordinateSettlements(string decreeId, int realmId, IReadOnlyDictionary<int, SettlementData> settlements, TileData[] tiles, int currentDay, Func<int, int> tileOfAnchor = null, Func<int, int> officialForAnchor = null, int originAnchorId = -1)
	{
		List<DecreeOrder> list = new List<DecreeOrder>();
		if (settlements == null || settlements.Count == 0)
		{
			return list;
		}
		foreach (KeyValuePair<int, SettlementData> settlement in settlements)
		{
			SettlementData value = settlement.Value;
			if ((originAnchorId >= 0 && value.anchorId != originAnchorId) || (originAnchorId < 0 && value.controllerAnchorId >= 0) || value.tileIndex < 0 || value.tileIndex >= tiles.Length || tiles[value.tileIndex].ownerRealmId != realmId)
			{
				continue;
			}
			foreach (int controlledAnchorId in value.controlledAnchorIds)
			{
				if (settlements.TryGetValue(controlledAnchorId, out var value2) && value2.anchorId != value.anchorId)
				{
					int num = tileOfAnchor?.Invoke(value2.anchorId) ?? value2.tileIndex;
					if (num >= 0)
					{
						list.Add(new DecreeOrder
						{
							orderId = _nextOrderId++,
							decreeId = decreeId,
							realmId = realmId,
							fromAnchorId = value.anchorId,
							targetAnchorId = value2.anchorId,
							targetTile = num,
							officialCharacterId = (officialForAnchor?.Invoke(value2.anchorId) ?? (-1)),
							issuedDay = currentDay,
							state = DecreeOrderState.Pending
						});
					}
				}
			}
		}
		return list;
	}

	public static int TravelDays(AdminDivision from, AdminDivision to, int mapWidth)
	{
		if (from?.tiles == null || to?.tiles == null || from.tiles.Count == 0 || to.tiles.Count == 0)
		{
			return 1;
		}
		(int x, int y) tuple = Center(from, mapWidth);
		(int, int) tuple2 = Center(to, mapWidth);
		int num = Math.Abs(tuple.x - tuple2.Item1);
		int num2 = Math.Abs(tuple.y - tuple2.Item2);
		float num3 = (float)Math.Sqrt(num * num + num2 * num2);
		return Math.Max(1, (int)Math.Ceiling(num3 / 3f));
	}

	private static (int x, int y) Center(AdminDivision division, int mapWidth)
	{
		if (mapWidth <= 0)
		{
			mapWidth = 1;
		}
		long num = 0L;
		long num2 = 0L;
		int num3 = 0;
		foreach (int tile in division.tiles)
		{
			num += tile % mapWidth;
			num2 += tile / mapWidth;
			num3++;
		}
		if (num3 != 0)
		{
			return (x: (int)(num / num3), y: (int)(num2 / num3));
		}
		return (x: 0, y: 0);
	}

	public static void AttachCourier(DecreeOrder order, int courierActorId)
	{
		if (order != null)
		{
			order.courierActorId = courierActorId;
			if (order.state == DecreeOrderState.Pending)
			{
				order.state = DecreeOrderState.InTransit;
			}
		}
	}

	public static bool CourierArrived(DecreeOrder order, int courierTile)
	{
		if (order == null || order.targetTile < 0)
		{
			return false;
		}
		if (courierTile != order.targetTile)
		{
			return false;
		}
		order.state = DecreeOrderState.Delivered;
		return true;
	}

	public static bool Advance(DecreeOrder order, float days)
	{
		if (order == null || order.state == DecreeOrderState.Enforced || order.state == DecreeOrderState.Refused || order.state == DecreeOrderState.Profiteering)
		{
			return false;
		}
		order.progressDays += Math.Max(0f, days);
		if (order.progressDays >= (float)order.totalTravelDays)
		{
			order.progressDays = order.totalTravelDays;
			if (order.state == DecreeOrderState.Pending || order.state == DecreeOrderState.InTransit)
			{
				order.state = DecreeOrderState.Delivered;
			}
			return true;
		}
		order.state = DecreeOrderState.InTransit;
		return false;
	}

	public static DecreeOrderState Resolve(DecreeOrder order, CharacterData official, float legitimacy, float corruption, float localUnrest, Random rng)
	{
		if (order == null)
		{
			return DecreeOrderState.Failed;
		}
		if (rng == null)
		{
			rng = new Random(1);
		}
		if (official == null || !official.isAlive)
		{
			order.state = DecreeOrderState.Refused;
			order.note = "官员空缺/死亡";
			return order.state;
		}
		float ep;
		float es;
		float em;
		float eman;
		float ec;
		float esch;
		float ech;
		official.GetEffectiveStats(out ep, out es, out em, out eman, out ec, out esch, out ech);
		float management = eman;
		float conspiracy = ec;
		float val = 0.45f + management / 200f - corruption * 0.3f - localUnrest * 0.25f + legitimacy * 0.2f;
		float num = (order.executionStrength = Math.Max(0f, Math.Min(1f, (order.compliance = Math.Max(0.05f, Math.Min(0.95f, val))) + (float)(rng.NextDouble() * 0.2 - 0.1))));
		if (num >= 0.75f)
		{
			order.state = DecreeOrderState.Enforced;
		}
		else if (num >= 0.45f)
		{
			order.state = DecreeOrderState.PartiallyEnforced;
		}
		else if (num >= 0.28f)
		{
			order.state = DecreeOrderState.Delayed;
		}
		else if (conspiracy > 60f && rng.NextDouble() < 0.3499999940395355)
		{
			order.state = DecreeOrderState.Profiteering;
		}
		else
		{
			order.state = DecreeOrderState.Refused;
		}
		order.note = order.state switch
		{
			DecreeOrderState.Enforced => "已执行", 
			DecreeOrderState.PartiallyEnforced => $"执行力度不足（{num:P0}）", 
			DecreeOrderState.Delayed => "消极拖延", 
			DecreeOrderState.Profiteering => "借机谋利", 
			_ => "拒绝执行", 
		};
		return order.state;
	}
}
}
