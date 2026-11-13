using System;

namespace MyriadCreation.Simulation.Laws
{
[Serializable]
public class LawEffect
{
	public float taxMultiplier = 1f;

	public float stabilityPerYear;

	public float legitimacyPerYear;

	public float manpowerMultiplier = 1f;

	public float unrestPerYear;
}
}
