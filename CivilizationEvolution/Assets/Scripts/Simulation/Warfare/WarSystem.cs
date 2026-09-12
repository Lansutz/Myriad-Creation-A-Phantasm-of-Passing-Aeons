using System.Collections.Generic;
using UnityEngine;
using CivilizationEvolution.Core;
using CivilizationEvolution.Core.Constants;
using CivilizationEvolution.Core.Data;
using CivilizationEvolution.Core.Dto;
using CivilizationEvolution.Core.Enums;
using CivilizationEvolution.Simulation.Diplomacy;
using CivilizationEvolution.Simulation.Economy;
using CivilizationEvolution.Simulation.Events;
using CivilizationEvolution.Simulation.Generation;
using CivilizationEvolution.Simulation.Modding;
using CivilizationEvolution.Simulation.Politics;
using CivilizationEvolution.Simulation.Settlement;
using CivilizationEvolution.Simulation.Society;
using CivilizationEvolution.Simulation.WorldState;
using CivilizationEvolution.World.Generation;
using CivilizationEvolution.World.Hydrology;
using CivilizationEvolution.World.Settlement;
using CivilizationEvolution.World.Terrain;





namespace CivilizationEvolution.Simulation.Warfare
{
 /// <summary>兵种定义</summary>

 /// <summary>军团（兵力块集合）</summary>

 /// <summary>战斗管理器</summary>

 /// <summary>战斗结果</summary>

 /// 战争状态（战争闭环——分数累计/白和/胜利判定）
 /// 分数按 WarRules score 体系：野战胜利=scoreBattle、歼灭敌军=按规模、
 /// 占城=scoreCity 等（当前实现：战斗胜利+占领加分）
    }
