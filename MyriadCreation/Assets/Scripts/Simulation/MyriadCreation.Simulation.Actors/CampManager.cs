using System;
using System.Collections.Generic;
using MyriadCreation.Core.Data;
using MyriadCreation.Simulation.WorldState;
using MyriadCreation.World.Anchor;
using MyriadCreation.World.Settlement;
using UnityEngine;
using MyriadCreation.Content.Definitions;
using MyriadCreation.Core.Enums;

namespace MyriadCreation.Simulation.Actors
{
public class CampManager
{
	private readonly GameWorld _world;

	private readonly List<CampData> _camps = new List<CampData>();

	private readonly Dictionary<int, CampData> _campById = new Dictionary<int, CampData>();

	private int _nextCampId = 1;

	public IReadOnlyList<CampData> AllCamps => _camps;

	public int Count => _camps.Count;

	public IEnumerable<HabitationSite> ActiveHabitationSites
	{
		get
		{
			foreach (CampData camp in _camps)
			{
				if (!camp.isAbandoned && camp.mobility != CampMobility.Packed)
				{
					yield return camp;
				}
			}
		}
	}

	public CampManager(GameWorld world)
	{
		_world = world;
	}

	public CampData GetCamp(int campId)
	{
		_campById.TryGetValue(campId, out var value);
		return value;
	}

	public bool TryGetActiveSiteByAnchor(int anchorId, out HabitationSite site)
	{
		foreach (CampData camp in _camps)
		{
			if (!camp.isAbandoned && camp.anchorId == anchorId)
			{
				site = camp;
				return true;
			}
		}
		site = null;
		return false;
	}

	public List<CampData> GetCampsAtTile(int tileIndex)
	{
		List<CampData> list = new List<CampData>();
		foreach (CampData camp in _camps)
		{
			if (camp.tileIndex == tileIndex && !camp.isAbandoned && camp.mobility != CampMobility.Packed)
			{
				list.Add(camp);
			}
		}
		return list;
	}

	public List<CampData> GetCampsOfRealm(int realmId)
	{
		List<CampData> list = new List<CampData>();
		foreach (CampData camp in _camps)
		{
			if (camp.ownerRealmId == realmId && !camp.isAbandoned)
			{
				list.Add(camp);
			}
		}
		return list;
	}

	public CampData EstablishCamp(CampType type, int tileIndex, int population, int ownerRealmId = -1, int ownerActorId = -1, string name = "")
	{
		if (tileIndex < 0 || tileIndex >= _world.tiles.Length)
		{
			return null;
		}
		TileData tileData = _world.tiles[tileIndex];
		if (!tileData.exists || !tileData.isLand)
		{
			return null;
		}
		CampData campData = new CampData
		{
			campId = _nextCampId++,
			type = type,
			tileIndex = tileIndex,
			population = population,
			ownerRealmId = ownerRealmId,
			ownerActorId = ownerActorId,
			establishedDay = _world.currentDay,
			campName = (string.IsNullOrEmpty(name) ? (GetDefaultCampName(type) + "#" + _nextCampId) : name)
		};
		GameWorld world = _world;
		if (world.anchors == null)
		{
			world.anchors = new Dictionary<int, AnchorData>();
		}
		int num = 1;
		foreach (int key in _world.anchors.Keys)
		{
			if (key >= num)
			{
				num = key + 1;
			}
		}
		AnchorData anchorData = new AnchorData
		{
			anchorId = num,
			provinceId = tileData.provinceId,
			tileIndex = tileIndex,
			x = 0.5f,
			y = 0.5f
		};
		campData.anchorId = anchorData.anchorId;
		campData.anchor = anchorData;
		_world.anchors[anchorData.anchorId] = anchorData;
		_camps.Add(campData);
		_campById[campData.campId] = campData;
		tileData.campId = campData.campId;
		_world.tiles[tileIndex] = tileData;
		Debug.Log((object)$"[CampManager] 建立{type}营寨#{campData.campId}，地块={tileIndex}，人口={population}");
		return campData;
	}

	public void RestoreCamp(CampData camp)
	{
		if (camp == null)
		{
			throw new ArgumentNullException("camp");
		}
		if (camp.campId <= 0 || _campById.ContainsKey(camp.campId))
		{
			throw new InvalidOperationException($"Invalid or duplicate camp ID {camp.campId}.");
		}
		if (!Guid.TryParse(camp.siteId, out var _))
		{
			throw new InvalidOperationException($"Camp {camp.campId} has an invalid site UUID.");
		}
		if (camp.tileIndex < 0 || _world.tiles == null || camp.tileIndex >= _world.tiles.Length)
		{
			throw new InvalidOperationException($"Camp {camp.campId} has an invalid tile index.");
		}
		camp.isAbandoned = false;
		camp.InitializeModeData(camp.siteMode);
		_camps.Add(camp);
		_campById.Add(camp.campId, camp);
		_nextCampId = Math.Max(_nextCampId, camp.campId + 1);
		TileData tileData = _world.tiles[camp.tileIndex];
		tileData.campId = camp.campId;
		_world.tiles[camp.tileIndex] = tileData;
	}

