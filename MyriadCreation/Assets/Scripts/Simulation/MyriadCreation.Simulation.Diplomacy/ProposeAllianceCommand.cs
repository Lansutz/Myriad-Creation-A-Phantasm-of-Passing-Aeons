using MyriadCreation.Core.Contracts;
using MyriadCreation.Content.Definitions;

namespace MyriadCreation.Simulation.Diplomacy
{
public readonly struct ProposeAllianceCommand(int realmAId, int realmBId, AllianceType type) : ISimulationCommand
{
	public readonly int RealmAId = realmAId;

	public readonly int RealmBId = realmBId;

	public readonly AllianceType Type = type;
}
}
