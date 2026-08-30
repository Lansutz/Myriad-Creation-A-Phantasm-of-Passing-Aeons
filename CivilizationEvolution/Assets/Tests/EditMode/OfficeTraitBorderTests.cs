using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using CivilizationEvolution.Core;
using CivilizationEvolution.Culture;
using CivilizationEvolution.Map;
using CivilizationEvolution.Politics;

namespace CivilizationEvolution.Tests
{
    /// <summary>
    /// 官职称号 / 民族特质 / 省界判定 测试
    /// </summary>
    public class OfficeTraitBorderTests
    {
        [SetUp]
        public void Setup()
        {
            ContentRegistry.Reset();
            ContentRegistry.Initialize();
            Localization.Initialize("zh-Hans"); // 本地化默认简体（称号文本断言依赖）
        }

        // ===== 官职称号 =====

        [Test]
        public void OfficeTitle_DefaultFallback()
        {
            // 无文化定制 → 默认称号键（office_<office>）
            Assert.AreEqual("office_Governor", OfficeTitleCatalog.GetTitleKey(null, OfficialOffice.Governor, "Kingdom"));
            Assert.AreEqual("office_Steward", OfficeTitleCatalog.GetTitleKey(null, OfficialOffice.Steward, "Tribal"));
        }

        [Test]
        public void OfficeTitle_CultureCustomization()
        {
            // Laethis 定制：Kingdom|Governor → office_laethis_gov_kingdom
            Assert.IsTrue(ContentRegistry.TryGetCulture(1, out var pack));
            string key = OfficeTitleCatalog.GetTitleKey(pack.data, OfficialOffice.Governor, "Kingdom");
            Assert.AreEqual("office_laethis_gov_kingdom", key, "文化定制优先");

            // 未定制组合（Kingdom|Marshal）→ 默认
            Assert.AreEqual("office_Marshal", OfficeTitleCatalog.GetTitleKey(pack.data, OfficialOffice.Marshal, "Kingdom"));
        }

        [Test]
        public void OfficeTitle_LocalizedText()
        {
            Assert.IsTrue(ContentRegistry.TryGetCulture(1, out var pack));
            // 定制称号本地化文本
            Assert.AreEqual("河谷都护", OfficeTitleCatalog.GetTitle(pack.data, OfficialOffice.Governor, "Kingdom"));
            // 默认称号
            Assert.AreEqual("元帅", OfficeTitleCatalog.GetTitle(pack.data, OfficialOffice.WarCommander, "Empire"));
        }

        // ===== 省界判定 =====

        [Test]
        public void ProvinceBorder_DetectsBoundary()
        {
            // 3×4 网格：tiles[10]（右下区）异省，tile4（中心）与其不相邻
            var tiles = new TileData[12];
            for (int i = 0; i < 12; i++)
                tiles[i] = new TileData { tileIndex = i, isLand = true, provinceId = 0 };
            tiles[10].provinceId = 1; // 右下区异省

            Assert.IsTrue(Province.IsBorder(tiles, 3, 4, 10), "邻异省应为边界");
            Assert.IsFalse(Province.IsBorder(tiles, 3, 4, 4), "无异省邻域非边界");
        }

        [Test]
        public void ProvinceBorder_SeaNeighborNotBorder()
        {
            // 中心省 0 邻海（isLand=false）——不算省界
            var tiles = new TileData[4];
            for (int i = 0; i < 4; i++)
                tiles[i] = new TileData { tileIndex = i, isLand = true, provinceId = 0 };
            tiles[1].isLand = false; // 右邻海

            Assert.IsFalse(Province.IsBorder(tiles, 2, 2, 0), "邻海不算省界");
        }
    }
}
