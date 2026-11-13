using System.Collections.Generic;

namespace MyriadCreation.Simulation.Systems
{
public class RetinueSystem
{
	private readonly Dictionary<int, RetinueLevel> _realmLevels = new Dictionary<int, RetinueLevel>();

	private readonly Dictionary<int, List<RetinueBand>> _realmBands = new Dictionary<int, List<RetinueBand>>();

	public RetinueLevel GetLevel(int realmId)
	{
		if (!_realmLevels.TryGetValue(realmId, out var value))
		{
			return RetinueLevel.None;
		}
		return value;
	}

	public void SetLevel(int realmId, RetinueLevel level)
	{
		_realmLevels[realmId] = level;
	}

	public List<RetinueBand> GetBands(int realmId)
	{
		if (!_realmBands.TryGetValue(realmId, out var value))
		{
			return new List<RetinueBand>();
		}
		return value;
	}

	public void AddBand(int realmId, RetinueBand band)
	{
		if (!_realmBands.ContainsKey(realmId))
		{
			_realmBands[realmId] = new List<RetinueBand>();
		}
		_realmBands[realmId].Add(band);
	}

	public bool IsInstitutionalized(int realmId)
	{
		return GetLevel(realmId) >= RetinueLevel.Institutionalized;
	}

	public bool IsFeudalized(int realmId)
	{
		return GetLevel(realmId) >= RetinueLevel.Feudalized;
	}
}
}
