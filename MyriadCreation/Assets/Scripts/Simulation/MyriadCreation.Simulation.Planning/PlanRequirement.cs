using System;

namespace MyriadCreation.Simulation.Planning
{
[Serializable]
public struct PlanRequirement(string key, float amount)
{
	public string key = key ?? string.Empty;

	public float amount = amount;
}
}
