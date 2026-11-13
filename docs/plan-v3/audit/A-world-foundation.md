# 世界地基 审计（2026-11-09）

> 只读审计。所有锚点相对 `MyriadCreation/`。三态：在跑 / 半跑 / 空转 / 不存在。

## 0. 范围

已读（行数 = `Measure-Object -Line`）：
- `Assets/Scripts/World/MyriadCreation.World.Terrain/`：PlanetTerrainGenerator 377、SeaLandGenerator 800、HydrologySystem 345、HydraulicErosion 206、TerrainGenerator 157、SphericalNoise 256、SphericalVoronoi 167、TerrainPCA 385、FeatureManager 170、RegionalTerrainShaper 60、TerrainTemplateSystem 401、TerrainTemplatePreset 32、WorldTemplate 23、TerrainScale 8
- `Assets/Scripts/World/MyriadCreation.World.Climate/`：PlanetClimateSimulator 371、AtmosphericCirculation 263、HumidityProvince 14、ThermalBelt 13
- `Assets/Scripts/World/MyriadCreation.World.Hydrology/`：OceanCurrentSimulator 190、OceanZoning 133、RiverNavigability 27
- `Assets/Scripts/World/MyriadCreation.World.Biome/`：BiomeAttributes 531、HoldridgeBiomeClassifier 286、BiomeDef 44、BiomeRegistry 12
- `Assets/Scripts/World/MyriadCreation.World.Anchor/`：AnchorGenerator 249、VirtualControlCalculator 295、ControlTriangle 108、AnchorData 24
- `Assets/Scripts/World/MyriadCreation.World.Generation/`：MapGenerationConfig 440、WorldSetupOptions 128、InitialContentMode 11、SandboxSpawnRates 18、Seed 10
- `Assets/Scripts/World/MyriadCreation.World/`：Province 44、ProvinceGenerator 316
- `Assets/Scripts/World/MyriadCreation.World.Settlement/`：SettlementTypologySystem 354、SettlementData 622、其余 60 个文件按需 grep（空间/地形部分）
- `Assets/Scripts/World/MyriadCreation.World.Editing/`：IMapEditCommand 10、MapEditHistory 48、MoveSelectionCommand 95、RaiseSelectionCommand 59
- `Assets/Scripts/Rendering/`（12 个 .cs 全部）：MapRenderer 757、MapRenderer.Texture 381、MapRenderer.Colors 353、MapRenderer.Nomadic 162、SphericalMapRenderer 255、SettlementIconLayer 469、CyowariMapStyle 236、MapLayerConfig 130、MapLayerRegistry 97、IMapEditController 19、Editing/IMapEditCommandService 38、Editing/RuntimeMapEditingController 247
- `Assets/Editor/Tools/TerrainPainter/`：BrushMode 13、BrushShape 8（**全目录仅此 2 个枚举文件**）
- 主循环相关：`GameWorld.Generation.cs` 585、`GameWorld.Simulation.cs`（348–368 / 470–534）、`GameWorld.Editing.cs` 114、`GameWorld.Persistence.cs` 92、`WorldMapSchedule.cs` 28、`WorldMapSimulationSystem.cs` 131、`DisasterSchedule.cs` 39、`GameWorld.Core.cs`（520–600）、`SaveSystem.cs`（407–502）、`TileSaveData.cs` 36、`GameSaveSystem.cs`（121–141 / 413–440）、`WorldGenerationService.cs` 92、`GenerationPipeline.cs` 271、`ResourceGenerator.cs` 172、`GameManager.cs`（120–240 / 290–440）、`UIManager.MapMode.cs` 200、`MainMenuUI.cs`（300–345）
- `Assets/StreamingAssets/Base/Biome/Biomes.json`（24312 字节，JSON 解析 = 92 条）

未覆盖：`World/Settlement/` 中纯社会/经济类（CityDistrictSystem、EconomicCompositionSystem、WallDefense、SettlementMarket 等约 55 个文件）不在本域；`SphericalNoise`/`SphericalVoronoi`/`TerrainPCA` 只判调用关系未逐行读算法；未运行 Unity 编辑器/测试；未读 `.meta`、场景序列化细节以外的资产内容。

## 1. 系统清单

