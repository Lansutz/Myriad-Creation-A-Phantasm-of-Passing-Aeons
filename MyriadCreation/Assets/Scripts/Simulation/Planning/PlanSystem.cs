using System;
using System.Collections.Generic;

namespace CivilizationEvolution.Simulation.Planning
{
    /// <summary>
    /// 全局计划调度器。
    /// 统一管理“意图 → 准备 → 执行 → 验证/结算”的生命周期；领域知识由 IPlanExecutor 提供。
    /// </summary>
    public sealed class PlanSystem
    {
        private readonly Dictionary<int, Plan> _plans = new Dictionary<int, Plan>();
        private readonly Dictionary<PlanType, IPlanExecutor> _executors = new Dictionary<PlanType, IPlanExecutor>();
        private readonly List<int> _activePlanIds = new List<int>();
        private readonly List<int> _scratchPlanIds = new List<int>();
        private int _nextPlanId = 1;
        private int _currentDay;

        public event Action<Plan> PlanCreated;
        public event Action<Plan, PlanState, PlanState> PlanStateChanged;
        public event Action<Plan> PlanCompleted;
        public event Action<Plan> PlanFailed;
        public event Action<Plan> PlanCancelled;

        public int CurrentDay => _currentDay;
        public int PlanCount => _plans.Count;
        public int ActivePlanCount => _activePlanIds.Count;

        public void RegisterExecutor(IPlanExecutor executor)
        {
            if (executor == null) throw new ArgumentNullException(nameof(executor));
            _executors[executor.Type] = executor;
        }

        public bool UnregisterExecutor(PlanType type) => _executors.Remove(type);

        public Plan CreatePlan(PlanType type, int initiatorId, int targetId, int day,
            string title = null, string description = null, float estimatedDays = 0f,
            string purpose = null, string targetKind = null)
        {
            var plan = new Plan(_nextPlanId++, type, initiatorId, targetId, day)
            {
                title = title ?? string.Empty,
                description = description ?? string.Empty,
                purpose = purpose ?? string.Empty,
                targetKind = targetKind ?? string.Empty,
                estimatedDays = Math.Max(0f, estimatedDays)
            };
            _plans.Add(plan.planId, plan);
            PlanCreated?.Invoke(plan);
            return plan;
        }

        public bool TryGetPlan(int planId, out Plan plan) => _plans.TryGetValue(planId, out plan);
        public Plan GetPlan(int planId) { _plans.TryGetValue(planId, out var plan); return plan; }

        public bool TrySetPurpose(int planId, string purpose)
        {
            if (!_plans.TryGetValue(planId, out var plan) || plan.IsTerminal) return false;
            plan.purpose = purpose ?? string.Empty;
            return true;
        }

        public bool TrySetActivity(int planId, string activity)
        {
            if (!_plans.TryGetValue(planId, out var plan) || plan.IsTerminal) return false;
            plan.currentActivity = activity ?? string.Empty;
            plan.activity.Set(plan.currentActivity, plan.currentActivity, "executing");
            plan.isWaiting = false;
            plan.waitCondition = default(CivilizationEvolution.Core.Contracts.SimulationWaitCondition);
            return true;
        }

        public bool TrySetActivity(int planId, string activityId, string definitionId, string stateCode)
        {
            if (!_plans.TryGetValue(planId, out var plan) || plan.IsTerminal) return false;
            plan.activity.Set(activityId, definitionId, stateCode);
            plan.currentActivity = activityId ?? string.Empty;
            return true;
        }

        public bool TrySetWait(int planId, CivilizationEvolution.Core.Contracts.SimulationWaitCondition condition)
        {
            if (!_plans.TryGetValue(planId, out var plan) || plan.IsTerminal || plan.state != PlanState.Executing)
                return false;
            if (!condition.IsValid) return false;
            plan.waitCondition = condition;
            plan.isWaiting = true;
            plan.activity.stateCode = "waiting";
            return true;
        }

