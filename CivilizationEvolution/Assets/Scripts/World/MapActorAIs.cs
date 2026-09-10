using CivilizationEvolution.Core;
using UnityEngine;

namespace CivilizationEvolution.World
{
    /// <summary>
    /// 流民 AI：从战乱/饥荒地区逃向安全（高秩序、高粮食、无战争）地区。
    /// 到达安全地区后转化为当地人口块，或被政权接纳/驱逐。
    /// </summary>
    public class RefugeeAI : IMapActorAI
    {
        private int _retargetCooldown = 0;
        private const int RetargetInterval = 30; // 每30天重新选择目标

        public void Think(GameWorld world, MapActor actor)
        {
            _retargetCooldown--;
            if (_retargetCooldown > 0 && actor.targetTile >= 0) return;
            _retargetCooldown = RetargetInterval;

            // 寻找最近的安全地块（高秩序、高粮食、非战争）
            int bestTile = -1;
            float bestScore = float.MinValue;
            int searchRadius = 20;

            int cx = actor.currentTile % world.mapWidth;
            int cy = actor.currentTile / world.mapWidth;

            for (int dy = -searchRadius; dy <= searchRadius; dy++)
            {
                for (int dx = -searchRadius; dx <= searchRadius; dx++)
                {
                    int nx = cx + dx;
                    int ny = cy + dy;
                    if (nx < 0 || nx >= world.mapWidth || ny < 0 || ny >= world.mapHeight) continue;
                    int idx = ny * world.mapWidth + nx;
                    if (idx < 0 || idx >= world.tiles.Length) continue;
                    var tile = world.tiles[idx];
                    if (!tile.exists || !tile.isLand) continue;

                    // 评分：秩序+粮食-距离-战争
                    float dist = Mathf.Abs(dx) + Mathf.Abs(dy);
                    float score = tile.order * 0.5f + tile.development * 10f * 0.3f - dist * 2f;
                    if (tile.ownerRealmId >= 0) score += 10f; // 有政权的地区更安全
                    if (score > bestScore)
                    {
                        bestScore = score;
                        bestTile = idx;
                    }
                }
            }

            if (bestTile >= 0) actor.SetTarget(bestTile);
        }

        public void Act(GameWorld world, MapActor actor)
        {
            // 到达目标后：转化为人口（简化实现）
            if (actor.currentTile == actor.targetTile)
            {
                var tile = world.tiles[actor.currentTile];
                if (tile.order > 50f && tile.development * 10f > 30f)
                {
                    // 流民被接纳，转化为人口
                    GetTilePopulation(tile) += actor.population;
                    actor.population = 0; // 消亡
                    Debug.Log($"[RefugeeAI] 流民#{actor.actorId} 在地块{actor.currentTile}被接纳，{actor.population}人融入当地");
                }
                else
                {
                    // 地区不安全，继续寻找
                    _retargetCooldown = 0;
                }
            }
        }
    }

    /// <summary>
    /// 游牧民 AI：在草原上逐水草而居，随机移动寻找高肥力地块。
    /// 可被政权招募为骑兵，或在边境劫掠。
    /// </summary>
    public class NomadAI : IMapActorAI
    {
        private int _wanderCooldown = 0;

        public void Think(GameWorld world, MapActor actor)
        {
            _wanderCooldown--;
            if (_wanderCooldown > 0 && actor.targetTile >= 0) return;
            _wanderCooldown = Random.Range(15, 45);

            // 随机选择附近的草原/肥沃地块
            int cx = actor.currentTile % world.mapWidth;
            int cy = actor.currentTile / world.mapWidth;
            int radius = Random.Range(5, 15);

            for (int attempt = 0; attempt < 10; attempt++)
            {
                int dx = Random.Range(-radius, radius + 1);
                int dy = Random.Range(-radius, radius + 1);
                int nx = cx + dx;
                int ny = cy + dy;
                if (nx < 0 || nx >= world.mapWidth || ny < 0 || ny >= world.mapHeight) continue;
                int idx = ny * world.mapWidth + nx;
                if (idx < 0 || idx >= world.tiles.Length) continue;
                var tile = world.tiles[idx];
                if (!tile.exists || !tile.isLand) continue;
                // 偏好草原
                if (tile.biome == GameEnums.BiomeType.Savanna ||
                    tile.biome == GameEnums.BiomeType.TemperateGrassland ||
                    tile.biome == GameEnums.BiomeType.Steppe ||
                    tile.fertility > 30f)
                {
                    actor.SetTarget(idx);
                    return;
                }
            }
        }

        public void Act(GameWorld world, MapActor actor)
        {
            // 在肥沃地块恢复补给
            var tile = world.tiles[actor.currentTile];
            if (tile.fertility > 40f)
            {
                actor.supplies = Mathf.Min(200f, actor.supplies + 5f);
                actor.health = Mathf.Min(100f, actor.health + 1f);
            }
        }
    }

