using System.Collections.Generic;

namespace MyriadCreation.Simulation.State
{
    [System.Serializable]
    public class ActiveDisaster
    {
        public DisasterDef def;
        public int centerTile;
        public List<int> affectedTiles = new List<int>();
        public int startDay;
        public int startYear;
        public int remainingDays;
        public float severity;
    }
}
