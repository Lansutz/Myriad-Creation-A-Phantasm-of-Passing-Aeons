using System.Collections.Generic;
using MyriadCreation.Core.Contracts;
using MyriadCreation.Content.Definitions;
using MyriadCreation.Core.Data;
using MyriadCreation.Core.Enums;
using MyriadCreation.World;
using MyriadCreation.World.Anchor;
using MyriadCreation.Simulation.Systems;
using UnityEngine;
using MyriadCreation.World.Settlement;
using MyriadCreation.Simulation.Diplomacy;
using MyriadCreation.Simulation.State;
using MyriadCreation.Simulation.WorldState;

namespace MyriadCreation.Simulation.WorldState
{
/// <summary>
/// IMapWorldView 的 host 适配器（B7）：把 GameWorld 的宽高/地块/文化/外交/聚落锚点转成 Rendering 可用的只读视图。
/// 只缓存 MapWidth/MapHeight（世界生成后尺寸固定）；Wrap/Player/关系/聚落读取保持实时，避免世界重绑后陈旧。
/// </summary>
public sealed class MapWorldViewAdapter : IMapWorldView, IMapSettlementView, IMapOverlayView, IMapRealmView, IMapArmyView
{
	private readonly GameWorld _world;

	private readonly int _mapWidth;

	private readonly int _mapHeight;

	public MapWorldViewAdapter(GameWorld world)
	{
		_world = world;
		_mapWidth = ((world != null) ? world.mapWidth : 0);
		_mapHeight = ((world != null) ? world.mapHeight : 0);
	}

	public int MapWidth => _mapWidth;

	public int MapHeight => _mapHeight;

	public bool WrapX => _world != null && _world.config != null && _world.config.wrapX;

	public bool WrapY => _world != null && _world.config != null && _world.config.wrapY;

	public int PlayerRealmId => ((_world != null) ? _world.PlayerRealmId : -1);

	public MapWrapMode WrapMode => ((_world != null) ? _world.wrapMode : MapWrapMode.Flat);

	public bool IsProvinceBorder(int tileIndex)
	{
		if (_world == null || _world.tiles == null || tileIndex < 0 || tileIndex >= _world.tiles.Length)
		{
			return false;
		}
		return Province.IsBorder(_world.tiles, _mapWidth, _mapHeight, tileIndex);
	}

	public bool TileExists(int tileIndex)
	{
		return TryGetTile(tileIndex, out TileData tile) && tile.exists;
	}

	public bool TryGetTile(int tileIndex, out TileData tile)
	{
		tile = default;
		if (_world?.tiles == null || tileIndex < 0 || _mapWidth <= 0 || _mapHeight <= 0)
		{
			return false;
		}
		if (tileIndex >= _mapWidth * _mapHeight || tileIndex >= _world.tiles.Length)
		{
			return false;
		}
		tile = _world.tiles[tileIndex];
		return true;
	}

	public bool TryGetCulture(int cultureId, out MapCultureView culture)
	{
		culture = null;
		if (_world?.cultures == null || !_world.cultures.TryGetValue(cultureId, out var data) || data == null)
		{
			return false;
		}
		culture = new MapCultureView
		{
			cultureId = data.cultureId,
			colorR = data.color.r,
			colorG = data.color.g,
			colorB = data.color.b,
			colorA = data.color.a,
			parentCultureId = data.parentCultureId,
			allowsBranching = data.allowsBranching
		};
		return true;
	}

	public bool TryGetDiplomacyRelation(int viewerRealmId, int otherRealmId, out MapDiplomacyView relation)
	{
		relation = null;
		var manager = _world?.GetDiplomacyManager();
		if (manager == null)
		{
			return false;
		}
		var data = manager.GetRelation(viewerRealmId, otherRealmId);
		if (data == null)
		{
			return false;
		}
		bool activeAlliance = false;
		bool factionAlliance = false;
		if (data.activeAlliances != null)
		{
			foreach (var alliance in data.activeAlliances)
			{
				if (alliance == null || !alliance.isActive)
				{
					continue;
				}
				activeAlliance = true;
				if (alliance.type == AllianceType.Faction)
				{
					factionAlliance = true;
				}
			}
		}
		relation = new MapDiplomacyView
		{
			hasRelation = true,
			isAtWar = data.isAtWar,
			isHostile = data.IsHostile,
			relation = data.relation,
			hasActiveAlliance = activeAlliance,
			hasFactionAlliance = factionAlliance
		};
		return true;
	}

