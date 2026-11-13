# 全项目完成度评估 + 地形/气候/灾害/植被测试覆盖审计

- **项目**：Myriad Creation（Unity 6 文明模拟），工程根 `MyriadCreation/`
- **分支**：`architecture-2-0-refactor`，工作区 1,644 项未提交改动
- **审计方式**：只读。grep / 文件读取 / 只读目录统计；未运行 Unity、未编译、未跑测试、未修改任何代码或资产
- **快照时间**：2026-11-02 13:15–13:30（Asia/Shanghai）
- **基线结果**：`Logs/editmode-results.xml`（2026-11-02 12:56 本地）**594/594 通过**，`Logs/playmode-results.xml` 与 `playmode-gfx-results.xml`（12:58/12:59）各 **9/9 通过**。本报告所有数字来自文件系统实际统计，与结果文件一致
- **口径说明**：本文“完成度”= **功能实现完整度**（有什么/缺什么），与“测试成熟度”分开标注；“测试数”为该模块相关 fixture 的用例数（`[Test]`+`[TestCase]`），按关键词 + 文件名映射，非严格覆盖统计

---

## 一、量化指标

### 1.1 程序集与代码量（csproj Compile 项实测）

| 程序集 | .cs 数 | 职责 |
|---|---:|---|
| MyriadCreation.Simulation | 395 | 模拟主程序集（Systems 109 / State 70 / WorldState 34 / Units 29 / Genetics 26 / AI 12 / Actors 17 …） |
| MyriadCreation.Content | 164 | 内容定义（Definitions 105 + Faith 12）+ Modding 47 |
| MyriadCreation.Tests | 102 | EditMode 测试 |
| MyriadCreation.World | 86 | 世界生成/地形 14、气候 4、群系 4、水文 4、聚落 44、Anchor 5、Editing 4、根 2 |
| MyriadCreation.Core | 76 | Data 14 / Enums 15 / Contracts 35 / Events 7 / Dto 2 / Constants 1 / 根 2 |
| MyriadCreation.Laws | 32 | 法律系统独立装配 |
| MyriadCreation.UI | 28 | UI 面板 + Presentation 只读文本模型 |
| MyriadCreation.Infrastructure | 22 | 存档 20 + 音频 2 |
| MyriadCreation.Rendering | 12 | 地图渲染 |
| MyriadCreation.App | 8 | Bootstrap/场景流/会话 |
| MyriadCreation.App.Contracts | 8 | App 契约 |
| MyriadCreation.Tests.PlayMode | 7 | PlayMode 测试 |
| MyriadCreation.Editor | 7 | 编辑器工具 |
| MyriadCreation.Culture | 7 | 文化宿主 |
| MyriadCreation.Faith | 2 | 信仰 Runtime |
| Assembly-CSharp | 34 | 预定义程序集（无 asmdef 的脚本） |
| **合计** | **~990 编译项 / Assets 内 956 个 .cs** | Scripts 812 + Tests 109 + UI 28 + Editor 7 |

asmdef 共 15 个：Core、World、Simulation、Laws、Faith、Culture、Content、Infrastructure、Rendering、UI、App、App.Contracts、Editor、Tests、Tests.PlayMode。

### 1.2 测试规模

| 套件 | 文件 | `[Test]` | `[TestCase]` | 用例合计 | 结果 |
|---|---:|---:|---:|---:|---|
| EditMode | 102 | 582 | 12 | **594** | 594/594 通过 |
| PlayMode | 7 | 9（`[Test]`/`[UnityTest]`） | 0 | **9** | 9/9 通过（gfx 复跑 9/9） |
| **总计** | **109** | 591 | 12 | **603** | 全绿 |

