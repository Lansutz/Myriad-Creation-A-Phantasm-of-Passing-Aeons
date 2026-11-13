# H 域审计：革新 / 科研 / 计划 / AI / 事件 / 调度器 / 存档主干 / 应用与 UI 入口（2026-11-09）

## 0. 范围

读取（行数为 `Measure-Object -Line` 实测）：

| 目录/文件 | 规模 |
|---|---|
| `Assets/Scripts/Simulation/MyriadCreation.Simulation.Scheduling/**` | 11 文件 / 420 行（逐个读完：SimulationScheduler/Scheduler primitives） |
| `Assets/Scripts/Simulation/MyriadCreation.Simulation.AI/**` | 12 文件 / 1181 行（AIController、AIDecisionSystem 全文；其余读 API） |
| `Assets/Scripts/Simulation/MyriadCreation.Simulation.Innovation/**` | 2 文件 / 34 行（全读） |
| `Simulation/.../State/InnovationTree.cs` | 971 行（全读） |
| `Systems/{InnovationResearchSchedule,ResearchPlanSchedule,ResearchPlanSystem,ResearchPlanExecutor,ResearchPlanData,CharacterResearchManager,CharacterResearchData,InnovationKnowledgeSystem,PlanSystem,PlanSchedule,AISchedule,EventSchedule,SimulationStatisticsRecorder,ConstructionPlanSystem,SandboxSpawnSystem}` | 全读或读至判定点 |
| `Simulation/.../WorldState/GameWorld.Simulation.cs` | 1225 行（`RegisterSimulationSchedules` 792–852 与全部注册点全读） |
| `WorldState/WorldMapSchedule.cs`、`WorldCompositionRoot.cs`、`GameWorld.HostHooks.cs`、`GameWorld.Persistence.cs` | 全读 |
| `CultureHost/CultureSchedule.cs`、`FaithHost/WarfareReligionSchedule.cs`、`Systems/*Schedule.cs`（17 个） | 全读（每个注册体的第一层工作） |
| `Assets/StreamingAssets/Base/Innovation/Innovations.json` | 186 条（PowerShell + ConvertFrom-Json 统计） |
| `Assets/Scripts/Infrastructure/MyriadCreation.Infrastructure.Save/**` | 22 文件 / 4224 行（SaveData、SaveSystem 捕获/还原段全读；其余按需） |
| `Assets/Scripts/App/**` | 16 文件 / 2015 行（Bootstrap、GameManager 读至判定点） |
| `Assets/UI/**` | 28 文件 / 4641 行（全部文件名 + 4 处关键字 grep，未逐行读面板实现） |
| `Assets/Tests/EditMode/` 137 文件 / `PlayMode/` 7 文件 | 按文件名分桶 + 11 个关键字 grep（未逐文件读） |

未覆盖（明确写出）：① 15 个 Schedule 背后各系统（Economy/Diplomacy/Character/Settlement/Combat 等）的**内部**逻辑我只核实了第一层是否有早退，未逐行验证其产出的语义正确性；② `Assets/UI` 面板实现未逐行读，UI 结论只基于「文件名 + 关键字 grep 命中数」；③ 测试文件只做域分桶与关键字命中，未逐个读断言；④ 未运行 Unity/测试，全部结论来自静态阅读。

## 1. 系统清单

### 1.1 主循环注册全表（`RegisterSimulationSchedules()` = `GameWorld.Simulation.cs:792-852`，共 **29** 条）

驱动链：`GameWorld.Update`（`GameWorld.Core.cs:985-993`，`tickInterval=1f` 见 `:73`）→ `GameTick`（`GameWorld.Simulation.cs:764-769`）→ `TickBudgeted(..., budgetMs, clock)`。

