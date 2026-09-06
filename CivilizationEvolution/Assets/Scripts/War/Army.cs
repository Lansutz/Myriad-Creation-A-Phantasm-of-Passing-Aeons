using System.Collections.Generic;
using UnityEngine;
using CivilizationEvolution.Core;
using CivilizationEvolution.Diplomacy;
using CivilizationEvolution.Map;

namespace CivilizationEvolution.War
{
    public class Army
    {
        public int armyId;
        public string armyName;
        public int ownerRealmId;
        public int commanderId; // 将领角色ID
        public int currentTileIndex;

        // 兵力块：unitId -> 数量
        [System.NonSerialized] public Dictionary<int, float> unitCounts = new Dictionary<int, float>();


        // 状态
        public GameEnums.CombatState state = GameEnums.CombatState.Idle;
        public float organization = 100f; // 组织度 0~100
        public float morale = 100f; // 士气 0~100
        public float supply = 100f; // 补给 0~100

        // 移动
        public List<int> movePath = new List<int>();
        public int moveTargetTile = -1;
        public float moveProgress = 0f;

        /// <summary>计算军团总兵力</summary>
        public float GetTotalManpower(Dictionary<int, UnitDef> unitDefs)
        {
            float total = 0f;
            foreach (var kv in unitCounts)
            {
                if (unitDefs.TryGetValue(kv.Key, out var def))
                    total += kv.Value * def.manpowerCost;
            }
            return total;
        }

        /// <summary>计算军团战斗力</summary>
        public float CalculateCombatPower(Dictionary<int, UnitDef> unitDefs, TileData tile, int raceId = -1)
        {
            float power = 0f;
            var terrainType = GetTerrainTacticType(tile);

            foreach (var kv in unitCounts)
            {
                if (!unitDefs.TryGetValue(kv.Key, out var def)) continue;

                float unitPower = (def.meleeAttack + def.rangedAttack + def.defense) * kv.Value;
                float terrainMod = def.terrainModifiers.GetValueOrDefault(terrainType, 1f);
                power += unitPower * terrainMod;
            }

            // 组织度和士气修正
            power *= (0.5f + organization / 200f) * (0.5f + morale / 200f);

            // 补给不足修正
            if (supply < 30f)
                power *= 0.5f + supply / 60f;

            return power;
        }

        /// <summary>每日军团Tick</summary>
        public void DailyTick(Dictionary<int, UnitDef> unitDefs, TileData[] tiles, SeaLandGenerator seaLand)
        {
            // 补给消耗
            float dailySupply = 0f;
            foreach (var kv in unitCounts)
            {
                if (unitDefs.TryGetValue(kv.Key, out var def))
                    dailySupply += kv.Value * def.supplyConsumption;
            }
            supply = Mathf.Max(0f, supply - dailySupply);

            // 补给不足：组织度和士气下降
            if (supply <= 0f)
            {
                organization = Mathf.Max(0f, organization - 5f);
                morale = Mathf.Max(0f, morale - 10f);

                // 非本土作战触发劫掠
                if (tiles[currentTileIndex].ownerRealmId != ownerRealmId)
                {
                    // 劫掠：降低地块稳定值和发展度
                    tiles[currentTileIndex].stability = Mathf.Max(0f, tiles[currentTileIndex].stability - 3f);
                    tiles[currentTileIndex].development = Mathf.Max(0f, tiles[currentTileIndex].development - 0.01f);
                }
            }

            // 移动
            if (state == GameEnums.CombatState.Marching && movePath.Count > 0)
            {
                MoveTick(unitDefs, tiles, seaLand);
            }
        }

