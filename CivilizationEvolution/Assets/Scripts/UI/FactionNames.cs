using CivilizationEvolution.Politics;

namespace CivilizationEvolution.UI
{
    public static class FactionNames
    {
        public static string Get(FactionStance s) => s switch
        {
            FactionStance.Conservative => "保守派",
            FactionStance.Reformist => "改革派",
            FactionStance.Radical => "激进派",
            FactionStance.Reactionary => "复辟派",
            _ => s.ToString()
        };
    }
}
