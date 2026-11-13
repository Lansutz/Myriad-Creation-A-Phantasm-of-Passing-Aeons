# Faith / Culture / Laws 独立 asmdef 可行性方案（B4 / B5 / B6-part2）

> 状态：**只读方案卡产物，未改任何代码/asmdef**。实现批次按 §6 执行。
> 快照时间：当前 worktree（task-72 契约层完成、task-71 `Definitions → Content.Definitions` 改名进行中）。
> 数据来源：`Assets/Scripts/**`、`Assets/UI/**`、`Assets/Tests/**` 的只读扫描（脚本见附录 A）。
> 关联文档：`SYSTEM_OWNERSHIP.md` §7.2/§7.3/§7.4、`CONTENT_PIPELINE_FAITH_CULTURE.md`、`APP_EXTRACTION_PLAN.md`。

---

## 0. 结论摘要（给下一次实现卡）

**可行**，但前提不是"直接加 3 个 asmdef"，而是先做一层 **host 反转 + 共享定义下沉**：

1. **目标只加 3 个程序集 + 2 个共享层**（不新增 `App.Contracts`、不新增 `GameState`、不动 `World`）：
   - `MyriadCreation.Faith`、`MyriadCreation.Culture`、`MyriadCreation.Laws`：**refs = `Core` + `Content`**（目标态互不引用）。
   - 共享数据/定义继续留在 `Core`（契约/DTO/枚举）与 `Content`（内容定义、内容目录、只读查询接口）。
   - `RealmData`/`RealmDTO` 留在 `Simulation`，**反向引用 Laws 的状态类型**（`Simulation → Laws` 是允许方向）；`CultureData` 留在 Culture、`FaithSystem` 留在 Faith、`FaithSaveData` 留在 Infrastructure.Save。
   - `World` 保持 `refs = Core`，**不需要** Faith/Culture/Laws 的数据类型（见 §3.3）。
2. **关键环**（现在是同程序集内的类型环，加 asmdef 后会变成程序集环）：
   - `State ↔ Laws`：`RealmData.cs:72,74,76` 持 `LawComposition/RealmLawState/RealmDecreeState`；`LawAndDecreeSystem.cs` 有 12 个 `RealmData` 入口（§1.5）。
   - `Culture ↔ Simulation`：`CultureStageEvolutionSystem` 直接用 `GameWorld/RealmData/InnovationTree/SocialStratificationCalculator`（:179/299/333）；反过来 `Simulation/State/Systems/Modding` 用 `CultureData`（§1.4）。
   - `Faith ↔ Simulation`：`CharacterData`（`CanonizationSystem:14,31`、`FaithSystem:98,118`）、`RealmData/CharacterManager`（`GreatHolyWarSystem:52,76`）、`GameWorld`（`FaithContracts`）；反向 `CharacterManager/ThoughtManager/Modding/UI/Save` 用 `FaithSystem`。
   - `Laws ↔ Simulation`：除 `RealmData` 外还有 `GameWorld`（`LawContracts`）、`Systems` 的 `EconomyManager/ManpowerSystem/PoliticsSimulationSystem/SocietyManager`（`LawEffectRuntime`）、`CharacterData`（`InheritanceLaw/DecreePropagationSystem`）。
   - **没有** `Faith↔Culture` 环；`Faith→Laws` 只有 `GreatHolyWarSystem:91 InheritanceLaw` 一处；`Culture→Laws` 只有 `CultureData.cs:82 InheritanceLaw` 一处（这两处可通过将 `InheritanceLaw` 配置部分与战争文件下沉/上移消除，见 §4.2）。
3. **推荐顺序：B6 Laws → B5 Culture → B4 Faith**（与卡面建议一致）：
   - Laws 是**唯一被 State 直接持有的下层系统**（`RealmData/RealmDTO` 字段），且其它两系统的 `InheritanceLaw` 依赖可随 Laws 的状态/定义归属一起处理；先把 Laws 变 "纯下层 asmdef"，`Simulation→Laws` 才有稳定方向。
   - Culture 第二：9 文件、host 集中在 `CultureStageEvolutionSystem` 的 4 个方法，是最小 pilot；且它依赖的 `InheritanceLaw` 在 Laws 完成后已有归属。
   - Faith 最后：定义/目录下沉 Content 后运行时只剩 ~3 文件，但外部消费者最多（UI/Rendering/Infra/Save/Tests 共 23 文件）；放最后便于集中改引用。
4. **每系统最小首步 3 项**（可直接照做，见 §7）：先做 host 移出 + 状态/定义归属 + 1 个 context 接口，**仍保持同一程序集**；三步都绿后再加 asmdef。
5. **门禁**：每一步先 Unity batchmode 编译 + 目标测试，最后 full EditMode/PlayMode + 存档回归；asmdef 反引检查（新系统 asmdef 的 `references` 不得含 `MyriadCreation.Simulation`）。回滚点见 §6.5。

---

## 1. 现状快照（证据）

### 1.1 当前 asmdef DAG

| 程序集 | asmdef | references | 备注 |
|---|---|---|---|
| `MyriadCreation.Core` | `Scripts/Core/core.asmdef` | **无** | 含 `MyriadCreation.Core.Contracts`（契约层已落地） |
| `MyriadCreation.World` | `Scripts/World/world.asmdef` | Core | 地图/聚落生成模型；**不依赖 Simulation** |
| `MyriadCreation.Content` | `Scripts/Content/MyriadCreation.Content.asmdef` | Core | 目前只有 `Content.Definitions` 95 文件；内容加载器仍在 `Simulation.Modding` |
| `MyriadCreation.Simulation` | `Scripts/Simulation/sim.asmdef` | Core, World, Content | 555 文件；Faith/Culture/Laws 目前都在这里 |
| `MyriadCreation.Infrastructure` | `.../MyriadCreation.Infrastructure.asmdef` | Core, World, Simulation, Content | Save/Audio |
| `MyriadCreation.Rendering` | `.../MyriadCreation.Rendering.asmdef` | Core, World, Simulation, Content | |
| `MyriadCreation.UI` | `Assets/UI/MyriadCreation.UI.asmdef` | Core, Simulation, Rendering, Infrastructure, World, Content, App | 17/32 import Simulation |
| `MyriadCreation.App` | `Scripts/App/MyriadCreation.App.asmdef` | Core, Infrastructure, UI | |

> 现状 DAG 本身无环；问题只在 `MyriadCreation.Simulation` **单程序集内**把三个系统的 data/logic/host glue 混在一起。

### 1.2 三系统规模（当前文件/行数）

| 系统 | 目录 | .cs | 行数 | 子目录 |
|---|---|---|---|---|
| Faith | `Scripts/Simulation/MyriadCreation.Simulation.Faith` | 18 | 1193 | `Definitions/`(9) `Runtime/`(6) `State/`(3) |
| Culture | `Scripts/Simulation/MyriadCreation.Simulation.Culture` | 9 | 1410 | `Definitions/`(1) `Runtime/`(6) `State/`(2) |
| Laws | `Scripts/Simulation/MyriadCreation.Simulation.Laws` | 50 | 7070 | 扁平（task-63/65/66 已合并） |

### 1.3 当前"系统 → 外部命名空间"的直接 using 依赖（文件数）

| 系统 | 外部 `using MyriadCreation.Simulation.*`（文件数） | 说明 |
|---|---|---|
| Faith | `Simulation.Definitions`(13, 旧名残留) / `State`(9) / `Systems`(5) / `WorldState`(1) / `Laws`(1) / `Scheduling`(1) | 13 个 Definitions 是 task-71 改名过渡残留；真实 host 依赖：State/Systems/WorldState/Scheduling |
| Culture | `State`(6) / `Systems`(5) / `Modding`(4) / `WorldState`(2) / `Scheduling`(1) / `Actors`(1) / `Laws`(1) | 真实 host 依赖：GameWorld/RealmData/Systems/Modding/Actors/Scheduling |
| Laws | `State`(15) / `Systems`(6) / `WorldState`(2) / `Scheduling`(0) / `Laws`(5, 自引用) / `Definitions`(2, 旧名残留) | 真实 host 依赖：RealmData/CharacterData/Systems/GameWorld |