| # | 注册 id | cadence | order | 注册点 | 注册体内第一层工作 | 三态 |
|---|---|---|---|---|---|---|
| 1 | `world.map.recalculate-dirty` | Daily + dirty `world.terrain` | 10 | `WorldMapSchedule.cs:22` | `system.RecalculateDirty()` | 在跑（按需） |
| 2 | `disaster.daily` | Daily | 11 | `DisasterSchedule.cs:32` | `DisasterSystem.DailyTick` + `DiseaseSystem.DailyTick` | 在跑 |
| 3 | `economy.daily` | Daily | 12 | `EconomySchedule.cs:21` | `economy.DailyTick()` + 发布 `EconomyDayTickedEvent` | 在跑 |
| 4 | `building.daily` | Daily | 13 | `BuildingSchedule.cs:20` | 遍历 `_tileBuildings` 推进未完工建筑（`BuildingSystem.cs:180-192`） | 在跑 |
| 5 | `population.daily` | Daily | 14 | `PopulationSchedule.cs:20` | `PopulationTick()`（`Parallel.For` 全图，`Simulation.cs:53-109`） | 在跑 |
| 6 | `politics.daily` | Daily | 15 | `PoliticsSchedule.cs:20` | `PoliticsSimulationSystem.DailyTick`（`:74`） | 在跑 |
| 7 | `settlement-control.daily` | Daily | 16 | `SettlementSchedule.cs:30` | `_controlRuntime.DailyTick()` | 在跑（未深查） |
| 8 | `settlement.map-actors.daily` | Daily | 17 | `SettlementSchedule.cs:31` | `_mapActors.Tick(deltaDays)` | 在跑 |
| 9 | `settlement.camps.daily` | Daily | 18 | `SettlementSchedule.cs:32` | `_camps.Tick(deltaDays)` | 在跑 |
| 10 | `settlement-evolution.daily` | Daily | 19 | `SettlementSchedule.cs:33` | `_evolutionRuntime.DailyTick()` | 在跑（未深查） |
| 11 | `settlement-recovery.daily` | Daily | 20 | `SettlementSchedule.cs:34` | `_destructionRuntime.DailyTickRecovery()` | 在跑（未深查） |
| 12 | `settlement-abandonment.daily` | Daily | 21 | `SettlementSchedule.cs:35` | `_abandonmentRuntime.DailyCheckBanditSpawn()` | 在跑（未深查） |
| 13 | `culture-stage.monthly` | **Monthly** | 22 | `CultureSchedule.cs:20` | 体内再套 `if (currentDay % 30 == 0)`（`Simulation.cs:854-860`）→ 双重 30 日门 | 在跑 |
| 14 | `diplomacy.daily` | Daily | 23 | `DiplomacySchedule.cs:24` | `CurrentDay=day; DailyTick()` | 在跑 |
| 15 | `warfare-religion.daily` | Daily | 24 | `WarfareReligionSchedule.cs:51` | combat + 信仰狂热 + 圣战聚落 + 战争结算 四连 | 在跑 |
| 16 | `laws.effects.yearly` | Daily（体内年份 guard） | 24 | `Simulation.cs:807` | `TickLawEffectsYearly`（`Succession.cs:373`）→ `LawEffectRuntime.ApplyYearlyIfNewYear`（`:120`，`_lastLawEffectsYear` guard `Succession.cs:367`） | 在跑（每年一次） |
| 17 | `characters.daily` | Daily | 25 | `CharacterSchedule.cs:28` | `characters.DailyTick(day, year)` | 在跑 |
| 18 | `laws.pending.daily` | Daily | 26 | `Simulation.cs:802` | `TickPendingLawsDaily()`（`Simulation.cs:775-790`） | 在跑 |
| 19 | `succession.daily` | Daily | 26 | `SuccessionSchedule.cs:20` | `CheckRulerSuccessions()` | 在跑 |
| 20 | `thought.daily` | Daily | 27 | `ThoughtSchedule.cs:20` | `_thoughtManager.DailyTick(year)`（`ThoughtManager.cs:74-96`） | 在跑 |
| 21 | `justice.daily` | Daily | 27 | `Simulation.cs:830` | `DailyJusticeTick()` 遍历 `_judicialSystems`（`Simulation.cs:1217-1223`） | **空转**（见 2.1） |
| 22 | `ai.daily` | Daily | 28 | `AISchedule.cs:53` | `MissionaryTick` + `SyncRulers` + `ai.DailyTick` + `ai.ExecutePendingIntents` | 在跑 |
| 23 | `events.daily` | Daily | 29 | `EventSchedule.cs:20` | `ProcessEvents()` 清空 `_eventQueue`（`Core.cs:359-394`） | 在跑（监听器恒空） |
| 24 | `innovation.legacy_research.daily` | Daily | 29 | `InnovationResearchSchedule.cs:36` | 每政权 `ai.GetResearchRate` >0 才 `innovations.DailyTick` | 半跑（见 2.2） |
| 25 | `research.breakthrough.daily` | Daily | 29 | `ResearchPlanSchedule.cs:20` | `ProcessPracticeBreakthroughs(deltaDays)`（`ResearchPlanSystem.cs:92-111`） | 半跑（见 2.2） |
| 26 | `plans.daily` | Daily | 30 | `PlanSchedule.cs:20` | `_planSystem.DailyTick(day, deltaDays)` | 半跑（见 2.3） |
| 27 | `simulation.time.daily` | Daily | 31 | `SimulationTimeSchedule.cs:19` | `AdvanceTime()`（`Simulation.cs:348-368`） | 在跑 |
| 28 | `sandbox.spawn.monthly` | **Monthly** | 36 | `Simulation.cs:848` | `SandboxSpawnSystem.MonthlyTick()` | 在跑（默认开启） |
| 29 | `decrees.propagation.daily` | Daily | 37 | `Simulation.cs:840` | `TickDecreeOrders()`（`Simulation.cs:705-762`） | 在跑（UI 可触发） |

