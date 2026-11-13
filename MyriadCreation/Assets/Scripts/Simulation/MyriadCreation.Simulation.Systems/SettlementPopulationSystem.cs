using System.Collections.Generic;
using MyriadCreation.Core.Data;
using MyriadCreation.Core.Enums;
using MyriadCreation.Simulation.State;
using MyriadCreation.Simulation.WorldState;
using MyriadCreation.World.Settlement;
using UnityEngine;

namespace MyriadCreation.Simulation.Systems
{
/// <summary>
/// D40 人口单源：`TileData.populationBlocks` 是唯一权威，聚落人口只是聚合快照。
/// 聚合范围：聚落自身地块 + 受控锚点对应聚落的地块，按地块索引去重；不递归展开从属聚落。
/// </summary>
public static class SettlementPopulationSystem
{
	public static void RecomputeAll(GameWorld world)
	{
		if (world?.settlements == null)
		{
			return;
		}
		foreach (SettlementData settlement in world.settlements.Values)
		{
			Recompute(world, settlement);
		}
	}

	public static float Recompute(GameWorld world, SettlementData settlement)
	{
		if (world?.tiles == null || settlement == null)
		{
			return 0f;
		}
		float total = 0f;
		foreach (int tileIndex in CollectTiles(world, settlement))
		{
			total += GetTilePopulation(world.tiles, tileIndex);
		}
		settlement.derivedPopulation = total;
		return total;
	}

	/// <summary>聚落聚合范围：自身地块 + 受控锚点地块（去重）。</summary>
	public static List<int> CollectTiles(GameWorld world, SettlementData settlement)
	{
		List<int> result = new List<int>();
		if (world?.tiles == null || settlement == null)
		{
			return result;
		}
		HashSet<int> seen = new HashSet<int>();
		AddTile(settlement.tileIndex, seen, result);
		List<int> controlled = settlement.controlledAnchorIds;
		if (controlled != null && world.settlements != null)
		{
			foreach (int anchorId in controlled)
			{
				if (world.settlements.TryGetValue(anchorId, out SettlementData other) && other != null)
				{
					AddTile(other.tileIndex, seen, result);
				}
			}
		}
		return result;
	}

	/// <summary>
	/// 伤亡落到人口块：按聚合人口比例削减各块，而不是改聚落标量。
	/// 没有人口块时快照清零，不回落旧标量。
	/// </summary>
	public static void ApplyCasualties(GameWorld world, SettlementData settlement, float casualties)
	{
		if (world?.tiles == null || settlement == null || casualties <= 0f)
		{
			return;
		}
		List<int> tiles = CollectTiles(world, settlement);
		float total = 0f;
		foreach (int tileIndex in tiles)
		{
			total += GetTilePopulation(world.tiles, tileIndex);
		}
		if (total <= 0f)
		{
			settlement.derivedPopulation = 0f;
			return;
		}
		float share = Mathf.Clamp01(casualties / total);
		foreach (int tileIndex in tiles)
		{
			TileData tile = world.tiles[tileIndex];
			if (tile.populationBlocks == null)
			{
				continue;
			}
			for (int i = 0; i < tile.populationBlocks.Count; i++)
			{
				PopulationBlock block = tile.populationBlocks[i];
				block.count = Mathf.Max(0f, block.count * (1f - share));
				tile.populationBlocks[i] = block;
			}
			world.tiles[tileIndex] = tile;
		}
		Recompute(world, settlement);
	}

	/// <summary>单地块版伤亡（无 GameWorld 的调用方，如踩踏）：按该地块人口比例削块，返回剩余人口。</summary>
	public static float ApplyCasualtiesToTile(TileData[] tiles, int tileIndex, float casualties)
	{
		if (tiles == null || tileIndex < 0 || tileIndex >= tiles.Length || casualties <= 0f)
		{
			return GetTilePopulation(tiles, tileIndex);
		}
		TileData tile = tiles[tileIndex];
		float total = GetTilePopulation(tiles, tileIndex);
		if (total <= 0f || tile.populationBlocks == null)
		{
			return 0f;
		}
		float share = Mathf.Clamp01(casualties / total);
		for (int i = 0; i < tile.populationBlocks.Count; i++)
		{
			PopulationBlock block = tile.populationBlocks[i];
			block.count = Mathf.Max(0f, block.count * (1f - share));
			tile.populationBlocks[i] = block;
		}
		tiles[tileIndex] = tile;
		return total - Mathf.Min(casualties, total);
	}

	/// <summary>生成/营地转聚落时若地块没有人口块，用初始人口补一个块（生成期初始值入口）。</summary>
	public static void SeedIfEmpty(GameWorld world, SettlementData settlement, float initialPopulation)
	{
		if (world?.tiles == null || settlement == null || initialPopulation <= 0f)
		{
			return;
		}
		int tileIndex = settlement.tileIndex;
		if (tileIndex < 0 || tileIndex >= world.tiles.Length)
		{
			return;
		}
		TileData tile = world.tiles[tileIndex];
		if (tile.populationBlocks != null && tile.populationBlocks.Count > 0 && GetTilePopulation(world.tiles, tileIndex) > 0f)
		{
			Recompute(world, settlement);
			return;
		}
		AddPopulation(world, settlement, initialPopulation);
	}

	/// <summary>向聚落所在地块追加一个人口块（营地转聚落等生成期转移）。</summary>
	public static void AddPopulation(GameWorld world, SettlementData settlement, float amount)
	{
		if (world?.tiles == null || settlement == null || amount <= 0f)
		{
			return;
		}
		int tileIndex = settlement.tileIndex;
		if (tileIndex < 0 || tileIndex >= world.tiles.Length)
		{
			return;
		}
		TileData tile = world.tiles[tileIndex];
		int cultureId = -1;
		int faithId = -1;
		if (world.realms != null && world.realms.TryGetValue(tile.ownerRealmId, out RealmData realm) && realm != null)
		{
			cultureId = realm.primaryCultureId;
			faithId = realm.stateReligionId;
		}
		tile.populationBlocks ??= new List<PopulationBlock>();
		tile.populationBlocks.Add(new PopulationBlock
		{
			count = amount,
			raceId = 0,
			cultureId = cultureId,
			faithId = faithId,
			socialClass = GameEnums.SocialClass.Peasant,
			satisfaction = 60f
		});
		world.tiles[tileIndex] = tile;
		Recompute(world, settlement);
	}

	private static void AddTile(int tileIndex, HashSet<int> seen, List<int> result)
	{
		if (tileIndex >= 0 && seen.Add(tileIndex))
		{
			result.Add(tileIndex);
		}
	}

	private static float GetTilePopulation(TileData[] tiles, int tileIndex)
	{
		if (tiles == null || tileIndex < 0 || tileIndex >= tiles.Length || tiles[tileIndex].populationBlocks == null)
		{
			return 0f;
		}
		float total = 0f;
		foreach (PopulationBlock block in tiles[tileIndex].populationBlocks)
		{
			total += block.count;
		}
		return total;
	}
}
}
