# L 域审计：状态模型（State / Definitions / Events）— 2026-11-09

> 只读审计，未修改任何 `.cs`。锚点格式 `路径:行号`，路径相对 `MyriadCreation/`。只写事实与缺口，不写方案。

## 0. 范围与机械统计

- 全量枚举 85 文件 / 8738 行：`Assets/Scripts/Simulation/MyriadCreation.Simulation.State`（71）、`.Definitions`（12）、`.Events`（2）。
- 机械统计（`Get-ChildItem -Filter *.cs | Select-String -Pattern '^\s*public\s+...'`）：`public` 字段 **821**、`[NonSerialized]` **39**。逐文件计数见 §1「字段」列。
- **路径更正**：`TileData` / `SettlementData` / `AnchorData` / `SimulationEventBus` / `Plan` / `PlanSystem` 均不在上述三目录，实际位置为 `Assets/Scripts/Core/MyriadCreation.Core.Data/TileData.cs`、`Assets/Scripts/World/MyriadCreation.World.Settlement/SettlementData.cs`、`Assets/Scripts/World/MyriadCreation.World.Anchor/AnchorData.cs`、`Assets/Scripts/Core/MyriadCreation.Core.Events/SimulationEventBus.cs`、`Assets/Scripts/Simulation/MyriadCreation.Simulation.Planning/Plan.cs`、`Assets/Scripts/Simulation/MyriadCreation.Simulation.Systems/PlanSystem.cs`。它们全部跨域被读写，故纳入本次只读核对。
- 精读清单：上述 6 个 + `RealmData.cs`、`RealmDTO.cs`、`CharacterData.cs`、`Army.cs`、`Chronicle.cs`，加存档侧 `SaveData.cs` / `SaveSystem.cs` / `SettlementPersistence.cs` / `HabitationSiteSaveData.cs` / `ArmySaveData.cs` / `WorldEntitySaveData.cs`。

## 1. 容器清单（按域分组）

| 域 | 容器（文件:行，public 字段数） | 持有者 | 主要读者 |
|---|---|---|---|
| 政权 | `RealmData` `Simulation.State/RealmData.cs:15`（37，含 7 个 NonSerialized） | `GameWorld.realms` `WorldState/GameWorld.Core.cs:92` | `RealmSituationBuilder.cs:151`、`RealmQueryAdapter.cs:114`、`FaithContracts.cs:42`、`CultureContextAdapter.cs:47`、`AIDecisionSystem.cs:174`、`DiplomacyManager.cs:63` |
| 政权 | `RealmDTO` `State/RealmDTO.cs:12`（23）｜`RealmSituation.cs:31`（11）｜`RealmSociety.cs`（6） | 存档 `SaveData.realms` `Save.cs:39`；运行时由 builder 现算 | `SaveSystem.cs:384/428`、UI 查询适配器 |
| 政权 | `Faction.cs`（11）｜`IdeologyMovement.cs`（15）｜`SocialDifferentiation.cs`（0 字段/260 行纯计算）｜`RegimeChangeDynamics.cs`（0/314）｜`StructuralTension.cs`（4）｜`Subordination.cs`（11） | `GameWorld` 各系统私有字典 | `PoliticsSimulationSystem`、`RegimeChange*` |
| 地块 | `TileData` `Core.Data/TileData.cs:8`（struct，53） | `GameWorld.tiles` `GameWorld.Core.cs:77`（值数组，按引用共享） | 全库 99 处 `.ownerRealmId`、23 处 `.fertility`、`PathfindingSystem.cs:288`、`MapRenderer` |
| 聚落 | `SettlementData` `World.Settlement/SettlementData.cs:12`（47，5 个 NonSerialized）｜`AnchorData` `World.Anchor/AnchorData.cs:7`（13，3 个 NonSerialized）｜`ActiveBuilding.cs`（10） | `GameWorld.settlements` `GameWorld.Core.cs:106`、`GameWorld.anchors` `:103`、`GameWorld.provinces` `:100` | `SettlementTypologySystem.cs:299`、`SettlementControlSystem.cs:93`、`SettlementPersistence.cs:85`、`CityDistrictSystem.cs:11` |
| 角色 | `CharacterData` `State/CharacterData.cs:11`（84，2 个 NonSerialized）｜`FamilyNode.cs`（15）｜`CharacterBond.cs`（10）｜`MentalHealthSystem.cs`（0/119）｜`AchievementRecord.cs`（21） | `CharacterManager`（私有字典） | `CharacterSaveData.cs:28`、AI/继位/UI 查询 |
| 军事 | `Army` `State/Army.cs:14`（19 + 7 个 NonSerialized）｜`Navy.cs`（15）｜`WarState.cs`（10）｜`WarGoal.cs`（9）｜`Diplomacy/DiplomaticRelation.cs`（25）｜`Treaty.cs`（13）｜`Alliance.cs`（12） | `GameWorld.armies` `:109`、`GameWorld.GetWars()` | `CombatManager.cs`、`DiplomacyManager`、`ArmyPersistence.cs:28` |
| 世界 | `Chronicle` `State/Chronicle.cs:7`（仅 1 个 public 属性 + 私有 List，容量 500 行截断 `:37`）｜`InnovationTree.cs`（0/909，状态在私有字典）｜`RaceData.cs`（40） | `GameWorld` 单例字段 | `GameSaveSystem.cs:316`（旧路径）、UI 编年史面板 |

