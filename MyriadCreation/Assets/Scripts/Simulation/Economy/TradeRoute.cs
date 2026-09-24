using System.Collections.Generic;
using UnityEngine;
using MyriadCreation.Core;
using MyriadCreation.Core.Constants;
using MyriadCreation.Core.Data;
using MyriadCreation.Core.Dto;
using MyriadCreation.Core.Enums;
using MyriadCreation.Simulation.Events;
using MyriadCreation.Simulation.Generation;
using MyriadCreation.Simulation.Modding;
using MyriadCreation.Simulation.Politics;
using MyriadCreation.Simulation.Society;
using MyriadCreation.Simulation.WorldState;



namespace MyriadCreation.Simulation.Economy
{
    public class TradeRoute
    {
        public int fromRegionId;
        public int toRegionId;
        public List<int> nodeTileIndices = new List<int>();
        public float baseEfficiency = 1.0f;
        public float currentEfficiency = 1.0f;
        public bool isBlocked = false;

 /// <summary>计算贸易效率修正</summary>
        public void CalculateEfficiency(TileData[] tiles)
        {
            if (isBlocked || nodeTileIndices.Count == 0) { currentEfficiency = 0f; return; }

            float efficiency = baseEfficiency;

 // 道路等级
            float avgRoad = 0f;
            foreach (int idx in nodeTileIndices)
                avgRoad += (int)tiles[idx].roadLevel;
            avgRoad /= nodeTileIndices.Count;
            efficiency *= 0.6f + avgRoad * 0.15f;

 // 治安
            float avgStability = 0f;
            foreach (int idx in nodeTileIndices)
                avgStability += tiles[idx].stability;
            avgStability /= nodeTileIndices.Count;
            efficiency *= 0.5f + avgStability / 200f;

 // 距离衰减
            efficiency *= Mathf.Exp(-nodeTileIndices.Count * 0.02f);

            currentEfficiency = Mathf.Clamp(efficiency, 0f, 1.5f);
        }
    }
}
