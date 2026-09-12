using System.Collections.Generic;
using UnityEngine;
using CivilizationEvolution.Core;
using CivilizationEvolution.Economy;
using CivilizationEvolution.Politics;
using CivilizationEvolution.Race;
using CivilizationEvolution.Tech;
using CivilizationEvolution.Thought;

namespace CivilizationEvolution.Character
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
