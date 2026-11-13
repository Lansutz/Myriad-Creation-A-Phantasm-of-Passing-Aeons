using System.Collections.Generic;
using NUnit.Framework;
using MyriadCreation.Core.Enums;
using MyriadCreation.Simulation.Definitions;
using MyriadCreation.Simulation.ContentHost;
using MyriadCreation.Simulation.Systems;

namespace MyriadCreation.Tests
{
    /// <summary>头衔目录（封闭开发：kind 字符串模型已移除，统一 TitleDerivedCategory）。</summary>
    public class TitleCatalogTests
    {
        [Test]
        public void Titles_Loaded_FromJson()
        {
            Assert.IsTrue(ContentRegistry.Titles.Count > 0, "Titles.json 已加载");

            var king = TitleCatalog.Get("title_king");
            Assert.IsNotNull(king, "王在表");
            Assert.AreEqual(TitleDerivedCategory.Landed, king.category, "王 = 领地头衔");
            Assert.AreEqual(2.0f, king.rank, "王位阶 2.0");

            var prefect = TitleCatalog.Get("title_prefect");
            Assert.IsNotNull(prefect, "郡守在表");
            Assert.AreEqual(TitleDerivedCategory.Office, prefect.category, "官僚头衔");

            var duke = TitleCatalog.Get("title_duke");
            Assert.IsNotNull(duke, "公爵在表");
            Assert.AreEqual(TitleDerivedCategory.NobleRank, duke.category, "贵族爵位");
        }

        [Test]
        public void Title_RankComparison_Flexible()
        {
            var king = TitleCatalog.Get("title_king");
            var emperor = TitleCatalog.Get("title_emperor");
            var vassalKing = TitleCatalog.Get("title_vassal_king");
            var khan = TitleCatalog.Get("title_khan");

            Assert.IsTrue(TitleCatalog.IsHigher(emperor, king), "皇帝>王");
            Assert.IsTrue(TitleCatalog.IsHigher(king, vassalKing), "王>藩王");
            Assert.AreEqual((int)king.rank, (int)khan.rank, "王与可汗同级");
            Assert.Less(vassalKing.rank, king.rank, "藩王 1.8<王 2.0");
        }

        [Test]
        public void ByCategory_Highest_AndWeight()
        {
            var highest = TitleCatalog.Highest(TitleDerivedCategory.Landed);
            Assert.IsNotNull(highest);
            Assert.GreaterOrEqual(highest.rank, 3.0f, "领地头衔最高≥皇帝级");

            Assert.IsTrue(TitleCatalog.ByCategory(TitleDerivedCategory.Office).Count >= 2, "官僚头衔≥2");
            Assert.IsTrue(TitleCatalog.ByCategory(TitleDerivedCategory.NobleRank).Count >= 4, "贵族爵位≥4");

            var picked = TitleCatalog.PickByWeight(new List<TitleDefinition>
            {
                TitleCatalog.Get("title_king"),
                TitleCatalog.Get("title_khan")
            }, new System.Random(42));
            Assert.IsNotNull(picked, "权重选择非空");
            Assert.IsTrue(picked.titleId == "title_king" || picked.titleId == "title_khan");
        }

        [Test]
        public void Category_Registration_IsVisibleToCatalog()
        {
            TitleManager.Register(new TitleDefinition
            {
                titleId = "title_test_high_king",
                category = TitleDerivedCategory.Landed,
                rank = 3.5f,
                weight = 1f
            });

            var highest = TitleCatalog.Highest(TitleDerivedCategory.Landed);
            Assert.AreEqual("title_test_high_king", highest.titleId, "运行时注册的头衔可被目录看到");
        }
    }
}
