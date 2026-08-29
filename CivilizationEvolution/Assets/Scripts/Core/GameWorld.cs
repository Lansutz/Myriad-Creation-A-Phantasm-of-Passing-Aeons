using System.Collections.Generic;
using UnityEngine;
using CivilizationEvolution.Core;
using CivilizationEvolution.Map;
using CivilizationEvolution.Climate;
using CivilizationEvolution.Race;
using CivilizationEvolution.Culture;
using CivilizationEvolution.Economy;
using CivilizationEvolution.Politics;
using CivilizationEvolution.War;
using CivilizationEvolution.Diplomacy;
using CivilizationEvolution.Role;
using CivilizationEvolution.Thought;
using CivilizationEvolution.Disaster;
using CivilizationEvolution.Building;
using CivilizationEvolution.Tech;
using CivilizationEvolution.AI;

namespace CivilizationEvolution.Core
{
    /// <summary>
    /// 游戏世界主类
    /// 管理所有地块数据、子系统、脏标记重算、主循环
    /// </summary>
    public class GameWorld : MonoBehaviour
    {
        [Header("世界配置（ScriptableObject资产，留空则用默认值）")]
        [Tooltip("拖入 ScriptableObjects 目录下的 WorldConfig 资产；留空运行时自动创建默认配置")]
        public WorldConfig config;

        [Header("地图尺寸")]
        public int mapWidth = 128;
        public int mapHeight = 64;

        [Header("时间设置")]
        public float tickInterval = 1f;
        public int daysPerTick = 1;

        // ===== 核心数据 =====
        public TileData[] tiles;
        public Dictionary<int, RaceData> races = new Dictionary<int, RaceData>();
        public Dictionary<int, CultureData> cultures = new Dictionary<int, CultureData>();
        public Dictionary<int, TradeCenter> tradeCenters = new Dictionary<int, TradeCenter>();
        public Dictionary<int, GoodsDef> goodsDefs = new Dictionary<int, GoodsDef>();
        public Dictionary<int, RealmData> realms = new Dictionary<int, RealmData>();
        public Dictionary<int, UnitDef> unitDefs = new Dictionary<int, UnitDef>();

        // ===== 子系统 =====
        private SeaLandGenerator _seaLandGenerator;
        private PlanetClimateSimulator _climateSimulator;
        private EconomyManager _economyManager;
        private CurrencySystem _currencySystem;
        private TaxSystem _taxSystem;
        private PoliticalManager _politicalManager;
        private CombatManager _combatManager;
        private DiplomacyManager _diplomacyManager;
        private CharacterManager _characterManager;
        private ThoughtManager _thoughtManager;
        private DisasterSystem _disasterSystem;
        private DiseaseSystem _diseaseSystem;
        private BuildingSystem _buildingSystem;
        private InnovationTree _innovationTree;
        private AIManager _aiManager;

        // ===== 脏标记 =====
        private HashSet<int> _terrainDirtyTiles = new HashSet<int>();
        private HashSet<int> _climateDirtyTiles = new HashSet<int>();
        private bool _configDirty = false;

        // ===== 时间 =====
        public int currentYear = 1;
        public int currentDay = 1;
        public int currentSeason = 0;
        private float _tickTimer = 0f;

        // ===== 事件系统 =====
        private Queue<GameEvent> _eventQueue = new Queue<GameEvent>();
        private List<IGameEventListener> _eventListeners = new List<IGameEventListener>();

        void Awake()
        {
            InitializeWorld();
        }

        /// <summary>初始化世界</summary>
        public void InitializeWorld()
        {
            // 内容注册表幂等初始化（与 Bootstrap.Awake 无执行顺序依赖）
            if (!ContentRegistry.IsInitialized)
                ContentRegistry.Initialize();

            // 配置资产：拖入了SO则Instantiate运行时副本（避免污染资产），否则创建默认实例
            if (config == null)
            {
                config = WorldConfig.CreateRuntimeInstance();
            }
            else
            {
                config = Instantiate(config);
                config.name = "RuntimeWorldConfig";
                config.hideFlags = HideFlags.DontSave;
            }

            tiles = new TileData[mapWidth * mapHeight];
            for (int i = 0; i < tiles.Length; i++)
            {
                tiles[i] = new TileData
                {
                    tileIndex = i,
                    regionId = i / 16,
                    ownerRealmId = -1,
                    occupyingRealmId = -1,
                    exists = false, // 默认不存在，地形生成时创建陆地地块
                    elevation01 = 0f,
                    fertility = 0.5f,
                    development = 0.1f,
                    stability = 50f,
                    order = 50f,
                    populationBlocks = new List<PopulationBlock>(),
                    buildingLevels = new int[6]
                };
            }

            InitializeSubsystems();
            InitializeGoodsDefs();
            InitializeUnitDefs();
            InitializeTradeCenters();
            InitializeDefaultRaces();
            InitializeDefaultCultures();
            ApplyContentOverrides(); // 内容注册表按 id 覆盖内置默认（数据驱动优先）
            // InitializeDefaultRealms 移到 GenerateTerrain 之后（需要地形数据）

            Debug.Log($"[GameWorld] 世界初始化完成：{mapWidth}x{mapHeight} = {tiles.Length} 地块容量");
        }

