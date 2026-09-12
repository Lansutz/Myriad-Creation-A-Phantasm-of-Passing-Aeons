using System;
using System.Collections.Generic;
using CivilizationEvolution.Core.Enums;
using CivilizationEvolution.Infrastructure.Audio;
using CivilizationEvolution.Simulation.Society;



using CivilizationEvolution.World;
namespace CivilizationEvolution.Infrastructure.Save
{
 /// 完整游戏存档数据——包含地图、游戏时间、政权、编年史等核心游戏状态。
 /// 地图数据复用 MapSaveData，额外保存游戏运行时状态。
    [Serializable]
    public class GameSaveData
    {
 // ===== 元数据 =====
        public int version = 2;
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
        public BurgSaveData[] burgs;

 // ===== 政权数据（基本信息）=====
        public RealmBasicSaveData[] realms;

 // ===== 编年史/历史记录 =====
        public ChronicleEntrySaveData[] chronicleEntries;
    }

    [Serializable]
    public class RealmBasicSaveData
    {
        public int realmId;
        public string realmName;
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
