namespace CivilizationEvolution.Simulation.Characters
{
    public class EpithetDef
    {
        public string id;
        public string name;
        public EpithetConnotation connotation = EpithetConnotation.Neutral;
        public EpithetTier tier = EpithetTier.Common;
        public string note = ""; // 历史参照/说明
    }
}
