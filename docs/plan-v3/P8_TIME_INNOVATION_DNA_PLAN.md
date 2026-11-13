# P8 时间、城市周期、革新与 DNA 物种计划

> 用户口径（2026-11-14）：
> 1. 不要硬写“200 年建国”；先重新规划时间尺度：一天多少 tick、自转/公转周期参数。
> 2. 以人类为基准，智慧物种有较大种群后约 **5000–7000 年**出现最早城市，是更合理的量级（用户自己也不确定，需作为可调默认值而非历史定律）。
> 3. 革新是当前最大瓶颈：数量不足、研究机制没跑通；法律/法令等系统都靠革新才进入可选列表。
> 4. “社会实践总和/经验累计”机制要重做：不同革新需求不同，最低要求约 50%；灵感基础概率低，除非研究者是天才；每条社会实践经验怎么算要定清楚。
> 5. DNA 要加速：更细致的 DNA、与环境耦合、遗传变异、物种演化、适者生存。
> 6. 仓储设施建筑单独做：粮仓/金库等提供容量，设施可升级、消耗材料，配方表要设计。
> 7. 以上与现有 P7.1（革新/法令）、P7.5（长期气候）、P7.6（仓储设施）合并规划；先读代码再动手。

---

## 0. 已核实的代码事实（2026-11-14）

| 项 | 现状 | 位置 |
|---|---|---|
| 时间步 | `tickInterval=1s`、`daysPerTick=1`；1x 下 1 秒=1 游戏日，30x 下 1 秒=30 日 | `GameWorld.Core.cs:73/75`、`GameWorld.Simulation.cs:425-433` |
| 日/年 | 硬编码 365；`currentDay` 回绕到 1；全工程 47 处 `365` | 同上，`OfficeSystem`、`LawEffectRuntime`、`WarRules` 等 |
| 季节 | 每 91 天换季（`(day-1)/91`） | `GameWorld.Simulation.cs:433` |
| 倍速 | `GameSpeedScale` 五档 1/2/5/10/30，上限 30 | `App.Contracts/GameSpeedScale.cs` |
| 建国 | 聚落 ≥200 年、人口 ≥800、开发 ≥15、180 天口粮 | `StateFormationSystem.cs` |
| 聚落生成 | 世界开局即生成村庄/城市，聚落从第 0 年存在 | `AnchorGenerator` |
| 革新内容 | 186 条；era 0–5；researchCost 20–1500（均值 318） | `Innovations.json` |
| 政权研究 | 每日 `researchRate` 加到 `researchCost`，到点完成；无实践门槛、无灵感门槛 | `InnovationTree.DailyTick`、`InnovationResearchSchedule` |
| 个人实践 | `PracticeRecordedEvent` 只由建造（2 处）与战斗（1 处）发布；`Learn/SetMastery` 零调用者 → 个人熟练度恒 0 | `ResearchPlanSystem`、`InnovationKnowledgeSystem` |
| 实践驱动模型 | `MonthlyTickProgress` 整段未接线，只有 legacy 研究点路径在跑 | `InnovationTree.cs:798` |
| 解锁引用 | `unlockRefs` 120 条引用，运行时 0 消费者 | `InnovationDef.cs:72` |
| 法律门槛 | 50 条法律/12 组，仅 19 条由 10 个革新 id 把关；门槛链已存在 | `LawAndDecreeSystem.CanEnactLaw`、`Laws.json` |
| 革新 UI | `IInnovationQuery` 已备好，UI 消费 0 | `Assets/UI` |
| 建筑 | `BuildingDef` 只有 id/名称/类别/等级/造价/材料/工期/维护；无“仓储种类/容量/升级/革新门槛” | `BuildingDef.cs` |
| 仓储 | `UpdateStorageCapacities` 只读 `buildingLevels[0]`，容量 = 10000×(1+等级×0.1) | `EconomyManager.cs:113` |
| 仓储概念 | 已有粮仓建筑（102）与 `DistrictEffects.foodStorage`，但没接到经济容量 | `BuildingSystem.cs:48`、`CityDistrictSystem` |
| DNA | 有完整基因组/等位基因/遗传/表型/杂交引擎；目前只服务**角色个体** | `Simulation.Genetics/*`、`CharacterManager` |
| 物种 | `Species.json` 3 个物种（人/瓦伊凡/地龙）；`RaceData.speciesId` 指向物种 | `RaceData.cs:78` |
| 种群基因 | `PopulationBlock` 只有 `raceId`，没有等位基因频率/基因分布 | `PopulationBlock.cs` |
| 生态 | `Species.json`（生态）已有承载/食性/体型，但与遗传物种表是两套 | `EcologySpeciesCatalog` |

