using System;
using System.Collections.Generic;
using UnityEngine;
using CivilizationEvolution.Core;
using CivilizationEvolution.Character;

namespace CivilizationEvolution.Disaster
{
 /// 灾害系统
 /// 自然灾害与人为灾害，有触发条件、影响范围、持续时间、后果
    [System.Serializable]
    public class DisasterSystem
    {
        private readonly TileData[] _tiles;
        private readonly int _width;
        private readonly int _height;
        private readonly List<ActiveDisaster> _activeDisasters = new List<ActiveDisaster>();
        private readonly Dictionary<DisasterType, DisasterDef> _disasterDefs = new Dictionary<DisasterType, DisasterDef>();

        public DisasterSystem(TileData[] tiles, int width, int height)
        {
            _tiles = tiles;
            _width = width;
            _height = height;
            InitializeDisasterDefs();
        }

        private void InitializeDisasterDefs()
        {
 // 气象灾害
            _disasterDefs[DisasterType.Drought] = new DisasterDef
            {
                type = DisasterType.Drought,
                name = "干旱",
                category = DisasterCategory.Meteorological,
                baseFrequency = 0.02f,
                minDuration = 30,
                maxDuration = 180,
                baseSeverity = 50f
            };
            _disasterDefs[DisasterType.Flood] = new DisasterDef
            {
                type = DisasterType.Flood,
                name = "洪水",
                category = DisasterCategory.Meteorological,
                baseFrequency = 0.015f,
                minDuration = 3,
                maxDuration = 14,
                baseSeverity = 70f
            };
            _disasterDefs[DisasterType.ColdWave] = new DisasterDef
            {
                type = DisasterType.ColdWave,
                name = "寒潮",
                category = DisasterCategory.Meteorological,
                baseFrequency = 0.01f,
                minDuration = 5,
                maxDuration = 20,
                baseSeverity = 40f
            };
            _disasterDefs[DisasterType.HeatWave] = new DisasterDef
            {
                type = DisasterType.HeatWave,
                name = "热浪",
                category = DisasterCategory.Meteorological,
                baseFrequency = 0.01f,
                minDuration = 5,
                maxDuration = 30,
                baseSeverity = 35f
            };
            _disasterDefs[DisasterType.Storm] = new DisasterDef
            {
                type = DisasterType.Storm,
                name = "风暴",
                category = DisasterCategory.Meteorological,
                baseFrequency = 0.02f,
                minDuration = 1,
                maxDuration = 5,
                baseSeverity = 55f
            };

 // 地质灾害
            _disasterDefs[DisasterType.Earthquake] = new DisasterDef
            {
                type = DisasterType.Earthquake,
                name = "地震",
                category = DisasterCategory.Geological,
                baseFrequency = 0.003f,
                minDuration = 1,
                maxDuration = 3,
                baseSeverity = 85f
            };
            _disasterDefs[DisasterType.VolcanicEruption] = new DisasterDef
            {
                type = DisasterType.VolcanicEruption,
                name = "火山喷发",
                category = DisasterCategory.Geological,
                baseFrequency = 0.002f,
                minDuration = 3,
                maxDuration = 30,
                baseSeverity = 90f
            };
            _disasterDefs[DisasterType.Landslide] = new DisasterDef
            {
                type = DisasterType.Landslide,
                name = "山体滑坡",
                category = DisasterCategory.Geological,
                baseFrequency = 0.008f,
                minDuration = 1,
                maxDuration = 3,
                baseSeverity = 60f
            };

 // 生物灾害
            _disasterDefs[DisasterType.LocustPlague] = new DisasterDef
            {
                type = DisasterType.LocustPlague,
                name = "蝗灾",
                category = DisasterCategory.Biological,
                baseFrequency = 0.01f,
                minDuration = 10,
                maxDuration = 60,
                baseSeverity = 65f
            };
            _disasterDefs[DisasterType.CropFailure] = new DisasterDef
            {
                type = DisasterType.CropFailure,
                name = "农作物歉收",
                category = DisasterCategory.Biological,
                baseFrequency = 0.03f,
                minDuration = 30,
                maxDuration = 90,
                baseSeverity = 45f
            };

 // 人为灾害
            _disasterDefs[DisasterType.Fire] = new DisasterDef
            {
                type = DisasterType.Fire,
                name = "火灾",
                category = DisasterCategory.Anthropogenic,
                baseFrequency = 0.025f,
                minDuration = 1,
                maxDuration = 7,
                baseSeverity = 50f
            };
            _disasterDefs[DisasterType.Famine] = new DisasterDef
            {
                type = DisasterType.Famine,
                name = "饥荒",
                category = DisasterCategory.Anthropogenic,
                baseFrequency = 0f, // 由经济系统触发
                minDuration = 30,
                maxDuration = 180,
                baseSeverity = 95f
            };
        }

