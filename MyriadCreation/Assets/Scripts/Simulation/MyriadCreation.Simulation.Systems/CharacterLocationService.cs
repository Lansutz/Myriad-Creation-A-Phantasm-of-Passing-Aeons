using System.Collections.Generic;
using MyriadCreation.Simulation.State;
using MyriadCreation.Simulation.WorldState;
using MyriadCreation.World.Settlement;

namespace MyriadCreation.Simulation.Systems
{
public static class CharacterLocationService
{
	public static void EnterSettlement(GameWorld world, int characterId, int anchorId, int tileIndex)
	{
		CharacterData characterData = world?.GetCharacterManager()?.GetCharacter(characterId);
		if (characterData != null)
		{
			if (characterData.currentSettlementAnchorId != anchorId)
			{
				RemoveCurrentOccupancy(world, characterData);
				characterData.currentBlockId = "";
			}
			characterData.currentSettlementAnchorId = anchorId;
			characterData.currentTile = tileIndex;
		}
	}

	public static void LeaveSettlement(GameWorld world, int characterId)
	{
		CharacterData characterData = world?.GetCharacterManager()?.GetCharacter(characterId);
		if (characterData != null)
		{
			RemoveCurrentOccupancy(world, characterData);
			characterData.currentSettlementAnchorId = -1;
			characterData.currentBlockId = "";
		}
	}

	/// <summary>
	/// 进入建筑块：锚点仍是聚落级权威位置，块级位置写人物 <see cref="CharacterData.currentBlockId"/>；
	/// 容量与占用者对账由 <see cref="World.Settlement.SettlementBlock"/> 负责。
	/// 返回 false 表示块不存在或已满，人物位置不变。
	/// </summary>
	public static bool EnterBlock(GameWorld world, int characterId, int anchorId, int tileIndex, string blockId)
	{
		CharacterData characterData = world?.GetCharacterManager()?.GetCharacter(characterId);
		if (characterData == null || string.IsNullOrEmpty(blockId))
		{
			return false;
		}
		if (world.settlements == null || !world.settlements.TryGetValue(anchorId, out SettlementData settlement) || settlement == null)
		{
			return false;
		}
		SettlementBlock block = settlement.FindBlock(blockId);
		if (block == null)
		{
			return false;
		}
		bool alreadyInside = string.Equals(characterData.currentBlockId, blockId, System.StringComparison.Ordinal) && block.ContainsOccupant(characterId);
		if (!alreadyInside)
		{
			if (!block.CanContain(characterId))
			{
				return false;
			}
			RemoveCurrentOccupancy(world, characterData);
			if (!block.TryAddOccupant(characterId))
			{
				return false;
			}
		}
		characterData.currentSettlementAnchorId = anchorId;
		characterData.currentTile = tileIndex;
		characterData.currentBlockId = blockId;
		return true;
	}

	public static bool LeaveBlock(GameWorld world, int characterId)
	{
		CharacterData characterData = world?.GetCharacterManager()?.GetCharacter(characterId);
		if (characterData == null)
		{
			return false;
		}
		bool removed = RemoveCurrentOccupancy(world, characterData);
		characterData.currentBlockId = "";
		return removed;
	}

	private static bool RemoveCurrentOccupancy(GameWorld world, CharacterData characterData)
	{
		if (characterData == null || string.IsNullOrEmpty(characterData.currentBlockId))
		{
			return false;
		}
		if (world?.settlements != null && world.settlements.TryGetValue(characterData.currentSettlementAnchorId, out SettlementData settlement) && settlement != null)
		{
			SettlementBlock block = settlement.FindBlock(characterData.currentBlockId);
			if (block != null)
			{
				return block.RemoveOccupant(characterData.characterId);
			}
		}
		return false;
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
