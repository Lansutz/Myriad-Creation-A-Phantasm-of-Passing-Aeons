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
 /// MapRenderer.Colors —— 颜色计算（政权/地形/外交/同盟/文化/宗教颜色 + 地形着色）（partial class，与 MapRenderer.cs 共享字段）
    public partial class MapRenderer
    {

 /// 政权色（动态扩展：0-15 用原色板——>15 黄金角 HSL 哈希——
 /// 政权数任意增长不撞色——确定性）
        private Color GetRealmColor(int realmId)
        {
            if (realmId >= 0 && realmId < 16)
                return _politicalColors[realmId];
            float hue = (realmId * 0.6180339887f) % 1f;
            float sat = 0.55f + ((realmId >> 3) % 3) * 0.1f;
            float val = 0.62f + ((realmId >> 5) % 3) * 0.08f;
            return Color.HSVToRGB(hue, sat, val);
        }


 /// 地块颜色（大陆底图渲染——2026-09 方向：大陆有纸纹/笔触
 /// 质感[参照《地图上发生的事》terrain 分色+纸感]）：
 /// 基色 + 确定性纸纹调制——低频斑驳[地形性色带]+高频颗粒[笔触感]——
 /// 海洋保持平滑（水无纸纹）——modes 也带纸感[政权色如彩绘]
        private Color GetTileColor(int tileIndex)
        {
            if (!TryGetTileView(tileIndex, out TileData tile)) return VoidColor;
            Color c = GetTileColorRaw(tileIndex);

 // 纸纹调制（确定性——同 seed 同画面——hash 高频颗粒 + 低频斑驳）
            if (tile.exists && tile.isLand)
            {
                int x = tileIndex % mapWidth;
                int y = tileIndex / mapWidth;
 // 山体阴影+坡度笔触（地形类模式统一——系统性：Terrain/Biome/ // Climate 都过——纸张与地形有机结合——算法见下）
                if (displayMode == MapDisplayMode.Terrain
                    || displayMode == MapDisplayMode.Biome
                    || displayMode == MapDisplayMode.Climate)
                {
                    ApplyTerrainShading(ref c, tileIndex);
                }
                float h1 = HashNoise(x, y, 0);         // 高频颗粒（笔触）
                float h2 = HashNoise(x / 4, y / 4, 1); // 低频斑驳（色带）
                float grain = (h1 - 0.5f) * 0.05f + (h2 - 0.5f) * 0.08f;
                c.r = Mathf.Clamp01(c.r + grain);
                c.g = Mathf.Clamp01(c.g + grain);
                c.b = Mathf.Clamp01(c.b + grain * 0.7f);
            }
            return c;
        }


 /// 山体阴影+坡度笔触（NPR 风格化——纸与地形结合）：
 /// ① Hillshade：邻域高程梯度 × 固定光源（西北 45°）——朝光面提亮/
 /// 背光面压暗——山脉立体隆起
 /// ② 坡度笔触：slopeDegree 越高越暗（陡坡=浓重笔触——手绘阴影感——
 /// 缓坡留白纸面呼吸）
        private void ApplyTerrainShading(ref Color c, int tileIndex)
        {
            int x = tileIndex % mapWidth;
            int y = tileIndex / mapWidth;
            bool wrapX = ViewWrapX;
            bool wrapY = ViewWrapY;

 // 邻域高程（边界 clamp/wrap）
            float Get(int tx, int ty)
            {
                if (wrapX) tx = (tx + mapWidth) % mapWidth;
                else tx = Mathf.Clamp(tx, 0, mapWidth - 1);
                if (wrapY) ty = (ty + mapHeight) % mapHeight;
                else ty = Mathf.Clamp(ty, 0, mapHeight - 1);
                if (!TryGetTileView(ty * mapWidth + tx, out TileData ts))
                {
                    return 0f;
                }
                return ts.exists ? ts.elevation01 : 0f;
            }
            float eW = Get(x - 1, y), eE = Get(x + 1, y);
            float eN = Get(x, y - 1), eS = Get(x, y + 1);
            float dx = eE - eW;   // 东升为正
            float dy = eS - eN;   // 南升为正

 // 光源西北 45°（光照方向向量——东南面受光）
            float shade = (dx + dy) * 0.5f; // dot 简化（光 -1,-1 归一后的等效）
 // 海陆交接（低海拔近海）不平——只调陆地幅度
            float light = 1f + Mathf.Clamp(shade * 0.9f, -0.16f, 0.14f);

 // 坡度笔触（陡坡加重——slopeDegree 在手绘地图≈笔触密度）
            float slope = 0f;
            if (TryGetTileView(tileIndex, out TileData ts))
            {
                slope = ts.slopeDegree;
            }
            float ink = 1f - Mathf.Clamp01((slope - 12f) / 55f) * 0.22f; // 缓坡 1.0→陡坡 0.78

            float mul = light * ink;
            c.r = Mathf.Clamp01(c.r * mul);
            c.g = Mathf.Clamp01(c.g * mul);
            c.b = Mathf.Clamp01(c.b * mul);

            // 等高线：与西/北邻高程跨过同一间隔才落线，线宽保持 1px，避免糊成色带。
            if (_layerConfig != null && _layerConfig.showContours && TryGetTileView(tileIndex, out TileData current))
            {
                float interval = Mathf.Max(0.01f, _layerConfig.contourInterval);
                float e = current.elevation01;
                if (TerrainContour.ShouldDraw(e, Get(x - 1, y), Get(x, y - 1), interval))
                {
                    const float contourInk = 0.86f;
                    c.r = Mathf.Clamp01(c.r * contourInk);
                    c.g = Mathf.Clamp01(c.g * contourInk);
                    c.b = Mathf.Clamp01(c.b * contourInk);
                }
            }
        }


 /// <summary>地块基色（模式分派——biome/政权/人口/宗教……）</summary>
        private Color GetTileColorRaw(int tileIndex)
        {
            if (!TryGetTileView(tileIndex, out TileData tile)) return Color.gray;

            switch (displayMode)
            {
                case MapDisplayMode.Terrain:
 // 河流优先着色（水系蓝）
                    if (tile.isRiver) return new Color(0.25f, 0.45f, 0.85f, 1f);
                    Color terrainColor;
                    if (tile.isLand)
                    {
                        // 陆地按本世界实际高程范围归一化到调色板"低地→山地"段（0.45~0.95）。
                        // 上限 0.95：只有最高 10% 接近雪线，避免高程噪声把地块打成高频白点。
                        float tLand = Mathf.InverseLerp(_landElevMin, _landElevMax, tile.elevation01);
                        int landIndex = Mathf.Clamp(Mathf.RoundToInt(Mathf.Lerp(0.45f, 0.95f, tLand) * 255f), 0, 255);
                        Color land = _terrainColors[landIndex];

                        // 低地干湿差异：降水少 / 肥力低 → 偏干草原黄；否则保留草绿。
                        if (tLand < 0.62f)
                        {
                            float wet = Mathf.Clamp01(tile.annualPrecipMm / 900f);
                            float barren = 1f - Mathf.Clamp01(tile.fertility);
                            var dryColor = new Color(0.63f, 0.58f, 0.35f);
                            land = Color.Lerp(land, dryColor, Mathf.Clamp01((1f - wet) * 0.45f + barren * 0.25f));
                        }

                        // 寒冷地区雪线：年均温越低，高处越早覆雪（不只靠绝对高程）。
                        if (tile.annualTemp < -8f)
                        {
                            float snow = Mathf.Clamp01((-8f - tile.annualTemp) / 18f) * Mathf.Clamp01(tLand * 1.2f);
                            land = Color.Lerp(land, new Color(0.93f, 0.95f, 0.97f), Mathf.Clamp01(snow * 0.9f));
                        }
                        terrainColor = land;
                    }
                    else
                    {
                        // 水域：绝对高程 [-1,0] 映射到调色板水段 [0,0.44]（旧公式到 0.5，
                        // 近海 e≈-0.03 会落进陆地草绿段，海面出现绿色斑块）。
                        float tSea = Mathf.InverseLerp(-1f, 0f, tile.elevation01) * 0.44f;
                        int terrainIndex = Mathf.Clamp(Mathf.RoundToInt(tSea * 255f), 0, 255);
                        terrainColor = _terrainColors[terrainIndex];
                    }
 // 省界：只做**混色描边**，不能整格填 0.9 白——小省（8 格/省）时 87% 陆地都是边界，
 // 整格填充会把地图变成白噪点。是否显示仍受图层开关控制。
                    if (_layerConfig.showProvinceBorders && IsProvinceBorder(tileIndex))
                        return Color.Lerp(terrainColor, provinceBorderColor, 0.35f);
                    return terrainColor;

                case MapDisplayMode.Climate:
                    int climateIndex = Mathf.Clamp(Mathf.RoundToInt((tile.annualTemp + 55f) / 90f * 255f), 0, 255);
                    return _climateColors[climateIndex];

                case MapDisplayMode.Biome:
                    int biomeIndex = (int)tile.biome;
                    if (biomeIndex >= 0 && biomeIndex < _biomeColors.Length)
                        return _biomeColors[biomeIndex];
                    return Color.gray;

                case MapDisplayMode.Political:
                    return GetPoliticalDualSpaceColor(tileIndex);

                case MapDisplayMode.Population:
                    float pop = 0f;
                    if (tile.populationBlocks != null)
                        foreach (var pb in tile.populationBlocks)
                            pop += pb.count;
                    float popT = Mathf.Clamp(pop / 100f, 0f, 1f);
                    return Color.Lerp(new Color(0.9f, 0.9f, 0.9f), new Color(0.8f, 0.2f, 0.2f), popT);

                case MapDisplayMode.Diplomacy:
                    return GetDiplomacyColor(tile);

                case MapDisplayMode.Alliance:
                    return GetAllianceColor(tile);

                case MapDisplayMode.Culture:
                    return GetCultureColor(tile, false);

                case MapDisplayMode.CultureBranch:
                    return GetCultureColor(tile, true);

                case MapDisplayMode.Religion:
                    return GetReligionColor(tile, ReligionMapLevel.Religion);

                case MapDisplayMode.ReligionSuccession:
                    return GetReligionColor(tile, ReligionMapLevel.Succession);

                case MapDisplayMode.ReligionTradition:
                    return GetReligionColor(tile, ReligionMapLevel.Tradition);

                default:
                    return Color.gray;
            }
        }


 /// <summary>外交关系色（玩家视角：战争/敌对/盟约/友好/中立）</summary>
        private Color GetDiplomacyColor(TileData tile)
        {
            int owner = tile.ownerRealmId;
            if (owner < 0) return new Color(0.25f, 0.25f, 0.25f);
            int player = ViewPlayerRealmId;
            if (player < 0) return _politicalColors[owner % _politicalColors.Length];
            if (!HasDiplomacyManagerView) return _politicalColors[owner % _politicalColors.Length];
            if (owner == player) return FriendlyColor;
            if (!TryGetDiplomacyView(player, owner, out MapDiplomacyView rel)) return NeutralColor;
            if (rel.isAtWar) return WarColor;
            if (rel.isHostile) return HostileColor;
            if (rel.hasActiveAlliance) return AllyColor;
            return rel.relation >= 20f ? FriendlyColor : NeutralColor;
        }


 /// <summary>联盟阵营色（玩家盟友/阵营成员）</summary>
        private Color GetAllianceColor(TileData tile)
        {
            int owner = tile.ownerRealmId;
            if (owner < 0) return new Color(0.25f, 0.25f, 0.25f);
            int player = ViewPlayerRealmId;
            if (player < 0) return _politicalColors[owner % _politicalColors.Length];
            if (owner == player) return FriendlyColor;
            if (!TryGetDiplomacyView(player, owner, out MapDiplomacyView rel)) return NeutralColor;
            if (rel.hasFactionAlliance) return FactionColor;
            if (rel.hasActiveAlliance) return AllyColor;
            return NeutralColor;
        }


 /// <summary>文化色（branch=true 时允许分支的文化显示子文化色）</summary>
        private Color GetCultureColor(TileData tile, bool branch)
        {
            int cultureId = GetDominantBlockCulture(tile);
            if (cultureId < 0) return NeutralColor;
            if (!TryGetCultureView(cultureId, out MapCultureView culture)) return NeutralColor;

            if (branch)
            {
 // 分支文化：子文化（parentCultureId>=0）且父文化允许分支 → 子文化色
                if (culture.parentCultureId >= 0
                    && TryGetCultureView(culture.parentCultureId, out MapCultureView parent)
                    && parent.allowsBranching)
                    return ToColor(culture);
                return NeutralColor;
            }

 // 主文化：分支文化显示根文化色（同一谱系同色）
            if (culture.parentCultureId >= 0
                && TryGetCultureView(culture.parentCultureId, out MapCultureView root))
                return ToColor(root);
            return ToColor(culture);
        }

        private static Color ToColor(MapCultureView view)
        {
            return new Color(view.colorR, view.colorG, view.colorB, view.colorA);
        }


 /// <summary>宗教色（三级谱系：宗教/宗派/传统）</summary>
        private Color GetReligionColor(TileData tile, ReligionMapLevel level)
        {
            int faithId = GetDominantBlockFaith(tile);
            if (faithId < 0) return NeutralColor;
            return ReligionCatalog.GetColor(faithId, level);
        }

 // ===== 双色空间政治地图渲染（效忠树层级 vs 附庸朝贡，色相完全隔离）=====
 /// 双色空间政治地图颜色：
 /// core（本国本土）→ A色系按效忠树深度选色（深蓝→浅蓝）
 /// vassal_tribute（附庸/朝贡）→ B色（灰青色，色相与A色系隔离）
 /// foreign（外国）→ 外国政权色
 /// 占领/争议条纹在 UpdateMapTexture 像素层叠加（不修改底色）
        private Color GetPoliticalDualSpaceColor(int tileIndex)
        {
            if (!TryGetTileView(tileIndex, out TileData tile)) return _politicalColors[0];
            if (!tile.exists || !tile.isLand) return _politicalColors[0];

            GameEnums.SovereigntyStatus status = GetSovereigntyStatus(tile);
            switch (status)
            {
                case GameEnums.SovereigntyStatus.Core:
                    int depth = GetAllegianceDepth(tile);
                    int idx = Mathf.Clamp(depth, 0, _allegiancePalette.Length - 1);
                    return _allegiancePalette[idx];
                case GameEnums.SovereigntyStatus.VassalTribute:
                    return _vassalTributeColor;
                default:
                    int owner = tile.ownerRealmId;
                    if (owner >= 0 && owner < _politicalColors.Length)
                        return _politicalColors[owner];
                    return GetRealmColor(owner);
            }
        }

 /// 判断地块主权状态（相对于查看政权）：
 /// owner == 查看政权 → Core（本国本土）
 /// owner 是查看政权的附庸/朝贡 → VassalTribute
 /// 其他 → Foreign
        private GameEnums.SovereigntyStatus GetSovereigntyStatus(TileData tile)
        {
            int viewer = _dualSpaceViewRealmId >= 0 ? _dualSpaceViewRealmId : ViewPlayerRealmId;
            int owner = tile.ownerRealmId;
            if (viewer < 0 || owner < 0) return GameEnums.SovereigntyStatus.Foreign;
            if (owner == viewer) return GameEnums.SovereigntyStatus.Core;
            if (IsSubordinateView(viewer, owner)) return GameEnums.SovereigntyStatus.VassalTribute;
            return GameEnums.SovereigntyStatus.Foreign;
        }

 /// 效忠树深度（仅 Core 地块有效）。
 /// 暂时返回0（全部主圈色）——后续政治系统完善效忠树后，
 /// 在此处根据领地的封臣层级计算深度0/1/2/3。
        private int GetAllegianceDepth(TileData tile)
        {
 // TODO: 对接政治效忠树系统，根据封臣层级返回深度0-3
            return 0;
        }

 /// <summary>是否占领/争议（占领方≠所有者）</summary>
        private bool IsOccupiedDisputed(TileData tile)
        {
            return tile.occupyingRealmId >= 0
                && tile.occupyingRealmId != tile.ownerRealmId;
        }

 /// <summary>设置双色空间查看政权（外部调用，如点选地块时跟随）</summary>
        public void SetDualSpaceViewRealm(int realmId)
        {
            _dualSpaceViewRealmId = realmId;
            _forceMapRefresh = true;
        }

    }
}
