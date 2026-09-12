using System.Collections.Generic;
using System;
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
    [Serializable]
    public class ClassNeedReport
    {
        public GameEnums.SocialClass socialClass;
        [System.NonSerialized]
        public Dictionary<ClassNeedDimension, NeedDimensionScore> dimensions = new Dictionary<ClassNeedDimension, NeedDimensionScore>();
        [UnityEngine.Range(0f, 100f)] public float overallSatisfaction = 50f;  // 加权综合满足度
        public ClassNeedDimension worstDimension;                  // 最差维度（主要矛盾）
        [UnityEngine.Range(0f, 100f)] public float worstScore = 100f;
        public float population;                                   // 该阶层在政权内的总人口（外部统计填入）

        public NeedDimensionScore Get(ClassNeedDimension d) =>
            dimensions.TryGetValue(d, out var v) ? v : default;
    }
}
