using System.Collections.Generic;
using MyriadCreation.Core.Contracts;
using MyriadCreation.Simulation.State;
using MyriadCreation.Simulation.Systems;

namespace MyriadCreation.Simulation.WorldState
{
/// <summary>
/// UI 4b3：ISettlementPeopleQuery 的 Simulation host 适配器。
/// 已知人物转调 CharacterLocationService.GetKnownAtSettlement；公共人物按 CharacterData.isPublicFigure/currentSettlementAnchorId 过滤。
/// 复用 CharacterQueryAdapter 的 DTO 映射，空世界/非法 anchor 返回空列表。
/// </summary>
public sealed class SettlementPeopleQueryAdapter : ISettlementPeopleQuery
{
	private readonly GameWorld _world;
	private readonly CharacterQueryAdapter _characters;

	public SettlementPeopleQueryAdapter(GameWorld world)
	{
		_world = world;
		_characters = new CharacterQueryAdapter(world);
	}

	public IReadOnlyList<CharacterRowDto> GetKnownCharactersAtAnchor(int anchorId, int observerRealmId = -1)
	{
		List<CharacterRowDto> list = new List<CharacterRowDto>();
		if (_world == null || anchorId < 0)
		{
			return list;
		}
		List<CharacterData> known = CharacterLocationService.GetKnownAtSettlement(_world, anchorId, observerRealmId);
		if (known != null)
		{
			foreach (CharacterData character in known)
			{
				if (character == null)
				{
					continue;
				}
				CharacterRowDto dto = _characters.GetCharacter(character.characterId);
				if (dto != null)
				{
					list.Add(dto);
				}
			}
		}
		list.Sort((a, b) => a.characterId.CompareTo(b.characterId));
		return list;
	}

	public IReadOnlyList<CharacterRowDto> GetPublicFiguresAtAnchor(int anchorId)
	{
		List<CharacterRowDto> list = new List<CharacterRowDto>();
		CharacterManager manager = _world?.GetCharacterManager();
		if (manager == null || anchorId < 0)
		{
			return list;
		}
		foreach (CharacterData character in manager.GetAllCharacters().Values)
		{
			if (character == null || !character.isAlive || !character.isPublicFigure
				|| character.currentSettlementAnchorId != anchorId)
			{
				continue;
			}
			CharacterRowDto dto = _characters.GetCharacter(character.characterId);
			if (dto != null)
			{
				list.Add(dto);
			}
		}
		list.Sort((a, b) => a.characterId.CompareTo(b.characterId));
		return list;
	}
}
}