| # | 系统 | 三态 | 驱动证据（file:line） | 入口证据 | 存档证据 | 测试 | 一句话现状 |
|---|---|---|---|---|---|---|---|
| 1 | 高程/板块/海陆基底（PlanetTerrainGenerator） | 在跑 | `GameWorld.Generation.cs:144` 直调 `new PlanetTerrainGenerator(seed).Generate(...)`；噪声 `PlanetTerrainGenerator.cs:73-79`、分位归一 `:80`、球面 Voronoi 板块 `:82-92`、海陆/洋深 `:100-113` | `GameManager.cs:149` → `WorldGenerationService.cs:26-30` → `GenerateTerrain` | `TileSaveData.cs:10-11` elevation01/isLand 双向 | 间接（所有 `GenerateTerrain` 测试） | 真实成因链：噪声→板块→分位海平面；但 `QuantileNormalize` 恒返回 0.5（`:311`），日志里的"海平面阈值"永远是 0.5000 |
| 2 | 海陆阈值/破碎化/海域连通（SeaLandGenerator） | 半跑 | 生成期 `GameWorld.Generation.cs:169-188` 走 `RecalculateAll()`；运行期 dirty 走 `WorldMapSimulationSystem.cs:117` | 无 UI；仅经编辑/配置 dirty | **阈值状态不入档**：`TileSaveData.cs` 无 `_cachedSeaThreshold`/`_fragmentNoise` 字段，读档后 `InitializeSubsystems` 重建（`GameWorld.Core.cs:542`） | `SeaLandRecalcDeterminismTests` 5 例、`SeaLandPerformanceTests` 1 例 | 缺"③状态入档"：阈值/破碎噪声不落盘，同一操作在"新开局"与"读档"下语义不同 |
| 3 | 大气环流 GCM（AtmosphericCirculation） | 半跑 | 生成期一次：`PlanetTerrainGenerator.cs:209-213`；运行期唯一入口 `GameWorld.Simulation.cs:485-486` 属**死路径**（见 #19） | 无 | 输出仅落 `annualPrecipMm`/`airHumidityPct`（`TileSaveData.cs:14-15`） | **0 引用**（`Get-ChildItem Assets/Tests \| Select-String AtmosphericCirculation` → 0） | 缺"②驱动"：`Season` 字段（`:18`）只由 `MapGenerationConfig.cs:429` 写成 `ThermalEquator*0.25`，**不接运行季**；风场带与降水分档（`:135-151`、`:168-184`）是纬度查表常数 |
| 4 | 半经验气候（PlanetClimateSimulator） | 半跑 | 季节：`GameWorld.Simulation.cs:360` `UpdateForSeason(currentSeason)`（主循环 tick 内，`AdvanceTime :348-368`）；全量/dirty：`WorldMapSimulationSystem.cs:102/126` | 无 | `annualTemp/annualPrecipMm/airHumidityPct/biome/climateZone` 入档（`TileSaveData.cs`） | 间接（FragmentationClimateConsistencyTests 3 例） | 缺"②独立驱动"：它按季**整体重写**陆地 `annualTemp/annualPrecipMm/biome`（`:159-188`），与 GCM 结果互为覆盖 |
| 5 | 群系分类 | 半跑 | Holdridge：`PlanetTerrainGenerator.cs:228`、`GameWorld.Simulation.cs:496`、`GameWorld.Generation.cs:272`；表驱动：`PlanetClimateSimulator.cs:187`→`DetermineBiome :336-363` | 无 | `TileSaveData.cs:16` biome | `FragmentationClimateConsistencyTests.cs:136/149` 2 处 | 缺"单一真源"：**两套分类器交替写同一字段**，最后一次写者胜（季节 tick 写表驱动，生成/气候重算写 Holdridge） |
| 6 | 水力侵蚀（HydraulicErosion） | 半跑 | 生成期 `PlanetTerrainGenerator.cs:170-173`；重算期 `GameWorld.Simulation.cs:504-518`（死路径） | 无 | 结果只以 `elevation01` 入档 | 无直接测试 | 缺"②驱动"：生成后不再运行；`ApplyToErosion`（`MapGenerationConfig.cs:432-439`）只在死路径生效 |
| 7 | 河网（HydrologySystem + TerrainGenerator.TrackRivers） | 半跑 | `PlanetTerrainGenerator.cs:218-221`（HydrologySystem）→ **随后被 `GameWorld.Generation.cs:151` `TrackRivers` 覆盖** | 无 | `isRiver` 入档 `TileSaveData.cs:23`；`riverStreamOrder/Width/Depth/Current/Flow` 入档但**无生产者** | `WaterNavigabilityTests` 3 例（手工构造 TileData） | 缺"②驱动+数据自洽"：真河网（流向/汇流/Strahler）算完即被"每 4 格取高程>0.65 顺坡走"的粗算法覆写；等级/河宽/河深全表恒 0 |
| 8 | 洋流（OceanCurrentSimulator） | 半跑 | `PlanetTerrainGenerator.cs:214-217` 一次性 | 无 | 不独立入档 | **0 引用** | 缺"②驱动"：只在开图跑一次，产出的暖/寒流仅经 `ApplyCoastalEffects` 微调海岸 |
| 9 | 海洋分区（OceanZoning） | 空转 | 唯一调用 `GenerationPipeline.cs:242`，而该管线无生产入口（见 #19） | 无 | 输出 `oceanTier` 入档（`TileSaveData.cs:30`） | `WaterNavigabilityTests.cs:13-17` 直接 `new` 测（测的是没人调用的类） | `ApplyToTiles` 全工程 1 处调用且在死路径；实际 `oceanTier` 由 `PlanetTerrainGenerator.cs:105` 按深度阈值给 |
| 10 | 省份划分（ProvinceGenerator + Province） | 在跑 | `GameWorld.Generation.cs:380-387`；异步 `:549-563` | 同 #1 | `SaveSystem.cs:446-458` 恢复 provinces | `ProvinceGeneratorTests` 6 例（直接 `new`） | 在跑；`ResolveCellsPerProvince`（`:363-378`）依赖 `GenConfig.ProvinceCount`（默认实例，无 UI） |
| 11 | 锚点/聚落选址（AnchorGenerator） | 在跑 | `GameWorld.Generation.cs:397`；`Anchors`→`SettlementData.anchor` `:402-405` | 同 #1 | `SettlementPersistence.RestoreSites`（`SaveSystem.cs:468`） | `WorldGenerationSmokeTests` 2 例、`HabitationSiteTests` 等 | 选址判据只有"省中心 / 该省有海岸 / 该省有边境省界 / 随机抽成员格"（`AnchorGenerator.cs:53-113`）；**不用 biome/fertility/河流/资源**；命名 `:221-247` 为 2×3×4 字模板 |
| 12 | 地形→聚落定型/瓶颈 | 半跑 | `AnchorGenerator.cs:139` → `SettlementTypologySystem.DeriveInitialType`；`DetectBottleneck :70-117`、`DetectPortTier :124-147` | 无 | settlement 字段入档 | `SiteTypeLabelsTests`、`SettlementBlockTests` | 半成品：`HasLandInDirection`（`:119-122`）**恒返回 true** → 任何海岸格都判成 `StraitCrossing`；`:85-98` 空循环体 |
| 13 | 地图渲染（MapRenderer，12 种模式） | 在跑 | `MapRenderer.cs:178-213` Update→`UpdateMapTexture`；模式分派 `MapRenderer.Colors.cs:120-214`；模式枚举 `MapRenderer.cs:742-756` | `UIManager.MapMode.cs:59-64` + `UIManager.cs:491-502`（**自带硬编码模式表**） | 无（纯读） | `MapVisibilityPlayModeTests` 等（PlayMode 28 处引用 MapRenderer） | 在跑且**读活数据**（1s 重绘，`MapRenderer.cs:55`）；Terrain 模式含高程归一化 `MapRenderer.Texture.cs:31-44`、山体阴影 `Colors.cs:74-116` |
| 14 | 虚控制三角（VirtualControlCalculator） | 在跑 | 渲染侧 `MapRenderer.Texture.cs:61-64`→`DrawVirtualControlOverlay`；数据侧 `MapWorldViewAdapter.cs:302` | 政治模式默认开（`MapLayerConfig.cs:73`） | 无（每次现算） | 0 引用 | 在跑；Bowyer-Watson 三角化（`VirtualControlCalculator.cs:70-130`），无测试 |
| 15 | 运行时地图编辑（F2） | 半跑 | `RuntimeMapEditingController.cs:75-121`（F2 开关、框选、↑↓←→、±0.05）；命令执行 `WorldEditService.cs:38-164` | 场景内实例（`Main.unity:5115-5118`，`editingActive: 0`）；**无 UI 按钮** | 编辑结果入档（elevation01/isLand/biome） | `MapEditCommandServiceTests` 11 例、`MapEditCommandTests` 3 例、`WorldRebindOnLoadTests` 2 例 | 缺"②正规入口"+每次编辑触发破坏性全邻域重算（见 §4-1） |
| 16 | Editor 地形画笔工具（TerrainPainter） | 不存在 | 目录仅 `BrushMode.cs`（枚举 Raise/Lower/Smooth）、`BrushShape.cs`（枚举）；`Get-ChildItem Assets/Editor/Tools/TerrainPainter` = 2 文件 | 无 `[MenuItem]`/`EditorWindow`：全工程 `Select-String '\[MenuItem\|EditorWindow\|\[CustomEditor'` = **0 命中** | — | 无 | 只有枚举，没有工具本体；`MapRenderer.SetMapEditController`（`:60`）全工程 1 处命中=定义，注入方不存在 → `HandleEditorInput`（`:248-267`）因 `_mapEditor==null` 恒早退 |
| 17 | 球面投影（SphericalMapRenderer / SetProjectionMode） | 不存在 | `MapRenderer.cs:610-616` 切完只 `ForceRefresh()`，注释自认"球形投影的具体渲染实现待完善"；`SphericalMapRenderer.cs` 不在任何场景/prefab（guid `fd2ccadd…` 命中 0） | 无 | — | 0 引用 | 显示模式层面的"球形地图"是空壳：纯平面网格（`MapRenderer.cs:337-358` 只有 4 顶点） |
| 18 | 图层注册表（MapLayerRegistry） | 空转 | `MapLayerRegistry.cs:67-85` 注册 14 条；**非测试引用 0**（`MapLayerRegistry\.` total=8，nonTest=0） | 无：UI 用自带 `ModeMap`（`UIManager.cs:491-502`） | — | `SimulationLaneAndLayerRegistryTests` 4 例 | 与 UIManager 硬编码表重复，生产零消费者 |
| 19 | 生成管线/配置化生成（GenerationPipeline、GenerateAll、GenerateTerrainWithConfig、TerrainTemplateSystem、RegionalTerrainShaper、MapGenerationConfig） | 空转 | `GenerationPipeline.cs:114 GenerateAll` 生产调用者 0；`GameWorld.Generation.cs:355 GenerateAll` 生产调用者 0（`WorldGenerationService.cs:40` 仅测试调用）；配置应用点 `:327-332`、`MapGenerationConfig.cs:407-422` 全在 `GenerateTerrainWithConfig`（`:292-353`） | 无：`Select-String 'MapGenerationConfig\|GenConfig' Assets/UI, Assets/Scripts/App` = 0 命中 | `GenConfig` 是 `GameWorld.Core.cs:157` 的字段默认实例，无 UI/无存档字段 | `WorldSetupOptionsTests` 6 例、`WorldServicesTests` 12 例 | 生产开图走 `GenerateTerrain(seed)`（`GameManager.cs:149`），**绕过全部 9 阶段管线与 8 个世界模板/区域模板/侵蚀/GCM 参数**；`GenerationStage.Biomes` 分支只打日志（`:250-252`） |
| 20 | 资源点生成（ResourceGenerator） | 空转 | `ResourceGenerator.cs:11-49`；生产调用者 0（`ResourceGenerator\.GenerateAll\(` total=0） | 无 | `TileData.resources` 不在 `TileSaveData` 中 | `ResourceDepthTests`（只测 `RollDepth`）、`NaturalResourceGenerationTests` 1 例 | `tile.resources` 的消费者真实存在（`InnovationTree.cs:599-666`、`TileInfoQueryAdapter.cs:72-91`、`UIManager.Panels.cs:497-500`），但从来没人生产 |
| 21 | 地形→行军成本桥接（BarrierSystem / BiomeAttributes.GetMovementCost） | 空转 | 唯一写者 `GameWorld.Simulation.cs:497` 在死路径；`BarrierSystem` 全工程 **1 处命中=类声明**（`BarrierSystem.cs:10`） | 无 | **不持久化**：读档硬写 `movementCost = 1f`（`GameSaveSystem.cs:439`） | 无 | 消费者真实：`MilitaryMovementSystem.cs:80`、`MapActor.cs:187`、`SettlementInfluenceSystem.cs:244`；生成世界的 `movementCost` 恒为 struct 默认 0（消费者按 `>0?:1` 兜底） |
| 22 | 气候→灾害/疾病（DisasterSchedule） | 在跑 | `GameWorld.Simulation.cs:796` 注册；`DisasterSchedule.cs:32-36` Daily→`DisasterSystem.DailyTick` | 自动规则驱动 | 灾害对 `annualTemp/fertility/soilHumidityPct` 的改写随 tile 入档（`DisasterSystem.cs:304-334`） | `ArchitectureContractsTests.cs:40` 注册断言 | 在跑，且是**气候字段的第三类写者**（`DisasterSystem.cs:320/323` 改 `annualTemp`） |
| 23 | 群系内容表（Biomes.json → BiomeDef → BiomeRegistry） | 半跑 | 加载 `ContentRuntime.cs:51-58`→`:100-111`←`ContentRegistry.cs:94`（`Bootstrap.cs:72`/`GameWorld.Core.cs:497` 启动）；消费 `BiomeAttributes.cs:486-529` | 无 | BiomeDef 不入档（重载即重建） | `BiomeRegistry` 测试引用 0 | 92 条数据只兑现 `movementCost`/`habitability` 两字段，而这两条读取路径都在死路径或零消费者（见 §3） |

