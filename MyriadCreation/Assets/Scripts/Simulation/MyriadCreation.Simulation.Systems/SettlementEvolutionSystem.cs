using System.Collections.Generic;
using MyriadCreation.Core.Data;
using MyriadCreation.Simulation.Actors;
using MyriadCreation.Simulation.WorldState;
using MyriadCreation.World.Anchor;
using MyriadCreation.World.Settlement;
using UnityEngine;
using MyriadCreation.Core.Enums;

namespace MyriadCreation.Simulation.Systems
{
public static class SettlementEvolutionSystem
{
	public const float FORTIFIED_CAMP_DEFENSE_THRESHOLD = 40f;

	public const float FORT_SETTLEMENT_DEFENSE_THRESHOLD = 70f;

	public const int CAMP_TO_VILLAGE_POP = 50;

	public const int FORTIFIED_CAMP_TO_FORT_POP = 100;

	/// <summary>村镇进入城市形态的人口门槛。</summary>
	public const float CITY_POPULATION_THRESHOLD = 1200f;

	/// <summary>村镇进入城市形态的开发度门槛。</summary>
	public const float CITY_DEVELOPMENT_THRESHOLD = 20f;

	/// <summary>城市必须能养活脱产者：地区仓储至少覆盖人口 180 天口粮。</summary>
	public const int CITY_SURPLUS_DAYS = 180;

	private const float PERSON_FOOD_PER_DAY = 0.01f;

	/// <summary>
	/// 村镇→城市：人口、开发、市场、营建与粮食剩余都到位才转形态，不读世界/聚落年龄。
	/// 城市形态是分工与行政剩余的结果，出现年份只作观测。
	/// </summary>
	public static bool TryUrbanize(GameWorld world, SettlementData settlement)
	{
		if (world == null || settlement == null) return false;
		if (settlement.IsRuined || settlement.siteMode != SiteMode.SedentarySettlement) return false;
		if (settlement.settlementForm == SettlementForm.City) return false;
		if (settlement.tileIndex < 0 || settlement.tileIndex >= world.tiles.Length) return false;

		TileData tile = world.tiles[settlement.tileIndex];
		if (!tile.exists || !tile.isLand) return false;

		float population = Mathf.Max(settlement.population, PopulationLedger.Total(tile));
		if (population < CITY_POPULATION_THRESHOLD) return false;
		if (settlement.development < CITY_DEVELOPMENT_THRESHOLD) return false;

		// 没有市场就没有稳定的分工与交换；市场是“城市”而非“大村庄”的机制标志。
		if (!settlement.hasMarket && settlement.market == null) return false;

		// 营建到位才谈得上城墙/街区/公共建筑；未完工的村镇不升级形态。
		if (!SettlementTypologySystem.IsConstructionReady(settlement)) return false;

		float required = population * PERSON_FOOD_PER_DAY * CITY_SURPLUS_DAYS;
		if (StateFormationSystem.EdibleStock(world, tile.regionId) < required) return false;

		settlement.settlementForm = SettlementForm.City;
		settlement.cityForm = SettlementTypologySystem.DeriveCityForm(tile, settlement);
		settlement.primaryFunction = SettlementFunction.Administrative;
		if (world.FirstCityYear < 0) world.FirstCityYear = world.currentYear;
		world.CityFoundedCount++;
		world.GetChronicle()?.Add(
			"settlement_urbanized",
			$"{settlement.settlementName} 由村镇发展为城市（人口约 {Mathf.RoundToInt(population)}）",
			true);
		return true;
	}

	/// <summary>城市在缺水后维持多久才判衰退：3 年，给季节河/短期干旱留恢复窗口。</summary>
	public const int EnvironmentalCollapseDays = 3 * 365;

	/// <summary>
	/// 环境压力：聚落周边没有河流/海岸水体可依赖时，人口越大越撑不住；水源恢复则压力回退。
	/// 返回是否达到衰退阈值。
	/// </summary>
	public static bool ApplyEnvironmentalPressure(GameWorld world, SettlementData settlement, int deltaDays)
	{
		if (world?.tiles == null || settlement == null || settlement.IsRuined) return false;
		int support = CountWaterSupport(world.tiles, world.mapWidth, world.mapHeight, settlement.tileIndex, 2);
		bool needsWater = settlement.population >= 200f || settlement.settlementForm == SettlementForm.City;
		// 多年干旱：灾害系统压低土壤湿度与降水，即使河道还在，土地也养不起人。
		TileData site = settlement.tileIndex >= 0 && settlement.tileIndex < world.tiles.Length
			? world.tiles[settlement.tileIndex]
			: default;
		bool droughtStressed = site.soilHumidityPct < 12f && site.annualPrecipMm < 400f;
		if (needsWater && (support == 0 || droughtStressed))
		{
			settlement.environmentalPressureDays += Mathf.Max(1, deltaDays);
		}
		else
		{
			settlement.environmentalPressureDays = Mathf.Max(0, settlement.environmentalPressureDays - Mathf.Max(1, deltaDays) * 2);
		}
		if (settlement.environmentalPressureDays >= EnvironmentalCollapseDays)
		{
			settlement.lastDeclineCause = "water_loss";
			return true;
		}
		return false;
	}

