using System;

namespace MyriadCreation.Simulation.State
{
[Serializable]
public class ActiveBuilding
{
	public int buildingId;

	public int tileIndex;

	public int realmId;

	public int constructionDays;

	public int remainingDays;

	public bool isComplete;

	public int constructionPlanId = -1;

	public int builderCharacterId = -1;

	public int innovationId = -1;

	public bool ManagedByPlan => constructionPlanId >= 0;
}
}
