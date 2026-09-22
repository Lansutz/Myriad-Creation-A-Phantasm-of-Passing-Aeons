using System;
using System.Collections.Generic;

namespace CivilizationEvolution.Simulation.Planning
{
    /// <summary>
    /// 通用 Activity 驱动过程。
    /// PlanSystem 只负责生命周期；本类负责 Activity 的 Wait/Resume 与执行器分派。
    /// </summary>
    public sealed class ActivitySimulationProcess : ISimulationProcess
    {
        private readonly Dictionary<string, IActivityExecutor> _executors =
            new Dictionary<string, IActivityExecutor>(StringComparer.Ordinal);

        private int _nextActivityId = 1;

        public PlanType Type { get; }

        public ActivitySimulationProcess(PlanType type)
        {
            Type = type;
        }

        public void RegisterExecutor(IActivityExecutor executor)
        {
            if (executor == null) throw new ArgumentNullException(nameof(executor));
            if (string.IsNullOrEmpty(executor.ActivityKey))
                throw new ArgumentException("Activity executor key is required.", nameof(executor));

            _executors[executor.ActivityKey] = executor;
        }

        public bool UnregisterExecutor(string activityKey)
            => !string.IsNullOrEmpty(activityKey) && _executors.Remove(activityKey);

        public ProcessResult Execute(Plan plan, int currentDay, float deltaDays)
        {
            if (plan == null) return ProcessResult.Fail("plan_null");
            if (deltaDays <= 0f) return ProcessResult.Continue();

            Activity activity = GetOrCreateCurrentActivity(plan);
            if (activity == null) return ProcessResult.Fail("activity_not_configured");

            if (activity.state == ActivityState.Waiting)
            {
                activity.waitDays = Math.Max(0f, activity.waitDays - deltaDays);
                if (activity.waitDays > 0f)
                    return ProcessResult.Wait(activity.waitDays, "activity_waiting");

                activity.state = ActivityState.Running;
            }

            if (activity.state == ActivityState.Pending)
                activity.state = ActivityState.Running;

            if (!_executors.TryGetValue(activity.key, out var executor) || executor == null)
                return ProcessResult.Fail("activity_executor_not_registered");

            var context = new ActivityExecutionContext(plan, activity, currentDay, deltaDays);
            ProcessResult result = executor.Execute(context);

            if (result.progressDelta > 0f)
                activity.progress = Clamp01(activity.progress + result.progressDelta);

            switch (result.status)
            {
                case ProcessStatus.Wait:
                    activity.state = ActivityState.Waiting;
                    activity.waitDays = result.waitDays;
                    activity.resultCode = result.code;
                    return result;

                case ProcessStatus.Complete:
                    activity.progress = 1f;
                    activity.state = ActivityState.Completed;
                    activity.resultCode = result.code;
                    executor.OnActivityEnded(context);

                    if (TryAdvanceActivity(plan))
                        return ProcessResult.Continue(code: "next_activity");

                    return result;

                case ProcessStatus.Fail:
                    activity.state = ActivityState.Failed;
                    activity.resultCode = result.code;
                    executor.OnActivityEnded(context);
                    return result;

                case ProcessStatus.Cancel:
                    activity.state = ActivityState.Cancelled;
                    activity.resultCode = result.code;
                    executor.OnActivityEnded(context);
                    return result;

                default:
                    return result;
            }
        }

        public void OnProcessEnded(Plan plan)
        {
            // Activity 结束回调已在状态转换时触发；这里保留统一过程生命周期钩子。
        }

        private Activity GetOrCreateCurrentActivity(Plan plan)
        {
            if (plan.activities.Count == 0)
            {
                // 不擅自猜测领域动作；Plan 必须显式声明 Activity key。
                return null;
            }

            if (plan.activeActivityIndex < 0)
                plan.activeActivityIndex = 0;

            if (plan.activeActivityIndex >= plan.activities.Count)
                return null;

            return plan.activities[plan.activeActivityIndex];
        }

        private static bool TryAdvanceActivity(Plan plan)
        {
            int next = plan.activeActivityIndex + 1;
            if (next >= plan.activities.Count)
                return false;

            plan.activeActivityIndex = next;
            return true;
        }

        private float Clamp01(float value)
        {
            if (value <= 0f) return 0f;
            if (value >= 1f) return 1f;
            return value;
        }
    }
}
