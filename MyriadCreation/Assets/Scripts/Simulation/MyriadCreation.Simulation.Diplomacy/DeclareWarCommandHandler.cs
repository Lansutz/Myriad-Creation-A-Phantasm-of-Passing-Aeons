using System;
using MyriadCreation.Core.Contracts;

namespace MyriadCreation.Simulation.Diplomacy
{
public sealed class DeclareWarCommandHandler : ISimulationCommandHandler<DeclareWarCommand>
{
	private readonly Func<int, int, string, int, bool> _declareWar;

	public DeclareWarCommandHandler(Func<int, int, string, int, bool> declareWar)
	{
		_declareWar = declareWar ?? throw new ArgumentNullException("declareWar");
	}

	public CommandResult Handle(DeclareWarCommand command)
	{
		if (!_declareWar(command.AttackerRealmId, command.DefenderRealmId, command.Reason, command.ActorCharacterId))
		{
			return CommandResult.Failed("war_declaration_rejected");
		}
		return CommandResult.Succeeded("war_declared");
	}
}
}
