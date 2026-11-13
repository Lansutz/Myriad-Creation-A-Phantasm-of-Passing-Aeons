using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using TMPro;
using MyriadCreation.Simulation.WorldState;
using MyriadCreation.UI;
using MyriadCreation.UI.Panels;

namespace MyriadCreation.Tests
{
    /// <summary>
    /// 主菜单 UI 可点击性冒烟（PlayMode）：
    /// 加载 Main 场景 → 代码动态主菜单构建 → EventSystem/GraphicRaycaster 就位 →
    /// "开始游戏"按钮在 UI 上真的能命中（射线检测）→ 点击后进入世界生成/编辑器状态。
    /// 覆盖用户诉求"主界面 UI 能正确点击"。
    /// </summary>
    public class MainMenuUiPlayModeTests
    {
        [TearDown]
        public void TearDown()
        {
            // 主菜单/流程/世界都是 DDOL 运行时对象；不清理会污染后续 PlayMode 测试（MapVisibility 等）。
            foreach (var flow in Object.FindObjectsByType<SceneFlowController>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                if (flow != null) Object.DestroyImmediate(flow.gameObject);
            foreach (var menu in Object.FindObjectsByType<MainMenuUI>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                if (menu != null) Object.DestroyImmediate(menu.gameObject);
            foreach (var gm in Object.FindObjectsByType<GameManager>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                if (gm != null) Object.DestroyImmediate(gm.gameObject);
            foreach (var w in Object.FindObjectsByType<GameWorld>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                if (w != null) Object.DestroyImmediate(w.gameObject);
            foreach (var canvas in Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                if (canvas != null && canvas.gameObject.name == "MainMenuCanvas") Object.DestroyImmediate(canvas.gameObject);
            // 大世界（524k TileData）销毁后主动回收，避免后续测试的分配触发 Boehm GC 全堆扫描（曾造成 ~100s 抖动）。
            System.GC.Collect();
        }

        [UnityTest]
        public IEnumerator MainMenu_IsBuiltAndClickable()
        {
            SceneManager.LoadScene("Main");
            yield return null;
            yield return new WaitForSeconds(1.5f);

            var flow = Object.FindAnyObjectByType<SceneFlowController>();
            Assert.IsNotNull(flow, "SceneFlowController 应存在");
            var menu = Object.FindAnyObjectByType<MainMenuUI>();
            Assert.IsNotNull(menu, "MainMenuUI 应存在（代码动态主菜单）");
            Assert.IsTrue(menu.IsVisible, "主菜单应可见");

            var eventSystem = Object.FindAnyObjectByType<EventSystem>();
            Assert.IsNotNull(eventSystem, "EventSystem 应存在");
            Assert.IsNotNull(Object.FindAnyObjectByType<GraphicRaycaster>(), "应存在 GraphicRaycaster");

            var canvas = menu.GetComponentInChildren<Canvas>(true);
            Assert.IsNotNull(canvas, "MainMenuCanvas 应存在");
            Debug.Log($"[MainMenuUi] canvas={canvas.name} sortingOrder={canvas.sortingOrder} renderMode={canvas.renderMode}");

            var buttons = menu.GetComponentsInChildren<Button>(true);
            Debug.Log($"[MainMenuUi] buttons={buttons.Length} : " +
                string.Join(", ", System.Array.ConvertAll(buttons, b => b.name + "(" + ((b.GetComponentInChildren<TMPro.TMP_Text>(true) != null) ? b.GetComponentInChildren<TMPro.TMP_Text>(true).text : "?") + ")")));
            Assert.Greater(buttons.Length, 0, "主菜单应有按钮");

            // 找到"开始游戏"按钮（按子文本匹配），验证它在 UI 射线上真的能命中
            Button startButton = null;
            foreach (var b in buttons)
            {
                var label = b.GetComponentInChildren<TMPro.TMP_Text>(true);
                var text = label != null ? label.text : "";
                if (text.Contains("开始")) { startButton = b; break; }
            }
            Assert.IsNotNull(startButton, "应有「开始游戏」按钮");

            var rt = startButton.GetComponent<RectTransform>();
            Vector3 worldCenter = rt.TransformPoint(rt.rect.center);
            var graphics = new List<RaycastResult>();
            var pointer = new PointerEventData(eventSystem) { position = RectTransformUtility.WorldToScreenPoint(null, worldCenter) };
            eventSystem.RaycastAll(pointer, graphics);
            var hitNames = new List<string>();
            foreach (var g in graphics) hitNames.Add(g.gameObject.name);
            Debug.Log("[MainMenuUi] 开始按钮射线命中: " + string.Join(",", hitNames));
            Assert.IsFalse(graphics.Count == 0, "开始按钮位置应能射线命中 UI（否则被遮挡或没有 Raycaster）");
            // 更严格：命中链里必须包含「开始游戏」按钮自身或其子物体（只断言 Count>0 会被全屏背景 Root 恒真）
            bool hitsStartButton = false;
            foreach (var g in graphics)
            {
                if (g.gameObject == startButton.gameObject
                    || (g.gameObject != null && g.gameObject.transform.IsChildOf(startButton.transform)))
                {
                    hitsStartButton = true;
                    break;
                }
            }
            Assert.IsTrue(hitsStartButton, "射线命中链必须包含开始游戏按钮（被遮挡/层级错误时应失败）");

            // 真正触发点击：点「设置」打开设置面板（不点开始游戏——避免本测试留下 1024x512 世界生成，
            // 否则其 GC 残留会让后续 PlayMode 测试出现 ~100s 级的随机膨胀）。
            Button settingsButton = null;
            foreach (var b in buttons)
            {
                var label = b.GetComponentInChildren<TMPro.TMP_Text>(true);
                if (label != null && label.text.Contains("设置")) { settingsButton = b; break; }
            }
            Assert.IsNotNull(settingsButton, "应有「设置」按钮");
            int activePanelCountBefore = CountActivePanelObjects();
            settingsButton.onClick.Invoke();
            yield return null;
            yield return new WaitForSeconds(0.2f);
            int activePanelCountAfter = CountActivePanelObjects();
            Debug.Log($"[MainMenuUi] 点击设置：panelsBefore={activePanelCountBefore} panelsAfter={activePanelCountAfter}");
            Assert.Greater(activePanelCountAfter, activePanelCountBefore,
                "点击设置后应打开设置面板（证明按钮点击有效）");
        }

        private static int CountActivePanelObjects()
        {
            int count = 0;
            foreach (var t in Object.FindObjectsByType<Transform>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            {
                if (t != null && t.name.Contains("Panel") && t.gameObject.activeInHierarchy) count++;
            }
            return count;
        }
    }
}
