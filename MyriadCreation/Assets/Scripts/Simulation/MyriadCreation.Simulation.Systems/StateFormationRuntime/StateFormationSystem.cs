using System.Collections.Generic;
using MyriadCreation.Core.Data;
using MyriadCreation.Core.Enums;
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
/// 条件全部可指认：已是城市、有人口、有开发、有 180 天以上的口粮剩余；不读世界/聚落年龄。
/// </summary>
public static class StateFormationSystem
{
	/// <summary>世界尺度上限：防止小地图无限建国，也给长跑一个可预期的实体规模；不是经济/政治门槛。</summary>
	public const int MaxRealms = 8;

	public const float MinSettlementPopulation = 800f;

	public const float MinSettlementDevelopment = 15f;

	/// <summary>地区仓储至少要能覆盖人口 180 天口粮，才有多余养活脱产者。</summary>
	public const int RequiredSurplusDays = 180;

	/// <summary>
	/// 开国共同体已经具备的基础制度知识（习惯法、军队编制、城市特许、祭司阶层、祖先记忆、领土主权、文书行政）。
	/// 没有这份底子，新政权连最基础的部族惯例都无法推行；它们不是研究奖励，而是开国时的既有社会能力。
	/// </summary>
	public static readonly int[] FoundingKnowledge = { 2064, 995, 994, 2110, 2151, 958, 822 };

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

		// 没有城市形态就没有稳定的分工与行政剩余；城市本身由 SettlementEvolutionSystem 机制化涌现。
		if (settlement.settlementForm != SettlementForm.City) return false;

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
