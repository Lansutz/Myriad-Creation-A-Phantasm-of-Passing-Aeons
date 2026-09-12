
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
using CivilizationEvolution.Simulation.WorldState;


namespace CivilizationEvolution.Simulation.Actors
{
    /// <summary>
    /// 流民 AI：从战乱/饥荒地区逃向安全（高秩序、高发展、无战争）地区。
    /// 到达安全地区后转化为当地人口块，或被政权接纳/驱逐。
    /// </summary>
    public class RefugeeAI : IMapActorAI
    {
        private int _retargetCooldown = 0;
        private const int RetargetInterval = 30;

        public void Think(GameWorld world, MapActor actor)
        {
            _retargetCooldown--;
            if (_retargetCooldown > 0 && actor.targetTile >= 0) return;
            _retargetCooldown = RetargetInterval;

            int bestTile = -1;
            float bestScore = float.MinValue;
            int searchRadius = 20;
            int cx = actor.currentTile % world.mapWidth;
            int cy = actor.currentTile / world.mapWidth;

            for (int dy = -searchRadius; dy <= searchRadius; dy++)
            {
                for (int dx = -searchRadius; dx <= searchRadius; dx++)
                {
                    int nx = cx + dx, ny = cy + dy;
                    if (nx < 0 || nx >= world.mapWidth || ny < 0 || ny >= world.mapHeight) continue;
                    int idx = ny * world.mapWidth + nx;
                    if (idx < 0 || idx >= world.tiles.Length) continue;
                    var tile = world.tiles[idx];
                    if (!tile.exists || !tile.isLand) continue;

                    float dist = Mathf.Abs(dx) + Mathf.Abs(dy);
                    float score = tile.order * 0.5f + tile.development * 30f - dist * 2f;
                    if (tile.ownerRealmId >= 0) score += 10f;
                    if (score > bestScore) { bestScore = score; bestTile = idx; }
                }
            }
            if (bestTile >= 0) actor.SetTarget(bestTile);
        }

        public void Act(GameWorld world, MapActor actor)
        {
            if (actor.currentTile == actor.targetTile)
            {
                var tile = world.tiles[actor.currentTile];
                if (tile.order > 50f && tile.development > 0.3f)
                {
                    MapActorManager.AddPopulationToTile(ref world.tiles[actor.currentTile], actor.population);
                    Debug.Log($"[RefugeeAI] 流民#{actor.actorId} 在地块{actor.currentTile}被接纳，{actor.population}人融入当地");
                    actor.population = 0;
                }
                else
                {
                    _retargetCooldown = 0;
                }
            }
        }
    }

    /// <summary>
    /// 游牧民 AI：在草原上逐水草而居，随机移动寻找高肥力地块。
    /// 可被政权招募为骑兵，或在边境劫掠。肥力高的地块恢复补给。
    /// </summary>
    public class NomadAI : IMapActorAI
    {
        private int _wanderCooldown = 0;

        public void Think(GameWorld world, MapActor actor)
        {
            _wanderCooldown--;
            if (_wanderCooldown > 0 && actor.targetTile >= 0) return;
            _wanderCooldown = Random.Range(15, 45);

            int cx = actor.currentTile % world.mapWidth;
            int cy = actor.currentTile / world.mapWidth;
            int radius = Random.Range(5, 15);

            for (int attempt = 0; attempt < 10; attempt++)
            {
                int dx = Random.Range(-radius, radius + 1);
                int dy = Random.Range(-radius, radius + 1);
                int nx = cx + dx, ny = cy + dy;
                if (nx < 0 || nx >= world.mapWidth || ny < 0 || ny >= world.mapHeight) continue;
                int idx = ny * world.mapWidth + nx;
                if (idx < 0 || idx >= world.tiles.Length) continue;
                var tile = world.tiles[idx];
                if (!tile.exists || !tile.isLand) continue;
                if (tile.biome == GameEnums.BiomeType.Savanna ||
                    tile.biome == GameEnums.BiomeType.TemperateGrassland ||
                    tile.biome == GameEnums.BiomeType.TemperateGrassland ||
                    tile.fertility > 30f)
                {
                    actor.SetTarget(idx);
                    return;
                }
            }
        }

        public void Act(GameWorld world, MapActor actor)
        {
            var tile = world.tiles[actor.currentTile];
            if (tile.fertility > 40f)
            {
                actor.supplies = Mathf.Min(200f, actor.supplies + 5f);
                actor.health = Mathf.Min(100f, actor.health + 1f);
            }
        }
    }

