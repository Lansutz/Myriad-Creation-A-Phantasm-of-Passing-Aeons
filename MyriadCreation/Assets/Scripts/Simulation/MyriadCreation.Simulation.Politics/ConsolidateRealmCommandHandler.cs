using System;
using System.Collections.Generic;
using MyriadCreation.Core.Contracts;
using MyriadCreation.Core.Data;
using MyriadCreation.Simulation.State;
using UnityEngine;

namespace MyriadCreation.Simulation.Politics
{
public sealed class ConsolidateRealmCommandHandler : ISimulationCommandHandler<ConsolidateRealmCommand>
{
	private readonly Dictionary<int, RealmData> _realms;

	private readonly TileData[] _tiles;

	public ConsolidateRealmCommandHandler(Dictionary<int, RealmData> realms, TileData[] tiles)
	{
		_realms = realms ?? throw new ArgumentNullException("realms");
		_tiles = tiles ?? throw new ArgumentNullException("tiles");
	}

	public CommandResult Handle(ConsolidateRealmCommand command)
	{
		if (!_realms.TryGetValue(command.RealmId, out var value))
		{
			return CommandResult.Failed("realm_not_found");
		}
		int num = 0;
		foreach (int coreTile in value.coreTiles)
		{
			if (coreTile >= 0 && coreTile < _tiles.Length)
			{
				_tiles[coreTile].stability = Mathf.Min(100f, _tiles[coreTile].stability + 1f);
				_tiles[coreTile].order = Mathf.Min(100f, _tiles[coreTile].order + 0.5f);
				num++;
			}
		}
		if (num <= 0)
		{
			return CommandResult.Failed("consolidation_rejected");
		}
		return CommandResult.Succeeded("realm_consolidated");
	}
}
}
