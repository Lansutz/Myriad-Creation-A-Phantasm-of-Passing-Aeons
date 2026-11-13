using NUnit.Framework;
using MyriadCreation.App.Settings;
using MyriadCreation.UI.Common;

namespace MyriadCreation.Tests.EditMode
{
    /// <summary>
    /// 界面字体选项回归：choice 资源路径/资产名映射、非法 choice 回 0、TMP 字体缓存。
    /// TMP Settings/字体不可用时只跳过依赖实际 TMP_FontAsset 的用例，映射/归一化断言始终有效。
    /// </summary>
    public class TMPFontUtilityTests
    {
        [SetUp]
        public void SetUp()
        {
            // 字体缓存/诊断为进程级静态；每个用例从干净状态开始，避免缓存顺序影响断言。
            TMPFontUtility.ResetDiagnosticsForTests();
        }

        [Test]
        public void ResourcePathAndName_MapsChoices()
        {
            Assert.AreEqual("simhei", TMPFontUtility.GetResourcePath(0));
            Assert.AreEqual("Fonts/SourceHanSansSC-Regular", TMPFontUtility.GetResourcePath(1));
            Assert.AreEqual("Fonts/SourceHanSerifSC-Regular", TMPFontUtility.GetResourcePath(2));

            Assert.AreEqual("simhei-SDF", TMPFontUtility.GetFontAssetName(0));
            Assert.AreEqual("SourceHanSans-SDF", TMPFontUtility.GetFontAssetName(1));
            Assert.AreEqual("SourceHanSerif-SDF", TMPFontUtility.GetFontAssetName(2));
        }

        [Test]
        public void InvalidChoice_NormalizesToDefault()
        {
            Assert.AreEqual(0, TMPFontUtility.NormalizeChoice(-1));
            Assert.AreEqual(0, TMPFontUtility.NormalizeChoice(3));
            Assert.AreEqual(0, TMPFontUtility.NormalizeChoice(99));
            Assert.AreEqual("simhei", TMPFontUtility.GetResourcePath(99));
            Assert.AreEqual("simhei-SDF", TMPFontUtility.GetFontAssetName(-5));

            Assert.AreEqual(SettingsModel.MaxFontChoice, TMPFontUtility.NormalizeChoice(SettingsModel.MaxFontChoice));
            Assert.DoesNotThrow(() => TMPFontUtility.ApplyFontChoice(99));
        }

        [Test]
        public void GetFont_RepeatedCall_UsesCache()
        {
            var first = TMPFontUtility.GetFont(0);
            if (first == null)
            {
                Assert.Ignore("TMP Settings/字体资源不可用（EditMode 环境不稳定）");
            }

            Assert.GreaterOrEqual(TMPFontUtility.LastCreateElapsedMs, 0, "耗时诊断值必须非负");
            int created = TMPFontUtility.TotalFontAssetCreations;
            long elapsed = TMPFontUtility.LastCreateElapsedMs;
            var second = TMPFontUtility.GetFont(0);

            Assert.AreSame(first, second, "同一 choice 应复用缓存的 TMP_FontAsset");
            Assert.AreEqual(created, TMPFontUtility.TotalFontAssetCreations, "缓存命中不应重复 CreateFontAsset");
            Assert.AreEqual(elapsed, TMPFontUtility.LastCreateElapsedMs, "缓存命中不应更新实际创建耗时");
            Assert.GreaterOrEqual(TMPFontUtility.CachedFontAssetCount, 1);
        }

        [Test]
        public void GetFont_InvalidChoice_FallsBackToDefault()
        {
            var defaultFont = TMPFontUtility.GetFont(0);
            if (defaultFont == null)
            {
                Assert.Ignore("TMP Settings/字体资源不可用（EditMode 环境不稳定）");
            }

            var invalidFont = TMPFontUtility.GetFont(99);
            Assert.AreSame(defaultFont, invalidFont, "非法 choice 应回退 simhei");
            Assert.AreEqual("simhei-SDF", invalidFont.name);
        }
        [Test]
        public void ResetDiagnosticsForTests_ClearsCacheAndCounters()
        {
            var font = TMPFontUtility.GetFont(0);
            if (font == null)
            {
                Assert.Ignore("TMP Settings/字体资源不可用（EditMode 环境不稳定）");
            }

            Assert.GreaterOrEqual(TMPFontUtility.TotalFontAssetCreations, 1);
            Assert.GreaterOrEqual(TMPFontUtility.LastCreateElapsedMs, 0);

            TMPFontUtility.ResetDiagnosticsForTests();

            Assert.AreEqual(0, TMPFontUtility.TotalFontAssetCreations, "Reset 应清零创建计数");
            Assert.AreEqual(0, TMPFontUtility.CachedFontAssetCount, "Reset 应清空字体缓存");
            Assert.AreEqual(0, TMPFontUtility.LastCreateElapsedMs, "Reset 应清零耗时诊断");
        }
    }
}
