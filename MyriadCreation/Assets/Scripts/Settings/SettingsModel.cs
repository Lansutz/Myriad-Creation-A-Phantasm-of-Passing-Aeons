using System;
using System.Collections.Generic;
using UnityEngine;
using MyriadCreation.Core;
using MyriadCreation.Core.Enums;
using MyriadCreation.Infrastructure.Audio;

namespace MyriadCreation.App.Settings
{
    /// <summary>
    /// 游戏设置的可测试模型：负责默认值、范围夹取、PlayerPrefs 序列化与重置。
    /// 纯数据 + PlayerPrefs，不直接操作 UI；应用引擎设置由 <see cref="SettingsService"/> 负责。
    /// 所有键统一前缀 <c>MyriadCreation.Settings.</c>，避免与旧设置（screen_width 等）冲突。
    /// </summary>
    public sealed class SettingsModel
    {
        public const string KeyPrefix = "MyriadCreation.Settings.";
        public const string KeyInitialized = KeyPrefix + "Initialized";
        public const string KeyResolutionWidth = KeyPrefix + "Resolution.Width";
        public const string KeyResolutionHeight = KeyPrefix + "Resolution.Height";
        public const string KeyResolutionRefreshRate = KeyPrefix + "Resolution.RefreshRate";
        public const string KeyDisplayMode = KeyPrefix + "DisplayMode";
        public const string KeyVSync = KeyPrefix + "VSync";
        public const string KeyQualityLevel = KeyPrefix + "QualityLevel";
        public const string KeyTargetFrameRate = KeyPrefix + "TargetFrameRate";
        public const string KeyUiScale = KeyPrefix + "UiScale";
        public const string KeyLanguage = KeyPrefix + "Language";
        public const string KeyMasterVolume = KeyPrefix + "Volume.Master";
        public const string KeyMusicVolume = KeyPrefix + "Volume.Music";
        public const string KeySfxVolume = KeyPrefix + "Volume.Sfx";
        public const string KeyDefaultMapSize = KeyPrefix + "DefaultMapSize";
        public const string KeyFontChoice = KeyPrefix + "FontChoice";

        /// <summary>全部持久化键（重置/清理用；新增设置项必须同步加入此表）。</summary>
        public static readonly string[] AllKeys =
        {
            KeyInitialized, KeyResolutionWidth, KeyResolutionHeight, KeyResolutionRefreshRate, KeyDisplayMode,
            KeyVSync, KeyQualityLevel, KeyTargetFrameRate, KeyUiScale, KeyLanguage,
            KeyMasterVolume, KeyMusicVolume, KeySfxVolume, KeyDefaultMapSize, KeyFontChoice
        };

        /// <summary>帧率上限的可选值（-1 = 无限制）。</summary>
        public static readonly int[] AllowedFrameRates = { -1, 30, 60, 120, 144 };

        /// <summary>UI 缩放可选档位。</summary>
        public static readonly float[] AllowedUiScales = { 0.8f, 1f, 1.2f, 1.5f };

        /// <summary>支持切换的语言（BCP 47 码 / StreamingAssets Base/Localization 文件名）。</summary>
        public static readonly string[] SupportedLanguages = { "zh-Hans", "en" };

        public static readonly string[] LanguageLabels = { "简体中文", "English" };

        /// <summary>新游戏默认地图尺寸的可选项（与 Core.Enums.MapSizePreset 一致）。</summary>
        public static readonly MapSizePreset[] MapSizes =
        {
            MapSizePreset.Large, MapSizePreset.Huge, MapSizePreset.Reference, MapSizePreset.Enormous
        };

        public static readonly string[] MapSizeLabels =
        {
            "Large（1024×512）", "Huge（2048×1024）", "Reference（1920×1080）", "Enormous（3072×1536）"
        };

        /// <summary>界面字体选项：0=系统黑体(simhei)/1=思源黑体/2=思源宋体。</summary>
        public const int MaxFontChoice = 2;

