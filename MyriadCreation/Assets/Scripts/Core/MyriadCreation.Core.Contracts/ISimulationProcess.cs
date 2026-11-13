namespace MyriadCreation.Core.Contracts
{
public interface ISimulationProcess
{
	string ProcessId { get; }

	SimulationProcessResult Execute(in SimulationProcessContext context);
}
}
