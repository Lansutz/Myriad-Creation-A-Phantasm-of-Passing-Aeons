using NUnit.Framework;
using MyriadCreation.Simulation.Politics;

namespace MyriadCreation.Tests.EditMode
{
    /// <summary>TitleInstanceRegistry：按 titleKey 取/建，重复取同一实例。</summary>
    public class TitleInstanceRegistryTests
    {
        [TearDown]
        public void TearDown()
        {
            TitleInstanceRegistry.Clear();
        }

        [Test]
        public void GetOrCreate_ReturnsSameInstance()
        {
            TitleInstance a = TitleInstanceRegistry.GetOrCreate("realm_1_primary");
            TitleInstance b = TitleInstanceRegistry.GetOrCreate("realm_1_primary");
            Assert.AreSame(a, b);
        }

        [Test]
        public void TransferSenior_VisibleThroughRegistry()
        {
            TitleInstance instance = TitleInstanceRegistry.GetOrCreate("realm_2_primary");
            instance.TransferSenior(7, day: 100);
            Assert.IsTrue(TitleInstanceRegistry.TryGet("realm_2_primary", out TitleInstance found));
            Assert.AreEqual(7, found.GetSeniorHolder(100).characterId);
        }
    }
}
