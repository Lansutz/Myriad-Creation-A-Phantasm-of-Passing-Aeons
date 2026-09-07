namespace CivilizationEvolution.Map
{
    public struct AIBuildDecision
    {
        public bool shouldBuild;
        public float priority;
        public string reason;
        public FortSubtype fortSubtype;
        public PortTier targetPortTier;
    }
}
