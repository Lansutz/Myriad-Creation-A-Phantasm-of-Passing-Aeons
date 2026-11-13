using System;
using System.Collections.Generic;
using UnityEngine.Serialization;
using MyriadCreation.Core.Enums;

namespace MyriadCreation.World.Settlement
{
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
			throw new ArgumentOutOfRangeException("mode", mode, "Unknown site mode.");
		}
	}

	public bool HasActiveModeData()
	{
		return siteMode switch
		{
			SiteMode.SedentarySettlement => sedentarySettlementData != null, 
			SiteMode.FortifiedOutpost => fortifiedOutpostData != null, 
			SiteMode.TemporaryEncampment => temporaryEncampmentData != null, 
			_ => false, 
		};
	}
}
}
