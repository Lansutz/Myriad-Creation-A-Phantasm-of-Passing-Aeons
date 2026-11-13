using System;

namespace MyriadCreation.Core.Data
{
    /// <summary>
    /// 世界历法：日长（自转）、年长（公转）、季节、tick/日 与每日推进天数的唯一来源。
    /// 运行时不得再写死 365/91；物种或剧本可以覆盖这些值（例如非人类物种的日照/公转周期）。
    /// </summary>
    [Serializable]
    public sealed class GameCalendar
    {
        /// <summary>地球基准年长；旧接口/静态工具在拿不到日历时使用。</summary>
        public const int DefaultDaysPerYear = 365;

        /// <summary>地球基准的每季天数（365/4 向下取整）；旧静态工具在拿不到日历时使用。</summary>
        public const int DefaultDaysPerSeason = DefaultDaysPerYear / 4;

        /// <summary>一昼夜多少小时（自转）。仅用于显示与昼夜推导，不增加模拟 tick。</summary>
        public float hoursPerDay = 24f;

        /// <summary>一年多少天（公转）。默认地球基准。</summary>
        public int daysPerYear = DefaultDaysPerYear;

        /// <summary>一年分几季；季节边界按 daysPerYear/seasons 计算，不再写死 91。</summary>
        public int seasons = 4;

        /// <summary>一天包含几个模拟 tick。默认 1：世界模拟以“日”为原子步长。</summary>
        public int ticksPerDay = 1;

        /// <summary>一个模拟 tick 推进多少游戏日。史前快进/高倍速时可 >1。</summary>
        public int daysPerTick = 1;

        /// <summary>
        /// 自转对昼夜温差的一阶影响：转得越快，昼夜温差越小；转得越慢，温差越大。
        /// 24h=1.0；12h≈1.41；48h≈0.71。只作用于昼夜温差，不替代完整气候模型。
        /// </summary>
        public float DiurnalRangeFactor
        {
            get
            {
                float hours = Math.Max(1f, hoursPerDay);
                return (float)Math.Sqrt(24f / hours);
            }
        }

        public int DaysPerSeason
        {
            get
            {
                int count = Math.Max(1, seasons);
                return Math.Max(1, daysPerYear / count);
            }
        }

        /// <summary>dayOfYear 从 1 起。返回 0..seasons-1。</summary>
        public int SeasonIndex(int dayOfYear)
        {
            int index = (Math.Max(1, dayOfYear) - 1) / DaysPerSeason;
            return Math.Min(Math.Max(0, index), Math.Max(0, seasons - 1));
        }

        /// <summary>绝对日（从第 0 天起），用于年龄、任期、条约等跨年比较。</summary>
        public int AbsoluteDay(int year, int dayOfYear)
        {
            return year * daysPerYear + (Math.Max(1, dayOfYear) - 1);
        }

        public int YearOfAbsoluteDay(int absoluteDay)
        {
            int day = Math.Max(0, absoluteDay);
            return day / Math.Max(1, daysPerYear);
        }

        public int DayOfYearOfAbsoluteDay(int absoluteDay)
        {
            int day = Math.Max(0, absoluteDay);
            return day % Math.Max(1, daysPerYear) + 1;
        }

        /// <summary>由一天内的 tick 序号推导小时（0..hoursPerDay），用于昼夜显示。</summary>
        public float HourOfDay(int tickOfDay)
        {
            int ticks = Math.Max(1, ticksPerDay);
            float fraction = (Math.Max(0, tickOfDay) % ticks) / (float)ticks;
            return fraction * hoursPerDay;
        }

        /// <summary>把外部配置夹到可模拟范围，避免 0 年长/0 小时导致除零或时间停滞。</summary>
        public void Normalize()
        {
            hoursPerDay = Math.Min(240f, Math.Max(1f, hoursPerDay));
            daysPerYear = Math.Min(10000, Math.Max(30, daysPerYear));
            seasons = Math.Min(12, Math.Max(1, seasons));
            ticksPerDay = Math.Min(96, Math.Max(1, ticksPerDay));
            daysPerTick = Math.Min(3650, Math.Max(1, daysPerTick));
        }

        public GameCalendar Clone()
        {
            return new GameCalendar
            {
                hoursPerDay = hoursPerDay,
                daysPerYear = daysPerYear,
                seasons = seasons,
                ticksPerDay = ticksPerDay,
                daysPerTick = daysPerTick
            };
        }
    }
}
