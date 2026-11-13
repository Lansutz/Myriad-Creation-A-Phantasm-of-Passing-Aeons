using NUnit.Framework;
using UnityEngine;
using MyriadCreation.Core.Enums;
using MyriadCreation.App.Settings;

namespace MyriadCreation.Tests.EditMode
{
    /// <summary>
    /// 游戏设置模型回归（task-53）：默认值、范围夹取（分辨率越界回退/音量 0~1/缩放档位/帧率档位/
    /// 语言与地图尺寸回退）、PlayerPrefs 序列化往返、删除与重置默认、分辨率下拉 null 安全与去重。
    /// 所有 PlayerPrefs 写入测试结束即清理，避免污染真实存档设置。
    /// </summary>
    public class SettingsModelTests
    {
        [SetUp]
        public void SetUp()
        {
            SettingsModel.DeleteSaved();
        }

        [TearDown]
        public void TearDown()
        {
            SettingsModel.DeleteSaved();
        }

        [Test]
        public void CreateDefault_HasExpectedValues()
        {
            SettingsModel model = SettingsModel.CreateDefault();

            Assert.AreEqual(0, model.ResolutionWidth, "默认分辨率为 0×0（自动/当前）");
            Assert.AreEqual(0, model.ResolutionHeight, "默认分辨率为 0×0（自动/当前）");
            Assert.AreEqual(0, model.ResolutionRefreshRate, "默认刷新率为 0（自动，不指定 preferred refresh rate）");
            Assert.AreEqual(FullScreenMode.FullScreenWindow, model.DisplayMode);
            Assert.IsTrue(model.VSync, "默认开启垂直同步");
            Assert.AreEqual(SettingsModel.KeepCurrentQualityLevel, model.QualityLevel, "默认画质保持当前");
            Assert.AreEqual(SettingsModel.UnlimitedFrameRate, model.TargetFrameRate, "默认帧率无限制");
            Assert.AreEqual(1f, model.UiScale, 0.0001f);
            Assert.AreEqual("zh-Hans", model.Language);
            Assert.AreEqual(SettingsModel.DefaultMasterVolume, model.MasterVolume, 0.0001f);
            Assert.AreEqual(SettingsModel.DefaultMusicVolume, model.MusicVolume, 0.0001f);
            Assert.AreEqual(SettingsModel.DefaultSfxVolume, model.SfxVolume, 0.0001f);
            Assert.AreEqual(MapSizePreset.Large, model.DefaultMapSize, "默认新游戏地图尺寸 Large");
            Assert.IsFalse(SettingsModel.HasSaved(), "未 Save 前不应有保存标记");
        }

        [Test]
        public void Clamp_RepairsOutOfRangeValues()
        {
            SettingsModel model = SettingsModel.CreateDefault();
            model.ResolutionWidth = 100;
            model.ResolutionHeight = 100;
            model.ResolutionRefreshRate = 5000;
            model.DisplayMode = (FullScreenMode)999;
            model.QualityLevel = 99;
            model.TargetFrameRate = 45;
            model.UiScale = 0.5f;
            model.MasterVolume = -0.5f;
            model.MusicVolume = 1.5f;
            model.SfxVolume = 0.25f;
            model.Language = "xx-unsupported";
            model.DefaultMapSize = (MapSizePreset)99;

            model.Clamp(3);

            Assert.AreEqual(0, model.ResolutionWidth, "过小分辨率应回退为自动（0×0）");
            Assert.AreEqual(0, model.ResolutionHeight, "过小分辨率应回退为自动（0×0）");
            Assert.AreEqual(0, model.ResolutionRefreshRate, "分辨率非法时刷新率也应回退自动（0）");
            Assert.AreEqual(FullScreenMode.FullScreenWindow, model.DisplayMode, "非法显示模式应回退默认");
            Assert.AreEqual(2, model.QualityLevel, "画质等级应夹到 [0, count-1]");
            Assert.AreEqual(30, model.TargetFrameRate, "帧率 45 应夹到最近允许档（平局取较小 30）");
            Assert.AreEqual(0.8f, model.UiScale, 0.0001f, "UI 缩放 0.5 应夹到最近档 0.8");
            Assert.AreEqual(0f, model.MasterVolume, 0.0001f, "音量下限 0");
            Assert.AreEqual(1f, model.MusicVolume, 0.0001f, "音量上限 1");
            Assert.AreEqual(0.25f, model.SfxVolume, 0.0001f, "合法音量保持不变");
            Assert.AreEqual("zh-Hans", model.Language, "非法语言应回退中文");
            Assert.AreEqual(MapSizePreset.Large, model.DefaultMapSize, "非法地图尺寸应回退 Large");
        }

