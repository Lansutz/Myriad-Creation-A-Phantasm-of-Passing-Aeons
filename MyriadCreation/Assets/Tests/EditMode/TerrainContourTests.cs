using NUnit.Framework;
using MyriadCreation.Rendering;

namespace MyriadCreation.Tests.EditMode
{
    /// <summary>P11.4：等高线只在跨越高程间隔时落线，避免整片地形被涂成色带。</summary>
    public class TerrainContourTests
    {
        [Test]
        public void DrawsWhenCrossingInterval()
        {
            Assert.IsTrue(TerrainContour.ShouldDraw(0.12f, 0.08f, 0.09f, 0.05f),
                "跨过 0.10 这一层应落线");
        }

        [Test]
        public void DoesNotDrawWithinSameBand()
        {
            Assert.IsFalse(TerrainContour.ShouldDraw(0.11f, 0.12f, 0.13f, 0.05f),
                "同一层内不应出现等高线");
        }

        [Test]
        public void HandlesTinyIntervalSafely()
        {
            Assert.DoesNotThrow(() => TerrainContour.ShouldDraw(0.5f, 0.4f, 0.6f, 0f));
        }
    }
}
