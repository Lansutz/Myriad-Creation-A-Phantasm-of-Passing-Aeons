using System.Collections.Generic;
using NUnit.Framework;
using MyriadCreation.Content.Definitions;
using MyriadCreation.Simulation.ContentHost;

namespace MyriadCreation.Tests.EditMode
{
    /// <summary>
    /// 革新内容 schema：新条目（contentSchemaVersion=2）必须有定义、英文名、类型、领域、前置理由与解锁。
    /// 旧 185 条（schema 0/1）暂作为 legacy 豁免，迁移在后续批次逐条完成。
    /// </summary>
    public class InnovationSchemaTests
    {
        [SetUp]
        public void Setup()
        {
            ContentRegistry.Reset();
            ContentRegistry.Initialize();
        }

        private static IEnumerable<InnovationDef> PreciseEntries()
        {
            foreach (var pair in ContentRegistry.Innovations)
            {
                if (pair.Value != null && pair.Value.contentSchemaVersion >= 2)
                {
                    yield return pair.Value;
                }
            }
        }

        [Test]
        public void PreciseEntries_HaveRequiredSchemaFields()
        {
            var problems = new List<string>();
            int count = 0;
            foreach (var def in PreciseEntries())
            {
                count++;
                string id = def.innovationId.ToString();
                if (string.IsNullOrEmpty(def.innovationName)) problems.Add(id + " 缺 innovationName");
                if (string.IsNullOrEmpty(def.termEn)) problems.Add(id + " 缺 termEn");
                if (string.IsNullOrEmpty(def.definition)) problems.Add(id + " 缺 definition");
                if (string.IsNullOrEmpty(def.scope)) problems.Add(id + " 缺 scope");
                if (string.IsNullOrEmpty(def.category)) problems.Add(id + " 缺 category");
                if (def.kind == InnovationKind.Unknown) problems.Add(id + " 缺 kind");
                if (def.unlocks == null || def.unlocks.Count == 0) problems.Add(id + " 缺 unlocks");
            }
            Assert.Greater(count, 0, "应至少有一个 contentSchemaVersion=2 的精确条目");
            Assert.IsEmpty(problems, string.Join("\n", problems));
        }

        [Test]
        public void PreciseEntries_EveryPrerequisiteHasRationale()
        {
            var problems = new List<string>();
            foreach (var def in PreciseEntries())
            {
                var rationaleById = new Dictionary<int, string>();
                if (def.prerequisiteRationale != null)
                {
                    foreach (var entry in def.prerequisiteRationale)
                    {
                        if (entry != null && !string.IsNullOrEmpty(entry.rationale))
                        {
                            rationaleById[entry.prerequisiteId] = entry.rationale;
                        }
                    }
                }
                foreach (int prerequisite in def.GetPrerequisiteIds())
                {
                    if (!rationaleById.ContainsKey(prerequisite))
                    {
                        problems.Add(def.innovationId + " 的前置 " + prerequisite + " 缺 rationale");
                    }
                }
            }
            Assert.IsEmpty(problems, string.Join("\n", problems));
        }

        [Test]
        public void PreciseEntries_UsePreciseNames()
        {
            var banned = new HashSet<string>
            {
                "官僚制度", "中央集权", "中央集权思想", "中央集权行政",
                "印刷术", "公民权", "边界概念", "朝贡体系", "封建制度",
                "官僚帝国", "城邦制", "王国", "帝国",
                "吕公车", "轒辒车", "野驴投石机", "配重投石机",
            };
            var problems = new List<string>();
            foreach (var def in PreciseEntries())
            {
                if (banned.Contains(def.innovationName))
                {
                    problems.Add(def.innovationId + " 仍是伞形/分析术语：" + def.innovationName);
                }
                if (def.innovationName.Contains("术") || def.innovationName.Contains("思想"))
                {
                    problems.Add(def.innovationId + " 名称含泛化后缀：" + def.innovationName);
                }
                if (def.innovationName.Contains("·"))
                {
                    problems.Add(def.innovationId + " 名称是复合式命名，应拆成短名+轴标签：" + def.innovationName);
                }
            }
            Assert.IsEmpty(problems, string.Join("\n", problems));
        }

        [Test]
        public void PreciseEntries_LegacyIdsAndVariantTargetsAreValid()
        {
            var problems = new List<string>();
            foreach (var def in PreciseEntries())
            {
                if (def.legacyIds != null)
                {
                    foreach (int legacyId in def.legacyIds)
                    {
                        if (legacyId == def.innovationId)
                        {
                            problems.Add(def.innovationId + " legacyIds 含自身");
                        }
                        if (ContentRegistry.Innovations.ContainsKey(legacyId))
                        {
                            problems.Add(def.innovationId + " legacyIds " + legacyId + " 仍是独立条目");
                        }
                    }
                }
                if (def.variantOf >= 0 && !ContentRegistry.Innovations.ContainsKey(def.variantOf))
                {
                    problems.Add(def.innovationId + " variantOf " + def.variantOf + " 不存在");
                }
            }
            Assert.IsEmpty(problems, string.Join("\n", problems));
        }
    }
}
