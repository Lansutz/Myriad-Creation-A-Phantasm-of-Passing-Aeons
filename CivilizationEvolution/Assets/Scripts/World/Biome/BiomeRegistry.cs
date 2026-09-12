using System.Collections.Generic;

namespace CivilizationEvolution.Climate
{
    /// <summary>
    /// 群系配置覆盖字典：World 层持有硬编码默认值，Simulation 层的 ContentRegistry
    /// 从 JSON 加载后写入此字典，World 层查询时优先读覆盖值。避免 World 反向依赖 Simulation。
    /// </summary>
    public static class BiomeRegistry
    {
        public static readonly Dictionary<int, BiomeDef> Overrides = new Dictionary<int, BiomeDef>();

        public static bool TryGet(int id, out BiomeDef def)
            => Overrides.TryGetValue(id, out def);
    }
}