EditMode 分布（用例数 Top 10）：HabitationSite 23、ArchitectureContracts 18、UiDecoupling 16、WorldSeedAndFragmentation 14、SettingsModel 13、LawEffectRuntime 13、WorldServices 12、LawDecreePipeline 12、EconomyConservation 12、EntityRelationAndLivingGoods 12。PlayMode 覆盖政府面板、主菜单、地图可见性、存读档面板、设置、聚落详情、聚落图标 LOD。

### 1.3 内容资产

| 类别 | 数量 | 说明 |
|---|---:|---|
| StreamingAssets JSON | **30** | `Base/` 29 + `Mods/Dna/` 1 |
| 内容定义类型 | Base 下 19 个目录 / 约 21 类内容 | Biome、CharacterTemplate、Culture(2)、Dna、EthnicGroup、Ethos、FamilyTradition、Goods(2)、Innovation、Language、Laws、Localization(8)、MentalHealth、Properties、Race、Religion(2)、Title、Tradition、_generated |
| 内容 CSV | 8 | 莱提斯文化/语言的男/女名、姓氏、城名 |
| 内容体量 | Innovation 62KB、Laws 30KB、Religions 26KB、Biomes 24KB、Localization 各 16–18KB | 内容层规模可观 |
| 本地化 | 8 语言（en/zh-Hans/zh-Hant/ja/ko/fr/es/de） | 有 `Localization` 静态服务并接入 Bootstrap/Settings |
| 美术 | Art 48 文件（19 shader + 4 shadergraph + 6 字体 + 2 材质 + 4 asset，**无贴图/模型/动画**） | 程序化渲染为主 |
| 音频 | 7 文件 | SfxPlayer/MusicPlayerSystem + 少量资源 |
| 场景 | 1（`Main.unity`） | 单场景启动 |

---

## 二、分模块完成度（0–100%）

> 完成度 = 功能完整度；测试数列为该模块相关用例数；“缺什么”只列对 1.0 有实质影响的缺口。

