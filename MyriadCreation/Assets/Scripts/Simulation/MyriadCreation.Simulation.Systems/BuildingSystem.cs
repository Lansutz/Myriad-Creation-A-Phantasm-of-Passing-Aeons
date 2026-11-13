using System;
using System.Collections.Generic;
using MyriadCreation.Core.Data;
using MyriadCreation.Core.Enums;
using MyriadCreation.Core.Events;
using MyriadCreation.Simulation.State;
using UnityEngine;
using MyriadCreation.Content.Definitions;

namespace MyriadCreation.Simulation.Systems
{
[Serializable]
public class BuildingSystem
{
	private readonly TileData[] _tiles;

	private readonly Dictionary<int, BuildingDef> _buildingDefs = new Dictionary<int, BuildingDef>();

	private readonly Dictionary<int, List<ActiveBuilding>> _tileBuildings = new Dictionary<int, List<ActiveBuilding>>();

	private readonly SimulationEventBus _events;

	public InnovationTree Innovations { get; set; }

	public BuildingSystem(TileData[] tiles, SimulationEventBus events = null)
	{
		_tiles = tiles;
		_events = events;
		InitializeBuildingDefs();
	}

	private void InitializeBuildingDefs()
	{
		if (BuildingCatalog.IsLoaded)
		{
			foreach (KeyValuePair<int, BuildingDef> pair in BuildingCatalog.All)
			{
				_buildingDefs[pair.Key] = pair.Value;
			}
			return;
		}
		AddBuildingDef(100, "农田", BuildingCategory.Agriculture, 1, 100f, new Dictionary<int, float> { { 30, 20f } });
		AddBuildingDef(101, "灌溉渠", BuildingCategory.Agriculture, 2, 200f, new Dictionary<int, float>
		{
			{ 30, 30f },
			{ 40, 10f }
		});
		AddBuildingDef(102, "粮仓", BuildingCategory.Agriculture, 2, 300f, new Dictionary<int, float>
		{
			{ 30, 10f },
			{ 40, 20f }
		});
		AddBuildingDef(103, "磨坊", BuildingCategory.Agriculture, 3, 500f, new Dictionary<int, float> { { 31, 20f } });
		AddBuildingDef(200, "铁匠铺", BuildingCategory.Craft, 1, 200f, new Dictionary<int, float>
		{
			{ 50, 10f },
			{ 70, 5f }
		});
		AddBuildingDef(201, "木工坊", BuildingCategory.Craft, 1, 150f, new Dictionary<int, float> { { 30, 10f } });
		AddBuildingDef(202, "纺织坊", BuildingCategory.Craft, 2, 300f, new Dictionary<int, float> { { 11, 10f } });
		AddBuildingDef(203, "窑炉", BuildingCategory.Craft, 2, 400f, new Dictionary<int, float> { { 40, 15f } });
		AddBuildingDef(204, "造船厂", BuildingCategory.Craft, 3, 800f, new Dictionary<int, float>
		{
			{ 30, 50f },
			{ 70, 20f }
		});
		AddBuildingDef(300, "土路", BuildingCategory.Road, 1, 50f, new Dictionary<int, float> { { 40, 5f } });
		AddBuildingDef(301, "石砌路", BuildingCategory.Road, 2, 200f, new Dictionary<int, float> { { 40, 20f } });
		AddBuildingDef(302, "帝国大道", BuildingCategory.Road, 3, 500f, new Dictionary<int, float> { { 40, 50f } });
		AddBuildingDef(303, "桥梁", BuildingCategory.Road, 2, 300f, new Dictionary<int, float> { { 40, 30f } });
		AddBuildingDef(400, "木栅栏", BuildingCategory.Defense, 1, 100f, new Dictionary<int, float> { { 30, 20f } });
		AddBuildingDef(401, "石墙", BuildingCategory.Defense, 2, 400f, new Dictionary<int, float> { { 40, 50f } });
		AddBuildingDef(402, "箭塔", BuildingCategory.Defense, 2, 300f, new Dictionary<int, float>
		{
			{ 40, 20f },
			{ 70, 10f }
		});
		AddBuildingDef(403, "城堡", BuildingCategory.Defense, 3, 1000f, new Dictionary<int, float>
		{
			{ 40, 100f },
			{ 70, 30f }
		});
		AddBuildingDef(404, "要塞", BuildingCategory.Defense, 4, 2000f, new Dictionary<int, float>
		{
			{ 40, 200f },
			{ 70, 50f }
		});
		AddBuildingDef(500, "集市", BuildingCategory.Market, 1, 150f, new Dictionary<int, float> { { 30, 10f } });
		AddBuildingDef(501, "市场大厅", BuildingCategory.Market, 2, 400f, new Dictionary<int, float> { { 30, 30f } });
		AddBuildingDef(502, "商会会馆", BuildingCategory.Market, 3, 800f, new Dictionary<int, float> { { 30, 50f } });
		AddBuildingDef(503, "银行", BuildingCategory.Market, 4, 1500f, new Dictionary<int, float> { { 60, 100f } });
		AddBuildingDef(504, "海关", BuildingCategory.Market, 2, 300f, new Dictionary<int, float> { { 30, 20f } });
		AddBuildingDef(600, "村社公所", BuildingCategory.Admin, 1, 100f, new Dictionary<int, float> { { 30, 10f } });
		AddBuildingDef(601, "城镇厅", BuildingCategory.Admin, 2, 300f, new Dictionary<int, float> { { 40, 30f } });
		AddBuildingDef(602, "总督府", BuildingCategory.Admin, 3, 700f, new Dictionary<int, float> { { 40, 80f } });
		AddBuildingDef(603, "王宫", BuildingCategory.Admin, 4, 2000f, new Dictionary<int, float>
		{
			{ 40, 200f },
			{ 60, 100f }
		});
		AddBuildingDef(604, "法院", BuildingCategory.Admin, 2, 400f, new Dictionary<int, float> { { 40, 30f } });
		AddBuildingDef(605, "档案馆", BuildingCategory.Admin, 2, 300f, new Dictionary<int, float> { { 40, 20f } });
	}

