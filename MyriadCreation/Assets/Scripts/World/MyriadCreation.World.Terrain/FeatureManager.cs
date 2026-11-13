using System.Collections.Generic;
using MyriadCreation.Core.Data;
using MyriadCreation.Core.Enums;
using UnityEngine;

namespace MyriadCreation.World.Terrain
{
public class FeatureManager
{
	public class LockedSnapshot
	{
		public List<int> indices;

		public List<float> elevations;

		public List<bool> isLand;

		public LockedSnapshot(int capacity)
		{
			indices = new List<int>(capacity);
			elevations = new List<float>(capacity);
			isLand = new List<bool>(capacity);
		}
	}

	public class FeatureRegion
	{
		public int featureId;

		public string name;

		public List<int> memberTiles;
	}

	private readonly TileData[] _tiles;

	private readonly int _width;

	private readonly int _height;

	private readonly HashSet<int> _lockedSet = new HashSet<int>();

	private readonly Dictionary<int, FeatureRegion> _features = new Dictionary<int, FeatureRegion>();

	private int _nextFeatureId = 1;

	public int LockedCount => _lockedSet.Count;

	public bool HasLocked => _lockedSet.Count > 0;

	public FeatureManager(TileData[] tiles, int width, int height)
	{
		_tiles = tiles;
		_width = width;
		_height = height;
		for (int i = 0; i < tiles.Length; i++)
		{
			if (tiles[i].locked)
			{
				_lockedSet.Add(i);
			}
		}
	}

	public void LockTile(int tileIndex)
	{
		if (tileIndex >= 0 && tileIndex < _tiles.Length)
		{
			_tiles[tileIndex].locked = true;
			_lockedSet.Add(tileIndex);
		}
	}

	public void UnlockTile(int tileIndex)
	{
		if (tileIndex >= 0 && tileIndex < _tiles.Length)
		{
			_tiles[tileIndex].locked = false;
			_lockedSet.Remove(tileIndex);
		}
	}

	public void ToggleLock(int tileIndex)
	{
		if (IsLocked(tileIndex))
		{
			UnlockTile(tileIndex);
		}
		else
		{
			LockTile(tileIndex);
		}
	}

	public bool IsLocked(int tileIndex)
	{
		return _lockedSet.Contains(tileIndex);
	}

	public void LockRegion(IEnumerable<int> tileIndices, string name = null)
	{
		FeatureRegion featureRegion = new FeatureRegion
		{
			featureId = _nextFeatureId++,
			name = (name ?? $"区域{_nextFeatureId}"),
			memberTiles = new List<int>(tileIndices)
		};
		_features[featureRegion.featureId] = featureRegion;
		foreach (int memberTile in featureRegion.memberTiles)
		{
			LockTile(memberTile);
		}
		Debug.Log((object)$"[FeatureManager] 锁定区域 '{featureRegion.name}'：{featureRegion.memberTiles.Count} 地块");
	}

	public void UnlockFeature(int featureId)
	{
		if (!_features.TryGetValue(featureId, out var value))
		{
			return;
		}
		foreach (int memberTile in value.memberTiles)
		{
			UnlockTile(memberTile);
		}
		_features.Remove(featureId);
	}

	public void UnlockAll()
	{
		foreach (int item in _lockedSet)
		{
			_tiles[item].locked = false;
		}
		_lockedSet.Clear();
		_features.Clear();
		Debug.Log((object)"[FeatureManager] 已解锁全部地块");
	}

	public IReadOnlyCollection<int> GetLockedTiles()
	{
		return _lockedSet;
	}

	public LockedSnapshot SaveLockedSnapshot()
	{
		LockedSnapshot lockedSnapshot = new LockedSnapshot(_lockedSet.Count);
		foreach (int item in _lockedSet)
		{
			lockedSnapshot.indices.Add(item);
			lockedSnapshot.elevations.Add(_tiles[item].elevation01);
			lockedSnapshot.isLand.Add(_tiles[item].isLand);
		}
		return lockedSnapshot;
	}

	public void RestoreLockedTiles(LockedSnapshot snapshot)
	{
		if (snapshot == null || snapshot.indices.Count == 0)
		{
			return;
		}
		for (int i = 0; i < snapshot.indices.Count; i++)
		{
			int num = snapshot.indices[i];
			if (num >= 0 && num < _tiles.Length)
			{
				_tiles[num].elevation01 = snapshot.elevations[i];
				_tiles[num].isLand = snapshot.isLand[i];
				if (!_tiles[num].isLand)
				{
					float num2 = 0.5f;
					ref GameEnums.OceanTier oceanTier = ref _tiles[num].oceanTier;
					GameEnums.OceanTier oceanTier2;
					if (_tiles[num].elevation01 < num2 * 0.3f)
					{
						oceanTier2 = GameEnums.OceanTier.DeepSea;
					}
					else
					{
						oceanTier2 = ((_tiles[num].elevation01 < num2 * 0.7f) ? GameEnums.OceanTier.NearSea : GameEnums.OceanTier.Coast);
					}
					oceanTier = oceanTier2;
					_tiles[num].oceanDepth01 = Mathf.Max(0f, (num2 - _tiles[num].elevation01) / num2);
				}
				else
				{
					_tiles[num].oceanTier = GameEnums.OceanTier.Land;
					_tiles[num].oceanDepth01 = 0f;
				}
			}
		}
		Debug.Log((object)$"[FeatureManager] 已恢复 {snapshot.indices.Count} 个锁定地块的地形");
	}
}
}
