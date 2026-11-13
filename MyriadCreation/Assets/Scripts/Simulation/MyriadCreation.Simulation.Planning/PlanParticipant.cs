using System;

namespace MyriadCreation.Simulation.Planning
{
[Serializable]
public struct PlanParticipant(int characterId, string role = null)
{
	public int characterId = characterId;

	public string role = role ?? string.Empty;
}
}
