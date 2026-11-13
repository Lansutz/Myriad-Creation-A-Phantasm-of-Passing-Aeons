using System;

namespace MyriadCreation.Simulation.Scheduling
{
public interface ISimulationScheduler
{
	int Count { get; }

	void MarkDirty(string dirtyKey);

	void Register(string id, SimulationCadence cadence, int order, Action<SimulationTickContext> execute);

	void RegisterDirty(string id, SimulationCadence cadence, int order, string dirtyKey, Action<SimulationTickContext> execute);

	void RegisterPhased(string id, SimulationCadence cadence, int order, Action<SimulationTickContext> compute, Action<SimulationTickContext> apply);

	bool Unregister(string id);
}
}
