using System;
using System.Collections.Generic;
using NUnit.Framework;
using MyriadCreation.Simulation.Genetics;
using MyriadCreation.Content.Definitions;

namespace MyriadCreation.Tests.EditMode
{
    /// <summary>性染色体不只 XY：ZW / X0 / 单倍二倍体 / 环境 / 雌雄同体。</summary>
    public class SexChromosomeSystemTests
    {
        private static GenomeDef SingleLocus(SexDeterminationSystem system,
            GeneticInheritance inheritance = GeneticInheritance.XLinked)
        {
            return new GenomeDef
            {
                genomeId = "sex_test",
                chromosomeCount = 1,
                sexSystem = new SexChromosomeSystemDef
                {
                    system = system,
                    heterogameticChromosome = system == SexDeterminationSystem.ZW ? "W" : "Y",
                    homogameticChromosome = system == SexDeterminationSystem.ZW ? "Z" : "X",
                },
                loci = new List<GenomeLocusDef>
                {
                    new GenomeLocusDef
                    {
                        locusId = "sex_linked", chromosome = 0, inheritance = inheritance,
                        alleles = new List<AlleleDef>
                        {
                            new AlleleDef { alleleId = "A", symbol = "A", defaultFrequency = 0.5f },
                            new AlleleDef { alleleId = "a", symbol = "a", defaultFrequency = 0.5f },
                        }
                    }
                }
            };
        }

        private static GenomeInstance Individual(GenomeDef genome, GeneticSex sex, int paternal, int maternal)
        {
            return new GenomeInstance
            {
                genomeId = genome.genomeId,
                sex = sex,
                paternalAlleles = new[] { paternal },
                maternalAlleles = new[] { maternal },
            };
        }

        private enum Genus { Female, Male, Hermaphrodite }

        private static GeneticSex Sex(Genus genus) => genus switch
        {
            Genus.Female => GeneticSex.Female,
            Genus.Male => GeneticSex.Male,
            _ => GeneticSex.Hermaphrodite,
        };

        [Test]
        public void ZW_FemalePassesWToDaughters_AndZToSons()
        {
            var genome = SingleLocus(SexDeterminationSystem.ZW);
            var father = Individual(genome, Sex(Genus.Male), 0, 0);      // ZZ
            var mother = Individual(genome, Sex(Genus.Female), 0, -1);   // ZW
            var rng = new Random(5);

            var daughter = GeneticsEngine.Reproduce(genome, father, mother, GeneticSex.Female, rng, false);
            Assert.AreEqual(-1, daughter.MaternalAt(0), "女儿从母亲得到 W，无 Z 等位基因");
            Assert.AreEqual(0, daughter.PaternalAt(0), "女儿从父亲得到 Z");

            var son = GeneticsEngine.Reproduce(genome, father, mother, GeneticSex.Male, rng, false);
            Assert.AreEqual(0, son.PaternalAt(0));
            Assert.AreEqual(0, son.MaternalAt(0), "儿子从母亲得到 Z");
        }

        [Test]
        public void XO_SonsGetXFromMotherOnly()
        {
            var genome = SingleLocus(SexDeterminationSystem.XO);
            var father = Individual(genome, Sex(Genus.Male), -1, 0);     // X0
            var mother = Individual(genome, Sex(Genus.Female), 0, 1);

            var son = GeneticsEngine.Reproduce(genome, father, mother, GeneticSex.Male, new Random(1), false);
            Assert.AreEqual(-1, son.PaternalAt(0), "儿子没有父源 X");
            Assert.GreaterOrEqual(son.MaternalAt(0), 0);

            var daughter = GeneticsEngine.Reproduce(genome, father, mother, GeneticSex.Female, new Random(1), false);
            Assert.GreaterOrEqual(daughter.PaternalAt(0), 0, "女儿从父亲得到 X");
            Assert.GreaterOrEqual(daughter.MaternalAt(0), 0);
        }

        [Test]
        public void Haplodiploid_MaleIsHaploid()
        {
            var genome = SingleLocus(SexDeterminationSystem.Haplodiploid, GeneticInheritance.Autosomal);
            var father = Individual(genome, Sex(Genus.Male), -1, 0);
            var mother = Individual(genome, Sex(Genus.Female), 0, 1);

            var drone = GeneticsEngine.Reproduce(genome, father, mother, GeneticSex.Male, new Random(3), false);
            Assert.AreEqual(1, drone.ploidy);
            Assert.AreEqual(-1, drone.PaternalAt(0), "单倍体雄蜂无父源等位基因");

            var worker = GeneticsEngine.Reproduce(genome, father, mother, GeneticSex.Female, new Random(3), false);
            Assert.AreEqual(2, worker.ploidy);
        }

        [Test]
        public void EnvironmentalSex_BiasControlsPhenotype()
        {
            var genome = new GenomeDef { sexSystem = new SexChromosomeSystemDef { system = SexDeterminationSystem.Environmental } };
            Assert.AreEqual(GeneticSex.Female, GeneticsEngine.DetermineSex(genome, new Random(1), 1f));
            Assert.AreEqual(GeneticSex.Male, GeneticsEngine.DetermineSex(genome, new Random(1), 0f));
        }

        [Test]
        public void Hermaphroditic_ReturnsHermaphrodite()
        {
            var genome = new GenomeDef { sexSystem = new SexChromosomeSystemDef { system = SexDeterminationSystem.Hermaphroditic } };
            Assert.AreEqual(GeneticSex.Hermaphrodite, GeneticsEngine.DetermineSex(genome, new Random(2)));
        }
    }
}