        [Test]
        public void Clamp_KeepsValidValues_AndHandlesNoQualityLevels()
        {
            int qualityCount = SettingsModel.GetQualityLevelCount();
            int validQuality = qualityCount > 0 ? Mathf.Min(qualityCount - 1, 1) : SettingsModel.KeepCurrentQualityLevel;

            SettingsModel model = SettingsModel.CreateDefault();
            model.ResolutionWidth = 1920;
            model.ResolutionHeight = 1080;
            model.DisplayMode = FullScreenMode.Windowed;
            model.VSync = false;
            model.QualityLevel = validQuality;
            model.TargetFrameRate = 144;
            model.UiScale = 1.2f;
            model.MasterVolume = 0.5f;
            model.Language = "en";
            model.DefaultMapSize = MapSizePreset.Huge;

            model.Clamp(qualityCount);

            Assert.AreEqual(1920, model.ResolutionWidth);
            Assert.AreEqual(1080, model.ResolutionHeight);
            Assert.AreEqual(FullScreenMode.Windowed, model.DisplayMode);
            Assert.IsFalse(model.VSync);
            Assert.AreEqual(validQuality, model.QualityLevel);
            Assert.AreEqual(144, model.TargetFrameRate);
            Assert.AreEqual(1.2f, model.UiScale, 0.0001f);
            Assert.AreEqual("en", model.Language);
            Assert.AreEqual(MapSizePreset.Huge, model.DefaultMapSize);

            // 没有画质档（如无图形设备）时回到"保持当前"
            SettingsModel noQuality = SettingsModel.CreateDefault();
            noQuality.QualityLevel = 2;
            noQuality.Clamp(0);
            Assert.AreEqual(SettingsModel.KeepCurrentQualityLevel, noQuality.QualityLevel);

            // -1 = 保持当前，不应被夹成 0
            SettingsModel keepCurrent = SettingsModel.CreateDefault();
            keepCurrent.QualityLevel = SettingsModel.KeepCurrentQualityLevel;
            keepCurrent.Clamp(4);
            Assert.AreEqual(SettingsModel.KeepCurrentQualityLevel, keepCurrent.QualityLevel);
        }

        [Test]
        public void ClampHelpers_SnapToAllowedValues()
        {
            Assert.AreEqual(-1, SettingsModel.ClampFrameRate(-100));
            Assert.AreEqual(30, SettingsModel.ClampFrameRate(45));
            Assert.AreEqual(60, SettingsModel.ClampFrameRate(61));
            Assert.AreEqual(144, SettingsModel.ClampFrameRate(240));

            Assert.AreEqual(0.8f, SettingsModel.ClampUiScale(0.1f), 0.0001f);
            Assert.AreEqual(1.2f, SettingsModel.ClampUiScale(1.3f), 0.0001f);
            Assert.AreEqual(1.5f, SettingsModel.ClampUiScale(9f), 0.0001f);
            Assert.AreEqual(1f, SettingsModel.ClampUiScale(float.NaN), 0.0001f);

            Assert.AreEqual(2, SettingsModel.IndexOfFrameRate(60));
            Assert.AreEqual(0, SettingsModel.IndexOfFrameRate(9999));
            Assert.AreEqual(1, SettingsModel.IndexOfUiScale(1f));
            Assert.AreEqual(1, SettingsModel.IndexOfLanguage("en"));
            Assert.AreEqual(0, SettingsModel.IndexOfLanguage("xx"));
            Assert.AreEqual(2, SettingsModel.IndexOfMapSize(MapSizePreset.Reference));
            Assert.AreEqual(0, SettingsModel.IndexOfMapSize((MapSizePreset)99));
            Assert.AreEqual(0, SettingsModel.IndexOfDisplayMode(FullScreenMode.FullScreenWindow));
            Assert.AreEqual(3, SettingsModel.IndexOfDisplayMode(FullScreenMode.Windowed));
        }

