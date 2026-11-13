using System;
using System.Collections.Generic;
using MyriadCreation.Simulation.WorldState;
using MyriadCreation.World.Anchor;
using MyriadCreation.World.Settlement;
using UnityEngine;
using MyriadCreation.Core.Enums;

namespace MyriadCreation.Simulation.Actors
{
[Serializable]
public class CampData : HabitationSite
{
	public int campId;

	private CampType _type;

	public int tileIndex;

	public int ownerRealmId = -1;

	public int ownerActorId = -1;

	[NonSerialized]
	public AnchorData anchor;

	public float defense = 10f;

	public float supplies = 100f;

	public float morale = 80f;

	public int establishedDay;

	public float permanence;

	public bool isAbandoned;

	public Dictionary<string, float> attributes = new Dictionary<string, float>();

	public string campName
	{
		get
		{
			return siteName;
		}
		set
		{
			siteName = value;
		}
	}

	public CampType type
	{
		get
		{
			return _type;
		}
		set
		{
			_type = value;
			InitializeModeData(siteMode);
			if (siteMode == SiteMode.TemporaryEncampment)
			{
				temporaryEncampmentData.encampmentKind = value.ToString();
			}
			else if (siteMode == SiteMode.FortifiedOutpost)
			{
				fortifiedOutpostData.purpose = value.ToString();
			}
		}
	}

	public bool CanEvolveToSettlement
	{
		get
		{
			if (permanence >= 100f && type != CampType.Nomad && type != CampType.Temporary)
			{
				return population >= 50f;
			}
			return false;
		}
	}

	public CampData()
	{
		siteMode = SiteMode.TemporaryEncampment;
		InitializeModeData(siteMode);
	}

	public void Tick(GameWorld world, float deltaDays)
	{
		if (isAbandoned)
		{
			return;
		}
		float num = population * 0.1f * deltaDays;
		supplies -= num;
		if (supplies <= 0f)
		{
			morale -= deltaDays * 2f;
			population = Mathf.Max(0f, population - (float)Mathf.RoundToInt(deltaDays));
			if (population <= 0f || morale <= 0f)
			{
				isAbandoned = true;
				population = 0f;
				Debug.Log((object)$"[Camp] 营寨#{campId}({type}) 因补给耗尽被废弃，地块={tileIndex}");
				return;
			}
		}
		if (supplies > 50f && population > 20f)
		{
			permanence += deltaDays * 0.05f;
			if (permanence >= 100f)
			{
				permanence = 100f;
			}
		}
		if (type == CampType.Nomad)
		{
			permanence = 0f;
		}
	}
}
}
