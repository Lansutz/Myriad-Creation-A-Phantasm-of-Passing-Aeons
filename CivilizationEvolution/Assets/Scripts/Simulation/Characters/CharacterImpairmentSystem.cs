using System.Collections.Generic;

namespace CivilizationEvolution.Simulation.Characters
{
    /// <summary>
    /// 衰退系统——处理感官/能力的渐进性衰退
    /// 借鉴CK3的Clouded Eyes/Withering Mind/Fragile Bones思路
    /// 疾病、衰老、外伤都可以导致衰退，衰退到最高级后获得永久特质
    /// </summary>
    public class CharacterImpairmentSystem
    {
        /// <summary>
        /// 每日tick——检查衰退是否进展
        /// </summary>
        public void DailyTick(CharacterData character)
        {
            if (character.impairments == null) return;

            for (int i = character.impairments.Count - 1; i >= 0; i--)
            {
                var imp = character.impairments[i];
                imp.daysAtCurrentLevel++;

                // 检查是否进展到下一级
                if (imp.level < ImpairmentLevel.Profound && imp.progressionRate > 0)
                {
                    // 进展概率受等级影响——等级越高越难进展
                    float progressChance = imp.progressionRate / (int)imp.level;
                    if (UnityEngine.Random.value < progressChance)
                    {
                        imp.level++;
                        imp.daysAtCurrentLevel = 0;

                        // 达到最高级，获得永久特质
                        if (imp.level == ImpairmentLevel.Profound)
                        {
                            ApplyPermanentTrait(character, imp.type);
                        }
                    }
                }

                character.impairments[i] = imp;
            }
        }

        /// <summary>
        /// 添加或增加衰退等级
        /// </summary>
        public void AddImpairment(CharacterData character, ImpairmentType type, ImpairmentLevel level, float progressionRate, string cause, bool isReversible = false)
        {
            if (character.impairments == null)
                character.impairments = new List<ActiveImpairment>();

            // 检查是否已有该类型的衰退
            int existingIndex = character.impairments.FindIndex(imp => imp.type == type);
            if (existingIndex >= 0)
            {
                var existing = character.impairments[existingIndex];
                // 取较高等级
                if (level > existing.level)
                {
                    existing.level = level;
                    existing.daysAtCurrentLevel = 0;
                }
                // 取较快的进展速度
                if (progressionRate > existing.progressionRate)
                {
                    existing.progressionRate = progressionRate;
                }
                existing.cause = cause;
                character.impairments[existingIndex] = existing;
            }
            else
            {
                character.impairments.Add(new ActiveImpairment
                {
                    type = type,
                    level = level,
                    progressionRate = progressionRate,
                    daysAtCurrentLevel = 0,
                    cause = cause,
                    isReversible = isReversible
                });
            }

            // 如果直接添加到最高级，立即获得永久特质
            if (level >= ImpairmentLevel.Profound)
            {
                ApplyPermanentTrait(character, type);
            }
        }

        /// <summary>
        /// 达到最高级后获得永久特质
        /// </summary>
        private void ApplyPermanentTrait(CharacterData character, ImpairmentType type)
        {
            switch (type)
            {
                case ImpairmentType.Vision:
                    // 失明——通过疾病系统添加
                    CharacterDiseaseSystem.Instance?.InfectCharacter(character, "blindness", "impairment_progression");
                    break;
                case ImpairmentType.Hearing:
                    // 全聋
                    CharacterDiseaseSystem.Instance?.InfectCharacter(character, "deafness", "impairment_progression");
                    break;
                case ImpairmentType.Cognition:
                    // 失能/痴呆
                    CharacterDiseaseSystem.Instance?.InfectCharacter(character, "dementia", "impairment_progression");
                    break;
                case ImpairmentType.Mobility:
                    // 卧床/瘫痪
                    CharacterDiseaseSystem.Instance?.InfectCharacter(character, "paralysis", "impairment_progression");
                    break;
                case ImpairmentType.Speech:
                    // 失语
                    CharacterDiseaseSystem.Instance?.InfectCharacter(character, "aphasia", "impairment_progression");
                    break;
            }
        }

        /// <summary>
        /// 获取某类型的当前衰退等级
        /// </summary>
        public ImpairmentLevel GetImpairmentLevel(CharacterData character, ImpairmentType type)
        {
            if (character.impairments == null) return ImpairmentLevel.None;
            var imp = character.impairments.Find(i => i.type == type);
            return imp.type == type ? imp.level : ImpairmentLevel.None;
        }

        /// <summary>
        /// 获取衰退带来的属性修正
        /// </summary>
        public Dictionary<string, float> GetImpairmentMods(CharacterData character)
        {
            var mods = new Dictionary<string, float>();
            if (character.impairments == null) return mods;

            foreach (var imp in character.impairments)
            {
                int levelInt = (int)imp.level;
                if (levelInt == 0) continue;

                switch (imp.type)
                {
                    case ImpairmentType.Vision:
                        mods["prowess"] = mods.GetValueOrDefault("prowess") - levelInt * 2;
                        mods["scholarship"] = mods.GetValueOrDefault("scholarship") - levelInt;
                        mods["military"] = mods.GetValueOrDefault("military") - levelInt;
                        mods["charm"] = mods.GetValueOrDefault("charm") - levelInt * 0.5f;
                        break;
                    case ImpairmentType.Hearing:
                        mods["social"] = mods.GetValueOrDefault("social") - levelInt * 2;
                        mods["conspiracy"] = mods.GetValueOrDefault("conspiracy") - levelInt;
                        mods["management"] = mods.GetValueOrDefault("management") - levelInt * 0.5f;
                        break;
                    case ImpairmentType.Cognition:
                        mods["scholarship"] = mods.GetValueOrDefault("scholarship") - levelInt * 2;
                        mods["management"] = mods.GetValueOrDefault("management") - levelInt * 1.5f;
                        mods["conspiracy"] = mods.GetValueOrDefault("conspiracy") - levelInt;
                        mods["social"] = mods.GetValueOrDefault("social") - levelInt;
                        break;
                    case ImpairmentType.Mobility:
                        mods["prowess"] = mods.GetValueOrDefault("prowess") - levelInt * 3;
                        mods["military"] = mods.GetValueOrDefault("military") - levelInt;
                        mods["health"] = mods.GetValueOrDefault("health") - levelInt * 0.2f;
                        break;
                    case ImpairmentType.Speech:
                        mods["social"] = mods.GetValueOrDefault("social") - levelInt * 3;
                        mods["conspiracy"] = mods.GetValueOrDefault("conspiracy") - levelInt * 1.5f;
                        mods["management"] = mods.GetValueOrDefault("management") - levelInt;
                        break;
                }
            }

            return mods;
        }
    }
}
