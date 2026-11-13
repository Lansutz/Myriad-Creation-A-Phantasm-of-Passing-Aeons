using System;
using System.Collections.Generic;
using MyriadCreation.Simulation.Laws;

namespace MyriadCreation.Simulation.LawHost
{
    /// <summary>司法系统运行态；realmId 是 GameWorld._judicialSystems 的键。</summary>
    [Serializable]
    public class JudicialSystemSaveData
    {
        public int realmId;
        public int judicialSystemId;
        public string judicialSystemName = string.Empty;
        public int source;
        public List<Law> laws = new List<Law>();
        public float judicialEfficiency;
        public float corruption;
        public float lawEnforcement;
        public float severity;
        public bool useCapitalPunishment;
        public bool useCorporalPunishment;
        public bool useFines;
        public bool useImprisonment;
        public bool useExile;
        public bool useSlavery;
    }
}
