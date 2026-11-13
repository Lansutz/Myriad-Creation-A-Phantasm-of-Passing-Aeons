# 文化 / 信仰 / 宗教 / 传统 / 语言 / 思潮 / 族群 审计（2026-11-09）

## 0. 范围

**已读（逐文件，含行数）**
- `Simulation/MyriadCreation.Simulation.Culture/`（7 个）：`State/CultureData.cs` 196、`State/CultureStageDef.cs` 32、`Definitions/CultureStageCatalog.cs` 299、`Runtime/CultureSimilarity.cs` 193、`Runtime/CultureStageEvolutionSystem.cs` 215、`Runtime/CultureTransition.cs` 54、`StageEvolutionResult.cs` 12
- `Simulation/MyriadCreation.Simulation.CultureHost/`（7 个）：`CultureSchedule.cs` 26、`CultureStageEvolutionHost.cs` 164、`CultureContextAdapter.cs` 116、`CultureContracts.cs` 106、`CultureLineage.cs` 67、`ContentLookupAdapter.cs` 66、`CultureTransitionHost.cs` 57
- `Simulation/MyriadCreation.Simulation.Faith/`（2 个）：`Runtime/FaithSystem.cs` 172、`State/Doctrine.cs` 17
- `Simulation/MyriadCreation.Simulation.FaithHost/`（7 个）：`FaithContracts.cs` 311、`CanonizationSystem.cs` 97、`GreatHolyWarSystem.cs` 106、`GreatHolyWarState.cs`、`FaithContextAdapter.cs` 87、`ReligionSimulationEventHandler.cs` 51、`WarfareReligionSchedule.cs` 60
- `Simulation/.../Systems/`：`ThoughtManager.cs` 181、`ThoughtSchedule.cs` 26、`MissionarySystem.cs` 108、`SocialClassAvailability.cs` 107、`DiscoveryActors.cs` 68、`DiscoveryActorGroups.cs` 12
- `Simulation/.../State/`：`SchoolOfThought.cs` 65、`IdeologyMovement.cs` 80、`InnovationDiscovery.cs` 35、`DiscoveryGroups.cs` 17
- `Content/.../Definitions/Faith/`：`ReligionDef.cs` 76、`ReligionCatalog.cs` 307、`ReligionNodeType.cs`、`ReligionMapLevel.cs`；`TraditionFactorKey.cs`、`TraditionCategory.cs`、`TraditionElevation.cs`、`MovementPhase.cs`
- `Content/.../Modding/`：`TraditionDef.cs` 93、`LanguageDef.cs` 52、`EthnicGroupDef.cs` 42、`EthosDef.cs` 20、`HeritageDef.cs`、`LanguageFamilyCatalog.cs` 176、`LanguageFamilyNode.cs`、`ContentDefinitionIndex.cs`（load 段）
- `Core/.../Contracts/`：`CultureContracts.cs` 45、`FaithContracts.cs`、`IContentLookup.cs`
- `UI`：`Presentation/ReligionPanelText.cs` 70、`Panels/SocietyPanel.cs` 51、`Common/UIManager.Panels.cs`（相关段）
- `Infrastructure/Save`：`FaithPersistence.cs` 71、`FaithSaveData.cs` 189、`SaveData.cs` 78、`SaveSystem.cs`（相关行）
- 测试：`CultureStageEvolutionDataTests.cs` 179、`FaithPersistenceTests.cs` 230、`FaithQueryExtensionTests.cs` 203、`LanguageFamilyTests.cs` 140、`TitleLanguageDataTests.cs` 63
- 内容：`StreamingAssets/Base/{Culture,Religion,Tradition,Ethos,Language,EthnicGroup}/*`（JSON 解析计数见 §3）
- 文档对照：`docs/PLAN_INDEX.md:48-55`、`docs/design/FAITH_AS_CULTURE_MODEL.md`

