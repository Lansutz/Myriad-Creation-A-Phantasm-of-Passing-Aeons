using MyriadCreation.Core.Contracts;

namespace MyriadCreation.Simulation.Settlement
{
public readonly struct ResettleTileCommand(int tileIndex, int realmId, int settlerCount) : ISimulationCommand
{
	public readonly int TileIndex = tileIndex;

	public readonly int RealmId = realmId;

	public readonly int SettlerCount = settlerCount;
}
}
