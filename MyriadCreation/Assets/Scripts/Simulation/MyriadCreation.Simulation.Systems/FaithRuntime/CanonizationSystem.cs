using System.Collections.Generic;
using MyriadCreation.Simulation.Definitions;
using MyriadCreation.Simulation.State;
using MyriadCreation.Simulation.Systems;
using MyriadCreation.Content.Definitions;
using MyriadCreation.Simulation.Faith;

namespace MyriadCreation.Simulation.FaithHost
{
public static class CanonizationSystem
{
	private static int _nextSaintId = 1;

	private static readonly Dictionary<int, List<SaintDef>> _saintsByFaith = new Dictionary<int, List<SaintDef>>();

	private static IReadOnlyList<string> GetTraitIds(CharacterData c)
	{
		List<string> list = new List<string>();
		if (c?.traits != null)
		{
			foreach (PersonalityTrait trait in c.traits)
			{
				if (trait != null)
				{
					list.Add(trait.traitId);
				}
			}
		}
		return list;
	}

	public static bool IsCanonizationCandidate(FaithSystem faith, CharacterData c)
	{
		if (faith == null || c == null)
		{
			return false;
		}
		if (c.isAlive)
		{
			return false;
		}
		if (c.spiritualFulfillment < 80f)
		{
			return false;
		}
		return faith.GetVirtueScore(GetTraitIds(c)) >= 2;
	}

	public static SaintDef Canonize(FaithSystem faith, CharacterData c, string domain)
	{
		if (!IsCanonizationCandidate(faith, c))
		{
			return null;
		}
		if (faith.highPriestCharacterId < 0)
		{
			return null;
		}
		if (!_saintsByFaith.TryGetValue(faith.faithId, out var value))
		{
			value = (_saintsByFaith[faith.faithId] = new List<SaintDef>());
		}
		foreach (SaintDef item in value)
		{
			if (item.linkedCharacterId == c.characterId)
			{
				return item;
			}
		}
		SaintDef saintDef = new SaintDef
		{
			saintId = _nextSaintId++,
			linkedCharacterId = c.characterId,
			saintName = c.firstName + " " + c.lastName,
			domain = (string.IsNullOrEmpty(domain) ? "通用" : domain),
			faithId = faith.faithId,
			canonizationPiety = c.spiritualFulfillment
		};
		value.Add(saintDef);
		return saintDef;
	}

	public static List<SaintDef> GetSaints(int faithId)
	{
		if (!_saintsByFaith.TryGetValue(faithId, out var value))
		{
			return new List<SaintDef>();
		}
		return value;
	}

	public static void Reset()
	{
		_saintsByFaith.Clear();
	}
}
}
