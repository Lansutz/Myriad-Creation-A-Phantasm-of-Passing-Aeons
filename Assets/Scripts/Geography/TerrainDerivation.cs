using System.Collections.Generic;
using UnityEngine;

namespace Myriad.Geography
{
    /// <summary>Derives stable terrain values from edited elevation and climate; it does not mutate development.</summary>
    public static class TerrainDerivation
    {
        public static float CalculateSlopeDegrees(float elevationMeters, IEnumerable<float> neighbourElevationsMeters, float averageEdgeLengthMeters)
        {
            float maximumDifference = 0f;
            foreach (float neighbourElevation in neighbourElevationsMeters)
                maximumDifference = Mathf.Max(maximumDifference, Mathf.Abs(elevationMeters - neighbourElevation));
            return Mathf.Atan2(maximumDifference, Mathf.Max(1f, averageEdgeLengthMeters)) * Mathf.Rad2Deg;
        }

        public static LandformTag[] Classify(TileGeography tile, TileClimateData climate, bool hasVolcano, bool isDepression)
        {
            var tags = new List<LandformTag>();
            if (hasVolcano) tags.Add(LandformTag.Volcanic);
            if (tile.waterBody == WaterBodyKind.LargeInlandLake && tile.isSaltWater) tags.Add(LandformTag.SaltLake);
            if (isDepression) tags.Add(LandformTag.Depression);
            if (tile.isGate) tags.Add(LandformTag.GateFortress);
            if (tile.riverRank != RiverRank.None) tags.Add(LandformTag.RiverCrossing);
            if (climate.biome == BiomeType.Desert) tags.Add(LandformTag.Desert);
            if (climate.biome == BiomeType.IceSheet || climate.biome == BiomeType.Tundra) tags.Add(LandformTag.SnowOrTundra);
            if (climate.biome == BiomeType.TemperateForest || climate.biome == BiomeType.BorealForest) tags.Add(LandformTag.Forest);
            if (tile.slopeDegrees >= 30f) tags.Add(LandformTag.Mountain);
            else if (tile.slopeDegrees >= 12f) tags.Add(LandformTag.Hill);
            else tags.Add(LandformTag.Plain);
            return tags.ToArray();
        }

        public static float CalculateBaseFertility(TileGeography tile, TileClimateData climate, bool hasVolcano)
        {
            if (tile.waterBody != WaterBodyKind.None && tile.riverRank == RiverRank.None) return 0f;
            float moisture = Mathf.InverseLerp(20f, 75f, climate.soilHumidityPercent);
            float temperature = Mathf.InverseLerp(2f, 22f, climate.annualTemperatureC);
            float slopePenalty = Mathf.InverseLerp(5f, 45f, tile.slopeDegrees) * .45f;
            float riverBonus = tile.riverRank == RiverRank.MainRiver ? .3f : tile.riverRank == RiverRank.PrimaryTributary ? .18f : 0f;
            float volcanicBonus = hasVolcano ? .25f : 0f;
            return Mathf.Clamp01(moisture * temperature - slopePenalty + riverBonus + volcanicBonus);
        }
    }
}
