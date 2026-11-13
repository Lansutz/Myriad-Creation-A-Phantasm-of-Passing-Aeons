using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MyriadCreation.Core.Contracts;
using MyriadCreation.Core.Data;
using MyriadCreation.Core.Enums;
using MyriadCreation.Core.Events;
using MyriadCreation.Simulation.Scheduling;
using MyriadCreation.Simulation.AI;
using MyriadCreation.Simulation.Actors;
using MyriadCreation.Simulation.Diplomacy;
using MyriadCreation.Simulation.Economy;
using MyriadCreation.Simulation.Events;
using MyriadCreation.Simulation.Generation;
using MyriadCreation.Simulation.Innovation;
using MyriadCreation.Simulation.Laws;
using MyriadCreation.Content.Modding;
using MyriadCreation.Simulation.ContentHost;
using MyriadCreation.Simulation.Planning;
using MyriadCreation.Simulation.Politics;
using MyriadCreation.Simulation.Settlement;
using MyriadCreation.Simulation.State;
using MyriadCreation.Simulation.Systems;
using MyriadCreation.Simulation.Units;
using MyriadCreation.Simulation.Warfare;
using MyriadCreation.World;
using MyriadCreation.World.Anchor;
using MyriadCreation.World.Biome;
using MyriadCreation.World.Climate;
using MyriadCreation.World.Generation;
using MyriadCreation.World.Settlement;
using MyriadCreation.World.Terrain;
using UnityEngine;
using MyriadCreation.Simulation.Faith;
using MyriadCreation.Simulation.Culture;
using MyriadCreation.Content.Definitions;
using MyriadCreation.Simulation.LawHost;
using Object = UnityEngine.Object;
using Random = System.Random;
using MyriadCreation.Simulation.FaithHost;
using MyriadCreation.Simulation.CultureHost;

namespace MyriadCreation.Simulation.WorldState
{
public partial class GameWorld : MonoBehaviour
{
	private PlanSystem _planSystem;

	private ResearchPlanSystem _researchPlanSystem;

	private ConstructionPlanSystem _constructionPlanSystem;

	private readonly List<int> _practiceDirtyCharacters = new List<int>();

	[Header("世界配置（ScriptableObject资产，留空则用默认值）")]
	[Tooltip("拖入 Data 目录下的 WorldConfig 资产；留空运行时自动创建默认配置")]
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

	public TileData[] tiles;

	[NonSerialized]
	public Dictionary<int, RaceData> races = new Dictionary<int, RaceData>();

	[NonSerialized]
	public Dictionary<int, CultureData> cultures = new Dictionary<int, CultureData>();

	[NonSerialized]
	public Dictionary<int, TradeCenter> tradeCenters = new Dictionary<int, TradeCenter>();

	[NonSerialized]
	public Dictionary<int, GoodsDef> goodsDefs = new Dictionary<int, GoodsDef>();

	[NonSerialized]
	public Dictionary<int, RealmData> realms = new Dictionary<int, RealmData>();

	public int PlayerRealmId = -1;

	[NonSerialized]
	public Dictionary<int, UnitDef> unitDefs = new Dictionary<int, UnitDef>();

	[NonSerialized]
	public Dictionary<int, Province> provinces = new Dictionary<int, Province>();

	[NonSerialized]
	public Dictionary<int, AnchorData> anchors = new Dictionary<int, AnchorData>();

	[NonSerialized]
	public Dictionary<int, SettlementData> settlements = new Dictionary<int, SettlementData>();

	[NonSerialized]
	public Dictionary<int, Army> armies = new Dictionary<int, Army>();

	/// <summary>远征实体（P4.1）：地图知识的过程载体；由远征调度推进。</summary>
	public Dictionary<int, Expedition> expeditions = new Dictionary<int, Expedition>();

	private int _nextExpeditionId = 1;

	public int NextExpeditionId() => _nextExpeditionId++;

	public void SetNextExpeditionId(int next)
	{
		_nextExpeditionId = Math.Max(_nextExpeditionId, next);
	}

	private ExplorationPlanSystem _explorationPlanSystem;

	public ExplorationPlanSystem ExplorationPlans => _explorationPlanSystem ?? (_explorationPlanSystem = new ExplorationPlanSystem(this, Plans));

	public Plan CreateExplorationPlan(int realmId, int leaderCharacterId, int targetTile)
	{
		return ExplorationPlans.CreatePlan(realmId, leaderCharacterId, targetTile);
	}

	public Dictionary<int, Navy> navies = new Dictionary<int, Navy>();

	private readonly List<WarState> _wars = new List<WarState>();

	private readonly List<FaithSystem> _faithSystems = new List<FaithSystem>();

	/// <summary>宗派（G3）：`religion → denomination → faith` 的中间层，faith 必挂。</summary>
	private readonly List<Denomination> _denominations = new List<Denomination>();

	private ReligionSimulationEventHandler _religionSimulationEvents;

	public int startYear = -1;

	/// <summary>
	/// 确定性掷骰：委托给 L0 的 <see cref="MyriadCreation.Core.DeterministicRandom"/>。
	/// 原为 ThreadLocal&lt;Random&gt;(Environment.TickCount…)——使同一 seed 两次运行结果不同（M1 阻塞项）。
	/// 纯函数式：可在 Parallel.For 内安全调用，且同 seed 同输入必得同结果。
	/// </summary>
	internal bool Roll01(int tileIndex, int blockIndex, int salt, float chance)
	{
		return MyriadCreation.Core.DeterministicRandom.Roll01(randomSeed, currentDay, tileIndex, blockIndex, salt, chance);
	}

	public int startDay = -1;

	private readonly HashSet<int> _holySiteLostProcessed = new HashSet<int>();

	private int _nextWarId = 1;

	private int _nextArmyId = 1;

	private int _nextNavyId = 1;

	private MyriadCreation.World.Terrain.SeaLandGenerator _seaLandGenerator;

	/// <summary>海陆生成器当前绑定的重算 seed；与 randomSeed 不一致时在重算/重生成前重建，保证重算路径 RNG 与世界 seed 一致。</summary>
	private int _seaLandRecalcSeed = int.MinValue;

	private FeatureManager _featureManager;

	private MapActorManager _mapActorManager;

	private SandboxSpawnSystem _sandboxSpawnSystem;

	private CampManager _campManager;

	private PlanetClimateSimulator _climateSimulator;
	/// <summary>最近一次破碎化后补算派生气候的地块索引（供测试/诊断；非存档状态）。</summary>
	private readonly List<int> _lastFragmentationRecomputedTiles = new List<int>();
	public IReadOnlyList<int> LastFragmentationRecomputedTiles => _lastFragmentationRecomputedTiles;

	private PlanetTerrainGenerator _planetTerrainGenerator;

	private AtmosphericCirculation _atmosphericCirculation;

	private HydraulicErosion _hydraulicErosion;

	public MapGenerationConfig GenConfig = new MapGenerationConfig();

	public WorldSetupOptions SetupOptions = WorldSetupOptions.CreateDefault();

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

	private Chronicle _chronicle;

	private AIManager _aiManager;

	private AIIntentExecutor _aiIntentExecutor;

	private readonly SimulationEventBus _simulationEvents = new SimulationEventBus();

	private readonly SimulationCommandBus _simulationCommands = new SimulationCommandBus();

	private readonly SimulationScheduler _simulationScheduler = new SimulationScheduler();

	private PoliticsSimulationSystem _politicsSimulationSystem;

	private SettlementSimulationSystem _settlementSimulationSystem;

	private RegimeChangeDynamics _regimeDynamics;

