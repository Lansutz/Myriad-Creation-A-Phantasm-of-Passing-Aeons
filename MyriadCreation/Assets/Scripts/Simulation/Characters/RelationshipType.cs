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
    public enum RelationshipType
    {
        Stranger,   // 无结构关系
        Spouse,     // 配偶（婚姻）
        Parent,     // 父母（血缘）
        Child,      // 子女（血缘）
        Sibling,    // 兄弟姐妹（血缘）
        Mentor,     // 师（师承身份；机制化纽带见 BondType.MentorBond）
        Student,    // 徒
        Liege,      // 封君/上级（任职契约）
        Vassal      // 封臣/下级
    }
}
