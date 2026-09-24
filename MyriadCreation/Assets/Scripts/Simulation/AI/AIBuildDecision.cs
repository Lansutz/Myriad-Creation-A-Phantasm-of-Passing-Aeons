using MyriadCreation.Simulation.Generation;
using MyriadCreation.Simulation.Settlement;
using MyriadCreation.World.Generation;
using MyriadCreation.World.Hydrology;
using MyriadCreation.World.Settlement;
using MyriadCreation.World.Terrain;



namespace MyriadCreation.Simulation.AI
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
