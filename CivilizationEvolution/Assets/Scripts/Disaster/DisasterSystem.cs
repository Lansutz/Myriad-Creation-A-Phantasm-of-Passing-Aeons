using System;
using System.Collections.Generic;
using UnityEngine;
using CivilizationEvolution.Core;
using CivilizationEvolution.Role;

namespace CivilizationEvolution.Disaster
{
    /// <summary>
    /// 灾害系统
    /// 自然灾害与人为灾害，有触发条件、影响范围、持续时间、后果
    /// </summary>
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
    [System.Serializable]
    public class ActiveDisaster
    {
        public DisasterDef def;
        public int centerTile;
        public List<int> affectedTiles = new List<int>();
        public int startDay;
        public int startYear;
        public int remainingDays;
        public float severity;
    }

    /// <summary>灾害定义</summary>
    [System.Serializable]
    public struct DisasterDef
    {
        public DisasterType type;
        public string name;
        public DisasterCategory category;
        public float baseFrequency;
        public int minDuration;
        public int maxDuration;
        public float baseSeverity;
    }

    public enum DisasterType
    {
        // 气象灾害
        Drought,
        Flood,
        ColdWave,
        HeatWave,
        Storm,
        // 地质灾害
        Earthquake,
        VolcanicEruption,
        Landslide,
        // 生物灾害
        LocustPlague,
        CropFailure,
        // 人为灾害
        Fire,
        Famine
    }

    public enum DisasterCategory
    {
        Meteorological,
        Geological,
        Biological,
        Anthropogenic
    }

    /// <summary>
    /// 疾病系统
    /// 传染病与地方病，有传播机制、感染率、死亡率、治疗
    /// </summary>
    [System.Serializable]
    public class DiseaseSystem
    {
        private readonly TileData[] _tiles;
        private readonly CharacterManager _characterManager;
        private readonly int _width;
        private readonly int _height;
        private readonly Dictionary<DiseaseType, DiseaseDef> _diseaseDefs = new Dictionary<DiseaseType, DiseaseDef>();
        private readonly List<ActiveDisease> _activeDiseases = new List<ActiveDisease>();

        public DiseaseSystem(TileData[] tiles, CharacterManager characterManager, int width, int height)
        {
            _tiles = tiles;
            _characterManager = characterManager;
            _width = width;
            _height = height;
            InitializeDiseaseDefs();
        }

        private void InitializeDiseaseDefs()
        {
            _diseaseDefs[DiseaseType.Plague] = new DiseaseDef
            {
                type = DiseaseType.Plague,
                name = "鼠疫",
                baseInfectionRate = 0.15f,
                baseMortalityRate = 0.5f,
                baseRecoveryRate = 0.02f,
                incubationDays = 7,
                durationDays = 21,
                baseR0 = 3.0f
            };
            _diseaseDefs[DiseaseType.Smallpox] = new DiseaseDef
            {
                type = DiseaseType.Smallpox,
                name = "天花",
                baseInfectionRate = 0.12f,
                baseMortalityRate = 0.3f,
                baseRecoveryRate = 0.05f,
                incubationDays = 12,
                durationDays = 30,
                baseR0 = 3.5f
            };
            _diseaseDefs[DiseaseType.Cholera] = new DiseaseDef
            {
                type = DiseaseType.Cholera,
                name = "霍乱",
                baseInfectionRate = 0.1f,
                baseMortalityRate = 0.2f,
                baseRecoveryRate = 0.08f,
                incubationDays = 3,
                durationDays = 10,
                baseR0 = 2.5f
            };
            _diseaseDefs[DiseaseType.Typhus] = new DiseaseDef
            {
                type = DiseaseType.Typhus,
                name = "斑疹伤寒",
                baseInfectionRate = 0.08f,
                baseMortalityRate = 0.15f,
                baseRecoveryRate = 0.06f,
                incubationDays = 10,
                durationDays = 21,
                baseR0 = 2.0f
            };
            _diseaseDefs[DiseaseType.Malaria] = new DiseaseDef
            {
                type = DiseaseType.Malaria,
                name = "疟疾",
                baseInfectionRate = 0.06f,
                baseMortalityRate = 0.05f,
                baseRecoveryRate = 0.03f,
                incubationDays = 14,
                durationDays = 60,
                baseR0 = 1.5f,
                isEndemic = true
            };
            _diseaseDefs[DiseaseType.Tuberculosis] = new DiseaseDef
            {
                type = DiseaseType.Tuberculosis,
                name = "结核病",
                baseInfectionRate = 0.03f,
                baseMortalityRate = 0.1f,
                baseRecoveryRate = 0.01f,
                incubationDays = 90,
                durationDays = 365,
                baseR0 = 1.2f,
                isEndemic = true
            };
            _diseaseDefs[DiseaseType.Dysentery] = new DiseaseDef
            {
                type = DiseaseType.Dysentery,
                name = "痢疾",
                baseInfectionRate = 0.05f,
                baseMortalityRate = 0.08f,
                baseRecoveryRate = 0.1f,
                incubationDays = 2,
                durationDays = 7,
                baseR0 = 1.8f
            };
        }

