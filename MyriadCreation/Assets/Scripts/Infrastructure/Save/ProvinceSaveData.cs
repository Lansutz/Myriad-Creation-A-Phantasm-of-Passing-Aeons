using System;

using MyriadCreation.World;
namespace MyriadCreation.Infrastructure.Save
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
