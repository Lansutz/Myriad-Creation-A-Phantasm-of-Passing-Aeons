# 地基修复清单（F1–F8）

> 2026-11-09 20:20。用户指令："先修基础性的东西"。顺序按"不修就阻断其他一切"排。

| # | 项 | 现状 | 修什么 | 验收 | 状态 |
|---|---|---|---|---|---|
| **F1** | **确定性 RNG** | `ThreadRng = new ThreadLocal<Random>(Environment.TickCount …)`；`TitleCatalog/ContentRegistry/ContentQueries` 的 `new Random()` 时间种子；`_missionaryRng` 共享状态 | ① 新增 `GameWorld.Roll01(tile, block, salt, chance)`：由 (seed, day, tile, block, salt) 派生的**无状态**掷骰；② 人口满意度掷骰改用它；③ 三处 `new Random()` → `new Random(0)`；④ `_missionaryRng` 每次 tick 由 (seed, day) 重新派生 | 同 seed 跑两次 → 同一快照哈希 | **已改（4 处），待 Unity 编译+测试验证** |
| **F2** | **存档闭合** | `PlayerRealmId` 无写入；`primaryCultureId` 读档 −1；`settlementLevel` 只在 Unity 序列化回调同步；`titleInstances`/`primaryTitleKey` 只在 `[Obsolete] GameSaveSystem`；外交四表无 DTO；`chronicle` 不入档 | 全部迁进 `SaveSystem` + DTO + 迁移测试 | 读档前后：玩家政权、文化、聚落等级、头衔、外交、编年史一项不丢 | 待办 |
| **F3** | **双源合一** | 人口（地块块 vs SettlementData）、法律（`LawComposition` vs `RealmLawState`）、金库（`TreasuryLedger` vs `realm.treasury`）、遗传（`DnaData` vs `GenomeInstance`） | 各定唯一真源，另一侧改派生 | 单一写入口（grep 可证） | 待办 |
| **F4** | **内容外提** | 18 张表硬编码在 C#（建筑 29、单位 14、政体门槛 35、商品 57、灾害 12、绰号 81…） | 逐表迁 JSON；代码只解析 | 改内容不改 `.cs`（解耦信号 2） | 待办 |
| **F5** | **Mods 通路** | `Mods/Dna/DnaDefs.json` 被当包目录 ⇒ 永不加载；`ContentPackageValidator` 零运行时调用 | 修目录解析 + 校验器接启动流程 | 放一个 mod 能生效；坏 mod 有报错 | 待办 |
| **F6** | **头衔定义 36 字段只加载 7** | `TitleDefinition` 大量字段无 loader；`tier`/`superiorTitle` 不在 JSON | 补 loader + 内容 | 头衔树能建起来 | 待办 |
| **F7** | **国教无写入** | `RealmData.stateReligionId` 默认 −1，全仓无写入 | 开局赋国教 + 玩家可改 | 传教/热忱/圣战/圣地/面板五链同时通电 | 待办 |
| **F8** | **`faithId` 硬编码 0** | `CharacterManager` 创建人物时 faithId=0，与宗教 id 从 100 起冲突 | 按文化/政权推导 | 人物信仰不指向不存在的宗教 | 待办 |

---

## 0. 文件架构约束（每一条地基修复都必须先过这一关）

**15 个 asmdef 的现状**：`core.asmdef`（无引用，L0）→ `Content`/`World` → `Simulation`（`sim`/`Laws`/`Faith`/`Culture`）→ `Infrastructure` → `App.Contracts`/`App` → `Rendering`/UI。

| 层 | 允许放什么 | 禁止 |
|---|---|---|
| `MyriadCreation.Core` | 纯函数、纯数据、枚举、契约接口 | 引用任何上层；持有运行时状态 |
| `Content.Definitions` / `Content.Modding` | 内容 schema（`*Def`）、加载 | 写运行时状态 |
| `Simulation.*` | 运行时状态与系统 | 被 Core 引用；被 UI 直接改写 |
| `Infrastructure.Save` | DTO、迁移、序列化 | 依赖 UI；绕过 DTO 写世界 |
| `App` / UI | 只读 Contracts、命令下发 | 跨层写状态 |

**三条硬规则**（本轮起执行）：

1. **公用原语放 L0**：任何"多个系统都要用"的东西（随机、哈希、时间换算、值类型），进 `MyriadCreation.Core`。**不为了一次掷骰**让 Culture/Faith/Laws 去引用 `Simulation.WorldState`。
2. **修复不新增跨层引用**：每项地基修复完成后，`grep` 检查 `asmdef` 引用集是否变化；变了必须有理由。
3. **内容与代码分离**：新增内容表走 JSON（F4），代码只做解析/求值；`*Def` 只放 schema，不放逻辑。

---

## F1 的实际改动（本次）

| 文件 | 层 | 改动 |
|---|---|---|
| `Core/MyriadCreation.Core/DeterministicRandom.cs` | **L0（新增）** | `Hash/Value01/Roll01/Create` 四个纯函数：由 `(seed, day, a, b, salt)` 派生，无状态、可并发 |
| `GameWorld.Core.cs` | Simulation | 删 `ThreadLocal<Random>(Environment.TickCount…)`；`Roll01` 改为**委托** L0 原语 |
| `GameWorld.Simulation.cs:77` | Simulation | 满意度掷骰：`ThreadRng.Value.NextDouble() < 0.01` → `Roll01(i, j, 1, 0.01f)` |
| `GameWorld.Simulation.cs:226` | Simulation | `MissionaryTick` 每次用 `DeterministicRandom.Create(seed, day, salt=17)` 重建 rng |
| `GameWorld.Core.cs:243` | Simulation | `_missionaryRng` 由 `readonly` 固定种子改为可变（每 tick 派生） |
| `TitleCatalog.cs`、`ContentRegistry.cs`、`ContentQueries.cs` | Simulation/Modding | `new Random()` → `new Random(0)`（TODO：调用方传 seed 派生 rng） |

**架构上的收益**：随机原语现在在 L0，将来 `Culture`/`Faith`/`Laws`/`Economy` 需要可复现随机时，直接 `using MyriadCreation.Core;`——不需要引用世界状态程序集，也不需要再各自造一个 `ThreadLocal<Random>`。

**为什么先修 F1**：M0.5 的三张表（日耗时/内存/函数热点）全部依赖"同 seed 可复现"；不确定性在，基准就是噪声。

**未验证**：本机未找到 Unity 编辑器路径（`C:\Program Files\Unity*` 不存在），因此**编译与 706 用例尚未跑**。静态检查：`ThreadRng` 零残留、`Roll01` 单定义（委托）、`randomSeed` 为 `public int`、`using Random = System.Random` 在 `GameWorld.Core.cs:42` 与 `GameWorld.Simulation.cs:45` 均存在、`core.asmdef` **无引用**（故新文件不引入任何依赖）。
