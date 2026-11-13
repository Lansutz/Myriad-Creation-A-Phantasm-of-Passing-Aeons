using MyriadCreation.Core.Contracts;

namespace MyriadCreation.Simulation.Diplomacy
{
public readonly struct SendGiftCommand(int fromRealmId, int toRealmId, float amount) : ISimulationCommand
{
	public readonly int FromRealmId = fromRealmId;

	public readonly int ToRealmId = toRealmId;

	public readonly float Amount = amount;
}
}