### 1.4 外部消费者（只读扫描：按"系统 public 类型名"匹配的 distinct 文件数）

| 系统 | 外部消费文件总数 | Simulation | Tests | Infrastructure | UI | Rendering | 其它 |
|---|---|---|---|---|---|---|---|
| Faith | 23 | 11 | 4 | 4 | 3 | 1 | 0（Core 契约文件为误报，已剔除） |
| Culture | 17 | 11 | 3 | 2 | 1 | 0 | 0（Core 契约文件为误报，已剔除） |
| Laws | 40 | 22 | 13 | 0（仅经 RealmDTO 间接） | 3 | 0 | 2（Culture 1 / Faith 1；Content 2/Core 1 为误报已剔除） |

> 注：扫描按 token 近似，`Core/MyriadCreation.Core.Contracts/*Contracts.cs`、`Content/Definitions/InnovationField.Law`（枚举成员）等为**误报**，已从结论中剔除；实际外部实现类引用以 §1.5 明细为准。

**Faith 外部消费者（23 个实现文件）**
- Simulation(11)：`Modding/ContentRegistry.cs`、`ContentResolvers.cs`、`ContentRuntime.cs`（`ReligionDef/DoctrineOptionDef/ReligionCatalog`）；`Systems/CharacterManager.cs:85`、`ThoughtManager.cs:15/42/112/140/150`、`MissionarySystem.cs:13`、`CombatManager`；`WorldState/GameWorld.*`。
- Infrastructure(4)：`Save/FaithPersistence.cs:19-55`、`FaithSaveData.cs:49/122`、`SaveData.cs:70`、`SaveSystem.cs:398/496`。
- Tests(4)：`FaithPersistenceTests`、`RestoredSystemTests`、`RealmOverviewTextTests`、`SystemContractsTests`。
- UI(3)：`UIManager.Panels.cs:420-430`、`RealmOverviewText.cs:13`、`ReligionPanelText.cs:12/132`。
- Rendering(1)：`MapRenderer.Colors.cs:215-221,297`（`ReligionMapLevel`；若类型移到 Content，此依赖消失）。

**Culture 外部消费者（18）**
- Simulation(11)：`Systems/RealmSituationBuilder.cs:142`、`SocialClassAvailability.cs:10/63`、`SocialStratificationCalculator.cs:38`、`Economy/ClassEmergenceEvents.cs:34/68`、`State/InnovationTree.cs:459`、`State/OfficeTitleCatalog.cs:14/29`、`Modding/ContentRegistry.cs:28-37`、`ContentRuntime.cs:258` 等。
- Tests(3)：`CultureStageEvolutionDataTests`、`LanguageFamilyTests`、`SystemContractsTests`。
- Infrastructure(2)：`SaveData.cs:38`（`List<CultureData>`）、`SaveSystem.cs:369/381/422`。
- UI(1)：`SettlementPeopleText.cs:76`。

**Laws 外部消费者（40 个实现文件；另有 Content 2/Core 1 类型名误报已剔除）**
- Simulation(22)：`State/RealmData.cs:72/74/76`、`RealmDTO.cs:17/19/21`；`Systems/AdminDivisionSystem.cs:37/70`、`PoliticalAccessAnalyzer.cs:13/112`、`RealmSituationBuilder.cs:153`、`SuccessionSystem.cs:26/33/153/203`、`EligibilityRules`、`TitleEligibility`；`AI/AIDecisionSystem.cs:36`；`Units/UnitProvisioning.cs:18`；`WorldState/GameWorld.Simulation.cs:760/1098-1124`、`GameWorld.Queries.cs:277`、`GameWorld.Succession.cs:327/336-338`；`EconomyManager.cs:474`、`ManpowerSystem.cs:74/82`、`PoliticsSimulationSystem.cs:85`。
- Tests(13)：`LawAndDecreeTests`、`LawDecreePipelineTests`、`LawEffectRuntimeTests`、`LawModuleTests`、`FactionLawChangeTests`、`LawContentJsonTests`、`AdminDivisionTests`、`DecreePropagationTests` 等。
- UI(3)：`UIManager.cs:491`、`RealmOverviewText.cs:29-31`、`SettlementPeopleText.cs:81`。
- Culture(1)：`CultureData.cs:82`（`InheritanceLaw`）。
- Faith(1)：`GreatHolyWarSystem.cs:91`（`InheritanceLaw`）。

### 1.5 关键跨边界类型/调用点（实现批次要改的精确位置）

| 边界 | 现状证据 | 目标处理 |
|---|---|---|
| `RealmData → Laws` | `State/RealmData.cs:72` `LawComposition laws`；`:74` `RealmLawState lawState`；`:76` `RealmDecreeState decreeState`；`:103/105` `InheritanceLaw` 返回 | **保留**（`Simulation→Laws` 合法方向）；`InheritanceLaw` 配置下沉 Content 后改为 Content 类型 |
| `RealmDTO → Laws` | `State/RealmDTO.cs:17/19/21` 三个字段；`:79-80` From / `:136-137` To | 保留字段名/拷贝逻辑；仅程序集归属变化 |
| `Laws → RealmData` | `LawAndDecreeSystem.cs`：`:329 EnsureKnownLaws`、`:375 AddKnownLaw`、`:386 GrantCharter`、`:397 RevokeCharter`、`:406 CanPropose`、`:502/507 BeginEnactLaw`、`:578/585 TickAllPendingLaws`、`:600 CancelPendingLaw`、`:627 ForceSetLaw`、`:825 ForceReformSync`（共 12 个 public 入口）；`JudicialSystem.cs:64 TrialCrime(…, RealmData realm)`；`LawCompositionMigration.cs:46`；`LawReform.cs:10/19/43/59`；`LawDecreeView.cs:50` | 改为 `ILawContext` + 显式状态参数（`RealmLawState`/`RealmDecreeState`/`LawComposition`），`RealmData` 只在 host 适配器里 → `Laws` 不再 import Simulation |
| `Laws → CharacterData` | `InheritanceLaw.cs:143/153/222`（`DetermineHeir/GetHeirOrderedPool/IsSibling`）；`DecreePropagationSystem.cs:179 Resolve(..., CharacterData official, ...)`；`LawContracts.cs:56`（host） | 继承人选逻辑移到 host `InheritanceHeirSelection`（Laws 保留配置+`DistributeLand`）；`DecreePropagation` 移到 host；`LawContracts` 移到 host |
| `Laws → GameWorld/Systems` | `LawContracts.cs:16`；`LawSystem.cs:10`（0 外部引用，疑似死文件）；`LawEffectRuntime.cs:60/68/74/83/110/119`（`RealmData/RealmSociety`）；`LawEffectRuntime` 被 `WorldState/GameWorld.Succession.cs:338`、`Systems/ManpowerSystem.cs:82`、`PoliticsSimulationSystem.cs:85`、`EconomyManager.cs:474` 引用 | host 粘合文件（Contracts/View/EffectRuntime/LawSystem/Judicial/DecreePropagation）移出；Laws 只保留纯规则/状态/定义 |
| `Culture → GameWorld/RealmData` | `CultureStageEvolutionSystem.cs:179 FormRealmFromActor(GameWorld,…)`、`:299 MonthlyTick(GameWorld)`、`:333 HasUrbanCenter(GameWorld, RealmData)`；`CultureContracts.cs:15` | 前 3 个方法+ViewBuilder 移到 host；`CultureStageEvolutionSystem` 只保留 `EvaluateEvolution`（纯函数，24-176 行） |
| `Culture → Systems/Modding/Actors` | `CultureStageEvolutionSystem`（`SocialStratificationCalculator`、`ContentRegistry/Resolvers`、`MapActor/StateFormationPath/NomadicRealmSystem`）；`CultureLineage.cs:58 ContentRegistry`；`CultureTransition.cs:71 ContentResolvers`；`CultureData.cs:114/133 ContentResolvers`；`CultureData.cs:82 InheritanceLaw`、`:84 OfficeTitleEntry` | 内容查询改 `IContentLookup`（Content）；`InheritanceLaw` 配置下沉 Content；`OfficeTitleEntry` 下沉 Content；host 逻辑移出 |
| `Faith → State/Systems/WorldState/Laws` | `CanonizationSystem.cs:14/31`（CharacterData）、`FaithSystem.cs:98/118`（CharacterData）、`GreatHolyWarSystem.cs:52/61/76/91`（CharacterManager/RealmData/InheritanceLaw）、`WarfareReligionSchedule.cs:13`（CombatManager/Army/WarState/WarRules/ISimulationScheduler）、`FaithContracts.cs:15`（GameWorld） | 定义/目录下沉 Content；战争+调度的 3 文件与 `FaithContracts` 移 host；字符查询改 `IFaithContext` |
| `Faith 定义 → State` | `Definitions/*` 13 文件 `using Simulation.Definitions`（残留）、`State/Doctrine.cs`/`DoctrinePool.cs` 引 State；`FaithSystem` 字段用 `Deity/EcclesiasticalTitle`（现 State） | 定义文件 + `Deity/EcclesiasticalTitle` 下沉 Content；清掉旧 using |
| `CultureData → Modding` | `CultureData.cs:114/133` 用 `ContentResolvers.EthnicGroups/Traditions` | 改为 `IContentLookup`（Content）参数；`CultureData` 只留可序列化字段与纯计算 |
| `Simulation → Culture/Faith/Laws` | `GameWorld.Core.cs:79 cultures`、`:109 _faithSystems`、`:195 _judicialSystems`；`GameWorld.Queries.cs:105 GetFaithSystem`、`:262 GetOrCreateJudicialSystem`；`GameWorld.Simulation.cs:781-800` 调度注册、`:833-838 MonthlyCultureStageEvolution`、`:754-772 TickPendingLawsDaily`、`:1098-1124 DailyJusticeTick`；`GameWorld.Succession.cs:336-338 LawEffectRuntime` | **保留并加强**：Simulation 作为 host 反向引用三个系统 asmdef；把 `*Schedule`/`*Contracts`/View/Effect 粘合都收到 Simulation |