**order 语义**：`order` 不是严格的执行优先级，而是**排序主键的低位桶**——`SimulationScheduler.cs:136-140` 按 `(order, string.CompareOrdinal(id))` 排序，即同 order 的条目按 id 字典序执行。实际同 order 桶：24 = {laws.effects.yearly, warfare-religion.daily}→laws 先；26 = {laws.pending.daily, succession.daily}→laws 先；27 = {justice.daily, thought.daily}→justice 先；29 = {events.daily, innovation.*, research.*}→字典序。**没有任何条目声明依赖关系**，跨条目数据依赖全靠 order 数字隐式约定。

**cadence 语义**：`GetIntervalDays`（`SimulationScheduler.cs:174-184`）= Weekly 7 / Monthly 30 / Seasonal 90 / LongTerm 365 / 其它 1。触发判定是 `intervalDays <= 1 || day % intervalDays == 0`（`:131`），即：
- 「Monthly」= **每 30 个模拟日**（day 从 1 起、天然按 30/60/…/360 落点），**不是日历月**；
- 不存在「Yearly」枚举值；年效用 `day % 365 == 0` 表达时只在 day=365 命中，随后 `AdvanceTime` 把 day 重置为 1（`Simulation.cs:350-355`），故 LongTerm 恰为一年一次但**全工程 0 条注册使用**（Weekly/Seasonal/LongTerm 均为 0 使用）；
- 实际使用的 cadence 只有 Daily（27 条）与 Monthly（2 条）。

**预算与延迟语义**（`SimulationScheduler.cs:120-172`）：`SimulationBudgetGovernor.NextBudget` 在 4–12ms 间浮动（默认 8ms，`SimulationBudgetGovernor.cs:8-33`）；超预算条目进 `_pending`，**下一 tick 被无条件重新入列**（`:124-128`），且不再校验 cadence 与 dirty——延迟条目会跳过倍频门。延迟队列仍参与同一次排序，因此低 order 的延迟条目会在下一 tick 抢先执行。

**并行/确定性**：调度层**无并行**——`SimulationLane`（Environment/Civilization/Both）存在但所有注册走 4 参重载 → 恒 `Both`（`SimulationScheduler.cs:43-46`），`GameTick` 以 `Both` 调用（`Simulation.cs:768`），lane 无区分作用。唯一并行在 `PopulationTick` 内部（`Parallel.For`，`Simulation.cs:56`）。**确定性不可保证**：该并行体的事件 RNG 是 `ThreadLocal<Random>(() => new Random(Environment.TickCount ^ ManagedThreadId))`（`GameWorld.Core.cs:121`），叛乱/饥荒事件序列跨运行不可复现（`Simulation.cs:77`）。

### 1.2 三态清单（本域关键系统）

