using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEngine;

namespace MyriadCreation.Tests
{
    /// <summary>
    /// 数据生成物门禁（对应 Myriad Data Forge 的 Adapter Contract v1 / D-T3）：
    /// 1) `_generated/*.json` 必须有 schema_version/generated_at/artifact/requires/source；
    /// 2) requires 只能是 redistributable，且 source 在 datasets.lock.json 中有记录；
    /// 3) 语言谱系 parent 必须可解析且无环；
    /// 4) 署名文件必须保留 Glottolog / CC BY 4.0。
    /// 测试只读，不修改任何内容。
    /// </summary>
    public class DatasetIntegrityTests
    {
        private const string GeneratedFolder = "StreamingAssets/Base/_generated";
        private const string LockFileName = "datasets.lock.json";

        private static string GeneratedDir => Path.Combine(Application.dataPath, GeneratedFolder);
        private static string LockPath => Path.Combine(GeneratedDir, LockFileName);
        private static string LicensePath => Path.Combine(GeneratedDir, "LICENSES.md");

        [Serializable]
        private class SourceInfo
        {
            public string dataset;
            public string version;
            public string license;
            public string url;
            public string retrieved_at;
        }

        [Serializable]
        private class ArtifactHeader
        {
            public int schema_version;
            public string generated_at;
            public string artifact;
            public string requires;
            public SourceInfo source;
        }

        [Serializable]
        private class LockEntry
        {
            public string id;
            public string title;
            public string version;
            public string url;
            public string license;
            public string distribution;
        }

        [Serializable]
        private class LockRoot
        {
            public int schema_version;
            public List<LockEntry> datasets;
        }

        [Serializable]
        private class FamilyNode
        {
            public string id;
            public string parent;
            public string level;
            public string name;
        }

        [Serializable]
        private class FamilyArtifact
        {
            public List<FamilyNode> nodes;
        }

