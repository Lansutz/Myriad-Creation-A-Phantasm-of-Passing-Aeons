using MyriadCreation.Core.Contracts;

namespace MyriadCreation.Simulation.WorldState
{
    /// <summary>
    /// <see cref="IMapStatsView"/> 的 Simulation 侧适配器：只读转调 GameWorld，不向 UI 泄漏 Simulation 类型。
    /// 由 App 在创建/切换世界时新建并注入 UIManager；null 世界返回 0，不抛。
    /// </summary>
    public sealed class WorldReadModelAdapter : IMapStatsView
    {
        private readonly GameWorld _world;

        public WorldReadModelAdapter(GameWorld world)
        {
            _world = world;
        }

        public int MapWidth => _world != null ? _world.mapWidth : 0;

        public int MapHeight => _world != null ? _world.mapHeight : 0;

        public int TotalTiles => _world != null && _world.tiles != null ? _world.tiles.Length : 0;

        public int LandTiles => _world != null ? _world.GetLandTileCount() : 0;

        public int SeaTiles => _world != null ? _world.GetSeaTileCount() : 0;

        public int ProvinceCount => _world != null && _world.provinces != null ? _world.provinces.Count : 0;

        public int SettlementCount => _world != null && _world.settlements != null ? _world.settlements.Count : 0;

        public int RealmCount => _world != null && _world.realms != null ? _world.realms.Count : 0;

        public int CurrentYear => _world != null ? _world.currentYear : 0;

        public int CurrentDay => _world != null ? _world.currentDay : 0;

        public int StartYear => _world != null ? _world.startYear : 0;

        public int StartDay => _world != null ? _world.startDay : 0;
    }
}