        /// <summary>初始化所有子系统</summary>
        private void InitializeSubsystems()
        {
            _seaLandGenerator = new SeaLandGenerator(config, tiles, mapWidth, mapHeight);
            _climateSimulator = new PlanetClimateSimulator(config, tiles, mapWidth, mapHeight, _seaLandGenerator);
            _currencySystem = new CurrencySystem();
            _taxSystem = new TaxSystem();
            _economyManager = new EconomyManager(tiles, tradeCenters, goodsDefs, _currencySystem, _taxSystem);
            _politicalManager = new PoliticalManager(tiles, realms);
            _combatManager = new CombatManager(tiles, unitDefs, _seaLandGenerator);
            _diplomacyManager = new DiplomacyManager(realms);
            _characterManager = new CharacterManager();
            _thoughtManager = new ThoughtManager();
            _disasterSystem = new DisasterSystem(tiles, mapWidth, mapHeight);
            _diseaseSystem = new DiseaseSystem(tiles, _characterManager, mapWidth, mapHeight);
            _buildingSystem = new BuildingSystem(tiles);
            _innovationTree = new InnovationTree();
            _aiManager = new AIManager();
        }

        /// <summary>读档后重建子系统（引用类型无法序列化）</summary>
        public void ReinitializeSubsystems()
        {
            InitializeSubsystems();
            Debug.Log("[GameWorld] 子系统重建完成（读档后）");
        }

        /// <summary>生成随机地形</summary>
        public void GenerateTerrain(int seed = 42)
        {
            System.Random rng = new System.Random(seed);
            float[] fragmentNoise = new float[tiles.Length];

            for (int i = 0; i < tiles.Length; i++)
            {
                int x = i % mapWidth;
                int y = i / mapWidth;
                float nx = (float)x / mapWidth;
                float ny = (float)y / mapHeight;

                float height = 0f;
                height += Mathf.Sin(nx * 6.28f * 3f + seed) * 0.3f;
                height += Mathf.Sin(ny * 6.28f * 2f + seed * 0.5f) * 0.25f;
                height += Mathf.Sin((nx + ny) * 6.28f * 4f) * 0.15f;
                height += (float)(rng.NextDouble() - 0.5) * 0.1f;

                tiles[i].elevation01 = Mathf.Clamp(height, -1f, 1f);
                tiles[i].slopeDegree = Mathf.Abs(height) * 30f;
                fragmentNoise[i] = (float)rng.NextDouble();
                _terrainDirtyTiles.Add(i);
            }

            _seaLandGenerator.SetFragmentNoise(fragmentNoise);
            RecalculateAll();

            // 地形生成后所有地块都存在（随机生成完整矩形世界）
            // 地图编辑器中玩家可删除地块创建非矩形形状
            for (int i = 0; i < tiles.Length; i++)
            {
                tiles[i].exists = true;
            }

            for (int i = 0; i < tiles.Length; i++)
            {
                if (tiles[i].isLand)
                {
                    tiles[i].fertility = CalculateBaseFertility(i);
                }
            }

            // 地形生成后再初始化政权（修复：地形生成前isLand全为false）
            InitializeDefaultRealms();

            Debug.Log($"[GameWorld] 地形生成完成，陆地{GetLandTileCount()}地块，海洋{GetSeaTileCount()}地块");
        }

        /// <summary>计算地块基础肥力</summary>
        private float CalculateBaseFertility(int index)
        {
            ref TileData tile = ref tiles[index];
            float climateScore = Mathf.Clamp(tile.annualPrecipMm / 1000f, 0f, 1f) * 0.5f
                + Mathf.Clamp((tile.annualTemp + 10f) / 35f, 0f, 1f) * 0.3f;
            float terrainScore = (1f - tile.elevation01) * 0.2f;
            float soilScore = tile.soilHumidityPct / 100f * 0.2f;
            return Mathf.Clamp(climateScore + terrainScore + soilScore, 0.05f, 1f);
        }

        /// <summary>全量重算</summary>
        public void RecalculateAll()
        {
            _seaLandGenerator.RecalculateAll();
            _climateSimulator.RecalculateAll();
            _terrainDirtyTiles.Clear();
            _climateDirtyTiles.Clear();
            _configDirty = false;
        }