        private static IEnumerable<string> GeneratedArtifactPaths()
        {
            if (!Directory.Exists(GeneratedDir))
            {
                yield break;
            }
            foreach (string path in Directory.GetFiles(GeneratedDir, "*.json"))
            {
                if (string.Equals(Path.GetFileName(path), LockFileName, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }
                yield return path;
            }
        }

        private static LockRoot LoadLock()
        {
            Assert.IsTrue(File.Exists(LockPath), "缺少 datasets.lock.json：" + LockPath);
            LockRoot root = JsonUtility.FromJson<LockRoot>(File.ReadAllText(LockPath));
            Assert.IsNotNull(root, "datasets.lock.json 解析失败");
            Assert.IsNotNull(root.datasets, "datasets.lock.json 缺少 datasets 数组");
            return root;
        }

        [Test]
        public void GeneratedArtifacts_HaveAdapterHeader()
        {
            string[] paths = GeneratedArtifactPaths().ToArray();
            Assert.Greater(paths.Length, 0, "_generated 下应至少有一个生成产物");

            var problems = new List<string>();
            foreach (string path in paths)
            {
                string fileName = Path.GetFileName(path);
                ArtifactHeader header;
                try
                {
                    header = JsonUtility.FromJson<ArtifactHeader>(File.ReadAllText(path));
                }
                catch (Exception exception)
                {
                    problems.Add(fileName + " 解析失败：" + exception.Message);
                    continue;
                }
                if (header == null)
                {
                    problems.Add(fileName + " 解析为空");
                    continue;
                }
                if (header.schema_version < 1) problems.Add(fileName + " schema_version < 1");
                if (string.IsNullOrEmpty(header.generated_at)) problems.Add(fileName + " 缺少 generated_at");
                if (string.IsNullOrEmpty(header.artifact)) problems.Add(fileName + " 缺少 artifact");
                if (header.source == null) problems.Add(fileName + " 缺少 source");
                else
                {
                    if (string.IsNullOrEmpty(header.source.dataset)) problems.Add(fileName + " source.dataset 为空");
                    if (string.IsNullOrEmpty(header.source.version)) problems.Add(fileName + " source.version 为空");
                    if (string.IsNullOrEmpty(header.source.license)) problems.Add(fileName + " source.license 为空");
                }
            }
            Assert.IsEmpty(problems, string.Join("\n", problems));
        }

        [Test]
        public void GeneratedArtifacts_AreRedistributable_AndLocked()
        {
            LockRoot lockRoot = LoadLock();
            var lockById = lockRoot.datasets
                .Where(entry => entry != null && !string.IsNullOrEmpty(entry.id))
                .ToDictionary(entry => entry.id, entry => entry, StringComparer.OrdinalIgnoreCase);

            var problems = new List<string>();
            foreach (string path in GeneratedArtifactPaths())
            {
                string fileName = Path.GetFileName(path);
                ArtifactHeader header = JsonUtility.FromJson<ArtifactHeader>(File.ReadAllText(path));
                if (header == null || header.source == null)
                {
                    problems.Add(fileName + " 无法读取 source");
                    continue;
                }
                if (!string.Equals(header.requires, "redistributable", StringComparison.OrdinalIgnoreCase))
                {
                    problems.Add(fileName + " requires=" + (header.requires ?? "<null>") + "，只允许 redistributable 进入 Base/_generated");
                }
                if (!lockById.TryGetValue(header.source.dataset ?? string.Empty, out LockEntry entry))
                {
                    problems.Add(fileName + " 的数据源 " + header.source.dataset + " 未在 datasets.lock.json 中登记");
                    continue;
                }
                if (!string.Equals(entry.distribution, "redistributable", StringComparison.OrdinalIgnoreCase))
                {
                    problems.Add(fileName + " 数据源 " + entry.id + " 的 distribution=" + entry.distribution + "，不能在游戏包分发");
                }
                if (!string.Equals(entry.license, header.source.license, StringComparison.OrdinalIgnoreCase))
                {
                    problems.Add(fileName + " 的 license=" + header.source.license + " 与 lock=" + entry.license + " 不一致");
                }
            }
            Assert.IsEmpty(problems, string.Join("\n", problems));
        }

        [Test]
        public void LanguageFamilies_ParentsResolve_AndAreAcyclic()
        {
            string path = Path.Combine(GeneratedDir, "language-families.json");
            Assert.IsTrue(File.Exists(path), "缺少 language-families.json");
            FamilyArtifact artifact = JsonUtility.FromJson<FamilyArtifact>(File.ReadAllText(path));
            Assert.IsNotNull(artifact);
            Assert.IsNotNull(artifact.nodes);
            Assert.Greater(artifact.nodes.Count, 0, "语言谱系不应为空");

            var byId = new Dictionary<string, FamilyNode>(StringComparer.Ordinal);
            var problems = new List<string>();
            foreach (FamilyNode node in artifact.nodes)
            {
                if (node == null || string.IsNullOrEmpty(node.id))
                {
                    problems.Add("存在空 id 节点");
                    continue;
                }
                if (byId.ContainsKey(node.id))
                {
                    problems.Add("重复 id：" + node.id);
                    continue;
                }
                byId[node.id] = node;
            }
            foreach (FamilyNode node in byId.Values)
            {
                if (!string.IsNullOrEmpty(node.parent) && !byId.ContainsKey(node.parent))
                {
                    problems.Add(node.id + " 的 parent 不存在：" + node.parent);
                }
                // 环检测：沿 parent 向上，最多走节点数步
                string cursor = node.id;
                for (int step = 0; step <= byId.Count; step++)
                {
                    if (string.IsNullOrEmpty(cursor) || !byId.TryGetValue(cursor, out FamilyNode current)) break;
                    if (step == byId.Count)
                    {
                        problems.Add("parent 链存在环：" + node.id);
                        break;
                    }
                    cursor = current.parent;
                }
            }
            Assert.IsEmpty(problems, string.Join("\n", problems));
        }

        [Test]
        public void GeneratedLicense_KeepsAttribution()
        {
            Assert.IsTrue(File.Exists(LicensePath), "缺少 _generated/LICENSES.md 署名文件");
            string text = File.ReadAllText(LicensePath);
            StringAssert.Contains("Glottolog", text);
            StringAssert.Contains("CC BY 4.0", text);
            StringAssert.Contains("language-families", text);
        }
    }
}
