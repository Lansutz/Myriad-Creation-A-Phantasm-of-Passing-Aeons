using System.Collections.Generic;
using MyriadCreation.Core.Data;
using MyriadCreation.Core.Enums;
using MyriadCreation.Simulation.Culture;
using MyriadCreation.Simulation.State;
using MyriadCreation.Simulation.Systems;
using MyriadCreation.Simulation.WorldState;
using MyriadCreation.World.Settlement;
using UnityEngine;

namespace MyriadCreation.Simulation.Systems
{
/// <summary>
/// 政权自然形成的写入侧（P5.1）：规则判定在 <see cref="StateFormationSystem"/>，
/// 这里只负责把“一个聚落/行为体够格了”变成真实的 RealmData、领土、首领与家族。
/// 不送金库、不送稳定度、不造固定宫廷；一切初值来自当地的真实人口、剩余与聚落状态。
/// </summary>
public static class StateFormationHost
{
	private const int MaxClaimedNeighborTiles = 2;

	public static bool TryFormFromSettlement(GameWorld world, SettlementData settlement)
	{
		if (world == null || settlement == null || settlement.IsRuined) return false;
		if (settlement.tileIndex < 0 || settlement.tileIndex >= world.tiles.Length) return false;
		if (world.tiles[settlement.tileIndex].ownerRealmId >= 0) return false;

		int cultureId = ResolveCultureId(world, settlement);
		if (!TryNextRealmId(world, out int realmId)) return false;

		CultureData culture = cultureId >= 0 && world.cultures.TryGetValue(cultureId, out CultureData value) ? value : null;
		bool nomadic = culture != null && culture.GetSettlementDegree() < 0.4f;
		RealmData realm = new RealmData
		{
			realmId = realmId,
			realmName = settlement.settlementName,
			primaryCultureId = cultureId,
			realmForm = nomadic ? RealmForm.Nomadic : RealmForm.Sedentary,
			// 自然政权从零货币开始：国库不是开局赠送，税收与市场货币化后面才产生。
			treasury = 0f,
			prestige = 0f,
			stability = Mathf.Clamp(settlement.settlementStability, 0f, 100f),
			centralization = CentralizationFor(culture),
			stateReligionId = -1
		};
		world.realms[realmId] = realm;
		foreach (int innovationId in StateFormationSystem.FoundingKnowledge)
		{
			world.Innovations?.GrantInnovation(realmId, innovationId);
		}

		// 领土：聚落地块 + 少量同省无主邻地；不吞并其他聚落。
		List<int> claimed = new List<int>();
		ClaimTile(world, realm, settlement.tileIndex, claimed);
		HashSet<int> settlementTiles = CollectSettlementTiles(world);
		foreach (int neighbor in Neighbors(world, settlement.tileIndex))
		{
			if (claimed.Count > MaxClaimedNeighborTiles) break;
			if (!IsClaimableNeighbor(world, neighbor, settlement, settlementTiles)) continue;
			ClaimTile(world, realm, neighbor, claimed);
		}

		float population = RealmPopulation(world, realm);
		CharacterData leader = world.Characters?.CreateFoundingLeader(
			realm, cultureId, raceId: 0, faithId: -1, world.currentYear, out _);
		if (leader == null)
		{
			Rollback(world, realm, claimed);
			return false;
		}
		realm.prestige = Mathf.Max(0f, leader.prestige);
		world.Characters.SeedFoundingNotables(realm, cultureId, raceId: 0, faithId: -1, world.currentYear, population);

		if (nomadic)
		{
			realm.nomadicRange = new NomadicRange
			{
				centerTile = settlement.tileIndex,
				summerCampTile = settlement.tileIndex,
				winterCampTile = settlement.tileIndex
			};
		}

		if (world.FirstRealmYear < 0) world.FirstRealmYear = world.currentYear;
		world.NotifyRealmCreated();
		world.GetChronicle()?.Add(
			"state_formation",
			$"{settlement.settlementName} 的居民形成政权（{realm.realmForm}，人口约 {Mathf.RoundToInt(population)}）",
			true,
			realmId);
		Debug.Log($"[StateFormation] {realm.realmName} 由聚落#{settlement.anchorId}自然形成（{realm.realmForm}），地块={claimed.Count}，人口={population:F0}");
		return true;
	}

	private static bool TryNextRealmId(GameWorld world, out int realmId)
	{
		realmId = 0;
		foreach (int key in world.realms.Keys)
		{
			if (key >= realmId) realmId = key + 1;
		}
		return realmId < StateFormationSystem.MaxRealms;
	}

