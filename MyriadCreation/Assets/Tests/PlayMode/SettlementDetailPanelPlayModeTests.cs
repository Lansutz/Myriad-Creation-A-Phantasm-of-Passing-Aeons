using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using MyriadCreation.Core.Data;
using MyriadCreation.Simulation.Systems;
using MyriadCreation.Simulation.Units;
using MyriadCreation.Simulation.WorldState;
using MyriadCreation.UI.Common;
using MyriadCreation.UI.Panels;
using MyriadCreation.World.Generation;
using MyriadCreation.World.Settlement;

namespace MyriadCreation.Tests
{
    /// <summary>
    /// 聚落详情面板 PlayMode 冒烟（UI 2 / task-56）：直接构造含聚落的小世界 →
    /// Build 默认隐藏 → Show(anchorId) 面板激活且标题含聚落名、正文含人口/财富/市场库存等字段 →
    /// Hide 后不可见 → 不存在的 anchorId 不抛异常并显示"无数据"。
    /// 同时验证自建 Canvas 挂了 UiScaleBinder（动态 Canvas 应用已保存 UI 缩放）。
    /// </summary>
    public class SettlementDetailPanelPlayModeTests
    {
        private GameObject _worldGo;
        private GameWorld _world;
        private GameObject _panelGo;
        private SettlementDetailPanel _panel;

        [TearDown]
        public void TearDown()
        {
            if (_panelGo != null)
            {
                Object.DestroyImmediate(_panelGo);
            }
            if (_worldGo != null)
            {
                Object.DestroyImmediate(_worldGo);
            }
            MapUnitRegistry.Clear();
            CharacterKnowledgeRegistry.Clear();
            RelationRegistry.Clear();
            ContractRegistry.Clear();
            System.GC.Collect();
        }

        [UnityTest]
        public IEnumerator SettlementDetailPanel_ShowHide_AndInvalidAnchorIsSafe()
        {
            // 构造最小世界（Minimal：1 政权 + 5 聚落），不加载 Main 场景，避免 PlayMode 场景时序 flaky。
            _worldGo = new GameObject("SettlementDetailTestWorld");
            _world = _worldGo.AddComponent<GameWorld>();
            _world.mapWidth = 64;
            _world.mapHeight = 32;
            _world.randomSeed = 12345;
            _world.SetupOptions = WorldSetupOptions.Create(InitialContentMode.Minimal);
            _world.InitializeWorld();
            _world.GenerateTerrain(12345);
            yield return null;

            Assert.IsNotNull(_world.settlements, "世界应有聚落字典");
            Assert.Greater(_world.settlements.Count, 0, "前置：小世界应生成聚落（否则详情面板无从验证）");

            SettlementData target = null;
            int anchorId = -1;
            foreach (var pair in _world.settlements)
            {
                if (pair.Value != null && !string.IsNullOrEmpty(pair.Value.siteName))
                {
                    target = pair.Value;
                    anchorId = pair.Key;
                    break;
                }
            }
            Assert.IsNotNull(target, "前置：应至少有一个带名称的聚落");
            Assert.GreaterOrEqual(anchorId, 0, "锚点 id 应有效");

            _panelGo = new GameObject("SettlementDetailPanelTest");
            _panel = _panelGo.AddComponent<SettlementDetailPanel>();
            _panel.BindWorld(new WorldReadModel(_world));
            _panel.Build(null);

            Assert.IsFalse(_panel.IsOpen, "Build 后面板默认应隐藏");
            Assert.IsNotNull(_panelGo.GetComponentInChildren<UiScaleBinder>(true),
                "面板自建 Canvas 应挂 UiScaleBinder（动态 Canvas 应用已保存 UI 缩放）");

            _panel.Show(anchorId);
            yield return null;

            Assert.IsTrue(_panel.IsOpen, "Show(有效 anchorId) 后面板应打开");
            Assert.AreEqual(anchorId, _panel.CurrentAnchorId);
            StringAssert.Contains(target.siteName, _panel.TitleText, "标题应显示聚落名称");
            StringAssert.Contains("所属政权", _panel.BodyText, "正文应显示所属政权");
            StringAssert.Contains("守军", _panel.BodyText, "正文应显示守军");
            StringAssert.Contains("首都", _panel.BodyText, "正文应显示首都是否");

            _panel.Hide();
            yield return null;
            Assert.IsFalse(_panel.IsOpen, "Hide 后面板应不可见");

            // 不存在的 anchorId：不抛异常，打开并显示"无数据"
            Assert.DoesNotThrow(() => _panel.Show(-999), "不存在的 anchorId 不应抛异常");
            yield return null;
            Assert.IsTrue(_panel.IsOpen, "无效 anchorId 也应打开面板显示无数据");
            StringAssert.Contains("未找到", _panel.BodyText, "无效 anchorId 应显示无数据提示");

            _panel.Hide();
            yield return null;
            Assert.IsFalse(_panel.IsOpen, "再次 Hide 后面板应不可见");
        }
    }
}
