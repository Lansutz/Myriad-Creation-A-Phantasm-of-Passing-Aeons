using System;
using System.Collections.Generic;
using UnityEngine;
using CivilizationEvolution.Core;
using CivilizationEvolution.Core.Constants;
using CivilizationEvolution.Core.Data;
using CivilizationEvolution.Core.Dto;
using CivilizationEvolution.Core.Enums;
using CivilizationEvolution.Simulation.Culture;
using CivilizationEvolution.Simulation.Diplomacy;
using CivilizationEvolution.Simulation.Economy;
using CivilizationEvolution.Simulation.Events;
using CivilizationEvolution.Simulation.Generation;
using CivilizationEvolution.Simulation.Modding;
using CivilizationEvolution.Simulation.Politics;
using CivilizationEvolution.Simulation.Population;
using CivilizationEvolution.Simulation.Religion;
using CivilizationEvolution.Simulation.Settlement;
using CivilizationEvolution.Simulation.Society;
using CivilizationEvolution.Simulation.WorldState;
using CivilizationEvolution.World.Generation;
using CivilizationEvolution.World.Hydrology;
using CivilizationEvolution.World.Settlement;
using CivilizationEvolution.World.Terrain;







namespace CivilizationEvolution.Rendering
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
            ref TileData tile = ref world.tiles[tileIndex];
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
            bool wrapX = world != null && world.config.wrapX;
            bool wrapY = world != null && world.config.wrapY;

 // 邻域高程（边界 clamp/wrap）
            float Get(int tx, int ty)
            {
                if (wrapX) tx = (tx + mapWidth) % mapWidth;
                else tx = Mathf.Clamp(tx, 0, mapWidth - 1);
                if (wrapY) ty = (ty + mapHeight) % mapHeight;
                else ty = Mathf.Clamp(ty, 0, mapHeight - 1);
                int idx = ty * mapWidth + tx;
                var ts = world.tiles;
                return idx >= 0 && idx < ts.Length && ts[idx].exists
                    ? ts[idx].elevation01 : 0f;
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
            var ts = world.tiles;
            if (tileIndex >= 0 && tileIndex < ts.Length)
                slope = ts[tileIndex].slopeDegree;
            float ink = 1f - Mathf.Clamp01((slope - 12f) / 55f) * 0.22f; // 缓坡 1.0→陡坡 0.78

            float mul = light * ink;
            c.r = Mathf.Clamp01(c.r * mul);
            c.g = Mathf.Clamp01(c.g * mul);
            c.b = Mathf.Clamp01(c.b * mul);
        }


 /// <summary>地块基色（模式分派——biome/政权/人口/宗教……）</summary>
        private Color GetTileColorRaw(int tileIndex)
        {
            ref TileData tile = ref world.tiles[tileIndex];

            switch (displayMode)
            {
                case MapDisplayMode.Terrain:
 // 河流优先着色（水系蓝）
                    if (tile.isRiver) return new Color(0.25f, 0.45f, 0.85f, 1f);
 // 省界描边（与任一邻域省份不同 → 边界色）
                    if (IsProvinceBorder(tileIndex))
                        return provinceBorderColor;
                    int terrainIndex = Mathf.Clamp(Mathf.RoundToInt((tile.elevation01 + 1f) / 2f * 255f), 0, 255);
                    return _terrainColors[terrainIndex];

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

                case MapDisplayMode.Economy:
                    float devT = Mathf.Clamp(tile.development, 0f, 1f);
                    return Color.Lerp(new Color(0.5f, 0.5f, 0.5f), new Color(1f, 0.9f, 0.3f), devT);

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
            int player = world != null ? world.PlayerRealmId : -1;
            if (player < 0) return _politicalColors[owner % _politicalColors.Length];

            var dm = world.GetDiplomacyManager();
            if (dm == null) return _politicalColors[owner % _politicalColors.Length];
            if (owner == player) return FriendlyColor;

            var rel = dm.GetRelation(player, owner);
            if (rel == null) return NeutralColor;
            if (rel.isAtWar) return WarColor;
            if (rel.IsHostile) return HostileColor;
            if (rel.activeAlliances.Exists(a => a.isActive)) return AllyColor;
            return rel.relation >= 20f ? FriendlyColor : NeutralColor;
        }


 /// <summary>联盟阵营色（玩家盟友/阵营成员）</summary>
        private Color GetAllianceColor(TileData tile)
        {
            int owner = tile.ownerRealmId;
            if (owner < 0) return new Color(0.25f, 0.25f, 0.25f);
            int player = world != null ? world.PlayerRealmId : -1;
            if (player < 0) return _politicalColors[owner % _politicalColors.Length];
            if (owner == player) return FriendlyColor;

            var dm = world.GetDiplomacyManager();
            if (dm == null) return NeutralColor;
            var rel = dm.GetRelation(player, owner);
            if (rel == null) return NeutralColor;
            if (rel.activeAlliances.Exists(a => a.isActive && a.type == AllianceType.Faction))
                return FactionColor;
            if (rel.activeAlliances.Exists(a => a.isActive))
                return AllyColor;
            return NeutralColor;
        }


 /// <summary>文化色（branch=true 时允许分支的文化显示子文化色）</summary>
        private Color GetCultureColor(TileData tile, bool branch)
        {
            int cultureId = GetDominantBlockCulture(tile);
            if (cultureId < 0 || world == null) return NeutralColor;
            if (!world.cultures.TryGetValue(cultureId, out var culture)) return NeutralColor;

            if (branch)
            {
 // 分支文化：子文化（parentCultureId>=0）且父文化允许分支 → 子文化色
                if (culture.parentCultureId >= 0
                    && world.cultures.TryGetValue(culture.parentCultureId, out var parent)
                    && parent.allowsBranching)
                    return culture.color;
                return NeutralColor;
            }

 // 主文化：分支文化显示根文化色（同一谱系同色）
            if (culture.parentCultureId >= 0
                && world.cultures.TryGetValue(culture.parentCultureId, out var root))
                return root.color;
            return culture.color;
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
            ref TileData tile = ref world.tiles[tileIndex];
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
            int viewer = _dualSpaceViewRealmId >= 0 ? _dualSpaceViewRealmId
                : (world != null ? world.PlayerRealmId : -1);
            int owner = tile.ownerRealmId;
            if (viewer < 0 || owner < 0) return GameEnums.SovereigntyStatus.Foreign;
            if (owner == viewer) return GameEnums.SovereigntyStatus.Core;

            var dm = world != null ? world.Diplomacy : null;
            if (dm != null)
            {
                var sub = dm.GetSubordination(viewer, owner);
                if (sub != null) return GameEnums.SovereigntyStatus.VassalTribute;
            }
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
