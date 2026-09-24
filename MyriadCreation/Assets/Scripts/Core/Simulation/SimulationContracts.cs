using System;

namespace MyriadCreation.Core.Simulation
{
    /// <summary>Stable read-only query contract. Internal data structures remain replaceable.</summary>
    public interface ISimulationQuery<out TResult>
    {
        TResult Execute();
    }

    /// <summary>Stable request contract for world-changing operations.</summary>
    public interface ISimulationCommand<out TResult>
    {
        TResult Execute();
    }

    /// <summary>Common result envelope for domain processes.</summary>
    public readonly struct SimulationResult
    {
        public readonly bool succeeded;
        public readonly string code;

        public SimulationResult(bool succeeded, string code = "")
        {
            this.succeeded = succeeded;
            this.code = code ?? string.Empty;
        }

        public static SimulationResult Success(string code = "ok")
            => new SimulationResult(true, code);

        public static SimulationResult Failure(string code)
            => new SimulationResult(false, code);
    }

    /// <summary>
    /// Marker for runtime definitions. Authoring sources (Base/Editor/Mod/Scenario)
    /// are resolved into the same runtime definition contract.
    /// </summary>
    public interface IRuntimeDefinition
    {
        string Id { get; }
    }
}
