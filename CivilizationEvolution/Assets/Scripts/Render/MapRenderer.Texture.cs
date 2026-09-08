using System;
using System.Collections.Generic;
using UnityEngine;
using CivilizationEvolution.Core;
using CivilizationEvolution.Map;
using CivilizationEvolution.Politics;
using CivilizationEvolution.Culture;
using CivilizationEvolution.Diplomacy;

namespace CivilizationEvolution.Render
{
    /// <summary>
    /// MapRenderer.Texture —— 纹理更新（地图纹理重绘/强制刷新/获取纹理）（partial class，与 MapRenderer.cs 共享字段）
    /// </summary>
    public partial class MapRenderer
    {

        /// <summary>更新地图纹理</summary>
                private void UpdateMapTexture()
        {
            if (world == null || world.tiles == null) return;
            if (world.tiles.Length != mapWidth * mapHeight) return;

            // 复用像素缓冲区，仅在尺寸变化时重新分配
            int pixelCount = mapWidth * mapHeight;
            if (_pixelBuffer == null || _pixelBuffer.Length != pixelCount)
                _pixelBuffer = new Color[pixelCount];

            for (int i = 0; i < world.tiles.Length; i++)
            {
                if (!world.tiles[i].exists)
                {
                    _pixelBuffer[i] = VoidColor; // 虚空/地图外：深色
                    continue;
                }
                _pixelBuffer[i] = GetTileColor(i);
            }

            // ===== 虚控制范围渲染（政治地图模式下，半透明三角形内部）=====
            if (displayMode == MapDisplayMode.Political && _layerConfig.showVirtualControl)
            {
                DrawVirtualControlOverlay();
            }

            // ===== 聚落辐射范围渲染（政治地图模式下，半透明圆形）=====
            if (displayMode == MapDisplayMode.Political && _layerConfig.showInfluenceRadius)
            {
                DrawInfluenceRadiusOverlay();
            }

            // ===== 占领/争议条纹叠加（45°斜线，不修改底色，仅叠加）=====
            for (int i = 0; i < world.tiles.Length; i++)
            {
                if (!world.tiles[i].exists) continue;
                if (!IsOccupiedDisputed(world.tiles[i])) continue;
                int sx = i % mapWidth;
                int sy = i / mapWidth;
                // 45°斜线：(x + y) % spacing < spacing/2 时叠加条纹色
                int spacing = Mathf.Max(2, _occupationStripeSpacing);
                if ((sx + sy) % spacing < spacing / 2)
                {
                    _pixelBuffer[i] = Color.Lerp(_pixelBuffer[i],
                        _occupationStripeColor, _occupationStripeColor.a);
                }
            }

            // ===== 聚落独立图标渲染（可选叠加层，按等级大小+形态形状）=====
            if (_layerConfig.showBurgMarkers && world.burgs != null && world.burgs.Count > 0)
            {
                foreach (var burg in world.burgs.Values)
                {
                    if (!burg.IsMajorSettlement) continue;
                    if (burg.tileIndex < 0 || burg.tileIndex >= pixelCount) continue;
                    DrawSettlementIcon(burg, pixelCount);
                }
            }

            // ===== 省份边界线（可选叠加层）=====
            if (_layerConfig.showProvinceBorders && world.provinces != null && world.provinces.Count > 0)
            {
                for (int i = 0; i < pixelCount; i++)
                {
                    if (!world.tiles[i].exists) continue;
                    if (!IsProvinceBorder(i)) continue;
                    _pixelBuffer[i] = Color.Lerp(_pixelBuffer[i], provinceBorderColor, 0.7f);
                }
            }

            // ===== 网格线（可选叠加层）=====
            if (_layerConfig.showGrid)
            {
                int gridSpacing = Mathf.Max(8, mapWidth / 32); // 网格间距随地图尺寸调整
                for (int i = 0; i < pixelCount; i++)
                {
                    if (!world.tiles[i].exists) continue;
                    int x = i % mapWidth;
                    int y = i / mapWidth;
                    if (x % gridSpacing == 0 || y % gridSpacing == 0)
                    {
                        _pixelBuffer[i] = Color.Lerp(_pixelBuffer[i], new Color(0.5f, 0.5f, 0.5f, 0.3f), 0.3f);
                    }
                }
            }

            // 编辑器画笔预览（编辑模式下高亮鼠标悬停的画笔范围）
            if (_mapEditor != null && _mapEditor.IsEditMode && _hoverTile >= 0 && _mapEditor.CurrentTool != EditorTool.None)
            {
                var brushTiles = _mapEditor.GetBrushTileIndices(_hoverTile);
                Color previewColor = new Color(1f, 1f, 1f, 0.4f); // 半透明白色预览
                foreach (int bt in brushTiles)
                {
                    if (bt >= 0 && bt < pixelCount)
                    {
                        _pixelBuffer[bt] = Color.Lerp(_pixelBuffer[bt], previewColor, 0.5f);
                    }
                }
            }
            mapTexture.SetPixels(_pixelBuffer);

            mapTexture.Apply();
        }