        public bool TrySetResult(int planId, string resultCode, string resultSummary = null)
        {
            if (!_plans.TryGetValue(planId, out var plan)) return false;
            plan.resultCode = resultCode ?? string.Empty;
            plan.resultSummary = resultSummary ?? string.Empty;
            return true;
        }

        public bool AcceptPlan(int planId) => ChangeState(planId, PlanState.Accepted);

        public bool BeginPreparation(int planId)
        {
            if (!TryGetPlan(planId, out var plan)) return false;
            if (plan.state != PlanState.Accepted && plan.state != PlanState.Paused) return false;
            plan.phase = PlanPhase.Preparation;
            return ChangeState(planId, PlanState.Preparing);
        }

        public bool BeginExecution(int planId)
        {
            if (!TryGetPlan(planId, out var plan)) return false;
            if (plan.state != PlanState.Preparing && plan.state != PlanState.Paused) return false;
            plan.phase = PlanPhase.Execution;
            return ChangeState(planId, PlanState.Executing);
        }

        public bool PausePlan(int planId)
        {
            if (!TryGetPlan(planId, out var plan)) return false;
            if (plan.state != PlanState.Preparing && plan.state != PlanState.Executing) return false;
            return ChangeState(planId, PlanState.Paused);
        }

        public bool CancelPlan(int planId, string resultCode = "cancelled", string resultSummary = null)
        {
            if (!TryGetPlan(planId, out var plan) || plan.IsTerminal) return false;
            plan.resultCode = resultCode ?? string.Empty;
            plan.resultSummary = resultSummary ?? string.Empty;
            return ChangeState(planId, PlanState.Cancelled);
        }

        public bool FailPlan(int planId, string resultCode = "failed", string resultSummary = null)
        {
            if (!TryGetPlan(planId, out var plan) || plan.IsTerminal) return false;
            plan.resultCode = resultCode ?? string.Empty;
            plan.resultSummary = resultSummary ?? string.Empty;
            return ChangeState(planId, PlanState.Failed);
        }

        public bool CompletePlan(int planId, string resultCode = "completed", string resultSummary = null)
        {
            if (!TryGetPlan(planId, out var plan) || plan.IsTerminal) return false;
            plan.progress = 1f;
            plan.phase = PlanPhase.Resolution;
            plan.resultCode = resultCode ?? string.Empty;
            plan.resultSummary = resultSummary ?? string.Empty;
            return ChangeState(planId, PlanState.Completed);
        }

        /// <summary>每日推进。只有 Executing 计划会产生执行进度。</summary>
        public void DailyTick(int currentDay, float deltaDays = 1f)
        {
            _currentDay = currentDay;
            if (deltaDays <= 0f || _activePlanIds.Count == 0) return;

            _scratchPlanIds.Clear();
            _scratchPlanIds.AddRange(_activePlanIds);

            for (int i = 0; i < _scratchPlanIds.Count; i++)
            {
                int planId = _scratchPlanIds[i];
                if (!_plans.TryGetValue(planId, out var plan) || plan.IsTerminal || plan.state != PlanState.Executing)
                    continue;

                plan.elapsedDays += deltaDays;
                if (!_executors.TryGetValue(plan.type, out var executor) || executor == null)
                    continue;

                var result = executor.Execute(plan, deltaDays);

                if (!string.IsNullOrEmpty(result.currentActivity))
                {
                    plan.currentActivity = result.currentActivity;
                    plan.activity.Set(result.currentActivity, result.currentActivity, "executing");
                }
                if (result.outcome == PlanExecutionOutcome.Wait && result.hasWaitCondition)
                {
                    plan.waitCondition = result.waitCondition;
                    plan.isWaiting = true;
                    plan.activity.stateCode = "waiting";
                }
                else if (result.outcome != PlanExecutionOutcome.Wait)
                {
                    plan.isWaiting = false;
                    plan.waitCondition = default(CivilizationEvolution.Core.Contracts.SimulationWaitCondition);
                }
                if (!string.IsNullOrEmpty(result.resultSummary))
                    plan.resultSummary = result.resultSummary;

                float deltaProgress = result.progressDelta;
                if (float.IsNaN(deltaProgress) || float.IsInfinity(deltaProgress)) deltaProgress = 0f;
                if (deltaProgress > 0f)
                    plan.progress = Clamp01(plan.progress + deltaProgress);

                switch (result.outcome)
                {
                    case PlanExecutionOutcome.Complete:
                        CompletePlan(plan.planId,
                            string.IsNullOrEmpty(result.resultCode) ? "executor_completed" : result.resultCode,
                            result.resultSummary);
                        break;
                    case PlanExecutionOutcome.Fail:
                        FailPlan(plan.planId,
                            string.IsNullOrEmpty(result.resultCode) ? "executor_failed" : result.resultCode,
                            result.resultSummary);
                        break;
                    case PlanExecutionOutcome.Cancel:
                        CancelPlan(plan.planId,
                            string.IsNullOrEmpty(result.resultCode) ? "executor_cancelled" : result.resultCode,
                            result.resultSummary);
                        break;
                }

                if (plan.progress >= 1f && !plan.IsTerminal)
                    CompletePlan(plan.planId, "executor_completed");
            }
        }

