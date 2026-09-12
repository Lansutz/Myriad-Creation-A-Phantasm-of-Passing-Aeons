namespace CivilizationEvolution.Simulation.Society
{
    [System.Serializable]
    public class ActiveBuilding
    {
        public int buildingId;
        public int tileIndex;
        public int realmId;
        public int constructionDays;
        public int remainingDays;
        public bool isComplete;
    }
}
