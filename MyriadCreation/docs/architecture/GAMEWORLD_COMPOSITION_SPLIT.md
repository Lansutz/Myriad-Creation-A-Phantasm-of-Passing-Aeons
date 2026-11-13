# GameWorld 组合根 / 数据容器收口方案（B3-next）

> 类型：只读方案（B3 剩余大项）。依据：当前 worktree 静态扫描（2026-11-01）。
> 相关：`docs/architecture/SYSTEM_OWNERSHIP.md`、`APP_EXTRACTION_PLAN.md`、`RENDERING_WORLD_CONTRACTS.md`、`ASMDEF_SPLIT_FAITH_CULTURE_LAWS.md`。
> 目标：把 **App 职责（组合根/场景流程）** 与 **WorldState 数据容器** 分离，使 `GameWorld` 回到"数据 + 查询 + 薄门面"，创建/接线/生成/读档初始化各自成为独立 host service。

---

## 0. 结论摘要

- `GameWorld` 现为 **8 个 partial + 1 个声明文件，共 3600 行**（Core 953 / Simulation 1133 / Generation 586 / Succession 342 / Queries 319 / Editing 115 / Persistence 88 / Contracts 53 + GameWorld.cs 11）。
- 其中 **组合根/接线成员约 30 个**（`InitializeWorld/InitializeSubsystems/ReinitializeSubsystems/EnsureAIControllers/Initialize*Defs/InitializeDefault*/ApplySetupOptions/ApplyContentOverrides/InitializeFaithSystems/InitializePlanSystem/Register*CommandHandlers/RegisterSimulationSchedules/Create*PlanSystem/Contracts 适配器`），**生成管线约 20 个方法**，**纯数据字段 60+**，**查询方法约 25 个**，其余为 Simulation/Succession/Editing 行为。
- **推荐切分**（依赖方向不变，全部仍属 Simulation host 层）：
  1. `GameWorld` 保留 **数据字段 + 查询 + 契约适配器 + MonoBehaviour 宿主**（实现新 `IWorldReadModel`）；
  2. 子系统引用抽 `WorldServices`（纯容器）；
  3. 组合/接线抽 `WorldCompositionRoot`（Simulation）；
  4. 生成管线抽 `WorldGenerationService`（Simulation）；
  5. 读档运行时补齐抽 `WorldLoadInitializer`（Simulation）；
  6. 世界创建/销毁抽 `WorldFactory`（Simulation，供 App 与 Infrastructure.Save 共用，避免 Infra→App 反向依赖）。
- 4 步落地：**只读接口 → 生成服务 → 组合根/Services → App/Save 切换 + 删 GameWorld 生命周期门面**；每步编译/定向测试门禁 + 可回滚。
- `WorldFactory`/`WorldCompositionRoot` **不放 App**：`Infrastructure.Save` 在读档时要创建/初始化世界（当前 `SaveSystem.cs:273-274` 直接 `AddComponent<GameWorld>`），放 App 会让 Infrastructure 依赖 App；放 Simulation host 层则 App/Save 均可调用，且不新增向上依赖。

---

## 1. 现状统计（file:line）

### 1.1 partial 职责总表

