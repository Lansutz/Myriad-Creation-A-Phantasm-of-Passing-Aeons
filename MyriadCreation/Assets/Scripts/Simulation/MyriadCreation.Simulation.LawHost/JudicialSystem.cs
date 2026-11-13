using System;
using System.Collections.Generic;
using MyriadCreation.Simulation.State;
using UnityEngine;
using Random = UnityEngine.Random;
using MyriadCreation.Simulation.Laws;
using MyriadCreation.Content.Definitions;

namespace MyriadCreation.Simulation.LawHost
{
[Serializable]
public class JudicialSystem
{
	public int judicialSystemId;

	public string judicialSystemName;

	public LawSource source;

	public List<Law> laws = new List<Law>();

	public float judicialEfficiency = 0.5f;

	public float corruption = 0.2f;

	public float lawEnforcement = 0.5f;

	public float severity = 0.5f;

	public bool useCapitalPunishment = true;

	public bool useCorporalPunishment = true;

	public bool useFines = true;

	public bool useImprisonment = true;

	public bool useExile = true;

	public bool useSlavery = true;

	public bool IsCrimeCited(CrimeType crime)
	{
		bool flag = false;
		foreach (Law law in laws)
		{
			if (law.isActive)
			{
				flag = true;
				if (law.citedCrimes != null && law.citedCrimes.Contains(crime))
				{
					return true;
				}
			}
		}
		CrimeDefinition definition;
		if (!flag)
		{
			return CrimeRegistry.TryGet(crime, out definition);
		}
		return false;
	}

	public TrialResult TrialCrime(CrimeType crime, int suspectCharacterId, int realmId, RealmData realm)
	{
		TrialResult result = default;
		if (!CrimeRegistry.TryGet(crime, out var definition))
		{
			result.verdict = VerdictType.Dismissed;
			return result;
		}
		if (!IsCrimeCited(crime))
		{
			result.verdict = VerdictType.Dismissed;
			return result;
		}
		float num = judicialEfficiency * (1f - corruption) * lawEnforcement * 0.8f;
		result.verdict = ((!(Random.value < num)) ? VerdictType.NotGuilty : VerdictType.Guilty);
		if (result.verdict == VerdictType.Guilty)
		{
			result.punishment = ApplyPunishmentSwitch(definition.defaultPunishment);
			result.severity = definition.baseSeverity * severity;
			if (result.punishment == PunishmentType.Fine)
			{
				result.fineAmount = definition.baseSeverity * 10f;
			}
		}
		return result;
	}

	private PunishmentType ApplyPunishmentSwitch(PunishmentType basePunishment)
	{
		switch (basePunishment)
		{
		case PunishmentType.Death:
			if (!useCapitalPunishment)
			{
				if (!useCorporalPunishment)
				{
					return PunishmentType.Fine;
				}
				return PunishmentType.Corporal;
			}
			return basePunishment;
		case PunishmentType.Corporal:
			if (!useCorporalPunishment)
			{
				return PunishmentType.Fine;
			}
			return basePunishment;
		case PunishmentType.Exile:
			if (!useExile)
			{
				return PunishmentType.Imprisonment;
			}
			return basePunishment;
		case PunishmentType.Slavery:
			if (!useSlavery)
			{
				return PunishmentType.Imprisonment;
			}
			return basePunishment;
		case PunishmentType.Fine:
			if (!useFines)
			{
				return PunishmentType.Imprisonment;
			}
			return basePunishment;
		case PunishmentType.Imprisonment:
			if (!useImprisonment)
			{
				return PunishmentType.PublicHumiliation;
			}
			return basePunishment;
		default:
			return basePunishment;
		}
	}

	public void DailyTick()
	{
		judicialEfficiency = Mathf.Clamp(judicialEfficiency + Random.Range(-0.001f, 0.001f), 0.1f, 1f);
	}
}
}
