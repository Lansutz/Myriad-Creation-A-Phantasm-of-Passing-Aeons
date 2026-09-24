using System;
using System.Collections.Generic;
using UnityEngine;
using MyriadCreation.Core;
using MyriadCreation.Core.Events;
using MyriadCreation.Core.Constants;
using MyriadCreation.Core.Data;
using MyriadCreation.Core.Dto;
using MyriadCreation.Core.Enums;
using MyriadCreation.Simulation.Economy;
using MyriadCreation.Simulation.Events;
using MyriadCreation.Simulation.Generation;
using MyriadCreation.Simulation.Modding;
using MyriadCreation.Simulation.Politics;
using MyriadCreation.Simulation.Population;
using MyriadCreation.Simulation.WorldState;

namespace MyriadCreation.Simulation.Society
{
    /// 建筑基建系统
    /// 六大核心基建：农业、手工业、道路、城防、市场、行政
    [System.Serializable]
    public class BuildingSystem
    {
        private readonly TileData[] _tiles;
        private readonly Dictionary<int, BuildingDef> _buildingDefs = new Dictionary<int, BuildingDef>();
        private readonly Dictionary<int, List<ActiveBuilding>> _tileBuildings = new Dictionary<int, List<ActiveBuilding>>();
        private readonly SimulationEventBus _events;

        public BuildingSystem(TileData[] tiles, SimulationEventBus events = null)
        {
            _tiles = tiles;
            _events = events;
            InitializeBuildingDefs();
        }