| 系统 | 三态 | 驱动证据 | 入口证据 | 存档证据 | 测试 | 现状 |
|---|---|---|---|---|---|---|
| SimulationScheduler | 在跑 | `Simulation.cs:768` | — | 无（`_pending`/dirty/avgMs 不入档） | `SimulationSchedulerBudgetTests.cs` 等 5 文件 | 单线程、预算化、order+id 排序 |
| AI（AIController/AIManager/AIIntentExecutor） | 在跑 | `AISchedule.cs:53-58`，`GameWorld.Core.cs:582` 建 manager，`:597` / `Generation.cs:117` 建控制器 | 无 UI，纯自动 | **不入档**（personality/decisionTimer/intents 全丢） | `WorldServicesTests.cs`、`WorldGenerationSmokeTests.cs` | 决策→入队→执行闭环完整 |
| AIDecisionSystem | **空转** | 定义 `AIDecisionSystem.cs:10`，全工程引用 2 处且都在本文件（类声明+构造），**零实例化** | 无 | 无 | 0 文件 | 312 行死代码 |
| AIState / SettlementAISystem / AIBuildDecision | **空转** | `AIState.cs` 仅被 AIDecisionSystem 引用（48 处，全在该文件）；`SettlementAISystem` 全工程命中 1 处=自身声明；`AIBuildDecision` 仅 3 个无调用者的工厂方法 | 无 | 无 | 0 文件 | 197 行死代码 |
| InnovationTree | 在跑 | `InnovationResearchSchedule.cs:36`、`ResearchPlanExecutor`→`TryFormalizeResearch` | 无 UI | `SaveData.innovations`（`SaveData.cs:67`；`SaveSystem.cs:397/464`） | InnovationSystem/Persistence/Material/Discovery 等 | 定义 186 条，运行态入档 |
| InnovationTree.MonthlyTickProgress | **空转** | 定义 `InnovationTree.cs:677`，全工程命中 1 处 | — | `InnovationProgress` 有 DTO（`InnovationTreeSaveData.cs`） | 无 | 「生产实践驱动」模型整段未接线 |
| InnovationUnlock.unlockRefs | **空转** | 定义 `InnovationDef.cs:72`，运行时 0 消费者（仅测试 `InnovationUnlockRefTests.cs:47`、`EquipmentCatalogTests.cs:36`） | — | 随内容定义不归档 | 2 文件断言其内容 | 120 条解锁引用无人读 |
| ResearchPlanSystem | 半跑 | 注册 `ResearchPlanSchedule.cs:20`；`PracticeRecordedEvent` 由 `BuildingSystem.cs:226/241`、`CombatManager.cs:163` 发布 | 无 UI | **不入档**（`_data`/`_knowledge` 全丢） | 0 文件（`ResearchPlanSystem` 命中 0） | 见 2.2 |
| PlanSystem | 半跑 | 注册 `PlanSchedule.cs:20` | 无 UI | **不入档** | 仅 `ArchitectureContractsTests.cs` 提及 | 见 2.3 |
| ConstructionPlanSystem | **空转** | `GameWorld.CreateConstructionPlan`（`Core.cs:324`）**零调用者**（grep 仅定义处） | 无 | 无 | 0 | executor 注册但永无 plan 创建 |
| CharacterResearchManager / CharacterResearchData | **空转** | 全工程命中仅自身文件（`:6`/`:14`） | 无 | 无 | 0 | 77+78 行死代码 |
| InnovationKnowledgeSystem（mastery/practice） | 半跑 | `RecordPractice` 有上游；`Learn`/`SetMastery` **零调用者** | 无 | **不入档** | 0 | mastery 恒 0（见 2.2） |
| SimulationStatisticsRecorder | **空转** | 构造即订阅事件，但全工程仅 `SimulationEventBusTests.cs` 实例化 | 无 | 无 | 1 文件 | 运行时不构造 |
| IGameEventListener 扇出 | **空转** | `Core.cs:364` 消费 `_eventListeners`；`RegisterEventListener`（`Core.cs:468`）**零调用者** | 无 | — | 0 | 监听器列表恒空 |
| SaveSystem（v17） | 在跑 | `GameManager.cs:279/287` | 有（SaveLoadPanel） | 主干本体 | `Save*` 分桶 16 文件 | 见 2.5 |
| GameSaveSystem | **空转** | `[Obsolete]`（`GameSaveSystem.cs:39`），无调用者 | 无 | 自带旧逻辑 | 0 | 642 行遗留 |
| App 启动链 | 在跑 | `Bootstrap.Awake` → `GameManager`/`SceneFlowController`（`Bootstrap.cs:34-155`） | 有 | — | PlayMode 7 文件覆盖 6 面板+图标层 | 见 2.6 |

## 2. 数据模型现状（按问题 2–6 分节）

### 2.1 注册了但恒空转的项
`justice.daily`（`Simulation.cs:830`）是**唯一恒空转**的注册项：`DailyJusticeTick` 遍历 `_judicialSystems`（`:1217-1223`），该字典只在 `GetOrCreateJudicialSystem`（`GameWorld.Queries.cs:262-279`）里写入，而它唯一调用者是 `TryTrialCrime`（`Simulation.cs:1191`），`TryTrialCrime` 与 `MatchCrimes`（`:1186`）在 `Assets/**` 全工程**零调用者**（grep 命中=仅定义行）。→ 字典恒空，每 tick 空循环。

其余 28 条都有真实工作或其上游可用（`world.map` 为 dirty 按需；`sandbox.spawn.monthly` 默认开启见 `WorldSetupOptions.cs:37`）。

