using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using MyriadCreation.Simulation.Politics;
using MyriadCreation.Simulation.State;
using MyriadCreation.Simulation.Systems;
using MyriadCreation.Simulation.WorldState;
using MyriadCreation.UI.Presentation;

namespace MyriadCreation.Tests
{
    /// <summary>
    /// 行政区划树 UI 文本 4d 契约回归：仅保留 RealmOverviewText.Build(IWorldReadModel, realmId) 树断言；
    /// 旧 RealmDivisionText 详情重载随 Legacy 删除（无聚合 DTO 等价字段）。
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
            var go = new GameObject("AdminDivisionUiTextTestWorld");
            var world = go.AddComponent<GameWorld>();
            var realm = new RealmData { realmId = 1, realmName = "测试国" };
            realm.adminDivisions = MakeTree();
            world.realms = new Dictionary<int, RealmData> { { 1, realm } };
            var view = new WorldReadModel(world);

            var text = RealmOverviewText.Build(view, 1);

            Assert.IsTrue(text.Contains("── 行政区划 ──"), "区划树区存在");
            Assert.IsTrue(text.Contains("测试国·郡1"), "层2区划在树");
            Assert.IsTrue(text.Contains("测试国·郡2"), "多子区划");
            Assert.IsTrue(text.Contains("测试国·县1"), "层3区划在树（递归）");
            int idxJun = text.IndexOf("郡1");
            int idxXian = text.IndexOf("县1");
            Assert.Greater(idxXian, idxJun, "县在郡后（树序）");

            Object.DestroyImmediate(go);
        }
    }
}
