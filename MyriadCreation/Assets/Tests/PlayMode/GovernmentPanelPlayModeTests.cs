using System.Collections;
using System.Text;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using MyriadCreation.Core.Enums;
using MyriadCreation.Rendering;
using MyriadCreation.Simulation.WorldState;
using MyriadCreation.UI;
using MyriadCreation.UI.Common;
using MyriadCreation.UI.Panels;
using MyriadCreation.World.Generation;

namespace MyriadCreation.Tests
{
    /// <summary>
    /// 政府/法律面板接入 PlayMode 验证（task-60）：
    /// 加载 Main → 生成最小世界 → TopBar 上应出现动态「政府」按钮 →
    /// 点击打开面板并显示三个区块标题（已固化的制度/正在生效的法令/正在推行的法令）→
    /// 再点一次关闭；UIManager 公开方法 Open/Close 也能开关。
    /// </summary>
    public class GovernmentPanelPlayModeTests
    {
        [TearDown]
        public void TearDown()
        {
            // 动态面板/世界/渲染器都是运行时创建或 DDOL 对象，不清理会污染后续 PlayMode 测试。
            foreach (var panel in Object.FindObjectsByType<GovernmentPanel>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                if (panel != null) Object.DestroyImmediate(panel.gameObject);
            foreach (var w in Object.FindObjectsByType<GameWorld>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                if (w != null) Object.DestroyImmediate(w.gameObject);
            foreach (var r in Object.FindObjectsByType<MapRenderer>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                if (r != null) Object.DestroyImmediate(r.gameObject);
            foreach (var f in Object.FindObjectsByType<SceneFlowController>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                if (f != null) Object.DestroyImmediate(f.gameObject);
            foreach (var gm in Object.FindObjectsByType<GameManager>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                if (gm != null) Object.DestroyImmediate(gm.gameObject);
            // 大世界销毁后主动回收，避免后续测试分配触发全堆扫描。
            System.GC.Collect();
        }

        private static Button FindGovernmentButton()
        {
            Button[] buttons = Object.FindObjectsByType<Button>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            for (int i = 0; i < buttons.Length; i++)
            {
                TMPro.TMP_Text label = buttons[i].GetComponentInChildren<TMPro.TMP_Text>(true);
                if (label != null && label.text == "政府") return buttons[i];
            }
            return null;
        }

        private static string CollectLegacyText(GameObject root)
        {
            var builder = new StringBuilder(1024);
            Text[] texts = root.GetComponentsInChildren<Text>(true);
            for (int i = 0; i < texts.Length; i++)
            {
                builder.AppendLine(texts[i].text);
            }
            return builder.ToString();
        }

        [UnityTest]
        public IEnumerator GovernmentPanel_TopBarButton_OpensWithThreeSectionTitles_AndCloses()
        {
            SceneManager.LoadScene("Main");
            yield return null;
            yield return null;

            var gameManager = Object.FindAnyObjectByType<GameManager>();
            Assert.IsNotNull(gameManager, "场景中应有 GameManager");

            // 最小世界（1 政权 + 5 聚落），保证法政面板有数据源。
            gameManager.StartNewGame(64, 32, 12345, MapWrapMode.Cylindrical,
                WorldSetupOptions.Create(InitialContentMode.Minimal));
            yield return null;
            yield return null;

            var world = Object.FindAnyObjectByType<GameWorld>();
            Assert.IsNotNull(world, "应生成/复用 GameWorld");
            Assert.Greater(world.realms.Count, 0, "前置：应有政权数据（政府面板需要法政状态）");

            var ui = Object.FindAnyObjectByType<UIManager>();
            Assert.IsNotNull(ui, "场景中应有 UIManager");

            // 等 UIManager.Start/InitializeUI 跑完并动态创建 TopBar「政府」按钮。
            Button govButton = FindGovernmentButton();
            float wait = 0f;
            while (govButton == null && wait < 5f)
            {
                yield return null;
                wait += Time.unscaledDeltaTime;
                govButton = FindGovernmentButton();
            }
            Assert.IsNotNull(govButton, "TopBar 应动态创建「政府」按钮（代码创建，不改场景）");
            Assert.AreEqual("TopBar", govButton.transform.parent != null ? govButton.transform.parent.name : "",
                "「政府」按钮应挂在 TopBar 下");

            // 点击打开
            govButton.onClick.Invoke();
            yield return null;

            var panel = Object.FindAnyObjectByType<GovernmentPanel>(FindObjectsInactive.Include);
            Assert.IsNotNull(panel, "点击「政府」后应懒创建 GovernmentPanel");
            Assert.IsTrue(panel.IsOpen, "点击「政府」后政府面板应打开");

            string allText = CollectLegacyText(panel.gameObject);
            StringAssert.Contains("已固化的制度", allText, "面板应显示区块：已固化的制度");
            StringAssert.Contains("正在生效的法令", allText, "面板应显示区块：正在生效的法令");
            StringAssert.Contains("正在推行的法令", allText, "面板应显示区块：正在推行的法令");

            // 再点一次（toggle）关闭
            govButton.onClick.Invoke();
            yield return null;
            Assert.IsFalse(panel.IsOpen, "再次点击「政府」应关闭面板");

            // UIManager 公开方法也可打开/关闭（外部系统/快捷键入口复用）
            ui.OpenGovernmentPanel();
            yield return null;
            Assert.IsTrue(panel.IsOpen, "UIManager.OpenGovernmentPanel 应能打开面板");
            ui.CloseGovernmentPanel();
            yield return null;
            Assert.IsFalse(panel.IsOpen, "UIManager.CloseGovernmentPanel 应能关闭面板");
        }

        /// <summary>
        /// 顶栏「政府」按钮窄屏布局（task-62）：强制 TopBar 宽度模拟 1280×720 / 800×600，
        /// 断言宽度 ≥64（宽屏 86/窄屏 64）、不越出 TopBar 左右边界、不与 CharOpenBtn 重叠；
        /// 窄屏字号 12、宽屏 13。
        /// </summary>
        [UnityTest]
        public IEnumerator GovernmentPanel_TopBarButton_NarrowLayout_StaysInsideBar_AndClearsCharOpenButton()
        {
            SceneManager.LoadScene("Main");
            yield return null;
            yield return null;

            var gameManager = Object.FindAnyObjectByType<GameManager>();
            Assert.IsNotNull(gameManager, "场景中应有 GameManager");
            gameManager.StartNewGame(64, 32, 12345, MapWrapMode.Cylindrical,
                WorldSetupOptions.Create(InitialContentMode.Minimal));
            yield return null;
            yield return null;

            var ui = Object.FindAnyObjectByType<UIManager>();
            Assert.IsNotNull(ui, "场景中应有 UIManager");

            Button govButton = FindGovernmentButton();
            float wait = 0f;
            while (govButton == null && wait < 5f)
            {
                yield return null;
                wait += Time.unscaledDeltaTime;
                govButton = FindGovernmentButton();
            }
            Assert.IsNotNull(govButton, "TopBar 应动态创建「政府」按钮");

            GameObject topBarGo = GameObject.Find("TopBar");
            Assert.IsNotNull(topBarGo, "场景中应有 TopBar");
            var topBar = (RectTransform)topBarGo.transform;
            Transform charTransform = topBar.Find("CharOpenBtn");
            Assert.IsNotNull(charTransform, "TopBar 应包含 CharOpenBtn");
            var charRect = (RectTransform)charTransform;
            var govRect = (RectTransform)govButton.transform;
            var govLabel = govButton.GetComponentInChildren<TMPro.TMP_Text>(true);
            Assert.IsNotNull(govLabel, "「政府」按钮应有文字");

            ApplyAndAssertLayout(ui, govRect, govLabel, topBar, charRect, 1280, false);
            ApplyAndAssertLayout(ui, govRect, govLabel, topBar, charRect, 800, true);
            // 再恢复宽屏，验证恢复路径（宽度/字号/内边距还原）
            ApplyAndAssertLayout(ui, govRect, govLabel, topBar, charRect, 1280, false);
        }

        private static void ApplyAndAssertLayout(UIManager ui, RectTransform govRect, TMPro.TMP_Text govLabel,
            RectTransform topBar, RectTransform charRect, int screenWidth, bool expectNarrow)
        {
            // 模拟目标分辨率：强制 TopBar 宽度（场景 TopBar 横向拉伸；SetSizeWithCurrentAnchors 后 rect.width=目标值）
            topBar.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, screenWidth);
            ui.RefreshGovernmentButtonLayout(screenWidth);
            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate(topBar);

            float width = govRect.rect.width;
            Assert.IsTrue(width >= 64f - 0.5f,
                $"screenWidth={screenWidth} 时政府按钮宽度应 ≥64（实际 {width}）");
            Assert.AreEqual(expectNarrow ? 64f : 86f, width, 0.5f,
                $"screenWidth={screenWidth} 时政府按钮宽度应为 {(expectNarrow ? "64（窄屏）" : "86（宽屏）")}");
            Assert.AreEqual(expectNarrow ? 12 : 13, govLabel.fontSize, 0.01f,
                $"screenWidth={screenWidth} 时字号应为 {(expectNarrow ? 12 : 13)}");

            var govCorners = new Vector3[4];
            govRect.GetWorldCorners(govCorners);
            var barCorners = new Vector3[4];
            topBar.GetWorldCorners(barCorners);
            Assert.IsTrue(govCorners[0].x >= barCorners[0].x - 1f,
                $"screenWidth={screenWidth} 时政府按钮不应越出 TopBar 左边界");
            Assert.IsTrue(govCorners[2].x <= barCorners[2].x + 1f,
                $"screenWidth={screenWidth} 时政府按钮不应越出 TopBar 右边界");

            var charCorners = new Vector3[4];
            charRect.GetWorldCorners(charCorners);
            Assert.IsTrue(govCorners[0].x >= charCorners[2].x - 1f,
                $"screenWidth={screenWidth} 时政府按钮不应与 CharOpenBtn 重叠（应在其右侧）");
        }
    }
}