	private readonly Dictionary<int, JudicialSystem> _judicialSystems = new Dictionary<int, JudicialSystem>();

	private WorldMapSimulationSystem _worldMapSimulation;

	public int currentYear = 1;

	public int currentDay = 1;

	public int currentSeason;

	private float _tickTimer;

	private Queue<GameEvent> _eventQueue = new Queue<GameEvent>();

	private List<IGameEventListener> _eventListeners = new List<IGameEventListener>();

	private readonly SimulationBudgetGovernor _budgetGovernor = new SimulationBudgetGovernor();

	private int _lastDeferred;

	private int _missionaryTimer;

	private const int MissionaryIntervalDays = 15;

	// 传教掷骰：每次 MissionaryTick 由 (世界 seed, 当日) 重新派生，见 GameWorld.Simulation.cs。原为 readonly 固定种子，
	// 会让结果依赖"共享状态被调用过几次"，与 seed 无关。
	private Random _missionaryRng = new Random(0);

	private int _faithFervorDay = -999;

	private const int FaithFervorInterval = 30;

	public PlanSystem Plans => _planSystem ?? (_planSystem = CreatePlanSystem());

	public ResearchPlanSystem ResearchPlans => _researchPlanSystem ?? (_researchPlanSystem = CreateResearchPlanSystem());

	public ConstructionPlanSystem ConstructionPlans => _constructionPlanSystem ?? (_constructionPlanSystem = CreateConstructionPlanSystem());

	/// <summary>建筑只读入口（AI/UI 查定义与已建等级，不直接写状态）。</summary>
	public BuildingSystem Buildings => _buildingSystem;

	/// <summary>地图知识层（S19）：每政权已知地块位图；写入口只有 SeedOwnership/RevealRadius/Restore。</summary>
	public MapKnowledge Knowledge { get; } = new MapKnowledge();

	public CharacterManager Characters => _characterManager;

	public InnovationTree Innovations => _innovationTree;

	public IEnumerable<HabitationSite> HabitationSites
	{
		get
		{
			foreach (SettlementData value in settlements.Values)
			{
				yield return value;
			}
			if (_campManager == null)
			{
				yield break;
			}
			foreach (HabitationSite activeHabitationSite in _campManager.ActiveHabitationSites)
			{
				yield return activeHabitationSite;
			}
		}
	}

	public FeatureManager Features => _featureManager;

	public MapActorManager MapActors => _mapActorManager;

	public CampManager Camps => _campManager;

	public GenerationPipeline Pipeline { get; private set; }

	public SimulationCommandBus SimulationCommands => _simulationCommands;

	public SimulationScheduler SimulationScheduler => _simulationScheduler;

	public ISimulationScheduler Scheduler => _simulationScheduler;

	public SimulationEventBus SimulationEvents => _simulationEvents;

	public FactionManager Factions => _politicsSimulationSystem?.Factions;

	public RegimeChangeDynamics RegimeDynamics => _regimeDynamics;

	public void InitializePlanSystem()
	{
		_researchPlanSystem?.Dispose();
		_planSystem = new PlanSystem();
		_researchPlanSystem = CreateResearchPlanSystem();
		_constructionPlanSystem = null;
	}

	public float RecordInnovationPractice(int characterId, int innovationId, float amount)
	{
		if (amount <= 0f)
		{
			return 0f;
		}
		if (_researchPlanSystem == null && _innovationTree != null && _characterManager != null)
		{
			_researchPlanSystem = CreateResearchPlanSystem();
		}
		SimulationEvents.Publish(new PracticeRecordedEvent(characterId, innovationId, amount));
		return amount;
	}

	public void TickPlans(float deltaDays = 1f)
	{
		Plans.DailyTick(currentDay, deltaDays);
	}

	private ResearchPlanSystem CreateResearchPlanSystem()
	{
		return new ResearchPlanSystem(this, Plans);
	}

	private IntriguePlanSystem _intriguePlanSystem;

	public IntriguePlanSystem IntriguePlans => _intriguePlanSystem ?? (_intriguePlanSystem = new IntriguePlanSystem(this, Plans));

	/// <summary>旧档/读档后补一次官职（不改变已有 holder）。</summary>
	public void EnsureInitialOffices()
	{
		EnsureOfficeHolders();
	}

	public Plan CreateIntriguePlan(int realmId, int initiatorCharacterId, int targetCharacterId)
	{
		return IntriguePlans.CreatePlan(realmId, initiatorCharacterId, targetCharacterId);
	}

	private ConstructionPlanSystem CreateConstructionPlanSystem()
	{
		return new ConstructionPlanSystem(this, Plans, _buildingSystem);
	}

	private PlanSystem CreatePlanSystem()
	{
		return new PlanSystem();
	}

	public Plan CreateConstructionPlan(int realmId, int initiatorId, int tileIndex, int buildingId, int builderCharacterId = -1, int innovationId = -1)
	{
		if (_buildingSystem == null || !realms.TryGetValue(realmId, out var value))
		{
			return null;
		}
		return ConstructionPlans.CreateConstructionPlan(realmId, initiatorId, tileIndex, buildingId, value, builderCharacterId, innovationId);
	}

	private void RegisterDiplomacyCommandHandlers()
	{
		_simulationCommands.Register(new DeclareWarCommandHandler((int attacker, int defender, string reason, int actor) => DeclareWar(attacker, defender, reason, actor)));
		_simulationCommands.Register(new RaidSettlementCommandHandler(_diplomacyManager, tiles, _characterManager));
		_simulationCommands.Register(new ProposeAllianceCommandHandler(_diplomacyManager));
		_simulationCommands.Register(new SendGiftCommandHandler(_diplomacyManager));
		_simulationCommands.Register(new StartInnovationResearchCommandHandler(_innovationTree, this));
		_simulationCommands.Register(new ImproveRealmEconomyCommandHandler(realms, tiles));
		_simulationCommands.Register(new ConsolidateRealmCommandHandler(realms, tiles));
		_simulationCommands.Register(new MilitaryBuildUpCommandHandler(realms));
	}

	private void RegisterSettlementCommandHandlers()
	{
		SettlementDestructionRuntime runtime = new SettlementDestructionRuntime(this);
		LandAbandonmentRuntime runtime2 = new LandAbandonmentRuntime(this);
		_simulationCommands.Register(new DestroySettlementCommandHandler(runtime, _simulationEvents));
		_simulationCommands.Register(new AbandonSettlementTileCommandHandler(runtime2, _simulationEvents));
		_simulationCommands.Register(new ResettleTileCommandHandler(runtime2, _simulationEvents));
	}

	/// <summary>待处理事件数（诊断用）；长跑中应保持有界，不应逐日累积。</summary>
	public int PendingEventCount => _eventQueue.Count;

	public void EnqueueEvent(GameEvent evt)
	{
		_eventQueue.Enqueue(evt);
	}

	/// <summary>最近一次事件处理的条数（诊断用）。</summary>
	public int LastProcessedEventCount { get; private set; }

	/// <summary>累计饥荒事件数（人口后果的实际触发次数），供长跑诊断与调参使用；不参与模拟。</summary>
	public int FamineEventCount { get; private set; }

	/// <summary>最近一次饥荒判定的地块与库存/需求，供诊断定位“为什么又饥荒”；不参与模拟。</summary>
	public int LastFamineTile { get; internal set; } = -1;
	public float LastFamineFood { get; internal set; }
	public float LastFamineNeed { get; internal set; }

	/// <summary>每地块累计饥荒事件数（诊断用）。</summary>
	public int[] FamineByTile = System.Array.Empty<int>();