        [Test]
        public void SaveAndLoad_RoundTripsAllPersistedKeys()
        {
            int qualityCount = SettingsModel.GetQualityLevelCount();
            int qualityLevel = qualityCount > 0 ? Mathf.Min(qualityCount - 1, 1) : SettingsModel.KeepCurrentQualityLevel;

            SettingsModel model = SettingsModel.CreateDefault();
            model.ResolutionWidth = 1920;
            model.ResolutionHeight = 1080;
            model.ResolutionRefreshRate = 144;
            model.DisplayMode = FullScreenMode.Windowed;
            model.VSync = false;
            model.QualityLevel = qualityLevel;
            model.TargetFrameRate = 60;
            model.UiScale = 1.2f;
            model.Language = "en";
            model.MasterVolume = 0.25f;
            model.MusicVolume = 0.35f;
            model.SfxVolume = 0.45f;
            model.DefaultMapSize = MapSizePreset.Huge;
            model.Save();

            Assert.IsTrue(SettingsModel.HasSaved(), "Save 后应有保存标记");
            for (int i = 0; i < SettingsModel.AllKeys.Length; i++)
            {
                Assert.IsTrue(PlayerPrefs.HasKey(SettingsModel.AllKeys[i]),
                    $"保存后应存在键 {SettingsModel.AllKeys[i]}");
            }
            Assert.AreEqual(1920, PlayerPrefs.GetInt(SettingsModel.KeyResolutionWidth));
            Assert.AreEqual(1080, PlayerPrefs.GetInt(SettingsModel.KeyResolutionHeight));
            Assert.AreEqual(144, PlayerPrefs.GetInt(SettingsModel.KeyResolutionRefreshRate));
            Assert.AreEqual((int)FullScreenMode.Windowed, PlayerPrefs.GetInt(SettingsModel.KeyDisplayMode));
            Assert.AreEqual(0, PlayerPrefs.GetInt(SettingsModel.KeyVSync));
            Assert.AreEqual(qualityLevel, PlayerPrefs.GetInt(SettingsModel.KeyQualityLevel));
            Assert.AreEqual(60, PlayerPrefs.GetInt(SettingsModel.KeyTargetFrameRate));
            Assert.AreEqual(1.2f, PlayerPrefs.GetFloat(SettingsModel.KeyUiScale), 0.0001f);
            Assert.AreEqual("en", PlayerPrefs.GetString(SettingsModel.KeyLanguage));
            Assert.AreEqual(0.25f, PlayerPrefs.GetFloat(SettingsModel.KeyMasterVolume), 0.0001f);
            Assert.AreEqual(0.35f, PlayerPrefs.GetFloat(SettingsModel.KeyMusicVolume), 0.0001f);
            Assert.AreEqual(0.45f, PlayerPrefs.GetFloat(SettingsModel.KeySfxVolume), 0.0001f);
            Assert.AreEqual((int)MapSizePreset.Huge, PlayerPrefs.GetInt(SettingsModel.KeyDefaultMapSize));

            SettingsModel loaded = SettingsModel.Load();
            Assert.AreEqual(1920, loaded.ResolutionWidth);
            Assert.AreEqual(1080, loaded.ResolutionHeight);
            Assert.AreEqual(144, loaded.ResolutionRefreshRate, "刷新率应随分辨率一起持久化往返");
            Assert.AreEqual(FullScreenMode.Windowed, loaded.DisplayMode);
            Assert.IsFalse(loaded.VSync);
            Assert.AreEqual(qualityLevel, loaded.QualityLevel);
            Assert.AreEqual(60, loaded.TargetFrameRate);
            Assert.AreEqual(1.2f, loaded.UiScale, 0.0001f);
            Assert.AreEqual("en", loaded.Language);
            Assert.AreEqual(0.25f, loaded.MasterVolume, 0.0001f);
            Assert.AreEqual(0.35f, loaded.MusicVolume, 0.0001f);
            Assert.AreEqual(0.45f, loaded.SfxVolume, 0.0001f);
            Assert.AreEqual(MapSizePreset.Huge, loaded.DefaultMapSize);
        }

