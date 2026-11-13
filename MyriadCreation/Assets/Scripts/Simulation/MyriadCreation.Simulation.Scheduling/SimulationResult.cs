namespace MyriadCreation.Simulation.Scheduling
{
public readonly struct SimulationResult(bool succeeded, string code = "")
{
	public readonly bool succeeded = succeeded;

	public readonly string code = code ?? string.Empty;

	public static SimulationResult Success(string code = "ok")
	{
		return new SimulationResult(succeeded: true, code);
	}

	public static SimulationResult Failure(string code)
	{
		return new SimulationResult(succeeded: false, code);
	}
}
}
