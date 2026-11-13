using System;
using System.Collections.Generic;
using MyriadCreation.Core.Enums;
using MyriadCreation.Core.Data;
using MyriadCreation.Infrastructure.Audio;
using UnityEngine.Serialization;



using MyriadCreation.Save.Contracts;
using MyriadCreation.World;
using MyriadCreation.Simulation.State;
using MyriadCreation.Simulation.Systems;
using MyriadCreation.Content.Definitions;
namespace MyriadCreation.Infrastructure.Save
{
 /// 完整游戏存档数据——包含地图、游戏时间、政权、编年史等核心游戏状态。
 /// 地图数据复用 MapSaveData，额外保存游戏运行时状态。
    [Serializable]
    public class GameSaveData
    {
 // ===== 元数据 =====
        public int version = MyriadCreation.Core.Constants.GameConstants.SaveVersion;
        public string gameVersion = "0.1.0";
        public long saveTimestamp;
        public string saveName;

 // ===== 游戏时间 =====
        public int currentYear;
        public int currentDay;

 // ===== 地图基础 =====
        public int mapWidth;
        public int mapHeight;
        public int randomSeed;
        public int playerRealmId = -1;

 // ===== 地图数据（复用 MapSaveData 的结构）=====
        public TileSaveData[] tiles;
        public ProvinceSaveData[] provinces;
        [FormerlySerializedAs("burgs")]
        [FormerlySerializedAs("settlements")]
        public HabitationSiteSaveData[] habitation_sites;

        // ===== 运行时生成的新产品（合金/合成物等）=====
        public DynamicGoodsSaveData[] dynamicGoods;

        // ===== 实体关系与契约 =====
        public EntityRelation[] relations;
        public Contract[] contracts;


 // ===== 政权数据（基本信息）=====
        public RealmBasicSaveData[] realms;

        public TitleInstanceSaveData[] titleInstances;

 // ===== 编年史/历史记录 =====
        public ChronicleEntrySaveData[] chronicleEntries;
    }

    [Serializable]
    public class RealmBasicSaveData
    {
        public int realmId;
        public string realmName;
        public string primaryTitleKey;
        public int monarchId = -1;
        public float treasury;
        public float prestige;
        public float stability;
        public float centralization;
        public int suzerainId = -1;
        public int[] vassalIds;
        public int primaryCultureId = -1;
        public int stateReligionId = -1;
        public int[] controlledTiles;
    }

    [Serializable]
    public class TitleHoldingSaveData
    {
        public int characterId = -1;
        public int institutionId = -1;
        public int role;
        public float share = 1f;
        public int sinceDay = -1;
        public int endDay = -1;
        public float legitimacy = 50f;
    }

    [Serializable]
    public class TitleInstanceSaveData
    {
        public string titleId;
        public string definitionId;
        public TitleHoldingSaveData[] holders;
    }

    [Serializable]
    public class ChronicleEntrySaveData
    {
        public int entryId;
        public int tick;
        public int year;
        public string eventType;
        public string description;
        public bool major;
        public int[] participants;
    }
}
