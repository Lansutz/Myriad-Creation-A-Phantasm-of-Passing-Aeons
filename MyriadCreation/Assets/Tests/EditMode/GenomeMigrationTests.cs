using NUnit.Framework;
using MyriadCreation.Content.Definitions;
using MyriadCreation.Infrastructure.Save;
using MyriadCreation.Simulation.Genetics;
using MyriadCreation.Simulation.State;
using UnityEngine;

namespace MyriadCreation.Tests.EditMode
{
    /// <summary>
    /// D6 基因组结构迁移：旧 GenomeInstance 短数组按当前位点数补齐，
    /// 新位点取中性等位基因，旧位点原样保留，读写往返稳定。
    /// </summary>
    public class GenomeMigrationTests
    {
        [TearDown]
        public void TearDown()
        {
            GeneticsCatalog.ResetDefaults();
        }

        private static GenomeInstance LegacyHuman()
        {
            // 旧版人类基因组只有前 6 个位点；给出非默认值以验证“旧位点不被改写”
            int[] paternal = { 1, 1, 0, 1, 0, 1 };
            int[] maternal = { 1, 1, 1, 1, 1, 1 };
            return new GenomeInstance
            {
                genomeId = "genome_human",
                paternalAlleles = paternal,
                maternalAlleles = maternal
            };
        }

        [Test]
        public void Upgrade_ExtendsToCurrentLociAndKeepsExistingAlleles()
        {
            GenomeInstance legacy = LegacyHuman();
            int expected = GeneticsCatalog.GetDefaultHumanGenome().loci.Count;

            GenomeInstance upgraded = GenomeMigration.Upgrade(legacy);

            Assert.AreEqual(expected, upgraded.paternalAlleles.Length, "补齐到当前基因组位点数");
            Assert.AreEqual(expected, upgraded.maternalAlleles.Length);
            Assert.AreEqual(1, upgraded.paternalAlleles[0], "旧位点保持原值");
            Assert.AreEqual(1, upgraded.maternalAlleles[0]);
            Assert.AreEqual(GenomeMigration.CurrentVersion, upgraded.genomeVersion);
        }

        [Test]
        public void Upgrade_NewLociUseNeutralAlleles()
        {
            GenomeInstance upgraded = GenomeMigration.Upgrade(LegacyHuman());
            GenomeDef genome = GeneticsCatalog.GetDefaultHumanGenome();

            PhenotypeResult phenotype = GeneticsEngine.ComputePhenotype(genome, upgraded);

            Assert.AreEqual(1f, phenotype.metabolism.size, 0.001f, "新体型位点不得凭空给旧档加体型");
            Assert.AreEqual(0f, phenotype.metabolism.tempToleranceC, 0.001f);
            Assert.AreEqual(0.5f, phenotype.integument.insulation, 0.001f);
            Assert.AreEqual("none", phenotype.bodyPlan.wingType);
            Assert.AreEqual("skin", phenotype.integument.skinType);
        }

        [Test]
        public void Upgrade_IsIdempotent()
        {
            GenomeInstance upgraded = GenomeMigration.Upgrade(LegacyHuman());
            int[] paternal = upgraded.paternalAlleles;
            int[] maternal = upgraded.maternalAlleles;

            GenomeInstance again = GenomeMigration.Upgrade(upgraded);

            Assert.AreSame(upgraded, again, "升级应就地返回同一实例");
            Assert.AreSame(paternal, again.paternalAlleles, "已补齐的数组不应重写");
            Assert.AreSame(maternal, again.maternalAlleles);
        }

        [Test]
        public void Upgrade_SurvivesJsonRoundTrip()
        {
            GenomeInstance upgraded = GenomeMigration.Upgrade(LegacyHuman());
            string json = JsonUtility.ToJson(upgraded);
            GenomeInstance restored = JsonUtility.FromJson<GenomeInstance>(json);

            GenomeInstance migrated = GenomeMigration.Upgrade(restored);

            Assert.AreEqual(upgraded.paternalAlleles.Length, migrated.paternalAlleles.Length);
            Assert.AreEqual(GenomeMigration.CurrentVersion, migrated.genomeVersion);
        }

        [Test]
        public void UpgradedGenome_CanReproduce()
        {
            GenomeInstance father = GenomeMigration.Upgrade(LegacyHuman());
            GenomeInstance mother = GenomeMigration.Upgrade(LegacyHuman());
            GenomeDef genome = GeneticsCatalog.GetDefaultHumanGenome();

            GenomeInstance child = GeneticsEngine.Reproduce(genome, father, mother, GeneticSex.Female,
                new System.Random(11), allowMutation: false);

            Assert.AreEqual(genome.loci.Count, child.paternalAlleles.Length, "迁移后的实例可继续繁殖");
        }

        [Test]
        public void CharacterSaveRoundTrip_UpgradesLegacyGenome()
        {
            var character = new CharacterData { characterId = 9, genome = LegacyHuman() };

            CharacterSaveData dto = CharacterSaveData.FromCharacter(character);
            CharacterData restored = dto.ToCharacter();

            Assert.AreEqual(GeneticsCatalog.GetDefaultHumanGenome().loci.Count, restored.genome.paternalAlleles.Length,
                "角色存档往返应完成旧基因组补齐");
            Assert.AreEqual(GenomeMigration.CurrentVersion, restored.genome.genomeVersion);
        }

        [Test]
        public void CharacterGetGenome_UpgradesLegacyInstance()
        {
            var character = new CharacterData { characterId = 5, genome = LegacyHuman() };

            GenomeInstance genome = character.GetGenome(new RaceData());

            Assert.AreEqual(GeneticsCatalog.GetDefaultHumanGenome().loci.Count, genome.paternalAlleles.Length);
        }
    }
}