        /// <summary>每日疾病Tick</summary>
        public void DailyTick(int currentDay, int currentYear)
        {
            // 更新活跃疾病
            for (int i = _activeDiseases.Count - 1; i >= 0; i--)
            {
                var disease = _activeDiseases[i];
                disease.elapsedDays++;

                // 传播
                SpreadDisease(disease);

                // 感染者状态更新
                UpdateInfections(disease, currentDay, currentYear);

                // 检查是否消退
                if (disease.activeInfections == 0 && disease.elapsedDays > disease.def.durationDays * 2)
                {
                    Debug.Log($"[Disease] {disease.def.name} 已消退，总感染 {disease.totalInfected}，死亡 {disease.totalDeaths}");
                    _activeDiseases.RemoveAt(i);
                }
            }

            // 地方病持续存在
            MaintainEndemicDiseases();

            // 随机爆发新疾病
            TryOutbreakDiseases(currentDay, currentYear);
        }

        /// <summary>疾病传播</summary>
        private void SpreadDisease(ActiveDisease disease)
        {
            // 简化：在受影响地块中随机感染新人口
            foreach (int tileIdx in disease.affectedTiles)
            {
                if (_tiles[tileIdx].populationBlocks == null) continue;

                float population = 0f;
                foreach (var pb in _tiles[tileIdx].populationBlocks)
                    population += pb.count;

                if (population <= 0) continue;

                // 感染概率
                float infectionChance = disease.def.baseInfectionRate * disease.currentR0 / 10f;
                infectionChance *= GetDiseaseEnvironmentMod(disease.def.type, tileIdx);

                float newInfections = population * infectionChance * 0.01f;
                if (newInfections > 0.1f)
                {
                    disease.activeInfections += newInfections;
                    disease.totalInfected += newInfections;

                    // 从人口块中扣除（简化：直接减少人口）
                    float remaining = newInfections;
                    for (int j = 0; j < _tiles[tileIdx].populationBlocks.Count && remaining > 0; j++)
                    {
                        var pb = _tiles[tileIdx].populationBlocks[j];
                        float infected = Mathf.Min(pb.count * 0.1f, remaining);
                        pb.count -= infected;
                        remaining -= infected;
                        _tiles[tileIdx].populationBlocks[j] = pb;
                    }
                }
            }
        }

        /// <summary>更新感染者状态</summary>
        private void UpdateInfections(ActiveDisease disease, int currentDay, int currentYear)
        {
            if (disease.activeInfections <= 0) return;

            // 死亡
            float deaths = disease.activeInfections * disease.def.baseMortalityRate * 0.01f;
            disease.activeInfections -= deaths;
            disease.totalDeaths += deaths;

            // 恢复
            float recoveries = disease.activeInfections * disease.def.baseRecoveryRate;
            disease.activeInfections -= recoveries;
            disease.totalRecovered += recoveries;

            // R0自然衰减
            disease.currentR0 = Mathf.Lerp(disease.currentR0, disease.def.baseR0 * 0.5f, 0.01f);
        }

        /// <summary>维持地方病</summary>
        private void MaintainEndemicDiseases()
        {
            foreach (var def in _diseaseDefs.Values)
            {
                if (!def.isEndemic) continue;

                // 检查是否已有活跃的地方病
                bool exists = _activeDiseases.Exists(d => d.def.type == def.type);
                if (!exists)
                {
                    // 在适合的地区维持低水平流行
                    for (int i = 0; i < _tiles.Length; i++)
                    {
                        if (!_tiles[i].exists || !_tiles[i].isLand) continue;
                        if (GetDiseaseEnvironmentMod(def.type, i) > 0.8f)
                        {
                            var disease = new ActiveDisease
                            {
                                def = def,
                                affectedTiles = new List<int> { i },
                                elapsedDays = 0,
                                activeInfections = 1f,
                                currentR0 = def.baseR0 * 0.3f
                            };
                            _activeDiseases.Add(disease);
                            break;
                        }
                    }
                }
            }
        }

        /// <summary>尝试爆发疾病</summary>
        private void TryOutbreakDiseases(int currentDay, int currentYear)
        {
            foreach (var def in _diseaseDefs.Values)
            {
                if (def.isEndemic) continue;

                // 低概率爆发
                if (UnityEngine.Random.value < 0.0005f)
                {
                    OutbreakDisease(def.type, currentDay, currentYear);
                }
            }
        }

