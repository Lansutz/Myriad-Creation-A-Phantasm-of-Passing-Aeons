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








namespace MyriadCreation.Simulation.Characters
{
    public class EconomicalArchetypeInfo
    {
        public EconomicalArchetype archetype;
        public string displayName;
        public string description;
 /// <summary>行为偏置：战争/建设/宗教/阴谋/外交 五维倾向（-1~1），供 AI 决策读取</summary>
        public float warBias, buildBias, faithBias, schemeBias, diplomacyBias;

        public EconomicalArchetypeInfo(EconomicalArchetype archetype, string displayName, string description,
            float war, float build, float faith, float scheme, float diplomacy)
        {
            this.archetype = archetype; this.displayName = displayName; this.description = description;
            warBias = war; buildBias = build; faithBias = faith; schemeBias = scheme; diplomacyBias = diplomacy;
        }
    }
}
