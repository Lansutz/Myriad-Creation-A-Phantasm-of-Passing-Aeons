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

            // ===== 子地块/Burg 标记绘制（可选叠加层，根据开关）=====
            if (_layerConfig.showBurgMarkers && world.burgs != null && world.burgs.Count > 0)
            {
                foreach (var burg in world.burgs.Values)
                {
                    // 村庄太小不显示，只显示主要定居点
                    if (!burg.IsMajorSettlement) continue;
                    if (burg.tileIndex < 0 || burg.tileIndex >= pixelCount) continue;

                    Color burgColor = burg.type switch
                    {
                        BurgType.Capital => new Color(1f, 0.85f, 0.2f, 1f),   // 金色：首都
                        BurgType.City => new Color(1f, 1f, 1f, 1f),              // 白色：城市
                        BurgType.Port => new Color(0.3f, 0.6f, 1f, 1f),          // 蓝色：港口
                        BurgType.Fortress => new Color(0.9f, 0.3f, 0.2f, 1f),    // 红色：要塞
                        BurgType.Town => new Color(0.9f, 0.8f, 0.4f, 1f),        // 黄色：集镇
                        _ => new Color(0.6f, 0.6f, 0.6f, 1f)                      // 灰色
                    };

                    // 在Burg所在地块画 2x2 像素标记（大地图画3x3）
                    int bx = burg.tileIndex % mapWidth;
                    int by = burg.tileIndex / mapWidth;
                    int markSize = mapWidth > 512 ? 2 : 1;
                    for (int dy = 0; dy < markSize; dy++)
                    {
                        for (int dx = 0; dx < markSize; dx++)
                        {
                            int px = bx + dx;
                            int py = by + dy;
                            if (px >= 0 && px < mapWidth && py >= 0 && py < mapHeight)
                            {
                                int pi = py * mapWidth + px;
                                _pixelBuffer[pi] = burgColor;
                            }
                        }
                    }
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


        public void ForceRefresh()
        {
            _forceMapRefresh = true;
        }

        /// <summary>获取当前地图纹理（用于导出PNG）</summary>
        public Texture2D GetMapTexture() => mapTexture;

    }
}
