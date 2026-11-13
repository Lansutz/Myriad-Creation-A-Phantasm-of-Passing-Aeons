namespace MyriadCreation.Core.Contracts
{
public readonly struct SimulationWaitCondition(string conditionId, string description = "", bool pollOnSchedule = true)
{
	public readonly string ConditionId = conditionId ?? string.Empty;

	public readonly string Description = description ?? string.Empty;

	public readonly bool PollOnSchedule = pollOnSchedule;

	public bool IsValid => !string.IsNullOrWhiteSpace(ConditionId);
}
}
