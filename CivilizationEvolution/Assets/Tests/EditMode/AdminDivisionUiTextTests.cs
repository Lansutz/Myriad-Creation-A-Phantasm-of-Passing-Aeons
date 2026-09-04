using System.Collections.Generic;
using NUnit.Framework;
using CivilizationEvolution.Politics;
using CivilizationEvolution.Culture;

namespace CivilizationEvolution.Tests
{
    /// <summary>
    /// 行政区划树 UI 文本层测试（政权总览含树——区划详情页——
    /// 多级下钻的纯文本可测部分）
    /// </summary>
    public class AdminDivisionUiTextTests
    {
        private static List<AdminDivision> MakeTree()
        {
            var list = new List<AdminDivision>();
            list.Add(new AdminDivision { divisionId = 1, realmId = 1, level = 1, name = "测试国", parentDivisionId = -1 });
            list.Add(new AdminDivision { divisionId = 102, realmId = 1, level = 2, name = "测试国·郡1", parentDivisionId = 1, titleId = "title_gov_general" });
            list.Add(new AdminDivision { divisionId = 103, realmId = 1, level = 2, name = "测试国·郡2", parentDivisionId = 1 });
            list.Add(new AdminDivision { divisionId = 10201, realmId = 1, level = 3, name = "测试国·县1", parentDivisionId = 102 });
            return list;
        }

        [Test]
        public void Overview_ContainsDivisionTree()
        {
            var realm = new RealmData { realmId = 1, realmName = "测试国" };
            var society = new RealmSociety { realmId = 1, totalPopulation = 1000f };
            var tree = MakeTree();

            var text = RealmOverviewText.Build(realm, society, null, null, "",
                false, tree, null);

            Assert.IsTrue(text.Contains("── 行政区划 ──"), "区划树区存在");
            Assert.IsTrue(text.Contains("测试国·郡1"), "层2区划在树");
            Assert.IsTrue(text.Contains("测试国·郡2"), "多子区划");
            Assert.IsTrue(text.Contains("测试国·县1"), "层3区划在树（递归）");
            // 缩进层级（县在郡下——行缩进更多）
            int idxJun = text.IndexOf("郡1");
            int idxXian = text.IndexOf("县1");
            Assert.Greater(idxXian, idxJun, "县在郡后（树序）");
        }

        [Test]
        public void DivisionDetail_Page()
        {
            var tree = MakeTree();
            var division = tree[1]; // 郡1（层2——title_gov_general——有子县）

            var text = RealmDivisionText.Build(division, tree, 5000L, "张三", "测试国");

            Assert.IsTrue(text.Contains("测试国·郡1"), "区划名");
            Assert.IsTrue(text.Contains("第 2 级行政区"), "层级显示");
            Assert.IsTrue(text.Contains("辖境"), "辖境区");
            Assert.IsTrue(text.Contains("人口 5,000"), "人口显示");
            Assert.IsTrue(text.Contains("治理头衔：title_gov_general"), "治理头衔");
            Assert.IsTrue(text.Contains("治理者：张三"), "治理者");
            Assert.IsTrue(text.Contains("测试国·县1"), "子区划列表（下钻目标）");

            // 根区划详情（政权本身）
            var rootText = RealmDivisionText.Build(tree[0], tree, -1, "", "测试国");
            Assert.IsTrue(rootText.Contains("第 1 级行政区（政权本身——治理根）"), "根标记");
            Assert.IsTrue(rootText.Contains("郡1") && rootText.Contains("郡2"), "根的子区划");
        }
    }
}
