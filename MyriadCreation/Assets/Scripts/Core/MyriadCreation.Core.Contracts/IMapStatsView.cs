namespace MyriadCreation.Core.Contracts
{
    /// <summary>
    /// 地图/世界只读统计视图（B 类契约，UI 用）。
    /// 仅使用 Core 可表达类型（int），由 Simulation 侧适配器转调 GameWorld；
    /// UI 只依赖本接口，不直接引用 GameWorld。
    /// </summary>
    public interface IMapStatsView
    {
        int MapWidth { get; }
        int MapHeight { get; }
        int TotalTiles { get; }
        int LandTiles { get; }
        int SeaTiles { get; }
        int ProvinceCount { get; }
        int SettlementCount { get; }
        int RealmCount { get; }
        int CurrentYear { get; }
        int CurrentDay { get; }
        int StartYear { get; }
        int StartDay { get; }
    }
}
