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

	/// <summary>
	/// 按存档槽位取官职名（P3.1）：文化称谓只覆盖旧六职；新槽位走 OfficeDef 的 nameKey/name。
	/// 内容表缺失时回退枚举路径，保证旧档/工具环境可用。
	/// </summary>
	public static string GetTitleForSlot(CultureData culture, int slot, string polityKey)
	{
		if (slot < 0) return "";
		if (slot < 6 && culture != null && culture.officialTitles != null)
		{
			OfficialOffice legacy = (OfficialOffice)slot;
			foreach (OfficeTitleEntry officialTitle in culture.officialTitles)
			{
				if (officialTitle.office == legacy.ToString() && officialTitle.polityKey == polityKey)
				{
					return Localization.Get(officialTitle.titleKey);
				}
			}
		}
		if (OfficeCatalog.TryGet(slot, out OfficeDef def))
		{
			return Localization.Get(def.nameKey, def.name);
		}
		return slot < 6 ? Localization.Get(GetDefaultTitleKey(((OfficialOffice)slot).ToString(), polityKey)) : "";
	}
}
}
