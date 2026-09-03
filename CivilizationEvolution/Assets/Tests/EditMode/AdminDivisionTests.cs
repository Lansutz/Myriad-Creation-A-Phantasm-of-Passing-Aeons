using System.Collections.Generic;
using NUnit.Framework;
using CivilizationEvolution.Politics;
using CivilizationEvolution.Culture;

namespace CivilizationEvolution.Tests
{
    /// <summary>
    /// 行政区划树测试（批4：分封固定 4 层/郡县容量弹性 2-5——
    /// 树结构/层名/治理头衔绑定/地块归属查询）
    /// </summary>
    public class AdminDivisionTests
    {
        private static GovernmentComposition Compose(LocalSuccession mode)
        {
            var comp = new GovernmentComposition();
            comp.localSuccession = new ComponentChoice((int)mode);
            return comp;
        }

        [Test]
        public void Depth_Feudal_Fixed4()
        {
            // 分封（世袭领有）→ 固定 4 层（宗法树——与行政容量无关）
            var feudal = Compose(LocalSuccession.Hereditary);
            Assert.AreEqual(4, AdminDivisionSystem.GetAdminDepth(feudal, 0.1f), "分封低容量也 4 层");
            Assert.AreEqual(4, AdminDivisionSystem.GetAdminDepth(feudal, 0.9f), "分封高容量仍 4 层（宗法固定）");
        }

        [Test]
        public void Depth_Bureaucratic_CapacityDriven()
        {
            // 郡县（任命）→ 容量弹性 2-5
            var appointed = Compose(LocalSuccession.Appointed);
            Assert.AreEqual(2, AdminDivisionSystem.GetAdminDepth(appointed, 0.1f), "低容量 2 层（政权+一级）");
            Assert.AreEqual(3, AdminDivisionSystem.GetAdminDepth(appointed, 0.3f), "中低容量 3 层");
            Assert.AreEqual(4, AdminDivisionSystem.GetAdminDepth(appointed, 0.5f), "中容量 4 层");
            Assert.AreEqual(5, AdminDivisionSystem.GetAdminDepth(appointed, 0.8f), "高容量 5 层（上限）");

            // 考试（科举）同官僚线
            var exam = Compose(LocalSuccession.Examination);
            Assert.AreEqual(5, AdminDivisionSystem.GetAdminDepth(exam, 0.8f), "科举高容量 5 层");
        }

        [Test]
        public void Generate_Tree_Structure()
        {
            // 郡县 3 层生成：政权根→郡（2）→县（3）
            var realm = new RealmData { realmId = 1, realmName = "测试国" };
            var comp = Compose(LocalSuccession.Appointed);
            var tiles = new HashSet<int>();
            for (int i = 0; i < 40; i++) tiles.Add(i);

            var divisions = AdminDivisionSystem.Generate(realm, comp, 0.3f, tiles);
            Assert.Greater(divisions.Count, 1, "有子区划");

            // 根=层1（政权）——全领地
            var root = divisions[0];
            Assert.AreEqual(1, root.level, "根层 1");
            Assert.AreEqual("测试国", root.name, "根名=政权名");
            Assert.AreEqual(40, root.tiles.Count, "根含全部领地");

            // 有层 2（郡）和层 3（县）
            Assert.Greater(AdminDivisionSystem.AtLevel(divisions, 1, 2).Count, 0, "有郡层");
            Assert.Greater(AdminDivisionSystem.AtLevel(divisions, 1, 3).Count, 0, "有县层");

            // 治理头衔绑定（官僚线：层 2=总督/郡守级）
            var l2 = AdminDivisionSystem.AtLevel(divisions, 1, 2)[0];
            Assert.AreEqual("title_gov_general", l2.titleId, "层2官僚头衔");
            Assert.AreEqual(2, l2.level, "层2 级别");
        }

        [Test]
        public void Generate_Feudal_Tree()
        {
            // 分封 4 层：根→诸侯(2)→卿大夫(3)→士家(4)
            var realm = new RealmData { realmId = 2, realmName = "宗法国" };
            var comp = Compose(LocalSuccession.Hereditary);
            var tiles = new HashSet<int>();
            for (int i = 0; i < 60; i++) tiles.Add(i);

            var divisions = AdminDivisionSystem.Generate(realm, comp, 0.5f, tiles);
            Assert.AreEqual(4, AdminDivisionSystem.GetAdminDepth(comp, 0.5f), "分封 4 层");

            // 层 2 头衔=诸侯（分封线）
            var l2 = AdminDivisionSystem.AtLevel(divisions, 2, 2);
            Assert.Greater(l2.Count, 0, "有诸侯层");
            Assert.AreEqual("title_zhuhou", l2[0].titleId, "诸侯头衔绑定");

            // 层 4 头衔=低级爵（士家）
            var l4 = AdminDivisionSystem.AtLevel(divisions, 2, 4);
            Assert.AreEqual("title_baron", l4[0].titleId, "士家头衔");

            // 地块归属查询（某地块→最深层区划）
            var leaf = AdminDivisionSystem.DivisionOfTile(divisions, 2, 5);
            Assert.IsNotNull(leaf, "地块有归属区划");
            Assert.GreaterOrEqual(leaf.level, 2, "归属到子区");
        }
    }
}
