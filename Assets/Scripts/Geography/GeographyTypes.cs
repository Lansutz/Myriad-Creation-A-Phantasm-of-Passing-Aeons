using System;
using System.Collections.Generic;
using UnityEngine;

namespace Myriad.Geography
{
    public enum RoadLevel { None, Dirt, Official, Imperial }
    public enum SeaTier { Land, CoastalSea, InlandSea, OpenOcean }
    public enum WaterBodyKind { None, Ocean, LargeInlandLake, SmallInlandLake, River }
    public enum RiverRank { None, MountainStream, SecondaryTributary, PrimaryTributary, MainRiver }
    public enum LandformTag { Plain, Hill, Mountain, Forest, RiverCrossing, GateFortress, Desert, SnowOrTundra, Depression, RockHill, SaltLake, Volcanic }
    public enum ClimateZone { PolarFrigid, Subarctic, TemperateCold, TemperateMild, TemperateWarm, Subtropical, Tropical, HighlandAlpine, InlandAridTemperate }
    public enum BiomeType { IceSheet, Tundra, BorealForest, TemperateForest, TemperateGrassland, Desert, Steppe, Savanna, TropicalRainforest, TropicalMonsoon, Alpine, Wetland, Volcanic, SaltLake }
    public enum CirculationMode { SingleCell, DoubleCell, TripleCell }

    [Serializable]
    public struct RoadEffects
    {
        public float movementMultiplier;
        public float supplyMultiplier;
        public float tradeMultiplier;

        public static RoadEffects For(RoadLevel level)
        {
            switch (level)
            {
                case RoadLevel.Dirt: return new RoadEffects { movementMultiplier = .85f, supplyMultiplier = .8f, tradeMultiplier = .75f };
                case RoadLevel.Official: return new RoadEffects { movementMultiplier = .65f, supplyMultiplier = .6f, tradeMultiplier = .5f };
                case RoadLevel.Imperial: return new RoadEffects { movementMultiplier = .45f, supplyMultiplier = .4f, tradeMultiplier = .3f };
                default: return new RoadEffects { movementMultiplier = 1f, supplyMultiplier = 1f, tradeMultiplier = 1f };
            }
        }
    }

    [Serializable]
    public struct TileGeography
    {
        [Tooltip("Vertices deliberately describe a free-form polygon; this game does not use a hex grid.")]
        public Vector2[] polygon;
        public int legalSovereignId;
        public int temporaryOccupierId;
        public int provinceId;
        public bool isGate;
        [Range(0f, 1f)] public float stability;
        public bool isAtWar;
        [Min(0f)] public float development;
        public RoadLevel roadLevel;
        public SeaTier seaTier;
        public int seaConnectId;
        public WaterBodyKind waterBody;
        public RiverRank riverRank;
        public bool isSaltWater;
        public float elevationMeters;
        [Range(0f, 90f)] public float slopeDegrees;
        public float baseFertility;
        public LandformTag[] landforms;
        public ClimateZone climateZone;
        public BiomeType biome;

        public readonly float GateDefenseBonus => isGate ? .25f : 0f;
        public readonly RoadEffects GetRoadEffects() => RoadEffects.For(roadLevel);
        public readonly float VisualDesaturation => Mathf.Lerp(.05f, .65f, 1f - stability) + (isAtWar ? .15f : 0f);
        public readonly bool IsOccupied => temporaryOccupierId >= 0 && temporaryOccupierId != legalSovereignId;
    }

    public static class GeographyRules
    {
        public static float MovementLoss(TileGeography tile, bool attacking)
        {
            float slopeLoss = 1f + Mathf.InverseLerp(0f, 55f, tile.slopeDegrees) * (attacking ? .7f : .35f);
            return slopeLoss * tile.GetRoadEffects().movementMultiplier;
        }

        public static float DefenderMoraleBonus(TileGeography tile) => Mathf.InverseLerp(12f, 55f, tile.slopeDegrees) * .2f + tile.GateDefenseBonus;

        public static bool CanFreelySail(TileGeography a, TileGeography b) =>
            a.seaConnectId >= 0 && a.seaConnectId == b.seaConnectId && a.isSaltWater && b.isSaltWater;

        public static bool CanMigrate(TileGeography from, TileGeography to, bool borderBlocked) =>
            !borderBlocked && from.waterBody == WaterBodyKind.None && to.waterBody == WaterBodyKind.None;
    }
}
