namespace MyriadCreation.World.Settlement
{
public readonly struct SettlementEconomySnapshot(bool hasMarket, float marketSize, float warehouseCapacity, int productionSlots, float tradeConnectivity)
{
	public readonly bool hasMarket = hasMarket;

	public readonly float marketSize = marketSize;

	public readonly float warehouseCapacity = warehouseCapacity;

	public readonly int productionSlots = productionSlots;

	public readonly float tradeConnectivity = tradeConnectivity;
}
}
