using System;
using MyriadCreation.Content.Definitions;

namespace MyriadCreation.Simulation.State
{
[Serializable]
public class NegotiationClause
{
	public TreatyClauseType type;

	public string description;

	public int cost;

	public float value;

	public int durationDays = 1825;

	public bool isDemanded = true;
}
}