	/// <summary>半径内的河流/海岸水体数量；河道与海洋都算水源。</summary>
	public static int CountWaterSupport(TileData[] tiles, int width, int height, int tileIndex, int radius)
	{
		if (tiles == null || tileIndex < 0 || tileIndex >= tiles.Length) return 0;
		int cx = tileIndex % width;
		int cy = tileIndex / width;
		int count = 0;
		for (int y = cy - radius; y <= cy + radius; y++)
		{
			for (int x = cx - radius; x <= cx + radius; x++)
			{
				if (x < 0 || x >= width || y < 0 || y >= height) continue;
				TileData tile = tiles[y * width + x];
				if (!tile.exists) continue;
				if (tile.isRiver) count++;
				else if (!tile.isLand && tile.oceanTier != GameEnums.OceanTier.Land) count++;
			}
		}
		return count;
	}

	public static void DailyTick(GameWorld world)
	{
		if (world?.Camps == null)
		{
			return;
		}
		IReadOnlyList<CampData> allCamps = world.Camps.AllCamps;
		for (int num = allCamps.Count - 1; num >= 0; num--)
		{
			CampData campData = allCamps[num];
			if (!campData.isAbandoned && campData.type != CampType.Nomad && campData.CanEvolveToSettlement)
			{
				TryEvolveCampToSettlement(world, campData);
			}
		}
	}

	public static void FortifyCamp(GameWorld world, int campId, float materialInput)
	{
		CampData campData = world?.Camps?.GetCamp(campId);
		if (campData == null || campData.isAbandoned)
		{
			return;
		}
		campData.defense = Mathf.Min(100f, campData.defense + materialInput * 0.5f);
		campData.permanence = Mathf.Min(100f, campData.permanence + materialInput * 0.2f);
		if (campData.defense >= 40f && campData.attributes != null && !campData.attributes.ContainsKey("isFortified"))
		{
			if (!HabitationSiteTransitionSystem.TryTransition(campData, SiteMode.FortifiedOutpost, out var failure))
			{
				Debug.LogError((object)$"[SettlementEvolution] 营地#{campData.campId}无法升级为据点：{failure}");
				return;
			}
			campData.fortifiedOutpostData.purpose = campData.type.ToString();
			campData.attributes["isFortified"] = 1f;
			Debug.Log((object)$"[SettlementEvolution] 营地#{campData.campId}升级为据点（防御={campData.defense:F0}）");
		}
	}