**三态计数**：在跑 6（#1、#10、#11、#13、#14、#22）｜半跑 10（#2、#3、#4、#5、#6、#7、#8、#12、#15、#23）｜空转 5（#9、#18、#19、#20、#21）｜不存在 2（#16、#17）。

## 2. 数据模型现状

`TileData`（`Assets/Scripts/Core/MyriadCreation.Core.Data/TileData.cs:8-114`）本域相关字段的生产/消费矩阵：

| 字段 | 生产者 | 消费者 | 判定 |
|---|---|---|---|
| `elevation01` | `PlanetTerrainGenerator.cs:100`、`SeaLandGenerator.cs:301-302`、`PaintTerrain`（`GameWorld.Editing.cs:55`） | 渲染 `Colors.cs:134`、`AnchorGenerator.cs:224`、`SettlementTypologySystem.cs:83` | 在跑 |
| `isLand` / `isCoast` / `oceanTier` / `oceanDepth01` | `PlanetTerrainGenerator.cs:101-113`、`SeaLandGenerator.cs:303-347` | `Army.cs:366`、`BuildingAvailabilitySystem.cs:302-372`、`AnchorGenerator.cs:165` | 在跑 |
| `slopeDegree` | `PlanetTerrainGenerator.cs:138/199` | 渲染阴影 `Colors.cs:108`、`SettlementTypologySystem.cs:99` | 在跑 |
| `annualTemp` / `annualPrecipMm` / `airHumidityPct` / `climateZone` | **三个互斥写者**：GCM（`PlanetTerrainGenerator.cs:212`）、半经验（`PlanetClimateSimulator.cs:171-186`）、灾害（`DisasterSystem.cs:320-323`） | `MapRenderer.Colors.cs:141/148/170`、`DisasterSystem.cs:225-232`、`DiseaseSystem.cs:348-352` | 半跑（数据自洽性缺失） |
| `biome` | `PlanetTerrainGenerator.cs:228`（Holdridge）/ `PlanetClimateSimulator.cs:187`（表）/ 编辑器 `WorldEditService.cs:100` | 渲染 `Colors.cs:174`、`SettlementTypologySystem`、`InnovationTree` | 半跑（双分类器） |
| `fertility` | `PlanetTerrainGenerator.cs:230`、`GameWorld.Generation.cs:273-275`、灾害 `DisasterSystem.cs:313/326/329` | 渲染 `Colors.cs:142` | 在跑 |
| `isRiver` | `HydrologySystem.cs:308-315` → **被 `TerrainGenerator.cs:96-125` 全表覆写** | 渲染 `Colors.cs:128`、`SettlementTypologySystem.cs:104-112`、`SettlementInfluenceSystem.cs:246` | 半跑 |
| `riverStreamOrder` / `riverWidthMeters` / `riverDepthMeters` / `riverCurrentMps` / `riverFlowAccumulation` | **生产者 = 0**（`Select-String 'riverStreamOrder\s*=[^=]'` nonTest 仅 `GameSaveSystem.cs:135/427` 双向拷贝 + `TerrainConditions.cs:115` 只读拷出） | `RiverNavigability.cs:11-18`、`SettlementInfluenceSystem.cs:246`、`TerrainConditions.cs:50-52`、`SettlementTypologySystem.cs:138` | **承诺未兑现**（全表恒 0） |
| `waterAdjacentWeight` | `SeaLandGenerator.cs:334/345/488/494` | `PlanetClimateSimulator.cs:168/172/176/178/182` 等温/降水项 | 在跑，但**不入档**（`TileSaveData.cs` 无此字段），读档后为 0 → 读档世界气候与开图世界不同 |
| `soilHumidityPct` / `accumulatedTemp` / `frostFreeDays` / `diurnalTempRange` | 仅 `PlanetClimateSimulator.cs:172-185`（季节 tick 会跑） | `TransportCatalog.cs`、`TerrainConditions.cs:106` | 半跑：**不入档**，且只在半经验模型跑过之后才有值 |
| `movementCost` | `GameWorld.Simulation.cs:497`（死路径）/ `BarrierSystem.cs:38/43`（零调用）/ 读档 `GameSaveSystem.cs:439` 恒 1f | `MilitaryMovementSystem.cs:80`、`MapActor.cs:187`、`SettlementInfluenceSystem.cs:244` | **承诺未兑现** |
| `resources` | `ResourceGenerator.cs:31-43`（零调用） | `InnovationTree.cs:599-666`、`TileInfoPanel` | **承诺未兑现**（且不入档） |
| `isTerrainDirty` / `isClimateDirty` / `isEconomyDirty` | `WorldEditService.cs:204` 置 true | **0 消费者**（`WorldMapSimulationSystem` 用自己的 `HashSet`，`WorldMapSimulationSystem.cs:14-16`） | 空转字段 |
| `locked` | `PlanetTerrainGenerator.cs:98` 读、`FeatureManager.cs` 写 | 无其它读 | 半跑 |

