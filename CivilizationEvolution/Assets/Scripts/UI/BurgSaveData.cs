using System;

namespace CivilizationEvolution.UI
{
    [Serializable]
    public class BurgSaveData
    {
        public int burgId;
        public string burgName;
        public int type;
        public int provinceId;
        public int tileIndex;
        public float x;
        public float y;
        public float population;
        public float development;
        public float wealth;
        public float tradePower;
        public float fortification;
        public int garrison;
        public bool isCapital;
        public bool isPort;
        public bool isCoastal;
        public bool hasMarket;
        public bool hasTemple;
        public bool hasUniversity;
        public int buildLevel;
    }
}