    /// <summary>
    /// 商队 AI：在起点和终点之间往返运输物资。
    /// 到达目的地后完成交易，等待几天后返回。可被土匪劫掠，可被政权征税。
    /// </summary>
    public class CaravanAI : IMapActorAI
    {
        private readonly int _originTile;
        private readonly int _destinationTile;
        private bool _returning = false;
        private int _waitDays = 0;

        public CaravanAI(int originTile, int destinationTile)
        {
            _originTile = originTile;
            _destinationTile = destinationTile;
        }

        public void Think(GameWorld world, MapActor actor)
        {
            if (_waitDays > 0) { _waitDays--; return; }

            int target = _returning ? _originTile : _destinationTile;
            if (target >= 0 && target != actor.currentTile)
                actor.SetTarget(target);
        }

        public void Act(GameWorld world, MapActor actor)
        {
            if (!_returning && actor.currentTile == _destinationTile)
            {
                _waitDays = Random.Range(3, 10);
                _returning = true;
                actor.supplies = Mathf.Min(200f, actor.supplies + 50f);
            }
            else if (_returning && actor.currentTile == _originTile)
            {
                // 完成往返，商队解散
                actor.population = 0;
            }
        }
    }

    /// <summary>
    /// 野怪 AI：在荒野中游荡，袭击附近低秩序聚落，被军队清剿。
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

            if (_attackCooldown <= 0)
            {
                int cx = actor.currentTile % world.mapWidth;
                int cy = actor.currentTile / world.mapWidth;
                for (int dy = -5; dy <= 5; dy++)
                {
                    for (int dx = -5; dx <= 5; dx++)
                    {
                        int nx = cx + dx, ny = cy + dy;
                        if (nx < 0 || nx >= world.mapWidth || ny < 0 || ny >= world.mapHeight) continue;
                        int idx = ny * world.mapWidth + nx;
                        if (idx < 0 || idx >= world.tiles.Length) continue;
                        var t = world.tiles[idx];
                        if (t.development > 0.2f && t.order < 50f)
                        {
                            actor.SetTarget(idx);
                            _attackCooldown = Random.Range(20, 50);
                            return;
                        }
                    }
                }
            }

