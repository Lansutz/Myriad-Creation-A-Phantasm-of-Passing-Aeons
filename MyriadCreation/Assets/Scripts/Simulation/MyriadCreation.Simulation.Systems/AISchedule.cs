using System;
using System.Collections.Generic;
using MyriadCreation.Core.Data;
using MyriadCreation.Simulation.Scheduling;
using MyriadCreation.Simulation.AI;
using MyriadCreation.Simulation.State;
using MyriadCreation.Simulation.WorldState;

namespace MyriadCreation.Simulation.Systems
{
public static class AISchedule
{
	public const string ScheduleId = "ai.daily";

	public static void Register(ISimulationScheduler scheduler, AIManager ai, Func<Dictionary<int, RealmData>> realms, Func<TileData[]> tiles, DiplomacyManager diplomacy, InnovationTree innovation, CharacterManager characters, Action missionaryTick, AIIntentExecutor executor, Func<GameWorld> world = null)
	{
		if (scheduler == null)
		{
			throw new ArgumentNullException("scheduler");
		}
		if (ai == null)
		{
			throw new ArgumentNullException("ai");
		}
		if (realms == null)
		{
			throw new ArgumentNullException("realms");
		}
		if (tiles == null)
		{
			throw new ArgumentNullException("tiles");
		}
		if (diplomacy == null)
		{
			throw new ArgumentNullException("diplomacy");
		}
		if (innovation == null)
		{
			throw new ArgumentNullException("innovation");
		}
		if (characters == null)
		{
			throw new ArgumentNullException("characters");
		}
		if (missionaryTick == null)
		{
			throw new ArgumentNullException("missionaryTick");
		}
		if (executor == null)
		{
			throw new ArgumentNullException("executor");
		}
		scheduler.Register("ai.daily", SimulationCadence.Daily, 28, (SimulationTickContext _) =>
		{
			missionaryTick();
			ai.SyncRulers(characters);
			ai.DailyTick(realms(), tiles(), diplomacy, innovation, characters, world?.Invoke());
			ai.ExecutePendingIntents(executor);
		});
	}
}
}
