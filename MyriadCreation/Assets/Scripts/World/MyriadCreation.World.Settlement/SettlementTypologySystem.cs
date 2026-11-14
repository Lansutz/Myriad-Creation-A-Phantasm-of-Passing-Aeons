using System;
using MyriadCreation.Core.Data;
using MyriadCreation.Core.Enums;
using MyriadCreation.World.Hydrology;
using UnityEngine;

namespace MyriadCreation.World.Settlement
{
public static class SettlementTypologySystem
{
	public static void DeriveInitialType(SettlementData settlement, TileData tile, int width, int height)
	{
		if (settlement != null)
		{
			settlement.bottleneckType = DetectBottleneck(tile, width, height);
			settlement.portTier = DetectPortTier(tile);
			settlement.primaryFunction = DerivePrimaryFunction(tile, settlement);
			// 派生结果不降级调用方显式指定的 City（建立城市/殖民城市路径会先设 City，
			// 派生只看地形，不能把"玩家/事件建城"改成村落形态）。
			SettlementForm derivedForm = DeriveSettlementForm(tile, settlement);
			if (!(settlement.settlementForm == SettlementForm.City && derivedForm == SettlementForm.Village))
			{
				settlement.settlementForm = derivedForm;
			}
			if (settlement.settlementForm == SettlementForm.City)
			{
				settlement.cityForm = DeriveCityForm(tile, settlement);
			}
			else if (settlement.siteMode == SiteMode.FortifiedOutpost)
			{
				settlement.cityForm = CityForm.CitadelCity;
			}
			else
			{
				settlement.cityForm = CityForm.WalledTown;
			}
			settlement.upgradePath = DeriveUpgradePath(tile, settlement);
			settlement.settlementLevel = SettlementLevel.LevelI;
			settlement.evolutionStage = EvolutionStage.Stable;
			settlement.fortificationLevel = ((settlement.siteMode == SiteMode.FortifiedOutpost) ? FortificationLevels.Palisade : FortificationLevels.None);
			if (settlement.siteMode != SiteMode.SedentarySettlement && settlement.siteMode != SiteMode.FortifiedOutpost)
			{
				throw new InvalidOperationException("Typology cannot classify site mode '" + SiteModeKeys.ToKey(settlement.siteMode) + "' as a settled site.");
			}
			settlement.SetLegacyCategoryFromSiteMode();
			settlement.constructionProgress = 0f;
			settlement.constructionTier = 1;
			settlement.cityFocus = CityFocus.Balanced;
		}
	}

	public static void UpdateConstructionProgress(SettlementData settlement, float dailyDevelopmentGain)
	{
		if (settlement != null && settlement.settlementLevel < SettlementLevel.LevelV)
		{
			settlement.constructionProgress = Mathf.Min(100f, settlement.constructionProgress + dailyDevelopmentGain);
			settlement.constructionTier = Mathf.Clamp(Mathf.FloorToInt(settlement.constructionProgress / 20f) + 1, 1, 5);
		}
	}

	public static bool IsConstructionReady(SettlementData settlement)
	{
		if (settlement != null)
		{
			return settlement.constructionTier >= 5;
		}
		return false;
	}

	private static BottleneckType DetectBottleneck(TileData tile, int width, int height)
	{
		int num = tile.tileIndex % width;
		int num2 = tile.tileIndex / width;
		if (tile.isCoast || tile.oceanTier == GameEnums.OceanTier.Coast)
		{
			bool flag = HasLandInDirection(tile, width, height, -1, 0, 3);
			bool flag2 = HasLandInDirection(tile, width, height, 1, 0, 3);
			if (flag & flag2)
			{
				return BottleneckType.StraitCrossing;
			}
		}
		if (tile.elevation01 > 0.6f && tile.isLand)
		{
			for (int i = -1; i <= 1; i++)
			{
				for (int j = -1; j <= 1; j++)
				{
					if (j != 0 || i != 0)
					{
						int num3 = num + j;
						int num4 = num2 + i;
						if (num3 >= 0 && num3 < width && num4 < 0)
						{
						}
					}
				}
			}
			if (tile.slopeDegree > 20f)
			{
				return BottleneckType.MountainPass;
			}
		}
		if (tile.isRiver && tile.slopeDegree > 15f)
		{
			return BottleneckType.CanyonPass;
		}
		if (tile.isRiver && tile.elevation01 < 0.4f)
		{
			return BottleneckType.RiverFording;
		}
		if (tile.annualPrecipMm < 300f && tile.isRiver)
		{
			return BottleneckType.DesertCorridor;
		}
		return BottleneckType.None;
	}