| 文件 | 行数 | 主要职责 | 组合根/接线成员（行号） | 数据/查询/行为成员（行号） |
|---|---|---|---|---|
| `GameWorld.cs` | 11 | 声明 `partial class GameWorld : MonoBehaviour` + namespace（场景脚本） | — | — |
| `GameWorld.Core.cs` | 953 | 数据字段 + 默认内容 + 子系统构造 + 契约适配器 + 规划/命令注册 + MonoBehaviour 生命周期 | `InitializeWorld` 473；`InitializeSubsystems` 535；`ReinitializeSubsystems` 580；`EnsureAIControllers` 586；`InitializeGoodsDefs` 596；`InitializeUnitDefs` 719；`InitializeTradeCenters` 783；`InitializeDefaultRaces` 806；`InitializeDefaultCultures` 822；`ApplySetupOptions` 868；`ApplyContentOverrides` 878；`InitializeFaithSystems` 914；`InitializePlanSystem` 277；`CreatePlanSystem` 314；`CreateResearchPlanSystem` 304；`CreateConstructionPlanSystem` 309；`RegisterDiplomacyCommandHandlers` 328；`RegisterSettlementCommandHandlers` 340；`Awake` 939；`Update` 943 | 数据字段：`config` 59、`mapWidth` 62、`mapHeight` 64、`randomSeed` 66、`wrapMode` 69、`tickInterval` 72、`daysPerTick` 74、`tiles` 76、`races` 79、`cultures` 82、`tradeCenters` 85、`goodsDefs` 88、`realms` 91、`PlayerRealmId` 93、`unitDefs` 96、`provinces` 99、`anchors` 102、`settlements` 105、`armies` 108、`_wars` 110、`_faithSystems` 112、`_religionSimulationEvents` 114、`startYear/startDay` 116/120、`_nextWarId/_nextArmyId` 124/126、`_seaLandGenerator` 128、`_climateSimulator` 141、`_planetTerrainGenerator` 146、`_atmosphericCirculation` 148、`_hydraulicErosion` 150、`GenConfig` 152、`SetupOptions` 154、子系统字段 156-198、时钟 202-206、事件队列 210-226、`Plans/ResearchPlans/ConstructionPlans` 228-232、服务 getters 234-275 |
| `GameWorld.Generation.cs` | 586 | 地形/气候/水文/省/聚落/初始单位生成 + 异步生成 + pipeline | `EnsureSeaLandGeneratorSeed` 51；`InitializeDefaultRealms` 62；`GenerateTerrain` 123；`GenerateTerrainBase` 141；`GenerateTerrainWithConfig` 293；`GenerateAll` 356；`GenerateProvinces` 381（public 507）；`GenerateSettlements` 390（public 517）；`GenerateInitialUnits` 456；`GenerateTerrainAsync` 537；`RegenerateFromPipeline` 577 | 碎裂/气候辅助 170/204/245/269/287；`ResolveCellsPerProvince` 364；`CalculateBaseFertility` 498；`GenerationPipeline` 创建在 Core 481-484 |
| `GameWorld.Simulation.cs` | 1133 | 每 tick/每月/每年行为与调度注册、实体的运行期创建 | `RegisterSimulationSchedules` 774（由 Core 577 调用）；`MonthlyCultureStageEvolution` 836 | `PopulationTick` 50；`AdvanceTime` 338；`UpdateFaithFervor` 360；`CheckHolySiteLoss` 377；`CheckGreatHolyWarSettlements` 400；`CalculateClimate` 460；`RecalculateHydrology` 492；`RecalculateAll` 520；`IssueDecreeOrders` 526；`SpawnDecreeCouriers` 635；`TickDecreeOrders` 692；`GameTick` 746；`TickPendingLawsDaily` 757；`ProcessWarOutcomes` 844；`CreateCity` 871；`CreateWildernessSettlement` 930；`CreateHolySite` 990；`DeclareWar` 1018；`DeclareGreatHolyWar` 1040；`CreateArmy` 1075；司法 1094-1125 |
| `GameWorld.Succession.cs` | 342 | 继承/危机/家族/头衔行为 | — | `NotifyRulerTransition` 48；`CheckRulerSuccessions` 72；`HandleInterregnum` 128；`TryStartInterregnum` 168；`RecordSuccessionCrisis` 186；`AddWarAchievement` 224；`AddDefensiveWin` 240；`GetRealmRuler` 249；`EvaluateDeadRulerLife` 268；`GetRealmLandShare` 297；`GetRealmDivisibleEstates` 321；`GetEffectiveLaw` 326；`TickLawEffectsYearly` 337 |
| `GameWorld.Queries.cs` | 319 | 纯只读查询（无副作用） | — | `TryGetSiteByAnchor` 48；`TryGetSiteAtTile` 64；`GetSitesByMode` 78；`TryGetHabitationSite` 89；`GetFaithSystem` 106；`FaithSystems` 112；`GetCharacterName` 114；`TileExists` 124；`GetValidTiles` 133；`GetValidTileCount` 146；`GetNeighbours` 159；`GetTilePopulation` 164；`GetLandTileCount` 181；`GetSeaTileCount` 190；`GetConnectedSeaCount` 199；`GetTile` 208/213；服务 getters 218-314 |
| `GameWorld.Editing.cs` | 115 | 运行时地图编辑（编辑态写 tiles） | — | `RecalculateDirty` 47；`PaintTerrain` 52；`UpdateConfig` 62；`CreateTile` 69；`RemoveTile` 97 |
| `GameWorld.Persistence.cs` | 88 | 读档运行时补齐 / ID 计数器 | `PrepareLoadedWorld` 56（调用 `InitializeUnitDefs/InitializeFaithSystems/EnsureAIControllers` 66-68）；`AdoptRuntimeIdCounters` 76 | — |
| `GameWorld.Contracts.cs` | 53 | 法律/信仰/文化 Query+Command 适配器（懒加载） | `_lawContracts/_faithContracts/_cultureContracts` 17-21；`LawQuery/LawCommand` 24/27；`FaithQuery/FaithCommand` 30/33；`CultureQuery/CultureCommand` 36/39；`Get*` 41-51 | — |

