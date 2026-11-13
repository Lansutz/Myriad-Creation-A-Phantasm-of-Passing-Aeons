using System.Collections.Generic;
using MyriadCreation.Simulation.State;

namespace MyriadCreation.Simulation.Systems
{
public static class CharacterKnowledgeRegistry
{
	private static readonly Dictionary<long, KnownCharacterInfo> _known = new Dictionary<long, KnownCharacterInfo>();

	public static void Clear()
	{
		_known.Clear();
	}

	public static void Learn(int observerRealmId, CharacterData character, int day, string source)
	{
		if (character != null)
		{
			_known[Key(observerRealmId, character.characterId)] = new KnownCharacterInfo
			{
				observerRealmId = observerRealmId,
				characterId = character.characterId,
				knownSettlementAnchorId = character.currentSettlementAnchorId,
				lastSeenDay = day,
				source = (source ?? "")
			};
		}
	}

	public static bool IsKnown(int observerRealmId, int characterId)
	{
		return _known.ContainsKey(Key(observerRealmId, characterId));
	}

	public static void Forget(int observerRealmId, int characterId)
	{
		_known.Remove(Key(observerRealmId, characterId));
	}

	private static long Key(int observerRealmId, int characterId)
	{
		return ((long)observerRealmId << 32) | (uint)characterId;
	}
}
}