        /// <summary>脏区增量重算</summary>
        public void RecalculateDirty()
        {
            if (_configDirty)
            {
                RecalculateAll();
                return;
            }

            if (_terrainDirtyTiles.Count > 0)
            {
                _seaLandGenerator.RecalculateDirty(_terrainDirtyTiles);
                foreach (int idx in _terrainDirtyTiles)
                    _climateDirtyTiles.Add(idx);
                _terrainDirtyTiles.Clear();
            }

            if (_climateDirtyTiles.Count > 0)
            {
                _climateSimulator.RecalculateDirty(_climateDirtyTiles);
                _climateDirtyTiles.Clear();
            }
        }

        /// <summary>画笔修改地形</summary>
        public void PaintTerrain(int tileIndex, float newElevation)
        {
            if (tileIndex < 0 || tileIndex >= tiles.Length) return;
            tiles[tileIndex].elevation01 = newElevation;
            _terrainDirtyTiles.Add(tileIndex);
            MarkNeighboursDirty(tileIndex);
        }

        /// <summary>修改世界配置参数</summary>
        public void UpdateConfig(System.Action<WorldConfig> configUpdater)
        {
            configUpdater?.Invoke(config);
            _configDirty = true;
        }

        private void MarkNeighboursDirty(int centerIndex)
        {
            foreach (int n in _seaLandGenerator.GetNeighbourIndices(centerIndex))
                _terrainDirtyTiles.Add(n);
        }

        void Update()
        {
            _tickTimer += Time.deltaTime;
            if (_tickTimer >= tickInterval)
            {
                _tickTimer = 0f;
                GameTick();
            }
        }

        /// <summary>游戏主循环Tick</summary>
        private void GameTick()
        {
            // 1. 脏区重算
            RecalculateDirty();

            // 2. 灾害与疾病
            _disasterSystem.DailyTick(currentDay, currentYear);
            _diseaseSystem.DailyTick(currentDay, currentYear);

            // 3. 经济
            _economyManager.DailyTick();

            // 4. 建筑建造进度
            _buildingSystem.DailyTick();

            // 5. 人口
            PopulationTick();

            // 6. 政治
            _politicalManager.DailyTick();
            PoliticsTick();

            // 7. 外交（先同步世界时钟，供盟约/条约/事件时间戳使用）
            _diplomacyManager.CurrentDay = currentDay;
            _diplomacyManager.DailyTick();

            // 8. 战争（CombatManager暂无DailyTick方法，待实现军队系统后启用）
            // _combatManager.DailyTick(unitDefs, tiles, _seaLandGenerator);

            // 9. 角色与家族
            _characterManager.DailyTick(currentDay, currentYear);

            // 10. 思想与规范
            _thoughtManager.DailyTick(currentYear);

            // 11. AI决策
            _aiManager.DailyTick(realms, tiles, _diplomacyManager, _economyManager, _innovationTree);

            // 12. 事件处理
            ProcessEvents();

            // 13. 时间推进
            AdvanceTime();
        }

        /// <summary>人口Tick：自然增长、满意度、迁徙</summary>
        private void PopulationTick()
        {
            for (int i = 0; i < tiles.Length; i++)
            {
                if (!tiles[i].exists || !tiles[i].isLand || tiles[i].populationBlocks == null) continue;

                for (int j = 0; j < tiles[i].populationBlocks.Count; j++)
                {
                    var pb = tiles[i].populationBlocks[j];

                    if (races.TryGetValue(pb.raceId, out var race))
                    {
                        float growthRate = race.CalculatePopulationGrowthRate(tiles[i], pb.satisfaction);
                        pb.count *= (1f + growthRate * daysPerTick);
                    }

                    float satisfactionDelta = CalculateSatisfactionDelta(i, pb);
                    pb.satisfaction = Mathf.Clamp(pb.satisfaction + satisfactionDelta, 0f, 100f);

                    if (pb.satisfaction < 20f && Random.value < 0.01f)
                    {
                        EnqueueEvent(new GameEvent
                        {
                            eventType = GameEventType.Rebellion,
                            tileIndex = i,
                            severity = 100f - pb.satisfaction
                        });
                    }

                    tiles[i].populationBlocks[j] = pb;
                }

                if (tradeCenters.TryGetValue(tiles[i].regionId, out var tc))
                {
                    float foodStock = tc.inventory.GetValueOrDefault(0, 0f);
                    float totalPop = GetTilePopulation(i);
                    if (foodStock < totalPop * 0.01f && totalPop > 0)
                    {
                        EnqueueEvent(new GameEvent
                        {
                            eventType = GameEventType.Famine,
                            tileIndex = i,
                            severity = 50f
                        });
                    }
                }
            }
        }