### 1.1 无读者（写后无人消费，逐类点名）

计数口径：`Select-String -Pattern '\.字段\b'` 排除声明文件后的命中数；"全库引用=1"指该标识符在全库只出现一次（即声明本身）。

| 类 | 字段（个数） | 证据 |
|---|---|---|
| `RealmData` | `senateSeats`、`successionLawFromCulture`（2） | 前者全库引用=1（`RealmData.cs:105`）；后者引用=2（`:84` 声明 + `:111` 自读），**0 外部写** → 恒 true，"文化决定继承法"开关不可切换 |
| `SettlementData` | `settlementEvolution`、`settlementStability`、`foundingTick`（3） | `.字段` 读点=0；仅 `SettlementEvolutionSystem.cs:135-137` 在对象初始化器里写入 |
| `SettlementData` | `parentAnchorId`、`ticksSinceLastTransition`、`evolutionTarget`、`remnantFaithId`、`secondaryFunctions`（5） | 各全库引用=1（`:106/:102/:100/:126/:78`） |
| `CharacterData` | `conquests`/`warsWon`/`diplomaticVictories`/`cultureWorks`/`religiousDeeds`/`expeditions`/`vigilanceActs`（7） | 各引用=1（`CharacterData.cs:71-83`）；同名量真源是 `AchievementRecord`（`:61`），`EpithetSystem.cs:43` 读的是 `rec.*` |
| `TileData` | `terrainShade`、`isClimateDirty`、`isEconomyDirty`（3） | 各引用=1（`TileData.cs:40/108/110`） |

合计点名 **20 个无读者字段**。

### 1.2 无写者（读得到但无人维护）

| 类 | 字段 | 证据 |
|---|---|---|
| `AnchorData` | `influenceRadius`、`bottleneckTileIndex` | 写点=0（`AnchorData.cs:27/34` 默认 1/-1）；`SettlementControlSystem.cs:73` 只算局部变量不回写 |
| `AnchorData` | `isControlStable`、`controllerAnchorId` | 唯一写者 `SettlementControlSystem.cs:156/202`（单点维护，无集中校验） |
| `SettlementData` | `isCapital` | 6 处命中全部是 DTO/Adapter 搬运（`SettlementPersistence.cs:106/276`），**全库无 `isCapital = true`** |
| `RealmData` | `movementControl`、`nomadicSubtype` | 写点=0；`movementControl` 恒 `Loose`（`:51`），3 个读点读到的是默认值 |

## 2. `public` 可变字段的跨域写入（代表性命中 ≤10）

> **程序集前提纠正**：State / Definitions / Events / Settlement **同属一个程序集** `Simulation/sim.asmdef`（全仓仅 11 个 asmdef：`core`、`world`、`sim`、`app`、`infrastructure`、`content`、`rendering` + 3 个子 asmdef + `App.Contracts`）。因此下文"跨域"指**跨目录/跨系统归属**，真正的程序集边界只有 `core`（`TileData`）与 `world`（`SettlementData`/`AnchorData`）→ `sim` 这一层；下表 §2.1 行"跨 8 个程序集"应读作"跨 8 个域"。