	public void AbandonCamp(int campId)
	{
		if (_campById.TryGetValue(campId, out var value))
		{
			if (value.siteMode == SiteMode.FortifiedOutpost && !HabitationSiteTransitionSystem.TryTransition(value, SiteMode.TemporaryEncampment, out var failure))
			{
				Debug.LogError((object)$"[CampManager] 无法先将据点转为营地再消散：{failure}");
				return;
			}
			if (!HabitationSiteTransitionSystem.CanDissolve(value))
			{
				Debug.LogError((object)$"[CampManager] 只有临时营地可以消散，目标#{campId}模式={SiteModeKeys.ToKey(value.siteMode)}");
				return;
			}
			RemoveCamp(value);
			Debug.Log((object)$"[CampManager] 营寨#{campId}被废弃");
		}
	}

	public void RemoveTransferredCamp(int campId, string replacementSiteId)
	{
		if (!_campById.TryGetValue(campId, out var value))
		{
			throw new InvalidOperationException($"Camp {campId} does not exist for site transfer.");
		}
		if (value.siteId != replacementSiteId)
		{
			throw new InvalidOperationException($"Transferred site ID mismatch for camp {campId}.");
		}
		if (!_world.TryGetHabitationSite(replacementSiteId, out var site) || site == value)
		{
			throw new InvalidOperationException("Replacement habitation site '" + replacementSiteId + "' is not registered.");
		}
		RemoveCamp(value);
	}

	public void DestroyCamp(int campId, int attackerRealmId = -1)
	{
		if (!_campById.TryGetValue(campId, out var value))
		{
			return;
		}
		if (value.siteMode == SiteMode.FortifiedOutpost && !HabitationSiteTransitionSystem.TryTransition(value, SiteMode.TemporaryEncampment, out var failure))
		{
			Debug.LogError((object)$"[CampManager] 无法先将被摧毁据点转为营地：{failure}");
			return;
		}
		if (!HabitationSiteTransitionSystem.CanDissolve(value))
		{
			Debug.LogError((object)$"[CampManager] 只有临时营地可以在摧毁后消散，目标#{campId}模式={SiteModeKeys.ToKey(value.siteMode)}");
			return;
		}
		value.population = Mathf.Max(0f, value.population / 2f);
		if (value.tileIndex >= 0 && value.tileIndex < _world.tiles.Length)
		{
			TileData tileData = _world.tiles[value.tileIndex];
			if (tileData.campId == campId)
			{
				tileData.campId = -1;
			}
			tileData.order = Mathf.Max(0f, tileData.order - 10f);
			_world.tiles[value.tileIndex] = tileData;
		}
		RemoveCamp(value);
		Debug.Log((object)$"[CampManager] 营寨#{campId}被摧毁，攻击者政权={attackerRealmId}");
	}

	/// <summary>收起营地：脱离地面临时占用与锚点，但保留 campId/anchorId 以便原地或异地重扎。</summary>
	public bool PackCamp(int campId)
	{
		if (!_campById.TryGetValue(campId, out CampData camp) || camp == null || camp.isAbandoned)
		{
			return false;
		}
		if (camp.mobility == CampMobility.Packed) return true;
		if (camp.mobility == CampMobility.Moving) return false;
		ClearTileOccupancy(camp);
		if (camp.anchorId >= 0 && _world.anchors != null)
		{
			_world.anchors.Remove(camp.anchorId);
		}
		camp.anchor = null;
		camp.mobility = CampMobility.Packed;
		return true;
	}

	/// <summary>在指定地块重新扎营（Packed → Pitched）；地块须为陆地且可用。</summary>
	public bool UnpackCamp(int campId, int tileIndex)
	{
		if (!_campById.TryGetValue(campId, out CampData camp) || camp == null || camp.isAbandoned)
		{
			return false;
		}
		if (!IsValidCampTile(tileIndex)) return false;
		if (camp.mobility == CampMobility.Moving) return false;
		ClearTileOccupancy(camp);
		camp.tileIndex = tileIndex;
		camp.targetTileIndex = -1;
		PlaceAnchor(camp, tileIndex);
		OccupyTile(camp, tileIndex);
		camp.mobility = CampMobility.Pitched;
		return true;
	}

	/// <summary>开始迁移：从扎营或收起状态进入 Moving；锚点随队移动（收起状态则先在目的地重扎时才恢复）。</summary>
	public bool MoveCamp(int campId, int targetTileIndex)
	{
		if (!_campById.TryGetValue(campId, out CampData camp) || camp == null || camp.isAbandoned)
		{
			return false;
		}
		if (!IsValidCampTile(targetTileIndex)) return false;
		camp.targetTileIndex = targetTileIndex;
		camp.mobility = CampMobility.Moving;
		return true;
	}

