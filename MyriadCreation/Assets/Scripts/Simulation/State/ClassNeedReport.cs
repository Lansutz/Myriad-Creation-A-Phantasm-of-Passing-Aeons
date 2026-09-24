using System.Collections.Generic;
using System;
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
using MyriadCreation.Simulation.WorldState;



namespace MyriadCreation.Simulation.State
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
