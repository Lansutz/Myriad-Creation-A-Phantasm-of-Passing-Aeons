using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using MyriadCreation.Core.Contracts;
using MyriadCreation.UI.Panels;

namespace MyriadCreation.Tests.EditMode
{
    /// <summary>P1.4 UI：革新面板只读 IInnovationQuery，展示已研究与可研究。</summary>
    public class InnovationPanelViewTests
    {
        private GameObject _go;

        [TearDown]
        public void TearDown()
        {
            if (_go != null) Object.DestroyImmediate(_go);
        }

        [Test]
        public void Show_DisplaysResearchedAndAvailable()
        {
            _go = new GameObject("InnovationPanelViewTest");
            var view = _go.AddComponent<InnovationPanelView>();
            view.Build(_go.transform);
            var query = new FakeInnovations();
            query.names[11] = "深耕";
            query.names[12] = "轮作";
            query.names[13] = "水利";
            query.snapshot = new InnovationSnapshotDto
            {
                researchedCount = 1,
                researchedInnovationIds = new List<int> { 11 }
            };
            query.available.Add(new InnovationAvailabilityDto
            {
                innovationId = 12,
                name = "轮作",
                prerequisiteIds = new List<int> { 11 }
            });
            view.SetInnovationQuery(query);
            view.SetViewRealm(1);

            view.Show();

            Assert.IsTrue(view.IsOpen);
            StringAssert.Contains("已研究: 1 项", view.BodyText);
            StringAssert.Contains("深耕", view.BodyText);
            StringAssert.Contains("轮作", view.BodyText);
            StringAssert.Contains("前置: 深耕", view.BodyText);
        }

        [Test]
        public void Show_WithoutQuery_ShowsDash()
        {
            _go = new GameObject("InnovationPanelViewEmpty");
            var view = _go.AddComponent<InnovationPanelView>();
            view.Build(_go.transform);

            Assert.DoesNotThrow(view.Show);
            Assert.AreEqual("-", view.BodyText);
        }

        private sealed class FakeInnovations : IInnovationQuery
        {
            public readonly Dictionary<int, string> names = new Dictionary<int, string>();
            public readonly List<InnovationAvailabilityDto> available = new List<InnovationAvailabilityDto>();
            public InnovationSnapshotDto snapshot;

            public int GetResearchedCount(int realmId) => snapshot?.researchedCount ?? 0;

            public IReadOnlyList<int> GetResearchedInnovationIds(int realmId) =>
                snapshot?.researchedInnovationIds ?? new List<int>();

            public InnovationSnapshotDto GetInnovationSnapshot(int realmId) => snapshot;

            public bool HasInnovation(int realmId, int innovationId)
            {
                IReadOnlyList<int> ids = snapshot?.researchedInnovationIds;
                if (ids == null) return false;
                for (int i = 0; i < ids.Count; i++)
                {
                    if (ids[i] == innovationId) return true;
                }
                return false;
            }

            public string GetInnovationName(int innovationId) =>
                names.TryGetValue(innovationId, out string name) ? name : "#" + innovationId;

            public IReadOnlyList<InnovationAvailabilityDto> GetAvailableInnovations(int realmId) => available;

            public IReadOnlyList<int> GetPrerequisiteInnovationIds(int innovationId)
            {
                InnovationAvailabilityDto item = available.Find(a => a.innovationId == innovationId);
                return item?.prerequisiteIds ?? new List<int>();
            }

            public bool CanResearch(int realmId, int innovationId) => true;

            public bool IsTechAccessibleForGoods(int goodsId, int realmId) => true;

            public bool IsResourceDepthAccessible(int accessDepth, int realmId) => true;

            public bool CanAccessResource(int goodsId, int accessDepth, int realmId) => true;
        }
    }
}
