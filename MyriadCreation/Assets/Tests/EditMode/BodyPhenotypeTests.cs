using System.Collections.Generic;
using NUnit.Framework;
using MyriadCreation.Content.Definitions;
using MyriadCreation.Simulation.Genetics;

namespace MyriadCreation.Tests.EditMode
{
    /// <summary>
    /// D2 身体构造/体表/代谢表型：离散等位基因按显性表达，加性位点折算为可解释的生理量。
    /// </summary>
    public class BodyPhenotypeTests
    {
        private static GenomeLocusDef Discrete(string id, string discreteId, string plus, string minus)
        {
            return new GenomeLocusDef
            {
                locusId = id,
                chromosome = 0,
                alleles = new List<AlleleDef>
                {
                    new AlleleDef
                    {
                        alleleId = id + "_plus",
                        symbol = "A",
                        effects = new List<AlleleTraitEffect>
                        {
                            new AlleleTraitEffect { discreteTraitId = discreteId, discreteValue = plus }
                        }
                    },
                    new AlleleDef
                    {
                        alleleId = id + "_minus",
                        symbol = "a",
                        effects = new List<AlleleTraitEffect>
                        {
                            new AlleleTraitEffect { discreteTraitId = discreteId, discreteValue = minus }
                        }
                    }
                }
            };
        }

        private static GenomeLocusDef Additive(string id, string traitId, float effect)
        {
            return new GenomeLocusDef
            {
                locusId = id,
                chromosome = 0,
                alleles = new List<AlleleDef>
                {
                    new AlleleDef
                    {
                        alleleId = id + "_plus",
                        symbol = "A",
                        effects = new List<AlleleTraitEffect>
                        {
                            new AlleleTraitEffect { traitId = traitId, additiveEffect = effect }
                        }
                    },
                    new AlleleDef
                    {
                        alleleId = id + "_minus",
                        symbol = "a",
                        effects = new List<AlleleTraitEffect>
                        {
                            new AlleleTraitEffect { traitId = traitId, additiveEffect = 0f }
                        }
                    }
                }
            };
        }

        private static GenomeDef Genome()
        {
            return new GenomeDef
            {
                genomeId = "genome_test",
                loci = new List<GenomeLocusDef>
                {
                    Discrete("wing", "wingType", "membrane", "none"),
                    Discrete("horn", "hornType", "keratin", "none"),
                    Discrete("skin", "skinType", "scale", "skin"),
                    Additive("size", "size", 0.3f),
                    Additive("diet", "dietCarnivore", 0.5f),
                    Additive("temp", "tempToleranceC", 5f)
                }
            };
        }

        private static GenomeInstance Instance(int[] paternal, int[] maternal)
        {
            return new GenomeInstance
            {
                genomeId = "genome_test",
                paternalAlleles = paternal,
                maternalAlleles = maternal
            };
        }

        [Test]
        public void DiscreteTrait_DominantAlleleWins()
        {
            GenomeDef genome = Genome();
            // wing: membrane(A) / none(a)
            PhenotypeResult phenotype = GeneticsEngine.ComputePhenotype(genome, Instance(
                new[] { 0, 0, 0, 0, 0, 0 }, new[] { 1, 1, 1, 0, 0, 0 }));

            Assert.AreEqual("membrane", phenotype.bodyPlan.wingType, "显性膜翼应压过隐性无翼");
            Assert.AreEqual("keratin", phenotype.bodyPlan.hornType);
            Assert.AreEqual("scale", phenotype.integument.skinType);
        }

        [Test]
        public void DiscreteTrait_HomozygousRecessiveShowsRecessive()
        {
            GenomeDef genome = Genome();
            PhenotypeResult phenotype = GeneticsEngine.ComputePhenotype(genome, Instance(
                new[] { 1, 1, 1, 1, 1, 1 }, new[] { 1, 1, 1, 1, 1, 1 }));

            Assert.AreEqual("none", phenotype.bodyPlan.wingType);
            Assert.AreEqual("skin", phenotype.integument.skinType);
        }

