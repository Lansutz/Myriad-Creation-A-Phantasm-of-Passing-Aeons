# P10 全项目审计与下一阶段规划

> 2026-11-14。审计口径：静态调用链 + grep + 构建/测试基线（`_fc_verify/build-done.txt`、`test-editmode-last.xml`）+ 内容表实际计数。结论均给文件/证据；不凭印象。
>
> 基线：941 个 `.cs` / 97,537 行；19 个 asmdef；44 张基础内容 JSON；EditMode 1073 例、1063 通过、0 失败、10 个 Explicit 跳过；`EXIT=0`、零 `error CS`、28 条 `warning CS`（全部是测试里读已 `[Obsolete]` 的文化七板块字段）。

---

## 1. 审计结论（一句话）

**P9 把“内容/门槛/水体/改道”补齐了，但接上 M3 的“实践 50% 才可立项”之后，整个科技—军事—法律—宗教链出现了一个全局死锁：只有农业和采矿有实践经验来源，其余 24 个领域永远攒不到 50%，于是单位、法律、宗教、建筑、配方依次不可达。** 这不是平衡问题，是链路问题；必须先修它，再谈 5000 年校准。

---

## 2. P0：会阻断整局演化的断链

### 2.1 研究—实践死锁（最高优先）

- **实践来源只有 3 处**：`EconomyManager.cs:202`（采矿）、`EconomyManager.cs:213`（农业）、`SettlementProduction.cs:71`（配方，但其领域取决于配方要求的革新）。其余 23 个领域没有任何 `AddPractice` 调用。
- **立项门槛**：`InnovationTree.StartResearch`（`InnovationTree.cs:176`）在 `world != null` 时要求 `GetPractice(field) ≥ required×0.5`。
- **后果链**：
  - 所有 14 个兵种的门槛都落在 Craft / Metallurgy / Machinery / MilitaryInstitution / Transport / Economic（`Units.json` 实测），没有一个落在 Agriculture；这些领域实践为 0 → **永远无法解锁任何兵种**，`RecruitmentSystem` 只能空转。
  - 法律：除 7 条开国知识外，其余法律需要 Law/Governance/Bureaucracy/Economic 革新 → 不可达。
  - 宗教：根信仰要求 `祭祀神学`（Theology），教派要求 `正统教义`；Theology 实践为 0 → 任何宗教都不可用。
  - 建筑：木工坊（Construction）、纺织坊（Craft）、银行（Economic）、要塞（MilitaryInstitution）等全部不可达。
  - 配方：基础加工也被革新门槛锁住，Craft 实践无法启动（鸡生蛋）。
- **AI 侧放大**：`AIController.cs:233` 取 `GetAvailableInnovations`，`:258` 直接选 `[0]`；`GetAvailableInnovations`（`InnovationTree.cs:618-629`）只查前置、不查实践，`StartResearch` 失败后 AI 每天重试同一项，形成永久卡死。

**修法（不改原则）**：给每个 `InnovationField` 至少一条由真实活动产生的实践源；`GetAvailableInnovations`/AI 选题按“实践是否达 50%”过滤；新增“可达性审计”测试。

### 2.2 存档遗漏（P9 新状态全部未入档）

- `InnovationTreeSaveData` 无政权实践池 → 读档后实践归零，研究立刻回到死锁。
- `SettlementData.environmentalPressureDays / lastDeclineCause`、`GameWorld.CityFoundedCount / CityCollapsedCount / RiverAvulsionCount`、`Caravan.transportMode / capacity`、`TradeRoute.transportMode` 均无 DTO（`grep SaveRuntime` 零命中）。
- 现有保存往返测试只覆盖旧字段，所以全绿但漏档。

**修法**：补 DTO 与往返测试；把“新增运行时字段必须有存档策略”写进验收。

### 2.3 宗教链是“数据已接、运行时未接”

- `ReligionCatalog.IsAvailable`（`ReligionCatalog.cs:151`）**零消费者**；M2 给 44 条宗教补的 `requiredInnovations` 目前不产生任何行为。
- `SetStateReligion`（`FaithContracts.cs:280`）**除测试外零调用**；新政权 `stateReligionId=-1`（`StateFormationHost.cs:44`），没有任何运行时路径改宗或建国教。
- `Doctrines.json` 46 条：`parameters` 0 条、`bindsTo` 0 条（`DoctrineOptionDef` 有字段但内容为空）→ 教义没有任何机制效果。
- 结果：信仰、教派、圣战、封圣、宗教合法性这条大链在真实世界里仍不启动。

