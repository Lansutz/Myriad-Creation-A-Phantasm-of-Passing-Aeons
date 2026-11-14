using System.Collections.Generic;
using NUnit.Framework;
using MyriadCreation.Content.Definitions;
using MyriadCreation.Simulation.ContentHost;

namespace MyriadCreation.Tests.EditMode
{
    /// <summary>
    /// P9/M1 革新目录审计：数量、领域覆盖、时代覆盖、来源可核查、前置无环。
    /// 这些是内容层验收，不依赖运行时世界。
    /// </summary>
    public class InnovationCatalogAuditTests
    {
        private const int MinTotal = 420;
        private const int MinPerField = 10;
        private const int MinEra5 = 30;

        [SetUp]
        public void Setup()
        {
            ContentRegistry.Reset();
            ContentRegistry.Initialize();
        }

        private static List<InnovationDef> All()
        {
            var result = new List<InnovationDef>();
            foreach (var pair in ContentRegistry.Innovations)
            {
                if (pair.Value != null) result.Add(pair.Value);
            }
            return result;
        }

        [Test]
        public void Catalog_ReachesTotalAndFieldCoverage()
        {
            var all = All();
            Assert.GreaterOrEqual(all.Count, MinTotal, "革新总数不足");

            var perField = new Dictionary<InnovationField, int>();
            var era5 = 0;
            foreach (var def in all)
            {
                perField.TryGetValue(def.field, out int count);
                perField[def.field] = count + 1;
                if (def.era == 5) era5++;
            }

            var problems = new List<string>();
            foreach (InnovationField field in System.Enum.GetValues(typeof(InnovationField)))
            {
                perField.TryGetValue(field, out int count);
                if (count < MinPerField) problems.Add($"{field}: {count} < {MinPerField}");
            }
            Assert.IsEmpty(problems, "领域覆盖不足：\n" + string.Join("\n", problems));
            Assert.GreaterOrEqual(era5, MinEra5, "1500 年后的革新条目不足");
        }

        [Test]
        public void EveryEntry_HasVerifiableSource()
        {
            var problems = new List<string>();
            foreach (var def in All())
            {
                if (def.sources == null || def.sources.Count == 0)
                {
                    problems.Add($"{def.innovationId} {def.innovationName} 缺来源");
                    continue;
                }
                foreach (string source in def.sources)
                {
                    if (string.IsNullOrWhiteSpace(source) || source.Contains("待补"))
                    {
                        problems.Add($"{def.innovationId} {def.innovationName} 来源不可核查：{source}");
                        break;
                    }
                }
            }
            Assert.IsEmpty(problems, string.Join("\n", problems));
        }

        [Test]
        public void PrerequisiteGraph_IsAcyclic()
        {
            var all = All();
            var byId = new Dictionary<int, InnovationDef>();
            foreach (var def in all) byId[def.innovationId] = def;

            var state = new Dictionary<int, int>();
            var problems = new List<string>();

            void Visit(int id, Stack<int> path)
            {
                state.TryGetValue(id, out int mark);
                if (mark == 2) return;
                if (mark == 1)
                {
                    problems.Add("前置成环：" + string.Join(" -> ", path) + " -> " + id);
                    return;
                }
                state[id] = 1;
                path.Push(id);
                if (byId.TryGetValue(id, out var def))
                {
                    foreach (int prerequisite in def.GetPrerequisiteIds())
                    {
                        Visit(prerequisite, path);
                    }
                }
                path.Pop();
                state[id] = 2;
            }

            foreach (var def in all) Visit(def.innovationId, new Stack<int>());
            Assert.IsEmpty(problems, string.Join("\n", problems));
        }

        [Test]
        public void IdsAndNames_AreUnique()
        {
            var ids = new HashSet<int>();
            var names = new HashSet<string>();
            var problems = new List<string>();
            foreach (var def in All())
            {
                if (!ids.Add(def.innovationId)) problems.Add("重复 id：" + def.innovationId);
                if (!string.IsNullOrEmpty(def.innovationName) && !names.Add(def.innovationName))
                {
                    problems.Add("重复名称：" + def.innovationName);
                }
            }
            Assert.IsEmpty(problems, string.Join("\n", problems));
        }
    }
}
