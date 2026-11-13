# 地形/气候 → 食物/人口/经济耦合审计 + 动态环境字段建议

- 审计员：economy-auditor（只读）
- 仓库：`D:\Myriad-Creation-A-Phantasm-of-Passing-Aeons`，分支 `architecture-2-0-refactor`（工作区有大量未提交改动，行号以本次阅读快照为准，后续改动可能漂移）
- 方式：纯静态代码阅读 + 行号取证；未运行 Unity、未编译、未跑测试、未修改任何代码/资产
- 唯一写入：本文件
- 证据格式：`MyriadCreation/Assets/.../File.cs:行号`

---

## 0. 结论摘要（TL;DR）

**一句话：地形/气候只"贴"在了经济的边缘（价格、税基、选址、视觉、灾害触发），没有进入"食物产出"——因为食物产出这个环节根本不存在。**

1. **全链路断点**：`TradeCenter.inventory` 是全图唯一食物池，仅在初始化时每区播 1000 粮（`GameWorld.Core.cs:801`），之后只被消耗（`EconomyManager.cs:395-443`）与腐败（`:457-473`）；**没有任何代码把地形/肥力转成食物入库**。`localSupply = fertility × development × 0.5`（`TradeCenter.cs:110`）只参与定价（`:30-43` 与 `EconomyManager.cs:174-184`），不生产。
2. **贸易也不通**：`TradeRoute`/`tradeRoutes` 在游戏代码中**从未创建**（只有测试创建），`InitiateTrades` 无路线可发车 → 区域库存无法互补。**任何地形、任何气候，最终都会在约 1~2 年内把 1000 粮吃光并进入饥荒。**
3. **肥力只降不升**：生成期一次性由 Holdridge 公式算出（`HoldridgeBiomeClassifier.cs:242-284`），运行时只有灾害/蝗群减、火山与农业建筑加；没有土壤形成、植被恢复、休耕、灌溉维护。蝗群 `fertility - 2f`（`SwarmAI.cs:62`）在 0..1 标度上一击清零。
4. **标度冲突**：肥力实际是 0..1（`HoldridgeBiomeClassifier.cs:283`、`GameWorld.Generation.cs:273-274`），但 `NomadAI.cs:37,48`、`SwarmAI.cs:49`、`MapActorManager.cs:227` 按 0..100 比较（>30/>40），导致游牧不迁徙/不回血，蝗群生成分支不可达。
5. **气候变化的产出效应为 0**：改 `WorldConfig.greenhouseFactor/heatTransport` → `MarkConfigDirty` → `RecalculateAll` 只更新 temp/precip/humidity/climateZone/biome（`PlanetClimateSimulator.cs:159-188`），**不重算 fertility、movementCost、资源、供给、税基**；季节每 91 天用季节值覆写 `annualTemp/annualPrecipMm`（`GameWorld.Simulation.cs:338-355` + `PlanetClimateSimulator.cs:170-181`），字段语义与消费端全部错位。
6. **CarryingCapacity 与产出脱节**：容量公式完全不含肥力/气候/开发，只看 biome 固定倍率 + 区域食物库存（`CarryingCapacitySystem.cs:92-109`）；库存越低容量越低，是"饥饿→容量下降"的倒因为果，而不是"土地→容量"。
7. **性能 P0**：默认地图 1024×512 = 524,288 格、`region = i/16` 切片 = 32,768 个区域库；`UpdateStorageCapacities` 是 O(区域×全表) ≈ **1.7×10¹⁰ 次比较**，首次日结必然卡死（`EconomyManager.cs:87-107` + `GameWorld.Core.cs:511,786`）。
8. **存档可扩展性**：`TileSaveData`（`TileSaveData.cs:9-35`）已含 annualTemp/precip/humidity/biome/climateZone/fertility/development，但缺 soilHumidityPct/accumulatedTemp/frostFreeDays 等，也还没有动态环境字段；JsonUtility 缺字段默认 0，扩字段必须配版本迁移与回填。

---

## 1. 字段底账（TileData 当前有什么、量纲是什么）

| 字段 | 行 | 量纲/范围（实际代码） | 谁写 | 谁读（经济相关） |
|---|---|---|---|---|
| `annualTemp` | `Core.Data/TileData.cs:72` | 名义 °C、实际被季节值覆写（`PlanetClimateSimulator.cs:170-171`） | 生成期 `PlanetTerrainGenerator.cs:160`；气候重算 `PlanetClimateSimulator.cs:171`；热/寒潮 `DisasterSystem.cs:320,323` | 灾害触发 `DisasterSystem.cs:227-228,232`、种族适应 `RaceData.cs:177`、建筑可用 `BuildingAvailabilitySystem.cs:354-358`、聚落评分 `SettlementAISystem.cs:89`、渲染 `MapRenderer.Colors.cs:148-150` |
| `annualPrecipMm` | `TileData.cs:76` | 名义 mm/年、实际被季节系数覆写（1.3/0.7 等，`PlanetClimateSimulator.cs:179-181`） | 生成/重算（`PlanetTerrainGenerator.cs:162`、`:181`）、洋流 `OceanCurrentSimulator.cs:198,204` | 灾害触发 `DisasterSystem.cs:225-226,232`、贸易供/需（价格）`TradeCenter.cs:110`、建筑可用 `BuildingAvailabilitySystem.cs:346-350`、聚落评分 `SettlementAISystem.cs:66-87`、经济构成 `EconomicCompositionSystem.cs:37`、聚落类型 `SettlementTypologySystem.cs:112,167` |
| `airHumidityPct` | `TileData.cs:78` | 0..100 | `PlanetClimateSimulator.cs:182`、`AtmosphericCirculation` 路径 `GameWorld.Simulation.cs:480` | 疾病 `DiseaseSystem.cs:351`、种族适应 `RaceData.cs:178`、单位 `TerrainConditions.cs:105` |
| `soilHumidityPct` | `TileData.cs:80` | 0..100 | `PlanetClimateSimulator.cs:183`、旱/涝 `DisasterSystem.cs:312,316`、毁城 `SettlementDestructionSystem.cs:194` | **经济/农业代码零消费**（仅 `GameWorld.Generation.cs:502` 死代码与 `TerrainConditions.cs:106`） |
| `accumulatedTemp` | `TileData.cs:82` | °C·日 | `PlanetClimateSimulator.cs:184` | 无消费（待验证） |
| `frostFreeDays` | `TileData.cs:84` | 0..365 | `PlanetClimateSimulator.cs:185` | 无消费（待验证） |
| `climateZone` | `TileData.cs:86` | 枚举 | `PlanetClimateSimulator.cs:186`、`PlanetTerrainGenerator.cs:229` | `PlanetClimateSimulator.DetermineBiome`（`:336`） |
| `biome` | `TileData.cs:88` | 枚举 | 生成 `PlanetTerrainGenerator.cs:228`；`GameWorld.CalculateClimate`（Holdridge）`GameWorld.Simulation.cs:486`；WMS 重算（PlanetClimateSimulator）`PlanetClimateSimulator.cs:187`；破碎化修复 `GameWorld.Generation.cs:272` | 承载力倍率 `CarryingCapacitySystem.cs:16-90`；资源生成（死代码）`ResourceGenerator.cs:54-61`；移动成本 `GameWorld.Simulation.cs:487`；游牧/掠夺 AI；渲染 |
| `fertility` | `TileData.cs:90` | **0..1**（`HoldridgeBiomeClassifier.cs:283`） | 生成 `PlanetTerrainGenerator.cs:230`；破碎化修复 `GameWorld.Generation.cs:273-274`；灾害 `DisasterSystem.cs:295,313,326,329`；蝗群 `SwarmAI.cs:62`；农业建筑 `BuildingSystem.cs:271` | 税基 `EconomyManager.cs:483`；贸易虚拟供给/价格 `TradeCenter.cs:110`；资源品质（死代码）`ResourceGenerator.cs:142`；建筑/聚落 AI/渲染 |
| `development` | `TileData.cs:92` | **0..1**（`BuildingSystem.cs:280` clamp 0..1；`GameWorld.Generation.cs:315-316` 初始 0.1） | 生成 0.1；建筑市场 `BuildingSystem.cs:280`；经济改善 `ImproveRealmEconomyCommandHandler.cs:33`；灾害/军队/兽群扣减 | 税基 `EconomyManager.cs:483`；虚拟供给 `TradeCenter.cs:110`；承载力**未使用** |
| `stability` / `order` | `TileData.cs:94,96` | 0..100 | 大量系统 | 贸易路线效率 `TradeRoute.cs:39`、税率 `TaxSystem`、饥荒/叛乱 |
| `resources` | `TileData.cs:102` | `List<TileResource>` | **`ResourceGenerator.GenerateAll` 是死代码（无调用者）** | `InnovationTree.cs:640` 读取（恒空） |
| `isClimateDirty/isTerrainDirty/isEconomyDirty` | `TileData.cs:106-110` | bool | 未见写入（待验证） | 脏块实际由 `WorldMapSimulationSystem` 的 HashSet 管理（`WorldMapSimulationSystem.cs:14-18`） |

