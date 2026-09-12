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
