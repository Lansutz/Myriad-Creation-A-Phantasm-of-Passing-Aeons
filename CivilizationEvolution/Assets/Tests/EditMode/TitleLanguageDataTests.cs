using System.Collections.Generic;
using NUnit.Framework;
using CivilizationEvolution.Core;

namespace CivilizationEvolution.Tests
{
    /// <summary>
    /// 头衔/语言数据层测试（批1：语言名字池迁移+PlaceSuffixDef+TitleDef——
    /// 2026-09-03 用户设计：语言内人名/地名后缀可模组化——头衔位阶柔性数值）
    /// </summary>
    public class TitleLanguageDataTests
    {
        [Test]
        public void LanguageDef_NamesAndSuffixes()
        {
            // 语言名字池（语言级 CSV——同语言共享）
            Assert.IsTrue(ContentRegistry.TryGetLanguage("laethis_lang", out var lang), "Laethis 语言存在");
            Assert.Greater(lang.maleNames.Count, 0, "语言男性名池（从语言目录 CSV 加载）");
            Assert.Greater(lang.femaleNames.Count, 0, "语言女性名池");
            Assert.Greater(lang.familyNames.Count, 0, "语言姓氏池");

            // 地名后缀语义表（结构：semantic+word——语义分类可组合）
            lang.placeSuffixes.Add(new PlaceSuffixDef { semantic = "city", word = "城" });
            lang.placeSuffixes.Add(new PlaceSuffixDef { semantic = "fort", word = "堡" });
            Assert.AreEqual("城", lang.placeSuffixes[0].word, "城语义后缀词形");
        }

        [Test]
        public void TitleDef_FlexibleRankFields()
        {
            // 头衔位阶（柔性数值——2.0 王/2.4 王上王/1.8 藩王——小数同级微差）
            var king = new TitleDef { titleId = "title_king", kind = "monarch", rank = 2.0f, weight = 1f };
            var emperor = new TitleDef { titleId = "title_emperor", kind = "monarch", rank = 3.0f, weight = 1.2f };
            var vassalKing = new TitleDef { titleId = "title_vassal_king", kind = "monarch", rank = 1.8f };
            var bure = new TitleDef { titleId = "title_gov", kind = "bureaucratic", rank = 2.2f };
            var noble = new TitleDef { titleId = "title_duke", kind = "noble", rank = 1.5f };

            // 同级区间（1.x 一级/2.x 二级——整数=大等级）
            Assert.AreEqual(2, (int)king.rank, "王=2 级");
            Assert.AreEqual(3, (int)emperor.rank, "皇帝=3 级");
            Assert.Less(vassalKing.rank, king.rank, "藩王<王（皇下王——小数表达）");
            Assert.Greater(emperor.rank, king.rank, "皇帝>王");

            // 权重（同级选择用）
            Assert.Greater(emperor.weight, king.weight, "皇帝权重大于王");

            // 类别分离（官僚/贵族/君主——用户定稿分开）
            Assert.AreEqual("bureaucratic", bure.kind, "官僚类");
            Assert.AreEqual("noble", noble.kind, "贵族类");
            Assert.AreEqual("monarch", king.kind, "君主类");
        }
    }
}
