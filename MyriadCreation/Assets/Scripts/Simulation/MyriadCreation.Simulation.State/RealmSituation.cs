using System;
using System.Collections.Generic;
using MyriadCreation.Core.Enums;
using UnityEngine;

namespace MyriadCreation.Simulation.State
{
[Serializable]
public class RealmSituation
{
	public int realmId;

	[Range(0f, 1.5f)]
	public float foodSecurity = 1f;

	[Range(0f, 100f)]
	public float publicOrder = 60f;

	public bool atWar;

	public bool warOnHomeSoil;

	[Range(0f, 100f)]
	public float disasterSeverity;

	[NonSerialized]
	public Dictionary<GameEnums.SocialClass, float> taxPain = new Dictionary<GameEnums.SocialClass, float>();

	[NonSerialized]
	public Dictionary<GameEnums.SocialClass, float> politicalAccess = new Dictionary<GameEnums.SocialClass, float>();

	[Range(0f, 1f)]
	public float tradeFlow = 1f;

	[Range(0f, 1f)]
	public float monetaryStability = 1f;

	[NonSerialized]
	public Dictionary<GameEnums.SocialClass, bool> classRecognized = new Dictionary<GameEnums.SocialClass, bool>();

	[Range(0f, 100f)]
	public float stability = 50f;

	[Range(0f, 100f)]
	public float legitimacy = 50f;

	[Range(0f, 1f)]
	public float privilegeSecurity = 1f;

	public float GetTaxPain(GameEnums.SocialClass cls)
	{
		if (taxPain == null)
		{
			return 20f;
		}
		return taxPain.GetValueOrDefault(cls, 20f);
	}

	public float GetPoliticalAccess(GameEnums.SocialClass cls)
	{
		if (politicalAccess == null)
		{
			return 0.2f;
		}
		return politicalAccess.GetValueOrDefault(cls, 0.2f);
	}

	public bool IsRecognized(GameEnums.SocialClass cls)
	{
		if (classRecognized != null)
		{
			return classRecognized.GetValueOrDefault(cls, defaultValue: true);
		}
		return true;
	}
}
}
