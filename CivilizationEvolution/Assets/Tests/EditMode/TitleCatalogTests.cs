using System.Collections.Generic;
using NUnit.Framework;
using CivilizationEvolution.Core;
using CivilizationEvolution.Culture;

namespace CivilizationEvolution.Tests
{
    /// <summary>
    /// 头衔目录测试（批3：Titles.json 加载——三类+国名后缀——
    /// 位阶比较/文化专属/权重选择）
    /// </summary>
    public class TitleCatalogTests
    {
        [Test]
        public void Titles_Loaded_FromJson()
        {
            Assert.IsTrue(ContentRegistry.Titles.Count > 0, "Titles.json 已加载");
            // 君主类
            var king = TitleCatalog.Get("title_king");
            Assert.IsNotNull(king, "王在表");
            Assert.AreEqual("monarch", king.kind, "君主类");
            Assert.AreEqual(2.0f, king.rank, "王位阶 2.0");
            // 官僚类
            var prefect = TitleCatalog.Get("title_prefect");
            Assert.AreEqual("bureaucratic", prefect.kind, "官僚类");
            // 贵族类
            var duke = TitleCatalog.Get("title_duke");
            Assert.AreEqual("noble", duke.kind, "贵族类");
            // 国名后缀
            var kingdom = TitleCatalog.Get("realm_kingdom");
            Assert.AreEqual("realmSuffix", kingdom.kind, "国名后缀类");
        }

        [Test]
        public void Title_RankComparison_Flexible()
        {
            // 柔性位阶（整数大等级——小数同级微差）
            var king = TitleCatalog.Get("title_king");
            var emperor = TitleCatalog.Get("title_emperor");
            var vassalKing = TitleCatalog.Get("title_vassal_king");
            var khan = TitleCatalog.Get("title_khan");

            Assert.IsTrue(TitleCatalog.IsHigher(emperor, king), "皇帝>王");
            Assert.IsTrue(TitleCatalog.IsHigher(king, vassalKing), "王>藩王（皇下王）");
            Assert.AreEqual((int)king.rank, (int)khan.rank, "王与可汗同级（2.x）");
            Assert.Less(vassalKing.rank, king.rank, "藩王 1.8<王 2.0（小数微差）");
        }

        [Test]
        public void ByKind_Highest_AndWeight()
        {
            // 君主类最高=皇帝（3.0）
            var highest = TitleCatalog.Highest("monarch");
            Assert.IsNotNull(highest);
            Assert.GreaterOrEqual(highest.rank, 3.0f, "君主最高≥皇帝级");

            // 三类分离查询
            Assert.IsTrue(TitleCatalog.ByKind("bureaucratic").Count >= 2, "官僚头衔≥2（郡守/县令…）");
            Assert.IsTrue(TitleCatalog.ByKind("noble").Count >= 4, "贵族爵位≥4（公侯伯男…）");

            // 同级权重选择（王 vs 可汗——2.0 同级——权重不同——加权总能选出）
            var picked = TitleCatalog.PickByWeight(new List<TitleDef> {
                TitleCatalog.Get("title_king"), TitleCatalog.Get("title_khan")
            }, new System.Random(42));
            Assert.IsNotNull(picked, "权重选择非空");
            Assert.IsTrue(picked.titleId == "title_king" || picked.titleId == "title_khan", "同级选择结果在候选中");
        }

        [Test]
        public void CultureExclusive_Fallback()
        {
            // 无文化专属→回退通用（国王=通用）
            var king = TitleCatalog.Highest("monarch", cultureId: 999);
            Assert.IsNotNull(king, "无专属回退通用");

            // 文化专属（模拟模组加 Laethis 专属君主头衔）
            var laethisKing = new TitleDef
            {
                titleId = "title_laethis_king", kind = "monarch",
                rank = 2.3f, weight = 1.0f, cultureId = 1
            };
            ContentRegistry.Titles[laethisKing.titleId] = laethisKing;
            var forLaethis = TitleCatalog.Highest("monarch", cultureId: 1);
            Assert.AreEqual("title_laethis_king", forLaethis.titleId, "文化专属优先");
        }
    }
}
