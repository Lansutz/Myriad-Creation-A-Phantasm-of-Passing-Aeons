using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;

namespace MyriadCreation.Tests.EditMode
{
    /// <summary>
    /// B8-part1：程序集架构门禁（只读 asmdef，不改业务）。
    /// 解析 Assets/**/*.asmdef 的 name/references，断言：
    /// 1) 依赖图无环；2) Content 边界；3) Laws/Faith/Culture refs ⊆ {Core, Content}，且禁引用
    /// Simulation/World/Rendering/Infrastructure/App/UI/App.Contracts；
    /// 4) App 不引 UI；UI 必引 App 且不得引用 Simulation/Laws/Faith/Culture；
    /// 5) 分层序无向上引用；Rendering refs 必须恰为 {Core, Content}（不引 World/Simulation/App.Contracts 等）；
    /// 6) Editor/Tests 是显式豁免（可引用运行时全家），运行时程序集不得反向引用 Tests/Editor；
    /// 失败信息给出具体 asmdef 路径/引用对，便于定位。
    /// </summary>
    public class AsmdefArchitectureGateTests
    {
        private sealed class AsmdefInfo
        {
            public string Name;
            public string Path;
            public List<string> References;
        }

        // 卡片分层序：Core < Content < {Laws, Faith} < World < Simulation < {Rendering, Infrastructure} < App < UI
        private static readonly Dictionary<string, int> LayerRank = new Dictionary<string, int>(StringComparer.Ordinal)
        {
            { "MyriadCreation.Core", 0 },
            { "MyriadCreation.Content", 1 },
            { "MyriadCreation.Laws", 2 },
            { "MyriadCreation.Faith", 2 },
            { "MyriadCreation.Culture", 2 },
            { "MyriadCreation.World", 3 },
            { "MyriadCreation.Simulation", 4 },
            { "MyriadCreation.Rendering", 5 },
            { "MyriadCreation.Infrastructure", 5 },
            // App.Contracts 与 App 同层（契约程序集）；供 App/UI 使用，Rendering 不再引用。
            { "MyriadCreation.App.Contracts", 6 },
            { "MyriadCreation.App", 6 },
            { "MyriadCreation.UI", 7 },
        };

        // 显式豁免清单：测试/编辑器允许引用运行时全家；运行时程序集不得反向引用它们。
        private static readonly HashSet<string> ExplicitlyExemptAsmdefs = new HashSet<string>(StringComparer.Ordinal)
        {
            "MyriadCreation.Editor",
            "MyriadCreation.Tests",
            "MyriadCreation.Tests.PlayMode",
        };

        private static bool IsTestOrEditorAsmdef(string name, string path = null)
        {
            if (!string.IsNullOrEmpty(name))
            {
                if (ExplicitlyExemptAsmdefs.Contains(name)
                    || name.StartsWith("MyriadCreation.Tests", StringComparison.Ordinal)
                    || name.StartsWith("MyriadCreation.Editor", StringComparison.Ordinal))
                {
                    return true;
                }
            }

            // asmdef 名称可自定义：路径含 /Tests/ 或 /Editor/ 同样视为测试/编辑器程序集。
            if (!string.IsNullOrEmpty(path))
            {
                string normalized = path.Replace('\\', '/');
                if (normalized.Contains("/Tests/") || normalized.Contains("/Editor/")) return true;
            }
            return false;
        }

        private static bool IsTestOrEditorReference(string name)
        {
            if (string.IsNullOrEmpty(name)) return false;
            return name.StartsWith("MyriadCreation.Tests", StringComparison.Ordinal)
                || name.StartsWith("MyriadCreation.Editor", StringComparison.Ordinal);
        }

        private const string UiName = "MyriadCreation.UI";
        private const string SimulationName = "MyriadCreation.Simulation";
        private const string AppName = "MyriadCreation.App";

        // B8-final-gate：UI 不得直接引用的 Simulation 家族程序集。
        private static readonly string[] UiForbiddenFamily =
        {
            SimulationName,
            "MyriadCreation.Laws",
            "MyriadCreation.Faith",
            "MyriadCreation.Culture",
        };

