using System.Collections.Generic;
using UnityEngine;
using CivilizationEvolution.Core;
using CivilizationEvolution.Map;
using CivilizationEvolution.World;
using CivilizationEvolution.Climate;
using CivilizationEvolution.Race;
using CivilizationEvolution.Culture;
using CivilizationEvolution.Economy;
using CivilizationEvolution.Politics;
using CivilizationEvolution.War;
using CivilizationEvolution.Diplomacy;
using CivilizationEvolution.Character;
using CivilizationEvolution.Thought;
using CivilizationEvolution.Disaster;
using CivilizationEvolution.Building;
using CivilizationEvolution.Tech;
using CivilizationEvolution.AI;

namespace CivilizationEvolution.Core
{
 /// 游戏世界主类 /// 管理所有地块数据、子系统、脏标记重算、主循环    public partial class GameWorld : MonoBehaviour
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

 // ===== 核心数据 =====        public TileData[] tiles;
        [System.NonSerialized]
        public Dictionary<int, RaceData> races = new Dictionary<int, RaceData>();
        [System.NonSerialized]
        public Dictionary<int, CultureData> cultures = new Dictionary<int, CultureData>();
        [System.NonSerialized]
        public Dictionary<int, TradeCenter> tradeCenters = new Dictionary<int, TradeCenter>();
        [System.NonSerialized]
        public Dictionary<int, GoodsDef> goodsDefs = new Dictionary<int, GoodsDef>();
        [System.NonSerialized]
        public Dictionary<int, RealmData> realms = new Dictionary<int, RealmData>();
 /// <summary>玩家政权ID（-1表示无玩家/观察者模式）</summary>        public int PlayerRealmId = -1;
        [System.NonSerialized]
        public Dictionary<int, UnitDef> unitDefs = new Dictionary<int, UnitDef>();
 /// <summary>省份（provinceId → Province——地图结构层）</summary>        [System.NonSerialized]
        public Dictionary<int, Province> provinces = new Dictionary<int, Province>();
 /// <summary>子地块/Burg（burgId → BurgData——省份内定居点，对齐 CK3 男爵领）</summary>        [System.NonSerialized]
        public Dictionary<int, BurgData> burgs = new Dictionary<int, BurgData>();
 /// <summary>军队（armyId → Army——战争闭环）</summary>        [System.NonSerialized]
        public Dictionary<int, Army> armies = new Dictionary<int, Army>();
 /// <summary>战争状态列表（战争闭环——分数/胜负判定）</summary>        private readonly List<WarState> _wars = new List<WarState>();
 /// <summary>宗教运行时状态（每教统一个 FaithSystem——热忱/信徒/圣地—— /// 由 ReligionCatalog 初始化）</summary>        private readonly List<FaithSystem> _faithSystems = new List<FaithSystem>();
 /// <summary>开局年月（时间显示=已历时长——开局时记录）</summary>        public int startYear = -1;
 /// <summary>并行段用随机（ThreadLocal——每线程独立——Parallel.For 内 /// UnityEngine.Random 全局状态竞争会导致值错乱）</summary>        private static readonly System.Threading.ThreadLocal<System.Random> ThreadRng
            = new System.Threading.ThreadLocal<System.Random>(
                () => new System.Random(System.Environment.TickCount
                    ^ System.Threading.Thread.CurrentThread.ManagedThreadId));
        public int startDay = -1;
 /// <summary>圣地丢失已处理标记（faithId→已计热忱的圣地 tile——防重复刷）</summary>        private readonly HashSet<int> _holySiteLostProcessed = new HashSet<int>();
        private int _nextWarId = 1;
        private int _nextArmyId = 1;

 // ===== 子系统 =====        private SeaLandGenerator _seaLandGenerator;
        private FeatureManager _featureManager;
        private MapActorManager _mapActorManager;
        private CampManager _campManager;
        public FeatureManager Features => _featureManager;
        public MapActorManager MapActors => _mapActorManager;
        public CampManager Camps => _campManager;
        private PlanetClimateSimulator _climateSimulator;
        private PlanetTerrainGenerator _planetTerrainGenerator;
        private AtmosphericCirculation _atmosphericCirculation;
        private HydraulicErosion _hydraulicErosion;
        public MapGenerationConfig GenConfig = new MapGenerationConfig();

