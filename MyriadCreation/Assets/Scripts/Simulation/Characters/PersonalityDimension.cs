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
    public enum PersonalityDimension
    {
        Boldness,       // 大胆（怯懦↔勇猛）
        Sociability,    // 社交性（孤僻↔合群）
        Compassion,     // 悲悯（冷酷↔慈悲）
        Greed,          // 贪婪（慷慨↔贪婪）
        Honor,          // 荣誉（狡诈↔诚实/重诺）
        Rationality,    // 理性（冲动/狂热↔冷静理性）
        Vengefulness,   // 报复（宽恕↔睚眦必报）
        Piety           // 虔信（无神/愤世↔虔诚信奉）
    }
}
