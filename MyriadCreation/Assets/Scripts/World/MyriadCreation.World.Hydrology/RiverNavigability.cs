using MyriadCreation.Core.Data;
using MyriadCreation.Core.Enums;

namespace MyriadCreation.World.Hydrology
{
/// <summary>水体类型：海洋、海岸与河道都是水路，只是可通行条件不同。</summary>
public enum WaterBodyKind
{
	None,
	Ocean,
	Coast,
	River
}

/// <summary>运输方式：陆运、河运、海运。由真实水体连通性判定，不用抽象系数。</summary>
public enum TransportMode
{
	Land,
	River,
	Sea
}

/// <summary>
/// 水体可通行判定的唯一来源：海洋/海岸与可通航河道。单位移动、贸易与区域审计都读这里，
/// 不再各写一套宽度/深度阈值。
/// </summary>
public static class RiverNavigability
{
	public static WaterBodyKind Classify(TileData tile)
	{
		if (!tile.exists) return WaterBodyKind.None;
		if (!tile.isLand)
		{
			return tile.oceanTier == GameEnums.OceanTier.Coast ? WaterBodyKind.Coast : WaterBodyKind.Ocean;
		}
		return tile.isRiver ? WaterBodyKind.River : WaterBodyKind.None;
	}

	/// <summary>船只是否能在这块水体上航行；陆地（非河道）为 false。</summary>
	public static bool IsBoatPassable(TileData tile)
	{
		WaterBodyKind kind = Classify(tile);
		if (kind == WaterBodyKind.Ocean || kind == WaterBodyKind.Coast) return true;
		if (kind == WaterBodyKind.River) return IsNavigable(tile);
		return false;
	}

	public static bool IsNavigable(TileData tile)
	{
		if (!tile.isRiver) return false;
		return IsNavigable(tile.riverStreamOrder, tile.riverWidthMeters, tile.riverDepthMeters,
			tile.elevation01, tile.annualPrecipMm);
	}

	/// <summary>
	/// 两地之间是否存在可通航水道：从起点聚落相邻的可通行水体出发，沿水路 BFS 到终点聚落相邻水体。
	/// 起点/终点本身是陆地聚落，所以允许从相邻水体上下船。
	/// </summary>
	public static TransportMode ClassifyRoute(TileData[] tiles, int width, int height, int fromTile, int toTile)
	{
		if (tiles == null || width <= 0 || height <= 0) return TransportMode.Land;
		var visited = new bool[tiles.Length];
		var queue = new System.Collections.Generic.Queue<int>();
		bool sea = false;

		void EnqueueNeighbors(int tile)
		{
			if (tile < 0 || tile >= tiles.Length) return;
			int x = tile % width;
			int y = tile / width;
			int[] dx = { 1, -1, 0, 0 };
			int[] dy = { 0, 0, 1, -1 };
			for (int k = 0; k < 4; k++)
			{
				int nx = x + dx[k];
				int ny = y + dy[k];
				if (nx < 0 || nx >= width || ny < 0 || ny >= height) continue;
				int index = ny * width + nx;
				if (visited[index] || !IsBoatPassable(tiles[index])) continue;
				visited[index] = true;
				if (Classify(tiles[index]) != WaterBodyKind.River) sea = true;
				queue.Enqueue(index);
			}
		}

		EnqueueNeighbors(fromTile);
		var targets = new System.Collections.Generic.HashSet<int>();
		void CollectTargets(int tile)
		{
			if (tile < 0 || tile >= tiles.Length) return;
			int x = tile % width;
			int y = tile / width;
			int[] dx = { 1, -1, 0, 0 };
			int[] dy = { 0, 0, 1, -1 };
			for (int k = 0; k < 4; k++)
			{
				int nx = x + dx[k];
				int ny = y + dy[k];
				if (nx < 0 || nx >= width || ny < 0 || ny >= height) continue;
				int index = ny * width + nx;
				if (IsBoatPassable(tiles[index])) targets.Add(index);
			}
		}
		CollectTargets(toTile);
		if (queue.Count == 0 || targets.Count == 0) return TransportMode.Land;

		while (queue.Count > 0)
		{
			int index = queue.Dequeue();
			if (targets.Contains(index))
			{
				return sea ? TransportMode.Sea : TransportMode.River;
			}
			int x = index % width;
			int y = index / width;
			int[] dx = { 1, -1, 0, 0 };
			int[] dy = { 0, 0, 1, -1 };
			for (int k = 0; k < 4; k++)
			{
				int nx = x + dx[k];
				int ny = y + dy[k];
				if (nx < 0 || nx >= width || ny < 0 || ny >= height) continue;
				int next = ny * width + nx;
				if (visited[next] || !IsBoatPassable(tiles[next])) continue;
				visited[next] = true;
				if (Classify(tiles[next]) != WaterBodyKind.River) sea = true;
				queue.Enqueue(next);
			}
		}

		return TransportMode.Land;
	}

	/// <summary>可通航判定：2 级以上河道，或宽 ≥20m 且深 ≥0.6m；低地多雨区的无测站河道按可通航处理。</summary>
	public static bool IsNavigable(int streamOrder, float widthMeters, float depthMeters, float elevation01, float annualPrecipMm)
	{
		if (streamOrder >= 2) return true;
		if (widthMeters >= 20f && depthMeters >= 0.6f) return true;
		if (streamOrder == 0 && widthMeters <= 0f && elevation01 < 0.5f)
		{
			return annualPrecipMm > 600f;
		}
		return false;
	}
}
}
