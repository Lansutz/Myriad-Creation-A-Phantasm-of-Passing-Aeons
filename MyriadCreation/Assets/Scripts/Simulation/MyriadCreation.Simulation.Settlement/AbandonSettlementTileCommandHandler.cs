using MyriadCreation.Core.Contracts;
using MyriadCreation.Core.Events;
using MyriadCreation.Simulation.Systems;

namespace MyriadCreation.Simulation.Settlement
{
public sealed class AbandonSettlementTileCommandHandler : ISimulationCommandHandler<AbandonSettlementTileCommand>
{
	private readonly LandAbandonmentRuntime _runtime;

	private readonly SimulationEventBus _events;

	public AbandonSettlementTileCommandHandler(LandAbandonmentRuntime runtime, SimulationEventBus events)
	{
		_runtime = runtime;
		_events = events;
	}

	public CommandResult Handle(AbandonSettlementTileCommand command)
	{
		LandAbandonmentSystem.AbandonmentRecord? abandonmentRecord = _runtime.AbandonTile(command.TileIndex, command.Type, command.RealmId);
		if (!abandonmentRecord.HasValue)
		{
			return CommandResult.Failed("tile_cannot_be_abandoned");
		}
		_events.Publish(new SettlementTileAbandonedEvent(abandonmentRecord.Value.tileIndex, abandonmentRecord.Value.type, abandonmentRecord.Value.previousRealmId, command.RealmId));
		return CommandResult.Succeeded("tile_abandoned");
	}
}
}
