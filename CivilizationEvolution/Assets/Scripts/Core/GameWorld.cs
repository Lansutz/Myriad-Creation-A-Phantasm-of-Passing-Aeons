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
        public int randomSeed = 42;
        [Header("地图环绕模式")]
        public MapWrapMode wrapMode = MapWrapMode.Cylindrical;

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
        /// <summary>玩家政权ID（-1表示无玩家/观察者模式）</summary>
        public int PlayerRealmId = -1;
        public Dictionary<int, UnitDef> unitDefs = new Dictionary<int, UnitDef>();
        /// <summary>省份（provinceId → Province——地图结构层）</summary>
        public Dictionary<int, Province> provinces = new Dictionary<int, Province>();
        /// <summary>子地块/Burg（burgId → BurgData——省份内定居点，对齐 CK3 男爵领）</summary>
        public Dictionary<int, BurgData> burgs = new Dictionary<int, BurgData>();
        /// <summary>军队（armyId → Army——战争闭环）</summary>
        public Dictionary<int, Army> armies = new Dictionary<int, Army>();
        /// <summary>战争状态列表（战争闭环——分数/胜负判定）</summary>
        private readonly List<WarState> _wars = new List<WarState>();
        /// <summary>宗教运行时状态（每教统一个 FaithSystem——热忱/信徒/圣地——
        /// 由 ReligionCatalog 初始化）</summary>
        private readonly List<FaithSystem> _faithSystems = new List<FaithSystem>();
        /// <summary>圣地丢失已处理标记（faithId→已计热忱的圣地 tile——防重复刷）</summary>
        private readonly HashSet<int> _holySiteLostProcessed = new HashSet<int>();
        private int _nextWarId = 1;
        private int _nextArmyId = 1;

        // ===== 子系统 =====
        private SeaLandGenerator _seaLandGenerator;
        private PlanetClimateSimulator _climateSimulator;
        private PlanetTerrainGenerator _planetTerrainGenerator;
        private AtmosphericCirculation _atmosphericCirculation;
        private HydraulicErosion _hydraulicErosion;
        public MapGenerationConfig GenConfig = new MapGenerationConfig();
        private EconomyManager _economyManager;
        private CurrencySystem _currencySystem;
        private TaxSystem _taxSystem;
        private PoliticalManager _politicalManager;

        // 社会-派系-政体变迁链路（阶层需求→政治能量→派系组织化→关键节点博弈）
        private SocietyManager _societyManager = new SocietyManager();
        private FactionManager _factionManager = new FactionManager();
        private RegimeChangeDynamics _regimeDynamics;
        private readonly Dictionary<int, RealmSociety> _societyCache = new Dictionary<int, RealmSociety>();
        private float _differentiationTimer = 0f;
        private const float DifferentiationIntervalDays = 25f; // 社会分工/阶层分化推进间隔（天）
        private CombatManager _combatManager;
        private DiplomacyManager _diplomacyManager;
        private CharacterManager _characterManager;
        private ThoughtManager _thoughtManager;
        private DisasterSystem _disasterSystem;
        private DiseaseSystem _diseaseSystem;
        private BuildingSystem _buildingSystem;
        private InnovationTree _innovationTree;
        private Chronicle _chronicle;
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
                    provinceId = -1, // 省份归属（沃罗诺伊省区生成后赋值）
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
            _characterManager.Races = races; // 注入种族表（DNA 表达/混血基准依赖）
            _characterManager.Economy = _economyManager; // 注入经济（角色饮食联动）
            _characterManager.Tiles = tiles;   // 注入地块表（饮食按政权地块定位贸易中心）
            _characterManager.Realms = realms; // 注入政权表（饮食/领地定位）
            _thoughtManager = new ThoughtManager();
            _disasterSystem = new DisasterSystem(tiles, mapWidth, mapHeight);
            _diseaseSystem = new DiseaseSystem(tiles, _characterManager, mapWidth, mapHeight);
            _buildingSystem = new BuildingSystem(tiles);
            _innovationTree = new InnovationTree();
            _characterManager.Innovations = _innovationTree; // 注入革新树（家族传统解锁前置依赖）
            _chronicle = new Chronicle(); // 编年史（世界大事日志）
            // 政体变迁动力学需要革新树（可行性约束）与编年史（记录节点）
            _regimeDynamics = new RegimeChangeDynamics(_innovationTree, _chronicle);
            _diplomacyManager.Chronicle = _chronicle;
            _aiManager = new AIManager();
        }

        /// <summary>读档后重建子系统（引用类型无法序列化）</summary>
        public void ReinitializeSubsystems()
        {
            InitializeSubsystems();
            Debug.Log("[GameWorld] 子系统重建完成（读档后）");
        }

        /// <summary>生成随机地形（球形行星生成器：3D球面Simplex噪声+域扭曲+温度降水模拟+55群系）</summary>
        public void GenerateTerrain(int seed = 42)
        {
            randomSeed = seed;

            // 球形行星地形生成器：3D球面Simplex噪声+域扭曲+山脊叠加+温度降水+55群系+肥力
            var planetGen = new PlanetTerrainGenerator(seed);
            planetGen.Generate(tiles, mapWidth, mapHeight);

            // 标记所有地块为脏，触发渲染刷新
            for (int i = 0; i < tiles.Length; i++)
                _terrainDirtyTiles.Add(i);

            // 河流追踪（须在 isLand 判定完成后，复用旧TerrainGenerator的河流算法）
            var terrainGen = new TerrainGenerator(seed);
            terrainGen.TrackRivers(tiles);

            // 地形生成后再初始化政权（修复：地形生成前isLand全为false）
            InitializeDefaultRealms();

            // 省份生成（沃罗诺伊+Lloyd 松弛——地图结构层）
            GenerateProvinces(seed);
            GenerateBurgs(seed);

            Debug.Log($"[GameWorld] 球形地形生成完成，陆地{GetLandTileCount()}地块，海洋{GetSeaTileCount()}地块，省份{provinces.Count}个");
        }

        
        /// <summary>使用GenConfig生成地形（编辑器面板调用）</summary>
        public void GenerateTerrainWithConfig()
        {
            if (GenConfig == null) GenConfig = new MapGenerationConfig();

            int seed = GenConfig.GetActualSeed();
            randomSeed = seed;

            // 应用地图尺寸
            var (w, h) = GenConfig.GetMapDimensions();
            if (w != mapWidth || h != mapHeight)
            {
                mapWidth = w; mapHeight = h;
                tiles = new TileData[mapWidth * mapHeight];
                for (int i = 0; i < tiles.Length; i++)
                {
                    tiles[i] = new TileData { tileIndex = i, provinceId = -1, ownerRealmId = -1, occupyingRealmId = -1, exists = false, elevation01 = 0f, fertility = 0.5f, development = 0.1f, stability = 50f, order = 50f, populationBlocks = new List<PopulationBlock>(), buildingLevels = new int[6] };
                }
                InitializeSubsystems();
            }

            // 创建并配置地形生成器
            _planetTerrainGenerator = new PlanetTerrainGenerator(seed);
            GenConfig.ApplyToGenerator(_planetTerrainGenerator);
            _planetTerrainGenerator.Generate(tiles, mapWidth, mapHeight);

            // 河流追踪
            var terrainGen = new TerrainGenerator(seed);
            terrainGen.TrackRivers(tiles);

            // 标记脏
            for (int i = 0; i < tiles.Length; i++) _terrainDirtyTiles.Add(i);

            // 生成省份和Burg
            GenerateProvinces(seed);
            GenerateBurgs(seed);
            InitializeDefaultRealms();

            Debug.Log($"[GameWorld] 配置化地形生成完成：陆地{GetLandTileCount()}，海洋{GetSeaTileCount()}，省份{provinces.Count}");
        }

        /// <summary>计算气候（大气环流GCM：温度/降水/风/气压）</summary>
        public void CalculateClimate()
        {
            if (_atmosphericCirculation == null)
                _atmosphericCirculation = new AtmosphericCirculation(mapWidth, mapHeight);

            // 从tiles提取高程、海陆、温度数组
            float[] elevation = new float[tiles.Length];
            bool[] isLand = new bool[tiles.Length];
            float[] temperature = new float[tiles.Length];
            for (int i = 0; i < tiles.Length; i++)
            {
                elevation[i] = tiles[i].elevation01;
                isLand[i] = tiles[i].isLand;
                temperature[i] = tiles[i].annualTemp;
            }

            // 应用GenConfig气候参数
            GenConfig.ApplyToGCM(_atmosphericCirculation);

            // 运行大气环流模拟
            _atmosphericCirculation.Run(elevation, isLand, temperature);

            // 写回tiles
            for (int i = 0; i < tiles.Length; i++)
            {
                tiles[i].annualPrecipMm = _atmosphericCirculation.Precipitation[i];
                tiles[i].airHumidityPct = Mathf.Clamp(_atmosphericCirculation.SpecificHumidity[i] * 10000f, 0f, 100f);
                _climateDirtyTiles.Add(i);
            }

            // 用Holdridge分类器更新生物群系（静态类，直接调用）
            for (int i = 0; i < tiles.Length; i++)
            {
                if (tiles[i].isLand)
                {
                    float latAbs = Mathf.Abs((float)(TileGrid.ToY(i, mapWidth) - mapHeight * 0.5) / mapHeight * 180f);
                    tiles[i].biome = HoldridgeBiomeClassifier.Classify(
                        tiles[i].annualTemp, tiles[i].annualPrecipMm, tiles[i].elevation01,
                        tiles[i].isLand, tiles[i].isCoast, tiles[i].isRiver,
                        tiles[i].slopeDegree, latAbs);
                }
            }

            Debug.Log($"[GameWorld] 气候计算完成：GCM已运行，降水/湿度/生物群系已更新");
        }

        /// <summary>重算水文（水力侵蚀+河网）</summary>
        public void RecalculateHydrology()
        {
            if (_hydraulicErosion == null)
                _hydraulicErosion = new HydraulicErosion(mapWidth, mapHeight, randomSeed);

            // 从tiles提取高程和海陆
            float[] elevation = new float[tiles.Length];
            bool[] isLand = new bool[tiles.Length];
            for (int i = 0; i < tiles.Length; i++)
            {
                elevation[i] = tiles[i].elevation01;
                isLand[i] = tiles[i].isLand;
            }

            // 应用GenConfig水文参数
            GenConfig.ApplyToErosion(_hydraulicErosion, tiles.Length);
            _hydraulicErosion.WrapX = config.wrapX;

            // 运行水力侵蚀
            _hydraulicErosion.Run(elevation, isLand);
            _hydraulicErosion.ApplyToTiles(tiles, elevation);

            // 重新追踪河流
            var terrainGen = new TerrainGenerator(randomSeed);
            terrainGen.TrackRivers(tiles);

            // 标记脏
            for (int i = 0; i < tiles.Length; i++) _terrainDirtyTiles.Add(i);

            Debug.Log($"[GameWorld] 水文重算完成：水力侵蚀+河网追踪已执行");
        }

        /// <summary>全部生成（地形+气候+水文，编辑器一键生成）</summary>
        public void GenerateAll()
        {
            GenerateTerrainWithConfig();
            CalculateClimate();
            RecalculateHydrology();
            Debug.Log("[GameWorld] 全部生成完成");
        }
        private void GenerateProvinces(int seed)
        {
            // 省份密度随地图尺寸动态调整：大地图每省地块更多，避免省份数量爆炸
            // 公式：Max(48, sqrt(总地块) * 0.3)
            // 128×64(8192)→48地块/省≈170省；512×256(131072)→109≈1200省；1920×1080(2073600)→432≈4800省（对齐参考项目5226省）
            int totalTiles = mapWidth * mapHeight;
            int cellsPerProvince = Mathf.Max(48, (int)(Mathf.Sqrt(totalTiles) * 0.3));

            var generator = new ProvinceGenerator(tiles, mapWidth, mapHeight, config.wrapX);
            provinces = generator.Generate(seed + 777, cellsPerProvince,
                ProvinceGenerator.DefaultLloydIterations);
            Debug.Log($"[GameWorld] 省份生成：{cellsPerProvince}地块/省 → {provinces.Count}省");
        }


        /// <summary>生成子地块/Burg（省份内定居点：城市/港口/要塞/村庄）</summary>
        private void GenerateBurgs(int seed)
        {
            var generator = new BurgGenerator(tiles, mapWidth, mapHeight, provinces, seed);
            burgs = generator.Generate();
            int cityCount=0, portCount=0, fortCount=0, villageCount=0;
            foreach (var b in burgs.Values) {
                switch (b.type) { case BurgType.City: cityCount++; break; case BurgType.Port: portCount++; break; case BurgType.Fortress: fortCount++; break; default: villageCount++; break; }
            }
            Debug.Log($"[GameWorld] 子地块生成：{burgs.Count}个（城{cityCount}/港{portCount}/寨{fortCount}/村{villageCount}）");
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

            // 8. 战争（战争闭环：同地块交战→分数→胜负判定→停战）
            _combatManager.DailyTick(armies, _wars, _diplomacyManager.WarRules, currentDay);
            UpdateFaithFervor(currentDay);
            var endedWars = CombatManager.UpdateWarOutcomes(_wars, _diplomacyManager.WarRules, currentDay);
            foreach (var war in endedWars)
            {
                string outcomeText = war.outcome == "victory"
                    ? $"{realms[war.winnerId].realmName} 赢得战争胜利"
                    : "双方白和";
                _chronicle?.Add("war_end", outcomeText, major: true, war.attackerId, war.defenderId);
                _diplomacyManager.ForcePeace(war.attackerId, war.defenderId, currentDay,
                    _diplomacyManager.WarRules.truceYears, outcomeText);

                // 政体变迁接线：战败暴露国家无能，为战败方打开关键节点窗口（战胜/白和不触发）
                if (war.outcome == "victory" && war.winnerId >= 0)
                    NotifyWarDefeat(war, currentDay);
            }

            // 9. 角色与家族
            _characterManager.DailyTick(currentDay, currentYear);

            // 9.5 继位扶正（统治者死亡→继承人扶正→政体变迁注入）
            CheckRulerSuccessions();

            // 10. 思想与规范
            _thoughtManager.DailyTick(currentYear);

            // 11. AI决策（先同步统治者人格到 AI 偏置——人格漂移实时反映）
            _aiManager.SyncRulers(_characterManager);
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

                        // 人口承载约束（超载抑制增长——地理/仓储/贸易决定上限）
                        float capacity = CarryingCapacitySystem.CalculateCarryingCapacity(tiles[i], tradeCenters);
                        float overload = CarryingCapacitySystem.GetOverloadRatio(tiles[i], capacity);
                        if (overload > 1f)
                        {
                            // 超载 10% 起线性抑制，超载 200% 完全停止
                            float suppression = Mathf.Clamp01((overload - 1.1f) / 0.9f);
                            growthRate *= 1f - suppression;
                        }

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
            // 社会分化按固定间隔推进（对所有政权同步），避免每天搬运人口
            _differentiationTimer += daysPerTick;
            bool doDifferentiation = _differentiationTimer >= DifferentiationIntervalDays;
            if (doDifferentiation) _differentiationTimer = 0f;

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

                // 社会-派系-政体变迁脉冲（税收/稳定之后，外交/战争之前）
                SocietyPulse(realm, doDifferentiation);
            }
        }

        /// <summary>
        /// 社会-派系-政体变迁脉冲：情境采集 → 阶层需求满足度 → 政治能量 → 派系组织化 → 关键节点博弈。
        /// 阶层好感在此由真实需求满足度驱动（替代旧的机械回归中性值）。
        /// </summary>
        private void SocietyPulse(RealmData realm, bool doDifferentiation)
        {
            var sit = RealmSituationBuilder.Build(realm, tiles, _economyManager, _wars, armies,
                _disasterSystem, _innovationTree);

            // 先推进社会分工（人口在阶层间缓慢、守恒转移），再统计社会画像，保证派系看到的是最新阶层结构
            if (doDifferentiation)
                SocialDifferentiation.DifferentiateRealm(realm, tiles, sit);

            var society = _societyManager.EvaluateRealm(realm, tiles, sit);
            _societyManager.ApplyClassRelations(realm, society, daysPerTick);
            var characters = _characterManager.GetCharactersByRealm(realm.realmId);
            _factionManager.UpdateRealmFactions(society, realm, characters);
            _regimeDynamics.Tick(currentDay, realm, society, sit, _factionManager);
            _societyCache[realm.realmId] = society;
        }

        /// <summary>查询政权社会画像（UI/AI 用）</summary>
        public RealmSociety GetRealmSociety(int realmId) => _societyCache.GetValueOrDefault(realmId);
        public SocietyManager Society => _societyManager;
        public FactionManager Factions => _factionManager;
        public RegimeChangeDynamics RegimeDynamics => _regimeDynamics;

        /// <summary>
        /// 战争结束 → 政体变迁关键节点注入：按战败方领土被占比例区分普通战败与外部征服。
        /// 事件烈度仍需盖过"基础阈值+制度黏性"才真正开窗——低张力战败会被现制度吸收。
        /// </summary>
        private void NotifyWarDefeat(WarState war, int day)
        {
            if (_regimeDynamics == null) return;
            int winner = war.winnerId;
            int loser = winner == war.attackerId ? war.defenderId : war.attackerId;
            if (!realms.ContainsKey(loser)) return;

            int ownTiles = 0, occupiedByWinner = 0;
            foreach (var t in tiles)
            {
                if (!t.exists || t.ownerRealmId != loser) continue;
                ownTiles++;
                if (t.occupyingRealmId == winner) occupiedByWinner++;
            }
            float occupiedRatio = ownTiles > 0 ? (float)occupiedByWinner / ownTiles : 0f;
            var type = occupiedRatio >= 0.30f
                ? CriticalJunctureType.ForeignConquest
                : CriticalJunctureType.WarDefeat;
            float loserStability = realms[loser].stability;
            float severity = Mathf.Clamp(55f + occupiedRatio * 70f + (100f - loserStability) * 0.15f, 0f, 100f);
            _regimeDynamics.NotifyEvent(day, loser, type, severity);
        }

        /// <summary>
        /// 统治者更替 → 政体变迁关键节点注入（供未来继位系统 / 宫廷事件调用）。
        /// disputed=继位争议（绝嗣/幼主/僭夺）→继承危机；平稳继位但新君能力卓绝且大胆→强势改革者窗口；
        /// 平庸且无争议的平稳继位不打开窗口（制度平稳延续）。
        /// </summary>
        public void NotifyRulerTransition(int realmId, int newRulerCharId, bool disputed, int day = -1)
        {
            if (_regimeDynamics == null || !realms.ContainsKey(realmId)) return;
            int d = day >= 0 ? day : currentDay;

            if (disputed)
            {
                float severity = Mathf.Clamp(50f + (100f - realms[realmId].stability) * 0.25f, 0f, 100f);
                _regimeDynamics.NotifyEvent(d, realmId, CriticalJunctureType.SuccessionCrisis, severity);
                return;
            }

            var ruler = _characterManager?.GetCharacter(newRulerCharId);
            if (ruler == null) return;
            float competence = (ruler.diplomacy + ruler.stewardship + ruler.intrigue
                              + ruler.martial + ruler.warfare + ruler.learning) / 6f;
            if (competence >= 70f && ruler.boldness >= 20f)
                _regimeDynamics.NotifyEvent(d, realmId, CriticalJunctureType.StrongReformer, competence * 0.8f);
        }

        /// <summary>每日继位检查：统治者死亡 → 扶正/争议 → 编年史 + 政体变迁注入</summary>
        private void CheckRulerSuccessions()
        {
            foreach (var realm in realms.Values)
            {
                var result = SuccessionSystem.ExecuteSuccession(realm, _characterManager, currentDay);
                if (!result.triggered) continue;

                if (result.disputed)
                {
                    _chronicle?.Add("succession_crisis",
                        $"{realm.realmName} 继承危机：{result.reason}", major: true, realm.realmId);
                    NotifyRulerTransition(realm.realmId, -1, true, currentDay);
                }
                else if (result.succeeded)
                {
                    var newRuler = _characterManager?.GetCharacter(result.newRulerId);
                    string rulerName = newRuler != null ? $"{newRuler.firstName} {newRuler.lastName}" : "?";
                    _chronicle?.Add("succession",
                        $"{realm.realmName} 新君即位：{rulerName}", major: true, realm.realmId);
                    NotifyRulerTransition(realm.realmId, result.newRulerId, false, currentDay);
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
            // 步兵（兵种×革新：重装需铁制武器、精锐需炼钢、弩手需弩；物资：武器/盔甲）
            AddUnitDef(100, "轻装步兵", GameEnums.UnitCategory.Infantry, 1, 10f, 0f, 8f, 60f, 2f, 1f, 50f, 1f);
            AddUnitDef(101, "重装步兵", GameEnums.UnitCategory.Infantry, 2, 15f, 0f, 15f, 70f, 1.5f, 1.5f, 80f, 1.5f, 301);
            AddUnitDef(102, "精锐步兵", GameEnums.UnitCategory.Infantry, 3, 20f, 0f, 22f, 80f, 1.2f, 2f, 120f, 2f, 203);
            AddUnitDef(110, "弓箭手", GameEnums.UnitCategory.Infantry, 1, 5f, 12f, 6f, 50f, 2f, 0.8f, 40f, 1f, 907);
            AddUnitDef(111, "弩手", GameEnums.UnitCategory.Infantry, 2, 6f, 18f, 8f, 55f, 1.5f, 1f, 60f, 1.2f, 304);
            // 骑兵（轻骑需骑乘术、重骑需马镫、精锐需重装骑兵；物资：武器+盔甲+马）
            AddUnitDef(200, "轻骑兵", GameEnums.UnitCategory.Cavalry, 1, 12f, 5f, 8f, 65f, 4f, 1.5f, 60f, 1f, 923);
            AddUnitDef(201, "重骑兵", GameEnums.UnitCategory.Cavalry, 2, 20f, 8f, 18f, 75f, 3f, 2.5f, 100f, 1.5f, 924);
            AddUnitDef(202, "精锐骑兵", GameEnums.UnitCategory.Cavalry, 3, 28f, 10f, 25f, 85f, 2.5f, 3f, 150f, 2f, 303);
            AddUnitDef(203, "超重装骑兵", GameEnums.UnitCategory.Cavalry, 4, 36f, 12f, 32f, 95f, 2f, 4f, 200f, 3f, 1006); // 具装甲骑（人马俱甲）
            AddUnitDef(210, "战车兵", GameEnums.UnitCategory.Cavalry, 2, 20f, 6f, 14f, 70f, 3.5f, 2.5f, 100f, 2f, 1013); // 双马战车（青铜时代战场主宰）
            // 水军（桨帆需桨帆船、战舰需克拉克、撞角需撞角战术、远洋贸易船需远洋贸易）
            AddUnitDef(300, "桨帆船", GameEnums.UnitCategory.Navy, 1, 15f, 10f, 10f, 60f, 3f, 2f, 80f, 1f, 402);
            AddUnitDef(301, "帆船战舰", GameEnums.UnitCategory.Navy, 2, 20f, 15f, 15f, 70f, 4f, 2.5f, 120f, 1.5f, 405);
            AddUnitDef(302, "撞角战船", GameEnums.UnitCategory.Navy, 2, 28f, 4f, 14f, 65f, 3.5f, 2.5f, 100f, 2f, 1007); // 舰首包铜铁撞角
            AddUnitDef(303, "远洋贸易船", GameEnums.UnitCategory.Navy, 3, 8f, 2f, 18f, 70f, 4.5f, 1.5f, 80f, 2.5f, 1008); // 商船/贸易护卫

            // 步兵：武器/盔甲
            SetUnitRecruitCosts(101, (70, 1.2f), (71, 1f));
            SetUnitRecruitCosts(102, (70, 1.5f), (71, 1.5f));
            // 骑兵：武器+盔甲+马
            SetUnitRecruitCosts(200, (70, 1f), (20, 1f));
            SetUnitRecruitCosts(201, (70, 1.2f), (71, 1.2f), (20, 1.5f));
            SetUnitRecruitCosts(202, (70, 1.5f), (71, 1.5f), (20, 2f));
            SetUnitRecruitCosts(203, (70, 2f), (71, 2f), (20, 2.5f)); // 超重装：人马双甲
            SetUnitRecruitCosts(210, (70, 1.2f), (20, 2f)); // 战车：武器+双马
            // 船只：原木/加工木材 + 铁矿/棉花
            SetUnitRecruitCosts(300, (30, 2f), (31, 1f));
            SetUnitRecruitCosts(301, (31, 2f), (50, 1f));
            SetUnitRecruitCosts(302, (30, 2f), (50, 1.5f)); // 撞角需铁
            SetUnitRecruitCosts(303, (31, 2f), (11, 1f));   // 远洋船需加工木+帆布（棉花）
        }

        /// <summary>设置兵种招募物资（经济系统 goods 对接——goodsId → 数量）</summary>
        private void SetUnitRecruitCosts(int unitId, params (int goodsId, float amount)[] costs)
        {
            if (!unitDefs.TryGetValue(unitId, out var def)) return;
            def.recruitCost = new Dictionary<int, float>();
            foreach (var (goodsId, amount) in costs)
                def.recruitCost[goodsId] = amount;
            unitDefs[unitId] = def; // struct 需回写
        }

        private void AddUnitDef(int id, string name, GameEnums.UnitCategory category, int tier,
            float melee, float ranged, float def, float morale, float speed, float supply,
            float manpower, float recruitCost, params int[] requiredInnovations)
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
                requiredInnovations = new List<int>(requiredInnovations),
                recruitCost = new Dictionary<int, float> { { 70, recruitCost } },
                terrainModifiers = new Dictionary<GameEnums.TerrainTacticType, float>()
            };
        }

        private void InitializeTradeCenters()
        {
            int regionCount = (tiles.Length + 15) / 16;
            for (int r = 0; r < regionCount; r++)
            {
                var center = new TradeCenter
                {
                    regionId = r,
                    centerName = $"地区{r}贸易中心",
                    centerTileIndex = r * 16
                };
                // 开局启动粮储（物资 id 0 = 粮食）：经济产出尚未运转前，避免有人口的地块瞬间全判饥荒
                center.inventory[0] = 1000f;
                tradeCenters[r] = center;
            }
        }

        private void InitializeDefaultRaces()
        {
            // 预种族已删除（2026-08-29 定稿）：当前仅人类
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

                    // 给每块地分配初始人口（预种族已删除，全部人族）
                    if (tiles[tileIdx].populationBlocks.Count == 0)
                    {
                        tiles[tileIdx].populationBlocks.Add(new PopulationBlock
                        {
                            raceId = 0,
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

            // 为每个政权创建统治者与配偶（角色系统与生育机制的源头）
            _characterManager.CreateInitialRulers(realms, currentYear);

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

            // 文化地图颜色自动分配（未配置的文化按 id 取色）
            foreach (var kv in cultures)
                if (kv.Value.color == Color.white)
                    kv.Value.color = Color.HSVToRGB((kv.Key * 0.618f) % 1f, 0.55f, 0.85f);

            if (raceOverrides > 0 || cultureOverrides > 0)
                Debug.Log($"[GameWorld] 内容注册表覆盖：种族 +{raceOverrides}，文化 +{cultureOverrides}（数据驱动优先）");

            InitializeFaithSystems();
        }

        /// <summary>初始化宗教运行时（每教统一个 FaithSystem——热忱/美德罪行/领袖）</summary>
        private void InitializeFaithSystems()
        {
            _faithSystems.Clear();
            foreach (var kv in Culture.ReligionCatalog.All)
            {
                var def = kv.Value;
                if (def.nodeType != Culture.ReligionNodeType.Succession) continue; // 只教统有运行时
                var faith = new FaithSystem
                {
                    faithId = def.religionId,
                    faithName = def.religionName,
                    fervor = 50f,
                    highPriestCharacterId = -1
                };
                faith.virtues.AddRange(def.virtues);
                faith.sins.AddRange(def.sins);
                _faithSystems.Add(faith);
            }
            Debug.Log($"[GameWorld] 宗教运行时初始化：{_faithSystems.Count} 个教统");
        }

        /// <summary>获取教统运行时（无则 null）</summary>
        public FaithSystem GetFaithSystem(int faithId)
            => _faithSystems.Find(f => f.faithId == faithId);

        private int _faithFervorDay = -999;
        private const int FaithFervorInterval = 30;

        /// <summary>信仰热忱每日更新（30 天限频——长期和平冷却 -10/年——圣地丢失由
        /// 圣地系统检测；异教冲突由宣战处 AddFervor）</summary>
        private void UpdateFaithFervor(int currentDay)
        {
            if (currentDay - _faithFervorDay < FaithFervorInterval) return;
            _faithFervorDay = currentDay;
            foreach (var faith in _faithSystems)
            {
                // 长期和平冷却（约每年一次——每 360 天 -10）
                if (faith.fervor > 10f && currentDay % 360 == 0)
                    faith.AddFervor(-10f);

                // 圣地丢失检测（己方圣地被异教政权控制 → +50——"收复失地"狂热——
                // 十字军启动器；一次性标记防重复刷）
                CheckHolySiteLoss(faith);
            }
        }

        /// <summary>圣地丢失检测（圣地地块被非本教统国教政权控制 → 热忱+50）</summary>
        private void CheckHolySiteLoss(FaithSystem faith)
        {
            foreach (var tileIndex in faith.holySiteTileIndices)
            {
                int key = faith.faithId * 100000 + tileIndex;
                if (_holySiteLostProcessed.Contains(key)) continue;
                if (tileIndex < 0 || tileIndex >= tiles.Length) continue;
                int owner = tiles[tileIndex].ownerRealmId;
                if (owner < 0 || owner >= realms.Count) continue;
                int ownerFaith = realms[owner].stateReligionId;
                // 被异教控制（有国教且不同信仰）→ 丢失
                if (ownerFaith >= 0 && ownerFaith != faith.faithId)
                {
                    faith.AddFervor(50f);
                    _holySiteLostProcessed.Add(key);
                    _chronicle?.Add("religion", $"圣地失陷：{faith.faithName} 的圣地被异教控制——信仰热忱高涨",
                        major: true, owner);
                }
            }
        }

        /// <summary>
        /// 创建圣地（动态——封圣成功[圣髑移入]/圣迹事件/朝圣传统形成
        /// → 地块获 holy_site 标记——地图高亮——朝圣目标——被异教占领=热忱+50）
        /// </summary>
        public bool CreateHolySite(int faithId, int tileIndex)
        {
            if (tileIndex < 0 || tileIndex >= tiles.Length) return false;
            var faith = GetFaithSystem(faithId);
            if (faith == null) return false;
            if (faith.holySiteTileIndices.Contains(tileIndex)) return false;
            faith.holySiteTileIndices.Add(tileIndex);
            _chronicle?.Add("religion", $"{faith.faithName} 确立新的圣地（地块 {tileIndex}）",
                major: true);
            return true;
        }

        /// <summary>异教冲突热忱（宣战时调用——双方信仰不同 → +25）</summary>
        public void OnWarBetweenFaiths(int faithA, int faithB)
        {
            if (faithA == faithB) return;
            var fa = GetFaithSystem(faithA);
            var fb = GetFaithSystem(faithB);
            fa?.AddFervor(25f);
            fb?.AddFervor(25f);
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
        public List<WarState> GetWars() => _wars;

        /// <summary>宣战（外交宣战 + 创建战争状态——战争闭环入口）</summary>
        public bool DeclareWar(int attackerId, int defenderId, string reason)
        {
            if (!_diplomacyManager.DeclareWar(attackerId, defenderId, reason)) return false;
            _wars.Add(new WarState(_nextWarId++, attackerId, defenderId, currentDay));
            _chronicle?.Add("war", $"{realms[attackerId].realmName} 对 {realms[defenderId].realmName} 宣战：{reason}",
                major: true, attackerId, defenderId);

            // 异教冲突 → 双方信仰热忱 +25（宗教战争狂热——十字军/圣战的心理基础）
            if (attackerId >= 0 && defenderId >= 0 && attackerId < realms.Count && defenderId < realms.Count)
            {
                var faithA = realms[attackerId].stateReligionId;
                var faithB = realms[defenderId].stateReligionId;
                if (faithA >= 0 && faithB >= 0 && faithA != faithB)
                    OnWarBetweenFaiths(faithA, faithB);
            }
            return true;
        }

        /// <summary>创建军队（战争闭环——基础编成；招募物资/革新检查由调用方执行）</summary>
        public Army CreateArmy(int ownerRealmId, int commanderId, int tileIndex)
        {
            var army = new Army
            {
                armyId = _nextArmyId++,
                armyName = $"{realms[ownerRealmId].realmName}军",
                ownerRealmId = ownerRealmId,
                commanderId = commanderId,
                currentTileIndex = tileIndex
            };
            armies[army.armyId] = army;
            return army;
        }
        public DiplomacyManager GetDiplomacyManager() => _diplomacyManager;
        public CharacterManager GetCharacterManager() => _characterManager;
        public ThoughtManager GetThoughtManager() => _thoughtManager;
        public DisasterSystem GetDisasterSystem() => _disasterSystem;
        public DiseaseSystem GetDiseaseSystem() => _diseaseSystem;
        public BuildingSystem GetBuildingSystem() => _buildingSystem;
        public InnovationTree GetInnovationTree() => _innovationTree;
        public Chronicle GetChronicle() => _chronicle;
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