        /// <summary>计算人口块满意度变化</summary>
        private float CalculateSatisfactionDelta(int tileIndex, PopulationBlock pb)
        {
            float delta = 0f;
            ref TileData tile = ref tiles[tileIndex];

            delta += (tile.stability - 50f) * 0.01f;

            if (realms.TryGetValue(tile.ownerRealmId, out var realm))
            {
                delta += realm.taxSystem.GetTaxSatisfactionImpact(pb.socialClass) * 0.01f;
            }

            if (cultures.TryGetValue(pb.cultureId, out var culture))
            {
                delta += (pb.culturePenetration - 50f) * 0.005f;
            }

            if (races.TryGetValue(pb.raceId, out var race))
            {
                delta += (race.ClassRelationBaseline - 50f) * 0.005f;
            }

            return delta * daysPerTick;
        }

        /// <summary>政治Tick（兼容层：税收结算+稳定值）</summary>
        private void PoliticsTick()
        {
            foreach (var realm in realms.Values)
            {
                float taxIncome = _economyManager.SettleTaxes(realm.realmId);
                realm.treasury += taxIncome;

                for (int i = 0; i < tiles.Length; i++)
                {
                    if (!tiles[i].exists || tiles[i].ownerRealmId != realm.realmId) continue;
                    float targetStability = 50f + realm.centralization * 20f;
                    tiles[i].stability = Mathf.Lerp(tiles[i].stability, targetStability, 0.01f * daysPerTick);
                }
            }
        }

        /// <summary>时间推进</summary>
        private void AdvanceTime()
        {
            currentDay += daysPerTick;
            if (currentDay > 365)
            {
                currentDay = 1;
                currentYear++;
            }

            int newSeason = (currentDay - 1) / 91;
            if (newSeason != currentSeason && newSeason < 4)
            {
                currentSeason = newSeason;
                _climateSimulator.UpdateForSeason(currentSeason);
                EnqueueEvent(new GameEvent
                {
                    eventType = GameEventType.SeasonChange,
                    severity = currentSeason
                });
            }
        }

        /// <summary>事件系统</summary>
        public void EnqueueEvent(GameEvent evt)
        {
            _eventQueue.Enqueue(evt);
        }

        private void ProcessEvents()
        {
            while (_eventQueue.Count > 0)
            {
                var evt = _eventQueue.Dequeue();
                foreach (var listener in _eventListeners)
                {
                    listener.OnGameEvent(evt);
                }

                switch (evt.eventType)
                {
                    case GameEventType.Famine:
                        HandleFamine(evt);
                        break;
                    case GameEventType.Rebellion:
                        HandleRebellion(evt);
                        break;
                    case GameEventType.SeasonChange:
                        HandleSeasonChange(evt);
                        break;
                    case GameEventType.WarDeclaration:
                        HandleWarDeclaration(evt);
                        break;
                    case GameEventType.PeaceTreaty:
                        HandlePeaceTreaty(evt);
                        break;
                    case GameEventType.Plague:
                        HandlePlague(evt);
                        break;
                    case GameEventType.NaturalDisaster:
                        HandleNaturalDisaster(evt);
                        break;
                    case GameEventType.EconomicCrisis:
                        HandleEconomicCrisis(evt);
                        break;
                }
            }
        }

        private void HandleFamine(GameEvent evt)
        {
            if (evt.tileIndex < 0 || evt.tileIndex >= tiles.Length) return;
            for (int i = 0; i < tiles[evt.tileIndex].populationBlocks.Count; i++)
            {
                var pb = tiles[evt.tileIndex].populationBlocks[i];
                pb.count *= 0.95f;
                pb.satisfaction = Mathf.Max(0f, pb.satisfaction - 20f);
                tiles[evt.tileIndex].populationBlocks[i] = pb;
            }
            tiles[evt.tileIndex].stability = Mathf.Max(0f, tiles[evt.tileIndex].stability - 10f);
            Debug.Log($"[Event] 饥荒 @ 地块{evt.tileIndex}");
        }

        private void HandleRebellion(GameEvent evt)
        {
            if (evt.tileIndex < 0 || evt.tileIndex >= tiles.Length) return;
            tiles[evt.tileIndex].order = Mathf.Max(0f, tiles[evt.tileIndex].order - 15f);
            tiles[evt.tileIndex].stability = Mathf.Max(0f, tiles[evt.tileIndex].stability - 10f);
            Debug.Log($"[Event] 叛乱 @ 地块{evt.tileIndex}，严重度{evt.severity:F0}");
        }

