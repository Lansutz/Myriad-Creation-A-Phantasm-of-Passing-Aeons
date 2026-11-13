using System.Collections.Generic;
using System.IO;
using MyriadCreation.Core;
using NUnit.Framework;
using UnityEngine;
using MyriadCreation.Content.Modding;
using MyriadCreation.Simulation.State;
using MyriadCreation.Simulation.Systems;
using MyriadCreation.Simulation.Culture;
using MyriadCreation.Simulation.CultureHost;

namespace MyriadCreation.Tests.EditMode
{
    /// <summary>Phase 1：Glottolog 谱系内容的加载、校验与语言→文化挂靠。</summary>
    public class LanguageFamilyTests
    {
        private string _tempRoot;

        [TearDown]
        public void TearDown()
        {
            LanguageFamilyCatalog.Reset();
            if (!string.IsNullOrEmpty(_tempRoot) && Directory.Exists(_tempRoot))
                Directory.Delete(_tempRoot, true);
            _tempRoot = null;
        }

        private string WriteArtifact(string nodesJson)
        {
            _tempRoot = Path.Combine(Path.GetTempPath(), "mc-language-families-" + System.Guid.NewGuid().ToString("N"));
            string dir = Path.Combine(_tempRoot, ContentPaths.Base, "_generated");
            Directory.CreateDirectory(dir);
            File.WriteAllText(Path.Combine(dir, "language-families.json"),
                "{\"schema_version\":1,\"generated_at\":\"2026-10-27T00:00:00Z\"," +
                "\"artifact\":\"language-families\",\"requires\":\"redistributable\"," +
                "\"source\":{\"dataset\":\"glottolog\",\"version\":\"5.3\",\"license\":\"CC-BY-4.0\"}," +
                "\"nodes\":[" + nodesJson + "]}");
            return _tempRoot;
        }

        [Test]
        public void ShippedArtifact_LoadsAndResolvesChain()
        {
            LanguageFamilyCatalog.Initialize(Application.streamingAssetsPath);
            Assert.GreaterOrEqual(LanguageFamilyCatalog.Count, 50, "应加载 Glottolog 裁剪谱系");
            Assert.IsTrue(LanguageFamilyCatalog.TryGet("stan1293", out var node));
            Assert.AreEqual("language", node.level);
            var ancestors = LanguageFamilyCatalog.GetAncestors("stan1293");
            CollectionAssert.Contains(ancestors, "west2793");
            CollectionAssert.Contains(ancestors, "indo1319");
            Assert.AreEqual("indo1319", LanguageFamilyCatalog.GetRootFamilyId("stan1293"));
            Assert.AreEqual("glottolog", LanguageFamilyCatalog.Source.dataset);
            Assert.AreEqual("CC-BY-4.0", LanguageFamilyCatalog.Source.license);
        }

        [Test]
        public void MissingFile_IsSafe()
        {
            _tempRoot = Path.Combine(Path.GetTempPath(), "mc-lf-missing-" + System.Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_tempRoot);
            Assert.DoesNotThrow(() => LanguageFamilyCatalog.Initialize(_tempRoot));
            Assert.AreEqual(0, LanguageFamilyCatalog.Count);
            Assert.IsFalse(LanguageFamilyCatalog.TryGet("x", out _));
        }

        [Test]
        public void InvalidParentAndCycle_AreDropped()
        {
            string root = WriteArtifact(
                "{\"id\":\"a\",\"parent\":null,\"name\":\"A\",\"level\":\"family\"}," +
                "{\"id\":\"orphan\",\"parent\":\"missing\",\"name\":\"Orphan\",\"level\":\"language\"}," +
                "{\"id\":\"c1\",\"parent\":\"c2\",\"name\":\"C1\",\"level\":\"language\"}," +
                "{\"id\":\"c2\",\"parent\":\"c1\",\"name\":\"C2\",\"level\":\"language\"}");
            LanguageFamilyCatalog.Initialize(root);
            Assert.AreEqual(1, LanguageFamilyCatalog.Count);
            Assert.IsTrue(LanguageFamilyCatalog.TryGet("a", out _));
            Assert.IsFalse(LanguageFamilyCatalog.TryGet("orphan", out _));
            Assert.IsFalse(LanguageFamilyCatalog.TryGet("c1", out _));
        }

        [Test]
        public void ModsOverlay_OverridesBaseNode()
        {
            string root = WriteArtifact("{\"id\":\"a\",\"parent\":null,\"name\":\"BaseA\",\"level\":\"family\"}");
            string mods = Path.Combine(root, ContentPaths.Mods, "_generated");
            Directory.CreateDirectory(mods);
            File.WriteAllText(Path.Combine(mods, "language-families.json"),
                "{\"schema_version\":1,\"nodes\":[{\"id\":\"a\",\"parent\":null,\"name\":\"ModA\",\"level\":\"family\"}]}");
            LanguageFamilyCatalog.Initialize(root);
            Assert.IsTrue(LanguageFamilyCatalog.TryGet("a", out var node));
            Assert.AreEqual("ModA", node.name);
        }

        [Test]
        public void CultureLineage_SetsFamilyAndParent()
        {
            var cultures = new Dictionary<int, CultureData>
            {
                [0] = new CultureData { cultureId = 0, languageId = "lang_root" },
                [1] = new CultureData { cultureId = 1, languageId = "lang_child", parentCultureId = -1 },
            };
            var languages = new Dictionary<string, LanguageDef>
            {
                ["lang_root"] = new LanguageDef { languageId = "lang_root", familyId = "fam1" },
                ["lang_child"] = new LanguageDef
                {
                    languageId = "lang_child", familyId = "fam1", parentLanguageId = "lang_root"
                },
            };
            int changed = CultureLineage.Apply(cultures, id => languages.TryGetValue(id, out var def) ? def : null);
            Assert.AreEqual(3, changed);
            Assert.AreEqual("fam1", cultures[0].languageFamilyId);
            Assert.AreEqual("fam1", cultures[1].languageFamilyId);
            Assert.AreEqual(0, cultures[1].parentCultureId);
        }

        [Test]
        public void CultureLineage_DoesNotOverwriteAuthoredValues()
        {
            var cultures = new Dictionary<int, CultureData>
            {
                [0] = new CultureData { cultureId = 0, languageId = "lang_root" },
                [1] = new CultureData
                {
                    cultureId = 1, languageId = "lang_child",
                    parentCultureId = 0, languageFamilyId = "authored_family"
                },
            };
            var languages = new Dictionary<string, LanguageDef>
            {
                ["lang_root"] = new LanguageDef { languageId = "lang_root", familyId = "fam1" },
                ["lang_child"] = new LanguageDef
                {
                    languageId = "lang_child", familyId = "fam1", parentLanguageId = "lang_root"
                },
            };
            CultureLineage.Apply(cultures, id => languages.TryGetValue(id, out var def) ? def : null);
            Assert.AreEqual("authored_family", cultures[1].languageFamilyId);
            Assert.AreEqual(0, cultures[1].parentCultureId);
        }

        [Test]
        public void LanguageAndCultureFields_DefaultEmpty()
        {
            Assert.AreEqual(string.Empty, new LanguageDef().familyId);
            Assert.AreEqual(string.Empty, new LanguageDef().glottocode);
            Assert.AreEqual(string.Empty, new CultureData().languageFamilyId);
        }
    }
}
