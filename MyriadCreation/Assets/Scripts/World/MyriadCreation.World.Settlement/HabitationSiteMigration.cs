using System;
using MyriadCreation.Core.Enums;

namespace MyriadCreation.World.Settlement
{
public static class HabitationSiteMigration
{
	public static string CreateLegacySiteId(int anchorId)
	{
		return new Guid(anchorId, 0, 0, new byte[8]).ToString("D");
	}

	public static string ResolveSiteId(string storedSiteId, int legacyAnchorId)
	{
		if (string.IsNullOrEmpty(storedSiteId))
		{
			return CreateLegacySiteId(legacyAnchorId);
		}
		if (!Guid.TryParse(storedSiteId, out var result))
		{
			throw new FormatException("Invalid habitation site UUID '" + storedSiteId + "'.");
		}
		return result.ToString("D");
	}

	public static HabitationSite FromLegacy(SettlementData legacy, string stableSiteId)
	{
		if (legacy == null)
		{
			throw new ArgumentNullException("legacy");
		}
		if (!Guid.TryParse(stableSiteId, out var result))
		{
			throw new ArgumentException("A persisted UUID must be supplied by the caller.", "stableSiteId");
		}
		SiteMode siteMode = MapLegacyMode(legacy.settlementCategory);
		HabitationSite habitationSite = new HabitationSite(siteMode)
		{
			siteId = result.ToString("D"),
			siteName = legacy.settlementName,
			location = new SiteLocation
			{
				x = legacy.x,
				y = legacy.y
			},
			population = legacy.population,
			anchorId = legacy.anchorId
		};
		switch (siteMode)
		{
		case SiteMode.SedentarySettlement:
			habitationSite.sedentarySettlementData.settlementMorphology = MapLegacyMorphology(legacy.settlementForm);
			habitationSite.sedentarySettlementData.developmentStage = (int)legacy.settlementLevel;
			break;
		case SiteMode.FortifiedOutpost:
			habitationSite.fortifiedOutpostData.purpose = legacy.primaryFunction.ToString();
			break;
		}
		return habitationSite;
	}

	private static SiteMode MapLegacyMode(SettlementCategory category)
	{
		return category switch
		{
			SettlementCategory.PermanentSettlement => SiteMode.SedentarySettlement, 
			SettlementCategory.Outpost => SiteMode.FortifiedOutpost, 
			SettlementCategory.Camp => SiteMode.TemporaryEncampment, 
			_ => throw new ArgumentOutOfRangeException("category", category, "Unknown legacy settlement category."), 
		};
	}

	private static string MapLegacyMorphology(SettlementForm form)
	{
		return form switch
		{
			SettlementForm.Village => "village", 
			SettlementForm.City => "city", 
			_ => throw new ArgumentOutOfRangeException("form", form, "Unknown legacy settlement morphology."), 
		};
	}
}
}