### 2.2 革新（问题 3）
- **玩家入口 = 0**。`Assets/UI` 全目录对 `Innovation|Research|Plan` 的 grep 命中 **0**（唯一命中是 `GameManager.cs:152` 的无关注释）。读模型层其实已备好 `IInnovationQuery`（9 个方法，`GameWorld.ReadModel.cs:113-137`，经 `InnovationQueryAdapter.cs:15`），但 **6 个面板无一消费**（`IInnovationQuery` 在 `Assets/UI` 命中 0）。
- 唯一能让研究开始的路径是 AI：`AIController.SelectDailyIntent`（`AIController.cs:125-159`）→ `StartResearch` intent → `AIIntentExecutor.cs:29` → `StartInnovationResearchCommand` → `InnovationTree.StartResearch`。UI 侧无对应 command 注册到面板。
- **`unlockRefs`：JSON 中 89 条记录带该键、共 120 条引用**（PowerShell 统计：`with unlockRefs 非空 = 84`、`total entries = 120`；kind 分布 11=31 / 5=30 / 3=29 / 1=24 / 2=6）。**运行时消费者 = 0**——`unlockRefs` 只出现在定义（`InnovationDef.cs:72`）与两个测试文件；装备/配方门槛走的是另一条链（`EquipmentDef.requiredInnovation` + `AreItemInnovationsMet`，`InnovationTree.cs:260-272`）。
- **角色熟练度不会涨**：`InnovationKnowledgeSystem.Learn`（`:74`）/`SetMastery`（`:31`）全工程**零调用者**，唯一包装 `ResearchPlanSystem.Learn`（`:242`）也零调用者。`GetMastery` 恒 0 → `IsKnowledgeNeighbor` 要求 `GetMastery(prereq) >= 2`（`InnovationKnowledgeSystem.cs:128`）→ **167/186 条带前置的革新对任何角色恒不可发现**；`TryDailyBreakthrough`（`ResearchPlanSystem.cs:135-178`）对它们恒返回 false。只有无前置的 19 条 + 有 practice 记录的组合才可能推进。
- 「生产实践」路径确实存在：`BuildingSystem.cs:226/241`、`CombatManager.cs:163` 发布 `PracticeRecordedEvent` → `ResearchPlanSystem.OnPracticeRecorded`（`:51`）→ `RecordPractice`（`:67`）→ dirty 集合 → `ProcessPracticeBreakthroughs`。断点在 mastery 门槛与 0.03/日上限（`:272`）。

### 2.3 计划系统（问题 4 的落点）
- `PlanSystem`（440 行）状态机完整（Proposed→Accepted→Preparing→Executing→终态，`:375-412`），但**生产者只有 Research**：`ResearchPlanSystem.CreateResearchPlan`（`:194-222`）。`ConstructionPlanSystem.CreateConstructionPlan`（`:28`）的 public 包装 `GameWorld.CreateConstructionPlan`（`Core.cs:324`）**零调用者** → `ManagedByPlan`（`ActiveBuilding.cs:26`）恒 false，建筑永远走 `BuildingSystem` 直推路径。即 **Construction 计划分支 = 空转**。
- `ResearchPlanSystem.DailyTick`（全角色扫描版，`:118-133`）在调度里**未被调用**（`ResearchPlanSchedule.cs:20` 只调 `ProcessPracticeBreakthroughs`）→ 该 public 方法零调用者。
- **AI 决策后是否无人执行**：链路完整，无断链。`AISchedule.cs:53-58` 顺序为 `missionaryTick → SyncRulers → ai.DailyTick（产 intent）→ ai.ExecutePendingIntents（DrainIntents + 逐 intent 转 command）`；`AIIntentExecutor.cs:27-53` 把 8 种 intent 全部映射到已注册 command handler（handler 注册见 `Core.cs:335-351`）。**AI 可做的决策共 8 类**：StartResearch / RaidSettlement / DeclareWar / ImproveEconomy / ProposeAlliance / SendGift / ConsolidateRealm / MilitaryBuildUp（`AIIntentType.cs`）。调用点：`SelectDailyIntent` 每日一次（`AIController.cs:48`）、`TryRaid` 每 45 日（`:49,69-123`）、`MakeMajorDecision` 每 30 日（`:50-54,181-207`）。实际会被触发的子集受效用函数限制：`ExecuteGoal`（`:304-324`）只对 5 个 goal 出 intent，`ImproveEconomy/Consolidate/MilitaryBuildUp` 各 1 条，`Diplomacy` 按关系分叉（`:281-302`）。

### 2.4 事件（问题 4 边角）
`ProcessEvents` 每次 tick 清空 `_eventQueue` 并 switch 到 8 个 handler（`Core.cs:359-394`），工作真实。但 `_eventListeners` 扇出（`:364`）恒空（`RegisterEventListener` 零调用者）——事件只走内部 switch，无任何外部观察者（UI 不订阅）。

