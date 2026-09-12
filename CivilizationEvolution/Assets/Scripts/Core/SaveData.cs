using CivilizationEvolution.Culture;
using CivilizationEvolution.Economy;
using CivilizationEvolution.War;
using System.Collections.Generic;
using System;

namespace CivilizationEvolution.Core
{
    [Serializable]
    public class SaveData
    {
        public int version;
        public string saveTime;

 // 世界配置
        public int mapWidth;
        public int mapHeight;
        public int currentYear;
        public int currentDay;
        public int currentSeason;

 // 核心数据（TileData/CultureData/GoodsDef/TradeRoute 无字典，可直接序列化）
        public TileData[] tiles;
        public List<RaceDTO> races;
        public List<CultureData> cultures;
        public List<RealmDTO> realms;
        public List<TradeCenterDTO> tradeCenters;
        public List<GoodsDef> goodsDefs;
        public string configJson; // WorldConfig(ScriptableObject)的JSON快照

 // 战争状态（WarState 无字典字段，可直接序列化——读档恢复战争闭环）
        public List<WarState> wars;
    }
}
