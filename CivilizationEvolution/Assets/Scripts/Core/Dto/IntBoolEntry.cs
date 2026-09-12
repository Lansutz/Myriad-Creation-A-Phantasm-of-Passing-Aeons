using System;

namespace CivilizationEvolution.Core
{
    [Serializable]
    public class IntBoolEntry
    {
        public int key;
        public bool value;
        public IntBoolEntry() { }
        public IntBoolEntry(int k, bool v) { key = k; value = v; }
    }
}
