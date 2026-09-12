using System.Collections.Generic;

namespace CivilizationEvolution.Simulation.Religion
{
    [System.Serializable]
    public struct Deity
    {
        public string name;
        public string domain;      // 神职领域
        public float importance;   // 重要性 0~1
        public string symbol;
        public List<string> festivals;
    }
}
