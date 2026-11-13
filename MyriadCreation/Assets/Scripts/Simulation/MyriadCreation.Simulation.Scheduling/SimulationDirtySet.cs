using System.Collections.Generic;

namespace MyriadCreation.Simulation.Scheduling
{
public sealed class SimulationDirtySet
{
	private readonly Dictionary<string, int> _versions = new Dictionary<string, int>();

	public int Count => _versions.Count;

	public void Mark(string key)
	{
		if (!string.IsNullOrWhiteSpace(key))
		{
			_versions.TryGetValue(key, out var value);
			_versions[key] = value + 1;
		}
	}

	public bool IsDirty(string key)
	{
		if (!string.IsNullOrWhiteSpace(key))
		{
			return _versions.ContainsKey(key);
		}
		return false;
	}

	public int GetVersion(string key)
	{
		if (string.IsNullOrWhiteSpace(key))
		{
			return 0;
		}
		if (!_versions.TryGetValue(key, out var value))
		{
			return 0;
		}
		return value;
	}

	public bool Clear(string key)
	{
		if (!string.IsNullOrWhiteSpace(key))
		{
			return _versions.Remove(key);
		}
		return false;
	}

	public bool ClearIfVersion(string key, int expectedVersion)
	{
		if (string.IsNullOrWhiteSpace(key))
		{
			return false;
		}
		if (!_versions.TryGetValue(key, out var value) || value != expectedVersion)
		{
			return false;
		}
		_versions.Remove(key);
		return true;
	}

	public void ClearAll()
	{
		_versions.Clear();
	}
}
}
