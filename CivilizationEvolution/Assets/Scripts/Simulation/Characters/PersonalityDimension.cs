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
    public enum PersonalityDimension
    {
        Boldness,       // 大胆（怯懦↔勇猛）
        Compassion,     // 悲悯（冷酷↔慈悲）
        Greed,          // 贪婪（慷慨↔贪婪）
        Honor,          // 荣誉（狡诈↔诚实/重诺）
        Rationality,    // 理性（冲动/狂热↔冷静理性）
        Vengefulness,   // 报复（宽恕↔睚眦必报）
        Piety           // 虔信（无神/愤世↔虔诚信奉）
    }
}
