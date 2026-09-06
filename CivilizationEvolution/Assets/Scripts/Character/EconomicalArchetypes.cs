using System.Collections.Generic;
using UnityEngine;
using CivilizationEvolution.Core;
using CivilizationEvolution.Economy;
using CivilizationEvolution.Politics;
using CivilizationEvolution.Race;
using CivilizationEvolution.Tech;
using CivilizationEvolution.Thought;

namespace CivilizationEvolution.Role
{
    public static class EconomicalArchetypes
    {
        public static readonly EconomicalArchetypeInfo[] All =
        {
            new EconomicalArchetypeInfo(EconomicalArchetype.Balanced, "均衡之主", "无明显战略倾向，行事中庸", 0f, 0f, 0f, 0f, 0f),
            new EconomicalArchetypeInfo(EconomicalArchetype.Warlike, "好战之君", "崇尚武力，以战争扩张为第一要务", 0.8f, -0.2f, 0.1f, 0.1f, -0.2f),
            new EconomicalArchetypeInfo(EconomicalArchetype.Cautious, "谨慎之主", "谋定后动，偏好防御与巩固", -0.3f, 0.3f, 0.1f, 0.1f, 0.2f),
            new EconomicalArchetypeInfo(EconomicalArchetype.EconomicalBoom, "繁荣缔造者", "专注经济建设与领地开发", -0.2f, 0.8f, 0f, 0f, 0.2f),
            new EconomicalArchetypeInfo(EconomicalArchetype.PiousBuilder, "虔信营建者", "以信仰为名兴建宗教建筑与转化", -0.1f, 0.4f, 0.8f, -0.1f, 0.1f),
            new EconomicalArchetypeInfo(EconomicalArchetype.Conqueror, "征服者", "为征服而生，永不满足于现有疆土", 1.0f, -0.3f, 0.2f, 0.2f, -0.3f),
            new EconomicalArchetypeInfo(EconomicalArchetype.Unpredictable, "不可预测者", "行事乖张，令人难以捉摸", 0.3f, 0f, 0f, 0.3f, -0.2f),
            new EconomicalArchetypeInfo(EconomicalArchetype.Administrator, "行政巨匠", "崇尚法治与行政效率，精于吏治", -0.2f, 0.4f, 0.1f, 0f, 0.3f),
            new EconomicalArchetypeInfo(EconomicalArchetype.Schemer, "阴谋大师", "在阴影中操纵一切，偏好暗杀与勒索", 0.1f, 0f, 0f, 0.9f, -0.1f),
            new EconomicalArchetypeInfo(EconomicalArchetype.CulturalPatron, "文化赞助人", "推崇文化艺术，以软实力教化四方", -0.2f, 0.5f, 0.2f, 0f, 0.4f),
            new EconomicalArchetypeInfo(EconomicalArchetype.GodlessReformer, "不敬神的改革者", "漠视宗教权威，推动世俗化与制度改革", 0.1f, 0.3f, -0.8f, 0.2f, 0.1f),
        };

        public static EconomicalArchetypeInfo Get(EconomicalArchetype archetype)
        {
            foreach (var a in All)
                if (a.archetype == archetype) return a;
            return All[0];
        }
    }
}
