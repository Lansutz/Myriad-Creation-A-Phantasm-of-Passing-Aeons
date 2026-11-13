using System;
using System.Collections.Generic;
using MyriadCreation.Core.Contracts;
using MyriadCreation.Simulation.Planning;
using MyriadCreation.Content.Definitions;

namespace MyriadCreation.Simulation.Systems
{
public sealed class PlanSystem
{
	private readonly Dictionary<int, Plan> _plans = new Dictionary<int, Plan>();

	private readonly Dictionary<PlanType, IPlanExecutor> _executors = new Dictionary<PlanType, IPlanExecutor>();

	private readonly List<int> _activePlanIds = new List<int>();

	private readonly List<int> _scratchPlanIds = new List<int>();

	private int _nextPlanId = 1;

	private int _currentDay;

	public int CurrentDay => _currentDay;

	public int PlanCount => _plans.Count;

	public int ActivePlanCount => _activePlanIds.Count;

	public event Action<Plan> PlanCreated;

	public event Action<Plan, PlanState, PlanState> PlanStateChanged;

	public event Action<Plan> PlanCompleted;

	public event Action<Plan> PlanFailed;

	public event Action<Plan> PlanCancelled;

	public void RegisterExecutor(IPlanExecutor executor)
	{
		if (executor == null)
		{
			throw new ArgumentNullException("executor");
		}
		_executors[executor.Type] = executor;
	}

	public bool UnregisterExecutor(PlanType type)
	{
		return _executors.Remove(type);
	}

	public Plan CreatePlan(PlanType type, int initiatorId, int targetId, int day, string title = null, string description = null, float estimatedDays = 0f, string purpose = null, string targetKind = null)
	{
		Plan plan = new Plan(_nextPlanId++, type, initiatorId, targetId, day)
		{
			title = (title ?? string.Empty),
			description = (description ?? string.Empty),
			purpose = (purpose ?? string.Empty),
			targetKind = (targetKind ?? string.Empty),
			estimatedDays = Math.Max(0f, estimatedDays)
		};
		_plans.Add(plan.planId, plan);
		PlanCreated?.Invoke(plan);
		return plan;
	}

	public bool TryGetPlan(int planId, out Plan plan)
	{
		return _plans.TryGetValue(planId, out plan);
	}

	public Plan GetPlan(int planId)
	{
		_plans.TryGetValue(planId, out var value);
		return value;
	}

	public bool TrySetPurpose(int planId, string purpose)
	{
		if (!_plans.TryGetValue(planId, out var value) || value.IsTerminal)
		{
			return false;
		}
		value.purpose = purpose ?? string.Empty;
		return true;
	}

	public bool TrySetActivity(int planId, string activity)
	{
		if (!_plans.TryGetValue(planId, out var value) || value.IsTerminal)
		{
			return false;
		}
		value.currentActivity = activity ?? string.Empty;
		value.activity.Set(value.currentActivity, value.currentActivity, "executing");
		value.isWaiting = false;
		value.waitCondition = default;
		return true;
	}

	public bool TrySetActivity(int planId, string activityId, string definitionId, string stateCode)
	{
		if (!_plans.TryGetValue(planId, out var value) || value.IsTerminal)
		{
			return false;
		}
		value.activity.Set(activityId, definitionId, stateCode);
		value.currentActivity = activityId ?? string.Empty;
		return true;
	}

	public bool TrySetWait(int planId, SimulationWaitCondition condition)
	{
		if (!_plans.TryGetValue(planId, out var value) || value.IsTerminal || value.state != PlanState.Executing)
		{
			return false;
		}
		if (!condition.IsValid)
		{
			return false;
		}
		value.waitCondition = condition;
		value.isWaiting = true;
		value.activity.stateCode = "waiting";
		return true;
	}

	public bool TrySetResult(int planId, string resultCode, string resultSummary = null)
	{
		if (!_plans.TryGetValue(planId, out var value))
		{
			return false;
		}
		value.resultCode = resultCode ?? string.Empty;
		value.resultSummary = resultSummary ?? string.Empty;
		return true;
	}

	public bool AcceptPlan(int planId)
	{
		return ChangeState(planId, PlanState.Accepted);
	}

