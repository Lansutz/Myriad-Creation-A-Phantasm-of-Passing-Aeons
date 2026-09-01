using System.Collections.Generic;
using UnityEngine;
using CivilizationEvolution.Core;
using CivilizationEvolution.Economy;

namespace CivilizationEvolution.Politics
{
    /// <summary>
    /// 人口承载系统：地块人口上限（地理/仓储/贸易修正）——约束人口增长
    /// 与军事人力（ManpowerSystem）为两个独立概念
    /// </summary>
    public static class CarryingCapacitySystem
    {
        /// <summary>地形/群系基础承载系数（人口块单位——count；学术群系按承载能力分级）</summary>
        public static float GetTerrainMultiplier(TileData tile)
        {
            if (tile.isCoast && tile.biome != GameEnums.BiomeType.Fjord) return 1.1f;
            if (tile.isRiver) return 1.2f;

            switch (tile.biome)
            {
                // 顶级（冲积沃野与绿洲）
                case GameEnums.BiomeType.AlluvialPlain:
                case GameEnums.BiomeType.GreatRiverPlain:
                case GameEnums.BiomeType.Delta:
                case GameEnums.BiomeType.PluvialFan:
                case GameEnums.BiomeType.DesertOasis:
                case GameEnums.BiomeType.EndorheicLake: return 1.2f;
                // 高（宜耕宜牧）
                case GameEnums.BiomeType.Interfluvial:
                case GameEnums.BiomeType.SedimentaryBasin:
                case GameEnums.BiomeType.PiedmontBasin:
                case GameEnums.BiomeType.VolcanicAshPlain:
                case GameEnums.BiomeType.CoastalLowland:
                case GameEnums.BiomeType.TemperateGrassland:
                case GameEnums.BiomeType.DeciduousForest:
                case GameEnums.BiomeType.EvergreenForest:
                case GameEnums.BiomeType.MonsoonForest:
                case GameEnums.BiomeType.SemiAridShrubland:
                case GameEnums.BiomeType.LowHills: return 1f;
                // 中（盆地/高原/群岛/稀树）
                case GameEnums.BiomeType.EnclosedBasin:
                case GameEnums.BiomeType.LoessPlateau:
                case GameEnums.BiomeType.BrokenPlateau:
                case GameEnums.BiomeType.ContinentalIslands:
                case GameEnums.BiomeType.ContinentalIslet:
                case GameEnums.BiomeType.Savanna:
                case GameEnums.BiomeType.TropicalMonsoon:
                case GameEnums.BiomeType.PlateauMarsh: return 0.8f;
                // 低（沼泽/山地/密林）
                case GameEnums.BiomeType.WetMarshPlain:
                case GameEnums.BiomeType.Swamp:
                case GameEnums.BiomeType.RiverSourceMarsh:
                case GameEnums.BiomeType.CoastalSaltMarsh:
                case GameEnums.BiomeType.AlpineMeadow:
                case GameEnums.BiomeType.FoldMountains:
                case GameEnums.BiomeType.KarstMountains:
                case GameEnums.BiomeType.ErodedBadlands:
                case GameEnums.BiomeType.BorealForest:
                case GameEnums.BiomeType.TropicalRainforest:
                case GameEnums.BiomeType.Mangrove: return 0.6f;
                // 极低（干旱/高亢/群岛）
                case GameEnums.BiomeType.LoessKarst:
                case GameEnums.BiomeType.HighMountains:
                case GameEnums.BiomeType.InlandAridBasin:
                case GameEnums.BiomeType.HotDesert:
                case GameEnums.BiomeType.InlandDesert:
                case GameEnums.BiomeType.ColdDesert:
                case GameEnums.BiomeType.CoastalDesert:
                case GameEnums.BiomeType.SaltDesert:
                case GameEnums.BiomeType.GravelGobi:
                case GameEnums.BiomeType.VolcanicIslands:
                case GameEnums.BiomeType.VolcanicIslandArc:
                case GameEnums.BiomeType.Fjord: return 0.4f;
                // 近乎无（冰/冻原/离岛）
                case GameEnums.BiomeType.IceSheet:
                case GameEnums.BiomeType.MountainGlacier:
                case GameEnums.BiomeType.Tundra:
                case GameEnums.BiomeType.CoralAtoll:
                case GameEnums.BiomeType.ImpactCraterAtoll: return 0.2f;
                default: return 1f;
            }
        }