        private void HandleSeasonChange(GameEvent evt)
        {
            string[] seasonNames = { "春", "夏", "秋", "冬" };
            int s = Mathf.Clamp(Mathf.RoundToInt(evt.severity), 0, 3);
            Debug.Log($"[Event] 季节变换：{seasonNames[s]}季");
        }

        private void HandleWarDeclaration(GameEvent evt)
        {
            Debug.Log($"[Event] 战争爆发：政权{evt.realmId}，严重度{evt.severity:F0}");
        }

        private void HandlePeaceTreaty(GameEvent evt)
        {
            Debug.Log($"[Event] 和平条约：政权{evt.realmId}");
        }

        private void HandlePlague(GameEvent evt)
        {
            if (evt.tileIndex >= 0 && evt.tileIndex < tiles.Length)
            {
                _diseaseSystem.OutbreakDisease(DiseaseType.Plague, currentDay, currentYear, evt.tileIndex);
                Debug.Log($"[Event] 瘟疫爆发 @ 地块{evt.tileIndex}");
            }
        }

        private void HandleNaturalDisaster(GameEvent evt)
        {
            if (evt.tileIndex >= 0 && evt.tileIndex < tiles.Length)
            {
                _disasterSystem.TriggerDisaster(DisasterType.Earthquake, currentDay, currentYear, evt.tileIndex);
                Debug.Log($"[Event] 自然灾害 @ 地块{evt.tileIndex}");
            }
        }

        private void HandleEconomicCrisis(GameEvent evt)
        {
            if (realms.TryGetValue(evt.realmId, out var realm))
            {
                realm.treasury *= 0.7f;
                Debug.Log($"[Event] 经济危机：政权{evt.realmId}，国库-30%");
            }
        }

        public void RegisterEventListener(IGameEventListener listener)
        {
            _eventListeners.Add(listener);
        }

        public void UnregisterEventListener(IGameEventListener listener)
        {
            _eventListeners.Remove(listener);
        }

        // ===== 初始化辅助 =====
        private void InitializeGoodsDefs()
        {
            AddGoodsDef(0, "粮食", GameEnums.GoodsCategory.Food, 1.0f, 0.1f, true, 180f);
            AddGoodsDef(1, "肉类", GameEnums.GoodsCategory.Food, 2.0f, 0.15f, true, 14f);
            AddGoodsDef(2, "鱼类", GameEnums.GoodsCategory.Food, 1.5f, 0.1f, true, 7f);
            AddGoodsDef(3, "盐", GameEnums.GoodsCategory.Food, 3.0f, 0.05f, false, 0f);
            AddGoodsDef(10, "谷物", GameEnums.GoodsCategory.Crop, 0.8f, 0.1f, true, 365f);
            AddGoodsDef(11, "棉花", GameEnums.GoodsCategory.Crop, 2.5f, 0.08f, false, 0f);
            AddGoodsDef(20, "马", GameEnums.GoodsCategory.Livestock, 20f, 1f, false, 0f);
            AddGoodsDef(21, "牛", GameEnums.GoodsCategory.Livestock, 15f, 1f, false, 0f);
            AddGoodsDef(22, "羊", GameEnums.GoodsCategory.Livestock, 5f, 0.3f, false, 0f);
            AddGoodsDef(30, "原木", GameEnums.GoodsCategory.Wood, 0.5f, 0.5f, false, 0f);
            AddGoodsDef(31, "加工木材", GameEnums.GoodsCategory.Wood, 1.2f, 0.4f, false, 0f, 30, 0.8f);
            AddGoodsDef(40, "石料", GameEnums.GoodsCategory.Stone, 0.3f, 0.8f, false, 0f);
            AddGoodsDef(50, "铁矿", GameEnums.GoodsCategory.MetalOre, 1.5f, 0.3f, false, 0f);
            AddGoodsDef(51, "铜矿", GameEnums.GoodsCategory.MetalOre, 2.0f, 0.3f, false, 0f);
            AddGoodsDef(60, "金", GameEnums.GoodsCategory.PreciousMetal, 100f, 0.05f, false, 0f);
            AddGoodsDef(61, "银", GameEnums.GoodsCategory.PreciousMetal, 15f, 0.05f, false, 0f);
            AddGoodsDef(70, "武器", GameEnums.GoodsCategory.Equipment, 10f, 0.1f, false, 0f, 50, 0.3f);
            AddGoodsDef(71, "盔甲", GameEnums.GoodsCategory.Equipment, 15f, 0.2f, false, 0f, 50, 0.5f);
            AddGoodsDef(80, "丝绸", GameEnums.GoodsCategory.Luxury, 50f, 0.02f, false, 0f);
            AddGoodsDef(81, "香料", GameEnums.GoodsCategory.Luxury, 30f, 0.01f, false, 0f);
            AddGoodsDef(90, "奴隶", GameEnums.GoodsCategory.Slave, 30f, 0f, false, 0f);
        }

