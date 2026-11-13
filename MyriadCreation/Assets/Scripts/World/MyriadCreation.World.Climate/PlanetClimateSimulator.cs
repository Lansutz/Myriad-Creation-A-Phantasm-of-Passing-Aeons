using System.Collections.Generic;
using MyriadCreation.Core.Data;
using MyriadCreation.Core.Enums;
using MyriadCreation.World.Terrain;
using UnityEngine;

namespace MyriadCreation.World.Climate
{
public class PlanetClimateSimulator
{
	private readonly WorldConfig _config;

	private readonly TileData[] _tiles;

	/// <summary>世界历法（自转/公转）；为空时按地球基准。</summary>
	public MyriadCreation.Core.Data.GameCalendar Calendar;

	private readonly int _width;

	private readonly int _height;

	private readonly SeaLandGenerator _seaLand;

	private float _obliquity;

	private float _polarCircleLat;

	private float _tropicLat;

	private float _effectiveSolarConstant;

	private float[] _seasonTempMod = new float[4];

	private float[] _seasonPrecipMod = new float[4];

	public PlanetClimateSimulator(WorldConfig config, TileData[] tiles, int width, int height, SeaLandGenerator seaLand)
	{
		_config = config;
		_tiles = tiles;
		_width = width;
		_height = height;
		_seaLand = seaLand;
		RecalculateDerivedConstants();
		CalculateSeasonModifiers();
	}

	private void RecalculateDerivedConstants()
	{
		_obliquity = Mathf.Lerp(0f, 45f, _config.seasonIntensity);
		_tropicLat = _obliquity;
		_polarCircleLat = 90f - _obliquity;
		_effectiveSolarConstant = _config.stellarIrradiance * (1f - _config.albedo) / 4f;
	}

	private void CalculateSeasonModifiers()
	{
		float num = _obliquity / 23.5f * 15f;
		_seasonTempMod[0] = 0f;
		_seasonTempMod[1] = num;
		_seasonTempMod[2] = 0f;
		_seasonTempMod[3] = 0f - num;
		_seasonPrecipMod[0] = 1f;
		_seasonPrecipMod[1] = 1.3f;
		_seasonPrecipMod[2] = 1f;
		_seasonPrecipMod[3] = 0.7f;
	}

	public void RecalculateAll()
	{
		RecalculateDerivedConstants();
		CalculateSeasonModifiers();
		Vector2[] windField = CalculateWindField();
		for (int i = 0; i < _tiles.Length; i++)
		{
			if (_tiles[i].exists && _tiles[i].isLand)
			{
				CalculateTileClimate(i, windField, 0);
			}
		}
	}

	public void RecalculateDirty(HashSet<int> dirtyIndices)
	{
		RecalculateDerivedConstants();
		CalculateSeasonModifiers();
		Vector2[] windField = CalculateWindField();
		foreach (int dirtyIndex in dirtyIndices)
		{
			if (_tiles[dirtyIndex].exists && _tiles[dirtyIndex].isLand)
			{
				CalculateTileClimate(dirtyIndex, windField, 0);
			}
		}
	}

	public void UpdateForSeason(int season)
	{
		Vector2[] windField = CalculateWindField();
		for (int i = 0; i < _tiles.Length; i++)
		{
			if (_tiles[i].exists && _tiles[i].isLand)
			{
				CalculateTileClimate(i, windField, season);
			}
		}
	}

