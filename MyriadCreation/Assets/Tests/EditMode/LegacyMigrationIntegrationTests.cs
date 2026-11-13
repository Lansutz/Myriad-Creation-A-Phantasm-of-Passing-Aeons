using NUnit.Framework;
using MyriadCreation.Simulation.Genetics;
using MyriadCreation.Simulation.Laws;
using MyriadCreation.Simulation.State;
using MyriadCreation.Content.Definitions;
using MyriadCreation.Simulation.LawHost;

namespace MyriadCreation.Tests.EditMode
{
    /// <summary>旧 DNA/旧法律到新模型的迁移接线。</summary>
    public class LegacyMigrationIntegrationTests
    {
        [Test]
        public void LegacyDna_MigratesToGenomeInstance()
        {
            var race = new RaceData { genomeId = "genome_human" };
            var dna = new DnaData();
            dna.SetLocus(DnaLocus.Longevity, new LocusPair(Allele.Dominant, Allele.Recessive));
            dna.SetLocus(DnaLocus.Intelligence, new LocusPair(Allele.Recessive, Allele.Recessive));

            var genome = dna.ToGenomeInstance(race, isMale: false);
            Assert.IsNotNull(genome);
            Assert.AreEqual(6, genome.paternalAlleles.Length);
            Assert.AreNotEqual(genome.PaternalAt(0), genome.MaternalAt(0), "寿命座保持杂合");
            Assert.AreEqual(genome.PaternalAt(1), genome.MaternalAt(1), "智慧座保持纯合");
        }

        [Test]
        public void CharacterWithoutLegacyDna_GetsDefaultGenome()
        {
            var character = new CharacterData { characterId = 7, isMale = true };
            var genome = character.GetGenome(new RaceData());
            Assert.IsNotNull(genome);
            Assert.AreEqual(GeneticSex.Male, genome.sex);
            Assert.AreEqual(6, genome.paternalAlleles.Length);
        }

        [Test]
        public void LegacyLawComposition_MigratesToActiveLaws()
        {
            var composition = new LawComposition();
            composition.nationalTax.primary = 2;
            composition.militaryOrganization.primary = 1;

            var state = LawCompositionMigration.ToLawState(composition);
            Assert.AreEqual("tax_heavy", state.GetActiveLaw("taxation"));
            Assert.AreEqual("standing_army", state.GetActiveLaw("military"));
        }

        [Test]
        public void LawGroup_OnlyOneActiveInMigration()
        {
            var composition = new LawComposition();
            composition.nationalTax.primary = 0;
            var state = LawCompositionMigration.ToLawState(composition);
            Assert.AreEqual("tax_light", state.GetActiveLaw("taxation"));
            Assert.AreEqual(1, state.groups.Count);
        }
    }
}
