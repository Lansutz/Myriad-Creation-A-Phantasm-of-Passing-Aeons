using MyriadCreation.Core.Data;
using UnityEngine;

namespace MyriadCreation.World.Hydrology
{
/// <summary>
/// 河流改道（avulsion）：低坡度、高汇流的河段在洪水后可能弃用旧道、改走更低的新道。
/// 一次改道会废弃一小段河道，并在数格外开出新道；旧道沿岸的聚落由此失去水源。
/// </summary>
public static class RiverAvulsion
{
	/// <summary>年改道概率：汇流越大、坡度越缓越高；上限 0.08/年，避免全图同时改道。</summary>
	public static float AnnualRisk(TileData tile)
	{
		if (!tile.isRiver || tile.riverStreamOrder < 1) return 0f;
		float slope = Mathf.Max(0.01f, tile.slopeDegree);
		float risk = tile.riverFlowAccumulation / 200f * Mathf.Exp(-slope * 0.15f);
		return Mathf.Clamp(risk, 0f, 0.08f);
	}

	/// <summary>
	/// 每年最多一次改道：按年与种子确定性地遍历候选河段，风险由汇流/坡度决定。
	/// 返回是否有河段改道，并给出旧/新河道地块。
	/// </summary>
	public static bool TryAnnualAvulsion(TileData[] tiles, int width, int height, int seed, int year,
		out int oldChannelTile, out int newChannelTile)
	{
		oldChannelTile = -1;
		newChannelTile = -1;
		if (tiles == null || width <= 0 || height <= 0) return false;
		var rng = new System.Random(seed * 397 ^ year);
		int start = rng.Next(tiles.Length);
		for (int step = 0; step < tiles.Length; step++)
		{
			int index = (start + step) % tiles.Length;
			float risk = AnnualRisk(tiles[index]);
			if (risk <= 0f) continue;
			if (rng.NextDouble() >= risk) continue;
			if (TryAvulse(tiles, width, height, index, out int fresh))
			{
				oldChannelTile = index;
				newChannelTile = fresh;
				return true;
			}
		}
		return false;
	}

	/// <summary>
	/// 在给定河段上触发一次改道：废弃该段与相邻 1–2 段河道，在 2 格外的最低旱地开新道。
	/// 新河道继承部分河宽/河深/流速，河序从 1 开始重新发育。
	/// </summary>
	public static bool TryAvulse(TileData[] tiles, int width, int height, int tileIndex, out int newChannelTile)
	{
		newChannelTile = -1;
		if (tiles == null || width <= 0 || height <= 0) return false;
		if (tileIndex < 0 || tileIndex >= tiles.Length) return false;
		ref TileData old = ref tiles[tileIndex];
		if (!old.isRiver || old.riverStreamOrder < 1) return false;

		float oldWidth = old.riverWidthMeters;
		float oldDepth = old.riverDepthMeters;
		float oldCurrent = old.riverCurrentMps;

		int x = tileIndex % width;
		int y = tileIndex / width;
		int[] dx = { 1, -1, 0, 0 };
		int[] dy = { 0, 0, 1, -1 };

		// 一条河段不是单格断流：相邻的 1–2 段一并废弃，才像真正的改道。
		Dry(ref tiles[tileIndex]);
		int dried = 1;
		for (int k = 0; k < 4 && dried < 3; k++)
		{
			int nx = x + dx[k];
			int ny = y + dy[k];
			if (nx < 0 || nx >= width || ny < 0 || ny >= height) continue;
			int index = ny * width + nx;
			if (tiles[index].isRiver && tiles[index].riverStreamOrder >= 1)
			{
				Dry(ref tiles[index]);
				dried++;
			}
		}

		// 新河道在 2 格外的低地出现，模拟河道整体偏移。
		int fresh = FindLowland(tiles, width, height, x, y, 2);
		if (fresh < 0) fresh = FindLowland(tiles, width, height, x, y, 1);
		if (fresh < 0) return false;

		ref TileData channel = ref tiles[fresh];
		channel.isRiver = true;
		channel.riverStreamOrder = 1;
		channel.riverFlowAccumulation = Mathf.Max(1f, oldWidth);
		channel.riverWidthMeters = Mathf.Max(2f, oldWidth * 0.6f);
		channel.riverDepthMeters = Mathf.Max(0.2f, oldDepth * 0.6f);
		channel.riverCurrentMps = Mathf.Max(0.1f, oldCurrent * 0.8f);
		newChannelTile = fresh;
		return true;
	}

	private static void Dry(ref TileData tile)
	{
		tile.isRiver = false;
		tile.riverStreamOrder = 0;
		tile.riverFlowAccumulation = 0f;
		tile.riverWidthMeters = 0f;
		tile.riverDepthMeters = 0f;
		tile.riverCurrentMps = 0f;
	}

	/// <summary>在切比雪夫距离 radius 的环上找最低的、非河道的可淹陆地。</summary>
	private static int FindLowland(TileData[] tiles, int width, int height, int cx, int cy, int radius)
	{
		int best = -1;
		float bestElevation = float.MaxValue;
		for (int y = cy - radius; y <= cy + radius; y++)
		{
			for (int x = cx - radius; x <= cx + radius; x++)
			{
				if (x < 0 || x >= width || y < 0 || y >= height) continue;
				int dx = Mathf.Abs(x - cx);
				int dy = Mathf.Abs(y - cy);
				if (Mathf.Max(dx, dy) != radius) continue;
				int index = y * width + x;
				TileData candidate = tiles[index];
				if (!candidate.exists || !candidate.isLand || candidate.isRiver) continue;
				if (candidate.elevation01 < bestElevation)
				{
					bestElevation = candidate.elevation01;
					best = index;
				}
			}
		}
		return best;
	}
}
}
