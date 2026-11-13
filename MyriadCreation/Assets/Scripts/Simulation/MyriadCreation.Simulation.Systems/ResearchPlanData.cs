using System;

namespace MyriadCreation.Simulation.Systems
{
[Serializable]
public sealed class ResearchPlanData
{
	public int planId;

	public int characterId;

	public int realmId;

	public int innovationId;

	public float relevance;

	public bool discovered;

	public bool formallyUnlocked;

	public float verificationProgress;
}
}