	private static int ResolveCultureId(GameWorld world, SettlementData settlement)
	{
		if (settlement.tileIndex < 0 || settlement.tileIndex >= world.tiles.Length) return -1;
		TileData tile = world.tiles[settlement.tileIndex];
		if (tile.populationBlocks == null || tile.populationBlocks.Count == 0) return world.cultures.Count > 0 ? FirstCultureId(world) : -1;
		Dictionary<int, float> counts = new Dictionary<int, float>();
		foreach (PopulationBlock block in tile.populationBlocks)
		{
			if (block.cultureId < 0) continue;
			counts.TryGetValue(block.cultureId, out float current);
			counts[block.cultureId] = current + block.count;
		}
		int best = -1;
		float bestCount = 0f;
		foreach (KeyValuePair<int, float> pair in counts)
		{
			if (pair.Value > bestCount || (pair.Value == bestCount && pair.Key < best))
			{
				best = pair.Key;
				bestCount = pair.Value;
			}
		}
		if (best >= 0) return best;
		return world.cultures.Count > 0 ? FirstCultureId(world) : -1;
	}

	private static int FirstCultureId(GameWorld world)
	{
		int first = int.MaxValue;
		foreach (int key in world.cultures.Keys)
		{
			if (key < first) first = key;
		}
		return first == int.MaxValue ? -1 : first;
	}

	private static float CentralizationFor(CultureData culture)
	{
		// 中枢能力是组织形态的度量，不是加成： band/tribe 几乎无中枢，chiefdom 起才有常设机构。
		if (culture == null) return 0.05f;
		switch (culture.stage)
		{
			case GameEnums.CultureStage.Band: return 0.02f;
			case GameEnums.CultureStage.Tribe: return 0.05f;
			case GameEnums.CultureStage.Chiefdom: return 0.1f;
			case GameEnums.CultureStage.EthnicGroup: return 0.2f;
			default: return 0.3f;
		}
	}

	private static void ClaimTile(GameWorld world, RealmData realm, int tileIndex, List<int> claimed)
	{
		TileData tile = world.tiles[tileIndex];
		tile.ownerRealmId = realm.realmId;
		world.tiles[tileIndex] = tile;
		realm.coreTiles.Add(tileIndex);
		claimed.Add(tileIndex);
	}

	private static bool IsClaimableNeighbor(GameWorld world, int tileIndex, SettlementData origin, HashSet<int> settlementTiles)
	{
		if (tileIndex < 0 || tileIndex >= world.tiles.Length) return false;
		TileData tile = world.tiles[tileIndex];
		if (!tile.exists || !tile.isLand || tile.ownerRealmId >= 0) return false;
		if (settlementTiles.Contains(tileIndex)) return false;
		return tile.provinceId == world.tiles[origin.tileIndex].provinceId;
	}

	private static HashSet<int> CollectSettlementTiles(GameWorld world)
	{
		HashSet<int> result = new HashSet<int>();
		foreach (SettlementData settlement in world.settlements.Values)
		{
			if (settlement != null) result.Add(settlement.tileIndex);
		}
		return result;
	}

	private static IEnumerable<int> Neighbors(GameWorld world, int tileIndex)
	{
		int width = Mathf.Max(1, world.mapWidth);
		int height = Mathf.Max(1, world.mapHeight);
		int x = tileIndex % width;
		int y = tileIndex / width;
		for (int dy = -1; dy <= 1; dy++)
		{
			for (int dx = -1; dx <= 1; dx++)
			{
				if (dx == 0 && dy == 0) continue;
				int nx = x + dx;
				int ny = y + dy;
				if (nx < 0 || nx >= width || ny < 0 || ny >= height) continue;
				yield return ny * width + nx;
			}
		}
	}

	private static float RealmPopulation(GameWorld world, RealmData realm)
	{
		float population = 0f;
		foreach (int tileIndex in realm.coreTiles)
		{
			if (tileIndex >= 0 && tileIndex < world.tiles.Length) population += PopulationLedger.Total(world.tiles[tileIndex]);
		}
		return population;
	}

	private static void Rollback(GameWorld world, RealmData realm, List<int> claimed)
	{
		foreach (int tileIndex in claimed)
		{
			TileData tile = world.tiles[tileIndex];
			if (tile.ownerRealmId == realm.realmId) tile.ownerRealmId = -1;
			world.tiles[tileIndex] = tile;
		}
		world.realms.Remove(realm.realmId);
	}
}
}
