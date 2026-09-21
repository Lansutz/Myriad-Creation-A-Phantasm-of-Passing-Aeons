using System.Collections.Generic;
using UnityEngine;
using CivilizationEvolution.Core;
using CivilizationEvolution.Core.Constants;
using CivilizationEvolution.Core.Data;
using CivilizationEvolution.Core.Dto;
using CivilizationEvolution.Core.Enums;
using CivilizationEvolution.Simulation.Diplomacy;
using CivilizationEvolution.Simulation.Economy;
using CivilizationEvolution.Simulation.Events;
using CivilizationEvolution.Simulation.Generation;
using CivilizationEvolution.Simulation.Modding;
using CivilizationEvolution.Simulation.Politics;
using CivilizationEvolution.Simulation.Settlement;
using CivilizationEvolution.Simulation.Society;
using CivilizationEvolution.Simulation.WorldState;
using CivilizationEvolution.World.Generation;
using CivilizationEvolution.World.Hydrology;
using CivilizationEvolution.World.Settlement;
using CivilizationEvolution.World.Terrain;





namespace CivilizationEvolution.Simulation.Warfare
{
    public class WarState
    {
        public int warId;
        public int attackerId;
        public int defenderId;
        public float attackerScore;
        public float defenderScore;
        public int startDay;
        public int lastBattleDay = -1;
        public bool ended;
        public int winnerId = -1;      // -1=未决
        public string outcome = "";    // victory/white_peace/invalidated

        public WarState(int warId, int attackerId, int defenderId, int startDay)
        {
            this.warId = warId;
            this.attackerId = attackerId;
            this.defenderId = defenderId;
            this.startDay = startDay;
        }

 /// <summary>某方当前分数</summary>
        public float GetScore(int realmId) => realmId == attackerId ? attackerScore : defenderScore;

 /// <summary>己方加成（胜方得分按 WarRules.scoreBattle×规模系数）</summary>
        public void AddScore(int realmId, float amount)
        {
            if (realmId == attackerId) attackerScore += amount;
            else if (realmId == defenderId) defenderScore += amount;
        }
    }
}
