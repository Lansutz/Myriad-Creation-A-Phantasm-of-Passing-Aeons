namespace CivilizationEvolution.Simulation.Disaster
{
    [System.Serializable]
    public struct DisasterDef
    {
        public DisasterType type;
        public string name;
        public DisasterCategory category;
        public float baseFrequency;
        public int minDuration;
        public int maxDuration;
        public float baseSeverity;
    }
}
