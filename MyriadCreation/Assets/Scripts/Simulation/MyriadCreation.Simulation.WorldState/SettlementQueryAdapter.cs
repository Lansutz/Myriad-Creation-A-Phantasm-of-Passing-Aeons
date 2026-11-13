using System;
using System.Collections.Generic;
using MyriadCreation.Core.Contracts;
using MyriadCreation.Core.Data;
using MyriadCreation.Simulation.State;
using MyriadCreation.World.Settlement;

namespace MyriadCreation.Simulation.WorldState
{
/// <summary>
/// UI 4b 第一批：ISettlementQuery 的 Simulation host 适配器。
/// SettlementData 在 World.Settlement；本适配器只把它/GameWorld 转成 Core DTO，空世界/越界安全。
/// </summary>
public sealed class SettlementQueryAdapter : ISettlementQuery
{
	private readonly GameWorld _world;

	public SettlementQueryAdapter(GameWorld world)
	{
		_world = world;
	}

	public SettlementSnapshotDto GetSettlement(int anchorId)
	{
		if (_world?.settlements == null
			|| !_world.settlements.TryGetValue(anchorId, out SettlementData settlement)
			|| settlement == null)
		{
			return null;
		}
		return ToDto(settlement);
	}

	public IReadOnlyList<SettlementBlockRowDto> GetSettlementBlocks(int anchorId)
	{
		List<SettlementBlockRowDto> list = new List<SettlementBlockRowDto>();
		if (_world?.settlements == null
			|| !_world.settlements.TryGetValue(anchorId, out SettlementData settlement)
			|| settlement?.blocks == null)
		{
			return list;
		}
		foreach (SettlementBlock block in settlement.blocks)
		{
			if (block == null) continue;
			list.Add(new SettlementBlockRowDto
			{
				blockId = block.blockId ?? "",
				areaRatio = block.areaRatio,
				districtType = (int)block.districtType,
				kind = (int)block.kind,
				isWall = block.kind == SettlementBlockKind.Wall,
				wallLayerCount = block.wallLayerCount,
				enabled = block.enabled
			});
		}
		return list;
	}

	public IReadOnlyList<SettlementSnapshotDto> GetSettlementsInRealm(int realmId)
	{
		List<SettlementSnapshotDto> list = new List<SettlementSnapshotDto>();
		if (_world?.settlements != null)
		{
			foreach (SettlementData settlement in _world.settlements.Values)
			{
				if (settlement == null)
				{
					continue;
				}
				SettlementSnapshotDto dto = ToDto(settlement);
				if (dto != null && dto.realmId == realmId)
				{
					list.Add(dto);
				}
			}
		}
		list.Sort((a, b) => a.anchorId.CompareTo(b.anchorId));
		return list;
	}

	public IReadOnlyList<SettlementSnapshotDto> GetSettlementsNearTile(int tileIndex, int radius)
	{
		List<SettlementSnapshotDto> list = new List<SettlementSnapshotDto>();
		if (_world?.settlements == null || _world.mapWidth <= 0 || _world.mapHeight <= 0
			|| tileIndex < 0 || tileIndex >= _world.mapWidth * _world.mapHeight || radius < 0)
		{
			return list;
		}
		int tx = tileIndex % _world.mapWidth;
		int ty = tileIndex / _world.mapWidth;
		foreach (SettlementData settlement in _world.settlements.Values)
		{
			if (settlement == null)
			{
				continue;
			}
			int settlementTile = settlement.tileIndex;
			if (settlementTile < 0 || settlementTile >= _world.mapWidth * _world.mapHeight)
			{
				continue;
			}
			int sx = settlementTile % _world.mapWidth;
			int sy = settlementTile / _world.mapWidth;
			if (Math.Abs(sx - tx) + Math.Abs(sy - ty) <= radius)
			{
				list.Add(ToDto(settlement));
			}
		}
		list.Sort((a, b) => a.anchorId.CompareTo(b.anchorId));
		return list;
	}

	private SettlementSnapshotDto ToDto(SettlementData settlement)
	{
		int tileIndex = settlement.tileIndex;
		int realmId = -1;
		int cultureId = -1;
		int faithId = -1;
		if (_world?.tiles != null && tileIndex >= 0 && tileIndex < _world.tiles.Length)
		{
			TileData tile = _world.tiles[tileIndex];
			realmId = tile.ownerRealmId;
			GetDominantCultureFaith(tile, out cultureId, out faithId);
		}

		// 地块人口构成缺失时，回退政权主文化/国教；仍无则 -1。
		if ((cultureId < 0 || faithId < 0) && realmId >= 0 && _world?.realms != null
			&& _world.realms.TryGetValue(realmId, out RealmData realm) && realm != null)
		{
			if (cultureId < 0) cultureId = realm.primaryCultureId;
			if (faithId < 0) faithId = realm.stateReligionId;
		}

		return new SettlementSnapshotDto
		{
			anchorId = settlement.anchorId,
			settlementName = settlement.settlementName ?? "",
			tileIndex = tileIndex,
			population = settlement.population,
			wealth = settlement.wealth,
			garrison = settlement.garrison,
			garrisonQuality = settlement.garrisonQuality,
			garrisonQuantity = settlement.garrisonQuantity,
			constructionTier = settlement.constructionTier,
			siteMode = (int)settlement.siteMode,
			settlementForm = (int)settlement.settlementForm,
			settlementLevel = (int)settlement.settlementLevel,
			isCapital = settlement.isCapital,
			isPort = settlement.isPort,
			isRuined = settlement.IsRuined,
			majorityCultureId = cultureId,
			stateReligionId = faithId,
			realmId = realmId
		};
	}

	private static void GetDominantCultureFaith(TileData tile, out int cultureId, out int faithId)
	{
		cultureId = -1;
		faithId = -1;
		if (tile.populationBlocks == null)
		{
			return;
		}
		Dictionary<int, float> cultureCounts = new Dictionary<int, float>();
		Dictionary<int, float> faithCounts = new Dictionary<int, float>();
		foreach (PopulationBlock block in tile.populationBlocks)
		{
			if (block.cultureId >= 0)
			{
				cultureCounts.TryGetValue(block.cultureId, out float c);
				cultureCounts[block.cultureId] = c + block.count;
			}
			if (block.faithId >= 0)
			{
				faithCounts.TryGetValue(block.faithId, out float f);
				faithCounts[block.faithId] = f + block.count;
			}
		}
		cultureId = GetTopId(cultureCounts);
		faithId = GetTopId(faithCounts);
	}

	private static int GetTopId(Dictionary<int, float> counts)
	{
		int topId = -1;
		float topCount = 0f;
		foreach (var pair in counts)
		{
			if (topId < 0 || pair.Value > topCount)
			{
				topId = pair.Key;
				topCount = pair.Value;
			}
		}
		return topId;
	}
}
}
