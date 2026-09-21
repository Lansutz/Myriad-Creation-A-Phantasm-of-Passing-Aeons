namespace CivilizationEvolution.Simulation.Politics
{
    public enum CentralInstitution
    {
        None,                   // 无常设：部落临时集会
        Court,                  // 王庭：王室+近臣（宫廷决策）
        Assembly,               // 议会/元老院：代表审议（构成=AssemblyComposition）
        EldersCouncil,          // 长老议事会：长老资格制（部落/贵族传统——非共和）
        BureaucraticCore,       // 官僚中枢：宰相府/尚书台（文书行政）
        ReligiousCouncil,       // 宗教会议：教廷/教阶
        MilitaryCouncil         // 军事委员会：将领共议
    }
}