        [Test]
        public void AdditiveLoci_BecomeExplainablePhysiology()
        {
            GenomeDef genome = Genome();
            PhenotypeResult phenotype = GeneticsEngine.ComputePhenotype(genome, Instance(
                new[] { 1, 1, 1, 0, 0, 0 }, new[] { 1, 1, 1, 0, 0, 0 }));

            Assert.AreEqual(1.6f, phenotype.metabolism.size, 0.001f, "体型 = 基线 1 + 两份 +0.3");
            Assert.AreEqual(1f, phenotype.metabolism.dietCarnivore, 0.001f, "两份 +0.5 食性把肉食拉满");
            Assert.AreEqual(0f, phenotype.metabolism.dietHerbivore, 0.001f);
            Assert.AreEqual(10f, phenotype.metabolism.tempToleranceC, 0.001f, "两份 +5°C 耐温");
        }

        [Test]
        public void MissingLoci_FallBackToNeutralDefaults()
        {
            GenomeDef genome = Genome();
            // 旧实例：只有第一个位点，其余位点缺失
            PhenotypeResult phenotype = GeneticsEngine.ComputePhenotype(genome, Instance(new[] { 0 }, new[] { 0 }));

            Assert.AreEqual("membrane", phenotype.bodyPlan.wingType);
            Assert.AreEqual("none", phenotype.bodyPlan.hornType, "缺位点回退无角，而不是随机");
            Assert.AreEqual("skin", phenotype.integument.skinType);
            Assert.AreEqual(1f, phenotype.metabolism.size, 0.001f, "缺位点回退中性体型");
        }

        [Test]
        public void Mutation_CanFlipDiscreteAllele()
        {
            GenomeDef genome = new GenomeDef
            {
                genomeId = "genome_mut",
                loci = new List<GenomeLocusDef>
                {
                    new GenomeLocusDef
                    {
                        locusId = "wing",
                        mutationRatePerGeneration = 1f,
                        alleles = new List<AlleleDef>
                        {
                            new AlleleDef { alleleId = "w1", symbol = "A", defaultFrequency = 0.5f, effects = new List<AlleleTraitEffect> { new AlleleTraitEffect { discreteTraitId = "wingType", discreteValue = "membrane" } } },
                            new AlleleDef { alleleId = "w2", symbol = "a", defaultFrequency = 0.5f, effects = new List<AlleleTraitEffect> { new AlleleTraitEffect { discreteTraitId = "wingType", discreteValue = "none" } } }
                        }
                    }
                }
            };
            GenomeInstance father = GeneticsEngine.CreateFounder(genome, GeneticSex.Male, new System.Random(1));
            GenomeInstance mother = GeneticsEngine.CreateFounder(genome, GeneticSex.Female, new System.Random(2));

            GenomeInstance child = GeneticsEngine.Reproduce(genome, father, mother, GeneticSex.Female, new System.Random(3), allowMutation: true);

            Assert.AreNotEqual(father.PaternalAt(0), child.PaternalAt(0), "突变率 1 时子代等位基因应翻转（双等位基因位点）");
        }

        [Test]
        public void FoundationAndReproduction_KeepDiscreteAlleles()
        {
            GenomeDef genome = Genome();
            GenomeInstance founder = GeneticsEngine.CreateFounder(genome, GeneticSex.Female, new System.Random(7));
            GenomeInstance father = GeneticsEngine.CreateFounder(genome, GeneticSex.Male, new System.Random(8));

            GenomeInstance child = GeneticsEngine.Reproduce(genome, father, founder, GeneticSex.Male, new System.Random(9), allowMutation: false);

            Assert.AreEqual(genome.loci.Count, child.paternalAlleles.Length);
            Assert.AreEqual(genome.loci.Count, child.maternalAlleles.Length);
            Assert.AreEqual(genome.loci.Count, founder.paternalAlleles.Length, "创始人应覆盖全部新位点");
        }
    }
}
