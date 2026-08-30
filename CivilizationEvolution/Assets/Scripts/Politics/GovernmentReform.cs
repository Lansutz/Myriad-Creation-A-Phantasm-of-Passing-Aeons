using CivilizationEvolution.Core;
using CivilizationEvolution.Tech;

namespace CivilizationEvolution.Politics
{
    /// <summary>
    /// 政体改革（用户定稿：研究新革新后可以改革政体成分）
    /// 改革条件=目标成分的支撑革新已持有（PolityComponentInnovations）；
    /// 改革引发短暂动荡（稳定性下降）并记录编年史
    /// </summary>
    public static class GovernmentReform
    {
        /// <summary>目标成分是否可改革（支撑革新已持有——未持有不可改革）</summary>
        public static bool CanReform(RealmData realm, PolityComponentInnovations.PolityDimension dimension,
            int newComponent, InnovationTree innovations)
        {
            if (realm == null || innovations == null) return false;
            return PolityComponentInnovations.IsComponentAvailable(dimension, newComponent, innovations, realm.realmId);
        }

        /// <summary>
        /// 执行政体改革：检查支撑革新 → 应用新成分 → 稳定性下降 → 编年史记录
        /// 返回是否成功
        /// </summary>
        public static bool Reform(RealmData realm, PolityComponentInnovations.PolityDimension dimension,
            int newComponent, InnovationTree innovations, Chronicle chronicle = null)
        {
            if (!CanReform(realm, dimension, newComponent, innovations)) return false;

            string oldName = GetComponentName(dimension, GetCurrent(realm, dimension));
            string newName = GetComponentName(dimension, newComponent);

            // 应用新成分
            Apply(realm, dimension, newComponent);

            // 改革动荡：稳定性下降
            realm.stability = UnityEngine.Mathf.Max(0f, realm.stability - 5f);

            // 编年史（重大）
            chronicle?.Add("reform",
                $"{realm.realmName} 政体改革：{oldName} → {newName}（稳定性下降）",
                major: true, realm.realmId);

            return true;
        }

        private static int GetCurrent(RealmData realm, PolityComponentInnovations.PolityDimension dimension)
        {
            var c = realm.composition;
            return dimension switch
            {
                PolityComponentInnovations.PolityDimension.SupremeSuccession => c.supremeSuccession.primary,
                PolityComponentInnovations.PolityDimension.SupremeScope => c.supremeScope.primary,
                PolityComponentInnovations.PolityDimension.CentralSuccession => c.centralSuccession.primary,
                PolityComponentInnovations.PolityDimension.CentralInstitution => c.centralInstitution.primary,
                PolityComponentInnovations.PolityDimension.LocalSuccession => c.localSuccession.primary,
                PolityComponentInnovations.PolityDimension.LocalScope => c.localScope.primary,
                PolityComponentInnovations.PolityDimension.SpatialStructure => c.spatialStructure.primary,
                _ => -1
            };
        }

        private static void Apply(RealmData realm, PolityComponentInnovations.PolityDimension dimension, int component)
        {
            var c = realm.composition;
            switch (dimension)
            {
                case PolityComponentInnovations.PolityDimension.SupremeSuccession:
                    c.supremeSuccession.primary = component; break;
                case PolityComponentInnovations.PolityDimension.SupremeScope:
                    c.supremeScope.primary = component; break;
                case PolityComponentInnovations.PolityDimension.CentralSuccession:
                    c.centralSuccession.primary = component; break;
                case PolityComponentInnovations.PolityDimension.CentralInstitution:
                    c.centralInstitution.primary = component; break;
                case PolityComponentInnovations.PolityDimension.LocalSuccession:
                    c.localSuccession.primary = component; break;
                case PolityComponentInnovations.PolityDimension.LocalScope:
                    c.localScope.primary = component; break;
                case PolityComponentInnovations.PolityDimension.SpatialStructure:
                    c.spatialStructure.primary = component; break;
            }
        }

        private static string GetComponentName(PolityComponentInnovations.PolityDimension dimension, int component)
        {
            return dimension switch
            {
                PolityComponentInnovations.PolityDimension.SupremeSuccession => GovernmentComponentNames.NameSupremeSuccession(component),
                PolityComponentInnovations.PolityDimension.SupremeScope => GovernmentComponentNames.NameSupremeScope(component),
                PolityComponentInnovations.PolityDimension.CentralSuccession => GovernmentComponentNames.NameCentralSuccession(component),
                PolityComponentInnovations.PolityDimension.CentralInstitution => GovernmentComponentNames.NameCentralInstitution(component),
                PolityComponentInnovations.PolityDimension.LocalSuccession => GovernmentComponentNames.NameLocalSuccession(component),
                PolityComponentInnovations.PolityDimension.LocalScope => GovernmentComponentNames.NameLocalScope(component),
                PolityComponentInnovations.PolityDimension.SpatialStructure => GovernmentComponentNames.NameSpatialStructure(component),
                _ => "?"
            };
        }
    }
}