**修法**：先裁决“宗教门槛由谁消费”（建国教/改宗/传教三选一），再接 `IsAvailable` 与 `SetStateReligion` 的生产者；教义至少先落 1–2 个真实绑定点（婚姻/性别/继承），否则明确标注“仅展示”。

---

## 3. P1：会让长周期失真的设计问题

### 3.1 城市化只看人口 → 所有聚落都会变城市

- `SettlementEvolutionSystem.TryUrbanize`（`:47-57`）只要求人口 ≥1200、开发 ≥20、有市场、有 180 天口粮。
- 500 年诊断（128×64）：建城 263 座；48×24 的 7000 年观测：30 座聚落全部成城。原因是人口以约 1%/年增长，承载力上限 3000/地块，任何聚落最终都会越过 1200。
- **机制缺口**：没有“非农人口/分工”条件。`SocialDifferentiation` 已算 Merchant/Clergy/Slave 比例，可直接作为城市门槛（例如非农 ≥12–15%），这比再加一个抽象数值更符合“分工产生城市”。

### 3.2 批处理语义不一致

- `PopulationTick` 用 `growth × daysPerTick` 线性放大（`GameWorld.Simulation.cs:130`）；经济按 `days` 线性缩放产出/消费；货币 `DailyTick()` 不做天数缩放；贸易发车/商队移动仍按 tick。
- 在 90–180 天/tick 的长跑里，人口是“一步走 90 天”的线性近似，误差随步长放大；城市爆炸与此有关。
- **修法**：为批处理定义统一口径（复利或子步），并给每个系统标注“是否按天缩放”。

### 3.3 经济日结在默认地图上过重

- 128×64（8192 地块）7000 年观测在 90 天/tick 下超过 13 分钟仍未跑完；`EconomyManager.DailyTick` 每 tick 全表扫资源、供需、库存、腐损（`EconomyManager.cs:135-160`）。
- 30x 实时（1 tick/秒）下这是掉帧级开销；调度器的 4–12ms 预算被单个系统击穿。
- **修法**：按地区 dirty 结算、把 30 天库存重算真正按天摊销、把腐损改为按天复利的一次幂运算（已做）后再做区域分片；为经济增加“每 tick 处理地区数上限”。

### 3.4 新增字段有生产无消费

- `InnovationDef.insightWeight`、`requiredRealmMastery`：0 消费者（只有定义）。
- `InnovationKnowledgeSystem.GetRealmMastery`：只有测试调用。
- `CityCollapsedCount`：只写不读（无 UI/读模型）。
- **修法**：接 UI/研究面板，或删除字段；不保留“看起来有设计”的死字段。

### 3.5 缺少“内容可达性”审计

现有测试只验证门槛 id 能解析、法律/建筑有门槛；没有验证“从开局知识 + 实践来源出发，N 年内哪些内容可达”。2.1 的死锁正是这类测试能提前抓到的。
**修法**：新增 `ContentReachabilityTests`：模拟 0/50/200 年的实践积累，断言每个领域的根节点可立项、每个兵种至少一条可达路径、每条法律有可达路径（或显式标为“剧本专用”）。

---

## 4. P2：死代码、内容与工程卫生

| # | 项 | 证据 | 处置 |
|---|---|---|---|
| 1 | 双套法律革新门槛 | `LawEffects.requiredInnovations`（string）只被 `LawSystem` 读；`LawSystem` 全库无外部调用 | 删除 `LawSystem` 或迁移到 int 门槛；保留单一真源 |
| 2 | 种族层为空 | `RaceDefs.json` `races: []` | 要么补最小种族表，要么明确 DNA 仅个体层 |
| 3 | 文化传统无因子 | `Traditions.json` 13/13 `factors` 为空 | 内容补齐或标注“仅命名/展示” |
| 4 | 教义无参数/绑定 | `Doctrines.json` 46/46 无 `parameters`/`bindsTo` | 与 2.3 合并处理 |
| 5 | 本地化缺口 | `de/es/fr/ja/ko` 各 0 条，`en/zh-Hans/zh-Hant` 各 137 条 | 至少把新面板文案入表；缺语言显示回退而不是空 |
| 6 | 面板注册不完整 | `UiPanelRegistry` 只注册 4 个面板（`UIManager.cs:359-382`），其余仍由字段持有 | 按“一个 UI 一个文件 + 注册”原则收口 |
| 7 | 存档层引用过宽 | `Save.Runtime` 引用 `Audio`；`SaveRuntime/SettlementPersistence.cs` 仍在存档层做映射 | 引用收窄；映射器迁回领域层 |
| 8 | 上帝类 | `GameWorld.Simulation.cs` 80KB、`GameWorld.Core.cs` 56KB；`RealmData` 53 个 public 字段 | 按域拆 partial 文件；先私有化 treasury/stability/population |
| 9 | 测试警告 | 28 条 `CS0618` 全在 `CultureTransmissionTests` 读已废弃字段 | 改用聚合方法，清零警告 |
| 10 | 运行日志噪声 | 每月/每年重复 Debug.Log | 长跑关闭日志已做；正常游玩保留关键条目即可 |

