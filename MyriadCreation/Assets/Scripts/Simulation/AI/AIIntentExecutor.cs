using System;
using System.Collections.Generic;
using CivilizationEvolution.Core.Contracts;
using CivilizationEvolution.Simulation.Diplomacy;
using CivilizationEvolution.Simulation.Economy;
using CivilizationEvolution.Simulation.Innovation;
using CivilizationEvolution.Simulation.Politics;
using CivilizationEvolution.Simulation.Warfare;

namespace CivilizationEvolution.Simulation.AI
{
    /// <summary>
    /// AI 意图到领域写入契约的过渡适配器。
    /// 所有当前 AI Intent 均通过 Domain Command 写入领域；该类只负责路由，不拥有领域状态。
    /// </summary>
    public sealed class AIIntentExecutor
    {
        private readonly SimulationCommandBus _commands;
        public AIIntentExecutor(
            SimulationCommandBus commands)
        {
            _commands = commands ?? throw new ArgumentNullException(nameof(commands));
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
