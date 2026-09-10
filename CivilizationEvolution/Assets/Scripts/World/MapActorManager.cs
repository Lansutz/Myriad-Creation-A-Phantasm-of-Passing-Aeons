using System.Collections.Generic;
using CivilizationEvolution.Core;
using CivilizationEvolution.Military;
using UnityEngine;

namespace CivilizationEvolution.World
{
    /// <summary>
    /// 无主地图单位管理器。
    /// 负责：单位生成、每日更新、查询、与各系统联动。
    /// 接入 GameWorld 主循环，每 Tick 更新所有 MapActor。
    /// </summary>
    public class MapActorManager
    {
        private readonly GameWorld _world;
        private readonly List<MapActor> _actors = new List<MapActor>();
        private readonly Dictionary<int, MapActor> _actorById = new Dictionary<int, MapActor>();
        private int _nextActorId = 1;

        // ===== 生成参数（可调） =====
        public float RefugeeSpawnChance = 0.1f;     // 战乱地区每日流民生出概率
        public float NomadSpawnChance = 0.05f;      // 草原每日游牧民生出概率
        public float CaravanSpawnChance = 0.2f;     // 贸易中心每日商队生出概率
        public float WildBeastSpawnChance = 0.03f;  // 荒野每日野怪生出概率
        public int MaxActors = 500;                 // 最大同时存在单位数

        public MapActorManager(GameWorld world)
        {
            _world = world;
        }

        /// <summary>所有无主单位</summary>
        public IReadOnlyList<MapActor> AllActors => _actors;

        /// <summary>单位数量</summary>
        public int Count => _actors.Count;

        /// <summary>按类型统计</summary>
        public Dictionary<MapActorType, int> CountByType()
        {
            var result = new Dictionary<MapActorType, int>();
            foreach (var actor in _actors)
            {
                if (!result.ContainsKey(actor.type)) result[actor.type] = 0;
                result[actor.type]++;
            }
            return result;
        }

        /// <summary>按ID查询</summary>
        public MapActor GetActor(int actorId)
        {
            _actorById.TryGetValue(actorId, out var actor);
            return actor;
        }

        /// <summary>获取某地块上的所有单位</summary>
        public List<MapActor> GetActorsAtTile(int tileIndex)
        {
            var result = new List<MapActor>();
            foreach (var actor in _actors)
                if (actor.currentTile == tileIndex) result.Add(actor);
            return result;
        }

        /// <summary>获取某地块半径内的单位</summary>
        public List<MapActor> GetActorsNearTile(int tileIndex, int radius)
        {
            var result = new List<MapActor>();
            int width = _world.mapWidth;
            int height = _world.mapHeight;
            int cx = tileIndex % width;
            int cy = tileIndex / width;
            foreach (var actor in _actors)
            {
                int ax = actor.currentTile % width;
                int ay = actor.currentTile / width;
                int dist = Mathf.Abs(ax - cx) + Mathf.Abs(ay - cy);
                if (dist <= radius) result.Add(actor);
            }
            return result;
        }

        /// <summary>每日更新：生成 + Tick + 清理死亡</summary>
        public void Tick(float deltaDays)
        {
            // 生成新单位
            if (_actors.Count < MaxActors)
                TrySpawnActors();

            // 更新所有单位
            for (int i = _actors.Count - 1; i >= 0; i--)
            {
                var actor = _actors[i];
                actor.Tick(_world, deltaDays);
                if (!actor.IsAlive)
                {
                    _actors.RemoveAt(i);
                    _actorById.Remove(actor.actorId);
                }
            }
        }

        /// <summary>尝试生成各类无主单位</summary>
        private void TrySpawnActors()
        {
            // 流民：战乱/饥荒地区
            if (Random.value < RefugeeSpawnChance)
                TrySpawnRefugee();

            // 游牧民：草原地区
            if (Random.value < NomadSpawnChance)
                TrySpawnNomad();

            // 商队：贸易中心之间
            if (Random.value < CaravanSpawnChance)
                TrySpawnCaravan();

            // 野怪：荒野地区
            if (Random.value < WildBeastSpawnChance)
                TrySpawnWildBeast();
        }

        // ===== 具体生成逻辑 =====

