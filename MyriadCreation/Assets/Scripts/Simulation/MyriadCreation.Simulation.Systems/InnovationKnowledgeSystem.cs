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

	/// <summary>政权掌握：取政权内角色的最高熟练度；没有角色掌握则为 0。</summary>
	public int GetRealmMastery(IEnumerable<int> characterIds, int innovationId)
	{
		int best = MinMastery;
		if (characterIds == null) return best;
		foreach (int characterId in characterIds)
		{
			best = Math.Max(best, GetMastery(characterId, innovationId));
			if (best >= MasteryLevel3) break;
		}
		return best;
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

	public InnovationKnowledgeSaveData CaptureState()
	{
		var result = new InnovationKnowledgeSaveData();
		foreach (KeyValuePair<int, Dictionary<int, byte>> character in _masteryByCharacter)
		{
			if (character.Value == null) continue;
			foreach (KeyValuePair<int, byte> mastery in character.Value)
			{
				result.masteries.Add(new InnovationMasteryEntry
				{
					characterId = character.Key,
					innovationId = mastery.Key,
					level = mastery.Value
				});
			}
		}
		result.masteries.Sort((a, b) =>
		{
			int byCharacter = a.characterId.CompareTo(b.characterId);
			return byCharacter != 0 ? byCharacter : a.innovationId.CompareTo(b.innovationId);
		});
		foreach (KeyValuePair<string, float> practice in _practiceByCharacterInnovation)
		{
			result.practices.Add(new InnovationPracticeEntry { key = practice.Key, amount = practice.Value });
		}
		result.practices.Sort((a, b) => string.CompareOrdinal(a.key, b.key));
		return result;
	}

	public void RestoreState(InnovationKnowledgeSaveData saved)
	{
		_masteryByCharacter.Clear();
		_practiceByCharacterInnovation.Clear();
		if (saved == null) return;
		if (saved.masteries != null)
		{
			foreach (InnovationMasteryEntry entry in saved.masteries)
			{
				if (entry == null || entry.characterId < 0 || entry.innovationId <= 0) continue;
				int level = Math.Max(MinMastery, Math.Min(MasteryLevel3, entry.level));
				if (level <= 0) continue;
				if (!_masteryByCharacter.TryGetValue(entry.characterId, out Dictionary<int, byte> character))
				{
					character = new Dictionary<int, byte>();
					_masteryByCharacter[entry.characterId] = character;
				}
				character[entry.innovationId] = (byte)level;
			}
		}
		if (saved.practices != null)
		{
			foreach (InnovationPracticeEntry entry in saved.practices)
			{
				if (entry == null || string.IsNullOrEmpty(entry.key) || entry.amount <= 0f) continue;
				_practiceByCharacterInnovation[entry.key] = entry.amount;
			}
		}
	}

	private static string MakeKey(int characterId, int innovationId)
	{
		return characterId + "_" + innovationId;
	}
}
}
