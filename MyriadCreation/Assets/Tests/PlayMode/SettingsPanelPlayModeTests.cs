using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;
using TMPro;
using MyriadCreation.UI.Panels;
using MyriadCreation.App.Settings;

namespace MyriadCreation.Tests
{
    /// <summary>
    /// 设置面板 PlayMode 冒烟（task-53）：代码构建完整设置面板 → 存在分辨率下拉/音量滑条等控件 →
    /// 点「应用」后 PlayerPrefs（MyriadCreation.Settings.*）有值且主音量落到 AudioListener →
    /// 重新打开面板读取已保存值；batchmode 无图形设备（Screen.resolutions 为空）时分辨率下拉仍 null 安全。
    /// </summary>
    public class SettingsPanelPlayModeTests
    {
        private GameObject _canvas;
        private GameObject _panelObj;

        [SetUp]
        public void SetUp()
        {
            SettingsModel.DeleteSaved();
        }

        [TearDown]
        public void TearDown()
        {
            if (_panelObj != null)
            {
                Object.DestroyImmediate(_panelObj);
            }
            if (_canvas != null)
            {
                Object.DestroyImmediate(_canvas);
            }
            AudioListener.volume = 1f;
            SettingsModel.DeleteSaved();
        }

        private static T FindByName<T>(T[] items, string name) where T : Component
        {
            for (int i = 0; i < items.Length; i++)
            {
                if (items[i] != null && items[i].gameObject.name == name)
                {
                    return items[i];
                }
            }
            return null;
        }