### 2.5 存档（问题 5）
- **`GameConstants.SaveVersion = 17`**（`Assets/Scripts/Core/MyriadCreation.Core.Constants/GameConstants.cs:13`）；读档版本不一致只警告不拒绝（`SaveSystem.cs:263-265`）。
- **入档主干**（`SaveData.cs:22-77`，捕获 `SaveSystem.cs:357-405`）：地图/地块、races/cultures/realms/贸易中心/物产/关系/契约/config/seed、wars（v13 前已有）、角色+家族+官职+mapActors+mapUnits+knownCharacters（v11）、省份+据点（v12）、军队（v13）、**革新城（v14，`SaveData.cs:67`）**、信仰（v15）、纽带（v15）、海军（v17）。
- **不入档（本域）**：`PlanSystem`（`_plans`/`_activePlanIds`/`_nextPlanId`）、`ResearchPlanSystem`（`_data`、`_knowledge` 的 mastery/practice）、`AIManager`（AIController 的 personality/`_decisionTimer`/`_raidCooldown`/pending intents）、`SimulationScheduler`（`_pending` 延迟队列、dirty 版本、avgMs）、`CharacterResearchManager`、`GameWorld._judgmentSystems`/`_eventQueue`/`_missionaryTimer`/`_faithFervorDay`、`SandboxSpawnSystem` 的 `_lastPopulationSeedDay`。
- **「DTO 已定义但 Adapter 缺失」**：本域**不存在**这种情况——`PlanSaveData`/`AISaveData`/`ResearchSaveData` 之类的 DTO 在全工程 grep 命中 0，即这些系统**连 DTO 都没有**（`InnovationTree` 是唯一例外，有 `InnovationTreeSaveData` + 完整 Capture/Restore `:828-969`）。读档后 AI 控制器与调度器由 `PrepareLoadedWorld`（`Persistence.cs:56-70`：unitDefs + FaithSystems + `EnsureAIControllers`）与 `InitializeSubsystems`→`RegisterSimulationSchedules`（`Core.cs:582`）重建，**但计划/AI 内部状态从零开始**。

### 2.6 App / UI（问题 6）
- **启动链完整**：`Bootstrap.Awake`（`Bootstrap.cs:34-155`）→ `AppServices.RegisterSettings` → `ContentRegistry.Initialize` → `Localization` → `GameManager`（无则建）→ `InjectMapEditService/InjectUiReadViews` → `AppServices.GameSession.ConfigureNewGameDefaults` → 相机/灯光 → `SceneFlowController`（无则建）；`Start` 分支主菜单/直启（`:157-172`）。`GameManager.StartNewGame`（`:132-158`）→ `WorldFactory.Create` → 地形生成 → `GameWorld.InitializeSubsystems`（`Core.cs:540-583`，内含 `RegisterSimulationSchedules()` 于 `:582`）。`UIManager` 不是 Bootstrap 创建的——它是场景组件，自己在 `UIManager.cs:335/341` 向 `AppServices.RegisterUiHost` 注册；`GameManager` 通过 `AppServices.UiHost` 取用。→ 链路「Bootstrap→WorldFactory→GameWorld」闭合，UI 宿主依赖**场景内已挂 UIManager**（代码不兜底创建）。
- **UI 覆盖面**：`Assets/UI/Panels` 仅 6 个（Government / MainMenu / SaveLoad / Settings / SettlementDetail / Society）。UI 实际消费的查询接口仅 6 个：`IWorldReadModel`(20) / `ILawQuery`(5) / `IDecreeCommand`(5) / `IFaithQuery`(3) / `ISocietyQuery`(5) / `ISettlementPeopleQuery`(2)；**零消费**：`IInnovationQuery` / `IDiplomacyQuery` / `IEconomyQuery` / `IWarfareQuery` / `ICharacterQuery` / `ICultureQuery`（各 0 命中）。
- **无任何界面能触发/观察的模拟系统（按 29 条注册项计）= 22 项**：world.map(1)、disaster(2)、economy(3)、population(5)、settlement 六连中的 5 项（7–12 只经 SettlementDetailPanel 间接看到聚落结果，不算独立面板）、culture-stage(13)、diplomacy(14)、warfare-religion(15)、characters(17)、succession(19)、thought(20)、justice(21)、ai(22)、events(23)、innovation(24)、research(25)、plans(26)、sandbox.spawn(28)。有入口的只有：politics/laws（GovernmentPanel 经 `ILawQuery`/`IDecreeCommand`）、settlement（SettlementDetailPanel）、society（SocietyPanel）、faith（`IFaithQuery`）、存档（SaveLoadPanel）。

### 2.7 测试分布（问题 7，137 EditMode + 7 PlayMode）
按文件名正则分桶（含重叠计数，合计 137 全覆盖）：