        private void InitializeBuildingDefs()
        {
            AddBuildingDef(100, "农田", BuildingCategory.Agriculture, 1, 100f, new Dictionary<int, float> { { 30, 20f } });
            AddBuildingDef(101, "灌溉渠", BuildingCategory.Agriculture, 2, 200f, new Dictionary<int, float> { { 30, 30f }, { 40, 10f } });
            AddBuildingDef(102, "粮仓", BuildingCategory.Agriculture, 2, 300f, new Dictionary<int, float> { { 30, 10f }, { 40, 20f } });
            AddBuildingDef(103, "磨坊", BuildingCategory.Agriculture, 3, 500f, new Dictionary<int, float> { { 31, 20f } });

            AddBuildingDef(200, "铁匠铺", BuildingCategory.Craft, 1, 200f, new Dictionary<int, float> { { 50, 10f }, { 70, 5f } });
            AddBuildingDef(201, "木工坊", BuildingCategory.Craft, 1, 150f, new Dictionary<int, float> { { 30, 10f } });
            AddBuildingDef(202, "纺织坊", BuildingCategory.Craft, 2, 300f, new Dictionary<int, float> { { 11, 10f } });
            AddBuildingDef(203, "窑炉", BuildingCategory.Craft, 2, 400f, new Dictionary<int, float> { { 40, 15f } });
            AddBuildingDef(204, "造船厂", BuildingCategory.Craft, 3, 800f, new Dictionary<int, float> { { 30, 50f }, { 70, 20f } });

            AddBuildingDef(300, "土路", BuildingCategory.Road, 1, 50f, new Dictionary<int, float> { { 40, 5f } });
            AddBuildingDef(301, "石砌路", BuildingCategory.Road, 2, 200f, new Dictionary<int, float> { { 40, 20f } });
            AddBuildingDef(302, "帝国大道", BuildingCategory.Road, 3, 500f, new Dictionary<int, float> { { 40, 50f } });
            AddBuildingDef(303, "桥梁", BuildingCategory.Road, 2, 300f, new Dictionary<int, float> { { 40, 30f } });

            AddBuildingDef(400, "木栅栏", BuildingCategory.Defense, 1, 100f, new Dictionary<int, float> { { 30, 20f } });
            AddBuildingDef(401, "石墙", BuildingCategory.Defense, 2, 400f, new Dictionary<int, float> { { 40, 50f } });
            AddBuildingDef(402, "箭塔", BuildingCategory.Defense, 2, 300f, new Dictionary<int, float> { { 40, 20f }, { 70, 10f } });
            AddBuildingDef(403, "城堡", BuildingCategory.Defense, 3, 1000f, new Dictionary<int, float> { { 40, 100f }, { 70, 30f } });
            AddBuildingDef(404, "要塞", BuildingCategory.Defense, 4, 2000f, new Dictionary<int, float> { { 40, 200f }, { 70, 50f } });

            AddBuildingDef(500, "集市", BuildingCategory.Market, 1, 150f, new Dictionary<int, float> { { 30, 10f } });
            AddBuildingDef(501, "市场大厅", BuildingCategory.Market, 2, 400f, new Dictionary<int, float> { { 30, 30f } });
            AddBuildingDef(502, "商会会馆", BuildingCategory.Market, 3, 800f, new Dictionary<int, float> { { 30, 50f } });
            AddBuildingDef(503, "银行", BuildingCategory.Market, 4, 1500f, new Dictionary<int, float> { { 60, 100f } });
            AddBuildingDef(504, "海关", BuildingCategory.Market, 2, 300f, new Dictionary<int, float> { { 30, 20f } });

            AddBuildingDef(600, "村社公所", BuildingCategory.Admin, 1, 100f, new Dictionary<int, float> { { 30, 10f } });
            AddBuildingDef(601, "城镇厅", BuildingCategory.Admin, 2, 300f, new Dictionary<int, float> { { 40, 30f } });
            AddBuildingDef(602, "总督府", BuildingCategory.Admin, 3, 700f, new Dictionary<int, float> { { 40, 80f } });
            AddBuildingDef(603, "王宫", BuildingCategory.Admin, 4, 2000f, new Dictionary<int, float> { { 40, 200f }, { 60, 100f } });
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

        /// <summary>检查建造前置；不产生任何世界状态副作用。</summary>
        public bool CanBuildBuilding(int tileIndex, int buildingId, int realmId, RealmData realm)
        {
            if (realm == null || tileIndex < 0 || tileIndex >= _tiles.Length) return false;
            if (!_buildingDefs.TryGetValue(buildingId, out var def)) return false;
            if (_tiles[tileIndex].ownerRealmId != realmId) return false;
            if (realm.treasury < def.buildCost) return false;

            if (!_tileBuildings.ContainsKey(tileIndex))
                _tileBuildings[tileIndex] = new List<ActiveBuilding>();
            if (_tileBuildings[tileIndex].Count >= 5) return false;

            return true;
        }

        /// <summary>传统直接建造入口（兼容旧调用）；新系统应优先通过 ConstructionPlanSystem。</summary>
        public bool BuildBuilding(int tileIndex, int buildingId, int realmId, RealmData realm)
        {
            if (!CanBuildBuilding(tileIndex, buildingId, realmId, realm)) return false;
            return StartBuildingInternal(tileIndex, buildingId, realmId, realm, -1, -1, -1);
        }

        /// <summary>
        /// 创建由统一 Construction Plan 接管的建筑。
        /// 资金在计划创建时扣除，施工进度由 ConstructionPlanSystem 推进。
        /// </summary>
        public bool StartPlannedBuilding(int tileIndex, int buildingId, int realmId, RealmData realm,
            int constructionPlanId, int builderCharacterId = -1, int innovationId = -1)
        {
            if (!CanBuildBuilding(tileIndex, buildingId, realmId, realm)) return false;
            return StartBuildingInternal(tileIndex, buildingId, realmId, realm,
                constructionPlanId, builderCharacterId, innovationId);
        }

        private bool StartBuildingInternal(int tileIndex, int buildingId, int realmId, RealmData realm,
            int constructionPlanId, int builderCharacterId, int innovationId)
        {
            if (!_buildingDefs.TryGetValue(buildingId, out var def)) return false;

            realm.treasury -= def.buildCost;

            var building = new ActiveBuilding
            {
                buildingId = buildingId,
                tileIndex = tileIndex,
                realmId = realmId,
                constructionDays = def.buildDays,
                remainingDays = def.buildDays,
                isComplete = false,
                constructionPlanId = constructionPlanId,
                builderCharacterId = builderCharacterId,
                innovationId = innovationId
            };
            _tileBuildings[tileIndex].Add(building);
            return true;
        }

        /// <summary>每日建筑Tick；计划接管的施工不会被重复推进。</summary>
        public void DailyTick()
        {
            foreach (var kv in _tileBuildings)
            {
                for (int i = kv.Value.Count - 1; i >= 0; i--)
                {
                    var building = kv.Value[i];
                    if (building.isComplete || building.ManagedByPlan) continue;

                    AdvanceBuildingOneDay(building);
                }
            }
        }

        /// <summary>
        /// 推进某个计划施工。返回施工后的 0~1 进度。
        /// </summary>
        public float AdvancePlannedConstruction(int constructionPlanId, float deltaDays)
        {
            if (deltaDays <= 0f) return 0f;

            ActiveBuilding target = null;
            foreach (var kv in _tileBuildings)
            {
                for (int i = 0; i < kv.Value.Count; i++)
                {
                    var building = kv.Value[i];
                    if (!building.isComplete && building.constructionPlanId == constructionPlanId)
                    {
                        target = building;
                        break;
                    }
                }
                if (target != null) break;
            }

            if (target == null) return 1f;

            float before = target.remainingDays;
            target.remainingDays = Mathf.Max(0f, target.remainingDays - deltaDays);

            if (target.builderCharacterId >= 0 && target.innovationId >= 0)
            {
                float practice = Mathf.Max(0.1f, deltaDays);
                _events?.Publish(new PracticeRecordedEvent(target.builderCharacterId, target.innovationId, practice));
            }

            if (target.remainingDays <= 0f)
            {
                target.isComplete = true;
                OnBuildingComplete(target);
            }

            return 1f - target.remainingDays / Mathf.Max(1f, target.constructionDays);
        }

        private void AdvanceBuildingOneDay(ActiveBuilding building)
        {
            building.remainingDays = Mathf.Max(0f, building.remainingDays - 1f);
            if (building.builderCharacterId >= 0 && building.innovationId >= 0)
                _events?.Publish(new PracticeRecordedEvent(building.builderCharacterId, building.innovationId, 1f));

            if (building.remainingDays <= 0f)
            {
                building.isComplete = true;
                OnBuildingComplete(building);
            }
        }

        /// <summary>建筑完成时触发。</summary>
        private void OnBuildingComplete(ActiveBuilding building)
        {
            if (!_buildingDefs.TryGetValue(building.buildingId, out var def)) return;

            int categoryIndex = (int)def.category;
            if (_tiles[building.tileIndex].buildingLevels[categoryIndex] < def.tier)
                _tiles[building.tileIndex].buildingLevels[categoryIndex] = def.tier;

            ApplyBuildingEffects(building.tileIndex, def, true);
            Debug.Log($"[Building] {def.buildingName} 建造完成，地块 {building.tileIndex}");
        }

        private void ApplyBuildingEffects(int tileIndex, BuildingDef def, bool apply)
        {
            ref TileData tile = ref _tiles[tileIndex];
            float mod = apply ? 1f : -1f;

            switch (def.category)
            {
                case BuildingCategory.Agriculture:
                    tile.fertility = Mathf.Clamp(tile.fertility + def.tier * 0.05f * mod, 0f, 1f);
                    break;
                case BuildingCategory.Road:
                    tile.roadLevel = (GameEnums.RoadLevel)Mathf.Min(3, (int)tile.roadLevel + (apply ? 1 : -1));
                    break;
                case BuildingCategory.Defense:
                    tile.stability = Mathf.Clamp(tile.stability + def.tier * 2f * mod, 0f, 100f);
                    break;
                case BuildingCategory.Market:
                    tile.development = Mathf.Clamp(tile.development + def.tier * 0.02f * mod, 0f, 1f);
                    break;
                case BuildingCategory.Admin:
                    tile.order = Mathf.Clamp(tile.order + def.tier * 3f * mod, 0f, 100f);
                    break;
            }
        }

        public bool DemolishBuilding(int tileIndex, int buildingId)
        {
            if (!_tileBuildings.TryGetValue(tileIndex, out var buildings)) return false;
            var building = buildings.Find(b => b.buildingId == buildingId);
            if (building == null) return false;

            if (_buildingDefs.TryGetValue(buildingId, out var def))
                ApplyBuildingEffects(tileIndex, def, false);

            buildings.Remove(building);
            return true;
        }

        public BuildingDef? GetBuildingDef(int id) => _buildingDefs.TryGetValue(id, out var d) ? d : null;
        public List<ActiveBuilding> GetBuildingsAtTile(int tileIndex)
            => _tileBuildings.TryGetValue(tileIndex, out var b) ? b : new List<ActiveBuilding>();
        public IReadOnlyDictionary<int, BuildingDef> GetAllBuildingDefs() => _buildingDefs;
    }
}
