using System;
using System.Collections.Generic;
using MyriadCreation.Core.Contracts;
using MyriadCreation.Simulation.Diplomacy;
using MyriadCreation.Simulation.Economy;
using MyriadCreation.Simulation.Innovation;
using MyriadCreation.Simulation.Politics;
using MyriadCreation.Simulation.Warfare;
using MyriadCreation.Content.Definitions;

namespace MyriadCreation.Simulation.AI
{
public sealed class AIIntentExecutor
{
	private readonly SimulationCommandBus _commands;

	public AIIntentExecutor(SimulationCommandBus commands)
	{
		_commands = commands ?? throw new ArgumentNullException("commands");
	}

	public void Execute(IEnumerable<AIIntent> intents)
	{
		if (intents == null)
		{
			return;
		}
		foreach (AIIntent intent in intents)
		{
			switch (intent.Type)
			{
			case AIIntentType.StartResearch:
				_commands.Send(new StartInnovationResearchCommand(intent.ActorRealmId, intent.InnovationId));
				break;
			case AIIntentType.RaidSettlement:
				_commands.Send(new RaidSettlementCommand(intent.ActorRealmId, intent.TargetRealmId, intent.TargetTileIndex, intent.RaidType));
				break;
			case AIIntentType.DeclareWar:
				_commands.Send(new DeclareWarCommand(intent.ActorRealmId, intent.TargetRealmId, "领土扩张", intent.ActorCharacterId));
				break;
			case AIIntentType.ImproveEconomy:
				_commands.Send(new ImproveRealmEconomyCommand(intent.ActorRealmId));
				break;
			case AIIntentType.ProposeAlliance:
				_commands.Send(new ProposeAllianceCommand(intent.ActorRealmId, intent.TargetRealmId, intent.AllianceType));
				break;
			case AIIntentType.SendGift:
				_commands.Send(new SendGiftCommand(intent.ActorRealmId, intent.TargetRealmId, intent.Amount));
				break;
			case AIIntentType.ConsolidateRealm:
				_commands.Send(new ConsolidateRealmCommand(intent.ActorRealmId));
				break;
			case AIIntentType.MilitaryBuildUp:
				_commands.Send(new MilitaryBuildUpCommand(intent.ActorRealmId));
				break;
			}
		}
	}
}
}
