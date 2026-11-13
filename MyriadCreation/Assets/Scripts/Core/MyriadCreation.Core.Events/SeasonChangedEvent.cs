namespace MyriadCreation.Core.Events
{
public readonly struct SeasonChangedEvent(int day, int year, int season)
{
	public readonly int Day = day;

	public readonly int Year = year;

	public readonly int Season = season;
}
}