**未覆盖**
- 未启动 Unity，无运行时观测；全部为静态判定（grep + 读码 + JSON 解析）。
- 未读 `InnovationTree.cs` 全文（只读 480-520 段）、`GameWorld.Core.cs` 全文（只读 930-995）、`MapRenderer.Colors.cs`（只读 grep 行）。
- `Simulation/MyriadCreation.Simulation.Generation` 中除 `PlaceNameGenerator.cs` 外的生成器未读。
- `DiscoveryActors` 经核实属「革新发现层」而非文化扩散（`DiscoveryActorGroups` = Merchant/Artisan/Scholar），仅记 1 条零调用。

## 1. 系统清单

| 系统 | 三态 | 驱动证据 | 入口证据 | 存档证据 | 测试 | 一句话现状 |
|---|---|---|---|---|---|---|
| 文化阶段演化（`CultureStageEvolutionSystem`+`Host`+`CultureSchedule`） | **在跑** | `GameWorld.Simulation.cs:812` 注册 monthly；`:854-860` 每 30 天调 `CultureStageEvolutionHost.MonthlyTick(this)` | 无文化面板；仅角色详情显示文化名 `UIManager.Panels.cs:552` | ✅ `SaveData.cs:38` `List<CultureData> cultures`；`SaveSystem.cs:383` 捕获、`:424` 还原 | `CultureStageEvolutionDataTests.cs`（只测 `EvaluateEvolution`+目录） | 唯一真在跑的文化机制：按政权人口/革新/分层/专业化逐级升阶，第 5 级（HighCivilization）停在门口不再动 |
| 阶段回退（`CheckRegress`） | **空转** | `CultureStageEvolutionSystem.cs:170` 定义 | — | — | 0 | 全库 0 调用者（grep `CheckRegress` 命中 2，另 1 为注释）→ 文化阶段只能升不能降 |
| 七维相似度（`CultureSimilarity.CalculateSim`） | **空转** | `CultureSimilarity.cs:24-27` 读 7 个 `[Obsolete]` 字段 | — | — | 0 | 只被 `CultureTransition` 调用，而后者 0 外部调用者 |
| 传统因子聚合（`GetFactorSum`/`GetSettlementDegree`/`GetSeparationTendency`/`GetFusionTendency`） | **空转** | `CultureData.cs:125-190` | — | — | 0 | 两重死：调用链死（见上）＋内容侧 `factors` 全空（§3） |
| 分离/融合阻力（`CultureTransition`+`CultureTransitionHost`） | **空转** | `CultureTransition.cs:14/25/30/41`、`CultureTransitionHost.cs:14` 全部定义 | — | — | 0 | 5 个公开方法 0 外部调用者；「文化分裂/融合/同化」在代码里没有任何执行点 |
| 传统挂靠→阶层可用性 | **在跑（窄）** | `SocialClassAvailability.cs:44` → `CultureData.HasTradition` → `ContentLookupAdapter.cs:34` → `EthnicGroups.json` | 间接：阶层面板 `ClassPanelText` | 随 `cultures`/`populationBlocks` 入档 | 0 | 全库唯一一条「传统内容改变可观察量」的活链：`trad_craft_guild` → Artisan 副类可用 → 阶层涌现/分层度/政权情势 |
| 文化→革新研究加成（`GetEffectiveResearchRate`） | **空转** | `InnovationTree.cs:490` 定义 | — | — | 0 | 0 调用者 → `CultureData.innovationAffinities`（Laethis 有 3 条）不产生任何效果 |
| 信仰运行态（`FaithSystem`+`fervor`） | **半跑** | `GameWorld.Simulation.cs:814-817` → `UpdateFaithFervor:370-385`（30 天闸 + 第 360 天 −10 热忱） | `ReligionPanelText` 显示热忱 | ✅ `SaveSystem.cs:400/501` + `FaithSaveData.cs` | `FaithPersistenceTests`、`FaithQueryExtensionTests` | 热忱**只降不升**：唯一加值路径 `ReligionSimulationEventHandler.cs:28-29` 依赖 `OnWarBetweenFaiths`，而后者的前置 `stateReligionId>=0` 在生产中永不成立 |
| 传教（`MissionarySystem`+`MissionaryTick`） | **空转（恒早退）** | `GameWorld.Simulation.cs:834` 挂在 `AISchedule`；`:214-255` 实现 | — | 结果落在 `populationBlocks[].faithId`（随地块入档） | 0 | `:226-237` 收集 `stateReligionId>=0` 的政权，字典恒空 → 无条件 return。「注册 ≠ 有效」的标准样本 |
| 宗教树（`ReligionCatalog`） | **半跑** | `ContentDefinitionIndex.cs:418-419` 加载 44 节点 | 地图着色 `MapRenderer.Colors.cs:284` | 内容表不存档（读档重建） | `FaithPersistenceTests` 间接 | 加载在跑；**分裂/挂靠 7 个 API 全 0 调用**（`CreateTradition:53`/`CreateRite:70`/`CreateSuccession:85`/`CreateReligion:108`/`GetDivergence:126`/`IsAvailable:174`/`ResolveTraditionId:199`） |
| 大圣战（`GreatHolyWarSystem`） | **空转** | `GameWorld.Simulation.cs:410-433` 遍历 `ActiveWars` | 无 | `GreatHolyWarState` 无 DTO | 0 | `DeclareGreatHolyWar:1110` 0 调用；且 `CanDeclareGreatHolyWar` 恒 false（`highPriestCharacterId` 无生产者，只 `GameWorld.Core.cs:969` 置 −1）→ `ActiveWars` 恒空链 |
| 封圣（`CanonizationSystem`） | **空转** | `Canonize:49` 定义 | `FaithContracts.GetSaints:129` 有读取方但恒空 | ❌ `_saintsByFaith` 是 `static` 字典，无 DTO | `FaithQueryExtensionTests` 仅测空返回 | 0 调用者 + `:55` 要求 `highPriestCharacterId>=0`（恒 −1）→ 双保险恒 null |
| 圣地（`CreateHolySite`/`CheckHolySiteLoss`） | **空转** | `GameWorld.Simulation.cs:1008` 定义；`:387-408` 遍历 | `ReligionPanelText:47` 显示圣地数（恒 0） | 随 `faiths` 入档（恒空表） | 0 | `CreateHolySite` 0 调用 → `holySiteTileIndices` 恒空 → `CheckHolySiteLoss` 是空循环 |
| 思潮（`ThoughtManager`/`SchoolOfThought`/`IdeologyMovement`） | **空转** | `GameWorld.Simulation.cs:826-829` 注册 `thought.daily` 并调 `_thoughtManager.DailyTick` | 无 | ❌ 无 DTO；且字段全 `private`+`Dictionary`，JsonUtility 不序列化 | 0（全库 0 处引用） | 4 个容器（`_schools`/`_faiths`/`_judicialSystems`/`_movements`）恒空，`CreateSchool:30`/`CreateFaith:43`/`CreateJudicialSystem:55`/`CreateMovement:67` 全 0 调用，`GetThoughtManager`（`GameWorld.Queries.cs:252`）也 0 调用 |
| 思潮 5 阶段生命周期 | **不存在（有实现无实例）** | `IdeologyMovement.cs:47-78` Emerging→Growing→Peak→Declining→Extinct 完整实现；`MovementPhase.cs:3-10` 5 值 | — | — | 0 | 逻辑写完了，但没有任何代码创建 `IdeologyMovement` → 5 阶段永不发生 |
| 语言→人物命名 | **在跑** | `ContentDefinitionIndex.cs:342-345` 把 CSV 打进 `LanguageDef` 名池 → `ContentQueries.cs:9-49` → `CharacterManager.cs:971` | 人名出现在角色面板 | 随角色入档 | `TitleLanguageDataTests` | 命名实际走 `Culture/Laethis/*.csv`（同一批 CSV 也在 `Language/Laethis/`） |
| 语言→地名合成（`PlaceNameGenerator.Generate`） | **空转** | `PlaceNameGenerator.cs:102-119` 需 `terrainWords`+`placeSuffixes` | — | — | `PlaceNameGeneratorTests` 用假语言对象 | `Language.json` 两个字段都没有 → 对 Laethis 恒返回 `""`；`FounderCity`（`:121-133`）同样缺 `placeSuffixes["city"]` → `GameWorld.Simulation.cs:918-921`/`977` 恒落硬编码「新市镇」/「新拓居地」 |
| 语系（`CultureLineage`+`LanguageFamilyCatalog`） | **半跑** | `LanguageFamilyCatalog.Initialize`（`ContentRegistry.cs:113`，加载 `_generated/language-families.json` 12.9KB）；`CultureLineage.Apply`（`GameWorld.Core.cs:536`） | — | 随 `cultures` 入档 | `LanguageFamilyTests` | `Apply` 能跑但 `Language.json` 无 `familyId`/`parentLanguageId` → 写入 0 次；且 `CultureData.languageFamilyId`（`:57`）**零读取者** |
| 族群（`EthnicGroups.json`→`EthnicGroupDef`） | **半跑** | `ContentResolvers.EthnicGroups` 仅被 `ContentLookupAdapter.cs:37` 消费 | 间接（阶层） | ❌ 无族群实体，不入档 | `CultureStageEvolutionDataTests` 提及 | 1 个族群只有 `traditionIds` 起作用；`ethosId`/`languageId`/`innovationIds` 零读取 |
| Ethos（`EthosDef`） | **空转** | `ContentDefinitionIndex.cs:201-208/302-308` 加载 8 条 | 无 | 无 | 0 | `ContentResolvers.Ethos`（`ContentResolvers.cs:19`）无消费者；`EthosDef.effects` 0 读取；`CultureData.ethosId` 恒 `""` |
| 宗教面板 | **在跑（退化）** | `UIManager.Panels.cs:683-688` | `Assets/Scenes/Main.unity` 已挂 `religionPanelText` | — | `UiDecouplingTests` | 因 `stateReligionId` 恒 −1，永远走 `ReligionPanelText.cs:26-31` 分支 → 玩家只看得到「当前政权未确立国教」 |
| 社会面板（`SocietyPanelText`） | **不存在（本域零字段）** | `Panels/SocietyPanel.cs:14-43` 只有政权头+官职+阶层 | `Main.unity` 已挂 | — | `UiDecouplingTests` | 文化/信仰/族群在该面板 0 字段；`:9` 注释自称「暂由 SocietyPanelLegacy 的旧重载负责」——**该类全库不存在** |

