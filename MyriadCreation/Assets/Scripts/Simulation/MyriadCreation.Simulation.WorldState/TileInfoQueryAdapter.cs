using System.Collections.Generic;
using MyriadCreation.Core.Contracts;
using MyriadCreation.Core.Data;
using MyriadCreation.Core.Enums;
using MyriadCreation.Simulation.State;
using MyriadCreation.World;
using MyriadCreation.World.Settlement;

namespace MyriadCreation.Simulation.WorldState
{
/// <summary>
/// UI 4e：ITileInfoQuery 的 Simulation host 适配器。
/// 转调 GameWorld tiles/realms/provinces/settlements/goodsDefs；越界/空世界返回 null。
/// </summary>
public sealed class TileInfoQueryAdapter : ITileInfoQuery
{
	private readonly GameWorld _world;

	public TileInfoQueryAdapter(GameWorld world)
	{
		_world = world;
	}

	public TileInfoDto GetTileInfo(int tileIndex)
	{
		if (_world?.tiles == null || tileIndex < 0 || tileIndex >= _world.tiles.Length)
		{
			return null;
		}
		TileData tile = _world.tiles[tileIndex];
		TileInfoDto dto = new TileInfoDto
		{
			tileIndex = tileIndex,
			x = (_world.mapWidth > 0) ? tileIndex % _world.mapWidth : 0,
			y = (_world.mapWidth > 0) ? tileIndex / _world.mapWidth : 0,
			exists = tile.exists,
			isLand = tile.isLand,
			elevation01 = tile.elevation01,
			slopeDegree = tile.slopeDegree,
			terrainName = TerrainName(tile),
			biomeId = (int)tile.biome,
			biomeName = tile.biome.ToString(),
			fertility = tile.fertility,
			climateZoneId = (int)tile.climateZone,
			climateZoneName = tile.climateZone.ToString(),
			annualTemp = tile.annualTemp,
			annualPrecipMm = tile.annualPrecipMm,
			ownerRealmId = tile.ownerRealmId,
			provinceId = tile.provinceId,
			population = _world.GetTilePopulation(tileIndex),
			development = tile.development,
			stability = tile.stability
		};

		if (tile.ownerRealmId >= 0 && _world.realms != null
			&& _world.realms.TryGetValue(tile.ownerRealmId, out RealmData realm) && realm != null)
		{
			dto.ownerRealmName = realm.realmName ?? "";
		}
		if (tile.provinceId >= 0 && _world.provinces != null
			&& _world.provinces.TryGetValue(tile.provinceId, out Province province) && province != null)
		{
			dto.provinceName = province.provinceName ?? "";
		}
		if (_world.TryGetSiteAtTile(tileIndex, out HabitationSite site) && site != null)
		{
			dto.settlementAnchorId = site.anchorId;
			dto.settlementName = site.siteName ?? "";
		}

		List<TileResourceDto> resources = new List<TileResourceDto>();
		if (tile.resources != null)
		{
			foreach (TileResource resource in tile.resources)
			{
				string goodsName = "";
				if (_world.goodsDefs != null && _world.goodsDefs.TryGetValue(resource.goodsId, out var goods))
				{
					goodsName = goods.goodsName ?? "";
				}
				resources.Add(new TileResourceDto
				{
					goodsId = resource.goodsId,
					goodsName = goodsName,
					abundance = resource.abundance,
					discovered = resource.discovered,
					developed = resource.developed
				});
			}
		}
		dto.resources = resources;
		return dto;
	}

	private static string TerrainName(TileData tile)
	{
		if (!tile.isLand)
		{
			return "海洋";
		}
		if (tile.elevation01 >= 0.7f)
		{
			return "高山";
		}
		if (tile.elevation01 >= 0.45f)
		{
			return "丘陵";
		}
		if (tile.elevation01 >= 0.2f)
		{
			return "平原";
		}
		return "低地";
	}
}
}