---

## 2. 环检测

### 2.1 当前（同一 `MyriadCreation.Simulation` 程序集内）的类型环

| # | 环 | 证据 |
|---|---|---|
| C1 | `State ↔ Laws` | `RealmData`/`RealmDTO` 持 `LawComposition/RealmLawState/RealmDecreeState`；Laws 的 `LawAndDecreeSystem/JudicialSystem/LawReform/LawDecreeView/LawEffectRuntime` 以 `RealmData` 为参数 |
| C2 | `State ↔ Culture` | `Simulation.State.InnovationTree.cs:459`、`OfficeTitleCatalog.cs:14/29` 用 `CultureData`；`CultureStageEvolutionSystem` 用 `RealmData`（:193/307/333） |
| C3 | `Culture ↔ Systems` | Systems 用 `CultureData`；`CultureStageEvolutionSystem` 用 `SocialStratificationCalculator`/`StateFormationPath`/`NomadicRealmSystem` |
| C4 | `Culture ↔ Modding` | `ContentRuntime/ContentRegistry` 用 `CultureData`（JSON 加载）；Culture 的 `CultureLineage/CultureTransition/CultureData` 用 `ContentRegistry/ContentResolvers` |
| C5 | `Culture ↔ WorldState` | `GameWorld.cultures`/`MonthlyCultureStageEvolution` 用 Culture；`CultureStageEvolutionSystem`/`CultureContracts` 用 `GameWorld` |
| C6 | `Faith ↔ Systems` | `CharacterManager/ThoughtManager/MissionarySystem` 用 `FaithSystem`；`CanonizationSystem/FaithSystem` 用 `CharacterData`（Systems/State） |
| C7 | `Faith ↔ WorldState` | `GameWorld` 用 `FaithSystem`；`FaithContracts` 用 `GameWorld` |
| C8 | `Laws ↔ Systems` | Systems 用 `LawComposition/LawEffectRuntime/LawConstraints`；Laws 用 `EconomyManager/ManpowerSystem/PoliticsSimulationSystem/SocietyManager/AdminDivision/EligibilityRules` |
| C9 | `Laws ↔ WorldState` | `GameWorld` 用 `LawComposition/LawCatalog/LawEffectRuntime/LawAndDecreeSystem/JudicialSystem`；`LawContracts/LawSystem` 用 `GameWorld` |

> 单向边（不构成环）：`Faith→Laws`（`GreatHolyWarSystem:91`）、`Culture→Laws`（`CultureData:82`）、`Systems→Faith/Culture` 的部分只读、`World → 无`。

### 2.2 如果现在直接给 3 个文件夹加 asmdef（不做反转）会产生的程序集环

- `MyriadCreation.Simulation ↔ MyriadCreation.Faith`（C6/C7）
- `MyriadCreation.Simulation ↔ MyriadCreation.Culture`（C2/C3/C4/C5）
- `MyriadCreation.Simulation ↔ MyriadCreation.Laws`（C1/C8/C9）
- 若同时保留 `Faith→Laws`/`Culture→Laws` 边：`Faith→Laws`、`Culture→Laws` 本身无环（Laws 不反引），但会强迫 Laws 先拆；本方案选择把这 2 条边也消除（继承配置下沉 Content），得到互不引用的目标态。
- 不会产生的环：`Faith↔Culture`（无）、`World↔系统`（World 只引 Core）、`Content↔系统`（Content 只引 Core，系统引 Content）。

### 2.3 结论

1. **必须先把 host 粘合代码移出系统文件夹**（在同一 `Simulation` 程序集内先做，不改 asmdef），再翻转 asmdef；否则只能让系统 asmdef 引用 `Simulation`，形成反向依赖。
2. **必须先把跨系统共享的数据/定义下沉到 Content/Core**：`InheritanceLaw` 配置+枚举、`OfficeTitleEntry`、`Deity/EcclesiasticalTitle`、`Religion*` 定义/目录。否则 Culture/Faith 需要引用 Laws 或 Simulation。
3. `Simulation` **反向引用**三个系统是目标架构的一部分（host → systems），不是违规；但要保证系统 asmdef 的 `references` 中**永远不出现** `MyriadCreation.Simulation`（CI 可检查）。

---

## 3. 目标 asmdef 依赖图

### 3.1 目标程序集表

| 程序集 | 目录 | references（目标） | 说明 |
|---|---|---|---|
| `MyriadCreation.Core` | `Scripts/Core` | — | 含 `Core.Contracts`（接口/DTO/枚举）；**本轮不拆 App.Contracts** |
| `MyriadCreation.World` | `Scripts/World` | Core | 保持地图/聚落只读模型；**不引系统** |
| `MyriadCreation.Content` | `Scripts/Content` | Core | 内容定义 + `IContentLookup`（新增）+ 内容目录（`ReligionCatalog` 等下沉） |
| `MyriadCreation.Faith` | `Scripts/Simulation/MyriadCreation.Simulation.Faith` | **Core, Content** | 运行时 FaithSystem/Canonization/事件 handler；无 Simulation/World/Laws 引用 |
| `MyriadCreation.Culture` | `Scripts/Simulation/MyriadCreation.Simulation.Culture` | **Core, Content** | 纯文化阶段评估/文化数据/血缘相似；无 Simulation/World/Laws |
| `MyriadCreation.Laws` | `Scripts/Simulation/MyriadCreation.Simulation.Laws` | **Core, Content** | 法律/法令定义、状态、规则、内容加载；无 Simulation/World |
| `MyriadCreation.Simulation` | `Scripts/Simulation`（剩余） | Core, World, Content, **Faith, Culture, Laws** | host：GameWorld/调度/State/Systems/Save glue；**允许反引三系统** |
| `MyriadCreation.Infrastructure` | `Scripts/Infrastructure` | Core, World, Simulation, Content, **Faith, Culture**（Laws 仅按需） | Save 需要 FaithSystem/CultureData |
| `MyriadCreation.Rendering` | `Scripts/Rendering` | Core, World, Simulation, Content（Faith 仅在 `ReligionCatalog` 未下沉时临时需要） | 目标 B7 收缩到 Core/World/Contracts |
| `MyriadCreation.UI` | `Assets/UI` | Core, Simulation, Rendering, Infrastructure, World, Content, **Faith, Culture, Laws**（过渡；后续用 Core.Contracts） | |
| `MyriadCreation.App` | `Scripts/App` | Core, Infrastructure, UI | 不变 |
| `MyriadCreation.Tests` / `.PlayMode` | `Assets/Tests` | 全部 + 三新程序集 | 测试按需引用 |