---

## 5. 下一阶段计划（P10.1–P10.6）

### P10.1 实践覆盖与研究解冻（P0，先做）

- 为 26 个领域各建至少一条真实实践源：
  - 农业/采矿/手工：已有，补“建造/工具使用”给 Craft/Construction。
  - 法律/治理/行政：每次法律推行、税收结算、官职任命。
  - 军事思想/制度：每场战斗、征募、行军。
  - 经济/运输：每次商队成交、港口装卸。
  - 神学/宗教制度：每次祭祀/国教确立/教派争端。
  - 教育/文字/数学：书吏、学校、档案。
  - 医学：疫病处置、医院。
  - 仪式/节庆/习俗/传承：仪式与编年史记事。
- `GetAvailableInnovations` 按实践过滤；AI 选题只从可立项项里选。
- **验收**：0 年时每个领域至少一个根节点可立项；200 年模拟中每个领域至少完成 1 项；AI 不再重复提交不可立项项。

### P10.2 存档闭合（P0）

- 补实践池、环境压力/死因、城市计数、改道计数、商队运输方式与容量的 DTO 与往返测试。
- **验收**：新增字段逐一“写入→保存→读档→相等”；`SaveVersion` 递增与迁移说明。

### P10.3 宗教链裁决与接线（P0/P1）

- 裁决国教写入点（建国/改宗/传教三选一，按 D29 与 D33 的授权口径）。
- `ReligionCatalog.IsAvailable` 接入该路径；教义至少落一个真实绑定（婚姻/性别/继承）。
- **验收**：新政权在满足条件后能确立国教；不满足时 UI/命令给出原因；教义绑定改变一个可观察量。

### P10.4 城市化与长周期校准（P1）

- 城市门槛加入“非农人口比例/行政功能”，不再只看人口。
- 统一批处理口径（人口复利/子步；货币与贸易按天缩放或明确标注不缩放）。
- **验收**：500 年模拟的城市数不再等于聚落数；7000 年观测给出城市建立/死亡曲线，且不出现“全图皆城”。

### P10.5 经济性能（P1）

- 地区 dirty + 分片结算；库存重算按天摊销；30x 下单 tick 经济耗时回到预算内。
- **验收**：8192 地块、30x 语义下经济单 tick ≤ 预算；7000 年观测耗时显著下降（目标 < 5 分钟）。

### P10.6 工程卫生（P2）

- 删死代码（`LawSystem`/string 门槛）、补内容或标注、清警告、面板注册收口、存档层引用收窄、`GameWorld` 按域拆文件。
- **验收**：零 `warning CS`；`UiPanelRegistry` 覆盖所有面板；`Save.Runtime` 引用不含 Audio/领域实现。

---

## 6. 与总原则的对应

| 原则 | 本计划如何遵守 |
|---|---|
| 机制优先于数值 | 实践来自真实活动；城市化用非农分工；不新增“+X%”驱动 |
| 单一真源 | 法律门槛只保留 int 一套；实践池一个；存档一个 |
| 数据驱动 | 内容表改动不改 `.cs`；新门槛必须有可达性审计 |
| 过程优先 | 研究仍走立项/实践/完成，不瞬时解锁 |
| 确定性 | 改道/灾害继续由 seed 派生；批处理口径统一 |
| 性能是约束 | 经济分片；每系统声明 O(N)/O(dirty) |
| 可断言 | 每个 P10 项都有 EditMode 测试或长跑断言 |

## 7. 本轮不做

