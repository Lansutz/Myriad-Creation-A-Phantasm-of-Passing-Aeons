using System;
using MyriadCreation.Core.Contracts;
using MyriadCreation.Simulation.Systems;

namespace MyriadCreation.Simulation.Diplomacy
{
public sealed class ProposeAllianceCommandHandler : ISimulationCommandHandler<ProposeAllianceCommand>
{
	private readonly DiplomacyManager _diplomacy;

	public ProposeAllianceCommandHandler(DiplomacyManager diplomacy)
	{
		_diplomacy = diplomacy ?? throw new ArgumentNullException("diplomacy");
	}

	public CommandResult Handle(ProposeAllianceCommand command)
	{
		if (_diplomacy.ProposeAlliance(command.RealmAId, command.RealmBId, command.Type) == null)
		{
			return CommandResult.Failed("alliance_rejected");
		}
		return CommandResult.Succeeded("alliance_proposed");
	}
}
}