        [Test]
        public void DeleteSaved_RemovesAllKeysAndMarker()
        {
            SettingsModel model = SettingsModel.CreateDefault();
            model.Language = "en";
            model.MasterVolume = 0.5f;
            model.Save();
            Assert.IsTrue(SettingsModel.HasSaved());

            SettingsModel.DeleteSaved();

            Assert.IsFalse(SettingsModel.HasSaved(), "删除后不应再有保存标记");
            for (int i = 0; i < SettingsModel.AllKeys.Length; i++)
            {
                Assert.IsFalse(PlayerPrefs.HasKey(SettingsModel.AllKeys[i]),
                    $"删除后不应残留键 {SettingsModel.AllKeys[i]}");
            }
            SettingsModel loaded = SettingsModel.Load();
            Assert.AreEqual(0, loaded.ResolutionWidth, "无保存值是默认模型（0×0 自动）");
            Assert.AreEqual("zh-Hans", loaded.Language);
        }

        [Test]
        public void ResetToDefaults_ReturnsDefaultsAndClearsPersistence()
        {
            SettingsModel custom = SettingsModel.CreateDefault();
            custom.Language = "en";
            custom.DefaultMapSize = MapSizePreset.Enormous;
            custom.UiScale = 1.5f;
            custom.Save();
            Assert.IsTrue(SettingsModel.HasSaved());

            SettingsModel reset = SettingsModel.ResetToDefaults();

            Assert.IsFalse(SettingsModel.HasSaved(), "重置默认后不应保留保存标记");
            Assert.AreEqual("zh-Hans", reset.Language);
            Assert.AreEqual(MapSizePreset.Large, reset.DefaultMapSize);
            Assert.AreEqual(1f, reset.UiScale, 0.0001f);
            Assert.AreEqual(0, reset.ResolutionWidth);
            Assert.AreEqual(SettingsModel.DefaultMasterVolume, reset.MasterVolume, 0.0001f);
        }

        [Test]
        public void GetDefaultMapSizePreset_UsesSavedValueWithFallback()
        {
            Assert.AreEqual(MapSizePreset.Large,
                SettingsService.GetDefaultMapSizePreset(MapSizePreset.Large),
                "未保存时应返回场景序列化 fallback");

            SettingsModel model = SettingsModel.CreateDefault();
            model.DefaultMapSize = MapSizePreset.Reference;
            model.Save();
            Assert.AreEqual(MapSizePreset.Reference,
                SettingsService.GetDefaultMapSizePreset(MapSizePreset.Large),
                "已保存时应使用设置面板保存的默认地图尺寸");
        }