## 2. 数据模型现状

**文化：七维仍在供活消费者，新五链字段全空**

- 七维字段全部带 `[Obsolete]`：`CultureData.cs:20-51`（`livelihoodType`/`mobilityType`/`burialType`/`burialTypes`/`worshipVector`/`materialStyle`/`symbolicFocus`/`symbolicFoci`/`environmentAdapt`/`environmentAdapts`/`productionPreference`）。
- 但仍有 **2 处活消费**：`CultureStageEvolutionHost.cs:47` 读 `mobilityType` 决定 `RealmForm`（游牧/定居）；`SocialClassAvailability.cs:20` 读 `livelihoodType != 2` 决定 `Freeholder` 副类可用性。→ 「已废弃」的字段实际仍是文化差异化的唯一活输入。
- 新增链接字段全部**无生产者**：`ethnicGroupId`（`:59`）、`ethosId`（`:63`）、`heritageId`（`:61`）无写入无读取；`traditionIds`（`:65`）内容侧 0 条（唯一文化没写）；`languageFamilyId`（`:57`）1 写（`CultureLineage.cs:43`）0 读。
- `CultureData.faithId`（`:53`）唯一定义，唯一内容值 `-1`（`CultureData.json`），无读取者。

**信仰：宗教运行态齐备但归附面完全无生产者**