- 不新开平行系统；不复制参考项目代码。
- 不为“内容全”硬填；不可达的内容要么补路径，要么显式标注。
- 不在没有消费者时新增字段；新增字段必须同时给存档与读模型。

## 8. P10.1–P10.2 实施记录（2026-11-14）

- `settlement-practice.daily`：按阶层构成累计实践，26 领域全覆盖；军队/建筑另计。
- `GetResearchableInnovations` + AI 改用：只选实践达标的项。
- 存档：实践池、环境压力/死因、城市建立/死亡/改道计数入档；`SaveSystemRoundTripTests` 逐字段断言。
- 测试：`PracticeCoverageTests` 2 例；全量 `EXIT=0`、1075 例、1065 通过。
- 下一步：P10.3 宗教链裁决与接线。

## 9. P10.3 实施记录（2026-11-14）

- 内容：`isFolkDefault` + 601 民间信仰（faith 层，无门槛）；600 原始崇拜同步无门槛；生成期播种。
- 运行时：`TryAdoptMajorityFaithForRealm`（≥40% 多数 + 门槛）挂 `faith.adoption.daily`；`SetStateReligion` 校验 `ReligionCatalog.IsAvailable`。
- 教义绑定沿用 `DoctrineBindings`（已有 option→binding 映射）。
- 测试：`ReligionAdoptionTests` 3 例；全量 `EXIT=0`、1078 例、1068 通过。
- 下一步：P10.4 城市化非农条件与批处理口径。

## 10. P10.4 实施记录（2026-11-14）

- 城市化：市场 + 非农 ≥12% + 中心地职能（首都/actual port/贸易 ≥60/行政商业宗教文化工业）。
- 生成期初始职业构成（省治/港口 18%，村落 5%），走 `PopulationLedger.SplitToNewBlock`。
- 人口增长改复利 `(1+growth)^days`。
- 观测：500 年 128×64 城市 71/420；7000 年 48×24 城市 4/55、建立 4、死亡 0、firstCity=109。
- 测试：`SettlementUrbanizationTests` 5 例；全量 `EXIT=0`、1079 例、1069 通过。
- 下一步：P10.5 经济性能。

## 11. P10.5 实施记录（2026-11-14）

- 消费按地区聚合，避免每地块重复扫货种；缺粮才逐地块惩罚。
- 单产缓存：首次全表、之后每 tick 1/30 滚动刷新。
- 实测 8192 地块日结 3.24ms（旧约 28ms）；7000 年观测 149s → 85s。
- `EconomyTickPerformanceTests` + `EconomyConservationTests` 18/18。
- 下一步：P10.6 工程卫生。

## 12. P10.6 实施记录（2026-11-14）

- 删 `LawRuntime/LawSystem.cs`（零引用）与 `RuntimeMapEditingController` 的 `world` 字段 / obsolete `BindWorld`；控制器只依赖 `IMapWorldView` + `IMapEditCommandService`。
- `GameManager` 注入点兜底创建 `WorldEditService`，覆盖快速读档早于 `Awake` 的路径；`WorldRebindOnLoadTests` 改为"服务已注入 + 编辑落到新世界"的行为断言。
- warning 152 → 0：过时 `FindObjectsByType` 重载、迁移期旧字段 `#pragma` 标注、`UIManager.Panels` 未读字段、重复 using 扫描清零。
- `Save.Runtime` 移除 `MyriadCreation.Audio`；World/Simulation/Faith/Culture 的 DTO 映射仍是序列化边界欠账（A2/A3），不冒充已完成。
- 面板注册：补注册 `GovernmentPanel`；聚落详情/存读档/设置/主菜单按"选择驱动/流程驱动"口径留在注册表之外（签名不兼容，拒绝假适配）。
- 验证：`EXIT=0`、1079 例、1069 通过、0 失败、10 Explicit、零 warning CS。

### 12.1 仍未达成的 P10 验收项（不掩盖）

- **200 年每领域至少 1 项**：尚未做专项诊断；需长跑统计 `InnovationTree` 各领域完成数，若 AI 偏科则在选题侧加缺口优先，而不是放宽门槛。
- **面板全注册**：见上文口径，`IUserInterfacePanel` 覆盖运行时可开关面板；其余为流程/选择面板，未接入。
- **Save.Runtime 不引用领域实现**：仅移除 Audio；领域 DTO 映射仍在，属 A2/A3 大工程。