        private void AddGoodsDef(int id, string name, GameEnums.GoodsCategory category,
            float baseValue, float weight, bool hasShelfLife, float shelfLifeDays,
            int processedFrom = -1, float processingRatio = 1f)
        {
            goodsDefs[id] = new GoodsDef
            {
                goodsId = id,
                goodsName = name,
                category = category,
                baseValue = baseValue,
                weight = weight,
                hasShelfLife = hasShelfLife,
                shelfLifeDays = shelfLifeDays,
                processedFromId = processedFrom,
                processingRatio = processingRatio,
                originBiomes = new List<GameEnums.BiomeType>()
            };
        }

        private void InitializeUnitDefs()
        {
            // 步兵
            AddUnitDef(100, "轻装步兵", GameEnums.UnitCategory.Infantry, 1, 10f, 0f, 8f, 60f, 2f, 1f, 50f, 1f);
            AddUnitDef(101, "重装步兵", GameEnums.UnitCategory.Infantry, 2, 15f, 0f, 15f, 70f, 1.5f, 1.5f, 80f, 1.5f);
            AddUnitDef(102, "精锐步兵", GameEnums.UnitCategory.Infantry, 3, 20f, 0f, 22f, 80f, 1.2f, 2f, 120f, 2f);
            AddUnitDef(110, "弓箭手", GameEnums.UnitCategory.Infantry, 1, 5f, 12f, 6f, 50f, 2f, 0.8f, 40f, 1f);
            AddUnitDef(111, "弩手", GameEnums.UnitCategory.Infantry, 2, 6f, 18f, 8f, 55f, 1.5f, 1f, 60f, 1.2f);
            // 骑兵
            AddUnitDef(200, "轻骑兵", GameEnums.UnitCategory.Cavalry, 1, 12f, 5f, 8f, 65f, 4f, 1.5f, 60f, 1f);
            AddUnitDef(201, "重骑兵", GameEnums.UnitCategory.Cavalry, 2, 20f, 8f, 18f, 75f, 3f, 2.5f, 100f, 1.5f);
            AddUnitDef(202, "精锐骑兵", GameEnums.UnitCategory.Cavalry, 3, 28f, 10f, 25f, 85f, 2.5f, 3f, 150f, 2f);
            // 水军
            AddUnitDef(300, "桨帆船", GameEnums.UnitCategory.Navy, 1, 15f, 10f, 10f, 60f, 3f, 2f, 80f, 1f);
            AddUnitDef(301, "帆船战舰", GameEnums.UnitCategory.Navy, 2, 20f, 15f, 15f, 70f, 4f, 2.5f, 120f, 1.5f);
        }

        private void AddUnitDef(int id, string name, GameEnums.UnitCategory category, int tier,
            float melee, float ranged, float def, float morale, float speed, float supply,
            float manpower, float recruitCost)
        {
            unitDefs[id] = new UnitDef
            {
                unitId = id,
                unitName = name,
                category = category,
                tier = tier,
                meleeAttack = melee,
                rangedAttack = ranged,
                defense = def,
                morale = morale,
                speed = speed,
                supplyConsumption = supply,
                manpowerCost = manpower,
                recruitCost = new Dictionary<int, float> { { 70, recruitCost } },
                terrainModifiers = new Dictionary<GameEnums.TerrainTacticType, float>()
            };
        }

        private void InitializeTradeCenters()
        {
            int regionCount = (tiles.Length + 15) / 16;
            for (int r = 0; r < regionCount; r++)
            {
                tradeCenters[r] = new TradeCenter
                {
                    regionId = r,
                    centerName = $"地区{r}贸易中心",
                    centerTileIndex = r * 16
                };
            }
        }

        private void InitializeDefaultRaces()
        {
            races[0] = new RaceData
            {
                raceId = 0,
                raceName = "人族",
                baseLifespan = 1.0f,
                growthRate = 1.0f,
                reproductionRate = 1.0f,
                physicalStrength = 1.0f,
                diseaseResistance = 1.0f,
                environmentalTolerance = 1.0f,
                transformativity = 50f
            };
            races[1] = new RaceData
            {
                raceId = 1,
                raceName = "精灵族",
                baseLifespan = 2.5f,
                growthRate = 0.6f,
                reproductionRate = 0.4f,
                physicalStrength = 0.8f,
                diseaseResistance = 1.2f,
                environmentalTolerance = 0.9f,
                transformativity = 30f
            };
            races[2] = new RaceData
            {
                raceId = 2,
                raceName = "矮人族",
                baseLifespan = 1.8f,
                growthRate = 0.8f,
                reproductionRate = 0.7f,
                physicalStrength = 1.3f,
                diseaseResistance = 1.1f,
                environmentalTolerance = 1.2f,
                transformativity = 70f
            };
        }

