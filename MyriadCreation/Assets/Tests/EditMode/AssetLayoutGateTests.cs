using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEngine;

namespace MyriadCreation.Tests.EditMode
{
    /// <summary>
    /// Round 40 资产目录门禁（只读扫描 Assets 布局，不改资产）：
    /// 1) 禁止旧顶层目录回流（Fonts/TextMesh Pro/Resources/Prefabs/Settings/ScriptableObjects）；
    /// 2) 顶层目录 allowlist + 根文件 allowlist（Unity .meta 自动允许）；
    /// 3) simhei 全 Assets 唯一，且固定为 Art/Fonts/Resources/simhei.ttf；
    /// 4) TMP 视觉资源位于 Art/TextMesh Pro/Resources；
    /// 5) UI 音频 5 wav 位于 Audio/Resources/UI；
    /// 6) 顶层目录不得为空；
    /// 失败信息统一列出违规路径清单，便于修复。
    /// </summary>
    public class AssetLayoutGateTests
    {
        private static readonly string[] AllowedTopLevelDirectories =
        {
            "Art", "Audio", "Data", "Editor", "Scenes", "Scripts", "StreamingAssets", "Tests", "UI"
        };

        private static readonly string[] AllowedTopLevelFiles =
        {
            "csc.rsp", "ASSET_README.md"
        };

        private static readonly string[] ForbiddenTopLevelDirectories =
        {
            "Fonts",
            "TextMesh Pro",
            "Resources",
            "Prefabs",
            "Settings",
            "ScriptableObjects"
        };

        private static readonly string[] ExpectedAudioClips =
        {
            "click", "close", "error", "hover", "open"
        };

        private static string AssetsRoot => Application.dataPath;
        private static string Relative(string path) => path.Replace('\\', '/');

        [Test]
        public void ForbiddenTopLevelDirectories_DoNotExist()
        {
            var offenders = ForbiddenTopLevelDirectories
                .Where(name => Directory.Exists(Path.Combine(AssetsRoot, name)))
                .Select(name => "Assets/" + name)
                .ToList();

            Assert.That(offenders, Is.Empty,
                "禁止的旧顶层目录回流，请迁移到新所有权目录：\n" + FormatList(offenders));
        }

        [Test]
        public void TopLevelDirectories_AreAllowlisted()
        {
            var offenders = Directory.GetDirectories(AssetsRoot, "*", SearchOption.TopDirectoryOnly)
                .Select(path => Path.GetFileName(path))
                .Where(name => Array.IndexOf(AllowedTopLevelDirectories, name) < 0)
                .OrderBy(name => name, StringComparer.Ordinal)
                .ToList();

            Assert.That(offenders, Is.Empty,
                "发现未登记的 Assets 顶层目录（新增目录需同步更新 allowlist）：\n" + FormatList(offenders));
        }

        [Test]
        public void TopLevelFiles_AreAllowlisted()
        {
            var offenders = Directory.GetFiles(AssetsRoot, "*", SearchOption.TopDirectoryOnly)
                .Where(path => !path.EndsWith(".meta", StringComparison.OrdinalIgnoreCase))
                .Select(path => Path.GetFileName(path))
                .Where(name => Array.IndexOf(AllowedTopLevelFiles, name) < 0)
                .OrderBy(name => name, StringComparer.Ordinal)
                .ToList();

            Assert.That(offenders, Is.Empty,
                "Assets 根文件只允许 " + string.Join(", ", AllowedTopLevelFiles) + "（及 Unity .meta）：\n" +
                FormatList(offenders));
        }

        [Test]
        public void TopLevelDirectories_AreNotEmpty()
        {
            var empty = AllowedTopLevelDirectories
                .Select(name => Path.Combine(AssetsRoot, name))
                .Where(Directory.Exists)
                .Where(path => !Directory.EnumerateFileSystemEntries(path).Any())
                .Select(path => "Assets/" + Path.GetFileName(path))
                .OrderBy(name => name, StringComparer.Ordinal)
                .ToList();

            Assert.That(empty, Is.Empty,
                "Assets 顶层目录不得为空（空目录请删除或补 README/.gitkeep）：\n" + FormatList(empty));
        }

        [Test]
        public void SimheiFont_IsUniqueAndInArt()
        {
            string expected = Relative(Path.Combine("Art", "Fonts", "Resources", "simhei.ttf"));
            var matches = Directory.GetFiles(AssetsRoot, "*", SearchOption.AllDirectories)
                .Where(path => path.EndsWith(".ttf", StringComparison.OrdinalIgnoreCase)
                    || path.EndsWith(".otf", StringComparison.OrdinalIgnoreCase))
                .Where(path => Path.GetFileName(path).IndexOf("simhei", StringComparison.OrdinalIgnoreCase) >= 0)
                .Select(path => Relative(Path.GetRelativePath(AssetsRoot, path)))
                .OrderBy(path => path, StringComparer.Ordinal)
                .ToList();

            Assert.That(matches.Count, Is.EqualTo(1),
                "simhei 字体必须全 Assets 唯一，实际 = \n" + FormatList(matches));
            Assert.That(matches[0], Is.EqualTo(expected),
                "simhei 字体位置必须为 " + expected + "，实际 = " + matches[0]);
        }

        [Test]
        public void VisualResources_LiveInArt()
        {
            Assert.IsFalse(Directory.Exists(Path.Combine(AssetsRoot, "TextMesh Pro")),
                "禁止 Assets/TextMesh Pro 根目录回流；TMP 资源应位于 Art/TextMesh Pro");

            string tmpSettings = Relative(Path.Combine("Art", "TextMesh Pro", "Resources", "TMP Settings.asset"));
            Assert.IsTrue(File.Exists(Path.Combine(AssetsRoot, tmpSettings)),
                "缺少 TMP Settings 资产：" + tmpSettings);
        }

        [Test]
        public void AudioClips_ExistInAudioResourcesUi()
        {
            var offenders = ExpectedAudioClips
                .Select(name => Relative(Path.Combine("Audio", "Resources", "UI", name + ".wav")))
                .Where(path => !File.Exists(Path.Combine(AssetsRoot, path)))
                .ToList();

            Assert.That(offenders, Is.Empty,
                "缺少 UI 音频资源（Audio/Resources/UI）：\n" + FormatList(offenders));
            Assert.IsFalse(Directory.Exists(Path.Combine(AssetsRoot, "Resources")),
                "禁止 Assets/Resources 根目录回流；音频应位于 Audio/Resources");
        }

        private static string FormatList(IEnumerable<string> items)
        {
            return string.Join("\n", items.Select(item => " - " + item));
        }
    }
}