        /// <summary>基础承载（人口块 count 上限——50 人/块）</summary>
        public const float BaseCapacity = 100f;

        /// <summary>粮食类物资（仓储支撑人口的主粮：粮食0/肉类1/鱼类2/谷物10）</summary>
        public static readonly int[] FoodGoodsIds = { 0, 1, 2, 10 };

        /// <summary>
        /// 计算地块承载上限 = 基础 × 地形/群系系数 × 贸易修正 × 粮食支撑修正
        /// 粮食支撑：地区仓储（TradeCenter.inventory）中粮食类物资总量 ÷ 人口需求——
        /// 仓储存粮越多承载越高，缺粮压缩承载（仓储=地区物资仓库：本地产出+贸易品）
        /// </summary>
        public static float CalculateCarryingCapacity(TileData tile, IReadOnlyDictionary<int, TradeCenter> tradeCenters)
        {
            float capacity = BaseCapacity * GetTerrainMultiplier(tile);

            // 贸易修正（regionId 有贸易中心——贸易输入物资）
            if (tradeCenters != null && tradeCenters.TryGetValue(tile.regionId, out var tc))
            {
                capacity *= 1.15f;

                // 粮食支撑修正：仓储粮 ÷（人口 × 日需 0.01 × 30 天安全线）
                float foodStock = 0f;
                foreach (int gid in FoodGoodsIds)
                    foodStock += tc.inventory.GetValueOrDefault(gid, 0f);

                float demand = GetTotalPopulation(tile) * 0.01f * 30f;
                float support = demand > 0f ? foodStock / demand : 1f;
                capacity *= Mathf.Clamp(support, 0.5f, 2f);
            }

            return capacity;
        }

        /// <summary>地块当前总人口（count 合计）</summary>
        public static float GetTotalPopulation(TileData tile)
        {
            if (tile.populationBlocks == null) return 0f;
            float total = 0f;
            foreach (var pb in tile.populationBlocks) total += pb.count;
            return total;
        }

        /// <summary>超载率（>1 = 超载）</summary>
        public static float GetOverloadRatio(TileData tile, float capacity)
        {
            float total = GetTotalPopulation(tile);
            return capacity > 0f ? total / capacity : 1f;
        }
    }

    /// <summary>
    /// 军事人力系统：可用征募兵力 = 人口 × 阶层可征募率 × 地形修正
    /// 与人口承载独立（承载=人口上限，人力=征募池）
    /// </summary>
    public static class ManpowerSystem
    {
        /// <summary>阶层可征募率（人口的征募比例——奴隶不征募）</summary>
        public static float GetClassRecruitRate(GameEnums.SocialClass c) => c switch
        {
            GameEnums.SocialClass.Royalty => 0.05f,          // 王室亲卫
            GameEnums.SocialClass.NobilityClergy => 0.08f,   // 贵族骑士/教士卫队
            GameEnums.SocialClass.MerchantFreeman => 0.15f,  // 市民民兵
            GameEnums.SocialClass.Peasant => 0.10f,          // 农民征召
            GameEnums.SocialClass.Slave => 0f,               // 奴隶不征募
            _ => 0f
        };

        /// <summary>地块地形对征募的修正（山地难征/平原易征）</summary>
        public static float GetTerrainRecruitModifier(TileData tile) => CarryingCapacitySystem.GetTerrainMultiplier(tile) * 0.5f + 0.5f;

        /// <summary>政权分阶层人力池（key=SocialClass → 可征募人数；50人/块）</summary>
        public static Dictionary<GameEnums.SocialClass, float> GetRealmManpowerPool(
            int realmId, TileData[] tiles, IReadOnlyDictionary<int, RealmData> realms)
        {
            var pool = new Dictionary<GameEnums.SocialClass, float>();
            if (tiles == null || realms == null || !realms.TryGetValue(realmId, out var realm)) return pool;

            foreach (int tileIdx in realm.coreTiles)
            {
                if (tileIdx < 0 || tileIdx >= tiles.Length) continue;
                var tile = tiles[tileIdx];
                if (tile.populationBlocks == null) continue;

                float terrainMod = GetTerrainRecruitModifier(tile);
                foreach (var pb in tile.populationBlocks)
                {
                    float rate = GetClassRecruitRate(pb.socialClass);
                    if (rate <= 0f) continue;
                    float manpower = pb.count * 50f * rate * terrainMod; // count×50人×征募率×地形
                    pool[pb.socialClass] = pool.GetValueOrDefault(pb.socialClass) + manpower;
                }
            }
            return pool;
        }

