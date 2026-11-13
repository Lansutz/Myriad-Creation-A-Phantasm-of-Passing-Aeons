using System;
using System.Collections.Generic;
using NUnit.Framework;
using MyriadCreation.Simulation.Genetics;

namespace MyriadCreation.Tests.EditMode
{
    /// <summary>分子/数量遗传学核心：分离、重组、连锁、母系遗传、突变、表型确定性。</summary>
    public class GeneticsEngineTests
    {
        private static GenomeDef SimpleGenome(float mutationRate = 0f)
        {
            return new GenomeDef
            {
                genomeId = "test",
                chromosomeCount = 1,
                loci = new List<GenomeLocusDef>
                {
                    new GenomeLocusDef
                    {
                        locusId = "size", chromosome = 0, mapPositionCm = 0f,
                        mutationRatePerGeneration = mutationRate,
                        alleles = new List<AlleleDef>
                        {
                            new AlleleDef { alleleId = "A", symbol = "A", defaultFrequency = 0.5f,
                                effects = new List<AlleleTraitEffect>
                                {
                                    new AlleleTraitEffect { traitId = "size", additiveEffect = 1f }
                                } },
                            new AlleleDef { alleleId = "a", symbol = "a", defaultFrequency = 0.5f,
                                effects = new List<AlleleTraitEffect>
                                {
                                    new AlleleTraitEffect { traitId = "size", additiveEffect = 0f }
                                } }
                        }
                    }
                }
            };
        }

        private static GenomeInstance Individual(GenomeDef genome, int paternal, int maternal,
            GeneticSex sex = GeneticSex.Female)
        {
            return new GenomeInstance
            {
                genomeId = genome.genomeId,
                sex = sex,
                paternalAlleles = new[] { paternal },
                maternalAlleles = new[] { maternal },
            };
        }

        [Test]
        public void Haldane_MapsDistanceToRecombination()
        {
            Assert.AreEqual(0f, GeneticsEngine.RecombinationFraction(0f), 1e-6f);
            Assert.AreEqual(0.09063f, GeneticsEngine.RecombinationFraction(10f), 0.0001f);
            Assert.Less(GeneticsEngine.RecombinationFraction(50f), 0.5f);
        }

        [Test]
        public void MonohybridCross_ProducesExpectedGenotypes()
        {
            var genome = SimpleGenome();
            var aa = Individual(genome, 1, 1);
            var AA = Individual(genome, 0, 0);
            var rng = new Random(7);
            var counts = new int[3]; // 0=AA,1=Aa,2=aa
            for (int i = 0; i < 20000; i++)
            {
                var child = GeneticsEngine.Reproduce(genome, AA, aa, GeneticSex.Female, rng, allowMutation: false);
                counts[Index(genome, child)]++;
            }
            Assert.AreEqual(0, counts[0]);
            Assert.AreEqual(20000, counts[1]);
            Assert.AreEqual(0, counts[2]);
        }

        [Test]
        public void F1Cross_ApproachesMendelianRatio()
        {
            var genome = SimpleGenome();
            var f1 = Individual(genome, 0, 1);
            var rng = new Random(11);
            var counts = new int[3];
            for (int i = 0; i < 30000; i++)
            {
                var child = GeneticsEngine.Reproduce(genome, f1, f1, GeneticSex.Female, rng, allowMutation: false);
                counts[Index(genome, child)]++;
            }
            Assert.That(counts[1] / (float)(counts[0] + counts[2]), Is.EqualTo(1f).Within(0.08f));
        }

        [Test]
        public void LinkedLoci_RecombineAtHaldaneRate()
        {
            var genome = new GenomeDef
            {
                genomeId = "linked",
                chromosomeCount = 1,
                loci = new List<GenomeLocusDef>
                {
                    new GenomeLocusDef { locusId = "a", chromosome = 0, mapPositionCm = 0f,
                        alleles = new List<AlleleDef>
                        {
                            new AlleleDef { alleleId = "A", symbol = "A" },
                            new AlleleDef { alleleId = "a", symbol = "a" },
                        } },
                    new GenomeLocusDef { locusId = "b", chromosome = 0, mapPositionCm = 10f,
                        alleles = new List<AlleleDef>
                        {
                            new AlleleDef { alleleId = "B", symbol = "B" },
                            new AlleleDef { alleleId = "b", symbol = "b" },
                        } }
                }
            };
            // 相引相 A-B / a-b
            var parent = new GenomeInstance
            {
                genomeId = genome.genomeId,
                paternalAlleles = new[] { 0, 0 },
                maternalAlleles = new[] { 1, 1 },
            };
            // fix invalid field compile guard: remove if not present
            var rng = new Random(3);
            int recombinant = 0, total = 20000;
            for (int i = 0; i < total; i++)
            {
                int[] gamete = GeneticsEngine.BuildGamete(genome, parent, true, GeneticSex.Female, rng);
                bool parental = (gamete[0] == 0 && gamete[1] == 0) || (gamete[0] == 1 && gamete[1] == 1);
                if (!parental) recombinant++;
            }
            float rf = recombinant / (float)total;
            Assert.AreEqual(GeneticsEngine.RecombinationFraction(10f), rf, 0.02f);
        }

        [Test]
        public void Mitochondrial_InheritedFromMotherOnly()
        {
            var genome = new GenomeDef
            {
                genomeId = "mito",
                loci = new List<GenomeLocusDef>
                {
                    new GenomeLocusDef { locusId = "mt", inheritance = GeneticInheritance.Mitochondrial,
                        alleles = new List<AlleleDef>
                        {
                            new AlleleDef { alleleId = "M1", symbol = "M1" },
                            new AlleleDef { alleleId = "M2", symbol = "M2" },
                        } }
                }
            };
            var mother = Individual(genome, -1, 1);
            var father = Individual(genome, -1, 0);
            var child = GeneticsEngine.Reproduce(genome, father, mother, GeneticSex.Male, new Random(1), false);
            Assert.AreEqual(-1, child.PaternalAt(0));
            Assert.AreEqual(1, child.MaternalAt(0));
        }

        [Test]
        public void Mutation_ChangesAlleleAndCounts()
        {
            var genome = SimpleGenome(mutationRate: 1f);
            var parent = Individual(genome, 0, 1);
            var child = GeneticsEngine.Reproduce(genome, parent, parent, GeneticSex.Female, new Random(5), true);
            Assert.Greater(child.mutationCount, 0);
        }

        [Test]
        public void Phenotype_IsDeterministicAndAdditive()
        {
            var genome = SimpleGenome();
            var aa = Individual(genome, 1, 1);
            var Aa = Individual(genome, 0, 1);
            var AA = Individual(genome, 0, 0);
            Assert.AreEqual(0f, GeneticsEngine.ComputePhenotype(genome, aa).GetTrait("size"), 1e-5f);
            Assert.AreEqual(1f, GeneticsEngine.ComputePhenotype(genome, Aa).GetTrait("size"), 1e-5f);
            Assert.AreEqual(2f, GeneticsEngine.ComputePhenotype(genome, AA).GetTrait("size"), 1e-5f);
            Assert.AreEqual(GeneticsEngine.ComputePhenotype(genome, Aa).GetTrait("size"),
                GeneticsEngine.ComputePhenotype(genome, Aa).GetTrait("size"), 1e-6f);
        }

        private static int Index(GenomeDef genome, GenomeInstance child)
        {
            int a = child.PaternalAt(0), b = child.MaternalAt(0);
            if (a == 0 && b == 0) return 0;
            if (a == 1 && b == 1) return 2;
            return 1;
        }
    }
}
