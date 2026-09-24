using System;
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
using MyriadCreation.Simulation.Modding;
using MyriadCreation.Simulation.Politics;
using MyriadCreation.Simulation.Population;
using MyriadCreation.Simulation.Society;
using MyriadCreation.Simulation.WorldState;




namespace MyriadCreation.Simulation.Religion
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
