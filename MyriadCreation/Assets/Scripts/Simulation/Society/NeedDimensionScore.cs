using System;

namespace CivilizationEvolution.Simulation.Society
{
    [Serializable]
    public struct NeedDimensionScore
    {
        public ClassNeedDimension dimension;
        [UnityEngine.Range(0f, 100f)] public float score;   // 满足度 0~100
        public string reason;                   // 低于阈值时的主因（UI/AI 用，如"饥荒"、"商税过重"）
    }
}
