namespace MyriadCreation.Core.Contracts
{
public readonly struct SimulationActivityTransition(string nextActivityId, string reasonCode = "")
{
	public readonly string NextActivityId = nextActivityId ?? string.Empty;

	public readonly string ReasonCode = reasonCode ?? string.Empty;

	public bool HasNextActivity => !string.IsNullOrWhiteSpace(NextActivityId);
}
}