---

## 1. 目标与总原则

1. **时间是可配置的物理量，不是散落的 365**：日长（自转）、年长（公转）、季节、tick/日全部进 `GameCalendar`，其他系统只读它。
2. **5000–7000 年是默认值，不是硬编码**：写在 `WorldSetupOptions`/`CalendarConfig`，可按物种、地图尺度、剧本覆盖。
3. **革新由“社会实践”驱动，而不是每天自动加点**：实践是主路径，研究是组织化加速，灵感是稀有事件。
4. **个人与政权双层**：个人有熟练度/实践/灵感；政权有实践池、研究项目、掌握度。法律/建筑/配方/宗教只认政权掌握度。
5. **DNA 从角色层扩展到种群层**：等位基因频率、选择、突变、基因流、物种分化；环境是选择压力。
6. **仓储设施化**：容量由设施与等级决定，设施升级消耗材料，材料走现有配方链。
7. **每一步先读代码、加测试、build 全绿、提交推送**；不改与目标无关的系统。

---

## 2. 阶段

### P8.0 历法与时间尺度（前置，所有后续按它验收）

**设计**

- 新增 `GameCalendar`（Core 或 Simulation.State）：
  - `hoursPerDay`（自转，默认 24；仅显示与昼夜/潮汐，不增加模拟 tick）
  - `daysPerYear`（公转，默认 365）
  - `seasons`（默认 4；季节边界按 `daysPerYear/seasons` 计算，不再写死 91）
  - `ticksPerDay`（默认 1；一天一个模拟 tick。除非战斗/天气需要，不引入亚日 tick）
  - `daysPerTick`（每个模拟 tick 推进多少游戏日，默认 1）
  - `absoluteDay` / `year` / `dayOfYear` / `hourOfDay` 统一换算
- 速度语义改为**游戏日/现实秒**：1/2/5/10/30 由 `daysPerSecond` 决定；实现上可以是 `ticksPerSecond × daysPerTick`，但 UI 仍显示 1x–30x。
- **史前快进**：无政权时允许 `daysPerTick>1`（如 30/90/365），调度器按“累计天数区间”判定 cadence，而不是 `day % interval == 0`，避免跳日漏掉月度/年度系统。
- 全工程替换裸 `365`：`OfficeSystem`、`LawEffectRuntime`、`WarRules`、`RegimeChangeDynamics`、`SettlementEvolutionSystem`、`StateFormationSystem`、`CharacterManager` 等 47 处改为 `calendar.DaysPerYear` 或 `calendar.AbsoluteDay`。
- 季节/气候/灾害/疾病/经济全部读 `daysPerTick` 与 `calendar`，不再假设 365。

**验收**

- 同一 seed、同一配置下，`daysPerYear=365` 与 `daysPerYear=360` 两套世界都能跑 100 年，无“第 365 天特判”遗漏。
- 调度器在 `daysPerTick=30/90` 下仍能每年触发年度系统、每季触发季节系统（用区间判定）。
- 1x 与 30x 下，同一段游戏时间的模拟结果在允许误差内一致（确定性测试只比较聚合量，不比较并发事件顺序）。
- 五档速度 UI 标签与 `daysPerSecond` 一致。

**不做**：亚日 tick、实时昼夜光照、行星轨道可视化。

### P8.1 城市与政权的“长周期”时间线

**设计**

- 世界开局不是“已有城市”，而是**聚落谱系**：营地/村落 → 村镇 → 城市。开局生成的村庄保留，但“城市”是运行时事件。
- 新增 `SettlementUrbanizationSystem`：
  - 城市门槛由 `calendar` 年数 + 人口 + 开发 + 粮食剩余 + 革新共同决定。
  - 默认：世界年龄 ≥5000 年才可能出现第一座“城市”（Level III/人口门槛按地图尺度配置）；政权形成再晚 500–1500 年，默认 ≥6000 年。
  - 这些默认值写入 `WorldSetupOptions`，可按物种/剧本覆盖；**不是历史定律**。
- `StateFormationSystem` 的年龄门槛从“聚落 200 年”改为“世界年龄 + 聚落年龄 + 革新条件”，并保留人口/开发/仓储条件。
- 城市出现必须写编年史与 UI 事件（“第一座城市”），便于验收。

