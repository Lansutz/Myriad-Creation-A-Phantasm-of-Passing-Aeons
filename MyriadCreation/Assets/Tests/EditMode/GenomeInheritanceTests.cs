using NUnit.Framework;
using MyriadCreation.Content.Definitions;
using MyriadCreation.Core.Enums;
using MyriadCreation.Simulation.State;
using MyriadCreation.Simulation.Systems;

namespace MyriadCreation.Tests.EditMode
{
    /// <summary>P2.5 E：子代以 GenomeInstance 为唯一运行时遗传真源，不再保留 DnaData。</summary>
    public class GenomeInheritanceTests
    {
        [Test]
        public void ChildOfTwoParents_GetsGenomeOnly()
        {
            var manager = new CharacterManager();
            CharacterData father = manager.CreateCharacter("父", "测", 30, true, 0, 0, 0, CharacterRole.Noble);
            CharacterData mother = manager.CreateCharacter("母", "测", 28, false, 0, 0, 0, CharacterRole.Noble);

            CharacterData child = manager.CreateCharacter(
                "子", "测", 0, true, 0, 0, 0, CharacterRole.Noble,
                null, father.characterId, mother.characterId);

            Assert.IsNotNull(child.genome, "子代必须有 GenomeInstance");
            Assert.IsNull(child.dna, "运行时不再保留 DnaData");
            Assert.AreNotEqual(-1, child.genome.genomeId);
        }
    }
}
