using MyriadCreation.Core;
using MyriadCreation.Simulation.Culture;
using MyriadCreation.Content.Definitions;

namespace MyriadCreation.Simulation.State
{
public static class OfficeTitleCatalog
{
	public static string GetDefaultTitleKey(string office, string polityKey = "")
	{
		return "office_" + office;
	}

	public static string GetTitleKey(CultureData culture, OfficialOffice office, string polityKey)
	{
		if (culture != null && culture.officialTitles != null)
		{
			foreach (OfficeTitleEntry officialTitle in culture.officialTitles)
			{
				if (officialTitle.office == office.ToString() && officialTitle.polityKey == polityKey)
				{
					return officialTitle.titleKey;
				}
			}
		}
		return GetDefaultTitleKey(office.ToString(), polityKey);
	}

	public static string GetTitle(CultureData culture, OfficialOffice office, string polityKey)
	{
		return Localization.Get(GetTitleKey(culture, office, polityKey));
	}
}
}