---

## 2. 耦合链路（逐段公式 + 断点标注）

### 2.0 总览

```
[地形生成] PlanetTerrainGenerator
  ├─ annualTemp = f(纬度, 海拔, 海陆, 海岸, GlobalTempOffset)      PlanetTerrainGenerator.cs:160
  ├─ annualPrecipMm = f(|lat|, 坡度, 海陆, 海岸)                   PlanetTerrainGenerator.cs:162
  ├─ biome = Holdridge.Classify(T,P,elev,slope,lat)                PlanetTerrainGenerator.cs:228
  └─ fertility = Holdridge.CalculateFertility(T,P,elev,slope,biome) PlanetTerrainGenerator.cs:230  ← 只此一次
        ↓
[气候重算① GCM] GameWorld.CalculateClimate（AtmosphericCirculation + Holdridge）
  ├─ precip/airHumidity 覆写                                       GameWorld.Simulation.cs:477-482
  ├─ biome 再算（Holdridge） + movementCost                        GameWorld.Simulation.cs:483-488
  └─ fertility **不重算**                                          ← 断点 A
        ↓
[水文重算 + WMS 全量重算] RecalculateHydrology → WMS.RecalculateAll
  ├─ PlanetClimateSimulator：temp/precip/humidity/soilHumidity/
  │  accumulatedTemp/frostFreeDays/climateZone/biome 覆写           PlanetClimateSimulator.cs:159-188
  ├─ fertility **不重算**；movementCost **不重算**；resources 不重建  ← 断点 B（三口径漂移）
  └─ 季节边界每 91 天 UpdateForSeason（全表、带季节修正覆写 annual*）  GameWorld.Simulation.cs:338-355
        ↓
[承载力] CarryingCapacitySystem = 100 × biomeMult × (1.15 if 贸易中心) × clamp(区域food/(tilePop×0.3),0.5,2)
                                                                   CarryingCapacitySystem.cs:92-109
        ↓（无 fertility / 无 T / 无 P / 无 development）              ← 断点 C
[人口增长] r = 0.002×repro×(0.5+sat/100)×(0.3+fitness×0.7)          RaceData.cs:184-190
  fitness = f(annualTemp, airHumidity, elevation, biome)             RaceData.cs:175-182
  pop *= 1 + r×days；overload>1 时 r 再打折                          GameWorld.Simulation.cs:62-70
        ↓
[食物消耗] 每地块每天 pop×0.01 粮 + pop×0.002 盐，从**区域库**扣     EconomyManager.cs:395-443
  缺粮 → -0.2 稳定度/-0.1 秩序/日                                    EconomyManager.cs:451-455
  缺粮 → Famine 事件：pop×0.95、满意-20、稳定-10                       GameWorld.Core.cs:394-408
        ↓
[食物供给] 仅初始化每区 1000 粮                                     GameWorld.Core.cs:801
  区域库之间只能靠 TradeRoute+Caravan 调运
  → **游戏代码从不创建 tradeRoutes，只测试创建**                     ← 断点 D（无贸易）
  → localSupply = fertility×development×0.5 只用于价格，不入库        TradeCenter.cs:102-123 + 30-43
        ↓
[市场/税收] 价格 = baseValue × clamp(需求/供给,0.2,5) × clamp(1.5-库存/容量,0.3,1.5)
                                                                   TradeCenter.cs:30-43
  税基 = fertility × development × 10（每政权每天全表扫）             EconomyManager.cs:476-489
```

### 2.1 生成期：气候 → biome → fertility 公式

- 温度（`PlanetClimateSimulator.cs:164-171`）：
  `T = Clamp( equatorBase×(1-(|lat|/90)^1.5×(1.6-heatTransport×0.3)) − elev01×4×lapseRate + waterAdjacent×3 + (thermalEquatorLat 修正)×0.15 + seasonMod×( |lat|/90 ), −55, 35)`
- 降水（`:173-181`）：
  `P = Clamp( (ITCZ + 西风 + 季风 + 地形雨) × (1 − waterAdjacent×0.5) × seasonPrecipMod − 副热带干旱, 0, 4000 )`
