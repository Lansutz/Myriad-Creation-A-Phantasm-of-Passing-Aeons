# 聚落 / 行省 / 人口 审计（2026-11-09）

> 只读审计。代码为唯一真源；每条判定带 `file:line`（相对 `MyriadCreation/`）。未运行 Unity，测试结论只基于文件与方法名。

## 0. 范围

读取（全量读过，非抽样）：

- `Assets/Scripts/World/MyriadCreation.World.Settlement/**` — 48 个 `.cs` / 3,182 行（`SettlementData.cs` 710、`SettlementTypologySystem.cs` 354、`CityDistrictSystem.cs` 325、`EconomicCompositionSystem.cs` 250、`WallDefense.cs` 220；其余 ≤150）
- `Assets/Scripts/Simulation/MyriadCreation.Simulation.Settlement/**` — 9 个 `.cs` / 138 行（3 command + 3 handler + 3 event）
- `Simulation/MyriadCreation.Simulation.Systems/` 16 个 / 2,154 行：`SettlementSchedule`(38)、`SettlementSimulationSystem`(88)、`SettlementInfluenceSystem`(257)、`SettlementControlSystem`(242)、`SettlementControlRuntime`(55)、`SettlementEvolutionSystem`(219)、`SettlementEvolutionRuntime`(37)、`SettlementDestructionSystem`(338)、`SettlementDestructionRuntime`(21)、`LandAbandonmentSystem`(197)、`LandAbandonmentRuntime`(30)、`CarryingCapacitySystem`(135)、`PopulationSchedule`(26)、`PopulationStats`(140)、`BuildingAvailability`(33)、`BuildingAvailabilitySystem`(439)
- 邻接被读（判断调用链必需）：`GameWorld.Simulation.cs`、`GameWorld.Core.cs`、`GameWorld.Generation.cs`、`SettlementQueryAdapter.cs`、`MapWorldViewAdapter.cs`、`WorldReadModel.cs`、`SocietyManager.cs`、`SocialDifferentiation.cs`、`ClassNeedsSystem.cs`、`RaceData.cs`、`BarrierSystem.cs`、`UnitFounding.cs`、`SettlementAISystem.cs`、`NomadicRealmSystem.cs`、`MissionarySystem.cs`、`StampedeSystem.cs`、`CombatManager.cs`、`AIIntentExecutor.cs`、`DecreePropagationSystem.cs`
- UI：`Assets/UI/Panels/SettlementDetailPanel.cs`(271)、`Assets/UI/MyriadCreation.UI.Presentation/`(12 个 / 601 行，含 `SettlementPeopleText`、`PopulationCompositionPanelModel`、`PopulationCompositionRow`)
- 存档：`SettlementPersistence.cs`(297)、`HabitationSiteSaveData.cs`(288)、`ProvinceSaveData.cs`(13)、`SaveSystem.cs`(640，关键段)、`SaveData.cs`、`TileSaveData.cs`、`MapSaveData.cs`、`GameSaveSystem.cs`(标注 Obsolete 的旧路径)
- 测试 13 个：`HabitationSiteTests`(235)、`SettlementPersistenceTests`(231)、`SettlementBlockTests`(177)、`WallFortificationTests`(146)、`ProvinceGeneratorTests`(105)、`SettlementInfluenceTests`(104)、`SettlementBlockSaveTests`(102)、`PopulationCompositionTests`(87)、`SettlementMarketTests`(87)、`SettlementFoundingAndMarketSaveTests`(81)、`SettlementEconomyTests`(80)、`WallDefenseTests`(74)、`SettlementLayeringTests`(37)

未覆盖 / 明确不声称：

- 未运行 Editor 或测试，不判定"测试是否全绿"，只判定"有没有测试、测的是什么"。
- `MapRenderer.Texture.cs` 影响力叠加的绘制开关（政治模式是否真的打开）只看到读取点，未验证场景/prefab 绑定。
- 未穷尽反射/字符串派发；"零调用者"结论限于 `grep` 类型与成员名。
- 战斗结算内部（`CombatManager`）对城墙/守军的实际使用只做了调用点级确认，未逐行审计。
- 内容侧只看 `StreamingAssets/Base` 顶层目录，未逐个 JSON 解析。

## 1. 系统清单