        #region 聚落与虚控制渲染辅助方法

        /// <summary>虚控制范围计算结果缓存（避免每帧重算）</summary>
        private VirtualControlResult _cachedVirtualControl;
        private int _cachedVirtualControlFrame = -1;

        /// <summary>绘制聚落独立图标（按等级大小+形态形状）</summary>
        private void DrawSettlementIcon(BurgData burg, int pixelCount)
        {
            int bx = burg.tileIndex % mapWidth;
            int by = burg.tileIndex / mapWidth;

            // 图标大小根据等级：Ⅰ=2px, Ⅱ=3px, Ⅲ=4px, Ⅳ=5px, Ⅴ=6px
            int level = (int)burg.settlementLevel;
            int iconSize = Mathf.Clamp(level + 1, 2, 6);
            if (mapWidth > 512) iconSize = Mathf.Clamp(iconSize + 1, 3, 7);

            // 图标颜色根据类型
            Color iconColor = burg.type switch
            {
                BurgType.Capital => new Color(1f, 0.85f, 0.2f, 1f),
                BurgType.City => new Color(1f, 1f, 1f, 1f),
                BurgType.Port => new Color(0.3f, 0.6f, 1f, 1f),
                BurgType.Fortress => new Color(0.9f, 0.3f, 0.2f, 1f),
                BurgType.Town => new Color(0.9f, 0.8f, 0.4f, 1f),
                _ => new Color(0.6f, 0.6f, 0.6f, 1f)
            };

            int half = iconSize / 2;
            for (int dy = -half; dy <= half; dy++)
            {
                for (int dx = -half; dx <= half; dx++)
                {
                    int px = bx + dx, py = by + dy;
                    if (px < 0 || px >= mapWidth || py < 0 || py >= mapHeight) continue;

                    // 根据聚落形态决定图标形状
                    bool draw = burg.settlementType switch
                    {
                        // 城：方形图标
                        SettlementType.City => true,
                        // 堡：菱形/十字图标
                        SettlementType.Fort => Mathf.Abs(dx) + Mathf.Abs(dy) <= half + 1,
                        // 村镇：圆形图标
                        _ => dx * dx + dy * dy <= half * half + 1
                    };

                    if (draw)
                    {
                        int pi = py * mapWidth + px;
                        // 边缘用深色描边
                        bool isEdge = (dx == -half || dx == half || dy == -half || dy == half);
                        _pixelBuffer[pi] = isEdge ? Color.Lerp(iconColor, Color.black, 0.5f) : iconColor;
                    }
                }
            }
        }

