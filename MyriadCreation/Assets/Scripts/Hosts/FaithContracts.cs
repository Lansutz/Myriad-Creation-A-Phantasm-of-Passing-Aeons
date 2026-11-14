using System;
using System.Collections.Generic;
using MyriadCreation.Core.Contracts;
using MyriadCreation.Simulation.State;
using MyriadCreation.Simulation.WorldState;
using MyriadCreation.Simulation.Faith;
using MyriadCreation.Content.Definitions;

namespace MyriadCreation.Simulation.FaithHost
{
/// <summary>
/// 信仰系统契约适配器（B4）：实现 Core.Contracts 的 IFaithQuery/IFaithCommand，
/// 内部转调现有 FaithSystem / RealmData.stateReligionId；旧 API 全部保留。
/// </summary>
public sealed class FaithContracts : IFaithQuery, IFaithCommand
{
	private readonly GameWorld _world;

	public FaithContracts(GameWorld world)
	{
		_world = world;
	}

	private RealmData GetRealm(int realmId)
	{
		if (_world?.realms == null || !_world.realms.TryGetValue(realmId, out RealmData value))
		{
			return null;
		}
		return value;
	}

	private FaithSystem GetFaith(int faithId)
	{
		return _world?.GetFaithSystem(faithId);
	}

	// ===== IFaithQuery =====

	public int GetStateReligionId(int realmId)
	{
		return GetRealm(realmId)?.stateReligionId ?? -1;
	}

	public bool HasFaith(int faithId)
	{
		return GetFaith(faithId) != null;
	}

	public string GetFaithName(int faithId)
	{
		return GetFaith(faithId)?.faithName ?? "";
	}

	public float GetFervor(int faithId)
	{
		return GetFaith(faithId)?.fervor ?? 0f;
	}

	public IReadOnlyList<int> GetHolySiteTiles(int faithId)
	{
		FaithSystem faith = GetFaith(faithId);
		if (faith?.holySiteTileIndices == null)
		{
			return Array.Empty<int>();
		}
		return new List<int>(faith.holySiteTileIndices);
	}

	public bool TryGetFaithSnapshot(int faithId, out FaithSnapshotDto snapshot)
	{
		snapshot = null;
		FaithSystem faith = GetFaith(faithId);
		if (faith == null)
		{
			return false;
		}
		snapshot = new FaithSnapshotDto
		{
			faithId = faith.faithId,
			faithName = faith.faithName ?? "",
			fervor = faith.fervor,
			holySiteCount = ((faith.holySiteTileIndices != null) ? faith.holySiteTileIndices.Count : 0),
			faithType = (int)faith.type,
			faithOrganizationType = (int)faith.orgType,
			organizationType = (int)faith.orgType,
			hierarchyLevel = faith.hierarchyLevel,
			hierarchyName = HierarchyName(faith.hierarchyLevel),
			reformPillars = GetReformPillars(faith.faithId),
			authority = faith.CalculateReligiousAuthority(),
			isStateReligion = IsStateReligion(faith.faithId),
			highPriestCharacterId = faith.highPriestCharacterId
		};
		return true;
	}

	private bool IsStateReligion(int faithId)
	{
		if (_world?.realms == null)
		{
			return false;
		}
		if (_world.PlayerRealmId >= 0 && _world.realms.TryGetValue(_world.PlayerRealmId, out RealmData playerRealm)
			&& playerRealm != null)
		{
			return playerRealm.stateReligionId == faithId;
		}
		foreach (RealmData realm in _world.realms.Values)
		{
			if (realm != null && realm.stateReligionId == faithId)
			{
				return true;
			}
		}
		return false;
	}

	public bool CanDeclareGreatHolyWar(int realmId)
	{
		RealmData realm = GetRealm(realmId);
		if (realm == null)
		{
			return false;
		}
		FaithSystem faith = GetFaith(realm.stateReligionId);
		return faith != null && faith.CanDeclareGreatHolyWar();
	}

	public IReadOnlyList<FaithSaintDto> GetSaints(int faithId)
	{
		List<FaithSaintDto> list = new List<FaithSaintDto>();
		// 无世界时不暴露全局内容注册表里的圣人（避免 null-world 查询泄漏跨世界/跨测试数据）。
		if (_world == null)
		{
			return list;
		}
		List<SaintDef> saints = CanonizationSystem.GetSaints(faithId);
		if (saints != null)
		{
			foreach (SaintDef saint in saints)
			{
				if (saint == null)
				{
					continue;
				}
				list.Add(new FaithSaintDto
				{
					saintId = saint.saintId,
					saintName = saint.saintName ?? "",
					domain = saint.domain ?? "",
					canonizationPiety = saint.canonizationPiety,
					linkedCharacterId = saint.linkedCharacterId
				});
			}
		}
		list.Sort((a, b) => a.saintId.CompareTo(b.saintId));
		return list;
	}

