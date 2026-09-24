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

    public enum MovementPhase
    {
        Emerging,     // 萌芽
        Growing,      // 成长
        Peak,         // 高潮
        Declining,    // 衰退
        Extinct       // 消亡
    }
}