        private void InitializeDefaultCultures()
        {
            cultures[0] = new CultureData
            {
                cultureId = 0,
                cultureName = "中原文化",
                stage = GameEnums.CultureStage.HighCivilization,
                livelihoodType = 0,
                mobilityType = 0,
                burialType = 0,
                worshipVector = new float[] { 0.8f, 0.1f, 0.1f },
                materialStyle = 0,
                symbolicFocus = 0,
                environmentAdapt = 0,
                maturity = 0.8f
            };
            cultures[1] = new CultureData
            {
                cultureId = 1,
                cultureName = "游牧文化",
                stage = GameEnums.CultureStage.Chiefdom,
                livelihoodType = 1,
                mobilityType = 1,
                burialType = 1,
                worshipVector = new float[] { 0.1f, 0.8f, 0.1f },
                materialStyle = 1,
                symbolicFocus = 1,
                environmentAdapt = 1,
                maturity = 0.5f
            };
            cultures[2] = new CultureData
            {
                cultureId = 2,
                cultureName = "海洋文化",
                stage = GameEnums.CultureStage.HighCivilization,
                livelihoodType = 2,
                mobilityType = 0,
                burialType = 2,
                worshipVector = new float[] { 0.1f, 0.1f, 0.8f },
                materialStyle = 2,
                symbolicFocus = 2,
                environmentAdapt = 2,
                maturity = 0.7f
            };
        }

        private void InitializeDefaultRealms()
        {
            // 找到陆地地块分配给初始政权
            var landTiles = new List<int>();
            for (int i = 0; i < tiles.Length; i++)
            {
                if (tiles[i].isLand)
                    landTiles.Add(i);
            }

            if (landTiles.Count == 0) return;

            // 创建3个初始政权，各占1/3陆地
            int perRealm = landTiles.Count / 3;
            for (int r = 0; r < 3; r++)
            {
                var realm = new RealmData
                {
                    realmId = r,
                    realmName = r == 0 ? "中原王朝" : (r == 1 ? "游牧汗国" : "海洋城邦"),
                    treasury = 1000f,
                    prestige = 50f,
                    stability = 70f,
                    centralization = 0.5f
                };

                int start = r * perRealm;
                int end = (r == 2) ? landTiles.Count : (r + 1) * perRealm;
                for (int i = start; i < end; i++)
                {
                    int tileIdx = landTiles[i];
                    tiles[tileIdx].ownerRealmId = r;
                    realm.coreTiles.Add(tileIdx);

                    // 给每块地分配初始人口
                    if (tiles[tileIdx].populationBlocks.Count == 0)
                    {
                        tiles[tileIdx].populationBlocks.Add(new PopulationBlock
                        {
                            raceId = r % 3,
                            cultureId = r % 3,
                            count = 10f,
                            satisfaction = 60f,
                            socialClass = GameEnums.SocialClass.Peasant
                        });
                    }
                }

                realms[r] = realm;

                // 为每个政权创建AI控制器
                _aiManager.CreateController(r);
            }

            // 外交关系懒加载：首次GetRelation时自动创建
        }

        /// <summary>
        /// 内容注册表覆盖（数据驱动优先）
        /// ContentRegistry 已加载的 Base/Mods 内容按 id 覆盖内置默认；
        /// 与 ContentRegistry 的 "Mods 同名 Id 覆盖 Base" 语义一致。
        /// 注意：内容包 cultureId/raceId 须 > 0（ContentRegistry 的约定），内置默认为 0,1,2，
        /// 内容 id 与内置重叠时以内容为准。
        /// </summary>
        private void ApplyContentOverrides()
        {
            if (!ContentRegistry.IsInitialized) return;

            int raceOverrides = 0;
            foreach (var kv in ContentRegistry.Races)
            {
                races[kv.Key] = kv.Value;
                raceOverrides++;
            }

            int cultureOverrides = 0;
            foreach (var kv in ContentRegistry.Cultures)
            {
                cultures[kv.Key] = kv.Value.data;
                cultureOverrides++;
            }

            if (raceOverrides > 0 || cultureOverrides > 0)
                Debug.Log($"[GameWorld] 内容注册表覆盖：种族 +{raceOverrides}，文化 +{cultureOverrides}（数据驱动优先）");
        }

        // ===== 地块增删（自由形状地图支持） =====