        private void MoveTick(Dictionary<int, UnitDef> unitDefs, TileData[] tiles, SeaLandGenerator seaLand)
        {
            if (movePath.Count == 0) return;

            // 计算移动速度（取最慢兵种）
            float minSpeed = float.MaxValue;
            foreach (var kv in unitCounts)
            {
                if (unitDefs.TryGetValue(kv.Key, out var def))
                    minSpeed = Mathf.Min(minSpeed, def.speed);
            }
            if (minSpeed == float.MaxValue) minSpeed = 1f;

            // 地形修正
            float terrainMod = 1f - tiles[currentTileIndex].slopeDegree / 90f * 0.5f;
            if (tiles[currentTileIndex].roadLevel == GameEnums.RoadLevel.None)
                terrainMod *= 0.7f;

            moveProgress += minSpeed * terrainMod;
            if (moveProgress >= 1f)
            {
                moveProgress = 0f;
                currentTileIndex = movePath[0];
                movePath.RemoveAt(0);

                if (movePath.Count == 0)
                {
                    state = GameEnums.CombatState.Idle;
                }
            }
        }

        /// <summary>设置移动目标</summary>
        public void SetMoveTarget(int targetTile, TileData[] tiles, SeaLandGenerator seaLand, int mapWidth)
        {
            movePath = FindPath(currentTileIndex, targetTile, tiles, seaLand, mapWidth);
            moveTargetTile = targetTile;
            state = GameEnums.CombatState.Marching;
            moveProgress = 0f;
        }

        /// <summary>A*寻路（简化版，用List模拟优先队列保证兼容性）</summary>
        private List<int> FindPath(int start, int end, TileData[] tiles, SeaLandGenerator seaLand, int mapWidth)
        {
            var path = new List<int>();
            var openSet = new List<int>();
            var cameFrom = new Dictionary<int, int>();
            var gScore = new Dictionary<int, float>();
            var fScore = new Dictionary<int, float>();

            openSet.Add(start);
            gScore[start] = 0f;
            fScore[start] = Heuristic(start, end, mapWidth);

            while (openSet.Count > 0)
            {
                // 找fScore最小的节点
                int current = openSet[0];
                float minF = fScore.GetValueOrDefault(current, float.MaxValue);
                for (int i = 1; i < openSet.Count; i++)
                {
                    float f = fScore.GetValueOrDefault(openSet[i], float.MaxValue);
                    if (f < minF)
                    {
                        minF = f;
                        current = openSet[i];
                    }
                }

                openSet.Remove(current);

                if (current == end)
                {
                    while (cameFrom.ContainsKey(current))
                    {
                        path.Insert(0, current);
                        current = cameFrom[current];
                    }
                    return path;
                }

                foreach (int neighbor in seaLand.GetNeighbourIndices(current))
                {
                    if (!tiles[neighbor].isLand && tiles[neighbor].oceanTier == GameEnums.OceanTier.DeepSea)
                        continue;

                    float moveCost = 1f + tiles[neighbor].slopeDegree / 45f;
                    float tentativeG = gScore.GetValueOrDefault(current, float.MaxValue) + moveCost;

                    if (tentativeG < gScore.GetValueOrDefault(neighbor, float.MaxValue))
                    {
                        cameFrom[neighbor] = current;
                        gScore[neighbor] = tentativeG;
                        fScore[neighbor] = tentativeG + Heuristic(neighbor, end, mapWidth);
                        if (!openSet.Contains(neighbor))
                            openSet.Add(neighbor);
                    }
                }
            }

            return path;
        }

        private float Heuristic(int a, int b, int mapWidth)
        {
            int ax = a % mapWidth, ay = a / mapWidth;
            int bx = b % mapWidth, by = b / mapWidth;
            return Mathf.Abs(ax - bx) + Mathf.Abs(ay - by);
        }

        private GameEnums.TerrainTacticType GetTerrainTacticType(TileData tile)
        {
            if (tile.elevation01 > 0.5f) return GameEnums.TerrainTacticType.Mountain;
            if (tile.biome == GameEnums.BiomeType.BorealForest || tile.biome == GameEnums.BiomeType.DeciduousForest || tile.biome == GameEnums.BiomeType.TropicalRainforest)
                return GameEnums.TerrainTacticType.Forest;
            if (tile.biome == GameEnums.BiomeType.WetMarshPlain) return GameEnums.TerrainTacticType.Wetland;
            if (tile.biome == GameEnums.BiomeType.HotDesert) return GameEnums.TerrainTacticType.Desert;
            if (tile.buildingLevels[3] > 0) return GameEnums.TerrainTacticType.Fortress;
            return GameEnums.TerrainTacticType.Plain;
        }
    }
}
