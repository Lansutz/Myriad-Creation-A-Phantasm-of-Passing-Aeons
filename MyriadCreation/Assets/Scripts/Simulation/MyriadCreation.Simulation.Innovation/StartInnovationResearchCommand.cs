using MyriadCreation.Core.Contracts;

namespace MyriadCreation.Simulation.Innovation
{
public readonly struct StartInnovationResearchCommand(int realmId, int innovationId) : ISimulationCommand
{
	public readonly int RealmId = realmId;

	public readonly int InnovationId = innovationId;
}
}