	public bool BeginPreparation(int planId)
	{
		if (!TryGetPlan(planId, out var plan))
		{
			return false;
		}
		if (plan.state != PlanState.Accepted && plan.state != PlanState.Paused)
		{
			return false;
		}
		plan.phase = PlanPhase.Preparation;
		return ChangeState(planId, PlanState.Preparing);
	}

	public bool BeginExecution(int planId)
	{
		if (!TryGetPlan(planId, out var plan))
		{
			return false;
		}
		if (plan.state != PlanState.Preparing && plan.state != PlanState.Paused)
		{
			return false;
		}
		plan.phase = PlanPhase.Execution;
		return ChangeState(planId, PlanState.Executing);
	}

	public bool TryResumeWaitingPlan(int planId)
	{
		if (!_plans.TryGetValue(planId, out var value) || value.IsTerminal)
		{
			return false;
		}
		if (value.state != PlanState.Executing || !value.isWaiting)
		{
			return false;
		}
		value.isWaiting = false;
		value.waitCondition = default;
		value.activity.stateCode = "executing";
		return true;
	}

	public bool PausePlan(int planId)
	{
		if (!TryGetPlan(planId, out var plan))
		{
			return false;
		}
		if (plan.state != PlanState.Preparing && plan.state != PlanState.Executing)
		{
			return false;
		}
		return ChangeState(planId, PlanState.Paused);
	}

	public bool CancelPlan(int planId, string resultCode = "cancelled", string resultSummary = null)
	{
		if (!TryGetPlan(planId, out var plan) || plan.IsTerminal)
		{
			return false;
		}
		plan.resultCode = resultCode ?? string.Empty;
		plan.resultSummary = resultSummary ?? string.Empty;
		return ChangeState(planId, PlanState.Cancelled);
	}

	public bool FailPlan(int planId, string resultCode = "failed", string resultSummary = null)
	{
		if (!TryGetPlan(planId, out var plan) || plan.IsTerminal)
		{
			return false;
		}
		plan.resultCode = resultCode ?? string.Empty;
		plan.resultSummary = resultSummary ?? string.Empty;
		return ChangeState(planId, PlanState.Failed);
	}

	public bool CompletePlan(int planId, string resultCode = "completed", string resultSummary = null)
	{
		if (!TryGetPlan(planId, out var plan) || plan.IsTerminal)
		{
			return false;
		}
		plan.progress = 1f;
		plan.phase = PlanPhase.Resolution;
		plan.resultCode = resultCode ?? string.Empty;
		plan.resultSummary = resultSummary ?? string.Empty;
		return ChangeState(planId, PlanState.Completed);
	}

	public void DailyTick(int currentDay, float deltaDays = 1f)
	{
		_currentDay = currentDay;
		if (deltaDays <= 0f || _activePlanIds.Count == 0)
		{
			return;
		}
		_scratchPlanIds.Clear();
		_scratchPlanIds.AddRange(_activePlanIds);
		for (int i = 0; i < _scratchPlanIds.Count; i++)
		{
			int key = _scratchPlanIds[i];
			if (!_plans.TryGetValue(key, out var value) || value.IsTerminal || value.state != PlanState.Executing || value.isWaiting)
			{
				continue;
			}
			value.elapsedDays += deltaDays;
			if (_executors.TryGetValue(value.type, out var value2) && value2 != null)
			{
				PlanExecutionResult planExecutionResult = value2.Execute(value, deltaDays);
				if (!string.IsNullOrEmpty(planExecutionResult.currentActivity))
				{
					value.currentActivity = planExecutionResult.currentActivity;
					value.activity.Set(planExecutionResult.currentActivity, planExecutionResult.currentActivity, "executing");
				}
				if (planExecutionResult.outcome == PlanExecutionOutcome.Wait && planExecutionResult.hasWaitCondition)
				{
					value.waitCondition = planExecutionResult.waitCondition;
					value.isWaiting = true;
					value.activity.stateCode = "waiting";
				}
				else if (planExecutionResult.outcome != PlanExecutionOutcome.Wait)
				{
					value.isWaiting = false;
					value.waitCondition = default;
				}
				if (!string.IsNullOrEmpty(planExecutionResult.resultSummary))
				{
					value.resultSummary = planExecutionResult.resultSummary;
				}
				float num = planExecutionResult.progressDelta;
				if (float.IsNaN(num) || float.IsInfinity(num))
				{
					num = 0f;
				}
				if (num > 0f)
				{
					value.progress = Clamp01(value.progress + num);
				}
				switch (planExecutionResult.outcome)
				{
				case PlanExecutionOutcome.Complete:
					CompletePlan(value.planId, string.IsNullOrEmpty(planExecutionResult.resultCode) ? "executor_completed" : planExecutionResult.resultCode, planExecutionResult.resultSummary);
					break;
				case PlanExecutionOutcome.Fail:
					FailPlan(value.planId, string.IsNullOrEmpty(planExecutionResult.resultCode) ? "executor_failed" : planExecutionResult.resultCode, planExecutionResult.resultSummary);
					break;
				case PlanExecutionOutcome.Cancel:
					CancelPlan(value.planId, string.IsNullOrEmpty(planExecutionResult.resultCode) ? "executor_cancelled" : planExecutionResult.resultCode, planExecutionResult.resultSummary);
					break;
				}
				if (value.progress >= 1f && !value.IsTerminal)
				{
					CompletePlan(value.planId, "executor_completed");
				}
			}
		}
	}

