using System;

namespace MyriadCreation.Save.Contracts
{
    [Serializable]
    public class DenominationSaveData
    {
        public int denominationId = -1;
        public int religionId = -1;
        public int parentDenominationId = -1;
        public string denominationName = "";
        public bool isDefault;
        public int status;
        public float divergence;
        public int sinceDay;
    }
}
