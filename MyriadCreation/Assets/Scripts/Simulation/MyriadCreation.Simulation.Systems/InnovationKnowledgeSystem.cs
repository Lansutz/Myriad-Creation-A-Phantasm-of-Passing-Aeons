using System;
using System.Collections.Generic;
using MyriadCreation.Simulation.State;
using MyriadCreation.Content.Definitions;

namespace MyriadCreation.Simulation.Systems
{
public sealed class InnovationKnowledgeSystem
{
	public const int MinMastery = 0;

	public const int MasteryLevel1 = 1;

	public const int MasteryLevel2 = 2;

	public const int MasteryLevel3 = 3;

	private readonly Dictionary<int, Dictionary<int, byte>> _masteryByCharacter = new Dictionary<int, Dictionary<int, byte>>();

	private readonly Dictionary<string, float> _practiceByCharacterInnovation = new Dictionary<string, float>();

	public int GetMastery(int characterId, int innovationId)
	{
		if (_masteryByCharacter.TryGetValue(characterId, out var value) && value.TryGetValue(innovationId, out var value2))
		{
			return value2;
		}
		return 0;
	}

	public bool SetMastery(int characterId, int innovationId, int level)
	{
		if (characterId < 0 || innovationId <= 0)
		{
			return false;
		}
		level = Math.Max(0, Math.Min(3, level));
		if (!_masteryByCharacter.TryGetValue(characterId, out var value))
		{
			value = new Dictionary<int, byte>();
			_masteryByCharacter[characterId] = value;
		}
		int num = (value.TryGetValue(innovationId, out var value2) ? value2 : 0);
		if (level <= num)
		{
			return false;
		}
		value[innovationId] = (byte)level;
		return true;
	}

	public float RecordPractice(int characterId, int innovationId, float amount)
	{
		if (characterId < 0 || innovationId <= 0 || amount <= 0f)
		{
			return 0f;
		}
		string key = MakeKey(characterId, innovationId);
		_practiceByCharacterInnovation.TryGetValue(key, out var value);
		value += amount;
		_practiceByCharacterInnovation[key] = value;
		return value;
	}

	public float GetPractice(int characterId, int innovationId)
	{
		if (!_practiceByCharacterInnovation.TryGetValue(MakeKey(characterId, innovationId), out var value))
		{
			return 0f;
		}
		return value;
	}

	public bool Learn(int characterId, int innovationId, int targetLevel = 1)
	{
		if (characterId < 0 || innovationId <= 0)
		{
			return false;
		}
		int mastery = GetMastery(characterId, innovationId);
		int num = Math.Max(1, Math.Min(3, targetLevel));
		if (mastery >= num)
		{
			return false;
		}
		if (num > mastery + 1)
		{
			num = mastery + 1;
		}
		return SetMastery(characterId, innovationId, num);
	}

	public bool HasFoundation(int characterId, int innovationId)
	{
		return GetMastery(characterId, innovationId) >= 2;
	}

	public List<InnovationDef> GetDiscoveryCandidates(CharacterData character, InnovationTree innovations, int realmId)
	{
		List<InnovationDef> list = new List<InnovationDef>();
		if (character == null || innovations == null)
		{
			return list;
		}
		foreach (InnovationDef value in innovations.GetAllInnovations().Values)
		{
			if (!innovations.HasInnovation(realmId, value.innovationId) && IsKnowledgeNeighbor(character.characterId, value, innovations, realmId))
			{
				list.Add(value);
			}
		}
		return list;
	}

	private bool IsKnowledgeNeighbor(int characterId, InnovationDef def, InnovationTree innovations, int realmId)
	{
		List<int> prerequisiteIds = def.GetPrerequisiteIds();
		if (prerequisiteIds.Count == 0)
		{
			return true;
		}
		if (!innovations.ArePrerequisitesMet(null, realmId, def))
		{
			return false;
		}
		foreach (int prerequisite in prerequisiteIds)
		{
			if (GetMastery(characterId, prerequisite) >= 2)
			{
				return true;
			}
		}
		return false;
	}

	private static string MakeKey(int characterId, int innovationId)
	{
		return characterId + "_" + innovationId;
	}
}
}
