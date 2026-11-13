using System.Collections.Generic;

namespace MyriadCreation.Simulation.Laws
{
    /// <summary>
    /// 把现行 catalog 法律回写到政体组件（LawState → LawComposition）。
    /// 只映射语义唯一的组；central_institution 等一法多组件组不从法律反猜。
    /// </summary>
    public static class LawCompositionDeriver
    {
        private static readonly Dictionary<string, int> Succession = new Dictionary<string, int>
        {
            { "succ_hereditary", 0 },
            { "succ_elective", 1 },
            { "succ_usurpation", 3 },
            { "succ_rotation", 4 }
        };

        private static readonly Dictionary<string, int> SupremeScope = new Dictionary<string, int>
        {
            { "scope_absolute", 0 },
            { "scope_legally_bound", 1 },
            { "scope_custom_bound", 2 },
            { "scope_consensual", 3 },
            { "scope_divinely_bound", 4 }
        };

        private static readonly Dictionary<string, int> CentralSuccession = new Dictionary<string, int>
        {
            { "cent_succ_appointed", 0 },
            { "cent_succ_elected", 1 },
            { "cent_succ_examination", 2 },
            { "cent_succ_hereditary", 3 }
        };

        private static readonly Dictionary<string, int> LocalSuccession = new Dictionary<string, int>
        {
            { "local_succ_appointed", 0 },
            { "local_succ_elected", 1 },
            { "local_succ_hereditary", 3 },
            { "local_succ_charter", 4 }
        };

        private static readonly Dictionary<string, int> LocalScope = new Dictionary<string, int>
        {
            { "local_scope_full_autonomy", 0 },
            { "local_scope_fiscal_judicial", 1 },
            { "local_scope_military_only", 2 },
            { "local_scope_none", 3 }
        };

        private static readonly Dictionary<string, int> Spatial = new Dictionary<string, int>
        {
            { "spatial_unitary", 0 },
            { "spatial_federal", 1 },
            { "spatial_confederal", 2 }
        };

        public static void Apply(RealmLawState state, LawComposition laws)
        {
            if (state == null || laws == null) return;
            ApplyChoice(state.GetActiveLaw("succession"), Succession, laws.supremeSuccession);
            ApplyChoice(state.GetActiveLaw("supreme_scope"), SupremeScope, laws.supremeScope);
            ApplyChoice(state.GetActiveLaw("central_succession"), CentralSuccession, laws.centralSuccession);
            ApplyChoice(state.GetActiveLaw("local_succession"), LocalSuccession, laws.localSuccession);
            ApplyChoice(state.GetActiveLaw("local_scope"), LocalScope, laws.localScope);
            ApplyChoice(state.GetActiveLaw("spatial"), Spatial, laws.spatialStructure);
        }

        private static void ApplyChoice(string lawId, Dictionary<string, int> map, ComponentChoice choice)
        {
            if (string.IsNullOrEmpty(lawId) || choice == null) return;
            if (map.TryGetValue(lawId, out int component)) choice.primary = component;
        }
    }
}
