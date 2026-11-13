using System.Diagnostics;

namespace MyriadCreation.Simulation.WorldState
{
    public partial class GameWorld
    {
        /// <summary>
        /// Headless 长跑/验收台用：推进 N 天并执行已注册调度。
        /// 与正常 GameTick 使用同一 AdvanceTime + SimulationScheduler，避免两套时间语义。
        /// </summary>
        public void RunDiagnosticDays(int days)
        {
            if (days <= 0) return;
            RunPreloadNow();
            // Headless 长跑关闭 Unity 日志：逐条 Debug.Log/LogWarning 在编辑器下会抓栈，
            // 百年档曾因此写出 543MB 日志并把测试拖到无法完成。
            bool previousLogEnabled = UnityEngine.Debug.unityLogger.logEnabled;
            UnityEngine.Debug.unityLogger.logEnabled = false;
            try
            {
                for (int i = 0; i < days; i++)
                {
                    AdvanceTime();
                    var tickClock = Stopwatch.StartNew();
                    SimulationScheduler.TickBudgeted(currentDay, currentYear, daysPerTick, float.MaxValue,
                        () => tickClock.Elapsed.TotalSeconds, out _, out _);
                }
            }
            finally
            {
                UnityEngine.Debug.unityLogger.logEnabled = previousLogEnabled;
            }
        }
    }
}
