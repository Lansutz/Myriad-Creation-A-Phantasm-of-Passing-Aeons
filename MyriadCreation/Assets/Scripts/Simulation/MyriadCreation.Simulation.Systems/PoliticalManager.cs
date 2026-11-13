using System.Collections.Generic;
using MyriadCreation.Core.Data;
using MyriadCreation.Simulation.State;
using UnityEngine;

namespace MyriadCreation.Simulation.Systems
{
public class PoliticalManager
{
	private readonly TileData[] _tiles;

	private readonly Dictionary<int, RealmData> _realms;

	public PoliticalManager(TileData[] tiles, Dictionary<int, RealmData> realms)
	{
		_tiles = tiles;
		_realms = realms;
	}

	public bool OccupyTile(int tileIndex, int realmId)
	{
		if (tileIndex < 0 || tileIndex >= _tiles.Length)
		{
			return false;
		}
		if (!_realms.ContainsKey(realmId))
		{
			return false;
		}
		_tiles[tileIndex].occupyingRealmId = realmId;
		_tiles[tileIndex].stability = Mathf.Max(0f, _tiles[tileIndex].stability - 20f);
		_tiles[tileIndex].order = Mathf.Max(0f, _tiles[tileIndex].order - 15f);
		return true;
	}

	public bool CedeTile(int tileIndex, int fromRealmId, int toRealmId)
	{
		if (tileIndex < 0 || tileIndex >= _tiles.Length)
		{
			return false;
		}
		if (_tiles[tileIndex].ownerRealmId != fromRealmId)
		{
			return false;
		}
		_tiles[tileIndex].ownerRealmId = toRealmId;
		_tiles[tileIndex].occupyingRealmId = -1;
		if (_realms.TryGetValue(fromRealmId, out var value))
		{
			value.coreTiles.Remove(tileIndex);
		}
		if (_realms.TryGetValue(toRealmId, out var value2))
		{
			value2.claimedTiles.Add(tileIndex);
		}
		return true;
	}

	public float CalculateControlDegree(int tileIndex)
	{
		if (tileIndex < 0 || tileIndex >= _tiles.Length)
		{
			return 0f;
		}
		ref TileData reference = ref _tiles[tileIndex];
		float num = (reference.stability + reference.order) / 200f;
		if (reference.occupyingRealmId != -1 && reference.occupyingRealmId != reference.ownerRealmId)
		{
			num *= 0.5f;
		}
		num += (float)reference.buildingLevels[3] * 0.05f;
		return Mathf.Clamp01(num);
	}

	public void DailyTick()
	{
		foreach (RealmData value in _realms.Values)
		{
			RealmStability.Drift(value, 50f, 0.001f);
			if (value.CalculateRebellionRisk() > 70f && Random.value < 0.01f)
			{
				Debug.Log((object)("[Politics] " + value.realmName + " 爆发叛乱！"));
			}
		}
	}
}
}
