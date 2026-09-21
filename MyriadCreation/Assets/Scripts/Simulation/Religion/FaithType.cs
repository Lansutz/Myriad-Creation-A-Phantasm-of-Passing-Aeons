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

    public enum FaithType
    {
        Animistic,       // 泛灵论
        Polytheistic,    // 多神教
        Henotheistic,    // 单一主神教
        Monotheistic,    // 一神教
        Pantheistic,     // 泛神论
        Atheistic,       // 无神论
        Cosmic           // 宇宙论宗教
    }
}
