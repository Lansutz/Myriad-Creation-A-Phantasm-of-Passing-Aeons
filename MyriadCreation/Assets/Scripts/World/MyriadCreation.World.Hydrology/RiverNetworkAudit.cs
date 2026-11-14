using System.Collections.Generic;
using MyriadCreation.Core.Data;

namespace MyriadCreation.World.Hydrology
{
/// <summary>
/// 河网连通性审计：每条河段都应能沿河道走到海洋或内陆湖。
/// 断头河要么是内陆盆地（有湖），要么是生成错误；内容/世界校验用它报问题。
/// </summary>
public static class RiverNetworkAudit
{
	/// <summary>返回无法通过河道走到水体的河段数量。</summary>
	public static int CountRiverTilesWithoutOutlet(TileData[] tiles, int width, int height)
	{
		if (tiles == null || width <= 0 || height <= 0) return 0;
		int count = width * height;
		if (tiles.Length < count) count = tiles.Length;
		var visited = new bool[count];
		var queue = new Queue<int>();

		for (int i = 0; i < count; i++)
		{
			if (!tiles[i].isRiver) continue;
			if (!HasWaterNeighbor(tiles, width, height, i)) continue;
			visited[i] = true;
			queue.Enqueue(i);
		}

		while (queue.Count > 0)
		{
			int index = queue.Dequeue();
			int x = index % width;
			int y = index / width;
			Visit(tiles, visited, queue, width, height, x + 1, y);
			Visit(tiles, visited, queue, width, height, x - 1, y);
			Visit(tiles, visited, queue, width, height, x, y + 1);
			Visit(tiles, visited, queue, width, height, x, y - 1);
		}

		int orphan = 0;
		for (int i = 0; i < count; i++)
		{
			if (tiles[i].isRiver && !visited[i]) orphan++;
		}
		return orphan;
	}

	private static bool HasWaterNeighbor(TileData[] tiles, int width, int height, int index)
	{
		int x = index % width;
		int y = index / width;
		return IsWater(tiles, width, height, x + 1, y)
			|| IsWater(tiles, width, height, x - 1, y)
			|| IsWater(tiles, width, height, x, y + 1)
			|| IsWater(tiles, width, height, x, y - 1);
	}

	private static void Visit(TileData[] tiles, bool[] visited, Queue<int> queue, int width, int height, int x, int y)
	{
		if (!InBounds(x, y, width, height)) return;
		int index = y * width + x;
		if (visited[index] || !tiles[index].isRiver) return;
		visited[index] = true;
		queue.Enqueue(index);
	}

	private static bool IsWater(TileData[] tiles, int width, int height, int x, int y)
	{
		if (!InBounds(x, y, width, height)) return false;
		int index = y * width + x;
		return !tiles[index].isLand;
	}

	private static bool InBounds(int x, int y, int width, int height)
	{
		return x >= 0 && x < width && y >= 0 && y < height;
	}
}
}