- `FaithSystem.cs:45` `regionAdherence`、`:50` `faithRelations` 均 `[NonSerialized]` 且**生产侧零写入**（只有 `FaithSaveData.cs:155/165` 读档回填）；测试手工填（`FaithPersistenceTests.cs:92-96`）。
- 连带：`CalculateReligiousAuthority`（`:157-165`）恒等于 `churchInfluence*0.4 + followers*0.3`；`churchWealth`（`:34`）除存档外无读者；`missionaryZeal`（`:40`）/`tolerance`（`:42`）0 读者——`MissionarySystem.cs:23/33/35` 用的是写死 0.6/0.6/0.2。
- `FaithSystem.DailyTick`（`:167-170`，churchInfluence 向 50 收敛）只被 `ThoughtManager.cs:88` 调用，而 `ThoughtManager._faiths` 恒空 → 世界里的 22 个教会 Influence 永不动。

**宗教层级：文档的「宗教→中间层→传统」在代码里是三合一的 `nodeType`**

- `ReligionNodeType.cs:3-11` 5 值（Religion/Succession/Tradition/School/ReligiousSchool）共用 `ReligionDef` 一个类；`ReligionMapLevel.cs:3-8` 只认前 3 个。
- **无 `MiddleLayerDef`**：全库 grep `MiddleLayer|middleLayer` = 0 命中；文档（`FAITH_AS_CULTURE_MODEL.md:53-69`）要求的中间层、`PopulationTraditionComposition`、`CharacterData.traditionIds`、`ReligionDef.traditionId` 均不存在。
- **运行时主单位是 Succession，不是 Tradition**：`GameWorld.Core.cs:959-975` 只为 `nodeType == Succession` 造 `FaithSystem`（44 节点里 22 个）；8 个 Tradition 节点没有任何运行时对象。
- **传统不挂在宗教下**：`TraditionDef` 是文化侧类型（`Content.Modding`），字段仅 `traditionId/effects/factors/requiredInnovations/exclusiveToCultureIds/upgradesFrom/category/minRole/solidification/linkedInnovationIds`（`TraditionDef.cs:11-31`）——无 `religionId`、无 `middleLayerId`、无 `parentTraditionId`。即文档三条硬口径第 1 条（传统为主单位）与挂靠强约束，代码都不支持。
- **宗教身份可空**：`RealmData.cs:24` `stateReligionId = -1`（且无生产者）；`CultureData.cs:53` `faithId = -1`（唯一内容值即 −1）；`CharacterData.cs:53` `faithId` 是无默认值的 `int`，初始统治者硬编码传 `0`（`CharacterManager.cs:986/991`），而宗教 id 从 100 起（`Religions.json` 首条 100）→ 角色信仰是一个悬空 id。
- 分裂/挂靠：`ReligionCatalog.CreateSuccession/CreateTradition` 等 7 个 API 全 0 调用（§1）→ 运行时树结构不可变。

