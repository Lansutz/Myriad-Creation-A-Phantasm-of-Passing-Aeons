using System;

namespace CivilizationEvolution.Simulation.Society
{
    [Serializable]
    public class LocusFrequency
    {
        public DnaLocus locus;
        [UnityEngine.Range(0f, 1f)] public float dominantFrequency = 0.5f;
    }
}