| # | 字段 | 写入点（谁写） | 备注 |
|---|---|---|---|
| 1 | `RealmData.treasury` | `Systems/BuildingSystem.cs:162`、`Systems/DiplomacyManager.cs:63`（另 `:1006-1007` 转账）、`Units/TreasuryLedger.cs:311/326/334`、`Systems/NomadicRealmSystem.cs:80`、`Systems/PoliticsSimulationSystem.cs:87`、`AI/AIDecisionSystem.cs:174`、`LawHost/LawRealmHost.cs:105`、`CultureHost/CultureStageEvolutionHost.cs:88`（直赋 2000） | 全库 `.treasury\s*(\+=|-=|=)` = **14** 命中，跨 8 个程序集 |
| 2 | `RealmData.stability` | `DiplomacyManager.cs:430/594/611`、`LawHost/LawSystem.cs:110`、`LawHost/LawReform.cs:28`、`LawHost/LawEffectRuntime.cs:97`、`AIDecisionSystem.cs:175`、`State/RegimeChangeDynamics.cs:185/193`、`CultureStageEvolutionHost.cs:82/91` | `.stability` 全库 64 命中，含 `LawEffects` 结构体同名成员 |
| 3 | `RealmData.centralization` | `LawHost/LawSystem.cs:40`、`CultureStageEvolutionHost.cs:85/94`、`Laws/LawEffects.cs` 内 26 处累加 | 唯一夹取点 `LawSystem.cs:40` |
| 4 | `RealmData.monarchId/consulId/heirId` | `Systems/SuccessionSystem.cs:41/53/57/59/156/160/162`、`Save/SaveSystem.cs:590`（读档按角色表回填）、`Save/WorldEntitySaveData.cs:426-427` | 三个字段三处独立写 |
| 5 | `TileData.ownerRealmId` | `Systems/PoliticalManager.cs:46`、`Systems/LandAbandonmentSystem.cs:45/169`、`WorldState/GameWorld.Simulation.cs:427/967`、`Systems/SettlementDestructionSystem.cs:271`、`NomadicRealmSystem.cs:108`、`CultureStageEvolutionHost.cs:66` | `.ownerRealmId` 全库 99 命中 |
| 6 | `TileData.stability` | `State/Army.cs:145`（驻军 -3）、`Systems/DisasterSystem.cs:290/294`、`Systems/EconomyManager.cs:453`、`Systems/StampedeSystem.cs:160`、`PoliticalManager.cs:31/79`、`Politics/ConsolidateRealmCommandHandler.cs:33` | 量纲 0–100，夹取方式各不相同 |
| 7 | `TileData.fertility` | `Systems/BuildingSystem.cs:271`（clamp 0–1）、`DisasterSystem.cs:295/313/326/329`、`Actors/SwarmAI.cs:62`（`-2f`）、`World.Terrain/PlanetTerrainGenerator.cs:230` | 0–1 量纲下 `SwarmAI` 的 `-2f` 等于一次性归零 |
| 8 | `AnchorData.controlProgress` / `controllerAnchorId` | 唯一写者 `Systems/SettlementControlSystem.cs:95/147/152/202-203`；读侧 `SettlementData.cs:200-208`、`SettlementControlSystem.cs:93` | 控制权只在单系统内闭环，但字段是 public |
| 9 | `SettlementData.settlementLevel` | 属性写者 `World.Settlement/SettlementTypologySystem.cs:38/224/299`；读者 `CityDistrictSystem.cs:11/70/129`、`SettlementControlSystem.cs:93` | 属性实为私有 `_legacySettlementLevel` 的包装（`SettlementData.cs:290-304`） |
| 10 | `TileData.populationBlocks`（人口） | `Actors/MapActorManager.cs:329/344`、`Systems/MissionarySystem.cs:67`、`Systems/SandboxSpawnSystem.cs:69`、`State/SocialDifferentiation.cs:193`、`GameWorld.Generation.cs:105`、`World.Editing/MoveSelectionCommand.cs:77`（置 null） | 6 处不同系统直接增删 List |

