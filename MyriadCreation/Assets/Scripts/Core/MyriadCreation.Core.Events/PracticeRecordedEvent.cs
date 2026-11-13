namespace MyriadCreation.Core.Events
{
public readonly struct PracticeRecordedEvent(int characterId, int innovationId, float amount)
{
	public readonly int characterId = characterId;

	public readonly int innovationId = innovationId;

	public readonly float amount = amount;
}
}