| 系统 | 三态 | 驱动证据（file:line） | 入口证据 | 存档证据 | 测试 | 一句话现状 |
|---|---|---|---|---|---|---|
| 世界生成聚落（AnchorGenerator） | 在跑 | `GameWorld.Generation.cs:397` → `AnchorGenerator.cs:40-116`；`GenerateSettlements` 由 `GameWorld.Generation.cs:133/348` 调用 | 地图图标 `SettlementIconLayer`；`SettlementQueryAdapter.cs:23` | `SettlementPersistence.cs:85-133` | `SettlementFoundingAndMarketSaveTests.cs:33`、`SaveSystemRoundTripTests.cs:176-177` | 每个行省造 1 主聚落 + 港口/据点/村落，等级与人口在生成时随机定死 |
| 营地→聚落演化 | 在跑 | `SettlementSchedule.cs:33` → `SettlementSimulationSystem.cs:73-76` → `SettlementEvolutionRuntime.cs:17-20` → `SettlementEvolutionSystem.cs:22-37` | 无玩家入口（纯规则；`CanEvolveToSettlement` 门槛 `SettlementEvolutionSystem.cs:32/75/83`） | 演化结果走 `CaptureSites` | 无 | 营地人口 ≥50（或据点 ≥100）自动变聚落，是唯一"新建聚落"的活路径 |
| 聚落成长（构造度/等级提升） | 空转 | 三个写入器零调用者：`SettlementTypologySystem.cs:52`(UpdateConstructionProgress)、`:221`(CanLevelUp)、`:297`(PerformLevelUp) — `grep "UpdateConstructionProgress\|CanLevelUp\|PerformLevelUp"` = 3 命中，全在定义文件 | 无 | `constructionTier/constructionProgress` 入档（`SettlementPersistence.cs:112-113`） | 无 | `constructionTier` 生成时=1（`SettlementTypologySystem.cs:46-47`）、读档可回填，但永不前进；`settlementLevel` 只在生成/（死）毁灭路径被写 |
| 聚落毁灭 | 空转 | `SettlementDestructionSystem.cs:24` 仅由 `SettlementDestructionRuntime.cs:15-17` → `DestroySettlementCommandHandler.cs:19-22` 调用；`new DestroySettlementCommand(` 生产代码 0 命中（`GameWorld.Core.cs:349` 是 `CommandHandler`） | 无 UI、AI 意图 8 种无此项（`AIIntentExecutor.cs:32-54`） | `ruinLevel` 等字段**未入档** | 无（`HabitationSiteTests` 只测 `ApplyFacilityDamage`/`RepairFacilities` 单函数） | 六种破坏方式、人口损失表、流民生成全部写好，无人触发 |
| 废墟恢复 | 空转 | `SettlementSchedule.cs:34` 注册 → `SettlementSimulationSystem.cs:78-81` → `SettlementDestructionSystem.cs:277-311` | 无 | — | 无 | **注册后恒早退**：`:286` 要求 `ruinLevel > 0`，而 `ruinLevel` 唯一写者 `:38` 在死路径上 |
| 土地废弃 / 重定居 | 空转 | `LandAbandonmentSystem.cs:25`(AbandonTile)/`:154`(ResettleTile) 只被 `LandAbandonmentRuntime` 与 2 个 handler 使用；`AbandonSettlementTileCommand`/`ResettleTileCommand` 生产代码 0 派发 | 无 | `ownerRealmId` 在地块层入档 | 无 | 唯一另类入口 `NomadicRealmSystem.NomadicAbandon`（`:120-122`）本身零调用者 |
| 土匪刷新（挂在 abandonment 日 tick 上） | 在跑 | `SettlementSchedule.cs:35` → `SettlementSimulationSystem.cs:83-86` → `LandAbandonmentSystem.cs:177-195` | 无（AI 角色由 `MapActorManager` 驱动） | 地图角色入档 | 无 | 这条 schedule id 名字叫"废弃"，实际只干随机刷土匪（每日 2% × 5 次采样） |
| 宗主控制（Control） | 半跑 | `SettlementSchedule.cs:30` → `SettlementControlSystem.cs:23-33`（扫关系→更新影响力→推进度→判定完成） | **无玩家 UI**；唯一消费者是法令传播 `DecreePropagationSystem.cs:68-74` | **不入档**：`AnchorData.controllerAnchorId` 无对应 DTO，`HabitationSiteSaveData` 不存 | `SettlementInfluenceTests.cs:97-98` | 每日真实推进 `controlProgress` 并写 `anchor.controllerAnchorId`，但**没有税收/征兵/贸易消费**（见 §4-6） |
| 影响力场（InfluenceField） | 半跑 | `SettlementControlSystem.cs:29` → `SettlementInfluenceSystem.cs:151-157`（返回值被丢弃，只留副作用） | 地图叠加：`MapWorldViewAdapter.cs:334` → `MapRenderer.Texture.cs:309-318` | `anchor.influenceTier` 是 `[NonSerialized]`（`AnchorData.cs:31-32`），**不入档** | `SettlementInfluenceTests.cs:66-109` | 真 Dijkstra（地形/道路/河流成本 `:242-255`）；`tiersBySettlement` 无任何读取者；叠加层每帧重算全图 |
| 人口日 tick | 在跑 | `GameWorld.Simulation.cs:799` → `PopulationTick` `:53-109` | 无直接 UI；结果进全局事件（饥荒/叛乱） | `TileData.populationBlocks` 随 `SaveData.tiles` 原样序列化（`SaveData.cs:36`、`SaveSystem.cs:369/414`、`TileData.cs:100`） | `SaveSystemRoundTripTests` 未断言人口块 | 出生=种族成长率（`RaceData.cs:184-191`，**恒正**）；死亡只来自外部事件；**无迁移规则**（迁移只在死掉的废弃路径里） |
| 承载上限 | 在跑 | `GameWorld.Simulation.cs:66-72` → `CarryingCapacitySystem.cs:92-133` | 无 | 派生量，不存 | 无 | 只作为成长乘子（超载把成长率压到 0，永不为负）；粮食库存系数来自 `TradeCenter.inventory` |
| 人口统计 PopulationStats | 半跑 | `PopulationStats.cs:10/33/56/79/96/113` | `GetDominantFaith` 被 `MissionarySystem.cs:20,58`、`GameWorld.Simulation.cs:246` 使用 | 派生量 | 无 | `GetCultureShare/GetClassShare/DominantCulture/BuildShareText` **零消费者**（`SettlementQueryAdapter.cs:132-157` 自己另写了一份去重实现） |
| 阶层 / 职业 / 需求 | 在跑（政权级，非聚落级） | `PoliticsSimulationSystem.cs:137-140` → `SocialDifferentiation.DifferentiateRealm` `:109-196`（按 `socialClass` 重配人口块，写 economicClass/stratum/occupations `:226-247`）→ `SocietyManager.cs:87` `Needs.EvaluateRealm`（`ClassNeedsSystem.cs:38-76`）→ `ApplyClassRelations` `:157-169` | `SocietyPanel`（不在本域核） | `socialClass` 随人口块入档；`classRelations` 走 `RealmDTO` | `SocialClassBridgeTests.cs:88-140` | 需求维度（生计/安全/税负/政治通道/经济机会/制度承认/合法性/特权）真实计算，但**出口只有 unrest/loyalty**，不回写人口 |
| 城市区划 | 空转 | `CityDistrictSystem` 成员外部调用 = 0：`grep "CityDistrictSystem\."` = 0 命中；`GenerateDistricts`(`:9`)/`UpdateDistricts`(`:55`)/`CheckDistrictGeneration`(`:68`)/`GetDistrictEffects`(`:117`) 全无外部调用 | 无 | `districts`/`districtsGenerated` 入档（`SettlementPersistence.cs:118-128,297-311`） | `SettlementPersistenceTests.cs:185-187` | 只有读档能把区划灌回来；`settlementLevel < LevelIV` 门槛 + 等级永不变 ⇒ 实际永远不会生成 |
| 经济成分 | 半跑 | 初始化在跑：`AnchorGenerator.cs:140` → `EconomicCompositionSystem.cs:12-100`；更新零调用：`:102-170`(`UpdateComposition`) `grep` 0 命中 | 无 UI 显示（`GetCompositionDescription` `:230` 无消费者） | `economicSectors/economicRatios/primarySector` 入档（`SettlementPersistence.cs:114-117`） | `SettlementPersistenceTests.cs:173` | 生成时按地形/港口/首都算一次，此后冻结 |
| 聚落市场 / 合成 | 空转 | `GetOrCreateMarket`（`SettlementData.cs:339`）生产代码 1 命中=定义自身；`SettlementProduction.TryCraft` 8 命中全在测试 | 无 | `SettlementMarketSaveData` 会存（`SettlementPersistence.cs:130/317`），但运行时无实例可存 | `SettlementMarketTests`、`RecipeGateTests`、`DynamicGoodsTests` 全为单元测试 | 市场对象、配方门禁、容量校验齐备，游戏循环里没有生产者 |
| 城防（WallDefense） | 空转 | `WallDefense.Evaluate`（`WallDefense.cs:23`）13 命中全在 `WallDefenseTests`/`WallFortificationTests`；生产代码 0 | 无 | 城墙块本身入档（`SettlementBlocks`/`SettlementBlockSaveData`） | 13 处断言 | 城墙段、护城河、层数、材质都能算防御快照，但战斗结算不读它（`CombatManager` 里用 `StampedeSystem`，不用 `WallDefense`） |
| 建筑可用性 | 空转 | `BuildingAvailabilitySystem.CheckAvailability`（`:290`）唯一调用者是 `SettlementAISystem.cs:15-17`，而 `grep "SettlementAISystem\."` = 0 | 无 | — | 无 | 439 行地形/瓶颈/科技门禁表，整条链无生产调用 |
| 聚落 AI（建村/升级港/建渡口） | 空转 | `SettlementAISystem.cs:28/113/143`（ShouldBuildVillage/ShouldUpgradePort/ShouldBuildFerry）零调用者 | 无 | — | 无 | AI 永不主动建聚落；`AIIntentType` 只有 8 种且不含任何聚落处置（`AIIntentExecutor.cs:32-54`） |
| 行省（Province） | 半跑（缺模拟） | 生成在跑：`GameWorld.Generation.cs:385` → `ProvinceGenerator.cs:143-163`（Voronoi+Lloyd，写 `TileData.provinceId`） | 仅 UI 文本：`TileInfoQueryAdapter.cs:60-61` | `ProvinceSaveData` 往返（`SettlementPersistence.cs:29-67`） | `ProvinceGeneratorTests.cs:48-111`、`SaveSystemRoundTripTests.cs:172-174` | **纯地图分区**：4 个字段（id/name/center/memberTiles），无治理、无税、无征兵、无 tick |
| 聚落详情面板 | 在跑 | `GameManager.cs:438-442`（图标点击）→ `SettlementDetailPanel.cs:172-214` | 地图图标点击 `SettlementIconLayer.SettlementClicked` | 只读 | `SettlementDetailPanelPlayModeTests`、`UiDecouplingTests.cs:336` | 显示 11 个字段，其中"形态/等级"直接打印枚举 int 原值（`:212`） |
| 人口构成面板模型 / 聚落人物文本 | 空转 | `PopulationCompositionPanelModel.Build` 仅 `PopulationCompositionTests.cs:63-89`；`SettlementPeopleText.*` 生产代码 0 命中（`:Describe` 仅 `UiDecouplingTests.cs:283`） | 无面板引用 | — | 单元测试 | UI 模型层已写好，没有任何面板接线 |
| 玩家/单位建城 | 空转 | `UnitFounding.TryFoundSettlement`（`UnitFounding.cs:17`）1 命中=`UnitFoundingTests.cs:51` | 无 | 结果会入档 | 1 个失败路径测试 | `GameWorld.CreateCity/CreateWildernessSettlement`（`GameWorld.Simulation.cs:889/948`）无人调用，且**都不设 population** |
| 存档（聚落/行省） | 半跑 | `SaveSystem.cs:390-391`(Capture) / `:467-468`(Restore) → `SettlementPersistence.cs` | — | 见 §2「入档矩阵」 | `SettlementPersistenceTests`、`SaveSystemRoundTripTests` | 城墙/设施/市场/区划/人口/工事等级能往返；等级、聚焦、升级路径、废墟、控制、影响力全丢 |