## 3. 不变量缺失（金库 / 稳定 / 人口 / 控制权）

- **无集中入口**：`RealmData` 全 37 个字段 public（`RealmData.cs:17-105`），全类只有 3 个方法（`AdjustClassRelation:186` 带 clamp、`CalculateRebellionRisk:191`、`CalculateManpowerLimit:166`），**没有任何 setter/校验入口**。
- **金库**：`BuildingSystem.cs:162` 直接 `-=`，不做余额检查；余额检查只存在于 `TreasuryLedger.cs:306` 这一条路径。`TreasuryLedger.cs:334` 又把 `treasury` 用 `treasuryVault` 覆盖回去 → `treasury` 与国库货物 vault 是两份真源。
- **稳定度**：夹取三套并存——`Mathf.Clamp(...,0,100)`（`LawEffectRuntime.cs:97`）、`Mathf.Max(0,...)`（`DiplomacyManager.cs:430`）、直接赋常量（`CultureStageEvolutionHost.cs:82` 置 55 / `:91` 置 60）。`centralization` 0–1 语义仅 `LawSystem.cs:40` 一处夹取。
- **人口**：`populationBlocks` 是 public `List<PopulationBlock>`，6 个系统各自增删（§2.10），无总量守恒校验；`MoveSelectionCommand.cs:77` 会把它置 `null`，而 15+ 处读者只做 `== null` 早退。
- **控制权**：`AnchorData.controllerAnchorId`（标量）与 `controlProgress`（`Dictionary<int,float>`，`AnchorData.cs:25`）双源，无一致性校验；`SettlementControlSystem.cs:152-158` 回滚时直接改字段并从字典 `Remove`。
- `TileData` 的 `isTerrainDirty/isClimateDirty/isEconomyDirty`（`TileData.cs:106-110`）三面脏标记无统一清理入口。

## 4. Definitions ↔ State 边界（"内容改不动代码"的成因）

| 现象 | 证据 | 后果 |
|---|---|---|
| 内容硬编码在 Definitions 的 C# 里 | `Definitions/PersonalityTraitDatabase.cs:9`（`All = new PersonalityTrait[108]`）；`Definitions/ImpairmentRegistry.cs:24`（逐个 `new *ImpairmentDef()`） | `StreamingAssets/Base` 有 31 个 JSON 内容目录，但**没有 Trait / Impairment 目录** → 108 条特质与 5 类伤残只能改代码，Mod 无法覆盖 |
| 内容契约放在 State、实现在 Definitions | `State/IImpairmentDef.cs`、`State/ImpairmentStage.cs`（含 stageId/名称/6 项数值修正），实现类在 `Definitions/CognitionImpairmentDef.cs:5` 等 | 依赖方向倒置：State（更底层）被内容层反向绑定，且内容 payload 结构改动要改 State |
| 平衡表/常量放在 State | `State/InnovationProgressConfig.cs:5-19`（13 个 const）；`State/ClassNeedWeightTable.cs:10-18`（5×8 权重写死为字段初值）；`State/WarRules.cs:8-`（战分/停战/干预参数）；`State/MentalHealthSystem.cs:19`（5 条内建 `MentalDisorderDef`） | 调平衡必须改 C#；`Base/MentalHealth/*.json` 已存在 → **同一内容双源**（`ContentResolvers.MentalDisorders` 优先，内建兜底） |
| 规则/行为长在数据类上 | `State/TradeCenter.cs`（`GetGoodsPrice`/`AddGoods`/`RemoveGoods`）；`RealmData.cs:166-202`（`CalculateManpowerLimit`/`CalculateRebellionRisk`）；`DiplomaticRelation.cs:102`（`GetTreatyObligations`） | 存档对象同时是算法宿主，DTO 与运行时不可分离 |
| 内容键表放在 State | `State/DiscoveryGroups.cs:5-9`（3 个字符串常量）；`State/OfficeTitleCatalog.cs:9-12`（`"office_" + office`）；`State/CharacterAISelector.cs:7-30`（Role→Category 硬映射） | 新增职业/官职/发现渠道必须改代码 |
| State 反向依赖系统层 | `RealmData.cs:6-8` `using Simulation.Laws/ContentHost/Systems`；字段含 `LawComposition`/`RealmLawState`/`RealmDecreeState`/`TaxSystem`/`CurrencySystem`（`:59-80`） | State 无法脱离 Laws（`MyriadCreation.Laws.asmdef` 独立程序集）编译，改法律定义连带 State 重编 |

