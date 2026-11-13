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
	/// <summary>
	/// 一地块在顶级土地、完全开发下的常住人口上限（含单产余量，允许一部分脱产者）。
	/// 土地是有限的：再多的劳力也不能在一块地上无限增产，所以承载力只随土地质量线性变化。
	/// </summary>
	public const float MaxPopulationPerTile = 3000f;

	/// <summary>未开发土地仍有的采集/粗放耕种底子（占承载力的比例）。</summary>
	public const float DevelopmentFloor = 0.45f;

	/// <summary>开发度（开垦、灌溉、仓储、道路）对承载力的贡献比例。</summary>
	public const float DevelopmentWeight = 0.55f;

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
		float nppFactor = 0.5f + 0.5f * AgricultureYield.EffectiveNpp(tile);
		// 开发度代表开垦、灌溉、轮作与仓储：同一块地，未开发只能粗放采集，开发后能养更多脱产人口。
		float developmentFactor = DevelopmentFloor + DevelopmentWeight * Mathf.Clamp01(tile.development);
		// 土壤肥力与农业产出用同一乘子：承载力是“这块地能产出多少口粮”的另一种写法，
		// 不能只看 NPP 而忽略沙化/贫瘠，否则沙漠也会被算出几百人的承载力。
		float fertilityFactor = 0.2f + 0.8f * Mathf.Clamp01(tile.fertility);
		// 承载力是土地属性，不是粮仓存量：旧公式按地区库存抬高承载力，会造成
		// “人口涨→库存降→承载力跌→人口暴跌”的长周期振荡（P7 诊断里城市从 1104 掉到 397）。
		// 口径：可吸收劳力 × 单产 / 人均口粮；单产与农业产出同一套乘子，避免“承载力说能养 500、实际只产 300 的口粮”。
		float landQuality = GetTerrainMultiplier(tile) * nppFactor * developmentFactor * fertilityFactor;
		// 单产（AgricultureYield）已随肥力/开发下降；承载力只负责“这块地能住多少人”，
		// 两者都随土地质量线性缩放，避免出现“承载力说能养 500、实际只产 300 的口粮”的结构性饥荒。
		return MaxPopulationPerTile * landQuality;
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
