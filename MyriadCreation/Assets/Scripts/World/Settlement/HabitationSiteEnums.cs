namespace MyriadCreation.World.Settlement
{
    public enum SiteMode
    {
        SedentarySettlement,
        FortifiedOutpost,
        TemporaryEncampment
    }

    public static class SiteModeKeys
    {
        public const string SedentarySettlement = "sedentary_settlement";
        public const string FortifiedOutpost = "fortified_outpost";
        public const string TemporaryEncampment = "temporary_encampment";

        public static string ToKey(SiteMode mode)
        {
            switch (mode)
            {
                case SiteMode.SedentarySettlement:
                    return SedentarySettlement;
                case SiteMode.FortifiedOutpost:
                    return FortifiedOutpost;
                case SiteMode.TemporaryEncampment:
                    return TemporaryEncampment;
                default:
                    throw new System.ArgumentOutOfRangeException(nameof(mode), mode, "Unknown site mode.");
            }
        }

        public static bool TryParse(string key, out SiteMode mode)
        {
            switch (key)
            {
                case SedentarySettlement:
                    mode = SiteMode.SedentarySettlement;
                    return true;
                case FortifiedOutpost:
                    mode = SiteMode.FortifiedOutpost;
                    return true;
                case TemporaryEncampment:
                    mode = SiteMode.TemporaryEncampment;
                    return true;
                default:
                    mode = default;
                    return false;
            }
        }
    }

    public enum FacilityType
    {
        Wall,
        Gate,
        Trench,
        Palisade,
        Earthwork,
        Other
    }

    public enum ConstructionState
    {
        Planned,
        UnderConstruction,
        Complete,
        Ruined
    }

    public enum FacilityDamageState
    {
        Intact,
        Damaged,
        Disabled,
        Destroyed
    }

    public static class FacilityKeys
    {
        public static string ToFacilityTypeKey(FacilityType type)
        {
            switch (type)
            {
                case FacilityType.Wall: return "wall";
                case FacilityType.Gate: return "gate";
                case FacilityType.Trench: return "trench";
                case FacilityType.Palisade: return "palisade";
                case FacilityType.Earthwork: return "earthwork";
                case FacilityType.Other: return "other";
                default: throw new System.ArgumentOutOfRangeException(nameof(type), type, "Unknown facility type.");
            }
        }

        public static bool TryParseFacilityType(string key, out FacilityType type)
        {
            switch (key)
            {
                case "wall": type = FacilityType.Wall; return true;
                case "gate": type = FacilityType.Gate; return true;
                case "trench": type = FacilityType.Trench; return true;
                case "palisade": type = FacilityType.Palisade; return true;
                case "earthwork": type = FacilityType.Earthwork; return true;
                case "other": type = FacilityType.Other; return true;
                default: type = default; return false;
            }
        }

        public static string ToConstructionStateKey(ConstructionState state)
        {
            switch (state)
            {
                case ConstructionState.Planned: return "planned";
                case ConstructionState.UnderConstruction: return "under_construction";
                case ConstructionState.Complete: return "complete";
                case ConstructionState.Ruined: return "ruined";
                default: throw new System.ArgumentOutOfRangeException(nameof(state), state, "Unknown construction state.");
            }
        }

        public static bool TryParseConstructionState(string key, out ConstructionState state)
        {
            switch (key)
            {
                case "planned": state = ConstructionState.Planned; return true;
                case "under_construction": state = ConstructionState.UnderConstruction; return true;
                case "complete": state = ConstructionState.Complete; return true;
                case "ruined": state = ConstructionState.Ruined; return true;
                default: state = default; return false;
            }
        }

        public static string ToDamageStateKey(FacilityDamageState state)
        {
            switch (state)
            {
                case FacilityDamageState.Intact: return "intact";
                case FacilityDamageState.Damaged: return "damaged";
                case FacilityDamageState.Disabled: return "disabled";
                case FacilityDamageState.Destroyed: return "destroyed";
                default: throw new System.ArgumentOutOfRangeException(nameof(state), state, "Unknown facility damage state.");
            }
        }

        public static bool TryParseDamageState(string key, out FacilityDamageState state)
        {
            switch (key)
            {
                case "intact": state = FacilityDamageState.Intact; return true;
                case "damaged": state = FacilityDamageState.Damaged; return true;
                case "disabled": state = FacilityDamageState.Disabled; return true;
                case "destroyed": state = FacilityDamageState.Destroyed; return true;
                default: state = default; return false;
            }
        }
    }
}
