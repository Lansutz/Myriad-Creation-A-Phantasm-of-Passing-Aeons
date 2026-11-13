using System;

namespace MyriadCreation.Save.Contracts
{
    /// <summary>字符串→浮点字典项（JsonUtility 无字典，用 List 包装）。</summary>
    [Serializable]
    public struct FloatEntry
    {
        public string key;
        public float value;

        public FloatEntry(string key, float value)
        {
            this.key = key;
            this.value = value;
        }
    }

    /// <summary>整数→整数条目（官职 holder：officeId → characterId）。</summary>
    [Serializable]
    public struct IntPairEntry
    {
        public int key;
        public int value;

        public IntPairEntry(int key, int value)
        {
            this.key = key;
            this.value = value;
        }
    }

    /// <summary>角色知识（谁被哪个政权"知道"）存档项；CharacterKnowledgeRegistry 为静态表，需显式重建。</summary>
    [Serializable]
    public class KnownCharacterSaveData
    {
        public int observerRealmId;
        public int characterId;
        public int characterRealmId = -1;
        public int knownSettlementAnchorId = -1;
        public int lastSeenDay;
        public string source = "";
    }
}
