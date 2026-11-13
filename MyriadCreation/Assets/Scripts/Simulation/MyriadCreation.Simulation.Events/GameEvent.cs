using MyriadCreation.Content.Definitions;

namespace MyriadCreation.Simulation.Events
{
public struct GameEvent
{
	public GameEventType eventType;

	public int tileIndex;

	public int realmId;

	public float severity;

	public string description;
}
}