**思潮：数据模型存在但不可达**

- `ThoughtManager.cs:14-28` 4 个容器全 `private readonly`；`[Serializable]`（`:11`）对 `Dictionary` 字段无效，JsonUtility 不会写 → 无存档路径。
- `SchoolOfThought.cs` 全部字段（`:12-48` 含 `regionPenetration`/`schoolRelations` 两个 `[NonSerialized]` 字典）无生产者；`IdeologyMovement.cs` 同理。

**ID 命名空间混用（本域两处，均有 `file:line`）**

- `MissionarySystem.ConvertTile(TileData, int missionaryFaithId, int cultureId, ...)`（`MissionarySystem.cs:38`）第 3 参语义是 `cultureId`，调用处传的是 `ownerRealmId`（`GameWorld.Simulation.cs:251`）→ 转化出的 `PopulationBlock.cultureId` 被写成政权 id（`:70`）。
- `CreateInitialRulers` 把 `realmId` 当 `cultureId` 传入（`CharacterManager.cs:982/986/991`：`GenerateName(realmId, …)`、`CreateCharacter(…, realmId, raceId, 0, …)`）。

## 3. 内容数据现状

解析命令：`Get-Content <json> -Raw | ConvertFrom-Json` 后读数组长度（`StreamingAssets/Base` 下）。