- 湿度/积温/无霜期：`soilHumidity = airHumidity×0.7 + P/1000×30`（`:183`）、`accumulatedTemp=(T−10)×365`（`:184`）、`frostFreeDays=(T+5)/30×365`（`:185`）。
- biome 两套分类器：
  1) `HoldridgeBiomeClassifier.Classify`（`HoldridgeBiomeClassifier.cs:95-240`）：生物温度 `ABT=Clamp(T,0,30)`（T<10 再乘冷修正，`:9-17`）、`PET=ABT×50`（`:19-22`）、`PER=PET/P`（`:24-31`）、湿度省/热带的查表 + 海拔/坡度/海岸/河流特例。
  2) `PlanetClimateSimulator.DetermineBiome`（`PlanetClimateSimulator.cs:336-361`）：只用 climateZone + temp + precip + elevation + soilHumidity 的粗规则（如 `TemperateMild: P>600 → DeciduousForest 否则 TemperateGrassland`）。
  → 谁最后跑谁生效；WMS 重算（气候阶段之后）用 (2)，`GameWorld.CalculateClimate` 用 (1)，同一地块的 biome 在流水线里会被改两次（见 P1-4）。
- 肥力（`HoldridgeBiomeClassifier.cs:242-284`）：
  `fT = exp(−((ABT−15)/12)²)`、`fP = exp(−((PER−0.75)/1.5)²)`、`base = 0.8×fT×fP + 0.1`；
  biome 覆盖：冲积/大河平原/三角洲/火山灰平原 `max(base,0.85)`；河间/封闭盆地/绿洲 `max(base,0.7)`；森林类 `×0.85`；草原/稀树草原 `×0.9`；高山/冰盖/冰川/苔原/沙漠类 `×0.15`；
  再乘 `slopePenalty = Clamp01(1 − max(0, slope−10)/50)` 与 `elevPenalty = Clamp01(1 − max(0, elev01−0.5)×1.2)`，`Clamp01` 到 **0..1**。
- 另有死代码 `GameWorld.CalculateBaseFertility`（`GameWorld.Generation.cs:497-504`，`precip/1000×0.5 + (T+10)/35×0.3 + (1−elev)×0.2 + soilHumidity/100×0.2`），无调用者——第三套肥力公式只存在于源码里。

### 2.2 CarryingCapacitySystem（公式与输入）

`CarryingCapacitySystem.cs:92-109`：

```
CC = 100 × BiomeMultiplier(tile)                       // BaseCapacity=100  :12；倍率表 :16-90
   × (贸易中心存在 ? 1.15 : 1)                          // :95-97
   × clamp( Σ_{id∈{0,1,2,10}} 区域库 inventory[id]      // :98-103
            / (GetTotalPopulation(tile) × 0.01 × 30),   // 30 天安全线 :104
            0.5, 2)                                     // :106
```

- **输入**：`biome / isCoast / isRiver`（倍率）、`regionId` 对应区域库库存、tile 人口。
- **不输入**：`fertility`、`annualTemp`、`annualPrecipMm`、`soilHumidity`、`development`、`vegetation`。
- 消费点：人口增长（`GameWorld.Simulation.cs:62-69`）：`overload = tilePop/CC`，`overload>1` 时 `r ×= 1 − clamp01((overload−1.1)/0.9)`。
- 粒度矛盾：容量是**每地块**，食物库存却是**每 16 格区域**；同区域 16 个地块共享同一库存因子，但各算各的容量。

### 2.3 ResourceGenerator（地形→资源，实际未接线）

- `GenerateAll`（`ResourceGenerator.cs:11-49`）逐地块、逐 `isNaturalResource` 物产：`originBiomes.Contains(tile.biome)`（`:54-61`）→ `CheckSpawnCondition`（海拔/年降水/年均温/坡度/海岸/河流，`:96-135`）→ `quality`（野生动植物 `0.5+fertility×0.5`，`:142`；矿物按坡度 `:146`；水生按海岸/河流 `:150`）→ `abundance = baseAbundance×quality×rand(0.7,1.3)`（`:72`）。
- **无任何游戏代码调用 `ResourceGenerator.GenerateAll`**（全仓库仅 `ResourceDepthTests` 调用 `RollDepth`）→ `tile.resources` 全空，`InnovationTree.cs:640` 的 `EffectiveOutput` 加成恒为 0，矿/林/渔资源经济不存在。
- 即使被调用：`TileResource.depletion`（`TileResource.cs:18-24`）**没有任何写入点** → 资源永不枯竭，`EffectiveOutput=abundance×max(dev,0.1)×(1−depletion)` 恒为静态值。

### 2.4 "食物产出"——不存在（核心结论）

- 唯一入库入口：初始化 `inventory[food]=1000`（`GameWorld.Core.cs:801-802`，每个区域一次）。
- 之后唯一入库路径：`DeliverCargo`（`EconomyManager.cs:298-322`，`AddGoodsReturningAccepted` 唯一非测试调用点 `:309`），只服务于商队到货。
- 商队只能由 `InitiateTrades` 发出（`EconomyManager.cs:152-213`），而它要求 `tradeCenter.tradeRoutes` 非空（`:159-171`），**游戏代码没有任何 `new TradeRoute` / `tradeRoutes.Add`**（仅 `Assets/Tests/EditMode/EconomyConservationTests.cs:141,175,176,195,219`）→ 全图不产生商队。
- 唯一出库路径：人口口粮/盐（`EconomyManager.cs:419-437`）、腐败 `×0.999/日`（`:465`）、组队/建城（`TreasuryLedger.cs`）。
- `localSupply` 只在 `InitiateTrades` 选品与 `GetGoodsPrice` 中使用（`EconomyManager.cs:174-184`；`TradeCenter.cs:30-43`），**从不转成库存**。
- 数量级：初始人口 = 每陆地核心格 `10×initialPopulationScale`（`GameWorld.Generation.cs:100-114`），默认 scale=1（`WorldSetupOptions.cs:21`）→ 每 16 格区域约 160 人 → 1.6 粮/日 → 1000 粮约 625 天耗尽（叠加 0.1%/日腐败更快），人口增长还会加速。**约 1.7 年后全图饥荒，与地形肥力完全无关。**
- `SettlementData.population`（500~2000，`AnchorGenerator.cs:63,79,109`）是**独立数字**：不在 `TileData.populationBlocks` 里、不吃饭、不增长，只被毁城/踩踏扣减（`SettlementDestructionSystem.cs:43`、`StampedeSystem.cs:157`）。

### 2.5 人口增长公式

```
r_daily = 0.002 × reproductionRate × (0.5 + satisfaction/100) × (0.3 + fitness×0.7)   RaceData.cs:187-190
fitness = Clamp01( tempTerm×0.35 + humidityTerm×0.35 + altitudeTerm×0.3 , 0.1 , 1 )
          × (preferredBiomes 含本 biome ? 1.2 : 1)                                    RaceData.cs:175-182
tempTerm     = annualTemp < 10 ? Lerp(0.3,1,coldTolerance) : Lerp(0.3,1,heatTolerance)  :177
humidityTerm = airHumidity < 40 ? Lerp(0.3,1,aridityTolerance) : Lerp(0.3,1,humidityTolerance) :178
altitudeTerm = elev>0.4 ? Lerp(0.3,1,altitudeTolerance) : 1                             :179
pop *= 1 + r×days；overload>1 时 r ×= 1 − Clamp01((overload−1.1)/0.9)                    GameWorld.Simulation.cs:62-70
```

