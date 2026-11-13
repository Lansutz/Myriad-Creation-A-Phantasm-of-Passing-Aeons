using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using MyriadCreation.Core;
using MyriadCreation.Core.Enums;
using MyriadCreation.Rendering;
using MyriadCreation.Simulation.Generation;
using MyriadCreation.Simulation.WorldState;
using MyriadCreation.UI;
using MyriadCreation.UI.Common;
using MyriadCreation.UI.Panels;
using MyriadCreation.World.Generation;
using MyriadCreation.World.Settlement;

namespace MyriadCreation.Tests
{
    /// <summary>
    /// 聚落锚点图标层 PlayMode 验证（Map 1）：
    /// 生成含聚落的小世界 → 图标数 &gt; 0 → LOD 近景全显/远景仅大城市 →
    /// 点击（SelectAtScreen）选中高亮 **并打开聚落详情面板**、关闭后 IsOpen=false。
    /// </summary>
    public class SettlementIconLayerPlayModeTests
    {
        [TearDown]
        public void TearDown()
        {
            // 与 MapVisibilityPlayModeTests 同款清理：销毁测试生成的大世界/渲染对象，避免 524k TileData 泄漏。
            foreach (var w in Object.FindObjectsByType<GameWorld>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                if (w != null) Object.DestroyImmediate(w.gameObject);
            foreach (var r in Object.FindObjectsByType<MapRenderer>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                if (r != null) Object.DestroyImmediate(r.gameObject);
            foreach (var f in Object.FindObjectsByType<SceneFlowController>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                if (f != null) Object.DestroyImmediate(f.gameObject);
            foreach (var p in Object.FindObjectsByType<SettlementDetailPanel>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                if (p != null) Object.DestroyImmediate(p.gameObject);
            System.GC.Collect();
        }

        [UnityTest]
        public IEnumerator SettlementIcons_Lod_Select_OpensDetailPanel()
        {
            LogAssert.ignoreFailingMessages = false;
            SceneManager.LoadScene("Main");
            yield return null;
            yield return null;

            var gameManager = Object.FindAnyObjectByType<GameManager>();
            Assert.IsNotNull(gameManager, "场景中应有 GameManager");

            var world = Object.FindAnyObjectByType<GameWorld>();
            Assert.IsNotNull(world, "场景中应有 GameWorld");

            // 用"最小内容"预设：1 政权 + 5 聚落（默认 CreateDefault 不生成聚落）。
            var options = WorldSetupOptions.Create(InitialContentMode.Minimal);
            gameManager.StartNewGame(64, 32, 12345, MapWrapMode.Cylindrical, options);

            float wait = 0f;
            while (wait < 60f)
            {
                world = Object.FindAnyObjectByType<GameWorld>();
                if (world != null && world.GetLandTileCount() > 0 && world.settlements != null && world.settlements.Count > 0)
                    break;
                yield return null;
                wait += Time.deltaTime;
            }
            Assert.IsNotNull(world, "世界生成完成后应存在");
            Assert.Greater(world.GetLandTileCount(), 0, "应生成陆地");
            Assert.Greater(world.settlements.Count, 0, "应生成聚落（否则图标层无从验证）");

            var mapRenderer = Object.FindAnyObjectByType<MapRenderer>();
            Assert.IsNotNull(mapRenderer, "场景中应有 MapRenderer（MapPlane）");
            var camera = Camera.main;
            Assert.IsNotNull(camera, "场景中应有 Main Camera");

            // 等 MapRenderer 把地图尺寸从场景默认值重同步到新世界（64×32），
            // 否则 TileToWorld 仍按旧尺寸换算，图标位置/点击坐标会偏。
            wait = 0f;
            while ((mapRenderer.MapWidth != world.mapWidth || mapRenderer.MapHeight != world.mapHeight) && wait < 15f)
            {
                yield return null;
                wait += Time.deltaTime;
                world = Object.FindAnyObjectByType<GameWorld>();
            }
            Assert.AreEqual(world.mapWidth, mapRenderer.MapWidth, "MapRenderer 应已同步新世界地图宽度");
            Assert.AreEqual(world.mapHeight, mapRenderer.MapHeight, "MapRenderer 应已同步新世界地图高度");
            // 至少再等 2 帧：让 MapRenderer 尺寸重同步触发的图标层重建完成
            yield return null;
            yield return null;

            // 等 MapRenderer.Start 建立坐标/相机、图标层代码创建并完成首轮重建。
            var layer = Object.FindAnyObjectByType<SettlementIconLayer>();
            wait = 0f;
            while ((layer == null || layer.IconCount <= 0) && wait < 15f)
            {
                yield return null;
                wait += Time.deltaTime;
                layer = Object.FindAnyObjectByType<SettlementIconLayer>();
            }
            Assert.IsNotNull(layer, "MapRenderer/BindWorld 应代码创建 SettlementIconLayer");
            Assert.Greater(layer.IconCount, 0, "聚落图标数应 > 0");

            // 图标数 = 有有效锚点地块的聚落数（不等于聚落总数：个别聚落可能无 anchor）
            int expectedIcons = CountSettlementsWithAnchor(world);
            Assert.AreEqual(expectedIcons, layer.IconCount, "图标数应与有效锚点聚落数一致");

            // ===== LOD：近景全显示 =====
            camera.orthographicSize = 4f;
            layer.RefreshVisibility();
            Assert.IsFalse(layer.IsFarLod, "orthographicSize=4 应为近景");
            Assert.AreEqual(layer.IconCount, layer.VisibleIconCount, "近景应显示全部图标");
            Assert.AreEqual(layer.IconCount, layer.VisibleAnchors.Count, "VisibleAnchors 应与近景图标数一致");

            // ===== LOD：远景仅大城市（首都/港口/城市/LevelIII+）=====
            camera.orthographicSize = Mathf.Max(10f, mapRenderer.WorldHeight * 2f);
            layer.RefreshVisibility();
            Assert.IsTrue(layer.IsFarLod, "拉远后应为远景 LOD");
            int expectedFarIcons = CountMajorSettlements(world);
            Assert.AreEqual(expectedFarIcons, layer.VisibleIconCount,
                $"远景应只显示大城市（期望 {expectedFarIcons}，实际 {layer.VisibleIconCount}）");
            Assert.LessOrEqual(layer.VisibleIconCount, layer.IconCount, "远景可见数不能超过图标总数");

            // ===== 选中：只高亮，不打开 UI 面板 =====
            // 回到近景，保证目标聚落可见可点。
            camera.orthographicSize = 4f;
            layer.RefreshVisibility();

            bool found = TryFindSelectableSettlement(world, mapRenderer, camera, out int anchorId, out Vector2 screenPoint);
            Assert.IsTrue(found, "应能找到一个带有效锚点地块的聚落用于点击测试");

            layer.SelectAtScreen(screenPoint);
            yield return null; // 等 SettlementClicked 事件 → GameManager 打开详情面板
            yield return null;

            Assert.AreEqual(anchorId, layer.SelectedSettlementAnchorId,
                "SelectAtScreen 应选中点击位置的聚落锚点");
            Assert.GreaterOrEqual(anchorId, 0, "选中锚点 id 应有效");

            // ===== 点击图标 → 打开聚落详情面板（用户要求；面板实现由 UI 2 卡提供）=====
            var detailPanel = Object.FindAnyObjectByType<SettlementDetailPanel>(FindObjectsInactive.Include);
            Assert.IsNotNull(detailPanel, "点击聚落图标后应创建/打开 SettlementDetailPanel");
            Assert.IsTrue(detailPanel.IsOpen, "点击聚落图标后详情面板应打开");

            // 名称断言：面板暴露 TitleText/BodyText（UI 2 卡标注"测试/调试用"），断言显示对应聚落名
            Assert.IsTrue(TryGetSettlement(world, anchorId, out var clickedSettlement), "被选中锚点应能查到聚落数据");
            string expectedName = clickedSettlement.settlementName;
            Assert.IsFalse(string.IsNullOrEmpty(expectedName), "被选中聚落应有名称");
            string panelText = (detailPanel.TitleText ?? string.Empty) + "\n" + (detailPanel.BodyText ?? string.Empty);
            Assert.IsTrue(panelText.Contains(expectedName),
                $"详情面板应显示聚落名「{expectedName}」，实际标题/正文：{panelText}");

            // ===== 关闭 → IsOpen=false =====
            detailPanel.Hide();
            yield return null;
            Assert.IsFalse(detailPanel.IsOpen, "Hide 后聚落详情面板应关闭");
        }

        // ==================== 测试辅助 ====================

        private static int CountSettlementsWithAnchor(GameWorld world)
        {
            if (world == null || world.settlements == null) return 0;
            int count = 0;
            foreach (var kv in world.settlements)
            {
                var settlement = kv.Value;
                if (settlement == null) continue;
                if (settlement.anchor != null && settlement.anchor.tileIndex >= 0) { count++; continue; }
                if (settlement.anchorId >= 0 && world.anchors != null &&
                    world.anchors.TryGetValue(settlement.anchorId, out var anchor) && anchor != null && anchor.tileIndex >= 0)
                {
                    count++;
                }
            }
            return count;
        }

        private static bool IsMajor(SettlementData settlement)
        {
            if (settlement == null) return false;
            if (settlement.isCapital || settlement.isPort) return true;
            if (settlement.settlementForm == SettlementForm.City) return true;
            return settlement.settlementLevel >= SettlementLevel.LevelIII;
        }

        private static int CountMajorSettlements(GameWorld world)
        {
            if (world == null || world.settlements == null) return 0;
            int count = 0;
            foreach (var kv in world.settlements)
            {
                var settlement = kv.Value;
                if (settlement == null || !IsMajor(settlement)) continue;
                int tileIndex = settlement.anchor != null ? settlement.anchor.tileIndex : -1;
                if (tileIndex < 0 && world.anchors != null &&
                    world.anchors.TryGetValue(settlement.anchorId, out var anchor) && anchor != null)
                {
                    tileIndex = anchor.tileIndex;
                }
                if (tileIndex >= 0) count++;
            }
            return count;
        }

        /// <summary>选一个带有效锚点的聚落，并返回其地块中心对应的屏幕坐标（离屏幕中心最近者优先）。</summary>
        private static bool TryFindSelectableSettlement(GameWorld world, MapRenderer mapRenderer, Camera camera,
            out int anchorId, out Vector2 screenPoint)
        {
            anchorId = -1;
            screenPoint = Vector2.zero;
            if (world == null || world.settlements == null || mapRenderer == null || camera == null) return false;

            Vector2 screenCenter = new Vector2(Screen.width, Screen.height) * 0.5f;
            float bestDistance = float.MaxValue;
            bool found = false;
            foreach (var kv in world.settlements)
            {
                var settlement = kv.Value;
                if (settlement == null) continue;
                int tileIndex = settlement.anchor != null ? settlement.anchor.tileIndex : -1;
                if (tileIndex < 0 && world.anchors != null &&
                    world.anchors.TryGetValue(settlement.anchorId, out var anchor) && anchor != null)
                {
                    tileIndex = anchor.tileIndex;
                }
                if (tileIndex < 0) continue;

                Vector3 screen = camera.WorldToScreenPoint(mapRenderer.TileToWorld(tileIndex));
                if (screen.z < 0f) continue; // 在相机背后
                float distance = Vector2.Distance(screenCenter, screen);
                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    screenPoint = screen;
                    anchorId = settlement.anchorId;
                    found = true;
                }
            }
            return found;
        }

        /// <summary>按锚点 id 查聚落（settlements 字典键不一定等于 anchorId，做一次回退遍历）。</summary>
        private static bool TryGetSettlement(GameWorld world, int anchorId, out SettlementData settlement)
        {
            settlement = null;
            if (world == null || world.settlements == null) return false;
            if (world.settlements.TryGetValue(anchorId, out settlement) && settlement != null) return true;
            foreach (var kv in world.settlements)
            {
                if (kv.Value != null && kv.Value.anchorId == anchorId)
                {
                    settlement = kv.Value;
                    return true;
                }
            }
            settlement = null;
            return false;
        }
    }
}
