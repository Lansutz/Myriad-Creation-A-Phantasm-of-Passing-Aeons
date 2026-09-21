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
using CivilizationEvolution.Simulation.Politics;
using CivilizationEvolution.Simulation.Society;
using CivilizationEvolution.Simulation.WorldState;


namespace CivilizationEvolution.Simulation.Settlement
{
    /// <summary>
    /// 弃地类型。
    /// </summary>
    public enum AbandonmentType
    {
        Voluntary,  // 主动弃地：政权战略决策放弃（战略收缩、无法防守、统治成本过高）
        Forced,     // 被动弃地：战争失败/被驱逐（战败、被蛮族驱逐、割地）
        Natural,    // 自然弃地：环境恶化/经济崩溃（沙漠化、瘟疫、贸易路线转移）
        Nomadic     // 游牧弃地：游牧政权季节性迁移（拔营即走，无外交成本）
    }

    /// <summary>
    /// 弃地机制系统。
    /// 统一处理：四种弃地类型、地块状态变化、连锁反应（流民/秩序下降/蛮族滋生）、重新占领。
    /// 与聚落摧毁系统、MapActor、游牧政权联动。
    /// </summary>
    public static class LandAbandonmentSystem
    {
        /// <summary>弃地记录（用于历史和外交）</summary>
        public struct AbandonmentRecord
        {
            public int tileIndex;
            public int previousRealmId;
            public AbandonmentType type;
            public int day;
            public int populationLost;
        }

        /// <summary>
        /// 执行弃地：一个政权放弃某地块。
        /// </summary>
        /// <param name="world">游戏世界</param>
        /// <param name="tileIndex">地块索引</param>
        /// <param name="type">弃地类型</param>
        /// <param name="realmId">放弃的政权ID</param>
        /// <returns>弃地记录，null表示失败</returns>
        public static AbandonmentRecord? AbandonTile(GameWorld world, int tileIndex,
            AbandonmentType type, int realmId)
        {
            if (world == null || tileIndex < 0 || tileIndex >= world.tiles.Length) return null;
            var tile = world.tiles[tileIndex];
            if (!tile.exists) return null;

            int popLost = MapActorManager.GetTilePopulation(tile);

            var record = new AbandonmentRecord
            {
                tileIndex = tileIndex,
                previousRealmId = tile.ownerRealmId,
                type = type,
                day = world.currentDay,
                populationLost = popLost
            };

            // 1. 地块变为无主
            tile.ownerRealmId = -1;
            tile.occupyingRealmId = -1;

            // 2. 秩序下降（自然弃地降得最多——治理完全崩溃）
            float orderDrop = type == AbandonmentType.Natural ? 40f
                : type == AbandonmentType.Forced ? 30f
                : type == AbandonmentType.Voluntary ? 20f : 10f; // 游牧弃地秩序影响最小
            tile.order = Mathf.Max(0f, tile.order - orderDrop);

            // 3. 发展度下降（基础设施无人维护）
            float devDrop = type == AbandonmentType.Nomadic ? 0.05f : 0.15f;
            tile.development *= (1f - devDrop);

            // 4. 人口处理
            HandlePopulationOnAbandon(world, tileIndex, type, popLost);

            // 5. 游牧弃地：营寨拆除（拔营）
            if (type == AbandonmentType.Nomadic && tile.campId >= 0)
            {
                world.Camps?.AbandonCamp(tile.campId);
                tile.campId = -1;
            }

            world.tiles[tileIndex] = tile;
            Debug.Log($"[Abandonment] 地块{tileIndex}被{realmId}弃置（{type}），流失人口{popLost}");
            return record;
        }

        /// <summary>
        /// 批量弃地：放弃一组连续地块（如战略收缩整条边境线）。
        /// </summary>
        public static List<AbandonmentRecord> AbandonTiles(GameWorld world,
            IEnumerable<int> tileIndices, AbandonmentType type, int realmId)
        {
            var records = new List<AbandonmentRecord>();
            foreach (int idx in tileIndices)
            {
                var r = AbandonTile(world, idx, type, realmId);
                if (r.HasValue) records.Add(r.Value);
            }
            return records;
        }