        /// <summary>爆发疾病</summary>
        public ActiveDisease OutbreakDisease(DiseaseType type, int currentDay, int currentYear, int centerTile = -1)
        {
            if (!_diseaseDefs.TryGetValue(type, out var def)) return null;

            if (centerTile < 0)
            {
                var candidates = new List<int>();
                for (int i = 0; i < _tiles.Length; i++)
                {
                    if (_tiles[i].isLand && GetDiseaseEnvironmentMod(type, i) > 0.5f)
                        candidates.Add(i);
                }
                if (candidates.Count == 0) return null;
                centerTile = candidates[UnityEngine.Random.Range(0, candidates.Count)];
            }

            var disease = new ActiveDisease
            {
                def = def,
                centerTile = centerTile,
                affectedTiles = CalculateDiseaseSpreadArea(type, centerTile),
                startDay = currentDay,
                startYear = currentYear,
                elapsedDays = 0,
                activeInfections = 10f,
                currentR0 = def.baseR0
            };

            _activeDiseases.Add(disease);
            Debug.Log($"[Disease] {def.name} 爆发！中心地块 {centerTile}，影响 {disease.affectedTiles.Count} 地块");

            return disease;
        }

        /// <summary>计算疾病传播区域</summary>
        private List<int> CalculateDiseaseSpreadArea(DiseaseType type, int centerTile)
        {
            var affected = new List<int>();
            int radius = type switch
            {
                DiseaseType.Plague => 10,
                DiseaseType.Smallpox => 8,
                DiseaseType.Cholera => 6,
                DiseaseType.Typhus => 7,
                DiseaseType.Malaria => 5,
                DiseaseType.Tuberculosis => 4,
                DiseaseType.Dysentery => 5,
                _ => 5
            };

            int cx = centerTile % _width;
            int cy = centerTile / _width;

            for (int i = 0; i < _tiles.Length; i++)
            {
                if (!_tiles[i].exists || !_tiles[i].isLand) continue;
                int x = i % _width;
                int y = i / _width;
                if (Mathf.Abs(x - cx) + Mathf.Abs(y - cy) <= radius)
                    affected.Add(i);
            }

            return affected;
        }

        /// <summary>疾病环境修正</summary>
        private float GetDiseaseEnvironmentMod(DiseaseType type, int tileIndex)
        {
            ref TileData tile = ref _tiles[tileIndex];
            return type switch
            {
                DiseaseType.Plague => 0.5f + tile.airHumidityPct / 200f,
                DiseaseType.Smallpox => 0.6f + (100f - tile.annualTemp) / 200f,
                DiseaseType.Cholera => 0.3f + tile.airHumidityPct / 150f + tile.waterAdjacentWeight * 0.2f, // 修复：原 (1-waterAdjacent) 反向（近水反而低危）
                DiseaseType.Typhus => 0.4f + (100f - tile.stability) / 200f,
                DiseaseType.Malaria => 0.2f + tile.airHumidityPct / 100f + Mathf.Max(0f, tile.annualTemp) / 50f,
                DiseaseType.Tuberculosis => 0.5f + (100f - tile.annualTemp) / 200f,
                DiseaseType.Dysentery => 0.4f + tile.airHumidityPct / 200f,
                _ => 0.5f
            };
        }

        // ===== 查询接口 =====
        public IReadOnlyList<ActiveDisease> GetActiveDiseases() => _activeDiseases;

        public List<ActiveDisease> GetDiseasesAtTile(int tileIndex)
        {
            var result = new List<ActiveDisease>();
            foreach (var d in _activeDiseases)
            {
                if (d.affectedTiles.Contains(tileIndex))
                    result.Add(d);
            }
            return result;
        }

        public float GetTotalInfected()
        {
            float total = 0f;
            foreach (var d in _activeDiseases)
                total += d.activeInfections;
            return total;
        }

        public float GetTotalDeaths()
        {
            float total = 0f;
            foreach (var d in _activeDiseases)
                total += d.totalDeaths;
            return total;
        }
    }

    /// <summary>活跃疾病实例</summary>
    [System.Serializable]
    public class ActiveDisease
    {
        public DiseaseDef def;
        public int centerTile;
        public List<int> affectedTiles = new List<int>();
        public int startDay;
        public int startYear;
        public int elapsedDays;
        public float activeInfections;
        public float totalInfected;
        public float totalDeaths;
        public float totalRecovered;
        public float currentR0;
    }

    /// <summary>疾病定义</summary>
    [System.Serializable]
    public struct DiseaseDef
    {
        public DiseaseType type;
        public string name;
        public float baseInfectionRate;
        public float baseMortalityRate;
        public float baseRecoveryRate;
        public int incubationDays;
        public int durationDays;
        public float baseR0;
        public bool isEndemic;
    }

    public enum DiseaseType
    {
        Plague,
        Smallpox,
        Cholera,
        Typhus,
        Malaria,
        Tuberculosis,
        Dysentery
    }
}
