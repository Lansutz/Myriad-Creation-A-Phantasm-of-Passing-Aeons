using System;

namespace CivilizationEvolution.UI
{
    [Serializable]
    public class MapSaveData
    {
        public int version = 1;
        public int mapWidth;
        public int mapHeight;
        public int randomSeed;
        public long saveTimestamp;

        // 地形数据（紧凑存储）
        public TileSaveData[] tiles;

        // 省份数据
        public ProvinceSaveData[] provinces;

        // 子地块数据
        public BurgSaveData[] burgs;
    }
}