| # | 模块 | 完成度 | 有什么（代码/内容证据） | 缺什么 | 相关测试数 |
|---|---|---:|---|---|---:|
| 1 | 世界生成/地形 | **72%** | `PlanetTerrainGenerator`(377 行) 球面噪声→Voronoi 板块→侵蚀→地形 PCA→海洋环流→气候→水文→群系/肥力全管线；`TerrainGenerator` 河网；`HydraulicErosion`(227)、`FeatureManager`、`TerrainPCA`、`SphericalNoise/Voronoi`、`TerrainTemplateSystem`(世界/区域预设)、`TerrainScale`；`GameWorld.GenerateTerrain`(GameWorld.Generation.cs:122)；破碎化重算；1024×512 开图性能预算 | 地形子系统**无单测**（侵蚀/特征/PCA/模板/噪声 0 直接引用）；只支持 1 场景流程；无高度图导入的测试；无大图（2048+）回归 | ~30（集成） |
| 2 | 气候/群系 | **60%** | 双路径：新 `PlanetTerrainGenerator` 内 `AtmosphericCirculation`(气压/风场/ITCZ/西风/季风/地形雨)、`OceanCurrentSimulator`(暖冷流修正)、`HoldridgeBiomeClassifier`(286 行，Classify:95 + CalculateFertility:242)；旧 `PlanetClimateSimulator`(371 行，RecalculateAll:65/RecalculateDirty:79/UpdateForSeason:93，内联 DetermineClimateZone:300/DetermineBiome:336)；`Biomes.json` 24KB + `BiomeRegistry/BiomeAttributes` | **无任何气候公式单测**（ITCZ/西风/季风/地形雨/环流/洋流 0 引用）；旧新两条群系分类路径并存且有缺陷：`PlanetClimateSimulator.cs:353` Subarctic 三元两支都是 `Tundra`（死分支）；季节性 `UpdateForSeason` 是否在游戏时钟里调用无测试 | ~22（集成） |
| 3 | 生态/植被/土壤 | **22%** | 仅 `TileData` 标量：`fertility`、`soilHumidityPct`、`airHumidityPct`、`annualTemp/precip`、`biome`；肥力由 `PlanetTerrainGenerator.cs:230` Holdridge 公式计算、破碎化时 `GameWorld.Generation.cs:273` 重算；扰动点：`DisasterSystem`(295/313/326/329)、`SwarmAI.cs:62`、`BuildingSystem.cs:271` | **没有植被/林地/草原覆盖度系统，没有土壤退化/盐碱化/侵蚀-肥力再生闭环，没有生态演替**；肥力只有 0–1 标量与少量加减，无法支撑“过度放牧→地力下降→饥荒→迁徙”；生态相关测试 0 | 0（仅肥力断言 3 条） |
| 4 | 灾害 | **50%** | `DisasterSystem`(421 行)：11 类灾害（Drought/Flood/ColdWave/HeatWave/Storm/Earthquake/Volcanic/Landslide/Locust/CropFailure/Fire/Famine）、按气候挑源、影响半径、即时效果、每日 tick；`DisasterSchedule` 已注册进调度（GameWorld.Simulation.cs:778）；`CharacterDiseaseSystem`/`DiseaseSystem`/`ActiveDisease`；灾害影响 `RealmSituationBuilder`（民情） | **行为测试 0**（只有 ArchitectureContractsTests:37-40 验证 scheduler 注册）；**灾害状态不入档**（SaveData 无 disasters 字段，读档后灾害直接消失）；无灾害概率/季节/气候联动配置验证；无灾害→饥荒→人口迁移的闭环测试 | 0 |
| 5 | 经济/食物/人口 | **75%** | `EconomyManager`+`EconomySchedule`、`SettlementProduction`、`TradeCenter/TradeRoute/Caravan`、`Market`、`CurrencySystem`、`CarryingCapacitySystem`、`TaxSystem`；`PopulationSchedule`、`PopulationBlock/Stats`、`ClassNeedsSystem`、`SocialClassAvailability`；Goods/Recipes/DynamicGoods 内容；守恒/超容/在途货物损失都有测试 | 价格弹性/通胀/市场整合的模型化有限（未见价格公式单测）；贸易路线损耗/封锁/关税仅部分；食物→人口→阶级需求链路只有间接测试 | ~50（经济相关） |
| 6 | 聚落/城市 | **72%** | `World.Settlement` 44 文件：`SettlementTypologySystem`、区划/街区/城墙/建筑等级/市场/港口/首都/城市区划（districts）、`SettlementInfluenceSystem`、`SettlementEvolution/Control/Destruction`、`BuildingSystem`、`WallDefense`、营地系统；内容 `SiteType` 等 | 城市内部布局/道路/就业/通勤未建模；聚落级灾害破坏、围城攻防只有部分；`sied/occupation` 与存档衔接需验证 | ~60（含存档/UI） |
| 7 | 法律/政令 | **80%** | `MyriadCreation.Laws` 独立装配 32 文件 + `LawHost` 12：`LawComposition`、`LawEffectRuntime`、法令下发 `DecreePropagationSystem`（IssueDownward/IssueToSubordinate/信使/旅行时间/官员执行-拖延-拒绝）、`AdminDivision` 行政树、`FactionLawChange`；`Laws.json` 30KB；`LawContentJson` 内容校验 | 法令**信使路径**（AttachCourier/CourierArrived）无测试；多级/并行区划下发测试仅 2 层；法律效果覆盖面（税/兵役/民生）单测尚可但缺少长期累积效果验证 | ~55 |
| 8 | 信仰 | **58%** | `Content/.../Faith` 12 + `FaithHost` 7：`ReligionCatalog`、`ReligionSimulationEventHandler`、`GreatHolyWarSystem`、`CanonizationSystem`、`MissionarySystem`（AI 调度中）、`WarfareReligionSchedule`；`Religions.json` 26KB + `Doctrines.json` 7.6KB；`FaithSaveData` 已入档 | 教义效果、狂热（fervor）演化、宗教冲突、宗教→政治合法性链路的**行为测试缺失**（仅查询/持久化 10 条）；宗教地图扩散无测试 | ~10 |
| 9 | 文化 | **55%** | `CultureHost` 7：`CultureLineage`、`CultureSchedule`、`CultureStageEvolutionHost`、`CultureTransitionHost`、`CultureContextAdapter`；`CultureStages.json`、莱提斯文化/语言（Ethos/EthnicGroup/Tradition/Language 内容 + 8 CSV 命名库）；`LanguageFamilyCatalog` | 文化演化/同化/分裂的**行为测试仅 LanguageFamily+StageEvolution 13 条**；文化→法律/宗教/命名联动多为数据层验证；文化扩散无测试 | ~13 |
| 10 | 角色/家族 | **78%** | `State` 70 文件：`CharacterData`（性格/特质/疾病/伤残/心理/成就/关系/纽带/家庭）、`FamilyNode/FamilyTree`、`CharacterManager`、`DnaSystem`、`Genetics` 26 文件（孟德尔/连锁/性染色体/杂交/表型）、`SuccessionSystem`+`Regency`、`EpithetSystem`、`EvaluationSystem`、`AchievementRecord`、`MentalHealthSystem`、`BodyPartSystem` | `EpithetSystem/EvaluationSystem/AchievementRecord` **0 测试**；心理/伤残/身体部位系统几乎无行为测试；家族树大规模演算（数百代）无压力测试 | ~50 |
| 11 | 军队/战争 | **55%** | `Units` 29：`Army/TransportUnit/UnitProvisioning/UnitFounding/ManpowerSystem`；`CombatManager`、`MilitaryMovementSystem`、`RetinueSystem`、`WarJustificationSystem`、`PeaceNegotiationSystem`、`DiplomacyManager.WarRules`、`WarState/WarGoal/WarRules/CasusBelli/BattleResult`；`MilitaryBuildUpCommand`；`WarfareReligionSchedule` 已注册 | 战斗解决/围城/占领/和约签署**无行为测试**；`WarState` 只有 11 字段（无围城/占领/战争目标明细持久化）；海军只有“过海需船”，无海战；伤亡/后勤（补给线）链路弱 | ~25（军队/运输/存档） |
| 12 | 外交/派系 | **48%** | `Diplomacy` 9 个命令/处理器（宣战/同盟/赠礼/劫掠）、`DiplomacyManager`（关系值、同盟、附庸、共主、使馆、禁运、军通）、`DiplomacySchedule/RelationBridge`、`DiplomaticEvent`、`Treaty/Alliance/Subordination/NegotiationClause`；`FactionManager/Faction/IdeologyMovement/ThoughtManager`；`FactionLawChange` 测试 | **外交命令 0 测试**；**外交状态不入档**（SaveData 无 DiplomaticRelation/Alliance/Treaty/Subordination，`_nextTreatyId` 不回滚）→ 读档外交关系归零；派系也只测了“派系改法”，无派系生成/博弈/镇压测试 | ~7 |
| 13 | AI | **45%** | `Simulation.AI` 12：`AIController/AIManager/AIDecisionSystem/AIIntent/AIIntentExecutor/AIPersonality/AIStance/AIGoal`、`SettlementAISystem`、`Actors` 17（`NomadAI/SwarmAI`）；`AISchedule` 已注册（GameWorld.Simulation.cs:816） | **AI 决策行为 0 测试**（只有 WorldServicesTests 对 controller 数量做结构断言）；无“AI 是否会扩张/结盟/宣战/改革”的行为规格测试；AIIntent 执行失败/回滚无测试 | ~2（结构） |
| 14 | 存档/读档 | **78%** | `Infrastructure.Save` 20 文件，`SaveVersion = 16`；`SaveData` 覆盖 tiles/races/cultures/realms/tradeCenters/goods/dynamicGoods/relations/contracts/wars/characters/families/offices/actors/units/knownCharacters/provinces/habitationSites/armies/innovations/faiths/bonds；原子写入、往返、旧档兼容、各系统持久化均有测试 | **灾害、外交关系/同盟/附庸/条约、派系、围城/占领状态未入档**；`SaveData` 无对应表；需要验证 `relations/contracts` 是否能等价恢复外交（代码中未见重建）；无自动存档（`AutoSave` 0 引用）；无存档体积/大世界读档性能测试 | ~45 |
| 15 | UI | **70%** | `Assets/UI` 28 文件：主菜单、政府、社会、聚落详情、存读档、设置面板 + `Common`（UIManager/UiFactory/主题/缩放/SFX/LOD）+ `Presentation` 只读文本模型（区划树/概览/宗教/人口构成）；16 个 EditMode UI fixture + 7 个 PlayMode（9 用例）；`UiDecoupling` 保证 UI 不反向依赖模拟层 | 无本地化 UI 测试；无分辨率/多语言/长文本回归；无无障碍/键位；UI 与模拟解耦靠 gate 测试，但面板数据缺口（如灾害/外交面板）需验证 | ~50 |
| 16 | 架构/资产 | **80%** | 16 程序集 + 15 asmdef；`ArchitectureContracts`(18)、`AsmdefArchitectureGate`(6)、`AssetLayoutGate`(7)、`CompletionGate`(5)、`SystemContracts`(5)、`ContentDefinitionIndex`(5)、`TmpAssetIntegrity/TMPFont`(7)；内容包 `Content.Modding` 47 文件（校验/索引/Provider/Workspace）；仅 2 处 TODO | 美术资产几乎为空（无贴图/模型/动画/UI 精灵）；关键资产只有 4 项 gate；Mod 端到端（加载外部包→注册→生效）无测试；编辑器工具（MapEditing）测试薄 | ~48 |

