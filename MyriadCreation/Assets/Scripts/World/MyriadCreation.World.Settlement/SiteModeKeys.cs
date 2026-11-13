using System;
using MyriadCreation.Core.Enums;

namespace MyriadCreation.World.Settlement
{
public static class SiteModeKeys
{
	public const string SedentarySettlement = "sedentary_settlement";

	public const string FortifiedOutpost = "fortified_outpost";

	public const string TemporaryEncampment = "temporary_encampment";

	public static string ToKey(SiteMode mode)
	{
		return mode switch
		{
			SiteMode.SedentarySettlement => "sedentary_settlement", 
			SiteMode.FortifiedOutpost => "fortified_outpost", 
			SiteMode.TemporaryEncampment => "temporary_encampment", 
			_ => throw new ArgumentOutOfRangeException("mode", mode, "Unknown site mode."), 
		};
	}

	public static bool TryParse(string key, out SiteMode mode)
	{
		switch (key)
		{
		case "sedentary_settlement":
			mode = SiteMode.SedentarySettlement;
			return true;
		case "fortified_outpost":
			mode = SiteMode.FortifiedOutpost;
			return true;
		case "temporary_encampment":
			mode = SiteMode.TemporaryEncampment;
			return true;
		default:
			mode = SiteMode.SedentarySettlement;
			return false;
		}
	}
}
}
