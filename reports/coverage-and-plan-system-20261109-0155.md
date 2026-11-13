# 覆盖度更正 + 计划系统（S15）

> 2026-11-09 01:55（Asia/Shanghai）。触发：用户质问"你有看所有的系统吗？我们这里还有计划系统。"

---

## 1. 先答问题：没有，我没看完

| 读法 | 范围 |
|---|---|
| **我亲自读/grep** | 主循环与调度（`GameWorld.Simulation.cs`、`SimulationScheduler`、各 `*Schedule`）、法律契约层、经济日结、政治日结、`RaceData`/`DnaSystem`、`SocialClassAvailability`/`ClassEmergenceEvents`、`Plan`/`PlanSystem`/`PlanType`、`ContentSource`、`Chronicle`、存档关键段 |
| **八域子审计** | A 世界 23 系统 / B 聚落人口 ~30 / C 角色遗传 ~30 / D 政体法律 ~27 / E 文化信仰 ~18 / F 经济 ~12 / G 军事外交 ~28 / H 革新与主干 |
| **没系统覆盖** | `Content.Definitions`（**131 文件**）、`Simulation.Actors`（17）、`Content.Modding`（45）、`Simulation.Definitions`（12）、`Events`（2）、`App`（16）与 `UI`（28）的接线细节、`Tests`（144）的逐条语义 |

**计划系统确实漏了**——它在 H 域报告里只以"`plans.daily` 只跑 Research 分支"的形式出现一行，我没有把它当系统读。现在读了。

---

## 2. 计划系统是什么（Lead 亲读，`Simulation.Planning` 10 个文件）

- **不是"研究计划"的子模块，而是通用"项目/活动"引擎**：
  - `PlanType` **10 类**：Research / Intrigue / Engineering / Construction / Exploration / Trade / Diplomacy / Migration / Military / Custom（`PlanType.cs`）。
  - `Plan` 字段反映真实历史过程：发起者 `initiatorId`、所有者 `ownerId`、目标 `targetId/targetKind`、目的 `purpose`、标题/描述、`progress / elapsedDays / estimatedDays`、**参与者 `participants`**、**需求 `requirements`（key+amount）**、当前活动 `activity(id/definitionId/stateCode)`、**等待条件 `waitCondition`（`SimulationWaitCondition`）**、`isWaiting`、结果码/摘要。
  - **状态机**：Proposed → Accepted → Preparing → Executing →（Paused）→ Completed / Failed / Cancelled，转移表逐条校验（`PlanSystem.cs:375-412`）。
  - **事件**：`PlanCreated / PlanStateChanged / PlanCompleted / PlanFailed / PlanCancelled`。
  - **调度**：`plans.daily`（order 30）每天推进 active 计划，按 `PlanType` 找执行器（`PlanSystem.cs:237-304`）。
- **现状**：
  - 执行器只有 **2 个**：`ResearchPlanExecutor`、`ConstructionPlanExecutor`；
  - 创建者只有 **1 个在跑**：`ResearchPlanSystem`（`:208`）；`ConstructionPlanSystem.CreateConstructionPlan` **零调用者**；
  - 其余 **8 类既无执行器也无创建者**；
  - 计划**不入档、无 DTO**。
- **判定：半跑**。框架完整，研究类在转，建设类只差一个调用者，另外八类从零。

---

## 3. 为什么这条我该早点看到

它是**唯一"过程化"的通用载体**，恰好是另外几条原则的落地方式：

| 宪章原则 | 计划系统怎么承载它 |
|---|---|
| 反数值驱动（5） | 效果由"人+物资+时间+条件"产生，不是一次加法 |
| 渐进分层（3） | 新事物需要条件成熟；条件不满足就 `Wait` / `Fail` |
| 布罗代尔（2） | 不同计划的 `estimatedDays` 天然给出不同时间尺度 |
| 过程优先（11，新增） | 历史 = 计划与被中断的计划 |
| 玩家动词 | 玩家的"造城/开垦/远航/联姻/密谋/围城/变法"本来就该是下达计划，而不是点一下生效 |

所以它应该被当作 **S15 计划与执行**，作为 M3–M8 的**承载方式**，而不是某个系统的附属。

---

## 4. 已落盘

| 文档 | 改动 |
|---|---|
| `DESIGN_CHARTER.md` | 新增**原则 11「过程优先」**（含 10 类计划、状态机、现状只有 2 执行器/1 创建者）；§2 改为"十一条原则" |
| `SYSTEM_MASTER_PLAN.md` | 新增 **§0.2 覆盖度声明**（明确"没看完"，列出未覆盖清单）；§3.0 新增 **S15**；新增 **S3.5 计划系统块**（逐条锚点）；§5 顶部加"承载方式"说明；§7 门禁新增**原则 11**；M10 DoD 增加"计划面板能显示 ≥3 类计划并可取消" |
| `PROJECT.md` | 状态更新 |

---

## 5. 正在补的覆盖（4 个只读审计已启动）

| 审计 | 范围 | 要回答的问题 |
|---|---|---|
| I 内容与模组 | `Content.Definitions`(131) + `Modding`(45) + `StreamingAssets` 结构 | 内容域清单、加载链、校验、未被使用的内容、硬编码清单、**"加一个新种族/建筑/法律/革新要改哪些 .cs"** |
| J 单位与行为体 | `Units`(30) + `Actors`(17) | MapActor 12 种 AI 的职责、运输/货物/传令链、单位定义来源、与 Army 的关系、三类停摆点 |
| K 表现层 | `App`(16) + `UI`(28) + `Rendering`(12) + `Editor`(3) | 命令表（按钮→契约→后端）、契约缺口、场景与代码双源、启动链、不可达清单 |
| L 状态模型 | `State`(71) + `Definitions`(12) + `Events`(2) + `Settlement`(9) | 状态容器清单、`public` 字段跨域写入、不变量缺失、定义/状态边界、事件订阅、DTO 覆盖缺口 |

四份完成后，总纲 §3 会再补四块，§0.2 的"未覆盖"清单相应收缩。

---

## 6. 教训

1. **八份审计覆盖"域"，不等于覆盖"系统"**：计划系统横切九个域，落在任何一份域审计里都只会被写成一行，所以它被系统性忽略了。**横切系统必须单独立项审**。
2. **"只跑通一条分支"最容易骗过抽样**：`plans.daily` 每天在跑、日志正常、测试也有（`ArchitectureContractsTests` 测的是状态机），但它 10 条腿只有 1 条着地。
3. 覆盖度要**先声明再结论**：这份总纲以后每版都带 §0.2，读者据此知道"哪里可能有盲区"。
