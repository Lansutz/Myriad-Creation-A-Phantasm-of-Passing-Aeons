using System.Collections.Generic;



using UnityEngine;
using CivilizationEvolution.Core;
using CivilizationEvolution.Core.Constants;
using CivilizationEvolution.Core.Data;
using CivilizationEvolution.Core.Dto;
using CivilizationEvolution.Core.Enums;
using CivilizationEvolution.Simulation.Actors;
using CivilizationEvolution.Simulation.Economy;
using CivilizationEvolution.Simulation.Events;
using CivilizationEvolution.Simulation.Generation;
using CivilizationEvolution.Simulation.Modding;
using CivilizationEvolution.Simulation.Settlement;
using CivilizationEvolution.Simulation.Society;
using CivilizationEvolution.Simulation.WorldState;
using CivilizationEvolution.World.Generation;
using CivilizationEvolution.World.Hydrology;
using CivilizationEvolution.World.Settlement;
using CivilizationEvolution.World.Terrain;


namespace CivilizationEvolution.Simulation.Politics
{
    /// <summary>
    /// 政权根本形态：行国（游牧）/半定居/城国（定居）。
    /// 由文化的 mobilityType 决定初始形态，可通过定居化改革转化。
    /// 这不是政体类型细分，而是决定整个运作方式的根本分类。
    /// </summary>
    public enum RealmForm
    {
        Sedentary = 0,      // 城国：固定都城、固定边界、农业税收
        SemiSedentary = 1,  // 半定居：冬都+夏都、相对固定边界、农牧混合
        Nomadic = 2         // 行国：移动王庭、活动范围、畜牧+劫掠+保护费
    }

    /// <summary>
    /// 游牧亚型（仅行国形态下细分，由生态环境决定特性）。
    /// </summary>
    public enum NomadicSubtype
    {
        None = 0,       // 非游牧
        Steppe = 1,     // 草原游牧（马，水平迁徙，军事扩张最强）
        Highland = 2,   // 山地游牧（牦牛，垂直迁徙，易守难攻）
        Desert = 3,     // 沙漠游牧（骆驼，水源导向，贸易控制）
        Pastoral = 4    // 半农半牧（过渡形态，易定居化）
    }

    /// <summary>
    /// 游牧活动范围数据。
    /// 行国不"拥有"地块，而是"活动于"地块——用中心+半径+影响力衰减表示，不逐地块标记归属。
    /// </summary>
    [System.Serializable]
    public class NomadicRange
    {
        public int centerTile;             // 活动范围中心（当前王庭所在地）
        public int summerCampTile = -1;    // 夏营地
        public int winterCampTile = -1;    // 冬营地
        public float coreRadius = 5f;      // 核心活动区半径（影响力强）
        public float outerRadius = 12f;    // 边缘活动区半径（影响力弱）
        public float influence = 50f;      // 影响力 0-100（决定纳贡、控制强度）

        /// <summary>某地块是否在活动范围内</summary>
        public bool IsInRange(int tileIndex, int mapWidth, int mapHeight)
        {
            return GetDistanceToCenter(tileIndex, mapWidth, mapHeight) <= outerRadius;
        }

        /// <summary>某地块是否在核心活动区</summary>
        public bool IsInCore(int tileIndex, int mapWidth, int mapHeight)
        {
            return GetDistanceToCenter(tileIndex, mapWidth, mapHeight) <= coreRadius;
        }

        /// <summary>获取地块受游牧影响力（0-1，核心区高，边缘衰减）</summary>
        public float GetInfluenceAt(int tileIndex, int mapWidth, int mapHeight)
        {
            float dist = GetDistanceToCenter(tileIndex, mapWidth, mapHeight);
            if (dist > outerRadius) return 0f;
            if (dist <= coreRadius) return influence / 100f;
            // 线性衰减
            float t = 1f - (dist - coreRadius) / (outerRadius - coreRadius);
            return (influence / 100f) * t;
        }

        private float GetDistanceToCenter(int tileIndex, int mapWidth, int mapHeight)
        {
            int cx = centerTile % mapWidth, cy = centerTile / mapWidth;
            int tx = tileIndex % mapWidth, ty = tileIndex / mapWidth;
            // 左右连通
            int dx = Mathf.Abs(tx - cx);
            dx = Mathf.Min(dx, mapWidth - dx);
            int dy = Mathf.Abs(ty - cy);
            return dx + dy;
        }
    }

    /// <summary>
    /// 游牧政权系统：处理行国的活动范围、季节迁移、纳贡、定居化转化。
    /// </summary>
    public static class NomadicRealmSystem
    {
        /// <summary>季节</summary>
        public enum Season { Spring, Summer, Autumn, Winter }

        /// <summary>根据文化移动模式推导政权形态</summary>
        public static RealmForm FormFromMobility(int mobilityType)
        {
            switch (mobilityType)
            {
                case 0: return RealmForm.Nomadic;        // 高度流动→行国
                case 1: return RealmForm.SemiSedentary;  // 季节性营地→半定居
                default: return RealmForm.Sedentary;     // 定居→城国
            }
        }

