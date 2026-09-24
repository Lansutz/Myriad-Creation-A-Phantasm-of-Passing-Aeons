using System.Collections.Generic;
using UnityEngine;
using MyriadCreation.Core;
using MyriadCreation.Core.Constants;
using MyriadCreation.Core.Data;
using MyriadCreation.Core.Dto;
using MyriadCreation.Core.Enums;
using MyriadCreation.Simulation.Diplomacy;
using MyriadCreation.Simulation.Economy;
using MyriadCreation.Simulation.Events;
using MyriadCreation.Simulation.Generation;
using MyriadCreation.Simulation.Modding;
using MyriadCreation.Simulation.Politics;
using MyriadCreation.Simulation.Settlement;
using MyriadCreation.Simulation.Society;
using MyriadCreation.Simulation.WorldState;
using MyriadCreation.World.Generation;
using MyriadCreation.World.Hydrology;
using MyriadCreation.World.Settlement;
using MyriadCreation.World.Terrain;





namespace MyriadCreation.Simulation.Systems
{
 /// <summary>兵种定义</summary>

 /// <summary>军团（兵力块集合）</summary>

 /// <summary>战斗管理器</summary>

 /// <summary>战斗结果</summary>

 /// 战争状态（战争闭环——分数累计/白和/胜利判定）
 /// 分数按 WarRules score 体系：野战胜利=scoreBattle、歼灭敌军=按规模、
 /// 占城=scoreCity 等（当前实现：战斗胜利+占领加分）
    }