### 3.2 目标图（文字）

```
Core
 ├─ Content ─────────────┬──────────────┐
 ├─ World                │              │
 └─ Core.Contracts*      │              │
                          │              │
Faith ──► Core,Content    │              │
Culture ─► Core,Content ──┘              │
Laws ────► Core,Content ────────────────┘
        ▲          ▲          ▲
        │          │          │
   Simulation ─────┴──────────┘      (host：GameWorld/调度/State/Systems/Save glue)
        ▲
Infrastructure / Rendering / UI
```
*Core.Contracts 仍在 Core 程序集内（见 §3.4）。

### 3.3 回答卡面三问

1. **Simulation 是否反引？** 是，且必须：`GameWorld/GameWorld.Simulation/GameWorld.Queries/GameWorld.Succession/Save` 是组合根与状态持有者，需要直接使用三个系统的公开 API；方向是 `Simulation → 系统 asmdef`。只要系统 asmdef 不反引 Simulation，DAG 仍无环。
2. **World 是否需要 Faith/Culture/Laws 的数据类型？** **不需要**。`World`（地图/聚落生成）当前 `refs=Core`，不 import `MyriadCreation.Simulation.*`；Culture 需要的 `SettlementData/SettlementForm/SettlementLevel` 是 `World → Culture` 的只读方向（由 Culture 引用 World），而不是 World 持有系统数据。拆分目标让 Culture 连 `World` 也不需要（host 移出 `HasUrbanCenter` 后），World 保持不变；**不要**把 `RealmData/lawState/CultureData` 塞进 World（会破坏 "World 只放地图模型" 的 DAG）。
3. **App.Contracts?** 本轮**不建议**新建：`Core.Contracts` 当前已在纯 `Core` 程序集（`refs=[]`），Contract 接口/上下文只依赖 Core 类型，已满足"无 Simulation 反向依赖"。新建 `App.Contracts` 只会让 UI/Rendering/Infra/Simulation 全部加一次引用，收益要到 B3（App 组合根）才显现。**建议**：context 接口暂放 `Core.Contracts`；B3 若 Core 需要进一步纯净，再把 `Core.Contracts` 目录整体提升为 `MyriadCreation.App.Contracts` asmdef（文件 namespace 可保持不变，仅加 asmdef，见 §4.5 的"命名空间保持"技巧）。

### 3.4 命名空间保持（降低改动量的关键技巧）

Unity asmdef 只决定**程序集归属**，不要求 namespace 与程序集同名。因此拆程序集时可以：

1. 新 asmdef 放在现有系统文件夹根（如 `Simulation/MyriadCreation.Simulation.Faith/MyriadCreation.Faith.asmdef`），文件、namespace（`MyriadCreation.Simulation.Faith`）都不动；
2. 文件按 owner 需求在文件夹间移动时**保持原 namespace**（如把 `LawContracts.cs` 从 Laws 移到 `Simulation/WorldState/`，namespace 仍可暂留 `MyriadCreation.Simulation.Laws`，或统一改 host namespace）；
3. 跨程序集的引用通过 asmdef 的 `references` 解决，**大多数消费者只需改 asmdef JSON，不需改 .cs 的 using**；
4. 后续可另开卡统一 namespace（低风险纯改名）。

这样本批的"改动文件数"主要落在：host 移出文件、被反转的 API 实现/调用点、asmdef JSON；而不是全仓 using 重写。

---

## 4. 环反转方案

### 4.1 共享层归属

| 共享层 | 内容 | 归属 | 规则 |
|---|---|---|---|
| `Core` | `Core.Contracts` 的 `IFaithQuery/IFaithCommand/ICultureQuery/ICultureCommand/ILawQuery/ILawCommand`（已存在）、新增 `IFaithContext/ICultureContext/ILawContext`、`ICharacterQuery/IRealmQuery` 的最小 DTO | Core | 只用 int/string/枚举/简单 DTO；**不得**出现 Simulation/Content 类型（内容查询接口 `IContentLookup` 放 Content） |
| `Content` | 内容定义：`ReligionDef/Doctrine*/FaithType/FaithOrganizationType/ReligionMapLevel/ReligionNodeType/SaintDef`、`Deity/EcclesiasticalTitle`、`OfficeTitleEntry`、`InheritanceLaw` 配置+继承枚举、`IContentLookup` | Content | 纯数据/目录；不引用 Simulation/World |
| `World` | 地图/聚落模型（既有） | World | 不新增系统数据；不反引 Faith/Culture/Laws |
| 系统自身 | Laws：`RealmLawState/RealmDecreeState/LawComposition/LawCompositionMigration`?/`LawDef/...`/`Crime*`；Culture：`CultureData/CultureStageDef/CultureStageCatalog/CultureLineage/Similarity/Transition`；Faith：`FaithSystem/CanonizationSystem/ReligionSimulationEventHandler` | 各自 asmdef | 状态随 owner |
| `Infrastructure.Save` | `FaithSaveData`（FaithSystem 的存档 DTO） | Infrastructure | 引用 Faith 程序集；JsonUtility 结构不变 |
| `Simulation` | `RealmData/RealmDTO/CharacterData/Army/WarState/WarRules`、GameWorld、host glue（Contracts/View/Effect/Schedules/War） | Simulation | 允许引用所有下层 |

### 4.2 状态类型归属（卡面点名项）

| 类型 | 现在 | 目标归属 | 引用方向/说明 |
|---|---|---|---|
| `RealmData` | `Simulation.State` | **Simulation**（不迁 World/GameState） | 持 `LawComposition/RealmLawState/RealmDecreeState` → `Simulation→Laws`；faith/culture 仍只存 id/标量 |
| `RealmDTO` | `Simulation.State` | **Simulation** | 同 `RealmData`；`lawState/decreeState` 字段名、From/To 拷贝不变 |
| `RealmLawState` / `RealmDecreeState` | Laws | **Laws** | Laws 是 owner；Simulation/UI/Tests 通过 asmdef 引用 Laws |
| `LawComposition` / `ComponentChoice` / `PolityComponentInnovations` | Laws | **Laws** | `RealmData.laws` 字段类型 → Simulation→Laws；UI 的 `LawComposition` 依赖 UI→Laws |
| `LawGroupState/PendingLaw/ActiveDecree/DecreeCooldown/DecreeOrder/...` | Laws | **Laws** | 状态随 owner |
| `LawDef/LawGroupDef/LawRequirement/LawEffect/DecreeDef/LawConstraints/LawCatalog/LawEffects/Crime*` | Laws | **Laws** | 纯定义/规则；`DefaultLawCatalog/LawContentLoader` 也留 Laws |
| `InheritanceLaw` | Laws | **Content（配置+枚举+`DistributeLand`）；host 的 `DetermineHeir/GetHeirOrderedPool/IsSibling` → `Simulation` 的 `InheritanceHeirSelection`** | 这样 `CultureData.defaultSuccessionLaw` 与 `RealmData.GetEffectiveSuccessionLaw()` 都引用 Content，不再需要 Culture→Laws/State→Laws 的"逻辑"边（仅保留字段类型依赖到 Content） |
| `InheritanceScope/Branch/Gender/Age、LandInheritanceMode、TitleInheritanceMode` | Laws | **Content** | 纯枚举定义 |
| `CultureData` | Culture | **Culture** | 保持 `[Serializable]`；`SaveData.cultures` → Infrastructure→Culture；去掉 Modding/State/Laws using（内容方法改 `IContentLookup`，`InheritanceLaw` 改 Content，`OfficeTitleEntry` 移 Content） |
| `OfficeTitleEntry` | `Simulation.State` | **Content** | 纯 data；`CultureData.officialTitles` 字段类型 |
| `FaithSystem` | Faith | **Faith** | 运行时对象；存档由 `FaithSaveData` 捕获；char helper 改 `IFaithContext` |
| `FaithSaveData` | Infrastructure.Save | **Infrastructure.Save** | 不迁 Faith；引用 FaithSystem；字段名/JSON 不变 |
| `Deity/EcclesiasticalTitle` | `Simulation.State` | **Content** | 信仰定义引用 |
| `CharacterData/CharacterManager` | `Simulation.State/Systems` | **Simulation（不迁）** | Faith/Laws 通过 `IContext`/ID 查询，不直接引用；避免把 CharacterData 拉进 Content/World |
| `GreatHolyWarState` | Faith | **Simulation（host War）** | 与 `GreatHolyWarSystem/WarfareReligionSchedule` 一起上移 |

