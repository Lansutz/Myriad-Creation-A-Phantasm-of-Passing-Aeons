using System;

namespace MyriadCreation.Simulation.Genetics
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
