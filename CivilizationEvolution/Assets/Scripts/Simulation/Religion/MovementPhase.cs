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

    public enum MovementPhase
    {
        Emerging,     // 萌芽
        Growing,      // 成长
        Peak,         // 高潮
        Declining,    // 衰退
        Extinct       // 消亡
    }
}
