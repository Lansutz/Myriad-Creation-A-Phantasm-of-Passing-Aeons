using MyriadCreation.World.Settlement;
using MyriadCreation.Core.Enums;

namespace MyriadCreation.World.Presentation
{
public static class SiteTypeLabels
{
	public static string Group(SiteMode mode)
	{
		return mode switch
		{
			SiteMode.SedentarySettlement => "定居点", 
			SiteMode.FortifiedOutpost => "据点", 
			SiteMode.TemporaryEncampment => "营地", 
			_ => "", 
		};
	}

	public static string Level(SettlementLevel level)
	{
		return level switch
		{
			SettlementLevel.LevelI => "村落", 
			SettlementLevel.LevelII => "集镇", 
			SettlementLevel.LevelIII => "城邑", 
			SettlementLevel.LevelIV => "都会", 
			SettlementLevel.LevelV => "大都会", 
			_ => "城邑", 
		};
	}

	public static string TypeName(HabitationSite site)
	{
		if (site == null)
		{
			return "";
		}
		if (site is SettlementData settlementData)
		{
			if (settlementData.siteMode == SiteMode.SedentarySettlement)
			{
				return Level(settlementData.settlementLevel);
			}
			if (settlementData.siteMode == SiteMode.FortifiedOutpost)
			{
				if (settlementData.portTier >= PortTier.RiverPort && (settlementData.primaryFunction & SettlementFunction.Commercial) != 0)
				{
					return "港口";
				}
				return "堡垒";
			}
		}
		if (site.siteMode == SiteMode.TemporaryEncampment)
		{
			return "营地";
		}
		return Group(site.siteMode);
	}
}
}
