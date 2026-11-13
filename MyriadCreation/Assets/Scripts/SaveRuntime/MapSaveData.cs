using System;
using UnityEngine.Serialization;

using MyriadCreation.Save.Contracts;
using MyriadCreation.World.Settlement;
namespace MyriadCreation.Infrastructure.Save
{
    [Serializable]
    public class MapSaveData
    {
        public int version = MyriadCreation.Core.Constants.GameConstants.SaveVersion;
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

        // ===== 点/政权关系（Anchor 关系只在地图存档里） =====
        public MyriadCreation.Core.Data.EntityRelation[] relations;

    }
}
