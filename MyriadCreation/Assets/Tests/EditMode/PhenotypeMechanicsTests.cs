using System.Collections.Generic;
using NUnit.Framework;
using MyriadCreation.Content.Definitions;
using MyriadCreation.Simulation.Genetics;
using MyriadCreation.Simulation.State;
using MyriadCreation.Simulation.Systems;

namespace MyriadCreation.Tests.EditMode
{
    /// <summary>
    /// D3 表型→机制：角爪体型→个人战力、隔热耐温→寒热损耗、翼型→斥候范围。
    /// 反向断言：去掉对应位点后机制回到中性。
    /// </summary>
    public class PhenotypeMechanicsTests
    {
        [TearDown]
        public void TearDown()
        {
            GeneticsCatalog.ResetDefaults();
            DetectionContactService.CommanderResolver = null;
        }

        private static GenomeLocusDef Discrete(string id, string channel, string plus, string minus)
        {
            return new GenomeLocusDef
            {
                locusId = id,
                alleles = new List<AlleleDef>
                {
                    new AlleleDef { alleleId = id + "_p", symbol = "A", effects = new List<AlleleTraitEffect> { new AlleleTraitEffect { discreteTraitId = channel, discreteValue = plus } } },
                    new AlleleDef { alleleId = id + "_m", symbol = "a", effects = new List<AlleleTraitEffect> { new AlleleTraitEffect { discreteTraitId = channel, discreteValue = minus } } }
                }
            };
        }

        private static GenomeLocusDef Additive(string id, string trait, float effect)
        {
            return new GenomeLocusDef
            {
                locusId = id,
                alleles = new List<AlleleDef>
                {
                    new AlleleDef { alleleId = id + "_p", symbol = "A", effects = new List<AlleleTraitEffect> { new AlleleTraitEffect { traitId = trait, additiveEffect = effect } } },
                    new AlleleDef { alleleId = id + "_m", symbol = "a", effects = new List<AlleleTraitEffect> { new AlleleTraitEffect { traitId = trait, additiveEffect = 0f } } }
                }
            };
        }

        private static GenomeDef Genome(bool horned, bool winged, bool insulated)
        {
            return new GenomeDef
            {
                genomeId = "genome_mech",
                loci = new List<GenomeLocusDef>
                {
                    Discrete("horn", "hornType", horned ? "keratin" : "none", horned ? "keratin" : "none"),
                    Discrete("wing", "wingType", winged ? "membrane" : "none", winged ? "membrane" : "none"),
                    Discrete("skin", "skinType", insulated ? "scale" : "skin", insulated ? "scale" : "skin"),
                    Additive("size", "size", horned ? 0.4f : 0f),
                    Additive("armor", "naturalArmor", insulated ? 0.5f : 0f),
                    Additive("insulation", "insulation", insulated ? 0.5f : 0f)
                }
            };
        }

        private static PhenotypeResult Phenotype(GenomeDef genome)
        {
            var instance = new GenomeInstance
            {
                genomeId = genome.genomeId,
                paternalAlleles = new int[genome.loci.Count],
                maternalAlleles = new int[genome.loci.Count]
            };
            return GeneticsEngine.ComputePhenotype(genome, instance);
        }

        [Test]
        public void NeutralPhenotype_HasNoMechanicalBonus()
        {
            PhenotypeResult phenotype = Phenotype(Genome(horned: false, winged: false, insulated: false));

            Assert.AreEqual(0f, PhenotypeMechanics.ProwessBonus(phenotype), 0.001f);
            Assert.AreEqual(0, PhenotypeMechanics.ScoutRangeBonus(phenotype));
            Assert.AreEqual(0f, PhenotypeMechanics.TemperatureHealthPenalty(phenotype, 20f), 0.001f);
        }

        [Test]
        public void HornsSizeAndArmor_RaisePersonalProwess()
        {
            PhenotypeResult neutral = Phenotype(Genome(false, false, false));
            PhenotypeResult armed = Phenotype(Genome(true, false, true));

            Assert.Greater(PhenotypeMechanics.ProwessBonus(armed), PhenotypeMechanics.ProwessBonus(neutral),
                "角/体型/天然护甲应提高近身战力");
            Assert.Greater(PhenotypeMechanics.ProwessBonus(armed), 0f);
        }

        [Test]
        public void Insulation_ReducesColdPenaltyAndWorsensHeat()
        {
            PhenotypeResult bare = Phenotype(Genome(false, false, false));
            PhenotypeResult insulated = Phenotype(Genome(false, false, true));

            float bareCold = PhenotypeMechanics.TemperatureHealthPenalty(bare, -40f);
            float insulatedCold = PhenotypeMechanics.TemperatureHealthPenalty(insulated, -40f);
            float bareHeat = PhenotypeMechanics.TemperatureHealthPenalty(bare, 55f);
            float insulatedHeat = PhenotypeMechanics.TemperatureHealthPenalty(insulated, 55f);

            Assert.Greater(bareCold, 0f, "极寒应有健康损耗");
            Assert.Less(insulatedCold, bareCold, "隔热应减轻寒害");
            Assert.Greater(insulatedHeat, bareHeat, "隔热在酷暑应更难受");
            Assert.LessOrEqual(bareCold, PhenotypeMechanics.MaxDailyTemperaturePenalty);
        }

        [Test]
        public void Wings_GrantScoutRangeBonus()
        {
            PhenotypeResult winged = Phenotype(Genome(false, true, false));

            Assert.AreEqual(2, PhenotypeMechanics.ScoutRangeBonus(winged), "膜翼应提供 2 格斥候范围");
        }

        [Test]
        public void WingedCommander_IncreasesArmyScoutRange()
        {
            GenomeDef genome = Genome(false, true, false);
            GeneticsCatalog.RegisterGenome(genome);
            var commander = new CharacterData
            {
                characterId = 77,
                genome = new GenomeInstance
                {
                    genomeId = genome.genomeId,
                    paternalAlleles = new int[genome.loci.Count],
                    maternalAlleles = new int[genome.loci.Count]
                }
            };
            DetectionContactService.CommanderResolver = id => id == 77 ? commander : null;
            var army = new Army { armyId = 1, commanderId = 77 };
            var unitDefs = new Dictionary<int, UnitDef>();

            int winged = DetectionContactService.ComputeScoutRange(army, null, unitDefs);

            commander.genome = null; // 反向：去掉基因组 → 无表型加成
            int neutral = DetectionContactService.ComputeScoutRange(army, null, unitDefs);

            Assert.Greater(winged, neutral, "具翼指挥官应扩大斥候范围");
        }

        [Test]
        public void TemperaturePenalty_ActuallyDamagesHealth()
        {
            GenomeDef genome = Genome(false, false, true);
            GeneticsCatalog.RegisterGenome(genome);
            var warm = new CharacterData
            {
                characterId = 1,
                age = 20,
                health = 100f,
                birthDay = 200,
                genome = new GenomeInstance { genomeId = genome.genomeId, paternalAlleles = new int[genome.loci.Count], maternalAlleles = new int[genome.loci.Count] }
            };
            var bare = new CharacterData
            {
                characterId = 2,
                age = 20,
                health = 100f,
                birthDay = 200,
                genome = null
            };

            warm.DailyTick(10, 1, 55f);
            bare.DailyTick(10, 1, 55f);

            Assert.Less(warm.health, bare.health, "隔热在酷暑应比无表型者掉血更多");
        }
    }
}
