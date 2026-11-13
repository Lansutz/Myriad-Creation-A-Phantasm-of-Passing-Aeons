using System.Collections.Generic;
using MyriadCreation.Content.Definitions;

namespace MyriadCreation.Simulation.Systems
{
public class CharacterResearchManager
{
	public int realmId;

	private readonly Dictionary<int, CharacterResearchData> _characterResearch = new Dictionary<int, CharacterResearchData>();

	public const float MonthlyResearchCostPerCharacter = 5f;

	public CharacterResearchManager(int realmId)
	{
		this.realmId = realmId;
	}

	public CharacterResearchData GetCharacterResearch(int characterId)
	{
		if (!_characterResearch.TryGetValue(characterId, out var value))
		{
			value = new CharacterResearchData(characterId);
			_characterResearch[characterId] = value;
		}
		return value;
	}

	public bool StartCharacterResearch(int characterId, int innovationId, InnovationField field)
	{
		return GetCharacterResearch(characterId).StartResearch(innovationId, field);
	}

	public int GetResearcherCount(int innovationId)
	{
		int num = 0;
		foreach (KeyValuePair<int, CharacterResearchData> item in _characterResearch)
		{
			if (item.Value.currentResearchInnovationId == innovationId)
			{
				num++;
			}
		}
		return num;
	}

	public float GetTotalResearchEfficiency(int innovationId)
	{
		float num = 0f;
		foreach (KeyValuePair<int, CharacterResearchData> item in _characterResearch)
		{
			if (item.Value.currentResearchInnovationId == innovationId)
			{
				num += item.Value.ResearchEfficiency;
			}
		}
		return num;
	}

	public void DailyTick()
	{
		foreach (KeyValuePair<int, CharacterResearchData> item in _characterResearch)
		{
			item.Value.DailyTick();
		}
	}

	public void PracticeOldMethod(int characterId, float amount)
	{
		GetCharacterResearch(characterId).PracticeOldMethod(amount);
	}

	public void OnInnovationCompleted(int innovationId)
	{
		foreach (KeyValuePair<int, CharacterResearchData> item in _characterResearch)
		{
			if (item.Value.currentResearchInnovationId == innovationId)
			{
				item.Value.CompleteResearch();
			}
		}
	}

	public List<int> GetAllResearchers()
	{
		return new List<int>(_characterResearch.Keys);
	}
}
}
