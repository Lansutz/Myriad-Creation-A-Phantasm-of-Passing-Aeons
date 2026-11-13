using MyriadCreation.Core.Contracts;

namespace MyriadCreation.Simulation.Politics
{
public readonly struct ConsolidateRealmCommand(int realmId) : ISimulationCommand
{
	public readonly int RealmId = realmId;
}
}
