# 地形动态（地质/气候/侵蚀）接入审计与方案

> 类型：只读评估（task-135）。未修改任何代码/资产，未跑 Unity/编译/测试。
> 范围：地图生成管线、WorldSetupOptions/MapGenerationConfig、SimulationScheduler/DisasterSchedule、TileSaveData/SaveSystem、ITileInfoQuery/TileInfoDto、运行期全图遍历与随机性。
> 目标：为 vegetation / salinity / erosion / plate 等地形动态字段与运行期演化提供**可插入点、存档兼容策略、UI 契约与风险清单**。

---

## 0. 结论摘要

1. **生成管线**当前是 9 阶段静态数组（Seed→Terrain→Features→Climate→Hydrology→Biomes→Provinces→Settlements→Society），但 `Biomes` 是空实现，且 `Terrain` 阶段调用的 `GenerateTerrainWithConfig()` 已经生成省份/聚落/社会，后续 3 阶段重复；默认新开局（`GenerateTerrain`/`GenerateTerrainAsync`）**根本不走 GenerationPipeline**，也不跑 GCM/水文。新增板块/侵蚀阶段前必须先统一“谁执行管线”。
2. **运行期没有任何气候/地质演化调度**：`SimulationCadence.LongTerm=365` 已存在但全仓 0 处注册；现有 `DisasterSchedule` 是 Daily 模式范例；建议新增 `terrain.dynamics.longterm`（LongTerm，order 8~9，在 `WorldMapSchedule` order 10 之前），只增量改脏块，禁止全图重算。
3. **确定性是最大风险**：`UnityEngine.Random` 无种子全局状态、`MapGenerationConfig.GetActualSeed()` 在 Seed<0 时 `new Random().Next()`、`GameWorld.ThreadRng` 用 `Environment.TickCount`、`DisasterSystem/ResourceGenerator` 用 Unity Random；存档不保存 RNG 状态/active disasters。读档后同一 tick 不可能复现，运行期演化必须改为 **确定性随机（seed+year+tile/plate 派生）**，并把演化状态入档。
4. **存档**：当前 `SaveVersion=16`，`TileSaveData` 已有 26 字段；新增 vegetation/salinity/erosion/plate 建议 **升 17 + 迁移**（v16 读档时按气候/地形补默认值）；若只是每次读档重算的纯派生值，才考虑保持 16，但无法区分“旧档缺失”与“新档合法 0”。
5. **UI 已有** `annualTemp/annualPrecipMm/biome/fertility`（TileInfoDto + 地块面板），但缺湿度/土壤/霜冻/河流/植被/盐碱/侵蚀/板块；新增动态字段必须同时扩 `TileInfoDto` + `TileInfoQueryAdapter` + 面板。
6. **不合理设计**见 §5，共 10+ 条证据（静态气候、`isClimateDirty` 死字段、灾害硬编码无 JSON、生成参数不可配、全图遍历、管线重复/空阶段、RNG 不确定、active disaster 不入档等）。

---

## 1. 地图生成管线：阶段、顺序、可插入点

### 1.1 两条“管线”现状（重要）

| 路径 | 入口 | 实际执行内容 | 证据 |
|---|---|---|---|
| A. 默认新开局（同步） | `GameManager.StartNewGame` → `world.GenerateTerrain(seed)` | `GenerateTerrainBase`（PlanetTerrainGenerator + 河流 + 初始破碎化 + 派生气候补算）→ 政权/省份/聚落/初始单位；**不跑 GCM/水文** | `GameWorld.Generation.cs:122-138`、`140-155`、`169-188` |
| B. 默认新开局（异步） | `GameManager.StartNewGameAsync` → `GenerateTerrainAsync` | `GenerateTerrainBase` → 省份（后台 Task）→ 聚落/单位；**同样不跑 GCM/水文** | `GameWorld.Generation.cs:536-575` |
| C. 全量生成 | `GameWorld.GenerateAll()` | `GenerateTerrainWithConfig` → `CalculateClimate` → `RecalculateHydrology` | `GameWorld.Generation.cs:355-362` |
| D. GenerationPipeline | `GameWorld.Pipeline.GenerateAll()` / `RegenerateFromPipeline` | 9 阶段（见下表）；**仅 `RegenerateFromPipeline` 使用**，默认开局不用 | `GameWorld.Core.cs:264/484`、`GameWorld.Generation.cs:576-582`、`GenerationPipeline.cs:12-23` |