## 2. 数据模型现状

### 2.1 `SettlementData`（`SettlementData.cs`，710 行）字段级判定

| 字段 | 写者 | 读者 | 判定 |
|---|---|---|---|
| `population`(:20 继承自 `HabitationSite`) | 生成 `AnchorGenerator.cs:63/79/109`；营地演化 `SettlementEvolutionSystem.cs:127`；战斗踩踏 `StampedeSystem.cs:157`（经 `CombatManager.cs:104/126`） | UI `SettlementQueryAdapter.cs:114`；`CanLevelUp`(:247，死)；`ComputeBudget`(`SettlementInfluenceSystem.cs:166`) | 有生产者但**与地块人口块完全脱钩**：地块人口每日复利增长，聚落人口不动，两套数永不收敛 |
| `development` / `wealth` / `tradePower` | 生成时随机；`StampedeSystem.cs:162`、`SettlementDestructionSystem.cs:44-46`（死） | `SettlementEconomy.cs:21`、`EconomicCompositionSystem.cs:111/124`、UI | **只减不增**：没有任何经济产出回写聚落财富 |
| `constructionTier` / `constructionProgress` | 生成=1；死路径降级；读档回填 | `CanLevelUp`(:241，死)、UI 副标题 | 玩家看到"等级 1"恒定不变 |
| `settlementLevel` | 生成 `AnchorGenerator.cs:146-150`（村/据点=LevelII，城=LevelIII）；营地演化 `SettlementEvolutionSystem.cs:78/88`；死路径 | 控制半径 `SettlementControlSystem.cs:212`、据点影响 `BarrierSystem.cs:230`、法令、UI | **永不变化**；且**不入档**（见 §2.4） |
| `composition`(:31 `PopulationComposition`) | **无** | **无** | `grep "PopulationComposition"` 10 命中：定义/测试/这一行；`PopulationGroup`(economicClass/stratum/occupations/religionTraditionId) 是整套空壳 |
| `isCapital`(:38) | **无生产者**（`grep "isCapital ="` 全是存档/测试） | `EconomicCompositionSystem.cs:50`、`DeriveCityFocus`(:278)、UI | 全图**没有首都**；首都加成永不生效 |
| `market`(:33 `[NonSerialized]`) | `GetOrCreateMarket`(:339，仅测试) | 存档捕获 | 运行时恒 `null` |
| `districts` / `districtsGenerated` | 仅读档 | `SettlementData.cs:593-604`(block 迁移) | 空 |
| `evolutionStage` / `settlementEvolution` / `settlementStability` | 各写一次（`:39`/`EvolutionSystem:135-136`） | **无** | 三个装饰字段，且不入档 |
| `ruinLevel`/`ruinedDay`/`preRuinLevel`/`preRuinForm`/`recoveryProgress`/`remnantCultureId` | 只有死路径 `SettlementDestructionSystem.cs:35-40` | `IsRuined`(:306)、`IsDamaged`(:320)、UI `isRuined` | 恒 0；`remnantCultureId = provinceId`（`:39`）本身是错的语义（用省 id 当文化 id） |
| `primaryFunction` / `secondaryFunctions` / `cityFocus` / `cityForm` / `portTier` / `bottleneckType` / `upgradePath` | 生成时 `DeriveInitialType` | `SettlementEvolutionTree.GetStageName`(UI 描述)、`DeriveCityFocus` | 全部**不入档**，读档后回落默认值 |
| `fortificationLevel`(:93) | 生成；`RecalculateFortificationLevelFromBlocks`(读档 `:316`) | `GetEffectiveFortificationLevel`(:450) | 唯一有"缓存→权威重算"设计的字段 |

