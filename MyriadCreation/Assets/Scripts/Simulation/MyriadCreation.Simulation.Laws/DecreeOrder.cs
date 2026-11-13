using System;

namespace MyriadCreation.Simulation.Laws
{
[Serializable]
public class DecreeOrder
{
	public int orderId;

	public string decreeId = "";

	public int realmId;

	public int fromDivisionId;

	public int toDivisionId;

	public int fromAnchorId = -1;

	public int targetAnchorId = -1;

	public int targetTile = -1;

	public int officialCharacterId = -1;

	public int courierActorId = -1;

	public float executionStrength;

	public bool nextHopIssued;

	public int issuedDay;

	public int totalTravelDays = 1;

	public float progressDays;

	public float compliance;

	public DecreeOrderState state;

	public string note = "";
}
}
