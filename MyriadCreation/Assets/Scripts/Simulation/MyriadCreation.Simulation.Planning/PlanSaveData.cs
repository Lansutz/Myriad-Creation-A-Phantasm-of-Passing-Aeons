using System;
using System.Collections.Generic;

namespace MyriadCreation.Simulation.Planning
{
    /// <summary>计划运行态存档 DTO；只放字段，映射留在 PlanSystem。</summary>
    [Serializable]
    public class PlanSaveData
    {
        public int planId;
        public int type;
        public int state;
        public int phase;

        public int initiatorId = -1;
        public int ownerId = -1;
        public int targetId = -1;
        public string targetKind = string.Empty;
        public string purpose = string.Empty;
        public string title = string.Empty;
        public string description = string.Empty;

        public float progress;
        public float elapsedDays;
        public float estimatedDays;
        public int createdDay;
        public int lastStateChangeDay;

        public List<PlanParticipant> participants = new List<PlanParticipant>();
        public List<PlanRequirement> requirements = new List<PlanRequirement>();

        public string currentActivity = string.Empty;
        public string activityId = string.Empty;
        public string activityDefinitionId = string.Empty;
        public string activityStateCode = string.Empty;

        public string waitConditionId = string.Empty;
        public string waitConditionDescription = string.Empty;
        public bool waitConditionPollOnSchedule = true;
        public bool isWaiting;

        public string resultCode = string.Empty;
        public string resultSummary = string.Empty;
    }
}
