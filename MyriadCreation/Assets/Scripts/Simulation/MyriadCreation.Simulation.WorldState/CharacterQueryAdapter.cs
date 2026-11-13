using System.Collections.Generic;
using MyriadCreation.Core.Contracts;
using MyriadCreation.Simulation.State;
using MyriadCreation.Simulation.Systems;

namespace MyriadCreation.Simulation.WorldState
{
/// <summary>
/// UI 4b 第一批：ICharacterQuery 的 Simulation host 适配器。
/// 转调 GameWorld.GetCharacterManager()/RealmData.officeHolders；空世界/无 manager 返回 null/空列表。
/// </summary>
public sealed class CharacterQueryAdapter : ICharacterQuery
{
	private readonly GameWorld _world;

	public CharacterQueryAdapter(GameWorld world)
	{
		_world = world;
	}

	public CharacterRowDto GetCharacter(int characterId)
	{
		CharacterData data = _world?.GetCharacterManager()?.GetCharacter(characterId);
		return data != null ? ToDto(data) : null;
	}

	public IReadOnlyList<CharacterRowDto> GetCharactersInRealm(int realmId)
	{
		List<CharacterRowDto> list = new List<CharacterRowDto>();
		CharacterManager manager = _world?.GetCharacterManager();
		if (manager != null)
		{
			List<CharacterData> characters = manager.GetCharactersByRealm(realmId);
			if (characters != null)
			{
				foreach (CharacterData data in characters)
				{
					if (data != null)
					{
						list.Add(ToDto(data));
					}
				}
			}
		}
		list.Sort((a, b) => a.characterId.CompareTo(b.characterId));
		return list;
	}

	public CharacterRowDto GetRuler(int realmId)
	{
		if (_world?.realms == null || !_world.realms.TryGetValue(realmId, out RealmData realm) || realm == null)
		{
			return null;
		}
		CharacterManager manager = _world.GetCharacterManager();
		if (manager == null)
		{
			return null;
		}
		int rulerId = realm.GetSupremeRulerId();
		CharacterData ruler = rulerId >= 0 ? manager.GetCharacter(rulerId) : null;
		if (ruler == null)
		{
			ruler = manager.FindRulerOfRealm(realmId);
		}
		return ruler != null ? ToDto(ruler) : null;
	}

	private CharacterRowDto ToDto(CharacterData data)
	{
		bool isRuler = false;
		if (_world?.realms != null && _world.realms.TryGetValue(data.realmId, out RealmData realm) && realm != null)
		{
			isRuler = realm.GetSupremeRulerId() == data.characterId;
		}
		if (!isRuler)
		{
			isRuler = (int)data.role == (int)MyriadCreation.Content.Definitions.CharacterRole.Ruler;
		}
		int flags = 0;
		if (data.isAlive)
		{
			flags |= CharacterRowDto.FlagAlive;
		}
		if (isRuler)
		{
			flags |= CharacterRowDto.FlagRuler;
		}
		if (data.isPublicFigure)
		{
			flags |= CharacterRowDto.FlagPublicFigure;
		}

		return new CharacterRowDto
		{
			characterId = data.characterId,
			name = data.fullName ?? "",
			age = data.age,
			isMale = data.isMale,
			realmId = data.realmId,
			role = (int)data.role,
			officeTitleId = FindOfficeTitleId(data),
			flags = flags
		};
	}

	private int FindOfficeTitleId(CharacterData data)
	{
		if (_world?.realms == null || !_world.realms.TryGetValue(data.realmId, out RealmData realm)
			|| realm?.officeHolders == null)
		{
			return -1;
		}
		int best = -1;
		foreach (var pair in realm.officeHolders)
		{
			if (pair.Value == data.characterId && (best < 0 || pair.Key < best))
			{
				best = pair.Key;
			}
		}
		return best;
	}
}
}