- 气候进入的是**舒适度**（temp/humidity/biome），**肥力与食物不进增长公式**；食物只通过 Famine 事件按 `×0.95` 直接砍人口（`GameWorld.Core.cs:394-408`）。
- `satisfaction` 由稳定度/税负/文化/种族基线算（`GameWorld.Simulation.cs:108-126`），不含"粮食安全"。
- 阶级需求层的 `foodSecurity` 来自区域库存/（人口×0.01×30）（`RealmSituationBuilder.cs:97`），进入 `ClassNeedsSystem.cs:91,144`，但**不回写** tile 满意度/增长率——又是一条断链。

### 2.6 市场 / 贸易 / 税收

- 价格（`TradeCenter.cs:30-43`）：
  `ratio = supply>0 ? demand/max(0.01,supply) : 2`，`Clamp(ratio,0.2,5)`；
  `stockFactor = Clamp(1.5 − inventory/capacity, 0.3, 1.5)`；
  `price = baseValue × ratio × stockFactor`。
  其中 `supply = Σ_tiles(fertility×development×0.5)`（粮食；谷物 ×0.8），`demand = regionPop×0.01`（盐 ×0.002）（`TradeCenter.cs:102-123`）。
- 贸易路线效率（`TradeRoute.cs:21-45`）：`eff = base × (0.6+0.15×平均roadLevel) × (0.5+平均stability/200) × exp(−节点数×0.02)`，`Clamp(0,1.5)`；**不含地形坡度、河流、气候、灾害**。
- 商队移动（`Caravan.cs:42-60`）：`moveProgress += speed×routeEfficiency`，`tiles` 参数完全未使用——地形不影响贸易速度。
- 税基（`EconomyManager.cs:476-489`）：`baseOutput = fertility × development × 10`，再乘 `TaxSystem.CalculateTileTax`（稳定度、税种、阶级豁免）与法律乘子；**气候不进税基**。
- 区域库容量（`EconomyManager.cs:87-107`）：`capacity = 10000 × (1 + 0.1×max 建筑等级)`，每 30 天更新；但更新实现是 O(区域×全表)。
- 区域/市场地理：`regionId = i/16`（`GameWorld.Core.cs:511`）→ 每个"区域"是同行的 16 连格，与省份/流域/地形集群无关；贸易中心建在 `i×16`（`GameWorld.Core.cs:786-805`）。

### 2.7 聚落 / 建筑 / AI 选址

- 聚落选址：AnchorGenerator 按省中心/沿海/边境/随机点建，`settlement.population` 与 `development` 随机（`AnchorGenerator.cs:58-113`），**不读取 `tile.fertility` 做选址门槛**；只是类型/经济构成读取地形（`EconomicCompositionSystem.cs:37-44`：`precip<300 → +5` 某产业、`fertility>0.6 → +8` 农业；`SettlementTypologySystem.cs:112,167` 读取降水/河流）。
- AI 建村评分：`SettlementAISystem.CalculateFertilityScore` **不使用 `tile.fertility`**，而是重新用 `annualPrecipMm` 分档（`:66-87`）、`|annualTemp−18|`（`:89`）、`elevation` 分档（`:90-107`）、河流/海岸加分（`:108-110`）——名字叫 FertilityScore，与 `fertility` 字段无关。
- 建筑可用性：`BuildingAvailabilitySystem.cs:326-358` 读取 `annualPrecipMm/annualTemp` 上下限与"需荒漠"条件。
- 沙盒放置：`SandboxSpawnSystem.cs:133` 用 `fertility < 0.15` 拒绝（0..1 标度，正确）。
- 渲染：`MapRenderer.Colors.cs:141-150,170` 用降水/肥力/温度着色（视觉层，正确 0..1）。

### 2.8 灾害与环境事件

- 触发：按气候阈值从全图挑中心（`DisasterSystem.cs:218-244`）：Drought `P<800`、Flood `P>1000 && elev<0.3`、ColdWave `T<10`、HeatWave `T>20`、Locust `T>15 && P<600` 等；影响面按曼哈顿半径扫全图（`:246-280`）。
- 效果（每活跃灾害每天）：`Drought: soilHumidity−0.5, fertility−0.001`（`:312-313`）；`Flood: soilHumidity+2, stability−0.1`（`:316-317`）；`ColdWave: annualTemp−0.5`（`:320`）；`HeatWave: annualTemp+0.3`（`:323`）；`LocustPlague: fertility−0.005`（`:326`）；`CropFailure: fertility−0.002`（`:329`）；`Famine: stability−0.2, order−0.1`（`:331-334`）。
- **无库存/产出/价格/贸易/人口效果**；`Famine` 灾害 `baseFrequency=0`（`:144-153`）永不自动触发。寒潮/热浪永久改 `annualTemp` 且不重算 biome/fertility（无分类器调用）。

### 2.9 季节 / 气候变化

- `AdvanceTime` 每 91 天切季，调用 `_climateSimulator.UpdateForSeason(currentSeason)`（`GameWorld.Simulation.cs:338-355`），全表重算（`PlanetClimateSimulator.cs:93-103`）。
- `CalculateTileClimate(..., season)` 用 `seasonTempMod` 修正 `annualTemp`（`:170-171`）、用 `seasonPrecipMod`（0.7~1.3）覆盖 `annualPrecipMm`（`:179-181`）——字段名为 annual，实际是季节值，消费者（灾害/建筑/种族/聚落评分）会随季节"漂移"。
- 气候变化唯一入口：`GameWorld.UpdateConfig`（`GameWorld.Editing.cs:61-65`）修改 `WorldConfig`（`greenhouseFactor`/`heatTransport`/`seasonIntensity`，`WorldConfig.cs:64-77`）→ `WorldMapSimulationSystem.MarkConfigDirty`（`WorldMapSimulationSystem.cs:93-96`）→ `RecalculateAll` → `PlanetClimateSimulator.RecalculateAll`（`:65-77`）。**只更新 T/P/湿度/土壤湿度/climateZone/biome，不更新 fertility/movementCost/资源/产出**（`PlanetClimateSimulator.cs:159-188` 无 fertility 赋值）。

---

## 3. 不合理设计 / 风险清单

### P0

#### P0-1 食物产出环节不存在：地形/气候对"吃饱"零影响
- 证据：唯一食物来源是初始化 1000/区（`GameWorld.Core.cs:801`）；入库唯一路径是商队到货（`EconomyManager.cs:298-322`），而发车要求 `tradeRoutes`（`:159-171`），游戏代码从不创建路线（仅测试：`Tests/EditMode/EconomyConservationTests.cs:141,175`）；`localSupply` 只影响价格（`TradeCenter.cs:110` + `EconomyManager.cs:174-184`）。
- 触发：任意新局。
- 后果：所有地区库存单调下降 → 约 600~700 天（含腐败更快）后全图饥荒，`HandleFamine` 每日 `×0.95` 人口（`GameWorld.Core.cs:401`）→ 文明人口坍塌。地形再肥沃、气候再适宜都无法避免；`CarryingCapacity` 只是跟着库存下跌把容量打到下限，进一步压制增长，形成不可逆死亡螺旋。
- 修法要点：新增 `FoodProductionSystem`，按 `Σ over region tiles: arable(fertility, landUse, water) × climateYield(T,P,season,tech) × developmentMultiplier` 每日/每旬把粮食入区域库（同时把 `localSupply` 变成产出的派生视图），并给贸易创建路线（见 §5）。

