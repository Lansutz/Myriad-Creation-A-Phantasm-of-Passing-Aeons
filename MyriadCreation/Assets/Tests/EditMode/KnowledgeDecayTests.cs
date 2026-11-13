using NUnit.Framework;
using MyriadCreation.Simulation.State;
using MyriadCreation.Simulation.Systems;

namespace MyriadCreation.Tests.EditMode
{
    /// <summary>P2.3：信息滞后——非本政权人物长期无接触会被遗忘，本政权人物不会被时间遗忘。</summary>
    public class KnowledgeDecayTests
    {
        [TearDown]
        public void TearDown()
        {
            CharacterKnowledgeRegistry.Clear();
        }

        [Test]
        public void PruneStale_ForgetsForeignOnly()
        {
            var foreign = new CharacterData { characterId = 10, realmId = 2 };
            var local = new CharacterData { characterId = 11, realmId = 1 };
            CharacterKnowledgeRegistry.Learn(1, foreign, 100, "spy");
            CharacterKnowledgeRegistry.Learn(1, local, 100, "court");

            int pruned = CharacterKnowledgeRegistry.PruneStale(2000, 1095);

            Assert.AreEqual(1, pruned);
            Assert.IsFalse(CharacterKnowledgeRegistry.IsKnown(1, 10));
            Assert.IsTrue(CharacterKnowledgeRegistry.IsKnown(1, 11), "本政权人物在视野内，不应被遗忘");
        }

        [Test]
        public void RecentForeign_IsKept()
        {
            var foreign = new CharacterData { characterId = 20, realmId = 2 };
            CharacterKnowledgeRegistry.Learn(1, foreign, 1500, "envoy");

            Assert.AreEqual(0, CharacterKnowledgeRegistry.PruneStale(2000, 1095));
            Assert.IsTrue(CharacterKnowledgeRegistry.IsKnown(1, 20));
        }

        [Test]
        public void UnknownRealmLegacyEntry_IsConservative()
        {
            var legacy = new CharacterData { characterId = 30, realmId = -1 };
            CharacterKnowledgeRegistry.Learn(1, legacy, 100, "legacy");

            Assert.AreEqual(0, CharacterKnowledgeRegistry.PruneStale(5000, 1095));
        }
    }
}
