using CivilizationEvolution.Simulation.Generation;
using CivilizationEvolution.Simulation.Settlement;
using CivilizationEvolution.World.Generation;
using CivilizationEvolution.World.Hydrology;
using CivilizationEvolution.World.Settlement;
using CivilizationEvolution.World.Terrain;



namespace CivilizationEvolution.Simulation.AI
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