## 5. 事件

- **两条并行通道**：① `SimulationEventBus`（`Core.Events/SimulationEventBus.cs:6`，`Dictionary<Type,Delegate>` + 同步 `Publish`，`:48`）；② `GameEvent`/`IGameEventListener`（`Simulation.Events/GameEvent.cs:5`、`IGameEventListener.cs:3`，由 `GameWorld.Core.cs:217/364-366` 轮询）。
- **事件类型 9 个**：`WarDeclaredEvent`、`WarEndedEvent`、`SeasonChangedEvent`、`PracticeRecordedEvent`、`FaithConflictTriggeredEvent`、`EconomyDayTickedEvent`（`Core.Events/`），`SettlementTileResettledEvent`、`SettlementTileAbandonedEvent`、`SettlementDestroyedEvent`（`Simulation.Settlement/`）。
- **Publish 11 处**：`GameWorld.Core.cs:300`、`GameWorld.Simulation.cs:361/876/1032/1049`、`ResettleTileCommandHandler.cs:25`、`DestroySettlementCommandHandler.cs:25`、`AbandonSettlementTileCommandHandler.cs:26`、`EconomySchedule.cs:24`、`BuildingSystem.cs:226/241`、`CombatManager.cs:163`。
- **订阅者仅 3 类**：`ResearchPlanSystem.cs:48`（PracticeRecorded）、`ReligionSimulationEventHandler.cs:19`（FaithConflict）、`SimulationStatisticsRecorder.cs:45-48`（4 个纯计数）。
- **发了没人听**：`SettlementTileResettledEvent` / `SettlementTileAbandonedEvent` / `SettlementDestroyedEvent` —— 全库 `Subscribe` 命中 0（grep `.Subscribe` 共 8 命中，无一指向这三型）。
- **订阅者本身空转**：`SimulationStatisticsRecorder` 无任何实例化（`new SimulationStatisticsRecorder` grep = 0），故 `WarDeclared/WarEnded/EconomyDayTicked/SeasonChanged` 事实无消费者；`SimulationEventDirtyBridge.Create` 亦无调用者（grep = 0）。
- **第二通道空转**：`GameWorld.RegisterEventListener`（`GameWorld.Core.cs:468`）全库无调用者，`_eventListeners` 恒空，`:364-366` 的派发循环永远不执行。

## 6. 存档映射（三态；活档 = `SaveSystem` v2，`GameSaveSystem` 已 `[Obsolete]` 且未接循环 `GameSaveSystem.cs:39-40`）

**A. 入档**
- `RealmData`：treasury/prestige/stability/unrest/centralization/laws/lawState/decreeState/taxSystem/currencySystem/classRelations/coreTiles/claimedTiles/suzerainId/vassalIds/adminDivisions/摄政四件套（`RealmDTO.cs:73-128`）；monarchId/consulId/stateReligionId/statePatronSaintId/officeHolders 走 `RealmOfficeSaveData`（`WorldEntitySaveData.cs:397-434`）。
- `TileData` 整体直接序列化（`SaveData.cs:36` `public TileData[] tiles`；`PopulationBlock` 无字典字段，可被 JsonUtility 深拷）。
- `SettlementData` 主量 + blocks/market/districts（`SettlementPersistence.cs:85-133`）；`Army` 稳定量（`ArmySaveData.cs:41-78`，含 `unitCounts` 拍平）。

**B. 不入档但可重算**
- 军队战斗瞬时量 `originalManpower/totalCasualtyRate/pursuitBonus`（`ArmySaveData.cs:13-14` 明示读档重算）。
- `SettlementData.fortificationLevel`（`SettlementPersistence.cs:316` 由 blocks 重算）。
- `AnchorData.x/y/provinceId/tileIndex`（由 site 记录重建）。
- 地图单位里的军队视图条目（`SaveSystem.cs:611-614` 读档后按 army 重登记）。

