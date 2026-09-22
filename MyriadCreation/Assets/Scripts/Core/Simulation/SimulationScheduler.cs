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
        void RegisterDirty(string id, SimulationCadence cadence, int order, string dirtyKey, Action<SimulationTickContext> execute);
        bool Unregister(string id);
    }

    public sealed class SimulationDirtySet
    {
        private readonly Dictionary<string, int> _versions = new Dictionary<string, int>();

        public int Count => _versions.Count;

        public void Mark(string key)
        {
            if (string.IsNullOrWhiteSpace(key)) return;
            _versions.TryGetValue(key, out var version);
            _versions[key] = version + 1;
        }

        public bool IsDirty(string key)
            => !string.IsNullOrWhiteSpace(key) && _versions.ContainsKey(key);

        public int GetVersion(string key)
        {
            if (string.IsNullOrWhiteSpace(key)) return 0;
            return _versions.TryGetValue(key, out var version) ? version : 0;
        }

        public bool Clear(string key)
            => !string.IsNullOrWhiteSpace(key) && _versions.Remove(key);

        public bool ClearIfVersion(string key, int expectedVersion)
        {
            if (string.IsNullOrWhiteSpace(key)) return false;
            if (!_versions.TryGetValue(key, out var currentVersion) || currentVersion != expectedVersion)
                return false;
            _versions.Remove(key);
            return true;
        }

        public void ClearAll() => _versions.Clear();
    }

    public sealed class SimulationScheduler : ISimulationScheduler
    {
        private sealed class Entry
        {
            public string id;
            public int intervalDays;
            public int order;
            public string dirtyKey;
            public Action<SimulationTickContext> execute;
        }

        private readonly SimulationDirtySet _dirty = new SimulationDirtySet();
        private readonly Dictionary<string, Entry> _entries = new Dictionary<string, Entry>();

        public int Count => _entries.Count;
        public SimulationDirtySet Dirty => _dirty;

        public void Register(string id, SimulationCadence cadence, int order, Action<SimulationTickContext> execute)
        {
            if (string.IsNullOrWhiteSpace(id)) throw new ArgumentException("Scheduler id is required.", nameof(id));
            if (execute == null) throw new ArgumentNullException(nameof(execute));
            _entries[id] = new Entry { id=id, intervalDays=GetIntervalDays(cadence), order=order, dirtyKey=null, execute=execute };
        }

        public void RegisterDirty(string id, SimulationCadence cadence, int order, string dirtyKey, Action<SimulationTickContext> execute)
        {
            if (string.IsNullOrWhiteSpace(dirtyKey)) throw new ArgumentException("Dirty key is required.", nameof(dirtyKey));
            if (execute == null) throw new ArgumentNullException(nameof(execute));
            _entries[id] = new Entry { id=id, intervalDays=GetIntervalDays(cadence), order=order, dirtyKey=dirtyKey, execute=execute };
        }

        public bool Unregister(string id) => _entries.Remove(id);
        public void Clear() => _entries.Clear();

        public void Tick(int day, int year, int deltaDays)
        {
            var context = new SimulationTickContext(day, year, deltaDays);
            var due = new List<Entry>();

            foreach (var entry in _entries.Values)
            {
                if (!(entry.intervalDays <= 1 || day % entry.intervalDays == 0)) continue;
                if (entry.dirtyKey != null && !_dirty.IsDirty(entry.dirtyKey)) continue;
                due.Add(entry);
            }

            due.Sort((a,b)=>{ int c=a.order.CompareTo(b.order); return c!=0?c:string.CompareOrdinal(a.id,b.id); });

            foreach (var entry in due)
            {
                var dirtyVersion = entry.dirtyKey == null ? 0 : _dirty.GetVersion(entry.dirtyKey);
                entry.execute(context);

                // A handler may mark the same key again while recalculating. Only consume
                // the version that caused this execution; preserve any newer invalidation.
                if (entry.dirtyKey != null)
                    _dirty.ClearIfVersion(entry.dirtyKey, dirtyVersion);
            }
        }

        private static int GetIntervalDays(SimulationCadence cadence)
        {
            switch(cadence){ case SimulationCadence.Weekly:return 7; case SimulationCadence.Monthly:return 30; case SimulationCadence.Seasonal:return 90; case SimulationCadence.LongTerm:return 365; default:return 1; }
        }
    }
}
