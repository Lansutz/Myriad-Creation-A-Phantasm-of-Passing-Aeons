using System.Collections.Generic;
using UnityEngine;
using MyriadCreation.Core.Contracts;
using MyriadCreation.Core.Data;
using MyriadCreation.Core.Enums;

namespace MyriadCreation.Rendering
{
    public partial class MapRenderer
    {
        // 核心区/边缘区叠加透明度
        private const float NOMAD_CORE_ALPHA = 0.26f;
        private const float NOMAD_EDGE_ALPHA = 0.10f;
        // 虚线边界颜色（深色描点）
        private static readonly Color NomadBorderColor = new Color(0.12f, 0.10f, 0.08f, 0.85f);
        private static readonly Color NomadCoreBorderColor = new Color(1f, 0.95f, 0.7f, 0.7f);
        // 王庭图标色
        private static readonly Color NomadCourtColor = new Color(0.95f, 0.78f, 0.25f, 1f);

        /// <summary>绘制全部行国的活动范围（政治地图模式下调用）</summary>
        private void DrawNomadicRangeOverlay()
        {
            var realmView = CurrentRealmView;
            if (realmView == null) return;
            var realms = realmView.GetNomadicRealms();
            if (realms == null || realms.Count == 0) return;

            // 第一遍：填充半透明活动范围
            foreach (var realm in realms)
            {
                FillNomadicRange(realm);
            }

            // 第二遍：虚线边界（在填充之上描边，避免被相邻范围覆盖）
            foreach (var realm in realms)
            {
                DrawNomadicBorder(realm);
            }

            // 第三遍：移动王庭图标（最上层）
            foreach (var realm in realms)
            {
                DrawNomadicCourtMarker(realm);
            }
        }

        /// <summary>填充活动范围色块（核心实、边缘衰减）</summary>
        private void FillNomadicRange(MapRealmInfo realm)
        {
            Color realmColor = GetRealmColor(realm.realmId);
            int cx = realm.centerTile % mapWidth;
            int cy = realm.centerTile / mapWidth;
            int outer = Mathf.CeilToInt(realm.outerRadius);

            for (int dy = -outer; dy <= outer; dy++)
            {
                for (int dx = -outer; dx <= outer; dx++)
                {
                    int px = cx + dx, py = cy + dy;
                    if (py < 0 || py >= mapHeight) continue;
                    // 左右连通
                    if (ViewWrapX) px = (px + mapWidth) % mapWidth;
                    else if (px < 0 || px >= mapWidth) continue;

                    int pi = py * mapWidth + px;
                    if (pi < 0 || pi >= _pixelBuffer.Length) continue;
                    if (!TryGetTileView(pi, out TileData tile) || !tile.exists) continue;

                    float influence = NomadInfluenceAt(realm, pi);
                    if (influence <= 0f) continue;

                    bool isCore = NomadIsInCore(realm, pi);
                    // 核心区用较高透明度，边缘按影响力线性衰减
                    float alpha = isCore
                        ? NOMAD_CORE_ALPHA * Mathf.Clamp01(influence + 0.35f)
                        : NOMAD_EDGE_ALPHA * influence;
                    _pixelBuffer[pi] = Color.Lerp(_pixelBuffer[pi], realmColor, Mathf.Clamp01(alpha));
                }
            }
        }

