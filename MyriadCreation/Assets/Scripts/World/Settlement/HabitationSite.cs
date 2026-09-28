using System;
using System.Collections.Generic;
using UnityEngine.Serialization;

namespace MyriadCreation.World.Settlement
{
    [Serializable]
    public sealed class SiteLocation
    {
        public float x;
        public float y;
    }

    [Serializable]
    public sealed class SedentarySettlementData
    {
        public string settlementMorphology = string.Empty;
        public int developmentStage;
    }

    [Serializable]
    public sealed class FortifiedOutpostData
    {
        public string subtype = string.Empty;
        public string purpose = string.Empty;
    }

    [Serializable]
    public sealed class TemporaryEncampmentData
    {
        public string encampmentKind = string.Empty;
        public int establishedDay;
    }

    [Serializable]
    public class HabitationSite
    {
        public string siteId = Guid.NewGuid().ToString("D");
        public SiteMode siteMode;
        public string siteName = string.Empty;
        public SiteLocation location = new SiteLocation();
        [FormerlySerializedAs("population")]
        public float population;
        public string siteClass = string.Empty;
        public string controlFaction = string.Empty;

        // Internal spatial reference only; site mode and business data live on this object.
        [FormerlySerializedAs("anchorId")]
        public int anchorId = -1;

        public bool hasPreviousSiteMode;
        public SiteMode previousSiteMode;
        public bool hasPendingSiteMode;
        public SiteMode pendingSiteMode;

        public SedentarySettlementData sedentarySettlementData;
        public FortifiedOutpostData fortifiedOutpostData;
        public TemporaryEncampmentData temporaryEncampmentData;
        public List<FacilityData> facilities = new List<FacilityData>();

        public HabitationSite()
        {
            InitializeModeData(siteMode);
        }

        public HabitationSite(SiteMode mode)
        {
            siteMode = mode;
            InitializeModeData(mode);
        }

        public void InitializeModeData(SiteMode mode)
        {
            switch (mode)
            {
                case SiteMode.SedentarySettlement:
                    sedentarySettlementData = sedentarySettlementData ?? new SedentarySettlementData();
                    break;
                case SiteMode.FortifiedOutpost:
                    fortifiedOutpostData = fortifiedOutpostData ?? new FortifiedOutpostData();
                    break;
                case SiteMode.TemporaryEncampment:
                    temporaryEncampmentData = temporaryEncampmentData ?? new TemporaryEncampmentData();
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(mode), mode, "Unknown site mode.");
            }
        }

        public bool HasActiveModeData()
        {
            switch (siteMode)
            {
                case SiteMode.SedentarySettlement:
                    return sedentarySettlementData != null;
                case SiteMode.FortifiedOutpost:
                    return fortifiedOutpostData != null;
                case SiteMode.TemporaryEncampment:
                    return temporaryEncampmentData != null;
                default:
                    return false;
            }
        }
    }
}