	private void AddBuildingDef(int id, string name, BuildingCategory category, int tier, float cost, Dictionary<int, float> materials)
	{
		_buildingDefs[id] = new BuildingDef
		{
			buildingId = id,
			buildingName = name,
			category = category,
			tier = tier,
			buildCost = cost,
			materialCost = materials,
			buildDays = tier * 10
		};
	}

	public bool CanBuildBuilding(int tileIndex, int buildingId, int realmId, RealmData realm)
	{
		if (realm == null || tileIndex < 0 || tileIndex >= _tiles.Length)
		{
			return false;
		}
		if (!_buildingDefs.TryGetValue(buildingId, out var value))
		{
			return false;
		}
		if (_tiles[tileIndex].ownerRealmId != realmId)
		{
			return false;
		}
		if (realm.treasury < value.buildCost)
		{
			return false;
		}
		if (!_tileBuildings.ContainsKey(tileIndex))
		{
			_tileBuildings[tileIndex] = new List<ActiveBuilding>();
		}
		if (_tileBuildings[tileIndex].Count >= 5)
		{
			return false;
		}
		return true;
	}

	public bool BuildBuilding(int tileIndex, int buildingId, int realmId, RealmData realm)
	{
		if (!CanBuildBuilding(tileIndex, buildingId, realmId, realm))
		{
			return false;
		}
		return StartBuildingInternal(tileIndex, buildingId, realmId, realm, -1, -1, -1);
	}

	public bool StartPlannedBuilding(int tileIndex, int buildingId, int realmId, RealmData realm, int constructionPlanId, int builderCharacterId = -1, int innovationId = -1)
	{
		if (!CanBuildBuilding(tileIndex, buildingId, realmId, realm))
		{
			return false;
		}
		return StartBuildingInternal(tileIndex, buildingId, realmId, realm, constructionPlanId, builderCharacterId, innovationId);
	}

	private bool StartBuildingInternal(int tileIndex, int buildingId, int realmId, RealmData realm, int constructionPlanId, int builderCharacterId, int innovationId)
	{
		if (!_buildingDefs.TryGetValue(buildingId, out var value))
		{
			return false;
		}
		if (Innovations != null && !Innovations.IsUnlockGranted(realmId, InnovationUnlockKind.Building, buildingId))
		{
			return false;
		}
		if (!RealmTreasury.TrySpend(realm, value.buildCost))
		{
			return false;
		}
		ActiveBuilding item = new ActiveBuilding
		{
			buildingId = buildingId,
			tileIndex = tileIndex,
			realmId = realmId,
			constructionDays = value.buildDays,
			remainingDays = value.buildDays,
			isComplete = false,
			constructionPlanId = constructionPlanId,
			builderCharacterId = builderCharacterId,
			innovationId = innovationId
		};
		_tileBuildings[tileIndex].Add(item);
		return true;
	}

	public void DailyTick()
	{
		foreach (KeyValuePair<int, List<ActiveBuilding>> tileBuilding in _tileBuildings)
		{
			for (int num = tileBuilding.Value.Count - 1; num >= 0; num--)
			{
				ActiveBuilding activeBuilding = tileBuilding.Value[num];
				if (!activeBuilding.isComplete && !activeBuilding.ManagedByPlan)
				{
					AdvanceBuildingOneDay(activeBuilding);
				}
			}
		}
	}

