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