    /// <summary>
    /// 商队 AI：在两个贸易中心之间往返运输物资。
    /// 到达目的地后完成交易，然后返回起点。
    /// 可被土匪劫掠，可被政权征税。
    /// </summary>
    public class CaravanAI : IMapActorAI
    {
        private readonly int _destinationTile;
        private bool _returning = false;
        private int _waitDays = 0;

        public CaravanAI(int destinationTile)
        {
            _destinationTile = destinationTile;
        }

        public void Think(GameWorld world, MapActor actor)
        {
            if (_waitDays > 0)
            {
                _waitDays--;
                return;
            }

            int target = _returning ? actor.actorId >= 0 ? FindOriginTile(world, actor) : _destinationTile : _destinationTile;
            if (target >= 0 && target != actor.currentTile)
                actor.SetTarget(target);
        }

        public void Act(GameWorld world, MapActor actor)
        {
            if (actor.currentTile == _destinationTile && !_returning)
            {
                // 到达目的地，交易后等待几天再返回
                _waitDays = Random.Range(3, 10);
                _returning = true;
                actor.supplies = Mathf.Min(200f, actor.supplies + 50f);
            }
            else if (_returning && actor.currentTile != _destinationTile)
            {
                // 返回起点后消亡（简化：到达非目的地且在返回状态时消亡）
                // 实际应该记录起点，这里简化
            }
        }

        private int FindOriginTile(GameWorld world, MapActor actor)
        {
            // 简化：随机找一个有聚落的地块作为返回点
            for (int attempt = 0; attempt < 20; attempt++)
            {
                int idx = Random.Range(0, world.tiles.Length);
                if (world.tiles[idx].burgId >= 0) return idx;
            }
            return -1;
        }
    }

    /// <summary>
    /// 野怪 AI：在荒野中游荡，袭击附近聚落，被军队清剿。
    /// 偏好森林/山地，避免高秩序地区。
    /// </summary>
    public class WildBeastAI : IMapActorAI
    {
        private int _wanderCooldown = 0;
        private int _attackCooldown = 0;

        public void Think(GameWorld world, MapActor actor)
        {
            _wanderCooldown--;
            _attackCooldown--;

            // 寻找附近的聚落袭击
            if (_attackCooldown <= 0)
            {
                // 简化：检查附近地块是否有聚落
                int cx = actor.currentTile % world.mapWidth;
                int cy = actor.currentTile / world.mapHeight;
                for (int dy = -5; dy <= 5; dy++)
                {
                    for (int dx = -5; dx <= 5; dx++)
                    {
                        int nx = cx + dx, ny = cy + dy;
                        if (nx < 0 || nx >= world.mapWidth || ny < 0 || ny >= world.mapHeight) continue;
                        int idx = ny * world.mapWidth + nx;
                        if (idx < 0 || idx >= world.tiles.Length) continue;
                        if (world.tiles[idx].burgId >= 0 && world.tiles[idx].orderLevel < 50f)
                        {
                            actor.SetTarget(idx);
                            _attackCooldown = Random.Range(20, 50);
                            return;
                        }
                    }
                }
            }

            // 游荡：偏好荒野，避免高秩序
            if (_wanderCooldown <= 0 || actor.targetTile < 0)
            {
                _wanderCooldown = Random.Range(10, 30);
                int cx = actor.currentTile % world.mapWidth;
                int cy = actor.currentTile / world.mapHeight;
                for (int attempt = 0; attempt < 10; attempt++)
                {
                    int dx = Random.Range(-8, 9);
                    int dy = Random.Range(-8, 9);
                    int nx = cx + dx, ny = cy + dy;
                    if (nx < 0 || nx >= world.mapWidth || ny < 0 || ny >= world.mapHeight) continue;
                    int idx = ny * world.mapWidth + nx;
                    if (idx < 0 || idx >= world.tiles.Length) continue;
                    var tile = world.tiles[idx];
                    if (!tile.exists || !tile.isLand) continue;
                    if (tile.order < 40f && tile.development > 20f ? 1 : -1 < 0)
                    {
                        actor.SetTarget(idx);
                        return;
                    }
                }
            }
        }

        public void Act(GameWorld world, MapActor actor)
        {
            // 袭击聚落：减少人口和粮食
            var tile = world.tiles[actor.currentTile];
            if (tile.development > 20f ? 1 : -1 >= 0 && GetTilePopulation(tile) > 0 && _attackCooldown <= 0)
            {
                int casualties = Mathf.Min(GetTilePopulation(tile) / 10, actor.population);
                GetTilePopulation(tile) -= casualties;
                tile.development * 10f = Mathf.Max(0f, tile.development * 10f - 20f);
                actor.supplies = Mathf.Min(200f, actor.supplies + 30f);
                _attackCooldown = Random.Range(30, 60);
                Debug.Log($"[WildBeastAI] 野兽群#{actor.actorId} 袭击地块{actor.currentTile}，伤亡{casualties}人");
            }
        }
    }
}
