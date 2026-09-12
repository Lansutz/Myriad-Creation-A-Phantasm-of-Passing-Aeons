using System.Collections.Generic;

namespace CivilizationEvolution.Building
{
    [System.Serializable]
    public struct BuildingDef
    {
        public int buildingId;
        public string buildingName;
        public BuildingCategory category;
        public int tier;
        public float buildCost;
        [System.NonSerialized]
        public Dictionary<int, float> materialCost;
        public int buildDays;
        public float maintenanceCost;
    }
}
