using System;
using System.Collections.Generic;

namespace MyriadCreation.Simulation.Systems
{
    [Serializable]
    public class InnovationMasteryEntry
    {
        public int characterId;
        public int innovationId;
        public int level;
    }

    [Serializable]
    public class InnovationPracticeEntry
    {
        public string key = string.Empty;
        public float amount;
    }

    /// <summary>角色级革新掌握/练习量；按角色与革新排序，保证存档稳定。</summary>
    [Serializable]
    public class InnovationKnowledgeSaveData
    {
        public List<InnovationMasteryEntry> masteries = new List<InnovationMasteryEntry>();
        public List<InnovationPracticeEntry> practices = new List<InnovationPracticeEntry>();
    }
}