### 4.3 Host 点反转清单（按系统）

#### Faith（→ `IFaithContext` + host 移出）

| 现位置 | 现依赖 | 目标 |
|---|---|---|
| `Runtime/FaithContracts.cs:15` | `GameWorld` | 移到 `Simulation/WorldState/`（host adapter），不属 Faith asmdef |
| `Runtime/GreatHolyWarSystem.cs:52,61,76,91` | `CharacterManager/RealmData/InheritanceLaw/SuccessionSystem` | 移到 Simulation（战争 host，如 `Simulation/WorldState/FaithHost/War`）；用 host 的继承/角色服务 |
| `Runtime/WarfareReligionSchedule.cs:13` | `ISimulationScheduler/CombatManager/Army/WarState/WarRules` | 移到 `Simulation` 调度注册处（`GameWorld.Simulation.cs:793` 已是 host 注册点） |
| `Runtime/CanonizationSystem.cs:14,31`、`Runtime/FaithSystem.cs:98,118` | `CharacterData` | 改为 `int characterId` + `IFaithContext.GetVirtueTags/GetSinTags/…`；或在 host 中提供 `CanonizationScoring` |
| `Definitions/*`、`State/{Doctrine,DoctrinePool}`、`ReligionCatalog` | 内容/状态定义（含 State 的 `Deity/EcclesiasticalTitle`） | 下沉 Content；`Modding/ContentRegistry/Resolvers/Runtime` 改为 `using MyriadCreation.Content.Definitions`，不再 `using ...Faith` |
| `FaithSystem` 字段 `deities/doctrine/hierarchyTitles` | State/Definitions | Content 类型（移动后仅 `using Content`） |

#### Culture（→ `ICultureContext` + host 移出）

| 现位置 | 现依赖 | 目标 |
|---|---|---|
| `Runtime/CultureContracts.cs:15` | `GameWorld/RealmData` | 移到 `Simulation/WorldState/` host adapter |
| `Runtime/CultureSchedule.cs:11,21` | `ISimulationScheduler/SimulationTickContext` | 删除/上移；host `GameWorld.Simulation.cs:791` 注册 `MonthlyCultureStageEvolution` |
| `Runtime/CultureStageEvolutionSystem.cs:179-353`（`FormRealmFromActor/ApplyPathBonuses/MonthlyTick/HasUrbanCenter`） | `GameWorld/RealmData/MapActor/StateFormationPath/NomadicRealmSystem/InnovationTree/SocialStratificationCalculator/SettlementData` | 移到 `Simulation/WorldState/CultureHost.cs`；Culture 只保留 `EvaluateEvolution`（24-176）纯逻辑 |
| `Runtime/CultureLineage.cs:58`、`CultureTransition.cs:71`、`State/CultureData.cs:114/133` | `ContentRegistry/ContentResolvers` | 改为 `IContentLookup`（Content 接口，host 注入/参数传入） |
| `State/CultureData.cs:82` | `InheritanceLaw` | Content（继承配置下沉后） |
| `State/CultureData.cs:84` | `OfficeTitleEntry`（State） | Content |

#### Laws（→ `ILawContext` + host 移出）

| 现位置 | 现依赖 | 目标 |
|---|---|---|
| `LawContracts.cs` | GameWorld/RealmData/CharacterManager | 移到 `Simulation/WorldState/` host（对应 task-72 适配器属于 host 层） |
| `LawDecreeView.cs:50`（`Build(RealmData, …)`） | RealmData | 移到 host（或改成接收 `ILawViewSource` 快照 DTO）；UI 走 `ILawQuery.GetGovernmentView` |
| `LawEffectRuntime.cs`（全部） | RealmData/RealmSociety/Systems managers | 移到 `Simulation` host（效果应用属于 host 粘合）；Laws 只保留 `LawEffect/SumLawEffects` 等纯函数 |
| `LawSystem.cs:10`（0 外部引用） | GameWorld | 确认后删除或随 host 移出（不阻塞拆分） |
| `JudicialSystem.cs:64`、`DecreePropagationSystem.cs:179`、`LawCompositionMigration.cs:46` | RealmData/CharacterData/SettlementData | 移到 host（court/decree/迁移属于 host）；或改 `ILawContext` |
| `LawAndDecreeSystem.cs` 12 个 `RealmData` public API | RealmData | 改 `(RealmLawState state, ILawContext ctx, …)` 或 `LawRealmSnapshotDto`；`RealmData` 侧由 host 适配器解包 |
| `LawReform.cs:10/19/43/59`（`RealmData/InnovationTree/Chronicle`） | State/Systems | 改 `ILawContext` 或移到 host（`RegimeChangeDynamics.cs:260` 是唯一调用点） |
| `InheritanceLaw.cs:143+`（CharacterData 方法） | CharacterData | 方法移到 host `InheritanceHeirSelection`；Laws 保留配置/`GetName/DistributeLand` |

### 4.4 Context 接口草案（本轮放 `Core.Contracts`）

只使用 id/枚举/简单 DTO；host（Simulation）实现，系统通过 Init/构造/参数接收：

```csharp
// IFaithContext（host 实现；Faith 运行时只读）
bool TryGetCharacterVirtueTags(int characterId, List<string> buffer);
bool TryGetCharacterSinTags(int characterId, List<string> buffer);
int GetRealmFaithId(int realmId);
bool IsCharacterAlive(int characterId);
// ICultureContext（host 实现；Culture 纯逻辑）
int[] GetRealmIds();
int GetRealmCultureId(int realmId);
int[] GetRealmInnovationIds(int realmId);
void GetSocialStratification(int realmId, out float stratification, out float specialization, out int population);
bool HasUrbanCenter(int realmId);
int CurrentDay { get; }
// ILawContext（host 实现；Laws 规则）
int GetSupremeRulerId(int realmId);
float GetTreasury(int realmId);   // 需要写回时用 Apply* 命令，不直接暴露可变对象
float GetPrestige(int realmId);
float GetLegitimacy(int realmId);
float GetCentralization(int realmId);
float GetAuthority(int realmId);
int[] GetRealmInnovationIds(int realmId);
string GetCharacterName(int characterId);
void ApplyRealmEffect(int realmId, float stabilityDelta, float legitimacyDelta, float unrestDelta);
bool TryForceRealmRef(float? treasuryDelta, float? authorityDelta); // 或 host 侧 Ref 包装
```

> 若实现成本高，允许分两步：先 host 方法 `Simulation.LawHost` 静态类包装 `RealmData`，Laws 只依赖 `ILawContext` 的最小 5 个方法；其余继续 host 注入 `RealmLawState` 实例。