**C. 不入档且不可重算（静默丢失）**
| 字段 | 证据 |
|---|---|
| `RealmData.primaryCultureId` | `RealmData.cs:82`；`RealmDTO`（`:14-62`）与 `RealmOfficeSaveData` 均无此字段，`SaveSystem.cs:590` 只回填 monarchId → 读档后恒 -1，继承法/头衔/文化查询断链（`RealmData.cs:111`、`SocialQueries` 均依赖） |
| `RealmData.heirId` | `SuccessionSystem.cs:41/53/59`；存档侧 `heirId` grep = 0 |
| `RealmData.realmForm` / `nomadicSubtype` / `senateSeats` / `primaryTitleKey` / `movementControl` / `militaryAccessGranted` / `nomadicRange` | `RealmData.cs:22/44-57/105`；存档侧 `realmForm|movementControl|senateSeats` grep 均 = 0（`nomadicRange` 是 NonSerialized） |
| `AnchorData.controllerAnchorId` / `isControlStable` / `influenceRadius` / `bottleneckTileIndex` / `controlProgress` | `AnchorData.cs:19-34`；`SettlementPersistence.cs:251-258` 只重建 4 个字段，其余取默认值；存档侧 grep `controllerAnchorId|bottleneckTileIndex|influenceRadius` = 0 |
| `SettlementData.settlementLevel`（运行值） | `SettlementData.cs:290-304` 读私有 `_legacySettlementLevel`；还原路径只写 `sedentarySettlementData.developmentStage`（`SettlementPersistence.cs:287` → `HabitationSiteSaveData.cs:154`），不回写属性 → 读档后该属性返回 `LevelI`（枚举首值 0，`Core.Enums/SettlementLevel.cs:5`），与已还原的 `districts` 等字段不一致（未实测运行期后果，见 §7.8） |
| `SettlementData.upgradePath` / `cityFocus` / `portTier` / `bottleneckType` / `evolutionStage` / `foundingTick` / `parentAnchorId` / `settlementEvolution` / `settlementStability` / `ticksSinceLastTransition` / `ruinLevel` / `ruinedDay` / `recoveryProgress` / `remnantCultureId` / `remnantFaithId` | `SettlementData.cs:76-126`；Save 目录 grep 这些名字全部 = 0（`upgradePath` 影响 `SettlementEvolutionTree.cs:102` 的等级上限，`ruinLevel` 影响 `IsRuined`） |
| `Chronicle` 全部条目 | `Chronicle.cs:9` 私有 List；活档 `SaveData.cs` 无 chronicle 字段，仅旧 `GameSaveData.cs:58` 有 |
| `PlanSystem` 全部在飞计划 | `PlanSystem.cs:11` 私有 `Dictionary<int,Plan>`；`SaveData.cs` 无 plans 字段；`GameWorld.Core.cs:282-287` 读档重建空系统 |
| `SettlementData.garrisonQuality/garrisonQuantity` | `SettlementData.cs:108-112` 标 `[NonSerialized]`，存档侧无对应字段 |

**D. 按类三态总表**（71 个文件 / 74 个类型，`InnovationTreeSaveData.cs` 单文件含 4 个类型）