        public static readonly string[] FontChoiceLabels =
        {
            "系统黑体（simhei）", "思源黑体（Source Han Sans）", "思源宋体（Source Han Serif）"
        };

        /// <summary>显示模式下拉的可选值（顺序与 DisplayModeLabels 对应）。</summary>
        public static readonly FullScreenMode[] DisplayModes =
        {
            FullScreenMode.FullScreenWindow, FullScreenMode.ExclusiveFullScreen,
            FullScreenMode.MaximizedWindow, FullScreenMode.Windowed
        };

        public static readonly string[] DisplayModeLabels =
        {
            "全屏窗口（无边框）", "独占全屏", "最大化窗口", "窗口化"
        };

        public const int MinResolutionWidth = 640;
        public const int MinResolutionHeight = 480;
        public const int MaxResolutionDimension = 16384;
        /// <summary>刷新率上限（Hz）；0 = 自动（不指定 preferred refresh rate）。</summary>
        public const int MaxRefreshRate = 1000;
        public const int KeepCurrentQualityLevel = -1;
        public const int UnlimitedFrameRate = -1;

        public const float DefaultMasterVolume = 1f;
        public const float DefaultMusicVolume = 0.8f;
        public const float DefaultSfxVolume = 0.9f;

        /// <summary>分辨率下拉项（0×0 表示"自动（当前分辨率）"，应用时不下发 SetResolution）。</summary>
        public struct ResolutionOption
        {
            public int Width;
            public int Height;
            /// <summary>刷新率（Hz）；0 = 自动/未指定（应用时用 3 参 SetResolution）。</summary>
            public int RefreshRate;
            public string Label;
        }

        /// <summary>
        /// 分辨率三元组（宽/高/刷新率）：生产路径从 <see cref="Resolution"/> 转换而来；
        /// 测试/非 Unity 环境可直接构造，避免依赖 Unity 6 才可写的 <c>Resolution.refreshRateRatio</c>。
        /// </summary>
        public struct ResolutionDescriptor
        {
            public int Width;
            public int Height;
            public int RefreshRate;
        }

        public int ResolutionWidth { get; set; }
        public int ResolutionHeight { get; set; }
        /// <summary>刷新率（Hz）；0 = 自动（不指定 preferred refresh rate）。</summary>
        public int ResolutionRefreshRate { get; set; }
        public FullScreenMode DisplayMode { get; set; } = FullScreenMode.FullScreenWindow;
        public bool VSync { get; set; } = true;
        /// <summary>画质等级；-1 表示"保持当前"。</summary>
        public int QualityLevel { get; set; } = KeepCurrentQualityLevel;
        /// <summary>帧率上限；-1 表示无限制。</summary>
        public int TargetFrameRate { get; set; } = UnlimitedFrameRate;
        public float UiScale { get; set; } = 1f;
        public string Language { get; set; } = "zh-Hans";
        public float MasterVolume { get; set; } = DefaultMasterVolume;
        public float MusicVolume { get; set; } = DefaultMusicVolume;
        public float SfxVolume { get; set; } = DefaultSfxVolume;
        public MapSizePreset DefaultMapSize { get; set; } = MapSizePreset.Large;
        /// <summary>界面字体选择：0=系统黑体(simhei)/1=思源黑体/2=思源宋体。</summary>
        public int FontChoice { get; set; } = 0;

        public static SettingsModel CreateDefault()
        {
            return new SettingsModel
            {
                ResolutionWidth = 0,
                ResolutionHeight = 0,
                ResolutionRefreshRate = 0,
                DisplayMode = FullScreenMode.FullScreenWindow,
                VSync = true,
                QualityLevel = KeepCurrentQualityLevel,
                TargetFrameRate = UnlimitedFrameRate,
                UiScale = 1f,
                Language = "zh-Hans",
                MasterVolume = DefaultMasterVolume,
                MusicVolume = DefaultMusicVolume,
                SfxVolume = DefaultSfxVolume,
                DefaultMapSize = MapSizePreset.Large,
                FontChoice = 0
            };
        }

