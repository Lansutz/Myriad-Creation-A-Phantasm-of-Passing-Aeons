using System;
using UnityEngine.Serialization;

using MyriadCreation.World;
using MyriadCreation.World.Settlement;
namespace MyriadCreation.Infrastructure.Save
{
    [Serializable]
    public class MapSaveData
    {
        public int version = 3;
        public int mapWidth;
        public int mapHeight;
        public int randomSeed;
        public long saveTimestamp;

 // 地形数据（紧凑存储）
        public TileSaveData[] tiles;

 // 省份数据
        public ProvinceSaveData[] provinces;

 // 子地块数据
        [FormerlySerializedAs("burgs")]
        [FormerlySerializedAs("settlements")]
        public HabitationSiteSaveData[] habitation_sites;

    }
}
