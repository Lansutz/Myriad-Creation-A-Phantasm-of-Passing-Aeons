using System.Collections.Generic;


using UnityEngine;
using CivilizationEvolution.Core;
using CivilizationEvolution.Core.Constants;
using CivilizationEvolution.Core.Data;
using CivilizationEvolution.Core.Dto;
using CivilizationEvolution.Core.Enums;
using CivilizationEvolution.Simulation.Economy;
using CivilizationEvolution.Simulation.Events;
using CivilizationEvolution.Simulation.Generation;
using CivilizationEvolution.Simulation.Modding;
using CivilizationEvolution.Simulation.Politics;
using CivilizationEvolution.Simulation.Society;
using CivilizationEvolution.Simulation.Warfare;
using CivilizationEvolution.Simulation.WorldState;


namespace CivilizationEvolution.Simulation.Actors
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
        public float RefugeeSpawnChance = 0.1f;
        public float NomadSpawnChance = 0.05f;
        public float CaravanSpawnChance = 0.2f;
        public float WildBeastSpawnChance = 0.03f;
        public float BanditSpawnChance = 0.04f;
        public float AnimalDisasterSpawnChance = 0.01f;
        public int MaxActors = 500;

        public MapActorManager(GameWorld world)
        {
            _world = world;
        }

        public IReadOnlyList<MapActor> AllActors => _actors;
        public int Count => _actors.Count;

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

        public MapActor GetActor(int actorId)
        {
            _actorById.TryGetValue(actorId, out var actor);
            return actor;
        }

        public List<MapActor> GetActorsAtTile(int tileIndex)
        {
            var result = new List<MapActor>();
            foreach (var actor in _actors)
                if (actor.currentTile == tileIndex) result.Add(actor);
            return result;
        }

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
            if (_actors.Count < MaxActors)
                TrySpawnActors();

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

        private void TrySpawnActors()
        {
            if (Random.value < RefugeeSpawnChance) TrySpawnRefugee();
            if (Random.value < NomadSpawnChance) TrySpawnNomad();
            if (Random.value < CaravanSpawnChance) TrySpawnCaravan();
            if (Random.value < WildBeastSpawnChance) TrySpawnWildBeast();
            if (Random.value < BanditSpawnChance) TrySpawnBandit();
            if (Random.value < AnimalDisasterSpawnChance) TrySpawnAnimalDisaster();
        }

        // ===== 具体生成逻辑 =====

        private void TrySpawnRefugee()
        {
            for (int attempt = 0; attempt < 20; attempt++)
            {
                int idx = Random.Range(0, _world.tiles.Length);
                var tile = _world.tiles[idx];
                if (!tile.exists || !tile.isLand) continue;
                if (tile.order > 30f && tile.development > 0.2f) continue;

                var actor = CreateActor(MapActorType.Refugee, idx, Random.Range(50, 500));
                actor.actorName = "流民队伍#" + actor.actorId;
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
                if (tile.biome != GameEnums.BiomeType.Savanna &&
                    tile.biome != GameEnums.BiomeType.TemperateGrassland &&
                    tile.biome != GameEnums.BiomeType.TemperateGrassland) continue;

                var actor = CreateActor(MapActorType.Nomad, idx, Random.Range(100, 1000));
                actor.actorName = "游牧部落#" + actor.actorId;
                actor.AI = new NomadAI();
                actor.moveSpeed = 1.5f;
                return;
            }
        }

        private void TrySpawnCaravan()
        {
            int startTile = -1, endTile = -1;
            for (int attempt = 0; attempt < 30; attempt++)
            {
                int idx = Random.Range(0, _world.tiles.Length);
                var tile = _world.tiles[idx];
                if (!tile.exists || !tile.isLand) continue;
                if (tile.development < 0.2f) continue;
                if (startTile < 0) startTile = idx;
                else { endTile = idx; break; }
            }
            if (startTile < 0 || endTile < 0 || startTile == endTile) return;

            var actor = CreateActor(MapActorType.Caravan, startTile, Random.Range(10, 50));
            actor.actorName = "商队#" + actor.actorId;
            actor.AI = new CaravanAI(startTile, endTile);
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
                if (tile.biome != GameEnums.BiomeType.DeciduousForest &&
                    tile.biome != GameEnums.BiomeType.EvergreenForest &&
                    tile.biome != GameEnums.BiomeType.TropicalRainforest &&
                    tile.elevation01 < 0.5f) continue;

                var actor = CreateActor(MapActorType.WildBeast, idx, Random.Range(5, 50));
                actor.actorName = "野兽群#" + actor.actorId;
                actor.AI = new WildBeastAI();
                actor.isHostile = true;
                return;
            }
        }

        private void TrySpawnBandit()
        {
            for (int attempt = 0; attempt < 20; attempt++)
            {
                int idx = Random.Range(0, _world.tiles.Length);
                var tile = _world.tiles[idx];
                if (!tile.exists || !tile.isLand) continue;
                if (tile.order > 35f) continue;
                if (tile.ownerRealmId >= 0 && tile.order > 20f) continue;

                var actor = CreateActor(MapActorType.Bandit, idx, Random.Range(20, 200));
                actor.actorName = "土匪#" + actor.actorId;
                actor.AI = new BanditAI();
                actor.isHostile = true;
                actor.moveSpeed = 1.3f;
                return;
            }
        }

        private void TrySpawnAnimalDisaster()
        {
            for (int attempt = 0; attempt < 15; attempt++)
            {
                int idx = Random.Range(0, _world.tiles.Length);
                var tile = _world.tiles[idx];
                if (!tile.exists || !tile.isLand) continue;
                if (tile.fertility < 40f) continue; // 农业区才会爆发

                var actor = CreateActor(MapActorType.AnimalDisaster, idx, Random.Range(100, 1000));
                actor.actorName = "蝗灾#" + actor.actorId;
                actor.AI = new AnimalDisasterAI(Random.Range(60, 180));
                actor.moveSpeed = 2.0f;
                return;
            }
        }

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

        public void RemoveActor(int actorId)
        {
            if (_actorById.TryGetValue(actorId, out var actor))
            {
                _actors.Remove(actor);
                _actorById.Remove(actorId);
            }
        }

        // ===== 静态辅助方法 =====

        /// <summary>计算地块总人口（从populationBlocks汇总）</summary>
        public static int GetTilePopulation(TileData tile)
        {
            if (tile.populationBlocks == null) return 0;
            int total = 0;
            foreach (var block in tile.populationBlocks) total += Mathf.RoundToInt(block.count);
            return total;
        }

        /// <summary>向地块添加/减少人口（修改populationBlocks，正数增加，负数减少）</summary>
        public static void AddPopulationToTile(ref TileData tile, int amount)
        {
            if (amount == 0) return;
            if (tile.populationBlocks == null) tile.populationBlocks = new List<PopulationBlock>();

            if (amount > 0)
            {
                // 增加：加到第一个块或新建块
                if (tile.populationBlocks.Count > 0)
                {
                    var block = tile.populationBlocks[0];
                    block.count += amount;
                    tile.populationBlocks[0] = block;
                }
                else
                {
                    tile.populationBlocks.Add(new PopulationBlock { count = amount });
                }
            }
            else
            {
                // 减少：从各块依次扣除
                int remaining = -amount;
                for (int i = tile.populationBlocks.Count - 1; i >= 0 && remaining > 0; i--)
                {
                    var block = tile.populationBlocks[i];
                    if (block.count <= remaining)
                    {
                        remaining -= Mathf.RoundToInt(block.count);
                        tile.populationBlocks.RemoveAt(i);
                    }
                    else
                    {
                        block.count -= remaining;
                        tile.populationBlocks[i] = block;
                        remaining = 0;
                    }
                }
            }
        }

        public void Clear()
        {
            _actors.Clear();
            _actorById.Clear();
            _nextActorId = 1;
        }
    }
}
