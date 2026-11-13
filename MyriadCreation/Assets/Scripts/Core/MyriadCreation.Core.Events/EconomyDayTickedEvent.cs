namespace MyriadCreation.Core.Events
{
public readonly struct EconomyDayTickedEvent(int day)
{
	public readonly int Day = day;
}
}