> 结论：**新增地形动态不能只加进 GenerationPipeline**——默认开局不会执行它；必须同时改 `GenerateTerrain(Async)`/`GenerateAll` 或让 GameManager 统一走 Pipeline。

### 1.2 GenerationPipeline 阶段表

| # | Stage | ExecuteStage 行为 | 证据 | 评价 |
|---:|---|---|---:|---|
| 1 | `Seed` | 只读 `GenConfig.GetActualSeed()` 打日志；**不写 randomSeed** | `GenerationPipeline.cs:232-237` | 名不副实 |
| 2 | `Terrain` | `_world.GenerateTerrainWithConfig()`（内部已含政权/省/聚落/单位） | `GenerationPipeline.cs:238-240`、`GameWorld.Generation.cs:292-360` | 后续阶段重复 |
| 3 | `Features` | `OceanZoning.ApplyToTiles` | `GenerationPipeline.cs:241-243` | 可插入板块/海岸特征 |
| 4 | `Climate` | `_world.CalculateClimate()`（全图 GCM + biome/movementCost） | `GenerationPipeline.cs:244-246`、`GameWorld.Simulation.cs:460-490` | 可插入植被/盐碱派生 |
| 5 | `Hydrology` | `_world.RecalculateHydrology()`（全图水力侵蚀+河网） | `GenerationPipeline.cs:247-249`、`GameWorld.Simulation.cs:492-518` | 可插入侵蚀/泥沙 |
| 6 | `Biomes` | **空实现**，只打日志“群系已在气候阶段划分” | `GenerationPipeline.cs:250-252` | 应改造成植被/盐碱/侵蚀派生阶段 |
| 7 | `Provinces` | 再调 `_world.GenerateProvinces()` | `GenerationPipeline.cs:253-255` | Terrain 已生成，重复 |
| 8 | `Settlements` | 再调 `GenerateSettlements()` | `GenerationPipeline.cs:256-261` | 重复 |
| 9 | `Society` | 再调 `InitializeDefaultRealms()` | `GenerationPipeline.cs:262-267` | 重复 |

**可插入点**（推荐）：
- `PlateTectonics` / `PlateId`：若板块生成仍由 `PlanetTerrainGenerator` 内部 Voronoi 完成（`PlanetTerrainGenerator.cs:82`、`MapGenerationConfig.PlateCount:254`/`PlateBoundaryBoost:251`），在 **Terrain 之后**新增 `Tectonics` 阶段输出 `plateId/tectonicStress`；或把板块拆出为 Terrain 前置阶段（需改 `PlanetTerrainGenerator`/Terrain API）。
- `Vegetation` / `Salinity`：放在 **Climate 之后**（依赖 annualTemp/annualPrecip/biome/fertility/coast），可复用空转的 `Biomes` 阶段或新增 `Surface` 阶段。
- `Erosion` / `Sediment`：放在 **Hydrology 之后**（复用 HydraulicErosion 的坡度/流量，但不要把全图侵蚀每 tick 再跑）。
- `LongTerm` 演化**不进生成管线**，走 §2 的运行期调度。

### 1.3 WorldSetupOptions 现有开关与缺失开关

**现有字段**（`World/MyriadCreation.World.Generation/WorldSetupOptions.cs:9-31`）：
`initialContent`、`generatePolities`、`generateSettlements`、`generateInitialUnits`、`initialPolityCount`、`initialPopulationScale`、`initialSettlementCount`、`initialUnitCount`、`spawnRates`（`enableNaturalSpawning`/`mapActorMultiplier`）。
Preset：`Empty/GeographyOnly/Minimal/Standard/Custom`（`WorldSetupOptions.cs:58-101`）；`Normalize` 只 clamp 数量（`103-126`）。

**缺失开关（建议新增）**：

