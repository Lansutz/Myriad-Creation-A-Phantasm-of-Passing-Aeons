using System;
using UnityEngine;

namespace CivilizationEvolution.Render
{
 /// 地图可选叠加层（少量，可独立开关）。
 /// 设计原则：不搞无限叠加，只保留必要的、性能可控的叠加层。
 /// 地图模式（政治/文化/宗教等）是互斥单选，不在此枚举中。
    [Flags]
    public enum MapOverlayLayer
    {
 /// <summary>无叠加层</summary>
        None = 0,
 /// <summary>省份边界线</summary>
        ProvinceBorders = 1 << 0,
 /// <summary>聚落标记（首都/城市/港口/要塞/集镇）</summary>
        BurgMarkers = 1 << 1,
 /// <summary>网格线（经纬度/地块网格）</summary>
        Grid = 1 << 2,
 /// <summary>军队标记</summary>
        ArmyMarkers = 1 << 3,
 /// <summary>贸易路线</summary>
        TradeRoutes = 1 << 4,
    }

 /// 地图基础图层（始终叠加，不可关闭）。
 /// 这些是地图可读性的基础，任何地图模式下都必须显示。
    [Flags]
    public enum MapBaseLayer
    {
 /// <summary>地形底色（高程/海陆）——所有地图模式的底色</summary>
        TerrainBase = 1 << 0,
 /// <summary>海岸线（海陆边界线）</summary>
        Coastline = 1 << 1,
 /// <summary>河流（水系）</summary>
        Rivers = 1 << 2,
    }

 /// 地图图层配置。
 /// 设计原则：
 /// 1. 不搞无限叠加——只保留必要的、性能可控的叠加层
 /// 2. 基础图层必须放在一起——始终叠加，不可关闭
 /// 3. 地图模式互斥单选——选了文化就不能看政治，避免渲染压力
 /// 4. 可选叠加层少量——省份边界、聚落标记、网格、军队、贸易路线
    [Serializable]
    public class MapLayerConfig
    {
        [Header("基础图层（始终叠加，不可关闭）")]
        [Tooltip("地形底色：所有地图模式的底色，始终显示")]
        public bool showTerrainBase = true;
        [Tooltip("海岸线：海陆边界线，始终显示")]
        public bool showCoastline = true;
        [Tooltip("河流：水系，始终显示")]
        public bool showRivers = true;

        [Header("可选叠加层（少量，可独立开关）")]
        [Tooltip("省份边界线")]
        public bool showProvinceBorders = true;
        [Tooltip("聚落标记（首都/城市/港口/要塞/集镇）")]
        public bool showBurgMarkers = true;
        [Tooltip("网格线（经纬度/地块网格）")]
        public bool showGrid = false;
        [Tooltip("军队标记")]
        public bool showArmyMarkers = false;
        [Tooltip("贸易路线")]
        public bool showTradeRoutes = false;
        [Tooltip("虚控制范围（三角影响力，政治地图模式下显示）")]
        public bool showVirtualControl = true;
        [Tooltip("聚落辐射范围（政治地图模式下显示）")]
        public bool showInfluenceRadius = false;

 /// <summary>当前启用的可选叠加层位掩码</summary>
        public MapOverlayLayer ActiveOverlays
        {
            get
            {
                MapOverlayLayer mask = MapOverlayLayer.None;
                if (showProvinceBorders) mask |= MapOverlayLayer.ProvinceBorders;
                if (showBurgMarkers) mask |= MapOverlayLayer.BurgMarkers;
                if (showGrid) mask |= MapOverlayLayer.Grid;
                if (showArmyMarkers) mask |= MapOverlayLayer.ArmyMarkers;
                if (showTradeRoutes) mask |= MapOverlayLayer.TradeRoutes;
                return mask;
            }
        }

 /// <summary>检查某个可选叠加层是否启用</summary>
        public bool IsOverlayEnabled(MapOverlayLayer layer) =>
            (ActiveOverlays & layer) == layer;

 /// <summary>切换某个可选叠加层</summary>
        public void ToggleOverlay(MapOverlayLayer layer, bool enabled)
        {
            switch (layer)
            {
                case MapOverlayLayer.ProvinceBorders: showProvinceBorders = enabled; break;
                case MapOverlayLayer.BurgMarkers: showBurgMarkers = enabled; break;
                case MapOverlayLayer.Grid: showGrid = enabled; break;
                case MapOverlayLayer.ArmyMarkers: showArmyMarkers = enabled; break;
                case MapOverlayLayer.TradeRoutes: showTradeRoutes = enabled; break;
            }
        }

 /// <summary>重置为默认配置</summary>
        public void ResetToDefault()
        {
            showTerrainBase = true;
            showCoastline = true;
            showRivers = true;
            showProvinceBorders = true;
            showBurgMarkers = true;
            showGrid = false;
            showArmyMarkers = false;
            showTradeRoutes = false;
            showVirtualControl = true;
            showInfluenceRadius = false;
        }
    }
}
