using MyriadCreation.Core.Enums;
using MyriadCreation.World.Climate;
using UnityEngine;

namespace MyriadCreation.World.Biome
{
public static class HoldridgeBiomeClassifier
{
	public static float CalculateBiotemperature(float meanAnnualTemp)
	{
		float num = Mathf.Clamp(meanAnnualTemp, 0f, 30f);
		if (meanAnnualTemp < 10f)
		{
			num *= 0.6f + 0.4f * (meanAnnualTemp / 10f);
		}
		return num;
	}

	public static float CalculatePET(float biotemperature)
	{
		return Mathf.Max(0f, biotemperature * 50f);
	}

	public static float CalculatePER(float pet, float precipitation)
	{
		if (precipitation < 1f)
		{
			return 100f;
		}
		return pet / precipitation;
	}

	public static HumidityProvince GetHumidityProvince(float per)
	{
		if (per > 16f)
		{
			return HumidityProvince.SuperArid;
		}
		if (per > 8f)
		{
			return HumidityProvince.PerArid;
		}
		if (per > 4f)
		{
			return HumidityProvince.Arid;
		}
		if (per > 2f)
		{
			return HumidityProvince.SemiArid;
		}
		if (per > 1f)
		{
			return HumidityProvince.SubHumid;
		}
		if (per > 0.5f)
		{
			return HumidityProvince.Humid;
		}
		if (per > 0.25f)
		{
			return HumidityProvince.PerHumid;
		}
		return HumidityProvince.SuperHumid;
	}

	public static ThermalBelt GetThermalBelt(float abt)
	{
		if (abt < 1.5f)
		{
			return ThermalBelt.Nival;
		}
		if (abt < 3f)
		{
			return ThermalBelt.Tundra;
		}
		if (abt < 6f)
		{
			return ThermalBelt.Boreal;
		}
		if (abt < 12f)
		{
			return ThermalBelt.CoolTemperate;
		}
		if (abt < 18f)
		{
			return ThermalBelt.WarmTemperate;
		}
		if (abt < 24f)
		{
			return ThermalBelt.Subtropical;
		}
		return ThermalBelt.Tropical;
	}

	public static GameEnums.BiomeType Classify(float meanAnnualTemp, float precipitation, float elevation01, bool isLand, bool isCoast, bool isRiver, float slopeDegree, float latAbs)
	{
		if (!isLand)
		{
			if (isCoast)
			{
				return GameEnums.BiomeType.Mangrove;
			}
			return GameEnums.BiomeType.EndorheicLake;
		}
		float num = CalculateBiotemperature(meanAnnualTemp);
		float num2 = CalculatePER(CalculatePET(num), precipitation);
		ThermalBelt thermalBelt = GetThermalBelt(num);
		HumidityProvince humidityProvince = GetHumidityProvince(num2);
		if (elevation01 > 0.9f)
		{
			if (meanAnnualTemp < -5f)
			{
				return GameEnums.BiomeType.MountainGlacier;
			}
			return GameEnums.BiomeType.HighMountains;
		}
		if (elevation01 > 0.75f && slopeDegree > 20f)
		{
			if (thermalBelt <= ThermalBelt.Boreal)
			{
				return GameEnums.BiomeType.AlpineMeadow;
			}
			return GameEnums.BiomeType.FoldMountains;
		}
		if (isRiver && elevation01 < 0.4f && humidityProvince >= HumidityProvince.Humid)
		{
			return GameEnums.BiomeType.Swamp;
		}
		if (isCoast && precipitation > 1500f && thermalBelt >= ThermalBelt.Subtropical)
		{
			return GameEnums.BiomeType.Mangrove;
		}
		if (isCoast && elevation01 < 0.3f)
		{
			return GameEnums.BiomeType.CoastalLowland;
		}
		switch (thermalBelt)
		{
		case ThermalBelt.Nival:
			return GameEnums.BiomeType.IceSheet;
		case ThermalBelt.Tundra:
			if (humidityProvince > HumidityProvince.Arid)
			{
				return GameEnums.BiomeType.Tundra;
			}
			return GameEnums.BiomeType.ColdDesert;
		case ThermalBelt.Boreal:
			if (humidityProvince <= HumidityProvince.SemiArid)
			{
				return GameEnums.BiomeType.Tundra;
			}
			return GameEnums.BiomeType.BorealForest;
		case ThermalBelt.CoolTemperate:
			if (humidityProvince <= HumidityProvince.Arid)
			{
				return GameEnums.BiomeType.ColdDesert;
			}
			if (humidityProvince <= HumidityProvince.SemiArid)
			{
				return GameEnums.BiomeType.TemperateGrassland;
			}
			if (humidityProvince <= HumidityProvince.SubHumid)
			{
				if (!(elevation01 > 0.6f))
				{
					return GameEnums.BiomeType.DeciduousForest;
				}
				return GameEnums.BiomeType.LowHills;
			}
			return GameEnums.BiomeType.DeciduousForest;
		case ThermalBelt.WarmTemperate:
			if (humidityProvince <= HumidityProvince.Arid)
			{
				if (!isCoast)
				{
					return GameEnums.BiomeType.InlandDesert;
				}
				return GameEnums.BiomeType.CoastalDesert;
			}
			if (humidityProvince <= HumidityProvince.SemiArid)
			{
				return GameEnums.BiomeType.SemiAridShrubland;
			}
			if (humidityProvince <= HumidityProvince.SubHumid)
			{
				if (!(elevation01 > 0.55f))
				{
					return GameEnums.BiomeType.DeciduousForest;
				}
				return GameEnums.BiomeType.LowHills;
			}
			if (latAbs > 30f && latAbs < 45f && num2 > 0.8f)
			{
				return GameEnums.BiomeType.EvergreenForest;
			}
			return GameEnums.BiomeType.DeciduousForest;
		case ThermalBelt.Subtropical:
			if (humidityProvince <= HumidityProvince.Arid)
			{
				if (!isCoast)
				{
					return GameEnums.BiomeType.HotDesert;
				}
				return GameEnums.BiomeType.CoastalDesert;
			}
			if (humidityProvince <= HumidityProvince.SemiArid)
			{
				return GameEnums.BiomeType.Savanna;
			}
			if (humidityProvince <= HumidityProvince.SubHumid)
			{
				return GameEnums.BiomeType.TropicalMonsoon;
			}
			if (humidityProvince <= HumidityProvince.Humid)
			{
				return GameEnums.BiomeType.MonsoonForest;
			}
			return GameEnums.BiomeType.EvergreenForest;
		case ThermalBelt.Tropical:
			if (humidityProvince <= HumidityProvince.Arid)
			{
				if (!isCoast)
				{
					return GameEnums.BiomeType.HotDesert;
				}
				return GameEnums.BiomeType.CoastalDesert;
			}
			if (humidityProvince <= HumidityProvince.SemiArid)
			{
				return GameEnums.BiomeType.Savanna;
			}
			if (humidityProvince <= HumidityProvince.SubHumid)
			{
				return GameEnums.BiomeType.TropicalMonsoon;
			}
			return GameEnums.BiomeType.TropicalRainforest;
		default:
			return GameEnums.BiomeType.TemperateGrassland;
		}
	}

