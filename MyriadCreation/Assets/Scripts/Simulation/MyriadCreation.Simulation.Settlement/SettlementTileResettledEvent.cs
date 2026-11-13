namespace MyriadCreation.Simulation.Settlement
{
public readonly struct SettlementTileResettledEvent(int tileIndex, int realmId, int settlerCount)
{
	public readonly int TileIndex = tileIndex;

	public readonly int RealmId = realmId;

	public readonly int SettlerCount = settlerCount;
}
}
