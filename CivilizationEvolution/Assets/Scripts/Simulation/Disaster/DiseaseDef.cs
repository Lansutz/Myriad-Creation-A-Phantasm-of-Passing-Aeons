namespace CivilizationEvolution.Disaster
{
    [System.Serializable]
    public struct DiseaseDef
    {
        public DiseaseType type;
        public string name;
        public float baseInfectionRate;
        public float baseMortalityRate;
        public float baseRecoveryRate;
        public int incubationDays;
        public int durationDays;
        public float baseR0;
        public bool isEndemic;
    }
}