| 目录 | 条目 | 被代码可达的比例 | 证据 |
|---|---|---|---|
| `Culture/` | 1 个包（`Laethis`）+ `CultureStages.json` 5 阶段 | 1/1 被 `GameWorld.Core.cs:937-941` 载入 | `CultureData.json` 含七维（`worshipVector` 7 元）但**不含** `traditionIds`/`ethnicGroupId`/`ethosId`/`languageFamilyId`/`heritageId`；`faithId=-1`；`stage=2`(Chiefdom) |
| `Religion/Religions.json` | **44** 节点 | **22/44 = 50%** 造出 `FaithSystem`（仅 Succession） | `GameWorld.Core.cs:962`；分布：Religion 7 / Succession 22 / **Tradition 8** / School 6 / ReligiousSchool 1 |
| `Religion/Doctrines.json` | **46** 条 | 12/44 节点有 `selectedDoctrines` → 显示面可达（`FaithContracts.cs:167-186`） | `isMainstreamTradition` true = **0/44**（`ResolveTraditionId:215` 的偏好分支永不触发，且该函数本身 0 调用） |
| `Tradition/Traditions.json` | **13** 条 | 生效 **1/13 = 7.7%**（仅 `trad_craft_guild`，经 Artisan 门槛） | **13/13 条的 `factors` 为空数组** → `GetFactorSum` 恒 0；27 条 `effects` 0 消费者；`exclusiveToCultureIds` 仅 `trad_valley_elite=[1]`，读取方 `CanCultureHold` 0 调用 |
| `Ethos/Ethos.json` | **8** 条 | **0/8 = 0%** | 只有 loader（`ContentDefinitionIndex.cs:302-308`），`EthosDef.effects` 无消费者 |
| `Language/Laethis/Language.json` | **1** 条（`laethis_lang`） | 名池 100%（CSV 打包，`ContentDefinitionIndex.cs:342-345`）；地名合成 0%（无 `placeSuffixes`/`terrainWords`） | `Language.json` 只有 `languageId` + 5 条 `sampleWords`（`sampleWords` 亦 0 消费者） |
| `EthnicGroup/EthnicGroups.json` | **1** 条（`ethnos_laethis`, `cultureId:1`） | `traditionIds` 4 条中 1 条生效；`ethosId`/`languageId`/`innovationIds` 0 读取 | `ContentLookupAdapter.cs:37-52` |

补充：`Culture/Laethis/*.csv` 与 `Language/Laethis/*.csv` 内容重复（4 个文件同名同尺寸），同一批名池被两条装载路径各读一次。

## 4. 缺口清单（按影响排序）