| 域 | 文件数 |
|---|---|
| 政体/法律/派系/法庭 | 23 |
| 调度/门禁/架构/契约/读模型 | 24 |
| 世界/地形/生成 | 17 |
| 存档/读档/迁移 | 16 |
| **革新/科研/计划** | 15 |
| 聚落/据点/营地/建筑 | 12 |
| 经济/物产/贸易 | 11 |
| 军事/战争/外交/信仰 | 10 |
| 角色/人物 | 5 |
| 文化/思想/社会 | 2 |
| 未归入上述桶（UiText/Genetics/Property/Settings/Admin 等） | 30 |

**零覆盖域**（关键字 grep 命中 0 文件）：`ResearchPlanSystem`、`CharacterResearchManager`、`InnovationKnowledgeSystem`、`AIIntent`、以及全部死代码（`AIDecisionSystem`/`AISettlement`/`AIBuildDecision`/`AIState`）。`PlanSystem` 仅 1 文件提及（`ArchitectureContractsTests.cs`），无行为测试。PlayMode 7 文件全部是 UI/渲染（6 面板 + 图标层），**无一条模拟行为测试**。

## 3. 内容数据现状
- `Assets/StreamingAssets/Base/Innovation/Innovations.json`：**186 条**（`innovations` 单键）。
- 字段覆盖（ConvertFrom-Json 统计）：所有 186 条均有 `innovationId/field/era/researchCost/innovationName/description/contentSchemaVersion/termEn/definition/scope/category/kind/sources/unlocks/prerequisiteRationale`；`prerequisiteTree` **167** 条；`unlockRefs` **89** 条（非空 84，引用共 120）；`affinityTags` 69；`legacyIds`/`regions`/`variantOf` 各 38；`associatedGoods` 14；`softPrerequisites` 5；`providesCapabilities` 4；`capabilityModifiers` 2。
- `researchCost`：min 20 / max 1500 / avg 317.6。
- 被代码消费的定义字段：`prerequisiteTree`（`InnovationTree.cs:276-282`，167/186 生效）、`requiredAnyResources/requiredAllResources`（`:295-320`）、`softPrerequisites`（`:154-172`，仅 5 条）、`capabilityModifiers/requiredCapabilityValues`（`:106-153`）、`discoveryGroups`（`InnovationDiscovery.cs:5-22`；`ContentDefinitionIndexTests.cs` 断言 id 1/1025 有兜底组）、`unlocks`（字符串数组，无结构化消费）、`affinityTags`（`:499-509`）。
- **不被消费**：`unlockRefs`（120 条引用，运行时 0 消费者）、`legacyIds`、`regions`、`variantOf`、`category`、`sources`（未发现运行时读取）。

## 4. 缺口清单（按影响排序）

