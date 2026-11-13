using System;
using System.Collections.Generic;
using MyriadCreation.Simulation.State;
using MyriadCreation.Simulation.Laws;
using MyriadCreation.Simulation.Faith;
using MyriadCreation.Content.Definitions;
using MyriadCreation.Simulation.LawHost;

namespace MyriadCreation.Simulation.Systems
{
[Serializable]
public class ThoughtManager
{
	private readonly Dictionary<int, SchoolOfThought> _schools = new Dictionary<int, SchoolOfThought>();

	private readonly Dictionary<int, FaithSystem> _faiths = new Dictionary<int, FaithSystem>();

	private readonly Dictionary<int, JudicialSystem> _judicialSystems = new Dictionary<int, JudicialSystem>();

	private readonly List<IdeologyMovement> _movements = new List<IdeologyMovement>();

	private int _nextSchoolId = 1;

	private int _nextFaithId = 1;

	private int _nextLawId = 1;

	private int _nextMovementId = 1;

	public SchoolOfThought CreateSchool(string name, int founderId, int year)
	{
		SchoolOfThought schoolOfThought = new SchoolOfThought
		{
			schoolId = _nextSchoolId++,
			schoolName = name,
			founderCharacterId = founderId,
			foundingYear = year
		};
		_schools[schoolOfThought.schoolId] = schoolOfThought;
		return schoolOfThought;
	}

	public FaithSystem CreateFaith(string name, FaithType type)
	{
		FaithSystem faithSystem = new FaithSystem
		{
			faithId = _nextFaithId++,
			faithName = name,
			type = type
		};
		_faiths[faithSystem.faithId] = faithSystem;
		return faithSystem;
	}

	public JudicialSystem CreateJudicialSystem(string name, LawSource source)
	{
		JudicialSystem judicialSystem = new JudicialSystem
		{
			judicialSystemId = _nextLawId++,
			judicialSystemName = name,
			source = source
		};
		_judicialSystems[judicialSystem.judicialSystemId] = judicialSystem;
		return judicialSystem;
	}

	public IdeologyMovement CreateMovement(string name, int originRegionId, int startYear)
	{
		IdeologyMovement ideologyMovement = new IdeologyMovement
		{
			movementId = _nextMovementId++,
			movementName = name,
			originRegionId = originRegionId,
			startYear = startYear
		};
		_movements.Add(ideologyMovement);
		return ideologyMovement;
	}

	public void DailyTick(int currentYear)
	{
		foreach (SchoolOfThought value in _schools.Values)
		{
			value.DailyTick();
		}
		foreach (FaithSystem value2 in _faiths.Values)
		{
			value2.DailyTick();
		}
		foreach (JudicialSystem value3 in _judicialSystems.Values)
		{
			value3.DailyTick();
		}
		for (int num = _movements.Count - 1; num >= 0; num--)
		{
			_movements[num].DailyTick(currentYear);
			if (_movements[num].phase == MovementPhase.Extinct)
			{
				_movements.RemoveAt(num);
			}
		}
	}

	public SchoolOfThought GetSchool(int id)
	{
		if (!_schools.TryGetValue(id, out var value))
		{
			return null;
		}
		return value;
	}

	public FaithSystem GetFaith(int id)
	{
		if (!_faiths.TryGetValue(id, out var value))
		{
			return null;
		}
		return value;
	}

	public JudicialSystem GetJudicialSystem(int id)
	{
		if (!_judicialSystems.TryGetValue(id, out var value))
		{
			return null;
		}
		return value;
	}

	public IdeologyMovement GetMovement(int id)
	{
		return _movements.Find((IdeologyMovement m) => m.movementId == id);
	}

	public IReadOnlyDictionary<int, SchoolOfThought> GetAllSchools()
	{
		return _schools;
	}

	public IReadOnlyDictionary<int, FaithSystem> GetAllFaiths()
	{
		return _faiths;
	}

	public IReadOnlyList<IdeologyMovement> GetAllMovements()
	{
		return _movements;
	}

	public FaithSystem GetDominantFaith(int regionId)
	{
		FaithSystem result = null;
		float num = 0f;
		foreach (FaithSystem value2 in _faiths.Values)
		{
			if (value2.regionAdherence.TryGetValue(regionId, out var value) && value > num)
			{
				num = value;
				result = value2;
			}
		}
		return result;
	}

	public SchoolOfThought GetDominantSchool(int regionId)
	{
		SchoolOfThought result = null;
		float num = 0f;
		foreach (SchoolOfThought value2 in _schools.Values)
		{
			if (value2.regionPenetration.TryGetValue(regionId, out var value) && value > num)
			{
				num = value;
				result = value2;
			}
		}
		return result;
	}
}
}