| 类别 | 建议字段 | 理由 |
|---|---|---|
| 生成阶段 | `generatePlates`、`generateClimate`、`generateHydrology`、`generateErosion`、`generateVegetation`、`generateSalinity` | 当前这些阶段/派生永远执行或永远不执行（`GenerateAll` 固定跑 GCM/水文；`GeographyOnly` 也照跑地形基础） |
| 运行期动态 | `enableRuntimeTectonics`、`enableRuntimeClimateDrift`、`enableRuntimeErosion`、`dynamicsIntervalYears` | 无任何运行期开关；LongTerm 空置 |
| 演化速率 | `climateDriftPerYear`、`plateSpeedPerYear`、`erosionPerYear`、`seaLevelDeltaPerYear` | 现有 `MapGenerationConfig` 只有生成期强度（`ErosionRate:268` 等），没有运行期速率 |
| 灾害 | `disasterFrequencyScale`、`disasterSeverityScale`、`enableDisasters` | 灾害频率/时长/严重度硬编码（§5） |
| 存档 | `saveDynamicTileFields`、`tileDynamicsVersion` | 决定 v16 保持还是 v17 迁移 |
| 配置桥 | `MapGenerationConfig` 整体引用/预设 | **当前 `MapGenerationConfig` 只在 `GameWorld.GenConfig` 字段（`GameWorld.Core.cs:153`），WorldSetupOptions/UI 都没有入口** |

**MapGenerationConfig 已有但不可从 SetupOptions/UI 配置的生成参数**（`MapGenerationConfig.cs:112-277`）：
`Basemap`(114)、`TerrainScale`(118)、`WorldTemplate/RegionalTemplate`(121/124)、`Seed`(127)、`MapSize`(136)、`ProvinceCount`(140)、`Shape`(154)、`SeaLevel`(169)、`LandAmount`(173)、`Fragmentation`(177)、`Circulation`(185)、`GlobalTemperatureOffset`(201)、`RiverDensity`(206)、`ErosionIntensity`(210)、`PlateCount`(254)、`PlateBoundaryBoost`(251)、`ErosionRate/Deposition/Evaporation/Inertia`(268-277) 等；仅 `GameWorld.GenConfig` 公开字段（`GameWorld.Core.cs:153`）可被 Inspector/代码设置，`GameManager` 从未赋值（grep 仅 `Generation` 内部使用），UI 也没有入口。
**硬编码常量**：`PlanetOmega`(279)、`PlanetRadius`(281)、`SolarConstant`(283)、`UseMultithreading`(285)、`OceanIterations`(287)、`ErosionMaxLifetime`(289) 不可配。

---

## 2. 运行期调度：cadence、灾害模式、地质/气候注册与确定性

### 2.1 调度器 cadence 注册方式

- `SimulationCadence`：`Immediate/Daily/Weekly/Monthly/Seasonal/LongTerm`（`SimulationCadence.cs:3-11`）。
- `SimulationScheduler.Register(id, cadence, order, execute)`（`SimulationScheduler.cs:43-67`）/`RegisterDirty(...dirtyKey...)`（`69-93`）；`TickBudgeted` 条件：`intervalDays<=1 || day % intervalDays == 0`（`120-172`，条件在 `131`）。
- cadence→天数：`GetIntervalDays`（`174-184`）——`Weekly=7, Monthly=30, Seasonal=90, LongTerm=365, 其余=1`。
- 当前已注册 cadence：**全部 Daily，除了 `sandbox.spawn.monthly`（Monthly）**；`grep SimulationCadence.LongTerm` 除枚举/映射外 **0 注册**。
- **风险**：`TickBudgeted` 用 `day % intervalDays == 0`；若 `daysPerTick>1`（`GameWorld.tickInterval/daysPerTick`），`currentDay` 可能跳过 365 的整数倍，LongTerm 调度会漏年。运行期演化应使用 **year 续接/年份 guard**（参考 `laws.effects.yearly` 在 Daily 里 `year` guard，`GameWorld.Simulation.cs:789-794`），或新增 `Decadal/Century` cadence 并处理跨 day 跳变。

### 2.2 DisasterSchedule 模式（建议复制）

