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
		if (DisasterCatalog.IsLoaded)
		{
			foreach (KeyValuePair<DisasterType, DisasterDef> pair in DisasterCatalog.All)
			{
				_disasterDefs[pair.Key] = pair.Value;
			}
			return;
		}
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
			baseFrequency = 0f,
			minDuration = 30,
			maxDuration = 180,
			baseSeverity = 95f
		};
	}

	public void DailyTick(int currentDay, int currentYear)
	{
		for (int num = _activeDisasters.Count - 1; num >= 0; num--)
		{
			ActiveDisaster activeDisaster = _activeDisasters[num];
			activeDisaster.remainingDays--;
			ApplyDisasterEffects(activeDisaster);
			if (activeDisaster.remainingDays <= 0)
			{
				Debug.Log((object)$"[Disaster] {activeDisaster.def.name} 结束，影响 {activeDisaster.affectedTiles.Count} 个地块");
				_activeDisasters.RemoveAt(num);
			}
		}
		TryTriggerDisasters(currentDay, currentYear);
	}

	private void TryTriggerDisasters(int currentDay, int currentYear)
	{
		foreach (DisasterDef value in _disasterDefs.Values)
		{
			if (!(value.baseFrequency <= 0f))
			{
				float seasonDisasterMod = GetSeasonDisasterMod(value.type, currentDay);
				if (Random.value < value.baseFrequency * seasonDisasterMod * 0.01f)
				{
					TriggerDisaster(value.type, currentDay, currentYear);
				}
			}
		}
	}

	public ActiveDisaster TriggerDisaster(DisasterType type, int currentDay, int currentYear, int centerTile = -1)
	{
		if (!_disasterDefs.TryGetValue(type, out var value))
		{
			return null;
		}
		if (centerTile < 0)
		{
			centerTile = FindDisasterOriginTile(type);
		}
		List<int> list = CalculateAffectedTiles(type, centerTile);
		if (list.Count == 0)
		{
			return null;
		}
		ActiveDisaster activeDisaster = new ActiveDisaster
		{
			def = value,
			centerTile = centerTile,
			affectedTiles = list,
			startDay = currentDay,
			startYear = currentYear,
			remainingDays = Random.Range(value.minDuration, value.maxDuration + 1),
			severity = value.baseSeverity * Random.Range(0.7f, 1.3f)
		};
		_activeDisasters.Add(activeDisaster);
		Debug.Log((object)$"[Disaster] {value.name} 爆发！中心地块 {centerTile}，影响 {list.Count} 地块，持续 {activeDisaster.remainingDays} 天");
		ApplyImmediateEffects(activeDisaster);
		return activeDisaster;
	}

	private int FindDisasterOriginTile(DisasterType type)
	{
		List<int> list = new List<int>();
		for (int i = 0; i < _tiles.Length; i++)
		{
			if (_tiles[i].exists && _tiles[i].isLand && type switch
			{
				DisasterType.Drought => _tiles[i].annualPrecipMm < 800f, 
				DisasterType.Flood => _tiles[i].annualPrecipMm > 1000f && _tiles[i].elevation01 < 0.3f, 
				DisasterType.ColdWave => _tiles[i].annualTemp < 10f, 
				DisasterType.HeatWave => _tiles[i].annualTemp > 20f, 
				DisasterType.Earthquake => _tiles[i].elevation01 > 0.4f, 
				DisasterType.VolcanicEruption => _tiles[i].elevation01 > 0.6f, 
				DisasterType.Landslide => _tiles[i].slopeDegree > 20f, 
				DisasterType.LocustPlague => _tiles[i].annualTemp > 15f && _tiles[i].annualPrecipMm < 600f, 
				_ => true, 
			})
			{
				list.Add(i);
			}
		}
		if (list.Count == 0)
		{
			return Random.Range(0, _tiles.Length);
		}
		return list[Random.Range(0, list.Count)];
	}

	private List<int> CalculateAffectedTiles(DisasterType type, int centerTile)
	{
		List<int> list = new List<int>();
		int num = type switch
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

	private void ApplyImmediateEffects(ActiveDisaster disaster)
	{
		foreach (int affectedTile in disaster.affectedTiles)
		{
			ref TileData reference = ref _tiles[affectedTile];
			switch (disaster.def.type)
			{
			case DisasterType.Earthquake:
				reference.stability = Mathf.Max(0f, reference.stability - disaster.severity * 0.3f);
				reference.development = Mathf.Max(0f, reference.development - disaster.severity * 0.01f);
				break;
			case DisasterType.VolcanicEruption:
				reference.stability = Mathf.Max(0f, reference.stability - disaster.severity * 0.5f);
				reference.fertility = Mathf.Min(1f, reference.fertility + 0.1f);
				break;
			case DisasterType.Fire:
				reference.development = Mathf.Max(0f, reference.development - disaster.severity * 0.02f);
				break;
			}
		}
	}

	private void ApplyDisasterEffects(ActiveDisaster disaster)
	{
		foreach (int affectedTile in disaster.affectedTiles)
		{
			ref TileData reference = ref _tiles[affectedTile];
			switch (disaster.def.type)
			{
			case DisasterType.Drought:
				reference.soilHumidityPct = Mathf.Max(0f, reference.soilHumidityPct - 0.5f);
				// 干旱主要是当年减产与土壤失水，不是永久毁地：肥力日损压到 0.0002，
				// 与 EcosystemSystem 的年度恢复（约 12%/年）配平，避免几百年后全图肥力归零。
				reference.fertility = Mathf.Max(0f, reference.fertility - 0.0002f);
				break;
			case DisasterType.Flood:
				reference.soilHumidityPct = Mathf.Min(100f, reference.soilHumidityPct + 2f);
				reference.stability = Mathf.Max(0f, reference.stability - 0.1f);
				break;
			case DisasterType.ColdWave:
				reference.annualTemp = Mathf.Max(-50f, reference.annualTemp - 0.5f);
				break;
			case DisasterType.HeatWave:
				reference.annualTemp = Mathf.Min(50f, reference.annualTemp + 0.3f);
				break;
			case DisasterType.LocustPlague:
				// 蝗群吃掉的是植被与当季作物，土壤肥力只轻微受损。
				reference.fertility = Mathf.Max(0f, reference.fertility - 0.001f);
				break;
			case DisasterType.CropFailure:
				reference.fertility = Mathf.Max(0f, reference.fertility - 0.0005f);
				break;
			case DisasterType.Famine:
				reference.stability = Mathf.Max(0f, reference.stability - 0.2f);
				reference.order = Mathf.Max(0f, reference.order - 0.1f);
				break;
			}
		}
	}

	private float GetSeasonDisasterMod(DisasterType type, int dayOfYear)
	{
		int num = (dayOfYear - 1) / 91;
		return type switch
		{
			DisasterType.Drought => num switch
			{
				3 => 0.3f, 
				1 => 2f, 
				_ => 1f, 
			}, 
			DisasterType.Flood => num switch
			{
				0 => 1.5f, 
				1 => 2.5f, 
				_ => 0.5f, 
			}, 
			DisasterType.ColdWave => num switch
			{
				0 => 1.5f, 
				3 => 3f, 
				_ => 0.2f, 
			}, 
			DisasterType.HeatWave => num switch
			{
				3 => 0.1f, 
				1 => 3f, 
				_ => 0.8f, 
			}, 
			DisasterType.Storm => num switch
			{
				3 => 0.5f, 
				1 => 2f, 
				_ => 1f, 
			}, 
			DisasterType.LocustPlague => num switch
			{
				3 => 0.2f, 
				1 => 2.5f, 
				_ => 1f, 
			}, 
			DisasterType.CropFailure => (num == 2) ? 2f : 1f, 
			DisasterType.Fire => num switch
			{
				3 => 0.3f, 
				1 => 2f, 
				_ => 1f, 
			}, 
			_ => 1f, 
		};
	}

	public IReadOnlyList<ActiveDisaster> GetActiveDisasters()
	{
		return _activeDisasters;
	}

	public List<ActiveDisaster> GetDisastersAtTile(int tileIndex)
	{
		List<ActiveDisaster> list = new List<ActiveDisaster>();
		foreach (ActiveDisaster activeDisaster in _activeDisasters)
		{
			if (activeDisaster.affectedTiles.Contains(tileIndex))
			{
				list.Add(activeDisaster);
			}
		}
		return list;
	}

	public bool IsTileAffectedByDisaster(int tileIndex, DisasterType type)
	{
		foreach (ActiveDisaster activeDisaster in _activeDisasters)
		{
			if (activeDisaster.def.type == type && activeDisaster.affectedTiles.Contains(tileIndex))
			{
				return true;
			}
		}
		return false;
	}
}
}
