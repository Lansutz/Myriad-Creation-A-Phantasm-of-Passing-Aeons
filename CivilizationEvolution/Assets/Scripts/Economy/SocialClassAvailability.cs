using System.Collections.Generic;
using CivilizationEvolution.Culture;
using CivilizationEvolution.Core;
using CivilizationEvolution.Tech;

namespace CivilizationEvolution.Economy
{
    /// <summary>
    /// 社会阶层可用性（用户定稿：不是所有社会都有全部阶层——
    /// 阶层存在性由 革新 + 文化传统 决定）
    /// 例：农奴需庄园制度（952）；士人需文字（600）+官僚/科举；商人需铸币（701）；
    /// 游牧生计文化无农民；工匠行会传统→工匠可用
    /// </summary>
    public static class SocialClassAvailability
    {
        /// <summary>
        /// 亚阶层是否可用（革新+文化传统判定；innovations 为 null 时宽松=基础可用）
        /// </summary>
        public static bool IsSubclassAvailable(GameEnums.SocialSubclass subclass,
            CultureData culture, InnovationTree innovations, int realmId)
        {
            switch (subclass)
            {
                // ===== 农民四层 =====
                case GameEnums.SocialSubclass.Freeholder:
                    // 自耕农：需要农业（刀耕火种/三作任一）+ 非游牧生计
                    return HasAnyInnovation(innovations, realmId, 100, 916, 917, 918)
                        && (culture == null || culture.livelihoodType != 2); // 2=游牧
                case GameEnums.SocialSubclass.Tenant:
                    // 佃农：需要土地制度（封建/庄园）
                    return HasAnyInnovation(innovations, realmId, 501, 952);
                case GameEnums.SocialSubclass.Serf:
                    // 农奴：需要庄园制度（人身束缚于土地）
                    return HasInnovation(innovations, realmId, 952);
                case GameEnums.SocialSubclass.HiredLaborer:
                    // 雇农：需要农业+市场（无地雇工）
                    return HasAnyInnovation(innovations, realmId, 100, 916, 917, 918)
                        && HasAnyInnovation(innovations, realmId, 701);
                // ===== 自由民四民（士农工商） =====
                case GameEnums.SocialSubclass.Citizen:
                    // 市民：需要城邦/议会（公民权——雅典议事）或部落联盟
                    return HasAnyInnovation(innovations, realmId, 980, 500);
                case GameEnums.SocialSubclass.Merchant:
                    // 商人：需要铸币/贸易
                    return HasAnyInnovation(innovations, realmId, 701, 700);
                case GameEnums.SocialSubclass.Artisan:
                    // 工匠：需要工艺发展 或 工匠行会文化传统
                    if (HasAnyInnovation(innovations, realmId, 204, 200)) return true;
                    return HasTradition(culture, "trad_craft_guild");
                case GameEnums.SocialSubclass.Scholar:
                    // 士人：需要文字 + 官僚/科举（文士阶层）
                    return HasInnovation(innovations, realmId, 600)
                        && HasAnyInnovation(innovations, realmId, 503, 504);
                // ===== 奴隶四源 =====
                case GameEnums.SocialSubclass.DomesticSlave:
                    // 家奴：基础（私有财产观念——部落联盟）
                    return HasAnyInnovation(innovations, realmId, 500, 501);
                case GameEnums.SocialSubclass.StateSlave:
                    // 官奴：需要官僚/集权（国有劳役）
                    return HasAnyInnovation(innovations, realmId, 503, 502);
                case GameEnums.SocialSubclass.DebtSlave:
                    // 债务奴：需要成文法（抵债制度）
                    return HasInnovation(innovations, realmId, 505);
                case GameEnums.SocialSubclass.WarCaptiveSlave:
                    // 战俘奴：基础（战争即有俘虏——era0 弓箭/标枪）
                    return HasAnyInnovation(innovations, realmId, 906, 907, 300);
                default:
                    return false;
            }
        }

        /// <summary>主阶层是否可用（任一亚类可用即主类可用）</summary>
        public static bool IsClassAvailable(GameEnums.SocialClass socialClass,
            CultureData culture, InnovationTree innovations, int realmId)
        {
            foreach (var sub in GameEnums.SocialClassHierarchy.GetSubclasses(socialClass))
            {
                if (IsSubclassAvailable(sub, culture, innovations, realmId)) return true;
            }
            // 未细分阶层（王室/贵族教士）：按主类单独判定
            switch (socialClass)
            {
                case GameEnums.SocialClass.Royalty:
                    return HasAnyInnovation(innovations, realmId, 500, 501, 958);
                case GameEnums.SocialClass.NobilityClergy:
                    return HasAnyInnovation(innovations, realmId, 501, 601);
            }
            return false;
        }

        /// <summary>文化传统是否持有（文化包对接——传统挂载在族群 EthnicGroupDef.traditionIds）</summary>
        private static bool HasTradition(CultureData culture, string traditionId)
        {
            if (culture == null) return false;
            foreach (var group in ContentRegistry.EthnicGroups.Values)
            {
                if (group.cultureId == culture.cultureId
                    && group.traditionIds != null
                    && group.traditionIds.Contains(traditionId))
                    return true;
            }
            return false;
        }

        private static bool HasInnovation(InnovationTree innovations, int realmId, int id)
        {
            // 革新树未注入=宽松（基础可用性）
            return innovations == null || innovations.HasInnovation(realmId, id);
        }

        private static bool HasAnyInnovation(InnovationTree innovations, int realmId, params int[] ids)
        {
            if (innovations == null) return true;
            foreach (int id in ids)
                if (HasInnovation(innovations, realmId, id)) return true;
            return false;
        }
    }
}
