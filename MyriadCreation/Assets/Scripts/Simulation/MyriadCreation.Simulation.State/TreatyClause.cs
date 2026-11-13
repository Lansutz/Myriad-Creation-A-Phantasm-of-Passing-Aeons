using System;
using MyriadCreation.Content.Definitions;

namespace MyriadCreation.Simulation.State
{
[Serializable]
public struct TreatyClause
{
	public TreatyClauseType type;

	public string description;

	public float value;

	public int targetRealmId;

	public int fromRealmId;

	public int toRealmId;

	public int tileIndex;

	public int regionId;

	public int durationDays;
}
}
