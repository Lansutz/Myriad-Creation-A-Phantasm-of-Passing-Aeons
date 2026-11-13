using System;
using MyriadCreation.Content.Definitions;
using System.Collections.Generic;

namespace MyriadCreation.Simulation.Genetics
{
public sealed class PhenotypeResult
{
	public string[] traitIds = Array.Empty<string>();

	public float[] traitValues = Array.Empty<float>();

	public float polygenicScore;

	public float fitness = 1f;

	public bool viable = true;

	public List<string> expressedDiscreteTraits = new List<string>();

	public string[] appearanceIds = Array.Empty<string>();

	public float[] appearanceValues = Array.Empty<float>();

	public float inbreedingDepression;

	/// <summary>离散性状（wingType/hornType/skinType…）；由显性等位基因决定。</summary>
	public string[] discreteTraitIds = Array.Empty<string>();

	public string[] discreteTraitValues = Array.Empty<string>();

	public BodyPlanPhenotype bodyPlan;

	public IntegumentPhenotype integument;

	public MetabolicPhenotype metabolism;

	public float GetAppearance(string channelId)
	{
		if (string.IsNullOrEmpty(channelId) || appearanceIds == null)
		{
			return 0f;
		}
		for (int i = 0; i < appearanceIds.Length; i++)
		{
			if (appearanceIds[i] == channelId)
			{
				return appearanceValues[i];
			}
		}
		return 0f;
	}

	/// <summary>取离散性状值；缺省返回空串（调用方自行给默认值）。</summary>
	public string GetDiscrete(string traitId)
	{
		if (string.IsNullOrEmpty(traitId) || discreteTraitIds == null) return "";
		for (int i = 0; i < discreteTraitIds.Length; i++)
		{
			if (discreteTraitIds[i] == traitId) return discreteTraitValues[i];
		}
		return "";
	}

	public float GetTrait(string traitId)
	{
		for (int i = 0; i < traitIds.Length; i++)
		{
			if (traitIds[i] == traitId)
			{
				return traitValues[i];
			}
		}
		return 0f;
	}
}
}