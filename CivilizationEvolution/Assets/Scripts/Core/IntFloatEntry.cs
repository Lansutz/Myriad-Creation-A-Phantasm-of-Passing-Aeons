using System;

namespace CivilizationEvolution.Core
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