### 1.2 关键调用链（现状）

- `GameWorld.InitializeWorld()`（Core:473）→ 清静态注册表（`RelationRegistry/ContractRegistry/MapUnitRegistry/CharacterKnowledgeRegistry/DynamicGoodsRegistry`）→ 建 `GenerationPipeline`（481-484）→ `ContentRegistry.Initialize`（490-493）→ `config`（494-503）→ 分配 `tiles`（504-523）→ `InitializeSubsystems()`（524）→ `InitializeGoodsDefs/UnitDefs/TradeCenters/DefaultRaces/DefaultCultures/ApplyContentOverrides`（525-530）→ `CultureLineage.Apply`（531）。
- `InitializeSubsystems()`（Core:535）→ `SeaLandGenerator/FeatureManager/MapActorManager/CampManager/ClimateSim/WorldMapSimulation/...`（537-571）→ `RegisterSettlementCommandHandlers`（574）→ `RegisterDiplomacyCommandHandlers`（575）→ `RegisterSimulationSchedules()`（577）。
- `ReinitializeSubsystems()`（Core:580）就是 `InitializeSubsystems()` + log；读档路径 `SaveSystem.cs:458` 调用。
- `EnsureAIControllers()`（Core:586）由 `PrepareLoadedWorld`（Persistence:68）调用。
- `GenerateTerrainAsync`（Generation:537，App `GameManager.cs:198`）→ `GenerateTerrain/Base`；`GenerateTerrainBase` 在生成末尾再次 `InitializeSubsystems()`（Generation:324）与 `RegisterSimulationSchedules` 间接。
- `GameWorld.Update()`（Core:943）→ 每 `tickInterval` 调 `GameTick()`（Simulation:746）→ 各系统 tick/调度。

### 1.3 对外调用方与接口现状

**A. App（MyriadCreation.App）**
- `GameManager.cs`：`[SerializeField] GameWorld world` 39；`World => world` 48；`WorldChanged` 69；创建/复用世界 137-141、170-174；`InitializeWorld()` 149、181；`GenerateTerrain(seed)` 150；`GenerateTerrainAsync` 198；`InjectMapWorldView()` 222/354-358；读档重绑 268-280；`MapWorldViewAdapter` 创建 271/358。
- `WorldEditService.cs`：`Func<GameWorld>`/`BindWorld`/`ResolveWorld` 16-24/31/166-191（编辑服务持有具体 GameWorld）。

**B. UI（MyriadCreation.UI）**
- `UIManager.cs`：`[SerializeField] GameWorld world` 45；`BindWorld(GameWorld)` 182；`NotifyWorldChanged` 211；`BuildOfficeDisplay(GameWorld...)` 491。
- `SettlementPeopleText.cs`：`KnownAt/IsLeader/OfficeLabel/Describe/Build/BuildDivisionSummary/TryFindSettlementAnchorAtTile(GameWorld...)` 26/49/62/92/111/129/180。
- `GovernmentPanel.cs`：`GameWorld world` 20；`BindWorld` 62；`FindAnyObjectByType<GameWorld>()` 184。
- `SettlementDetailPanel.cs`：`_world` 30；`BindWorld` 52；`ResolveWorld` 198-210；`RefreshContent/BuildBodyText/ResolveRealmName(GameWorld...)` 214/305/351。

