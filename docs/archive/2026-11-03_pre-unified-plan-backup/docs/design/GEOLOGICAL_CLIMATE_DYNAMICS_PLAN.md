# 项目完成度评估 & 动态地质/气候系统设计

> **重要修正（2026-11-02）**：本文早期稿把经济简化为"食物/粮食"，这是错误的。
> 本项目物产是具体 `GoodsDef`（30+ 种，带物理属性、加工链、产地、保质），需求是阶层化的
> （`ClassNeedWeightTable` 5 阶层 × 8 维度），不存在"抽象粮食"或"生理条件"。
> 环境系统应影响**具体物产的产出/可获得性/价格**，再经**阶层需求篮子**传导。
> 详见配套修正文档 [GOODS_CLASS_ENVIRONMENT_MODEL.md](GOODS_CLASS_ENVIRONMENT_MODEL.md)；
> 下文凡出现"食物/粮食/产量"处，均应按该文档理解为"具体物产"。

> 依据：本轮三份只读审计 ——
> [PROJECT_COMPLETION_ASSESSMENT.md](PROJECT_COMPLETION_ASSESSMENT.md)、
> [TERRAIN_DYNAMICS_INTEGRATION.md](TERRAIN_DYNAMICS_INTEGRATION.md)、
> [TERRAIN_ECONOMY_COUPLING_AUDIT.md](TERRAIN_ECONOMY_COUPLING_AUDIT.md)
> 及代码/测试基线（EditMode 594/594、PlayMode 9/9/9、COMPILE_OK）。

---

# 一、项目完成度评估

## 1.1 规模（实测）

| 项 | 数值 |
|---|---|
| 程序集 | 16（运行时 12 + Editor/Tests/PlayMode 4） |
| C# 文件 | 956（Scripts 812 / Tests 109 / UI 28 / Editor 7） |
| 代码行 | ≈ 46,000（Scripts+UI+Tests） |
| EditMode 测试 | 102 文件 / **594 用例**（当前全绿） |
| PlayMode 测试 | 7 文件 / **9 用例**（无图形 + 有图形均绿） |
| 内容 | StreamingAssets JSON 30 + CSV 8 + 8 语言 |
| 美术 | 48 文件（19 Shader + 6 字体，**贴图/模型/动画几乎为空**） |

## 1.2 分模块完成度（0–100%，含依据）

| 模块 | 完成度 | 有什么 | 关键缺口 |
|---|---|---|---|
| 世界生成/地形 | 72% | 球面噪声、海陆、区域模板、Voronoi、PCA、水力侵蚀、断块/破碎化 | 板块构造、生成参数 UI/存档入口、默认开局不走完整管线 |
| 气候/群系 | 60% | `PlanetClimateSimulator`（季节）、`AtmosphericCirculation`（风/压/降水）、`OceanCurrentSimulator`、Holdridge 分类、气候带 | 默认开局不跑 GCM、气候只生成期算、季节值覆写年值、PCS 与 Holdridge 双实现漂移（`PlanetClimateSimulator.cs:353` 死分支） |
| 生态/植被/土壤 | **22%** | 仅 `fertility` 标量 + `soilHumidityPct`（只写不读） | 无植被覆盖率、土壤有机质、盐碱化、侵蚀、水位、土地利用 |
| 灾害 | 50% | 11 类灾害 + 每日调度 + 稳定性/肥力扣减 | 定义硬编码、0 行为测试、状态不入档、地震/火山不改地形、无海啸/海侵/盐尘暴、`Famine` 频率为 0 |
| 经济/食物/人口 | 75% | 市场、贸易中心、税基、消费、饥荒压力 | **地形→食物产出链缺失**（食物池只有初始化 1000/区）；贸易路线在正式代码从未创建；承载力不含 fertility/T/P；O(区域×全表) 性能炸弹 |
| 聚落/城市 | 72% | 选址、等级、建筑、人口块、毁灭 | 人口与地形/粮食脱节；聚落 `population` 独立不吃饭 |
| 法律/政令 | 80% | JSON 目录、效果、政令、司法、改革 | 最小回退集小；部分硬编码仍在 |
| 信仰 | 58% | 教义、圣人、大圣战、教阶 | 行为测试薄；与气候/灾害（天罚/祈雨）无联动 |
| 文化 | 55% | 阶段、演化、语言/族裔/传统 | 行为测试薄；与环境（农牧/迁徙）无联动 |
| 角色/家族 | 78% | 属性、特质、关系、家族、继承、DNA | 性格/特质 UI 已补；家族树交互仍浅 |
| 军队/战争 | 55% | 军队、补给、战斗骨架、围城 | 战争闭环（战斗/占领/和约）无行为测试，明细未持久化 |
| 外交/派系 | 48% | 关系、附庸、同盟、派系、政变 | **关系/同盟/条约/附庸/派系不入档**，读档归零 |
| AI | 45% | 控制器、寻路、游牧/蝗群 | 决策无行为测试；标度混用 bug（0..1 与 0..100） |
| 存档/读档 | 78% | SaveVersion 16、DTO 迁移、原子写 | 灾害/外交/派系/战争明细缺失；v16 部分运行时状态丢失 |
| UI | 70% | 契约化面板、字体设置、政府/聚落/角色/地块 | 无气候/环境/植被显示；美术资产近空 |
| 架构/资产 | 80% | DAG、门禁、Content 收敛、资产归位、TMP 门禁 | 无（架构层已收口） |
| **全项目** | **≈ 62–65%** | 系统骨架完整、可玩闭环仍缺 | 生态/气候动态、战争外交闭环、终局、美术 |

