using System;
using System.Collections.Generic;
using MyriadCreation.Core.Data;
using MyriadCreation.Simulation.State;
using UnityEngine;
using MyriadCreation.Content.Definitions;
using Random = UnityEngine.Random;

namespace MyriadCreation.Simulation.Systems
{
[Serializable]
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
			baseR0 = 3f
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
			baseR0 = 2f
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

	public void DailyTick(int currentDay, int currentYear)
	{
		for (int num = _activeDiseases.Count - 1; num >= 0; num--)
		{
			ActiveDisease activeDisease = _activeDiseases[num];
			activeDisease.elapsedDays++;
			SpreadDisease(activeDisease);
			InfectCharacters(activeDisease, currentDay, currentYear);
			UpdateInfections(activeDisease, currentDay, currentYear);
			if (activeDisease.activeInfections == 0f && activeDisease.elapsedDays > activeDisease.def.durationDays * 2)
			{
				Debug.Log((object)$"[Disease] {activeDisease.def.name} 已消退，总感染 {activeDisease.totalInfected}，死亡 {activeDisease.totalDeaths}");
				_activeDiseases.RemoveAt(num);
			}
		}
		MaintainEndemicDiseases();
		TryOutbreakDiseases(currentDay, currentYear);
	}

	private void SpreadDisease(ActiveDisease disease)
	{
		foreach (int affectedTile in disease.affectedTiles)
		{
			if (_tiles[affectedTile].populationBlocks == null)
			{
				continue;
			}
			float num = 0f;
			foreach (PopulationBlock populationBlock in _tiles[affectedTile].populationBlocks)
			{
				num += populationBlock.count;
			}
			if (num <= 0f)
			{
				continue;
			}
			float num2 = disease.def.baseInfectionRate * disease.currentR0 / 10f;
			num2 *= GetDiseaseEnvironmentMod(disease.def.type, affectedTile);
			float num3 = num * num2 * 0.01f;
			if (!(num3 > 0.1f))
			{
				continue;
			}
			disease.activeInfections += num3;
			disease.totalInfected += num3;
			float num4 = num3;
			for (int i = 0; i < _tiles[affectedTile].populationBlocks.Count; i++)
			{
				if (!(num4 > 0f))
				{
					break;
				}
				PopulationBlock value = _tiles[affectedTile].populationBlocks[i];
				float num5 = Mathf.Min(value.count * 0.1f, num4);
				value.count -= num5;
				num4 -= num5;
				_tiles[affectedTile].populationBlocks[i] = value;
			}
		}
	}

	private void InfectCharacters(ActiveDisease disease, int currentDay, int currentYear)
	{
		if (_characterManager == null || disease.affectedTiles == null || disease.affectedTiles.Count == 0)
		{
			return;
		}
		HashSet<int> hashSet = new HashSet<int>();
		foreach (int affectedTile in disease.affectedTiles)
		{
			if (affectedTile >= 0 && affectedTile < _tiles.Length && _tiles[affectedTile].ownerRealmId >= 0)
			{
				hashSet.Add(_tiles[affectedTile].ownerRealmId);
			}
		}
		if (hashSet.Count == 0)
		{
			return;
		}
		foreach (CharacterData value in _characterManager.GetAllCharacters().Values)
		{
			if (!value.isAlive || !hashSet.Contains(value.realmId))
			{
				continue;
			}
			float individualResistance = value.individualResistance;
			float num = Mathf.Clamp(1.6f - individualResistance / 100f * 1.4f, 0.2f, 1.6f);
			float num2 = disease.def.baseInfectionRate * (disease.currentR0 / 10f) * num * 0.02f;
			if (!(Random.value >= num2))
			{
				float num3 = disease.def.baseMortalityRate * 15f;
				value.health = Mathf.Max(0f, value.health - num3);
				if (value.health <= 0f)
				{
					value.Die(currentDay, currentYear, disease.def.name + "病逝");
					Debug.Log((object)$"[Disease] {value.fullName} 因{disease.def.name}病逝（抗性 {individualResistance:F0}）");
					continue;
				}
				Debug.Log((object)$"[Disease] {value.fullName} 感染{disease.def.name}（抗性 {individualResistance:F0}，健康 {value.health:F0}）");
			}
		}
	}

	private void UpdateInfections(ActiveDisease disease, int currentDay, int currentYear)
	{
		if (!(disease.activeInfections <= 0f))
		{
			float num = disease.activeInfections * disease.def.baseMortalityRate * 0.01f;
			disease.activeInfections -= num;
			disease.totalDeaths += num;
			float num2 = disease.activeInfections * disease.def.baseRecoveryRate;
			disease.activeInfections -= num2;
			disease.totalRecovered += num2;
			disease.currentR0 = Mathf.Lerp(disease.currentR0, disease.def.baseR0 * 0.5f, 0.01f);
		}
	}