#### P0-2 默认地图下 `UpdateStorageCapacities` 为 O(区域×全表)，首次日结即卡死
- 证据：默认地图 `GenerationMapSize.Medium = 1024×512`（`MapGenerationConfig.cs:346-355,456`）→ N=524,288；`regionId=i/16`（`GameWorld.Core.cs:511`）→ 区域数 `R=(N+15)/16=32,768`（`GameWorld.Core.cs:786`）；`UpdateStorageCapacities` 对每个区域再遍历全部 N 格（`EconomyManager.cs:93-106`）→ ≈ **1.72×10¹⁰ 次 `regionId` 比较/30 天**，且 `_storageUpdateDay=-1` 使首次 `DailyTick` 立即执行（`:109-119`）。
- 后果：开局第一天主线程长时间卡顿（按 1e8 次简单比较/秒估算 ~170 秒，实际含缓存未命中更久）；同时 32,768 个 `TradeCenter` 对象+字典+名称常驻内存。
- 修法要点：建区域→地块索引表（`Dictionary<int,List<int>>` 或 `int[] regionStart/Count`）一次构建，容量更新 O(N)；或按区域聚合建筑等级（生成期/建造时增量维护）。

### P1

#### P1-1 肥力只降不升（无土壤形成/植被恢复），且蝗群一键清零
- 证据：fertility 写入点全集：生成（`PlanetTerrainGenerator.cs:230`、`GameWorld.Generation.cs:273-274`）、火山 +0.1（`DisasterSystem.cs:295`）、旱 −0.001/日（`:313`）、蝗灾 −0.005/日（`:326`）、歉收 −0.002/日（`:329`）、蝗群 AI −2/次（`SwarmAI.cs:62`）、农业建筑 +0.05×tier（一次性，`BuildingSystem.cs:271`）。**没有任何自然恢复/休耕/施肥/侵蚀再沉积逻辑**。
- 后果：fertility 是单调递减资源；长期游戏所有耕地归零（尤其蝗群命中一次即 0）；与"文明千年"设定冲突。
- 修法要点：把 `fertility` 改为派生量，新增 `soilOrganic`、`vegetationCover`、`soilSalinity`、`erosionRate` 状态与恢复率（§4），农业建筑改为维护性加成。

#### P1-2 肥力 0..1 与 0..100 标度混用，导致游牧/蝗群功能失效
- 证据（0..1 事实）：`HoldridgeBiomeClassifier.cs:283` Clamp01；`BuildingSystem.cs:271` Clamp 0..1；`SandboxSpawnSystem.cs:133` `<0.15`。
- 证据（按 0..100 比较）：
  - `NomadAI.cs:37` `fertility > 30f` 才选目标 → 恒 false，游牧不向肥地迁徙；
  - `NomadAI.cs:48` `fertility > 40f` 才回补给 → 恒 false，游牧永不回血；
  - `SwarmAI.cs:49` `fertility > 30f` 才选目标 → 恒 false；
  - `MapActorManager.cs:227` `else if (!(fertility < 40f))` 才生成蝗群/鼠患 → 恒 false，该分支不可达（配合 SwarmAI.Act 的 −2，等于蝗群系统整体失效且即使生效也会秒清零）。
- 修法要点：统一 0..1；对这些点改 `>0.3f`/`>0.4f`；加单测断言 `0<=fertility<=1`。

#### P1-3 没有植被/土壤/盐碱/侵蚀/水位维度；`soilHumidityPct` 是"只写不读"字段；资源枯竭不存在
- 证据：`TileData` 字段全集（`TileData.cs:10-110`）无 vegetation/salinity/organic/erosion/waterTable/landUse；`soilHumidityPct` 写入 `PlanetClimateSimulator.cs:183`、`DisasterSystem.cs:312,316`、`SettlementDestructionSystem.cs:194`，经济侧零消费；`TileResource.depletion` 只有初始化 0（`TileResource.cs:39`），无写入；`ResourceGenerator` 死代码。
- 后果：伐木/灌溉/过牧/城市化/矿竭全部无法表达；`soilHumidityPct` 的旱涝效果不进入任何产出/承载/增长；资源经济与创新树资源加成空转。
- 修法要点：见 §4 字段与 §5 消费系统；同时把 `soilHumidityPct` 接入产出/承载力。

#### P1-4 气候变化不影响产量；季节重算只改"annual"字段且不改肥力/移动成本
- 证据：`PlanetClimateSimulator` 全文件无 `fertility` 赋值；`CalculateTileClimate` 写 temp/precip/humidity/soilHumidity/accumulatedTemp/frostFreeDays/climateZone/biome（`:159-188`），不写 fertility/movementCost；`GameWorld.CalculateClimate` 写 precip/humidity/biome/movementCost（`GameWorld.Simulation.cs:477-488`），不写 fertility；季节 `UpdateForSeason` 同（`:93-103`）。
- 后果：调 `greenhouseFactor`/`heatTransport` 做"气候变化"时，T/P/biome 变了，但税基、承载、食物产出（本就不存在）、资源品质全不变——气候变化的玩法意义为 0；同时 `movementCost`/`fertility` 停留在旧口径，与 biome 不一致。
- 修法要点：气候重算后统一派生 `fertilityBase = CalculateFertility(new T,P,...)`；`movementCost = BiomeAttributes.GetMovementCost(new biome)`；两者放进同一个 `RecalculateDerived(tile)`，供生成/破碎化/WMS/灾害共用。

#### P1-5 三套 biome/肥力口径漂移（Holdridge vs DetermineBiome vs 死代码公式）
- 证据：Holdridge 分类与肥力（`HoldridgeBiomeClassifier.cs:95-284`）；PlanetClimateSimulator 粗分类（`PlanetClimateSimulator.cs:336-361`）；`GameWorld.CalculateBaseFertility` 死代码（`GameWorld.Generation.cs:497-504`）。
- 后果：同一地块最终 biome 取决于最后一次重算（WMS 用粗分类覆盖 Holdridge），而 `movementCost` 由 Holdridge 那次写入（`GameWorld.Simulation.cs:487`）→ 移动成本与当前 biome 不匹配；`fertility` 又是更早的 T/P 快照 → 三者互指不同世界。灾害改 `annualTemp` 后更明显（永久漂移）。
- 修法要点：单一入口 `TileDerivedClimate.Recalculate(tile, worldConfig)`（T/P/humidity/soil → climateZone → biome → movementCost → fertilityBase）并全量替换三处调用；删除死代码。