**验收**

- 默认自然世界：5000 年前不出现城市；5000–7000 年窗口内出现第一座城市；政权在其后出现。
- 沙盒/剧本可覆盖为更短周期，测试用配置而非改常量。
- 220 年长跑不再作为 P8 的验收；改为“5000 年窗口”长跑（允许在测试中压缩 `daysPerTick`）。

### P8.2 革新车新机制（社会实践 / 灵感 / 掌握）

**设计**

1. **社会实践池（realm-level）**
   - `SocialPracticePool` 按 `innovationId`（或领域+具体条目）累计“实践经验”。
   - 来源与权重（可配置）：
     - 农业生产：产出 × 品质 × 规模（对数压缩）
     - 手工/建筑：建筑等级、配方产量、材料难度
     - 战争：战役规模、攻城、伤亡（经验来自组织与后勤）
     - 行政：法律推行、官职任命、税收、普查
     - 贸易：路线、商队、港口
     - 文化/信仰：学者、寺院、仪式、教育
   - 每条经验 = `baseWeight × scale^0.5 × qualityFactor × durationDays`；上限/衰减按领域设，避免大帝国自动全知。
2. **每条革新的实践需求**
   - `InnovationDef` 增加：`practiceRequirement`（默认 `researchCost × 0.5`）、`inspirationWeight`、`requiredRealmMastery`。
   - **最低 50% 规则**：政权实践池未达到 `practiceRequirement × 0.5` 时，不能正式立项研究；达到 50% 后可以研究，但剩余进度仍受实践/学者限制。
3. **个人层**
   - 角色有 `mastery 0–3`、`practice`、`scholarship/talent`。
   - 灵感/突破：基础概率很低（`0.0005/日` 量级），受实践相关性、学者属性、天才特质、机构（学宫/寺院/工坊）影响；上限 `0.03/日`。
   - 突破产生“研究计划”（个人发现 → 政权验证），不是直接解锁。
   - `Learn/SetMastery` 必须接线：从已完成革新、师承、机构学习中获得熟练度。
4. **政权掌握**
   - `RealmMastery` = 政权内角色熟练度的加权聚合（官员/学者/工匠）。
   - 法律、建筑、配方、宗教、装备的门槛从“已解锁”改为“已掌握（mastery ≥1）”；未掌握时显示“已知未掌握”。
5. **内容与接线**
   - 扩到覆盖矩阵：农业/畜牧/渔业/采矿/冶金/工艺/建筑/运输/军事/行政/法律/财政/文字/宗教/医学/天文/生态，每域至少 8–15 条，目标 300+ 条。
   - `unlockRefs` 全部接线：法律/法令、建筑、配方、装备、宗教、兵种、行政容量。
   - 50 条法律逐条补 `requiredInnovationIds`（当前仅 19 条）。
   - `MonthlyTickProgress` 要么替换为新的实践池结算，要么删除，不能留空转。
6. **UI**
   - 革新面板：当前研究、实践进度、可发现、已知未掌握、灵感事件；玩家可立项/换项/取消。
   - 读模型 `IInnovationQuery` 接线。

**验收**

- 无实践 → 不能研究；实践达 50% → 可立项；完成研究 → 政权掌握 → 法律/建筑/配方出现在可选列表。
- 个人熟练度可增长；167 条带前置革新不再对所有人恒不可发现。
- 天才角色灵感概率显著高于普通人；基础概率仍然低。
- 法律可选列表随革新/掌握变化（UI 与读模型测试）。

### P8.3 DNA、物种与自然选择

**设计**

1. **种群基因层**
   - 新增 `PopulationGenetics`：每地块/种族/物种的等位基因频率表（按 locus），与 `PopulationBlock` 关联（可用 `raceId + tileIndex` 键）。
   - 角色个体仍用 `GenomeInstance`；种群频率可由个体汇总，也可独立演化。
2. **选择与突变**
   - 每代（按物种 `lifespanYears`/`generationYears`）计算环境适应度：温度、湿度、营养、疾病、劳动/战争压力。
   - 适应度差 → 等位基因频率偏移；突变率按 locus；基因流来自迁移/征服/通婚。
   - 表型影响：体型、寿命、繁殖率、抗病、耐寒/耐热、劳动力、军事。
