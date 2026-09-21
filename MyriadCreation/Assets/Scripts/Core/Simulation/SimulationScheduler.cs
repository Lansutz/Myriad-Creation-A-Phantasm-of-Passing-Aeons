using System;
using System.Collections.Generic;

namespace CivilizationEvolution.Core.Simulation
{
    public readonly struct SimulationTickContext
    {
        public readonly int day;
        public readonly int year;
        public readonly int deltaDays;
        public SimulationTickContext(int day, int year, int deltaDays){ this.day=day; this.year=year; this.deltaDays=deltaDays; }
    }

    public enum SimulationCadence { Immediate, Daily, Weekly, Monthly, Seasonal, LongTerm }

    public interface ISimulationScheduler
    {
        int Count { get; }
        void Register(string id, SimulationCadence cadence, int order, Action<SimulationTickContext> execute);
        bool Unregister(string id);
    }

    public sealed class SimulationScheduler : ISimulationScheduler
    {
        private sealed class Entry { public string id; public int intervalDays; public int order; public Action<SimulationTickContext> execute; }
        private readonly Dictionary<string, Entry> _entries = new Dictionary<string, Entry>();
        public int Count => _entries.Count;
        public void Register(string id, SimulationCadence cadence, int order, Action<SimulationTickContext> execute)
        {
            if (string.IsNullOrWhiteSpace(id)) throw new ArgumentException("Scheduler id is required.", nameof(id));
            if (execute == null) throw new ArgumentNullException(nameof(execute));
            _entries[id] = new Entry { id=id, intervalDays=GetIntervalDays(cadence), order=order, execute=execute };
        }
        public bool Unregister(string id) => _entries.Remove(id);
        public void Clear() => _entries.Clear();
        public void Tick(int day, int year, int deltaDays)
        {
            var context = new SimulationTickContext(day, year, deltaDays);
            var due = new List<Entry>();
            foreach (var entry in _entries.Values) if (entry.intervalDays <= 1 || day % entry.intervalDays == 0) due.Add(entry);
            due.Sort((a,b)=>{ int c=a.order.CompareTo(b.order); return c!=0?c:string.CompareOrdinal(a.id,b.id); });
            foreach (var entry in due) entry.execute(context);
        }
        private static int GetIntervalDays(SimulationCadence cadence)
        {
            switch(cadence){ case SimulationCadence.Weekly:return 7; case SimulationCadence.Monthly:return 30; case SimulationCadence.Seasonal:return 90; case SimulationCadence.LongTerm:return 365; default:return 1; }
        }
    }
}
