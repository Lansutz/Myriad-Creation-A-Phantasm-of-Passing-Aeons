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
				if (!camp.isAbandoned)
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
			if (camp.tileIndex == tileIndex && !camp.isAbandoned)
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

	public void Tick(float deltaDays)
	{
		for (int num = _camps.Count - 1; num >= 0; num--)
		{
			CampData campData = _camps[num];
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
