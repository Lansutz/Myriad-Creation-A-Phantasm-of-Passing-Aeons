using UnityEngine;
using MyriadCreation.App.Contracts;
using MyriadCreation.UI.Common;
using MyriadCreation.UI.Panels;

namespace MyriadCreation.UI.Common
{
    /// <summary>
    /// 动态 UI 工厂：把 SceneFlow/GameManager 里原本的 AddComponent/FindAnyObjectByType 逻辑收拢到 UI 程序集，
    /// App 层只通过 <see cref="IUiFactory"/> 请求 UI，不反向依赖 UI 具体类型。
    /// </summary>
    public class UiFactory : IUiFactory
    {
        /// <summary>
        /// UI 侧自注册：Bootstrap 迁入 App 后不再由 App 引用 UI 具体类型；
        /// BeforeSceneLoad 保证早于场景 Start（SceneFlow 创建主菜单时可用），PlayMode 测试同样生效。
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void RegisterRuntimeFactory()
        {
            AppServices.RegisterUiFactory(new UiFactory());
        }

        private ISettlementDetailUi _settlementDetail;

        public IMainMenuUi CreateMainMenuUi(Transform parent)
        {
            var go = new GameObject("MainMenuUI");
            if (parent != null)
            {
                go.transform.SetParent(parent, false);
            }
            var ui = go.AddComponent<MainMenuUI>();
            return ui;
        }

        public ISettlementDetailUi EnsureSettlementDetailUi()
        {
            if (IsAlive(_settlementDetail))
            {
                return _settlementDetail;
            }
            _settlementDetail = null;

            var existing = Object.FindAnyObjectByType<SettlementDetailPanel>(FindObjectsInactive.Include);
            if (existing != null)
            {
                existing.Build(null);
                _settlementDetail = existing;
                return _settlementDetail;
            }

            var go = new GameObject("SettlementDetailPanel");
            var panel = go.AddComponent<SettlementDetailPanel>();
            panel.Build(null); // 面板自带 Canvas/Scaler/Raycaster（见 UI 2 卡实现）
            _settlementDetail = panel;
            return _settlementDetail;
        }

        public ISettlementDetailUi ExistingSettlementDetailUi
        {
            get
            {
                if (IsAlive(_settlementDetail))
                {
                    return _settlementDetail;
                }
                _settlementDetail = null;
                return null;
            }
        }

        public void EnsureGovernmentButton()
        {
            // 政府按钮由 UIManager 创建/布局；工厂只负责触发一次，避免 App 直接依赖 UIManager。
            var ui = AppServices.UiHost;
            if (ui != null)
            {
                ui.ShowGameUI();
            }
        }

        private static bool IsAlive(ISettlementDetailUi ui)
        {
            return ui != null && !(ui is Object unityObject && unityObject == null);
        }
    }
}
