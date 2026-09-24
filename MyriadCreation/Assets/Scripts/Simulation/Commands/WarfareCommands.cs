using System;
using System.Collections.Generic;
using MyriadCreation.Core.Contracts;
using MyriadCreation.Core.Data;

namespace MyriadCreation.Simulation.Warfare
{
    /// <summary>战争领域的军事预算写入契约。
    /// 当前只迁移既有 AI 行为：记录/消耗军事建设预算；实际兵力扩编仍由军队/招募领域负责。</summary>
    public readonly struct MilitaryBuildUpCommand : ISimulationCommand
    {
        public readonly int RealmId;
        public readonly float Budget;

        public MilitaryBuildUpCommand(int realmId, float budget = 50f)
        {
            RealmId = realmId;
            Budget = budget;
        }
    }

    public sealed class MilitaryBuildUpCommandHandler
        : ISimulationCommandHandler<MilitaryBuildUpCommand>
    {
        private readonly Dictionary<int, RealmData> _realms;

        public MilitaryBuildUpCommandHandler(Dictionary<int, RealmData> realms)
        {
            _realms = realms ?? throw new ArgumentNullException(nameof(realms));
        }

        public CommandResult Handle(MilitaryBuildUpCommand command)
        {
            if (!_realms.TryGetValue(command.RealmId, out var realm))
                return CommandResult.Failed("realm_not_found");

            if (command.Budget <= 0f || realm.treasury < command.Budget)
                return CommandResult.Failed("military_budget_rejected");

            realm.treasury -= command.Budget;
            return CommandResult.Succeeded("military_budget_committed");
        }
    }
}
