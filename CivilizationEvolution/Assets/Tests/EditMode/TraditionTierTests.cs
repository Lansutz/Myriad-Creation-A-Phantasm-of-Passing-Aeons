using NUnit.Framework;
using CivilizationEvolution.Core;

namespace CivilizationEvolution.Tests
{
    /// <summary>
    /// 文化传统分层测试（通用传统 × 文化特有传统——CK3 范式：
    /// 特有传统 = 通用传统升级版 + 文化专属）
    /// </summary>
    public class TraditionTierTests
    {
        [SetUp]
        public void Setup()
        {
            ContentRegistry.Reset();
            ContentRegistry.Initialize();
            Localization.Initialize("zh-Hans");
        }

        [Test]
        public void CommonTradition_AnyCultureCanHold()
        {
            Assert.IsTrue(ContentRegistry.TryGetTradition("trad_war_bands", out var warBands));
            Assert.IsTrue(warBands.IsCommon, "战团=通用传统");
            Assert.IsTrue(warBands.CanCultureHold(0), "任意文化可持有");
            Assert.IsTrue(warBands.CanCultureHold(1));
            Assert.IsTrue(warBands.CanCultureHold(99));
        }

        [Test]
        public void ExclusiveTradition_OnlyListedCultures()
        {
            Assert.IsTrue(ContentRegistry.TryGetTradition("trad_valley_elite", out var elite));
            Assert.IsFalse(elite.IsCommon, "河谷精兵=文化特有传统");
            Assert.IsTrue(elite.CanCultureHold(1), "Laethis 可持有");
            Assert.IsFalse(elite.CanCultureHold(0), "其他文化不可持有");
            Assert.IsFalse(elite.CanCultureHold(99), "未知文化不可持有");
        }

        [Test]
        public void ExclusiveTradition_UpgradesFromCommon()
        {
            Assert.IsTrue(ContentRegistry.TryGetTradition("trad_valley_elite", out var elite));
            Assert.IsTrue(ContentRegistry.TryGetTradition("trad_war_bands", out var warBands));

            Assert.AreEqual("trad_war_bands", elite.upgradesFrom, "河谷精兵升级自战团");

            // 升级版效果强于来源（levy 0.15 > 0.1）
            float eliteLevy = GetEffect(elite, "levy");
            float baseLevy = GetEffect(warBands, "levy");
            Assert.Greater(eliteLevy, baseLevy, "特有传统效果强于通用来源");

            // 征服传统（通用池升级版示例）
            Assert.IsTrue(ContentRegistry.TryGetTradition("trad_conqueror_legacy", out var conqueror));
            Assert.IsTrue(conqueror.IsCommon, "征服传统当前为通用池条目");
            Assert.AreEqual("trad_war_bands", conqueror.upgradesFrom);
            Assert.Greater(GetEffect(conqueror, "expansion"), 0f, "征服传统有扩张效果");
        }

        [Test]
        public void Laethis_MountsExclusiveTradition()
        {
            // Laethis 族群挂载的河谷精兵与其文化 id 匹配（专属校验通过）
            Assert.IsTrue(ContentRegistry.TryGetEthnicGroup("ethnos_laethis", out var group));
            Assert.IsTrue(ContentRegistry.TryGetTradition("trad_valley_elite", out var elite));

            Assert.IsTrue(group.traditionIds.Contains("trad_valley_elite"), "Laethis 已挂载");
            Assert.IsTrue(elite.CanCultureHold(group.cultureId), "挂载与专属文化匹配");
        }

        private static float GetEffect(TraditionDef trad, string key)
        {
            foreach (var e in trad.effects)
                if (e.key == key) return e.value;
            return 0f;
        }
    }
}
