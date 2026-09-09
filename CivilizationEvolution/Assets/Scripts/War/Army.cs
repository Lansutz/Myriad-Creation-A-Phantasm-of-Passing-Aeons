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


 // ===== 军事系统常量 =====
 /// <summary>每个人口块代表的人数（从50改为100，方便军队编制/补员/征兵计算）</summary>
        public const int ManpowerPerBlock = 100;
 /// <summary>补员批量比例：每次补员增加满编的10%（避免1人1人加造成CPU负担）</summary>
        public const float ReinforceBatchRatio = 0.10f;
 /// <summary>训练度对组织度损耗的减免系数：训练度100时组织度损耗降低50%</summary>
        public const float TrainingOrgLossReductionMax = 0.5f;
 /// <summary>组织度阈值：低于此值部队暂时失去战斗力</summary>
        public const float IneffectiveOrgThreshold = 20f;
 /// <summary>总伤亡率阈值：超过此值部队暂时失去战斗力</summary>
        public const float IneffectiveTotalCasualtyThreshold = 0.5f;
 /// <summary>瞬时伤亡率阈值：超过此值瞬间使组织度下降</summary>
        public const float InstantCasualtyOrgShockThreshold = 0.2f;
 /// <summary>战斗结束阈值：总人数不足原有的此比例时战斗结束</summary>
        public const float BattleEndManpowerRatio = 0.3f;
 /// <summary>瞬时伤亡率导致的组织度瞬间下降系数</summary>
        public const float InstantCasualtyOrgShock = 15f;

 // 状态
        public GameEnums.CombatState state = GameEnums.CombatState.Idle;
        public float organization = 100f; // 组织度 0~100
        public float morale = 100f; // 士气 0~100
        public float supply = 100f; // 补给 0~100
        public float training = 50f; // 训练度 0~100（新兵低，老兵高；训练度越高组织度掉越慢）

 // ===== 伤亡率与战斗状态 =====
 /// <summary>原始满编兵力（战斗开始时记录，用于计算伤亡率和战斗结束判定）</summary>
        [System.NonSerialized] public float originalManpower = 0f;
 /// <summary>总伤亡率（累计伤亡/原始兵力，0~1）</summary>
        [System.NonSerialized] public float totalCasualtyRate = 0f;
 /// <summary>瞬时伤亡率（当前战斗阶段的伤亡/原始兵力，0~1）</summary>
        [System.NonSerialized] public float instantCasualtyRate = 0f;
 /// <summary>是否暂时失去战斗力（组织度过低或总伤亡率过高）</summary>
        [System.NonSerialized] public bool isCombatIneffective = false;
 /// <summary>追击效率修正（敌方组织度过低时增加）</summary>
        [System.NonSerialized] public float pursuitBonus = 0f;

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

 // 组织度、士气、训练度修正
 // 训练度50为基准，100时+15%战斗力，0时-15%
            float trainingMod = 0.85f + training / 100f * 0.3f;
            power *= (0.5f + organization / 200f) * (0.5f + morale / 200f) * trainingMod;

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
 // 训练度越高，组织度掉的越慢（训练度100时降低50%损耗）
            if (supply <= 0f)
            {
                float trainingReduction = 1f - (training / 100f) * TrainingOrgLossReductionMax;
                organization = Mathf.Max(0f, organization - 5f * trainingReduction);
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

 // 训练度自然变化：和平/驻扎时缓慢提升，战斗/行军后可能下降
            if (state == GameEnums.CombatState.Idle && supply > 30f)
            {
 // 驻扎训练：每天+0.1训练度（有补给时）
                training = Mathf.Min(100f, training + 0.1f);
            }
        }

 /// <summary>批量补员：按满编的10%批量增加，避免1人1人加造成CPU负担</summary>
 /// <param name="unitId">兵种ID</param>
 /// <param name="targetCount">目标数量（满编数）</param>
 /// <param name="availableManpower">可用人力</param>
 /// <returns>实际消耗的人力</returns>
        public int ReinforceUnit(int unitId, float targetCount, int availableManpower, Dictionary<int, UnitDef> unitDefs)
        {
            if (!unitCounts.ContainsKey(unitId)) return 0;
            if (!unitDefs.TryGetValue(unitId, out var def)) return 0;

            float current = unitCounts[unitId];
            if (current >= targetCount) return 0;

 // 批量补员：每次增加满编的10%（即 ManpowerPerBlock * 10% = 10人）
            float batchAmount = targetCount * ReinforceBatchRatio;
            float addAmount = Mathf.Min(batchAmount, targetCount - current);

 // 计算需要的人力
            int manpowerNeeded = Mathf.CeilToInt(addAmount * def.manpowerCost);
            if (manpowerNeeded > availableManpower)
            {
 // 人力不足，按可用人力计算能补多少
                addAmount = (float)availableManpower / def.manpowerCost;
                manpowerNeeded = availableManpower;
            }

            unitCounts[unitId] = current + addAmount;

 // 新兵降低训练度：补员越多，训练度下降越多
            float totalManpower = GetTotalManpower(unitDefs);
            if (totalManpower > 0)
            {
                float newRecruitRatio = (addAmount * def.manpowerCost) / totalManpower;
                training = Mathf.Max(0f, training - newRecruitRatio * 20f);
            }

            return manpowerNeeded;
        }

 /// <summary>获取训练度等级描述</summary>
        public string GetTrainingLevel()
        {
            if (training >= 80f) return "精锐";
            if (training >= 60f) return "老兵";
            if (training >= 40f) return "常备";
            if (training >= 20f) return "新兵";
            return "民兵";
        }

        #region 战斗状态与伤亡率

 /// <summary>战斗开始时记录原始兵力</summary>
        public void RecordBattleStartManpower(Dictionary<int, UnitDef> unitDefs)
        {
            originalManpower = GetTotalManpower(unitDefs);
            totalCasualtyRate = 0f;
            instantCasualtyRate = 0f;
            isCombatIneffective = false;
            pursuitBonus = 0f;
        }

 /// <summary>更新伤亡率（每次 ApplyLosses 后调用）</summary>
        public void UpdateCasualtyRates(Dictionary<int, UnitDef> unitDefs, float lossesThisPhase)
        {
            if (originalManpower <= 0f) return;
            float current = GetTotalManpower(unitDefs);
            totalCasualtyRate = Mathf.Clamp01(1f - current / originalManpower);
            instantCasualtyRate = Mathf.Clamp01(lossesThisPhase / originalManpower);
        }

 /// <summary>检查是否暂时失去战斗力（组织度过低 OR 总伤亡率过高）</summary>
        public bool CheckCombatIneffective()
        {
            isCombatIneffective = organization <= IneffectiveOrgThreshold
                                || totalCasualtyRate >= IneffectiveTotalCasualtyThreshold;
            return isCombatIneffective;
        }

 /// <summary>瞬时伤亡率冲击：如果瞬时伤亡率过高，瞬间使组织度下降</summary>
        public void ApplyInstantCasualtyOrgShock()
        {
            if (instantCasualtyRate >= InstantCasualtyOrgShockThreshold)
            {
                float shock = InstantCasualtyOrgShock * (instantCasualtyRate / InstantCasualtyOrgShockThreshold);
                organization = Mathf.Max(0f, organization - shock);
            }
        }

 /// <summary>检查战斗是否应结束（总人数不足原有的阈值比例）</summary>
        public bool CheckBattleEnd(Dictionary<int, UnitDef> unitDefs)
        {
            if (originalManpower <= 0f) return true;
            float current = GetTotalManpower(unitDefs);
            return current / originalManpower <= BattleEndManpowerRatio;
        }

 /// 逃跑判定：取决于指挥官能力、士气、训练度。
 /// 返回 true=逃跑，false=死战/投降。
        public bool ShouldRetreat(float commanderAbility = 50f)
        {
 // 指挥官能力越高，越懂得适时撤退（但也可能死战）
 // 基础逃跑概率 = 50%，受以下修正：
            float retreatChance = 0.5f;
 // 士气越低，越容易逃跑
            retreatChance += (50f - morale) / 100f * 0.4f;
 // 训练度越低，越容易逃跑（精锐更可能死战）
            retreatChance += (50f - training) / 100f * 0.2f;
 // 指挥官能力适中时最可能撤退，过高或过低都可能死战
            float commanderFactor = 1f - Mathf.Abs(commanderAbility - 50f) / 50f;
            retreatChance *= 0.6f + commanderFactor * 0.4f;
 // 组织度极低时必然逃跑
            if (organization <= 5f) retreatChance = 1f;

            return UnityEngine.Random.value < Mathf.Clamp01(retreatChance);
        }

 /// <summary>计算追击效率：敌方组织度过低时增加我方追击效率（造成更多伤亡）</summary>
        public float CalculatePursuitEfficiency(Army enemy)
        {
            if (enemy == null || !enemy.isCombatIneffective) return 1f;
 // 敌方组织度越低，追击效率越高
            float orgFactor = 1f + (IneffectiveOrgThreshold - enemy.organization) / IneffectiveOrgThreshold * 0.5f;
 // 训练度越高，追击效率越高（精锐更擅长追击）
            float trainingFactor = 0.8f + training / 100f * 0.4f;
            return orgFactor * trainingFactor;
        }

        #endregion

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
