using System;
using System.Collections.Generic;
using MyriadCreation.Content.Definitions;

namespace MyriadCreation.Simulation.Scheduling
{
public sealed class SimulationTimingEntry
{
	public string id;
	public double avgMs;
	public int intervalDays;
	public int order;
	public int lane;
}

public sealed class SimulationScheduler : ISimulationScheduler
{
	private sealed class Entry
	{
		public string id;

		public int intervalDays;

		public int order;

		public string dirtyKey;

		public SimulationLane lane;

		public double avgMs = 0.1;

		public Action<SimulationTickContext> execute;

		/// <summary>两相系统中的 Apply 相；null = 单相系统。</summary>
		public Action<SimulationTickContext> apply;
	}

	private readonly SimulationDirtySet _dirty = new SimulationDirtySet();

	private readonly Dictionary<string, Entry> _entries = new Dictionary<string, Entry>();

	private readonly List<Entry> _pending = new List<Entry>();

	public int Count => _entries.Count;

	public int PendingCount => _pending.Count;

	public List<SimulationTimingEntry> GetTimings()
	{
		var result = new List<SimulationTimingEntry>(_entries.Count);
		foreach (Entry entry in _entries.Values)
		{
			result.Add(new SimulationTimingEntry
			{
				id = entry.id,
				avgMs = entry.avgMs,
				intervalDays = entry.intervalDays,
				order = entry.order,
				lane = (int)entry.lane
			});
		}
		result.Sort((a, b) => b.avgMs.CompareTo(a.avgMs));
		return result;
	}

	public SimulationDirtySet Dirty => _dirty;

	public void MarkDirty(string dirtyKey)
	{
		_dirty.Mark(dirtyKey);
	}

	public void Register(string id, SimulationCadence cadence, int order, Action<SimulationTickContext> execute)
	{
		Register(id, cadence, order, SimulationLane.Both, execute);
	}

	public void Register(string id, SimulationCadence cadence, int order, SimulationLane lane, Action<SimulationTickContext> execute)
	{
		if (string.IsNullOrWhiteSpace(id))
		{
			throw new ArgumentException("Scheduler id is required.", "id");
		}
		if (execute == null)
		{
			throw new ArgumentNullException("execute");
		}
		_entries[id] = new Entry
		{
			id = id,
			intervalDays = GetIntervalDays(cadence),
			order = order,
			dirtyKey = null,
			lane = lane,
			execute = execute
		};
	}

	/// <summary>两相注册：同一 tick 内先执行所有到期系统的 Compute，再统一执行 Apply（读后写锁步）。</summary>
	public void RegisterPhased(string id, SimulationCadence cadence, int order, Action<SimulationTickContext> compute, Action<SimulationTickContext> apply)
	{
		if (string.IsNullOrWhiteSpace(id))
		{
			throw new ArgumentException("Scheduler id is required.", "id");
		}
		if (compute == null || apply == null)
		{
			throw new ArgumentNullException("compute/apply");
		}
		_entries[id] = new Entry
		{
			id = id,
			intervalDays = GetIntervalDays(cadence),
			order = order,
			dirtyKey = null,
			lane = SimulationLane.Both,
			execute = compute,
			apply = apply
		};
	}

	public void RegisterDirty(string id, SimulationCadence cadence, int order, string dirtyKey, Action<SimulationTickContext> execute)
	{
		RegisterDirty(id, cadence, order, dirtyKey, SimulationLane.Both, execute);
	}

	public void RegisterDirty(string id, SimulationCadence cadence, int order, string dirtyKey, SimulationLane lane, Action<SimulationTickContext> execute)
	{
		if (string.IsNullOrWhiteSpace(dirtyKey))
		{
			throw new ArgumentException("Dirty key is required.", "dirtyKey");
		}
		if (execute == null)
		{
			throw new ArgumentNullException("execute");
		}
		_entries[id] = new Entry
		{
			id = id,
			intervalDays = GetIntervalDays(cadence),
			order = order,
			dirtyKey = dirtyKey,
			lane = lane,
			execute = execute
		};
	}

	public bool Unregister(string id)
	{
		return _entries.Remove(id);
	}

	public void Clear()
	{
		_entries.Clear();
	}

	public void Tick(int day, int year, int deltaDays)
	{
		TickBudgeted(day, year, deltaDays, float.MaxValue, null, out var _, out var _);
	}

	public void Tick(int day, int year, int deltaDays, SimulationLane lane)
	{
		TickBudgeted(day, year, deltaDays, lane, float.MaxValue, null, out var _, out var _);
	}

	public void TickBudgeted(int day, int year, int deltaDays, float budgetMs, Func<double> clock, out int executed, out int deferred)
	{
		TickBudgeted(day, year, deltaDays, SimulationLane.Both, budgetMs, clock, out executed, out deferred);
	}

	public void TickBudgeted(int day, int year, int deltaDays, SimulationLane lane, float budgetMs, Func<double> clock, out int executed, out int deferred)
	{
		SimulationTickContext obj = new SimulationTickContext(day, year, deltaDays);
		List<Entry> list = new List<Entry>();
		if (_pending.Count > 0)
		{
			list.AddRange(_pending);
			_pending.Clear();
		}
		foreach (Entry value in _entries.Values)
		{
			if (!list.Contains(value) && lane != SimulationLane.None && (lane == SimulationLane.Both || value.lane == SimulationLane.Both || (value.lane & lane) != 0) && (value.intervalDays <= 1 || day % value.intervalDays == 0) && (value.dirtyKey == null || _dirty.IsDirty(value.dirtyKey)))
			{
				list.Add(value);
			}
		}
		list.Sort((Entry a, Entry b) =>
		{
			int num7 = a.order.CompareTo(b.order);
			return (num7 == 0) ? string.CompareOrdinal(a.id, b.id) : num7;
		});
		executed = 0;
		deferred = 0;
		List<Entry> phasedApplied = new List<Entry>();
		double num = clock?.Invoke() ?? 0.0;
		int num2 = list.Count;
		for (int num3 = 0; num3 < list.Count; num3++)
		{
			Entry entry = list[num3];
			if (executed > 0 && clock != null && budgetMs >= 0f && budgetMs < float.MaxValue && (clock() - num) * 1000.0 + entry.avgMs > (double)budgetMs)
			{
				num2 = num3;
				break;
			}
			double num4 = clock?.Invoke() ?? 0.0;
			int expectedVersion = ((entry.dirtyKey != null) ? _dirty.GetVersion(entry.dirtyKey) : 0);
			entry.execute(obj);
			if (entry.apply != null)
			{
				phasedApplied.Add(entry);
			}
			if (entry.dirtyKey != null)
			{
				_dirty.ClearIfVersion(entry.dirtyKey, expectedVersion);
			}
			if (clock != null)
			{
				double num5 = (clock() - num4) * 1000.0;
				entry.avgMs = entry.avgMs * 0.7 + num5 * 0.3;
			}
			executed++;
		}
		// Apply 相：所有到期系统的 Compute 完成后统一写状态（锁步，不交错）。
		for (int a = 0; a < phasedApplied.Count; a++)
		{
			phasedApplied[a].apply(obj);
		}
		for (int num6 = num2; num6 < list.Count; num6++)
		{
			_pending.Add(list[num6]);
			deferred++;
		}
	}

	private static int GetIntervalDays(SimulationCadence cadence)
	{
		return cadence switch
		{
			SimulationCadence.Weekly => 7, 
			SimulationCadence.Monthly => 30, 
			SimulationCadence.Seasonal => 90, 
			SimulationCadence.LongTerm => 365, 
			_ => 1, 
		};
	}
}
}
