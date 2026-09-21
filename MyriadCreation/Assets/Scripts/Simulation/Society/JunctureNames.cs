using CivilizationEvolution.Simulation.Politics;
using CivilizationEvolution.Simulation.Population;



namespace CivilizationEvolution.Simulation.Society
{
    public static class JunctureNames
    {
        public static string Get(CriticalJunctureType t) => t switch
        {
            CriticalJunctureType.SuccessionCrisis => "继承危机",
            CriticalJunctureType.WarDefeat => "战败",
            CriticalJunctureType.FiscalCollapse => "财政破产",
            CriticalJunctureType.EliteSplit => "精英分裂",
            CriticalJunctureType.PopularUprising => "民众起义",
            CriticalJunctureType.ForeignConquest => "外敌征服",
            CriticalJunctureType.StrongReformer => "强势改革者",
            _ => t.ToString()
        };

        public static string GetOutcome(JunctureOutcomeType o) => o switch
        {
            JunctureOutcomeType.Reform => "改革",
            JunctureOutcomeType.Compromise => "妥协",
            JunctureOutcomeType.Reaction => "复辟",
            JunctureOutcomeType.Stalemate => "僵持",
            JunctureOutcomeType.Collapse => "崩溃",
            _ => "未决"
        };
    }
}
