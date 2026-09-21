using System;

namespace CivilizationEvolution.Simulation.Politics
{
    [Serializable]
    public class ActiveJuncture
    {
        public CriticalJunctureType type;
        public int startDay;
        public int remainingDays;      // 窗口剩余（窗口短暂）
        [UnityEngine.Range(0f, 100f)] public float severity;
        public bool resolved;
        public JunctureOutcomeType outcome = JunctureOutcomeType.None;
        public string note = "";
    }
}
