using System;
using System.Collections.Generic;
using MyriadCreation.Core.Contracts;
using MyriadCreation.Core.Data;
using MyriadCreation.Simulation.State;
using UnityEngine;

namespace MyriadCreation.Simulation.Economy
{
public sealed class ImproveRealmEconomyCommandHandler : ISimulationCommandHandler<ImproveRealmEconomyCommand>
{
	private readonly Dictionary<int, RealmData> _realms;

	private readonly TileData[] _tiles;

	public ImproveRealmEconomyCommandHandler(Dictionary<int, RealmData> realms, TileData[] tiles)
	{
		_realms = realms ?? throw new ArgumentNullException("realms");
		_tiles = tiles ?? throw new ArgumentNullException("tiles");
	}

	public CommandResult Handle(ImproveRealmEconomyCommand command)
	{
		if (!_realms.TryGetValue(command.RealmId, out var value))
		{
			return CommandResult.Failed("realm_not_found");
		}
		int num = 0;
		foreach (int coreTile in value.coreTiles)
		{
			if (coreTile >= 0 && coreTile < _tiles.Length && !(value.treasury <= 50f))
			{
				_tiles[coreTile].development = Mathf.Min(1f, _tiles[coreTile].development + 0.01f);
				value.treasury -= 10f;
				num++;
			}
		}
		if (num <= 0)
		{
			return CommandResult.Failed("economy_investment_rejected");
		}
		return CommandResult.Succeeded("economy_improved");
	}
}
}
