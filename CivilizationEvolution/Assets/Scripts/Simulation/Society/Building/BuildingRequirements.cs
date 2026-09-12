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
using CivilizationEvolution.Simulation.WorldState;


namespace CivilizationEvolution.Simulation.Society
{
 // ============================================================ // 建筑可用性条件系统 // 特定建筑受地形/水文/群系限制，不符合条件的不在UI显示 // ============================================================
 /// 可修建建筑类型（包括堡垒亚型、特殊城形态、港口设施等）
 /// 用于UI过滤和AI建造决策

 /// 修建条件（地形/水文/群系/海拔/坡度/资源/位置）
 /// 所有条件为AND关系，全部满足才可修建

 /// 建筑可用性检查结果
}