`DisasterSchedule`（`Simulation/MyriadCreation.Simulation.Systems/DisasterSchedule.cs:8-36`）：
- `ScheduleId = "disaster.daily"`；`Register(scheduler, disasters, diseases, Func<int> day, Func<int> year)`；
- `scheduler.Register("disaster.daily", SimulationCadence.Daily, 11, ctx => { disasters.DailyTick(day(), year()); diseases.DailyTick(day(), year()); });`
- 注册点：`GameWorld.Simulation.cs:774-835` 的 `RegisterSimulationSchedules`（`DisasterSchedule.Register` 在 `775`；order 10-37 固定排序）。
- 模式：静态 `XxxSchedule` 类 + `const ScheduleId` + `Register(ISimulationScheduler, deps..., day/year funcs)`；新系统应沿用。

### 2.3 运行期地质/气候演化的注册点（建议）

新增 `Simulation/.../TerrainDynamicsSchedule.cs`（模式同 DisasterSchedule），并在 `RegisterSimulationSchedules`（`GameWorld.Simulation.cs:774`）注册：
```
_simulationScheduler.Register("terrain.dynamics.longterm", SimulationCadence.LongTerm, 9, ctx =>
{
    _terrainDynamics?.TickYear(currentYear, ctx.deltaDays);
});
```
- **order 9**：早于 `WorldMapSchedule` order 10（`WorldMapSchedule.cs:22`），让演化写入的脏块在同 tick 被 `world.map.recalculate-dirty` 消费；若只改 climate/biome 不写地形高程，可直接写 tile 并 `worldMapSimulation.MarkClimateDirty`/`MarkTileDirty`，不触发全量 `RecalculateAll`。
- **板块/十年级**：`LongTerm`（365）年度执行；十年级事件在 tick 内部用 `currentYear % 10 == 0` guard（或新增 `Decadal` cadence，避免 `daysPerTick` 跳年问题）。
- **只增量**：用 tile 子集/板块边界/预计算脏块，禁止调用 `CalculateClimate`/`RecalculateHydrology`/`RecalculateAll` 全图方法（§5）。
- `LongTerm` 失败/跳年时可在 `GameWorld.AdvanceTime`（`GameWorld.Simulation.cs:338`）后用 `currentYear` 记录 `_lastDynamicsYear` 补算，但补算仍需确定性。

### 2.4 确定性 / 读档一致性评估（关键）

| 证据 | 问题 |
|---|---|
| `DisasterSystem.cs:7` `using Random = UnityEngine.Random;`；`179` `Random.value < baseFrequency*...`；`209` `Random.Range(minDuration,maxDuration+1)` | 全局 Unity RNG，未保存 state；读档后灾害序列/时长不同；且 `FindDisasterOriginTile` 用 `Random` 选中心（`218` 起） |
| `ResourceGenerator.cs:68/72/73` `Random.value/Range` | 地形生成期资源投放不可复现；同 seed 两次生成资源不同 |
| `MapGenerationConfig.cs:127` `Seed=-1`；`363-373` `GetActualSeed()` 当 Seed<0 返回 `new Random().Next()` | 默认配置下生成 seed 不确定；即使 `GenerateTerrain(seed)` 传 seed，`GenerateTerrainWithConfig` 走 `GenConfig` 时仍可能随机 |
| `MapGenerationConfig.cs:391` `RandomizeSeedString` 用 `new Random()` | UI 随机种子按钮不可复现 |
| `GameWorld.Core.cs:119` `ThreadLocal<Random>(() => new Random(Environment.TickCount ^ ManagedThreadId))`；`GameWorld.Simulation.cs:74` 用 `ThreadRng.Value.NextDouble()` | 人口/其它系统随机不可复现，且与演化共享全局随机源风险 |
| `DisasterSystem.cs:20` `_activeDisasters`；无 `ActiveDisaster` 入档（`GameSaveData`/`SaveData` 无 disasters 字段） | 读档后灾害列表丢失/不同；运行期灾害与地形动态耦合时更难复现 |
| 正向：`HydraulicErosion.cs:42 new Random(seed)`、`SphericalVoronoi.cs:29`、`SphericalNoise.cs:33`；`SeaLandGenerator` 注释 `78-79/121/354` 明确同 seed 可复现 | 地形生成基础已用 System.Random，可作为运行期随机实现范式 |