3. **物种分化**
   - 地理隔离 + 遗传距离 + 生殖隔离阈值 → 新 `SpeciesDef`/`RaceData`（运行时生成，入档）。
   - 杂交：`GeneticCompatibility`/`HybridizationResult` 接线；可育/不育按隔离等级。
4. **与生态/人口耦合**
   - 承载力和人口增长读表型（体型/食性/耐候），不只读 `RaceData` 常数。
   - 灭绝/迁徙/疾病作为选择压力。
5. **内容**
   - 扩充 `Genomes.json`（人/瓦伊凡/地龙已具雏形），补皮肤/角/翼/体型/代谢/抗性 locus；`DnaDefs.json` 补天赋/缺陷与环境响应。

**验收**

- 同一物种在寒冷/干旱环境 1000+ 年后，等位基因频率显著偏移（可复现、可解释）。
- 隔离种群达到阈值后产生新物种；杂交可育性符合隔离等级。
- 物种表型影响人口/承载/疾病，不只做展示。
- 存档往返：种群频率、物种、角色基因组不丢。

### P8.4 仓储设施与升级（P7.6 合并）

**设计**

- `BuildingDef` 扩展：`storageKind`（Food/Salt/Treasury/Goods）、`capacityPerTier`、`requiredInnovation`、`requiredMastery`、`upgradeFrom`、`upgradeMaterials`、`upgradeDays`。
- `EconomyManager.UpdateStorageCapacities` 改为读取地区内设施（按种类）汇总，不再 `buildingLevels[0]`。
- 新建/升级走 `ConstructionPlanSystem`（当前空转）→ 消耗材料/工时；材料走 `GoodsRecipe`，配方校验 `requiredBuilding` 与革新。
- 粮仓/盐仓/金库/货栈分级；无设施只有小型临时堆场。
- 贸易/征税/饥荒判定都读实际容量。

**验收**

- 新地区默认只有小货栈；建粮仓后粮食容量上升；升级消耗真实材料且可失败/可回退。
- 无粮仓地区不能无限囤粮；贸易目的地容量按设施计算。

### P8.5 观测、UI 与长跑验收

- 新增“历史年表”视图：城市出现、政权形成、重大革新、物种分化。
- 长跑：默认世界 7000 年（测试压缩 tick），检查城市窗口、政权、革新、物种四项。
- 性能：`daysPerTick>1`、30x/快进下的事件队列/调度预算不爆；预加载计划覆盖新内容表。

---

## 3. 顺序与依赖

1. **P8.0 历法**（前置；P8.1/P8.2/P8.3 都读它）。
2. **P8.1 城市时间线**（依赖 P8.0；可先用配置验证窗口）。
3. **P8.2 革新车**（依赖 P8.0 的年度/实践结算；与 P8.1 并行做数据与机制）。
4. **P8.3 DNA/物种**（可与 P8.2 并行读资料；运行时依赖人口/生态接口）。
5. **P8.4 仓储设施**（依赖 P8.2 的革新掌握与配方校验）。
6. **P8.5 观测/长跑**（最后）。

并行约束：P8.2 与 P8.3 改不同文件域，可并行；P8.4 等 P8.2 的 `requiredMastery` 接口；P8.1 的窗口测试依赖 P8.0。

---

## 4. 非目标

- 不做亚日 tick、实时轨道力学、完整流体气候。
- 不把 5000–7000 年写成历史定律；它是人类基准的默认值，可按物种/剧本覆盖。
- 不在 P8 重做整个法律内容；只补革新门槛与掌握判定。
- 不在 P8.3 做逐个体全基因组模拟（种群频率 + 角色个体两层即可）。

---

## 5. 风险与待查

1. **5000–7000 年的依据**：需查考古/人类学资料（最早
城市、农业起源、定居）确认量级；用户已说明不确定，先作为可调默认值。
2. **性能**：5000 年 × 365 天 ≈ 182.5 万日；必须用 `daysPerTick` 批处理与年度/十年聚合，否则无法在可接受时间跑完。
3. **调度 cadence**：当前 `day % 30 == 0` 在跳日时会漏；P8.0 必须改为区间判定。
4. **革新数量**：300+ 条是新内容量，需要覆盖矩阵与资料检索；不能只加 id 不加消费者。
5. **DNA 与现有 RaceData**：物种/种族/生态物种三套表要合并口径，避免“同一个物种三种写法”。
6. **存档**：种群频率、物种、实践池、掌握度、日历都要入档；当前 Plan/AI/调度器不入档的欠账一并处理。