 /// <summary>生成管线管理器（阶段顺序/依赖/进度回调/下游重算）</summary>        public GenerationPipeline Pipeline { get; private set; }
        private EconomyManager _economyManager;
        private CurrencySystem _currencySystem;
        private TaxSystem _taxSystem;
        private PoliticalManager _politicalManager;

 // 社会-派系-政体变迁链路（阶层需求→政治能量→派系组织化→关键节点博弈）        private SocietyManager _societyManager = new SocietyManager();
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

 // ===== 脏标记 =====        private HashSet<int> _terrainDirtyTiles = new HashSet<int>();
        private HashSet<int> _climateDirtyTiles = new HashSet<int>();
        private bool _configDirty = false;

 // ===== 时间 =====        public int currentYear = 1;
        public int currentDay = 1;
        public int currentSeason = 0;
        private float _tickTimer = 0f;

 // ===== 事件系统 =====        private Queue<GameEvent> _eventQueue = new Queue<GameEvent>();
        private List<IGameEventListener> _eventListeners = new List<IGameEventListener>();

        void Awake()
        {
 // 惰性初始化（2026-09 根因修复：主菜单模式——场景加载即自动 // InitializeWorld 会用默认 128×64 建空 tile 结构——玩家点"进入 // 世界"时 tiles 已存在[未生成地形]→条件跳过→黑屏空世界！ // 全部初始化延迟到 GameManager.StartNewGame（设尺寸后）显式 // Initialize+Generate——Awake 零初始化[注册表由 Bootstrap 管]）        }

        void Update()
        {
            _tickTimer += Time.deltaTime;
            if (_tickTimer >= tickInterval)
            {
                _tickTimer = 0f;
                GameTick();
            }
        }

 /// <summary>游戏主循环Tick</summary>        private void GameTick()
        {
 // 1. 脏区重算            RecalculateDirty();

 // 2. 灾害与疾病            _disasterSystem.DailyTick(currentDay, currentYear);
            _diseaseSystem.DailyTick(currentDay, currentYear);

 // 3. 经济            _economyManager.DailyTick();

 // 4. 建筑建造进度            _buildingSystem.DailyTick();

 // 5. 人口            PopulationTick();

 // 6. 政治            _politicalManager.DailyTick();
            PoliticsTick();

 // 6.5 聚落控制/影响力范围（高等级聚落控制低等级，驻扎部队影响控制速度，虹吸效应通过税收贸易自然表现）            CivilizationEvolution.Map.SettlementControlSystem.DailyTick(
                burgs, tiles, mapWidth, mapHeight, armies);

 // 6.6 无主地图单位（流民/游牧民/商队/雇佣兵/野怪/动物灾害）            _mapActorManager?.Tick(1f);

 // 6.7 营寨（军营/土匪寨/游牧营地/难民营）            _campManager?.Tick(1f);

 // 6.8 据点演化（营寨→坞堡→聚落）            CivilizationEvolution.Map.SettlementEvolutionSystem.DailyTick(this);

 // 6.9 废墟恢复（被摧毁聚落的重建）            CivilizationEvolution.War.SettlementDestructionSystem.DailyTickRecovery(this);

 // 6.10 弃地巡检（无主低秩序地块滋生土匪）            CivilizationEvolution.Map.LandAbandonmentSystem.DailyCheckBanditSpawn(this);

 // 7. 外交（先同步世界时钟，供盟约/条约/事件时间戳使用）            _diplomacyManager.CurrentDay = currentDay;
            _diplomacyManager.DailyTick();

 // 8. 战争（战争闭环：同地块交战→分数→胜负判定→停战）            _combatManager.DailyTick(armies, _wars, _diplomacyManager.WarRules, currentDay);
            UpdateFaithFervor(currentDay);

 // 大圣战结算钩子（关联战争结束→圣战方胜→受益人谈判）            CheckGreatHolyWarSettlements();
            var endedWars = CombatManager.UpdateWarOutcomes(_wars, _diplomacyManager.WarRules, currentDay);
            foreach (var war in endedWars)
            {
 // 战争行为计数器（绰号/评价数据：胜仗/败仗/防御大捷）                if (war.outcome == "victory" && war.winnerId >= 0)
                {
                    AddWarAchievement(war.winnerId, won: true);
                    int loserId = war.winnerId == war.attackerId ? war.defenderId : war.attackerId;
                    AddWarAchievement(loserId, won: false);
 // 防御大捷（被打的一方赢了——卫国成功——铁锤判定）                    if (war.winnerId == war.defenderId) AddDefensiveWin(war.winnerId);
                }

                string outcomeText = war.outcome == "victory"
                    ? $"{realms[war.winnerId].realmName} 赢得战争胜利"
                    : "双方白和";
                _chronicle?.Add("war_end", outcomeText, major: true, war.attackerId, war.defenderId);
                _diplomacyManager.ForcePeace(war.attackerId, war.defenderId, currentDay,
                    _diplomacyManager.WarRules.truceYears, outcomeText);

 // 官职补缺（死亡/空缺时任命——OfficeTitle 消费方）            EnsureOfficeHolders();
 // 行政区划树（空才生成——分封 4/郡县容量 2-5——政体改革重建后续）            EnsureAdminDivisions();

 // 政体变迁接线：战败暴露国家无能，为战败方打开关键节点窗口（战胜/白和不触发）                if (war.outcome == "victory" && war.winnerId >= 0)
                    NotifyWarDefeat(war, currentDay);
            }

 // 9. 角色与家族            _characterManager.DailyTick(currentDay, currentYear);

 // 9.5 继位扶正（统治者死亡→继承人扶正→政体变迁注入）            CheckRulerSuccessions();

 // 10. 思想与规范            _thoughtManager.DailyTick(currentYear);

 // 11. AI决策（先同步统治者人格到 AI 偏置——人格漂移实时反映） // 传教定期推进（15 天一次——政权传教渠道——查漏补缺接线）            MissionaryTick();

            _aiManager.SyncRulers(_characterManager);
            _aiManager.DailyTick(realms, tiles, _diplomacyManager, _economyManager, _innovationTree,
                _characterManager); // characters 传入——AI 劫掠屠城计数器

 // 12. 事件处理            ProcessEvents();

 // 13. 时间推进            AdvanceTime();
        }

