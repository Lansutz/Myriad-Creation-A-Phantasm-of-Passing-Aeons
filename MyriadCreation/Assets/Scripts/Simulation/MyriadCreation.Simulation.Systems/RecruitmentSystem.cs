using System.Collections.Generic;
using MyriadCreation.Content.Definitions;
using MyriadCreation.Core.Data;
using MyriadCreation.Core.Enums;
using MyriadCreation.Simulation.State;
using MyriadCreation.Simulation.WorldState;
using UnityEngine;

namespace MyriadCreation.Simulation.Systems
{
/// <summary>
/// P1.5 征兵：人口（地块人口块）→ 军队；物资按 UnitDef.recruitCost 从本区市场扣；
/// 兵种必须由已完成革新解锁（unlockRefs + requiredInnovations）。
/// </summary>
public static class RecruitmentSystem
{
	private const float MaxRecruitSharePerBlock = 0.1f;

	public static bool TryRecruit(GameWorld world, int realmId, int tileIndex, int unitId, float count, out Army army, out string reason)
	{
		army = null;
		reason = "";
		if (world?.realms == null || !world.realms.TryGetValue(realmId, out RealmData realm) || realm == null)
		{
			reason = "政权不存在";
			return false;
		}
		if (world.unitDefs == null || !world.unitDefs.TryGetValue(unitId, out UnitDef def))
		{
			reason = "兵种不存在";
			return false;
		}
		if (count <= 0f)
		{
			reason = "征召数量必须大于 0";
			return false;
		}
		if (def.requiredInnovations != null && world.Innovations != null)
		{
			foreach (int innovationId in def.requiredInnovations)
			{
				if (innovationId >= 0 && !world.Innovations.HasInnovation(realmId, innovationId))
				{
					reason = "缺少兵种所需革新";
					return false;
				}
			}
		}
		if (world.Innovations != null && !world.Innovations.IsUnlockGranted(realmId, InnovationUnlockKind.Unit, unitId))
		{
			reason = "兵种尚未由革新解锁";
			return false;
		}

		float manpower = count * Mathf.Max(1f, def.manpowerCost);
		if (!TryConsumeManpower(world, realm, manpower))
		{
			reason = "可征召人口不足";
			return false;
		}
		if (!TryConsumeRecruitGoods(world, tileIndex, def.recruitCost, count, out reason))
		{
			return false;
		}

		foreach (Army candidate in world.armies.Values)
		{
			if (candidate != null && candidate.ownerRealmId == realmId && candidate.currentTileIndex == tileIndex && candidate.state != GameEnums.CombatState.Dead)
			{
				army = candidate;
				break;
			}
		}
		if (army == null)
		{
			army = world.CreateArmy(realmId, -1, tileIndex);
		}
		army.unitCounts[unitId] = army.unitCounts.GetValueOrDefault(unitId) + count;
		army.organization = Mathf.Min(100f, army.organization + 5f);
		army.morale = Mathf.Min(100f, army.morale + 5f);
		return true;
	}

	private static bool TryConsumeManpower(GameWorld world, RealmData realm, float needed)
	{
		if (needed <= 0f)
		{
			return true;
		}
		List<int> tiles = new List<int>();
		HashSet<int> seen = new HashSet<int>();
		if (realm.coreTiles != null)
		{
			foreach (int tile in realm.coreTiles)
			{
				if (tile >= 0 && tile < world.tiles.Length && seen.Add(tile)) tiles.Add(tile);
			}
		}
		for (int i = 0; i < world.tiles.Length; i++)
		{
			if (world.tiles[i].ownerRealmId == realm.realmId && seen.Add(i)) tiles.Add(i);
		}
		float available = 0f;
		foreach (int tileIndex in tiles)
		{
			List<PopulationBlock> blocks = world.tiles[tileIndex].populationBlocks;
			if (blocks == null) continue;
			foreach (PopulationBlock block in blocks) available += block.count * MaxRecruitSharePerBlock;
		}
		if (available + 0.001f < needed)
		{
			return false;
		}
		float share = needed / available;
		foreach (int tileIndex in tiles)
		{
			TileData tile = world.tiles[tileIndex];
			if (tile.populationBlocks == null) continue;
			for (int i = 0; i < tile.populationBlocks.Count; i++)
			{
				PopulationBlock block = tile.populationBlocks[i];
				block.count = Mathf.Max(0f, block.count - block.count * MaxRecruitSharePerBlock * share);
				tile.populationBlocks[i] = block;
			}
			world.tiles[tileIndex] = tile;
		}
		SettlementPopulationSystem.RecomputeAll(world);
		return true;
	}

	private static bool TryConsumeRecruitGoods(GameWorld world, int tileIndex, Dictionary<int, float> recruitCost, float count, out string reason)
	{
		reason = "";
		if (recruitCost == null || recruitCost.Count == 0)
		{
			return true;
		}
		if (tileIndex < 0 || tileIndex >= world.tiles.Length)
		{
			reason = "地块无效";
			return false;
		}
		int regionId = world.tiles[tileIndex].regionId;
		if (world.tradeCenters == null || !world.tradeCenters.TryGetValue(regionId, out TradeCenter market) || market == null)
		{
			reason = "本区没有市场";
			return false;
		}
		foreach (KeyValuePair<int, float> cost in recruitCost)
		{
			float required = cost.Value * count;
			if (market.inventory.GetValueOrDefault(cost.Key, 0f) + 0.001f < required)
			{
				reason = "征兵物资不足";
				return false;
			}
		}
		foreach (KeyValuePair<int, float> cost in recruitCost)
		{
			market.RemoveGoods(cost.Key, cost.Value * count);
		}
		return true;
	}
}
}