        /// <summary>
        /// 季节迁移：行国王庭在冬营地/夏营地之间移动。
        /// </summary>
        public static void SeasonalMigration(GameWorld world, int realmId, Season season)
        {
            if (world == null) return;
            if (!world.realms.TryGetValue(realmId, out var realm)) return;
            var range = realm.nomadicRange;
            if (range == null) return;

            int target = season == Season.Winter ? range.winterCampTile : range.summerCampTile;
            if (target < 0) target = range.centerTile;
            if (target < 0) return;

            range.centerTile = target;
            Debug.Log($"[NomadicRealm] {realm.realmName} 王庭迁移至地块{target}（{season}）");
        }

        /// <summary>
        /// 活动范围内的定居聚落向行国纳贡（保护费）。
        /// </summary>
        public static float CollectTribute(GameWorld world, int realmId)
        {
            if (world == null) return 0f;
            if (!world.realms.TryGetValue(realmId, out var realm)) return 0f;
            if (realm.realmForm != RealmForm.Nomadic || realm.nomadicRange == null) return 0f;

            float totalTribute = 0f;
            foreach (var kvp in world.burgs)
            {
                var burg = kvp.Value;
                if (burg.IsRuined) continue;
                if (!realm.nomadicRange.IsInRange(burg.tileIndex, world.mapWidth, world.mapHeight)) continue;
                // 不属于游牧政权的聚落才纳贡
                var tile = world.tiles[burg.tileIndex];
                if (tile.ownerRealmId == realmId) continue;

                float influence = realm.nomadicRange.GetInfluenceAt(
                    burg.tileIndex, world.mapWidth, world.mapHeight);
                float tribute = burg.wealth * 0.05f * influence; // 影响力越高，保护费越多
                burg.wealth -= tribute;
                totalTribute += tribute;
                world.burgs[kvp.Key] = burg;
            }

            realm.treasury += totalTribute;
            return totalTribute;
        }

        /// <summary>
        /// 定居化改革：行国→城国。
        /// 需要：农业革新、固定都城、官僚制度（由革新系统检查前置）。
        /// </summary>
        public static bool SettleRealm(GameWorld world, int realmId, int capitalTile)
        {
            if (world == null) return false;
            if (!world.realms.TryGetValue(realmId, out var realm)) return false;
            if (realm.realmForm == RealmForm.Sedentary) return false;

            realm.realmForm = RealmForm.Sedentary;
            // 活动范围内的核心地块转为固定领土
            if (realm.nomadicRange != null)
            {
                for (int i = 0; i < world.tiles.Length; i++)
                {
                    if (realm.nomadicRange.IsInCore(i, world.mapWidth, world.mapHeight))
                    {
                        var tile = world.tiles[i];
                        if (tile.exists && tile.isLand && tile.ownerRealmId < 0)
                        {
                            tile.ownerRealmId = realmId;
                            realm.coreTiles.Add(i);
                            world.tiles[i] = tile;
                        }
                    }
                }
                realm.nomadicRange = null; // 放弃活动范围模式
            }
            Debug.Log($"[NomadicRealm] {realm.realmName} 完成定居化改革，定都地块{capitalTile}");
            return true;
        }

        /// <summary>
        /// 行国随时弃地（拔营即走，无外交成本）。
        /// </summary>
        public static void NomadicAbandon(GameWorld world, int realmId, int tileIndex)
        {
            LandAbandonmentSystem.AbandonTile(
                world, tileIndex, AbandonmentType.Nomadic, realmId);
        }

        /// <summary>
        /// 计算游牧政权的军事加成（全民皆兵、骑兵机动性）。
        /// </summary>
        public static void GetNomadicMilitaryBonus(RealmData realm,
            out float manpowerRatio, out float cavalryBonus, out float mobilityBonus)
        {
            manpowerRatio = 0f;
            cavalryBonus = 0f;
            mobilityBonus = 0f;
            if (realm.realmForm != RealmForm.Nomadic) return;

            manpowerRatio = 0.25f; // 25%可动员（城国10%）
            switch (realm.nomadicSubtype)
            {
                case NomadicSubtype.Steppe:
                    cavalryBonus = 0.30f;   // 草原骑兵最强
                    mobilityBonus = 0.40f;
                    break;
                case NomadicSubtype.Highland:
                    cavalryBonus = 0.10f;
                    mobilityBonus = 0.10f;  // 山地机动性受限
                    break;
                case NomadicSubtype.Desert:
                    cavalryBonus = 0.20f;
                    mobilityBonus = 0.30f;  // 沙漠驼队机动
                    break;
                case NomadicSubtype.Pastoral:
                    cavalryBonus = 0.15f;
                    mobilityBonus = 0.20f;
                    break;
            }
        }
    }
}
