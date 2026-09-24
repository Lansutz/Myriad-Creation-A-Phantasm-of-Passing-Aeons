using System;
using System.Collections.Generic;
using CivilizationEvolution.Core.Contracts;
using CivilizationEvolution.Simulation.Diplomacy;
using CivilizationEvolution.Simulation.Innovation;

namespace CivilizationEvolution.Simulation.AI
{
    /// <summary>
    /// AI 意图到领域写入契约的过渡适配器。
    /// 不再直接持有外交/地图/角色状态；具体规则由领域 CommandHandler 执行。
    /// </summary>
    public sealed class AIIntentExecutor
    {
        private readonly SimulationCommandBus _commands;
        private readonly InnovationTree _innovations;

        public AIIntentExecutor(
            SimulationCommandBus commands,
            InnovationTree innovations)
        {
            _commands = commands ?? throw new ArgumentNullException(nameof(commands));
            _innovations = innovations ?? throw new ArgumentNullException(nameof(innovations));
        }

        public void Execute(IEnumerable<AIIntent> intents)
        {
            if (intents == null) return;

            foreach (var intent in intents)
            {
                switch (intent.Type)
                {
                    case AIIntentType.StartResearch:
                        ExecuteStartResearch(intent);
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
                    case AIIntentType.ImproveEconomy:
                    case AIIntentType.ConsolidateRealm:
                    case AIIntentType.MilitaryBuildUp:
                        // 经济/内政类 Intent 暂保留在过渡执行器，待对应领域边界确认后迁移。
                        break;
                }
            }
        }

        private void ExecuteStartResearch(AIIntent intent)
        {
            if (intent.InnovationId != 0)
                _innovations.StartResearch(intent.ActorRealmId, intent.InnovationId);
        }
    }
}
