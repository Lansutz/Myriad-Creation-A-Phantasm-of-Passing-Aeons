using System.Collections.Generic;
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
using CivilizationEvolution.Simulation.Society;
using CivilizationEvolution.Simulation.WorldState;



namespace CivilizationEvolution.Simulation.Warfare
{
    [System.Serializable]
    public struct UnitDef
    {
        public int unitId;
        public string unitName;
        public GameEnums.UnitCategory category;
        public int tier; // 1轻型 2中型 3重型 4超重型

 // 战斗属性
        public float meleeAttack;
        public float rangedAttack;
        public float defense;
        public float morale;
        public float speed; // 地块/天
        public float supplyConsumption; // 每日补给消耗

 // 招募消耗
        [System.NonSerialized] public Dictionary<int, float> recruitCost; // goodsId -> 数量

        public float manpowerCost; // 人力消耗

 // 地形偏好
        [System.NonSerialized] public Dictionary<GameEnums.TerrainTacticType, float> terrainModifiers;


 /// 解锁前置革新（兵种必须有对应革新才能征募——重骑兵需马镫等）
 /// 由 AddUnitDef 赋值（struct 不能带字段初始化器）
        public List<int> requiredInnovations;
    }
}