 /// <summary>每日灾害Tick</summary>
        public void DailyTick(int currentDay, int currentYear)
        {
 // 更新活跃灾害
            for (int i = _activeDisasters.Count - 1; i >= 0; i--)
            {
                var disaster = _activeDisasters[i];
                disaster.remainingDays--;

 // 应用灾害效果
                ApplyDisasterEffects(disaster);

                if (disaster.remainingDays <= 0)
                {
                    Debug.Log($"[Disaster] {disaster.def.name} 结束，影响 {disaster.affectedTiles.Count} 个地块");
                    _activeDisasters.RemoveAt(i);
                }
            }

 // 随机触发新灾害
            TryTriggerDisasters(currentDay, currentYear);
        }

 /// <summary>尝试触发灾害</summary>
        private void TryTriggerDisasters(int currentDay, int currentYear)
        {
            foreach (var def in _disasterDefs.Values)
            {
                if (def.baseFrequency <= 0f) continue;

 // 季节修正
                float seasonMod = GetSeasonDisasterMod(def.type, currentDay);

 // 随机触发
                if (UnityEngine.Random.value < def.baseFrequency * seasonMod * 0.01f)
                {
                    TriggerDisaster(def.type, currentDay, currentYear);
                }
            }
        }

 /// <summary>触发灾害</summary>
        public ActiveDisaster TriggerDisaster(DisasterType type, int currentDay, int currentYear, int centerTile = -1)
        {
            if (!_disasterDefs.TryGetValue(type, out var def)) return null;

 // 选择中心地块
            if (centerTile < 0)
                centerTile = FindDisasterOriginTile(type);

 // 计算影响范围
            var affectedTiles = CalculateAffectedTiles(type, centerTile);
            if (affectedTiles.Count == 0) return null;

            var disaster = new ActiveDisaster
            {
                def = def,
                centerTile = centerTile,
                affectedTiles = affectedTiles,
                startDay = currentDay,
                startYear = currentYear,
                remainingDays = UnityEngine.Random.Range(def.minDuration, def.maxDuration + 1),
                severity = def.baseSeverity * UnityEngine.Random.Range(0.7f, 1.3f)
            };

            _activeDisasters.Add(disaster);
            Debug.Log($"[Disaster] {def.name} 爆发！中心地块 {centerTile}，影响 {affectedTiles.Count} 地块，持续 {disaster.remainingDays} 天");

 // 立即应用一次性效果
            ApplyImmediateEffects(disaster);

            return disaster;
        }

 /// <summary>寻找灾害起源地块</summary>
        private int FindDisasterOriginTile(DisasterType type)
        {
 // 根据灾害类型选择合适的起源地块
            var candidates = new List<int>();
            for (int i = 0; i < _tiles.Length; i++)
            {
                if (!_tiles[i].exists || !_tiles[i].isLand) continue;

                bool suitable = type switch
                {
                    DisasterType.Drought => _tiles[i].annualPrecipMm < 800f,
                    DisasterType.Flood => _tiles[i].annualPrecipMm > 1000f && _tiles[i].elevation01 < 0.3f,
                    DisasterType.ColdWave => _tiles[i].annualTemp < 10f,
                    DisasterType.HeatWave => _tiles[i].annualTemp > 20f,
                    DisasterType.Earthquake => _tiles[i].elevation01 > 0.4f,
                    DisasterType.VolcanicEruption => _tiles[i].elevation01 > 0.6f,
                    DisasterType.Landslide => _tiles[i].slopeDegree > 20f,
                    DisasterType.LocustPlague => _tiles[i].annualTemp > 15f && _tiles[i].annualPrecipMm < 600f,
                    _ => true
                };

                if (suitable) candidates.Add(i);
            }

            if (candidates.Count == 0)
                return UnityEngine.Random.Range(0, _tiles.Length);

            return candidates[UnityEngine.Random.Range(0, candidates.Count)];
        }

 /// <summary>计算受影响地块</summary>
        private List<int> CalculateAffectedTiles(DisasterType type, int centerTile)
        {
            var affected = new List<int>();
            int radius = type switch
            {
                DisasterType.Drought => 15,
                DisasterType.Flood => 5,
                DisasterType.ColdWave => 20,
                DisasterType.HeatWave => 12,
                DisasterType.Storm => 8,
                DisasterType.Earthquake => 6,
                DisasterType.VolcanicEruption => 8,
                DisasterType.Landslide => 2,
                DisasterType.LocustPlague => 10,
                DisasterType.CropFailure => 8,
                DisasterType.Fire => 3,
                DisasterType.Famine => 10,
                _ => 5
            };

 // 曼哈顿距离筛选
            int cx = centerTile % _width; // 地图宽度来自构造参数（修复：原硬编码128）
            int cy = centerTile / _width;

            for (int i = 0; i < _tiles.Length; i++)
            {
                if (!_tiles[i].exists || !_tiles[i].isLand) continue;
                int x = i % _width;
                int y = i / _width;
                int dist = Mathf.Abs(x - cx) + Mathf.Abs(y - cy);
                if (dist <= radius)
                    affected.Add(i);
            }

            return affected;
        }