**C. Infrastructure.Save（关键！）**
- `SaveSystem.cs:273-274`：读档时 **`new GameObject + AddComponent<GameWorld>()`**；`ReinitializeSubsystems()` 458；`PrepareLoadedWorld()` 492；使用 GameWorld API：`SetupOptions/config/mapWidth/mapHeight/randomSeed/tiles/races/cultures/realms/goodsDefs/tradeCenters/GetCharacterManager/GetWars/Innovations/MapActors/AdoptRuntimeIdCounters`（`world.` grep）。
- `SaveSystem.cs:415` 注释称“AddComponent 时 Awake 已先跑 InitializeWorld”，但实际 `GameWorld.Awake()`（Core:939）为空——**注释已过期**，读档初始化实际靠 `ApplyToWorld/ReinitializeSubsystems/PrepareLoadedWorld`；收口时必须澄清。
- `FaithPersistence.cs`：依赖 `PrepareLoadedWorld` 后的 FaithSystem 骨架（13/40）。

**D. Tests / Editor**
- `new GameWorld()`：5 文件（`EntityRelationAndLivingGoodsTests`、`InnovationSystemTests`、`SettlementFoundingAndMarketSaveTests`×2、`SettlementPersistenceTests`、`UnitFoundingTests`）。
- `AddComponent<GameWorld>()`：EditMode 21 文件 + PlayMode 2 文件；`InitializeWorld` 12+2；`GenerateTerrain` 10+2；`PrepareLoadedWorld` 2（Infrastructure）；`BindWorld` App/UI/Tests。
- Editor：**0 处** GameWorld 引用。
- 场景：`Scenes/Main.unity` 引用 `GameWorld.cs` GUID `dcde6bad2048e87478c6da58636d81f4` **1 次**（场景脚本绑定，必须保留 meta/GUID）。

**E. 已有接口**
- 有：`IGameSession`（App.Contracts:15/22，`World` 直接暴露 `GameWorld`）；`IMapWorldView/IMapSettlementView/IMapOverlayView/IMapRealmView`（Core.Contracts；`MapWorldViewAdapter` 实现，WorldState:28；由 App `GameManager` 271/358 创建）；`ICultureContext/IContentLookup/ILawContext` 等子系统查询契约。
- **缺**：`IWorldReadModel` / `IWorldQuery` 等世界级只读接口（全仓无声明）；UI/Save/Editor 仍直接持有 `GameWorld`。

---

## 2. 推荐切分（目标类型与依赖）

### 2.1 目标类型

| 类型 | 层/程序集 | 职责 | 依赖 |
|---|---|---|---|
| `GameWorld`（保留） | Simulation.WorldState | MonoBehaviour **数据容器 + 只读查询** + 契约适配器 + 薄门面（`InitializeWorld/Generate*` 保留 delegate 过渡）+ 实现 `IWorldReadModel` | Core/Content/World/Simulation 各系统 |
| `IWorldReadModel`（新） | Core.Contracts | 世界级只读视图（`Tiles/Realms/Settlements/Anchors/Armies/Cultures/...` + 查询方法 + `currentDay/Year`），供 UI/Save/Editor | Core/Content |
| `WorldServices`（新） | Simulation.WorldState | 子系统引用容器（Economy/Politics/Characters/AI/Innovation/Diplomacy/Combat/…，现 Core:156-198 私有字段）；只读 getters | 各系统类型 |
| `WorldCompositionRoot`（新） | Simulation.WorldState | `InitializeSubsystems`、默认内容（goods/units/trade/races/cultures/faith）、`ApplySetupOptions/ApplyContentOverrides`、`InitializePlanSystem`、`Register*CommandHandlers`、`RegisterSimulationSchedules`、`EnsureAIControllers`；`Compose(GameWorld)` | GameWorld + WorldServices + Simulation 系统 |
| `WorldGenerationService`（新） | Simulation.WorldState | 地形/气候/水文/省/聚落/初始单位生成 + `GenerateTerrainAsync` + `RegenerateFromPipeline` + 碎裂辅助 | GameWorld(data) + World.Terrain/Generation |
| `WorldLoadInitializer`（新） | Simulation.WorldState | `PrepareLoadedWorld`、`AdoptRuntimeIdCounters`、读档后 `EnsureAIControllers` | GameWorld + WorldServices |
| `WorldFactory`（新） | Simulation.WorldState | `CreateRuntimeWorld(...)`（`new GameObject + AddComponent<GameWorld>` + 初始 map/seed/options）、`CreateEmptyWorld()`、销毁旧世界；供 App 与 Infrastructure.Save 共用 | UnityEngine + GameWorld |
| `WorldTickHost`（可选，Phase 4） | Simulation.WorldState | 迁移 `Update/GameTick/tickInterval`；过渡期 GameWorld.Update 只 delegate | GameWorld + services |
| `IWorldFactory`（可选） | Core.Contracts | 若 Infrastructure 需要解耦创建细节；最小首步不引入 | — |

