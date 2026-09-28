using System;

namespace MyriadCreation.World.Settlement
{
    public static class HabitationSiteMigration
    {
        public static string CreateLegacySiteId(int anchorId)
        {
            return new Guid(anchorId, 0, 0, new byte[8]).ToString("D");
        }

        public static string ResolveSiteId(string storedSiteId, int legacyAnchorId)
        {
            if (string.IsNullOrEmpty(storedSiteId))
                return CreateLegacySiteId(legacyAnchorId);
            if (!Guid.TryParse(storedSiteId, out var siteGuid))
                throw new FormatException($"Invalid habitation site UUID '{storedSiteId}'.");
            return siteGuid.ToString("D");
        }

        public static HabitationSite FromLegacy(SettlementData legacy, string stableSiteId)
        {
            if (legacy == null)
                throw new ArgumentNullException(nameof(legacy));
            if (!Guid.TryParse(stableSiteId, out var parsedSiteId))
                throw new ArgumentException("A persisted UUID must be supplied by the caller.", nameof(stableSiteId));

            var mode = MapLegacyMode(legacy.settlementCategory);
            var site = new HabitationSite(mode)
            {
                siteId = parsedSiteId.ToString("D"),
                siteName = legacy.settlementName,
                location = new SiteLocation { x = legacy.x, y = legacy.y },
                population = legacy.population,
                anchorId = legacy.anchorId
            };

            if (mode == SiteMode.SedentarySettlement)
            {
                site.sedentarySettlementData.settlementMorphology = MapLegacyMorphology(legacy.settlementForm);
                site.sedentarySettlementData.developmentStage = (int)legacy.settlementLevel;
            }
            else if (mode == SiteMode.FortifiedOutpost)
            {
                site.fortifiedOutpostData.subtype = legacy.fortSubtype.ToString();
                site.fortifiedOutpostData.purpose = legacy.primaryFunction.ToString();
            }

            return site;
        }

        private static SiteMode MapLegacyMode(SettlementCategory category)
        {
            switch (category)
            {
                case SettlementCategory.PermanentSettlement:
                    return SiteMode.SedentarySettlement;
                case SettlementCategory.Outpost:
                    return SiteMode.FortifiedOutpost;
                case SettlementCategory.Camp:
                    return SiteMode.TemporaryEncampment;
                default:
                    throw new ArgumentOutOfRangeException(nameof(category), category, "Unknown legacy settlement category.");
            }
        }

        private static string MapLegacyMorphology(SettlementForm form)
        {
            switch (form)
            {
                case SettlementForm.Village:
                    return "village";
                case SettlementForm.City:
                    return "city";
                default:
                    throw new ArgumentOutOfRangeException(nameof(form), form, "Unknown legacy settlement morphology.");
            }
        }
    }
}