        private void TrySpawnRefugee()
        {
            // 在战乱或饥荒地区找一个地块
            for (int attempt = 0; attempt < 20; attempt++)
            {
                int idx = Random.Range(0, _world.tiles.Length);
                var tile = _world.tiles[idx];
                if (!tile.exists || !tile.isLand) continue;
                // 简化：低秩序或低粮食地区生成流民
                if (tile.order > 30f && tile.development * 10f > 20f) continue;

                var actor = CreateActor(MapActorType.Refugee, idx, Random.Range(50, 500));
                actor.actorName = $"流民队伍#{actor.actorId}";
                actor.AI = new RefugeeAI();
                actor.morale = 40f;
                return;
            }
        }

        private void TrySpawnNomad()
        {
            for (int attempt = 0; attempt < 20; attempt++)
            {
                int idx = Random.Range(0, _world.tiles.Length);
                var tile = _world.tiles[idx];
                if (!tile.exists || !tile.isLand) continue;
                // 草原/稀树草原生成游牧民
                if (tile.biome != GameEnums.BiomeType.Savanna &&
                    tile.biome != GameEnums.BiomeType.TemperateGrassland &&
                    tile.biome != GameEnums.BiomeType.Steppe) continue;

                var actor = CreateActor(MapActorType.Nomad, idx, Random.Range(100, 1000));
                actor.actorName = $"游牧部落#{actor.actorId}";
                actor.AI = new NomadAI();
                actor.moveSpeed = 1.5f;
                return;
            }
        }

        private void TrySpawnCaravan()
        {
            // 找两个有聚落的地块作为起点和终点
            int startTile = -1, endTile = -1;
            for (int attempt = 0; attempt < 30; attempt++)
            {
                int idx = Random.Range(0, _world.tiles.Length);
                var tile = _world.tiles[idx];
                if (!tile.exists || !tile.isLand) continue;
                if (tile.development > 20f ? 1 : -1 < 0) continue;
                if (startTile < 0) startTile = idx;
                else { endTile = idx; break; }
            }
            if (startTile < 0 || endTile < 0 || startTile == endTile) return;

            var actor = CreateActor(MapActorType.Caravan, startTile, Random.Range(10, 50));
            actor.actorName = $"商队#{actor.actorId}";
            actor.AI = new CaravanAI(endTile);
            actor.moveSpeed = 1.2f;
            actor.supplies = 200f;
        }

        private void TrySpawnWildBeast()
        {
            for (int attempt = 0; attempt < 20; attempt++)
            {
                int idx = Random.Range(0, _world.tiles.Length);
                var tile = _world.tiles[idx];
                if (!tile.exists || !tile.isLand) continue;
                // 森林/山地/荒野生成野怪
                if (tile.biome != GameEnums.BiomeType.DeciduousForest &&
                    tile.biome != GameEnums.BiomeType.EvergreenForest &&
                    tile.biome != GameEnums.BiomeType.Rainforest &&
                    tile.elevation01 < 0.5f) continue;

                var actor = CreateActor(MapActorType.WildBeast, idx, Random.Range(5, 50));
                actor.actorName = $"野兽群#{actor.actorId}";
                actor.AI = new WildBeastAI();
                actor.isHostile = true;
                return;
            }
        }

        /// <summary>创建单位并注册</summary>
        private MapActor CreateActor(MapActorType type, int tileIndex, int population)
        {
            var actor = new MapActor
            {
                actorId = _nextActorId++,
                type = type,
                currentTile = tileIndex,
                population = population
            };
            _actors.Add(actor);
            _actorById[actor.actorId] = actor;
            return actor;
        }

        /// <summary>手动生成单位（事件/模组调用）</summary>
        public MapActor SpawnActor(MapActorType type, int tileIndex, int population, IMapActorAI ai = null)
        {
            var actor = CreateActor(type, tileIndex, population);
            actor.AI = ai;
            return actor;
        }

        /// <summary>移除单位</summary>
        public void RemoveActor(int actorId)
        {
            if (_actorById.TryGetValue(actorId, out var actor))
            {
                _actors.Remove(actor);
                _actorById.Remove(actorId);
            }
        }

        /// <summary>计算地块总人口（从populationBlocks汇总）</summary>
        public static int GetTilePopulation(TileData tile)
        {
            if (tile.populationBlocks == null) return 0;
            int total = 0;
            foreach (var block in tile.populationBlocks) total += Mathf.RoundToInt(block.count);
            return total;
        }

        /// <summary>清空所有单位</summary>
        public void Clear()
        {
            _actors.Clear();
            _actorById.Clear();
            _nextActorId = 1;
        }
    }
}