	private Vector2[] CalculateWindField()
	{
		//IL_002a: Unknown result type (might be due to invalid IL or missing references)
		//IL_002f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0075: Unknown result type (might be due to invalid IL or missing references)
		//IL_0064: Unknown result type (might be due to invalid IL or missing references)
		//IL_00cb: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ba: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a4: Unknown result type (might be due to invalid IL or missing references)
		//IL_0093: Unknown result type (might be due to invalid IL or missing references)
		//IL_007a: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a9: Unknown result type (might be due to invalid IL or missing references)
		//IL_0127: Unknown result type (might be due to invalid IL or missing references)
		//IL_012c: Unknown result type (might be due to invalid IL or missing references)
		//IL_010b: Unknown result type (might be due to invalid IL or missing references)
		//IL_010d: Unknown result type (might be due to invalid IL or missing references)
		//IL_010f: Unknown result type (might be due to invalid IL or missing references)
		//IL_011c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0121: Unknown result type (might be due to invalid IL or missing references)
		Vector2[] array = new Vector2[_tiles.Length];
		for (int i = 0; i < _tiles.Length; i++)
		{
			if (!_tiles[i].exists)
			{
				array[i] = Vector2.zero;
				continue;
			}
			float tileLatitude = GetTileLatitude(i);
			float num = Mathf.Abs(tileLatitude);
			bool flag = tileLatitude > 0f;
			Vector2 val;
			if (num < 30f)
			{
				val = (flag ? new Vector2(0.7f, -0.7f) : new Vector2(0.7f, 0.7f));
			}
			else if (num < 60f)
			{
				val = (flag ? new Vector2(-0.7f, 0.7f) : new Vector2(-0.7f, -0.7f));
			}
			else
			{
				val = (flag ? new Vector2(0.8f, -0.6f) : new Vector2(0.8f, 0.6f));
			}
			if (_config.monsoonStrength > 0.3f && num < 35f)
			{
				float num2 = _config.monsoonStrength * _tiles[i].waterAdjacentWeight;
				val = Vector2.Lerp(val, -val, num2 * 0.5f);
			}
			array[i] = val.normalized;
		}
		return array;
	}

	private void CalculateTileClimate(int index, Vector2[] windField, int season)
	{
		ref TileData reference = ref _tiles[index];
		float tileLatitude = GetTileLatitude(index);
		float num = Mathf.Abs(tileLatitude);
		float num2 = CalculateEquatorBaseTemp();
		float num3 = Mathf.Pow(num / 90f, 1.5f) * (1.6f - _config.heatTransport * 0.3f);
		float num4 = num2 * (1f - num3);
		float num5 = (0f - reference.elevation01) * 4000f / 1000f * _config.lapseRate;
		float num6 = reference.waterAdjacentWeight * 3f;
		float num7 = (0f - (Mathf.Abs(tileLatitude - _config.thermalEquatorLat) - num)) * 0.15f;
		float num8 = _seasonTempMod[season] * (num / 90f);
		reference.annualTemp = Mathf.Clamp(num4 + num5 + num6 + num7 + num8, -55f, 35f);
		reference.diurnalTempRange = ((1f - reference.waterAdjacentWeight) * 12f + reference.elevation01 * 4f + 4f) * (Calendar != null ? Calendar.DiurnalRangeFactor : 1f);
		float num9 = CalculateITCZPrecipitation(tileLatitude);
		float num10 = CalculateWesterlyPrecipitation(tileLatitude);
		float num11 = CalculateSubtropicalDry(tileLatitude);
		float num12 = CalculateMonsoonPrecipitation(tileLatitude, reference.waterAdjacentWeight, season);
		float num13 = CalculateOrographicPrecipitation(index, windField);
		float num14 = 1f - reference.waterAdjacentWeight * 0.5f;
		float num15 = _seasonPrecipMod[season];
		float num16 = (num9 + num10 + num12 + num13) * num14 * num15 - num11;
		reference.annualPrecipMm = Mathf.Clamp(num16, 0f, 4000f);
		reference.airHumidityPct = Mathf.Clamp(Mathf.Lerp(20f, 95f, reference.annualPrecipMm / 2000f) + reference.waterAdjacentWeight * 10f, 5f, 100f);
		reference.soilHumidityPct = Mathf.Clamp(reference.airHumidityPct * 0.7f + reference.annualPrecipMm / 1000f * 30f, 0f, 100f);
		reference.accumulatedTemp = Mathf.Max(0f, reference.annualTemp - 10f) * 365f;
		reference.frostFreeDays = Mathf.Clamp((reference.annualTemp + 5f) / 30f * 365f, 0f, 365f);
		reference.climateZone = DetermineClimateZone(reference.annualTemp, num, reference.elevation01, reference.annualPrecipMm);
		// 只更新气候带：群系与肥力统一由生成期的 Holdridge 分类器负责。
		// 本类的时间/降水模型是第二套口径，若在这里重判群系，会在首个 tick 把全图改判成沙漠/冰盖，
		// 而聚落选址与肥力仍按生成期群系计算，城市就会坐在“实际不产粮的沃土”上饿死（P7 诊断）。
		// 气候模型统一（两套口径合并）列入 P7.5 板块漂移/长期气候阶段处理。
	}