| 态 | 类（个数） |
|---|---|
| **入档**（29） | `RealmData`(部分)、`RealmDTO`、`TaxSystemDTO`、`AdminDivisionSaveData`、`CharacterData`、`CharacterRelation`、`CharacterBond`、`FamilyNode`、`AchievementRecord`、`DnaData`、`BodyPartData`、`SubStructureData`、`DiagnosedCondition`、`ActiveImpairment`、`ActiveCharacterDisease`、`InnovationTree`、`InnovationProgress`、`InnovationTreeSaveData`、`RealmInnovationSaveData`、`InnovationProgressSaveData`、`StringFloatEntry`、`Army`、`Navy`、`WarState`、`TradeCenter`、`TradeCenterDTO`、`TradeRoute`、`RaceData`、`RaceDTO` |
| **不入档且不可重算**（14） | `Chronicle`、`ChronicleEntry`、`DiplomaticRelation`、`Alliance`、`Treaty`、`TreatyClause`、`CasusBelli`、`WarGoal`、`Subordination`、`NegotiationState`、`NegotiationClause`、`ActiveBuilding`、`ActiveDisaster`、`ActiveDisease` |
| **靠重算（有损）**（14） | `Faction`、`RegimeChangeState`、`StructuralTension`、`RegimeChangeDynamics`、`RealmSituation`、`RealmSociety`、`ClassProfile`、`ClassNeedReport`、`NeedDimensionScore`、`IdeologyMovement`、`SchoolOfThought`、`Caravan`、`BattleResult`、`SuccessionResult` |
| **无状态/静态**（17） | `PopulationIdentity`、`SocialDifferentiation`、`ClassNeedWeightTable`、`InnovationProgressConfig`、`DiscoveryGroups`、`OfficeTitleCatalog`、`CharacterAISelector`、`MentalHealthSystem`、`IImpairmentDef`、`ImpairmentStage`、`PersonalityTrait`、`CharacterAICategory`、`BodyPartState`、`DnaExpression`、`LocusPair`、`WarRules`、`InnovationDiscovery`（后者全库 1 引用，**空转**） |

- 旁路漏项：`Systems/PlanSystem.cs:11` 的 `_plans` 虽不在 State，但它是 `RealmData`/`ActiveBuilding` 的上游状态，无 DTO（`SaveData.cs:22-77` 无 plans 段）→ 与"不入档且不可重算"同级。
- `DnaExpression`/`LocusPair` 之所以可归"无状态"，依据是 `CharacterSaveData.cs:22` 明示"dnaExpression/appearance 为派生缓存，可由 genome 重算"。

## 7. 缺口清单（按影响排序）

| # | 缺口 | 影响 | 证据 | 严重度 |
|---|---|---|---|---|
| 1 | `settlementLevel` 不入档且读档不重建 | 读档后所有聚落回落 `LevelI`：城防/影响力预算/区划解锁/升级上限全错档 | §6.C 第 5 行；读点 40+/10 域（`BarrierSystem.cs:230`、`SettlementControlSystem.cs:170`、`CityDistrictSystem.cs:129`、`SettlementTypologySystem.cs:224`、`SettlementEvolutionTree.cs:102`） | P0 |
| 2 | 外交全表不入档 | 关系值归 0/50，联盟/条约/停战/CB/附庸全灭 | `DiplomacyManager.cs:15/17`；Save 基建 grep=0 | P0 |
| 3 | 编年史不入档 | 长历史读档后年表空白 | `Chronicle.cs:9`；`SaveData.cs` 无 chronicle 字段 | P0 |
| 4 | 政体/国教/主流文化/继承人指针不入档 | 政体判定、信仰加成、继承链读档回默认；`primaryCultureId=-1` 使 `RealmData.cs:111` 的文化继承法分支永久失效 | `RealmDTO.cs:14-62`；写点 `CultureStageEvolutionHost.cs:44/48`、`FaithContracts.cs:294`、`SuccessionSystem.cs:59` | P0 |
| 5 | `PlanSystem` 无 DTO | 研究/建设计划读档中断，`ActiveBuilding.constructionPlanId` 悬空 | `PlanSystem.cs:11`；`SaveData.cs:22-77` | P1 |
| 6 | 两套人口零同步 | UI/军队读 `settlement.population`（45 读点/23 文件），模拟只改 `tile.populationBlocks`（9 文件写）→ 显示与真实长期背离 | 聚落人口唯一写点 `StampedeSystem.cs:157`；全库无同步赋值 | P1 |
| 7 | 金库/稳定/控制权无集中闸门 | 15 个 treasury 写点（7 域）、64 个 stability 写点（20 文件）、4 个"归属"字段 4 域各写 | §3；`TreasuryLedger.cs:334` 双货币表示 | P1 |
| 8 | `GameEvent` 队列只进不出 | 饥荒/叛乱/瘟疫/灾害/经济危机 5 类处理不可达；`SimulationStatisticsRecorder` 无实例化使 4 个事件实际无消费者 | `GameWorld.Core.cs:359`、`:217`、`:468`；`SimulationStatisticsRecorder.cs:6/26` | P1 |
| 9 | Settlement 三事件无订阅者 + 三 Command 无提交点 | 聚落摧毁/弃地/重殖运行期不可触发 | §5 | P1 |
| 10 | `ActiveBuilding`/`ActiveDisaster`/`ActiveDisease` 不入档 | 在建工程、灾害、疫病读档即消失 | `BuildingSystem.cs:19`、`DisasterSystem.cs:20`、`DiseaseSystem.cs:24` | P1 |
| 11 | 20 个无读者字段 + 4 类无写者字段 | `movementControl` 恒 `Loose`、`successionLawFromCulture` 恒 true、`influenceRadius` 恒 1、`isCapital` 恒 false；7 个成就计数与 `AchievementRecord` 重复 | §1.1 / §1.2 | P2 |
| 12 | 内容硬编码/双源 | 108 特质、5 类伤残、13 个革新常量、5×8 阶级权重、5 条精神障碍内建只能改代码；精神障碍 JSON 与内建 Def 双源 | §4 | P2 |

