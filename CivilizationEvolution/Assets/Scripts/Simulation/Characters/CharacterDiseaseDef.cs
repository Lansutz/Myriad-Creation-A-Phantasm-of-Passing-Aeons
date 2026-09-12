namespace CivilizationEvolution.Simulation.Characters
{
    /// <summary>
    /// 角色疾病定义——数据驱动，可由模组覆盖/新增
    /// </summary>
    [System.Serializable]
    public struct CharacterDiseaseDef
    {
        public string diseaseId;              // 疾病ID
        public string diseaseName;            // 疾病名称
        public string description;            // 疾病描述
        public CharacterDiseaseCategory category;  // 疾病分类
        public TransmissionType transmission; // 传播方式

        // 疾病参数
        public float baseInfectionRate;       // 基础感染率（传染病用）
        public float baseMortalityRate;       // 基础死亡率（每日死亡概率）
        public float baseRecoveryRate;        // 基础恢复率（每日恢复概率）
        public int incubationDays;            // 潜伏期天数
        public int acuteDurationDays;         // 急性期持续天数
        public bool isChronic;                // 是否转为慢性病
        public bool isPermanent;              // 是否永久不可逆
        public bool isGenetic;                // 是否遗传病（DNA决定）

        // 属性修正（急性期生效，慢性期部分生效）
        public float healthMod;               // 健康修正（每日扣减）
        public float prowessMod;              // 勇武修正
        public float socialMod;               // 社交修正
        public float militaryMod;             // 军事修正
        public float managementMod;           // 管理修正
        public float conspiracyMod;           // 阴谋修正
        public float scholarshipMod;          // 学识修正
        public float charmMod;                // 魅力修正
        public float fertilityMod;            // 生育力修正

        // 触发条件（非传染病用）
        public int minAgeOnset;               // 最小发病年龄
        public int maxAgeOnset;               // 最大发病年龄
        public float obesityThreshold;        // 肥胖阈值（超过则风险上升）
        public float stressThreshold;         // 压力阈值（超过则风险上升）
        public bool combatInjury;             // 是否战斗伤病
        public bool accidentInjury;           // 是否意外伤病

        // 治疗
        public bool treatable;                // 是否可治疗
        public float treatmentRecoveryBonus;  // 治疗恢复率加成
        public string requiredInnovation;     // 所需革新（如"医学"）才能有效治疗
    }
}