        /// <summary>绘制虚控制范围叠加（半透明三角形内部）</summary>
        private void DrawVirtualControlOverlay()
        {
            // 缓存计算结果（同一帧内不重复计算）
            int frame = Time.frameCount;
            if (_cachedVirtualControl == null || _cachedVirtualControlFrame != frame)
            {
                _cachedVirtualControl = VirtualControlCalculator.Calculate(
                    world.burgs, world.tiles, mapWidth, mapHeight);
                _cachedVirtualControlFrame = frame;
            }

            if (_cachedVirtualControl == null) return;

            // 虚控制地块叠加半透明颜色（按政权颜色）
            foreach (var kv in _cachedVirtualControl.virtualControlTiles)
            {
                int tileIdx = kv.Key;
                int realmId = kv.Value;
                if (tileIdx < 0 || tileIdx >= _pixelBuffer.Length) continue;
                // 已经是实际占领的地块不叠加（只叠加未占领的虚控制区）
                if (world.tiles[tileIdx].ownerRealmId == realmId) continue;

                Color realmColor = GetRealmColor(realmId);
                // 虚控制用更低的透明度（0.15），区分于实际占领
                _pixelBuffer[tileIdx] = Color.Lerp(_pixelBuffer[tileIdx], realmColor, 0.15f);
            }

            // 绘制控制点之间的连接线（虚控制线）
            foreach (var (a, b) in _cachedVirtualControl.connections)
            {
                if (!world.burgs.ContainsKey(a) || !world.burgs.ContainsKey(b)) continue;
                var burgA = world.burgs[a];
                var burgB = world.burgs[b];
                DrawLineOnTexture(
                    burgA.tileIndex % mapWidth, burgA.tileIndex / mapWidth,
                    burgB.tileIndex % mapWidth, burgB.tileIndex / mapWidth,
                    new Color(1f, 1f, 1f, 0.3f));
            }
        }

        /// <summary>绘制聚落辐射范围叠加（半透明圆形）</summary>
        private void DrawInfluenceRadiusOverlay()
        {
            if (world.burgs == null) return;

            foreach (var burg in world.burgs.Values)
            {
                if (!burg.IsMajorSettlement) continue;
                if (burg.tileIndex < 0 || burg.tileIndex >= _pixelBuffer.Length) continue;

                int bx = burg.tileIndex % mapWidth;
                int by = burg.tileIndex / mapWidth;
                int realmId = world.tiles[burg.tileIndex].ownerRealmId;
                if (realmId < 0) continue;

                // 辐射半径根据等级：Ⅰ=1, Ⅱ=2, Ⅲ=3, Ⅳ=4, Ⅴ=5
                int radius = (int)burg.settlementLevel;
                radius = Mathf.Clamp(radius, 1, 5);

                Color realmColor = GetRealmColor(realmId);
                for (int dy = -radius; dy <= radius; dy++)
                {
                    for (int dx = -radius; dx <= radius; dx++)
                    {
                        if (dx * dx + dy * dy > radius * radius) continue;
                        int px = bx + dx, py = by + dy;
                        if (px < 0 || px >= mapWidth || py < 0 || py >= mapHeight) continue;
                        int pi = py * mapWidth + px;
                        // 辐射范围用极低透明度（0.08），只做视觉暗示
                        float distFactor = 1f - (float)(dx * dx + dy * dy) / (radius * radius);
                        _pixelBuffer[pi] = Color.Lerp(_pixelBuffer[pi], realmColor, 0.08f * distFactor);
                    }
                }
            }
        }

        /// <summary>在纹理上画直线（Bresenham）</summary>
        private void DrawLineOnTexture(int x0, int y0, int x1, int y1, Color color)
        {
            int dx = Mathf.Abs(x1 - x0), dy = Mathf.Abs(y1 - y0);
            int sx = x0 < x1 ? 1 : -1, sy = y0 < y1 ? 1 : -1;
            int err = dx - dy;
            while (true)
            {
                if (x0 >= 0 && x0 < mapWidth && y0 >= 0 && y0 < mapHeight)
                {
                    int pi = y0 * mapWidth + x0;
                    if (pi >= 0 && pi < _pixelBuffer.Length)
                        _pixelBuffer[pi] = Color.Lerp(_pixelBuffer[pi], color, color.a);
                }
                if (x0 == x1 && y0 == y1) break;
                int e2 = 2 * err;
                if (e2 > -dy) { err -= dy; x0 += sx; }
                if (e2 < dx) { err += dx; y0 += sy; }
            }
        }

        #endregion

        public void ForceRefresh()
        {
            _forceMapRefresh = true;
        }

        /// <summary>获取当前地图纹理（用于导出PNG）</summary>
        public Texture2D GetMapTexture() => mapTexture;

    }
}
