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








namespace MyriadCreation.Simulation.Definitions
{
    public enum BondType
    {
        BloodBond,        // 血脉羁绊（跨代血缘的机制化联结）
        SwornBrotherhood, // 结义兄弟
        MentorBond,       // 师徒羁绊（师承的机制化纽带，对应 RelationshipType.Mentor/Student）
        Rivalry,          // 宿怨（敌对纽带·轻度）
        Romance,          // 爱情羁绊
        ComradesInArms,   // 战友羁绊
        OathBond,         // 誓言羁绊
        Nemesis           // 死敌（敌对纽带·重度）
    }
}