        [Test]
        public void BuildResolutionOptions_IsNullSafeDedupesAndSorts()
        {
            var fromNull = SettingsModel.BuildResolutionOptions(null);
            Assert.AreEqual(1, fromNull.Count, "无分辨率列表时至少保留「自动」项（batchmode null 安全）");
            Assert.AreEqual(0, fromNull[0].Width);
            Assert.AreEqual(0, fromNull[0].Height);
            StringAssert.Contains("自动", fromNull[0].Label);

            var fromEmpty = SettingsModel.BuildResolutionOptions(new Resolution[0]);
            Assert.AreEqual(1, fromEmpty.Count, "空分辨率列表时同样 null 安全");

            var resolutions = new[]
            {
                new Resolution { width = 1920, height = 1080 },
                new Resolution { width = 1920, height = 1080 }, // 重复项应去重
                new Resolution { width = 1280, height = 720 },
                new Resolution { width = 320, height = 200 },   // 低于最小分辨率应忽略
                new Resolution { width = 3840, height = 2160 }
            };
            var options = SettingsModel.BuildResolutionOptions(resolutions);

            Assert.AreEqual(4, options.Count, "自动 + 3 个去重后的有效分辨率");
            Assert.AreEqual("3840×2160", options[1].Label, "分辨率应按数值从大到小排序");
            Assert.AreEqual("1920×1080", options[2].Label);
            Assert.AreEqual("1280×720", options[3].Label);

            Assert.AreEqual(2, SettingsModel.FindResolutionIndex(options, 1920, 1080));
            Assert.AreEqual(0, SettingsModel.FindResolutionIndex(options, 0, 0));
            Assert.AreEqual(0, SettingsModel.FindResolutionIndex(options, 9999, 9999), "未知分辨率回退自动项");
            Assert.AreEqual(0, SettingsModel.FindResolutionIndex(null, 1920, 1080), "null 列表回退自动项");
        }

        [Test]
        public void CreateFromRuntime_ReturnsClampedModel()
        {
            SettingsModel model = SettingsModel.CreateFromRuntime();
            Assert.IsNotNull(model);
            Assert.IsTrue(model.MasterVolume >= 0f && model.MasterVolume <= 1f);
            Assert.IsTrue(model.MusicVolume >= 0f && model.MusicVolume <= 1f);
            Assert.IsTrue(model.SfxVolume >= 0f && model.SfxVolume <= 1f);
            Assert.GreaterOrEqual(
                System.Array.IndexOf(SettingsModel.SupportedLanguages, model.Language), 0, "运行时语言应回退到支持列表内");
            Assert.GreaterOrEqual(
                System.Array.IndexOf(SettingsModel.MapSizes, model.DefaultMapSize), 0, "运行时地图尺寸应合法");
            Assert.GreaterOrEqual(
                System.Array.IndexOf(SettingsModel.AllowedUiScales, model.UiScale), 0, "运行时 UI 缩放应合法");
            Assert.GreaterOrEqual(
                System.Array.IndexOf(SettingsModel.AllowedFrameRates, model.TargetFrameRate), 0, "运行时帧率应合法");
            if (model.ResolutionWidth != 0 || model.ResolutionHeight != 0)
            {
                Assert.GreaterOrEqual(model.ResolutionWidth, SettingsModel.MinResolutionWidth);
                Assert.GreaterOrEqual(model.ResolutionHeight, SettingsModel.MinResolutionHeight);
            }
            Assert.GreaterOrEqual(model.ResolutionRefreshRate, 0, "刷新率必须合法（0=自动）");
        }