**建议**：
- 运行期地质/气候 `IRandomSource`：`seed = Hash(world.randomSeed, scheduleId, year, tileIndex/plateId)`，每次纯函数 roll；不用 `UnityEngine.Random`、不用 `ThreadRng`。
- 若演化需要连续随机流，在 `GameSaveData` 增加 `evolutionRandomState`/`dynamicsYear`/`plateStress` 等状态，读档恢复；**至少 active disasters 要入档**（或明确“读档后灾害清空”的产品决策）。
- 存档已有 `world.randomSeed`（`GameSaveData.cs:34` 保存、`GameSaveSystem.cs:74/343` 读写、`SaveData.cs:33` v16 起），可作为确定性的根种子；但 `GetActualSeed` 的 -1 随机必须改成“Bootstrap 先固化 seed 再保存”。

---

## 3. 存档兼容：TileSaveData 与 SaveVersion

### 3.1 当前字段

- `TileSaveData`（`Infrastructure/.../TileSaveData.cs:7-35`）26 字段：
  `exists,isLand,elevation01,slopeDegree,annualTemp,annualPrecipMm,airHumidityPct,biome,climateZone,fertility,provinceId,ownerRealmId,occupyingRealmId,isCoast,isRiver,riverStreamOrder,riverFlowAccumulation,riverWidthMeters,riverDepthMeters,riverCurrentMps,seaConnectId,oceanTier,roadLevel,development,stability,order`。
- `TileData`（`Core.Data/TileData.cs:10-110`）还有大量字段**不入档**：
  `locked(34), terrainShade(40), isGate(42), passable(46), movementCost(48), hasBarrier(50), barrierOwnerRealmId(52), barrierStrength(54), nearbyFortId(56), fortInfluenceLevel(58), oceanDepth01(66), waterAdjacentWeight(70), diurnalTempRange(74), soilHumidityPct(80), accumulatedTemp(82), frostFreeDays(84), isTerrainDirty(106), isClimateDirty(108), isEconomyDirty(110), buildingLevels(98), populationBlocks(100), resources(102), campId(104)`。
  读档时 `passable=true/movementCost=1/buildings=empty/populationBlocks=empty` 被重置/重建（`GameSaveSystem.cs:383-420`），说明“派生/运行时字段”本来就允许不入档或重算。
- 写入/读取：`GameSaveSystem.SaveMapData`（`110-146`）、`LoadMapData` 还原（`383-420`）；`SaveVersion=16`（`GameConstants.cs:13`）；`SaveSystem` 版本不匹配只 warning（`SaveSystem.cs:263-265`）；`GameSaveData.tiles`（`GameSaveData.cs:38`）、旧 `MapSaveData.tiles`（`MapSaveData.cs:18`）。

### 3.2 新增字段方案

建议新增（命名可按最终设计调整）：
```
float vegetation01;   // 0..1 植被覆盖（派生自 biome/climate/fertility，可演化）
float salinity01;     // 0..1 盐碱化（coast/arid/irrigation 派生）
float erosion01;      // 0..1 累计侵蚀/沉积状态（运行期演化）
int   plateId;        // 板块 id（生成期 Voronoi 分配；运行期可移动）
```
可选：`float sediment01`、`int tectonicStress`、`float soilMoisture01`（若把土壤湿度也持久化）。

**方案 A（推荐）：升 SaveVersion 17 + 迁移**
- 改 `GameConstants.SaveVersion=17`；`TileSaveData` 增字段；`SaveMapData` 写、`LoadMapData` 读；`GameSaveData` 可加 world 级 `dynamicsState`（进化年份/海平面偏移/板块速度）。
- `SaveSystem` 读旧档：`data.version < 17` 时在 `ApplyToWorld`/`LoadMapData` 后执行 `TileDynamicsMigration.EnsureDefaults(world)`：按 `biome/annualTemp/annualPrecip/elevation/slope/coast` 计算 vegetation/salinity/erosion 初值，plateId 由生成期板块地图回填（或 -1 表示未知）；不把旧档的 0 当合法值。
- 旧档仍可加载（`SaveSystem` 已有版本 warning 机制，`SaveSystem.cs:263-265`）；**写回**统一写 17；`SaveSystemRoundTripTests`/`LegacySaveCompatibilityTests` 增加 v16→17 用例。
- 回滚：新增字段/版本号单独提交；旧档迁移函数可回退。