#### P1-6 CarryingCapacity 与真实产出脱节
- 证据：公式不含 fertility/T/P/development（`CarryingCapacitySystem.cs:92-109`）；食物因子来自"区域库存/（tile人口×0.3）"（`:104-106`），库存是消耗型；下限 0.5（`:106`）保证永不归零；容量按 tile、库存按区域（粒度不一致）。
- 后果：容量是"库存的镜像"而非"土地潜力"：肥沃与贫瘠同 biome 倍率下容量相同；饥饿时容量下降进一步压制增长（倒因为果）；即使库存为 0，容量仍 ≥50×biomeMult，等于永远"半容量"运行。
- 修法要点：`CC = 100 × biomeMult × fertilityFactor × waterFactor × techFactor`（土地潜力），食物因素改为独立的"实际供养率"影响增长率；区域库存只用于饥荒判定。

#### P1-7 人类开发对自然单向、无代价
- 证据：`development` 提升只影响税基（`EconomyManager.cs:483`）与虚拟供给（`TradeCenter.cs:110`）；提升途径：农业建筑每次 +0.02×tier 一次性（`BuildingSystem.cs:280`）、经济改善命令 +0.01/次（`ImproveRealmEconomyCommandHandler.cs:33`）；对 fertility/soilHumidity/植被无任何负反馈；无伐木/灌溉/过牧/城市化逻辑（全局检索 deforest/logging/timber 无命中）。
- 后果：最优策略永远是"堆开发"；土地退化、盐碱化、森林消失等文明史核心机制缺失。
- 修法要点：新增 `landUse`+退化字段；建筑/人口/牲畜按类型扣 vegetation/soilOrganic/waterTable，并在产出公式中体现边际递减。

#### P1-8 灾害影响面偏窄、口径分裂
- 证据：灾害效果仅 stability/development/fertility/annualTemp/soilHumidity（`DisasterSystem.cs:282-337`），无库存/产出/价格/贸易路线/人口效果；`DisasterType.Famine` `baseFrequency=0`（`:144-153`）；蝗灾灾害 −0.005 肥力（`:326`）与蝗群 AI −2（`SwarmAI.cs:62`）差 400 倍；寒潮/热浪永久修改 `annualTemp`（`:320,323`）却不重算 biome/fertility。
- 后果：灾害对经济的传导只有"稳定度→税/贸易效率"的间接链；农业歉收没有粮食损失；气候类灾害会累积出与 biome 不一致的世界。
- 修法要点：灾害按类型产出"损失包"（粮食库存、当季产量、牲畜、树冠、表土），并调用 §5 的产量系统；温度偏移后调用统一 `RecalculateDerived`。

#### P1-9 人口/聚落/市场的地理与统计口径失配
- 证据：初始人口=每个政权陆地核心格 `10×scale`，与地形无关（`GameWorld.Generation.cs:100-114`）；政权领土=按陆地索引顺序连续切片（`:93-99`），不是地理连贯区域；`SettlementData.population`（`AnchorGenerator.cs:63`）独立于 `TileData.populationBlocks`、不吃饭、不增长；聚落选址不看 fertility（`AnchorGenerator.cs:58-113`）；"区域"是固定 16 连格（`GameWorld.Core.cs:511`）而非省/流域。
- 后果：地图上最宜居的河谷与冰原初始人口一样；聚落人口与粮食经济无关；"地区"不具地理意义，贸易/承载/需求全部建立在任意切片上。
- 修法要点：人口按 `terrain suitability × CC` 分布；区域改为省/流域/聚落腹地；`SettlementData.population` 与地块人口块合并或建立换算。

#### P1-10 其余性能项（除 P0-2）
默认 N=524,288、R=32,768 下：
- `PopulationTick` 每日 `Parallel.For(N)`，每格做字典查找+承载力计算（`GameWorld.Simulation.cs:53-100`）；
- `ProcessConsumption` 每日 O(N)（`EconomyManager.cs:399-438`）；
- `SettleTaxes` 每政权每日 O(N)（`EconomyManager.cs:479-487` + `PoliticsSimulationSystem.cs:83-88`），12 政权≈6.3M/日；
- 灾害每次触发扫全图两次（`DisasterSystem.cs:221-244`、`:267-279`），多灾害并发时可达数千万次/日；
- `UpdateForSeason` 4 次/年全表重算 + 风向场 O(N)（`PlanetClimateSimulator.cs:93-103,105-157`）；
- `ProcessShelfLife` 每日遍历 R 个库的所有库存键（`EconomyManager.cs:457-473`）；
- 地域供应/需求每日 R×16 遍历（`EconomyManager.cs:129-138`），本身 O(N) 但每格含人口求和。
→ 建议：区域聚合 + 每旬/每月一次的环境 tick（用 `IJobParallelFor` 或分批预算），把每日全表扫描降到区域级或脏区级。

### P2

1. **`localSupply` 单位/口径混乱**：按格累加 `fertility×development×0.5`（最多 0.5/格），而 `localDemand` 用 `regionPopulation×0.01`（可能数百）→ 供需比长期偏向 5 倍价格上限，价格信号失真（`TradeCenter.cs:110-115,39`）。
2. **`GetGoodsPrice` 无实际交易闭环**：只有 `InitiateTrades` 使用，而它无路线可跑；UI 若展示价格，展示的是"不会成交的理论价"。
3. **`TradeCenter.RemoveGoods` 允许扣到 0 但不删键**（`TradeCenter.cs:81-90`），零值库存长期残留，`GetTotalInventory`/shelf 循环无谓开销。
4. **区域容量更新周期 30 天**（`EconomyManager.cs:111-119`），但建筑可即时升降级 → 容量滞后；且容量只按 `buildingLevels[0]`（`EconomyManager.cs:100`）判定。
5. **`SettlementAISystem.CalculateFertilityScore` 名不副实**（不用 `fertility`，`SettlementAISystem.cs:64-111`），AI 与地图渲染/经济的"肥力"不是同一口径。
6. **`TileSaveData` 字段缺口**：缺 `soilHumidityPct/accumulatedTemp/frostFreeDays/waterAdjacentWeight/diurnalTempRange`（`TileSaveData.cs:9-35` 对比 `TileData.cs:72-90`），读档后这些字段归 0；一旦未来接入经济/承载，会导致读档后世界行为突变（需迁移回填）。
7. **两套地图存档 DTO**：`GameSaveData.tiles`（`GameSaveData.cs:38`）与 `MapSaveData.tiles`（`MapSaveData.cs:18`）都直接序列化 `TileSaveData`；扩字段要同步改 `GameSaveSystem.cs:114-141/386-403` 与地图存档路径。
8. **`isClimateDirty/isTerrainDirty/isEconomyDirty` 字段**（`TileData.cs:106-110`）与 WMS 的 HashSet 脏块并存，语义重复，未见使用（待验证）。
9. **`CalculateClimate`（GCM）与 `PlanetClimateSimulator` 两套气候模型并存**：生成流水线两者都跑（`GenerationPipeline.cs:244-249`；`GameWorld.GenerateAll` `GameWorld.Generation.cs:355-361`），结果以最后运行的 PCS 为准，GCM 的降水/湿度被覆盖——维护双模型成本高且易漂移。
10. **`GetOverloadRatio` 在 `capacity<=0` 时返回 1**（`CarryingCapacitySystem.cs:125-133`），掩盖配置错误（例如未来把 BaseCapacity 设为 0 时不会报警）。
11. **地图尺寸默认值来自 `GenConfig`**，`GameWorld.Core.cs:63-65` 的 128×64 默认会被 `GenerateTerrainWithConfig` 用 `GetMapDimensions()` 覆盖（`GameWorld.Generation.cs:298-303`）；`GetMapDimensions` 不区分 `TerrainScale`，局部地图同样是 1024×512（`MapGenerationConfig.cs:346-355`）。
12. **`ResourceGenerator` 死代码仍在仓库**，容易被误以为资源经济已实现；`InnovationTree` 的资源加成恒 0（`InnovationTree.cs:640`）。

