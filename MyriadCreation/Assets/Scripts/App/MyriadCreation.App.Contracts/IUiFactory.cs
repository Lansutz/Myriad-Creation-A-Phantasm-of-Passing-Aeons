using UnityEngine;

namespace MyriadCreation.App.Contracts
{
    /// <summary>
    /// 动态 UI 工厂契约：接管原来散落在 SceneFlow/GameManager 里的 AddComponent/FindAnyObjectByType。
    /// 实现者：MyriadCreation.UI.Common.UiFactory。
    /// </summary>
    public interface IUiFactory
    {
        IMainMenuUi CreateMainMenuUi(Transform parent);
        ISettlementDetailUi EnsureSettlementDetailUi();
        ISettlementDetailUi ExistingSettlementDetailUi { get; }
        void EnsureGovernmentButton();
    }
}
