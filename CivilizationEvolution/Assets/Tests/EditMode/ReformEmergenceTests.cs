using System.Collections.Generic;
using NUnit.Framework;
using CivilizationEvolution.Core;
using CivilizationEvolution.Culture;
using CivilizationEvolution.Economy;
using CivilizationEvolution.Politics;
using CivilizationEvolution.Tech;

namespace CivilizationEvolution.Tests
{
    /// <summary>
    /// 政体改革 + 阶层出现事件 测试（用户定稿）
    /// 研究新革新后可改革政体成分；标志性革新完成→新阶层出现
    /// </summary>
    public class ReformEmergenceTests
    {
        private InnovationTree _tree;
        private Chronicle _chronicle;

        [SetUp]
        public void Setup()
        {
            ContentRegistry.Reset();
            ContentRegistry.Initialize();
            _tree = new InnovationTree();
            _chronicle = new Chronicle { CurrentTick = 100, CurrentYear = 5 };
        }

        private static void Complete(InnovationTree tree, int id)
        {
            Assert.IsTrue(tree.StartResearch(1, id), $"革新 {id} 应可研究");
            tree.DailyTick(1, 100000f);
        }

        // ===== 政体改革 =====

        [Test]
        public void Reform_RequiresSupportingInnovation()
        {
            var realm = new RealmData { realmId = 1, realmName = "改革国" };

            // 无支撑革新：邦联制（需部落联盟 500）不可改革
            Assert.IsFalse(GovernmentReform.CanReform(realm,
                PolityComponentInnovations.PolityDimension.SpatialStructure,
                (int)SpatialStructure.Confederal, _tree), "无部落联盟时不可改为邦联制");
            Assert.IsFalse(GovernmentReform.Reform(realm,
                PolityComponentInnovations.PolityDimension.SpatialStructure,
                (int)SpatialStructure.Confederal, _tree, _chronicle), "改革应被拒绝");

            // 完成部落联盟后：可改革
            Complete(_tree, 500);
            Assert.IsTrue(GovernmentReform.CanReform(realm,
                PolityComponentInnovations.PolityDimension.SpatialStructure,
                (int)SpatialStructure.Confederal, _tree), "有支撑革新后可改革");
        }

        [Test]
        public void Reform_AppliesComponentAndRecords()
        {
            var realm = new RealmData { realmId = 1, realmName = "改革国" };
            float stabilityBefore = realm.stability;

            Complete(_tree, 500);
            bool ok = GovernmentReform.Reform(realm,
                PolityComponentInnovations.PolityDimension.SpatialStructure,
                (int)SpatialStructure.Confederal, _tree, _chronicle);

            Assert.IsTrue(ok, "改革应成功");
            Assert.AreEqual((int)SpatialStructure.Confederal, realm.composition.spatialStructure.primary,
                "央地结构应更新为邦联制");
            Assert.That(realm.stability, Is.LessThan(stabilityBefore), "改革引发稳定性下降");

            // 编年史记录（重大）
            Assert.AreEqual(1, _chronicle.GetEntriesByType("reform").Count, "改革应记录编年史");
            Assert.IsTrue(_chronicle.GetEntriesByType("reform")[0].major, "改革为重大事件");
        }

        [Test]
        public void Reform_ToExamination_AfterKeju()
        {
            // B1 改为考试选任需科举——完整链后改革
            var realm = new RealmData { realmId = 1, realmName = "文官国" };
            Assert.IsFalse(GovernmentReform.CanReform(realm,
                PolityComponentInnovations.PolityDimension.CentralSuccession,
                (int)CentralSuccession.Examination, _tree), "无科举时不可改考试选任");

            // 完成科举链
            Complete(_tree, 200); Complete(_tree, 600); Complete(_tree, 204);
            Complete(_tree, 205); Complete(_tree, 820); Complete(_tree, 821);
            Complete(_tree, 505); Complete(_tree, 822); Complete(_tree, 807);
            Complete(_tree, 808); Complete(_tree, 823); Complete(_tree, 958);
            Complete(_tree, 959); Complete(_tree, 502); Complete(_tree, 503);
            Complete(_tree, 945); Complete(_tree, 504);

            Assert.IsTrue(GovernmentReform.Reform(realm,
                PolityComponentInnovations.PolityDimension.CentralSuccession,
                (int)CentralSuccession.Examination, _tree, _chronicle), "科举后可改革");
            Assert.AreEqual((int)CentralSuccession.Examination, realm.composition.centralSuccession.primary);
        }

        // ===== 阶层出现事件 =====

        [Test]
        public void Emergence_Coinage_BringsMerchants()
        {
            Assert.IsTrue(ContentRegistry.TryGetCulture(1, out var pack));

            // 铸币前无商人
            Assert.IsFalse(SocialClassAvailability.IsSubclassAvailable(
                GameEnums.SocialSubclass.Merchant, pack.data, _tree, 1));

            Complete(_tree, 200); Complete(_tree, 201); Complete(_tree, 700);
            Complete(_tree, 701); // 铸币

            var emerging = ClassEmergenceEvents.GetEmergingClasses(701, pack.data, _tree, 1);
            Assert.IsTrue(emerging.Contains(GameEnums.SocialSubclass.Merchant), "铸币完成→商人阶层出现");
            StringAssert.Contains("商人", ClassEmergenceEvents.GetEventText(GameEnums.SocialSubclass.Merchant));
        }

        [Test]
        public void Emergence_Manorialism_BringsSerfs()
        {
            Complete(_tree, 500); Complete(_tree, 501); Complete(_tree, 952);

            var emerging = ClassEmergenceEvents.GetEmergingClasses(952, null, _tree, 1);
            Assert.IsTrue(emerging.Contains(GameEnums.SocialSubclass.Serf), "庄园制度→农奴阶层出现");
        }

        [Test]
        public void Emergence_ScriptAlone_NotScholar()
        {
            // 文字完成但官僚未完成 → 士人次级条件不足，不出现
            Complete(_tree, 200); Complete(_tree, 600); // 仅文字

            var emerging = ClassEmergenceEvents.GetEmergingClasses(600, null, _tree, 1);
            Assert.IsFalse(emerging.Contains(GameEnums.SocialSubclass.Scholar),
                "仅文字无官僚时士人不应出现（完整可用性判定）");
        }

        [Test]
        public void Emergence_RecordsChronicle()
        {
            Complete(_tree, 200); Complete(_tree, 201); Complete(_tree, 700);
            Complete(_tree, 701);

            int count = ClassEmergenceEvents.RecordEmergence(701, "商贾国", null, _tree, 1, _chronicle);
            Assert.GreaterOrEqual(count, 1, "应有阶层出现记录");
            Assert.AreEqual(1, _chronicle.GetEntriesByType("class_emergence").Count, "编年史记录阶层出现");
        }

        [Test]
        public void Emergence_OnInnovationCompletedEvent()
        {
            // 完成事件接线验证
            int completedId = -1;
            _tree.OnInnovationCompleted += (realmId, innovationId) => completedId = innovationId;

            Complete(_tree, 200); Complete(_tree, 201); Complete(_tree, 700);
            Complete(_tree, 701);

            Assert.AreEqual(701, completedId, "完成事件应触发");
        }
    }
}