        /// <summary>绘制活动范围虚线边界（外缘深色虚线+核心缘浅色密点）</summary>
        private void DrawNomadicBorder(MapRealmInfo realm)
        {
            int cx = realm.centerTile % mapWidth;
            int cy = realm.centerTile / mapWidth;
            int outer = Mathf.CeilToInt(realm.outerRadius);
            int core = Mathf.CeilToInt(realm.coreRadius);

            for (int dy = -outer; dy <= outer; dy++)
            {
                for (int dx = -outer; dx <= outer; dx++)
                {
                    int px = cx + dx, py = cy + dy;
                    if (py < 0 || py >= mapHeight) continue;
                    if (ViewWrapX) px = (px + mapWidth) % mapWidth;
                    else if (px < 0 || px >= mapWidth) continue;

                    int pi = py * mapWidth + px;
                    if (pi < 0 || pi >= _pixelBuffer.Length) continue;
                    if (!TryGetTileView(pi, out TileData tile) || !tile.exists) continue;

                    int manhattan = ManhattanWrap(dx, dy);
                    // 外边界：位于外缘环上，棋盘间隔画虚线点
                    if (manhattan == Mathf.RoundToInt(realm.outerRadius) && (px + py) % 2 == 0)
                    {
                        _pixelBuffer[pi] = Color.Lerp(_pixelBuffer[pi], NomadBorderColor, NomadBorderColor.a);
                    }
                    // 核心区边界：密一点的浅色环
                    else if (manhattan == core && (px + py) % 2 == 1)
                    {
                        if (NomadIsInCore(realm, pi))
                            _pixelBuffer[pi] = Color.Lerp(_pixelBuffer[pi], NomadCoreBorderColor, NomadCoreBorderColor.a * 0.6f);
                    }
                }
            }
        }

        /// <summary>绘制移动王庭图标（菱形毡帐+中心高亮，3x3~4x4）</summary>
        private void DrawNomadicCourtMarker(MapRealmInfo realm)
        {
            int center = realm.centerTile;
            if (center < 0 || center >= _pixelBuffer.Length) return;
            int bx = center % mapWidth;
            int by = center / mapWidth;
            const int half = 2; // 5x5 图标

            for (int dy = -half; dy <= half; dy++)
            {
                for (int dx = -half; dx <= half; dx++)
                {
                    int manhattan = Mathf.Abs(dx) + Mathf.Abs(dy);
                    // 菱形外框
                    bool diamondEdge = manhattan == half;
                    // 内部中心高亮（3x3 内核）
                    bool inner = manhattan <= 1;
                    if (!diamondEdge && !inner) continue;

                    int px = bx + dx, py = by + dy;
                    if (py < 0 || py >= mapHeight) continue;
                    if (ViewWrapX) px = (px + mapWidth) % mapWidth;
                    else if (px < 0 || px >= mapWidth) continue;

                    int pi = py * mapWidth + px;
                    if (pi < 0 || pi >= _pixelBuffer.Length) continue;

                    if (inner)
                        _pixelBuffer[pi] = NomadCourtColor;                       // 中心亮金
                    else
                        _pixelBuffer[pi] = Color.Lerp(NomadCourtColor, Color.black, 0.55f); // 菱形深色边
                }
            }
        }

        /// <summary>曼哈顿距离（左右连通下 dx 已由调用方处理，这里仅绝对值和）</summary>
        private int ManhattanWrap(int dx, int dy) => Mathf.Abs(dx) + Mathf.Abs(dy);

        // ===== 行国范围数学（与旧 NomadicRange 公式一致）=====

        private float NomadDistanceToCenter(MapRealmInfo realm, int tileIndex)
        {
            int cx = realm.centerTile % mapWidth;
            int cy = realm.centerTile / mapWidth;
            int tx = tileIndex % mapWidth;
            int ty = tileIndex / mapWidth;
            int dx = Mathf.Abs(tx - cx);
            dx = Mathf.Min(dx, mapWidth - dx);
            int dy = Mathf.Abs(ty - cy);
            return dx + dy;
        }

        private bool NomadIsInCore(MapRealmInfo realm, int tileIndex)
        {
            return NomadDistanceToCenter(realm, tileIndex) <= realm.coreRadius;
        }

        private float NomadInfluenceAt(MapRealmInfo realm, int tileIndex)
        {
            float distance = NomadDistanceToCenter(realm, tileIndex);
            if (distance > realm.outerRadius) return 0f;
            if (distance <= realm.coreRadius) return realm.influence / 100f;
            float t = 1f - (distance - realm.coreRadius) / (realm.outerRadius - realm.coreRadius);
            return realm.influence / 100f * t;
        }
    }
}