	public static float CalculateFertility(float meanAnnualTemp, float precipitation, float elevation01, float slopeDegree, GameEnums.BiomeType biome)
	{
		float num = CalculateBiotemperature(meanAnnualTemp);
		float num2 = CalculatePER(CalculatePET(num), precipitation);
		float num3 = Mathf.Exp(0f - Mathf.Pow((num - 15f) / 12f, 2f));
		float num4 = Mathf.Exp(0f - Mathf.Pow((num2 - 0.75f) / 1.5f, 2f));
		float num5 = num3 * num4 * 0.8f + 0.1f;
		switch (biome)
		{
		case GameEnums.BiomeType.AlluvialPlain:
		case GameEnums.BiomeType.GreatRiverPlain:
		case GameEnums.BiomeType.Delta:
		case GameEnums.BiomeType.VolcanicAshPlain:
			num5 = Mathf.Max(num5, 0.85f);
			break;
		case GameEnums.BiomeType.Interfluvial:
		case GameEnums.BiomeType.EnclosedBasin:
		case GameEnums.BiomeType.DesertOasis:
			num5 = Mathf.Max(num5, 0.7f);
			break;
		case GameEnums.BiomeType.DeciduousForest:
		case GameEnums.BiomeType.EvergreenForest:
		case GameEnums.BiomeType.TropicalRainforest:
			num5 *= 0.85f;
			break;
		case GameEnums.BiomeType.Savanna:
		case GameEnums.BiomeType.TemperateGrassland:
			num5 *= 0.9f;
			break;
		case GameEnums.BiomeType.HighMountains:
		case GameEnums.BiomeType.IceSheet:
		case GameEnums.BiomeType.MountainGlacier:
		case GameEnums.BiomeType.Tundra:
		case GameEnums.BiomeType.HotDesert:
		case GameEnums.BiomeType.InlandDesert:
		case GameEnums.BiomeType.ColdDesert:
			num5 *= 0.15f;
			break;
		}
		num5 *= Mathf.Clamp01(1f - Mathf.Max(0f, slopeDegree - 10f) / 50f);
		num5 *= Mathf.Clamp01(1f - Mathf.Max(0f, elevation01 - 0.5f) * 1.2f);
		return Mathf.Clamp01(num5);
	}
}
}
