using System;

namespace MyriadCreation.Content.Definitions
{
[Serializable]
public class AlleleTraitEffect
{
	public string traitId = "";

	public float additiveEffect;

	public float dominanceDeviation;

	public float environmentalSensitivity;

	public AppearanceChannel appearanceChannel;

	public float appearanceAmount;
}
}
