using System;

namespace CivilizationEvolution.Simulation.Society
{
    [Serializable]
    public struct DnaExpression
    {
        public float longevityOffsetYears;   // 寿命偏移（年）
        public float intelligenceOffset;     // 智慧偏移（±15 量级）
        public float martialOffset;          // 勇武偏移（±15 量级）
        public float resistanceOffset;       // 综合抗性偏移（±15 量级）
        public string appearanceTag;         // 外观标签（肤色/体型/面部）
        public string talentId;              // 触发的天赋（空=无）
        public string defectId;              // 触发的遗传病（空=无）
        public bool carriesDefect;           // 隐性携带者（Aa，不发病但可遗传）
    }
}