**方案 B：保持 SaveVersion 16（仅当字段每次读档可完全重算）**
- 加字段但 `TileSaveData` 缺省 0；`LoadMapData` 后无条件 `EnsureDerived(world)` 重算四字段；因此不区分旧档/新档 0。
- 风险：运行期演化产生的 erosion/plate/vegetation 历史**不能**靠重算恢复；若产品接受“读档后地形动态回退到静态派生”，才可选 B。
- 不建议把状态型字段（erosion/plate 演化）走 B。

**其它兼容点**：
- `TileSaveData` 是 struct 数组，JsonUtility 对缺失字段默认 0/false，新增字段本身不会破坏旧档解析；但“缺失 vs 合法 0”必须靠 SaveVersion/迁移函数区分。
- 1024×512=524,288 tiles；新增 4 字段（float/int）JSON 体积显著增加（约数 MB~十几 MB），可考虑只存动态脏块/差分，但第一版先完整 struct 保持简单。
- `TileInfoDto`/`TileData`/`TileSaveData` 三处字段必须同步；`GameSaveSystem` 映射漏一处会导致“读档后值丢失”。
- 读档顺序：`LoadMapData` 还原 tiles → `ReinitializeSubsystems`/`PrepareLoadedWorld`（`SaveSystem.cs:458-496`）→ 新动态字段必须在子系统重建后、WMS 重算前恢复/补算。

---

## 4. UI：ITileInfoQuery / TileInfoDto 覆盖与缺口

- `ITileInfoQuery`（`Core.Contracts/TileCharacterContracts.cs:76-80`）：`TileInfoDto GetTileInfo(int tileIndex)`；`IWorldReadModel` 已组合 `ITileInfoQuery`（`IWorldReadModel.cs:9-10`）。
- `TileInfoDto`（`TileCharacterContracts.cs:20-73`）现有：`tileIndex/x/y/exists/terrainName/elevation01/slopeDegree/isLand/biomeId/biomeName/fertility/climateZoneId/climateZoneName/annualTemp/annualPrecipMm/ownerRealmId/ownerRealmName/provinceId/provinceName/settlementAnchorId/settlementName/population/development/stability/resources`。
- **已暴露**：`annualTemp`、`annualPrecipMm`、`biome/biomeName`、`fertility`（以及气候带/海拔/坡度/人口/资源）。
- **未暴露**：`airHumidityPct`（TileData:78）、`soilHumidityPct`(80)、`accumulatedTemp`(82)、`frostFreeDays`(84)、`diurnalTempRange`(74)、`isCoast`(62)、河流字段（`isRiver/riverStreamOrder/flow/width/depth/current`，20-30）、`oceanDepth01`(66)、`waterAdjacentWeight`(70)、道路/关卡/屏障/堡垒（44-58）、`campId`(104)；以及新增 `vegetation01/salinity01/erosion01/plateId`。
- `TileInfoQueryAdapter`（`Simulation.WorldState/TileInfoQueryAdapter.cs:31-53`）当前只映射上述子集；地块面板 `UIManager.Panels.cs:469/485-489` 展示“年均温/年降水/温度带/群系/肥力”，以及地形/人口/资源/省份/聚落。
- **建议 DTO 扩展**（向后兼容，只加字段）：
  `airHumidityPct`、`soilHumidityPct`、`frostFreeDays`、`accumulatedTemp`、`isCoast`、`isRiver/riverStreamOrder/riverFlowAccumulation/riverWidthMeters/riverDepthMeters/riverCurrentMps`、`vegetation01/salinity01/erosion01/plateId`；`TileInfoQueryAdapter` 从 TileData 映射；面板增加“湿度/土壤/植被/盐碱/侵蚀/板块/河流”行（可折叠）。
- 新增字段在 `TileInfoDto` 必须是 Core 可表达类型（int/float/bool/string），不能引 World/Simulation 类型；符合现有契约模式。

