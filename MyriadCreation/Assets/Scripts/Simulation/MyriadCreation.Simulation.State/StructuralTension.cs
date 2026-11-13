using System;
using System.Collections.Generic;
using MyriadCreation.Core.Enums;
using UnityEngine;

namespace MyriadCreation.Simulation.State
{
[Serializable]
public class StructuralTension
{
	[Range(0f, 100f)]
	public float classMismatch;

	[Range(0f, 100f)]
	public float fiscalMilitary;

	[Range(0f, 100f)]
	public float legitimacyErosion;

	[Range(0f, 100f)]
	public float total;

	public void Recalculate(RealmSociety society, RealmSituation sit, RealmData realm)
	{
		float num = 0f;
		float num2 = 0f;
		foreach (KeyValuePair<GameEnums.SocialClass, ClassProfile> @class in society.classes)
		{
			ClassProfile value = @class.Value;
			float politicalAccess = sit.GetPoliticalAccess(@class.Key);
			float num3 = 1f - Mathf.Clamp01(politicalAccess);
			float influence = value.influence;
			num += value.influence * num3 * (1f + (100f - value.satisfaction) / 200f);
			num2 += influence;
		}
		classMismatch = ((num2 > 0f) ? Mathf.Clamp(num / num2 * 100f, 0f, 100f) : 0f);
		float num4 = 0f;
		if (sit.atWar)
		{
			num4 += 25f;
		}
		if (sit.warOnHomeSoil)
		{
			num4 += 30f;
		}
		num4 += Mathf.Clamp((0f - realm.treasury) / 20f, 0f, 30f);
		float num5 = 0f;
		int num6 = 0;
		foreach (float value2 in sit.taxPain.Values)
		{
			num5 += value2;
			num6++;
		}
		if (num6 > 0)
		{
			num4 += num5 / (float)num6 * 0.15f;
		}
		fiscalMilitary = Mathf.Clamp(num4, 0f, 100f);
		legitimacyErosion = Mathf.Clamp((100f - sit.legitimacy) * 0.6f + (100f - sit.stability) * 0.4f, 0f, 100f);
		total = Mathf.Clamp(classMismatch * 0.4f + fiscalMilitary * 0.3f + legitimacyErosion * 0.3f, 0f, 100f);
	}
}
}