        /// <summary>政权总可用人力</summary>
        public static float GetRealmTotalManpower(int realmId, TileData[] tiles, IReadOnlyDictionary<int, RealmData> realms)
        {
            float total = 0f;
            foreach (var v in GetRealmManpowerPool(realmId, tiles, realms).Values) total += v;
            return total;
        }
    }


    /// <summary>
    /// 人口三维占比统计（阶层/文化/信仰——count 加权——传播机制数据基础）
    /// 主流=count 最大块——文化/宗教地图按主流着色——面板显示占比明细
    /// </summary>
    public static class PopulationStats
    {
        /// <summary>该文化占比（0-1——count 加权）</summary>
        public static float GetCultureShare(TileData tile, int cultureId)
        {
            float total = 0f, match = 0f;
            if (tile.populationBlocks == null) return 0f;
            foreach (var pb in tile.populationBlocks)
            {
                total += pb.count;
                if (pb.cultureId == cultureId) match += pb.count;
            }
            return total > 0f ? match / total : 0f;
        }

        /// <summary>该信仰占比（0-1）</summary>
        public static float GetFaithShare(TileData tile, int faithId)
        {
            float total = 0f, match = 0f;
            if (tile.populationBlocks == null) return 0f;
            foreach (var pb in tile.populationBlocks)
            {
                total += pb.count;
                if (pb.faithId == faithId) match += pb.count;
            }
            return total > 0f ? match / total : 0f;
        }

        /// <summary>该阶层占比（0-1）</summary>
        public static float GetClassShare(TileData tile, GameEnums.SocialClass socialClass)
        {
            float total = 0f, match = 0f;
            if (tile.populationBlocks == null) return 0f;
            foreach (var pb in tile.populationBlocks)
            {
                total += pb.count;
                if (pb.socialClass == socialClass) match += pb.count;
            }
            return total > 0f ? match / total : 0f;
        }

        /// <summary>主流文化（count 最大块——-1 无）</summary>
        public static int GetDominantCulture(TileData tile)
        {
            if (tile.populationBlocks == null || tile.populationBlocks.Count == 0) return -1;
            var best = tile.populationBlocks[0];
            foreach (var pb in tile.populationBlocks)
                if (pb.count > best.count) best = pb;
            return best.cultureId;
        }

        /// <summary>主流信仰（count 最大块——-1 无）</summary>
        public static int GetDominantFaith(TileData tile)
        {
            if (tile.populationBlocks == null || tile.populationBlocks.Count == 0) return -1;
            var best = tile.populationBlocks[0];
            foreach (var pb in tile.populationBlocks)
                if (pb.count > best.count) best = pb;
            return best.faithId;
        }

        /// <summary>占比明细文本（地块信息面板——"佛教 70% · 原始崇拜 30%"）</summary>
        public static string BuildShareText(TileData tile, System.Func<int, string> cultureName, System.Func<int, string> faithName)
        {
            if (tile.populationBlocks == null || tile.populationBlocks.Count == 0) return "";
            var parts = new System.Collections.Generic.List<string>();
            var seenCulture = new System.Collections.Generic.HashSet<int>();
            var seenFaith = new System.Collections.Generic.HashSet<int>();
            foreach (var pb in tile.populationBlocks)
            {
                if (pb.cultureId >= 0 && seenCulture.Add(pb.cultureId))
                    parts.Add($"{cultureName(pb.cultureId)} {GetCultureShare(tile, pb.cultureId) * 100f:F0}%");
            }
            var faithParts = new System.Collections.Generic.List<string>();
            foreach (var pb in tile.populationBlocks)
            {
                if (pb.faithId >= 0 && seenFaith.Add(pb.faithId))
                    faithParts.Add($"{faithName(pb.faithId)} {GetFaithShare(tile, pb.faithId) * 100f:F0}%");
            }
            return $"文化：{string.Join(" · ", parts)}｜信仰：{string.Join(" · ", faithParts)}";
        }
    }
}