## 1.3 最严重的问题（P0，先于任何环境新功能）

1. **食物产出链缺失**：地形肥力不进任何产出池；默认 1024×512 地图每区 1000 粮、人口消耗 ≈1.6 粮/日 → 约 600–700 天全图饥荒，与地形无关（`GameWorld.Core.cs:801`、`EconomyManager.cs:93-106`、`EconomyConservationTests.cs:141+`）。
2. **性能炸弹**：`UpdateStorageCapacities` 为 O(区域×全表) ≈ 1.7e10 次比较（1024×512=524,288 tile × 32,768 region），首次日结即卡死；加环境 tick 前必须先修。
3. **存档缺口**：`DisasterSystem` 活动灾害、`DiplomacyManager` 关系/附庸、`FactionManager`、战争明细均不入档，读档归零（`SaveVersion=16` 但无对应表）。
4. **生态/土壤闭环不存在**：只有 `fertility` 标量，肥力只降不升（除一次性建筑/火山），土地退化→饥荒不成立。
5. **无胜利/终局条件**（全仓 0 Victory/WinCondition）。

---

# 二、不合理设计清单（有证据，按严重度）

| # | 问题 | 证据 | 处理建议 |
|---|---|---|---|
| D1 | 灾害定义硬编码在系统里，Content 已有 `DisasterDef` 类型却无 JSON 加载 | `DisasterSystem.cs:32-150`；`DisasterDef.cs` | 移入 `StreamingAssets/Base/Disasters/Disasters.json` + 最小回退 |
| D2 | 灾害/资源生成用全局 `UnityEngine.Random`，不可复现 | `DisasterSystem.cs:7/179/209`；`ResourceGenerator.cs:68/72/73`；`MapGenerationConfig.cs:127/363` | 注入种子化 RNG（seed+year+tile 派生），存档保存 RNG 状态 |
| D3 | 气候只在生成期计算，运行期不更新；`isClimateDirty` 死字段 | `PlanetTerrainGenerator.cs:160-230`；`TileData.cs:108`；`WorldMapSchedule.cs:16/72-78` | 已有 `PlanetClimateSimulator`，接入 LongTerm/季节调度 + dirty tile 集合 |
| D4 | 季节直接覆写 `annualTemp/annualPrecipMm`（年值语义被季节值污染） | `PlanetClimateSimulator.cs:159-188` | 拆 `seasonTempMod/seasonPrecipMod` 与年值字段；消费者只读年值 |
| D5 | 双气候/群系实现漂移（Holdridge vs PCS.DetermineBiome），Subarctic 两支都 Tundra | `PlanetClimateSimulator.cs:353`；`PlanetTerrainGenerator.cs:228`；`HoldridgeBiomeClassifier` | 统一为"一个分类器 + 一个气候模型"；删除死分支 |
| D6 | `BiomeType` 混用地貌（AlluvialPlain/FoldMountains）与植被气候（TropicalRainforest/Desert） | `GameEnums.cs:28-87` | 新增 `LandformType` 或把植被/生态从 biome 派生；过渡期用映射表 |
| D7 | 肥力只降不升；无土壤形成/休耕/有机质 | `DisasterSystem.cs:313/326/329`；`BuildingSystem.cs:271`；`SwarmAI.cs:62` | `soilOrganic` + 恢复项；fertility 改为派生量 |
| D8 | 0..1 与 0..100 标度混用（游牧/蝗群判断永远不成立/永远成立） | `NomadAI.cs:37/48`、`SwarmAI.cs:49`、`MapActorManager.cs:227` | 全仓统一 0..1（或统一 0..100），加测试 |
| D9 | 承载力不含 fertility/T/P/开发；食物因子是消耗型库存（因果倒置） | `CarryingCapacitySystem.cs:92-109` | 改为"潜在产能 = f(土壤、气候、技术、开发) − 退化"；库存只作短期缓冲 |
| D10 | 生成管线重复/空阶段：Terrain 阶段已生成省/聚落/社会，后续阶段再跑；Biomes 空实现；默认开局不走管线 | `GenerationPipeline.cs:12-23/228-269`；`GameWorld.Generation.cs:122-138/355-362` | 重排阶段：Tectonics→Terrain→Climate→Hydrology→Biomes/Ecology→Provinces→Settlements→Society；默认开局走同一管线 |
| D11 | 生成参数（`MapGenerationConfig`）不可配置、不进 SetupOptions/UI | `MapGenerationConfig.cs:114-277`；`WorldSetupOptions.cs:9-31` | 把关键参数暴露到 `WorldSetupOptions` + 主菜单自定义面板 + 存档 |
| D12 | 灾害只改 stability/development/fertility，不扣库存/产量/价格/贸易；`Famine` 频率=0 | `DisasterSystem.cs:282-330`；`ActiveDisaster` | 灾害效果接入经济/人口/环境；灾害入档 + 编年史 |
| D13 | 默认地图 1024×512 = 524,288 tiles，全图扫描多（气候/水文/灾害/税/季节） | `MapGenerationConfig.cs:346-355`；`DisasterSystem.cs:218+`；`GameWorld.GenerateAll` | 分区/脏块/跨帧预算；环境 tick 只处理脏块与区域聚合 |
| D14 | 外交/派系/战争明细不入档 | `SaveVersion=16`；`DiplomacyManager`/`FactionManager` 无 SaveData | 新增 SaveData 表 + SaveVersion 17 |
| D15 | 美术资产近乎为空（无贴图/模型/动画） | `Assets/Art` 48 文件 | 这是产品完成度问题，不是代码问题；需要美术管线 |

