using System.Collections.Generic;
using UnityEngine;
using CivilizationEvolution.Core;
using CivilizationEvolution.War;
using CivilizationEvolution.Economy;

namespace CivilizationEvolution.Politics
{
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
                    float manpower = pb.count * Army.ManpowerPerBlock * rate * terrainMod; // count×100人(Army.ManpowerPerBlock)×征募率×地形
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
}
