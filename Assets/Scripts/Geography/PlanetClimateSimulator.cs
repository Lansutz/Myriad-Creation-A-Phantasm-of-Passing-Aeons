using System;
using System.Collections.Generic;
using UnityEngine;

namespace Myriad.Geography
{
    public interface IClimateSimulator
    {
        void RecalculateDirtyTiles(ISet<int> dirtyTileIndices);
    }

    [Serializable]
    public sealed class SeaLandParams
    {
        [Range(0f, 1f)] public float seaLevel = .5f;
        [Range(.1f, .8f)] public float landAmount = .29f;
        [Range(0f, 1f)] public float landFragment = .4f;
        [Range(0f, 1f)] public float coastFragment = .3f;
        [Range(0f, 1f)] public float oceanBuffer = .35f;
        [Range(70f, 90f)] public float planetMaxLatitude = 90f;
        [Range(280f, 400f)] public float planetTotalLongitude = 360f;
    }

    [Serializable]
    public sealed class ClimateParams
    {
        public CirculationMode circulationMode = CirculationMode.TripleCell;
        [Range(-30f, 30f)] public float thermalEquatorLatitude = 7f;
        [Range(0f, 50f)] public float tropicalNorthEdge = 23f;
        [Range(-50f, 0f)] public float tropicalSouthEdge = -23f;
        [Range(800f, 1600f)] public float stellarIrradiance = 1361f;
        [Range(0f, 1f)] public float seasonIntensity = .4f;
        [Range(.2f, 1.8f)] public float heatTransport = 1f;
        [Range(0f, 1.2f)] public float monsoonStrength = .8f;

        [NonSerialized] public float albedo = .3f;
        [NonSerialized] public float greenhouseWarming = 33f;
        [NonSerialized] public float lapseRatePerKm = 6.5f;
    }

    [Serializable]
    public struct TileTerrainInput
    {
        public int tileIndex;
        [Range(-90f, 90f)] public float latitude;
        [Range(0f, 1f)] public float elevation01;
        [Range(0f, 90f)] public float slopeDegrees;
        [Range(0f, 1f)] public float waterAdjacentWeight;
        public bool isLand;
        public bool isCoast;
        [Range(-1f, 1f)] public float windwardFactor;
        public float regionalTemperatureOffset;
        public float regionalHumidityOffsetPercent;
    }

    [Serializable]
    public struct TileClimateData
    {
        public float annualTemperatureC;
        public float diurnalTemperatureRangeC;
        public float annualPrecipitationMm;
        public float airHumidityPercent;
        public float soilHumidityPercent;
        public float accumulatedTemperature;
        public float frostFreeDays;
        public ClimateZone climateZone;
        public BiomeType biome;
    }

    internal struct ClimateCaches
    {
        public float maxLatitude, thermalEquator, subtropicalBoundary, subpolarBoundary;
    }

    /// <summary>Pure climate calculator. Call only for tiles marked dirty after geography edits.</summary>
    public sealed class PlanetClimateSimulator : IClimateSimulator
    {
        private readonly SeaLandParams seaLand;
        private readonly ClimateParams climate;
        private ClimateCaches caches;
        public TileTerrainInput[] Terrains { get; }
        public TileClimateData[] Climates { get; }

        public PlanetClimateSimulator(SeaLandParams seaLand, ClimateParams climate, TileTerrainInput[] terrains)
        {
            this.seaLand = seaLand ?? throw new ArgumentNullException(nameof(seaLand));
            this.climate = climate ?? throw new ArgumentNullException(nameof(climate));
            Terrains = terrains ?? throw new ArgumentNullException(nameof(terrains));
            Climates = new TileClimateData[terrains.Length];
            RefreshGlobalCache();
        }

        public void RefreshGlobalCache()
        {
            int cells = climate.circulationMode == CirculationMode.SingleCell ? 1 : climate.circulationMode == CirculationMode.DoubleCell ? 2 : 3;
            float width = seaLand.planetMaxLatitude / cells * climate.heatTransport;
            caches = new ClimateCaches {
                maxLatitude = seaLand.planetMaxLatitude,
                thermalEquator = climate.thermalEquatorLatitude,
                subtropicalBoundary = width,
                subpolarBoundary = Mathf.Min(seaLand.planetMaxLatitude, width * 2.25f)
            };
        }

        public void RecalculateDirtyTiles(ISet<int> dirtyTileIndices)
        {
            if (dirtyTileIndices == null) throw new ArgumentNullException(nameof(dirtyTileIndices));
            foreach (int index in dirtyTileIndices)
                if (index >= 0 && index < Terrains.Length) Climates[index] = Calculate(Terrains[index]);
        }

