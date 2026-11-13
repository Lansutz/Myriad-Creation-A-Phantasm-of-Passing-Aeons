using System;

namespace MyriadCreation.Save.Contracts
{
    /// <summary>省份存档 DTO（纯字段）。</summary>
    [Serializable]
    public class ProvinceSaveData
    {
        public int provinceId;
        public string provinceName;
        public int centerTileIndex;
        public int[] memberTiles;
    }
}