	public bool IsSubordinate(int viewerRealmId, int otherRealmId)
	{
		var manager = _world?.GetDiplomacyManager();
		return manager != null && manager.GetSubordination(viewerRealmId, otherRealmId) != null;
	}

	public IReadOnlyList<int> GetSettlementAnchorIds()
	{
		List<int> list = new List<int>();
		if (_world?.settlements != null)
		{
			foreach (int key in _world.settlements.Keys)
			{
				list.Add(key);
			}
		}
		return list;
	}
	// ===== IMapArmyView =====

	public int ArmyCount => ((_world?.armies != null) ? _world.armies.Count : 0);

	public IReadOnlyList<MapArmyInfo> GetArmies()
	{
		List<MapArmyInfo> list = new List<MapArmyInfo>();
		if (_world?.armies == null)
		{
			return list;
		}
		foreach (var army in _world.armies.Values)
		{
			if (army == null || army.state == GameEnums.CombatState.Dead)
			{
				continue;
			}
			list.Add(ToArmyInfo(army));
		}
		return list;
	}

	private MapArmyInfo ToArmyInfo(Army army)
	{
		return new MapArmyInfo
		{
			armyId = army.armyId,
			realmId = army.ownerRealmId,
			tileIndex = army.currentTileIndex,
			armyName = army.armyName,
			manpower = army.GetTotalManpower(_world?.unitDefs),
			organization = army.organization,
			combatState = (int)army.state,
			isMoving = army.movePath != null && army.movePath.Count > 0
		};
	}

	// ===== IMapSettlementView =====

	public int SettlementCount => ((_world?.settlements != null) ? _world.settlements.Count : 0);

	public int AnchorCount => ((_world?.anchors != null) ? _world.anchors.Count : 0);

	public IReadOnlyList<MapSettlementInfo> GetSettlements()
	{
		List<MapSettlementInfo> list = new List<MapSettlementInfo>();
		if (_world?.settlements != null)
		{
			foreach (var settlement in _world.settlements.Values)
			{
				if (settlement != null)
				{
					list.Add(ToSettlementInfo(settlement.anchorId >= 0 ? settlement.anchorId : 0, settlement));
				}
			}
		}
		return list;
	}

	public IReadOnlyList<MapSettlementInfo> GetMajorSettlements()
	{
		List<MapSettlementInfo> list = new List<MapSettlementInfo>();
		if (_world?.settlements != null)
		{
			foreach (var settlement in _world.settlements.Values)
			{
				if (settlement != null && settlement.IsMajorSettlement)
				{
					list.Add(ToSettlementInfo(settlement.anchorId >= 0 ? settlement.anchorId : 0, settlement));
				}
			}
		}
		return list;
	}

	public bool TryGetSettlement(int anchorId, out MapSettlementInfo settlement)
	{
		settlement = null;
		if (_world?.settlements == null || !_world.settlements.TryGetValue(anchorId, out var data) || data == null)
		{
			return false;
		}
		settlement = ToSettlementInfo(anchorId, data);
		return true;
	}

	public bool TryGetAnchorAtTile(int tileIndex, out int anchorId)
	{
		anchorId = -1;
		if (_world == null || tileIndex < 0)
		{
			return false;
		}
		if (_world.TryGetSiteAtTile(tileIndex, out var site) && site != null && site.anchorId >= 0)
		{
			anchorId = site.anchorId;
			return true;
		}
		return false;
	}

	private MapSettlementInfo ToSettlementInfo(int anchorIdKey, SettlementData data)
	{
		int tileIndex = data.tileIndex;
		if (tileIndex < 0 && _world?.anchors != null && data.anchorId >= 0
			&& _world.anchors.TryGetValue(data.anchorId, out var anchor) && anchor != null)
		{
			tileIndex = anchor.tileIndex;
		}
		return new MapSettlementInfo
		{
			anchorId = data.anchorId >= 0 ? data.anchorId : anchorIdKey,
			tileIndex = tileIndex,
			name = data.settlementName ?? "",
			isMajor = data.IsMajorSettlement,
			isCapital = data.isCapital,
			isPort = data.isPort,
			isRuined = data.IsRuined,
			siteMode = (int)data.siteMode,
			settlementForm = (int)data.settlementForm,
			settlementLevel = (int)data.settlementLevel,
			population = data.population
		};
	}

