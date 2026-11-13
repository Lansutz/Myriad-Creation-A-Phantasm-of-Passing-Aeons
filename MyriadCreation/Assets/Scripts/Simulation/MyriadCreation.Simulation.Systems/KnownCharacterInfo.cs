namespace MyriadCreation.Simulation.Systems
{
public struct KnownCharacterInfo
{
	public KnownCharacterInfo()
	{
		observerRealmId = 0;
		characterId = 0;
		characterRealmId = -1;
		knownSettlementAnchorId = -1;
		lastSeenDay = 0;
		source = "";
	}

	public int observerRealmId;

	public int characterId;

	/// <summary>被观察者当时的所属政权（-1=未知/旧档）；用于判断"是否仍在观察者视野内"。</summary>
	public int characterRealmId = -1;

	public int knownSettlementAnchorId;

	public int lastSeenDay;

	public string source;
}
}
