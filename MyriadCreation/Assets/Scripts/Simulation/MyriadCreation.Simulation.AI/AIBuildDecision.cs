using MyriadCreation.World.Settlement;

namespace MyriadCreation.Simulation.AI
{
public struct AIBuildDecision
{
	public bool shouldBuild;

	public float priority;

	public string reason;

	public PortTier targetPortTier;
}
}