| # | 缺口 | 影响 | 证据 | 严重度 |
|---|---|---|---|---|
| 1 | `stateReligionId` 在生产中**从无写入**（唯一写入点 `FaithContracts.cs:280-296` 只被测试调用） | 单点掐断 5 条链：传教恒早退、热忱只降不升、大圣战不可宣、圣地失陷不可触发、宗教面板恒显示「无国教」 | `RealmData.cs:24`；`GameWorld.Simulation.cs:229/234-237/1055`；`ReligionPanelText.cs:26`；grep `SetStateReligion` 生产命中 0（命中全在 `SystemContractsTests.cs:232/237`） | **P0** |
| 2 | 文化分裂/融合/同化/传播全链 0 调用 | 文化除「阶段」与「地图颜色」外不产生任何可观察量；问题 1「有没有真实消费者改变可观察量」的答案是：只有阶层门槛一条窄链 | `CultureTransition.cs:14/25/30/41`；`CultureSimilarity.cs:170/177/186`；`CultureTransitionHost.cs:14`；`CultureStageEvolutionSystem.cs:170` | **P0** |
| 3 | `Traditions.json` 13/13 条 `factors` 为空 | 即使把链路接上，`GetSettlementDegree`/`GetSeparationTendency`/`GetFusionTendency`/`CalculateFactorSimilarity` 仍恒 0（后者恒返回 1.0：`CultureSimilarity.cs:69-70` 传 `content=null` → `CultureData.cs:127-130` 直接 `return 0f`） | `Traditions.json` 解析：`factors` 非空计数 = 0 | **P0** |
| 4 | `highPriestCharacterId` 无生产者 | 封圣（`CanonizationSystem.cs:55`）与大圣战（`FaithSystem.cs:92`）永久不可用 | grep `highPriestCharacterId\s*=` 生产命中仅 `GameWorld.Core.cs:969`（初始化 −1） | **P0** |
| 5 | 思潮 4 容器恒空、无生产者、不入档 | 「思潮 5 阶段生命周期」有实现无实例；玩家与 AI 都无法产生思潮 | `ThoughtManager.cs:14-28/30/43/55/67`；`GameWorld.Queries.cs:252` 0 调用；`SaveData.cs` 无 thought 字段 | **P1** |
| 6 | 宗教分裂/挂靠 API 全 0 调用 | `PLAN_INDEX.md:50-55` 的「只借分裂+挂靠」在代码里 0 落地；宗教树运行时不可变 | `ReligionCatalog.cs:53/70/85/108/126/174/199` | **P1** |
| 7 | 运行时主单位是 Succession 而非 Tradition；`TraditionDef` 无 `religionId`/`middleLayerId`/`parentTraditionId`；无中间层类型 | 代码实现的是 CK3 式 Religion→Succession 两级 + 文化侧平铺传统，与文档三层层级不是同一套 | `GameWorld.Core.cs:959-975`（只 Succession）；`ReligionNodeType.cs`；`TraditionDef.cs:11-31`；grep `MiddleLayer` = 0 命中 | **P1** |
| 8 | 圣地 0 生产者 | `holySiteTileIndices` 恒空 → `CheckHolySiteLoss` 空循环、面板圣地数恒 0、`FaithSaveData` 该字段恒空数组 | `GameWorld.Simulation.cs:1008` 0 调用；`:387-408` | **P1** |
| 9 | 文化新五链字段零内容零消费 | `ethnicGroupId`/`ethosId`/`heritageId`/`traditionIds`/`languageFamilyId` 全空或只有写没有读 → 文化无法与族群/思潮/传承/语系建立任何关系 | `CultureData.cs:57-65`；`CultureData.json` 字段清单 | **P1** |
| 10 | ID 命名空间混用（文化/政权/信仰） | 传教产出的人口块 `cultureId` 被写成政权 id；初始统治者 `cultureId=realmId`、`faithId=0`（悬空） | `GameWorld.Simulation.cs:251` vs `MissionarySystem.cs:38/70`；`CharacterManager.cs:982/986/991` | **P1** |
| 11 | `populationBlocks[].cultureId` 无消费者 | 人口文化分组不驱动任何机制（叛乱/同化/征税皆不读） | `PopulationStats.cs:10` `GetCultureShare`、`:79` `GetDominantCulture` 均 0 调用；`MissionarySystem` 只用 `GetDominantFaith` | **P1** |
| 12 | `ReligionDef` 34/44 条有 `liturgicalLanguage`/`scripturalLanguage`，`worldview`/`founder`/`headName`/`communionName` 亦有内容 | 全部 0 读取 → 语言/礼仪/世界观维度不进入模拟 | grep：`liturgicalLanguage`/`scripturalLanguage` 命中仅 `ReligionDef.cs`；`worldview`/`headName`/`communionName` 只出现在 `ReligionCatalog` 的死创建函数与 `GetColor` | **P2** |
| 13 | `TraditionDef.effects`（27 条）、`solidification`/`upgradesFrom`/`linkedInnovationIds` | 0 读取；`category` 内容里未填 → 全部默认 `Production`，`RoleGateFulfilled` 恒 true | `TraditionDef.cs:13/21/29/31/84-91`；grep `upgradesFrom`/`linkedInnovationIds` 命中仅定义 | **P2** |
| 14 | `EthosDef.effects` 8 条 | 0 读取 | §3 表 | **P2** |
| 15 | `PlaceNameGenerator.Generate` 对唯一语言恒 `""` | 地形+后缀合成地名的能力等于不存在；建城名走硬编码兜底 | `PlaceNameGenerator.cs:108-113`；`Language.json` 无 `placeSuffixes`/`terrainWords` | **P2** |
| 16 | 面板层：`SocietyPanelText` 本域零字段；`ReligionPanelText.cs:10`/`SocietyPanel.cs:9` 注释引用的 "Legacy 旧重载"/`SocietyPanelLegacy` 全库不存在 | 文化无面板；宗教面板信息量 = 名字+热忱+圣地数+主保（主保实参恒 −1，`UIManager.Panels.cs:687`） | grep `SocietyPanelLegacy` = 0；`partial class ReligionPanelText` 仅 1 个 part | **P2** |
| 17 | `FaithSystem` 余字段无消费者：`churchWealth`/`missionaryZeal`/`tolerance`/`faithRelations`；`TaxPressureConversion` 0 调用 | 教会经济、传教强度、宽容度、教际关系四维不参与模拟 | `FaithSystem.cs:34/40/42/50`；`MissionarySystem.cs:80` | **P2** |
| 18 | 阶段回退 `CheckRegress` 0 调用 | 文化只能升阶，无法因失去城市/分层崩塌而降级 | `CultureStageEvolutionSystem.cs:170` | **P2** |

