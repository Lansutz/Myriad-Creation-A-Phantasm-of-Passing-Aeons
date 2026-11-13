using System;
using MyriadCreation.Core.Contracts;
using MyriadCreation.Core.Data;
using MyriadCreation.Core.Enums;
using MyriadCreation.Simulation.State;
using MyriadCreation.Simulation.Systems;

namespace MyriadCreation.Simulation.Diplomacy
{
public sealed class RaidSettlementCommandHandler : ISimulationCommandHandler<RaidSettlementCommand>
{
	private readonly DiplomacyManager _diplomacy;

	private readonly TileData[] _tiles;

	private readonly CharacterManager _characters;

	public RaidSettlementCommandHandler(DiplomacyManager diplomacy, TileData[] tiles, CharacterManager characters)
	{
		_diplomacy = diplomacy ?? throw new ArgumentNullException("diplomacy");
		_tiles = tiles ?? throw new ArgumentNullException("tiles");
		_characters = characters;
	}

	public CommandResult Handle(RaidSettlementCommand command)
	{
		(bool, bool, float) tuple = _diplomacy.RaidSettlement(command.RaiderRealmId, command.TargetRealmId, command.TargetTileIndex, command.RaidType, _tiles);
		if (!tuple.Item1)
		{
			return CommandResult.Failed("raid_rejected");
		}
		if (command.RaidType == GameEnums.RaidType.Massacre && _characters != null)
		{
			CharacterData characterData = _characters.FindRulerOfRealm(command.RaiderRealmId);
			if (characterData != null)
			{
				characterData.achievements.massacres++;
			}
		}
		return CommandResult.Succeeded(tuple.Item2 ? "raid_completed_war_declared" : "raid_completed");
	}
}
}