	public List<Plan> GetPlans(PlanType? type = null, PlanState? state = null)
	{
		List<Plan> list = new List<Plan>();
		foreach (KeyValuePair<int, Plan> plan in _plans)
		{
			Plan value = plan.Value;
			if ((!type.HasValue || value.type == type.Value) && (!state.HasValue || value.state == state.Value))
			{
				list.Add(value);
			}
		}
		return list;
	}

	public bool RemoveCompletedPlan(int planId)
	{
		if (!_plans.TryGetValue(planId, out var value) || !value.IsTerminal)
		{
			return false;
		}
		RemoveFromActive(planId);
		return _plans.Remove(planId);
	}

	private bool ChangeState(int planId, PlanState newState)
	{
		if (!_plans.TryGetValue(planId, out var value))
		{
			return false;
		}
		if (value.state == newState || value.IsTerminal)
		{
			return false;
		}
		PlanState state = value.state;
		if (!IsValidTransition(state, newState))
		{
			return false;
		}
		value.state = newState;
		value.lastStateChangeDay = _currentDay;
		switch (newState)
		{
		case PlanState.Preparing:
		case PlanState.Executing:
			AddToActive(planId);
			break;
		case PlanState.Completed:
		case PlanState.Failed:
		case PlanState.Cancelled:
			RemoveFromActive(planId);
			break;
		}
		PlanStateChanged?.Invoke(value, state, newState);
		switch (newState)
		{
		case PlanState.Completed:
			PlanCompleted?.Invoke(value);
			break;
		case PlanState.Failed:
			PlanFailed?.Invoke(value);
			break;
		case PlanState.Cancelled:
			PlanCancelled?.Invoke(value);
			break;
		}
		return true;
	}

	private static bool IsValidTransition(PlanState from, PlanState to)
	{
		switch (from)
		{
		case PlanState.Proposed:
			if (to != PlanState.Accepted && to != PlanState.Cancelled)
			{
				return to == PlanState.Failed;
			}
			return true;
		case PlanState.Accepted:
			if (to != PlanState.Preparing && to != PlanState.Paused && to != PlanState.Cancelled)
			{
				return to == PlanState.Failed;
			}
			return true;
		case PlanState.Preparing:
			if (to != PlanState.Executing && to != PlanState.Paused && to != PlanState.Cancelled)
			{
				return to == PlanState.Failed;
			}
			return true;
		case PlanState.Executing:
			if (to != PlanState.Paused && to != PlanState.Completed && to != PlanState.Failed)
			{
				return to == PlanState.Cancelled;
			}
			return true;
		case PlanState.Paused:
			if (to != PlanState.Preparing && to != PlanState.Executing && to != PlanState.Cancelled)
			{
				return to == PlanState.Failed;
			}
			return true;
		default:
			return false;
		}
	}

	private void AddToActive(int planId)
	{
		if (!_activePlanIds.Contains(planId))
		{
			_activePlanIds.Add(planId);
		}
	}

	private void RemoveFromActive(int planId)
	{
		_activePlanIds.Remove(planId);
	}

	private static float Clamp01(float value)
	{
		if (value <= 0f)
		{
			return 0f;
		}
		if (value >= 1f)
		{
			return 1f;
		}
		return value;
	}
}
}
