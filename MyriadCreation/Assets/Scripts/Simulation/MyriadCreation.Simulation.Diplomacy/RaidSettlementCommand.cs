using MyriadCreation.Core.Contracts;
using MyriadCreation.Core.Enums;

namespace MyriadCreation.Simulation.Diplomacy
{
public readonly struct RaidSettlementCommand(int raiderRealmId, int targetRealmId, int targetTileIndex, GameEnums.RaidType raidType) : ISimulationCommand
{
	public readonly int RaiderRealmId = raiderRealmId;

	public readonly int TargetRealmId = targetRealmId;

	public readonly int TargetTileIndex = targetTileIndex;

	public readonly GameEnums.RaidType RaidType = raidType;
}
}
