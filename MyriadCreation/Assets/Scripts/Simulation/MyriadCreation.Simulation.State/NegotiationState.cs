using System;
using System.Collections.Generic;
using UnityEngine;

namespace MyriadCreation.Simulation.State
{
[Serializable]
public class NegotiationState
{
	public int attackerId;

	public int defenderId;

	public float attackerWarScore;

	public float defenderWarScore;

	public List<NegotiationClause> attackerDemands = new List<NegotiationClause>();

	public List<NegotiationClause> attackerConcessions = new List<NegotiationClause>();

	public int AttackerTotalCost
	{
		get
		{
			int num = 0;
			foreach (NegotiationClause attackerDemand in attackerDemands)
			{
				num += attackerDemand.cost;
			}
			foreach (NegotiationClause attackerConcession in attackerConcessions)
			{
				num -= attackerConcession.cost;
			}
			return num;
		}
	}

	public bool IsAttackerWithinLimit => AttackerTotalCost <= Mathf.CeilToInt(attackerWarScore);
}
}