        /// <summary>以当前引擎/运行时状态构造模型（无保存值时作为面板初值，保证"打开即显示当前值"）。</summary>
        public static SettingsModel CreateFromRuntime()
        {
            SettingsModel model = CreateDefault();
            int width = Screen.currentResolution.width;
            int height = Screen.currentResolution.height;
            if (width < MinResolutionWidth || height < MinResolutionHeight)
            {
                width = Screen.width;
                height = Screen.height;
            }
            if (width >= MinResolutionWidth && height >= MinResolutionHeight)
            {
                model.ResolutionWidth = width;
                model.ResolutionHeight = height;
                model.ResolutionRefreshRate = GetRefreshRateHz(Screen.currentResolution);
            }
            model.DisplayMode = Screen.fullScreenMode;
            model.VSync = QualitySettings.vSyncCount > 0;
            model.QualityLevel = QualitySettings.GetQualityLevel();
            model.TargetFrameRate = ClampFrameRate(Application.targetFrameRate);
            model.UiScale = 1f;
            model.Language = Array.IndexOf(SupportedLanguages, Localization.CurrentLanguage) >= 0
                ? Localization.CurrentLanguage
                : "zh-Hans";
            model.MasterVolume = AudioListener.volume;
            MusicPlayerSystem music = MusicPlayerSystem.Instance;
            if (music != null)
            {
                model.MusicVolume = music.Volume;
            }
            model.DefaultMapSize = LoadDefaultMapSize(MapSizePreset.Large);
            model.Clamp(GetQualityLevelCount());
            return model;
        }

        public static bool HasSaved()
        {
            return PlayerPrefs.GetInt(KeyInitialized, 0) == 1;
        }

        /// <summary>读保存值；没有保存过则返回默认（不使用运行时值，便于测试确定性）。</summary>
        public static SettingsModel Load()
        {
            if (!HasSaved())
            {
                return CreateDefault();
            }
            var model = new SettingsModel
            {
                ResolutionWidth = PlayerPrefs.GetInt(KeyResolutionWidth, 0),
                ResolutionHeight = PlayerPrefs.GetInt(KeyResolutionHeight, 0),
                ResolutionRefreshRate = PlayerPrefs.GetInt(KeyResolutionRefreshRate, 0),
                DisplayMode = (FullScreenMode)PlayerPrefs.GetInt(KeyDisplayMode, (int)FullScreenMode.FullScreenWindow),
                VSync = PlayerPrefs.GetInt(KeyVSync, 1) != 0,
                QualityLevel = PlayerPrefs.GetInt(KeyQualityLevel, KeepCurrentQualityLevel),
                TargetFrameRate = PlayerPrefs.GetInt(KeyTargetFrameRate, UnlimitedFrameRate),
                UiScale = PlayerPrefs.GetFloat(KeyUiScale, 1f),
                Language = PlayerPrefs.GetString(KeyLanguage, "zh-Hans"),
                MasterVolume = PlayerPrefs.GetFloat(KeyMasterVolume, DefaultMasterVolume),
                MusicVolume = PlayerPrefs.GetFloat(KeyMusicVolume, DefaultMusicVolume),
                SfxVolume = PlayerPrefs.GetFloat(KeySfxVolume, DefaultSfxVolume),
                DefaultMapSize = (MapSizePreset)PlayerPrefs.GetInt(KeyDefaultMapSize, (int)MapSizePreset.Large),
                FontChoice = PlayerPrefs.GetInt(KeyFontChoice, 0)
            };
            model.Clamp(GetQualityLevelCount());
            return model;
        }

        /// <summary>面板/启动用：有保存值读保存值，否则读运行时当前值。</summary>
        public static SettingsModel LoadOrCreate()
        {
            return HasSaved() ? Load() : CreateFromRuntime();
        }