	private float CalculateEquatorBaseTemp()
	{
		float num = _effectiveSolarConstant / 238.175f;
		return 27f * num + _config.greenhouseFactor * 0.3f;
	}

	private float CalculateITCZPrecipitation(float lat)
	{
		float num = 0f;
		int num2 = _config.circulationMode switch
		{
			GameEnums.CirculationMode.SingleCell => 1, 
			GameEnums.CirculationMode.DoubleCell => 2, 
			GameEnums.CirculationMode.TripleCell => 3, 
			_ => 3, 
		};
		for (int i = 0; i < num2; i++)
		{
			float num3 = _config.thermalEquatorLat + ((float)i - (float)num2 / 2f) * 15f;
			float num4 = Mathf.Abs(lat - num3);
			num += Mathf.Exp((0f - num4) * num4 / 100f) * 1500f;
		}
		return num;
	}

	private float CalculateWesterlyPrecipitation(float lat)
	{
		float num = Mathf.Abs(lat);
		if (num > 35f && num < 65f)
		{
			float num2 = 50f - Mathf.Abs(num - 50f);
			return Mathf.Max(0f, num2) * 15f;
		}
		return 0f;
	}

	private float CalculateSubtropicalDry(float lat)
	{
		float num = Mathf.Abs(lat);
		if (num > 18f && num < 38f)
		{
			float num2 = 10f - Mathf.Abs(num - 28f);
			return Mathf.Max(0f, num2) * 80f;
		}
		return 0f;
	}

	private float CalculateMonsoonPrecipitation(float lat, float waterWeight, int season)
	{
		if (Mathf.Abs(lat) < 35f && waterWeight > 0.2f)
		{
			return _config.monsoonStrength * waterWeight * _config.seasonIntensity * 800f * season switch
			{
				3 => 0.3f, 
				1 => 1.5f, 
				_ => 1f, 
			};
		}
		return 0f;
	}

	private float CalculateOrographicPrecipitation(int index, Vector2[] windField)
	{
		//IL_0022: Unknown result type (might be due to invalid IL or missing references)
		//IL_0027: Unknown result type (might be due to invalid IL or missing references)
		//IL_004d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0060: Unknown result type (might be due to invalid IL or missing references)
		ref TileData reference = ref _tiles[index];
		if (reference.elevation01 <= 0.05f)
		{
			return 0f;
		}
		Vector2 val = windField[index];
		int num = index % _width;
		int num2 = index / _width;
		float num3 = 0f;
		int num4 = 0;
		for (int i = 1; i <= 3; i++)
		{
			int num5 = num - Mathf.RoundToInt(val.x * (float)i);
			int num6 = num2 - Mathf.RoundToInt(val.y * (float)i);
			if (_config.wrapX)
			{
				num5 = (num5 % _width + _width) % _width;
			}
			if (_config.wrapY)
			{
				num6 = (num6 % _height + _height) % _height;
			}
			if (num5 >= 0 && num5 < _width && num6 >= 0 && num6 < _height)
			{
				int num7 = num6 * _width + num5;
				num3 += _tiles[num7].elevation01;
				num4++;
			}
		}
		if (num4 == 0)
		{
			return reference.elevation01 * 200f;
		}
		num3 /= (float)num4;
		float num8 = reference.elevation01 - num3;
		if (num8 > 0f)
		{
			return num8 * 800f + reference.elevation01 * 100f;
		}
		return num8 * 400f;
	}

