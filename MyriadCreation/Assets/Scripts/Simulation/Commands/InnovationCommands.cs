using System;
using MyriadCreation.Core.Contracts;

namespace MyriadCreation.Simulation.Innovation
{
    /// <summary>革新领域的写入契约。</summary>
    public readonly struct StartInnovationResearchCommand : ISimulationCommand
    {
        public readonly int RealmId;
        public readonly int InnovationId;

        public StartInnovationResearchCommand(int realmId, int innovationId)
        {
            RealmId = realmId;
            InnovationId = innovationId;
        }
    }

    public sealed class StartInnovationResearchCommandHandler
        : ISimulationCommandHandler<StartInnovationResearchCommand>
    {
        private readonly InnovationTree _innovations;

        public StartInnovationResearchCommandHandler(InnovationTree innovations)
        {
            _innovations = innovations ?? throw new ArgumentNullException(nameof(innovations));
        }

        public CommandResult Handle(StartInnovationResearchCommand command)
            => _innovations.StartResearch(command.RealmId, command.InnovationId)
                ? CommandResult.Succeeded("research_started")
                : CommandResult.Failed("research_rejected");
    }
}
