using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using MyriadCreation.Core.Contracts;
using MyriadCreation.Core.Data;
using MyriadCreation.Rendering;
using MyriadCreation.UI.Common;
using MyriadCreation.UI.Panels;

namespace MyriadCreation.Tests.EditMode
{
    /// <summary>
    /// P4.2 城市俯视：布局→确定性纹理、缓存、独立面板注册与空态。
    /// </summary>
    public class CityViewTests
    {
        private sealed class FakeSettlementQuery : ISettlementQuery
        {
            public SettlementSnapshotDto Settlement = new SettlementSnapshotDto { anchorId = 1, settlementName = "测试城" };
            public List<SettlementBlockRowDto> Blocks = new List<SettlementBlockRowDto>();

            public SettlementSnapshotDto GetSettlement(int anchorId) => anchorId == 1 ? Settlement : null;

            public IReadOnlyList<SettlementBlockRowDto> GetSettlementBlocks(int anchorId)
                => anchorId == 1 ? Blocks : new List<SettlementBlockRowDto>();

            public IReadOnlyList<SettlementSnapshotDto> GetSettlementsInRealm(int realmId) => new List<SettlementSnapshotDto>();

            public IReadOnlyList<SettlementSnapshotDto> GetSettlementsNearTile(int tileIndex, int radius) => new List<SettlementSnapshotDto>();
        }

        private static CityLayoutPlan Plan()
        {
            var specs = new List<CityBlockSpec>
            {
                new CityBlockSpec { blockId = "market", areaRatio = 2f, districtType = 1, enabled = true },
                new CityBlockSpec { blockId = "temple", areaRatio = 1f, districtType = 6, enabled = true },
                new CityBlockSpec { blockId = "wall", areaRatio = 0.2f, isWall = true, enabled = true }
            };
            return CityLayoutGenerator.Build(specs, 100, 100);
        }

        [Test]
        public void Render_ProducesDeterministicTexture()
        {
            CityLayoutPlan plan = Plan();

            Texture2D first = CityLayoutRenderer.Render(plan);
            Texture2D second = CityLayoutRenderer.Render(plan);

            Assert.AreEqual(100, first.width);
            Assert.AreEqual(100, first.height);
            Color[] a = first.GetPixels();
            Color[] b = second.GetPixels();
            Assert.AreEqual(a.Length, b.Length);
            for (int i = 0; i < a.Length; i++)
            {
                Assert.AreEqual(a[i], b[i], "同布局必须同图");
            }
            Assert.AreNotEqual(new Color(0.10f, 0.11f, 0.13f), a[50 * 100 + 50], "街区应被上色");
            Object.DestroyImmediate(first);
            Object.DestroyImmediate(second);
        }

        [Test]
        public void RenderCached_ReusesAndEvicts()
        {
            CityLayoutRenderer.ClearCache();
            CityLayoutPlan plan = Plan();

            Texture2D first = CityLayoutRenderer.RenderCached("a", plan, capacity: 1);
            Texture2D again = CityLayoutRenderer.RenderCached("a", plan, capacity: 1);
            Assert.AreSame(first, again, "同 key 应命中缓存");

            Texture2D second = CityLayoutRenderer.RenderCached("b", plan, capacity: 1);
            Assert.IsNotNull(second);
            Assert.IsTrue(first == null, "容量 1 时应淘汰旧纹理");
            CityLayoutRenderer.ClearCache();
        }

        [Test]
        public void Panel_RegistersAndShowsEmptyState()
        {
            var go = new GameObject("CityViewTest");
            try
            {
                var panel = go.AddComponent<CityViewPanel>();
                panel.Build(go.transform);
                var registry = new UiPanelRegistry();
                registry.Register(panel);

                Assert.IsTrue(registry.TryGet(CityViewPanel.Id, out IUserInterfacePanel found));
                Assert.AreSame(panel, found);

                var query = new FakeSettlementQuery();
                panel.SetQuery(query);
                panel.ShowFor(1);

                Assert.IsTrue(panel.IsOpen);
                Assert.AreEqual("该聚落暂无区块布局", panel.EmptyText);
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void Panel_WithBlocksRendersLayout()
        {
            var go = new GameObject("CityViewTest2");
            try
            {
                var panel = go.AddComponent<CityViewPanel>();
                panel.Build(go.transform);
                var query = new FakeSettlementQuery();
                query.Blocks.Add(new SettlementBlockRowDto { blockId = "market", areaRatio = 1f, districtType = 1, enabled = true });
                panel.SetQuery(query);

                panel.ShowFor(1);

                Assert.IsTrue(panel.IsOpen);
                Assert.AreEqual("", panel.EmptyText);
                StringAssert.Contains("测试城", panel.TitleText);
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }
    }
}
