using CivilizationEvolution.Core;

namespace CivilizationEvolution.Core
{
    [System.Serializable]
    public struct PopulationBlock
    {
        public float count;
        public int raceId;
        public int cultureId;
        public int faithId;
        public GameEnums.SocialClass socialClass;
        public int profession;
        public float satisfaction;
        public float culturePenetration;
    }
}