        /// <summary>创建地块（设置exists=true并初始化默认值）</summary>
        public bool CreateTile(int tileIndex)
        {
            if (tileIndex < 0 || tileIndex >= tiles.Length) return false;
            if (tiles[tileIndex].exists) return false;

            tiles[tileIndex].exists = true;
            tiles[tileIndex].ownerRealmId = -1;
            tiles[tileIndex].occupyingRealmId = -1;
            tiles[tileIndex].stability = 50f;
            tiles[tileIndex].order = 50f;
            tiles[tileIndex].development = 0.1f;
            if (tiles[tileIndex].populationBlocks == null)
                tiles[tileIndex].populationBlocks = new List<PopulationBlock>();
            if (tiles[tileIndex].buildingLevels == null)
                tiles[tileIndex].buildingLevels = new int[6];

            _terrainDirtyTiles.Add(tileIndex);
            MarkNeighboursDirty(tileIndex);
            return true;
        }

        /// <summary>删除地块（设置exists=false，清空数据）</summary>
        public bool RemoveTile(int tileIndex)
        {
            if (tileIndex < 0 || tileIndex >= tiles.Length) return false;
            if (!tiles[tileIndex].exists) return false;

            tiles[tileIndex].exists = false;
            tiles[tileIndex].ownerRealmId = -1;
            tiles[tileIndex].occupyingRealmId = -1;
            tiles[tileIndex].populationBlocks?.Clear();

            MarkNeighboursDirty(tileIndex);
            return true;
        }

        /// <summary>检查地块是否存在</summary>
        public bool TileExists(int tileIndex)
        {
            if (tileIndex < 0 || tileIndex >= tiles.Length) return false;
            return tiles[tileIndex].exists;
        }

        /// <summary>获取所有存在的地块索引列表</summary>
        public List<int> GetValidTiles()
        {
            var result = new List<int>();
            for (int i = 0; i < tiles.Length; i++)
            {
                if (tiles[i].exists) result.Add(i);
            }
            return result;
        }

        /// <summary>获取存在的地块数量</summary>
        public int GetValidTileCount()
        {
            int count = 0;
            for (int i = 0; i < tiles.Length; i++)
            {
                if (tiles[i].exists) count++;
            }
            return count;
        }

        /// <summary>获取邻接地块（支持左右连通环绕）</summary>
        public List<int> GetNeighbours(int tileIndex)
        {
            return TileGrid.GetNeighbours(tileIndex, mapWidth, mapHeight, config.wrapX, config.wrapY);
        }

        // ===== 查询接口 =====
        public float GetTilePopulation(int tileIndex)
        {
            if (tileIndex < 0 || tileIndex >= tiles.Length) return 0f;
            float total = 0f;
            if (tiles[tileIndex].populationBlocks != null)
                foreach (var pb in tiles[tileIndex].populationBlocks)
                    total += pb.count;
            return total;
        }

        public int GetLandTileCount() => _seaLandGenerator.GetTotalLandTiles();
        public int GetSeaTileCount() => _seaLandGenerator.GetTotalSeaTiles();
        public int GetConnectedSeaCount() => _seaLandGenerator.GetConnectedSeaCount();
        public TileData GetTile(int x, int y) => tiles[y * mapWidth + x];
        public TileData GetTile(int index) => tiles[index];
        public EconomyManager GetEconomyManager() => _economyManager;
        public SeaLandGenerator GetSeaLandGenerator() => _seaLandGenerator;
        public PlanetClimateSimulator GetClimateSimulator() => _climateSimulator;
        public CombatManager GetCombatManager() => _combatManager;
        public DiplomacyManager GetDiplomacyManager() => _diplomacyManager;
        public CharacterManager GetCharacterManager() => _characterManager;
        public ThoughtManager GetThoughtManager() => _thoughtManager;
        public DisasterSystem GetDisasterSystem() => _disasterSystem;
        public DiseaseSystem GetDiseaseSystem() => _diseaseSystem;
        public BuildingSystem GetBuildingSystem() => _buildingSystem;
        public InnovationTree GetInnovationTree() => _innovationTree;
        public AIManager GetAIManager() => _aiManager;
        public PoliticalManager GetPoliticalManager() => _politicalManager;
    }

    /// <summary>游戏事件类型</summary>
    public enum GameEventType
    {
        Famine,
        Rebellion,
        SeasonChange,
        WarDeclaration,
        PeaceTreaty,
        Plague,
        NaturalDisaster,
        EconomicCrisis
    }

    /// <summary>游戏事件</summary>
    [System.Serializable]
    public struct GameEvent
    {
        public GameEventType eventType;
        public int tileIndex;
        public int realmId;
        public float severity;
        public string description;
    }

    /// <summary>游戏事件监听器接口</summary>
    public interface IGameEventListener
    {
        void OnGameEvent(GameEvent evt);
    }
}
