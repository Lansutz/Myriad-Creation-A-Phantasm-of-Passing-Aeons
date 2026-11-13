using NUnit.Framework;
using MyriadCreation.Simulation.State;
using MyriadCreation.Simulation.Systems;

namespace MyriadCreation.Tests.EditMode
{
    /// <summary>角色位置在聚落；公开信息与同政权信息决定可见性。</summary>
    public class CharacterLocationTests
    {
        [SetUp]
        public void ResetKnowledge() => CharacterKnowledgeRegistry.Clear();

        [Test]
        public void Character_IsNotKnownUntilInformationArrives()
        {
            var official = new CharacterData { characterId = 11, isPublicFigure = true, realmId = 2, currentSettlementAnchorId = 7 };
            Assert.IsFalse(CharacterLocationService.IsKnownTo(official, 0),
                "公众人物也不能跨政权自动可见");
            CharacterKnowledgeRegistry.Learn(0, official, 1, "rumor");
            Assert.IsTrue(CharacterLocationService.IsKnownTo(official, 0));
        }

        [Test]
        public void Knowledge_IsPerObserver()
        {
            var character = new CharacterData { characterId = 12, realmId = 2, currentSettlementAnchorId = 7 };
            CharacterKnowledgeRegistry.Learn(2, character, 1, "court");
            Assert.IsTrue(CharacterLocationService.IsKnownTo(character, 2));
            Assert.IsFalse(CharacterLocationService.IsKnownTo(character, 0));
        }
    }
}
