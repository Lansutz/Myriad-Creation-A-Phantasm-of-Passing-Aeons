using System;
using System.Collections.Generic;
using MyriadCreation.Core.Enums;

namespace MyriadCreation.Simulation.State
{
[Serializable]
public class Treaty
{
	public int treatyId;

	public string treatyName;

	public int signerAId;

	public int signerBId;

	public int signedDay;

	public int expiryDay = -1;

	public List<TreatyClause> clauses = new List<TreatyClause>();

	public bool isActive = true;

	public float warScoreAtSigning;

	public List<GameEnums.WarGoalType> originalWarGoals = new List<GameEnums.WarGoalType>();

	public bool goalsFullyAchieved;

	public int truceUntilDay;

	public bool isPeaceTreaty;

	public bool IsExpired(int currentDay)
	{
		if (expiryDay > 0)
		{
			return currentDay > expiryDay;
		}
		return false;
	}
}
}
