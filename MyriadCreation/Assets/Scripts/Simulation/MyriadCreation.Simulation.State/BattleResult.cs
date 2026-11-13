using System;
using MyriadCreation.Simulation.Systems;

namespace MyriadCreation.Simulation.State
{
[Serializable]
public struct BattleResult
{
	public bool attackerWins;

	public float attackerLosses;

	public float defenderLosses;

	public bool battleEndedByManpower;

	public bool attackerRetreated;

	public bool defenderRetreated;

	public StampedeEvent attackerStampede;

	public StampedeEvent defenderStampede;
}
}
