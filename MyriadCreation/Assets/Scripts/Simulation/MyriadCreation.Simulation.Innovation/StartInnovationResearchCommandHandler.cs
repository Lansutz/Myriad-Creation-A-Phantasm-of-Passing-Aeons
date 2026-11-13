using System;
using MyriadCreation.Core.Contracts;
using MyriadCreation.Simulation.State;
using MyriadCreation.Simulation.WorldState;

namespace MyriadCreation.Simulation.Innovation
{
public sealed class StartInnovationResearchCommandHandler : ISimulationCommandHandler<StartInnovationResearchCommand>
{
	private readonly InnovationTree _innovations;

	private readonly GameWorld _world;

	public StartInnovationResearchCommandHandler(InnovationTree innovations, GameWorld world = null)
	{
		_innovations = innovations ?? throw new ArgumentNullException("innovations");
		_world = world;
	}

	public CommandResult Handle(StartInnovationResearchCommand command)
	{
		if (!_innovations.StartResearch(command.RealmId, command.InnovationId, _world))
		{
			return CommandResult.Failed("research_rejected");
		}
		return CommandResult.Succeeded("research_started");
	}
}
}
