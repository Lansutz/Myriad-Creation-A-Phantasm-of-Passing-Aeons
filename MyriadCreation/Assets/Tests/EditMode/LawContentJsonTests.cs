using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using MyriadCreation.Simulation.Laws;
using UnityEngine;
using MyriadCreation.Simulation.LawHost;

namespace MyriadCreation.Tests.EditMode
{
    /// <summary>
    /// 法律定义 JSON 化（定义与脚本分离）测试：
    /// StreamingAssets/Base/Laws/Laws.json 存在且能解析；解析结果与测试基准 <see cref="LawCatalogReference"/>
    /// （原完整硬编码目录迁移至测试侧）逐字段等值；文件缺失/损坏时生产代码回退**最小回退集**不抛；
    /// 重复加载幂等；加载后 DefaultLawCatalog/CreateGroups 走 JSON 内容。
    /// </summary>
    public class LawContentJsonTests
    {
        [SetUp]
        public void SetUp()
        {
            LawContentLoader.Reset();
        }

        [TearDown]
        public void TearDown()
        {
            LawContentLoader.Reset();
        }

        private static int CountLaws(IReadOnlyList<LawGroupDef> groups)
        {
            int num = 0;
            foreach (LawGroupDef group in groups)
            {
                if (group?.laws != null)
                {
                    num += group.laws.Count;
                }
            }
            return num;
        }

        // ===================== 1. JSON 存在且可解析 =====================

        [Test]
        public void LawsJson_ExistsAndParses_WithExpectedCounts()
        {
            string path = LawContentLoader.ResolveDefaultPath();
            Assert.IsFalse(string.IsNullOrEmpty(path), "Application.streamingAssetsPath 应可用");
            Assert.IsTrue(File.Exists(path), "法律定义 JSON 应存在：" + path);

            Assert.IsTrue(LawContentLoader.LoadFromFile(path), LawContentLoader.LastError);
            Assert.IsTrue(LawContentLoader.HasContent);

            List<LawGroupDef> referenceGroups = LawCatalogReference.CreateGroups();
            List<DecreeDef> referenceDecrees = LawCatalogReference.CreateDecrees();
            Assert.AreEqual(referenceGroups.Count, LawContentLoader.Groups.Count, "组数量应与测试基准（JSON 对照）一致");
            Assert.AreEqual(12, LawContentLoader.Groups.Count, "默认目录应为 12 组");
            Assert.AreEqual(50, CountLaws(LawContentLoader.Groups), "默认目录应为 50 条法律");
            Assert.AreEqual(referenceDecrees.Count, LawContentLoader.Decrees.Count, "法令数量应与测试基准一致");
            Assert.AreEqual(2, LawContentLoader.Decrees.Count);
        }

        // ===================== 2. 解析内容与硬编码默认逐字段等值 =====================

        [Test]
        public void LawsJson_FieldsMatchHardcodedDefaults()
        {
            string path = LawContentLoader.ResolveDefaultPath();
            Assert.IsTrue(LawContentLoader.LoadFromFile(path), LawContentLoader.LastError);

            List<LawGroupDef> defaults = LawCatalogReference.CreateGroups();
            Assert.AreEqual(defaults.Count, LawContentLoader.Groups.Count);
            for (int i = 0; i < defaults.Count; i++)
            {
                LawGroupDef expectedGroup = defaults[i];
                LawGroupDef actualGroup = LawContentLoader.Groups[i];
                Assert.AreEqual(expectedGroup.groupId, actualGroup.groupId);
                Assert.AreEqual(expectedGroup.name, actualGroup.name);
                Assert.AreEqual(expectedGroup.order, actualGroup.order);
                Assert.AreEqual(expectedGroup.description, actualGroup.description);
                Assert.AreEqual(expectedGroup.laws.Count, actualGroup.laws.Count, "组 " + expectedGroup.groupId + " 法数量");

                Dictionary<string, LawDef> actualById = new Dictionary<string, LawDef>();
                foreach (LawDef law in actualGroup.laws)
                {
                    actualById[law.lawId] = law;
                }
                foreach (LawDef expected in expectedGroup.laws)
                {
                    Assert.IsTrue(actualById.ContainsKey(expected.lawId), "JSON 缺少法律 " + expected.lawId);
                    AssertLawEqual(expected, actualById[expected.lawId]);
                }
            }

            Dictionary<string, DecreeDef> actualDecrees = new Dictionary<string, DecreeDef>();
            foreach (DecreeDef decree in LawContentLoader.Decrees)
            {
                actualDecrees[decree.decreeId] = decree;
            }
            foreach (DecreeDef expected in LawCatalogReference.CreateDecrees())
            {
                Assert.IsTrue(actualDecrees.ContainsKey(expected.decreeId), "JSON 缺少法令 " + expected.decreeId);
                AssertDecreeEqual(expected, actualDecrees[expected.decreeId]);
            }
        }

