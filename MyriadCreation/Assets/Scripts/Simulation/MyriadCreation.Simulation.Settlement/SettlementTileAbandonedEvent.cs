using MyriadCreation.Simulation.Systems;

namespace MyriadCreation.Simulation.Settlement
{
public readonly struct SettlementTileAbandonedEvent(int tileIndex, AbandonmentType type, int previousRealmId, int realmId)
{
	public readonly int TileIndex = tileIndex;

	public readonly AbandonmentType Type = type;

	public readonly int PreviousRealmId = previousRealmId;

	public readonly int RealmId = realmId;
}
}
