using CivilizationEvolution.Map;

namespace CivilizationEvolution.AI
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
