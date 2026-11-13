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
    /// 总完成门禁（只读）：把 W2/W4/W5/W6 的边界做成可执行断言。
    /// 失败输出命中文件:行清单；本测试不修改任何资产/代码。
    /// </summary>
    public class CompletionGateTests
    {
        private static string AssetsRoot => Application.dataPath;

        private static readonly Regex UiForbiddenUsing = new Regex(
            @"^\s*using\s+MyriadCreation\.(Simulation|Laws|Faith|Culture)(\.|;)",
            RegexOptions.Multiline | RegexOptions.Compiled);

        private static readonly Regex ContentForbiddenUsing = new Regex(
            @"^\s*using\s+MyriadCreation\.(Simulation|World)(\.|;)",
            RegexOptions.Multiline | RegexOptions.Compiled);

        private static readonly Regex LegacyModdingNamespace = new Regex(
            @"^\s*namespace\s+MyriadCreation\.Simulation\.Modding\b",
            RegexOptions.Multiline | RegexOptions.Compiled);

        private static readonly string[] KeyAssets =
        {
            "Art/Fonts/Resources/simhei.ttf",
            "Art/TextMesh Pro/Resources/TMP Settings.asset",
            "Audio/Resources/UI/click.wav",
            "Data/WorldConfig/DefaultWorldConfig.asset",
        };

        [Test]
        public void Ui_HasNoSimulationFamilyUsing()
        {
            var offenders = ScanCSharpLines("UI", new[] { UiForbiddenUsing });
            Assert.That(offenders, Is.Empty,
                "Assets/UI 不得引用 Simulation/Laws/Faith/Culture：\n" + FormatList(offenders));
        }

        [Test]
        public void Content_HasNoSimulationOrWorldUsing()
        {
            var offenders = ScanCSharpLines("Scripts/Content", new[] { ContentForbiddenUsing });
            Assert.That(offenders, Is.Empty,
                "Assets/Scripts/Content 不得引用 Simulation/World：\n" + FormatList(offenders));
        }

        [Test]
        public void Repo_HasNoLegacySimulationModdingNamespace()
        {
            var offenders = ScanCSharpLines("Scripts", new[] { LegacyModdingNamespace });
            offenders.AddRange(ScanCSharpLines("UI", new[] { LegacyModdingNamespace }));
            offenders.AddRange(ScanCSharpLines("Editor", new[] { LegacyModdingNamespace }));
            offenders = offenders.Distinct().OrderBy(x => x, StringComparer.Ordinal).ToList();

            Assert.That(offenders, Is.Empty,
                "全仓不得再有 旧 Modding namespace 声明：\n" + FormatList(offenders));
        }

        [Test]
        public void Ui_HasNoContractPlaceholderText()
        {
            var offenders = new List<string>();
            foreach (string file in EnumerateCSharp("UI"))
            {
                string[] lines = File.ReadAllLines(file);
                for (int i = 0; i < lines.Length; i++)
                {
                    if (lines[i].Contains("契约未提供"))
                    {
                        offenders.Add(ToAssetRelative(file) + ":" + (i + 1));
                    }
                }
            }

            Assert.That(offenders, Is.Empty,
                "Assets/UI 不得保留\"契约未提供\"占位（W5 要求面板 100% 契约化）：\n" + FormatList(offenders));
        }

        [Test]
        public void KeyAssets_Exist()
        {
            var missing = KeyAssets
                .Where(relative => !File.Exists(Path.Combine(AssetsRoot, relative)))
                .Select(relative => "Assets/" + relative)
                .ToList();

            Assert.That(missing, Is.Empty,
                "关键资产缺失：\n" + FormatList(missing));
        }

        private static List<string> ScanCSharpLines(string relativeRoot, IEnumerable<Regex> patterns)
        {
            var offenders = new List<string>();
            foreach (string file in EnumerateCSharp(relativeRoot))
            {
                string[] lines = File.ReadAllLines(file);
                for (int i = 0; i < lines.Length; i++)
                {
                    string line = lines[i];
                    if (patterns.Any(pattern => pattern.IsMatch(line)))
                    {
                        offenders.Add(ToAssetRelative(file) + ":" + (i + 1));
                    }
                }
            }
            return offenders;
        }

        private static IEnumerable<string> EnumerateCSharp(string relativeRoot)
        {
            string root = Path.Combine(AssetsRoot, relativeRoot);
            if (!Directory.Exists(root)) yield break;
            foreach (string file in Directory.EnumerateFiles(root, "*.cs", SearchOption.AllDirectories))
            {
                yield return file;
            }
        }

        private static string ToAssetRelative(string absolutePath)
        {
            return "Assets/" + Path.GetRelativePath(AssetsRoot, absolutePath).Replace('\\', '/');
        }

        private static string FormatList(IEnumerable<string> items)
        {
            return string.Join("\n", items.Select(item => " - " + item));
        }
    }
}