`SettlementData.anchor`：`AnchorData.x/y` 恒 `0.5/0.5`（`AnchorGenerator.cs:127-128`），无任何消费者改写 → 锚点无亚格坐标。
`SaveData`：`movementCost`/`resources`/`waterAdjacentWeight`/`soilHumidityPct`/`isTerrainDirty` 均不在 `TileSaveData`（`TileSaveData.cs:7-35`）→ 读档即丢（见 §4-2）。

## 3. 内容数据现状

- `Assets/StreamingAssets/Base/Biome/Biomes.json`：24,312 字节，`ConvertFrom-Json` → `biomes.Count = 92`。
- 加载链：`Bootstrap.cs:72` / `GameWorld.Core.cs:497` → `ContentRegistry.cs:94` → `ContentRuntime.cs:51-58`（`Biome/Biomes.json`）→ `LoadBiomes :100-111` → `BiomeRegistry.Overrides`（`BiomeRegistry.cs:7`）。加载真实存在（非空转）。
- **字段兑现率**：`BiomeDef`（`BiomeDef.cs:10-28`）8 个数据字段中，有消费者的只有 `movementCost`（`BiomeAttributes.cs:486-488`）与 `habitability`（`:499-501`，但 `GetHabitability` 全工程 1 处命中=定义，**0 消费者**）。`color`（`ParseColor` 1 处命中=定义）、`fertilityMultiplier`、`isOcean`、`icon`、`iconDensity`、`category` **全部 0 消费者**。渲染器用的是自建 92 项硬编码色板（`MapRenderer.cs:421-481`），不读 JSON。
- 实际被使用的 92 条数据只有 `movementCost` 一条通道，而该通道的最终消费点（`GameWorld.Simulation.cs:497` → `tile.movementCost`）在死路径上 → **92 条内容的净效果 ≈ 0**。
- 与代码枚举规模对照：`MapRenderer.cs:421` `new Color[Enum.GetValues(typeof(BiomeType)).Length]`，硬编码赋值 60 项（`:423-481`），JSON 92 条 —— 两者条目集不同源。

