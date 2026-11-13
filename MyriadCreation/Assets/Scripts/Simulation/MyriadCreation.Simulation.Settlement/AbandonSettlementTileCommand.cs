using MyriadCreation.Core.Contracts;
using MyriadCreation.Simulation.Systems;

namespace MyriadCreation.Simulation.Settlement
{
public readonly struct AbandonSettlementTileCommand(int tileIndex, AbandonmentType type, int realmId) : ISimulationCommand
{
	public readonly int TileIndex = tileIndex;

	public readonly AbandonmentType Type = type;

	public readonly int RealmId = realmId;
}
}