	private GameEnums.ClimateZone DetermineClimateZone(float temp, float absLat, float elevation, float precip)
	{
		if (elevation > 0.6f && temp < 5f)
		{
			return GameEnums.ClimateZone.HighlandAlpine;
		}
		if (absLat > 30f && absLat < 55f && precip < 300f)
		{
			return GameEnums.ClimateZone.InlandAridTemperate;
		}
		if (temp < -10f)
		{
			return GameEnums.ClimateZone.PolarFrigid;
		}
		if (temp < 0f)
		{
			return GameEnums.ClimateZone.Subarctic;
		}
		if (temp < 8f)
		{
			return GameEnums.ClimateZone.TemperateCold;
		}
		if (temp < 15f)
		{
			return GameEnums.ClimateZone.TemperateMild;
		}
		if (temp < 20f)
		{
			return GameEnums.ClimateZone.TemperateWarm;
		}
		if (temp < 24f)
		{
			return GameEnums.ClimateZone.Subtropical;
		}
		return GameEnums.ClimateZone.Tropical;
	}

	private GameEnums.BiomeType DetermineBiome(GameEnums.ClimateZone zone, float temp, float precip, float elevation, float soilHumidity)
	{
		if (elevation > 0.7f)
		{
			return GameEnums.BiomeType.AlpineMeadow;
		}
		if (temp < -15f)
		{
			return GameEnums.BiomeType.IceSheet;
		}
		if (soilHumidity > 85f && elevation < 0.3f)
		{
			return GameEnums.BiomeType.WetMarshPlain;
		}
		return zone switch
		{
			GameEnums.ClimateZone.PolarFrigid => GameEnums.BiomeType.IceSheet, 
			GameEnums.ClimateZone.Subarctic => (precip > 300f) ? GameEnums.BiomeType.Tundra : GameEnums.BiomeType.Tundra, 
			GameEnums.ClimateZone.TemperateCold => (precip > 500f) ? GameEnums.BiomeType.BorealForest : GameEnums.BiomeType.TemperateGrassland, 
			GameEnums.ClimateZone.TemperateMild => (precip > 600f) ? GameEnums.BiomeType.DeciduousForest : GameEnums.BiomeType.TemperateGrassland, 
			GameEnums.ClimateZone.TemperateWarm => (precip > 700f) ? GameEnums.BiomeType.DeciduousForest : GameEnums.BiomeType.TemperateGrassland, 
			GameEnums.ClimateZone.Subtropical => (precip > 800f) ? GameEnums.BiomeType.TropicalMonsoon : ((precip > 400f) ? GameEnums.BiomeType.Savanna : GameEnums.BiomeType.HotDesert), 
			GameEnums.ClimateZone.Tropical => (precip > 2000f) ? GameEnums.BiomeType.TropicalRainforest : ((precip > 800f) ? GameEnums.BiomeType.TropicalMonsoon : ((precip > 400f) ? GameEnums.BiomeType.Savanna : GameEnums.BiomeType.HotDesert)), 
			GameEnums.ClimateZone.HighlandAlpine => GameEnums.BiomeType.AlpineMeadow, 
			GameEnums.ClimateZone.InlandAridTemperate => (precip < 200f) ? GameEnums.BiomeType.HotDesert : GameEnums.BiomeType.TemperateGrassland, 
			_ => GameEnums.BiomeType.TemperateGrassland, 
		};
	}

	private float GetTileLatitude(int index)
	{
		float num = (float)(index / _width) / (float)(_height - 1);
		return Mathf.Lerp(_config.planetMaxLat, 0f - _config.planetMaxLat, num);
	}
}
}