**放置理由**：`GameWorld` 是 MonoBehaviour（场景对象），其创建必须发生在一个能 `AddComponent` 的地方；App 与 Infrastructure.Save 都要创建/加载世界。把 `WorldFactory`/`WorldCompositionRoot` 放 **Simulation host 层**，可让 App（场景/流程）与 Infrastructure.Save（读档）共同调用且**不新增 Infrastructure→App 或 Infrastructure→UI 反向依赖**；App 仅保留"场景绑定（MapRenderer/UI/编辑控制器）"的粘合。

### 2.2 目标依赖图

```
Core ── Content ── World
                    │
                    └── Simulation
                          ├── GameWorld (数据+查询+IWorldReadModel)
                          ├── WorldServices (子系统引用容器)
                          ├── WorldCompositionRoot ──► GameWorld/WorldServices/各 Simulation 系统
                          ├── WorldGenerationService ──► GameWorld + World.Terrain/Generation
                          ├── WorldLoadInitializer ──► GameWorld/WorldServices
                          ├── WorldFactory ──► GameWorld（Unity）
                          └── MapWorldViewAdapter ──► GameWorld (IMap*View)
                                 │
              ┌──────────────────┼───────────────────┐
              ▼                  ▼                   ▼
            App                UI                Infrastructure.Save
   (GameManager/SceneFlow)  (IWorldReadModel/     (WorldFactory/
    scene 绑定 + 流程)       IMap*View)            WorldLoadInitializer/
                                                  IWorldReadModel)
```

方向不变：`App/UI/Infrastructure → Simulation`；`Simulation` 不引 App/UI；Rendering 已只依赖 Core/Content（B7-part3）。

---

## 3. 分步顺序、门禁与回滚

### Step 1：只读接口（无行为改动，最小风险）

- 新增 `Core.Contracts/IWorldReadModel.cs`（数据/查询只读成员，按 1.1 数据/查询清单裁剪）；`GameWorld : IWorldReadModel` 实现。
- UI/Save 的 `GameWorld` 具体参数先保留（只增加接口实现），把 `MapWorldViewAdapter`/UI 读取路径逐步换成接口（可选）。
- 文件数约 3-5（新接口 + GameWorld 声明 + 可选 UI/Save 适配）。
- 门禁：编译 + `SystemContractsTests`、`MapWorldViewAdapterTests`、UI PlayMode。
- 回滚：删除 `IWorldReadModel.cs`、撤销 `GameWorld` 接口声明。

### Step 2：生成服务（WorldGenerationService）

- 把 `GameWorld.Generation.cs` 的生成方法体移入 `WorldGenerationService`（新文件，~500 行），`GameWorld.Generation.cs` 保留 `GenerateTerrain/GenerateTerrainAsync/GenerateAll/GenerateProvinces/GenerateSettlements/GenerateInitialUnits/RegenerateFromPipeline` **delegate 包装**（旧调用点/Tests 零改动）。
- `GenerationPipeline`/碎裂/气候辅助随服务迁移；`InitializeDefaultRealms`、`EnsureSeaLandGeneratorSeed` 移入服务或保留薄包装。
- 文件数约 2-3（新服务 + GameWorld.Generation 瘦身）。
- 门禁：编译 + `WorldGenerationSmokeTests`/`FragmentationClimateConsistencyTests`/`WorldSeed*` + PlayMode 地图可见性。
- 回滚：把方法体移回 Generation.cs，删除服务文件。

### Step 3：组合根 + WorldServices

