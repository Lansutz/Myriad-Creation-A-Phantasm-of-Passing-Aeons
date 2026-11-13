using MyriadCreation.Core.Contracts;
using MyriadCreation.Core.Events;
using MyriadCreation.Simulation.Systems;

namespace MyriadCreation.Simulation.Settlement
{
public sealed class DestroySettlementCommandHandler : ISimulationCommandHandler<DestroySettlementCommand>
{
	private readonly SettlementDestructionRuntime _runtime;

	private readonly SimulationEventBus _events;

	public DestroySettlementCommandHandler(SettlementDestructionRuntime runtime, SimulationEventBus events)
	{
		_runtime = runtime;
		_events = events;
	}

	public CommandResult Handle(DestroySettlementCommand command)
	{
		if (string.IsNullOrEmpty(_runtime.DestroySettlement(command.SettlementAnchorId, command.Method, command.AttackerRealmId)))
		{
			return CommandResult.Failed("settlement_not_found");
		}
		_events.Publish(new SettlementDestroyedEvent(command.SettlementAnchorId, command.Method, command.AttackerRealmId));
		return CommandResult.Succeeded("settlement_destroyed");
	}
}
}
