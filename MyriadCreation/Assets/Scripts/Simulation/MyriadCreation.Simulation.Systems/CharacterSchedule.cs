using System;
using MyriadCreation.Simulation.Scheduling;

namespace MyriadCreation.Simulation.Systems
{
public static class CharacterSchedule
{
	public const string ScheduleId = "characters.daily";

	public static void Register(ISimulationScheduler scheduler, CharacterManager characters, Func<int> day, Func<int> year)
	{
		if (scheduler == null)
		{
			throw new ArgumentNullException("scheduler");
		}
		if (characters == null)
		{
			throw new ArgumentNullException("characters");
		}
		if (day == null)
		{
			throw new ArgumentNullException("day");
		}
		if (year == null)
		{
			throw new ArgumentNullException("year");
		}
		scheduler.Register("characters.daily", SimulationCadence.Daily, 25, (SimulationTickContext _) =>
		{
			characters.DailyTick(day(), year());
		});
	}
}
}