        public void Save()
        {
            PlayerPrefs.SetInt(KeyResolutionWidth, ResolutionWidth);
            PlayerPrefs.SetInt(KeyResolutionHeight, ResolutionHeight);
            PlayerPrefs.SetInt(KeyResolutionRefreshRate, ResolutionRefreshRate);
            PlayerPrefs.SetInt(KeyDisplayMode, (int)DisplayMode);
            PlayerPrefs.SetInt(KeyVSync, VSync ? 1 : 0);
            PlayerPrefs.SetInt(KeyQualityLevel, QualityLevel);
            PlayerPrefs.SetInt(KeyTargetFrameRate, TargetFrameRate);
            PlayerPrefs.SetFloat(KeyUiScale, UiScale);
            PlayerPrefs.SetString(KeyLanguage, Language ?? "zh-Hans");
            PlayerPrefs.SetFloat(KeyMasterVolume, MasterVolume);
            PlayerPrefs.SetFloat(KeyMusicVolume, MusicVolume);
            PlayerPrefs.SetFloat(KeySfxVolume, SfxVolume);
            PlayerPrefs.SetInt(KeyDefaultMapSize, (int)DefaultMapSize);
            PlayerPrefs.SetInt(KeyFontChoice, FontChoice);
            PlayerPrefs.SetInt(KeyInitialized, 1);
            PlayerPrefs.Save();
        }

        /// <summary>删除全部已保存设置键（不触碰其它系统）。</summary>
        public static void DeleteSaved()
        {
            for (int i = 0; i < AllKeys.Length; i++)
            {
                PlayerPrefs.DeleteKey(AllKeys[i]);
            }
            PlayerPrefs.Save();
        }

        /// <summary>重置为默认（删除旧键并返回默认模型；调用方负责 Save/Apply）。</summary>
        public static SettingsModel ResetToDefaults()
        {
            DeleteSaved();
            return CreateDefault();
        }

        /// <summary>把非法/越界值夹取回合法范围；qualityLevelCount = 当前 QualitySettings 档位数（0 表示无画质档）。</summary>
        public void Clamp(int qualityLevelCount)
        {
            bool resolutionValid = ResolutionWidth >= MinResolutionWidth
                && ResolutionHeight >= MinResolutionHeight
                && ResolutionWidth <= MaxResolutionDimension
                && ResolutionHeight <= MaxResolutionDimension;
            if (!resolutionValid)
            {
                ResolutionWidth = 0;
                ResolutionHeight = 0;
                ResolutionRefreshRate = 0;
            }
            else if (ResolutionRefreshRate < 0 || ResolutionRefreshRate > MaxRefreshRate)
            {
                ResolutionRefreshRate = 0;
            }

            if (Array.IndexOf(DisplayModes, DisplayMode) < 0)
            {
                DisplayMode = FullScreenMode.FullScreenWindow;
            }
            TargetFrameRate = ClampFrameRate(TargetFrameRate);
            UiScale = ClampUiScale(UiScale);
            MasterVolume = Mathf.Clamp01(MasterVolume);
            MusicVolume = Mathf.Clamp01(MusicVolume);
            SfxVolume = Mathf.Clamp01(SfxVolume);
            if (Array.IndexOf(SupportedLanguages, Language) < 0)
            {
                Language = "zh-Hans";
            }
            if (Array.IndexOf(MapSizes, DefaultMapSize) < 0)
            {
                DefaultMapSize = MapSizePreset.Large;
            }
            if (FontChoice < 0 || FontChoice > MaxFontChoice)
            {
                FontChoice = 0;
            }
            if (qualityLevelCount <= 0)
            {
                QualityLevel = KeepCurrentQualityLevel;
            }
            else if (QualityLevel >= 0)
            {
                QualityLevel = Mathf.Clamp(QualityLevel, 0, qualityLevelCount - 1);
            }
        }