        [Test]
        public void BuildResolutionOptions_DedupesByRefreshRateAndFindsWithRefreshRate()
        {
            var descriptors = new[]
            {
                new SettingsModel.ResolutionDescriptor { Width = 1920, Height = 1080, RefreshRate = 60 },
                new SettingsModel.ResolutionDescriptor { Width = 1920, Height = 1080, RefreshRate = 144 },
                new SettingsModel.ResolutionDescriptor { Width = 1920, Height = 1080, RefreshRate = 60 }, // 重复项
                new SettingsModel.ResolutionDescriptor { Width = 1920, Height = 1080, RefreshRate = 0 },  // 自动刷新率
                new SettingsModel.ResolutionDescriptor { Width = 1280, Height = 720, RefreshRate = 60 },
                new SettingsModel.ResolutionDescriptor { Width = 320, Height = 200, RefreshRate = 60 }   // 低于最小分辨率
            };
            var options = SettingsModel.BuildResolutionOptionsFromDescriptors(descriptors);

            Assert.AreEqual(5, options.Count, "自动 + 3 个 1920×1080 刷新率变体 + 1 个 1280×720");
            Assert.AreEqual("1920×1080 @144Hz", options[1].Label, "同宽高不同刷新率应按刷新率降序并带 Hz 标签");
            Assert.AreEqual("1920×1080 @60Hz", options[2].Label);
            Assert.AreEqual("1920×1080", options[3].Label, "刷新率 0 的项保持旧标签（自动刷新率）");
            Assert.AreEqual("1280×720 @60Hz", options[4].Label);

            Assert.AreEqual(2, SettingsModel.FindResolutionIndex(options, 1920, 1080, 60));
            Assert.AreEqual(1, SettingsModel.FindResolutionIndex(options, 1920, 1080, 144));
            Assert.AreEqual(3, SettingsModel.FindResolutionIndex(options, 1920, 1080, 0));
            Assert.AreEqual(0, SettingsModel.FindResolutionIndex(options, 0, 0, 0), "0×0 为自动项");
            Assert.AreEqual(0, SettingsModel.FindResolutionIndex(options, 1920, 1080, 30), "未知刷新率回退自动项");
            Assert.AreEqual(3, SettingsModel.FindResolutionIndex(options, 1920, 1080), "旧存档只有宽高时优先回显刷新率 0 的项");

            Assert.AreEqual(0, SettingsModel.GetRefreshRateHz(new Resolution { width = 0, height = 0 }),
                "空 Resolution 的刷新率为 0（batchmode 安全）");
        }
        [Test]
        public void FontChoice_DefaultAndClamp()
        {
            SettingsModel model = SettingsModel.CreateDefault();
            Assert.AreEqual(0, model.FontChoice, "默认界面字体为 0（simhei）");
            Assert.AreEqual("MyriadCreation.Settings.FontChoice", SettingsModel.KeyFontChoice);
            Assert.AreEqual(2, SettingsModel.MaxFontChoice);
            Assert.AreEqual(3, SettingsModel.FontChoiceLabels.Length);

            model.FontChoice = -1;
            model.Clamp(0);
            Assert.AreEqual(0, model.FontChoice, "负数越界应回 0");

            model.FontChoice = 3;
            model.Clamp(0);
            Assert.AreEqual(0, model.FontChoice, "超出 MaxFontChoice 应回 0");

            model.FontChoice = 2;
            model.Clamp(0);
            Assert.AreEqual(2, model.FontChoice, "合法字体选项保留");

            Assert.AreEqual(0, SettingsModel.CreateFromRuntime().FontChoice, "CreateFromRuntime 保持默认 0");
        }

        [Test]
        public void FontChoice_SaveLoadAndAllKeys()
        {
            Assert.IsTrue(System.Array.IndexOf(SettingsModel.AllKeys, SettingsModel.KeyFontChoice) >= 0,
                "AllKeys 必须包含 FontChoice 键");

            SettingsModel model = SettingsModel.CreateDefault();
            model.FontChoice = 2;
            model.Save();

            Assert.IsTrue(PlayerPrefs.HasKey(SettingsModel.KeyFontChoice));
            Assert.AreEqual(2, PlayerPrefs.GetInt(SettingsModel.KeyFontChoice));
            Assert.AreEqual(2, SettingsModel.Load().FontChoice, "字体选项应持久化往返");

            SettingsModel.DeleteSaved();
            Assert.IsFalse(PlayerPrefs.HasKey(SettingsModel.KeyFontChoice), "DeleteSaved 应清理字体键");
            Assert.AreEqual(0, SettingsModel.Load().FontChoice, "无保存值时字体默认 0");
        }
    }
}
