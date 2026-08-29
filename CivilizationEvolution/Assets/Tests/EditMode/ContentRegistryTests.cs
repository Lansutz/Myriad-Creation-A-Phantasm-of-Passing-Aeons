using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using CivilizationEvolution.Core;
using CivilizationEvolution.Culture;

namespace CivilizationEvolution.Tests
{
    /// <summary>
    /// 模组化接口层 EditMode 测试（企划书 1.2 模组扩展规范）
    /// 真实加载 Assets/StreamingAssets（EditMode 下 streamingAssetsPath 指向项目目录）
    /// </summary>
    public class ContentRegistryTests
    {
        [SetUp]
        public void Setup()
        {
            ContentRegistry.Reset();
            ContentRegistry.Initialize();
        }

        // ===== 六类注册表加载 =====

        [Test]
        public void ContentRegistry_LoadsAllSixTypes()
        {
            Assert.That(ContentRegistry.Cultures.Count, Is.GreaterThan(0), "文化包应加载");
            Assert.That(ContentRegistry.Races.Count, Is.GreaterThan(0), "种族定义应加载");
            Assert.That(ContentRegistry.Ethos.Count, Is.GreaterThan(0), "族群精神定义表应加载");
            Assert.That(ContentRegistry.Traditions.Count, Is.GreaterThan(0), "文化传统定义表应加载");
            Assert.That(ContentRegistry.Languages.Count, Is.GreaterThan(0), "语言定义应加载");
            Assert.That(ContentRegistry.EthnicGroups.Count, Is.GreaterThan(0), "族群实体应加载");
        }

        // ===== 族群实体引用解析（Ethnos 支柱：精神/语言/传统） =====

        [Test]
        public void EthnicGroup_Laethis_AllPillarsResolve()
        {
            Assert.IsTrue(ContentRegistry.TryGetEthnicGroup("ethnos_laethis", out var group), "应存在莱希斯族群");
            Assert.AreEqual("莱希斯族群", group.name);

            // 挂靠文化
            Assert.IsTrue(ContentRegistry.TryGetCulture(group.cultureId, out var culture), "族群应挂靠文化");
            Assert.AreEqual("Laethis", culture.data.cultureName);

            // 族群精神
            Assert.IsTrue(ContentRegistry.TryGetEthos(group.ethosId, out var ethos), "族群精神应可解析");
            Assert.IsFalse(string.IsNullOrEmpty(ethos.description), "族群精神应有写实描述");

            // 语言
            Assert.IsTrue(ContentRegistry.TryGetLanguage(group.languageId, out var language), "语言应可解析");
            Assert.IsFalse(string.IsNullOrEmpty(language.scriptType), "语言应有书写系统");

            // 文化传统（全部可解析）
            Assert.That(group.traditionIds.Count, Is.GreaterThan(0), "族群应承载文化传统");
            foreach (var tid in group.traditionIds)
                Assert.IsTrue(ContentRegistry.TryGetTradition(tid, out _), $"传统 {tid} 应可解析");
        }

        [Test]
        public void Culture_Laethis_SevenPillarsExtended()
        {
            Assert.IsTrue(ContentRegistry.TryGetCulture(1, out var pack));
            var c = pack.data;
            Assert.AreEqual(7, c.worshipVector.Length, "崇拜权重向量应为 7 维（生殖/自然/祖先/死亡/图腾/形象/巫术）");
            Assert.That(c.burialTypes.Count, Is.GreaterThan(0), "葬俗应多选");
            Assert.That(c.symbolicFoci.Count, Is.GreaterThan(0), "象征焦点应主次双焦点");
            Assert.That(c.environmentAdapts.Count, Is.GreaterThan(0), "环境适应应多选");
            Assert.AreEqual("laethis_lang", c.languageId, "文化应引用默认语言");
        }

        // ===== 传统互斥（CK3 traditions 类比） =====

        [Test]
        public void Tradition_AncestorCult_IncompatibleWithIconoclast()
        {
            Assert.IsTrue(ContentRegistry.TryGetTradition("trad_ancestor_cult", out var ancestor));
            Assert.IsTrue(ContentRegistry.TryGetTradition("trad_iconoclast", out var iconoclast));
            Assert.IsTrue(ancestor.incompatibleWith.Contains("trad_iconoclast"), "祖先祭祀应互斥破坏圣像");
            Assert.IsTrue(iconoclast.incompatibleWith.Contains("trad_ancestor_cult"), "互斥应双向声明");
        }

        // ===== 文化相似度集合版（企划书 7.4.6 Jaccard 重合度） =====

        [Test]
        public void CultureSimilarity_SetJaccard_BurialOverlap()
        {
            // 对照设计：两组文化其余板块全相同（默认 0），仅葬俗不同
            // a{1,3} vs b{1} → Jaccard 0.5；对照 c/d 空集回退单值 0=0 → 1
            var a = new CultureData { burialTypes = new List<int> { 1, 3 } };
            var b = new CultureData { burialTypes = new List<int> { 1 } };
            var c = new CultureData();
            var d = new CultureData();

            float diff = CultureSimilarity.CalculateSim(a, b) - CultureSimilarity.CalculateSim(c, d);
            Assert.That(diff, Is.EqualTo(0.15f * (0.5f - 1f)).Within(0.001f),
                $"Jaccard 贡献应使葬俗项从 1 降到 0.5（权重 0.15）");
        }

        [Test]
        public void CultureSimilarity_EmptySets_FallbackToSingle()
        {
            // 双方集合空：回退单值比较；worshipVector 相同则全板块一致 → 相似度 1
            var a = new CultureData
            {
                burialType = 2,
                worshipVector = new float[] { 1, 0, 0, 0, 0, 0, 0 }
            };
            var b = new CultureData
            {
                burialType = 2,
                worshipVector = new float[] { 1, 0, 0, 0, 0, 0, 0 }
            };
            Assert.AreEqual(1f, CultureSimilarity.CalculateSim(a, b), 0.001f, "同单值葬俗+同崇拜向量应完全相似");
        }

        // ===== 模组覆盖语义（Mods 同名 Id 覆盖 Base） =====

        [Test]
        public void ModsOverride_ByIdSemantics()
        {
            // 语义验证：直接覆盖注册表同 Id 条目（Mods 加载路径由加载器保证后载）
            var custom = new EthosDef { ethosId = "ethos_endurance", name = "定制坚忍" };
            ContentRegistry.Ethos["ethos_endurance"] = custom;
            Assert.IsTrue(ContentRegistry.TryGetEthos("ethos_endurance", out var loaded));
            Assert.AreEqual("定制坚忍", loaded.name, "同名 Id 应覆盖");
        }
    }
}
