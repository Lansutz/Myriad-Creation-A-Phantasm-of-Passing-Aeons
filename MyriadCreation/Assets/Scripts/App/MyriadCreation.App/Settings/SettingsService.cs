using System;
using UnityEngine;
using UnityEngine.UI;
using MyriadCreation.Core;
using MyriadCreation.Core.Enums;
using MyriadCreation.Infrastructure.Audio;

namespace MyriadCreation.App.Settings
{
    /// <summary>
    /// 设置应用服务：把 <see cref="SettingsModel"/> 落到 Unity 引擎（分辨率/显示模式/vSync/画质/帧率/UI 缩放/音量/语言）。
    /// Bootstrap.Awake 调 <see cref="ApplySaved"/>（先于任何 UI 构建应用），面板应用按钮调 <see cref="Apply"/>。
    /// 项目没有 AudioMixer：主音量走 AudioListener，音乐走 MusicPlayerSystem 现有音量入口，
    /// 音效走 Infrastructure.Audio 的 SfxPlayer 总线（本服务把 SfxVolume 推给 SfxPlayer.Volume）。
    /// </summary>
    public static class SettingsService
    {
        /// <summary>UI 缩放基准分辨率（与主菜单 CanvasScaler 一致）。</summary>
        public const float BaseReferenceWidth = 1920f;
        public const float BaseReferenceHeight = 1080f;

        /// <summary>当前生效设置（Bootstrap 启动时初始化）。</summary>
        public static SettingsModel Current { get; private set; } = SettingsModel.CreateDefault();

        /// <summary>启动应用：有存档读存档，否则按运行时当前值；随后把设置落到引擎。</summary>
        public static void ApplySaved()
        {
            SettingsModel model = SettingsModel.LoadOrCreate();
            model.Clamp(SettingsModel.GetQualityLevelCount());
            Apply(model);
        }

        /// <summary>把模型落到引擎并设为当前设置。</summary>
        public static void Apply(SettingsModel model)
        {
            if (model == null)
            {
                return;
            }
            model.Clamp(SettingsModel.GetQualityLevelCount());
            Current = model;

            ApplyResolution(model);
            ApplyVSync(model);
            ApplyQuality(model);
            ApplyFrameRate(model);
            ApplyUiScale(model.UiScale);
            ApplyAudio(model);
            ApplyLanguage(model.Language);
        }

        /// <summary>重置为默认：删除已保存键，应用默认值（分辨率保持自动）。</summary>
        public static SettingsModel ResetToDefaults()
        {
            SettingsModel model = SettingsModel.ResetToDefaults();
            model.Clamp(SettingsModel.GetQualityLevelCount());
            Apply(model);
            return model;
        }

        /// <summary>新游戏默认地图尺寸：读设置面板保存值，未保存/非法时用 fallback（场景序列化默认）。</summary>
        public static MapSizePreset GetDefaultMapSizePreset(MapSizePreset fallback)
        {
            return SettingsModel.LoadDefaultMapSize(fallback);
        }

        private static void ApplyResolution(SettingsModel model)
        {
            try
            {
                bool hasResolution = model.ResolutionWidth >= SettingsModel.MinResolutionWidth
                    && model.ResolutionHeight >= SettingsModel.MinResolutionHeight;
                // 无图形设备（batchmode / -nographics）时不强行改造分辨率，避免测试/CI 环境异常；
                // 此时 UI 下拉仍展示"自动（当前分辨率）"，null 安全。
                if (Application.isBatchMode && (Screen.resolutions == null || Screen.resolutions.Length == 0))
                {
                    return;
                }
                if (hasResolution)
                {
#if UNITY_2022_2_OR_NEWER
                    // Unity 6：带 preferred refresh rate 的重载（RefreshRate numerator/denominator）。
                    // 模型刷新率 0 = 自动，此时退回 3 参重载由引擎选择默认刷新率。
                    if (model.ResolutionRefreshRate > 0)
                    {
                        Screen.SetResolution(
                            model.ResolutionWidth,
                            model.ResolutionHeight,
                            model.DisplayMode,
                            new RefreshRate
                            {
                                numerator = (uint)Mathf.Clamp(model.ResolutionRefreshRate, 1, SettingsModel.MaxRefreshRate),
                                denominator = 1
                            });
                    }
                    else
                    {
                        Screen.SetResolution(model.ResolutionWidth, model.ResolutionHeight, model.DisplayMode);
                    }
#else
                    // 旧 Unity 没有 RefreshRate API：回退 3 参重载（忽略刷新率字段，行为与旧版一致）。
                    Screen.SetResolution(model.ResolutionWidth, model.ResolutionHeight, model.DisplayMode);
#endif
                }
                else
                {
                    Screen.fullScreenMode = model.DisplayMode;
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[SettingsService] 应用分辨率/显示模式失败：" + ex.Message);
            }
        }

        private static void ApplyVSync(SettingsModel model)
        {
            QualitySettings.vSyncCount = model.VSync ? 1 : 0;
        }

        private static void ApplyQuality(SettingsModel model)
        {
            int count = SettingsModel.GetQualityLevelCount();
            if (count <= 0 || model.QualityLevel < 0)
            {
                return;
            }
            int level = Mathf.Clamp(model.QualityLevel, 0, count - 1);
            QualitySettings.SetQualityLevel(level, true);
        }

        private static void ApplyFrameRate(SettingsModel model)
        {
            Application.targetFrameRate = model.TargetFrameRate;
        }

        /// <summary>把所有 CanvasScaler 的 referenceResolution 按缩放档位换算（基准 1920×1080）。</summary>
        public static void ApplyUiScale(float scale)
        {
            float clamped = SettingsModel.ClampUiScale(scale);
            CanvasScaler[] scalers = UnityEngine.Object.FindObjectsByType<CanvasScaler>(
                FindObjectsInactive.Include, FindObjectsSortMode.None);
            if (scalers == null)
            {
                return;
            }
            for (int i = 0; i < scalers.Length; i++)
            {
                CanvasScaler scaler = scalers[i];
                if (scaler == null)
                {
                    continue;
                }
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(BaseReferenceWidth / clamped, BaseReferenceHeight / clamped);
            }
        }

        private static void ApplyAudio(SettingsModel model)
        {
            AudioListener.volume = Mathf.Clamp01(model.MasterVolume);
            MusicPlayerSystem music = MusicPlayerSystem.Instance;
            if (music != null)
            {
                music.Volume = Mathf.Clamp01(model.MusicVolume);
            }
            // 音效总线在 Infrastructure.Audio（Infrastructure 不能反向引用 UI）：这里把模型音量推给 SfxPlayer，
            // 各处 Play 统一用 SfxPlayer.Volume 作为总线音量（UI→Infrastructure 方向合法，避免循环依赖）。
            SfxPlayer.SetVolume(model.SfxVolume);
        }

        private static void ApplyLanguage(string language)
        {
            if (string.IsNullOrEmpty(language))
            {
                return;
            }
            if (!string.Equals(Localization.CurrentLanguage, language, StringComparison.Ordinal))
            {
                Localization.Initialize(language);
            }
        }
    }
}