**加权估算：全项目功能完成度约 62–65%**（权重按 1.0 期望：世界/模拟 50%、内容/存档/UI 30%、美术/打磨 20%）。若只算“模拟与系统层”，约 70%；算上美术与终局体验，约 55%。

---

## 三、地形/气候/群系/灾害/肥力/生态 测试覆盖审计（重点）

### 3.1 相关测试文件与用例数

| 测试文件 | 用例 | 覆盖对象 | 强度评价 |
|---|---:|---|---|
| `WorldSeedAndFragmentationTests.cs` | 14 | 海陆/破碎噪声由 seed 决定、RecalculateAll 确定性、存读档保留 seed 与地形 | **集成强**：逐地块断言 noise/elevation/land 差异 |
| `SeaLandRecalcDeterminismTests.cs` | 5 | `SeaLandGenerator.RecalculateAll` 同 seed 确定/异 seed 不同、world 级 seed 接入 | 中：只测确定性，不测形态正确性 |
| `FragmentationClimateConsistencyTests.cs` | 3 | 破碎化后 biome/fertility 与 `HoldridgeBiomeClassifier.Classify/CalculateFertility` **逐地块一致**；同 seed 派生气候确定；读档不重算保存的 annualTemp/precip/biome/fertility | **本模块唯一直接验证公式的测试**（作为 oracle） |
| `RegionalTerrainShaperTests.cs` | 2 | `SeaWest`/`Island` 区域模板把边缘变海 | 弱：仅 43 行、边界点断言 |
| `SeaLandPerformanceTests.cs` | 1 | 1024×512 开图预算 + 同 seed 稳定（哈希/陆地数） | 中：单一尺寸、宽松预算 |
| `WaterNavigabilityTests.cs` | 3 | 海洋分区 4 档、河流可通航、`TerrainConditions` 过海需船 | 中：`RiverNavigability`/`OceanZoning` 有覆盖 |
| `WorldGenerationSmokeTests.cs` | 2 | 生成不抛异常 + 全链路存读档 | 弱：Count 级 |
| `WorldSetupOptionsTests.cs` | 6 | 初始内容/生成选项 | 与地形弱相关 |
| **合计** | **36** | | 地形/气候类约占 EditMode 594 的 **6.1%** |

