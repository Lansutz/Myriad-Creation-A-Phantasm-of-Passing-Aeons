using System;

using CivilizationEvolution.World;
namespace CivilizationEvolution.Infrastructure.Save
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