	public static SettlementData TryEvolveCampToSettlement(GameWorld world, CampData camp)
	{
		if ((Object)(object)world == (Object)null || camp == null)
		{
			return null;
		}
		if (camp.type == CampType.Nomad)
		{
			return null;
		}
		bool flag = camp.attributes != null && camp.attributes.TryGetValue("isFortified", out var value) && value > 0f;
		SiteMode siteMode;
		SettlementLevel settlementLevel;
		SettlementForm settlementForm;
		if (flag && camp.defense >= 70f && camp.population >= 100f)
		{
			siteMode = SiteMode.FortifiedOutpost;
			settlementLevel = SettlementLevel.LevelII;
			settlementForm = SettlementForm.Village;
		}
		else
		{
			if (!(camp.population >= 50f))
			{
				return null;
			}
			siteMode = SiteMode.SedentarySettlement;
			settlementLevel = SettlementLevel.LevelI;
			settlementForm = SettlementForm.Village;
		}
		if (camp.siteMode == SiteMode.TemporaryEncampment && !HabitationSiteTransitionSystem.TryTransition(camp, SiteMode.FortifiedOutpost, out var failure))
		{
			Debug.LogError((object)$"[SettlementEvolution] 营地#{camp.campId}无法先转为据点：{failure}");
			return null;
		}
		if (camp.siteMode != SiteMode.FortifiedOutpost)
		{
			Debug.LogError((object)$"[SettlementEvolution] 营地#{camp.campId}不处于可演化模式：{SiteModeKeys.ToKey(camp.siteMode)}");
			return null;
		}
		camp.fortifiedOutpostData.purpose = camp.type.ToString();
		if (world.anchors == null)
		{
			world.anchors = new Dictionary<int, AnchorData>();
		}
		int num = ((camp.anchorId >= 0) ? camp.anchorId : NextAnchorId(world));
		world.anchors.TryGetValue(num, out var value2);
		AnchorData anchorData = value2 ?? new AnchorData
		{
			anchorId = num,
			provinceId = world.tiles[camp.tileIndex].provinceId,
			tileIndex = camp.tileIndex,
			x = camp.location.x,
			y = camp.location.y
		};
		anchorData.anchorId = num;
		anchorData.provinceId = world.tiles[camp.tileIndex].provinceId;
		anchorData.tileIndex = camp.tileIndex;
		SettlementData settlementData = new SettlementData
		{
			siteId = camp.siteId,
			previousSiteMode = camp.previousSiteMode,
			hasPreviousSiteMode = camp.hasPreviousSiteMode,
			anchorId = num,
			anchor = anchorData,
			settlementName = camp.campName,
			population = camp.population,
			development = camp.permanence * 0.1f,
			wealth = camp.supplies * 0.5f,
			fortification = camp.defense * 0.1f,
			garrison = Mathf.RoundToInt(camp.population / 5f),
			siteMode = SiteMode.FortifiedOutpost,
			constructionProgress = 0f,
			constructionTier = 1,
			settlementEvolution = 100f,
			settlementStability = 50f,
			foundingTick = world.calendar.AbsoluteDay(world.currentYear, world.currentDay),
			fortificationLevel = (flag ? FortificationLevels.Palisade : FortificationLevels.None)
		};
		// SettlementData 构造时按默认 SiteMode 初始化模式数据，据点数据可能为 null；
		// 长跑里营地首次演化成聚落就会在这里 NRE（P7 250 年诊断实际触发）。
		if (camp.fortifiedOutpostData != null)
		{
			settlementData.fortifiedOutpostData ??= new FortifiedOutpostData();
			settlementData.fortifiedOutpostData.purpose = camp.fortifiedOutpostData.purpose;
		}
		settlementData.SetLegacyCategoryFromSiteMode();
		if (siteMode == SiteMode.SedentarySettlement && !HabitationSiteTransitionSystem.TryTransition(settlementData, SiteMode.SedentarySettlement, out var failure2))
		{
			Debug.LogError((object)$"[SettlementEvolution] 无法将栖居点#{settlementData.siteId}从据点转为定居点：{failure2}");
			return null;
		}
		settlementData.settlementForm = settlementForm;
		settlementData.settlementLevel = settlementLevel;
		world.anchors[num] = anchorData;
		world.settlements[num] = settlementData;
		// D40：营地人口转入地块人口块，聚落人口改为派生聚合。
		SettlementPopulationSystem.AddPopulation(world, settlementData, camp.population);
		world.Camps.RemoveTransferredCamp(camp.campId, settlementData.siteId);
		TileData tileData = world.tiles[camp.tileIndex];
		tileData.campId = -1;
		world.tiles[camp.tileIndex] = tileData;
		Debug.Log((object)$"[SettlementEvolution] 营寨#{camp.campId}({camp.type}) 演化为聚落#{num}({settlementForm})");
		return settlementData;
	}

	public static SettlementData SettleNomadCamp(GameWorld world, int campId)
	{
		CampData campData = world?.Camps?.GetCamp(campId);
		if (campData == null || campData.type != CampType.Nomad)
		{
			return null;
		}
		if (campData.population < 50f)
		{
			return null;
		}
		campData.type = CampType.Military;
		campData.permanence = 100f;
		return TryEvolveCampToSettlement(world, campData);
	}

	public static CampData DegradeSettlementToCamp(GameWorld world, int anchorId, CampType campType = CampType.Military, int ownerRealmId = -1)
	{
		if ((Object)(object)world == (Object)null || !world.settlements.TryGetValue(anchorId, out var value))
		{
			return null;
		}
		if (value.IsRuined)
		{
			return null;
		}
		CampData campData = world.Camps.EstablishCamp(campType, value.tileIndex, Mathf.RoundToInt(value.population), ownerRealmId, -1, value.settlementName);
		if (campData != null)
		{
			if (!HabitationSiteTransitionSystem.TryDowngradeDueToDestruction(value, SiteMode.TemporaryEncampment, out var failure))
			{
				world.Camps.AbandonCamp(campData.campId);
				Debug.LogError((object)$"[SettlementEvolution] 无法将栖居点#{value.siteId}降为营地：{failure}");
				return null;
			}
			campData.siteId = value.siteId;
			campData.anchorId = value.anchorId;
			campData.location.x = value.x;
			campData.location.y = value.y;
			campData.defense = value.fortification * 10f;
			campData.supplies = value.wealth;
			world.settlements.Remove(anchorId);
			Debug.Log((object)$"[SettlementEvolution] 聚落#{anchorId}降级为营寨#{campData.campId}");
		}
		return campData;
	}

	private static int NextAnchorId(GameWorld world)
	{
		int num = 0;
		foreach (int key in world.anchors.Keys)
		{
			if (key > num)
			{
				num = key;
			}
		}
		return num + 1;
	}
}
}
