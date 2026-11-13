using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using MyriadCreation.Content.Definitions;
using MyriadCreation.Content.Modding;
using MyriadCreation.Simulation.Genetics;

namespace MyriadCreation.Tests.EditMode
{
    /// <summary>
    /// D4/D5 物种目录、生殖隔离与纯内容驱动的瓦伊凡样例：代码不得按物种名特判。
    /// </summary>
    public class SpeciesWyvernTests
    {
        [TearDown]
        public void TearDown()
        {
            GeneticsCatalog.ResetDefaults();
        }

        private static void LoadRealContent()
        {
            var index = new ContentDefinitionIndex();
            index.Initialize(Application.streamingAssetsPath, ContentDefinitionIndex.InnovationFallbackGroups.Default);
            GeneticsCatalog.ResetDefaults();
            foreach (GenomeDef genome in index.Genomes.Values) GeneticsCatalog.RegisterGenome(genome);
            foreach (SpeciesDef species in index.Species.Values) GeneticsCatalog.RegisterSpecies(species);
        }

        [Test]
        public void WyvernContent_LoadsFromJson()
        {
            LoadRealContent();

            Assert.IsTrue(GeneticsCatalog.TryGetGenome("genome_wyvern", out GenomeDef genome), "瓦伊凡基因组应来自 JSON");
            Assert.AreEqual(10, genome.loci.Count);
            Assert.IsTrue(GeneticsCatalog.TryGetSpecies("species_wyvern", out SpeciesDef species));
            Assert.AreEqual("genome_wyvern", species.genomeId);
        }

        [Test]
        public void WyvernPhenotype_ComesFromGenomeNotSpeciesName()
        {
            LoadRealContent();
            GeneticsCatalog.TryGetGenome("genome_wyvern", out GenomeDef genome);
            GenomeInstance founder = GeneticsEngine.CreateFounder(genome, GeneticSex.Female, new System.Random(3));

            PhenotypeResult phenotype = GeneticsEngine.ComputePhenotype(genome, founder);

            Assert.AreEqual("membrane", phenotype.bodyPlan.wingType, "膜翼来自 wing 位点，不是物种名特判");
            Assert.AreEqual("keratin", phenotype.bodyPlan.hornType);
            Assert.AreEqual("long", phenotype.bodyPlan.tailType);
            Assert.AreEqual("quadruped", phenotype.bodyPlan.posture);
            Assert.AreEqual("scale", phenotype.integument.skinType);
            Assert.AreEqual(1.8f, phenotype.metabolism.size, 0.001f);
            Assert.AreEqual(1f, phenotype.metabolism.dietCarnivore, 0.001f);
            Assert.AreEqual(0.9f, phenotype.integument.insulation, 0.001f);
            Assert.AreEqual(0.8f, phenotype.integument.naturalArmor, 0.001f);
        }

        [Test]
        public void CloseRelativeWithoutWings_IsNotForcedToFly()
        {
            LoadRealContent();
            GeneticsCatalog.TryGetGenome("genome_drake", out GenomeDef genome);
            GenomeInstance founder = GeneticsEngine.CreateFounder(genome, GeneticSex.Male, new System.Random(5));

            PhenotypeResult phenotype = GeneticsEngine.ComputePhenotype(genome, founder);

            Assert.AreEqual("none", phenotype.bodyPlan.wingType, "地龙没有翼位点优势等位基因，不应因同属龙类而有翼");
            Assert.AreEqual("keratin", phenotype.bodyPlan.hornType);
            Assert.AreEqual("scale", phenotype.integument.skinType);
        }

        [Test]
        public void SpeciesIsolationMatrix_RespectsContent()
        {
            LoadRealContent();
            GeneticsCatalog.TryGetSpecies("species_wyvern", out SpeciesDef wyvern);
            GeneticsCatalog.TryGetSpecies("species_drake", out SpeciesDef drake);
            GeneticsCatalog.TryGetSpecies("species_human", out SpeciesDef human);

            Assert.IsTrue(GeneticCompatibility.Evaluate(wyvern, wyvern).allowed, "同种可育");
            Assert.IsFalse(GeneticCompatibility.Evaluate(wyvern, human).allowed, "远缘（内容声明隔离）不可育");
            Assert.IsFalse(GeneticCompatibility.Evaluate(drake, human).allowed, "地龙与人类也不可育");
        }

        [Test]
        public void SpeciesCatalog_ResolvesByGenome()
        {
            LoadRealContent();

            Assert.AreEqual("species_wyvern", GeneticsCatalog.FindSpeciesForGenome("genome_wyvern")?.speciesId);
            Assert.AreEqual("species_human", GeneticsCatalog.FindSpeciesForGenome("genome_human")?.speciesId);
        }
    }
}
