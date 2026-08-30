using System.Collections.Generic;
using CivilizationEvolution.Core;
using CivilizationEvolution.Culture;

namespace CivilizationEvolution.Politics
{
    /// <summary>官职（中央/地方治理的职位槽位）</summary>
    public enum OfficialOffice
    {
        Governor,           // 地方长官（省/行省首脑）
        DistrictGovernor,   // 区级长官（下辖分区）
        Chancellor,         // 大法官/首席文官
        Steward,            // 司库/财政官
        Marshal,            // 统军官（军事行政）
        WarCommander        // 元帅（野战指挥）
    }

    /// <summary>官职称号条目（文化定制：某文化某政体语境下某官职的称号键）</summary>
    [System.Serializable]
    public class OfficeTitleEntry
    {
        public string office;      // OfficialOffice 枚举名
        public string polityKey;   // 政体语境键：Kingdom/Empire/Federation/Republic/Tribal/Theocracy
        public string titleKey;    // 本地化键（缺键回退默认称号）
    }

    /// <summary>
    /// 官职称号目录：文化定制优先 → 默认表回退
    /// 政体语境键：Kingdom/Empire/Federation/Republic/Tribal/Theocracy（称号随政体语境变化）
    /// </summary>
    public static class OfficeTitleCatalog
    {
        /// <summary>默认称号键（office 枚举名 → 本地化键 office_&lt;office&gt;——政体语境仅文化定制时区分）</summary>
        public static string GetDefaultTitleKey(string office, string polityKey = "")
        {
            return $"office_{office}";
        }

        /// <summary>
        /// 查询文化定制称号键（CultureData.officialTitles 匹配 office+polityKey），无定制回退默认
        /// </summary>
        public static string GetTitleKey(CultureData culture, OfficialOffice office, string polityKey)
        {
            if (culture != null && culture.officialTitles != null)
            {
                foreach (var entry in culture.officialTitles)
                {
                    if (entry.office == office.ToString() && entry.polityKey == polityKey)
                        return entry.titleKey;
                }
            }
            return GetDefaultTitleKey(office.ToString(), polityKey);
        }

        /// <summary>查询显示文本（本地化解析；缺键回退键名）</summary>
        public static string GetTitle(CultureData culture, OfficialOffice office, string polityKey)
        {
            return Localization.Get(GetTitleKey(culture, office, polityKey));
        }
    }
}
