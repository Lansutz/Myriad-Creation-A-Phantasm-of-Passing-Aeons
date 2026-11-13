using System;
using System.Collections.Generic;
using MyriadCreation.Core.Data;
using MyriadCreation.Core.Enums;
using UnityEngine;
using MyriadCreation.Content.Definitions;

namespace MyriadCreation.Simulation.State
{
[Serializable]
public class RaceData
{
	public int raceId;

	public string raceName;

	public string description;

	[Header("生理参数")]
	[Range(0.5f, 2.5f)]
	public float baseLifespan = 1f;

	[Range(0.5f, 2f)]
	public float growthRate = 1f;

	[Range(0.5f, 2f)]
	public float reproductionRate = 1f;

	[Range(0.5f, 2f)]
	public float physicalStrength = 1f;

	[Range(0.5f, 2f)]
	public float diseaseResistance = 1f;

	[Range(0.5f, 2f)]
	public float environmentalTolerance = 1f;

	[Header("感官认知")]
	[Range(0.5f, 2f)]
	public float visualAcuity = 1f;

	[Range(0.5f, 2f)]
	public float auditoryRange = 1f;

	[Range(0.5f, 2f)]
	public float olfactorySensitivity = 1f;

	[Range(0.5f, 2f)]
	public float cognitiveCapacity = 1f;

	[Header("变革性（唯一社会维度）")]
	[Range(0f, 100f)]
	public float transformativity = 50f;

	[NonSerialized]
	[Header("经济偏好")]
	public Dictionary<GameEnums.GoodsCategory, float> productionModifiers = new Dictionary<GameEnums.GoodsCategory, float>();

	[NonSerialized]
	public Dictionary<GameEnums.GoodsCategory, float> consumptionModifiers = new Dictionary<GameEnums.GoodsCategory, float>();

	[Header("军事偏好")]
	[Range(0.5f, 2f)]
	public float infantryBonus = 1f;

	[Range(0.5f, 2f)]
	public float cavalryBonus = 1f;

	[Range(0.5f, 2f)]
	public float navyBonus = 1f;

	[Range(0.5f, 2f)]
	public float moraleBase = 1f;

	[Header("基因组（新遗传学权威）")]
	public string genomeId = "genome_human";

	public string speciesId = "species_human";

	[Header("DNA 基准（个体偏移的种族锚点）")]
	[Tooltip("智慧基准 0-100，DNA 智慧偏移叠加其上")]
	[Range(0f, 100f)]
	public float intelligenceBaseline = 50f;

	[Tooltip("勇武基准 0-100，DNA 勇武偏移叠加其上")]
	[Range(0f, 100f)]
	public float martialBaseline = 50f;

	[Tooltip("寿命区间中心（年），DNA 寿命偏移决定个体预期寿命")]
	[Range(40f, 120f)]
	public float lifespanBaseYears = 75f;

	[Tooltip("寿命区间半宽（年），区间 = 基准±半宽")]
	[Range(5f, 30f)]
	public float lifespanRangeYears = 15f;

	[Tooltip("综合抗性基准 0-100")]
	[Range(0f, 100f)]
	public float resistanceBaseline = 50f;

	[Header("DNA 基因频率（各基因座显性等位基因 A 的频率；列表未覆盖的基因座取默认 0.5）")]
	public List<LocusFrequency> locusFrequencies = new List<LocusFrequency>();

	[Header("环境适配")]
	public List<GameEnums.BiomeType> preferredBiomes = new List<GameEnums.BiomeType>();

	[Range(0f, 1f)]
	public float coldTolerance = 0.5f;

	[Range(0f, 1f)]
	public float heatTolerance = 0.5f;

	[Range(0f, 1f)]
	public float aridityTolerance = 0.5f;

	[Range(0f, 1f)]
	public float humidityTolerance = 0.5f;

	[Range(0f, 1f)]
	public float altitudeTolerance = 0.5f;

	public float CultureMaturityRate => 0.5f + transformativity / 100f;

	public float SeparationResistanceMod => 1f - transformativity / 200f;

	public float FusionResistanceMod => 1f - transformativity / 150f;

	public float ClassRelationBaseline => 30f + transformativity * 0.4f;

	public float InnovationGainMultiplier => 0.5f + transformativity / 100f;

	public float RebellionThreshold => 25f + transformativity * 0.3f;

	public float SocialMobility => transformativity / 100f;

	public float ViolenceBaseline => 30f + Mathf.Abs(transformativity - 50f) * 0.4f;

	public string GetTransformativityTier()
	{
		if (transformativity >= 75f)
		{
			return "高变革性：个体主义、创新驱动、阶层流动、文化多元";
		}
		if (transformativity >= 40f)
		{
			return "中变革性：平衡型，传统与创新并存";
		}
		return "低变革性：集体主义、传统坚守、阶层固化、文化正统";
	}

	public string GetGenomeId()
	{
		if (!string.IsNullOrEmpty(genomeId))
		{
			return genomeId;
		}
		return "genome_human";
	}

	public float GetLocusAFrequency(DnaLocus locus)
	{
		if (locusFrequencies != null)
		{
			foreach (LocusFrequency locusFrequency in locusFrequencies)
			{
				if (locusFrequency.locus == locus)
				{
					return Mathf.Clamp01(locusFrequency.dominantFrequency);
				}
			}
		}
		return 0.5f;
	}

	public float CalculateEnvironmentFitness(TileData tile)
	{
		float num = ((tile.annualTemp < 10f) ? Mathf.Lerp(0.3f, 1f, coldTolerance) : Mathf.Lerp(0.3f, 1f, heatTolerance));
		float num2 = ((tile.airHumidityPct < 40f) ? Mathf.Lerp(0.3f, 1f, aridityTolerance) : Mathf.Lerp(0.3f, 1f, humidityTolerance));
		float num3 = ((tile.elevation01 > 0.4f) ? Mathf.Lerp(0.3f, 1f, altitudeTolerance) : 1f);
		float num4 = (preferredBiomes.Contains(tile.biome) ? 1.2f : 1f);
		return Mathf.Clamp(num * 0.35f + num2 * 0.35f + num3 * 0.3f, 0.1f, 1f) * num4;
	}

	public float CalculatePopulationGrowthRate(TileData tile, float satisfaction)
	{
		float num = CalculateEnvironmentFitness(tile);
		// 前现代人口自然增长约 0.1%–1%/年；旧值 0.002/日 ≈ 73%/年，人口一年就冲满承载力，
		// 随后靠饥荒/迁出回落，长跑里表现为剧烈振荡而不是缓慢生灭（P7 诊断）。
		float num2 = 0.00003f * reproductionRate;
		float num3 = 0.5f + satisfaction / 100f;
		float num4 = 0.3f + num * 0.7f;
		return num2 * num3 * num4;
	}

	public float GetDiseaseInfectionMod()
	{
		return 1f / diseaseResistance;
	}
}
}