---

## 4. 动态环境字段设计建议（接入点，不改源码）

### 4.1 新增字段（建议放 `TileEnvironment` 子结构，全部 0..1 归一，避免再出现 0..100 混用）

| 字段 | 类型/范围 | 语义 | 初始值（生成期） | 主要写入者 | 主要消费者 |
|---|---|---|---|---|---|
| `vegetationCover` | 0..1 | 植被盖度（森林/草/灌丛的加权） | 由 biome 查表（森林 0.8~0.95、草原 0.5、荒漠 0.05、农田 0.3） | 生态 tick、伐木建筑、火灾、放牧、城市化 | 侵蚀、肥力、承载力、资源再生、渲染 |
| `soilOrganic` | 0..1 | 表土有机质 | `0.3 + 0.7×fertilityBase`，湿润区更高 | 凋落物积累、耕作消耗、侵蚀、洪水沉积 | 肥力（主要因子） |
| `soilSalinity` | 0..1 | 盐碱度 | 干旱区 `0.2+`，湿润区 0；沿海低地更高 | 灌溉（干旱区上升）、蒸发、洪水/降雨淋洗 | 有效肥力、作物产量 |
| `erosionRate` | 0..1（或 mm/yr 归一） | 侵蚀强度/潜在流失 | `f(slope, precip, vegetationCover)` | 生态 tick、暴雨/洪水、坡地开垦 | 有机质流失、河道淤积、肥力 |
| `waterTable` | 0..1（1=地表水） | 地下水位/可用灌溉水 | `f(距河/湖/海, precip, 海拔)` | 灌溉抽取（下降）、洪水/河道（上升） | 干旱敏感性、灌溉上限、产量 |
| `landUse` | enum/int | Virgin/Cropland/Pasture/Logged/Urban/Mine | 生成期按 biome 设 Virgin；聚落格 Urban | 建城、农业建筑、伐木、采矿、放牧 | 产量公式、植被目标值、侵蚀 |
| `degradation`（可选） | 0..1 | 综合退化度（派生） | 0 | 由上述字段派生 | UI/警告、承载力 |

同时建议：
- `fertility` 保留为**派生缓存**（兼容现有读取点），公式改为
  `fertilityEff = clamp01(fertilityBase(biome, T, P, slope, elev) × (0.4+0.6×soilOrganic) × (1−0.8×soilSalinity) × (0.5+0.5×vegetationCover) × (1−0.5×erosionRate) × waterFactor(waterTable))`；
- 所有新字段默认值在 `GameWorld.Core.cs` 的 TileData 初始化与 `GameWorld.Generation.cs:290-303` 的初始化处补上；**禁止用 0 表示"未初始化"**（用显式 `initialized` 或版本回填），避免重蹈 0 号物产 sentinel 覆辙。

### 4.2 消费系统（建议新增/改造）

1. `EnvironmentTickSystem`（建议每 10~30 天一次，区域/脏块级，避免每日全表）：
   - 植被演替：`veg += (vegTarget(T,P,biome,landUse) − veg) × regrowRate × Δt`；`landUse=Urban` 时 target=0.05；`Logged` 时先降后恢复；火灾/蝗群即时扣除。
   - 有机质：`org += (litter(veg) − decay(org,T) − tillage(landUse) − erosionOut) × Δt`。
   - 盐碱化：`sal += (irrigationInArid(landUse, waterTable, P) − leaching(P, flood)) × Δt`；`Clamp01`。
   - 侵蚀：`ero = f(slope, P, veg)`；坡地+低植被+暴雨升高；洪水事件即时加成。
   - 水位：河流/湖泊/海岸邻接给 baseline；灌溉区按耕地面积下抽；洪水上升；`Clamp01`。
2. `FoodProductionSystem`（每日或每旬，区域聚合）：`regionFoodOut = Σ_tiles( arableArea(landUse) × yieldBase(fertilityEff, T, P, frostFreeDays, accumulatedTemp) × techMult × seasonMult )`，然后 `TradeCenter.AddGoodsReturningAccepted(0, regionFoodOut)`；在产量公式里显式使用 `frostFreeDays/accumulatedTemp`（让它们从"死字段"变成经济输入）。
3. 改造现有消费者：
   - `CarryingCapacitySystem`：`CC = 100 × biomeMult × (0.5+1.5×fertilityEff) × waterFactor × techFactor`，去掉"库存/人口"因子（或保留为短期调节并明确注释）；
   - `TradeCenter.UpdateSupplyDemand`：`localSupply = 实际产量`（由 `FoodProductionSystem` 写入），而不是 `fertility×development` 猜测；
   - `EconomyManager.SettleTaxes`：`baseOutput` 用 `fertilityEff/landUse` 而不仅是 `fertility×development`；
   - `DisasterSystem`：旱灾扣 `soilHumidity/waterTable/产量`，洪水扣当季产量并加 `soilOrganic`、降 `vegetation`，蝗灾扣 `vegetation/产量` 而非只扣肥力；
   - `RaceData.CalculateEnvironmentFitness`：可加入 `foodSecurity`/`vegetation` 作为宜居项；
   - `ResourceGenerator`：接进生成流程，并按 `depletion += extraction/abundance` 更新（矿/林/渔）；
   - `MapRenderer`：用 vegetation/landUse 着色（森林/农田/城市肉眼可见）；
   - `SettlementAISystem`/`SandboxSpawnSystem`：选址统一用同一 `fertilityEff` 与 `waterTable`；
   - `TileInfoQueryAdapter`/UI：暴露环境字段供玩家查看（否则仍是静默系统）。