	private void ProcessEvents()
	{
		int processed = 0;
		while (_eventQueue.Count > 0)
		{
			processed++;
			GameEvent evt = _eventQueue.Dequeue();
			foreach (IGameEventListener eventListener in _eventListeners)
			{
				eventListener.OnGameEvent(evt);
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
		LastProcessedEventCount = processed;
	}

	private void HandleFamine(GameEvent evt)
	{
		FamineEventCount++;
		if (evt.tileIndex >= 0 && evt.tileIndex < tiles.Length)
		{
			// 死亡比例跟着缺口走：缺口 50% 死 1%，粮仓彻底见底才死 2%。
			float mortality = Mathf.Clamp01(evt.severity / 100f) * 0.02f;
			float survivorScale = 1f - mortality;
			for (int i = 0; i < tiles[evt.tileIndex].populationBlocks.Count; i++)
			{
				PopulationBlock value = tiles[evt.tileIndex].populationBlocks[i];
				value.satisfaction = Mathf.Max(0f, value.satisfaction - 20f);
				tiles[evt.tileIndex].populationBlocks[i] = value;
				PopulationLedger.Scale(tiles[evt.tileIndex].populationBlocks, i, survivorScale);
			}
			tiles[evt.tileIndex].stability = Mathf.Max(0f, tiles[evt.tileIndex].stability - 10f);
			// 不逐条 Debug.Log：编辑器下每条都抓栈，长跑时会成为 events.daily 的主要成本。
		}
	}

	private void HandleRebellion(GameEvent evt)
	{
		if (evt.tileIndex >= 0 && evt.tileIndex < tiles.Length)
		{
			tiles[evt.tileIndex].order = Mathf.Max(0f, tiles[evt.tileIndex].order - 15f);
			tiles[evt.tileIndex].stability = Mathf.Max(0f, tiles[evt.tileIndex].stability - 10f);
		}
	}

	private void HandleSeasonChange(GameEvent evt)
	{
		string[] array = new string[4] { "春", "夏", "秋", "冬" };
		int num = Mathf.Clamp(Mathf.RoundToInt(evt.severity), 0, 3);
		Debug.Log((object)("[Event] 季节变换：" + array[num] + "季"));
	}

	private void HandleWarDeclaration(GameEvent evt)
	{
		Debug.Log((object)$"[Event] 战争爆发：政权{evt.realmId}，严重度{evt.severity:F0}");
	}

	private void HandlePeaceTreaty(GameEvent evt)
	{
		Debug.Log((object)$"[Event] 和平条约：政权{evt.realmId}");
	}

	private void HandlePlague(GameEvent evt)
	{
		if (evt.tileIndex >= 0 && evt.tileIndex < tiles.Length)
		{
			_diseaseSystem.OutbreakDisease(DiseaseType.Plague, currentDay, currentYear, evt.tileIndex);
			Debug.Log((object)$"[Event] 瘟疫爆发 @ 地块{evt.tileIndex}");
		}
	}

	private void HandleNaturalDisaster(GameEvent evt)
	{
		if (evt.tileIndex >= 0 && evt.tileIndex < tiles.Length)
		{
			_disasterSystem.TriggerDisaster(DisasterType.Earthquake, currentDay, currentYear, evt.tileIndex);
			Debug.Log((object)$"[Event] 自然灾害 @ 地块{evt.tileIndex}");
		}
	}

	private void HandleEconomicCrisis(GameEvent evt)
	{
		if (realms.TryGetValue(evt.realmId, out var value))
		{
			value.treasury *= 0.7f;
			Debug.Log((object)$"[Event] 经济危机：政权{evt.realmId}，国库-30%");
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

	public void InitializeWorld()
	{
		RelationRegistry.Clear();
		ContractRegistry.Clear();
		MapUnitRegistry.Clear();
		CharacterKnowledgeRegistry.Clear();
		// 动态物产是全局静态表：新局不清理会把上一局的合金/合成物带进新世界（审计 A-P1-6）。
		DynamicGoodsRegistry.Clear();
		MyriadCreation.Simulation.Units.TreasuryLedger.ClearHistory();
		if (Pipeline == null)
		{
			GenerationPipeline generationPipeline = (Pipeline = new GenerationPipeline(this));
		}
		if (startYear < 0)
		{
			startYear = currentYear;
			startDay = currentDay;
		}
		if (!ContentRegistry.IsInitialized)
		{
			ContentRegistry.Initialize();
		}
		if ((Object)(object)config == (Object)null)
		{
			config = WorldConfig.CreateRuntimeInstance();
		}
		else
		{
			config = Object.Instantiate<WorldConfig>(config);
			((Object)config).name = "RuntimeWorldConfig";
			((Object)config).hideFlags = (HideFlags)52;
		}
		tiles = new TileData[mapWidth * mapHeight];
		for (int i = 0; i < tiles.Length; i++)
		{
			tiles[i] = new TileData
			{
				tileIndex = i,
				regionId = i / 16,
				provinceId = -1,
				ownerRealmId = -1,
				occupyingRealmId = -1,
				exists = false,
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
		ApplyContentOverrides();
		CultureLineage.Apply(cultures);
		Debug.Log((object)$"[GameWorld] 世界初始化完成：{mapWidth}x{mapHeight} = {tiles.Length} 地块容量");
	}

	private void InitializeSubsystems()
	{
		_seaLandGenerator = new MyriadCreation.World.Terrain.SeaLandGenerator(config, tiles, mapWidth, mapHeight, randomSeed);
		_seaLandRecalcSeed = randomSeed;
		_featureManager = new FeatureManager(tiles, mapWidth, mapHeight);
		_mapActorManager = new MapActorManager(this);
		ApplySetupOptions();
		_campManager = new CampManager(this);
		_climateSimulator = new PlanetClimateSimulator(config, tiles, mapWidth, mapHeight, _seaLandGenerator);
		_worldMapSimulation = new WorldMapSimulationSystem(_seaLandGenerator, _climateSimulator);
		_worldMapSimulation.BeforeFullRecalculate = EnsureSeaLandGeneratorSeed;
		_currencySystem = new CurrencySystem();
		_taxSystem = new TaxSystem();
		_economyManager = new EconomyManager(tiles, tradeCenters, goodsDefs, _currencySystem, _taxSystem, realms, mapWidth, settlements);
		_politicalManager = new PoliticalManager(tiles, realms);
		_combatManager = new CombatManager(tiles, unitDefs, _seaLandGenerator, SimulationEvents, mapWidth);
		_diplomacyManager = new DiplomacyManager(realms);
		_characterManager = new CharacterManager();
		DetectionContactService.CommanderResolver = (int characterId) => _characterManager?.GetCharacter(characterId);
		_characterManager.Races = races;
		_characterManager.Economy = _economyManager;
		_characterManager.Tiles = tiles;
		_characterManager.Realms = realms;
		_thoughtManager = new ThoughtManager();
		_disasterSystem = new DisasterSystem(tiles, mapWidth, mapHeight);
		_diseaseSystem = new DiseaseSystem(tiles, _characterManager, mapWidth, mapHeight);
		_buildingSystem = new BuildingSystem(tiles, SimulationEvents);
		_innovationTree = new InnovationTree();
		if (_buildingSystem != null)
		{
			_buildingSystem.Innovations = _innovationTree;
		}
		_characterManager.Innovations = _innovationTree;
		InitializePlanSystem();
		_innovationTree.OnInnovationCompleted += OnInnovationCompletedHandler;
		_chronicle = new Chronicle();
		_diplomacyManager.Chronicle = _chronicle;
		_aiManager = new AIManager();
		SocietyManager societyManager = new SocietyManager();
		FactionManager factionManager = new FactionManager();
		_regimeDynamics = new RegimeChangeDynamics(_innovationTree, _chronicle);
		Dictionary<int, RealmSociety> societyCache = new Dictionary<int, RealmSociety>();
		_politicsSimulationSystem = new PoliticsSimulationSystem(_politicalManager, realms, tiles, armies, _wars, _economyManager, _disasterSystem, _innovationTree, societyManager, factionManager, _regimeDynamics, _characterManager, societyCache);
		_settlementSimulationSystem = new SettlementSimulationSystem(settlements, tiles, mapWidth, mapHeight, armies, this, _mapActorManager, _campManager);
		RegisterSettlementCommandHandlers();
		RegisterDiplomacyCommandHandlers();
		_aiIntentExecutor = new AIIntentExecutor(_simulationCommands);
		RegisterSimulationSchedules();
	}

	public void ReinitializeSubsystems()
	{
		InitializeSubsystems();
		Debug.Log((object)"[GameWorld] 子系统重建完成（读档后）");
	}

	private void EnsureAIControllers()
	{
		if (_aiManager == null || realms == null) return;
		var existing = _aiManager.GetAllControllers();
		foreach (var realmId in realms.Keys)
		{
			if (!existing.ContainsKey(realmId)) _aiManager.CreateController(realmId);
		}
	}

	/// <summary>
	/// 新政权形成的统一收尾：行政区划、AI 控制器、外交关系。
	/// 由 StateFormationHost 在写入领土/首领/家族之后调用；不生成官职（官职由每日 tick 按制度门槛补）。
	/// </summary>
	internal void NotifyRealmCreated()
	{
		EnsureAdminDivisions();
		EnsureAIControllers();
		_diplomacyManager?.EnsureRelations();
	}

	private void InitializeGoodsDefs()
	{
		if (GoodsCatalog.IsLoaded)
		{
			foreach (KeyValuePair<int, GoodsDef> pair in GoodsCatalog.All)
			{
				goodsDefs[pair.Key] = pair.Value;
			}
			ApplyGoodsProperties();
			return;
		}
		AddGoodsDef(0, "小麦", GameEnums.GoodsCategory.Crop, 1f, 0.1f, hasShelfLife: true, 7300f);
		AddGoodsDef(1, "肉类", GameEnums.GoodsCategory.Food, 2f, 0.15f, hasShelfLife: true, 14f);
		AddGoodsDef(2, "鱼类", GameEnums.GoodsCategory.Food, 1.5f, 0.1f, hasShelfLife: true, 7f);
		AddGoodsDef(3, "盐", GameEnums.GoodsCategory.Food, 3f, 0.05f, hasShelfLife: false, 0f);
		AddGoodsDef(10, "稻谷", GameEnums.GoodsCategory.Crop, 0.8f, 0.1f, hasShelfLife: true, 7300f);
		// 兜底表必须显式标可食：AddGoodsDef 不会推断 edible，漏标会让消费链以为全图无粮。
		MarkEdible(0);
		MarkEdible(1);
		MarkEdible(2);
		MarkEdible(10);
		AddGoodsDef(15, "粟", GameEnums.GoodsCategory.Crop, 1.0f, 0.1f, hasShelfLife: true, 7300f);
		AddGoodsDef(16, "黍", GameEnums.GoodsCategory.Crop, 1.0f, 0.1f, hasShelfLife: true, 7300f);
		AddGoodsDef(17, "大麦", GameEnums.GoodsCategory.Crop, 0.9f, 0.1f, hasShelfLife: true, 365f);
		AddGoodsDef(18, "玉米", GameEnums.GoodsCategory.Crop, 0.9f, 0.1f, hasShelfLife: true, 365f);
		AddGoodsDef(19, "马铃薯", GameEnums.GoodsCategory.Crop, 1.2f, 0.2f, hasShelfLife: true, 180f);
		AddGoodsDef(100, "面粉", GameEnums.GoodsCategory.Food, 2f, 0.1f, hasShelfLife: true, 180f);
		AddGoodsDef(101, "米", GameEnums.GoodsCategory.Food, 2f, 0.1f, hasShelfLife: true, 365f);
		AddGoodsDef(11, "棉花", GameEnums.GoodsCategory.Crop, 2.5f, 0.08f, hasShelfLife: false, 0f);
		AddGoodsDef(12, "棉布", GameEnums.GoodsCategory.Cloth, 6f, 0.05f, hasShelfLife: false, 0f);
		AddGoodsDef(110, "亚麻", GameEnums.GoodsCategory.Fiber, 2f, 0.05f, hasShelfLife: false, 0f);
		AddGoodsDef(111, "芦苇", GameEnums.GoodsCategory.Fiber, 1f, 0.05f, hasShelfLife: false, 0f);
		AddGoodsDef(112, "莎草", GameEnums.GoodsCategory.Fiber, 1.5f, 0.05f, hasShelfLife: false, 0f);
		AddGoodsDef(120, "麻布", GameEnums.GoodsCategory.Cloth, 6f, 0.05f, hasShelfLife: false, 0f);
		AddGoodsDef(121, "莎草纸", GameEnums.GoodsCategory.Cloth, 8f, 0.05f, hasShelfLife: false, 0f);
		AddGoodsDef(122, "苇席", GameEnums.GoodsCategory.Cloth, 3f, 0.1f, hasShelfLife: false, 0f);
		AddGoodsDef(13, "皮革", GameEnums.GoodsCategory.Leather, 4f, 0.08f, hasShelfLife: false, 0f);
		AddGoodsDef(130, "生皮", GameEnums.GoodsCategory.Hide, 2f, 0.1f, hasShelfLife: false, 0f);
		AddGoodsDef(131, "毛皮", GameEnums.GoodsCategory.Fur, 8f, 0.05f, hasShelfLife: false, 0f);
		AddGoodsDef(140, "黏土", GameEnums.GoodsCategory.Clay, 0.5f, 0.4f, hasShelfLife: false, 0f);
		AddGoodsDef(141, "陶器", GameEnums.GoodsCategory.Ceramic, 4f, 0.2f, hasShelfLife: false, 0f);
		AddGoodsDef(142, "石灰", GameEnums.GoodsCategory.Stone, 2f, 0.2f, hasShelfLife: false, 0f);
		AddGoodsDef(143, "玻璃", GameEnums.GoodsCategory.Glass, 10f, 0.1f, hasShelfLife: false, 0f);
		AddGoodsDef(14, "木炭", GameEnums.GoodsCategory.Wood, 1.5f, 0.2f, hasShelfLife: false, 0f);
		AddGoodsDef(20, "马", GameEnums.GoodsCategory.Livestock, 20f, 1f, hasShelfLife: false, 0f);
		AddGoodsDef(21, "牛", GameEnums.GoodsCategory.Livestock, 15f, 1f, hasShelfLife: false, 0f);
		AddGoodsDef(22, "羊", GameEnums.GoodsCategory.Livestock, 5f, 0.3f, hasShelfLife: false, 0f);
		AddGoodsDef(23, "骆驼", GameEnums.GoodsCategory.Livestock, 30f, 1f, hasShelfLife: false, 0f);
		AddGoodsDef(24, "驴", GameEnums.GoodsCategory.Livestock, 8f, 0.4f, hasShelfLife: false, 0f);
		AddGoodsDef(25, "骡", GameEnums.GoodsCategory.Livestock, 12f, 0.5f, hasShelfLife: false, 0f);
		AddGoodsDef(26, "牦牛", GameEnums.GoodsCategory.Livestock, 18f, 1f, hasShelfLife: false, 0f);
		AddGoodsDef(27, "驯鹿", GameEnums.GoodsCategory.Livestock, 14f, 0.8f, hasShelfLife: false, 0f);
		AddGoodsDef(28, "羊驼", GameEnums.GoodsCategory.Livestock, 10f, 0.3f, hasShelfLife: false, 0f);
		AddGoodsDef(30, "原木", GameEnums.GoodsCategory.Wood, 0.5f, 0.5f, hasShelfLife: false, 0f);
		AddGoodsDef(31, "加工木材", GameEnums.GoodsCategory.Wood, 1.2f, 0.4f, hasShelfLife: false, 0f, 30, 0.8f);
		AddGoodsDef(40, "石料", GameEnums.GoodsCategory.Stone, 0.3f, 0.8f, hasShelfLife: false, 0f);
		AddGoodsDef(50, "铁矿", GameEnums.GoodsCategory.MetalOre, 1.5f, 0.3f, hasShelfLife: false, 0f);
		AddGoodsDef(51, "铜矿", GameEnums.GoodsCategory.MetalOre, 2f, 0.3f, hasShelfLife: false, 0f);
		AddGoodsDef(54, "锡矿", GameEnums.GoodsCategory.MetalOre, 3f, 0.25f, hasShelfLife: false, 0f);
		AddGoodsDef(58, "铅矿", GameEnums.GoodsCategory.MetalOre, 1.2f, 0.3f, hasShelfLife: false, 0f);
		AddGoodsDef(52, "铁", GameEnums.GoodsCategory.Metal, 4f, 0.15f, hasShelfLife: false, 0f, 50, 0.6f);
		AddGoodsDef(53, "铜", GameEnums.GoodsCategory.Metal, 5f, 0.15f, hasShelfLife: false, 0f, 51, 0.6f);
		AddGoodsDef(55, "锡", GameEnums.GoodsCategory.Metal, 8f, 0.1f, hasShelfLife: false, 0f, 54, 0.5f);
		AddGoodsDef(59, "铅", GameEnums.GoodsCategory.Metal, 2.5f, 0.2f, hasShelfLife: false, 0f, 58, 0.7f);
		AddGoodsDef(56, "青铜", GameEnums.GoodsCategory.Metal, 10f, 0.15f, hasShelfLife: false, 0f, 53, 0.8f);
		AddGoodsDef(57, "钢", GameEnums.GoodsCategory.Metal, 15f, 0.12f, hasShelfLife: false, 0f, 52, 0.5f);
		AddGoodsDef(60, "金", GameEnums.GoodsCategory.Metal, 100f, 0.05f, hasShelfLife: false, 0f);
		AddGoodsDef(61, "银", GameEnums.GoodsCategory.Metal, 15f, 0.05f, hasShelfLife: false, 0f);
		AddGoodsDef(70, "武器", GameEnums.GoodsCategory.Equipment, 10f, 0.1f, hasShelfLife: false, 0f, 50, 0.3f);
		AddGoodsDef(71, "盔甲", GameEnums.GoodsCategory.Equipment, 15f, 0.2f, hasShelfLife: false, 0f, 50, 0.5f);
		AddGoodsDef(80, "丝绸", GameEnums.GoodsCategory.Luxury, 50f, 0.02f, hasShelfLife: false, 0f);
		AddGoodsDef(81, "香料", GameEnums.GoodsCategory.Luxury, 30f, 0.01f, hasShelfLife: false, 0f);
		AddGoodsDef(90, "奴隶", GameEnums.GoodsCategory.Slave, 30f, 0f, hasShelfLife: false, 0f);
		MarkNaturalResource(50, ResourceType.Mineral, 0.4f, -1, 0.6f, 0.3f);
		MarkNaturalResource(51, ResourceType.Mineral, 0.35f, -1, 0.7f, 0.2f);
		MarkNaturalResource(54, ResourceType.Mineral, 0.15f, 1104, 0.4f, 0.4f);
		MarkNaturalResource(58, ResourceType.Mineral, 0.3f, -1, 0.7f, 0.2f);
		MarkNaturalResource(30, ResourceType.Forest, 0.6f, -1);
		MarkNaturalResource(40, ResourceType.Mineral, 0.5f, -1, 0.85f, 0.1f);
		MarkNaturalResource(3, ResourceType.Special, 0.3f, 934, 0.5f, 0.3f);
		MarkNaturalResource(130, ResourceType.WildAnimal, 0.12f, -1);
		MarkNaturalResource(131, ResourceType.WildAnimal, 0.08f, -1);
		MarkNaturalResource(140, ResourceType.Special, 0.35f, -1, 0.9f, 0.1f);
		MarkPerishable(0, 0.2f);
		MarkPerishable(1, 0.1f);
		MarkPerishable(2, 0.15f);
		MarkPerishable(10, 0.05f);
		MarkLiving(20, 0.02f, 1);
		MarkLiving(21, 0.02f, 1);
		MarkLiving(22, 0.02f, 1);
		MarkLiving(23, 0.02f, 1);
		MarkLiving(24, 0.02f, 1);
		MarkLiving(25, 0.02f, 1);
		MarkLiving(26, 0.02f, 1);
		MarkLiving(27, 0.02f, 1);
		MarkLiving(28, 0.02f, 1);
		MarkLiving(90, 0f, 0);
		ApplyGoodsProperties();
	}

	private void ApplyGoodsProperties()
	{
		foreach (int item in new List<int>(goodsDefs.Keys))
		{
			if (GoodsPropertyCatalog.TryGet(item, out var properties))
			{
				GoodsDef value = goodsDefs[item];
				value.properties = new List<GoodsPropertyValue>(properties);
				goodsDefs[item] = value;
			}
		}
	}

	private void MarkEdible(int goodsId)
	{
		if (goodsDefs.TryGetValue(goodsId, out var value))
		{
			value.edible = true;
			goodsDefs[goodsId] = value;
		}
	}

	private void MarkPerishable(int goodsId, float spoilagePerDay)
	{
		if (goodsDefs.TryGetValue(goodsId, out var value))
		{
			value.isPerishable = true;
			value.spoilagePerDay = spoilagePerDay;
			goodsDefs[goodsId] = value;
		}
	}

	private void MarkLiving(int goodsId, float spoilagePerDay, int slaughterGoodsId)
	{
		if (goodsDefs.TryGetValue(goodsId, out var value))
		{
			value.isLiving = true;
			value.spoilagePerDay = spoilagePerDay;
			value.canBeSlaughtered = slaughterGoodsId > 0;
			value.slaughterGoodsId = slaughterGoodsId;
			goodsDefs[goodsId] = value;
		}
	}

	private void MarkNaturalResource(int goodsId, ResourceType type, float abundance, int requiredInnovation, float surfaceShare = 1f, float shaftShare = 0f)
	{
		if (goodsDefs.TryGetValue(goodsId, out var value))
		{
			value.isNaturalResource = true;
			value.resourceType = type;
			value.baseAbundance = abundance;
			value.requiredInnovation = requiredInnovation;
			value.originBiomes = DefaultNaturalBiomes(type);
			value.spawnCondition = ResourceSpawnCondition.Unrestricted;
			value.surfaceDepthShare = surfaceShare;
			value.shaftDepthShare = shaftShare;
			value.renewable = type == ResourceType.Forest || type == ResourceType.WildAnimal || type == ResourceType.WildPlant || type == ResourceType.Aquatic;
			goodsDefs[goodsId] = value;
		}
	}

	private static List<GameEnums.BiomeType> DefaultNaturalBiomes(ResourceType type)
	{
		var biomes = new List<GameEnums.BiomeType>();
		foreach (GameEnums.BiomeType biome in Enum.GetValues(typeof(GameEnums.BiomeType)))
		{
			if (biome == GameEnums.BiomeType.IceSheet || biome == GameEnums.BiomeType.MountainGlacier) continue;
			biomes.Add(biome);
		}
		return biomes;
	}

	private void AddGoodsDef(int id, string name, GameEnums.GoodsCategory category, float baseValue, float weight, bool hasShelfLife, float shelfLifeDays, int processedFrom = -1, float processingRatio = 1f)
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
		if (UnitCatalog.IsLoaded)
		{
			foreach (KeyValuePair<int, UnitDef> pair in UnitCatalog.All)
			{
				unitDefs[pair.Key] = pair.Value;
			}
			return;
		}
		AddUnitDef(100, "轻装步兵", GameEnums.UnitArmType.Infantry, 1, 10f, 0f, 8f, 60f, 2f, 1f, 50f, 1f);
		AddUnitDef(101, "重装步兵", GameEnums.UnitArmType.Infantry, 2, 15f, 0f, 15f, 70f, 1.5f, 1.5f, 80f, 1.5f, 202);
		AddUnitDef(102, "精锐步兵", GameEnums.UnitArmType.Infantry, 3, 20f, 0f, 22f, 80f, 1.2f, 2f, 120f, 2f, 203);
		AddUnitDef(110, "弓箭手", GameEnums.UnitArmType.Infantry, 1, 5f, 12f, 6f, 50f, 2f, 0.8f, 40f, 1f, 907);
		AddUnitDef(111, "弩手", GameEnums.UnitArmType.Infantry, 2, 6f, 18f, 8f, 55f, 1.5f, 1f, 60f, 1.2f, 304);
		AddUnitDef(200, "轻骑兵", GameEnums.UnitArmType.Cavalry, 1, 12f, 5f, 8f, 65f, 4f, 1.5f, 60f, 1f, 923);
		AddUnitDef(201, "重骑兵", GameEnums.UnitArmType.Cavalry, 2, 20f, 8f, 18f, 75f, 3f, 2.5f, 100f, 1.5f, 924);
		AddUnitDef(202, "具装甲骑", GameEnums.UnitArmType.Cavalry, 3, 28f, 10f, 25f, 85f, 2.5f, 3f, 150f, 2f, 303);
		AddUnitDef(203, "超重装骑兵", GameEnums.UnitArmType.Cavalry, 4, 36f, 12f, 32f, 95f, 2f, 4f, 200f, 3f, 1006);
		AddUnitDef(210, "战车兵", GameEnums.UnitArmType.Cavalry, 2, 20f, 6f, 14f, 70f, 3.5f, 2.5f, 100f, 2f, 1013);
		AddUnitDef(300, "桨帆船", GameEnums.UnitArmType.Navy, 1, 15f, 10f, 10f, 60f, 3f, 2f, 80f, 1f, 402);
		AddUnitDef(301, "帆船战舰", GameEnums.UnitArmType.Navy, 2, 20f, 15f, 15f, 70f, 4f, 2.5f, 120f, 1.5f, 405);
		AddUnitDef(302, "撞角战船", GameEnums.UnitArmType.Navy, 2, 28f, 4f, 14f, 65f, 3.5f, 2.5f, 100f, 2f, 1007);
		AddUnitDef(303, "远洋贸易船", GameEnums.UnitArmType.Navy, 3, 8f, 2f, 18f, 70f, 4.5f, 1.5f, 80f, 2.5f, 1008);
		SetUnitRecruitCosts(101, (70, 1.2f), (71, 1f));
		SetUnitRecruitCosts(102, (70, 1.5f), (71, 1.5f));
		SetUnitRecruitCosts(200, (70, 1f), (20, 1f));
		SetUnitRecruitCosts(201, (70, 1.2f), (71, 1.2f), (20, 1.5f));
		SetUnitRecruitCosts(202, (70, 1.5f), (71, 1.5f), (20, 2f));
		SetUnitRecruitCosts(203, (70, 2f), (71, 2f), (20, 2.5f));
		SetUnitRecruitCosts(210, (70, 1.2f), (20, 2f));
		SetUnitRecruitCosts(300, (30, 2f), (31, 1f));
		SetUnitRecruitCosts(301, (31, 2f), (50, 1f));
		SetUnitRecruitCosts(302, (30, 2f), (50, 1.5f));
		SetUnitRecruitCosts(303, (31, 2f), (11, 1f));
	}

	private void SetUnitRecruitCosts(int unitId, params (int goodsId, float amount)[] costs)
	{
		if (unitDefs.TryGetValue(unitId, out var value))
		{
			value.recruitCost = new Dictionary<int, float>();
			for (int i = 0; i < costs.Length; i++)
			{
				var (key, value2) = costs[i];
				value.recruitCost[key] = value2;
			}
			unitDefs[unitId] = value;
		}
	}

	private void AddUnitDef(int id, string name, GameEnums.UnitArmType category, int tier, float melee, float ranged, float def, float morale, float speed, float supply, float manpower, float recruitCost, params int[] requiredInnovations)
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
		int num = (tiles.Length + 15) / 16;
		// 区域库同时播粮和货币：补给结算（尤其个人出资降级国库后回退区域库）
		// 要求同一个库能同时付粮+币，只有粮会让城库/省库永远付不出。
		int foodGoodsId = FindFoodGoodsId();
		// 用付款结算同一个选择器（优先高价值金属），避免"播的是铁、结算找的是金"的选择器分叉。
		int currencyGoodsId = MyriadCreation.Simulation.Units.TreasuryLedger.SelectCurrencyGoodsId(goodsDefs);
		if (currencyGoodsId < 0) currencyGoodsId = FindCurrencyGoodsId();
		for (int i = 0; i < num; i++)
		{
			TradeCenter tradeCenter = new TradeCenter
			{
				regionId = i,
				centerName = $"地区{i}贸易中心",
				centerTileIndex = i * 16
			};
			if (foodGoodsId >= 0) tradeCenter.inventory[foodGoodsId] = 1000f;
			if (currencyGoodsId >= 0) tradeCenter.inventory[currencyGoodsId] = 1000f;
			tradeCenters[i] = tradeCenter;
		}
	}

	private void InitializeDefaultRaces()
	{
		races[0] = new RaceData
		{
			raceId = 0,
			raceName = "人族",
			baseLifespan = 1f,
			growthRate = 1f,
			reproductionRate = 1f,
			physicalStrength = 1f,
			diseaseResistance = 1f,
			environmentalTolerance = 1f,
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
			worshipVector = new float[3] { 0.8f, 0.1f, 0.1f },
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
			worshipVector = new float[3] { 0.1f, 0.8f, 0.1f },
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
			worshipVector = new float[3] { 0.1f, 0.1f, 0.8f },
			materialStyle = 2,
			symbolicFocus = 2,
			environmentAdapt = 2,
			maturity = 0.7f
		};
	}

	public void ApplySetupOptions()
	{
		SetupOptions.Normalize();
		if (_mapActorManager != null)
		{
			_mapActorManager.NaturalSpawning = SetupOptions.spawnRates.enableNaturalSpawning;
			_mapActorManager.SpawnRateMultiplier = SetupOptions.spawnRates.mapActorMultiplier;
		}
	}

	private void ApplyContentOverrides()
	{
		//IL_00c3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00fe: Unknown result type (might be due to invalid IL or missing references)
		if (!ContentRegistry.IsInitialized)
		{
			return;
		}
		int num = 0;
		foreach (RaceData item in ContentResolvers.Races.All)
		{
			races[item.raceId] = item;
			num++;
		}
		int num2 = 0;
		foreach (ContentRegistry.CultureContentPack item2 in ContentResolvers.Cultures.All)
		{
			cultures[item2.data.cultureId] = item2.data;
			num2++;
		}
		foreach (KeyValuePair<int, CultureData> culture in cultures)
		{
			if (culture.Value.color == Color.white)
			{
				culture.Value.color = Color.HSVToRGB((float)culture.Key * 0.618f % 1f, 0.55f, 0.85f);
			}
		}
		if (num > 0 || num2 > 0)
		{
			Debug.Log((object)$"[GameWorld] 内容注册表覆盖：种族 +{num}，文化 +{num2}（数据驱动优先）");
		}
		InitializeFaithSystems();
	}

	/// <summary>
	/// G2：解析"传承"（lineage）。规则：
	/// ① 内容显式给了 `lineageFaithId` → 用它（须指向存在的 faith 节点，否则报错并置 -1）；
	/// ② 没给 → 若层级父本身是 faith 节点，则传承 = 父（旧数据的兼容推导）；
	/// ③ 其余 → -1（初代信仰）。
	/// 结果做**环检测**：沿传承链最多走 64 步，成环则报错并置 -1。
	/// </summary>
	private static int ResolveLineageFaithId(ReligionDef def)
	{
		int lineage = def.lineageFaithId;
		if (lineage < 0
			&& def.parentReligionId >= 0
			&& ReligionCatalog.All.TryGetValue(def.parentReligionId, out ReligionDef parent)
			&& parent != null
			&& parent.nodeType == ReligionNodeType.Succession)
		{
			lineage = parent.religionId;
		}
		if (lineage < 0 || lineage == def.religionId) return -1;
		if (!ReligionCatalog.All.TryGetValue(lineage, out ReligionDef lineageDef) || lineageDef == null)
		{
			Debug.LogError((object)$"[GameWorld] faith `{def.religionName}` 的传承 lineageFaithId={lineage} 不存在，置 -1");
			return -1;
		}
		// 环检测
		int cursor = lineage;
		for (int i = 0; i < 64 && cursor >= 0; i++)
		{
			if (cursor == def.religionId)
			{
				Debug.LogError((object)$"[GameWorld] faith `{def.religionName}` 的传承链成环（回到自身），置 -1");
				return -1;
			}
			if (!ReligionCatalog.All.TryGetValue(cursor, out ReligionDef node) || node == null) break;
			cursor = node.lineageFaithId;
		}
		return lineage;
	}

	/// <summary>
	/// G3/G4：建宗派并让每个 faith 落到宗派。
	/// ① `nodeType == Tradition` 的节点是宗派；其 `parentReligionId` 只能是 religion（层级父），
	///    宗派自身的分裂关系走 `parentDenominationId`；
	/// ② 某 religion 没有任何真实宗派时，补一个 `isDefault=true` 的默认宗派；
	/// ③ faith 挂宗派顺序：内容显式 `denominationId` → 层级父宗派 → 默认宗派 → 沿父链上溯；
	/// ④ 若某 religion 只有分支宗派、其未分宗 faith 无处可挂，才为该 religion 补默认宗派（回退）。
	/// </summary>
	private void InitializeDenominations()
	{
		_denominations.Clear();
		var byId = new Dictionary<int, Denomination>();
		var defaultByReligion = new Dictionary<int, Denomination>();
		var realDefs = new List<ReligionDef>();

		// ① 真实宗派：层级父必须是 religion。
		foreach (KeyValuePair<int, ReligionDef> item in ReligionCatalog.All)
		{
			ReligionDef value = item.Value;
			if (value == null || value.nodeType != ReligionNodeType.Tradition) continue;
			if (value.parentReligionId < 0
				|| !ReligionCatalog.All.TryGetValue(value.parentReligionId, out ReligionDef parent)
				|| parent == null
				|| parent.nodeType != ReligionNodeType.Religion)
			{
				Debug.LogError((object)$"[GameWorld] 宗派 `{value.religionName}`(id={value.religionId}) 的 parentReligionId={value.parentReligionId} 不是 religion，跳过建运行态");
				continue;
			}
			var denom = new Denomination
			{
				denominationId = value.religionId,
				religionId = value.parentReligionId,
				parentDenominationId = value.parentDenominationId,
				denominationName = value.religionName,
				isDefault = false,
				status = value.isMainstreamTradition ? DenominationStatus.Mainstream : DenominationStatus.Tolerated,
			};
			_denominations.Add(denom);
			byId[denom.denominationId] = denom;
			realDefs.Add(value);
		}

		// ② 宗派分裂父校验（与内容声明顺序无关）：必须指向同 religion 的另一个宗派，且不能成环。
		foreach (ReligionDef value in realDefs)
		{
			Denomination denom = byId[value.religionId];
			int parentDenomId = value.parentDenominationId;
			if (parentDenomId < 0)
			{
				denom.parentDenominationId = -1;
				continue;
			}
			if (!byId.TryGetValue(parentDenomId, out Denomination parentDenom) || parentDenom == null)
			{
				Debug.LogError((object)$"[GameWorld] 宗派 `{value.religionName}`(id={value.religionId}) 的 parentDenominationId={parentDenomId} 不存在，置 -1");
				denom.parentDenominationId = -1;
				continue;
			}
			if (parentDenom.religionId != denom.religionId)
			{
				Debug.LogError((object)$"[GameWorld] 宗派 `{value.religionName}`(id={value.religionId}) 的分裂父 {parentDenomId} 不属于同一 religion，置 -1");
				denom.parentDenominationId = -1;
				continue;
			}
			bool cycle = false;
			int cursor = parentDenomId;
			for (int depth = 0; depth < 64 && cursor >= 0; depth++)
			{
				if (cursor == value.religionId) { cycle = true; break; }
				if (!byId.TryGetValue(cursor, out Denomination node) || node == null) break;
				cursor = node.parentDenominationId;
			}
			if (cycle)
			{
				Debug.LogError((object)$"[GameWorld] 宗派 `{value.religionName}`(id={value.religionId}) 的分裂链成环，置 -1");
				denom.parentDenominationId = -1;
				continue;
			}
			denom.parentDenominationId = parentDenomId;
		}

		// ③ 无真实宗派的 religion 先补默认宗派。
		foreach (KeyValuePair<int, ReligionDef> item in ReligionCatalog.All)
		{
			ReligionDef value = item.Value;
			if (value == null || value.nodeType != ReligionNodeType.Religion) continue;
			bool hasReal = false;
			for (int i = 0; i < _denominations.Count; i++)
			{
				if (!_denominations[i].isDefault && _denominations[i].religionId == value.religionId) { hasReal = true; break; }
			}
			if (hasReal) continue;
			EnsureDefaultDenomination(value.religionId, byId, defaultByReligion);
		}

		// ④ faith 挂宗派。
		int attached = 0;
		int orphan = 0;
		int reanchored = 0;
		int fallbackDefaults = 0;
		foreach (FaithSystem faith in _faithSystems)
		{
			if (faith == null) continue;
			int parentId = faith.religionId;

			// 4.1 内容显式声明（ReligionDef.denominationId），必须与上位 religion 一致。
			if (ReligionCatalog.All.TryGetValue(faith.faithId, out ReligionDef faithDef)
				&& faithDef != null
				&& faithDef.denominationId >= 0)
			{
				if (byId.TryGetValue(faithDef.denominationId, out Denomination explicitDenom)
					&& explicitDenom != null
					&& explicitDenom.religionId == parentId)
				{
					faith.denominationId = explicitDenom.denominationId;
					explicitDenom.faithIds.Add(faith.faithId);
					attached++;
					continue;
				}
				Debug.LogError((object)$"[GameWorld] faith `{faith.faithName}`(id={faith.faithId}) 的显式 denominationId={faithDef.denominationId} 无效或不属于 religion={parentId}，改走兜底");
			}

			// 4.2 层级父本身就是宗派（兼容旧内容）。
			if (parentId >= 0 && byId.TryGetValue(parentId, out Denomination parentDenom) && parentDenom != null)
			{
				faith.denominationId = parentDenom.denominationId;
				parentDenom.faithIds.Add(faith.faithId);
				attached++;
				continue;
			}

			// 4.3 religion 默认宗派。
			if (parentId >= 0 && defaultByReligion.TryGetValue(parentId, out Denomination def) && def != null)
			{
				faith.denominationId = def.denominationId;
				def.faithIds.Add(faith.faithId);
				attached++;
				continue;
			}

			// 4.4 沿父链上溯（跳过 faith / school 等非层级节点）。
			int cursor = parentId;
			int anchorDenom = -1;
			int resolvedReligion = -1;
			for (int depth = 0; depth < 16 && cursor >= 0; depth++)
			{
				if (byId.TryGetValue(cursor, out Denomination up) && up != null) { anchorDenom = up.denominationId; break; }
				if (defaultByReligion.TryGetValue(cursor, out Denomination upDef) && upDef != null) { anchorDenom = upDef.denominationId; break; }
				if (ReligionCatalog.All.TryGetValue(cursor, out ReligionDef node) && node != null)
				{
					if (node.nodeType == ReligionNodeType.Religion)
					{
						resolvedReligion = node.religionId;
						break;
					}
					if (node.parentReligionId >= 0)
					{
						cursor = node.parentReligionId;
						continue;
					}
				}
				break;
			}
			// 默认宗派的合成 id 是负数（`-100000 - religionId`），守卫必须判 `!= -1`。
			if (anchorDenom != -1 && byId.TryGetValue(anchorDenom, out Denomination anchor) && anchor != null)
			{
				faith.denominationId = anchor.denominationId;
				anchor.faithIds.Add(faith.faithId);
				attached++;
				reanchored++;
				continue;
			}

			// 4.5 只有分支宗派的 religion：为未分宗 faith 回退补默认宗派。
			if (resolvedReligion >= 0)
			{
				Denomination fallback = EnsureDefaultDenomination(resolvedReligion, byId, defaultByReligion);
				if (fallback != null)
				{
					faith.denominationId = fallback.denominationId;
					fallback.faithIds.Add(faith.faithId);
					attached++;
					fallbackDefaults++;
					continue;
				}
			}

			orphan++;
			Debug.LogWarning((object)$"[GameWorld] faith `{faith.faithName}`(id={faith.faithId}) 无法挂宗派（religionId={parentId} 及上溯均无 religion/宗派），denominationId 保持 -1");
		}

		int realCount = _denominations.Count - defaultByReligion.Count;
		int emptyReal = 0;
		foreach (Denomination denomination in _denominations)
		{
			if (denomination != null && !denomination.isDefault && denomination.faithIds.Count == 0) emptyReal++;
		}
		Debug.Log((object)$"[GameWorld] 宗派初始化：{_denominations.Count} 个（真实 {realCount}，默认 {defaultByReligion.Count}，其中回退补默认 {fallbackDefaults}）；faith 挂宗派 {attached} 个（其中上溯锚点 {reanchored} 个），未挂 {orphan} 个；真实宗派暂无 faith {emptyReal} 个");
	}

	private Denomination EnsureDefaultDenomination(int religionId, Dictionary<int, Denomination> byId, Dictionary<int, Denomination> defaultByReligion)
	{
		if (defaultByReligion.TryGetValue(religionId, out Denomination existing) && existing != null) return existing;
		if (!ReligionCatalog.All.TryGetValue(religionId, out ReligionDef religion)
			|| religion == null
			|| religion.nodeType != ReligionNodeType.Religion)
		{
			return null;
		}
		var fallback = new Denomination
		{
			denominationId = -100000 - religionId,   // 负号段：默认宗派的稳定合成 id
			religionId = religionId,
			denominationName = religion.religionName,
			isDefault = true,
			status = DenominationStatus.Mainstream,
		};
		_denominations.Add(fallback);
		byId[fallback.denominationId] = fallback;
		defaultByReligion[religionId] = fallback;
		return fallback;
	}

	private void InitializeFaithSystems()
	{
		_faithSystems.Clear();
		// G1（RELIGION_V2 §0.5）：先按 nodeType 判层，再只对 **faith 层** 建运行态。
		// 设计口径：faith 是主运行单位（不是 religion）；Tradition 那批实际是"宗派"，
		// 归 denomination 层，等 G3 建 Denomination 后再建运行态，此处先计数不建。
		int faithLayer = 0;
		int religionLayer = 0;
		int denominationLayer = 0;
		int schoolLayer = 0;
		foreach (KeyValuePair<int, ReligionDef> item in ReligionCatalog.All)
		{
			ReligionDef value = item.Value;
			switch (value.nodeType)
			{
			case ReligionNodeType.Succession:
				faithLayer++;
				break;
			case ReligionNodeType.Religion:
				religionLayer++;
				break;
			case ReligionNodeType.Tradition:
				denominationLayer++;
				break;
			default:
				schoolLayer++;
				break;
			}
		}
		foreach (KeyValuePair<int, ReligionDef> item in ReligionCatalog.All)
		{
			ReligionDef value = item.Value;
			if (value.nodeType == ReligionNodeType.Succession)
			{
				// "faith 必挂"：faith 层的父 religion 必须存在（根信仰除外）。
				// 不满足只记错误、不默认归属——宁可少一个 faith，也不要错挂。
				if (!value.IsRoot && !ReligionCatalog.All.ContainsKey(value.parentReligionId))
				{
					Debug.LogError($"[GameWorld] faith `{value.religionName}`(id={value.religionId}) 的父 religion id={value.parentReligionId} 不存在，跳过建运行态");
					continue;
				}
				FaithSystem faithSystem = new FaithSystem
				{
					faithId = value.religionId,
					faithName = value.religionName,
					// G1：运行态带上位 religion。根信仰（无父）暂时留 -1 并告警，
					// 等 G1 收口时把"根 faith 自己就是一个 religion"这条补上。
					religionId = value.parentReligionId,
					denominationId = -1,
					lineageFaithId = ResolveLineageFaithId(value),
					fervor = 50f,
					highPriestCharacterId = -1
				};
				if (value.IsRoot)
				{
					Debug.LogWarning((object)$"[GameWorld] faith `{value.religionName}` 无上位 religion（根信仰），religionId=-1 待 G1 收口");
				}
				faithSystem.virtues.AddRange(value.virtues);
				faithSystem.sins.AddRange(value.sins);
				_faithSystems.Add(faithSystem);
			}
		}
		Debug.Log((object)$"[GameWorld] 宗教运行时初始化：{_faithSystems.Count} 个教统；分层计数 faith={faithLayer} religion={religionLayer} denomination={denominationLayer}(待G3) school={schoolLayer}");
		InitializeDenominations();
		_religionSimulationEvents?.Dispose();
		_religionSimulationEvents = new ReligionSimulationEventHandler(_faithSystems, _simulationEvents);
	}

	private void Awake()
	{
	}

	private void Update()
	{
		TickPreload();
		_tickTimer += Time.deltaTime;
		if (_tickTimer >= tickInterval)
		{
			_tickTimer = 0f;
			GameTick();
		}
	}
}
}
