using System.Collections.Generic;

namespace Myriad.MapEditor
{
    public struct HeightChange
    {
        public int Index;
        public float Before;
        public float After;

        public HeightChange(int index, float before, float after)
        {
            Index = index;
            Before = before;
            After = after;
        }
    }

    /// <summary>A complete, reversible edit. Derived values are intentionally not stored.</summary>
    public sealed class MapOperation
    {
        public readonly string Tool;
        public readonly IReadOnlyList<HeightChange> HeightChanges;
        public readonly float BeforeSeaLevel;
        public readonly float AfterSeaLevel;

        public MapOperation(string tool, List<HeightChange> heightChanges, float beforeSeaLevel, float afterSeaLevel)
        {
            Tool = tool;
            HeightChanges = heightChanges;
            BeforeSeaLevel = beforeSeaLevel;
            AfterSeaLevel = afterSeaLevel;
        }
    }
}
