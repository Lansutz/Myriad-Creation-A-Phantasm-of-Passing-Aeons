using System;
using MyriadCreation.Core.Contracts;
using MyriadCreation.Core.Data;
using MyriadCreation.Core.Enums;
using MyriadCreation.Simulation.Characters;

namespace MyriadCreation.Simulation.Diplomacy
{
    /// <summary>
    /// 外交/战争领域的写入契约。
    /// AI、UI 等调用方只提交 Command，不直接操作 DiplomacyManager。
    /// </summary>
    public readonly struct DeclareWarCommand : ISimulationCommand
    {
        public readonly int AttackerRealmId;
        public readonly int DefenderRealmId;
        public readonly string Reason;

        public DeclareWarCommand(int attackerRealmId, int defenderRealmId, string reason)
        {
            AttackerRealmId = attackerRealmId;
            DefenderRealmId = defenderRealmId;
            Reason = reason ?? string.Empty;
        }
    }

    public readonly struct RaidSettlementCommand : ISimulationCommand
    {
        public readonly int RaiderRealmId;
        public readonly int TargetRealmId;
        public readonly int TargetTileIndex;
        public readonly GameEnums.RaidType RaidType;

        public RaidSettlementCommand(
            int raiderRealmId,
            int targetRealmId,
            int targetTileIndex,
            GameEnums.RaidType raidType)
        {
            RaiderRealmId = raiderRealmId;
            TargetRealmId = targetRealmId;
            TargetTileIndex = targetTileIndex;
            RaidType = raidType;
        }
    }

    public readonly struct ProposeAllianceCommand : ISimulationCommand
    {
        public readonly int RealmAId;
        public readonly int RealmBId;
        public readonly AllianceType Type;

        public ProposeAllianceCommand(int realmAId, int realmBId, AllianceType type)
        {
            RealmAId = realmAId;
            RealmBId = realmBId;
            Type = type;
        }
    }

    public readonly struct SendGiftCommand : ISimulationCommand
    {
        public readonly int FromRealmId;
        public readonly int ToRealmId;
        public readonly float Amount;

        public SendGiftCommand(int fromRealmId, int toRealmId, float amount)
        {
            FromRealmId = fromRealmId;
            ToRealmId = toRealmId;
            Amount = amount;
        }
    }

    public sealed class DeclareWarCommandHandler : ISimulationCommandHandler<DeclareWarCommand>
    {
        private readonly Func<int, int, string, bool> _declareWar;

        public DeclareWarCommandHandler(Func<int, int, string, bool> declareWar)
        {
            _declareWar = declareWar ?? throw new ArgumentNullException(nameof(declareWar));
        }

        public CommandResult Handle(DeclareWarCommand command)
            => _declareWar(command.AttackerRealmId, command.DefenderRealmId, command.Reason)
                ? CommandResult.Succeeded("war_declared")
                : CommandResult.Failed("war_declaration_rejected");
    }

    public sealed class RaidSettlementCommandHandler
        : ISimulationCommandHandler<RaidSettlementCommand>
    {
        private readonly DiplomacyManager _diplomacy;
        private readonly TileData[] _tiles;
        private readonly CharacterManager _characters;

        public RaidSettlementCommandHandler(
            DiplomacyManager diplomacy,
            TileData[] tiles,
            CharacterManager characters)
        {
            _diplomacy = diplomacy ?? throw new ArgumentNullException(nameof(diplomacy));
            _tiles = tiles ?? throw new ArgumentNullException(nameof(tiles));
            _characters = characters;
        }

        public CommandResult Handle(RaidSettlementCommand command)
        {
            var result = _diplomacy.RaidSettlement(
                command.RaiderRealmId,
                command.TargetRealmId,
                command.TargetTileIndex,
                command.RaidType,
                _tiles);

            if (!result.success)
                return CommandResult.Failed("raid_rejected");

            if (command.RaidType == GameEnums.RaidType.Massacre && _characters != null)
            {
                var ruler = _characters.FindRulerOfRealm(command.RaiderRealmId);
                if (ruler != null)
                    ruler.achievements.massacres++;
            }

            return CommandResult.Succeeded(result.warDeclared
                ? "raid_completed_war_declared"
                : "raid_completed");
        }
    }

    public sealed class ProposeAllianceCommandHandler
        : ISimulationCommandHandler<ProposeAllianceCommand>
    {
        private readonly DiplomacyManager _diplomacy;

        public ProposeAllianceCommandHandler(DiplomacyManager diplomacy)
        {
            _diplomacy = diplomacy ?? throw new ArgumentNullException(nameof(diplomacy));
        }

        public CommandResult Handle(ProposeAllianceCommand command)
            => _diplomacy.ProposeAlliance(
                    command.RealmAId,
                    command.RealmBId,
                    command.Type) != null
                ? CommandResult.Succeeded("alliance_proposed")
                : CommandResult.Failed("alliance_rejected");
    }

    public sealed class SendGiftCommandHandler
        : ISimulationCommandHandler<SendGiftCommand>
    {
        private readonly DiplomacyManager _diplomacy;

        public SendGiftCommandHandler(DiplomacyManager diplomacy)
        {
            _diplomacy = diplomacy ?? throw new ArgumentNullException(nameof(diplomacy));
        }

        public CommandResult Handle(SendGiftCommand command)
            => _diplomacy.SendGift(
                    command.FromRealmId,
                    command.ToRealmId,
                    command.Amount)
                ? CommandResult.Succeeded("gift_sent")
                : CommandResult.Failed("gift_rejected");
    }
}
