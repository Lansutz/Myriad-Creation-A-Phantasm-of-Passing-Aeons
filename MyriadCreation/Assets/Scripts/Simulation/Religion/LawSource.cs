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

    public enum LawSource
    {
        Customary,      // 习惯法
        Statutory,       // 成文法
        Religious,       // 宗教法
        Common,          // 普通法
        Civil,           // 大陆法系
        Mixed            // 混合法
    }
}