### 3.2 直接公式单测统计（关键结论）

| 逻辑 | 直接单测 | 证据 |
|---|---|---|
| `HoldridgeBiomeClassifier.Classify`（:95） | **仅 1 处 oracle 用法** | `FragmentationClimateConsistencyTests.cs:136` |
| `HoldridgeBiomeClassifier.CalculateFertility`（:242） | **仅 1 处 oracle 用法** | 同上 `:149` |
| `CalculateBiotemperature/PET/PER/GetHumidityProvince/GetThermalBelt` | **0** | grep 测试目录无引用 |
| `PlanetTerrainGenerator.Generate`（:48） | 0（仅 2 个集成 fixture 间接） | — |
| `PlanetClimateSimulator`（RecalculateAll/RecalculateDirty/UpdateForSeason、ITCZ/西风/季风/地形雨） | **0** | grep 测试目录 0 引用 |
| `AtmosphericCirculation` / `OceanCurrentSimulator` / `HydraulicErosion` / `FeatureManager` / `TerrainPCA` / `SphericalNoise` / `SphericalVoronoi` / `TerrainTemplateSystem` / `HydrologySystem` | **0** | 全部 0 测试引用 |
| `DisasterSystem`（11 类灾害、源点选择、影响范围、肥力修改） | **0 行为测试** | 唯一引用是 `ArchitectureContractsTests.cs:37-40` 的 scheduler 注册检查 |
| 生态/植被/土壤系统 | **不存在独立实现，测试 0** | — |
| 肥力扰动（灾害/蝗群/建筑） | **0** | `DisasterSystem.cs:295/313/326/329`、`SwarmAI.cs:62`、`BuildingSystem.cs:271` 均无测试引用 |

