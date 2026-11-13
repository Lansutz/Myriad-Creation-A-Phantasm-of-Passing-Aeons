using System;
using System.Collections.Generic;
using MyriadCreation.Core.Contracts;
using MyriadCreation.Simulation.State;

namespace MyriadCreation.Simulation.Warfare
{
public sealed class MilitaryBuildUpCommandHandler : ISimulationCommandHandler<MilitaryBuildUpCommand>
{
	private readonly Dictionary<int, RealmData> _realms;

	public MilitaryBuildUpCommandHandler(Dictionary<int, RealmData> realms)
	{
		_realms = realms ?? throw new ArgumentNullException("realms");
	}

	public CommandResult Handle(MilitaryBuildUpCommand command)
	{
		if (!_realms.TryGetValue(command.RealmId, out var value))
		{
			return CommandResult.Failed("realm_not_found");
		}
		if (command.Budget <= 0f || value.treasury < command.Budget)
		{
			return CommandResult.Failed("military_budget_rejected");
		}
		RealmTreasury.Spend(value, command.Budget);
		return CommandResult.Succeeded("military_budget_committed");
	}
}
}
