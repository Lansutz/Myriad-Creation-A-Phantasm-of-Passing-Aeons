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