	// ===== IMapOverlayView =====

	public int ProvinceCount => ((_world?.provinces != null) ? _world.provinces.Count : 0);

	public bool TryGetAdminDivisionOverlay(out MapAdminDivisionOverlay overlay)
	{
		overlay = null;
		if (_world?.realms == null || _mapWidth <= 0 || _mapHeight <= 0)
		{
			return false;
		}
		int n = _mapWidth * _mapHeight;
		int[] divisionAt = new int[n];
		byte[] levelAt = new byte[n];
		for (int i = 0; i < n; i++)
		{
			divisionAt[i] = -1;
		}
		bool hasAnyDivisionTile = false;
		foreach (var realm in _world.realms.Values)
		{
			if (realm?.adminDivisions == null)
			{
				continue;
			}
			foreach (var division in realm.adminDivisions)
			{
				if (division?.tiles == null || division.level <= 1)
				{
					continue;
				}
				foreach (int tile in division.tiles)
				{
					if (tile < 0 || tile >= n || division.level <= levelAt[tile])
					{
						continue;
					}
					levelAt[tile] = (byte)Mathf.Min(255, division.level);
					divisionAt[tile] = division.divisionId;
					hasAnyDivisionTile = true;
				}
			}
		}
		if (!hasAnyDivisionTile)
		{
			return false;
		}
		overlay = new MapAdminDivisionOverlay { divisionIdByTile = divisionAt, levelByTile = levelAt };
		return true;
	}

	public bool TryGetVirtualControlOverlay(out MapVirtualControlOverlay overlay)
	{
		overlay = null;
		if (_world?.settlements == null || _world.tiles == null || _mapWidth <= 0 || _mapHeight <= 0)
		{
			return false;
		}
		VirtualControlResult result = VirtualControlCalculator.Calculate(_world.settlements, _world.tiles, _mapWidth, _mapHeight);
		if (result == null)
		{
			return false;
		}
		MapVirtualControlOverlay dto = new MapVirtualControlOverlay();
		if (result.virtualControlTiles != null)
		{
			foreach (var pair in result.virtualControlTiles)
			{
				dto.realmByTile[pair.Key] = pair.Value;
			}
		}
		if (result.connections != null)
		{
			foreach (var conn in result.connections)
			{
				dto.connectionFrom.Add(conn.Item1);
				dto.connectionTo.Add(conn.Item2);
			}
		}
		overlay = dto;
		return true;
	}

	public bool TryGetInfluenceOverlay(out MapInfluenceOverlay overlay)
	{
		overlay = null;
		if (_world?.settlements == null || _world.tiles == null || _mapWidth <= 0 || _mapHeight <= 0)
		{
			return false;
		}
		InfluenceField field = SettlementInfluenceSystem.LastField;
		if (field?.tierByTile == null || field.sourceAnchorByTile == null)
		{
			return false;
		}
		overlay = new MapInfluenceOverlay
		{
			tierByTile = field.tierByTile,
			sourceAnchorByTile = field.sourceAnchorByTile
		};
		return true;
	}

	// ===== IMapRealmView =====

	public IReadOnlyList<MapRealmInfo> GetNomadicRealms()
	{
		List<MapRealmInfo> list = new List<MapRealmInfo>();
		if (_world?.realms != null)
		{
			foreach (var realm in _world.realms.Values)
			{
				if (realm == null || realm.realmForm != RealmForm.Nomadic || realm.nomadicRange == null)
				{
					continue;
				}
				list.Add(new MapRealmInfo
				{
					realmId = realm.realmId,
					centerTile = realm.nomadicRange.centerTile,
					summerCampTile = realm.nomadicRange.summerCampTile,
					winterCampTile = realm.nomadicRange.winterCampTile,
					coreRadius = realm.nomadicRange.coreRadius,
					outerRadius = realm.nomadicRange.outerRadius,
					influence = realm.nomadicRange.influence
				});
			}
		}
		return list;
	}

}
}
