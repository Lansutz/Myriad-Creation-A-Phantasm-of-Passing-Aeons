using System;
using System.IO;
using NUnit.Framework;
using MyriadCreation.Content.Modding;
using UnityEngine;

namespace MyriadCreation.Tests.EditMode
{
    /// <summary>
    /// P2：ContentDefinitionIndex（纯内容定义索引）测试。
    /// 验证真实 StreamingAssets 定义加载、Innovation 1/1025 兜底与 fallback 注入、
    /// 空目录/坏 JSON/Reset 安全性；不依赖 ContentRegistry/Simulation。
    /// </summary>
    public class ContentDefinitionIndexTests
    {
        private static string StreamingRoot => Application.streamingAssetsPath;

        private static string CreateTempRoot()
        {
            string root = Path.Combine(Path.GetTempPath(), "MyriadContentIndexTests_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(root);
            return root;
        }

        private static void DeleteTempRoot(string root)
        {
            if (!string.IsNullOrEmpty(root) && Directory.Exists(root))
            {
                Directory.Delete(root, true);
            }
        }

        [Test]
        public void Initialize_LoadsDefinitionsAndInnovationFallback()
        {
            var index = new ContentDefinitionIndex();
            index.Initialize(StreamingRoot, ContentDefinitionIndex.InnovationFallbackGroups.Default);

            Assert.IsTrue(index.IsInitialized);
            Assert.Greater(index.Ethos.Count, 0, "Ethos.json 应加载出定义");
            Assert.Greater(index.Languages.Count, 0, "Language 目录应加载出语言");
            Assert.Greater(index.Religions.Count, 0, "Religions.json 应加载出宗教");
            Assert.Greater(index.Titles.Count, 0, "Titles.json 应加载出头衔");

            Assert.IsTrue(index.Innovations.ContainsKey(1), "基础革新 id=1 兜底应存在");
            Assert.IsTrue(index.Innovations.ContainsKey(1025), "领土王国 id=1025 兜底应存在");
            Assert.AreEqual(1, index.Innovations[1].innovationId);
            Assert.AreEqual(1025, index.Innovations[1025].innovationId);
            Assert.IsNotNull(index.Innovations[1].discoveryGroups);
            Assert.IsNotNull(index.Innovations[1025].discoveryGroups);
            Assert.IsNotEmpty(index.Innovations[1].discoveryGroups);
            Assert.IsNotEmpty(index.Innovations[1025].discoveryGroups);
        }

        [Test]
        public void InnovationFallbackGroups_AreInjectedByHostStrings()
        {
            var index = new ContentDefinitionIndex();
            index.Initialize(StreamingRoot, new ContentDefinitionIndex.InnovationFallbackGroups(
                "host-merchant", "host-scholar", "host-artisan"));

            Assert.AreEqual("host-scholar", index.Innovations[1025].discoveryGroups[0]);
            Assert.AreEqual("host-artisan", index.Innovations[1].discoveryGroups[0]);
        }

        [Test]
        public void EmptyRoot_IsSafe()
        {
            string root = CreateTempRoot();
            try
            {
                var index = new ContentDefinitionIndex();
                Assert.DoesNotThrow(() => index.Initialize(root, ContentDefinitionIndex.InnovationFallbackGroups.Default));
                Assert.IsTrue(index.IsInitialized);
                Assert.AreEqual(0, index.Ethos.Count);
                Assert.AreEqual(0, index.Innovations.Count);
            }
            finally
            {
                DeleteTempRoot(root);
            }
        }

        [Test]
        public void InvalidJson_IsSafe()
        {
            string root = CreateTempRoot();
            string ethosDir = Path.Combine(root, "Base", "Ethos");
            Directory.CreateDirectory(ethosDir);
            File.WriteAllText(Path.Combine(ethosDir, "Ethos.json"), "{ invalid json");

            try
            {
                var index = new ContentDefinitionIndex();
                Assert.DoesNotThrow(() => index.Initialize(root, ContentDefinitionIndex.InnovationFallbackGroups.Default));
                Assert.IsTrue(index.IsInitialized);
                Assert.AreEqual(0, index.Ethos.Count);
            }
            finally
            {
                DeleteTempRoot(root);
            }
        }

        [Test]
        public void Reset_ClearsAndCanReinitialize()
        {
            var index = new ContentDefinitionIndex();
            index.Initialize(StreamingRoot, ContentDefinitionIndex.InnovationFallbackGroups.Default);
            Assert.Greater(index.Ethos.Count, 0);

            index.Reset();
            Assert.IsFalse(index.IsInitialized);
            Assert.AreEqual(0, index.Ethos.Count);
            Assert.AreEqual(0, index.Innovations.Count);

            index.Initialize(StreamingRoot, ContentDefinitionIndex.InnovationFallbackGroups.Default);
            Assert.IsTrue(index.IsInitialized);
            Assert.Greater(index.Ethos.Count, 0);
            Assert.IsTrue(index.Innovations.ContainsKey(1025));
        }
    }
}
