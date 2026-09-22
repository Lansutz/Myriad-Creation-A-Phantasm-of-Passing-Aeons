using System;
using CivilizationEvolution.Core.Events;
using CivilizationEvolution.Core.Simulation;

namespace CivilizationEvolution.Simulation.Politics
{
    /// <summary>
    /// Politics domain runtime. The scheduler only invokes this system; world composition
    /// and cross-domain reactions are kept behind the simulation boundary.
    /// </summary>
    public sealed class PoliticsSimulationSystem
    {
        private readonly PoliticalManager _politics;
        private readonly Action _worldPolitics;

        public PoliticsSimulationSystem(PoliticalManager politics, Action worldPolitics)
        {
            _politics = politics ?? throw new ArgumentNullException(nameof(politics));
            _worldPolitics = worldPolitics ?? throw new ArgumentNullException(nameof(worldPolitics));
        }

        public void DailyTick()
        {
            _politics.DailyTick();
            _worldPolitics();
        }
    }

    public static class PoliticsSimulationSchedule
    {
        public const string ScheduleId = "politics.daily";

        public static void Register(ISimulationScheduler scheduler, PoliticsSimulationSystem system)
        {
            if (scheduler == null) throw new ArgumentNullException(nameof(scheduler));
            if (system == null) throw new ArgumentNullException(nameof(system));
            scheduler.Register(ScheduleId, SimulationCadence.Daily, 15, _ => system.DailyTick());
        }
    }
}
