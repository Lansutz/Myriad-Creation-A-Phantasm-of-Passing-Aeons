using MyriadCreation.Core.Data;
using MyriadCreation.Simulation.Systems;
using MyriadCreation.World.Settlement;
using UnityEngine;
using MyriadCreation.Content.Definitions;

namespace MyriadCreation.Simulation.AI
{
public static class SettlementAISystem
{
	public const int BaseSettlementCapacityPerRegion = 8;

	public const float SettlementPer10kPopulation = 0.5f;

	private static bool CheckBuildingAvailability(BuildableType type, TileData tile, SettlementData existingSettlement = null, int techLevel = 0)
	{
		return BuildingAvailabilitySystem.CheckAvailability(type, tile, existingSettlement, techLevel).available;
	}

	public static int CalculateRegionCapacity(int regionTileCount, float regionPopulation, float foodSurplus)
	{
		int num = Mathf.CeilToInt((float)regionTileCount / 500f * 8f);
		int num2 = Mathf.CeilToInt(regionPopulation / 10000f * 0.5f);
		int num3 = ((foodSurplus > 0f) ? Mathf.CeilToInt(foodSurplus / 100f) : 0);
		return Mathf.Max(2, Mathf.Min(num, num2 + num3 + 2));
	}

	public static AIBuildDecision ShouldBuildVillage(TileData tile, float regionPopulationPressure, float tradeRouteFlow, float foodCapacity, int currentSettlementCount, int regionCapacity)
	{
		AIBuildDecision result = new AIBuildDecision
		{
			shouldBuild = false,
			priority = 0f,
			reason = ""
		};
		if (currentSettlementCount >= regionCapacity)
		{
			result.reason = "地区城镇容量已满";
			return result;
		}
		if (!tile.isLand || tile.elevation01 > 0.7f)
		{
			result.reason = "地形不适宜建村";
			return result;
		}
		float num = CalculateFertilityScore(tile);
		if (num < 0.3f)
		{
			result.reason = "地块肥力不足";
			return result;
		}
		float num2 = Mathf.Clamp01(regionPopulationPressure / 100f);
		float num3 = Mathf.Clamp01(tradeRouteFlow / 100f);
		float num4 = Mathf.Clamp01(foodCapacity / 100f);
		result.priority = num * 0.4f + num2 * 0.25f + num3 * 0.2f + num4 * 0.15f;
		if (result.priority > 0.5f)
		{
			result.shouldBuild = true;
			result.reason = $"肥力{num:0.00} 人口压力{num2:0.00} 商路{num3:0.00}";
		}
		return result;
	}

	public static float CalculateFertilityScore(TileData tile)
	{
		float annualPrecipMm = tile.annualPrecipMm;
		float num;
		if (annualPrecipMm < 200f)
		{
			num = 0.1f;
		}
		else if (annualPrecipMm < 400f)
		{
			num = 0.4f;
		}
		else if (annualPrecipMm < 800f)
		{
			num = 0.8f;
		}
		else if (annualPrecipMm < 1500f)
		{
			num = 1f;
		}
		else
		{
			num = ((!(annualPrecipMm < 2500f)) ? 0.5f : 0.7f);
		}
		float num2 = num;
		float num3 = Mathf.Clamp01(1f - Mathf.Abs(tile.annualTemp - 18f) / 20f);
		num = tile.elevation01;
		if (num < 0.3f)
		{
			annualPrecipMm = 1f;
		}
		else if (num < 0.5f)
		{
			annualPrecipMm = 0.8f;
		}
		else if (num < 0.65f)
		{
			annualPrecipMm = 0.5f;
		}
		else
		{
			annualPrecipMm = ((!(num < 0.8f)) ? 0.05f : 0.2f);
		}
		float num4 = annualPrecipMm;
		float num5 = (tile.isRiver ? 0.15f : 0f);
		float num6 = (tile.isCoast ? 0.1f : 0f);
		return Mathf.Clamp01(num2 * 0.4f + num3 * 0.25f + num4 * 0.25f + num5 + num6);
	}

	public static AIBuildDecision ShouldUpgradePort(SettlementData settlement, float tradeDemand, float navalNeed, PortTier currentTier)
	{
		AIBuildDecision result = new AIBuildDecision
		{
			shouldBuild = false,
			priority = 0f,
			reason = ""
		};
		if (!settlement.isCoastal && !settlement.isPort)
		{
			result.reason = "非沿海/沿河聚落";
			return result;
		}
		if (currentTier >= PortTier.ImperialPort)
		{
			result.reason = "已达最高港口等级";
			return result;
		}
		float num = Mathf.Clamp01(tradeDemand / 100f);
		float num2 = Mathf.Clamp01(navalNeed / 100f);
		float num3 = ((settlement.portTier != PortTier.None) ? 0.2f : 0f);
		result.priority = num * 0.5f + num2 * 0.3f + num3;
		if (result.priority > 0.5f)
		{
			result.shouldBuild = true;
			result.reason = $"贸易需求{num:0.00} 海军需求{num2:0.00}";
		}
		return result;
	}

	public static AIBuildDecision ShouldBuildFerry(TileData tile, float crossingDemand, bool hasSettlementNearby, int currentFerryCount, int maxFerries)
	{
		AIBuildDecision result = new AIBuildDecision
		{
			shouldBuild = false,
			priority = 0f,
			reason = ""
		};
		if (currentFerryCount >= maxFerries)
		{
			result.reason = "渡口中限";
			return result;
		}
		if (!tile.isRiver && !tile.isCoast)
		{
			result.reason = "非水域边缘";
			return result;
		}
		float num = Mathf.Clamp01(crossingDemand / 100f);
		float num2 = (hasSettlementNearby ? 0.3f : 0f);
		result.priority = num * 0.6f + num2;
		if (result.priority > 0.45f)
		{
			result.shouldBuild = true;
			result.reason = $"渡河需求{num:0.00}";
		}
		return result;
	}
}
}
