using System;
using MyriadCreation.Simulation.Definitions;
using MyriadCreation.Content.Modding;
using MyriadCreation.Simulation.State;
using MyriadCreation.Content.Definitions;
using MyriadCreation.Core.Enums;

namespace MyriadCreation.Simulation.Systems
{
public static class RealmTitleEvolution
{
	public const int TerritorialKingdomInnovation = 1025;

	public static bool IsTerritorial(RealmData realm, InnovationTree innovations)
	{
		if (realm == null || innovations == null)
		{
			return false;
		}
		return innovations.HasInnovation(realm.realmId, 1025);
	}

	public static string GetRealmDisplayName(RealmData realm, InnovationTree innovations)
	{
		return realm?.realmName ?? "";
	}

	public static string GetRulerTitleDisplay(RealmData realm, InnovationTree innovations, int primaryCultureId, Func<int, string> resolvePlural = null)
	{
		if (realm == null)
		{
			return "";
		}
		string title = "王";
		TitleDefinition titleDef = TitleCatalog.Highest(TitleDerivedCategory.Landed);
		if (titleDef != null)
		{
			title = TitleWord(titleDef);
		}
		string people = (resolvePlural != null) ? resolvePlural(primaryCultureId) : "";
		if (!string.IsNullOrEmpty(people))
		{
			return people + "之" + title;
		}
		return realm.realmName + "之" + title;
	}

	private static string TitleWord(TitleDefinition t)
	{
		return t.titleId switch
		{
			"title_king" => "王", 
			"title_emperor" => "皇帝", 
			"title_khan" => "可汗", 
			"title_khagan" => "大汗", 
			"title_vassal_king" => "王", 
			_ => "王", 
		};
	}
}
}
