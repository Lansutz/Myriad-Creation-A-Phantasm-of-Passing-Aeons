using System;

namespace MyriadCreation.Simulation.Systems
{
[Serializable]
public class StampedeEvent
{
	public StampedeType type;

	public int tileIndex;

	public int realmId;

	public int armyId = -1;

	public int anchorId = -1;

	public float severity;

	public int casualties;

	public int durationDays;

	public int startDay;

	public string description;
}
}
