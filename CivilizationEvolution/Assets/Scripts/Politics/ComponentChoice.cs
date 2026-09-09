using System.Collections.Generic;
using System;

namespace CivilizationEvolution.Politics
{
    [Serializable]
    public class ComponentChoice
    {
 /// <summary>主导成分（●，该维度枚举的 int 值）</summary>        public int primary;
 /// <summary>次要成分（○，0~2 个，其余选项）</summary>        public List<int> secondary = new List<int>();

        public ComponentChoice() { }

        public ComponentChoice(int primary, params int[] secondary)
        {
            this.primary = primary;
            if (secondary != null)
                this.secondary.AddRange(secondary);
        }

 /// <summary>是否包含某成分（主导或次要）</summary>        public bool Contains(int component)
        {
            if (primary == component) return true;
            return secondary.Contains(component);
        }
    }
}