        public List<Plan> GetPlans(PlanType? type = null, PlanState? state = null)
        {
            var result = new List<Plan>();
            foreach (var kv in _plans)
            {
                var plan = kv.Value;
                if (type.HasValue && plan.type != type.Value) continue;
                if (state.HasValue && plan.state != state.Value) continue;
                result.Add(plan);
            }
            return result;
        }

        public bool RemoveCompletedPlan(int planId)
        {
            if (!_plans.TryGetValue(planId, out var plan) || !plan.IsTerminal) return false;
            RemoveFromActive(planId);
            return _plans.Remove(planId);
        }

        private bool ChangeState(int planId, PlanState newState)
        {
            if (!_plans.TryGetValue(planId, out var plan)) return false;
            if (plan.state == newState || plan.IsTerminal) return false;

            PlanState oldState = plan.state;
            if (!IsValidTransition(oldState, newState)) return false;

            plan.state = newState;
            plan.lastStateChangeDay = _currentDay;

            if (newState == PlanState.Preparing || newState == PlanState.Executing)
                AddToActive(planId);
            else if (newState == PlanState.Completed || newState == PlanState.Failed || newState == PlanState.Cancelled)
                RemoveFromActive(planId);

            PlanStateChanged?.Invoke(plan, oldState, newState);

            if (newState == PlanState.Completed)
                PlanCompleted?.Invoke(plan);
            else if (newState == PlanState.Failed)
                PlanFailed?.Invoke(plan);
            else if (newState == PlanState.Cancelled)
                PlanCancelled?.Invoke(plan);
            return true;
        }

        private static bool IsValidTransition(PlanState from, PlanState to)
        {
            switch (from)
            {
                case PlanState.Proposed:
                    return to == PlanState.Accepted || to == PlanState.Cancelled || to == PlanState.Failed;
                case PlanState.Accepted:
                    return to == PlanState.Preparing || to == PlanState.Paused || to == PlanState.Cancelled || to == PlanState.Failed;
                case PlanState.Preparing:
                    return to == PlanState.Executing || to == PlanState.Paused || to == PlanState.Cancelled || to == PlanState.Failed;
                case PlanState.Executing:
                    return to == PlanState.Paused || to == PlanState.Completed || to == PlanState.Failed || to == PlanState.Cancelled;
                case PlanState.Paused:
                    return to == PlanState.Preparing || to == PlanState.Executing || to == PlanState.Cancelled || to == PlanState.Failed;
                default:
                    return false;
            }
        }

        private void AddToActive(int planId)
        {
            if (!_activePlanIds.Contains(planId)) _activePlanIds.Add(planId);
        }

        private void RemoveFromActive(int planId) => _activePlanIds.Remove(planId);

        private static float Clamp01(float value)
        {
            if (value <= 0f) return 0f;
            if (value >= 1f) return 1f;
            return value;
        }
    }
}