	private static bool HasLandInDirection(TileData tile, int width, int height, int dx, int dy, int dist)
	{
		return true;
	}

	private static PortTier DetectPortTier(TileData tile)
	{
		if (!tile.isCoast && !tile.isRiver)
		{
			return PortTier.None;
		}
		if (tile.isCoast && tile.slopeDegree > 10f)
		{
			return PortTier.DeepWaterPort;
		}
		if (tile.isCoast)
		{
			return PortTier.IntermediatePort;
		}
		if (tile.isRiver && RiverNavigability.IsNavigable(tile))
		{
			return PortTier.RiverPort;
		}
		if (tile.isRiver)
		{
			return PortTier.None;
		}
		return PortTier.Anchorage;
	}

	private static SettlementFunction DerivePrimaryFunction(TileData tile, SettlementData settlement)
	{
		if (settlement.bottleneckType != BottleneckType.None)
		{
			if (settlement.bottleneckType == BottleneckType.StraitCrossing || settlement.bottleneckType == BottleneckType.RiverFording)
			{
				return SettlementFunction.Crossing;
			}
			return SettlementFunction.Military;
		}
		if (settlement.portTier >= PortTier.IntermediatePort)
		{
			return SettlementFunction.Commercial;
		}
		if (tile.elevation01 > 0.65f && tile.slopeDegree > 15f)
		{
			return SettlementFunction.Mining;
		}
		if (tile.annualPrecipMm < 300f)
		{
			_ = tile.isRiver;
			return SettlementFunction.Agricultural;
		}
		return SettlementFunction.Agricultural;
	}

	private static SettlementForm DeriveSettlementForm(TileData tile, SettlementData settlement)
	{
		// 初始派生是"新建聚落定型"，不是"已有聚落改型"：
		// 用 TryTransition 会被转换规则拒绝（Sedentary→FortifiedOutpost 只允许反向），
		// 旧实现直接抛 ForbiddenTransition，导致山地隘口/河谷/荒漠走廊上的
		// 建城、圈地、生成聚落全部崩世界生成。
		if (settlement.bottleneckType != BottleneckType.None && settlement.primaryFunction == SettlementFunction.Military)
		{
			if (settlement.siteMode == SiteMode.SedentarySettlement)
			{
				settlement.InitializeModeData(SiteMode.FortifiedOutpost);
				settlement.siteMode = SiteMode.FortifiedOutpost;
			}
			return SettlementForm.Village;
		}
		if (settlement.portTier >= PortTier.IntermediatePort && settlement.primaryFunction == SettlementFunction.Commercial)
		{
			return SettlementForm.City;
		}
		_ = settlement.primaryFunction;
		_ = 16;
		return SettlementForm.Village;
	}

	public static CityForm DeriveCityForm(TileData tile, SettlementData settlement)
	{
		if (settlement.portTier >= PortTier.RiverPort)
		{
			return CityForm.Irregular;
		}
		if (tile.elevation01 > 0.6f)
		{
			return CityForm.MountainCity;
		}
		if (settlement.primaryFunction == SettlementFunction.Administrative && tile.elevation01 < 0.4f)
		{
			return CityForm.Square;
		}
		return CityForm.Circular;
	}

	private static UpgradePath DeriveUpgradePath(TileData tile, SettlementData settlement)
	{
		return SettlementEvolutionTree.DeriveEvolutionPath(tile, settlement);
	}