---

## 5. 不合理设计证据清单（file:line）

| # | 问题 | 证据 | 影响 |
|---:|---|---|---|
| 1 | **静态气候，永不运行期更新** | `CalculateClimate` 仅在 `GenerationPipeline.cs:244-246` 和 `GameWorld.Generation.cs:358` 调用；`RegisterSimulationSchedules`（`GameWorld.Simulation.cs:774-835`）无气候/地质 schedule；`WorldMapSchedule`（`WorldMapSchedule.cs:22`）只在 `world.terrain` dirty 时重算，`CalculateClimate` 不标该 dirty | 气候/群系/肥力不随时间变化；动态气候无入口 |
| 2 | **`isClimateDirty` 死字段** | 唯一出现 `TileData.cs:108`；实际 dirty 由 `WorldMapSimulationSystem.cs:16/72-78` 的 `_climateDirtyTiles` 管理；`CalculateClimate` 调 `MarkClimateDirty`（`GameWorld.Simulation.cs:481`）但从不写 tile 字段 | 误导读者、无法按 tile 追踪气候脏状态 |
| 3 | **灾害定义硬编码，Content `DisasterDef` 未接 JSON** | `DisasterSystem.cs:32-150` 硬编码 12 个 `DisasterDef`；`Content.Definitions/DisasterDef.cs:6`；`StreamingAssets` 无 disaster JSON、`ContentRuntime` 无 Disaster loader；`DisasterDef` 仅 `DisasterSystem`/`ActiveDisaster` 用 | 灾害频率/时长/严重度不可内容化/热更/本地化 |
| 4 | **生成参数不可配** | `MapGenerationConfig.cs:114-277` 参数齐全，但 `WorldSetupOptions.cs:9-31` 无地形/气候/板块/侵蚀开关；`GameWorld.GenConfig` 仅 `GameWorld.Core.cs:153` 字段，`GameManager`/UI 未赋值（grep）；`MapGenerationConfig.cs:279-289` 物理常量硬编码 | 玩家/内容作者无法调地形；只能改代码/Inspector |
| 5 | **生成 seed 不稳定** | `MapGenerationConfig.cs:127 Seed=-1`；`GetActualSeed` `363-373` 当 Seed<0 `new Random().Next()`；`RandomizeSeedString` `391+` 用未播种 Random | 同配置两次生成地形不同；存档 seed 跟生成 seed 可能不一致 |
| 6 | **Unity Random 全局状态** | `DisasterSystem.cs:7/179/209`；`ResourceGenerator.cs:68/72/73`；`GameWorld.Core.cs:119` + `GameWorld.Simulation.cs:74`；`ThreadRng` 用 TickCount | 读档/重放不一致；跨系统随机耦合；无法复现 |
| 7 | **active disaster 不入档** | `DisasterSystem.cs:20` 列表；`GameSaveData`/`SaveData` 无 disaster 字段（grep）；`SaveSystem` 无恢复 | 读档灾害消失/时序错乱，动态地形若有灾害耦合会分叉 |
| 8 | **管线重复/空阶段/默认不走管线** | `GenerationPipeline.Terrain` 调 `GenerateTerrainWithConfig`（`GenerationPipeline.cs:238-240`，该函数内部已生成省/聚落/社会 `GameWorld.Generation.cs:340-354`）→ Pipeline 又跑 Provinces/Settlements/Society（`253-267`）；`Biomes` 空实现（`250-252`）；`Seed` 只打日志（`232-237`）；默认 `GenerateTerrain` 不走 Pipeline（`GameWorld.Generation.cs:122-138`） | 阶段进度/重算语义不可靠；重复生成成本；新阶段插不进去 |
| 9 | **同步/异步生成不跑 GCM/水文** | `GenerateTerrain` `122-138`、`GenerateTerrainAsync` `536-575` 只 `GenerateTerrainBase` + 省/聚落/单位；`GenerateAll` 才气候/水文（`355-362`） | 新开局气候/水文与 Editor GenerateAll 不一致；动态气候没有基线 GCM |
| 10 | **全图遍历成本** | `CalculateClimate` 三次 `tiles.Length` 循环（`GameWorld.Simulation.cs:469/477/483`）；`RecalculateHydrology` `498-508` + `HydraulicErosion.Run` 全表；`WorldMapSimulationSystem.RecalculateAll` `98-106`/`RecalculateDirty` `108-129`；`MarkAllTerrainDirty` `53-59`；`DisasterSystem.FindDisasterOriginTile` `218+` 扫全图；`TryTriggerDisasters` `172-183` 每 tick 12 定义；`MapGenerationConfig.cs:46-51` 1024×512=524288 地块 | 若把演化挂 Daily 会卡顿；必须增量/分批/采样 |
| 11 | **气候脏标记冗余** | `CalculateClimate` `481` 对每 tile `MarkClimateDirty`，随后 `GameWorld.Simulation.cs:508/515` 的 WMS `RecalculateAll` 清空；`isClimateDirty` 未用（#2） | 无效集合写入，掩盖真正按 tile 的脏语义 |
| 12 | **`GeographyOnly` 不“地理-only”** | `WorldSetupOptions.ApplyPreset` `71-79` 只关政权/聚落/单位；`GenerateTerrain` 仍生成地形基础；`GenerateAll` 仍跑 GCM/水文 | 预设语义与名字不符，无法做纯地理世界 |

