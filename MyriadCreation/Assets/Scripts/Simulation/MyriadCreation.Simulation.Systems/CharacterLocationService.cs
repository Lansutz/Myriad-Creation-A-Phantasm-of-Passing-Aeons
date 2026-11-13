using System.Collections.Generic;
using MyriadCreation.Simulation.State;
using MyriadCreation.Simulation.WorldState;

namespace MyriadCreation.Simulation.Systems
{
public static class CharacterLocationService
{
	public static void EnterSettlement(GameWorld world, int characterId, int anchorId, int tileIndex)
	{
		CharacterData characterData = world?.GetCharacterManager()?.GetCharacter(characterId);
		if (characterData != null)
		{
			characterData.currentSettlementAnchorId = anchorId;
			characterData.currentTile = tileIndex;
		}
	}

	public static void LeaveSettlement(GameWorld world, int characterId)
	{
		CharacterData characterData = world?.GetCharacterManager()?.GetCharacter(characterId);
		if (characterData != null)
		{
			characterData.currentSettlementAnchorId = -1;
		}
	}

	public static bool IsKnownTo(CharacterData character, int observerRealmId)
	{
		if (character != null)
		{
			return CharacterKnowledgeRegistry.IsKnown(observerRealmId, character.characterId);
		}
		return false;
	}

	public static List<CharacterData> GetKnownAtSettlement(GameWorld world, int anchorId, int observerRealmId)
	{
		List<CharacterData> list = new List<CharacterData>();
		CharacterManager characterManager = world?.GetCharacterManager();
		if (characterManager == null)
		{
			return list;
		}
		foreach (CharacterData value in characterManager.GetAllCharacters().Values)
		{
			if (value != null && value.isAlive && value.currentSettlementAnchorId == anchorId && IsKnownTo(value, observerRealmId))
			{
				list.Add(value);
			}
		}
		return list;
	}
}
}