 /// <summary>查询政权社会画像（UI/AI 用）</summary>        public RealmSociety GetRealmSociety(int realmId) => _societyCache.GetValueOrDefault(realmId);
        public SocietyManager Society => _societyManager;
        public FactionManager Factions => _factionManager;
        public RegimeChangeDynamics RegimeDynamics => _regimeDynamics;
        public DiplomacyManager Diplomacy => _diplomacyManager;

 /// 通过生成管线全量生成（带进度回调）。 /// 统一走 GenerationPipeline 的阶段管理，替代直接调用各生成方法。        public void GenerateAllWithPipeline()
        {
            Pipeline ??= new GenerationPipeline(this);
            Pipeline.GenerateAll();
        }

 /// 从指定阶段开始重算下游（管线增量重算）。 /// 例如修改地形后：world.RegenerateFromPipeline(GenerationStage.Terrain)        public void RegenerateFromPipeline(GenerationStage fromStage)
        {
            Pipeline ??= new GenerationPipeline(this);
            Pipeline.RegenerateFrom(fromStage);
        }

 /// 革新完成→阶层出现事件（查漏补缺接线：ClassEmergenceEvents 原为 /// 孤儿——订阅 InnovationTree.OnInnovationCompleted——铸币→商人/ /// 庄园→农奴/文字+官僚→士人……）        private int _missionaryTimer = 0;
        private const int MissionaryIntervalDays = 15;
        private readonly System.Random _missionaryRng = new System.Random(20260904);

