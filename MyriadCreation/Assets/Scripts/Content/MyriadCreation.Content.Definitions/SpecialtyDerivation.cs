using System;

namespace MyriadCreation.Content.Definitions
{
    /// <summary>
    /// 专精派生：专精 = f(基本能力, 训练)。M3 目标公式：
    /// specialty = 100 × √(basicNorm × trainingNorm) + 修正
    /// 天赋高但无训练 → 近 0；训练足但天赋差 → 有上限。纯函数，不依赖 Unity。
    /// </summary>
    public static class SpecialtyDerivation
    {
        public static float BasicNorm(BasicAbilities abilities, SpecialtyType specialty)
        {
            if (abilities == null)
            {
                return 0f;
            }

            switch (specialty)
            {
                case SpecialtyType.Prowess:
                    return Weighted(abilities.strength, 0.4f, abilities.coordination, 0.4f, abilities.endurance, 0.2f);
                case SpecialtyType.Military:
                    return Weighted(abilities.logic, 0.4f, abilities.observation, 0.3f, abilities.endurance, 0.2f, abilities.intuition, 0.1f);
                case SpecialtyType.Management:
                    return Weighted(abilities.logic, 0.4f, abilities.memory, 0.3f, abilities.charisma, 0.2f, abilities.observation, 0.1f);
                case SpecialtyType.Social:
                    return Weighted(abilities.eloquence, 0.4f, abilities.charisma, 0.3f, abilities.insight, 0.2f, abilities.intuition, 0.1f);
                case SpecialtyType.Conspiracy:
                    return Weighted(abilities.insight, 0.4f, abilities.inspiration, 0.3f, abilities.observation, 0.2f, abilities.logic, 0.1f);
                case SpecialtyType.Scholarship:
                    return Weighted(abilities.memory, 0.4f, abilities.inspiration, 0.3f, abilities.logic, 0.2f, abilities.observation, 0.1f);
                default:
                    return 0f;
            }
        }

        /// <summary>专精派生值 0-100；trainingNorm 0-1。</summary>
        public static float Derive(SpecialtyType specialty, BasicAbilities abilities, float trainingNorm, float talentBonus = 0f)
        {
            float basic = Clamp01(BasicNorm(abilities, specialty) / 100f);
            float training = Clamp01(trainingNorm);
            float value = 100f * (float)Math.Sqrt(basic * training) + talentBonus;
            return Clamp(value, 0f, 100f);
        }

        private static float Weighted(float a1, float w1, float a2, float w2, float a3, float w3, float a4 = 0f, float w4 = 0f)
        {
            float sum = a1 * w1 + a2 * w2 + a3 * w3 + a4 * w4;
            float weight = w1 + w2 + w3 + w4;
            return weight <= 0f ? 0f : Clamp(sum / weight, 0f, 100f);
        }

        private static float Clamp(float value, float min, float max)
        {
            return value < min ? min : (value > max ? max : value);
        }

        private static float Clamp01(float value)
        {
            return Clamp(value, 0f, 1f);
        }
    }
}
