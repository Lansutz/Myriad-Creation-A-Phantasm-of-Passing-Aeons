using System;
using System.Collections.Generic;
using MyriadCreation.Core.Dto;
using MyriadCreation.Core.Enums;
using MyriadCreation.Content.Definitions;

namespace MyriadCreation.Simulation.State
{
[Serializable]
public class RaceDTO
{
	public int raceId;

	public string raceName;

	public string description;

	public float baseLifespan;

	public float growthRate;

	public float reproductionRate;

	public float physicalStrength;

	public float diseaseResistance;

	public float environmentalTolerance;

	public float visualAcuity;

	public float auditoryRange;

	public float olfactorySensitivity;

	public float cognitiveCapacity;

	public float transformativity;

	public List<IntFloatEntry> productionModifiers = new List<IntFloatEntry>();

	public List<IntFloatEntry> consumptionModifiers = new List<IntFloatEntry>();

	public float infantryBonus;

	public float cavalryBonus;

	public float navyBonus;

	public float moraleBase;

	public List<int> preferredBiomes = new List<int>();

	public float coldTolerance;

	public float heatTolerance;

	public float aridityTolerance;

	public float humidityTolerance;

	public float altitudeTolerance;

	public string genomeId = "genome_human";

	public string speciesId = "species_human";

	public float intelligenceBaseline;

	public float martialBaseline;

	public float lifespanBaseYears;

	public float lifespanRangeYears;

	public float resistanceBaseline;

	public List<LocusFrequency> locusFrequencies = new List<LocusFrequency>();

	public static RaceDTO FromRaceData(RaceData r)
	{
		RaceDTO raceDTO = new RaceDTO
		{
			raceId = r.raceId,
			raceName = r.raceName,
			description = r.description,
			baseLifespan = r.baseLifespan,
			growthRate = r.growthRate,
			reproductionRate = r.reproductionRate,
			physicalStrength = r.physicalStrength,
			diseaseResistance = r.diseaseResistance,
			environmentalTolerance = r.environmentalTolerance,
			visualAcuity = r.visualAcuity,
			auditoryRange = r.auditoryRange,
			olfactorySensitivity = r.olfactorySensitivity,
			cognitiveCapacity = r.cognitiveCapacity,
			transformativity = r.transformativity,
			infantryBonus = r.infantryBonus,
			cavalryBonus = r.cavalryBonus,
			navyBonus = r.navyBonus,
			moraleBase = r.moraleBase,
			coldTolerance = r.coldTolerance,
			heatTolerance = r.heatTolerance,
			aridityTolerance = r.aridityTolerance,
			humidityTolerance = r.humidityTolerance,
			altitudeTolerance = r.altitudeTolerance,
			genomeId = r.genomeId,
			speciesId = r.speciesId,
			intelligenceBaseline = r.intelligenceBaseline,
			martialBaseline = r.martialBaseline,
			lifespanBaseYears = r.lifespanBaseYears,
			lifespanRangeYears = r.lifespanRangeYears,
			resistanceBaseline = r.resistanceBaseline,
			locusFrequencies = (r.locusFrequencies ?? new List<LocusFrequency>())
		};
		if (r.productionModifiers != null)
		{
			foreach (KeyValuePair<GameEnums.GoodsCategory, float> productionModifier in r.productionModifiers)
			{
				raceDTO.productionModifiers.Add(new IntFloatEntry((int)productionModifier.Key, productionModifier.Value));
			}
		}
		if (r.consumptionModifiers != null)
		{
			foreach (KeyValuePair<GameEnums.GoodsCategory, float> consumptionModifier in r.consumptionModifiers)
			{
				raceDTO.consumptionModifiers.Add(new IntFloatEntry((int)consumptionModifier.Key, consumptionModifier.Value));
			}
		}
		if (r.preferredBiomes != null)
		{
			foreach (GameEnums.BiomeType preferredBiome in r.preferredBiomes)
			{
				raceDTO.preferredBiomes.Add((int)preferredBiome);
			}
		}
		return raceDTO;
	}

	public RaceData ToRaceData()
	{
		RaceData raceData = new RaceData
		{
			raceId = raceId,
			raceName = raceName,
			description = description,
			genomeId = (string.IsNullOrEmpty(genomeId) ? "genome_human" : genomeId),
			speciesId = (string.IsNullOrEmpty(speciesId) ? "species_human" : speciesId),
			baseLifespan = baseLifespan,
			growthRate = growthRate,
			reproductionRate = reproductionRate,
			physicalStrength = physicalStrength,
			diseaseResistance = diseaseResistance,
			environmentalTolerance = environmentalTolerance,
			visualAcuity = visualAcuity,
			auditoryRange = auditoryRange,
			olfactorySensitivity = olfactorySensitivity,
			cognitiveCapacity = cognitiveCapacity,
			transformativity = transformativity,
			infantryBonus = infantryBonus,
			cavalryBonus = cavalryBonus,
			navyBonus = navyBonus,
			moraleBase = moraleBase,
			coldTolerance = coldTolerance,
			heatTolerance = heatTolerance,
			aridityTolerance = aridityTolerance,
			humidityTolerance = humidityTolerance,
			altitudeTolerance = altitudeTolerance,
			intelligenceBaseline = intelligenceBaseline,
			martialBaseline = martialBaseline,
			lifespanBaseYears = lifespanBaseYears,
			lifespanRangeYears = lifespanRangeYears,
			resistanceBaseline = resistanceBaseline,
			locusFrequencies = (locusFrequencies ?? new List<LocusFrequency>())
		};
		foreach (IntFloatEntry productionModifier in productionModifiers)
		{
			raceData.productionModifiers[(GameEnums.GoodsCategory)productionModifier.key] = productionModifier.value;
		}
		foreach (IntFloatEntry consumptionModifier in consumptionModifiers)
		{
			raceData.consumptionModifiers[(GameEnums.GoodsCategory)consumptionModifier.key] = consumptionModifier.value;
		}
		foreach (int preferredBiome in preferredBiomes)
		{
			raceData.preferredBiomes.Add((GameEnums.BiomeType)preferredBiome);
		}
		return raceData;
	}
}
}
