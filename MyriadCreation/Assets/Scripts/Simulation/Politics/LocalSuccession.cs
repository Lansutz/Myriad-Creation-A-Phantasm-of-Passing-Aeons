namespace CivilizationEvolution.Simulation.Politics
{
    public enum LocalSuccession
    {
        Appointed,      // 任命（主体=LocalAppointAuthority：中央/教会/军事）——举荐/派任
        Elected,        // 选举/推举（范围=资格要素：本地公民/部落/自治市）
        Examination,    // 考试选拔（科举/文官考试——官僚体系精英选拔）
        Hereditary,     // 世袭领有（身份=LocalLordship：封臣/宗室/军功）——君主制专属
        CityCharter     // 城市特许自治：自治权来源=特许状契约（中世纪自由城市——内部选举+特许保障）
    }
}
