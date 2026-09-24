using MyriadCreation.Core;
using MyriadCreation.Core.Constants;
using MyriadCreation.Core.Dto;
using MyriadCreation.Core.Enums;



namespace MyriadCreation.Core.Data
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