- 把 Core:535-595（InitializeSubsystems/ReinitializeSubsystems/EnsureAIControllers/默认内容/契约适配器/plan/command handlers/schedules 注册）移入 `WorldCompositionRoot`；子系统私有字段（Core:156-198）移入 `WorldServices`，GameWorld 的 getter（Core:234-275）delegate（保持公开 API）。
- `GameWorld.InitializeWorld` 保留骨架（清静态注册表、建 Pipeline、config、tiles、`CompositionRoot.Compose(this)`），`ReinitializeSubsystems` 调 `CompositionRoot.Reinitialize(this)`；`PrepareLoadedWorld` 调 `WorldLoadInitializer`（本步可先内联）。
- 注意 `SystemContractsTests` 通过反射读 `_faithSystems`（68-71）：迁移前保留字段或同步更新测试（Tests 不在本卡写范围时，字段保留为 `WorldServices` 内可见字段）。
- 文件数约 3-5（新 CompositionRoot + WorldServices + GameWorld.Core 瘦身）。
- 门禁：编译 + `SystemContractsTests`/Law/Faith/Culture 契约测试 + 存档 `*PersistenceTests` + PlayMode（聚落图标/政府面板）。
- 回滚：恢复 Core 字段与方法体，删除两个新文件。

### Step 4：App / Save 切换（WorldFactory + WorldLoadInitializer + 去门面）

- App `GameManager` 创建世界改用 `WorldFactory.CreateRuntimeWorld(...)`/`CreateEmptyWorld()`（137-141/170-174/268-280 的创建与绑定拆成"factory 创建 + App 绑定 View"）；仍保留 `FindAnyObjectByType` 复用逻辑。
- `SaveSystem` 读档改用 `WorldFactory.CreateEmptyWorld()` + `WorldLoadInitializer`（或 `IWorldFactory` 注入），删除 273-274 的直接 `AddComponent`；`ReinitializeSubsystems/PrepareLoadedWorld` 调用切到服务。
- 删除 `GameWorld` 的生命周期 wrapper（保留 deprecated 转发一个版本），Tests 逐步改用 factory/initializer（5 个 `new GameWorld` 测试优先保留 `GameWorld` 数据 API，不强制改）。
- 文件数约 6-10（App 1-2、Save 1-2、GameWorld 1、Tests 若干）。
- 门禁：编译 + **全量 EditMode/PlayMode** + 存档 smoke（Save/Load 往返、聚落图标、地图可见性）。
- 回滚：恢复 App/Save 调用与 GameWorld wrapper。

> 每步独立可编译、可回滚；**建议 1→2→3→4 顺序**，禁止在 Step 2/3 同时改 App/Save。

---

## 4. 风险与注意

1. **partial 序列化**：`GameWorld` 是 MonoBehaviour，public 字段（Core:59-226）会被场景/序列化捕获；迁移只允许移 **private 子系统引用**（156-198）和逻辑方法，**不要移动/删除 public 数据字段**，否则 Main.unity 序列化数据丢失。`GameWorld.cs` 声明文件与 `.meta`（GUID `dcde6bad…`，场景 1 处引用）保持不动。
2. **测试 `new GameWorld()` 的 fake-null**：5 个测试直接 `new GameWorld()`（非 GameObject 语义），若引入 factory 后改测试为 `AddComponent` 会改变生命周期；本方案保留 GameWorld 的数据/查询 API，factory 仅用于运行时创建，测试可继续直接 new（只测数据/查询/生成委托时）。
3. **SaveSystem 反向依赖**：`SaveSystem.cs:273-274` 直接创建 GameWorld，若把 factory 放 App 会引入 `Infrastructure→App`；因此 factory 必须在 Simulation host 层。Step 4 切换时保持 `Infrastructure→Simulation` 方向。
4. **过期注释**：`SaveSystem.cs:415` 声称 `AddComponent` 时 Awake 会 `InitializeWorld`，实际 `Awake()`（Core:939）为空；收口时以实际调用链为准并修正注释（避免误判 load 行为）。
5. **静态注册表清理顺序**：`InitializeWorld` 先清 `RelationRegistry/ContractRegistry/MapUnitRegistry/CharacterKnowledgeRegistry/DynamicGoodsRegistry`（475-480），测试依赖该顺序（跨世界泄漏审计 A-P1-6）；CompositionRoot 必须原样保留顺序。
6. **默认内容种子**（goods/units/trade/races/cultures/faith/plans/AI）是 Save 读档的前置（`PrepareLoadedWorld` 依赖 UnitDefs/FaithSystem）；迁移时保持 `InitializeWorld → 默认内容 → ReinitializeSubsystems/PrepareLoadedWorld` 的时序。
7. **Update tick**：`GameWorld.Update`（943-951）驱动 `GameTick`，PlayMode 依赖；Step 4 之前不要移除 MonoBehaviour tick，或新增 `WorldTickHost` 并保持 GameWorld.Update delegate。
8. **UI 具体类型**：UI 4 处直接持有 GameWorld（1.3B），`IWorldReadModel` 只是新增能力；逐步切 UI 需另卡（本方案不强制），避免与 UI 程序集门禁冲突。
9. **反射测试**：`SystemContractsTests` 反射 `_faithSystems` 字段（68-71）；Step 3 迁字段时要么保留字段（推荐），要么同步更新测试。
10. **场景对象唯一性**：`GameManager` 复用场景已有 GameWorld 的逻辑（137/170）与 View 注入（271/358）需在 Step 4 保留，否则 PlayMode 黑屏/空世界（B3 已修过一次）。

