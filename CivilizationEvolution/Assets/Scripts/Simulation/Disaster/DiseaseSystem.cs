

using System.Collections.Generic;
using UnityEngine;
using CivilizationEvolution.Core;
using CivilizationEvolution.Core.Constants;
using CivilizationEvolution.Core.Data;
using CivilizationEvolution.Core.Dto;
using CivilizationEvolution.Core.Enums;
using CivilizationEvolution.Simulation.Characters;
using CivilizationEvolution.Simulation.Economy;
using CivilizationEvolution.Simulation.Events;
using CivilizationEvolution.Simulation.Generation;
using CivilizationEvolution.Simulation.Modding;
using CivilizationEvolution.Simulation.Politics;
using CivilizationEvolution.Simulation.Society;
using CivilizationEvolution.Simulation.WorldState;


namespace CivilizationEvolution.Simulation.Disaster
{
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

 // 角色感染（DNA 抗性对接：个体抗性修正感染概率与死亡率）
                InfectCharacters(disease, currentDay, currentYear);

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

 /// 角色感染（DNA 抗性对接）
 /// 活跃疾病每日对影响地块政权的存活角色，按个体抗性修正概率感染：
 /// - 个体抗性 100 → 感染概率 ×0.2；抗性 0 → ×1.6（individualResistance = 种族基准 + DNA 偏移）
 /// - 感染扣健康（按疾病死亡率），健康归零角色死亡
 /// - 感染概率远低于人口块感染（角色是珍稀个体，避免快速灭绝）
        private void InfectCharacters(ActiveDisease disease, int currentDay, int currentYear)
        {
            if (_characterManager == null || disease.affectedTiles == null || disease.affectedTiles.Count == 0)
                return;

 // 疾病影响地块的政权集合（去重）
            var affectedRealms = new HashSet<int>();
            foreach (int idx in disease.affectedTiles)
            {
                if (idx >= 0 && idx < _tiles.Length && _tiles[idx].ownerRealmId >= 0)
                    affectedRealms.Add(_tiles[idx].ownerRealmId);
            }
            if (affectedRealms.Count == 0) return;

            foreach (var c in _characterManager.GetAllCharacters().Values)
            {
                if (!c.isAlive || !affectedRealms.Contains(c.realmId)) continue;

                float resistance = c.individualResistance;
                float mod = Mathf.Clamp(1.6f - resistance / 100f * 1.4f, 0.2f, 1.6f);

                float chance = disease.def.baseInfectionRate * (disease.currentR0 / 10f) * mod * 0.02f;
                if (UnityEngine.Random.value >= chance) continue;

                float damage = disease.def.baseMortalityRate * 15f;
                c.health = Mathf.Max(0f, c.health - damage);
                if (c.health <= 0f)
                {
                    c.Die(currentDay, currentYear, $"{disease.def.name}病逝");
                    Debug.Log($"[Disease] {c.fullName} 因{disease.def.name}病逝（抗性 {resistance:F0}）");
                }
                else
                {
                    Debug.Log($"[Disease] {c.fullName} 感染{disease.def.name}（抗性 {resistance:F0}，健康 {c.health:F0}）");
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
}