### 4.5 调度（GameScheduler）处理

- `ISimulationScheduler/SimulationTickContext/SimulationCadence` 属于 `Simulation.Scheduling`，**不下沉**；
- 系统**不再拥有** `*Schedule` 注册类：`CultureSchedule`/`WarfareReligionSchedule` 注册逻辑上移到 `GameWorld.RegisterSimulationSchedules()`（已有调用点 `GameWorld.Simulation.cs:781-800`）；
- 系统只暴露静态 `Tick`/纯评估方法，由 host schedule 回调；
- 这样 Faith/Culture asmdef 不需要 `Scheduling` 引用。

---

## 5. 存档 / 序列化影响

### 5.1 字段与归属

| 存档点 | 现类型/位置 | 拆分后 | 兼容性要求 |
|---|---|---|---|
| `SaveData.faiths` | `List<FaithSaveData>`（Infrastructure） | 字段不变；`FaithSaveData` 引用 Faith 的 `FaithSystem` | Infrastructure→Faith；`SaveVersion=16` 不变；JSON 字段名不变 |
| `SaveData.cultures` | `List<CultureData>`（Infrastructure） | 字段不变；`CultureData` 在 Culture asmdef | Infrastructure→Culture；`[Serializable]`/字段名不变 |
| `SaveData.realms` | `List<RealmDTO>`（Simulation） | 字段不变；`RealmDTO.lawState/decreeState` 类型在 Laws asmdef | Simulation→Laws；JSON 不变 |
| `RealmData.laws/lawState/decreeState` | Simulation.State | 类型归属 Laws；字段名不变 | `RealmData` 仍在 Simulation；`RealmData.GetEffectiveSuccessionLaw()` 返回 Content 的 `InheritanceLaw` |
| `FaithSystem` 运行态 | Faith | 不入档（既有设计）；由 `FaithPersistence` 快照 overlay | 保持 `FaithPersistence.cs:19-67`、`SaveSystem.cs:398/496` 行为；可后续改 `IFaithSnapshot`，本批不强制 |
| `CultureData` | Culture | 直接序列化到 `SaveData.cultures` | 字段（含 `InheritanceLaw defaultSuccessionLaw`）类型变化不影响 JsonUtility 字段名；不得改字段名/删除 `[Serializable]` |

### 5.2 硬性要求

1. **不改字段名、不改嵌套结构、不删除 `[Serializable]`、不改 `RealmDTO.From/To` 的语义（`State/RealmDTO.cs:79-80/136-137`）**；JsonUtility 按字段名/公有字段序列化，程序集/namespace 不影响 JSON。
2. **不升 `SaveVersion`**（当前 16）；拆分是纯代码组织，不产生存档格式变化。
3. **回归门禁**：`SaveSystemRoundTripTests`、`LegacySaveCompatibilityTests`、`FaithPersistenceTests`、`LawDecreePipelineTests`（RealmDTO 往返）、`CultureStageEvolutionDataTests`；full EditMode/PlayMode。
4. **迁移顺序**：先 run 一次"旧档加载/新档保存"基线，再动 asmdef；每次 asmdef 翻转后重跑；出现 JSON 字段差异时优先回滚（不要改字段名兼容）。

---

## 6. 分步实施路线（推荐顺序：Laws → Culture → Faith）

> 每个 Phase 都拆成 **A（同程序集内 host 反转，不改 asmdef）** 与 **B（asmdef 翻转，改 JSON 引用）** 两半；A 绿后再做 B，便于回滚。
> "改文件数"为粗估（含 .cs/.asmdef/.meta 移动；不含仅 asmdef refs 的消费者）。

### Phase 0：前置（不新增系统 asmdef）

1. **完成 `Definitions → Content.Definitions` 改名一致性**（task-71）：确保只有一套 namespace，避免同时存在 `MyriadCreation.Simulation.Definitions` 与 `MyriadCreation.Content.Definitions`；Faith 中 13 个残留 using 清理。
2. **定义/内容下沉 Content**：
   - Faith `Definitions/*`（9 文件）+ `State/{Doctrine,DoctrinePool}` + `ReligionCatalog` → `Content`（或 `Content/Definitions/Faith/`）；
   - `Simulation.State/{Deity,EcclesiasticalTitle,OfficeTitleEntry}` → Content；
   - `Laws/{InheritanceScope,Branch,Gender,Age,LandInheritanceMode,TitleInheritanceMode}` + `InheritanceLaw` 配置部分 → Content；`InheritanceLaw` 的 `DetermineHeir/GetHeirOrderedPool/IsSibling` 移到 `Simulation` host `InheritanceHeirSelection`。
3. **Content 增加 `IContentLookup`**（`Content` 程序集，refs Core）：`TryGetReligion/TryGetLanguage/TryGetTradition/TryGetEthnicGroup/TryGetCulture` 等；host 用现有 `ContentRegistry/ContentResolvers` 实现适配；**不要求把 Modding 迁 Content**（可后续 B2 完整做）。
4. **Core.Contracts 增加 context 接口**（`IFaithContext/ICultureContext/ILawContext` + 必要 DTO）；host 在 `Simulation` 实现。
5. **host 粘合文件从系统文件夹移到 `Simulation`**（保持同程序集，可保留原 namespace）：
   - Faith：`FaithContracts`、`GreatHolyWarSystem`、`WarfareReligionSchedule`、`GreatHolyWarState`；
   - Culture：`CultureContracts`、`CultureSchedule`、`CultureStageEvolutionSystem` 的 host 方法（`FormRealmFromActor/ApplyPathBonuses/MonthlyTick/HasUrbanCenter`）；
   - Laws：`LawContracts`、`LawDecreeView`、`LawEffectRuntime`、`LawSystem`、`JudicialSystem`、`DecreePropagationSystem`、`LawCompositionMigration`、`LawReform`（后两者也可先改 `ILawContext`）。
   - 目标：三个系统文件夹内不再 `using MyriadCreation.Simulation.State/Systems/WorldState/Actors/Modding/Scheduling`（编译通过，asmdef 未动）。
   - 预估：~40-60 文件（含移动/using），风险低（无 asmdef 变化）。
6. **门禁**：编译 + 全量 EditMode/PlayMode（行为不变）；存档 roundtrip 绿；作为 **R0 回滚点**（git commit）。

### Phase 1：B6 Laws（先做，下层）

1. **A（同程序集）**：
   - `LawAndDecreeSystem` 的 12 个 `RealmData` public API 改为 `ILawContext` + `RealmLawState/RealmDecreeState/LawComposition` 显式参数；host 侧 `RealmData` → 适配器解包。
   - `LawReform/LawCatalog/LawEffects` 的 RealmData 依赖清理；`LawEffectRuntime`/`JudicialSystem`/`DecreePropagation`/`LawDecreeView`/`LawContracts`/`LawSystem` 已移 host。
   - 移除系统文件夹对 `State/Systems/WorldState` 的 using；编译 + `Law*Tests`（`LawAndDecreeTests/LawDecreePipelineTests/LawEffectRuntimeTests/LawModuleTests/LawContentJsonTests/FactionLawChangeTests/SystemContractsTests`）。
2. **B（翻转 asmdef）**：
   - 新增 `Simulation/MyriadCreation.Simulation.Laws/MyriadCreation.Laws.asmdef`：`references = ["MyriadCreation.Core","MyriadCreation.Content"]`；`name = MyriadCreation.Laws`。
   - 给 `Simulation` 加 `MyriadCreation.Laws` 引用；`UI`、`Tests` 加；`Infrastructure`/`Rendering` 仅在直接使用 Laws 类型时加（当前分析 Infra 无需，Rendering 无）。
   - 全仓编译 + 全部 law tests + save roundtrip；作为 **R1 回滚点**。
3. **预估**：~25-35 文件（含移动 8、`LawAndDecreeSystem` 大改 1、host/上下文 5、asmdef refs 5-7）；风险中（RealmData API 面广，tests 13 文件需适配）。

