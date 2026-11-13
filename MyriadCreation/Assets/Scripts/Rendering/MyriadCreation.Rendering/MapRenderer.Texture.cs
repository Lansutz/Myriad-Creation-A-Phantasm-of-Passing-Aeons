using System;
using System.Collections.Generic;
using UnityEngine;
using MyriadCreation.Core;
using MyriadCreation.Core.Constants;
using MyriadCreation.Core.Data;
using MyriadCreation.Core.Dto;
using MyriadCreation.Core.Enums;
using MyriadCreation.Core.Contracts;
using MyriadCreation.Content.Definitions;







namespace MyriadCreation.Rendering
{
 /// MapRenderer.Texture —— 纹理更新（地图纹理重绘/强制刷新/获取纹理）（partial class，与 MapRenderer.cs 共享字段）
    public partial class MapRenderer
    {

 /// <summary>更新地图纹理</summary>
                private void UpdateMapTexture()
        {
            if (mapWidth <= 0 || mapHeight <= 0) return;
            int pixelCount = mapWidth * mapHeight;

 // 地形模式：先算当前世界陆地高程 min/max，供调色板归一化（否则整图发白，见 GetTileColorRaw）
            if (displayMode == MapDisplayMode.Terrain)
            {
                float min = float.MaxValue, max = float.MinValue;
                for (int i = 0; i < pixelCount; i++)
                {
                    if (!TryGetTileView(i, out TileData tile) || !tile.exists || !tile.isLand) continue;
                    float e = tile.elevation01;
                    if (e < min) min = e;
                    if (e > max) max = e;
                }
                if (max - min < 0.05f) max = min + 0.05f;
                _landElevMin = min;
                _landElevMax = max;
            }

 // 复用像素缓冲区，仅在尺寸变化时重新分配
            if (_pixelBuffer == null || _pixelBuffer.Length != pixelCount)
                _pixelBuffer = new Color[pixelCount];

            for (int i = 0; i < pixelCount; i++)
            {
                if (!TryGetTileView(i, out TileData tile) || !tile.exists)
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

 // ===== 行国游牧活动范围渲染（政治地图模式下，半透明色块+虚线边界+移动王庭）=====
            if (displayMode == MapDisplayMode.Political && _layerConfig.showNomadicRange)
            {
                DrawNomadicRangeOverlay();
            }

 // ===== 占领/争议条纹叠加（45°斜线，不修改底色，仅叠加）=====
            for (int i = 0; i < pixelCount; i++)
            {
                if (!TryGetTileView(i, out TileData stripeTile) || !stripeTile.exists) continue;
                if (!IsOccupiedDisputed(stripeTile)) continue;
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
 // 视野 LOD：满图视野下有上万个聚落（实测 1024×512 生成 10077 个，其中 9482 个主聚落），
 // 全部画 3~7px 图标会把地图糊成白噪点。缩到全图时不画标记，拉近（≤45% 全图视野）才显示。
            bool showSettlementMarkers = _layerConfig.showSettlementMarkers
                && _zoomLevel <= _cameraFitSize * 0.45f;
            var settlementView = CurrentSettlementView;
            if (showSettlementMarkers && settlementView != null && settlementView.SettlementCount > 0)
            {
                foreach (var settlement in settlementView.GetMajorSettlements())
                {
                    if (settlement.tileIndex < 0 || settlement.tileIndex >= pixelCount) continue;
                    DrawSettlementIcon(settlement, pixelCount);
                }
            }

 // ===== 省份边界线（可选叠加层；地形模式已在地形色里做混色描边，避免重复叠加发白）=====
            var overlayView = CurrentOverlayView;
            if (_layerConfig.showProvinceBorders && displayMode != MapDisplayMode.Terrain
                && overlayView != null && overlayView.ProvinceCount > 0)
            {
                for (int i = 0; i < pixelCount; i++)
                {
                    TryGetTileView(i, out TileData provinceTile);
                    if (!provinceTile.exists) continue;
                    if (!IsProvinceBorder(i)) continue;
                    _pixelBuffer[i] = Color.Lerp(_pixelBuffer[i], provinceBorderColor, 0.35f);
                }
            }

 // ===== 行政区划层级边界（可选叠加层）=====
            if (_layerConfig.showAdminBorders)
                DrawAdminDivisionBorders();

 // ===== 网格线（可选叠加层）=====
            if (_layerConfig.showGrid)
            {
                int gridSpacing = Mathf.Max(8, mapWidth / 32); // 网格间距随地图尺寸调整
                for (int i = 0; i < pixelCount; i++)
                {
                    if (!TryGetTileView(i, out TileData gridTile) || !gridTile.exists) continue;
                    int x = i % mapWidth;
                    int y = i / mapWidth;
                    if (x % gridSpacing == 0 || y % gridSpacing == 0)
                    {
                        _pixelBuffer[i] = Color.Lerp(_pixelBuffer[i], new Color(0.5f, 0.5f, 0.5f, 0.3f), 0.3f);
                    }
                }
            }

            #if UNITY_EDITOR
 // 编辑器画笔预览（编辑模式下高亮鼠标悬停的画笔范围）
            if (_mapEditor != null && _mapEditor.IsEditMode && _hoverTile >= 0 && _mapEditor.IsPreviewToolActive)
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
            #endif
            mapTexture.SetPixels(_pixelBuffer);

            mapTexture.Apply();
        }


        #region 聚落与虚控制渲染辅助方法

 /// <summary>虚控制范围计算结果缓存（避免每帧重算）</summary>
        private MapVirtualControlOverlay _cachedVirtualControl;
        private int _cachedVirtualControlFrame = -1;

 /// <summary>绘制聚落独立图标（按等级大小+形态形状）</summary>
        private void DrawSettlementIcon(MapSettlementInfo settlement, int pixelCount)
        {
            int bx = settlement.tileIndex % mapWidth;
            int by = settlement.tileIndex / mapWidth;

 // 图标大小按等级（纹理像素）；注意 1 纹理像素 = 1 地块，
 // 旧值 2~6px 表示"占据 2~6 个地块"，上万聚落时几乎盖满地图 → 收紧到 1~3。
            int level = settlement.settlementLevel;
            int iconSize = Mathf.Clamp(level, 1, 3);

 // 图标颜色根据类型
            Color iconColor = settlement.siteMode switch
            {
                (int)SiteMode.SedentarySettlement when settlement.settlementForm == (int)SettlementForm.City
                    => new Color(1f, 0.85f, 0.2f, 1f),
                (int)SiteMode.SedentarySettlement => new Color(0.95f, 0.85f, 0.5f, 1f),
                (int)SiteMode.FortifiedOutpost => new Color(0.9f, 0.3f, 0.2f, 1f),
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
                    bool draw = settlement.siteMode switch
                    {
 // 据点：菱形/十字图标
                        (int)SiteMode.FortifiedOutpost => Mathf.Abs(dx) + Mathf.Abs(dy) <= half + 1,
 // 城市形态：方形图标
                        (int)SiteMode.SedentarySettlement when settlement.settlementForm == (int)SettlementForm.City => true,
 // 定居点与营地：圆形图标
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
        /// <summary>
        /// 行政层级边界：每个 tile 取最深的 AdminDivision（level 最大），
        /// 相邻 tile 属于不同最深区划时画线；层级越高线越细/越淡。
        /// </summary>
        private void DrawAdminDivisionBorders()
        {
            if (_pixelBuffer == null) return;
            var overlayView = CurrentOverlayView;
            if (overlayView == null || !overlayView.TryGetAdminDivisionOverlay(out MapAdminDivisionOverlay overlay)
                || overlay.divisionIdByTile == null || overlay.levelByTile == null) return;

            int n = _pixelBuffer.Length;
            int[] divisionAt = overlay.divisionIdByTile;
            byte[] levelAt = overlay.levelByTile;
            for (int i = 0; i < n; i++)
            {
                if (i >= divisionAt.Length || i >= levelAt.Length) break;
                if (levelAt[i] < 2) continue;
                if (!TryGetTileView(i, out TileData tile) || !tile.exists) continue;
                int x = i % mapWidth;
                int y = i / mapWidth;
                bool border = false;
                if (x > 0 && divisionAt[i - 1] >= 0 && divisionAt[i - 1] != divisionAt[i]) border = true;
                if (!border && x < mapWidth - 1 && divisionAt[i + 1] >= 0 && divisionAt[i + 1] != divisionAt[i]) border = true;
                if (!border && y > 0 && divisionAt[i - mapWidth] >= 0 && divisionAt[i - mapWidth] != divisionAt[i]) border = true;
                if (!border && y < mapHeight - 1 && divisionAt[i + mapWidth] >= 0 && divisionAt[i + mapWidth] != divisionAt[i]) border = true;
                if (!border) continue;

                float alpha = levelAt[i] >= 4 ? 0.35f : levelAt[i] == 3 ? 0.5f : 0.65f;
                var color = new Color(0.95f, 0.9f, 0.75f, alpha);
                _pixelBuffer[i] = Color.Lerp(_pixelBuffer[i], color, alpha);
            }
        }


        private void DrawVirtualControlOverlay()
        {
            int frame = Time.frameCount;
            if (_cachedVirtualControl == null || _cachedVirtualControlFrame != frame)
            {
                var overlayView = CurrentOverlayView;
                _cachedVirtualControl = null;
                if (overlayView != null && overlayView.TryGetVirtualControlOverlay(out MapVirtualControlOverlay overlay))
                {
                    _cachedVirtualControl = overlay;
                }
                _cachedVirtualControlFrame = frame;
            }
            if (_cachedVirtualControl == null) return;

            foreach (var kv in _cachedVirtualControl.realmByTile)
            {
                int tileIdx = kv.Key;
                int realmId = kv.Value;
                if (tileIdx < 0 || tileIdx >= _pixelBuffer.Length) continue;
                if (!TryGetTileView(tileIdx, out TileData tile) || tile.ownerRealmId == realmId) continue;
                Color realmColor = GetRealmColor(realmId);
                _pixelBuffer[tileIdx] = Color.Lerp(_pixelBuffer[tileIdx], realmColor, 0.15f);
            }

            var settlementView = CurrentSettlementView;
            for (int i = 0; i < _cachedVirtualControl.connectionFrom.Count; i++)
            {
                int a = _cachedVirtualControl.connectionFrom[i];
                int b = _cachedVirtualControl.connectionTo[i];
                if (settlementView == null
                    || !settlementView.TryGetSettlement(a, out MapSettlementInfo settlementA)
                    || !settlementView.TryGetSettlement(b, out MapSettlementInfo settlementB)) continue;
                DrawLineOnTexture(
                    settlementA.tileIndex % mapWidth, settlementA.tileIndex / mapWidth,
                    settlementB.tileIndex % mapWidth, settlementB.tileIndex / mapWidth,
                    new Color(1f, 1f, 1f, 0.3f));
            }
        }


 /// <summary>绘制聚落辐射范围叠加（半透明圆形）</summary>
        private MapInfluenceOverlay _cachedInfluenceField;
        private int _cachedInfluenceFrame = -1;

        /// <summary>
        /// 政权影响力并集：政治模式下只画“政权范围”，不画每个聚落一圈。
        /// 强度来自 SettlementInfluenceSystem 的地形/巡逻可达成本，alpha 随档位。
        /// </summary>
        private void DrawInfluenceRadiusOverlay()
        {
            int frame = Time.frameCount;
            if (_cachedInfluenceField == null || _cachedInfluenceFrame != frame)
            {
                var overlayView = CurrentOverlayView;
                _cachedInfluenceField = null;
                if (overlayView != null && overlayView.TryGetInfluenceOverlay(out MapInfluenceOverlay overlay))
                {
                    _cachedInfluenceField = overlay;
                }
                _cachedInfluenceFrame = frame;
            }
            if (_cachedInfluenceField == null) return;
            if (_cachedInfluenceField.tierByTile == null) return;

            var settlementView = CurrentSettlementView;
            if (_cachedInfluenceField.sourceAnchorByTile == null) return;
            for (int i = 0; i < _cachedInfluenceField.tierByTile.Length; i++)
            {
                int tier = _cachedInfluenceField.tierByTile[i];
                if (tier <= 0 || i >= _pixelBuffer.Length) continue;

                int source = i < _cachedInfluenceField.sourceAnchorByTile.Length ? _cachedInfluenceField.sourceAnchorByTile[i] : -1;
                if (!TryGetTileView(i, out TileData tile)) continue;
                int realmId = tile.ownerRealmId;
                if (realmId < 0 && source >= 0 && settlementView != null
                    && settlementView.TryGetSettlement(source, out MapSettlementInfo settlement)
                    && settlement.tileIndex >= 0 && settlement.tileIndex < mapWidth * mapHeight)
                {
                    if (TryGetTileView(settlement.tileIndex, out TileData st))
                    {
                        realmId = st.ownerRealmId;
                    }
                }
                if (realmId < 0) continue;

                float alpha = tier >= 4 ? 0.26f : tier == 3 ? 0.18f : tier == 2 ? 0.11f : 0.05f;
                _pixelBuffer[i] = Color.Lerp(_pixelBuffer[i], GetRealmColor(realmId), alpha);
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
