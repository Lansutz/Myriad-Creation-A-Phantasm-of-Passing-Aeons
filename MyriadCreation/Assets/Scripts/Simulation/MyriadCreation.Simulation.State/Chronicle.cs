using System;
using System.Collections.Generic;

namespace MyriadCreation.Simulation.State
{
[Serializable]
public class Chronicle
{
	private readonly List<ChronicleEntry> _entries = new List<ChronicleEntry>();

	private int _nextEntryId = 1;

	private const int MaxEntries = 500;

	public int CurrentTick { get; set; }

	public int CurrentYear { get; set; } = 1;

	public int Count => _entries.Count;

	public ChronicleEntry Add(string eventType, string description, bool major = false, params int[] participants)
	{
		ChronicleEntry chronicleEntry = new ChronicleEntry
		{
			entryId = _nextEntryId++,
			tick = CurrentTick,
			year = CurrentYear,
			eventType = eventType,
			description = description,
			major = major
		};
		if (participants != null)
		{
			chronicleEntry.participants.AddRange(participants);
		}
		_entries.Add(chronicleEntry);
		if (_entries.Count > 500)
		{
			_entries.RemoveAt(0);
		}
		return chronicleEntry;
	}

	public List<ChronicleEntry> GetEntries()
	{
		return _entries;
	}

	/// <summary>读档还原：保留原 entryId，并把下一条自增 id 推到最大值之后。</summary>
	public void RestoreEntries(IEnumerable<ChronicleEntry> entries, int currentTick, int currentYear)
	{
		_entries.Clear();
		_nextEntryId = 1;
		CurrentTick = currentTick;
		CurrentYear = currentYear;
		if (entries != null)
		{
			foreach (ChronicleEntry entry in entries)
			{
				if (entry == null) continue;
				var copy = new ChronicleEntry
				{
					entryId = entry.entryId,
					tick = entry.tick,
					year = entry.year,
					eventType = entry.eventType,
					description = entry.description,
					major = entry.major
				};
				if (entry.participants != null) copy.participants.AddRange(entry.participants);
				_entries.Add(copy);
				if (copy.entryId >= _nextEntryId) _nextEntryId = copy.entryId + 1;
			}
		}
		while (_entries.Count > MaxEntries) _entries.RemoveAt(0);
	}

	public List<ChronicleEntry> GetMajorEntries()
	{
		List<ChronicleEntry> list = new List<ChronicleEntry>();
		for (int num = _entries.Count - 1; num >= 0; num--)
		{
			if (_entries[num].major)
			{
				list.Add(_entries[num]);
			}
		}
		return list;
	}

	public List<ChronicleEntry> GetEntriesByType(string eventType)
	{
		List<ChronicleEntry> list = new List<ChronicleEntry>();
		for (int num = _entries.Count - 1; num >= 0; num--)
		{
			if (_entries[num].eventType == eventType)
			{
				list.Add(_entries[num]);
			}
		}
		return list;
	}
}
}
