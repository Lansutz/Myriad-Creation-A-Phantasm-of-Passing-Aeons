using UnityEngine;
using UnityEngine.UI;
using MyriadCreation.App.Settings;

namespace MyriadCreation.UI.Common
{
    /// <summary>
    /// 动态 Canvas 的 UI 缩放绑定器：给运行时代码新建的 Canvas 挂一个，OnEnable/Start 时把已保存的
    /// UI 缩放应用到 CanvasScaler（Bootstrap.Awake 应用设置时这些动态 Canvas 还不存在，需要各自补一次）。
    /// 典型用法：<c>UiScaleBinder.Attach(canvasObj);</c>（SettlementDetailPanel 自己的 Canvas 已使用）。
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class UiScaleBinder : MonoBehaviour
    {
        private CanvasScaler _scaler;

        /// <summary>给动态创建的 Canvas 挂载绑定器（重复调用不会重复挂）。</summary>
        public static void Attach(GameObject canvasObj)
        {
            if (canvasObj == null)
            {
                return;
            }
            if (canvasObj.GetComponent<UiScaleBinder>() == null)
            {
                canvasObj.AddComponent<UiScaleBinder>();
            }
        }

        private void Awake()
        {
            EnsureScaler();
        }

        private void OnEnable()
        {
            Apply();
        }

        private void Start()
        {
            Apply();
        }

        /// <summary>应用已保存的 UI 缩放（对项目中所有 CanvasScaler 生效，与本面板 Canvas 一致）。</summary>
        public void Apply()
        {
            EnsureScaler();
            if (_scaler == null)
            {
                return;
            }
            // 先保证本 Canvas 采用工程统一的 ScaleWithScreenSize 基准，随后交给 SettingsService 统一按缩放档位换算。
            _scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            if (_scaler.referenceResolution.x <= 1f || _scaler.referenceResolution.y <= 1f)
            {
                _scaler.referenceResolution = new Vector2(
                    SettingsService.BaseReferenceWidth, SettingsService.BaseReferenceHeight);
            }
            SettingsService.ApplyUiScale(SettingsService.Current.UiScale);
        }

        private void EnsureScaler()
        {
            if (_scaler != null)
            {
                return;
            }
            _scaler = GetComponent<CanvasScaler>();
            if (_scaler == null)
            {
                _scaler = gameObject.AddComponent<CanvasScaler>();
            }
        }
    }
}
