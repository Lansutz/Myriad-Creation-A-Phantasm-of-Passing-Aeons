namespace MyriadCreation.Core.Events
{
public readonly struct FaithConflictTriggeredEvent(int faithA, int faithB, int day)
{
	public readonly int FaithA = faithA;

	public readonly int FaithB = faithB;

	public readonly int Day = day;
}
}
