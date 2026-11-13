using System;

namespace MyriadCreation.App.Contracts
{
    /// <summary>
    /// 时间尺度：游戏速度只有五档（1/2/5/10/30），暂停用 0。
    /// 语义 = 游戏日/现实秒（tickInterval=1s、daysPerTick=1 时）；高倍速由 GameCalendar.daysPerTick 批处理。
    /// 放在契约层，是因为设置端（GameManager）与展示端（UIManager）分属两个程序集，
    /// 档位表必须单源，否则 UI 标签和真实 timeScale 会漂移。
    /// </summary>
    public static class GameSpeedScale
    {
        public const float Paused = 0f;

        /// <summary>上限 30 日/秒：再高会让日 tick 与事件队列在同一帧内堆积；史前跨度用 daysPerTick 批处理。</summary>
        public const float Max = 30f;

        public static readonly float[] Options = { 1f, 2f, 5f, 10f, 30f };

        public static float Clamp(float speed)
        {
            if (speed < Paused) return Paused;
            if (speed > Max) return Max;
            return speed;
        }

        /// <summary>精确命中返回下标；不在档位表里返回 -1（旧档/旧场景可能留下 3x）。</summary>
        public static int IndexOf(float speed)
        {
            for (int i = 0; i < Options.Length; i++)
            {
                if (Math.Abs(Options[i] - speed) < 0.001f) return i;
            }
            return -1;
        }

        /// <summary>循环切到下一档；当前不是档位值时从第一档开始。</summary>
        public static float Next(float speed)
        {
            int index = IndexOf(speed);
            return index < 0 ? Options[0] : Options[(index + 1) % Options.Length];
        }
    }
}
