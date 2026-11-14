using System.Collections.Generic;
using NUnit.Framework;
using MyriadCreation.Content.Definitions;
using MyriadCreation.Infrastructure.Save;
using MyriadCreation.Simulation.ContentHost;
using MyriadCreation.Simulation.State;
using MyriadCreation.Simulation.Systems;

namespace MyriadCreation.Tests.EditMode
{
    /// <summary>
    /// P11.1：专精节点 = 技能值 + 真实实践；只有解锁后才出现对应操作/战术。
    /// </summary>
    public class SpecialtyPerkTests
    {
        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            ContentRegistry.Initialize();
        }

        private static CharacterData Soldier(float military, float experience)
        {
            var c = new CharacterData { military = military };
            c.AddSpecialtyExperience(SpecialtyType.Military, experience);
            return c;
        }

        [Test]
        public void HighSkillWithoutPractice_DoesNotUnlock()
        {
            CharacterData c = Soldier(95f, 0f);
            List<string> unlocked = SpecialtyPerkSystem.TryUnlock(c, SpecialtyType.Military);

            CollectionAssert.IsEmpty(unlocked, "没有真实实践不得解锁，哪怕技能值很高");
            Assert.IsFalse(c.HasPerk("scout_detach"));
        }

        [Test]
        public void SkillAndPractice_UnlocksActionPerk()
        {
            CharacterData c = Soldier(80f, 330f);
            List<string> unlocked = SpecialtyPerkSystem.TryUnlock(c, SpecialtyType.Military);

            CollectionAssert.Contains(unlocked, "scout_detach");
            CollectionAssert.Contains(unlocked, "feigned_retreat");
            Assert.IsTrue(SpecialtyPerkSystem.CanUseAction(c, "scout_detach"),
                "解锁后对应操作应对该人物可用");
        }

        [Test]
        public void Unlock_IsIdempotent()
        {
            CharacterData c = Soldier(80f, 330f);
            SpecialtyPerkSystem.TryUnlock(c, SpecialtyType.Military);
            List<string> again = SpecialtyPerkSystem.TryUnlock(c, SpecialtyType.Military);

            CollectionAssert.IsEmpty(again, "重复调用不应重复解锁");
            Assert.AreEqual(1, CountPerk(c, "scout_detach"));
        }

        [Test]
        public void SaveRoundTrip_PreservesExperienceAndPerks()
        {
            CharacterData c = Soldier(80f, 330f);
            SpecialtyPerkSystem.TryUnlock(c, SpecialtyType.Military);

            CharacterSaveData dto = CharacterSaveData.FromCharacter(c);
            CharacterData restored = dto.ToCharacter();

            Assert.AreEqual(c.GetSpecialtyExperience(SpecialtyType.Military),
                restored.GetSpecialtyExperience(SpecialtyType.Military), 1e-4f);
            CollectionAssert.Contains(restored.unlockedPerks, "scout_detach");
            Assert.IsTrue(restored.HasPerk("scout_detach"));
        }

        private static int CountPerk(CharacterData c, string perkId)
        {
            int count = 0;
            foreach (string id in c.unlockedPerks) if (id == perkId) count++;
            return count;
        }
    }
}
