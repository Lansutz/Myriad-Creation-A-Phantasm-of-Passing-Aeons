using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;

namespace Myriad.Geography
{
    /// <summary>
    /// Owns dirty climate updates. Terrain and water editors should call MarkTileAndNeighboursDirty;
    /// it never schedules a full-map climate refresh for a local edit.
    /// </summary>
    public sealed class ClimateDirtyRecalculationSystem : MonoBehaviour
    {
        [SerializeField] private SeaLandParams seaLand = new SeaLandParams();
        [SerializeField] private ClimateParams climate = new ClimateParams();
        [SerializeField] private TileTerrainInput[] terrainInputs = Array.Empty<TileTerrainInput>();
        [Tooltip("Each row contains the neighbouring free-form polygon tile indices.")]
        [SerializeField] private TileNeighbourList[] neighbours = Array.Empty<TileNeighbourList>();

        private readonly HashSet<int> dirty = new HashSet<int>();
        private PlanetClimateSimulator simulator;
        private NativeArray<TileTerrainInput> scheduledInputs;
        private NativeArray<TileClimateData> scheduledOutput;
        private NativeArray<int> scheduledIndices;
        private JobHandle pendingJob;
        private bool hasPendingJob;

        public TileClimateData[] Climates => simulator?.Climates;

        private void Awake()
        {
            simulator = new PlanetClimateSimulator(seaLand, climate, terrainInputs);
            for (int i = 0; i < terrainInputs.Length; i++) dirty.Add(i);
        }

        private void Update()
        {
            CompletePendingJob();
            if (!hasPendingJob && dirty.Count > 0) ScheduleDirtyJob();
        }

        private void OnDestroy()
        {
            if (hasPendingJob)
            {
                pendingJob.Complete();
                hasPendingJob = false;
            }
            DisposeNativeArrays();
        }

        public void MarkTileAndNeighboursDirty(int tileIndex)
        {
            AddDirty(tileIndex);
            if (tileIndex < 0 || tileIndex >= neighbours.Length) return;
            foreach (int neighbour in neighbours[tileIndex].indices) AddDirty(neighbour);
        }

        public void SetTerrain(int tileIndex, TileTerrainInput replacement)
        {
            if (tileIndex < 0 || tileIndex >= terrainInputs.Length) throw new ArgumentOutOfRangeException(nameof(tileIndex));
            terrainInputs[tileIndex] = replacement;
            MarkTileAndNeighboursDirty(tileIndex);
        }

        public void RefreshGlobalClimateParameters()
        {
            simulator.RefreshGlobalCache();
            for (int i = 0; i < terrainInputs.Length; i++) dirty.Add(i);
        }

        private void ScheduleDirtyJob()
        {
            int count = dirty.Count;
            scheduledInputs = new NativeArray<TileTerrainInput>(terrainInputs, Allocator.TempJob);
            scheduledOutput = new NativeArray<TileClimateData>(count, Allocator.TempJob);
            scheduledIndices = new NativeArray<int>(count, Allocator.TempJob);
            int dirtyIndex = 0;
            foreach (int tileIndex in dirty) scheduledIndices[dirtyIndex++] = tileIndex;
            dirty.Clear();

            var job = new ClimateTileJob {
                inputs = scheduledInputs,
                indices = scheduledIndices,
                output = scheduledOutput,
                maxLatitude = seaLand.planetMaxLatitude,
                thermalEquator = climate.thermalEquatorLatitude,
                subtropicalBoundary = seaLand.planetMaxLatitude / CellCount(climate.circulationMode) * climate.heatTransport,
                subpolarBoundary = Mathf.Min(seaLand.planetMaxLatitude, seaLand.planetMaxLatitude / CellCount(climate.circulationMode) * climate.heatTransport * 2.25f),
                irradiance = climate.stellarIrradiance,
                albedo = climate.albedo,
                greenhouseWarming = climate.greenhouseWarming,
                lapseRatePerKm = climate.lapseRatePerKm,
                heatTransport = climate.heatTransport,
                monsoonStrength = climate.monsoonStrength
            };
            pendingJob = job.Schedule(count, 32);
            hasPendingJob = true;
        }

        private void CompletePendingJob()
        {
            if (!hasPendingJob || !pendingJob.IsCompleted) return;
            pendingJob.Complete();
            for (int i = 0; i < scheduledIndices.Length; i++) simulator.Climates[scheduledIndices[i]] = scheduledOutput[i];
            DisposeNativeArrays();
            hasPendingJob = false;
        }

        private void DisposeNativeArrays()
        {
            if (scheduledInputs.IsCreated) scheduledInputs.Dispose();
            if (scheduledOutput.IsCreated) scheduledOutput.Dispose();
            if (scheduledIndices.IsCreated) scheduledIndices.Dispose();
        }