        private SettingsPanel CreatePanel()
        {
            _canvas = new GameObject("TestCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            _canvas.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;

            _panelObj = new GameObject("SettingsPanel", typeof(RectTransform));
            _panelObj.transform.SetParent(_canvas.transform, false);
            SettingsPanel panel = _panelObj.AddComponent<SettingsPanel>();
            bool closed = false;
            panel.Build(_canvas.transform, () => closed = true);
            panel.Show();
            _closedState = () => closed;
            return panel;
        }

        private System.Func<bool> _closedState;

        [UnityTest]
        public IEnumerator SettingsPanel_BuildsControls_AndAppliesToPlayerPrefs()
        {
            SettingsPanel panel = CreatePanel();
            yield return null;

            TMP_Dropdown[] dropdowns = _canvas.GetComponentsInChildren<TMP_Dropdown>(true);
            Slider[] sliders = _canvas.GetComponentsInChildren<Slider>(true);
            Toggle[] toggles = _canvas.GetComponentsInChildren<Toggle>(true);
            Assert.GreaterOrEqual(dropdowns.Length, 7,
                "设置面板应包含 分辨率/显示模式/画质/帧率/UI缩放/语言/地图尺寸 等下拉");
            Assert.GreaterOrEqual(sliders.Length, 3, "设置面板应包含主音量/音乐音量/音效音量 3 条滑条");
            Assert.GreaterOrEqual(toggles.Length, 1, "设置面板应包含垂直同步开关");

            TMP_Dropdown resolution = FindByName(dropdowns, "分辨率");
            Assert.IsNotNull(resolution, "应有分辨率下拉");
            Assert.Greater(resolution.options.Count, 0,
                "batchmode 无分辨率时应保留「自动（当前分辨率）」项（null 安全）");
            StringAssert.Contains("自动", resolution.options[0].text);

            TMP_Dropdown displayMode = FindByName(dropdowns, "显示模式");
            Assert.IsNotNull(displayMode, "应有显示模式下拉");
            TMP_Dropdown language = FindByName(dropdowns, "语言");
            Assert.IsNotNull(language, "应有语言下拉");
            Assert.GreaterOrEqual(language.options.Count, 2, "语言至少包含 简体中文/English");
            TMP_Dropdown mapSize = FindByName(dropdowns, "新游戏默认地图尺寸");
            Assert.IsNotNull(mapSize, "应有新游戏默认地图尺寸下拉");
            Assert.GreaterOrEqual(mapSize.options.Count, 4, "默认地图尺寸应包含 4 档");

            Slider master = FindByName(sliders, "主音量");
            Slider music = FindByName(sliders, "音乐音量");
            Slider sfx = FindByName(sliders, "音效音量");
            Assert.IsNotNull(master, "应有主音量滑条");
            Assert.IsNotNull(music, "应有音乐音量滑条");
            Assert.IsNotNull(sfx, "应有音效音量滑条");

            // 分辨率切到「自动」，避免 PlayMode 测试真的改窗口分辨率
            resolution.value = 0;
            master.value = 0.35f;
            music.value = 0.45f;
            sfx.value = 0.55f;

            Button apply = null;
            Button[] buttons = _canvas.GetComponentsInChildren<Button>(true);
            for (int i = 0; i < buttons.Length; i++)
            {
                TMP_Text label = buttons[i].GetComponentInChildren<TMP_Text>(true);
                if (label != null && label.text == "应用")
                {
                    apply = buttons[i];
                    break;
                }
            }
            Assert.IsNotNull(apply, "应有「应用」按钮");
            apply.onClick.Invoke();
            yield return null;

            Assert.IsTrue(_closedState != null && _closedState(), "点「应用」后应触发关闭回调");
            Assert.IsTrue(SettingsModel.HasSaved(), "点「应用」后设置应已持久化（MyriadCreation.Settings.*）");
            Assert.IsTrue(PlayerPrefs.HasKey(SettingsModel.KeyInitialized), "应有设置初始化标记键");
            Assert.AreEqual(0.35f, PlayerPrefs.GetFloat(SettingsModel.KeyMasterVolume), 0.01f,
                "主音量应写入 PlayerPrefs");
            Assert.AreEqual(0.45f, PlayerPrefs.GetFloat(SettingsModel.KeyMusicVolume), 0.01f,
                "音乐音量应写入 PlayerPrefs");
            Assert.AreEqual(0.55f, PlayerPrefs.GetFloat(SettingsModel.KeySfxVolume), 0.01f,
                "音效音量应写入 PlayerPrefs");
            Assert.AreEqual(0.35f, AudioListener.volume, 0.01f, "主音量应应用到 AudioListener");

            // 重新打开面板：应读取已保存的当前值
            panel.Show();
            yield return null;
            Assert.AreEqual(0.35f, master.value, 0.01f, "重新打开面板应显示已保存的主音量");
            Assert.AreEqual(0.45f, music.value, 0.01f, "重新打开面板应显示已保存的音乐音量");
        }

        [UnityTest]
        public IEnumerator SettingsPanel_ResetButton_RestoresDefaults()
        {
            CreatePanel();
            yield return null;

            Slider[] sliders = _canvas.GetComponentsInChildren<Slider>(true);
            Slider master = FindByName(sliders, "主音量");
            Assert.IsNotNull(master, "应有主音量滑条");
            master.value = 0.2f;

            Button reset = null;
            Button[] buttons = _canvas.GetComponentsInChildren<Button>(true);
            for (int i = 0; i < buttons.Length; i++)
            {
                TMP_Text label = buttons[i].GetComponentInChildren<TMP_Text>(true);
                if (label != null && label.text == "重置默认")
                {
                    reset = buttons[i];
                    break;
                }
            }
            Assert.IsNotNull(reset, "应有「重置默认」按钮");
            reset.onClick.Invoke();
            yield return null;

            Assert.AreEqual(1f, master.value, 0.01f, "重置默认后主音量应回到默认 100%");
            Assert.AreEqual(1f, PlayerPrefs.GetFloat(SettingsModel.KeyMasterVolume), 0.01f,
                "重置默认后应把默认值写回 PlayerPrefs");
            Assert.IsTrue(SettingsModel.HasSaved(), "重置默认后仍应保留保存标记");
        }
    }
}