 /// 建城（事件接口——君主/政权在地块建城——纪念命名）： /// 命名优先=建城者名+城语义后缀[FounderCity——亚历山大城式—— /// 查建城者文化→语言→城词]——语言缺城词→回退程序化生成        public Map.BurgData CreateCity(int realmId, int tileIndex, int founderCharId)
        {
            if (tileIndex < 0 || tileIndex >= tiles.Length) return null;
            if (!tiles[tileIndex].isLand) return null;
            ref TileData tile = ref tiles[tileIndex];
            if (tile.ownerRealmId != realmId) return null; // 只能在本政权领地建城

            int nextId = 1;
            foreach (var b in burgs.Keys) if (b >= nextId) nextId = b + 1;

            string name = "";
            var founder = _characterManager?.GetCharacter(founderCharId);
            if (founder != null)
            {
 // 建城者文化→语言→城词（纪念名）                if (cultures != null && cultures.TryGetValue(founder.cultureId, out var cd))
                {
                    if (ContentRegistry.TryGetLanguage(cd.languageId, out var lang))
                        name = Culture.PlaceNameGenerator.FounderCity(
                            $"{founder.firstName}{founder.lastName}", lang);
                }
            }
            if (string.IsNullOrEmpty(name))
                name = "新市镇";

            var burg = new Map.BurgData
            {
                burgId = nextId,
                burgName = name,
                type = Map.BurgType.City,
                provinceId = tile.provinceId,
                tileIndex = tileIndex,
                x = 0.5f, y = 0.5f,
                isCoastal = tile.isCoast,
                buildLevel = 1,
            };
            Map.SettlementTypologySystem.DeriveInitialType(burg, tile, mapWidth, mapHeight);
            burg.settlementType = Map.SettlementEvolutionSystem.InferFromBurgType(burg.type);
            burgs[burg.burgId] = burg;
            string founderName = founder != null ? founder.firstName + founder.lastName : "某人";
            _chronicle?.Add("city_founded",
                $"{founderName} 建立城市 {name}",
                major: true, realmId);
            return burg;
        }

 /// <summary>获取教统运行时（无则 null）</summary>        public FaithSystem GetFaithSystem(int faithId)
            => _faithSystems.Find(f => f.faithId == faithId);

        private int _faithFervorDay = -999;
        private const int FaithFervorInterval = 30;

        private string GetCharacterName(int characterId)
        {
            var cm = GetCharacterManager();
            var c = cm?.GetCharacter(characterId);
            return c != null ? c.firstName + " " + c.lastName : characterId.ToString();
        }

 /// 创建圣地（动态——封圣成功[圣髑移入]/圣迹事件/朝圣传统形成 /// → 地块获 holy_site 标记——地图高亮——朝圣目标——被异教占领=热忱+50）        public bool CreateHolySite(int faithId, int tileIndex)
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

 /// <summary>异教冲突热忱（宣战时调用——双方信仰不同 → +25）</summary>        public void OnWarBetweenFaiths(int faithA, int faithB)
        {
            if (faithA == faithB) return;
            var fa = GetFaithSystem(faithA);
            var fb = GetFaithSystem(faithB);
            fa?.AddFervor(25f);
            fb?.AddFervor(25f);
        }

 // ===== 地块增删（自由形状地图支持） =====
 /// <summary>创建地块（设置exists=true并初始化默认值）</summary>        public bool CreateTile(int tileIndex)
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

 /// <summary>删除地块（设置exists=false，清空数据）</summary>        public bool RemoveTile(int tileIndex)
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

 /// <summary>检查地块是否存在</summary>        public bool TileExists(int tileIndex)
        {
            if (tileIndex < 0 || tileIndex >= tiles.Length) return false;
            return tiles[tileIndex].exists;
        }

 /// <summary>获取所有存在的地块索引列表</summary>        public List<int> GetValidTiles()
        {
            var result = new List<int>();
            for (int i = 0; i < tiles.Length; i++)
            {
                if (tiles[i].exists) result.Add(i);
            }
            return result;
        }

 /// <summary>获取存在的地块数量</summary>        public int GetValidTileCount()
        {
            int count = 0;
            for (int i = 0; i < tiles.Length; i++)
            {
                if (tiles[i].exists) count++;
            }
            return count;
        }

 /// <summary>获取邻接地块（支持左右连通环绕）</summary>        public List<int> GetNeighbours(int tileIndex)
        {
            return TileGrid.GetNeighbours(tileIndex, mapWidth, mapHeight, config.wrapX, config.wrapY);
        }

