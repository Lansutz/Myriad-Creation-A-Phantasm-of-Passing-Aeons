using MyriadCreation.Core.Data;
using UnityEngine;

namespace MyriadCreation.Simulation.Systems
{
/// <summary>
/// 生态演替与野生群落（P3.2，参考 Earth Simulator 的 ecology/fauna 两段模型，重写而非移植）：
/// 植被向“NPP 潜力 − 人口开垦”的目标缓慢趋近，野生草食动物再跟随植被与狩猎压力。
/// 聚合口径（每地块一个 wildlife 0–1），不存物种清单，避免逐格物种表的开销。
/// 成本：每月 O(N)；确定性，无随机。
/// </summary>
public static class EcosystemSystem
{
	/// <summary>植被恢复速率（每月，Earth Simulator 的 GROW 同量级）。</summary>
	public const float VegetationGrowthPerMonth = 0.02f;

	/// <summary>植被退化速率（每月，EARTH 的 DECAY：破坏快于恢复）。</summary>
	public const float VegetationDecayPerMonth = 0.065f;

	/// <summary>野生群落响应速率（每月）。</summary>
	public const float WildlifeResponsePerMonth = 0.03f;

	/// <summary>人口压力拉满时，目标植被最多被压掉的比例。</summary>
	public const float MaxClearanceShare = 0.75f;

	/// <summary>人口压力拉满时，野生群落相对植被承载的上限比例。</summary>
	public const float MaxWildlifeSuppression = 0.9f;

	/// <summary>每地块野生承载 = 植被 × 该系数（0–1 密度口径）。</summary>
	public const float WildlifePerVegetation = 0.8f;

	/// <summary>狩猎口粮占人均日粮的比例（其余靠农牧；比例小，避免把野味当主粮）。</summary>
	public const float HuntFoodPerPersonPerDay = 0.001f;

	/// <summary>野生密度 1.0 对应每日可提供的食物单位（丰度→肉的换算）。</summary>
	public const float WildlifeFoodCapacityPerUnit = 20f;

	/// <summary>捕鱼口粮占人均日粮的比例（与狩猎同级，不把鱼当主粮）。</summary>
	public const float FishFoodPerPersonPerDay = 0.001f;

	/// <summary>鱼群密度 1.0 对应每日可提供的食物单位。</summary>
	public const float FishFoodCapacityPerUnit = 20f;

	/// <summary>放牧压力对目标植被的最大压低比例。</summary>
	public const float MaxGrazingShare = 0.3f;

	/// <summary>鱼群恢复/捕捞响应速率（每月）。</summary>
	public const float FishResponsePerMonth = 0.03f;

	/// <summary>
	/// 狩猎：按人口需求从野生群落取肉，返回食物单位，并就地扣减野生密度。
	/// 无人、无兽、非陆地返回 0；扣减与产出同源，不允许只产不扣。
	/// </summary>
	public static float Harvest(ref TileData tile, float days)
	{
		if (!tile.exists || !tile.isLand || days <= 0f) return 0f;
		EnsureInitialized(ref tile);
		if (tile.wildlife <= 0f || tile.populationBlocks == null) return 0f;
		float people = 0f;
		for (int i = 0; i < tile.populationBlocks.Count; i++)
		{
			people += Mathf.Max(0f, tile.populationBlocks[i].count);
		}
		if (people <= 0f) return 0f;
		float capacity = tile.wildlife * WildlifeFoodCapacityPerUnit * days;
		float demand = people * HuntFoodPerPersonPerDay * days;
		float harvest = Mathf.Min(capacity, demand);
		if (harvest <= 0f) return 0f;
		tile.wildlife = Mathf.Clamp01(tile.wildlife - harvest / WildlifeFoodCapacityPerUnit);
		return harvest;
	}

	/// <summary>捕鱼：沿海地块按人口需求取鱼，返回食物单位并扣减 fishStock。</summary>
	public static float HarvestFish(ref TileData tile, float days)
	{
		if (!tile.exists || !tile.isLand || !tile.isCoast || days <= 0f) return 0f;
		if (tile.fishStock <= 0f || tile.populationBlocks == null) return 0f;
		float people = 0f;
		for (int i = 0; i < tile.populationBlocks.Count; i++)
		{
			people += Mathf.Max(0f, tile.populationBlocks[i].count);
		}
		if (people <= 0f) return 0f;
		float capacity = tile.fishStock * FishFoodCapacityPerUnit * days;
		float demand = people * FishFoodPerPersonPerDay * days;
		float harvest = Mathf.Min(capacity, demand);
		if (harvest <= 0f) return 0f;
		tile.fishStock = Mathf.Clamp01(tile.fishStock - harvest / FishFoodCapacityPerUnit);
		return harvest;
	}