### Phase 2：B5 Culture

1. **A**：
   - `CultureStageEvolutionSystem` 只留 `EvaluateEvolution`（纯逻辑）；host 方法移到 `Simulation.WorldState/CultureHost.cs`。
   - `CultureData` 去 Modding/State/Laws using：`IContentLookup` 参数化内容方法、`InheritanceLaw`/`OfficeTitleEntry` 已 Content。
   - `CultureLineage/CultureTransition` 内容查询改 `IContentLookup`；`SocialStratificationCalculator` 调用改 `ICultureContext`。
   - 编译 + `CultureStageEvolutionDataTests/LanguageFamilyTests`。
2. **B**：新增 `MyriadCreation.Culture.asmdef`（refs Core, Content）；`Simulation` 加引用；`UI/Infrastructure/Tests` 加引用；编译 + 目标测试 + save（`SaveData.cultures`）+ full；**R2 回滚点**。
3. **预估**：~20-30 文件（9 系统文件 + host 2 + asmdef refs 6-8 + 测试）；风险中低（纯逻辑/数据多，host 只有 1 文件）。

### Phase 3：B4 Faith

1. **A**：
   - 定义/目录已 Content；`FaithSystem` 的 char helper 改 `IFaithContext`；`CanonizationSystem` 改 context；`ReligionSimulationEventHandler` 保持（Core.Events）。
   - host 文件已在 Simulation（Phase 0）；确认 Faith 文件夹只剩 `FaithSystem/CanonizationSystem/ReligionSimulationEventHandler`（+可保留 `FaithContracts` 若不愿移，但那样需 refs Simulation → 不允许，必须移）。
   - 编译 + `FaithPersistenceTests/RestoredSystemTests` + Modding 内容加载测试（`ContentRegistry` 改为 Content 定义后回归）。
2. **B**：新增 `MyriadCreation.Faith.asmdef`（refs Core, Content）；`Simulation` 加引用；`Infrastructure`（FaithPersistence/FaithSaveData）、`Tests` 加引用；`Rendering` 仅当还直接用 Faith 类型时加（目标为 0）；`UI` 过渡加；编译 + full + save；**R3 回滚点**。
3. **预估**：~30-45 文件（18 文件重分布/移动 + host 4 + asmdef refs 7-9 + Modding/UI/Infra 引用）；风险中（外部消费者最多，但都是只读/引用层调整）。

### Phase 4：收尾与门禁固化

- 更新 `SYSTEM_OWNERSHIP.md` 的 asmdef DAG/度量；把"三系统 asmdef refs 只允许 Core+Content"写成 CI/脚本检查（`grep references`）。
- 跑 `docs/architecture` 的审计命令，记录新基线：`UI/Rendering` 对三系统的直接 import 数应降到 0（除 UI 过渡契约）；后续 B7/B8 再收 UI/Rendering。
- 可选：把 `Core.Contracts` 提升为 `App.Contracts` asmdef（B3 后），文件 namespace 不变。

### 6.5 回滚点汇总

| 点 | 时机 | 回滚动作 |
|---|---|---|
| R0 | Phase 0 完成（定义下沉 + host 移出，无 asmdef） | 反向移动文件/恢复 using；无 asmdef 变化，风险最低 |
| R1 | Laws asmdef 翻转后（A 已绿） | 删除 `MyriadCreation.Laws.asmdef`，移除消费者 refs；文件仍在原地可继续工作（namespace 未变） |
| R2 | Culture asmdef 翻转后 | 同上（删除 Culture asmdef/refs） |
| R3 | Faith asmdef 翻转后 | 同上 |
| 任意 | 存档/行为回归失败 | 优先回退到上一个 R 点；**不要**通过改存档字段名/版本"修"兼容性 |

---

## 7. 每系统"最小首步 3 个接口/类型移动"（下一次实现卡可直接照做）

> 均**先保持同一程序集**（只移动文件/加接口，不创建 asmdef）；三小步都编译绿后再做 §6 的 B 翻转。

### 7.1 Laws（B6）

1. **移 host glue**：`LawContracts.cs`、`LawDecreeView.cs`、`LawEffectRuntime.cs`、`LawSystem.cs`、`JudicialSystem.cs`、`DecreePropagationSystem.cs`、`LawCompositionMigration.cs`、`LawReform.cs` 从 `Simulation.Laws/` 移到 `Simulation/WorldState/LawHost/`（保持 namespace 暂不变，更新 `using`/调用点）。验收：Laws 文件夹不再 import `WorldState/Systems`（除纯规则依赖）。
2. **定义 `ILawContext` + `LawRealmSnapshotDto`（Core.Contracts）**：先只覆盖 `GetSupremeRulerId/GetTreasury/GetPrestige/GetLegitimacy/GetCentralization/GetAuthority/GetRealmInnovationIds` + `GetCharacterName`；把 `LawAndDecreeSystem` 的 12 个 `RealmData` API 改为 `(ILawContext ctx, RealmLawState state, …)`（或 host 包装 `RealmLawState` 的函数）；`Simulation.WorldState` 增加 `LawRealmContext` 适配 `RealmData`。验收：`GameWorld.Simulation.cs:760` 等 1-2 个 host 调用点编译通过。
3. **继承/州府逻辑下沉**：`InheritanceLaw` 的 `DetermineHeir/GetHeirOrderedPool/IsSibling`（`:143-235`）移到 `Simulation/WorldState/InheritanceHeirSelection.cs`；`InheritanceLaw` 配置+`DistributeLand/GetName` 与 6 个枚举移到 `Content`（或 Phase 0 已做）。更新 `SuccessionSystem.cs:33/205`、`GreatHolyWarSystem`（若 host）、`CultureData.cs:82`。验收：`Law` 文件夹不再有 `CharacterData`（除注释）。

### 7.2 Culture（B5）

1. **host 方法移出**：`CultureStageEvolutionSystem.cs:179-353`（`FormRealmFromActor/ApplyPathBonuses/MonthlyTick/HasUrbanCenter`）移到 `Simulation/WorldState/CultureHost.cs`；`CultureSchedule.cs`+`CultureContracts.cs` 移到 host。Culture 只保留 `EvaluateEvolution`（`:24-176`）+ `CultureStageCatalog/Def`。验收：Culture 文件夹无 `GameWorld/RealmData/SettlementData` 引用。
2. **内容查询接口化**：`IContentLookup`（Content）+ host 适配器；把 `CultureData.cs:114/133`、`CultureLineage.cs:58`、`CultureTransition.cs:71` 的 `ContentResolvers/ContentRegistry` 调用改成 `IContentLookup` 参数或 `CultureContentExtensions`；`OfficeTitleEntry` 移 Content。验收：Culture 文件夹无 `Simulation.Modding` using。
3. **`ICultureContext`（Core.Contracts）**：覆盖 `GetRealmIds/GetRealmCultureId/GetRealmInnovationIds/GetSocialStratification/HasUrbanCenter/CurrentDay`，host 用 `SocialStratificationCalculator` 实现；`CultureStageEvolutionSystem.MonthlyTick` 包装函数改调 context。验收：Culture 文件夹无 `Simulation.State/Systems/Actors` using。

### 7.3 Faith（B4）

1. **host 文件移出**：`FaithContracts.cs` → host；`GreatHolyWarSystem.cs`、`WarfareReligionSchedule.cs`、`GreatHolyWarState.cs` → `Simulation/WorldState/FaithHost/`（战争/调度）；验收：Faith 文件夹无 `WorldState/Scheduling` using。
2. **定义/内容下沉**：`Faith/Definitions/*`（9 文件）+ `State/{Doctrine,DoctrinePool}` + `ReligionCatalog` → `Content`；`Deity/EcclesiasticalTitle` 从 `Simulation.State` 移 Content；`Modding/ContentRegistry/Resolvers/Runtime` 的 `using ...Faith` 改 `Content.Definitions`；`UI/Rendering/Infrastructure` 对 `ReligionMapLevel/ReligionCatalog` 的引用改 Content。验收：Faith 文件夹只剩 `FaithSystem/CanonizationSystem/ReligionSimulationEventHandler`。
3. **`IFaithContext`（Core.Contracts）**：`TryGetCharacterVirtueTags/SinTags`、`GetRealmFaithId`、`IsCharacterAlive`；`FaithSystem.GetVirtueScore/GetSinScore(CharacterData)` 与 `CanonizationSystem` 改为 `(int characterId, IFaithContext)` 或移到 host `FaithCharacterScoring`。验收：Faith 文件夹无 `CharacterData/CharacterManager` 引用。

