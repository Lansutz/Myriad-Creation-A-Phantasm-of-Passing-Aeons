using System.Collections.Generic;

namespace MyriadCreation.Simulation.Characters
{
    /// <summary>
    /// 子结构数据——病因数据层
    /// 每个身体部位下有若干子结构，子结构的定量数据就是病因数据
    /// 这部分数据在后台模拟，不直接显示给玩家
    /// 病名（白内障/青光眼等）是子结构病变达到阈值后的诊断结果
    /// </summary>
    [System.Serializable]
    public struct SubStructureData
    {
        public SubStructureType type;          // 子结构类型
        public float condition;                 // 病因数据 0-100（0=健康，100=严重病变）
        public float progressionRate;           // 每日进展速度（0=不进展）
        public List<string> causingFactors;     // 导致因素列表（"aging"/"genetic"/"injury"/"diabetes"/"infection"...）

        public SubStructureData(SubStructureType type)
        {
            this.type = type;
            this.condition = 0f;
            this.progressionRate = 0f;
            this.causingFactors = new List<string>();
        }
    }

    /// <summary>
    /// 已诊断的病症——子结构病变达到阈值并经过诊断后显示
    /// 诊断前玩家只看到模糊症状，诊断后才显示具体病名
    /// </summary>
    [System.Serializable]
    public struct DiagnosedCondition
    {
        public string conditionId;              // 病症ID（如"cataract"、"glaucoma"）
        public string displayName;              // 显示名称（如"白内障"、"青光眼"）
        public SubStructureType sourceSubStructure; // 来源子结构
        public BodyPartType sourcePart;         // 来源部位
        public int diagnosedDay;                // 诊断日期（游戏内天数）
        public float severityAtDiagnosis;       // 诊断时的严重程度
    }

    /// <summary>
    /// 身体部位状态
    /// </summary>
    public enum BodyPartState
    {
        Healthy,    // 健康
        Injured,    // 受伤（HP < 100%）
        Critical,   // 重伤（HP < 30%）
        Destroyed   // 功能丧失（HP = 0）
    }

    /// <summary>
    /// 身体部位数据——宏观部位
    /// 包含若干子结构（病因数据），HP和功能水平由子结构汇总计算
    /// </summary>
    [System.Serializable]
    public struct BodyPartData
    {
        public BodyPartType part;                // 部位类型
        public float maxHp;                      // 最大HP
        public float currentHp;                  // 当前HP（由子结构condition汇总计算）
        public float functionLevel;              // 功能水平 0-1（0=完全丧失，1=正常，由子结构汇总计算）
        public BodyPartState state;              // 状态
        public List<SubStructureData> subStructures; // 子结构列表（病因数据）
        public List<DiagnosedCondition> diagnosedConditions; // 已诊断的病症

        public BodyPartData(BodyPartType part)
        {
            this.part = part;
            this.maxHp = 100f;
            this.currentHp = 100f;
            this.functionLevel = 1f;
            this.state = BodyPartState.Healthy;
            this.subStructures = new List<SubStructureData>();
            this.diagnosedConditions = new List<DiagnosedCondition>();
        }
    }
}