### 3.3 无测试的关键逻辑清单（文件:行）

1. `Scripts/World/MyriadCreation.World.Terrain/PlanetTerrainGenerator.cs:48-236`——主生成管线（球面噪声→Voronoi 板块→侵蚀→PCA→大气环流→洋流→水文→Holdridge 分类→肥力），无单元级断言，只有 2 个集成 fixture。
2. `.../World.Biome/HoldridgeBiomeClassifier.cs:95-240`（Classify 全分支）与 `:242-286`（CalculateFertility 各群系系数）——无独立分支测试（仅被当作期望值 oracle）。
3. `.../World.Climate/PlanetClimateSimulator.cs:65-101`（RecalculateAll/Dirty/Season）、`:196-280`（ITCZ/西风/季风/地形雨映射）——0 测试。
4. `.../World.Climate/PlanetClimateSimulator.cs:336-371`（DetermineBiome）：**`:353` Subarctic 分支 `(precip > 300f) ? Tundra : Tundra` 两支相同，属死分支缺陷**，无测试可拦。
5. `.../World.Hydrology/HydrologySystem.cs:116-345`（Run 优先级淹没、ApplyToTiles、河网统计）——0 测试；`OceanCurrentSimulator` 全类、`HydraulicErosion` 全类 227 行、`TerrainPCA`、`SphericalNoise/Voronoi`、`FeatureManager`、`TerrainTemplateSystem` 全部 0 测试。
6. `.../Simulation/MyriadCreation.Simulation.Systems/DisasterSystem.cs:156`（DailyTick 概率/季节）、`:187`（TriggerDisaster 源点与影响范围）、`:260+`（即时效果）、`:295/313/326/329`（肥力改变）——0 行为测试。
7. `.../Simulation/MyriadCreation.Simulation.Actors/SwarmAI.cs:62`（蝗群 −2 肥力）与 `.../Systems/BuildingSystem.cs:271`（建筑 +0.05×tier 肥力）——0 测试。
8. `.../Simulation/MyriadCreation.Simulation.WorldState/GameWorld.Generation.cs:203-290`（破碎化后逐地块重算 climate/biome/fertility，海地块 fertility=0）——仅被 1 个 fixture 间接覆盖。
9. `.../Scripts/Rendering/MapRenderer.Colors.cs` 的地形/群系配色映射——渲染侧无测试（属视觉，可接受但需人工验证）。

