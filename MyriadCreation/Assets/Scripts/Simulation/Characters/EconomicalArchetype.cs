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
    public enum EconomicalArchetype
    {
        Balanced,           // 平衡型：无明显倾向（默认）
        Warlike,            // 好战者：boldness>0 + greed>=0 + 好战特质/高阈值
        Cautious,           // 谨慎者：boldness<=0 + 偏执/怯懦/低大胆+耐心
        EconomicalBoom,     // 经济繁荣者（建设者）：大胆>0 + 勤勉特质，非好战
        PiousBuilder,       // 虔诚建设者：zeal>0 + 虔诚/勤勉特质，非好战
        Conqueror,          // 征服者：特殊（征服特质），优先级最高
        Unpredictable,      // 不可预测者：轻浮/疯狂
        Administrator,      // 行政官僚（扩展）：理性高 + 荣誉高 + 贪婪低
        Schemer,            // 阴谋家（扩展）：狡诈高 + 报复高 + 理性中
        CulturalPatron,     // 文化赞助人（扩展）：慈悲高 + 荣誉高 + 虔诚中
        GodlessReformer     // 不敬神的改革者（扩展）：虔诚强负 + 理性高 + 大胆中
    }
}
