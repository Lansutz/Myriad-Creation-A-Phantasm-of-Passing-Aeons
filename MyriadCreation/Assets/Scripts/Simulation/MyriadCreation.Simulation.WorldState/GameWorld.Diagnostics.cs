using System.Diagnostics;
using UnityEngine;

namespace MyriadCreation.Simulation.WorldState
{
    public partial class GameWorld
    {
        /// <summary>
        /// Headless 长跑/验收台用：推进恰好 days 天并执行已注册调度。
        /// 与正常 GameTick 使用同一 SimulationScheduler（时间由 simulation.time.daily 推进），避免两套时间语义。
        /// daysPerTick>1 时按批处理折算 tick 次数，总天数仍是调用方给的 days。
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
                int perTick = Mathf.Max(1, daysPerTick);
                int ticks = Mathf.CeilToInt((float)days / perTick);
                for (int i = 0; i < ticks; i++)
                {
                    // 时间由 simulation.time.daily 推进；这里重复调用会让长跑以两倍速度老化。
                    var tickClock = Stopwatch.StartNew();
                    SimulationScheduler.TickBudgeted(currentDay, currentYear, perTick, float.MaxValue,
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