## 4. 缺口清单（按影响排序）

| # | 缺口 | 影响 | 证据 | 严重度 |
|---|---|---|---|---|
| 1 | 每次地形编辑都把该格及邻域**再减一次海平面阈值** | 抬高一格 → 该格 `elevation01 -= _cachedSeaThreshold(≈0.5)` → 陆变深海，海岸/连通随之改写；读档后阈值=0 → 同一操作行为不同 | `SeaLandGenerator.cs:301-302`（`elevation01 = elevation01 - seaThreshold`）+ `:228`（dirty 用 `_cachedSeaThreshold`）+ `:665-689`（`EnsureRecalcState` 不重算阈值）+ `WorldEditService.cs:199-207`（每次编辑 `RecalculateDirty`）；测试自认：`WorldSeedAndFragmentationTests.cs:492-493` | P0 |
| 2 | 河网五字段零生产者，而 `isRiver` 被二次覆写 | 河运/水运/渡口规则在真实世界上永远走"无等级河道"分支；`riverDepth>=0.6` 类判定恒假 | `HydrologySystem.cs:308-315` 只写 `isRiver`；`PlanetTerrainGenerator.cs:220` 之后 `GameWorld.Generation.cs:151` 再 `TrackRivers` 全表重置 `isRiver`；grep 计数见 §2 | P0 |
| 3 | 双气候生产者 + 双群系分类器互写，季节 tick 全表重写 | `annualTemp/annualPrecipMm/biome` 的最终值取决于最后跑哪条路径；每季（约 91 天）陆地全局改一次气候与群系；群系图会随季度换色 | `GameWorld.Simulation.cs:360`→`PlanetClimateSimulator.cs:93-103/159-188`；对照 `PlanetTerrainGenerator.cs:209-213/228`、`GameWorld.Simulation.cs:496` | P0 |
| 4 | `movementCost` 与 `resources` 无生产者 | 地形完全不参与行军代价（运输/军事规则读恒 0/1）；资源点永远为空，而革新与地块面板在等这批数据 | `BarrierSystem` 1 命中；`GameSaveSystem.cs:439`；`ResourceGenerator.GenerateAll` 0 命中；消费者 `MilitaryMovementSystem.cs:80`、`InnovationTree.cs:599` | P0 |
| 5 | 生产开图绕过整条生成管线与全部世界生成配置 | 8 个世界模板、区域模板、侵蚀强度、GCM 季风/雨影/地形雨参数、省份数量/Lloyd 次数全部不可达；`MapGenerationConfig` 440 行无 UI 无存档 | `GameManager.cs:149`→`GameWorld.Generation.cs:140-155`；死链路 `:292-353`、`:355-361`、`GenerationPipeline.cs:114` | P0 |
| 6 | GCM 不接受运行季：`Season` 只在配置应用时写一次 | 所谓"GCM"的太阳辐射/季风项（`AtmosphericCirculation.cs:64-74`、`:105-110`）在整局内恒定 | `AtmosphericCirculation.cs:18`；唯一写点 `MapGenerationConfig.cs:429` | P1 |
| 7 | 聚落选址不含任何地理适宜性 | 聚落密度/类型与地形、河流、肥力、资源无关；命名 24 种组合 | `AnchorGenerator.cs:53-113`、`:221-247` | P1 |
| 8 | 地形瓶颈检测半成品 | `HasLandInDirection` 恒 true → 所有海岸聚落判成海峡渡口；`DetectBottleneck` 有一段空循环体 | `SettlementTypologySystem.cs:119-122`、`:85-98`、`:74-82` | P1 |
| 9 | 编辑工具链分裂：Editor 画笔不存在、MapRenderer 画刷注入方不存在、只剩 F2 运行时编辑器 | 无鼠标涂色画笔（只有框选+键盘 ±0.05+单格 API）；`IMapEditController` 契约空挂 | `Assets/Editor/Tools/TerrainPainter`（2 文件）；`MapRenderer.cs:60/248-267`；`RuntimeMapEditingController.cs:75-121` | P1 |
| 10 | 球面投影是空壳 | `MapProjectionMode.Spherical` 无渲染实现；`SphericalMapRenderer` 未进场景 | `MapRenderer.cs:610-616`；guid 命中 0 | P1 |
| 11 | 读档丢失地形派生状态 | `waterAdjacentWeight`/`soilHumidityPct`/`accumulatedTemp`/`frostFreeDays`/`movementCost` 不入档 → 读档后季节 tick 用 0 值重算气候 | `TileSaveData.cs:7-35` 对照 `PlanetClimateSimulator.cs:168-185` | P1 |
| 12 | `MapLayerRegistry` 与 UI 模式表双源 | 新图层只注册到 registry 不会出现在 UI；反之亦然 | `MapLayerRegistry.cs:67-85`（非测试引用 0）对照 `UIManager.cs:491-502` | P2 |
| 13 | 叠加层开关无消费者/无 UI | `showArmyMarkers`/`showTradeRoutes`/`showCoastline`/`showRivers`/`showTerrainBase` 渲染端 0 读取；`ToggleOverlay` 非测试 1 命中=定义 | `MapLayerConfig.cs:53-71`；grep 计数见 §1-18 | P2 |
| 14 | `OceanZoning`/`CyowariMapStyle` 死件 | `CyowariMapStyle` 非测试引用 0 且不在场景；`OceanZoning` 仅死管线调用 | `CyowariMapStyle.cs:19`；`GenerationPipeline.cs:242` | P2 |
| 15 | 破碎化 `QuantileNormalize` 恒返回 0.5 | 日志"海平面阈值"恒 0.5000，海陆比例实际由 `TargetLandFraction` 决定但表现为常数，易误判 | `PlanetTerrainGenerator.cs:292-312`（`:311 return 0.5f`） | P2 |

