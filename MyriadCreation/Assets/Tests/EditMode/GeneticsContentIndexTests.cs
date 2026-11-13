using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using MyriadCreation.Content.Modding;
using MyriadCreation.Content.Definitions;
using UnityEngine;

namespace MyriadCreation.Tests.EditMode
{
    /// <summary>
    /// D1 基因组/物种内容化：纯定义由 ContentDefinitionIndex 解析，Base 打底、Mods 按 id 覆盖、坏引用被拒。
    /// </summary>
    public class GeneticsContentIndexTests
    {
        [System.Serializable]
        private class GenomeProbe
        {
            public List<GenomeDef> genomes = new List<GenomeDef>();
        }

        private string _root;

        [SetUp]
        public void SetUp()
        {
            _root = Path.Combine(Path.GetTempPath(), "myriad_genetics_idx_" + System.Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(Path.Combine(_root, "Base", "Genetics"));
        }

        [TearDown]
        public void TearDown()
        {
            if (!string.IsNullOrEmpty(_root) && Directory.Exists(_root)) Directory.Delete(_root, true);
        }

        private static string GenomeJson(string id, int chromosomes)
        {
            return "{\"genomes\":[{\"genomeId\":\"" + id + "\",\"speciesId\":\"species_test\",\"chromosomeCount\":" + chromosomes + "," +
                   "\"loci\":[{\"locusId\":\"body\",\"chromosome\":0,\"inheritance\":0,\"alleles\":[" +
                   "{\"alleleId\":\"a\",\"symbol\":\"A\",\"effects\":[{\"traitId\":\"size\",\"additiveEffect\":0.2}],\"defaultFrequency\":0.5}," +
                   "{\"alleleId\":\"b\",\"symbol\":\"a\",\"effects\":[{\"traitId\":\"size\",\"additiveEffect\":-0.2}],\"defaultFrequency\":0.5}" +
                   "]}]}]}";
        }

        private void WriteBase(string genomes, string species)
        {
            File.WriteAllText(Path.Combine(_root, "Base", "Genetics", "Genomes.json"), genomes);
            File.WriteAllText(Path.Combine(_root, "Base", "Genetics", "Species.json"), species);
        }

        [Test]
        public void Initialize_LoadsGenomesAndSpecies()
        {
            WriteBase(GenomeJson("genome_test", 1),
                "{\"species\":[{\"speciesId\":\"species_test\",\"name\":\"测试种\",\"genomeId\":\"genome_test\"}]}");
            GenomeProbe probe = JsonUtility.FromJson<GenomeProbe>(File.ReadAllText(Path.Combine(_root, "Base", "Genetics", "Genomes.json")));
            Assert.AreEqual(1, probe.genomes.Count, "前置：JSON 本身应能解析出 1 个基因组");
            var index = new ContentDefinitionIndex();

            index.Initialize(_root, ContentDefinitionIndex.InnovationFallbackGroups.Default);

            Assert.IsTrue(index.Genomes.ContainsKey("genome_test"), "基因组应进入内容索引");
            Assert.IsTrue(index.Species.ContainsKey("species_test"), "物种应进入内容索引");
        }

        [Test]
        public void Mods_OverrideBaseById()
        {
            WriteBase(GenomeJson("genome_test", 1),
                "{\"species\":[{\"speciesId\":\"species_test\",\"name\":\"测试种\",\"genomeId\":\"genome_test\"}]}");
            string modDir = Path.Combine(_root, "Mods", "mod_a", "Genetics");
            Directory.CreateDirectory(modDir);
            File.WriteAllText(Path.Combine(modDir, "Genomes.json"), GenomeJson("genome_test", 4));
            File.WriteAllText(Path.Combine(modDir, "Species.json"),
                "{\"species\":[{\"speciesId\":\"species_test\",\"name\":\"模组种\",\"genomeId\":\"genome_test\"}]}");
            var index = new ContentDefinitionIndex();

            index.Initialize(_root, ContentDefinitionIndex.InnovationFallbackGroups.Default);

            Assert.AreEqual(4, index.Genomes["genome_test"].chromosomeCount, "Mods 应按 id 覆盖 Base");
            Assert.AreEqual("模组种", index.Species["species_test"].name, "物种也应被 Mods 覆盖");
        }

        [Test]
        public void BadSpeciesReference_IsRejected()
        {
            WriteBase(GenomeJson("genome_test", 1),
                "{\"species\":[" +
                "{\"speciesId\":\"species_ok\",\"name\":\"好种\",\"genomeId\":\"genome_test\"}," +
                "{\"speciesId\":\"species_bad\",\"name\":\"坏种\",\"genomeId\":\"genome_missing\"}]}");
            var index = new ContentDefinitionIndex();

            index.Initialize(_root, ContentDefinitionIndex.InnovationFallbackGroups.Default);

            Assert.IsTrue(index.Species.ContainsKey("species_ok"));
            Assert.IsFalse(index.Species.ContainsKey("species_bad"), "引用不存在基因组的物种应被剔除");
        }
    }
}