### 2.2 `AnchorData`（`AnchorData.cs`，36 行）

- `controllerAnchorId`(:19) 有写者（`SettlementControlSystem.cs:152/202`），唯一读者是 `SettlementData.controllerAnchorId`(:176) 与法令传播；**不入档**。
- `controlledAnchorIds`(:22)/`controlProgress`(:25) 标 `[NonSerialized]`；读档后为空，靠每日 `ScanControlRelationships`(`:63-105`) 重算 ⇒ 控制进度每次读档清零。
- `influenceRadius`(:27)/`bottleneckTileIndex`(:34)：**零写者**，对应 `SettlementData` 属性(:212/:236)也零读者 ⇒ 死字段（控制半径实际用 `Mathf.Clamp((int)settlementLevel,1,5)`，`SettlementControlSystem.cs:210-213`）。
- `influenceTier`(:32) `[NonSerialized]`，由 `SettlementInfluenceSystem.cs:131` 在 **tick 与 UI 查询两条路径**都写（`MapWorldViewAdapter.cs:334` 是查询侧写模拟状态）。

### 2.3 `PopulationBlock` / `PopulationGroup` / `Province`

- 真实人口 = `TileData.populationBlocks`（`TileData.cs:100`，`List<PopulationBlock>`），字段 11 个（`PopulationBlock.cs`）。
- `profession`(:19)：唯一写者 `SocialDifferentiation.cs:243` 写死 `0`；无读者 ⇒ 死字段。
- `culturePenetration`(:29)：**零写者**；唯一读者 `GameWorld.Simulation.cs:122` `(pb.culturePenetration - 50f) * 0.005f` ⇒ 每个地块**恒定 -0.25/日 满意度惩罚**（默认值 0 被当成"远低于 50"）。
- `economicClass`/`stratum`/`occupations`：不是独立维度，是 `socialClass` 的查表结果（`SocialDifferentiation.cs:226-247` → `LegacyIdentityMapping`）。
- `Province`（`Province.cs:8-16`）：4 个字段，无人口、无税收、无行政、无归属政权；`IsBorder`(:18) 是唯一的逻辑函数。

