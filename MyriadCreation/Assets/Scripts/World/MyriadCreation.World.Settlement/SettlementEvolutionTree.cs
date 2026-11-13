using System.Collections.Generic;
using MyriadCreation.Core.Data;
using MyriadCreation.Core.Enums;

namespace MyriadCreation.World.Settlement
{
public static class SettlementEvolutionTree
{
	private static readonly string[] NaturalStages = new string[5] { "村落", "集镇", "城邑", "都会", "大都会" };

	private static readonly string[] PlannedStages = new string[5] { "新城", "府城", "规划城", "都会", "大都会" };

	private static readonly string[] FortressStages = new string[5] { "堡垒", "军镇", "军事城市", "军事都会", "大都会" };

	private static readonly string[] CastleStages = new string[5] { "城堡", "贵族城", "行政城市", "行政都会", "大都会" };

	private static readonly string[] PortStages = new string[5] { "锚地", "港口集镇", "港口城市", "贸易都会", "大都会" };

	private static readonly Dictionary<UpgradePath, string[]> PathStages = new Dictionary<UpgradePath, string[]>
	{
		{
			UpgradePath.NaturalGrowth,
			NaturalStages
		},
		{
			UpgradePath.PlannedCity,
			PlannedStages
		},
		{
			UpgradePath.FortressGrowth,
			FortressStages
		},
		{
			UpgradePath.CastleGrowth,
			CastleStages
		},
		{
			UpgradePath.PortDevelopment,
			PortStages
		}
	};

	private static readonly Dictionary<UpgradePath, SettlementLevel> PathMaxLevel = new Dictionary<UpgradePath, SettlementLevel>
	{
		{
			UpgradePath.NaturalGrowth,
			SettlementLevel.LevelV
		},
		{
			UpgradePath.PlannedCity,
			SettlementLevel.LevelV
		},
		{
			UpgradePath.FortressGrowth,
			SettlementLevel.LevelV
		},
		{
			UpgradePath.CastleGrowth,
			SettlementLevel.LevelV
		},
		{
			UpgradePath.PortDevelopment,
			SettlementLevel.LevelV
		}
	};

	public static string GetStageName(SettlementData settlement)
	{
		if (settlement == null)
		{
			return "未知";
		}
		if (!PathStages.TryGetValue(settlement.upgradePath, out var value))
		{
			value = NaturalStages;
		}
		int num = (int)settlement.settlementLevel;
		if (num < 0)
		{
			num = 0;
		}
		if (num >= value.Length)
		{
			num = value.Length - 1;
		}
		return value[num];
	}

	public static SettlementLevel GetMaxLevel(UpgradePath path)
	{
		if (!PathMaxLevel.TryGetValue(path, out var value))
		{
			return SettlementLevel.LevelV;
		}
		return value;
	}

	public static bool IsAtMaxLevel(SettlementData settlement)
	{
		if (settlement != null)
		{
			return settlement.settlementLevel >= GetMaxLevel(settlement.upgradePath);
		}
		return false;
	}

	public static UpgradePath DeriveEvolutionPath(TileData tile, SettlementData settlement)
	{
		if (settlement.siteMode == SiteMode.FortifiedOutpost)
		{
			return UpgradePath.FortressGrowth;
		}
		if (settlement.portTier >= PortTier.IntermediatePort)
		{
			return UpgradePath.PortDevelopment;
		}
		return UpgradePath.NaturalGrowth;
	}

	public static string GetPathDescription(UpgradePath path)
	{
		return path switch
		{
			UpgradePath.NaturalGrowth => "自然发展的定居点，经济成分由地理条件和物产自然决定", 
			UpgradePath.PlannedCity => "人为规划建造的城市（迁都/殖民/军屯）", 
			UpgradePath.FortressGrowth => "从军事堡垒发展而来的路径", 
			UpgradePath.CastleGrowth => "从贵族城堡发展而来的路径", 
			UpgradePath.PortDevelopment => "从港口/渡口发展而来的贸易路径", 
			_ => "未知发展路径", 
		};
	}
}
}
