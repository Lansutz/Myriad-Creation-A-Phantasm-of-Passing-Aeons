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