        public TileClimateData Calculate(TileTerrainInput tile)
        {
            float equatorialC = BlackBodyCelsius(climate.stellarIrradiance, climate.albedo) + climate.greenhouseWarming;
            float latitudeDistance = Mathf.Abs(tile.latitude - caches.thermalEquator);
            float latitudeFactor = 1f - Mathf.Clamp01(latitudeDistance / caches.maxLatitude);
            float heatTransport = (1f - latitudeFactor) * equatorialC * .28f * climate.heatTransport;
            float elevationCooling = tile.elevation01 * 4f * climate.lapseRatePerKm;
            float temperature = equatorialC * latitudeFactor + heatTransport - elevationCooling + tile.regionalTemperatureOffset;
            temperature = Mathf.Lerp(temperature, temperature * .92f, tile.waterAdjacentWeight);

            float diurnalRange = (tile.isLand ? 18f : 6f) * (1f - tile.waterAdjacentWeight);
            if (tile.slopeDegrees > 30f) diurnalRange *= 1.5f;
            float precipitationFactor = PrecipitationFactor(latitudeDistance);
            if (tile.isLand && tile.isCoast) precipitationFactor += climate.monsoonStrength * .5f;
            precipitationFactor *= tile.windwardFactor > 0f ? 1.5f : tile.windwardFactor < 0f ? .4f : 1f;
            if (!tile.isLand) precipitationFactor *= 1.2f;
            float precipitation = Mathf.Max(0f, 1000f * precipitationFactor * (1f + tile.regionalHumidityOffsetPercent / 100f));
            float airHumidity = Mathf.Clamp(precipitation / 20f - diurnalRange * .8f + tile.waterAdjacentWeight * 30f, 0f, 100f);

            return new TileClimateData {
                annualTemperatureC = temperature,
                diurnalTemperatureRangeC = diurnalRange,
                annualPrecipitationMm = precipitation,
                airHumidityPercent = airHumidity,
                soilHumidityPercent = Mathf.Clamp(airHumidity * (1f - tile.slopeDegrees / 90f * .4f), 0f, 100f),
                accumulatedTemperature = Mathf.Max(0f, temperature) * 120f,
                frostFreeDays = Mathf.Lerp(0f, 365f, Mathf.Clamp01((temperature + 5f) / 25f)),
                climateZone = ClassifyZone(tile, temperature, precipitation),
                biome = ClassifyBiome(tile, temperature, precipitation)
            };
        }

        private float PrecipitationFactor(float latitudeDistance)
        {
            if (latitudeDistance < caches.subtropicalBoundary * .5f) return 1.4f;
            if (latitudeDistance < caches.subtropicalBoundary) return .3f;
            if (latitudeDistance < caches.subpolarBoundary) return 1.1f;
            return .2f;
        }

        private static float BlackBodyCelsius(float irradiance, float albedo)
        {
            const float sigma = 5.670374419e-8f;
            return Mathf.Pow(irradiance * (1f - albedo) / (4f * sigma), .25f) - 273.15f;
        }

        private static ClimateZone ClassifyZone(TileTerrainInput tile, float temperature, float precipitation)
        {
            if (tile.elevation01 > .75f) return ClimateZone.HighlandAlpine;
            if (Mathf.Abs(tile.latitude) > 25f && precipitation < 350f && temperature >= 0f && temperature < 16f) return ClimateZone.InlandAridTemperate;
            if (temperature < -10f) return ClimateZone.PolarFrigid;
            if (temperature < 0f) return ClimateZone.Subarctic;
            if (temperature < 8f) return ClimateZone.TemperateCold;
            if (temperature < 16f) return ClimateZone.TemperateMild;
            if (temperature < 22f) return ClimateZone.TemperateWarm;
            if (temperature < 28f) return ClimateZone.Subtropical;
            return ClimateZone.Tropical;
        }

        private static BiomeType ClassifyBiome(TileTerrainInput tile, float temperature, float precipitation)
        {
            if (!tile.isLand) return BiomeType.Wetland;
            if (tile.elevation01 > .75f) return BiomeType.Alpine;
            if (temperature < 0f) return BiomeType.IceSheet;
            if (temperature < 10f) return precipitation < 250f ? BiomeType.Tundra : BiomeType.BorealForest;
            if (temperature < 18f) return precipitation < 250f ? BiomeType.Desert : precipitation < 500f ? BiomeType.TemperateGrassland : BiomeType.TemperateForest;
            return precipitation < 250f ? BiomeType.Desert : precipitation < 700f ? BiomeType.Savanna : precipitation < 1500f ? BiomeType.TropicalMonsoon : BiomeType.TropicalRainforest;
        }
    }
}
