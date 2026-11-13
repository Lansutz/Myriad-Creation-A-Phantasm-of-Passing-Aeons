using System;

namespace MyriadCreation.Simulation.AI
{
    /// <summary>AI 长期决策状态；当前日临时意图队列不入档。</summary>
    [Serializable]
    public class AIControllerSaveData
    {
        public int realmId;
        public AIPersonality personality;
        public float decisionTimer;
        public int currentGoal;
        public int targetRealmId = -1;
        public int targetTileIndex = -1;
        public int raidCooldown;
    }
}
