using System;

namespace CivilizationEvolution.Simulation.Planning
{
    /// <summary>
    /// Activity 生命周期。Activity 是 Plan 内部可暂停、等待、恢复的最小执行单元。
    /// </summary>
    public enum ActivityState
    {
        Pending,
        Running,
        Waiting,
        Completed,
        Failed,
        Cancelled
    }

    /// <summary>Plan 内的一个运行时活动。</summary>
    [Serializable]
    public sealed class Activity
    {
        public int activityId;
        public string key = string.Empty;
        public ActivityState state = ActivityState.Pending;
        public float progress;
        public float elapsedDays;
        public float waitDays;
        public string resultCode = string.Empty;

        public Activity(int activityId, string key)
        {
            this.activityId = activityId;
            this.key = key ?? string.Empty;
        }

        public bool IsTerminal =>
            state == ActivityState.Completed ||
            state == ActivityState.Failed ||
            state == ActivityState.Cancelled;
    }

    /// <summary>统一过程结果。Process 层不直接修改 Plan 生命周期。</summary>
    public enum ProcessStatus
    {
        Continue,
        Complete,
        Wait,
        Fail,
        Cancel
    }

    public readonly struct ProcessResult
    {
        public readonly ProcessStatus status;
        public readonly float progressDelta;
        public readonly float waitDays;
        public readonly string code;

        public ProcessResult(
            ProcessStatus status,
            float progressDelta = 0f,
            float waitDays = 0f,
            string code = null)
        {
            this.status = status;
            this.progressDelta = progressDelta;
            this.waitDays = Math.Max(0f, waitDays);
            this.code = code ?? string.Empty;
        }

        public static ProcessResult Continue(float progressDelta = 0f, string code = null)
            => new ProcessResult(ProcessStatus.Continue, progressDelta, 0f, code);

        public static ProcessResult Complete(string code = "completed")
            => new ProcessResult(ProcessStatus.Complete, 0f, 0f, code);

        public static ProcessResult Wait(float waitDays, string code = "waiting")
            => new ProcessResult(ProcessStatus.Wait, 0f, waitDays, code);

        public static ProcessResult Fail(string code = "failed")
            => new ProcessResult(ProcessStatus.Fail, 0f, 0f, code);

        public static ProcessResult Cancel(string code = "cancelled")
            => new ProcessResult(ProcessStatus.Cancel, 0f, 0f, code);
    }

    /// <summary>Activity 执行上下文。</summary>
    public readonly struct ActivityExecutionContext
    {
        public readonly Plan plan;
        public readonly Activity activity;
        public readonly int currentDay;
        public readonly float deltaDays;

        public ActivityExecutionContext(Plan plan, Activity activity, int currentDay, float deltaDays)
        {
            this.plan = plan;
            this.activity = activity;
            this.currentDay = currentDay;
            this.deltaDays = deltaDays;
        }
    }

    /// <summary>
    /// Activity 级领域执行器。Construction/Research/Military/Diplomacy 等领域只实现自己的行为。
    /// </summary>
    public interface IActivityExecutor
    {
        string ActivityKey { get; }
        ProcessResult Execute(ActivityExecutionContext context);
        void OnActivityEnded(ActivityExecutionContext context);
    }

    /// <summary>
    /// Plan 级运行时过程。负责选择/推进 Activity，不拥有具体领域规则。
    /// </summary>
    public interface ISimulationProcess
    {
        PlanType Type { get; }
        ProcessResult Execute(Plan plan, int currentDay, float deltaDays);
        void OnProcessEnded(Plan plan);
    }
}
