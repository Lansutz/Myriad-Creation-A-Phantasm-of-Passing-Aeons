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




namespace MyriadCreation.Simulation.Definitions
{


    public enum FaithOrganizationType
    {
        Decentralized,   // 去中心化
        Congregational,  // 公理制
        Episcopal,       // 主教制
        Papal,           // 教皇制
        Theocratic,      // 神权制
        StateChurch      // 国教会
    }
}
