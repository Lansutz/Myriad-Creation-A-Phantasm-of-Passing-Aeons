using System;
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
				characterRealmId = character.realmId,
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

	/// <summary>
	/// 信息滞后（P2.3）：非本政权的人物，超过 maxAgeDays 未有接触则从"已知"中移除；
	/// 本政权人物不因时间被遗忘（他们在视野内）。旧档缺少 characterRealmId 的条目保守保留。
	/// </summary>
	public static int PruneStale(int currentDay, int maxAgeDays)
	{
		if (maxAgeDays <= 0)
		{
			return 0;
		}
		List<long> stale = null;
		foreach (KeyValuePair<long, KnownCharacterInfo> pair in _known)
		{
			KnownCharacterInfo info = pair.Value;
			if (info.characterRealmId < 0 || info.characterRealmId == info.observerRealmId)
			{
				continue;
			}
			if (currentDay - info.lastSeenDay <= maxAgeDays)
			{
				continue;
			}
			stale ??= new List<long>();
			stale.Add(pair.Key);
		}
		if (stale == null)
		{
			return 0;
		}
		for (int i = 0; i < stale.Count; i++)
		{
			_known.Remove(stale[i]);
		}
		return stale.Count;
	}

	private static long Key(int observerRealmId, int characterId)
	{
		return ((long)observerRealmId << 32) | (uint)characterId;
	}
}
}