        private static void AssertLawEqual(LawDef expected, LawDef actual)
        {
            Assert.AreEqual(expected.lawId, actual.lawId);
            Assert.AreEqual(expected.groupId, actual.groupId);
            Assert.AreEqual(expected.name, actual.name);
            Assert.AreEqual(expected.description, actual.description);
            Assert.AreEqual(expected.tier, actual.tier);
            Assert.AreEqual(expected.enactmentCostTreasury, actual.enactmentCostTreasury, 0.001f);
            Assert.AreEqual(expected.enactmentCostPrestige, actual.enactmentCostPrestige, 0.001f);
            Assert.AreEqual(expected.upkeepPerYear, actual.upkeepPerYear, 0.001f);
            Assert.AreEqual(expected.isInitiallyKnown, actual.isInitiallyKnown, "isInitiallyKnown " + expected.lawId);
            Assert.AreEqual(expected.enactmentDays, actual.enactmentDays, "enactmentDays " + expected.lawId);
            Assert.AreEqual(expected.proposalAuthority, actual.proposalAuthority, "authority " + expected.lawId);
            Assert.AreEqual(expected.requiredInstitution, actual.requiredInstitution, "requiredInstitution " + expected.lawId);

            AssertLawRequirementEqual(expected.requirements, actual.requirements, expected.lawId);
            AssertLawEffectEqual(expected.effects, actual.effects, expected.lawId);
        }

        private static void AssertLawRequirementEqual(LawRequirement expected, LawRequirement actual, string id)
        {
            Assert.IsNotNull(actual, "requirements 缺失 " + id);
            Assert.AreEqual(expected.minTreasury, actual.minTreasury, 0.001f);
            Assert.AreEqual(expected.minLegitimacy, actual.minLegitimacy, 0.001f);
            Assert.AreEqual(expected.minCentralization, actual.minCentralization, 0.001f);
            CollectionAssert.AreEqual(expected.requiredLawIds, actual.requiredLawIds, "requiredLawIds " + id);
            CollectionAssert.AreEqual(expected.requiredInnovationIds, actual.requiredInnovationIds, "requiredInnovationIds " + id);
        }

        private static void AssertLawEffectEqual(LawEffect expected, LawEffect actual, string id)
        {
            Assert.IsNotNull(actual, "effects 缺失 " + id);
            Assert.AreEqual(expected.taxMultiplier, actual.taxMultiplier, 0.001f, "taxMultiplier " + id);
            Assert.AreEqual(expected.stabilityPerYear, actual.stabilityPerYear, 0.001f, "stabilityPerYear " + id);
            Assert.AreEqual(expected.legitimacyPerYear, actual.legitimacyPerYear, 0.001f, "legitimacyPerYear " + id);
            Assert.AreEqual(expected.manpowerMultiplier, actual.manpowerMultiplier, 0.001f, "manpowerMultiplier " + id);
            Assert.AreEqual(expected.unrestPerYear, actual.unrestPerYear, 0.001f, "unrestPerYear " + id);
        }

        private static void AssertDecreeEqual(DecreeDef expected, DecreeDef actual)
        {
            Assert.AreEqual(expected.decreeId, actual.decreeId);
            Assert.AreEqual(expected.name, actual.name);
            Assert.AreEqual(expected.description, actual.description);
            Assert.AreEqual(expected.costTreasury, actual.costTreasury, 0.001f);
            Assert.AreEqual(expected.costAuthority, actual.costAuthority, 0.001f);
            Assert.AreEqual(expected.durationDays, actual.durationDays);
            Assert.AreEqual(expected.cooldownDays, actual.cooldownDays);
            AssertLawRequirementEqual(expected.requirements, actual.requirements, expected.decreeId);
            AssertLawEffectEqual(expected.effects, actual.effects, expected.decreeId);
        }

        // ===================== 3. 数据驱动的 DefaultLawCatalog =====================