### 2.4 入档矩阵（`SettlementPersistence.cs` + `HabitationSiteSaveData.cs`）

入档：`siteId/siteMode/siteName/location/anchorId/population/development/wealth/tradePower/fortification/garrison/isCapital/isPort/isCoastal/hasMarket/hasTemple/hasUniversity/constructionProgress/constructionTier/economicComposition/primarySector/economicInitialized/districts/blocks/market/facilities`，`SedentarySettlementSaveData.development_stage` 承接 `settlementLevel`（`CaptureModeData` `HabitationSiteSaveData.cs:99-108`）。

不入档（读档后靠默认值或重算）：

| 丢失项 | 后果 | 证据 |
|---|---|---|
| `settlementLevel` | **读档回落 LevelI(0)**：`RestoreSites` 只赋 `settlementForm`(`SettlementPersistence.cs:269`)，`ApplyModeData`(:154-155) 写的是 `developmentStage`，而 `settlementLevel` getter 读 `_legacySettlementLevel`(:290-304) —— 无人回填 | `SettlementPersistence.cs:259-321`；`SettlementData.cs:290-304` |
| `cityFocus/cityForm/portTier/bottleneckType/upgradePath/primaryFunction/secondaryFunctions/evolutionStage/isCoastal`（部分） | 读档后 UI 描述与 AI 派生失效 | 无 DTO 字段 |
| `ruinLevel/ruinedDay/preRuinLevel/recoveryProgress/remnantCultureId`、`settlementEvolution/settlementStability` | 废墟状态无法保存 | 无 DTO 字段 |
| `controllerAnchorId/controlledAnchorIds/controlProgress/influenceTier/isControlStable` | 控制关系与进度每次读档清零 | 无 DTO 字段 |
| 营地 `attributes` 之外的营地派生量 | — | `CaptureCampData`(:199-227) |
| `fortificationLevel` | **靠重算**：`RecalculateFortificationLevelFromBlocks`(`:435-447`)，读档 `SettlementPersistence.cs:316` | 设计成立 |
| `blocks` 缺失时 | **靠重算**：`EnsureBlocksFromLegacy`(`:527-605`)，读档 `SettlementPersistence.cs:314-315` | 设计成立 |