 // ===== 查询接口 =====        public float GetTilePopulation(int tileIndex)
        {
            if (tileIndex < 0 || tileIndex >= tiles.Length) return 0f;
            float total = 0f;
            if (tiles[tileIndex].populationBlocks != null)
                foreach (var pb in tiles[tileIndex].populationBlocks)
                    total += pb.count;
            return total;
        }

 // 防御（惰性化后：未 StartNewGame 时生成器 null——查询安全返回 0）        public int GetLandTileCount() => _seaLandGenerator != null ? _seaLandGenerator.GetTotalLandTiles() : 0;
        public int GetSeaTileCount() => _seaLandGenerator != null ? _seaLandGenerator.GetTotalSeaTiles() : 0;
        public int GetConnectedSeaCount() => _seaLandGenerator != null ? _seaLandGenerator.GetConnectedSeaCount() : 0;
        public TileData GetTile(int x, int y) => tiles[y * mapWidth + x];
        public TileData GetTile(int index) => tiles[index];
        public EconomyManager GetEconomyManager() => _economyManager;
        public SeaLandGenerator GetSeaLandGenerator() => _seaLandGenerator;
        public PlanetClimateSimulator GetClimateSimulator() => _climateSimulator;
        public CombatManager GetCombatManager() => _combatManager;
        public List<WarState> GetWars() => _wars;

 /// <summary>宣战（外交宣战 + 创建战争状态——战争闭环入口）</summary>        public bool DeclareWar(int attackerId, int defenderId, string reason)
        {
            if (!_diplomacyManager.DeclareWar(attackerId, defenderId, reason)) return false;
            _wars.Add(new WarState(_nextWarId++, attackerId, defenderId, currentDay));
            _chronicle?.Add("war", $"{realms[attackerId].realmName} 对 {realms[defenderId].realmName} 宣战：{reason}",
                major: true, attackerId, defenderId);

 // 异教冲突 → 双方信仰热忱 +25（宗教战争狂热——十字军/圣战的心理基础）            if (attackerId >= 0 && defenderId >= 0 && attackerId < realms.Count && defenderId < realms.Count)
            {
                var faithA = realms[attackerId].stateReligionId;
                var faithB = realms[defenderId].stateReligionId;
                if (faithA >= 0 && faithB >= 0 && faithA != faithB)
                    OnWarBetweenFaiths(faithA, faithB);
            }
            return true;
        }

 /// 发起大圣战（号召制——热忱≥60+有领袖——创建 WarState 正常结算—— /// 战争结束圣战方胜→受益人谈判[继承法线外者]——土地归受益人）        public bool DeclareGreatHolyWar(int faithId, int callerRealmId, int targetRealmId,
            int targetTile, string reason)
        {
            var faith = GetFaithSystem(faithId);
            if (faith == null) return false;
 // 条件：热忱≥60 + 有宗教领袖（教宗/哈里发——领袖落角色后判定）            if (!faith.CanDeclareGreatHolyWar()) return false;
            if (callerRealmId < 0 || callerRealmId >= realms.Count) return false;
            if (targetRealmId < 0 || targetRealmId >= realms.Count) return false;

            var war = War.GreatHolyWarSystem.Declare(faithId, callerRealmId, targetRealmId,
                targetTile, currentDay, hasLeader: true, fervor: faith.fervor);
            if (war == null) return false;

 // 战争结算绑定：创建 WarState（圣战方=号召者）——正常分数制            if (!_diplomacyManager.DeclareWar(callerRealmId, targetRealmId, reason)) return false;
            var warState = new WarState(_nextWarId++, callerRealmId, targetRealmId, currentDay);
            _wars.Add(warState);
            War.GreatHolyWarSystem.BindWar(war, warState.warId);

 // 异教冲突热忱（大圣战=异教战争——已由 DeclareWar 处理——不重复）            _chronicle?.Add("religion",
                $"{realms[callerRealmId].realmName} 号召大圣战讨伐 {realms[targetRealmId].realmName}",
                major: true, callerRealmId, targetRealmId);
            return true;
        }

 /// <summary>创建军队（战争闭环——基础编成；招募物资/革新检查由调用方执行）</summary>        public Army CreateArmy(int ownerRealmId, int commanderId, int tileIndex)
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

    }