---

## 6. 推荐接入顺序（供实现卡拆分）

| 步骤 | 内容 | 关键文件 | 门禁 |
|---|---|---|---|
| D0（只读/设计） | 固化本报告；决定 SaveVersion 16/17、RuntimeDynamics 配置归属 | 本文档 | — |
| D1 生成期字段 | `TileData` + `TileSaveData` 增 `vegetation01/salinity01/erosion01/plateId`；PlanetTerrainGenerator 输出 plateId（`PlanetTerrainGenerator.cs:82` Voronoi）；Climate 后派生 vegetation/salinity；Hydrology 后初始化 erosion；生成期字段默认值 | Core.Data/TileData、World.Generation、GameWorld.Generation、TileSaveData | EditMode 地形/存档测试 |
| D2 存档 | SaveVersion 17 + `GameSaveSystem.SaveMapData/LoadMapData` 映射 + v16 迁移（`TileDynamicsMigration`） | GameConstants、TileSaveData、GameSaveData、GameSaveSystem | `SaveSystemRoundTripTests`/`LegacySaveCompatibilityTests` |
| D3 运行期调度 | `TerrainDynamicsSystem` + `TerrainDynamicsSchedule`（LongTerm order 9，`RegisterSimulationSchedules`）；确定性 RNG；增量脏块；`WorldSetupOptions` 运行期开关 | Simulation.WorldState、Scheduling、WorldSetupOptions | EditMode scheduler/确定性测试 |
| D4 UI | `TileInfoDto`/`TileInfoQueryAdapter`/面板补字段（湿度/土壤/植被/盐碱/侵蚀/板块/河流） | Core.Contracts/TileCharacterContracts、TileInfoQueryAdapter、UIManager.Panels | EditMode UiTileCharacterPanelsTests + PlayMode 面板 |
| D5 清理 | `isClimateDirty` 删除或改用；DisasterDef JSON 管线（Content + StreamingAssets）；MapGenerationConfig 外露 | 多文件 | 全量门禁 |

**最小接入点 3 个（不改现有行为即可先落）**：
1. `TileData`/`TileSaveData` 加 `plateId`（生成期只读分配，v16 也可缺省 -1）；
2. `Biomes` 空阶段（`GenerationPipeline.cs:250-252`）改为 vegetation/salinity 派生（只读现有 climate 字段）；
3. `RegisterSimulationSchedules`（`GameWorld.Simulation.cs:774`）加 `terrain.dynamics.longterm`（LongTerm、order 9、确定性种子、只标脏不跑全图）。

---

## 7. 只读边界

- 本报告未改任何 `.cs/.asset/.json/.unity/asmdef`；未跑 Unity/编译/测试；结论来自静态扫描与现有测试/存档结构。
- 行号基于当前 worktree（2026-11-02）；若并行任务改动文件，以 Lead 编译时版本为准。
