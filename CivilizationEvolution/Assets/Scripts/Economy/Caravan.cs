using System.Collections.Generic;
using UnityEngine;
using CivilizationEvolution.Core;

namespace CivilizationEvolution.Economy
{
    public class Caravan
    {
        public int caravanId;
        public int fromRegionId;
        public int toRegionId;
        public int currentNodeIndex;
        [System.NonSerialized]
        public Dictionary<int, float> cargo = new Dictionary<int, float>();
        public float capacity = 500f;
        public float speed = 1f;
        public bool isMoving = true;
        public float moveProgress = 0f;

        public float GetCargoWeight(Dictionary<int, GoodsDef> goodsDefs)
        {
            float weight = 0f;
            foreach (var kv in cargo)
            {
                if (goodsDefs.TryGetValue(kv.Key, out var def))
                    weight += kv.Value * def.weight;
            }
            return weight;
        }

 /// <summary>商队移动Tick</summary>        public bool MoveTick(TradeRoute route, TileData[] tiles)
        {
            if (!isMoving || route.isBlocked) return false;

            moveProgress += speed * route.currentEfficiency;
            if (moveProgress >= 1f)
            {
                moveProgress = 0f;
                currentNodeIndex++;
                if (currentNodeIndex >= route.nodeTileIndices.Count)
                {
                    isMoving = false;
                    return true; // 到达目的地
                }
            }
            return false;
        }
    }
}