        [Test]
        public void DefaultLawCatalog_UsesLoadedJson_AndFallsBackToMinimalSet()
        {
            Assert.IsTrue(LawContentLoader.LoadFromFile(LawContentLoader.ResolveDefaultPath()), LawContentLoader.LastError);

            List<LawGroupDef> loadedViaCatalog = DefaultLawCatalog.CreateGroups();
            Assert.AreEqual(12, loadedViaCatalog.Count);
            Assert.AreEqual(50, CountLaws(loadedViaCatalog), "加载 JSON 后 CreateGroups 应返回 JSON 内容");

            // 返回的是深拷贝：修改不影响后续调用
            loadedViaCatalog[0].laws[0].name = "被外部改掉";
            Assert.AreNotEqual("被外部改掉", DefaultLawCatalog.CreateGroups()[0].laws[0].name, "CreateGroups 返回深拷贝");

            // JSON 缺失时生产代码只回退最小集（task-87：完整 9 组/32 法已迁到测试基准 LawCatalogReference）
            string missing = Path.Combine(Path.GetTempPath(), "laws_json_should_not_exist_12345.json");
            if (File.Exists(missing))
            {
                File.Delete(missing);
            }
            Assert.IsFalse(LawContentLoader.LoadFromFile(missing), "缺失文件应返回 false");
            List<LawGroupDef> fallbackGroups = DefaultLawCatalog.CreateGroups();
            Assert.AreEqual(2, fallbackGroups.Count, "最小回退目录应为 2 组（taxation/military）");
            Assert.AreEqual(5, CountLaws(fallbackGroups), "最小回退目录应为 5 条核心法律");
            Assert.AreEqual(1, DefaultLawCatalog.CreateDecrees().Count, "最小回退目录应为 1 条法令");
        }

        // ===================== 4. 缺失/损坏回退 =====================

        [Test]
        public void MissingOrCorruptJson_FallsBackToMinimalSet_WithoutThrow()
        {
            string missing = Path.Combine(Path.GetTempPath(), "laws_json_should_not_exist_12345.json");
            if (File.Exists(missing))
            {
                File.Delete(missing);
            }

            Assert.IsFalse(LawContentLoader.LoadFromFile(missing), "缺失文件应返回 false");
            Assert.IsFalse(LawContentLoader.HasContent);
            Assert.IsFalse(string.IsNullOrEmpty(LawContentLoader.LastError), "缺失文件应记录失败原因（日志可追溯）");

            List<LawGroupDef> fallbackGroups = DefaultLawCatalog.CreateGroups();
            Assert.AreEqual(2, fallbackGroups.Count, "缺失文件回退最小 2 组（taxation/military）");
            Assert.AreEqual(5, CountLaws(fallbackGroups), "缺失文件回退 5 条核心法律");
            Assert.AreEqual(1, DefaultLawCatalog.CreateDecrees().Count, "缺失文件回退 1 条法令");
            Dictionary<string, LawDef> fallbackLaws = DefaultLawCatalog.IndexLaws(fallbackGroups);
            string[] coreLawIds = { "tax_light", "tax_standard", "tax_heavy", "militia", "standing_army" };
            for (int i = 0; i < coreLawIds.Length; i++)
            {
                Assert.IsTrue(fallbackLaws.ContainsKey(coreLawIds[i]), "最小回退应包含核心法律 " + coreLawIds[i]);
            }

            Assert.IsFalse(LawContentLoader.ApplyJson(""), "空内容应返回 false");
            Assert.IsFalse(LawContentLoader.ApplyJson("{ this is not json"), "损坏 JSON 应返回 false 不抛");
            Assert.IsFalse(LawContentLoader.HasContent);
            Assert.IsFalse(string.IsNullOrEmpty(LawContentLoader.LastError), "损坏 JSON 应记录失败原因");
            Assert.AreEqual(5, CountLaws(DefaultLawCatalog.CreateGroups()), "损坏 JSON 回退 5 条核心法律");
        }

        // ===================== 5. 幂等 =====================

        [Test]
        public void LoadFromFile_IsIdempotent()
        {
            string path = LawContentLoader.ResolveDefaultPath();
            Assert.IsTrue(LawContentLoader.LoadFromFile(path));
            int groupsFirst = LawContentLoader.Groups.Count;
            int lawsFirst = CountLaws(LawContentLoader.Groups);
            int decreesFirst = LawContentLoader.Decrees.Count;

            Assert.IsTrue(LawContentLoader.LoadFromFile(path), "重复加载应成功");
            Assert.AreEqual(groupsFirst, LawContentLoader.Groups.Count, "重复加载不应累积组");
            Assert.AreEqual(lawsFirst, CountLaws(LawContentLoader.Groups), "重复加载不应累积法律");
            Assert.AreEqual(decreesFirst, LawContentLoader.Decrees.Count, "重复加载不应累积法令");

            Assert.IsTrue(LawContentLoader.EnsureLoaded());
            Assert.AreEqual(groupsFirst, LawContentLoader.Groups.Count);
            Assert.AreEqual(50, CountLaws(DefaultLawCatalog.CreateGroups()));
        }
    }
}
