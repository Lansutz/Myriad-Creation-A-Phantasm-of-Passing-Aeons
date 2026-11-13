using System;
using MyriadCreation.Core.Contracts;
using MyriadCreation.Simulation.Systems;

namespace MyriadCreation.Simulation.Diplomacy
{
public sealed class SendGiftCommandHandler : ISimulationCommandHandler<SendGiftCommand>
{
	private readonly DiplomacyManager _diplomacy;

	public SendGiftCommandHandler(DiplomacyManager diplomacy)
	{
		_diplomacy = diplomacy ?? throw new ArgumentNullException("diplomacy");
	}

	public CommandResult Handle(SendGiftCommand command)
	{
		if (!_diplomacy.SendGift(command.FromRealmId, command.ToRealmId, command.Amount))
		{
			return CommandResult.Failed("gift_rejected");
		}
		return CommandResult.Succeeded("gift_sent");
	}
}
}
