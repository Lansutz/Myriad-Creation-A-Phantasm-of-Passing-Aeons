using NUnit.Framework;
using MyriadCreation.Simulation.State;

namespace MyriadCreation.Tests.EditMode
{
    /// <summary>M3：有效属性出口（特质/精神疾病修正生效）。</summary>
    public class EffectiveStatsTests
    {
        [Test]
        public void MentalDisorder_ReducesEffectiveSocial()
        {
            var character = new CharacterData { social = 50f, mentalDisorderId = "depression" };
            character.GetEffectiveStats(out _, out float social, out _, out _, out _, out _, out _);
            Assert.Less(social, 50f, "抑郁应降低有效社交");
        }

        [Test]
        public void TraitModifiers_ApplyToEffectiveStats()
        {
            var character = new CharacterData { prowess = 50f, social = 50f };
            character.traits.Add(new PersonalityTrait { martialMod = -10f, diplomacyMod = -5f });
            character.GetEffectiveStats(out float prowess, out float social, out _, out _, out _, out _, out _);
            Assert.AreEqual(40f, prowess, 0.01f);
            Assert.AreEqual(45f, social, 0.01f);
        }
    }
}