人口块本身**入档**（`SaveData.tiles` 直接序列化 `TileData[]`，`SaveData.cs:36`/`SaveSystem.cs:369`，`PopulationBlock` 是 `[Serializable]` 公开字段 struct）——但与 `SettlementData.population` 是两套数。

## 3. 内容数据现状

- `Assets/StreamingAssets/Base/` 顶层 20 个目录：`Biome / CharacterTemplate / Crafting / Culture / Dna / Equipment / EthnicGroup / Ethos / FamilyTradition / Goods / Innovation / Language / Laws / Localization / MentalHealth / Properties / Race / Religion / Title / Tradition`。**没有 Settlement / Province / Population / Building 目录** ⇒ 本域内容条目数 = 0。
- 聚落全部类型是 C# 枚举硬编码：`SettlementCategory`、`SiteMode`、`SettlementForm`、`SettlementLevel`、`CityForm`、`CityFocus`、`PortTier`、`BottleneckType`、`UpgradePath`、`EvolutionStage`、`EconomicSector`、`CityDistrictType`、`FacilityType`、`WallMaterial`、`OutpostKind`。mod 侧无法新增聚落类型/等级曲线/设施类型（`FacilityKeys.cs` 只做枚举↔字符串映射）。
- 硬编码参数集中在：破坏损失表 `SettlementDestructionSystem.cs:14-22`、升级人口门槛 `SettlementTypologySystem.cs:246`、演化门槛 `SettlementEvolutionSystem.cs:14-20`、承载基数 `CarryingCapacitySystem.cs:12-14`、建筑门禁 `BuildingAvailabilitySystem.cs:29-289`。
- 测试覆盖分布：**有**单函数覆盖（mode 转换/block 迁移/影响力成本/墙体快照/市场配方/存档往返）；**无**任何覆盖「成长推进 → 等级提升」「毁灭 → 恢复」「控制完成 → 税收变化」「读档后等级保持」的端到端测试。`SettlementPersistenceTests.cs:163-191` 断言了人口/财富/守军/建造等级/经济成分/城墙/设施/区划/市场，**未断言** `settlementLevel`、`cityFocus`、`ruinLevel`。

