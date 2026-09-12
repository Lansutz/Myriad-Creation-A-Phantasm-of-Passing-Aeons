using System;
using System.Collections.Generic;

namespace CivilizationEvolution.Simulation.Society
{
    [Serializable]
    public class DnaData
    {
        [System.NonSerialized]
        public Dictionary<DnaLocus, LocusPair> loci = new Dictionary<DnaLocus, LocusPair>();
        public int mutationCount;              // 该个体 DNA 发生突变的基因座数量
        public float inbreedingCoefficient;    // 近亲系数 0~1

        public LocusPair GetLocus(DnaLocus locus)
        {
            return loci.TryGetValue(locus, out var pair) ? pair : new LocusPair(Allele.Dominant, Allele.Dominant);
        }

        public void SetLocus(DnaLocus locus, LocusPair pair) => loci[locus] = pair;
    }
}
