namespace MyriadCreation.Core.Events
{
public readonly struct WarEndedEvent(int warId, int attackerId, int defenderId, int winnerId, string outcome, int day)
{
	public readonly int WarId = warId;

	public readonly int AttackerId = attackerId;

	public readonly int DefenderId = defenderId;

	public readonly int WinnerId = winnerId;

	public readonly string Outcome = outcome;

	public readonly int Day = day;
}
}
