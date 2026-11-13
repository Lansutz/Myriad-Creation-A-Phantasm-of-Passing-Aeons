# 覆盖审计收尾报告（I / J / K / L 全部完成）

> 2026-11-09 02:25（Asia/Shanghai）。至此 A–L **12 份**域/横切审计全部落盘。

---

## 1. 覆盖结果

| 审计 | 文件 | 行数 | 关键补出 |
|---|---|---|---|
| K 表现与入口 | `audit/K-presentation.md` | 161 | 12 种显示模式无切换入口（恒为 Religion）；地图点选空实现（`HandleMouseClick`）；9 处序列化断链；存档面板无存档按钮（只有 F5）；F9 读档软锁；地图编辑双入口皆空 |
| I 内容与模组 | `audit/I-content-modding.md` | 178 | **18 张内容表在 C#**；**Mods 样例永不加载**（`Mods/Dna/DnaDefs.json` 被当包目录）；**14 个包域零 schema 校验**；`id=0` 一条坏数据 → 整份 JSON 剩余条目静默丢弃；革新 1/1025 被代码无条件覆盖 |
| J 单位与行为体 | `audit/J-units-actors.md` | 143 | **三套实体并行**（Army 无 tick / MapActor 在跑 / Caravan 才是真物流）；传令官链唯一贯通但只由玩家触发，且 `TryFund` 早于 `FindCourierLeader` ⇒ 无领队时**钱粮白扣不回滚**；**行为体对客户端不可见**；开局四类行为体共用 `NomadAI`；营寨约 12 天必自灭 |
| L 状态模型 | `audit/L-state-models.md` | 94 | `RealmData` **37 字段全 public 无守卫**；`treasury` **14 处直接写**跨 8 程序集；**金库双源**（`TreasuryLedger:334` vault 覆盖 treasury）；`stability` 三套夹取；**事件 9 类型/11 发布/仅 3 订阅**，统计记录器全库无实例化；存档**静默丢 20+ 字段**（含 `primaryCultureId` ⇒ 读档后继承法/头衔断链）；`SwarmAI.cs:62` 量纲 bug（0–1 的 fertility 减 2f） |

## 2. 路径口径更正（L 域指出，必须记住）

`TileData` / `SettlementData` / `AnchorData` / `SimulationEventBus` / `Plan` / `PlanSystem` **都不在** `Simulation.State / Definitions / Events` 三个目录，实际位于：

- `Core/MyriadCreation.Core.Data`（TileData 等）
- `World/MyriadCreation.World.Settlement`、`World/MyriadCreation.World.Anchor`
- `Core/MyriadCreation.Core.Events`（SimulationEventBus）
- `Simulation/MyriadCreation.Simulation.Planning` 与 `Simulation.Systems`（Plan/PlanSystem）

⇒ 以后任何"按目录审状态"的做法都会漏，必须按**类型**找。

## 3. 这四份审计改了总纲的哪些结论

| 原判断 | 修正后 |
|---|---|
| "玩家有 6 个真动词" | 收紧：立法/法令 4 命令 + 聚落详情 + 新档/读档/调速 + 设置；地图点选、模式切换、存档按钮、角色/族谱、地图编辑**全部不可达** |
| "可模组化是既定方向" | **只在革新一个域成立**；18 张表在代码里；Mods 通路本身有洞 |
| "军队系统空转" | 更准确：**三套实体并行**，Army 完全空转、MapActor 在跑但玩家看不见、Caravan 才是真物流 |
| "存档覆盖大部分状态" | 实际**静默丢 20+ 字段**，其中 `primaryCultureId` 丢失会让读档后继承法与头衔判定断链 |
| "系统耦合、架构解耦" | 程序集层做到，**内容层与状态层没做到**：18 张硬编码表、6 组双真源、`RealmData` 无守卫 |

## 4. 对里程碑的连带影响

1. **M1（地基）扩项**：除 seed RNG、`PlayerRealmId`/`Chronicle` 入档外，必须补 **`primaryCultureId`/`heirId`/`realmForm` 等 20+ 丢失字段**；否则 M8 的继承与头衔在"读档后"永远断链。
2. **M2（世界与时间）前置**：先修"地图点选 + 模式切换"两条基础交互，否则一切观测面无入口。
3. **M3（生产-人口-聚落）**：`fertility` 量纲 bug（`SwarmAI.cs:62`）要先修，否则土地会被一次虫群永久归零。
4. **M7（战争外交）**：先合三套实体，再谈征召与战斗。
5. **M10（表现）**：补的不只是面板，还要补"行为体可见"（渲染层对 actor 零命中）。

## 5. 仍未覆盖（诚实标注）

- `Tests` 144 个文件的逐条语义（只统计了数量与部分文件）；
- `App`(16) 与 `UI`(28) 的逐行实现（K 只到"面板/命令/断链"级）；
- `Content.Definitions` 131 个文件的逐域细节（I 给了清单与三态，未逐字段）；
- `Simulation.Modding` 45 个文件（I 按加载链读）。

## 6. 产物

| 路径 | 说明 |
|---|---|
| `docs/plan-v3/audit/{I,J,K,L}-*.md` | 四份新审计（共 576 行） |
| `docs/plan-v3/SYSTEM_MASTER_PLAN.md` | §0.2 覆盖度重写、§0 第 10 条、§3 新增 I/J/K/L 四块、§4.4 新增 6 条耦合点（8–13） |
| `DECISIONS.md` | D23 扩到 13 条内容外提项 + 模组可行性结论 |
| 本报告 | 覆盖收尾 |
