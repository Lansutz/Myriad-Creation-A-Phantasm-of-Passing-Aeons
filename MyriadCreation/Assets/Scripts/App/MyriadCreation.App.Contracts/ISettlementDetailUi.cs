using UnityEngine;
using MyriadCreation.Core.Contracts;

namespace MyriadCreation.App.Contracts
{
    /// <summary>
    /// 聚落详情 UI 反向契约。实现者：MyriadCreation.UI.Panels.SettlementDetailPanel。
    /// UI 4d：BindWorld 改收只读聚合契约 IWorldReadModel，UI 契约不再出现旧世界类型。
    /// </summary>
    public interface ISettlementDetailUi
    {
        bool IsOpen { get; }

        void BindWorld(IWorldReadModel world);
        void Build(Transform parent);
        void Show(int settlementAnchorId);
        void Hide();
    }
}
