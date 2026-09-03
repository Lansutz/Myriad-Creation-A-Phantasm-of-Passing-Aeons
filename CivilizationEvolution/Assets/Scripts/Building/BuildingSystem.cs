using System.Collections.Generic;
using UnityEngine;
using CivilizationEvolution.Core;
using CivilizationEvolution.Economy;
using CivilizationEvolution.Politics;

namespace CivilizationEvolution.Building
{
    /// <summary>
    /// 建筑基建系统
    /// 六大核心基建：农业、手工业、道路、城防、市场、行政
    /// </summary>
    [System.Serializable]
    public class BuildingSystem
    {
        private readonly TileData[] _tiles;
        private readonly Dictionary<int, BuildingDef> _buildingDefs = new Dictionary<int, BuildingDef>();
        private readonly Dictionary<int, List<ActiveBuilding>> _tileBuildings = new Dictionary<int, List<ActiveBuilding>>();

        public BuildingSystem(TileData[] tiles)
        {
            _tiles = tiles;
            InitializeBuildingDefs();
        }

        private void InitializeBuildingDefs()
        {
            // 农业基建
            AddBuildingDef(100, "农田", BuildingCategory.Agriculture, 1, 100f, new Dictionary<int, float> { { 30, 20f } });
            AddBuildingDef(101, "灌溉渠", BuildingCategory.Agriculture, 2, 200f, new Dictionary<int, float> { { 30, 30f }, { 40, 10f } });
            AddBuildingDef(102, "粮仓", BuildingCategory.Agriculture, 2, 300f, new Dictionary<int, float> { { 30, 10f }, { 40, 20f } });
            AddBuildingDef(103, "磨坊", BuildingCategory.Agriculture, 3, 500f, new Dictionary<int, float> { { 31, 20f } });

            // 手工业基建
            AddBuildingDef(200, "铁匠铺", BuildingCategory.Craft, 1, 200f, new Dictionary<int, float> { { 50, 10f }, { 70, 5f } });
            AddBuildingDef(201, "木工坊", BuildingCategory.Craft, 1, 150f, new Dictionary<int, float> { { 30, 10f } });
            AddBuildingDef(202, "纺织坊", BuildingCategory.Craft, 2, 300f, new Dictionary<int, float> { { 11, 10f } });
            AddBuildingDef(203, "窑炉", BuildingCategory.Craft, 2, 400f, new Dictionary<int, float> { { 40, 15f } });
            AddBuildingDef(204, "造船厂", BuildingCategory.Craft, 3, 800f, new Dictionary<int, float> { { 30, 50f }, { 70, 20f } });

            // 道路基建
            AddBuildingDef(300, "土路", BuildingCategory.Road, 1, 50f, new Dictionary<int, float> { { 40, 5f } });
            AddBuildingDef(301, "石砌路", BuildingCategory.Road, 2, 200f, new Dictionary<int, float> { { 40, 20f } });
            AddBuildingDef(302, "帝国大道", BuildingCategory.Road, 3, 500f, new Dictionary<int, float> { { 40, 50f } });
            AddBuildingDef(303, "桥梁", BuildingCategory.Road, 2, 300f, new Dictionary<int, float> { { 40, 30f } });

            // 城防基建
            AddBuildingDef(400, "木栅栏", BuildingCategory.Defense, 1, 100f, new Dictionary<int, float> { { 30, 20f } });
            AddBuildingDef(401, "石墙", BuildingCategory.Defense, 2, 400f, new Dictionary<int, float> { { 40, 50f } });
            AddBuildingDef(402, "箭塔", BuildingCategory.Defense, 2, 300f, new Dictionary<int, float> { { 40, 20f }, { 70, 10f } });
            AddBuildingDef(403, "城堡", BuildingCategory.Defense, 3, 1000f, new Dictionary<int, float> { { 40, 100f }, { 70, 30f } });
            AddBuildingDef(404, "要塞", BuildingCategory.Defense, 4, 2000f, new Dictionary<int, float> { { 40, 200f }, { 70, 50f } });

            // 市场基建
            AddBuildingDef(500, "集市", BuildingCategory.Market, 1, 150f, new Dictionary<int, float> { { 30, 10f } });
            AddBuildingDef(501, "市场大厅", BuildingCategory.Market, 2, 400f, new Dictionary<int, float> { { 30, 30f } });
            AddBuildingDef(502, "商会会馆", BuildingCategory.Market, 3, 800f, new Dictionary<int, float> { { 30, 50f } });
            AddBuildingDef(503, "银行", BuildingCategory.Market, 4, 1500f, new Dictionary<int, float> { { 60, 100f } });
            AddBuildingDef(504, "海关", BuildingCategory.Market, 2, 300f, new Dictionary<int, float> { { 30, 20f } });

            // 行政基建
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

        /// <summary>建造建筑</summary>
        public bool BuildBuilding(int tileIndex, int buildingId, int realmId, RealmData realm)
        {
            if (tileIndex < 0 || tileIndex >= _tiles.Length) return false;
            if (!_buildingDefs.TryGetValue(buildingId, out var def)) return false;
            if (_tiles[tileIndex].ownerRealmId != realmId) return false;

            // 检查资源
            if (realm.treasury < def.buildCost) return false;

            // 检查建筑上限（每地块最多5个建筑）
            if (!_tileBuildings.ContainsKey(tileIndex))
                _tileBuildings[tileIndex] = new List<ActiveBuilding>();
            if (_tileBuildings[tileIndex].Count >= 5) return false;

            // 扣除资源
            realm.treasury -= def.buildCost;

            // 开始建造
            var building = new ActiveBuilding
            {
                buildingId = buildingId,
                tileIndex = tileIndex,
                realmId = realmId,
                constructionDays = def.buildDays,
                remainingDays = def.buildDays,
                isComplete = false
            };
            _tileBuildings[tileIndex].Add(building);

            return true;
        }

        /// <summary>每日建筑Tick（建造进度）</summary>
        public void DailyTick()
        {
            foreach (var kv in _tileBuildings)
            {
                for (int i = kv.Value.Count - 1; i >= 0; i--)
                {
                    var building = kv.Value[i];
                    if (!building.isComplete)
                    {
                        building.remainingDays--;
                        if (building.remainingDays <= 0)
                        {
                            building.isComplete = true;
                            OnBuildingComplete(building);
                        }
                    }
                }
            }
        }

        /// <summary>建筑完成时触发</summary>
        private void OnBuildingComplete(ActiveBuilding building)
        {
            if (!_buildingDefs.TryGetValue(building.buildingId, out var def)) return;

            // 更新地块基建等级
            int categoryIndex = (int)def.category;
            if (_tiles[building.tileIndex].buildingLevels[categoryIndex] < def.tier)
                _tiles[building.tileIndex].buildingLevels[categoryIndex] = def.tier;

            // 应用效果
            ApplyBuildingEffects(building.tileIndex, def, true);

            Debug.Log($"[Building] {def.buildingName} 建造完成，地块 {building.tileIndex}");
        }

        /// <summary>应用建筑效果</summary>
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

        /// <summary>拆除建筑</summary>
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

        // ===== 查询接口 =====
        public BuildingDef? GetBuildingDef(int id) => _buildingDefs.TryGetValue(id, out var d) ? d : null;
        public List<ActiveBuilding> GetBuildingsAtTile(int tileIndex)
        {
            return _tileBuildings.TryGetValue(tileIndex, out var b) ? b : new List<ActiveBuilding>();
        }
        public IReadOnlyDictionary<int, BuildingDef> GetAllBuildingDefs() => _buildingDefs;
    }

    /// <summary>建筑定义</summary>
    [System.Serializable]
    public struct BuildingDef
    {
        public int buildingId;
        public string buildingName;
        public BuildingCategory category;
        public int tier;
        public float buildCost;
        [System.NonSerialized]
        public Dictionary<int, float> materialCost;
        public int buildDays;
        public float maintenanceCost;
    }

    /// <summary>活跃建筑</summary>
    [System.Serializable]
    public class ActiveBuilding
    {
        public int buildingId;
        public int tileIndex;
        public int realmId;
        public int constructionDays;
        public int remainingDays;
        public bool isComplete;
    }

    public enum BuildingCategory
    {
        Agriculture = 0,  // 农业
        Craft = 1,        // 手工业
        Road = 2,         // 道路
        Defense = 3,      // 城防
        Market = 4,       // 市场
        Admin = 5         // 行政
    }
}