 /// <summary>应用一次性效果</summary>
        private void ApplyImmediateEffects(ActiveDisaster disaster)
        {
            foreach (int tileIdx in disaster.affectedTiles)
            {
                ref TileData tile = ref _tiles[tileIdx];

                switch (disaster.def.type)
                {
                    case DisasterType.Earthquake:
                        tile.stability = Mathf.Max(0f, tile.stability - disaster.severity * 0.3f);
                        tile.development = Mathf.Max(0f, tile.development - disaster.severity * 0.01f);
                        break;
                    case DisasterType.VolcanicEruption:
                        tile.stability = Mathf.Max(0f, tile.stability - disaster.severity * 0.5f);
                        tile.fertility = Mathf.Min(1f, tile.fertility + 0.1f); // 火山灰增加肥力
                        break;
                    case DisasterType.Fire:
                        tile.development = Mathf.Max(0f, tile.development - disaster.severity * 0.02f);
                        break;
                }
            }
        }

 /// <summary>应用持续效果</summary>
        private void ApplyDisasterEffects(ActiveDisaster disaster)
        {
            foreach (int tileIdx in disaster.affectedTiles)
            {
                ref TileData tile = ref _tiles[tileIdx];

                switch (disaster.def.type)
                {
                    case DisasterType.Drought:
                        tile.soilHumidityPct = Mathf.Max(0f, tile.soilHumidityPct - 0.5f);
                        tile.fertility = Mathf.Max(0f, tile.fertility - 0.001f);
                        break;
                    case DisasterType.Flood:
                        tile.soilHumidityPct = Mathf.Min(100f, tile.soilHumidityPct + 2f);
                        tile.stability = Mathf.Max(0f, tile.stability - 0.1f);
                        break;
                    case DisasterType.ColdWave:
                        tile.annualTemp = Mathf.Max(-50f, tile.annualTemp - 0.5f);
                        break;
                    case DisasterType.HeatWave:
                        tile.annualTemp = Mathf.Min(50f, tile.annualTemp + 0.3f);
                        break;
                    case DisasterType.LocustPlague:
                        tile.fertility = Mathf.Max(0f, tile.fertility - 0.005f);
                        break;
                    case DisasterType.CropFailure:
                        tile.fertility = Mathf.Max(0f, tile.fertility - 0.002f);
                        break;
                    case DisasterType.Famine:
                        tile.stability = Mathf.Max(0f, tile.stability - 0.2f);
                        tile.order = Mathf.Max(0f, tile.order - 0.1f);
                        break;
                }
            }
        }

 /// <summary>季节灾害修正</summary>
        private float GetSeasonDisasterMod(DisasterType type, int dayOfYear)
        {
            int season = (dayOfYear - 1) / 91; // 0春 1夏 2秋 3冬
            return type switch
            {
                DisasterType.Drought => season == 1 ? 2f : (season == 3 ? 0.3f : 1f),
                DisasterType.Flood => season == 1 ? 2.5f : (season == 0 ? 1.5f : 0.5f),
                DisasterType.ColdWave => season == 3 ? 3f : (season == 0 ? 1.5f : 0.2f),
                DisasterType.HeatWave => season == 1 ? 3f : (season == 3 ? 0.1f : 0.8f),
                DisasterType.Storm => season == 1 ? 2f : (season == 3 ? 0.5f : 1f),
                DisasterType.LocustPlague => season == 1 ? 2.5f : (season == 3 ? 0.2f : 1f),
                DisasterType.CropFailure => season == 2 ? 2f : 1f,
                DisasterType.Fire => season == 1 ? 2f : (season == 3 ? 0.3f : 1f),
                _ => 1f
            };
        }

 // ===== 查询接口 =====
        public IReadOnlyList<ActiveDisaster> GetActiveDisasters() => _activeDisasters;

        public List<ActiveDisaster> GetDisastersAtTile(int tileIndex)
        {
            var result = new List<ActiveDisaster>();
            foreach (var d in _activeDisasters)
            {
                if (d.affectedTiles.Contains(tileIndex))
                    result.Add(d);
            }
            return result;
        }

        public bool IsTileAffectedByDisaster(int tileIndex, DisasterType type)
        {
            foreach (var d in _activeDisasters)
            {
                if (d.def.type == type && d.affectedTiles.Contains(tileIndex))
                    return true;
            }
            return false;
        }
    }

 /// <summary>活跃灾害实例</summary>

 /// <summary>灾害定义</summary>

 /// 疾病系统
 /// 传染病与地方病，有传播机制、感染率、死亡率、治疗

 /// <summary>活跃疾病实例</summary>

 /// <summary>疾病定义</summary>

}
