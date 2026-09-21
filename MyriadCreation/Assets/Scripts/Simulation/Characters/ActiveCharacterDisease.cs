namespace CivilizationEvolution.Simulation.Characters
{
    /// <summary>
    /// 活跃疾病实例——角色身上正在发生的疾病
    /// </summary>
    [System.Serializable]
    public class ActiveCharacterDisease
    {
        public string diseaseId;              // 疾病ID（对应 CharacterDiseaseDef.diseaseId）
        public DiseaseStage stage;            // 当前阶段
        public int elapsedDays;               // 已经过天数
        public int stageDays;                 // 当前阶段已持续天数
        public float severity;                // 严重程度（0-1，影响属性修正幅度）
        public bool isTreated;                // 是否正在接受治疗
        public int treatmentDays;             // 已治疗天数
        public float infectionSource;         // 感染来源（0=自然发病，1=人口瘟疫，2=角色传播，3=战斗受伤，4=意外，5=遗传）

        public ActiveCharacterDisease()
        {
            diseaseId = "";
            stage = DiseaseStage.Incubation;
            elapsedDays = 0;
            stageDays = 0;
            severity = 0.5f;
            isTreated = false;
            treatmentDays = 0;
            infectionSource = 0;
        }

        public ActiveCharacterDisease(string id, float source = 0f)
        {
            diseaseId = id;
            stage = DiseaseStage.Incubation;
            elapsedDays = 0;
            stageDays = 0;
            severity = 0.5f;
            isTreated = false;
            treatmentDays = 0;
            infectionSource = source;
        }
    }
}
