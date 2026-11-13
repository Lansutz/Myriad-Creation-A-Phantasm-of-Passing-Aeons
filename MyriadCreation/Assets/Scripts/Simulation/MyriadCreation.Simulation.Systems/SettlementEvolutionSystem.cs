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
			foundingTick = world.currentDay,
			fortificationLevel = (flag ? FortificationLevels.Palisade : FortificationLevels.None)
		};
		settlementData.fortifiedOutpostData.purpose = camp.fortifiedOutpostData.purpose;
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
			if (!HabitationSiteTransitionSystem.TryTransition(value, SiteMode.TemporaryEncampment, out var failure))
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