        /// <summary>弃地时的人口处理</summary>
        private static void HandlePopulationOnAbandon(GameWorld world, int tileIndex,
            AbandonmentType type, int pop)
        {
            if (pop <= 0 || world.MapActors == null) return;

            switch (type)
            {
                case AbandonmentType.Voluntary:
                    // 主动弃地：大部分人口随政权撤离（迁移到相邻己方地块），少量留下
                    MigratePopulation(world, tileIndex, pop * 0.7f);
                    SpawnRefugees(world, tileIndex, Mathf.RoundToInt(pop * 0.2f));
                    break;

                case AbandonmentType.Forced:
                    // 被动弃地：部分人口逃亡，部分被征服者留下
                    SpawnRefugees(world, tileIndex, Mathf.RoundToInt(pop * 0.5f));
                    break;

                case AbandonmentType.Natural:
                    // 自然弃地：大部分人口成为流民
                    SpawnRefugees(world, tileIndex, Mathf.RoundToInt(pop * 0.8f));
                    break;

                case AbandonmentType.Nomadic:
                    // 游牧弃地：人口随部落迁移（不产生流民，直接消失——他们跟着走了）
                    MapActorManager.AddPopulationToTile(ref world.tiles[tileIndex], -pop);
                    break;
            }
        }

        /// <summary>人口迁移到相邻己方地块</summary>
        private static void MigratePopulation(GameWorld world, int fromTile, float amount)
        {
            int pop = Mathf.RoundToInt(amount);
            if (pop <= 0) return;
            var from = world.tiles[fromTile];
            int ownerId = from.ownerRealmId; // 弃地前已设为-1，需用record里的
            // 简化：找相邻有人口承载能力的地块
            int w = world.mapWidth;
            int cx = fromTile % w, cy = fromTile / w;
            for (int dy = -1; dy <= 1; dy++)
            {
                for (int dx = -1; dx <= 1; dx++)
                {
                    if (dx == 0 && dy == 0) continue;
                    int nx = cx + dx, ny = cy + dy;
                    if (nx < 0 || nx >= w || ny < 0 || ny >= world.mapHeight) continue;
                    int idx = ny * w + nx;
                    if (idx < 0 || idx >= world.tiles.Length) continue;
                    var neighbor = world.tiles[idx];
                    if (neighbor.exists && neighbor.isLand)
                    {
                        MapActorManager.AddPopulationToTile(ref world.tiles[idx], pop / 4);
                    }
                }
            }
            // 原地块人口减少
            MapActorManager.AddPopulationToTile(ref world.tiles[fromTile], -pop);
        }

        /// <summary>产生流民MapActor</summary>
        private static void SpawnRefugees(GameWorld world, int tileIndex, int count)
        {
            if (count < 5) return;
            var refugee = world.MapActors.SpawnActor(
                MapActorType.Refugee, tileIndex, count, new RefugeeAI());
            refugee.actorName = "弃地流民#" + refugee.actorId;
            refugee.morale = 25f;
            // 原地块人口减少
            MapActorManager.AddPopulationToTile(ref world.tiles[tileIndex], -count);
        }

        /// <summary>
        /// 重新占领/定居弃地。
        /// </summary>
        /// <param name="world">游戏世界</param>
        /// <param name="tileIndex">地块</param>
        /// <param name="newRealmId">新政权ID</param>
        /// <param name="settlerCount">定居者数量</param>
        public static bool ResettleTile(GameWorld world, int tileIndex,
            int newRealmId, int settlerCount)
        {
            if (world == null || tileIndex < 0 || tileIndex >= world.tiles.Length) return false;
            var tile = world.tiles[tileIndex];
            if (!tile.exists || !tile.isLand) return false;
            if (tile.ownerRealmId >= 0 && tile.ownerRealmId != newRealmId) return false; // 已有主

            tile.ownerRealmId = newRealmId;
            tile.order = Mathf.Max(tile.order, 20f); // 恢复基本秩序
            MapActorManager.AddPopulationToTile(ref world.tiles[tileIndex], settlerCount);
            world.tiles[tileIndex] = tile;

            Debug.Log($"[Abandonment] 政权{newRealmId}重新定居地块{tileIndex}，迁入{settlerCount}人");
            return true;
        }

        /// <summary>
        /// 每日检查：长期无主低秩序地块可能滋生土匪/蛮族（MapActor生成钩子）。
        /// </summary>
        public static void DailyCheckBanditSpawn(GameWorld world)
        {
            if (world == null || world.MapActors == null) return;
            // 概率极低，每天抽少量地块检查
            if (Random.value > 0.02f) return;

            for (int attempt = 0; attempt < 5; attempt++)
            {
                int idx = Random.Range(0, world.tiles.Length);
                var tile = world.tiles[idx];
                if (!tile.exists || !tile.isLand) continue;
                if (tile.ownerRealmId >= 0) continue;       // 有主
                if (tile.order > 25f) continue;              // 秩序不算太差
                if (tile.development < 0.05f) continue;      // 太荒无人烟

                // 滋生土匪
                var bandit = world.MapActors.SpawnActor(
                    MapActorType.Bandit, idx, Random.Range(15, 80), new BanditAI());
                bandit.actorName = "土匪#" + bandit.actorId;
                bandit.isHostile = true;
                return;
            }
        }
    }
}
