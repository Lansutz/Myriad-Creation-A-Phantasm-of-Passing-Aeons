using System;

namespace CivilizationEvolution.Simulation.Economy
{
    /// <summary>
    /// 资源类型（决定生成规则和开发方式）。
    /// 物资（GoodsDef）同时是自然资源和经济商品：
    /// WildAnimal/WildPlant/Mineral/Aquatic/Forest/Special = 地图上可生成的自然资源点；
    /// Manufactured = 纯加工品，不在地图上生成，由低级物资加工而来。
    /// </summary>
    public enum ResourceType
    {
        /// <summary>野生动物（可驯化/狩猎）：野马、野牛、驯鹿、骆驼</summary>
        WildAnimal = 0,
        /// <summary>野生植物（可栽培/采集）：野生谷物、野果、药材</summary>
        WildPlant = 1,
        /// <summary>矿物/矿石（需开采，自然资源）：铜矿石、铁矿石、锡矿石、岩盐、宝石、金矿石、银矿石</summary>
        Mineral = 2,
        /// <summary>水产（需捕捞）：鱼类、贝类、珍珠</summary>
        Aquatic = 3,
        /// <summary>森林（需采伐）：木材、猎物、树脂</summary>
        Forest = 4,
        /// <summary>加工品（非自然资源，由低级物资加工而来）：金属（铜、铁、金、银，由矿石冶炼）、器械、奢侈品等</summary>
        Manufactured = 5,
        /// <summary>特殊资源：黑曜石、沥青、硫磺</summary>
        Special = 6
    }

    /// <summary>
    /// 资源生成条件（地理阈值）。
    /// 地图生成时，地块需满足这些条件才可能生成对应资源点。
    /// 所有阈值为0或负值时表示不限制。
    /// </summary>
    [Serializable]
    public struct ResourceSpawnCondition
    {
        /// <summary>最小海拔（0-1，0=不限制）</summary>
        public float minElevation;
        /// <summary>最大海拔（0-1，1=不限制）</summary>
        public float maxElevation;
        /// <summary>最小年降水（mm，0=不限制）</summary>
        public float minPrecipitation;
        /// <summary>最大年降水（mm，9999=不限制）</summary>
        public float maxPrecipitation;
        /// <summary>最小年均温（°C，-999=不限制）</summary>
        public float minTemperature;
        /// <summary>最大年均温（°C，999=不限制）</summary>
        public float maxTemperature;
        /// <summary>最大坡度（度，90=不限制）</summary>
        public float maxSlope;
        /// <summary>是否需要海岸</summary>
        public bool requiresCoast;
        /// <summary>是否需要河流</summary>
        public bool requiresRiver;

        /// <summary>默认构造：全部不限制</summary>
        public static ResourceSpawnCondition Unrestricted => new ResourceSpawnCondition
        {
            minElevation = 0f,
            maxElevation = 1f,
            minPrecipitation = 0f,
            maxPrecipitation = 9999f,
            minTemperature = -999f,
            maxTemperature = 999f,
            maxSlope = 90f,
            requiresCoast = false,
            requiresRiver = false
        };
    }
}
