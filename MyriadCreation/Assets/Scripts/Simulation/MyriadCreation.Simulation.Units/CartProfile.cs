using System;

namespace MyriadCreation.Simulation.Units
{
[Serializable]
public struct CartProfile
{
	public float capacity;

	public int requiredDraftStrength;

	public bool canBePulledByPeople;

	public float speedPenalty;
}
}
