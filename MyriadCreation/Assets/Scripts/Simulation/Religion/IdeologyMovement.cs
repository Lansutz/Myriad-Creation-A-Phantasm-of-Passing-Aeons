using System;
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
using CivilizationEvolution.Simulation.Modding;
using CivilizationEvolution.Simulation.Politics;
using CivilizationEvolution.Simulation.Population;
using CivilizationEvolution.Simulation.Society;
using CivilizationEvolution.Simulation.WorldState;




namespace CivilizationEvolution.Simulation.Religion
{
    public class IdeologyMovement
    {
        public int movementId;
        public string movementName;
        public string description;
        public int originRegionId;
        public int startYear;
        public int peakYear = -1;
        public int endYear = -1;

        public MovementPhase phase = MovementPhase.Emerging;

 // 核心主张
        public List<string> coreTenets = new List<string>();
        [NonSerialized] public Dictionary<string, float> policyPositions = new Dictionary<string, float>();


 // 传播
        public float momentum = 0f;       // 势头 0~100
        public float radicalism = 0.5f;   // 激进程度
        public float appeal = 0.5f;        // 吸引力

 // 参与者
        public List<int> supporterCharacterIds = new List<int>();
        [NonSerialized] public Dictionary<int, float> regionSupport = new Dictionary<int, float>();


 // 关联学派/信仰
        public List<int> associatedSchoolIds = new List<int>();
        public List<int> associatedFaithIds = new List<int>();

 /// <summary>每日思潮Tick</summary>
        public void DailyTick(int currentYear)
        {
 // 思潮生命周期
            int age = currentYear - startYear;

            if (phase == MovementPhase.Emerging && age > 5)
                phase = MovementPhase.Growing;
            if (phase == MovementPhase.Growing && momentum > 70f)
            {
                phase = MovementPhase.Peak;
                peakYear = currentYear;
            }
            if (phase == MovementPhase.Peak && age > 20)
                phase = MovementPhase.Declining;
            if (phase == MovementPhase.Declining && momentum < 10f)
            {
                phase = MovementPhase.Extinct;
                endYear = currentYear;
            }

 // 势头变化
            float momentumChange = phase switch
            {
                MovementPhase.Emerging => 0.5f,
                MovementPhase.Growing => 2f,
                MovementPhase.Peak => 0f,
                MovementPhase.Declining => -1.5f,
                MovementPhase.Extinct => -0.5f,
                _ => 0f
            };
            momentum = Mathf.Clamp(momentum + momentumChange + UnityEngine.Random.Range(-1f, 1f), 0f, 100f);
        }
    }
}
