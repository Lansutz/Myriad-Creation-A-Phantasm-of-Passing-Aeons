using System.Collections.Generic;
using UnityEngine;
using CivilizationEvolution.Core;
using CivilizationEvolution.Politics;

namespace CivilizationEvolution.Render
{
    /// <summary>
    /// MapRenderer.Nomadic —— 行国（游牧政权）活动范围渲染。
    ///
    /// 视觉语言（与城国固定领土明确区分）：
    /// - 活动范围：政权色半透明色块，核心区较实、边缘按影响力衰减（不逐地块固定归属）
    /// - 外边界：虚线点（棋盘间隔），区别于城国实线边界
    /// - 核心区边界：一圈更密的浅色点，标示冬夏核心牧场
    /// - 移动王庭：centerTile 画毡帐/旗帜图标（菱形外框+中心高亮），随季节迁移
    /// </summary>
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
            if (world?.realms == null) return;

            // 第一遍：填充半透明活动范围
            foreach (var kvp in world.realms)
            {
                var realm = kvp.Value;
                if (realm.realmForm != RealmForm.Nomadic || realm.nomadicRange == null) continue;
                FillNomadicRange(realm);
            }

            // 第二遍：虚线边界（在填充之上描边，避免被相邻范围覆盖）
            foreach (var kvp in world.realms)
            {
                var realm = kvp.Value;
                if (realm.realmForm != RealmForm.Nomadic || realm.nomadicRange == null) continue;
                DrawNomadicBorder(realm);
            }

            // 第三遍：移动王庭图标（最上层）
            foreach (var kvp in world.realms)
            {
                var realm = kvp.Value;
                if (realm.realmForm != RealmForm.Nomadic || realm.nomadicRange == null) continue;
                DrawNomadicCourtMarker(realm);
            }
        }

        /// <summary>填充活动范围色块（核心实、边缘衰减）</summary>
        private void FillNomadicRange(RealmData realm)
        {
            var range = realm.nomadicRange;
            Color realmColor = GetRealmColor(realm.realmId);
            int cx = range.centerTile % mapWidth;
            int cy = range.centerTile / mapWidth;
            int outer = Mathf.CeilToInt(range.outerRadius);

            for (int dy = -outer; dy <= outer; dy++)
            {
                for (int dx = -outer; dx <= outer; dx++)
                {
                    int px = cx + dx, py = cy + dy;
                    if (py < 0 || py >= mapHeight) continue;
                    // 左右连通
                    if (world.config.wrapX) px = (px + mapWidth) % mapWidth;
                    else if (px < 0 || px >= mapWidth) continue;

                    int pi = py * mapWidth + px;
                    if (pi < 0 || pi >= _pixelBuffer.Length) continue;
                    if (!world.tiles[pi].exists) continue;

                    float influence = range.GetInfluenceAt(pi, mapWidth, mapHeight);
                    if (influence <= 0f) continue;

                    bool isCore = range.IsInCore(pi, mapWidth, mapHeight);
                    // 核心区用较高透明度，边缘按影响力线性衰减
                    float alpha = isCore
                        ? NOMAD_CORE_ALPHA * Mathf.Clamp01(influence + 0.35f)
                        : NOMAD_EDGE_ALPHA * influence;
                    _pixelBuffer[pi] = Color.Lerp(_pixelBuffer[pi], realmColor, Mathf.Clamp01(alpha));
                }
            }
        }

        /// <summary>绘制活动范围虚线边界（外缘深色虚线+核心缘浅色密点）</summary>
        private void DrawNomadicBorder(RealmData realm)
        {
            var range = realm.nomadicRange;
            int cx = range.centerTile % mapWidth;
            int cy = range.centerTile / mapWidth;
            int outer = Mathf.CeilToInt(range.outerRadius);
            int core = Mathf.CeilToInt(range.coreRadius);

            for (int dy = -outer; dy <= outer; dy++)
            {
                for (int dx = -outer; dx <= outer; dx++)
                {
                    int px = cx + dx, py = cy + dy;
                    if (py < 0 || py >= mapHeight) continue;
                    if (world.config.wrapX) px = (px + mapWidth) % mapWidth;
                    else if (px < 0 || px >= mapWidth) continue;

                    int pi = py * mapWidth + px;
                    if (pi < 0 || pi >= _pixelBuffer.Length || !world.tiles[pi].exists) continue;

                    int manhattan = ManhattanWrap(dx, dy);
                    // 外边界：位于外缘环上，棋盘间隔画虚线点
                    if (manhattan == Mathf.RoundToInt(range.outerRadius) && (px + py) % 2 == 0)
                    {
                        _pixelBuffer[pi] = Color.Lerp(_pixelBuffer[pi], NomadBorderColor, NomadBorderColor.a);
                    }
                    // 核心区边界：密一点的浅色环
                    else if (manhattan == core && (px + py) % 2 == 1)
                    {
                        bool inCore = range.IsInCore(pi, mapWidth, mapHeight);
                        if (inCore)
                            _pixelBuffer[pi] = Color.Lerp(_pixelBuffer[pi], NomadCoreBorderColor, NomadCoreBorderColor.a * 0.6f);
                    }
                }
            }
        }

        /// <summary>绘制移动王庭图标（菱形毡帐+中心高亮，3x3~4x4）</summary>
        private void DrawNomadicCourtMarker(RealmData realm)
        {
            int center = realm.nomadicRange.centerTile;
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
                    if (world.config.wrapX) px = (px + mapWidth) % mapWidth;
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
    }
}