## 5. 测试覆盖（本域）

`Assets/Tests/EditMode/` 相关 17 文件、**89 个 `[Test]`/`[TestCase]`**（逐文件 `Select-String '^\s*\[Test\]|^\s*\[TestCase'`）：
`WorldSeedAndFragmentationTests` 14、`WorldServicesTests` 12、`MapEditCommandServiceTests` 11、`MapWorldViewAdapterTests` 7、`ProvinceGeneratorTests` 6、`MapUnitTests` 6、`WorldSetupOptionsTests` 6、`SeaLandRecalcDeterminismTests` 5、`SimulationLaneAndLayerRegistryTests` 4、`FragmentationClimateConsistencyTests` 3、`WaterNavigabilityTests` 3、`MapEditCommandTests` 3、`SettlementInfluenceTests` 3、`WorldGenerationSmokeTests` 2、`RegionalTerrainShaperTests` 2、`SeaLandPerformanceTests` 1、`NaturalResourceGenerationTests` 1。

- **真从入口驱动**：`WorldSeedAndFragmentationTests`、`SeaLandPerformanceTests`、`FragmentationClimateConsistencyTests`、`WorldGenerationSmokeTests` 等走 `world.GenerateTerrain(seed)`（生产同款入口，如 `WorldGenerationSmokeTests.cs:71/87`、`WorldSeedAndFragmentationTests.cs:272/363`）。
- **直接 `new` 对象**：`ProvinceGeneratorTests.cs:39/90/106`（直构生成器）、`WaterNavigabilityTests.cs:27-29`（手写 TileData）、`MapUnitTests.cs:78-80`、`ResourceDepthTests.cs:67-74`、`SimulationLaneAndLayerRegistryTests.cs:48-70`（测一个生产零引用的注册表）。
- **零测试**：`AtmosphericCirculation`（GCM）、`OceanCurrentSimulator`、`HoldridgeBiomeClassifier` 仅 2 处间接断言（`FragmentationClimateConsistencyTests.cs:136/149`）、`VirtualControlCalculator`、`SphericalMapRenderer`、`BarrierSystem`、`TerrainTemplateSystem`、`FeatureManager`。
- 断言强度：`WorldSeedAndFragmentationTests.cs:530` 只断言"脏格邻域发生了变化（`changedInside > 0`）"，未断言变化方向/量级 → 上述 §4-1 的破坏性重算在测试里是"通过"的。