**零调用者清单（grep 证据汇总，均在本域内）**

`CultureSimilarity.CalculateSeparationResistance:170` / `CalculateFusionResistance:177` / `CalculateSpreadRate:186`；`CultureTransition.GetIndividualResistance:14` / `CanCharacterAdopt:25` / `GetGroupResistance:30` / `CanCultureMerge:41`；`CultureTransitionHost.ListTraditionCandidates:14`；`CultureStageEvolutionSystem.CheckRegress:170`；`ReligionCatalog.CreateTradition:53` / `CreateRite:70` / `CreateSuccession:85` / `CreateReligion:108` / `GetDivergence:126` / `IsAvailable:174` / `ResolveTraditionId:199`；`CanonizationSystem.Canonize:49` / `IsCanonizationCandidate:32`；`GreatHolyWarSystem.Rally:62` / `OnLinkedWarEnded:53`；`GameWorld.DeclareGreatHolyWar:1110` / `CreateHolySite:1008`；`MissionarySystem.TaxPressureConversion:80`；`ThoughtManager.CreateSchool:30` / `CreateFaith:43` / `CreateJudicialSystem:55` / `CreateMovement:67` / `GetAllSchools:136` / `GetAllMovements:146` / `GetDominantFaith(int):151` / `GetDominantSchool:166`；`GameWorld.GetThoughtManager:252`（`GameWorld.Queries.cs`）；`TraditionDef.CanCultureHold:55`；`PopulationStats.GetCultureShare:10` / `GetDominantCulture:79`；`InnovationTree.GetEffectiveResearchRate:490`；`DiscoveryActors.FromSettlement:8`（仅测试）。

另：`GameSaveSystem`（642 行，另一套并行存档器，写 `primaryCultureId`/`stateReligionId`）**全库仅自引用，0 实例化** → `SaveSystem` 是唯一活存档器，本域「入档」判定均以它为准。

## 5. 我核实不了的事

1. **运行时行为**：未启动 Unity 编辑器/Play 模式，所有三态判定为静态推断（注册点、调用点、内容计数）。未观测实际日志。
2. **`cultures` 段从哪个 SaveVersion 引入**：`SaveData.cs:38` 无版本注释（同文件其他段都有）；`SaveSystem.cs:371-424` 无版本分支。只能确认「当前版本会写会读」，不能确认引入版本。
3. **`Main.unity` 中 `religionPanelText` 是否真的指向 `ReligionPanelText` 输出控件**：只在场景文件中匹配到字段名，未读 `UIManager` 的序列化绑定细节。
4. **AI 侧是否存在字符串/反射驱动的国教设置**：已 grep 全部 `Assets/**/*.cs` 的 `stateReligionId\s*=`，仅见初始化、`FaithContracts`、存档还原三处；但未审计 AI 意图执行器（`_aiIntentExecutor`）的完整行为表，不能排除未来意图名映射。
5. **`CultureStageCatalog` 读 JSON 与代码默认值的等价性**：只读了 `CultureStageCatalog.cs` 的加载/回退骨架（`:40-80`）与 `CultureStageEvolutionDataTests.cs` 的断言意图，未逐字段核对 5 个阶段的全部阈值。
6. **`Schema/` 与 `Assets/StreamingAssets/Base/_generated/language-families.json`**：文件存在（12.9KB，`LanguageFamilyTests` 断言 ≥50 节点），但未核对 Laethis 的 `laethis_lang` 是否在谱系表中有对应节点（`Language.json` 无 `glottocode`，很可能对不上）。
7. **其它域的交叉影响**：`Traditions`/`Ethos`/`Heritage` 可能被法律/头衔域以未检索到的形式消费（如 `LawDef` 持有 `FamilyTraditionDef` 引用），本次只在文化/信仰域内做了 grep 收敛。