## 8. 未核清单

1. `Definitions/` 12 文件的**内容管线消费已部分核实**：`StreamingAssets/Base` 下 31 个 JSON 目录中无 Trait、无 Impairment 目录，`Base/MentalHealth`、`Base/Title` 存在；但 `PersonalityTraitDatabase.All`(108) 与 `ImpairmentRegistry` 的 4 个 Def 是否另有 JSON 载荷、以及 `TitleManager` 的实际数据源，未逐条核对。
2. `CharacterSaveData.cs`（738 行）字段覆盖度**已按名称比对**：`CharacterData` 84 个 public 字段中 18 个在 DTO 内无同名项，其中 `boldness` 等 8 个性格维度由 `personality[8]`（`CharacterSaveData.cs:178`）承载、`identity`/`fullName`/`isAlive` 为派生属性、7 个成就计数本就是死字段（§1.1）→ **未核**的是"并行数组拍平后的逐元素等价性"，尤其是 `bodyParts` 三层索引（`:151-175`）。
3. `InnovationTree.cs` 私有状态**已核**：`CaptureState`(`:828`)/`RestoreState`(`:908`) 覆盖 `InnovationProgress` 全部字段（含 `gainBreakdown` → `StringFloatEntry`，`InnovationTreeSaveData.cs:37-48`）；**未核** `RegimeChangeDynamics.cs`(314) 是否有不依赖 `_states` 的旁路状态。
4. 未核 `LawEffects` 结构体 `stability/centralization/economy` 的全部 32 个 effect 分支是否存在未被 `LawEffectRuntime` 消费的死项。
5. 未核 `TileData` 53 字段中被实际写入的比例（仅核了 ownerRealmId/stability/fertility/populationBlocks 四项）。
6. 未核 `RaceData`/`RaceDTO`（40/34 字段）与 `TradeCenter`/`TradeCenterDTO` 的往返一致性。
7. 未核 UI 层对这些容器的直接写入（`Assets/UI/**` 只做了关键词计数，未逐面板读）。
8. `SettlementData.settlementLevel` 读档失真的**运行时后果**未实测（需 load→save 往返测试才能确认是否被 `OnBeforeSerialize` 或后续系统回写修正）。
9. `WorldCompositionRoot.InitializeSubsystems` / `PrepareLoadedWorld` 未逐行读 → 不排除读档后另有一处把 `developmentStage` 回写成 `settlementLevel` 的补偿代码（`SettlementPersistence.cs` 全 324 行与 `SaveSystem.ApplyToWorld` 全 640 行都没有）。
10. `TileData` 全字段**实际落盘**未验证：只静态确认 `SaveData.cs:36` 是 `TileData[]` 且 `PopulationBlock` 标 `[Serializable]`；`SaveSystemRoundTripTests.cs` 存在但未执行。
11. `isCapital`、`garrisonQuality/Quantity` 等标记是否由 `.unity`/`.prefab`/`.asset` 的 Unity 序列化赋值：只 grep 了 `.cs`，未查场景资产。
12. `GameWorld.Core.cs:359 ProcessEvents` 是否有反射/Inspector 入口：静态 0 命中，未排除非代码路径。