	/// <summary>
	/// 火灾判定（确定性）：干燥、炎热、植被高才可能起火；同一 tile/year 永远同一结果。
	/// 阈值是模型常量，不是平衡数值。
	/// </summary>
	public static bool ShouldBurn(in TileData tile, int year)
	{
		if (!tile.exists || !tile.isLand) return false;
		if (tile.vegetation < 0.7f || tile.annualPrecipMm >= 500f || tile.annualTemp < 20f) return false;
		unchecked
		{
			int hash = tile.tileIndex * 73856093 ^ year * 19349663;
			return (hash & 0x7fffffff) % 10000 < 25;
		}
	}

	/// <summary>火灾后果：烧掉植被/野生/牲畜；不做逐帧燃烧动画。</summary>
	public static void Burn(ref TileData tile)
	{
		tile.vegetation = Mathf.Clamp01(tile.vegetation * 0.3f);
		tile.wildlife = Mathf.Clamp01(tile.wildlife * 0.5f);
		tile.livestock = Mathf.Clamp01(tile.livestock * 0.5f);
	}

	/// <summary>旧档/新地块惰性初始化：植被从 NPP 起步，野生群落按植被比例起步。</summary>
	public static void EnsureInitialized(ref TileData tile)
	{
		if (!tile.exists || !tile.isLand) return;
		if (tile.vegetation <= 0f) tile.vegetation = Mathf.Clamp01(tile.npp);
		if (tile.wildlife <= 0f) tile.wildlife = tile.vegetation * 0.4f;
	}

	/// <summary>人口压力 0–1：人越多、植被越少，开垦/狩猎压力越大。</summary>
	public static float PopulationPressure(in TileData tile)
	{
		if (tile.populationBlocks == null) return 0f;
		float people = 0f;
		for (int i = 0; i < tile.populationBlocks.Count; i++)
		{
			people += Mathf.Max(0f, tile.populationBlocks[i].count);
		}
		float vegetation = Mathf.Max(0.05f, tile.vegetation);
		return Mathf.Clamp01(people / (200f + 800f * vegetation));
	}

	/// <summary>推进 <paramref name="days"/> 天的演替；返回发生变化的陆地地块数。</summary>
	public static int Tick(TileData[] tiles, float days, int year = 0)
	{
		if (tiles == null || days <= 0f) return 0;
		float months = days / 30f;
		int changed = 0;
		for (int i = 0; i < tiles.Length; i++)
		{
			ref TileData tile = ref tiles[i];
			if (!tile.exists || !tile.isLand) continue;
			EnsureInitialized(ref tile);
			float pressure = PopulationPressure(in tile);
			float people = PopulationOf(in tile);
			float livestockTarget = Mathf.Clamp01(people / 800f);
			float livestock = Step(tile.livestock, livestockTarget, 0.02f, 0.02f, months);
			float grazing = Mathf.Clamp01(livestock);
			float targetVegetation = Mathf.Clamp01(tile.npp) * (1f - MaxClearanceShare * pressure) * (1f - MaxGrazingShare * grazing);
			float oldVegetation = tile.vegetation;
			float vegetation = Step(tile.vegetation, targetVegetation,
				VegetationGrowthPerMonth, VegetationDecayPerMonth, months);
			tile.vegetation = vegetation;
			if (ShouldBurn(in tile, year)) Burn(ref tile);
			vegetation = tile.vegetation;
			float targetWildlife = vegetation * WildlifePerVegetation * (1f - MaxWildlifeSuppression * pressure);
			float wildlife = Step(tile.wildlife, targetWildlife,
				WildlifeResponsePerMonth, WildlifeResponsePerMonth, months);
			float fish = tile.fishStock;
			if (tile.isCoast)
			{
				float targetFish = 0.8f * (1f - 0.5f * pressure);
				fish = Step(tile.fishStock, targetFish, FishResponsePerMonth, FishResponsePerMonth, months);
			}
			if (!Mathf.Approximately(vegetation, oldVegetation) || !Mathf.Approximately(wildlife, tile.wildlife)
				|| !Mathf.Approximately(fish, tile.fishStock) || !Mathf.Approximately(livestock, tile.livestock))
			{
				tile.vegetation = vegetation;
				tile.wildlife = wildlife;
				tile.fishStock = fish;
				tile.livestock = livestock;
				changed++;
			}
		}
		return changed;
	}

	private static float PopulationOf(in TileData tile)
	{
		if (tile.populationBlocks == null) return 0f;
		float people = 0f;
		for (int i = 0; i < tile.populationBlocks.Count; i++) people += Mathf.Max(0f, tile.populationBlocks[i].count);
		return people;
	}

	private static float Step(float current, float target, float growRate, float decayRate, float months)
	{
		float rate = (target > current ? growRate : decayRate) * months;
		return Mathf.Clamp01(Mathf.Lerp(current, target, Mathf.Clamp01(rate)));
	}
}
}
