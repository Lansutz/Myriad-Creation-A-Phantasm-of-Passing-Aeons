using System.Collections.Generic;
using MyriadCreation.Core.Data;
using MyriadCreation.Content.Definitions;
using MyriadCreation.Simulation.State;
using MyriadCreation.Simulation.WorldState;
using MyriadCreation.World.Settlement;
using UnityEngine;

namespace MyriadCreation.Simulation.Systems
{
/// <summary>
/// 政权自然形成的判定侧（P5.1）：只回答“哪个聚落/行为体已经具备建国的物质条件”，
/// 不写世界状态；写入由 <see cref="StateFormationHost"/> 完成。
/// 条件全部可指认：有人口、有开发、有多年积累、有 90 天以上的口粮剩余。
/// </summary>
public static class StateFormationSystem
{
	/// <summary>世界尺度上限：防止小地图无限建国，也给长跑一个可预期的实体规模；不是经济/政治门槛。</summary>
	public const int MaxRealms = 8;

	/// <summary>聚落至少存在这么多年，才谈得上形成政权（P7：从 8 年拉到 200 年量级）。</summary>
	public const int MinSettlementAgeYears = 200;

	public const float MinSettlementPopulation = 800f;

	public const float MinSettlementDevelopment = 15f;

	/// <summary>地区仓储至少要能覆盖人口 180 天口粮，才有多余养活脱产者。</summary>
	public const int RequiredSurplusDays = 180;

	private const float PersonFoodPerDay = 0.01f;

	public static bool MonthlyTick(GameWorld world)
	{
		if (world?.settlements == null || world.tiles == null) return false;
		if (world.realms.Count >= MaxRealms) return false;

		SettlementData candidate = null;
		foreach (SettlementData settlement in world.settlements.Values)
		{
			if (!IsEligible(world, settlement)) continue;
			// 同月多个候选时取最小 anchorId，保证同 seed 同结果。
			if (candidate == null || settlement.anchorId < candidate.anchorId) candidate = settlement;
		}
		if (candidate == null) return false;
		return StateFormationHost.TryFormFromSettlement(world, candidate);
	}

	public static bool IsEligible(GameWorld world, SettlementData settlement)
	{
		if (world == null || settlement == null || settlement.IsRuined) return false;
		if (settlement.settlementCategory == SettlementCategory.Outpost) return false;
		if (settlement.tileIndex < 0 || settlement.tileIndex >= world.tiles.Length) return false;
		TileData tile = world.tiles[settlement.tileIndex];
		if (!tile.exists || !tile.isLand || tile.ownerRealmId >= 0) return false;

		// foundingTick 用绝对日（calendar.AbsoluteDay），不能用 currentDay（它每年回绕到 1）。
		int absoluteDay = world.calendar.AbsoluteDay(world.currentYear, world.currentDay);
		int ageDays = absoluteDay - settlement.foundingTick;
		if (ageDays < MinSettlementAgeYears * world.calendar.daysPerYear) return false;

		float population = Mathf.Max(settlement.population, PopulationLedger.Total(tile));
		if (population < MinSettlementPopulation) return false;
		if (settlement.development < MinSettlementDevelopment) return false;

		float edibleStock = EdibleStock(world, tile.regionId);
		float required = population * PersonFoodPerDay * RequiredSurplusDays;
		return edibleStock >= required;
	}

	/// <summary>地区仓储里真正能吃的货（按 nutrition 折算）；盐/矿不充饥。</summary>
	public static float EdibleStock(GameWorld world, int regionId)
	{
		if (world?.tradeCenters == null || !world.tradeCenters.TryGetValue(regionId, out TradeCenter center) || center?.inventory == null)
		{
			return 0f;
		}
		float total = 0f;
		foreach (KeyValuePair<int, float> pair in center.inventory)
		{
			if (pair.Value <= 0f) continue;
			if (!world.goodsDefs.TryGetValue(pair.Key, out GoodsDef def)) continue;
			bool edible = def.edible || def.category == MyriadCreation.Core.Enums.GameEnums.GoodsCategory.Crop
				|| def.category == MyriadCreation.Core.Enums.GameEnums.GoodsCategory.Livestock;
			if (!edible) continue;
			float nutrition = def.nutrition <= 0f ? 1f : def.nutrition;
			total += pair.Value * nutrition;
		}
		return total;
	}
}
}
