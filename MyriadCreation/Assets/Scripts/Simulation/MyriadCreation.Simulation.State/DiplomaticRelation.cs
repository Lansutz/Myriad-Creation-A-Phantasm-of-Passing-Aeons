using System;
using System.Collections.Generic;
using MyriadCreation.Core.Enums;
using MyriadCreation.Simulation.Diplomacy;
using UnityEngine;
using MyriadCreation.Content.Definitions;

namespace MyriadCreation.Simulation.State
{
[Serializable]
public class DiplomaticRelation
{
	public int realmAId;

	public int realmBId;

	[Range(-100f, 100f)]
	public float relation;

	[Range(0f, 100f)]
	public float trust = 50f;

	[Range(0f, 100f)]
	public float threat = 50f;

	public bool isAtWar;

	public bool hasTradeEmbargo;

	public bool hasDiplomaticRelations = true;

	public int warDeclaredDay = -1;

	public int truceUntilDay = -1;

	[Range(0f, 100f)]
	public float hostilityLevel;

	public int hostileSinceDay = -1;

	public int lastSurpriseAttackerId = -1;

	public int lastSurpriseAttackDay = -1;

	public GameEnums.ConflictLevel conflictLevel;

	public int lastRaidAttackerId = -1;

	public int lastRaidDay = -1;

	public int raidCount;

	public List<CasusBelli> casusBelliList = new List<CasusBelli>();

	public List<WarGoal> activeWarGoals = new List<WarGoal>();

	public List<DiplomaticEvent> eventHistory = new List<DiplomaticEvent>();

	public List<Alliance> activeAlliances = new List<Alliance>();

	public List<Treaty> activeTreaties = new List<Treaty>();

	public Subordination subordination;

	public SpecialBondType specialBond;

	public bool IsHostile
	{
		get
		{
			if (!(hostilityLevel >= 50f))
			{
				return isAtWar;
			}
			return true;
		}
	}

	public void SetSpecialBond(SpecialBondType bond)
	{
		specialBond = bond;
	}

	public void ClearSpecialBond()
	{
		specialBond = SpecialBondType.None;
	}

	public SubordinationType? GetSovereigntyStatus(int selfId)
	{
		if (subordination == null || !subordination.isActive)
		{
			return null;
		}
		if (subordination.suzerainId != selfId)
		{
			return subordination.type;
		}
		return null;
	}

	public List<Alliance> GetTreatyObligations()
	{
		return activeAlliances;
	}

	public bool HasTreatyObligation(AllianceType type)
	{
		foreach (Alliance activeAlliance in activeAlliances)
		{
			if (activeAlliance.type == type && activeAlliance.isActive)
			{
				return true;
			}
		}
		return false;
	}

	public float CalculateOverallAttitude()
	{
		return relation * 0.5f + (trust - 50f) * 0.6f - (threat - 50f) * 0.4f;
	}

	public bool IsWillingToNegotiate()
	{
		if (hasDiplomaticRelations)
		{
			return CalculateOverallAttitude() > -60f;
		}
		return false;
	}

	public void AddEvent(DiplomaticEvent evt)
	{
		eventHistory.Add(evt);
		if (eventHistory.Count > 100)
		{
			eventHistory.RemoveAt(0);
		}
	}

	public void DailyDecay()
	{
		relation = Mathf.Lerp(relation, 0f, 0.001f);
		trust = Mathf.Lerp(trust, 50f, 0.0005f);
		threat = Mathf.Lerp(threat, 50f, 0.001f);
	}
}
}
