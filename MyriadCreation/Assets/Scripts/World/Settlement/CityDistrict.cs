using System;

namespace MyriadCreation.World.Settlement
{
    /// <summary>
    /// 城市区划数据
    /// 只有大型城市（Ⅳ级都会/Ⅴ级大都会）才会有区划
    /// 区划是城市的内部分区，不是独立的聚居点
    /// 区划的形成受进化路径影响，不同起源的城市区划结构不同
    /// </summary>
    [Serializable]
    public class CityDistrict
    {
        /// <summary>区划类型</summary>
        public CityDistrictType districtType;

        /// <summary>区划名称（可选，自动生成或玩家命名）</summary>
        public string districtName;

        /// <summary>面积比例（0~1，占城市总面积的比例）</summary>
        public float areaRatio;

        /// <summary>人口比例（0~1，占城市总人口的比例）</summary>
        public float populationRatio;

        /// <summary>发展度/建设度（0~100）</summary>
        public float development;

        /// <summary>财富</summary>
        public float wealth;

        /// <summary>创建时间（游戏内日期，用于历史记录）</summary>
        public float foundedDate;

        public CityDistrict()
        {
            districtType = CityDistrictType.None;
            districtName = "";
            areaRatio = 0f;
            populationRatio = 0f;
            development = 0f;
            wealth = 0f;
            foundedDate = 0f;
        }

        public CityDistrict(CityDistrictType type, string name, float area, float pop, float dev, float wealthVal, float date)
        {
            districtType = type;
            districtName = name;
            areaRatio = area;
            populationRatio = pop;
            development = dev;
            wealth = wealthVal;
            foundedDate = date;
        }
    }
}
