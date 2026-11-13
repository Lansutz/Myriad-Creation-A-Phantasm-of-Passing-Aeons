using MyriadCreation.Core.Contracts;

namespace MyriadCreation.Simulation.Warfare
{
public readonly struct MilitaryBuildUpCommand(int realmId, float budget = 50f) : ISimulationCommand
{
	public readonly int RealmId = realmId;

	public readonly float Budget = budget;
}
}
