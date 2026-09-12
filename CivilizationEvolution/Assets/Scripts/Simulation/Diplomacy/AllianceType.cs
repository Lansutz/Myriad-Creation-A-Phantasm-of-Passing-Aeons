namespace CivilizationEvolution.Simulation.Diplomacy
{
    public enum AllianceType
    {
        NonAggressionPact,    // 互不侵犯：承诺不开战，可单方撕毁（信誉惩罚）
        DefensiveAlliance,     // 防御同盟：仅被第三方攻击时共同作战
        OffensiveAlliance,     // 进攻同盟：主动宣战时共同作战，防御时不强制
        TotalAlliance,         // 全面同盟：任何情况共同作战，共享军事情报
        Faction                // 阵营：多边军事政治集团，常设协调机构+集体安全
    }
}
