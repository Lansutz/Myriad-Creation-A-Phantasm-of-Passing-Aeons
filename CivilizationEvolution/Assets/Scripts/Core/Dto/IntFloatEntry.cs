using System;

namespace CivilizationEvolution.Core.Dto
{
    [Serializable]
    public class IntFloatEntry
    {
        public int key;
        public float value;
        public IntFloatEntry() { }
        public IntFloatEntry(int k, float v) { key = k; value = v; }
    }
}
