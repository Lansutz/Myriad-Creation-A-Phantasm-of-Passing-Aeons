namespace CivilizationEvolution.Simulation.Characters
{
    /// <summary>
    /// 角色疾病定义——数据驱动，可由模组覆盖/新增
    /// 借鉴CK3的健康特质(Health traits)逻辑：疾病就是一个特质，有健康修正和属性修正
    /// 疾病可以导致感官/能力衰退（如白内障导致视力衰退），衰退到最高级后获得永久特质
    /// </summary>
    [System.Serializable]
    public struct CharacterDiseaseDef
    {
        public string diseaseId;              // 疾病ID
        public string diseaseName;            // 疾病名称
        public string description;            // 疾病描述
        public CharacterDiseaseCategory category;  // 疾病分类
        public TransmissionType transmission; // 传播方式

        // 导致的衰退（可选）——疾病可以导致某种感官/能力的渐进性衰退
        public ImpairmentType? causesImpairment;   // 导致哪种衰退（如Vision）
        public float impairmentProgressionRate;    // 衰退每日进展概率（0-1）
        public ImpairmentLevel initialImpairmentLevel;  // 患病时初始衰退等级

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