## 4. 缺口清单（按影响排序）

grep 证据汇总（命令均在 `MyriadCreation/Assets` 下、`-Filter *.cs` 递归）：

```
"CityDistrictSystem\."      total=0  prod=0
"SettlementAISystem\."      total=0  prod=0
"BarrierSystem\."           total=0  prod=0
"WallDefense\."             total=13 prod=0（全在 Tests）
"SettlementProduction\."    total=8  prod=0
"UnitFounding\."            total=1  prod=0
"SettlementPeopleText\."    total=1  prod=0
"UpdateConstructionProgress|CanLevelUp|PerformLevelUp" total=3 prod=3（全为定义）
"new DestroySettlementCommand("  total=0
```

| # | 缺口 | 影响 | 证据 | 严重度 |
|---|---|---|---|---|
| 1 | **成长段整段缺失**：村落→集镇→城邑→都会的等级/建设度推进无调用者 | 开局生成什么样，一千天后还是什么样；`constructionTier` 恒 1，`settlementLevel` 恒为生成值；`CanLevelUp` 的人口/城墙/路径门槛全部空转 | `SettlementTypologySystem.cs:52/221/297`（0 外部调用）；`:46-47` | P0 |
| 2 | **毁灭段无触发源** | 战争只打地块（`DiplomacyManager.RaidSettlement:902-974`），不毁聚落；`ruinLevel` 恒 0 ⇒ 恢复 tick 恒早退 ⇒ 废墟/流民/屠城/水淹玩法全不存在 | `SettlementDestructionSystem.cs:24,38,286`；`GameWorld.Core.cs:349`；`AIIntentExecutor.cs:32-54` | P0 |
| 3 | **废弃/重定居段无触发源** | 无"弃地/复耕"玩法；游牧弃地 `NomadicAbandon` 零调用 | `LandAbandonmentSystem.cs:25,154`；`NomadicRealmSystem.cs:120-122`；`SettlementControlSystem` 与该系统无交集 | P0 |
| 4 | **两套人口互不同步** | `TileData.populationBlocks` 每日复利增长；`SettlementData.population` 只在生成/踩踏时变。聚落 UI 的人口与地图人口是无关的两个数 | `GameWorld.Simulation.cs:73`；`AnchorGenerator.cs:63` vs `SettlementEvolutionSystem.cs:127`；`SettlementQueryAdapter.cs:114` | P0 |
| 5 | **人口无迁移、成长恒正** | `CalculatePopulationGrowthRate` 只返回正数（`:187-190`），死亡仅靠外部事件；`PopulationTick` 内无任何迁移/失业/挤出的分支 | `RaceData.cs:184-191`；`GameWorld.Simulation.cs:53-109`；迁移仅 `LandAbandonmentSystem.cs:104-141`（死路径） | P0 |
| 6 | **控制链无经济后果** | `GetTaxSiphonModifier`(0.9)/`GetTradeSiphonModifier`(0.8) 无任何调用者；`grep "settlements."` 全清单里没有税收/人力系统读聚落 ⇒ 控制一个村不产生任何收益 | `SettlementControlSystem.cs:224-240`；`SettlementControlRuntime.cs:45-53` | P1 |
| 7 | **控制/影响力状态不入档** | 读档后 `controllerAnchorId=-1`、`controlProgress` 清空、`influenceTier=0`；玩家建成一半的控制关系消失 | 无 DTO 字段（`HabitationSiteSaveData` 全字段已核）；`AnchorData.cs:21-32` | P1 |
| 8 | **`settlementLevel` 不入档** | LevelIII 的城读档变 LevelI：控制半径 3→1、据点影响半径缩小、UI 显示错、`CanLevelUp`（若将来接上）基准错 | `SettlementPersistence.cs:269`（只赋 form）；`SettlementData.cs:290-304` | P1 |
| 9 | **`isCapital` 无生产者** | 首都不存在 ⇒ 首都经济加成、`CityFocus.Administrative`、UI"首都：否"永久为假 | `EconomicCompositionSystem.cs:50-56`；`SettlementTypologySystem.cs:278`；`grep "isCapital ="` 只在存档/测试 | P1 |
| 10 | **城市区划 / 经济成分更新零调用** | 区划永不生成（且门槛需 LevelIV，等级永不变）；经济成分生成后冻结，`primarySector` 终生不变 | `CityDistrictSystem.cs:9,55,68`（0 调用）；`EconomicCompositionSystem.cs:102`（0 调用） | P1 |
| 11 | **聚落市场/合成零生产者** | `market` 恒 null；`SettlementProduction.TryCraft` 只在测试里跑；聚落"经济"对玩法不可见 | `SettlementData.cs:339`；`SettlementProduction.cs:12-23` | P1 |
| 12 | **城防只装饰** | `WallDefense.Evaluate` 零生产调用；城墙等级/护城河/材质不影响任何战斗结算 | `WallDefense.cs:23`；13 命中全在测试 | P1 |
| 13 | **建筑可用性 + 聚落 AI 整链空转** | AI 永不建村/升级港/建渡口；`ShouldBuildVillage` 的承载力/贸易流/肥力判据永不评估 | `SettlementAISystem.cs:28,113,143`；`BuildingAvailabilitySystem.cs:290` | P1 |
| 14 | **行省无玩法** | `Province` 仅 4 字段，无治理/税率/征兵/首府；UI 只显示名字 | `Province.cs:8-16`；`TileInfoQueryAdapter.cs:60-61` | P1 |
| 15 | **玩家建城路径不可达且不设人口** | `CreateCity`/`CreateWildernessSettlement` 无人调用，且都不写 `population`（生成=0）⇒ 若接上，新城市人口为 0 且永不增长 | `GameWorld.Simulation.cs:889-1006`；`UnitFounding.cs:17` | P1 |
| 16 | **'承诺未兑现'字段 6 个** | `AnchorData.influenceRadius`、`AnchorData.bottleneckTileIndex`、`PopulationBlock.profession`、`SettlementData.composition`、`settlementEvolution`、`settlementStability` | 见 §2.1/§2.2 | P2 |
| 17 | **`culturePenetration` 无写者导致恒定满意度惩罚** | 每块每日 -0.25 满意度，来源是一个从未被赋值的字段 | `PopulationBlock.cs:29`；`GameWorld.Simulation.cs:122` | P2 |
| 18 | **UI 打印枚举 int 原值** | 详情面板显示"形态：1｜等级：2"而非"城/城邑" | `SettlementDetailPanel.cs:212`（`settlementForm`/`settlementLevel` 是 `int`，`UiQueryContracts.cs:90-96`） | P2 |
| 19 | **人口构成/聚落人物 UI 模型零接线** | `PopulationCompositionPanelModel`、`SettlementPeopleText` 无面板引用 | `PopulationCompositionTests.cs:63-89`；`UiDecouplingTests.cs:283` | P2 |
| 20 | **影响力叠加每帧全图重算** | `MapRenderer.Texture.cs:309-318` 每帧判 `_cachedInfluenceFrame != frame` 后调 `MapWorldViewAdapter.cs:334`，每次对全部主要聚落跑一遍 Dijkstra；且该查询路径**写** `anchor.influenceTier` | `MapRenderer.Texture.cs:306-318`；`SettlementInfluenceSystem.cs:131` | P2 |

