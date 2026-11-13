using NUnit.Framework;
using MyriadCreation.Content.Definitions;
using MyriadCreation.Simulation.ContentHost;

namespace MyriadCreation.Tests.EditMode
{
    /// <summary>宗教/信仰/思想域冻结，不在本轮革新审计里改动。</summary>
    public class InnovationReviewPolicyTests
    {
        [SetUp]
        public void Setup() { ContentRegistry.Reset(); ContentRegistry.Initialize(); }

        [Test]
        public void FaithThoughtEntries_AreFrozen()
        {
            foreach (int id in new[] { 601, 602, 604, 809, 810, 982 })
            {
                Assert.IsTrue(ContentRegistry.Innovations.TryGetValue(id, out var def), "缺少革新 " + id);
                Assert.IsTrue(InnovationReviewPolicy.IsFrozen(def), "革新 " + id + " 应冻结");
            }
        }

        [Test]
        public void TechnicalEntries_AreNotFrozen()
        {
            foreach (int id in new[] { 201, 202, 305, 400, 802, 931 })
            {
                Assert.IsTrue(ContentRegistry.Innovations.TryGetValue(id, out var def), "缺少革新 " + id);
                Assert.IsFalse(InnovationReviewPolicy.IsFrozen(def), "革新 " + id + " 不应冻结");
            }
        }
    }
}
