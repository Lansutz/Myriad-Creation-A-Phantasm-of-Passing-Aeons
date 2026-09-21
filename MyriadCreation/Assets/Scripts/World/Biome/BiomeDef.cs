using System;

using UnityEngine;
using CivilizationEvolution.Core;
using CivilizationEvolution.Core.Constants;
using CivilizationEvolution.Core.Data;
using CivilizationEvolution.Core.Dto;
using CivilizationEvolution.Core.Enums;


namespace CivilizationEvolution.World.Biome
{
    /// <summary>
    /// 群系定义（JSON 可序列化，参考 Azgaar FMG 的 biomesData 数据驱动设计）。
    /// 从 StreamingAssets/Base/Biome/Biomes.json 加载，Mods 同名覆盖。
    /// 包含群系的显示名称、颜色、移动成本、可居住性等可配置属性。
    /// </summary>
    [Serializable]
    public class BiomeDef
    {
        /// <summary>群系 ID（对应 BiomeType 枚举的整数值）</summary>
        public int biomeId;

        /// <summary>群系显示名称</summary>
        public string name = "";

        /// <summary>群系颜色（十六进制，如 #4a7c23）</summary>
        public string color = "#808080";

        /// <summary>基础移动成本（1.0=平原，50=极难通行）</summary>
        public float movementCost = 1.5f;

        /// <summary>可居住性（0-100，影响人口承载和聚落生成）</summary>
        public float habitability = 50f;

        /// <summary>农业肥力修正（0-2，1.0=正常）</summary>
        public float fertilityMultiplier = 1.0f;

        /// <summary>群系分类（A低水沃野/B高地硬骨/C极端覆盖/D海洋）</summary>
        public string category = "C";

        /// <summary>是否海洋群系</summary>
        public bool isOcean = false;

        /// <summary>图标名称（用于地形渲染的装饰图标）</summary>
        public string icon = "";

        /// <summary>图标密度（0-150）</summary>
        public int iconDensity = 0;

        /// <summary>转换为 BiomeType 枚举</summary>
        public GameEnums.BiomeType BiomeType => (GameEnums.BiomeType)biomeId;

        /// <summary>解析颜色（失败返回灰色）</summary>
        public Color ParseColor()
        {
            if (ColorUtility.TryParseHtmlString(color, out var c)) return c;
            return Color.gray;
        }
    }
}