        /// <summary>帧率夹取到允许集合（最近值，平局取较小档；-1 无限制）。</summary>
        public static int ClampFrameRate(int frameRate)
        {
            int best = AllowedFrameRates[0];
            int bestDistance = int.MaxValue;
            for (int i = 0; i < AllowedFrameRates.Length; i++)
            {
                int distance = Mathf.Abs(AllowedFrameRates[i] - frameRate);
                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    best = AllowedFrameRates[i];
                }
            }
            return best;
        }

        /// <summary>UI 缩放夹取到最近档位。</summary>
        public static float ClampUiScale(float scale)
        {
            if (float.IsNaN(scale))
            {
                return 1f;
            }
            float best = AllowedUiScales[0];
            float bestDistance = float.MaxValue;
            for (int i = 0; i < AllowedUiScales.Length; i++)
            {
                float distance = Mathf.Abs(AllowedUiScales[i] - scale);
                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    best = AllowedUiScales[i];
                }
            }
            return best;
        }

        public static int GetQualityLevelCount()
        {
            string[] names = QualitySettings.names;
            return names == null ? 0 : names.Length;
        }

        public static int IndexOfFrameRate(int frameRate)
        {
            int index = Array.IndexOf(AllowedFrameRates, frameRate);
            return index < 0 ? Array.IndexOf(AllowedFrameRates, UnlimitedFrameRate) : index;
        }

        public static int IndexOfUiScale(float scale)
        {
            float clamped = ClampUiScale(scale);
            int index = Array.IndexOf(AllowedUiScales, clamped);
            return index < 0 ? 1 : index;
        }

        public static int IndexOfLanguage(string language)
        {
            int index = Array.IndexOf(SupportedLanguages, language);
            return index < 0 ? 0 : index;
        }

        public static int IndexOfMapSize(MapSizePreset preset)
        {
            int index = Array.IndexOf(MapSizes, preset);
            return index < 0 ? 0 : index;
        }

        public static int IndexOfDisplayMode(FullScreenMode mode)
        {
            int index = Array.IndexOf(DisplayModes, mode);
            return index < 0 ? 0 : index;
        }

        /// <summary>读取保存的默认地图尺寸（供 Bootstrap 新游戏使用）；无保存/非法值返回 fallback。</summary>
        public static MapSizePreset LoadDefaultMapSize(MapSizePreset fallback)
        {
            if (!HasSaved())
            {
                return fallback;
            }
            int stored = PlayerPrefs.GetInt(KeyDefaultMapSize, (int)fallback);
            var preset = (MapSizePreset)stored;
            return Array.IndexOf(MapSizes, preset) >= 0 ? preset : fallback;
        }

        /// <summary>
        /// Unity 6 分辨率刷新率读取：<c>Resolution.refreshRateRatio</c>（numerator/denominator）四舍五入为 Hz；
        /// 旧 Unity（无 RefreshRate API）编译期回退 0 = 自动。batchmode/无图形设备时 Resolution 为空值，同样返回 0。
        /// </summary>
        public static int GetRefreshRateHz(Resolution resolution)
        {
#if UNITY_2022_2_OR_NEWER
            RefreshRate ratio = resolution.refreshRateRatio;
            if (ratio.denominator > 0u && ratio.numerator > 0u)
            {
                double hz = (double)ratio.numerator / (double)ratio.denominator;
                if (!double.IsNaN(hz) && !double.IsInfinity(hz) && hz > 0.0)
                {
                    return Mathf.Clamp((int)System.Math.Round(hz), 0, MaxRefreshRate);
                }
            }
            return 0;
#else
            return 0;
#endif
        }

        /// <summary>
        /// 构造分辨率下拉项：首项固定"自动（当前分辨率）"；其余按 宽×高×刷新率 去重并按（宽降序、高降序、
        /// 刷新率降序）排序。刷新率来自 Unity 6 <c>Resolution.refreshRateRatio</c>（旧版 Unity 恒 0）。
        /// 无图形设备（batchmode）时 Screen.resolutions 为空，仍返回"自动"一项，保证 UI 构建 null 安全。
        /// </summary>
        public static List<ResolutionOption> BuildResolutionOptions(Resolution[] resolutions)
        {
            if (resolutions == null)
            {
                return BuildResolutionOptionsFromDescriptors(null);
            }
            var descriptors = new ResolutionDescriptor[resolutions.Length];
            for (int i = 0; i < resolutions.Length; i++)
            {
                descriptors[i] = new ResolutionDescriptor
                {
                    Width = resolutions[i].width,
                    Height = resolutions[i].height,
                    RefreshRate = GetRefreshRateHz(resolutions[i])
                };
            }
            return BuildResolutionOptionsFromDescriptors(descriptors);
        }

        /// <summary>
        /// 测试/非 Unity 来源用入口：按 (width,height,refreshRate) 三元组构建下拉项，
        /// 与 <see cref="BuildResolutionOptions(Resolution[])"/> 共用同一去重/排序/标签实现。
        /// </summary>
        public static List<ResolutionOption> BuildResolutionOptionsFromDescriptors(ResolutionDescriptor[] descriptors)
        {
            var options = new List<ResolutionOption>
            {
                new ResolutionOption { Width = 0, Height = 0, RefreshRate = 0, Label = "自动（当前分辨率）" }
            };
            if (descriptors == null || descriptors.Length == 0)
            {
                return options;
            }
            var seen = new HashSet<long>();
            var display = new List<ResolutionOption>();
            for (int i = 0; i < descriptors.Length; i++)
            {
                int width = descriptors[i].Width;
                int height = descriptors[i].Height;
                int refreshRate = Mathf.Clamp(descriptors[i].RefreshRate, 0, MaxRefreshRate);
                if (width < MinResolutionWidth || height < MinResolutionHeight)
                {
                    continue;
                }
                if (width > MaxResolutionDimension || height > MaxResolutionDimension)
                {
                    continue;
                }
                long key = ((long)width * 100000L + height) * 100000L + refreshRate;
                if (!seen.Add(key))
                {
                    continue;
                }
                display.Add(new ResolutionOption
                {
                    Width = width,
                    Height = height,
                    RefreshRate = refreshRate,
                    Label = refreshRate > 0 ? width + "×" + height + " @" + refreshRate + "Hz" : width + "×" + height
                });
            }
            display.Sort((a, b) =>
            {
                int byWidth = b.Width.CompareTo(a.Width);
                if (byWidth != 0)
                {
                    return byWidth;
                }
                int byHeight = b.Height.CompareTo(a.Height);
                return byHeight != 0 ? byHeight : b.RefreshRate.CompareTo(a.RefreshRate);
            });
            options.AddRange(display);
            return options;
        }

        /// <summary>
        /// 在分辨率下拉项中查找当前模型值：先按 宽×高×刷新率 精确匹配；
        /// 未指定刷新率（0=自动/旧存档）时回退到同宽高的任意刷新率项；仍失败回退自动项（索引 0）。
        /// </summary>
        public static int FindResolutionIndex(List<ResolutionOption> options, int width, int height, int refreshRate = 0)
        {
            if (options == null || options.Count == 0)
            {
                return 0;
            }
            int targetRefreshRate = Mathf.Clamp(refreshRate, 0, MaxRefreshRate);
            for (int i = 0; i < options.Count; i++)
            {
                if (options[i].Width == width && options[i].Height == height
                    && options[i].RefreshRate == targetRefreshRate)
                {
                    return i;
                }
            }
            if (targetRefreshRate == 0)
            {
                // 兼容旧存档（只存了 w×h）与"自动刷新率"选中：同宽高任意刷新率项都可回显。
                for (int i = 0; i < options.Count; i++)
                {
                    if (options[i].Width == width && options[i].Height == height && width > 0 && height > 0)
                    {
                        return i;
                    }
                }
            }
            return 0;
        }
    }
}
