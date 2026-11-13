using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;
using MyriadCreation.Core.Data;
using MyriadCreation.Infrastructure.Save;
using MyriadCreation.Simulation.Units;
using MyriadCreation.Simulation.WorldState;
using MyriadCreation.UI.Panels;
using MyriadCreation.World.Generation;

namespace MyriadCreation.Tests
{
    /// <summary>
    /// 存档面板"删除存档"PlayMode 交互（task-33 删除入口的补测）：
    /// 删除需二次确认；第一次点击不删档；第二次真删；全程不得触发"加载存档"回调（防误触父条目）。
    /// </summary>
    public class SaveLoadPanelPlayModeTests
    {
        private const string SaveName = "__ui_delete_panel_test";
        private GameObject _canvas;
        private GameObject _panelObj;
        private GameWorld _world;

        [TearDown]
        public void TearDown()
        {
            if (_panelObj != null) Object.DestroyImmediate(_panelObj);
            if (_canvas != null) Object.DestroyImmediate(_canvas);
            if (_world != null) Object.DestroyImmediate(_world.gameObject);
            SaveSystem.DeleteSave(SaveName);
            MapUnitRegistry.Clear();
        }

        private void CreateSaveFile()
        {
            var go = new GameObject("SavePanelTestWorld");
            _world = go.AddComponent<GameWorld>();
            _world.mapWidth = 24;
            _world.mapHeight = 12;
            _world.SetupOptions = WorldSetupOptions.CreateStandard();
            _world.InitializeWorld();
            _world.GenerateTerrain(7);
            Assert.IsTrue(SaveSystem.SaveGame(_world, SaveName), "前置：测试存档应写入成功");
        }

        [UnityTest]
        public IEnumerator SaveLoadPanel_DeleteNeedsConfirm_AndNeverTriggersLoad()
        {
            CreateSaveFile();
            string path = System.IO.Path.Combine(SaveSystem.GetSaveDirectory(), SaveName + ".sav");
            Assert.IsTrue(System.IO.File.Exists(path), "前置：存档文件应存在");

            int loadCalls = 0;
            _canvas = new GameObject("TestCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            _canvas.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;

            _panelObj = new GameObject("SaveLoadPanel", typeof(RectTransform));
            _panelObj.transform.SetParent(_canvas.transform, false);
            var panel = _panelObj.AddComponent<SaveLoadPanel>();
            panel.Build(_canvas.transform, _ => loadCalls++, () => { });
            panel.Show();
            yield return null;

            // 找到本测试存档条目的删除按钮（按钮文本为"删除"）
            Button deleteButton = null;
            foreach (var b in _canvas.GetComponentsInChildren<Button>(true))
            {
                var t = b.GetComponentInChildren<TMPro.TMP_Text>(true);
                if (t != null && t.text == "删除") { deleteButton = b; break; }
            }
            Assert.IsNotNull(deleteButton, "存档条目应有「删除」按钮");

            // 第一次点击：只进入确认态，不删档、不触发加载
            deleteButton.onClick.Invoke();
            yield return null;
            Assert.IsTrue(System.IO.File.Exists(path), "第一次点删除不应真的删档（应进入确认态）");
            var label = deleteButton.GetComponentInChildren<TMPro.TMP_Text>(true);
            Assert.AreEqual("确认删除", label.text, "第一次点删除后按钮文案应为「确认删除」");
            Assert.AreEqual(0, loadCalls, "删除操作不得触发加载存档");

            // 第二次点击：真删档，仍不触发加载
            deleteButton.onClick.Invoke();
            yield return null;
            Assert.IsFalse(System.IO.File.Exists(path), "第二次点「确认删除」应真的删除存档文件");
            Assert.AreEqual(0, loadCalls, "删除操作不得触发加载存档");

            Debug.Log("[SaveLoadPanelTest] 二次确认删除流程通过（未触发加载）");
        }
    }
}
