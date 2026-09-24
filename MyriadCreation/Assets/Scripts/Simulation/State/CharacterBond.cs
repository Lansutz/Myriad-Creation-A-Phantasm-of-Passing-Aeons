using System.Collections.Generic;
using UnityEngine;
using MyriadCreation.Core;
using MyriadCreation.Core.Constants;
using MyriadCreation.Core.Data;
using MyriadCreation.Core.Dto;
using MyriadCreation.Core.Enums;
using MyriadCreation.Simulation.Economy;
using MyriadCreation.Simulation.Events;
using MyriadCreation.Simulation.Generation;
using MyriadCreation.Simulation.Innovation;
using MyriadCreation.Simulation.Modding;
using MyriadCreation.Simulation.Politics;
using MyriadCreation.Simulation.Population;
using MyriadCreation.Simulation.Religion;
using MyriadCreation.Simulation.Society;
using MyriadCreation.Simulation.WorldState;








namespace MyriadCreation.Simulation.State
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
