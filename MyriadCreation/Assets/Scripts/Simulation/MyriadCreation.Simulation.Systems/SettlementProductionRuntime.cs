using System.Collections.Generic;
using MyriadCreation.Content.Definitions;
using MyriadCreation.Content.Modding;
using MyriadCreation.Core.Data;
using MyriadCreation.Simulation.State;
using MyriadCreation.Simulation.WorldState;
using MyriadCreation.World.Settlement;

namespace MyriadCreation.Simulation.Systems
{
    /// <summary>
    /// 聚落配方日结：已有市场的聚落按配方表尝试一次生产。
    /// 库存唯一写入口仍是 SettlementMarket；本类只决定“今天能尝试哪些配方”。
    /// </summary>
    public sealed class SettlementProductionRuntime
    {
        private readonly Dictionary<int, SettlementData> _settlements;
        private readonly TileData[] _tiles;
        private readonly GameWorld _world;
        private readonly List<GoodsRecipe> _recipes = new List<GoodsRecipe>();

        public SettlementProductionRuntime(Dictionary<int, SettlementData> settlements, TileData[] tiles, GameWorld world)
        {
            _settlements = settlements;
            _tiles = tiles;
            _world = world;
            foreach (GoodsRecipe recipe in RecipeCatalog.GetAll())
            {
                if (recipe != null && recipe.outputKind == RecipeOutputKind.Good)
                    _recipes.Add(recipe);
            }
        }

        public void DailyTick()
        {
            if (_settlements == null || _tiles == null || _world == null || _recipes.Count == 0) return;

            foreach (SettlementData settlement in _settlements.Values)
            {
                if (settlement == null || (!settlement.hasMarket && settlement.market == null)) continue;
                int tileIndex = settlement.tileIndex;
                if (tileIndex < 0 || tileIndex >= _tiles.Length) continue;
                int realmId = _tiles[tileIndex].ownerRealmId;
                if (realmId < 0) continue;

                SettlementMarket market = settlement.market ?? settlement.GetOrCreateMarket();
                if (market == null || market.capacity <= 0f) continue;

                for (int i = 0; i < _recipes.Count; i++)
                {
                    SettlementProduction.TryCraft(market, _recipes[i].outputGoodsId, out _, _world.Innovations, realmId, null);
                }
            }
        }
    }
}
