using System;
using System.Collections.Generic;
using MyriadCreation.Core.Data;
using MyriadCreation.Core.Enums;
using MyriadCreation.World.Settlement;

namespace MyriadCreation.Simulation.Systems
{
public static class SettlementInfluenceSystem
{
	private struct Node(int tile, float cost)
	{
		public int tile = tile;

		public float cost = cost;
	}

	private sealed class MinHeap
	{
		private readonly List<Node> _items = new List<Node>();

		public int Count => _items.Count;

		public void Push(Node node)
		{
			_items.Add(node);
			int num = _items.Count - 1;
			while (num > 0)
			{
				int num2 = (num - 1) / 2;
				if (!(_items[num2].cost <= _items[num].cost))
				{
					List<Node> items = _items;
					int index = num2;
					List<Node> items2 = _items;
					int index2 = num;
					Node value = _items[num];
					Node value2 = _items[num2];
					items[index] = value;
					items2[index2] = value2;
					num = num2;
					continue;
				}
				break;
			}
		}

		public Node Pop()
		{
			Node result = _items[0];
			int index = _items.Count - 1;
			_items[0] = _items[index];
			_items.RemoveAt(index);
			int num = 0;
			while (true)
			{
				int num2 = num * 2 + 1;
				int num3 = num2 + 1;
				int num4 = num;
				if (num2 < _items.Count && _items[num2].cost < _items[num4].cost)
				{
					num4 = num2;
				}
				if (num3 < _items.Count && _items[num3].cost < _items[num4].cost)
				{
					num4 = num3;
				}
				if (num4 == num)
				{
					break;
				}
				List<Node> items = _items;
				int index2 = num;
				List<Node> items2 = _items;
				int index3 = num4;
				Node value = _items[num4];
				Node value2 = _items[num];
				items[index2] = value;
				items2[index3] = value2;
				num = num4;
			}
			return result;
		}
	}

	public static InfluenceField Calculate(Dictionary<int, SettlementData> settlements, TileData[] tiles, int mapWidth, int mapHeight, InfluenceSettings settings = null)
	{
		if (settings == null)
		{
			settings = InfluenceSettings.Default;
		}
		InfluenceField influenceField = new InfluenceField
		{
			tierByTile = new byte[tiles.Length],
			sourceAnchorByTile = new int[tiles.Length]
		};
		for (int i = 0; i < influenceField.sourceAnchorByTile.Length; i++)
		{
			influenceField.sourceAnchorByTile[i] = -1;
		}
		if (settlements == null || settlements.Count == 0)
		{
			return influenceField;
		}
		List<SettlementData> list = new List<SettlementData>(settlements.Values);
		list.Sort((SettlementData a, SettlementData b) => b.settlementLevel.CompareTo(a.settlementLevel));
		foreach (SettlementData item in list)
		{
			if (!item.IsMajorSettlement || item.tileIndex < 0 || item.tileIndex >= tiles.Length || !tiles[item.tileIndex].exists || !tiles[item.tileIndex].isLand)
			{
				continue;
			}
			float num = ComputeBudget(item, settings);
			float[] array = Dijkstra(item, tiles, mapWidth, mapHeight, num, settings);
			foreach (int controlledAnchorId in item.controlledAnchorIds)
			{
				if (!settlements.TryGetValue(controlledAnchorId, out var value) || value.tileIndex < 0 || value.tileIndex >= array.Length)
				{
					continue;
				}
				float num2 = array[value.tileIndex];
				if (!float.IsInfinity(num2) && !(num2 > num))
				{
					InfluenceTier influenceTier = TierFor(num2 / Math.Max(0.001f, num), settings);
					if (!influenceField.tiersBySettlement.TryGetValue(controlledAnchorId, out var value2) || influenceTier > value2)
					{
						influenceField.tiersBySettlement[controlledAnchorId] = influenceTier;
					}
				}
			}
			for (int num3 = 0; num3 < array.Length; num3++)
			{
				if (!float.IsInfinity(array[num3]) && !(array[num3] > num))
				{
					InfluenceTier influenceTier2 = TierFor(array[num3] / Math.Max(0.001f, num), settings);
					if ((byte)influenceTier2 >= influenceField.tierByTile[num3])
					{
						influenceField.tierByTile[num3] = (byte)influenceTier2;
						influenceField.sourceAnchorByTile[num3] = item.anchorId;
					}
				}
			}
		}
		return influenceField;
	}

	private static InfluenceField _lastField;

	/// <summary>最近一次日结算出的影响力场（只读快照）；查询路径不得重算或写状态（P2.6）。</summary>
	public static InfluenceField LastField => _lastField;

