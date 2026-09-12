
using System.Collections.Generic;
using System;
using CivilizationEvolution.Core;
using CivilizationEvolution.Core.Constants;
using CivilizationEvolution.Core.Data;
using CivilizationEvolution.Core.Dto;
using CivilizationEvolution.Core.Enums;
using CivilizationEvolution.Simulation.Economy;
using CivilizationEvolution.Simulation.Events;
using CivilizationEvolution.Simulation.Generation;
using CivilizationEvolution.Simulation.Modding;
using CivilizationEvolution.Simulation.Politics;
using CivilizationEvolution.Simulation.WorldState;



namespace CivilizationEvolution.Simulation.Society
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
 // DNA 基准与基因频率（v3 存档新增；旧档缺失字段走默认值）
        public float intelligenceBaseline;
        public float martialBaseline;
        public float lifespanBaseYears;
        public float lifespanRangeYears;
        public float resistanceBaseline;
        public List<LocusFrequency> locusFrequencies = new List<LocusFrequency>();

        public static RaceDTO FromRaceData(RaceData r)
        {
            var dto = new RaceDTO
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
                intelligenceBaseline = r.intelligenceBaseline,
                martialBaseline = r.martialBaseline,
                lifespanBaseYears = r.lifespanBaseYears,
                lifespanRangeYears = r.lifespanRangeYears,
                resistanceBaseline = r.resistanceBaseline,
                locusFrequencies = r.locusFrequencies ?? new List<LocusFrequency>()
            };
            if (r.productionModifiers != null)
                foreach (var kv in r.productionModifiers) dto.productionModifiers.Add(new IntFloatEntry((int)kv.Key, kv.Value));
            if (r.consumptionModifiers != null)
                foreach (var kv in r.consumptionModifiers) dto.consumptionModifiers.Add(new IntFloatEntry((int)kv.Key, kv.Value));
            if (r.preferredBiomes != null)
                foreach (var b in r.preferredBiomes) dto.preferredBiomes.Add((int)b);
            return dto;
        }

        public RaceData ToRaceData()
        {
            var r = new RaceData
            {
                raceId = raceId,
                raceName = raceName,
                description = description,
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
                locusFrequencies = locusFrequencies ?? new List<LocusFrequency>()
            };
            foreach (var e in productionModifiers) r.productionModifiers[(GameEnums.GoodsCategory)e.key] = e.value;
            foreach (var e in consumptionModifiers) r.consumptionModifiers[(GameEnums.GoodsCategory)e.key] = e.value;
            foreach (int b in preferredBiomes) r.preferredBiomes.Add((GameEnums.BiomeType)b);
            return r;
        }
    }
}