| # | 缺口 | 影响 | 证据 | 严重度 |
|---|---|---|---|---|
| 1 | 革新/科研/计划 **完全没有玩家入口**；唯一研究发起者是 AI | 玩家无法研究任何革新，整条科技树只能旁观 | `Assets/UI` grep `Innovation|Research|Plan` = 0；`IInnovationQuery` UI 命中 0；`StartInnovationResearchCommand` 唯一发送方 `AIIntentExecutor.cs:29` | P0 |
| 2 | 角色 mastery 恒 0，`Learn`/`SetMastery` 零调用者 | 167/186 条带前置革新对任何角色恒不可发现，个人突破链基本不启动 | `InnovationKnowledgeSystem.cs:74,31`；`ResearchPlanSystem.cs:242`；门槛 `InnovationKnowledgeSystem.cs:128` | P0 |
| 3 | 革新 `unlockRefs`（120 条）无运行时消费者 | 革新「解锁了什么」不进任何 gameplay 判断 | 定义 `InnovationDef.cs:72`；全工程消费仅 2 个测试文件 | P1 |
| 4 | `MonthlyTickProgress`（生产实践驱动的革新进度模型）零调用者 | 「实践→品质→革新」设计整段未接线，只有 legacy 研究点路径在跑 | `InnovationTree.cs:677` 全工程命中 1 处；`InnovationProgress` DTO 仍随存档 `CaptureState:860-893` 序列化一个永空字典 | P1 |
| 5 | `justice.daily` 恒空转（`_judicialSystems` 恒空）+ `TryTrialCrime`/`MatchCrimes` 零调用者 | 每 tick 空循环；司法系统整体无入口 | `Simulation.cs:830,1191,1217-1223`；`Queries.cs:262` | P1 |
| 6 | 计划/AI/调度器状态**全部不入档**（连 DTO 都没有） | 读档后 orchestrator 全量清零：进行中的研究计划、AI 性格/冷却/意图、延迟队列、mastery/practice 全部丢失 | `SaveData.cs:22-77` 无对应字段；`PlanSaveData/AISaveData/ResearchSaveData` grep = 0；读档只 `PrepareLoadedWorld`（`Persistence.cs:56-70`） | P1 |
| 7 | `ConstructionPlanSystem` 整条分支死：`CreateConstructionPlan` 零调用者 | `ManagedByPlan` 恒 false，建造永远绕过计划系统 | `Core.cs:324`；`ActiveBuilding.cs:26`；`BuildingSystem.cs:186` | P1 |
| 8 | 512 行 AI 死代码（`AIDecisionSystem` 312 + `AIState` 22 + `SettlementAISystem` 164 + `AIBuildDecision` 11）零实例化、零测试 | 阅读/维护成本；存在两套互相不连的 AI 决策模型 | `AIDecisionSystem.cs:10`（引用 2 处均在本文件）；`SettlementAISystem.cs:9`（命中 1）；`AIBuildDecision.cs:5` | P2 |
| 9 | 并行体 RNG 非确定性（`Environment.TickCount ^ ManagedThreadId`） | 同一 seed 的两次运行事件序列不同，难以复现 bug | `GameWorld.Core.cs:121`；`Simulation.cs:56,77` | P2 |
| 10 | 同 order 桶按 id 字典序执行，无显式依赖声明 | order 语义不可自解释；新增条目可能无意改变同桶顺序 | `SimulationScheduler.cs:136-140`；同桶实例见 1.1 | P2 |
| 11 | 延迟队列条目跳过 cadence 与 dirty 校验 | 超预算条目下一 tick 无条件重跑，Monthly/dirty 语义被绕过 | `SimulationScheduler.cs:124-128,167-171` | P2 |
| 12 | `_eventListeners` 扇出恒空 | 事件只服务内部 switch，任何外部观察者（UI/统计）拿不到 | `Core.cs:364,468`（`RegisterEventListener` 零调用者） | P2 |
| 13 | `SimulationStatisticsRecorder` 运行时不构造 | 统计能力只有测试在用 | 仅 `SimulationEventBusTests.cs` 实例化 | P2 |
| 14 | `CharacterResearchManager`/`CharacterResearchData`（155 行）零引用 | 与 `ResearchPlanSystem` 并存的第二套「角色研究」模型，无人用 | 全工程命中仅自身文件 | P2 |
| 15 | `GameSaveSystem`（642 行）`[Obsolete]` 但仍编译 | 双存档实现并存，易误用 | `GameSaveSystem.cs:39` | P2 |
| 16 | 科研/计划/AI 零测试；PlayMode 无模拟行为测试 | 上述断链没有任何自动化防线 | `ResearchPlanSystem`/`CharacterResearchManager`/`InnovationKnowledgeSystem`/`AIIntent` grep 命中 0 文件 | P2 |
| 17 | lane（Environment/Civilization）机制无任何区分使用 | 12 个 lane 相关文件只提供 `Both` 一条实际路径，无法按环境/文明分流或并行 | `SimulationScheduler.cs:43-46`；`GameTick` 传 `Both`（`Simulation.cs:768`） | P2 |

## 5. 我核实不了的事
1. **未运行 Unity、未跑 EditMode/PlayMode 测试**：全部「在跑/空转」判定是静态阅读 + grep 呼叫链，不是运行证据。若存在反射/序列化/Inspector 注入式调用，我的「零调用者」结论会失真（本域内未发现 `SendMessage`/`Invoke`/`FindObjectOfType`+类型名字符串等模式，但未穷举）。
2. `Assets/UI` 面板实现未逐行读：UI 结论基于「文件名 + 接口名 grep 命中数」，可能存在通过 `IWorldReadModel` 间接读革新数据但未出现 `Innovation` 字样的写法（`IWorldReadModel` 在 UI 命中 20 次，我未逐个核对那 20 处调用的成员名）。
3. 15 个 Schedule 背后系统（Economy/Diplomacy/Character/Settlement/Combat/Thought 等）我只核实了「第一层有无早退」，其内部是否在特定状态下恒空转未逐行验证——因此 1.1 表中标注「未深查」的 4 项（settlement-control / evolution / recovery / abandonment）属于**未验证为在跑**。
4. `PoliticsSimulationSystem`、`DiplomacyManager`、`CharacterManager` 的 `DailyTick` 我只看到方法签名成功命中，未读其早退条件；它们的「在跑」是「有非空工作体」而非「有可观察产出」。
5. `law` 域与 `settlement` 域的细节归其他域审计；本报告只覆盖它们在主循环的注册与入口，不评价其机制正确性。
6. 存档「哪些字段真正往返」我只做了静态对齐（DTO 字段 ↔ Capture/Restore 代码），未做实际存读往返实验；`InnovationPersistenceTests.cs` 的存在说明革新城曾被实测，其余系统无同类测试。
