using System;
using System.Collections.Generic;
using MyriadCreation.Core.Contracts;
using MyriadCreation.Simulation.Faith;
using MyriadCreation.Simulation.WorldState;

namespace MyriadCreation.Simulation.FaithHost
{
/// <summary>
/// IFaithContext 的 host 实现（B4）：把 GameWorld/FaithSystem/CharacterManager 收束为 id + 简单值；
/// Faith 运行时只依赖 Core.Contracts 接口，不引用 Simulation.State/Systems/WorldState。
/// </summary>
public sealed class FaithContextAdapter : IFaithContext
{
	private readonly GameWorld _world;

	public FaithContextAdapter(GameWorld world)
	{
		_world = world;
	}

	public int GetDay()
	{
		return ((_world != null) ? _world.currentDay : 0);
	}

	public int GetRealmStateReligionId(int realmId)
	{
		if (_world?.realms == null || !_world.realms.TryGetValue(realmId, out var realm) || realm == null)
		{
			return -1;
		}
		return realm.stateReligionId;
	}

	public float GetFaithFervor(int faithId)
	{
		FaithSystem faith = _world?.GetFaithSystem(faithId);
		if (faith == null)
		{
			return 0f;
		}
		return faith.fervor;
	}

	public IReadOnlyList<int> GetHolySiteIds(int faithId)
	{
		FaithSystem faith = _world?.GetFaithSystem(faithId);
		if (faith?.holySiteTileIndices == null)
		{
			return Array.Empty<int>();
		}
		return new List<int>(faith.holySiteTileIndices);
	}

	public int GetAdherenceCount(int faithId)
	{
		FaithSystem faith = _world?.GetFaithSystem(faithId);
		if (faith?.regionAdherence == null)
		{
			return 0;
		}
		return faith.regionAdherence.Count;
	}

	public string GetCharacterName(int characterId)
	{
		var manager = _world?.GetCharacterManager();
		var character = manager?.GetCharacter(characterId);
		if (character == null)
		{
			return "";
		}
		return (character.firstName ?? "") + " " + (character.lastName ?? "");
	}

	public IReadOnlyList<string> GetCharacterTraitIds(int characterId)
	{
		var manager = _world?.GetCharacterManager();
		var character = manager?.GetCharacter(characterId);
		if (character?.traits == null)
		{
			return Array.Empty<string>();
		}
		List<string> list = new List<string>();
		foreach (var trait in character.traits)
		{
			if (trait != null && !string.IsNullOrEmpty(trait.traitId))
			{
				list.Add(trait.traitId);
			}
		}
		return list;
	}
}
}
