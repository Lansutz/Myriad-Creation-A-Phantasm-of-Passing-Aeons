using MyriadCreation.Core.Contracts;
using MyriadCreation.Core.Events;
using MyriadCreation.Simulation.Warfare;

namespace MyriadCreation.Simulation.Settlement
{
    public readonly struct DestroySettlementCommand : ISimulationCommand
    {
        public readonly int BurgId;
        public readonly DestructionMethod Method;
        public readonly int AttackerRealmId;

        public DestroySettlementCommand(int anchorId, DestructionMethod method, int attackerRealmId = -1)
        {
            BurgId = anchorId;
            Method = method;
            AttackerRealmId = attackerRealmId;
        }
    }

    public readonly struct AbandonSettlementTileCommand : ISimulationCommand
    {
        public readonly int TileIndex;
        public readonly AbandonmentType Type;
        public readonly int RealmId;

        public AbandonSettlementTileCommand(int tileIndex, AbandonmentType type, int realmId)
        {
            TileIndex = tileIndex;
            Type = type;
            RealmId = realmId;
        }
    }

    public readonly struct ResettleTileCommand : ISimulationCommand
    {
        public readonly int TileIndex;
        public readonly int RealmId;
        public readonly int SettlerCount;

        public ResettleTileCommand(int tileIndex, int realmId, int settlerCount)
        {
            TileIndex = tileIndex;
            RealmId = realmId;
            SettlerCount = settlerCount;
        }
    }

    public readonly struct SettlementDestroyedEvent
    {
        public readonly int BurgId;
        public readonly DestructionMethod Method;
        public readonly int AttackerRealmId;

        public SettlementDestroyedEvent(int anchorId, DestructionMethod method, int attackerRealmId)
        {
            BurgId = anchorId;
            Method = method;
            AttackerRealmId = attackerRealmId;
        }
    }

    public readonly struct SettlementTileAbandonedEvent
    {
        public readonly int TileIndex;
        public readonly AbandonmentType Type;
        public readonly int PreviousRealmId;
        public readonly int RealmId;

        public SettlementTileAbandonedEvent(
            int tileIndex,
            AbandonmentType type,
            int previousRealmId,
            int realmId)
        {
            TileIndex = tileIndex;
            Type = type;
            PreviousRealmId = previousRealmId;
            RealmId = realmId;
        }
    }

    public readonly struct SettlementTileResettledEvent
    {
        public readonly int TileIndex;
        public readonly int RealmId;
        public readonly int SettlerCount;

        public SettlementTileResettledEvent(int tileIndex, int realmId, int settlerCount)
        {
            TileIndex = tileIndex;
            RealmId = realmId;
            SettlerCount = settlerCount;
        }
    }

    public sealed class DestroySettlementCommandHandler
        : ISimulationCommandHandler<DestroySettlementCommand>
    {
        private readonly SettlementDestructionRuntime _runtime;
        private readonly SimulationEventBus _events;

        public DestroySettlementCommandHandler(
            SettlementDestructionRuntime runtime,
            SimulationEventBus events)
        {
            _runtime = runtime;
            _events = events;
        }

        public CommandResult Handle(DestroySettlementCommand command)
        {
            string result = _runtime.DestroySettlement(
                command.BurgId,
                command.Method,
                command.AttackerRealmId);

            if (string.IsNullOrEmpty(result))
                return CommandResult.Failed("settlement_not_found");

            _events.Publish(new SettlementDestroyedEvent(
                command.BurgId,
                command.Method,
                command.AttackerRealmId));

            return CommandResult.Succeeded("settlement_destroyed");
        }
    }

    public sealed class AbandonSettlementTileCommandHandler
        : ISimulationCommandHandler<AbandonSettlementTileCommand>
    {
        private readonly LandAbandonmentRuntime _runtime;
        private readonly SimulationEventBus _events;

        public AbandonSettlementTileCommandHandler(
            LandAbandonmentRuntime runtime,
            SimulationEventBus events)
        {
            _runtime = runtime;
            _events = events;
        }

        public CommandResult Handle(AbandonSettlementTileCommand command)
        {
            var record = _runtime.AbandonTile(
                command.TileIndex,
                command.Type,
                command.RealmId);

            if (!record.HasValue)
                return CommandResult.Failed("tile_cannot_be_abandoned");

            _events.Publish(new SettlementTileAbandonedEvent(
                record.Value.tileIndex,
                record.Value.type,
                record.Value.previousRealmId,
                command.RealmId));

            return CommandResult.Succeeded("tile_abandoned");
        }
    }

    public sealed class ResettleTileCommandHandler
        : ISimulationCommandHandler<ResettleTileCommand>
    {
        private readonly LandAbandonmentRuntime _runtime;
        private readonly SimulationEventBus _events;

        public ResettleTileCommandHandler(
            LandAbandonmentRuntime runtime,
            SimulationEventBus events)
        {
            _runtime = runtime;
            _events = events;
        }

        public CommandResult Handle(ResettleTileCommand command)
        {
            if (!_runtime.ResettleTile(
                    command.TileIndex,
                    command.RealmId,
                    command.SettlerCount))
            {
                return CommandResult.Failed("tile_cannot_be_resettled");
            }

            _events.Publish(new SettlementTileResettledEvent(
                command.TileIndex,
                command.RealmId,
                command.SettlerCount));

            return CommandResult.Succeeded("tile_resettled");
        }
    }
}