---

## 5. 每步“最小首步 3 项”清单

**Step 1（只读接口）**
1. 新增 `Core.Contracts/IWorldReadModel.cs`（先只放 `tiles/realms/settlements/anchors/armies/cultures` + `currentDay/currentYear` + `TileExists/GetTile/GetLandTileCount/TryGetSiteAtTile` 等只读成员）。
2. `GameWorld` 声明实现 `IWorldReadModel`（方法体复用 Queries.cs，无行为变化）。
3. 编译 + `SystemContractsTests`/`MapWorldViewAdapterTests` 通过；UI 暂不改。

**Step 2（生成服务）**
1. 新建 `WorldGenerationService`，搬 `GenerateTerrain/GenerateTerrainBase/GenerateTerrainWithConfig/GenerateAll/GenerateProvinces/GenerateSettlements/GenerateInitialUnits/GenerateTerrainAsync/RegenerateFromPipeline` 方法体。
2. `GameWorld.Generation.cs` 改为 delegate 包装（签名不变，App/Tests 零改动）。
3. 跑生成/碎裂/气候 + PlayMode 地图可见性测试。

**Step 3（组合根）**
1. 新建 `WorldServices`（搬 Core:156-198 子系统引用，保持 GameWorld getter delegate）。
2. 新建 `WorldCompositionRoot`（搬 `InitializeSubsystems` + 默认内容 + plan/command/schedule 注册 + `EnsureAIControllers` + `ApplySetupOptions/ApplyContentOverrides/InitializeFaithSystems`）。
3. `GameWorld.InitializeWorld/ReinitializeSubsystems` 改为骨架 + delegate；`SystemContractsTests` 反射字段保持可用。

**Step 4（App/Save 切换）**
1. 新建 `WorldFactory`（`CreateRuntimeWorld/CreateEmptyWorld`），App GameManager 创建路径改用 factory（保留复用/绑定逻辑）。
2. `SaveSystem` 读档改用 factory + `WorldLoadInitializer`（或注入 `IWorldFactory`），删除直接 `AddComponent<GameWorld>`。
3. GameWorld 生命周期 wrapper 标记 `[Obsolete]`（或保留一版）；全量 EditMode/PlayMode + 存读档 smoke。

---

## 6. 统计口径

- partial 行数/成员行号来自 `Scripts/Simulation/MyriadCreation.Simulation.WorldState/GameWorld*.cs` 静态扫描。
- 调用方来自全仓 `grep`（`new GameWorld`/`AddComponent<GameWorld>`/`.InitializeWorld(`/`.GenerateTerrain(`/`.GenerateTerrainAsync(`/`.PrepareLoadedWorld(`/`.ReinitializeSubsystems(`/`.BindWorld(` 等），按 asmdef 归属统计。
- 场景引用来自 `Scenes/Main.unity` 与 `GameWorld.cs.meta` GUID 对比。
- 本卡只读产出，未移动/删除/修改任何 `.cs`/`.asmdef`/场景。
