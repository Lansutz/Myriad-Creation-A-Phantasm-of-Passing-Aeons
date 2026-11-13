using MyriadCreation.Core.Contracts;

namespace MyriadCreation.Simulation.Economy
{
public readonly struct ImproveRealmEconomyCommand(int realmId) : ISimulationCommand
{
	public readonly int RealmId = realmId;
}
}
