using System;
using MyriadCreation.Content.Definitions;

namespace MyriadCreation.Simulation.State
{
[Serializable]
public class Alliance
{
	public AllianceType type;

	public int realmAId;

	public int realmBId;

	public int signedDay;

	public int durationDays;

	public bool isActive = true;

	public float tradeEfficiencyBonus;

	public float tariffReduction;

	public bool mutualDefense;

	public bool jointOffensive;

	public bool militaryAccess;

	public float relationRequirement;

	public bool IsExpired(int currentDay)
	{
		if (durationDays > 0)
		{
			return currentDay - signedDay > durationDays;
		}
		return false;
	}

	public bool CheckConditions(DiplomaticRelation relation)
	{
		if (relation.relation >= relationRequirement)
		{
			return !relation.isAtWar;
		}
		return false;
	}
}
}
