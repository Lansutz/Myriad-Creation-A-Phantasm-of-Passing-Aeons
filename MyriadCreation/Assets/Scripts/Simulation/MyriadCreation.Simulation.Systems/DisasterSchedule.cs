using System;
using MyriadCreation.Simulation.Scheduling;

namespace MyriadCreation.Simulation.Systems
{
public static class DisasterSchedule
{
	public const string ScheduleId = "disaster.daily";

	public static void Register(ISimulationScheduler scheduler, DisasterSystem disasters, DiseaseSystem diseases, Func<int> day, Func<int> year)
	{
		if (scheduler == null)
		{
			throw new ArgumentNullException("scheduler");
		}
		if (disasters == null)
		{
			throw new ArgumentNullException("disasters");
		}
		if (diseases == null)
		{
			throw new ArgumentNullException("diseases");
		}
		if (day == null)
		{
			throw new ArgumentNullException("day");
		}
		if (year == null)
		{
			throw new ArgumentNullException("year");
		}
		scheduler.Register("disaster.daily", SimulationCadence.Daily, 11, (SimulationTickContext _) =>
		{
			disasters.DailyTick(day(), year());
			diseases.DailyTick(day(), year());
		});
	}
}
}
