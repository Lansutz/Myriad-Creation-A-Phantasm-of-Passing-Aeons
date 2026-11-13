# 头衔系统审计整合（S16）

> 2026-11-09 02:40（Asia/Shanghai）。专项审计：`docs/plan-v3/audit/M-title-system.md`（113 行）。

---

## 1. 一句话

**头衔是"有名无实的一行记录"**：模型、实例、继位转交都在跑，但设计的五条（kind 派生 / 位阶组合序 / 共持与角色 / 资格席位任期 / 称谓层）只落地了一条（单一持有），而它连存档都记在已废弃的存档器里。

## 2. 八条硬事实（逐条带锚点）

| # | 事实 | 后果 | 证据 |
|---|---|---|---|
| 1 | **三模式是假分支**：`definitionId` 恒绑 `TitleCatalog.Highest(Landed)`，而 `Highest` 恒为 `title_basileus`(3.1) | `successionMode` 恒为 `RealmLaw`；`None` 分支不可达；`OwnLaw` 与 `RealmLaw` 走同一 else；`ownLawPreset` 零消费者（`InheritanceLawCatalog` 仅测试引用） | `GameWorld.Succession.cs:62-77` |
| 2 | **所有君主称号退化为"王"** | 编年史即位条目一律"某某即位为王"；`RealmTitleEvolutionTests:66-68` 通过属巧合 | `RealmTitleEvolution.TitleWord:48-59` 无 basileus 分支 |
| 3 | **头衔不入活档** | 读档后头衔全空 | 序列化只在 `GameSaveSystem.cs:292-311/636-646`（整类 `[Obsolete]`）；活路径 `SaveData.cs` 无字段 |
| 4 | **读档残留上一局持有者** | 新局/读档不干净 | `TitleInstanceRegistry.Clear()` 唯一生产调用也在死路径 |
| 5 | **开局 0 头衔、0 官职** | 玩家开局看不到任何头衔体系 | 头衔只在 `NotifyRulerTransition` 写；官职 6 席补缺唯一触发点是**战争结束**（`GameWorld.Simulation.cs:880`） |
| 6 | **官职三套并行** | 同一职位有三份事实，互相不校验 | `officeHolders`（真源，入活档）/ `office_*` 影子 `TitleInstance`（无 definitionId）/ `AdminDivision.holderCharacterId` |
| 7 | **29/36 模板字段无生产者** | 头衔树、资格、席位、任期全空 | `TitleManager.cs:32-41` 只映射 7；`tier`/`superiorTitle`/`eligibility`/`seats` 在 `Titles.json` 不存在；8 个查询 API 生产 0 调用 |
| 8 | **称谓层只有壳** | 只有谥号在继位盖棺时真写 | `appellationId` 无生产者、`ResolveAppellation` 零调用、`EpithetCatalog` 81 条零引用；`CharacterData.ownedTitles` 入档但 0 写 0 读 |

补充：UI 无头衔面板、无授予（`ActionIds.AppointTitle` 零使用者）；"官职体系"段因 `UIManager.Panels.cs:679` 不传参永不渲染；区划树把 `titleId` 原串（`title_zhuhou`）直接打印给玩家。

## 3. 与权威裁决的对账（`DECISIONS_M1_M2_M3` §1）

| 裁决 | 代码状态 |
|---|---|
| 头衔 = 权利束 + 绑定；官职 = 带 office 模块的头衔 | 概念在文档里，代码里 `rights`/`bindings`/`modules` 无运行时表示 |
| `kind` 派生视图、不落库 | 代码里既无派生器，也无 `title_kind` 字段——**两项都没有** |
| `TitleHolding` 支持 role（sole/senior/co-ruler/elector/regent）+ share | 只写 `Sole`，`share` 恒 1 |
| `appointment_methods` / `eligibility` / `inheritable` / `seats` / `term_years` | 字段不在 JSON、无加载、无消费者 |
| 位阶 = 组合序 + 绑定（§11） | 仍是单值 `rank`，且 `Highest()` 让所有政权绑同一个头衔 |

## 4. 对计划的改动

- 总纲 §3.0 新增 **S16 头衔与官职**；§3 新增 S16 块（上表）。
- **M5**（法律/官职）施工扩到 **11 项**：官职三套合一套、补缺改每日、任命=授衔并加 `ActionIds.AppointTitle` 入口、定义层补齐、位阶改组合序、修 UI 传参与原串打印。
- **M8**（角色/继承）施工扩到 **10 项**：头衔入活档 + 活路径 `Clear()`、三模式真分支、称号按头衔决定、`tier/superiorTitle` 补齐。
- 顺序不变但更硬：**M1 存档闭合**是 M8 的前置；否则头衔与继承都只活在内存里。

## 5. 未验证

1. 未跑 Unity / 未做存档往返实测（"读档残留持有者"是静态路径推演）。
2. `Titles.json` 在工作区有未提交改动（`kind → category` 口径），本审计按工作区版本判定，未与 git HEAD 比对。
3. 未打开场景资产确认是否有 UI 侧注入。

## 6. 产物

| 路径 | 说明 |
|---|---|
| `docs/plan-v3/audit/M-title-system.md` | 头衔专项审计（113 行） |
| `docs/plan-v3/SYSTEM_MASTER_PLAN.md` | S16 清单项 + §3 S16 块 + M5/M8 施工扩展 |
| 本报告 | 整合记录 |
