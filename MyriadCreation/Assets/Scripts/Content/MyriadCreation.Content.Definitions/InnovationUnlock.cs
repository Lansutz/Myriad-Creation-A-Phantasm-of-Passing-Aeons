using System;

namespace MyriadCreation.Content.Definitions
{
    public enum InnovationUnlockKind
    {
        Unknown = 0,
        Good = 1,
        Unit = 2,
        Building = 3,
        Equipment = 4,
        Law = 5,
        CharacterClass = 6,
        CultureTrait = 7,
        Religion = 8,
        PolityForm = 9,
        Other = 10,
        Recipe = 11,
    }

    /// <summary>革新解锁的具体内容引用；革新是能力，具体物品/单位/建筑仍是各自内容。</summary>
    [Serializable]
    public class InnovationUnlock
    {
        public InnovationUnlockKind kind;
        public int id;
        public string key = string.Empty;

        public static InnovationUnlock Of(InnovationUnlockKind kind, int id)
        {
            return new InnovationUnlock { kind = kind, id = id };
        }

        public static InnovationUnlock Keyed(InnovationUnlockKind kind, string key)
        {
            return new InnovationUnlock { kind = kind, key = key };
        }
    }
}