	/// <summary>日结：先算场（纯读），再整批写回 anchor 影响力等级；两步锁步执行。</summary>
	public static void UpdateControlTiers(Dictionary<int, SettlementData> settlements, TileData[] tiles, int mapWidth, int mapHeight, InfluenceSettings settings = null)
	{
		if (settlements == null || settlements.Count == 0)
		{
			_lastField = null;
			return;
		}
		InfluenceField field = Calculate(settlements, tiles, mapWidth, mapHeight, settings);
		Apply(settlements, field);
		_lastField = field;
	}

	/// <summary>Apply 相：只写模拟状态，不重新计算。</summary>
	public static void Apply(Dictionary<int, SettlementData> settlements, InfluenceField field)
	{
		if (settlements == null || field?.tiersBySettlement == null)
		{
			return;
		}
		foreach (KeyValuePair<int, InfluenceTier> pair in field.tiersBySettlement)
		{
			if (settlements.TryGetValue(pair.Key, out SettlementData settlement) && settlement?.anchor != null)
			{
				settlement.anchor.influenceTier = (int)pair.Value;
			}
		}
	}

	public static InfluenceTier GetTierAt(byte tier)
	{
		return (InfluenceTier)Math.Max(0, Math.Min(4, (int)tier));
	}

	private static float ComputeBudget(SettlementData settlement, InfluenceSettings settings)
	{
		float num = Math.Max(0f, settlement.population);
		float val = settings.baseBudget + (float)settlement.settlementLevel * settings.levelBudget + (float)Math.Sqrt(num) * settings.populationBudgetFactor + (float)settlement.garrison * settings.garrisonBudgetFactor;
		return Math.Max(3f, Math.Min(settings.maxBudget, val));
	}

	private static InfluenceTier TierFor(float fraction, InfluenceSettings settings)
	{
		if (fraction <= settings.coreFraction)
		{
			return InfluenceTier.Core;
		}
		if (fraction <= settings.secureFraction)
		{
			return InfluenceTier.Secure;
		}
		if (fraction <= settings.contestedFraction)
		{
			return InfluenceTier.Contested;
		}
		if (fraction <= 1f)
		{
			return InfluenceTier.Periphery;
		}
		return InfluenceTier.None;
	}

	private static float[] Dijkstra(SettlementData source, TileData[] tiles, int width, int height, float budget, InfluenceSettings settings)
	{
		float[] array = new float[tiles.Length];
		for (int i = 0; i < array.Length; i++)
		{
			array[i] = float.PositiveInfinity;
		}
		MinHeap minHeap = new MinHeap();
		int tileIndex = source.tileIndex;
		array[tileIndex] = 0f;
		minHeap.Push(new Node(tileIndex, 0f));
		while (minHeap.Count > 0)
		{
			Node node = minHeap.Pop();
			if (!(node.cost > array[node.tile]))
			{
				if (node.cost > budget)
				{
					break;
				}
				int num = node.tile % width;
				int num2 = node.tile / width;
				Relax(num + 1, num2, node, tiles, array, minHeap, width, height, budget, settings);
				Relax(num - 1, num2, node, tiles, array, minHeap, width, height, budget, settings);
				Relax(num, num2 + 1, node, tiles, array, minHeap, width, height, budget, settings);
				Relax(num, num2 - 1, node, tiles, array, minHeap, width, height, budget, settings);
			}
		}
		return array;
	}

	private static void Relax(int nx, int ny, Node from, TileData[] tiles, float[] costs, MinHeap heap, int width, int height, float budget, InfluenceSettings settings)
	{
		if (nx < 0 || nx >= width || ny < 0 || ny >= height)
		{
			return;
		}
		int num = ny * width + nx;
		if (tiles[num].exists && tiles[num].isLand)
		{
			float num2 = TerrainCost(tiles[num], settings);
			float num3 = from.cost + num2;
			if (!(num3 > budget) && !(num3 >= costs[num]))
			{
				costs[num] = num3;
				heap.Push(new Node(num, num3));
			}
		}
	}

	public static float TerrainCost(TileData tile, InfluenceSettings settings)
	{
		float num = ((tile.movementCost > 0f) ? tile.movementCost : 1f);
		num *= 1f + Math.Max(0f, tile.slopeDegree) * settings.slopeCostFactor;
		if (tile.isRiver && tile.riverDepthMeters >= 0.6f)
		{
			num *= settings.riverCrossingCostMultiplier;
		}
		if (tile.roadLevel > GameEnums.RoadLevel.None)
		{
			num *= settings.roadCostMultiplier;
		}
		return Math.Max(0.15f, num);
	}
}
}