---

## 8. 风险与开放问题

| 风险 | 说明 | 缓解 |
|---|---|---|
| task-71 改名进行中 | `Simulation.Definitions` 与 `Content.Definitions` 当前并存（旧目录仅 12 类型，新目录 95 类型；Faith 13 文件仍 import 旧 namespace） | Phase 0 第一项先统一；本方案所有新引用写 `MyriadCreation.Content.Definitions` |
| `ContentRegistry/Resolvers` 仍在 Simulation | Culture/Faith 若直接引用 Modding 会与独立目标冲突 | Phase 0 `IContentLookup`（Content）+ host 适配；Modding 整体迁 Content 留给后续 B2 |
| `LawAndDecreeSystem` API 面大 | 12 个 `RealmData` public API，13 个测试文件直接调用 | 保留 host 包装函数（`Simulation.WorldState.LawHost`）供旧 tests 迁移；先在 A 阶段内部改签名、tests 跟随；必要时旧签名 `[Obsolete]` 过渡 |
| `CultureData` 内容方法 | `ContentResolvers` 静态依赖 | `IContentLookup` 参数化/扩展方法；避免在 `[Serializable]` 数据类里持有服务引用 |
| Faith 定义迁移面 | `Modding` + UI/Rendering/Infra/Save 同时引用 | 一次性把定义下沉 Content；Rendering 的 `ReligionMapLevel/ReligionCatalog` 引用改 Content 后可去掉 Faith 引用 |
| 命名空间保持带来的"名不副实" | 文件在 host 文件夹但 namespace 仍是 Laws/Faith 等 | 允许一轮；B 阶段后另开卡统一 namespace（纯改名，低风险） |
| asmdef 引用遗漏 | UI/Rendering/Infra/Tests 直接使用新程序集类型时未加 refs | 编译门禁 + 附录 A 的 `grep references` 反引检查；逐消费者清单见 §1.4 |
| 存档兼容 | `RealmDTO/RealmData` 字段类型程序集变化 | 不改字段名/不升版本；save roundtrip 门禁 |
| `World` 是否被拉入 | 若把 `RealmData/CultureData` 放进 World 会破坏"地图模型"边界 | 明确 `World` 不承接状态；状态留各自系统/Simulation |

**开放问题（需 Lead/后续卡决定）**
1. `LawEffectRuntime` 是放 host（Simulation）还是留在 Laws 用 `ILawContext` 读/写 realm 数值？本方案倾向 **host**（效果应用是粘合，Laws 只保留纯计算），实现卡可按此执行。
2. `FaithSaveData`/`FaithPersistence` 是否本批就改 `IFaithSnapshot`（收益：Infrastructure 不再依赖 FaithSystem 细节），或先只加 asmdef refs 后续再改；本方案允许后者。
3. `UI/Rendering` 的目标是只依赖 `Core/World/Contracts`（B7/B8 范围）；本批允许过渡性 refs，但需在 B7 前移除。
4. 是否要在 Phase 4 把 `Core.Contracts` 提升为独立 `App.Contracts` asmdef：取决于 B3 App 组合根进度，本批不做。

---

## 附录 A：快照复现命令（只读）

```bash
cd MyriadCreation/Assets

# 1) asmdef 依赖
for f in $(find Scripts UI -name "*.asmdef"); do echo "--- $f"; cat "$f"; done

# 2) 三系统文件/行数
for d in Faith Culture Laws; do
  echo "$d: $(find Scripts/Simulation/MyriadCreation.Simulation.$d -name '*.cs' | wc -l) files, $(wc -l Scripts/Simulation/MyriadCreation.Simulation.$d/**/*.cs 2>/dev/null | tail -1)"
done

# 3) 系统目录的外部 using（文件数）
grep -rho "using MyriadCreation\.Simulation\.[A-Za-z.]*;" Scripts/Simulation/MyriadCreation.Simulation.Faith | sort | uniq -c

# 4) 跨边界调用点（示例）
grep -rn "RealmData" Scripts/Simulation/MyriadCreation.Simulation.Laws | grep -v "///"
grep -rn "GameWorld" Scripts/Simulation/MyriadCreation.Simulation.Culture Scripts/Simulation/MyriadCreation.Simulation.Faith
grep -rn "CharacterData" Scripts/Simulation/MyriadCreation.Simulation.Faith Scripts/Simulation/MyriadCreation.Simulation.Laws

# 5) asmdef 反引门禁（拆分后应输出空：系统 asmdef refs 不含 Simulation）
for n in Faith Culture Laws; do
  f="Scripts/Simulation/MyriadCreation.Simulation.$n/MyriadCreation.$n.asmdef"
  grep -q "MyriadCreation.Simulation" "$f" && echo "REVERSE DEP: $f" || true
done
```

## 附录 B：关键证据索引

- `RealmData` 法律字段：`Simulation/MyriadCreation.Simulation.State/RealmData.cs:72,74,76`
- `RealmDTO` 法律字段：`.../State/RealmDTO.cs:17,19,21`；From/To：`:79-80,136-137`
- Laws → RealmData 入口：`Laws/LawAndDecreeSystem.cs:329,375,386,397,406,502,507,578,585,600,627,825`
- Laws 视图/配置 host 依赖：`LawDecreeView.cs:50`、`LawReform.cs:10,19`、`LawCompositionMigration.cs:46`、`LawEffectRuntime.cs:60/83/110/119`、`LawContracts.cs:16`、`LawSystem.cs:10`
- Culture host 依赖：`CultureStageEvolutionSystem.cs:179,299,333`；内容查询：`CultureData.cs:114,133`、`CultureLineage.cs:58`、`CultureTransition.cs:71`；继承/头衔字段：`CultureData.cs:82,84`
- Faith host 依赖：`CanonizationSystem.cs:14,31`、`FaithSystem.cs:98,118`、`GreatHolyWarSystem.cs:52,61,76,91`、`WarfareReligionSchedule.cs:13`、`FaithContracts.cs:15`
- 外部消费者：`Simulation.Systems/CharacterManager.cs:85`、`ThoughtManager.cs:15/42`、`Systems/SocialStratificationCalculator.cs:38`、`Systems/RealmSituationBuilder.cs:142`、`Modding/ContentRegistry.cs:7-8,28-37,63`、`Modding/ContentRuntime.cs:7-8,90,215,258,464-478`、`Rendering/MapRenderer.Colors.cs:215-221,297`、`Infrastructure.Save/FaithPersistence.cs:19-55`、`SaveData.cs:38,70`、`SaveSystem.cs:398,496`、`UI/UIManager.cs:491`、`RealmOverviewText.cs:13,29-31`、`SettlementPeopleText.cs:76/81`
- GameWorld host：`GameWorld.Core.cs:79,109,111,195`、`GameWorld.Queries.cs:105,262`、`GameWorld.Simulation.cs:754-772,781-800,811,833-838,1098-1124`、`GameWorld.Succession.cs:336-338`
- 当前 asmdef：`Core/core.asmdef`、`World/world.asmdef`、`Content/MyriadCreation.Content.asmdef`、`Simulation/sim.asmdef`、`Infrastructure/MyriadCreation.Infrastructure.asmdef`、`Rendering/MyriadCreation.Rendering.asmdef`、`UI/MyriadCreation.UI.asmdef`、`App/MyriadCreation.App.asmdef`
