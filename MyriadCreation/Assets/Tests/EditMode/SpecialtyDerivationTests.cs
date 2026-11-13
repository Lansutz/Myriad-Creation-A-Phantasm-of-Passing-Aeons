using NUnit.Framework;
using MyriadCreation.Content.Definitions;

namespace MyriadCreation.Tests.EditMode
{
    /// <summary>M3：专精派生公式（天赋×训练，乘法，无训练近 0）。</summary>
    public class SpecialtyDerivationTests
    {
        private static BasicAbilities Abilities(float v)
        {
            return new BasicAbilities
            {
                strength = v,
                coordination = v,
                endurance = v,
                memory = v,
                logic = v,
                inspiration = v,
                eloquence = v,
                insight = v,
                charisma = v,
                observation = v,
                intuition = v,
                reaction = v
            };
        }

        [Test]
        public void NoTraining_YieldsNearZero()
        {
            Assert.AreEqual(0f, SpecialtyDerivation.Derive(SpecialtyType.Prowess, Abilities(100f), 0f), 0.001f);
        }

        [Test]
        public void FullBasicAndTraining_YieldsHundred()
        {
            Assert.AreEqual(100f, SpecialtyDerivation.Derive(SpecialtyType.Prowess, Abilities(100f), 1f), 0.001f);
        }

        [Test]
        public void HalfBasicHalfTraining_YieldsHalf()
        {
            float value = SpecialtyDerivation.Derive(SpecialtyType.Social, Abilities(50f), 0.5f);
            Assert.AreEqual(50f, value, 0.001f);
        }

        [Test]
        public void DifferentSpecialties_UseDifferentBasicMix()
        {
            var strengthHeavy = Abilities(0f);
            strengthHeavy.strength = 100f;
            strengthHeavy.coordination = 100f;
            strengthHeavy.endurance = 100f;

            Assert.AreEqual(100f, SpecialtyDerivation.BasicNorm(strengthHeavy, SpecialtyType.Prowess), 0.001f);
            Assert.Less(SpecialtyDerivation.BasicNorm(strengthHeavy, SpecialtyType.Scholarship), 100f);
        }
    }
}