	public IReadOnlyList<FaithDoctrineDto> GetDoctrines(int faithId)
	{
		List<FaithDoctrineDto> list = new List<FaithDoctrineDto>();
		if (_world == null)
		{
			return list;
		}
		ReligionDef religion = ReligionCatalog.Get(faithId);
		if (religion?.selectedDoctrines != null && religion.selectedDoctrines.Count > 0)
		{
			foreach (string doctrineId in religion.selectedDoctrines)
			{
				DoctrineOptionDef option = DoctrinePool.Get(doctrineId);
				if (option == null)
				{
					continue;
				}
				list.Add(new FaithDoctrineDto
				{
					doctrineId = doctrineId,
					name = option.optionName ?? "",
					pillar = option.pillar ?? "",
					description = option.description ?? ""
				});
			}
			return list;
		}

		FaithSystem faith = GetFaith(faithId);
		if (faith?.doctrines != null)
		{
			foreach (Doctrine doctrine in faith.doctrines)
			{
				list.Add(new FaithDoctrineDto
				{
					doctrineId = doctrine.name ?? "",
					name = doctrine.name ?? "",
					pillar = doctrine.category.ToString(),
					description = doctrine.description ?? ""
				});
			}
		}
		return list;
	}

	public int GetPatronSaintId(int realmId)
	{
		return GetRealm(realmId)?.statePatronSaintId ?? -1;
	}

	public string GetPatronSaintName(int realmId)
	{
		RealmData realm = GetRealm(realmId);
		if (realm == null || realm.statePatronSaintId <= 0)
		{
			return "";
		}
		List<SaintDef> saints = CanonizationSystem.GetSaints(realm.stateReligionId);
		if (saints != null)
		{
			foreach (SaintDef saint in saints)
			{
				if (saint != null && saint.saintId == realm.statePatronSaintId)
				{
					return saint.saintName ?? "";
				}
			}
		}
		return "";
	}

	private static string HierarchyName(int level)
	{
		switch (level)
		{
		case 0: return "无教阶（松散）";
		case 1: return "教区制";
		case 2: return "主教区制";
		case 3: return "大主教区制";
		case 4: return "枢机团/牧首会议";
		default: return level.ToString();
		}
	}

	private IReadOnlyList<string> GetReformPillars(int faithId)
	{
		List<string> pillars = new List<string>();
		HashSet<string> seen = new HashSet<string>();
		ReligionDef religion = ReligionCatalog.Get(faithId);
		if (religion?.selectedDoctrines != null)
		{
			foreach (string doctrineId in religion.selectedDoctrines)
			{
				DoctrineOptionDef option = DoctrinePool.Get(doctrineId);
				if (option != null && !string.IsNullOrEmpty(option.pillar) && seen.Add(option.pillar))
				{
					pillars.Add(option.pillar);
				}
			}
		}
		if (pillars.Count == 0)
		{
			FaithSystem faith = GetFaith(faithId);
			if (faith?.doctrines != null)
			{
				foreach (Doctrine doctrine in faith.doctrines)
				{
					string pillar = doctrine.category.ToString();
					if (seen.Add(pillar))
					{
						pillars.Add(pillar);
					}
				}
			}
		}
		return pillars;
	}

	// ===== IFaithCommand =====

	public bool SetStateReligion(int realmId, int faithId, out string reason)
	{
		reason = "";
		RealmData realm = GetRealm(realmId);
		if (realm == null)
		{
			reason = "政权不存在";
			return false;
		}
		FaithSystem faith = faithId >= 0 ? GetFaith(faithId) : null;
		if (faithId >= 0 && faith == null)
		{
			reason = "信仰不存在：" + faithId;
			return false;
		}
		if (faith != null && faith.religionId >= 0
			&& !ReligionCatalog.IsAvailable(faith.religionId,
				id => _world.Innovations != null && _world.Innovations.HasInnovation(realmId, id)))
		{
			reason = "缺少该宗教的前置革新";
			return false;
		}
		realm.stateReligionId = faithId;
		return true;
	}

	public bool AddFervor(int faithId, float delta, out string reason)
	{
		reason = "";
		FaithSystem faith = GetFaith(faithId);
		if (faith == null)
		{
			reason = "信仰不存在：" + faithId;
			return false;
		}
		faith.AddFervor(delta);
		return true;
	}
}
}
