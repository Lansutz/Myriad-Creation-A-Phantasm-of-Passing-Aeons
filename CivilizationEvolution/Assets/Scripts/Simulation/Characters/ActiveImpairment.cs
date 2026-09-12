namespace CivilizationEvolution.Simulation.Characters
{
    /// <summary>
    /// 活跃的衰退状态实例
    /// 记录衰退类型ID、当前阶段索引、进展速度、导致原因
    /// </summary>
    [System.Serializable]
    public struct ActiveImpairment
    {
        public string impairmentId;        // 衰退类型ID（如"vision"、"hearing"）
        public int stageIndex;             // 当前阶段索引（0=轻度，1=中度，2=重度，3=极重度）
        public float progressionRate;      // 每日进展概率（0-1）
        public int daysAtCurrentStage;     // 在当前阶段的天数
        public string cause;               // 导致原因（diseaseId或"aging"或"injury"）
        public bool isReversible;          // 是否可逆（某些早期衰退可以恢复）
    }
}
