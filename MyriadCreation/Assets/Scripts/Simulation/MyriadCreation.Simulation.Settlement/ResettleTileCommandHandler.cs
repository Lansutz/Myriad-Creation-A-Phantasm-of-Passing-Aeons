using MyriadCreation.Core.Contracts;
using MyriadCreation.Core.Events;
using MyriadCreation.Simulation.Systems;

namespace MyriadCreation.Simulation.Settlement
{
public sealed class ResettleTileCommandHandler : ISimulationCommandHandler<ResettleTileCommand>
{
	private readonly LandAbandonmentRuntime _runtime;

	private readonly SimulationEventBus _events;

	public ResettleTileCommandHandler(LandAbandonmentRuntime runtime, SimulationEventBus events)
	{
		_runtime = runtime;
		_events = events;
	}

	public CommandResult Handle(ResettleTileCommand command)
	{
		if (!_runtime.ResettleTile(command.TileIndex, command.RealmId, command.SettlerCount))
		{
			return CommandResult.Failed("tile_cannot_be_resettled");
		}
		_events.Publish(new SettlementTileResettledEvent(command.TileIndex, command.RealmId, command.SettlerCount));
		return CommandResult.Succeeded("tile_resettled");
	}
}
}
