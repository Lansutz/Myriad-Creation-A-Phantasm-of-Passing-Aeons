using System;
using MyriadCreation.Core.Contracts;

namespace MyriadCreation.Simulation.Planning
{
[Serializable]
public readonly struct PlanExecutionResult(PlanExecutionOutcome outcome, float progressDelta = 0f, string currentActivity = null, string resultCode = null, string resultSummary = null, SimulationWaitCondition waitCondition = default(SimulationWaitCondition), bool hasWaitCondition = false)
{
	public readonly PlanExecutionOutcome outcome = outcome;

	public readonly float progressDelta = progressDelta;

	public readonly string currentActivity = currentActivity ?? string.Empty;

	public readonly string resultCode = resultCode ?? string.Empty;

	public readonly string resultSummary = resultSummary ?? string.Empty;

	public readonly SimulationWaitCondition waitCondition = waitCondition;

	public readonly bool hasWaitCondition = hasWaitCondition;

	public static PlanExecutionResult Continue(float progressDelta = 0f, string currentActivity = null)
	{
		return new PlanExecutionResult(PlanExecutionOutcome.Continue, progressDelta, currentActivity);
	}

	public static PlanExecutionResult Complete(string resultCode = "completed", string currentActivity = null, string resultSummary = null)
	{
		return new PlanExecutionResult(PlanExecutionOutcome.Complete, 0f, currentActivity, resultCode, resultSummary);
	}

	public static PlanExecutionResult Wait(string conditionId, string description = null, bool pollOnSchedule = true, string currentActivity = null)
	{
		return new PlanExecutionResult(PlanExecutionOutcome.Wait, 0f, currentActivity, "waiting", description, new SimulationWaitCondition(conditionId, description, pollOnSchedule), hasWaitCondition: true);
	}

	public static PlanExecutionResult Fail(string resultCode = "failed", string resultSummary = null)
	{
		return new PlanExecutionResult(PlanExecutionOutcome.Fail, 0f, null, resultCode, resultSummary);
	}

	public static PlanExecutionResult Cancel(string resultCode = "cancelled", string resultSummary = null)
	{
		return new PlanExecutionResult(PlanExecutionOutcome.Cancel, 0f, null, resultCode, resultSummary);
	}
}
}
