using CivilizationEvolution.Core;
using CivilizationEvolution.Politics;
using CivilizationEvolution.Tech;

namespace CivilizationEvolution.Culture
{
    /// <summary>
    /// 头衔演化系统（2026-09-03 用户设计——革新驱动领土化）：
    /// ①头衔期（领土王国革新前）：头衔=个人属格——"某某族群人之王"
    ///   （rex Francorum 式——统辖人非土地——无领土实体概念）
    /// ②领土化（持有革新 1025 领土王国）：王国想象形成——
    ///   king-dom（王之土）——政权成为领土实体——显示"XX王国"
    /// 族群对接：某某人=文化族群（EthnicGroupDef 永久标签——
    /// 复数称谓 GetPluralName——语言词条）
    /// </summary>
    public static class RealmTitleEvolution
    {
        /// <summary>领土王国革新 ID（1025——制度-政制 era2——前置部落联盟）</summary>
        public const int TerritorialKingdomInnovation = 1025;

        /// <summary>是否已领土化（政权持有革新）</summary>
        public static bool IsTerritorial(RealmData realm, InnovationTree innovations)
        {
            if (realm == null || innovations == null) return false;
            return innovations.HasInnovation(realm.realmId, TerritorialKingdomInnovation);
        }

        /// <summary>政权名（演化两态）：领土化=加领土后缀（XX王国——realmSuffix 类
        /// TitleDef）——头衔期=原名（人治形态——由族群王称表达）</summary>
        public static string GetRealmDisplayName(RealmData realm, InnovationTree innovations)
        {
            if (realm == null) return "";
            if (IsTerritorial(realm, innovations))
            {
                // 领土化：名+王国/汗国等后缀（realmSuffix——最高位阶通用）
                var suffix = TitleCatalog.Highest("realmSuffix");
                if (suffix != null && !string.IsNullOrEmpty(suffix.titleId)
                    && !realm.realmName.EndsWith("国") && !realm.realmName.EndsWith("朝"))
                    return realm.realmName + SuffixWord(suffix);
            }
            return realm.realmName;
        }

        /// <summary>国名后缀词（王国/帝国/汗国——titleId→词——映射表）</summary>
        private static string SuffixWord(TitleDef suffix)
        {
            switch (suffix.titleId)
            {
                case "realm_kingdom": return "王国";
                case "realm_empire": return "帝国";
                case "realm_khaganate": return "汗国";
                default: return "国";
            }
        }

        /// <summary>
        /// 君主王称（头衔期形态）："某某族群人之王"——族群复数称谓+王
        /// （rex Francorum 同构——统辖人）——领土化后=王国之王
        /// </summary>
        public static string GetRulerTitleDisplay(RealmData realm, InnovationTree innovations,
            int primaryCultureId, System.Func<int, string> resolvePlural = null)
        {
            if (realm == null) return "";
            string kingWord = "王";
            var king = TitleCatalog.Highest("monarch", primaryCultureId);
            if (king != null) kingWord = TitleWord(king);

            if (!IsTerritorial(realm, innovations))
            {
                // 头衔期：{族群复数}之{王}（某某人之王）
                string people = resolvePlural != null ? resolvePlural(primaryCultureId) : "";
                if (!string.IsNullOrEmpty(people))
                    return $"{people}之{kingWord}";
                return $"{realm.realmName}之{kingWord}";
            }
            // 领土化：{政权名}之{王}（XX王国之王）
            return $"{GetRealmDisplayName(realm, innovations)}之{kingWord}";
        }

        private static string TitleWord(TitleDef t)
        {
            switch (t.titleId)
            {
                case "title_king": return "王";
                case "title_emperor": return "皇帝";
                case "title_khan": return "可汗";
                case "title_khagan": return "大汗";
                case "title_vassal_king": return "王";
                default: return "王";
            }
        }
    }
}
