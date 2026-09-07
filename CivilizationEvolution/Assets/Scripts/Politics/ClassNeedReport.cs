using System.Collections.Generic;
using System;
using CivilizationEvolution.Core;

namespace CivilizationEvolution.Politics
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