---

## 四、到 1.0 还缺什么

### P0（不解决则 1.0 不成立）

1. **存档完整性：政治/灾害状态不入档。** `SaveData`（`SaveVersion=16`）没有 Disaster / DiplomaticRelation / Alliance / Treaty / Subordination / Faction 表；`DiplomacyManager._relations/_subordinations/_nextTreatyId`、`FactionManager._factionsByRealm`、`DisasterSystem._activeDisasters` 全是运行时状态。读档后：灾害消失、外交关系/同盟/附庸/停战归零、派系消失。证据：`SaveData.cs:22-74`、`DiplomacyManager.cs:11-25`、`FactionManager.cs:11-30`、`DisasterSystem.cs:24-30`，且 `SaveSystem.cs` 中无任何这些词。
2. **战争闭环未验证且未持久化明细。** `WarState` 仅 11 个字段（无围城/占领/战争目标/和约条款）；战斗解决、围城、占领、和平谈判（`PeaceNegotiationSystem`、`Treaty`）无行为测试。1.0 的“打—占—和”闭环不可信。
3. **生态/植被/土壤系统缺失。** 只有 `fertility` 标量，没有植被/林地覆盖、土壤退化/恢复、生态演替；“文明压榨土地→地力衰退→饥荒/迁徙”这一文明模拟核心负反馈不成立。
4. **灾害系统 0 测试、不入档。** 11 类灾害是重要世界事件，但既无行为验证也无存档，等于“有代码无保障”。
5. **无胜利条件/终局。** 全仓 grep 无 `VictoryCondition/WinCondition`；1.0 需要明确目标与结束条件（或明确定位为纯沙盒，需产品决策）。

### P1（显著影响 1.0 质量）

1. **气候/群系双实现分叉且有缺陷：** 新路径 `PlanetTerrainGenerator` 用 `HoldridgeBiomeClassifier`，旧路径 `PlanetClimateSimulator` 自带内联 `DetermineBiome`；`PlanetClimateSimulator.cs:353` 死分支（Subarctic 两支 Tundra）。需要统一到 Holdridge 并删除/降级旧路径，同时补公式单测。
2. **地形/水文/气候子系统 0 单测：** 侵蚀、特征、PCA、球面噪声/Voronoi、模板预设、`HydrologySystem`、`OceanCurrentSimulator`、`AtmosphericCirculation` 全无测试；地形只靠 2 个集成 fixture 保护，任何回归只能靠“开图不崩 + 确定性”。
3. **外交命令 0 测试 + 外交状态不持久化**（见 P0-1）。
4. **AI 行为 0 测试：** `AIDecisionSystem/AIIntentExecutor/AIPersonality/SettlementAISystem` 无任何规格测试；AI 是否扩张/结盟/改革不可回归。
5. **信仰/文化行为测试薄：** 信教义效果/狂热/圣战/传教、文化演化/同化只有查询与持久化测试（10–13 条）。
6. **本地化无测试、UI 硬编码风险：** 8 语言 JSON 已就绪、`Localization` 已接入 Bootstrap/Settings，但测试目录对 Localization 的引用为 0；UI 文本大量中文常量需逐项迁入内容键。
7. **美术资产近乎为空：** Art 仅 48 文件（19 shader + 6 字体 + 少量材质），无角色/建筑/地形贴图、无动画；1.0 视觉表现缺口大。
8. **无自动存档、无教程/引导：** `AutoSave`/`Tutorial`/`Onboarding` 0 引用。

