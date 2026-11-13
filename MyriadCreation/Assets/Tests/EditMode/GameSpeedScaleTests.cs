using NUnit.Framework;
using MyriadCreation.App.Contracts;

namespace MyriadCreation.Tests.EditMode
{
    /// <summary>P7 时间尺度：五档速度的单源契约（设置端与 UI 共用）。</summary>
    public class GameSpeedScaleTests
    {
        [Test]
        public void Options_AreFiveAscendingSpeeds()
        {
            CollectionAssert.AreEqual(new[] { 1f, 2f, 5f, 10f, 30f }, GameSpeedScale.Options);
            Assert.AreEqual(GameSpeedScale.Options[GameSpeedScale.Options.Length - 1], GameSpeedScale.Max,
                "最后一档应与上限一致，否则 UI 永远选不到最高速");
        }

        [Test]
        public void Clamp_CapsAtThirty_AndRejectsNegative()
        {
            Assert.AreEqual(30f, GameSpeedScale.Clamp(60f), "超过 30 倍必须被钳到 30");
            Assert.AreEqual(30f, GameSpeedScale.Clamp(30f));
            Assert.AreEqual(0f, GameSpeedScale.Clamp(-3f), "负速度按暂停处理");
            Assert.AreEqual(10f, GameSpeedScale.Clamp(10f));
        }

        [Test]
        public void Next_CyclesAllFive_AndWrapsToFirst()
        {
            Assert.AreEqual(2f, GameSpeedScale.Next(1f));
            Assert.AreEqual(5f, GameSpeedScale.Next(2f));
            Assert.AreEqual(10f, GameSpeedScale.Next(5f));
            Assert.AreEqual(30f, GameSpeedScale.Next(10f));
            Assert.AreEqual(1f, GameSpeedScale.Next(30f), "30x 之后应回到 1x");
        }

        [Test]
        public void Next_FromNonOptionSpeed_StartsAtFirst()
        {
            Assert.AreEqual(1f, GameSpeedScale.Next(3f), "旧档/旧场景的 3x 不在档位表内，下一次循环从 1x 开始");
            Assert.AreEqual(1f, GameSpeedScale.Next(0f), "暂停状态下点速度按钮应从 1x 开始");
        }
    }
}