4. 存档与开关：
   - `TileSaveData` 扩展：`vegetationCover, soilOrganic, soilSalinity, erosionRate, waterTable, landUse, envInitialized`（float/int）；`MapSaveData` 复用同一结构；
   - 版本：`GameConstants.SaveVersion` 从 16 升到 17（`Core.Constants/GameConstants.cs:13`），在 `SaveJsonMigration`（`SaveJsonMigration.cs:22-70`）加 `version<17` 的回填：用当前 `biome/fertility/annualTemp/annualPrecipMm` 反推初始植被/有机质，`waterTable` 由「isRiver/isCoast/waterAdjacentWeight」估算，`landUse` 按聚落/建筑推断；
   - 功能开关：`WorldConfig.enableDynamicEnvironment`（默认 false 供旧档/保守模式；新档默认 true），关闭时 `EnvironmentTick` 直接 return，读写仍走派生 `fertilityBase`（保证存档双向兼容）；
   - 兼容性规则：新增字段缺失时不能当 0 用（0 可能是合法值），必须靠 `envInitialized` 或版本号触发回填；JsonUtility 读取旧档缺字段=0 是默认行为（`GameSaveSystem.cs:168` `JsonUtility.FromJson`），所以迁移必须显式写回。
5. 顺序/确定性：环境 tick 放在 `world.map`(10) 之后、`economy.daily`(12) 之前（参考 `WorldMapSchedule.cs:22`、`EconomySchedule.cs:21` 的优先级编号）；用固定 seed 的 RNG，禁止在并行循环里用 `UnityEngine.Random`；产量入库与人口消费同一 tick 内先后明确（建议先产后人）。
6. 性能约束：环境 tick 以"区域聚合 + 每 10~30 天"运行；每日只做增量（灾害、建城、伐木、耕作改变时 `MarkEnvironmentDirty(tile)`）；禁止每日 O(N) 全表（当前已有多处，见 P1-10）。

### 4.3 最小改动路线（建议分三期）

- **Phase 1（修链路，不改玩法深度）**：`FoodProductionSystem`（用现有 `fertility`、`biome`、`annualTemp/annualPrecipMm`、`development`）+ 创建 `TradeRoute`（按省/邻接区域）+ 修 `UpdateStorageCapacities` O(N)。做完后"地形→吃饱"首次成立，可观测：肥沃地区库存稳定、沙漠地区需要贸易。
- **Phase 2（加环境状态）**：新增 §4.1 字段 + `EnvironmentTickSystem` + 灾害/开发反馈 + 存档扩展/迁移/开关。
- **Phase 3（人口/聚落地理重构）**：人口按环境适宜度分布、区域改省份/流域、`SettlementData.population` 与地块人口统一。

---

## 5. 最小验证/回归清单（不跑 Unity，可写成 EditMode 测试）

1. **产出守恒/链路**：构造 1 省 16 格（各 fertility=0.8、development=0.3）→ 跑 10 天 → 断言区域库粮食增加 且 `Σ产量 == Δinventory + 消费`。
2. **地形分级**：同人口、同 development，fertility=0.9 与 0.1 的两块地 → 产量比≈5~9 倍；沙漠 biome 产量 < 冲积平原。
3. **气候敏感性**：改 `WorldConfig.greenhouseFactor` 后 `RecalculateDerivedAll()` → 断言 `fertility`/产量随 T/P 改变（当前会失败，即证明 P1-4）。
4. **标度断言**：遍历所有地块断言 `0<=fertility<=1`、`0<=soilOrganic<=1` 等；用 `NomadAI/SwarmAI` 阈值处断言行为（当前会失败）。
5. **肥力恢复**：连续 365 天不耕作/不灾害 → 断言 `soilOrganic/vegetation` 回归平衡、`fertilityEff` 上升；蝗群一次不应清零。
6. **存档往返**：旧版（无新字段）存档读入 → 断言 `envInitialized=true` 且字段由回填公式得到（非 0）；新版存档往返一致；开关关闭时行为与旧版一致。
7. **性能基准**：1024×512 地图上 `UpdateStorageCapacities` 应从 >100s 降到 <1s；环境 tick 单次预算 <50ms。
8. **贸易**：创建 2 条路线 → 断言发车/到货/守恒（`EconomyConservationTests` 已有类似用例，可复用 `Assets/Tests/EditMode/EconomyConservationTests.cs`）。

---

## 6. 待验证 / 未覆盖

1. **`ResourceGenerator.GenerateAll` 是否通过反射/Editor 调用**：本次全仓库 .cs 检索无调用者（只测试引用 `RollDepth`），但未检查 UnityEditor 脚本/反射；若确无调用，"资源经济未实现"成立。
2. **`isClimateDirty/isTerrainDirty/isEconomyDirty`** 字段是否在别处写入（本次检索未见），疑似废弃标记。
3. **两套气候模型（GCM vs PlanetClimateSimulator）在运行时谁最终生效**：按代码顺序 WMS.RecalculateAll 在 `RecalculateHydrology` 内后跑，覆盖 GCM 结果；但若运行时另有 `CalculateClimate` 调用路径（未检索到），结论需修正。
4. **`annualTemp/annualPrecipMm` 的季节覆写在读档后是否被重算**：读档恢复的是上次季节快照，可能全年停在某一季；需运行验证。
5. **地图尺寸实际取值**：默认 `MapSize=Medium`（1024×512）已确认，但 UI/新游戏请求可能覆盖 `MapSize`；不同尺寸下 P0-2 量级按 R×N 同比例放大。
6. **`SettlementData.population` 是否有隐藏消费方**（如 UI 人口显示之外的赋税/兵役）：本次只确认经济/食物链路不消费它。
7. **`RealmSituationBuilder.foodSecurity` 是否间接影响 tile satisfaction**：目前只看到 `ClassNeedsSystem`，未追到人口增长公式，需确认。

---

## 7. 验收对照

- 耦合链路：已覆盖 `TileData` 的 annualTemp/annualPrecipMm/airHumidity/soilHumidity/accumulatedTemp/frostFreeDays/climateZone/biome/fertility/development → CarryingCapacity、ResourceGenerator、食物/库存、人口增长、市场/贸易/税收、聚落/建筑/AI、灾害、季节/气候变化，全部带 `文件:行` 与公式。
- 不合理清单：**P0 2 条 + P1 10 条 + P2 12 条**，均附证据、触发条件、后果、修法方向（含 P0-1「无食物产出」、P0-2「O(R×N) 卡死」这两个可直接复现的错误）。
- 字段建议：给出 7 个新环境字段、派生肥力公式、5 个消费系统接入点、`TileSaveData` 扩展 + `SaveVersion 16→17` 迁移 + `enableDynamicEnvironment` 开关兼容方案。
- 只读：未修改 `MyriadCreation/Assets/**`，未运行 Unity/编译/测试；唯一写入本文件。
