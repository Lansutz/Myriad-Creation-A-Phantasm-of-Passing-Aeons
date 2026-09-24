namespace MyriadCreation.Simulation.Definitions
{
    [System.Serializable]
    public struct CrimeDefinition
    {
        public CrimeType type;
        public string name;
        public float baseSeverity;
        public PunishmentType defaultPunishment;
    }
}
