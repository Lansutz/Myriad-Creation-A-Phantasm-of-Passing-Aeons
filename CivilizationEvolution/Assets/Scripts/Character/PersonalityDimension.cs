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