	public static bool CanLevelUp(SettlementData settlement, out string reason)
	{
		reason = "";
		SettlementLevel settlementLevel = settlement.settlementLevel + 1;
		if (settlementLevel > SettlementLevel.LevelV)
		{
			reason = "已达最高等级";
			return false;
		}
		SettlementLevel maxLevel = SettlementEvolutionTree.GetMaxLevel(settlement.upgradePath);
		if (settlementLevel > maxLevel)
		{
			reason = $"此发展路径最高只能达到 {maxLevel}";
			return false;
		}
		if (settlement.settlementForm == SettlementForm.Village && settlementLevel >= SettlementLevel.LevelIII && settlement.bottleneckType == BottleneckType.None && settlement.portTier < PortTier.IntermediatePort)
		{
			reason = "村镇需先转型为城才能升到Ⅲ级以上";
			return false;
		}
		if (!IsConstructionReady(settlement))
		{
			reason = $"建设度不足（需第5档/100，当前第{settlement.constructionTier}档/{settlement.constructionProgress:0}）";
			return false;
		}
		float[] array = new float[5] { 0f, 500f, 2000f, 8000f, 20000f };
		if (settlement.population < array[(int)settlementLevel])
		{
			reason = $"人口不足（需{array[(int)settlementLevel]:0}，当前{settlement.population:0}）";
			return false;
		}
		if (settlementLevel >= SettlementLevel.LevelIII && settlement.GetEffectiveFortificationLevel() < FortificationLevels.EarthenRampart)
		{
			reason = "Ⅲ级以上需要城墙防御";
			return false;
		}
		if (settlementLevel == SettlementLevel.LevelIV && settlement.cityFocus == CityFocus.Balanced)
		{
			settlement.cityFocus = DeriveCityFocus(settlement);
		}
		return true;
	}

	public static CityFocus DeriveCityFocus(SettlementData settlement)
	{
		if (settlement.primaryFunction.HasFlag(SettlementFunction.Military) && settlement.siteMode == SiteMode.FortifiedOutpost)
		{
			return CityFocus.Defense;
		}
		if (settlement.bottleneckType != BottleneckType.None || settlement.primaryFunction.HasFlag(SettlementFunction.Crossing))
		{
			return CityFocus.Hub;
		}
		if (settlement.portTier >= PortTier.DeepWaterPort || settlement.primaryFunction.HasFlag(SettlementFunction.Commercial))
		{
			return CityFocus.Trade;
		}
		if (settlement.isCapital || settlement.primaryFunction.HasFlag(SettlementFunction.Administrative))
		{
			return CityFocus.Administrative;
		}
		if (settlement.hasTemple)
		{
			return CityFocus.Religious;
		}
		if (settlement.hasUniversity)
		{
			return CityFocus.Cultural;
		}
		if (settlement.primaryFunction.HasFlag(SettlementFunction.Mining))
		{
			return CityFocus.Mining;
		}
		return CityFocus.Balanced;
	}

	public static void PerformLevelUp(SettlementData settlement)
	{
		settlement.settlementLevel++;
		settlement.evolutionStage = EvolutionStage.Transformed;
		settlement.constructionProgress = 0f;
		settlement.constructionTier = 1;
		// 城防升级由独立的城墙建造/工程路径处理；等级提升不再免费赠送城墙。
		settlement.RecalculateFortificationLevelFromBlocks();
		if (settlement.settlementLevel >= SettlementLevel.LevelIV && settlement.cityForm == CityForm.WalledTown)
		{
			settlement.cityForm = ((settlement.siteMode == SiteMode.FortifiedOutpost) ? CityForm.StarCity : CityForm.Circular);
		}
		Debug.Log((object)$"[SettlementTypology] {settlement.settlementName} 升级到 {settlement.settlementLevel}");
	}

	public static bool CheckFormLevelConstraint(SettlementData settlement)
	{
		SettlementLevel settlementLevel = settlement.settlementLevel;
		if ((uint)(settlementLevel - 3) <= 1u)
		{
			return settlement.settlementForm != SettlementForm.Village;
		}
		return true;
	}

	public static string GetFullDescription(SettlementData settlement)
	{
		string stageName = SettlementEvolutionTree.GetStageName(settlement);
		string text;
		switch (settlement.siteMode)
		{
		case SiteMode.SedentarySettlement:
			if (settlement.settlementForm == SettlementForm.Village)
			{
				text = "村镇";
				break;
			}
			if (settlement.settlementForm == SettlementForm.City)
			{
				text = "城";
				break;
			}
			goto default;
		case SiteMode.FortifiedOutpost:
			text = "据点";
			break;
		case SiteMode.TemporaryEncampment:
			text = "营地";
			break;
		default:
			text = "未知";
			break;
		}
		string arg = text;
		return $"{stageName}·{arg}·{settlement.cityFocus}";
	}
}
}
