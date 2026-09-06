using System;

namespace CivilizationEvolution.UI
{
    [Serializable]
    public struct TileSaveData
    {
        public bool exists;
        public bool isLand;
        public float elevation01;
        public float slopeDegree;
        public float annualTemp;
        public float annualPrecipMm;
        public float airHumidityPct;
        public int biome;
        public int climateZone;
        public float fertility;
        public int provinceId;
        public int ownerRealmId;
        public int occupyingRealmId;
        public bool isCoast;
        public bool isRiver;
        public int seaConnectId;
        public int oceanTier;
        public int roadLevel;
        public float development;
        public float stability;
        public float order;
    }
}
