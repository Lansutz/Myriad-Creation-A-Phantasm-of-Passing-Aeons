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
    /// TMP 资产完整性门禁（只读）：
    /// 1) 扫描 Assets/**/*.meta 与 Library/PackageCache/**/*.meta 的 GUID 集合；
    /// 2) TMP 目录 *.asset/*.mat 与 Main.unity 中所有 guid: 引用必须可解析（或特殊零 GUID 白名单）；
    /// 3) TMP Settings 默认字体/样式表/Sprite 与 LiberationSans 四个资产的 shader GUID 定向校验。
    /// 失败信息输出 文件:行 guid 清单；不修改任何资产。
    /// </summary>
    public class TmpAssetIntegrityTests
    {
        private static readonly Regex GuidLineRegex = new Regex(
            @"^\s*guid:\s*([0-9a-fA-F]{32})", RegexOptions.Multiline | RegexOptions.Compiled);

        private static readonly Regex GuidReferenceRegex = new Regex(
            @"guid:\s*([0-9a-fA-F]{32})", RegexOptions.Compiled);

        // Unity 内置/特殊对象 GUID：全 0、内置资源 e000、内置材质/天空盒 f000 等。
        private static readonly HashSet<string> SpecialGuidWhitelist = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "00000000000000000000000000000000",
            "0000000000000000e000000000000000",
            "0000000000000000f000000000000000",
        };

        private sealed class GuidSources
        {
            public HashSet<string> Assets;
            public HashSet<string> PackageCache;
            public bool PackageCacheExists;

            public bool Contains(string guid)
            {
                return Assets.Contains(guid) || PackageCache.Contains(guid);
            }

            public IEnumerable<string> All
            {
                get { return Assets.Concat(PackageCache); }
            }
        }

        [Test]
        public void GuidReferences_ResolveAgainstAssetsAndPackageCache()
        {
            GuidSources sources = CollectGuidSources();
            Assert.That(sources.Assets.Count, Is.GreaterThan(0), "Assets/**/*.meta 应至少存在一个有效 guid");
            if (sources.PackageCacheExists)
            {
                Assert.That(sources.PackageCache.Count, Is.GreaterThan(0),
                    "Library/PackageCache 存在但未解析到 guid；PackageCache 完整性检查不可信");
            }

            var targets = new List<string>();
            string tmpRoot = Path.Combine(Application.dataPath, "Art", "TextMesh Pro");
            if (Directory.Exists(tmpRoot))
            {
                targets.AddRange(Directory.GetFiles(tmpRoot, "*.asset", SearchOption.AllDirectories));
                targets.AddRange(Directory.GetFiles(tmpRoot, "*.mat", SearchOption.AllDirectories));
            }
            string mainScene = Path.Combine(Application.dataPath, "Scenes", "Main.unity");
            if (File.Exists(mainScene)) targets.Add(mainScene);

            Assert.That(targets, Is.Not.Empty, "应扫描到 TMP 资产/Main.unity");

            var offenders = new List<string>();
            foreach (string path in targets)
            {
                string[] lines = File.ReadAllLines(path);
                for (int i = 0; i < lines.Length; i++)
                {
                    foreach (Match match in GuidReferenceRegex.Matches(lines[i]))
                    {
                        string guid = match.Groups[1].Value;
                        if (SpecialGuidWhitelist.Contains(guid)) continue;
                        if (sources.Contains(guid)) continue;
                        offenders.Add($"{ToAssetRelative(path)}:{i + 1} guid:{guid}");
                    }
                }
            }

            Assert.That(offenders, Is.Empty,
                "TMP 资产/Main.unity 存在未解析 GUID（检查 .meta 是否断链/资产是否被移走）：\n" +
                string.Join("\n", offenders));
        }

        [Test]
        public void TmpSettings_DefaultAssetsAndShaders_AreValid()
        {
            var sources = CollectGuidSources();
            string tmpRoot = Path.Combine(Application.dataPath, "Art", "TextMesh Pro");
            string settingsPath = Path.Combine(tmpRoot, "Resources", "TMP Settings.asset");
            string fontAssetPath = Path.Combine(tmpRoot, "Resources", "Fonts & Materials", "LiberationSans SDF.asset");
            string styleSheetPath = Path.Combine(tmpRoot, "Resources", "Style Sheets", "Default Style Sheet.asset");
            string sdfPath = fontAssetPath;
            string fallbackPath = Path.Combine(tmpRoot, "Resources", "Fonts & Materials", "LiberationSans SDF - Fallback.asset");
            string dropShadowPath = Path.Combine(tmpRoot, "Resources", "Fonts & Materials", "LiberationSans SDF - Drop Shadow.mat");
            string outlinePath = Path.Combine(tmpRoot, "Resources", "Fonts & Materials", "LiberationSans SDF - Outline.mat");
            string shaderMetaPath = Path.Combine(tmpRoot, "Shaders", "TMP_SDF.shader.meta");

            Assert.IsTrue(File.Exists(settingsPath), "缺少 TMP Settings：" + ToAssetRelative(settingsPath));
            Assert.IsTrue(File.Exists(fontAssetPath), "缺少 LiberationSans SDF.asset");
            Assert.IsTrue(File.Exists(styleSheetPath), "缺少 Default Style Sheet.asset");
            Assert.IsTrue(File.Exists(shaderMetaPath), "缺少 TMP_SDF.shader.meta");

            string settings = File.ReadAllText(settingsPath);
            string fontGuid = ReadMetaGuid(fontAssetPath + ".meta");
            string styleGuid = ReadMetaGuid(styleSheetPath + ".meta");

            Assert.AreEqual(fontGuid, ReadGuidField(settings, "m_defaultFontAsset"),
                "TMP Settings.m_defaultFontAsset 必须指向 LiberationSans SDF.asset");
            Assert.AreEqual(styleGuid, ReadGuidField(settings, "m_defaultStyleSheet"),
                "TMP Settings.m_defaultStyleSheet 必须指向 Default Style Sheet.asset");

            // m_defaultSpriteAsset: {fileID: 0} 表示无 Sprite Asset；非 0 时 guid 必须有效。
            Match spriteMatch = Regex.Match(settings,
                @"m_defaultSpriteAsset:\s*\{fileID:\s*(\d+)(?:,\s*guid:\s*([0-9a-fA-F]{32}))?",
                RegexOptions.Singleline);
            Assert.IsTrue(spriteMatch.Success, "无法解析 TMP Settings.m_defaultSpriteAsset");
            long spriteFileId = long.Parse(spriteMatch.Groups[1].Value);
            string spriteGuid = spriteMatch.Groups[2].Success ? spriteMatch.Groups[2].Value : null;
            if (spriteFileId != 0)
            {
                Assert.IsNotNull(spriteGuid, "m_defaultSpriteAsset 非 0 fileID 时必须带 guid");
                Assert.IsTrue(sources.Contains(spriteGuid), "m_defaultSpriteAsset guid 未解析：" + spriteGuid);
            }

            string expectedShaderGuid = ReadMetaGuid(shaderMetaPath);
            Assert.IsTrue(sources.Contains(expectedShaderGuid),
                "TMP_SDF shader guid 未解析：" + expectedShaderGuid);

            foreach (string path in new[] { sdfPath, fallbackPath, dropShadowPath, outlinePath })
            {
                Assert.IsTrue(File.Exists(path), "缺少 TMP 资产：" + ToAssetRelative(path));
                string text = File.ReadAllText(path);
                Match match = Regex.Match(text,
                    @"m_Shader:\s*\{fileID:\s*\d+,\s*guid:\s*([0-9a-fA-F]{32})",
                    RegexOptions.Singleline);
                Assert.IsTrue(match.Success, "缺少 m_Shader guid：" + ToAssetRelative(path));
                string shaderGuid = match.Groups[1].Value;
                Assert.AreEqual(expectedShaderGuid, shaderGuid,
                    "TMP 资产 shader guid 与 TMP_SDF.shader 不一致：" + ToAssetRelative(path));
                Assert.IsTrue(sources.Contains(shaderGuid),
                    "TMP 资产 shader guid 未解析：" + shaderGuid + " @ " + ToAssetRelative(path));
            }
        }

        private static GuidSources CollectGuidSources()
        {
            var assets = LoadMetaGuids(Application.dataPath);
            string projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            string packageCache = Path.Combine(projectRoot, "Library", "PackageCache");
            var package = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            bool packageExists = Directory.Exists(packageCache);
            if (packageExists)
            {
                package = LoadMetaGuids(packageCache);
            }
            else
            {
                Debug.LogWarning("[TmpAssetIntegrityTests] Library/PackageCache 不存在，跳过 PackageCache GUID 检查：" + packageCache);
            }

            return new GuidSources
            {
                Assets = assets,
                PackageCache = package,
                PackageCacheExists = packageExists,
            };
        }

        private static HashSet<string> LoadMetaGuids(string root)
        {
            var result = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (!Directory.Exists(root)) return result;

            foreach (string meta in Directory.EnumerateFiles(root, "*.meta", SearchOption.AllDirectories))
            {
                Match match = GuidLineRegex.Match(File.ReadAllText(meta));
                if (match.Success) result.Add(match.Groups[1].Value);
            }
            return result;
        }

        private static string ReadMetaGuid(string metaPath)
        {
            Assert.IsTrue(File.Exists(metaPath), "缺少 meta：" + metaPath);
            Match match = GuidLineRegex.Match(File.ReadAllText(metaPath));
            Assert.IsTrue(match.Success, "meta 缺少 guid：" + metaPath);
            return match.Groups[1].Value;
        }

        private static string ReadGuidField(string assetText, string field)
        {
            Match match = Regex.Match(assetText,
                Regex.Escape(field) + @":\s*\{[^}]*?guid:\s*([0-9a-fA-F]{32})",
                RegexOptions.Singleline);
            Assert.IsTrue(match.Success, "无法解析字段 guid：" + field);
            return match.Groups[1].Value;
        }

        private static string ToAssetRelative(string absolutePath)
        {
            return "Assets/" + Path.GetRelativePath(Application.dataPath, absolutePath).Replace('\\', '/');
        }
    }
}
