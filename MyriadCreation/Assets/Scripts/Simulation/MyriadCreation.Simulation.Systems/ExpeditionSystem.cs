using System;
using System.Collections.Generic;
using MyriadCreation.Core.Data;
using MyriadCreation.Core.Enums;
using MyriadCreation.Simulation.State;

namespace MyriadCreation.Simulation.Systems
{
/// <summary>
/// 远征运行（P4.1）：BFS 路线 + 每日推进 + 补给/拦截/知识揭示。
/// 路线只算一次；运行期 O(路线长度)，不重算寻路。
/// </summary>
public static class ExpeditionSystem
{
	public const float SupplyPerDay = 0.2f;

	public const int RevealRadius = 1;

	/// <summary>八向 BFS，只走陆地；找不到路径返回 null。</summary>
	public static List<int> FindRoute(TileData[] tiles, int width, int height, int start, int target)
	{
		if (tiles == null || width <= 0 || height <= 0) return null;
		if (start < 0 || target < 0 || start >= tiles.Length || target >= tiles.Length) return null;
		if (!tiles[start].isLand || !tiles[target].isLand) return null;
		int[] previous = new int[tiles.Length];
		for (int i = 0; i < previous.Length; i++) previous[i] = -1;
		bool[] visited = new bool[tiles.Length];
		Queue<int> queue = new Queue<int>();
		visited[start] = true;
		queue.Enqueue(start);
		while (queue.Count > 0)
		{
			int current = queue.Dequeue();
			if (current == target) break;
			int cx = current % width;
			int cy = current / width;
			for (int dy = -1; dy <= 1; dy++)
			{
				for (int dx = -1; dx <= 1; dx++)
				{
					if (dx == 0 && dy == 0) continue;
					int nx = cx + dx;
					int ny = cy + dy;
					if (nx < 0 || ny < 0 || nx >= width || ny >= height) continue;
					int next = ny * width + nx;
					if (visited[next] || !tiles[next].isLand) continue;
					visited[next] = true;
					previous[next] = current;
					queue.Enqueue(next);
				}
			}
		}
		if (!visited[target]) return null;
		List<int> route = new List<int>();
		int cursor = target;
		while (cursor >= 0)
		{
			route.Add(cursor);
			if (cursor == start) break;
			cursor = previous[cursor];
		}
		route.Reverse();
		return route;
	}

	/// <summary>每日推进：补给、移动、知识揭示、拦截判定。返回是否发生状态变化。</summary>
	public static bool Tick(Expedition expedition, TileData[] tiles, int width, int height,
		MapKnowledge knowledge, Dictionary<int, Army> armies, Chronicle chronicle)
	{
		if (expedition == null || expedition.IsTerminal) return false;
		if (expedition.route == null || expedition.route.Count == 0)
		{
			Fail(expedition, "no_route", chronicle);
			return true;
		}
		expedition.supplies = Math.Max(0f, expedition.supplies - SupplyPerDay);
		if (expedition.supplies <= 0f)
		{
			Fail(expedition, "supplies", chronicle);
			return true;
		}
		if (expedition.routeCursor < expedition.route.Count)
		{
			expedition.currentTileIndex = expedition.route[expedition.routeCursor];
			expedition.routeCursor++;
			knowledge?.RevealRadius(expedition.ownerRealmId, expedition.currentTileIndex, RevealRadius, width, height);
			if (HasEnemyArmy(expedition, armies))
			{
				Fail(expedition, "intercepted", chronicle);
				return true;
			}
		}
		if (expedition.routeCursor >= expedition.route.Count)
		{
			expedition.state = ExpeditionState.Completed;
			chronicle?.Add("expedition", "远征队抵达目标并返回", false, expedition.ownerRealmId);
			return true;
		}
		return true;
	}

	private static bool HasEnemyArmy(Expedition expedition, Dictionary<int, Army> armies)
	{
		if (armies == null || expedition.currentTileIndex < 0) return false;
		foreach (Army army in armies.Values)
		{
			if (army != null && army.state != GameEnums.CombatState.Dead
				&& army.ownerRealmId != expedition.ownerRealmId
				&& army.currentTileIndex == expedition.currentTileIndex)
			{
				return true;
			}
		}
		return false;
	}

	private static void Fail(Expedition expedition, string reason, Chronicle chronicle)
	{
		expedition.state = ExpeditionState.Failed;
		expedition.failReason = reason ?? "";
		chronicle?.Add("expedition_failed", "远征失败：" + reason, false, expedition.ownerRealmId);
	}
}
}
