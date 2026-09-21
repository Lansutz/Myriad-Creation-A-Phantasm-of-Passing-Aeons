using System;
using System.Collections.Generic;

namespace CivilizationEvolution.Core.Simulation
{
    public readonly struct SimulationTickContext
    {
        public readonly int day;
        public readonly int year;
        public readonly float deltaDays;

        public SimulationTickContext(int day, int year, float deltaDays)
        {
            this.day = day;
            this.year = year;
            this.deltaDays = deltaDays;
        }
    }

    public enum SimulationCadence
    {
        Immediate,
        Daily,
        Weekly,
        Monthly,
        Seasonal,
        LongTerm
    }

    /// <summary>
    /// Central cadence scheduler. Domain systems register work with a cadence instead of being
    /// hard-coded into the world loop. Ordering is explicit and stable; scheduling is not domain-aware.
    /// </summary>
    public sealed class SimulationScheduler
    {
        private sealed class Entry
        {
            public readonly string id;
            public readonly SimulationCadence cadence;
            public readonly int intervalDays;
            public readonly int order;
            public readonly Action<SimulationTickContext> execute;

            public Entry(string id, SimulationCadence cadence, int intervalDays, int order,
                Action<SimulationTickContext> execute)
            {
                this.id = id;
                this.cadence = cadence;
                this.intervalDays = Math.Max(1, intervalDays);
                this.order = order;
                this.execute = execute;
            }
        }

        private readonly List<Entry> _entries = new List<Entry>();
        private bool _sorted;

        public int Count => _entries.Count;

        public void Register(string id, SimulationCadence cadence, int order,
            Action<SimulationTickContext> execute)
            => Register(id, cadence, cadence == SimulationCadence.Immediate ? 1 : CadenceDays(cadence),
                order, execute);

        public void Register(string id, SimulationCadence cadence, int intervalDays, int order,
            Action<SimulationTickContext> execute)
        {
            if (string.IsNullOrEmpty(id)) throw new ArgumentException("Scheduler entry id is required.", nameof(id));
            if (execute == null) throw new ArgumentNullException(nameof(execute));
            if (_entries.Exists(x => x.id == id))
                throw new InvalidOperationException("Scheduler entry already registered: " + id);

            _entries.Add(new Entry(id, cadence, intervalDays, order, execute));
            _sorted = false;
        }

        public bool Unregister(string id)
        {
            int index = _entries.FindIndex(x => x.id == id);
            if (index < 0) return false;
            _entries.RemoveAt(index);
            return true;
        }

        public void Clear() => _entries.Clear();

        public void Tick(int day, int year, float deltaDays = 1f)
        {
            if (!_sorted)
            {
                _entries.Sort((a, b) =>
                {
                    int order = a.order.CompareTo(b.order);
                    return order != 0 ? order : string.CompareOrdinal(a.id, b.id);
                });
                _sorted = true;
            }

            var context = new SimulationTickContext(day, year, deltaDays);
            foreach (var entry in _entries)
            {
                if (ShouldRun(entry, day)) entry.execute(context);
            }
        }

        private static bool ShouldRun(Entry entry, int day)
        {
            if (entry.cadence == SimulationCadence.Immediate) return true;
            return day > 0 && day % entry.intervalDays == 0;
        }

        private static int CadenceDays(SimulationCadence cadence)
        {
            switch (cadence)
            {
                case SimulationCadence.Weekly: return 7;
                case SimulationCadence.Monthly: return 30;
                case SimulationCadence.Seasonal: return 90;
                case SimulationCadence.LongTerm: return 365;
                default: return 1;
            }
        }
    }
}