	public float AdvancePlannedConstruction(int constructionPlanId, float deltaDays)
	{
		if (deltaDays <= 0f)
		{
			return 0f;
		}
		ActiveBuilding activeBuilding = null;
		foreach (KeyValuePair<int, List<ActiveBuilding>> tileBuilding in _tileBuildings)
		{
			for (int i = 0; i < tileBuilding.Value.Count; i++)
			{
				ActiveBuilding activeBuilding2 = tileBuilding.Value[i];
				if (!activeBuilding2.isComplete && activeBuilding2.constructionPlanId == constructionPlanId)
				{
					activeBuilding = activeBuilding2;
					break;
				}
			}
			if (activeBuilding != null)
			{
				break;
			}
		}
		if (activeBuilding == null)
		{
			return 1f;
		}
		_ = activeBuilding.remainingDays;
		activeBuilding.remainingDays = Mathf.Max(0, activeBuilding.remainingDays - Mathf.RoundToInt(deltaDays));
		if (activeBuilding.builderCharacterId >= 0 && activeBuilding.innovationId >= 0)
		{
			float amount = Mathf.Max(0.1f, deltaDays);
			_events?.Publish(new PracticeRecordedEvent(activeBuilding.builderCharacterId, activeBuilding.innovationId, amount));
		}
		if ((float)activeBuilding.remainingDays <= 0f)
		{
			activeBuilding.isComplete = true;
			OnBuildingComplete(activeBuilding);
		}
		return 1f - (float)activeBuilding.remainingDays / Mathf.Max(1f, (float)activeBuilding.constructionDays);
	}

	private void AdvanceBuildingOneDay(ActiveBuilding building)
	{
		building.remainingDays = Mathf.Max(0, building.remainingDays - 1);
		if (building.builderCharacterId >= 0 && building.innovationId >= 0)
		{
			_events?.Publish(new PracticeRecordedEvent(building.builderCharacterId, building.innovationId, 1f));
		}
		if ((float)building.remainingDays <= 0f)
		{
			building.isComplete = true;
			OnBuildingComplete(building);
		}
	}

	private void OnBuildingComplete(ActiveBuilding building)
	{
		if (_buildingDefs.TryGetValue(building.buildingId, out var value))
		{
			int category = (int)value.category;
			if (_tiles[building.tileIndex].buildingLevels[category] < value.tier)
			{
				_tiles[building.tileIndex].buildingLevels[category] = value.tier;
			}
			ApplyBuildingEffects(building.tileIndex, value, apply: true);
			Debug.Log((object)$"[Building] {value.buildingName} 建造完成，地块 {building.tileIndex}");
		}
	}

	private void ApplyBuildingEffects(int tileIndex, BuildingDef def, bool apply)
	{
		ref TileData reference = ref _tiles[tileIndex];
		float num = (apply ? 1f : (-1f));
		switch (def.category)
		{
		case BuildingCategory.Agriculture:
			reference.fertility = Mathf.Clamp(reference.fertility + (float)def.tier * 0.05f * num, 0f, 1f);
			break;
		case BuildingCategory.Road:
			reference.roadLevel = (GameEnums.RoadLevel)Mathf.Min(3, (int)(reference.roadLevel + (apply ? 1 : (-1))));
			break;
		case BuildingCategory.Defense:
			reference.stability = Mathf.Clamp(reference.stability + (float)def.tier * 2f * num, 0f, 100f);
			break;
		case BuildingCategory.Market:
			reference.development = Mathf.Clamp(reference.development + (float)def.tier * 0.02f * num, 0f, 1f);
			break;
		case BuildingCategory.Admin:
			reference.order = Mathf.Clamp(reference.order + (float)def.tier * 3f * num, 0f, 100f);
			break;
		case BuildingCategory.Craft:
			break;
		}
	}

	public bool DemolishBuilding(int tileIndex, int buildingId)
	{
		if (!_tileBuildings.TryGetValue(tileIndex, out var value))
		{
			return false;
		}
		ActiveBuilding activeBuilding = value.Find((ActiveBuilding b) => b.buildingId == buildingId);
		if (activeBuilding == null)
		{
			return false;
		}
		if (_buildingDefs.TryGetValue(buildingId, out var value2))
		{
			ApplyBuildingEffects(tileIndex, value2, apply: false);
		}
		value.Remove(activeBuilding);
		return true;
	}

	public BuildingDef? GetBuildingDef(int id)
	{
		if (!_buildingDefs.TryGetValue(id, out var value))
		{
			return null;
		}
		return value;
	}

	public List<ActiveBuilding> GetBuildingsAtTile(int tileIndex)
	{
		if (!_tileBuildings.TryGetValue(tileIndex, out var value))
		{
			return new List<ActiveBuilding>();
		}
		return value;
	}

	public IReadOnlyDictionary<int, BuildingDef> GetAllBuildingDefs()
	{
		return _buildingDefs;
	}
}
}