## 6. 我核实不了的事

1. 未实际运行 Unity（无 batchmode 编译/测试执行）→ 89 个用例是否全绿、以及 §4 各 P0 在运行时是否真能复现（例如 `_cachedSeaThreshold` 在初始化后是否真为非 0），只有代码路径与注释/测试注释两类证据，没有运行日志。
2. `Main.unity` 中 MapRenderer 的实际序列化参数（`displayMode`/`_layerConfig` 各开关初值）只核到字段名与 `editingActive: 0`，未逐字段比对默认值与场景值。
3. `TargetLandFraction=0.3` 的最终陆地比例，以及 1024×512 下 `HydraulicErosion`/`AtmosphericCirculation` 的耗时与内存，未实测（仅见 `SeaLandPerformanceTests` 存在但未运行）。
4. `_fragmentNoise`（`SeaLandGenerator.cs:93`）在 `SetFragmentNoise`（编辑器画笔）之外的取值是否被任何 UI 覆盖，未找到入口，但无法排除通过反射/场景序列化注入。
5. `WorldConfig`（ScriptableObject）中 `landAmount`/`seaLevel`/`landFragment`/`oceanBuffer` 是否有 settings 面板可改（`SettingsPanel.cs` 630 行未逐行读），因此"玩家可否影响海陆比例"未定。
6. `AtmosphericCirculation` 与 `PlanetClimateSimulator` 谁在**开图结束后**的最终值胜出，取决于 dirty 队列时序；仅静态阅读得出"季节 tick 是最后写者"，未运行时验证。
