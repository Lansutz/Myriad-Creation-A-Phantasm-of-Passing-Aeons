using System.Collections.Generic;
using UnityEngine;
using CivilizationEvolution.Core;
using CivilizationEvolution.Core.Constants;
using CivilizationEvolution.Core.Data;
using CivilizationEvolution.Core.Dto;
using CivilizationEvolution.Core.Enums;
using CivilizationEvolution.Simulation.Economy;
using CivilizationEvolution.Simulation.Events;
using CivilizationEvolution.Simulation.Generation;
using CivilizationEvolution.Simulation.Innovation;
using CivilizationEvolution.Simulation.Modding;
using CivilizationEvolution.Simulation.Politics;
using CivilizationEvolution.Simulation.Population;
using CivilizationEvolution.Simulation.Religion;
using CivilizationEvolution.Simulation.Society;
using CivilizationEvolution.Simulation.WorldState;








namespace CivilizationEvolution.Simulation.Characters
{
    public class CharacterBond
    {
        public int bondId;
        public int characterAId;
        public int characterBId;
        public BondType type;
        public int establishedDay;
        public float strength = 50f; // 羁绊强度 0~100

 // 羁绊效果
        public float combatBonusWhenTogether = 0f;
        public float diplomacyBonus = 0f;
        public float stressReduction = 0f;
        public bool sharedFate = false;

 /// <summary>每日羁绊Tick</summary>
        public void DailyTick()
        {
 // 羁绊强度自然变化
            strength = Mathf.Clamp(strength + UnityEngine.Random.Range(-1f, 1f), 0f, 100f);
        }
    }
}
