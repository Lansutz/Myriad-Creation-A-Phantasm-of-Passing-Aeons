using System;

namespace CivilizationEvolution.UI
{
    [Serializable]
    public class ProvinceSaveData
    {
        public int provinceId;
        public string provinceName;
        public int centerTileIndex;
        public int[] memberTiles;
    }
}
