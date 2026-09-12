namespace CivilizationEvolution.Simulation.Characters
{
    /// <summary>
    /// 活跃的衰退状态实例
    /// 记录衰退类型、当前等级、进展速度、导致原因
    /// </summary>
    [System.Serializable]
    public struct ActiveImpairment
    {
        public ImpairmentType type;           // 衰退类型
        public ImpairmentLevel level;         // 当前等级
        public float progressionRate;         // 每日进展概率（0-1）
        public int daysAtCurrentLevel;        // 在当前等级的天数
        public string cause;                  // 导致原因（diseaseId或"aging"或"injury"）
        public bool isReversible;             // 是否可逆（某些早期衰退可以恢复）
    }
}
