using System;
using System.Collections.Generic;
using UnityEngine;
using CivilizationEvolution.Core.Contracts;
using CivilizationEvolution.Core.Data;
using CivilizationEvolution.Simulation.Diplomacy;
using CivilizationEvolution.Simulation.Economy;
using CivilizationEvolution.Simulation.Innovation;
using CivilizationEvolution.Simulation.Politics;
using CivilizationEvolution.Simulation.Warfare;

namespace CivilizationEvolution.Simulation.AI
{
    /// <summary>
    /// AI 意图到领域写入契约的过渡适配器。
    /// 外交/战争 Intent 已经进入 Domain Command；经济/内政 Intent 暂留此处，
    /// 避免迁移过程中改变既有模拟行为，待对应领域边界确认后再迁移。
    /// </summary>
    public sealed class AIIntentExecutor
    {
        private readonly SimulationCommandBus _commands;
        private readonly InnovationTree _innovations;
        private readonly Dictionary<int, RealmData> _realms;
        private readonly TileData[] _tiles;

        public AIIntentExecutor(
            SimulationCommandBus commands,
            InnovationTree innovations,
            Dictionary<int, RealmData> realms,
            TileData[] tiles)
        {
            _commands = commands ?? throw new ArgumentNullException(nameof(commands));
            _innovations = innovations ?? throw new ArgumentNullException(nameof(innovations));
            _realms = realms ?? throw new ArgumentNullException(nameof(realms));
            _tiles = tiles ?? throw new ArgumentNullException(nameof(tiles));
        }

        public void Execute(IEnumerable<AIIntent> intents)
        {
            if (intents == null) return;

            foreach (var intent in intents)
            {
                switch (intent.Type)
                {
                    case AIIntentType.StartResearch:
                        _commands.Send(new StartInnovationResearchCommand(
                            intent.ActorRealmId,
                            intent.InnovationId));
                        break;
                    case AIIntentType.RaidSettlement:
                        _commands.Send(new RaidSettlementCommand(
                            intent.ActorRealmId,
                            intent.TargetRealmId,
                            intent.TargetTileIndex,
                            intent.RaidType));
                        break;
                    case AIIntentType.DeclareWar:
                        _commands.Send(new DeclareWarCommand(
                            intent.ActorRealmId,
                            intent.TargetRealmId,
                            "领土扩张"));
                        break;
                    case AIIntentType.ImproveEconomy:
                        _commands.Send(new ImproveRealmEconomyCommand(intent.ActorRealmId));
                        break;
                    case AIIntentType.ProposeAlliance:
                        _commands.Send(new ProposeAllianceCommand(
                            intent.ActorRealmId,
                            intent.TargetRealmId,
                            intent.AllianceType));
                        break;
                    case AIIntentType.SendGift:
                        _commands.Send(new SendGiftCommand(
                            intent.ActorRealmId,
                            intent.TargetRealmId,
                            intent.Amount));
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
