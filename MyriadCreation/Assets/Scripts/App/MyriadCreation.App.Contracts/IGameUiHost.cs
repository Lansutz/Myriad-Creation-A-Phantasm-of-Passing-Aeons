using MyriadCreation.Core.Contracts;

namespace MyriadCreation.App.Contracts
{
    /// <summary>
    /// 游戏内 UI 反向契约（UI 实现、App 只依赖本接口）。
    /// 实现者：MyriadCreation.UI.Common.UIManager。
    /// UI 4d：World 参数统一为只读聚合契约 IWorldReadModel（UI 契约不再出现旧世界类型）。
    /// </summary>
    public interface IGameUiHost
    {
        void ShowLoading(string message);
        void SetLoadingProgress(float progress, string message);
        void HideLoading();
        void ShowGameUI();
        void HideGameUI();
        void SetGameUIVisible(bool visible);
        void BindWorld(IWorldReadModel world);
        void NotifyWorldChanged(IWorldReadModel world);
        void OpenSettlementDetail(int anchorId);

        // B 类只读视图/命令注入（UI 内部消费 Core 契约）
        void SetWorldReadModel(IWorldReadModel view);
        void SetMapStatsView(IMapStatsView view);
        void SetLawServices(ILawQuery query, ILawCommand command);
        void SetDecreeCommand(IDecreeCommand command);

        bool IsGovernmentPanelOpen { get; }
        bool IsSettlementDetailOpen { get; }
    }
}
