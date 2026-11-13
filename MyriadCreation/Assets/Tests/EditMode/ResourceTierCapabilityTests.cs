using NUnit.Framework;
using MyriadCreation.Core.Data;
using MyriadCreation.Simulation.ContentHost;
using MyriadCreation.Simulation.Systems;

namespace MyriadCreation.Tests.EditMode
{
    /// <summary>资源三层（露天/坑采/深井）与采矿能力：不能把深井能力白送给露天采掘。</summary>
    public class ResourceTierCapabilityTests
    {
        [SetUp]
        public void Setup()
        {
            ContentRegistry.Reset();
            ContentRegistry.Initialize();
        }

        [Test]
        public void SurfaceMining_OnlyProvidesSurface()
        {
            Assert.IsTrue(ContentRegistry.Innovations.TryGetValue(802, out var surface));
            CollectionAssert.AreEquivalent(new[] { "mining.surface" }, surface.providesCapabilities);
            CollectionAssert.DoesNotContain(surface.providesCapabilities, "mining.shaft");
            CollectionAssert.DoesNotContain(surface.providesCapabilities, "mining.deep");
        }

        [Test]
        public void DeepMining_ProvidesShaftAndDeep()
        {
            Assert.IsTrue(ContentRegistry.Innovations.TryGetValue(803, out var deep));
            CollectionAssert.Contains(deep.providesCapabilities, "mining.shaft");
            CollectionAssert.Contains(deep.providesCapabilities, "mining.deep");
            CollectionAssert.DoesNotContain(deep.providesCapabilities, "mining.surface");
        }

        [Test]
        public void DepthAccess_UsesTierCapabilities()
        {
            var surface = new System.Collections.Generic.HashSet<string> { "mining.surface" };
            Assert.IsTrue(ResourceAccess.IsDepthAccessible(ResourceAccessDepth.Surface, surface));
            Assert.IsFalse(ResourceAccess.IsDepthAccessible(ResourceAccessDepth.Shaft, surface));
            Assert.IsFalse(ResourceAccess.IsDepthAccessible(ResourceAccessDepth.Deep, surface));

            var deep = new System.Collections.Generic.HashSet<string> { "mining.shaft", "mining.deep" };
            Assert.IsTrue(ResourceAccess.IsDepthAccessible(ResourceAccessDepth.Shaft, deep));
            Assert.IsTrue(ResourceAccess.IsDepthAccessible(ResourceAccessDepth.Deep, deep));
        }
    }
}