        private static readonly Regex NameRegex = new Regex(@"""name""\s*:\s*""([^""]+)""");
        private static readonly Regex ReferencesRegex = new Regex(@"""references""\s*:\s*\[(.*?)\]", RegexOptions.Singleline);
        private static readonly Regex QuotedRegex = new Regex(@"""([^""]+)""");

        [Test]
        public void AsmdefArchitecture_FollowsLayeringAndBoundaryRules()
        {
            var asmdefs = LoadAsmdefs();
            var violations = new List<string>();

            CheckAcyclic(asmdefs, violations);
            CheckContentBoundary(asmdefs, violations);
            CheckCoreContentSubsystems(asmdefs, violations);
            CheckSubsystemForbiddenRefs(asmdefs, violations);
            CheckAppBoundary(asmdefs, violations);
            CheckUiBoundary(asmdefs, violations);
            CheckRenderingBoundary(asmdefs, violations);
            CheckRuntimeAssembliesDoNotReferenceTestsOrEditor(asmdefs, violations);
            CheckLayering(asmdefs, violations);

            Assert.That(violations, Is.Empty,
                "asmdef 架构违规（请按条修复，不要放宽门禁）：\n - " + string.Join("\n - ", violations));
        }

        [Test]
        public void UI_DoesNotReferenceSimulationFamily()
        {
            var asmdefs = LoadAsmdefs();
            var violations = new List<string>();
            AsmdefInfo ui = Require(asmdefs, UiName, violations);
            Assert.That(violations, Is.Empty, string.Join("\n", violations));

            var forbiddenRefs = ui.References
                .Where(reference => Array.IndexOf(UiForbiddenFamily, reference) >= 0)
                .ToList();

            Assert.That(forbiddenRefs, Is.Empty,
                "UI 不得引用 Simulation 家族程序集：\n - 违规 refs: " + string.Join(", ", forbiddenRefs) +
                "\n UI 实际 refs: " + string.Join(", ", ui.References));
        }

        [Test]
        public void RuntimeAssemblies_DoNotReferenceTestsOrEditor()
        {
            var asmdefs = LoadAsmdefs();
            var violations = new List<string>();
            CheckRuntimeAssembliesDoNotReferenceTestsOrEditor(asmdefs, violations);
            Assert.That(violations, Is.Empty,
                "运行时程序集不得引用 Tests/Editor（豁免只允许反向）：" + string.Join(", ", violations));
        }

        [Test]
        public void Editor_And_Tests_AreExplicitlyExempted()
        {
            var asmdefs = LoadAsmdefs();
            var missing = ExplicitlyExemptAsmdefs.Where(name => !asmdefs.ContainsKey(name)).ToList();
            Assert.That(missing, Is.Empty,
                "缺少显式豁免 asmdef：" + string.Join(", ", missing) +
                "；豁免清单 = " + string.Join(", ", ExplicitlyExemptAsmdefs));

            var notExempt = asmdefs.Keys
                .Where(name => IsTestOrEditorAsmdef(name) && !ExplicitlyExemptAsmdefs.Contains(name))
                .ToList();
            Assert.That(notExempt, Is.Empty,
                "发现未显式登记的 Tests/Editor asmdef：" + string.Join(", ", notExempt));
        }

        [Test]
        public void Rendering_DoesNotReferenceAppContracts()
        {
            var asmdefs = LoadAsmdefs();
            var rendering = Require(asmdefs, "MyriadCreation.Rendering", new List<string>());
            Assert.That(rendering.References.Contains("MyriadCreation.App.Contracts"), Is.False,
                "Rendering 不得引用 App.Contracts（B8 后 IMapEditCommandService 已下沉 Rendering/Editing；" +
                "references=" + string.Join(", ", rendering.References) + "）");
        }

        [Test]
        public void Rendering_ReferencesOnlyCoreAndContent()
        {
            var asmdefs = LoadAsmdefs();
            var rendering = Require(asmdefs, "MyriadCreation.Rendering", new List<string>());
            CollectionAssert.AreEquivalent(
                new[] { "MyriadCreation.Core", "MyriadCreation.Content" },
                rendering.References,
                "Rendering refs 必须恰为 {Core, Content}（B7-part3 去 World 后）；实际 = " + string.Join(", ", rendering.References));
        }

        // ---------------- 规则实现 ----------------

        private static void CheckAcyclic(Dictionary<string, AsmdefInfo> asmdefs, List<string> violations)
        {
            string cycle = FindCycle(asmdefs);
            if (cycle != null)
            {
                violations.Add("依赖图存在环：" + cycle);
            }
        }

        private static void CheckContentBoundary(Dictionary<string, AsmdefInfo> asmdefs, List<string> violations)
        {
            var content = Require(asmdefs, "MyriadCreation.Content", violations);
            var forbidden = new HashSet<string>(StringComparer.Ordinal)
            {
                "MyriadCreation.Simulation",
                "MyriadCreation.World",
                "MyriadCreation.UI",
                "MyriadCreation.Rendering",
                "MyriadCreation.Infrastructure",
                "MyriadCreation.App",
            };

            foreach (string reference in content.References)
            {
                if (forbidden.Contains(reference))
                {
                    violations.Add("Content 不得引用 " + reference + "（" + PathOf(content) + " 的 references 含禁用依赖）");
                }
            }
        }

        private static void CheckCoreContentSubsystems(Dictionary<string, AsmdefInfo> asmdefs, List<string> violations)
        {
            var allowed = new HashSet<string>(StringComparer.Ordinal) { "MyriadCreation.Core", "MyriadCreation.Content" };

            // task-86：Culture 程序集已存在；与 Laws/Faith 一样强制断言，不再跳过。
            foreach (string name in new[] { "MyriadCreation.Laws", "MyriadCreation.Faith", "MyriadCreation.Culture" })
            {
                var info = Require(asmdefs, name, violations);
                foreach (string reference in info.References)
                {
                    if (!allowed.Contains(reference))
                    {
                        violations.Add(name + " refs 超出 {Core, Content}：" + name + " -> " + reference + "（" + PathOf(info) + "）");
                    }
                }
            }
        }

        /// <summary>task-86：Laws/Faith/Culture 不得出现上层依赖（防回退）。</summary>
        private static void CheckSubsystemForbiddenRefs(Dictionary<string, AsmdefInfo> asmdefs, List<string> violations)
        {
            var forbidden = new HashSet<string>(StringComparer.Ordinal)
            {
                "MyriadCreation.Simulation",
                "MyriadCreation.World",
                "MyriadCreation.Rendering",
                "MyriadCreation.Infrastructure",
                "MyriadCreation.App",
                "MyriadCreation.UI",
                "MyriadCreation.App.Contracts",
            };

            foreach (string name in new[] { "MyriadCreation.Laws", "MyriadCreation.Faith", "MyriadCreation.Culture" })
            {
                var info = Require(asmdefs, name, violations);
                foreach (string reference in info.References)
                {
                    if (forbidden.Contains(reference))
                    {
                        violations.Add(name + " 不得引用 " + reference + "（" + PathOf(info) + "）");
                    }
                }
            }
        }

        private static void CheckAppBoundary(Dictionary<string, AsmdefInfo> asmdefs, List<string> violations)
        {
            var app = Require(asmdefs, AppName, violations);
            if (app.References.Contains(UiName))
            {
                violations.Add("App 不得引用 UI：" + AppName + " -> " + UiName + "（" + PathOf(app) + "）");
            }
        }

        /// <summary>B8-final-gate：UI 必引 App，且不得引 Simulation/Laws/Faith/Culture（防回退）。</summary>
        private static void CheckUiBoundary(Dictionary<string, AsmdefInfo> asmdefs, List<string> violations)
        {
            var ui = Require(asmdefs, UiName, violations);
            if (!ui.References.Contains(AppName))
            {
                violations.Add("UI 必须引用 App：" + UiName + " 的 references 缺少 " + AppName + "（" + PathOf(ui) + "）");
            }
            foreach (string forbidden in UiForbiddenFamily)
            {
                if (ui.References.Contains(forbidden))
                {
                    violations.Add("UI 不得引用 " + forbidden + "（" + PathOf(ui) + " 的 refs 含 Simulation 家族）");
                }
            }
        }

        /// <summary>B7-part3：Rendering 最终 refs 恰为 {Core, Content}；禁止 World/Simulation/App.Contracts 等（防回退）。</summary>
        private static void CheckRenderingBoundary(Dictionary<string, AsmdefInfo> asmdefs, List<string> violations)
        {
            var rendering = Require(asmdefs, "MyriadCreation.Rendering", violations);
            var expected = new HashSet<string>(StringComparer.Ordinal) { "MyriadCreation.Core", "MyriadCreation.Content" };
            foreach (string reference in rendering.References)
            {
                if (!expected.Contains(reference))
                {
                    violations.Add("Rendering refs 只允许 Core/Content：MyriadCreation.Rendering -> " + reference + "（" + PathOf(rendering) + "）");
                }
            }
            foreach (string required in expected)
            {
                if (!rendering.References.Contains(required))
                {
                    violations.Add("Rendering refs 缺少必需依赖：" + required + "（" + PathOf(rendering) + "）");
                }
            }
        }

        /// <summary>运行时程序集不得反向引用测试/编辑器（Editor/Tests 反之允许引用运行时全家）。</summary>
        private static void CheckRuntimeAssembliesDoNotReferenceTestsOrEditor(
            Dictionary<string, AsmdefInfo> asmdefs, List<string> violations)
        {
            foreach (var source in asmdefs.Values)
            {
                if (IsTestOrEditorAsmdef(source.Name, source.Path)) continue;
                foreach (string reference in source.References)
                {
                    if (IsTestOrEditorReference(reference))
                    {
                        violations.Add("运行时程序集不得引用 Tests/Editor：" + source.Name + " -> " + reference +
                            "（" + PathOf(source) + "）");
                    }
                }
            }
        }

        private static void CheckLayering(Dictionary<string, AsmdefInfo> asmdefs, List<string> violations)
        {
            CheckOrder(violations, "MyriadCreation.Core", "MyriadCreation.Content");
            CheckOrder(violations, "MyriadCreation.Content", "MyriadCreation.Laws");
            CheckOrder(violations, "MyriadCreation.Content", "MyriadCreation.Faith");
            CheckOrder(violations, "MyriadCreation.Content", "MyriadCreation.Culture");
            CheckSameLayer(violations, "MyriadCreation.Laws", "MyriadCreation.Faith", "MyriadCreation.Culture");
            CheckOrder(violations, "MyriadCreation.Laws", "MyriadCreation.World");
            CheckOrder(violations, "MyriadCreation.Faith", "MyriadCreation.World");
            CheckOrder(violations, "MyriadCreation.Culture", "MyriadCreation.World");
            CheckOrder(violations, "MyriadCreation.World", "MyriadCreation.Simulation");
            CheckOrder(violations, "MyriadCreation.Simulation", "MyriadCreation.Rendering");
            CheckOrder(violations, "MyriadCreation.Simulation", "MyriadCreation.Infrastructure");
            CheckOrder(violations, "MyriadCreation.Rendering", "MyriadCreation.App");
            CheckOrder(violations, "MyriadCreation.Infrastructure", "MyriadCreation.App");
            CheckOrder(violations, "MyriadCreation.App", "MyriadCreation.UI");

            foreach (var source in asmdefs.Values)
            {
                if (IsTestOrEditorAsmdef(source.Name, source.Path)) continue;
                if (!LayerRank.TryGetValue(source.Name, out int sourceRank))
                {
                    // 未列入分层表的新程序集：层序检查跳过并记录（当前所有运行时 asmdef 均已映射）。
                    TestContext.WriteLine("[gate] 未列入分层表，跳过层序检查：" + source.Name + "（" + PathOf(source) + "）");
                    continue;
                }

                foreach (string reference in source.References)
                {
                    if (!LayerRank.TryGetValue(reference, out int targetRank)) continue;

                    string edge = source.Name + "->" + reference;
                    if (targetRank > sourceRank)
                    {
                        violations.Add("向上引用：" + edge + "（" + source.Name + " rank=" + sourceRank +
                            " -> " + reference + " rank=" + targetRank + "；" + PathOf(source) + "）");
                    }
                }
            }
        }

        private static void CheckSameLayer(List<string> violations, params string[] names)
        {
            int? expected = null;
            foreach (string name in names)
            {
                if (!LayerRank.TryGetValue(name, out int rank))
                {
                    violations.Add("分层表缺少条目：" + name);
                    continue;
                }
                if (expected.HasValue && expected.Value != rank)
                {
                    violations.Add("同层断言失败：" + name + "(rank=" + rank + ") 与 " + names[0] +
                        "(rank=" + expected.Value + ") 不在同一层");
                }
                else
                {
                    expected = rank;
                }
            }
        }

        private static void CheckOrder(List<string> violations, string lower, string higher)
        {
            if (!LayerRank.TryGetValue(lower, out int lowerRank) || !LayerRank.TryGetValue(higher, out int higherRank))
            {
                violations.Add("分层表缺少条目：" + lower + " 或 " + higher);
                return;
            }
            if (lowerRank >= higherRank)
            {
                violations.Add("分层序错误：" + lower + "(rank=" + lowerRank + ") 必须低于 " + higher + "(rank=" + higherRank + ")");
            }
        }

        // ---------------- 解析/工具 ----------------

        private static Dictionary<string, AsmdefInfo> LoadAsmdefs()
        {
            var result = new Dictionary<string, AsmdefInfo>(StringComparer.Ordinal);
            string root = Application.dataPath;
            string[] files = Directory.GetFiles(root, "*.asmdef", SearchOption.AllDirectories);
            Assert.GreaterOrEqual(files.Length, 10, "Assets 下未找到足够 asmdef（root=" + root + "）");

            foreach (string path in files)
            {
                string text = File.ReadAllText(path);
                Match nameMatch = NameRegex.Match(text);
                Assert.That(nameMatch.Success, Is.True, "asmdef 缺少 name 字段：" + path);
                string name = nameMatch.Groups[1].Value;

                var references = new List<string>();
                Match refsMatch = ReferencesRegex.Match(text);
                if (refsMatch.Success)
                {
                    foreach (Match match in QuotedRegex.Matches(refsMatch.Groups[1].Value))
                    {
                        string reference = match.Groups[1].Value;
                        // 兼容 GUID 引用（当前仓库用名字引用）；GUID 无法在无 AssetDatabase 情况下解析，跳过并记录。
                        if (reference.StartsWith("GUID:", StringComparison.Ordinal))
                        {
                            TestContext.WriteLine("[gate] 跳过 GUID 引用：" + name + " -> " + reference + "（" + path + "）");
                            continue;
                        }
                        references.Add(reference);
                    }
                }

                Assert.That(result.ContainsKey(name), Is.False, "asmdef name 重复：" + name + "（" + path + "）");
                result[name] = new AsmdefInfo { Name = name, Path = path, References = references };
            }

            return result;
        }

        private static AsmdefInfo Require(Dictionary<string, AsmdefInfo> asmdefs, string name, List<string> violations)
        {
            if (asmdefs.TryGetValue(name, out AsmdefInfo info))
            {
                return info;
            }
            violations.Add("缺少 asmdef：" + name);
            return new AsmdefInfo { Name = name, Path = "(missing)", References = new List<string>() };
        }

        private static string FindCycle(Dictionary<string, AsmdefInfo> asmdefs)
        {
            var state = new Dictionary<string, int>(StringComparer.Ordinal); // 0/不存在=未访问，1=访问中，2=完成
            var stack = new List<string>();
            foreach (string name in asmdefs.Keys)
            {
                string cycle = VisitForCycle(name, asmdefs, state, stack);
                if (cycle != null) return cycle;
            }
            return null;
        }

        private static string VisitForCycle(string name, Dictionary<string, AsmdefInfo> asmdefs,
            Dictionary<string, int> state, List<string> stack)
        {
            state.TryGetValue(name, out int status);
            if (status == 1)
            {
                int start = stack.IndexOf(name);
                return string.Join(" -> ", stack.Skip(start).Concat(new[] { name }));
            }
            if (status == 2) return null;

            state[name] = 1;
            stack.Add(name);
            foreach (string reference in asmdefs[name].References)
            {
                if (!asmdefs.ContainsKey(reference)) continue;
                string cycle = VisitForCycle(reference, asmdefs, state, stack);
                if (cycle != null) return cycle;
            }
            stack.RemoveAt(stack.Count - 1);
            state[name] = 2;
            return null;
        }

        private static string PathOf(AsmdefInfo info)
        {
            return info.Path.Replace('\\', '/');
        }
    }
}
