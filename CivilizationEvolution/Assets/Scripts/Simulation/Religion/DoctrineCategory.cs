using System;
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
using CivilizationEvolution.Simulation.Modding;
using CivilizationEvolution.Simulation.Politics;
using CivilizationEvolution.Simulation.Population;
using CivilizationEvolution.Simulation.Society;
using CivilizationEvolution.Simulation.WorldState;




namespace CivilizationEvolution.Simulation.Religion
{

    public enum DoctrineCategory
    {
        Cosmology,       // 宇宙论
        Soteriology,     // 救赎论
        Ethics,          // 伦理学
        Ritual,          // 仪式
        Ecclesiology,    // 教会论
        Eschatology,     // 末世论
        Political        // 政治神学
    }
}
