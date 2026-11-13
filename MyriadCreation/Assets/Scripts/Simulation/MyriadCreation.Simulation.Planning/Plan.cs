using System;
using System.Collections.Generic;
using MyriadCreation.Core.Contracts;

namespace MyriadCreation.Simulation.Planning
{
[Serializable]
public sealed class Plan
{
	public int planId;

	public PlanType type;

	public PlanState state;

	public PlanPhase phase;

	public int initiatorId = -1;

	public int ownerId = -1;

	public int targetId = -1;

	public string targetKind = string.Empty;

	public string purpose = string.Empty;

	public string title = string.Empty;

	public string description = string.Empty;

	public float progress;

	public float elapsedDays;

	public float estimatedDays;

	public int createdDay;

	public int lastStateChangeDay;

	public readonly List<PlanParticipant> participants = new List<PlanParticipant>();

	public readonly List<PlanRequirement> requirements = new List<PlanRequirement>();

	public string currentActivity = string.Empty;

	public PlanActivityRuntime activity = new PlanActivityRuntime();

	public SimulationWaitCondition waitCondition;

	public bool isWaiting;

	public string resultCode = string.Empty;

	public string resultSummary = string.Empty;

	public bool IsTerminal
	{
		get
		{
			if (state != PlanState.Completed && state != PlanState.Failed)
			{
				return state == PlanState.Cancelled;
			}
			return true;
		}
	}

	public Plan(int planId, PlanType type, int initiatorId, int targetId, int createdDay)
	{
		this.planId = planId;
		this.type = type;
		state = PlanState.Proposed;
		phase = PlanPhase.Preparation;
		this.initiatorId = initiatorId;
		this.targetId = targetId;
		this.createdDay = createdDay;
		lastStateChangeDay = createdDay;
	}
}
}
