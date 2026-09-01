using NUnit.Framework;
using TMPro;
using CivilizationEvolution.UI;

namespace CivilizationEvolution.Tests
{
    /// <summary>
    /// TMP 字体测试（SDF 动态字体生成——中文字体可用性）
    /// </summary>
    public class TMPFontTests
    {
        [Test]
        public void ChineseFont_GeneratesSdf()
        {
            var font = TMPFontUtility.GetChineseFont();

            Assert.IsNotNull(font, "TMP 中文字体应生成（TMP Settings+shader 就位）");
            Assert.AreEqual("simhei-SDF", font.name, "字体命名");
            Assert.IsNotNull(font.atlasTexture, "图集纹理存在（动态图集按需渲染——初始 1×1）");
        }

        [Test]
        public void ChineseFont_Cached()
        {
            var f1 = TMPFontUtility.GetChineseFont();
            var f2 = TMPFontUtility.GetChineseFont();
            Assert.AreSame(f1, f2, "懒生成缓存——同一实例");
        }

        [Test]
        public void ApplyToAll_CountsTexts()
        {
            // 无场景文本时返回 0（不崩溃）
            int count = TMPFontUtility.ApplyChineseFontToAll();
            Assert.GreaterOrEqual(count, 0, "应用遍历不崩溃");
        }
    }
}
