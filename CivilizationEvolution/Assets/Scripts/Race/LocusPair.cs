using System;

namespace CivilizationEvolution.Race
{
    [Serializable]
    public struct LocusPair
    {
        public Allele paternal;   // 来自父亲的等位基因
        public Allele maternal;   // 来自母亲的等位基因

        public LocusPair(Allele p, Allele m) { paternal = p; maternal = m; }

        /// <summary>AA 纯合显性</summary>
        public bool IsHomozygousDominant => paternal == Allele.Dominant && maternal == Allele.Dominant;
        /// <summary>Aa 杂合</summary>
        public bool IsHeterozygous => paternal != maternal;
        /// <summary>aa 纯合隐性</summary>
        public bool IsHomozygousRecessive => paternal == Allele.Recessive && maternal == Allele.Recessive;
    }
}
