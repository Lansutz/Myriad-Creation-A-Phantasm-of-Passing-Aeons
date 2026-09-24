using System;

using MyriadCreation.World;
namespace MyriadCreation.Infrastructure.Save
{
    [Serializable]
    public class BurgSaveData
    {
        public int burgId;
        public string burgName;
        public int type;
        public int provinceId;
        public int tileIndex;
        public float x;
        public float y;
        public float population;
        public float development;
        public float wealth;
        public float tradePower;
        public float fortification;
        public int garrison;
        public bool isCapital;
        public bool isPort;
        public bool isCoastal;
        public bool hasMarket;
        public bool hasTemple;
        public bool hasUniversity;
        public int settlementCategory;
        public float constructionProgress;
        public int constructionTier;

        // ===== 经济成分系统 =====
        public int[] economicSectors;      // 成分类型数组（EconomicSector枚举值）
        public float[] economicRatios;     // 成分比例数组
        public int primarySector;          // 主要经济成分
        public bool economicInitialized;   // 是否已初始化

        // ===== 城市区划系统 =====
        public CityDistrictSaveData[] districts;  // 区划数组
        public bool districtsGenerated;            // 是否已生成区划
    }

    /// <summary>
    /// 城市区划保存数据
    /// </summary>
    [Serializable]
    public class CityDistrictSaveData
    {
        public int districtType;        // 区划类型（CityDistrictType枚举值）
        public string districtName;     // 区划名称
        public float areaRatio;         // 面积比例
        public float populationRatio;   // 人口比例
        public float development;       // 发展度
        public float wealth;            // 财富
        public float foundedDate;       // 创建时间
    }
}
