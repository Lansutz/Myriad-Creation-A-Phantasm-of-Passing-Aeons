namespace CivilizationEvolution.Simulation.Politics
{
    public enum CentralSuccession
    {
        Appointed,      // 上级决断：君主/中枢任免
        Elected,        // 集体选择：选举/推举（范围=资格要素：大众/贵族/权贵）
        Examination,    // 客观标准：考试/竞争选任（科举/文官考试）
        Hereditary      // 血缘：官位世袭（世卿世禄）
    }
}