---

# 三、可复用的现有基础（你的新功能不必从零开始）

| 已有模块 | 位置 | 可复用点 |
|---|---|---|
| `PlanetClimateSimulator` | `World/Climate/` | 季节气候、风场、`RecalculateAll/RecalculateDirty/UpdateForSeason` |
| `AtmosphericCirculation` | `World/Climate/` | 气压/风/湿度/降水/季风/雨影 |
| `OceanCurrentSimulator` | `World/Hydrology/` | 洋流（风驱动+科氏力+扩散）、暖/寒流、沿海效应 |
| `HydrologySystem` | `World/Hydrology/` | 河网/汇流/流向（最小堆） |
| `HydraulicErosion` | `World/Terrain/` | 水力侵蚀（生成期） |
| `DisasterSystem` + `DisasterDef/Type` | `Simulation/Systems` | 11 类灾害骨架（需迁 JSON + 确定性） |
| `WorldMapSchedule` | `Simulation/WorldState` | 区域地块年度更新模式（可作环境 tick 模板） |
| `SimulationScheduler` | `Simulation/Scheduling` | cadence：Daily/Weekly/Monthly/Seasonal/**LongTerm(365)** |
| `TileData` 气候字段 | `Core.Data` | 已有 temp/precip/humidity/biome/climateZone/fertility |

---

# 四、动态地质/气候系统设计（板块漂移 + 局内变化）

## 4.1 设计目标

1. **生成期模拟**（新开局可选）：板块构造 → 海陆/山脉/火山/盆地 → 洋流 → 气候 → 水文 → 初始土壤/植被。
2. **局内变化**（运行时可选、带开关）：气候波动、海平面/海侵海退、侵蚀/沉积、盐碱化/荒漠化、植被演替、人类开发反馈、地质灾害/气象灾害。
3. **所有开关默认关闭/保持旧行为**；旧存档可加载、可随时开启动态化（用当前状态初始化）。
4. 性能可控：环境 tick 只跑脏块/区域聚合，绝不每帧全图。

## 4.2 模式与开关

`WorldSetupOptions`（生成期）新增：

| 开关 | 默认 | 作用 |
|---|---|---|
| `enablePlateTectonics` | false（新档推荐 true） | 生成板块、边界、造山/裂谷/热点 |
| `enableClimateModel` | true（生成期） | 跑 GCM/季节气候（现默认开局应接入） |
| `enableHydrology` | true | 河网/湖泊/洋流 |
| `enableInitialEcology` | false | 生成初始植被/土壤/盐碱 |
| `plateCountHint` | 8 | 板块数量范围 |
| `geologicalActivity` | 1.0 | 构造/火山/地震频率倍率 |

运行时开关（存档 `EnvironmentDynamicsOptions`，主菜单/设置可改）：

| 开关 | 默认 | 作用 |
|---|---|---|
| `enableClimateDynamics` | false | 年际/十年际气候漂移、ENSO 类振荡 |
| `enableSeaLevelChange` | false | 冰量/温度驱动海平面、海侵海退 |
| `enableErosionSediment` | false | 侵蚀/沉积/三角洲进退 |
| `enableVegetationDynamics` | false | 植被演替、覆盖度变化 |
| `enableSoilDynamics` | false | 有机质、盐碱化、荒漠化 |
| `enableHumanImpact` | false | 伐木/灌溉/过牧/城市化反馈 |
| `enableGeologicalDisasters` | true | 地震/火山/海啸/滑坡（受构造应力驱动） |
| `enableMeteorologicalDisasters` | true | 旱/涝/风暴/热浪/寒潮/蝗灾/野火 |
| `environmentSpeed` | 1.0 | 环境时间倍率（0.25–10） |
| `geologicalTimeCompression` | 1000 | 1 游戏年 = N 地质年（用于板块漂移可见化） |

> 运行时全关 = 完全保持现有行为（气候/地形静态），旧档兼容。

## 4.3 数据模型（TileData 新增，归一 0..1，byte/ushort 量化）

| 字段 | 类型/范围 | 语义 |
|---|---|---|
| `plateId` | short | 所属板块（-1 海洋/未分） |
| `tectonicStress` | byte 0..1 | 边界应力累积（地震触发） |
| `crustAge` | byte 0..1 | 洋壳年龄（越老越深） |
| `vegetationCover` | byte 0..1 | 实际植被覆盖率（**不是所有群系都 100%**） |
| `vegetationPotential` | byte 0..1 | 气候/土壤决定的潜在上限 |
| `soilOrganic` | byte 0..1 | 土壤有机质/肥力基础 |
| `soilSalinity` | byte 0..1 | 盐碱化（灌溉/海侵/蒸发） |
| `waterTable` | byte 0..1 | 地下水位/可用水 |
| `erosionRate` | byte 0..1 | 当前侵蚀强度（累积） |
| `sedimentDepth` | byte 0..1 | 沉积厚度（三角洲/平原） |
| `landUse` | byte 0..1 | 人类开发强度（伐木/农田/牧场/城市） |
| `irrigation` | byte 0..1 | 灌溉强度（盐碱化驱动） |
| `environmentInitialized` | bool | 旧档首次开启动态时初始化标记 |

**植被覆盖率示例上限（不必 100%）**：TropicalRainforest 0.85–0.98；BorealForest 0.70–0.90；TemperateGrassland 0.45–0.75；Savanna 0.25–0.50；SemiAridShrubland 0.10–0.30；HotDesert 0.01–0.08；SaltDesert 0–0.03；Wetland 0.90–1.00；IceSheet 0。实际值受干扰（火/放牧/伐木）向下偏离，受演替向上恢复。

**fertility 改为派生**（不再单独维护）：

```
fertility = fertilityBase(biome, T, P, slope, elevation)
          × (0.4 + 0.6×soilOrganic)
          × (1 − 0.8×soilSalinity)
          × (0.5 + 0.5×vegetationCover)
          × (1 − 0.5×erosionRate)
          × waterFactor(waterTable)
```

## 4.4 生成期流程（GenerationPipeline 重排）

```
Seed → Tectonics(新) → Terrain → Features(海陆/海岸) → Climate(GCM)
     → Hydrology(河/湖) → OceanCurrents(洋流) → Biomes/Ecology(群系+初始植被/土壤)
     → Provinces → Settlements → Society
```

- **Tectonics**：Voronoi 板块（复用 `SphericalVoronoi`）→ 板块运动向量 → 边界类型（汇聚/离散/转换）→ 造山带/裂谷/岛弧/热点；输出 elevation 基线 + `plateId` + `tectonicStress` 初值。
- **Climate**：现有 `PlanetClimateSimulator` 跑全量 `RecalculateAll()`；接入 `AtmosphericCirculation`（风/雨影/季风）与 `OceanCurrentSimulator`（暖流增温增湿、寒流降温减湿、沿海效应）。
- **Ecology**：由气候/土壤/坡度派生 `vegetationPotential`，`vegetationCover = potential × (1 − disturbance)`；`soilOrganic = f(biome, T, P, 初始剖面)`；`soilSalinity = f(沿海/干旱/灌溉)`。
- **深时预热（可选）**：生成后快速跑 N 个"地质年"步（仅侵蚀/沉积/土壤，不全图气候），让地形/土壤看起来"有历史"；开关控制 N 与耗时。

## 4.5 局内演化系统

新增 `Simulation.Systems.Environment`（或 `Simulation.Environment` 子程序集）：

| 系统 | 调度 | 作用 |
|---|---|---|
| `ClimateDynamicsSystem` | Seasonal + LongTerm | 年际温度/降水距平、ENSO 振荡、火山冬天、轨道漂移；写气候字段与 dirty |
| `SeaLevelSystem` | LongTerm (每 5–10 年) | 冰量/温度→海平面；海侵海退改 `isLand/isCoast/oceanTier`，沿海盐碱化 |
| `ErosionSedimentSystem` | LongTerm (每 10 年) | 河流/坡面侵蚀、沉积、三角洲；仅沿河网/海岸与脏块 |
| `VegetationSystem` | LongTerm + Seasonal | Logistic 生长趋近 `vegetationPotential`；火/放牧/伐木干扰；演替（裸地→草→灌→林） |
| `SoilSystem` | LongTerm | 有机质积累/分解、盐碱化（灌溉+蒸发）、荒漠化阈值、恢复（休耕） |
| `LandUseSystem` | Monthly/Yearly | 人口密度/聚落等级/农田/牧场/伐木→`landUse`、`irrigation`；城市化硬化地表 |
| `DisasterSystem`（扩展） | Daily | 气候/构造驱动灾害；效果接入库存/产量/人口/环境；灾害编年史 |

调度：`terrain.dynamics.longterm`（LongTerm, order 9，早于 WorldMap 10），每 365 天；内部按 `environmentSpeed` 拆成 1–N 步、按脏块/区域聚合，禁止全图。

**人类开发反馈（关键）**：

- 伐木/开垦：`landUse↑` → `vegetationCover↓`、`erosionRate↑`、`soilOrganic↓`；
- 灌溉（干旱区）：`irrigation↑` → 短期产量↑，长期 `soilSalinity↑` → 肥力↓ → 弃耕/荒漠化；
- 过牧：`landUse↑` → 植被↓、侵蚀↑；
- 城市化：`landUse↑` → 下渗↓、洪水风险↑、本地温度↑（热岛）；
- 政策/科技（法律/革新）可提供系数修正：轮作、休耕、梯田、水利、护林。

## 4.6 灾害扩展

| 类别 | 灾害 | 触发因子 | 效果 |
|---|---|---|---|
| 气象 | 干旱/洪水/风暴/热浪/寒潮/野火/蝗灾 | 降水距平、温度、洋流、植被/燃料 | 产量、库存、价格、植被、土壤、人口 |
| 地质 | 地震/火山/滑坡/海啸 | 板块边界应力、坡度、降雨、岩性 | 地形/建筑/道路/人口/火山灰气候 |
| 海洋 | 海啸/海侵/咸潮入侵 | 海底地震、海平面、潮汐 | 沿海聚落、盐碱化、港口 |
| 生物 | 蝗灾/疫病/森林火灾 | 植被/作物状态、气候 | 食物、植被、人口 |

新增 `DisasterType`：`Tsunami`、`SeaLevelTransgression`、`SaltwaterIntrusion`、`Sandstorm`、`Wildfire`（复用现有 11 类）。

## 4.7 架构放置

| 层 | 新文件（建议） |
|---|---|
| `World.Terrain` | `PlateTectonicsGenerator.cs`、`PlateBoundary.cs`、`ErosionSedimentSystem.cs`（生成期） |
| `World.Climate` | 复用 `PlanetClimateSimulator`/`AtmosphericCirculation`；新增 `ClimateDynamics.cs`（距平/振荡） |
| `World.Hydrology` | 复用 `OceanCurrentSimulator`/`HydrologySystem`；新增 `SeaLevelModel.cs` |
| `Simulation.Systems` | `EnvironmentTickSchedule.cs`、`VegetationSystem.cs`、`SoilSystem.cs`、`LandUseSystem.cs`；扩展 `DisasterSystem` |
| `Content.Definitions` | `EnvironmentDef`/`VegetationProfile`/`DisasterDef` JSON 化；`StreamingAssets/Base/Environment/*.json` |
| `Core.Data` | `TileData` 新字段、`EnvironmentDynamicsOptions`、`ActiveDisaster` 入档 DTO |
| `Core.Contracts` | `TileInfoDto` 增湿度/植被/盐碱/侵蚀/板块等字段 |
| `Infrastructure.Save` | SaveVersion 17 + `TileSaveData`/`EnvironmentSaveData` + 迁移 |
| `UI` | 主菜单/设置开关；地块面板环境字段；世界概览 |

## 4.8 存档与兼容

- **SaveVersion 16 → 17**：`TileSaveData` 加新字段（默认 0）；新增 `EnvironmentSaveData`（板块、全局气候、海平面、RNG 状态、开关）；`SaveJsonMigration` 从 v16 回填：按 biome/climate 推 `vegetationPotential/Cover/soilOrganic/soilSalinity`。
- 旧档默认所有动态开关 **false**；用户开启时调用 `EnvironmentInitializer` 从当前状态生成新字段（标记 `environmentInitialized`）。
- 灾害/外交/派系等运行时状态（D14）建议同一版本补齐（独立迁移）。

## 4.9 确定性

- 新增 `IRandomSource`（种子化 PCG/`System.Random`），由 `world.randomSeed + year + tile/plate` 派生；替换 `DisasterSystem`/`ResourceGenerator`/生成期 `UnityEngine.Random`。
- 存档保存 RNG 计数器/状态；同种子同操作 → 同结果（测试覆盖）。

## 4.10 性能预算

- 全图 524,288 tile 不可每次全扫：环境 tick 用 **脏块 + 区域聚合**，每块 32×32；LongTerm 每 365 天一次，目标 < 50ms（跨帧/协程分片）。
- 气候：GCM 只在生成期全量；运行时只对脏块（火山/海侵/大灾害/人类开发）局部重算。
- 植被/土壤：只更新有聚落/人类活动/灾害/气候异常的脏块；其余按 10 年周期抽样。
- 渲染：复用 `isTerrainDirty/isClimateDirty` 标志驱动地图重绘（当前 dirty 字段未被使用，正好接上）。

## 4.11 测试与验收

| 类别 | 用例 |
|---|---|
| 生成期 | 同种子 → 同板块/海岸/气候/植被（确定性）；板块边界/造山带存在；气候→群系一致性；开关关闭时与旧版逐 tile 一致 |
| 气候 | Holdridge/PCS 单测；季节不污染年值；洋流暖/寒效应；雨影/季风 |
| 生态 | 植被上限按群系（沙漠≠100%）；干扰下降/演替恢复；盐碱化阈值；荒漠化不可逆阈值 |
| 人类 | 伐木→植被↓；灌溉→盐碱↑；休耕→有机质↑ |
| 灾害 | 构造应力→地震频率；旱涝触发；灾害效果进入库存/产量；灾害入档 |
| 海平面 | 升温→海侵；沿海 tile 变海/盐碱化 |
| 存档 | v16→v17 迁移；新字段 roundtrip；旧档开关默认关闭 |
| 性能 | 524k 图 LongTerm tick 预算；环境关闭时 0 额外开销 |
| UI | 地块环境字段展示；开关持久化 |

## 4.15 civ-atlas 算法参考（本轮新增，最贴近的实现）

> 完整对照见 [REFERENCE_PROJECTS_ANALYSIS.md](REFERENCE_PROJECTS_ANALYSIS.md) §6；**AGPL-3.0，只借鉴算法，不复制代码**。

- **生成参数**：`cells`（默认 36k，可到 200k）、`landFraction=0.33`、`plates=30`、`mountains=1`、`temperature=0`、`rainfall=1`、`continents=0`（自动 2~5）、`gather/rugged/hardness/north` 全部可调。
- **板块**：板块绕欧拉轴角速度 `ω`，速度 `v = ω × p`；边界速度差定汇聚/离散；洋壳年龄→海底深度（脊 -2600m → 深海平原 -5000m）、海沟 -7000~-9000m；大陆架/岛弧/热点。
- **侵蚀**：流功率 `dh/dt = U - K·A^m·S`，`K=1, m=0.5, dt=0.9`；Braun & Willett (2013) 隐式解；Priority-Flood（Barnes 2014）填洼；汇水按降雨加权。
- **气候**：纬度温度表（0°→27°C、30°→20、45°→11、60°→1、75°→-11、90°→-24），6.5°C/km；风带 0–30 信风、30–60 西风、60+ 极地东风，±5° 平滑，季节 ±6°；水汽平流/迎风坡/雨影；洋流用流函数环流（副热带 0–46°、副极地 46–72°，西边界流 300km，影响半径 1300–2200km）。
- **生态**：Whittaker 16 群落；每群落数值 `habitability/travelCost/roadCost/trekPace/famineRisk/soilFragility/grain/pasture/timber/oil`（0–1）——**grain/pasture/timber/oil 直接映射我们的物产类别**。
- **资源/发展**：每州 12 类资源 0–3 档（粮/牲畜/铜锡/铁/煤/木/盐/渔/石油/稀有/金银）；发现/可采/枯竭/回长；地形大事后按新地形重分档；时代门槛卡资源；人口上限×生产能力。
- **确定性**：`subSeed(seed,'用途')`、`keyed(cellAnchor, year, purpose)`、`round24` 超越函数舍入；生成器版本号；存档只存 seed/params/edits。
- **地形大事**：在指定年份施加火山/地震抬升/海侵；以"自然力"注入生成流程（侵蚀之前），之后侵蚀/排水/气候/群落重算；**以前历史不变，之后继续**；失败回滚。

**对我们的落地要求**：球面 Delaunay → 我们的 tile 网格（1024×512）+ 球面渲染；AGPL → 独立实现；`grain/pasture/timber/oil` → 具体物产产出函数；资源 0–3 档 + 枯竭/回长 → `TileData` 资源字段与 `GoodsProductionSystem`。

## 4.13 参考实现参数（来自 Earth Simulator，可作为首版初值）

> 详细对照见 [REFERENCE_PROJECTS_ANALYSIS.md](REFERENCE_PROJECTS_ANALYSIS.md)。以下参数直接做成
> `StreamingAssets/Base/Environment/ClimateParams.json` 与 `EcologyParams.json`，可调、可 Mod 覆盖。

**气候**：`T_equator=30, T_pole=-25, fatten=1.25, latTiltDeg=12, seasonAmpMax=22, lapseK=32.5, oceanInertia=0.09, landRelax=0.5, oceanSeason=0.25, diffLand=0.21, diffWater=0.28, advectSST=0.82, windHeat=0.34, co2Sens=3.6, iceThresh=-2, meltThresh=1.5, beltShiftDeg=7, monsoonK=0.055, monsoonMax=0.55, curEvery=8, curIters=110, qsatA=9.0, qsatB=0.058, evapOcean=0.30, evapLand=0.14, windReach=2.2, advH=0.95, rainBase=0.045, rainITCZ=0.17, rainMidlat=0.08, rainSubtropDry=0.72, rainOro=0.55, rainCold=0.45, mmScale=700, seaPerDeg=0.0001, seaRate=0.00002`。

**生态**：Miami NPP（`3000/(1+exp(1.315-0.119T))` 与 `3000(1-exp(-0.000664P))` 取 min，再乘热限制）；肥力 `base(arable)×(0.5+0.5·precipGauss)×(0.6+0.4·tempGauss)+0.35·river−0.40·slope+0.30·volcanic`，乘 `0.45+0.55·eco`、减 `0.30·pollution`；生物群系带 `biomeConfirm` 滞回；`ECO_STRIPE=4` 分条。

**灾害年概率（标准图，按面积缩放）**：火山 0.012、地震 0.022、飓风 0.06、干旱 0.030、洪水 0.028、野火 0.035、瘟疫 0.010、陨石 0.0005、暴雪 0.020；火山灰降温 1.6°C/强喷发；干旱降水×0.38；火灾蔓延 0.55/步。

**动态海平面**：目标 `seaBase + (T̄−T_ref)×seaPerDeg`，年变化不超过 `seaRate`；达到 1e-4 阈值才重算海岸/资源/边界。

## 4.14 数据驱动 schema（借鉴 FantasyMapSimulator）

- `Environment/BiomeResources.json`：群系→物产清单（`goodsId/probability/baseValue/affectedBy[]`），对应 `GoodsDef.originBiomes` 与 `baseAbundance`；`affectedBy` 指向革新/法律/阶层技能修正。
- `Environment/Disaster.json`：灾害定义（类别/年概率/持续时间/强度/影响字段/物产影响权重），替换 `DisasterSystem` 硬编码。
- `Economy/ClassDemand.json`：阶层×物产需求篮子（权重、可替代组、最低/期望数量）。
- `Environment/ClimateParams.json`、`EcologyParams.json`：上节参数，支持 Mod 覆盖。

## 4.12 分阶段实施计划（每阶段可独立验收）

| 阶段 | 内容 | 交付/验收 |
|---|---|---|
| **E0 前置修复** | 物产生产链（ResourceGenerator 落点 + SettlementProduction 接线 + ClassDemandSystem 阶层需求篮子）、`UpdateStorageCapacities` O(n²) 修复、RNG 确定性、灾害入档 | 具体物产（谷物/木材/鱼/盐/矿石）有产出入库；阶层需求按具体物产结算 |
| **E1 数据模型** | `TileData` 环境字段 + `TileSaveData`/`EnvironmentSaveData` + SaveVersion 17 迁移 + 开关数据结构 | 旧档加载、字段 roundtrip |
| **E2 生成期板块** | `PlateTectonicsGenerator` + Pipeline 重排 + `plateId/stress` + 生成期植被/土壤初始化 + 开关 | 同种子确定性；开关关=旧行为 |
| **E3 生成期气候/洋流** | 默认开局接 GCM/洋流/季节；修复双实现/死分支 | 气候→群系一致；性能达标 |
| **E4 局内环境 tick** | Vegetation/Soil/Erosion/SeaLevel/LandUse + LongTerm 调度 + 脏块 | 开关关闭 0 变化；开启后 100 年可见演变 |
| **E5 人类反馈** | 人口/聚落/农田→landUse/irrigation；政策/科技修正；退化→产量→饥荒闭环 | 伐木/灌溉/休耕行为测试 |
| **E6 灾害整合** | 灾害 JSON、板块应力触发、海啸/野火、经济后果、编年史、入档 | 灾害行为测试 + 读档保持 |
| **E7 UI/内容/文档** | 主菜单+设置开关、地块面板、概览；环境 JSON；README/架构图 | 门禁 + PlayMode |

**决策点（需你拍板）**：
1. 板块漂移"局内可见"的时间压缩比（1 游戏年 = 100/1000/10000 地质年？影响视觉与性能）。
2. 动态开关默认值：新档默认开 or 关（建议新档"气候+植被"开、板块/海平面关，旧档全关）。
3. 是否接受 SaveVersion 17（含外交/派系补齐）还是先只做环境字段（v16 不动会丢失字段）。
4. 环境 tick 目标预算（建议 LongTerm < 50ms，跨帧分片）。