        private static int CellCount(CirculationMode mode) => mode == CirculationMode.SingleCell ? 1 : mode == CirculationMode.DoubleCell ? 2 : 3;
    }

    [Serializable]
    public struct TileNeighbourList { public int[] indices; }

    internal struct ClimateTileJob : IJobParallelFor
    {
        [ReadOnly] public NativeArray<TileTerrainInput> inputs;
        [ReadOnly] public NativeArray<int> indices;
        public NativeArray<TileClimateData> output;
        public float maxLatitude, thermalEquator, subtropicalBoundary, subpolarBoundary;
        public float irradiance, albedo, greenhouseWarming, lapseRatePerKm, heatTransport, monsoonStrength;

        public void Execute(int workIndex)
        {
            TileTerrainInput tile = inputs[indices[workIndex]];
            float equatorialC = Mathf.Pow(irradiance * (1f - albedo) / (4f * 5.670374419e-8f), .25f) - 273.15f + greenhouseWarming;
            float latitudeDistance = Mathf.Abs(tile.latitude - thermalEquator);
            float latitudeFactor = 1f - Mathf.Clamp01(latitudeDistance / maxLatitude);
            float temperature = equatorialC * latitudeFactor + (1f - latitudeFactor) * equatorialC * .28f * heatTransport - tile.elevation01 * 4f * lapseRatePerKm + tile.regionalTemperatureOffset;
            temperature = Mathf.Lerp(temperature, temperature * .92f, tile.waterAdjacentWeight);
            float diurnal = (tile.isLand ? 18f : 6f) * (1f - tile.waterAdjacentWeight) * (tile.slopeDegrees > 30f ? 1.5f : 1f);
            float rainFactor = latitudeDistance < subtropicalBoundary * .5f ? 1.4f : latitudeDistance < subtropicalBoundary ? .3f : latitudeDistance < subpolarBoundary ? 1.1f : .2f;
            if (tile.isLand && tile.isCoast) rainFactor += monsoonStrength * .5f;
            rainFactor *= tile.windwardFactor > 0f ? 1.5f : tile.windwardFactor < 0f ? .4f : 1f;
            if (!tile.isLand) rainFactor *= 1.2f;
            float precipitation = Mathf.Max(0f, 1000f * rainFactor * (1f + tile.regionalHumidityOffsetPercent / 100f));
            float airHumidity = Mathf.Clamp(precipitation / 20f - diurnal * .8f + tile.waterAdjacentWeight * 30f, 0f, 100f);
            output[workIndex] = new TileClimateData {
                annualTemperatureC = temperature, diurnalTemperatureRangeC = diurnal, annualPrecipitationMm = precipitation,
                airHumidityPercent = airHumidity, soilHumidityPercent = Mathf.Clamp(airHumidity * (1f - tile.slopeDegrees / 90f * .4f), 0f, 100f),
                accumulatedTemperature = Mathf.Max(0f, temperature) * 120f, frostFreeDays = Mathf.Lerp(0f, 365f, Mathf.Clamp01((temperature + 5f) / 25f)),
                climateZone = ClimateClassification.Zone(tile, temperature, precipitation), biome = ClimateClassification.Biome(tile, temperature, precipitation)
            };
        }
    }

    internal static class ClimateClassification
    {
        public static ClimateZone Zone(TileTerrainInput tile, float temperature, float precipitation)
        {
            if (tile.elevation01 > .75f) return ClimateZone.HighlandAlpine;
            if (Mathf.Abs(tile.latitude) > 25f && precipitation < 350f && temperature >= 0f && temperature < 16f) return ClimateZone.InlandAridTemperate;
            if (temperature < -10f) return ClimateZone.PolarFrigid; if (temperature < 0f) return ClimateZone.Subarctic; if (temperature < 8f) return ClimateZone.TemperateCold;
            if (temperature < 16f) return ClimateZone.TemperateMild; if (temperature < 22f) return ClimateZone.TemperateWarm; if (temperature < 28f) return ClimateZone.Subtropical; return ClimateZone.Tropical;
        }
        public static BiomeType Biome(TileTerrainInput tile, float temperature, float precipitation)
        {
            if (!tile.isLand) return BiomeType.Wetland; if (tile.elevation01 > .75f) return BiomeType.Alpine; if (temperature < 0f) return BiomeType.IceSheet;
            if (temperature < 10f) return precipitation < 250f ? BiomeType.Tundra : BiomeType.BorealForest;
            if (temperature < 18f) return precipitation < 250f ? BiomeType.Desert : precipitation < 500f ? BiomeType.TemperateGrassland : BiomeType.TemperateForest;
            return precipitation < 250f ? BiomeType.Desert : precipitation < 700f ? BiomeType.Savanna : precipitation < 1500f ? BiomeType.TropicalMonsoon : BiomeType.TropicalRainforest;
        }
    }
}