### P2（1.0 打磨项）

1. 性能回归只有 1 条 1024×512 预算测试；缺 2048/4096 大地图、内存峰值、存档体积、读档耗时的持续基准。
2. PlayMode 仅 9 条 UI 用例；缺“长时间模拟/存档读档后继续运行”的 PlayMode 冒烟。
3. Mod 支持（47 文件）没有“加载外部内容包→注册→生效”端到端测试。
4. `AchievementRecord/EvaluationSystem/EpithetSystem`、`MentalHealthSystem`、`BodyPartSystem` 等无测试。
5. 多语言/分辨率/长文本 UI 回归、无障碍；`SeaLandPerformanceTests` 预算宽松（仅 5000ms 上限）。
6. 内容校验（`ContentPackageValidator`）与 8 语言内容键完整性无 gate。

---

## 五、方法与局限 / 待验证

1. **完成度百分比为审计判断**：依据“代码/内容是否存在 + 是否有测试保护 + 是否有端到端闭环”三方面打分，非产品方量化指标；误差约 ±10%。
2. **测试数与模块映射**按 fixture 名 + 关键词 grep 近似；一个 fixture 可能跨模块（如 `WorldGenerationSmokeTests` 同时算地形与存档）。
3. **待验证**：
   - `relations/contracts` 是否等价承载外交关系/同盟（代码中未发现 `DiplomaticRelation/Alliance/Treaty` 的捕获与恢复，但 `Contract` 语义可能与同盟重叠）；
   - `PlanetClimateSimulator` 的季节更新 `UpdateForSeason` 是否被游戏时钟实际调用（`WorldMapSimulationSystem` 持有它，未确认调用频率）；
   - `PlanetTerrainGenerator` 与 `TerrainGenerator` 两条河网路径是否都进入正式开图（当前 `GenerateTerrain` 先 Planet 生成再 `TerrainGenerator.TrackRivers`）；
   - 灾害是否在别处以事件/编年史形式间接持久化（`Chronicle` 有记录，状态本身未入档）；
   - 美术资产是否计划在 1.0 前外包/补齐（Art 目录当前几乎无美术资源）。

---

## 附录：本次全部数字的取证命令（只读）

```bash
# 程序集
for p in MyriadCreation/*.csproj; do echo "$p $(grep -c 'Compile Include=' $p)"; done
find MyriadCreation/Assets -name "*.cs" | wc -l          # 956
# 测试
grep -rc "\[Test\]" MyriadCreation/Assets/Tests/EditMode | awk -F: '{s+=$2}END{print s}'   # 582
grep -rc "\[TestCase" MyriadCreation/Assets/Tests/EditMode | awk -F: '{s+=$2}END{print s}' # 12
grep -rc "\[Test\]\|\[UnityTest\]" MyriadCreation/Assets/Tests/PlayMode | awk -F: '{s+=$2}END{print s}' # 9
head -c 300 MyriadCreation/Logs/editmode-results.xml     # 594/594
# 内容
find MyriadCreation/Assets/StreamingAssets -name "*.json" | wc -l   # 30
find MyriadCreation/Assets/StreamingAssets -name "*.csv"  | wc -l   # 8
ls MyriadCreation/Assets/StreamingAssets/Base
# 地形/气候/灾害/生态
grep -rlnE "PlanetTerrainGenerator|Holdridge|PlanetClimateSimulator|HydrologySystem|DisasterSystem|terrain|climate" MyriadCreation/Assets/Tests/EditMode
grep -rn "Disaster" MyriadCreation/Assets/Tests/EditMode/ArchitectureContractsTests.cs   # 仅注册
grep -rn "Tundra : GameEnums.BiomeType.Tundra" MyriadCreation/Assets/Scripts/World   # 死分支
grep -rn "public " MyriadCreation/Assets/Scripts/Infrastructure/MyriadCreation.Infrastructure.Save/SaveData.cs
```
