using System.Collections.Generic;
using MyriadCreation.Content.Definitions;
using MyriadCreation.Simulation.State;

namespace MyriadCreation.Simulation.Systems
{
    /// <summary>
    /// 专精节点解锁：技能值达到门槛且该专精有足够真实实践，才解锁对应操作/战术。
    /// 纯函数判断，调用方决定何时调用（日结/事件后）。
    /// </summary>
    public static class SpecialtyPerkSystem
    {
        /// <summary>尝试解锁该专精下所有达标节点；返回本次新解锁的 id。</summary>
        public static List<string> TryUnlock(CharacterData character, SpecialtyType specialty)
        {
            var unlocked = new List<string>();
            if (character == null) return unlocked;
            float skill = character.GetSpecialtySkill(specialty);
            float practice = character.GetSpecialtyExperience(specialty);
            foreach (SpecialtyPerkDef def in SpecialtyPerkCatalog.ForSpecialty(specialty))
            {
                if (def == null || character.HasPerk(def.perkId)) continue;
                if (skill < def.requiredSkill || practice < def.requiredPractice) continue;
                if (character.unlockedPerks == null) character.unlockedPerks = new List<string>();
                character.unlockedPerks.Add(def.perkId);
                unlocked.Add(def.perkId);
            }
            return unlocked;
        }

        /// <summary>全专精扫描（人物成长/读档后调用）。</summary>
        public static List<string> TryUnlockAll(CharacterData character)
        {
            var unlocked = new List<string>();
            if (character == null) return unlocked;
            foreach (SpecialtyType specialty in System.Enum.GetValues(typeof(SpecialtyType)))
            {
                unlocked.AddRange(TryUnlock(character, specialty));
            }
            return unlocked;
        }

        public static bool CanUseAction(CharacterData character, string perkId)
        {
            return character != null && character.HasPerk(perkId);
        }
    }
}
