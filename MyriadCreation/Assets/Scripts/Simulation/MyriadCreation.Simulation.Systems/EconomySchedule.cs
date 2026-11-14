using System;
using MyriadCreation.Core.Events;
using MyriadCreation.Simulation.Scheduling;

namespace MyriadCreation.Simulation.Systems
{
public static class EconomySchedule
{
	public const string ScheduleId = "economy.daily";

	public static void Register(ISimulationScheduler scheduler, EconomyManager economy, SimulationEventBus events = null, Func<int> dayProvider = null)
	{
		if (scheduler == null)
		{
			throw new ArgumentNullException("scheduler");
		}
		if (economy == null)
		{
			throw new ArgumentNullException("economy");
		}
		scheduler.Register("economy.daily", SimulationCadence.Daily, 12, (SimulationTickContext context) =>
		{
			economy.DailyTick(context.deltaDays);
			events?.Publish(new EconomyDayTickedEvent(dayProvider?.Invoke() ?? 0));
		});
	}
}
}