## 5. 我核实不了的事

1. **未运行 Unity / 测试**。§1 的"测试"列只说明存在哪些测试文件与断言，不说明它们当前是否通过。
2. **影响力叠加是否真的在游戏里被绘制**：`DrawInfluenceRadiusOverlay` 需要 `CurrentOverlayView` 与政治模式开关，绑定关系在场景/prefab 里，未核。
3. **反射/字符串派发**：`SettlementAISystem`、`UnitFounding`、`DestroySettlementCommand` 等的"零调用者"结论基于类型/成员名 grep；无法排除反射、`UnityEvent` 序列化绑定或 mod 侧调用。
4. **`SettlementControlSystem` 与 `DecreePropagationSystem` 的实际耦合强度**：只确认了法令系统读 `controlledAnchorIds/controllerAnchorId`（`:68-74`），未核法令 tick 是否真在玩家政权上触发。
5. **`CombatManager` 是否在别处消费 `SettlementData`（守军/城防）**：只确认了 `StampedeSystem` 两处调用点，战斗全流程未逐行审计（属战斗域）。
6. **Obsolete 的 `GameSaveSystem`（`GameSaveSystem.cs:39-40`）是否仍被任何调试菜单调用**：只确认 `GameManager.cs:279/287` 用的是 `SaveSystem`。
7. **`HabitationSiteMigration.cs`(78 行)** 仅读完签名与用途注释，未逐个校核旧档迁移分支。