            if (_wanderCooldown <= 0 || actor.targetTile < 0)
            {
                _wanderCooldown = Random.Range(10, 30);
                int cx = actor.currentTile % world.mapWidth;
                int cy = actor.currentTile / world.mapWidth;
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
                    if (tile.order < 40f)
                    {
                        actor.SetTarget(idx);
                        return;
                    }
                }
            }
        }

        public void Act(GameWorld world, MapActor actor)
        {
            var tile = world.tiles[actor.currentTile];
            int pop = MapActorManager.GetTilePopulation(tile);
            if (tile.development > 0.2f && pop > 0 && _attackCooldown <= 0)
            {
                int casualties = Mathf.Min(pop / 10, actor.population);
                MapActorManager.AddPopulationToTile(ref world.tiles[actor.currentTile], -casualties);
                tile.development = Mathf.Max(0f, tile.development - 0.02f);
                world.tiles[actor.currentTile] = tile;
                actor.supplies = Mathf.Min(200f, actor.supplies + 30f);
                _attackCooldown = Random.Range(30, 60);
                Debug.Log($"[WildBeastAI] 野兽群#{actor.actorId} 袭击地块{actor.currentTile}，伤亡{casualties}人");
            }
        }
    }

    /// <summary>
    /// 土匪 AI：在低秩序地区劫掠商队和村镇，可建立营寨。
    /// 被军队清剿后解散，幸存者可能加入其他土匪或成为流民。
    /// </summary>
    public class BanditAI : IMapActorAI
    {
        private int _wanderCooldown = 0;
        private int _raidCooldown = 0;
        private bool _hasCamp = false;

        public void Think(GameWorld world, MapActor actor)
        {
            _wanderCooldown--;
            _raidCooldown--;

            // 寻找劫掠目标：附近的商队或低秩序聚落
            if (_raidCooldown <= 0)
            {
                var nearby = world.MapActors?.GetActorsNearTile(actor.currentTile, 8);
                if (nearby != null)
                {
                    foreach (var other in nearby)
                    {
                        if (other.type == MapActorType.Caravan && !other.isHostile)
                        {
                            actor.SetTarget(other.currentTile);
                            _raidCooldown = Random.Range(15, 30);
                            return;
                        }
                    }
                }
                // 找低秩序聚落
                int cx = actor.currentTile % world.mapWidth;
                int cy = actor.currentTile / world.mapWidth;
                for (int dy = -6; dy <= 6; dy++)
                {
                    for (int dx = -6; dx <= 6; dx++)
                    {
                        int nx = cx + dx, ny = cy + dy;
                        if (nx < 0 || nx >= world.mapWidth || ny < 0 || ny >= world.mapHeight) continue;
                        int idx = ny * world.mapWidth + nx;
                        if (idx < 0 || idx >= world.tiles.Length) continue;
                        var t = world.tiles[idx];
                        if (t.development > 0.15f && t.order < 40f)
                        {
                            actor.SetTarget(idx);
                            _raidCooldown = Random.Range(15, 30);
                            return;
                        }
                    }
                }
            }

            // 游荡：偏好低秩序、偏远地区
            if (_wanderCooldown <= 0 || actor.targetTile < 0)
            {
                _wanderCooldown = Random.Range(10, 25);
                int cx = actor.currentTile % world.mapWidth;
                int cy = actor.currentTile / world.mapWidth;
                for (int attempt = 0; attempt < 10; attempt++)
                {
                    int dx = Random.Range(-10, 11);
                    int dy = Random.Range(-10, 11);
                    int nx = cx + dx, ny = cy + dy;
                    if (nx < 0 || nx >= world.mapWidth || ny < 0 || ny >= world.mapHeight) continue;
                    int idx = ny * world.mapWidth + nx;
                    if (idx < 0 || idx >= world.tiles.Length) continue;
                    var tile = world.tiles[idx];
                    if (!tile.exists || !tile.isLand) continue;
                    if (tile.order < 35f && tile.ownerRealmId < 0)
                    {
                        actor.SetTarget(idx);
                        return;
                    }
                }
            }
        }

        public void Act(GameWorld world, MapActor actor)
        {
            var tile = world.tiles[actor.currentTile];
            // 劫掠：掠夺物资和人口
            int pop = MapActorManager.GetTilePopulation(tile);
            if (tile.development > 0.1f && pop > 0 && _raidCooldown <= 0)
            {
                int loot = Mathf.Min(pop / 8, actor.population / 2);
                MapActorManager.AddPopulationToTile(ref world.tiles[actor.currentTile], -loot);
                actor.population += loot / 2; // 掳走一半人口
                actor.supplies = Mathf.Min(200f, actor.supplies + 40f);
                tile.order = Mathf.Max(0f, tile.order - 5f);
                world.tiles[actor.currentTile] = tile;
                _raidCooldown = Random.Range(20, 40);
                Debug.Log($"[BanditAI] 土匪#{actor.actorId} 劫掠地块{actor.currentTile}，掳走{loot / 2}人");
            }
            // 在无主低秩序地区建立营寨
            if (!_hasCamp && tile.ownerRealmId < 0 && tile.order < 30f && actor.population > 50)
            {
                _hasCamp = true;
                actor.attributes["hasCamp"] = 1f;
                Debug.Log($"[BanditAI] 土匪#{actor.actorId} 在地块{actor.currentTile}建立营寨");
            }
        }
    }

    /// <summary>
    /// 雇佣兵 AI：各地游荡，可被政权招募。
    /// 未被招募时在贸易中心附近活动，接受雇佣后跟随雇主军队作战。
    /// </summary>
    public class MercenaryAI : IMapActorAI
    {
        private int _wanderCooldown = 0;
        private int _employerRealmId = -1;

        public void Think(GameWorld world, MapActor actor)
        {
            _wanderCooldown--;
            if (_wanderCooldown > 0 && actor.targetTile >= 0) return;
            _wanderCooldown = Random.Range(20, 50);

            // 未被雇佣：向高发展地区（贸易中心）移动寻找雇主
            if (_employerRealmId < 0)
            {
                int cx = actor.currentTile % world.mapWidth;
                int cy = actor.currentTile / world.mapWidth;
                int bestTile = -1;
                float bestDev = 0f;
                for (int dy = -15; dy <= 15; dy++)
                {
                    for (int dx = -15; dx <= 15; dx++)
                    {
                        int nx = cx + dx, ny = cy + dy;
                        if (nx < 0 || nx >= world.mapWidth || ny < 0 || ny >= world.mapHeight) continue;
                        int idx = ny * world.mapWidth + nx;
                        if (idx < 0 || idx >= world.tiles.Length) continue;
                        var t = world.tiles[idx];
                        if (!t.exists || !t.isLand) continue;
                        if (t.development > bestDev && t.ownerRealmId >= 0)
                        { bestDev = t.development; bestTile = idx; }
                    }
                }
                if (bestTile >= 0) actor.SetTarget(bestTile);
            }
        }

        public void Act(GameWorld world, MapActor actor)
        {
            // 雇佣兵在高发展地区消耗补给换金钱（简化：补给消耗减半）
            var tile = world.tiles[actor.currentTile];
            if (tile.development > 0.3f)
            {
                actor.supplies = Mathf.Min(200f, actor.supplies + 2f);
            }
        }

        public void SetEmployer(int realmId) { _employerRealmId = realmId; }
    }

    /// <summary>
    /// 动物灾害 AI：蝗虫/鼠患等，从爆发地向周围农业区扩散，破坏肥力和发展。
    /// 持续一段时间后自然消亡，或被政权通过特定革新/事件扑灭。
    /// </summary>
    public class AnimalDisasterAI : IMapActorAI
    {
        private int _lifespanDays;
        private int _spreadCooldown = 0;

        public AnimalDisasterAI(int lifespanDays = 120)
        {
            _lifespanDays = lifespanDays;
        }

        public void Think(GameWorld world, MapActor actor)
        {
            _lifespanDays--;
            _spreadCooldown--;

            if (_lifespanDays <= 0)
            {
                actor.population = 0; // 灾害自然结束
                return;
            }

            // 向高肥力农业区扩散
            if (_spreadCooldown <= 0 || actor.targetTile < 0)
            {
                _spreadCooldown = Random.Range(5, 15);
                int cx = actor.currentTile % world.mapWidth;
                int cy = actor.currentTile / world.mapWidth;
                for (int attempt = 0; attempt < 8; attempt++)
                {
                    int dx = Random.Range(-4, 5);
                    int dy = Random.Range(-4, 5);
                    int nx = cx + dx, ny = cy + dy;
                    if (nx < 0 || nx >= world.mapWidth || ny < 0 || ny >= world.mapHeight) continue;
                    int idx = ny * world.mapWidth + nx;
                    if (idx < 0 || idx >= world.tiles.Length) continue;
                    var tile = world.tiles[idx];
                    if (!tile.exists || !tile.isLand) continue;
                    if (tile.fertility > 30f)
                    {
                        actor.SetTarget(idx);
                        return;
                    }
                }
            }
        }

        public void Act(GameWorld world, MapActor actor)
        {
            // 破坏当前地块的肥力和发展
            var tile = world.tiles[actor.currentTile];
            tile.fertility = Mathf.Max(0f, tile.fertility - 2f);
            tile.development = Mathf.Max(0f, tile.development - 0.005f);
            world.tiles[actor.currentTile] = tile;
        }
    }

    /// <summary>
    /// 朝圣者 AI：前往宗教圣地，到达后停留一段时间再返回或解散。
    /// 沿途可被征税，到达圣地增加宗教权威（简化：停留后解散）。
    /// </summary>
    public class PilgrimAI : IMapActorAI
    {
        private readonly int _shrineTile;
        private int _stayDays = 0;
        private bool _atShrine = false;

        public PilgrimAI(int shrineTile)
        {
            _shrineTile = shrineTile;
        }

        public void Think(GameWorld world, MapActor actor)
        {
            if (_atShrine)
            {
                _stayDays--;
                if (_stayDays <= 0)
                    actor.population = 0; // 朝圣完成，解散
                return;
            }
            if (actor.currentTile != _shrineTile && _shrineTile >= 0)
                actor.SetTarget(_shrineTile);
        }

        public void Act(GameWorld world, MapActor actor)
        {
            if (actor.currentTile == _shrineTile && !_atShrine)
            {
                _atShrine = true;
                _stayDays = Random.Range(10, 30);
                Debug.Log($"[PilgrimAI] 朝圣者#{actor.actorId} 到达圣地地块{_shrineTile}，停留{_stayDays}天");
            }
        }
    }
}
