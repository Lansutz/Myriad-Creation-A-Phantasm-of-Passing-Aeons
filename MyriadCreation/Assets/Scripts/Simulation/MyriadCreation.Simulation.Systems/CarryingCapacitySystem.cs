using System.Collections.Generic;
using MyriadCreation.Core.Data;
using MyriadCreation.Core.Enums;
using MyriadCreation.Simulation.State;
using UnityEngine;
using MyriadCreation.Content.Definitions;

namespace MyriadCreation.Simulation.Systems
{
public static class CarryingCapacitySystem
{
	public const float BaseCapacity = 100f;

	public static readonly int[] FoodGoodsIds = new int[4] { 0, 1, 2, 10 };

	public static float GetTerrainMultiplier(TileData tile)
	{
		if (tile.isCoast && tile.biome != GameEnums.BiomeType.Fjord)
		{
			return 1.1f;
		}
		if (tile.isRiver)
		{
			return 1.2f;
		}
		switch (tile.biome)
		{
		case GameEnums.BiomeType.AlluvialPlain:
		case GameEnums.BiomeType.GreatRiverPlain:
		case GameEnums.BiomeType.Delta:
		case GameEnums.BiomeType.PluvialFan:
		case GameEnums.BiomeType.DesertOasis:
		case GameEnums.BiomeType.EndorheicLake:
			return 1.2f;
		case GameEnums.BiomeType.Interfluvial:
		case GameEnums.BiomeType.SedimentaryBasin:
		case GameEnums.BiomeType.PiedmontBasin:
		case GameEnums.BiomeType.VolcanicAshPlain:
		case GameEnums.BiomeType.LowHills:
		case GameEnums.BiomeType.CoastalLowland:
		case GameEnums.BiomeType.DeciduousForest:
		case GameEnums.BiomeType.EvergreenForest:
		case GameEnums.BiomeType.MonsoonForest:
		case GameEnums.BiomeType.SemiAridShrubland:
		case GameEnums.BiomeType.TemperateGrassland:
			return 1f;
		case GameEnums.BiomeType.EnclosedBasin:
		case GameEnums.BiomeType.LoessPlateau:
		case GameEnums.BiomeType.BrokenPlateau:
		case GameEnums.BiomeType.ContinentalIslands:
		case GameEnums.BiomeType.ContinentalIslet:
		case GameEnums.BiomeType.PlateauMarsh:
		case GameEnums.BiomeType.Savanna:
		case GameEnums.BiomeType.TropicalMonsoon:
			return 0.8f;
		case GameEnums.BiomeType.WetMarshPlain:
		case GameEnums.BiomeType.Swamp:
		case GameEnums.BiomeType.FoldMountains:
		case GameEnums.BiomeType.KarstMountains:
		case GameEnums.BiomeType.ErodedBadlands:
		case GameEnums.BiomeType.CoastalSaltMarsh:
		case GameEnums.BiomeType.BorealForest:
		case GameEnums.BiomeType.TropicalRainforest:
		case GameEnums.BiomeType.Mangrove:
		case GameEnums.BiomeType.RiverSourceMarsh:
		case GameEnums.BiomeType.AlpineMeadow:
			return 0.6f;
		case GameEnums.BiomeType.InlandAridBasin:
		case GameEnums.BiomeType.LoessKarst:
		case GameEnums.BiomeType.HighMountains:
		case GameEnums.BiomeType.Fjord:
		case GameEnums.BiomeType.VolcanicIslands:
		case GameEnums.BiomeType.VolcanicIslandArc:
		case GameEnums.BiomeType.HotDesert:
		case GameEnums.BiomeType.InlandDesert:
		case GameEnums.BiomeType.ColdDesert:
		case GameEnums.BiomeType.CoastalDesert:
		case GameEnums.BiomeType.SaltDesert:
		case GameEnums.BiomeType.GravelGobi:
			return 0.4f;
		case GameEnums.BiomeType.CoralAtoll:
		case GameEnums.BiomeType.ImpactCraterAtoll:
		case GameEnums.BiomeType.IceSheet:
		case GameEnums.BiomeType.MountainGlacier:
		case GameEnums.BiomeType.Tundra:
			return 0.2f;
		default:
			return 1f;
		}
	}

	public static float CalculateCarryingCapacity(TileData tile, IReadOnlyDictionary<int, TradeCenter> tradeCenters)
	{
		float nppFactor = tile.npp > 0f ? 0.5f + 0.5f * Mathf.Clamp01(tile.npp) : 1f;
		float num = 100f * GetTerrainMultiplier(tile) * nppFactor;
		if (tradeCenters != null && tradeCenters.TryGetValue(tile.regionId, out var value))
		{
			num *= 1.15f;
			float num2 = 0f;
			int[] foodGoodsIds = FoodGoodsIds;
			foreach (int key in foodGoodsIds)
			{
				num2 += value.inventory.GetValueOrDefault(key, 0f);
			}
			float num3 = GetTotalPopulation(tile) * 0.01f * 30f;
			float num4 = ((num3 > 0f) ? (num2 / num3) : 1f);
			num *= Mathf.Clamp(num4, 0.5f, 2f);
		}
		return num;
	}

	public static float GetTotalPopulation(TileData tile)
	{
		if (tile.populationBlocks == null)
		{
			return 0f;
		}
		float num = 0f;
		foreach (PopulationBlock populationBlock in tile.populationBlocks)
		{
			num += populationBlock.count;
		}
		return num;
	}

	public static float GetOverloadRatio(TileData tile, float capacity)
	{
		float totalPopulation = GetTotalPopulation(tile);
		if (!(capacity > 0f))
		{
			return 1f;
		}
		return totalPopulation / capacity;
	}
}
}