	private void MaintainEndemicDiseases()
	{
		foreach (DiseaseDef def in _diseaseDefs.Values)
		{
			if (!def.isEndemic || _activeDiseases.Exists((ActiveDisease d) => d.def.type == def.type))
			{
				continue;
			}
			for (int num = 0; num < _tiles.Length; num++)
			{
				if (_tiles[num].exists && _tiles[num].isLand && GetDiseaseEnvironmentMod(def.type, num) > 0.8f)
				{
					ActiveDisease item = new ActiveDisease
					{
						def = def,
						affectedTiles = new List<int> { num },
						elapsedDays = 0,
						activeInfections = 1f,
						currentR0 = def.baseR0 * 0.3f
					};
					_activeDiseases.Add(item);
					break;
				}
			}
		}
	}

	private void TryOutbreakDiseases(int currentDay, int currentYear)
	{
		foreach (DiseaseDef value in _diseaseDefs.Values)
		{
			if (!value.isEndemic && Random.value < 0.0005f)
			{
				OutbreakDisease(value.type, currentDay, currentYear);
			}
		}
	}

	public ActiveDisease OutbreakDisease(DiseaseType type, int currentDay, int currentYear, int centerTile = -1)
	{
		if (!_diseaseDefs.TryGetValue(type, out var value))
		{
			return null;
		}
		if (centerTile < 0)
		{
			List<int> list = new List<int>();
			for (int i = 0; i < _tiles.Length; i++)
			{
				if (_tiles[i].isLand && GetDiseaseEnvironmentMod(type, i) > 0.5f)
				{
					list.Add(i);
				}
			}
			if (list.Count == 0)
			{
				return null;
			}
			centerTile = list[Random.Range(0, list.Count)];
		}
		ActiveDisease activeDisease = new ActiveDisease
		{
			def = value,
			centerTile = centerTile,
			affectedTiles = CalculateDiseaseSpreadArea(type, centerTile),
			startDay = currentDay,
			startYear = currentYear,
			elapsedDays = 0,
			activeInfections = 10f,
			currentR0 = value.baseR0
		};
		_activeDiseases.Add(activeDisease);
		Debug.Log((object)$"[Disease] {value.name} 爆发！中心地块 {centerTile}，影响 {activeDisease.affectedTiles.Count} 地块");
		return activeDisease;
	}

	private List<int> CalculateDiseaseSpreadArea(DiseaseType type, int centerTile)
	{
		List<int> list = new List<int>();
		int num = type switch
		{
			DiseaseType.Plague => 10, 
			DiseaseType.Smallpox => 8, 
			DiseaseType.Cholera => 6, 
			DiseaseType.Typhus => 7, 
			DiseaseType.Malaria => 5, 
			DiseaseType.Tuberculosis => 4, 
			DiseaseType.Dysentery => 5, 
			_ => 5, 
		};
		int num2 = centerTile % _width;
		int num3 = centerTile / _width;
		for (int i = 0; i < _tiles.Length; i++)
		{
			if (_tiles[i].exists && _tiles[i].isLand)
			{
				int num4 = i % _width;
				int num5 = i / _width;
				if (Mathf.Abs(num4 - num2) + Mathf.Abs(num5 - num3) <= num)
				{
					list.Add(i);
				}
			}
		}
		return list;
	}

	private float GetDiseaseEnvironmentMod(DiseaseType type, int tileIndex)
	{
		ref TileData reference = ref _tiles[tileIndex];
		return type switch
		{
			DiseaseType.Plague => 0.5f + reference.airHumidityPct / 200f, 
			DiseaseType.Smallpox => 0.6f + (100f - reference.annualTemp) / 200f, 
			DiseaseType.Cholera => 0.3f + reference.airHumidityPct / 150f + reference.waterAdjacentWeight * 0.2f, 
			DiseaseType.Typhus => 0.4f + (100f - reference.stability) / 200f, 
			DiseaseType.Malaria => 0.2f + reference.airHumidityPct / 100f + Mathf.Max(0f, reference.annualTemp) / 50f, 
			DiseaseType.Tuberculosis => 0.5f + (100f - reference.annualTemp) / 200f, 
			DiseaseType.Dysentery => 0.4f + reference.airHumidityPct / 200f, 
			_ => 0.5f, 
		};
	}

	public IReadOnlyList<ActiveDisease> GetActiveDiseases()
	{
		return _activeDiseases;
	}

	public List<ActiveDisease> GetDiseasesAtTile(int tileIndex)
	{
		List<ActiveDisease> list = new List<ActiveDisease>();
		foreach (ActiveDisease activeDisease in _activeDiseases)
		{
			if (activeDisease.affectedTiles.Contains(tileIndex))
			{
				list.Add(activeDisease);
			}
		}
		return list;
	}

	public float GetTotalInfected()
	{
		float num = 0f;
		foreach (ActiveDisease activeDisease in _activeDiseases)
		{
			num += activeDisease.activeInfections;
		}
		return num;
	}

	public float GetTotalDeaths()
	{
		float num = 0f;
		foreach (ActiveDisease activeDisease in _activeDiseases)
		{
			num += activeDisease.totalDeaths;
		}
		return num;
	}
}
}
