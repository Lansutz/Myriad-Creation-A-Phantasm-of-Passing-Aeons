using System.Collections.Generic;

namespace CivilizationEvolution.Simulation.Disaster
{
    [System.Serializable]
    public class ActiveDisease
    {
        public DiseaseDef def;
        public int centerTile;
        public List<int> affectedTiles = new List<int>();
        public int startDay;
        public int startYear;
        public int elapsedDays;
        public float activeInfections;
        public float totalInfected;
        public float totalDeaths;
        public float totalRecovered;
        public float currentR0;
    }
}
