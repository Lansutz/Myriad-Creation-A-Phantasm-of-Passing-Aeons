using System;
using System.Collections.Generic;
using MyriadCreation.Core.Contracts;
using MyriadCreation.Simulation.Laws;
using MyriadCreation.Simulation.State;
using MyriadCreation.Simulation.Systems;
using UnityEngine;

namespace MyriadCreation.Simulation.WorldState
{
/// <summary>
/// UI 4b 第一批：IRealmQuery 的 Simulation host 适配器。
/// 只把 RealmData/GameWorld 转成 Core DTO；不缓存、不引用泄漏；空世界返回 null/空列表。
/// </summary>
public sealed class RealmQueryAdapter : IRealmQuery
{
	private readonly GameWorld _world;

	public RealmQueryAdapter(GameWorld world)
	{
		_world = world;
	}

	public RealmSnapshotDto GetRealm(int realmId)
	{
		if (_world?.realms == null || !_world.realms.TryGetValue(realmId, out RealmData realm) || realm == null)
		{
			return null;
		}
		return ToDto(realm);
	}

	public IReadOnlyList<RealmSnapshotDto> GetAllRealms()
	{
		List<RealmSnapshotDto> list = new List<RealmSnapshotDto>();
		if (_world?.realms != null)
		{
			foreach (RealmData realm in _world.realms.Values)
			{
				if (realm != null)
				{
					list.Add(ToDto(realm));
				}
			}
		}
		list.Sort((a, b) => a.realmId.CompareTo(b.realmId));
		return list;
	}

	public IReadOnlyList<AdminDivisionNodeDto> GetAdminDivisions(int realmId)
	{
		List<AdminDivisionNodeDto> list = new List<AdminDivisionNodeDto>();
		if (_world?.realms == null || !_world.realms.TryGetValue(realmId, out RealmData realm)
			|| realm?.adminDivisions == null)
		{
			return list;
		}
		foreach (AdminDivision division in realm.adminDivisions)
		{
			if (division == null)
			{
				continue;
			}
			list.Add(new AdminDivisionNodeDto
			{
				divisionId = division.divisionId,
				realmId = division.realmId,
				name = division.name ?? "",
				level = division.level,
				parentId = division.parentDivisionId,
				tileCount = (division.tiles != null) ? division.tiles.Count : 0,
				titleId = division.titleId ?? "",
				holderCharacterId = division.holderCharacterId,
				holderName = GetCharacterName(division.holderCharacterId)
			});
		}
		return list;
	}

	private RealmSnapshotDto ToDto(RealmData realm)
	{
		int rulerId = realm.GetSupremeRulerId();
		Color color = RealmColor(realm.realmId);
		return new RealmSnapshotDto
		{
			realmId = realm.realmId,
			realmName = realm.realmName ?? "",
			colorR = color.r,
			colorG = color.g,
			colorB = color.b,
			territoryCount = CountTerritory(realm.realmId),
			population = SumPopulation(realm.realmId),
			treasury = realm.treasury,
			stability = realm.stability,
			legitimacy = (realm.lawState != null) ? realm.lawState.legitimacy : 0f,
			centralization = realm.centralization,
			isMonarchy = realm.laws != null
				&& realm.laws.supremeSovereignty == LawConstraints.SupremeSovereignty.Monarchy,
			rulerId = rulerId,
			rulerName = GetCharacterName(rulerId),
			governmentForm = (int)realm.realmForm,
			governmentLabel = RealmFormLabel((int)realm.realmForm),
			capitalAnchorId = FindCapitalAnchor(realm.realmId)
		};
	}

	private int CountTerritory(int realmId)
	{
		if (_world?.tiles != null)
		{
			int count = 0;
			for (int i = 0; i < _world.tiles.Length; i++)
			{
				if (_world.tiles[i].exists && _world.tiles[i].ownerRealmId == realmId)
				{
					count++;
				}
			}
			return count;
		}
		if (_world?.realms != null && _world.realms.TryGetValue(realmId, out RealmData realm)
			&& realm?.coreTiles != null)
		{
			return realm.coreTiles.Count;
		}
		return 0;
	}

	private float SumPopulation(int realmId)
	{
		if (_world?.tiles == null)
		{
			return 0f;
		}
		float sum = 0f;
		for (int i = 0; i < _world.tiles.Length; i++)
		{
			if (_world.tiles[i].exists && _world.tiles[i].ownerRealmId == realmId)
			{
				sum += _world.GetTilePopulation(i);
			}
		}
		return sum;
	}

	private int FindCapitalAnchor(int realmId)
	{
		if (_world?.settlements != null && _world.tiles != null)
		{
			foreach (var settlement in _world.settlements.Values)
			{
				if (settlement == null || !settlement.isCapital)
				{
					continue;
				}
				int tileIndex = settlement.tileIndex;
				if (tileIndex >= 0 && tileIndex < _world.tiles.Length
					&& _world.tiles[tileIndex].ownerRealmId == realmId)
				{
					return settlement.anchorId;
				}
			}
		}
		return -1;
	}

	private string GetCharacterName(int characterId)
	{
		if (characterId < 0)
		{
			return "";
		}
		var manager = _world?.GetCharacterManager();
		CharacterData character = manager?.GetCharacter(characterId);
		return character?.fullName ?? "";
	}

	private static string RealmFormLabel(int form)
	{
		switch (form)
		{
		case 0: return "定居";
		case 1: return "半定居";
		case 2: return "行国";
		default: return "";
		}
	}

	/// <summary>UI 展示用稳定色：与地图色板同公式族，但 Core DTO 只暴露 0..1 float。</summary>
	private static Color RealmColor(int realmId)
	{
		if (realmId < 0)
		{
			return new Color(0.5f, 0.5f, 0.5f, 1f);
		}
		float hue = (realmId * 0.6180339887f) % 1f;
		float sat = 0.55f + ((realmId >> 3) % 3) * 0.1f;
		float val = 0.62f + ((realmId >> 5) % 3) * 0.08f;
		return Color.HSVToRGB(hue, sat, val);
	}
}
}
