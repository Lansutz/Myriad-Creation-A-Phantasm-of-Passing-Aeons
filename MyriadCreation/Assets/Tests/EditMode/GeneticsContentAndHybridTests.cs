using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using MyriadCreation.Content.Definitions;
using MyriadCreation.Simulation.Genetics;

namespace MyriadCreation.Tests.EditMode
{
    /// <summary>生殖隔离/基因渗入/外观映射/内容管理器。</summary>
    public class GeneticsContentAndHybridTests
    {
        private static SpeciesDef Species(string id, ReproductiveIsolation isolation = ReproductiveIsolation.None,
            float introgression = 1f)
        {
            return new SpeciesDef
            {
                speciesId = id, genomeId = "genome_test",
                reproductiveIsolation = isolation, paternalIntrogressionRate = introgression, maternalIntrogressionRate = introgression,
                offspringUsesMotherSpecies = true,
            };
        }

        [Test]
        public void Compatibility_NoIsolationButLowIntrogression()
        {
            var father = Species("species_a");
            var mother = Species("species_b", ReproductiveIsolation.None, 0.1f);
            var result = GeneticCompatibility.Evaluate(father, mother);
            Assert.IsTrue(result.allowed);
            Assert.AreEqual(0.1f, result.introgressionRate, 1e-4f);
            Assert.IsTrue(result.useMotherSpecies);
        }

        [Test]
        public void Compatibility_SupportsDifferentPaternalAndMaternalRates()
        {
            var father = Species("species_a");
            var mother = Species("species_b");
            mother.paternalIntrogressionRate = 0.05f;
            mother.maternalIntrogressionRate = 0.6f;
            var result = GeneticCompatibility.Evaluate(father, mother);
            Assert.AreEqual(0.05f, result.paternalIntrogressionRate, 1e-4f);
            Assert.AreEqual(0.6f, result.maternalIntrogressionRate, 1e-4f);
        }

        [Test]
        public void Compatibility_PrezygoticIsolationBlocks()
        {
            var father = Species("species_a", ReproductiveIsolation.Prezygotic);
            var mother = Species("species_b");
            Assert.IsFalse(GeneticCompatibility.Evaluate(father, mother).allowed);
        }

        [Test]
        public void HybridReproduce_ZeroIntrogressionKeepsMotherGenesOnly()
        {
            var genome = new GenomeDef
            {
                genomeId = "genome_test",
                loci = new List<GenomeLocusDef>
                {
                    new GenomeLocusDef { locusId = "a", alleles = new List<AlleleDef>
                    {
                        new AlleleDef { alleleId = "A" },
                        new AlleleDef { alleleId = "a" },
                    } }
                }
            };
            var father = new GenomeInstance { genomeId = "genome_test", paternalAlleles = new[] { 0 }, maternalAlleles = new[] { 0 } };
            var mother = new GenomeInstance { genomeId = "genome_test", paternalAlleles = new[] { 1 }, maternalAlleles = new[] { 1 } };
            var child = GeneticsEngine.ReproduceHybrid(genome, father, mother, GeneticSex.Female, 0f, new System.Random(2), false);
            Assert.AreEqual(child.MaternalAt(0), child.PaternalAt(0), "零渗入时父方基因不保留");
            Assert.AreEqual(1, child.PaternalAt(0));
        }

        [Test]
        public void Appearance_BlueBloodChangesStructuralEyeColor()
        {
            var red = new AppearanceStyleDef
            {
                styleId = "red",
                bloodPigmentId = "hemoglobin",
                eyeModel = "structural",
                pigments = new List<PigmentDef>
                {
                    new PigmentDef { pigmentId = "hemoglobin", color = new Color(0.7f, 0.05f, 0.05f) },
                }
            };
            var blue = new AppearanceStyleDef
            {
                styleId = "blue",
                bloodPigmentId = "hemocyanin",
                eyeModel = "structural",
                pigments = new List<PigmentDef>
                {
                    new PigmentDef { pigmentId = "hemocyanin", color = new Color(0.1f, 0.25f, 0.75f) },
                }
            };
            var redEye = GeneticsAppearance.ComputeEyeColor(red, new Color(0.7f, 0.05f, 0.05f),
                new Color(0.4f, 0.3f, 0.2f), 0f, 0f);
            var blueEye = GeneticsAppearance.ComputeEyeColor(blue, new Color(0.1f, 0.25f, 0.75f),
                new Color(0.4f, 0.3f, 0.2f), 0f, 0f);
            Assert.AreNotEqual(redEye, blueEye, "不同血液色素应影响结构色眼睛");
        }

        [Test]
        public void ContentManager_DefaultRoundTripAndValidation()
        {
            var manager = GeneticsContentManager.CreateDefault();
            Assert.Greater(manager.Genomes.Count, 0);
            string json = manager.ToJson();
            var loaded = new GeneticsContentManager();
            Assert.IsTrue(loaded.LoadFromJson(json, out string error), error);
            Assert.Greater(loaded.Genomes.Count, 0);
            Assert.Greater(loaded.Species.Count, 0);

            var broken = new GeneticsContentManager();
            broken.Register(new SpeciesDef { speciesId = "s", genomeId = "missing" });
            Assert.Greater(broken.Validate().Count, 0);
        }
    }
}