	private void TickCampMovement(CampData camp)
	{
		if (camp == null || camp.mobility != CampMobility.Moving) return;
		if (camp.targetTileIndex < 0 || !IsValidCampTile(camp.targetTileIndex))
		{
			camp.mobility = CampMobility.Pitched;
			camp.targetTileIndex = -1;
			return;
		}
		int dx = Mathf.Clamp((camp.targetTileIndex % _world.mapWidth) - (camp.tileIndex % _world.mapWidth), -1, 1);
		int dy = Mathf.Clamp((camp.targetTileIndex / _world.mapWidth) - (camp.tileIndex / _world.mapWidth), -1, 1);
		int nextX = camp.tileIndex % _world.mapWidth + dx;
		int nextY = camp.tileIndex / _world.mapWidth + dy;
		int next = nextY * _world.mapWidth + nextX;
		// 迁移只走陆地；遇水/无效格则停一天，不改道（寻路由上层 AI 负责）。
		if (next >= 0 && next < _world.tiles.Length && _world.tiles[next].exists && _world.tiles[next].isLand)
		{
			ClearTileOccupancy(camp);
			camp.tileIndex = next;
			PlaceAnchor(camp, next);
			OccupyTile(camp, next);
		}
		if (camp.tileIndex == camp.targetTileIndex)
		{
			camp.targetTileIndex = -1;
			camp.mobility = CampMobility.Pitched;
		}
	}

	private bool IsValidCampTile(int tileIndex)
	{
		return tileIndex >= 0 && _world.tiles != null && tileIndex < _world.tiles.Length
			&& _world.tiles[tileIndex].exists && _world.tiles[tileIndex].isLand;
	}

	private void OccupyTile(CampData camp, int tileIndex)
	{
		TileData tile = _world.tiles[tileIndex];
		tile.campId = camp.campId;
		_world.tiles[tileIndex] = tile;
	}

	private void ClearTileOccupancy(CampData camp)
	{
		if (camp == null || _world.tiles == null) return;
		if (camp.tileIndex < 0 || camp.tileIndex >= _world.tiles.Length) return;
		TileData tile = _world.tiles[camp.tileIndex];
		if (tile.campId == camp.campId) tile.campId = -1;
		_world.tiles[camp.tileIndex] = tile;
	}

	private void PlaceAnchor(CampData camp, int tileIndex)
	{
		if (_world.anchors == null) _world.anchors = new Dictionary<int, AnchorData>();
		AnchorData anchor = camp.anchor;
		if (anchor == null)
		{
			anchor = new AnchorData
			{
				anchorId = camp.anchorId,
				provinceId = _world.tiles[tileIndex].provinceId,
				tileIndex = tileIndex
			};
		}
		anchor.tileIndex = tileIndex;
		anchor.provinceId = _world.tiles[tileIndex].provinceId;
		camp.anchor = anchor;
		_world.anchors[anchor.anchorId] = anchor;
	}

	public void Tick(float deltaDays)
	{
		for (int num = _camps.Count - 1; num >= 0; num--)
		{
			CampData campData = _camps[num];
			TickCampMovement(campData);
			campData.Tick(_world, deltaDays);
			if (campData.isAbandoned)
			{
				RemoveCamp(campData);
			}
		}
	}

	private void RemoveCamp(CampData camp)
	{
		camp.isAbandoned = true;
		if (camp.tileIndex >= 0 && camp.tileIndex < _world.tiles.Length)
		{
			TileData tileData = _world.tiles[camp.tileIndex];
			if (tileData.campId == camp.campId)
			{
				tileData.campId = -1;
			}
			_world.tiles[camp.tileIndex] = tileData;
		}
		_camps.Remove(camp);
		_campById.Remove(camp.campId);
	}

	private string GetDefaultCampName(CampType type)
	{
		return type switch
		{
			CampType.Temporary => "临时营地", 
			CampType.Military => "军营", 
			CampType.Bandit => "土匪寨", 
			CampType.Nomad => "游牧营地", 
			CampType.Refugee => "难民营", 
			_ => "营地", 
		};
	}

	public void Clear()
	{
		foreach (CampData camp in _camps)
		{
			if (camp.tileIndex >= 0 && _world.tiles != null && camp.tileIndex < _world.tiles.Length)
			{
				TileData tileData = _world.tiles[camp.tileIndex];
				if (tileData.campId == camp.campId)
				{
					tileData.campId = -1;
				}
				_world.tiles[camp.tileIndex] = tileData;
			}
		}
		_camps.Clear();
		_campById.Clear();
		_nextCampId = 1;
	}
}
}
