using System.Collections.Generic;
using MyriadCreation.Content.Definitions;
using MyriadCreation.Content.Modding;
using MyriadCreation.Core.Contracts;
using MyriadCreation.Simulation.ContentHost;
using MyriadCreation.Simulation.Culture;
using MyriadCreation.Simulation.Faith;
using MyriadCreation.Simulation.State;
using MyriadCreation.Simulation.WorldState;

namespace MyriadCreation.Simulation.Systems
{
    /// <summary>
    /// P11.5 传统链：革新完成 → 文化传统固化 → 成熟后转换为教义或推动立法。
    /// 只读现有革新池/文化成熟度；不新增平行进度条。
    /// </summary>
    public static class TraditionConversionSystem
    {
        /// <summary>按已掌握革新与成熟度为政权主文化固化传统；返回本次新增的传统 id。</summary>
        public static List<string> TrySpawnTraditions(GameWorld world, int realmId)
        {
            var added = new List<string>();
            if (world == null || world.Innovations == null) return added;
            if (!world.realms.TryGetValue(realmId, out RealmData realm) || realm == null) return added;
            if (!world.cultures.TryGetValue(realm.primaryCultureId, out CultureData culture) || culture == null) return added;

            HashSet<int> owned = world.Innovations.GetRealmInnovations(realmId);
            foreach (TraditionDef def in ContentResolvers.Traditions.All)
            {
                if (def == null || string.IsNullOrEmpty(def.traditionId)) continue;
                if (culture.HasTradition(def.traditionId, null)) continue;
                if (culture.maturity < def.solidification) continue;
                if (!PrerequisitesMet(def, owned)) continue;
                if (HasIncompatible(def, culture)) continue;
                culture.traditionIds.Add(def.traditionId);
                added.Add(def.traditionId);
            }
            return added;
        }

        /// <summary>把一个已固化的传统转换为教义或法律；返回真正生效的目标 id。</summary>
        public static bool TryConvert(GameWorld world, int realmId, string traditionId,
            int currentDay, out string appliedTarget, ILawCommand lawCommand = null)
        {
            appliedTarget = "";
            if (world == null || string.IsNullOrEmpty(traditionId)) return false;
            if (!world.realms.TryGetValue(realmId, out RealmData realm) || realm == null) return false;
            if (!world.cultures.TryGetValue(realm.primaryCultureId, out CultureData culture) || culture == null) return false;
            if (!culture.HasTradition(traditionId, null)) return false;

            foreach (TraditionConversionDef def in TraditionConversionCatalog.ForTradition(traditionId))
            {
                if (def == null || culture.maturity < def.minimumMaturity) continue;
                if (def.targetKind == "doctrine")
                {
                    FaithSystem faith = world.GetFaithSystem(realm.stateReligionId);
                    if (faith == null) continue;
                    if (faith.doctrineIds == null) faith.doctrineIds = new List<string>();
                    if (faith.doctrineIds.Contains(def.targetId)) continue;
                    faith.doctrineIds.Add(def.targetId);
                    appliedTarget = def.targetId;
                    return true;
                }
                if (def.targetKind == "law")
                {
                    ILawCommand law = lawCommand ?? world.LawCommand;
                    if (law == null) continue;
                    if (!law.BeginEnactLaw(realmId, def.targetId, -1, currentDay, out string _)) continue;
                    appliedTarget = def.targetId;
                    return true;
                }
            }
            return false;
        }

        private static bool PrerequisitesMet(TraditionDef def, HashSet<int> owned)
        {
            if (def.requiredInnovations == null || def.requiredInnovations.Count == 0) return false;
            for (int i = 0; i < def.requiredInnovations.Count; i++)
            {
                if (!owned.Contains(def.requiredInnovations[i])) return false;
            }
            return true;
        }

        private static bool HasIncompatible(TraditionDef def, CultureData culture)
        {
            if (def.incompatibleWith == null || culture.traditionIds == null) return false;
            for (int i = 0; i < def.incompatibleWith.Count; i++)
            {
                if (culture.traditionIds.Contains(def.incompatibleWith[i])) return true;
            }
            return false;
        }
    }
}
