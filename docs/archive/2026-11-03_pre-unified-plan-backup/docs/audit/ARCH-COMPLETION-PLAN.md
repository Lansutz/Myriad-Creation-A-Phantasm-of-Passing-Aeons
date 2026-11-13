# ARCH-COMPLETION-PLAN：程序集解耦"彻底落实"审计与批次计划

## 执行状态摘要（Round 35 架构同步，2026-11-02）

> 本文件 §1–§9 为 task-64 审计原文（含旧批次 B0–B6），**不改历史内容**；当前执行状态以本摘要与仓库根
> `docs/architecture/CURRENT_ARCHITECTURE.md`、`docs/architecture/SYSTEM_OWNERSHIP.md` 为准。

**旧编号（本文件 §5：B0–B6）——全部完成**
- B0 清理非法 `*.csproj` 副本 ✅；B1 平铺目录物理归位 42 文件 ✅（task-43）；
- B2 结算经济簇 → `World.Settlement`，V1–V3 跨层违规归零 ✅（task-44）；
- B3 删死代码 `MapSaveSystem` + Infrastructure 移除 Rendering，V4 归零 ✅（task-45）；
- B4 Presentation 拆分（`Simulation.Presentation` 已不存在）✅（task-47）；
- B5 灰区 `Core.Simulation` → `Simulation.Scheduling` ✅（task-46）；
- B6 TMP 示例清理 + Editor 收尾 ✅（TMP 示例 `.cs` = 0）。

**新编号（SYSTEM_OWNERSHIP：B1–B8）——全部完成**
- B1 owner 文件夹归位 ✅；B2 `MyriadCreation.Content` 独立（refs=Core）✅；
- **B3 `MyriadCreation.App` + `App.Contracts` 抽取——Step 1–4（服务层 + 迁移）✅**；
- B4 Faith / B5 Culture / B6 Laws 独立 asmdef（refs 均为 Core+Content）✅；
- B7 World/Rendering 视图契约：Rendering 去 Simulation/App.Contracts/World，refs 恰为 Core+Content ✅；
- **B8 架构门禁 + UI 4d ✅**：UI 去 Simulation/Laws/Faith/Culture 引用、删 10 个 Legacy、UI refs 收敛；
  `AsmdefArchitectureGateTests` 已删除 `KnownTransitionEdges` 白名单并新增 UI 家族硬边界断言；无环、硬违规 0。

**当前实测（15 asmdef）**
- 运行时 12：Core / Content / Laws / Faith / Culture / World / Simulation / Rendering / Infrastructure / App.Contracts / App / UI；
  工具/测试 3：Editor / Tests / Tests.PlayMode（门禁豁免）。
- `Laws/Faith/Culture refs = {Core, Content}`；`Rendering refs = {Core, Content}`。
- `UI refs = {Core, Rendering, UGUI, TMP, Infrastructure, World, Content, App, App.Contracts}`，**无 Simulation 家族**；
  `Assets/UI` 内 Simulation 家族 using = 0，Legacy `.cs` = 0；App 不引 UI，UI 必引 App。
- 门禁：无白名单、无环、无向上引用；UI 边界硬断言通过。

**Round 35 基线**：EditMode **552/552**、PlayMode **9/9**（Lead 执行）。
**主要遗留**：GameWorld 组合根 hooks 进行中；UI 地块/角色面板契约补齐中；App.Contracts 仍引 Simulation/World；
Modding 仍在 Simulation；Editor/Tests 门禁豁免。详见 `CURRENT_ARCHITECTURE.md` §5。

---

- 分支 `architecture-2-0-refactor`；本卡：只读审计 + 无风险清理（删除构建残留），**未移动任何 .cs**
- 扫描口径：`Assets/` 下 874 个 `.cs`（**34 个属第三方 TextMesh Pro 示例、编入默认 Assembly-CSharp**；其余 840 个编入 8 个工程 asmdef：Core 66 / World 88 / Simulation 555 / Infrastructure 23 / Rendering 10 / UI 16 / Editor 10 / Tests 69 / Tests.PlayMode 3）
- 判定规则：
  1. **层级错位**：文件所在"程序集根目录"对应 asmdef 层 ≠ 命名空间第 2 段层（Core/World/Simulation/Infrastructure/Rendering/UI）
  2. **语义错层**：层级一致但代码职责属于更上层（表现层/编辑器/平台层）
  3. **平铺/未镜像**：asmdef 根目录直接放 `.cs`，或目录名与命名空间尾段不对应
  4. 所有"当前编入哪个 asmdef"由最近祖先 `.asmdef` 判定；引用/使用方由全仓类型名 grep 得到

## 1. 结论摘要

1. **层级错位仅 4 个文件 / 3 个目录**（Lead 已侦察的 3 处已全部覆盖，另有 1 处同簇文件）：
   `Core/MyriadCreation.Simulation.Definitions`(1)、`Core/MyriadCreation.Simulation.Economy`(1)、`World/MyriadCreation.Simulation.Systems`(2)。
2. **跨层违规共 4 处、3 类**，且全部由上述错位造成（`Core→Simulation` 1 处、`World→Simulation` 2 处）；`Infrastructure→Rendering` 仅 1 处且来自死代码 `MapSaveSystem`，**可完全解耦**。
3. **`Simulation/MyriadCreation.Simulation.Presentation` 属"语义错层"而非层级错位**：17 个文件中，12 个只被 UI（7 类型）+ 测试使用、2 个被 Simulation 逻辑使用（`PlaceNameGenerator`、`LawComponentNames`）、3 个是 0 引用死代码；因此不能整体平移，需按消费方拆分。
4. **灰区**：`Core/MyriadCreation.Core.Simulation`（11 文件）按"命名空间第 2 段"算合规，但其类型只被 Simulation(41 类型引用)+Tests(9) 使用、Core 自身 0 使用 → 实质是"仿真运行时原语"，建议迁移或改名（需 Lead 决策）。
5. **平铺目录 42 文件**待整理：Infrastructure/Audio(2)+Save(21)、Rendering 根 6 + Overlays 1 + Terrain 2（后两者目录名与命名空间不符）、Editor 根 7（两个命名空间混放）、UI 根 3。
6. **建议 6 个批次执行**，每批一次编译验证；**Batch 2 完成后跨层违规归零**（`Core→Simulation`、`World→Simulation` 全部消失），Batch 3 完成后 `Infrastructure→Rendering` 归零。
7. 本卡已清理构建残留 **6 文件**（3 个 `.csproj` + 3 个 `.csproj.meta`，均确认未被 git 跟踪）；未删 `TEMPdecompiled-sim/`（556 文件/2.7MB，反编译参考物，非构建输出，见 §8）。

## 2. 错位全量清单

### 2.1 A 类：层级错位（命名空间层 ≠ 程序集层）

| # | 目录（相对 Assets） | 文件数 | 文件 | 命名空间 | 当前编入 asmdef | 使用方（文件:行） |
|---|---|---|---|---|---|---|
| A1 | `Scripts/Core/MyriadCreation.Simulation.Definitions` | 1 | `DerivedEconomicProperties.cs` | `MyriadCreation.Simulation.Definitions` | **MyriadCreation.Core** | `Scripts/Core/MyriadCreation.Simulation.Economy/SettlementMarket.cs:82`（同错位簇）、`Tests/EditMode/DerivedEconomicPropertiesTests.cs:15-43` |
| A2 | `Scripts/Core/MyriadCreation.Simulation.Economy` | 1 | `SettlementMarket.cs` | `MyriadCreation.Simulation.Economy` | **MyriadCreation.Core** | `Scripts/Infrastructure/Save/SettlementMarketSaveData.cs:23,35-37`；`Scripts/Simulation/…/Systems/SettlementProduction.cs:9`；`Scripts/World/MyriadCreation.World.Settlement/SettlementData.cs:35,339,343`；`Tests/EditMode/{DynamicGoodsTests,SettlementFoundingAndMarketSaveTests,SettlementMarketTests,SettlementPersistenceTests}` |
| A3 | `Scripts/World/MyriadCreation.Simulation.Systems` | 2 | `SettlementEconomy.cs`、`SettlementEconomySnapshot.cs` | `MyriadCreation.Simulation.Systems` | **MyriadCreation.World** | `SettlementData.cs:343`（`SettlementEconomy.GetWarehouseCapacity`）；`Tests/EditMode/SettlementEconomyTests.cs:14-90`；`SettlementEconomySnapshot` 仅被 `SettlementEconomy.cs` 内部使用 |

**影响**：A1/A2 的类型实际编译进 **Core** 程序集（最低层），A3 编译进 **World**；命名空间却声明为 Simulation 层。当前能编译，是因为**消费者所在的程序集恰好都低于/等于这些错误归属**（Core≤Simulation、World≤Simulation），属于"靠错误归属掩盖的层级违例"，一旦有人按命名空间加 asmdef 引用约束即暴露。

### 2.2 B 类：语义错层——`Simulation/MyriadCreation.Simulation.Presentation`（17 文件）

程序集层 = Simulation（合规），但内容是**表现层文本/模型**，按消费方可拆 3 组：

| 组 | 文件 | 数量 | 消费方（文件:行） | 建议去向 |
|---|---|---|---|---|
| B-UI | `ClassPanelText.cs`、`ClassNames.cs`、`FactionNames.cs`、`JunctureNames.cs`、`RealmDivisionText.cs`、`RealmOverviewText.cs`、`ReligionPanelText.cs`、`SettlementPeopleText.cs`、`PopulationCompositionCategory.cs`、`PopulationCompositionPanelModel.cs`、`PopulationCompositionRow.cs`、`SiteTypeLabels.cs` | 12 | `UI/Panels/SocietyPanel.cs:25`（using）；`UI/Common/UIManager.Panels.cs:32`；`Tests/EditMode/{PopulationCompositionTests.cs:4; SiteTypeLabelsTests.cs:2; AdminDivisionUiTextTests.cs:7; RealmOverviewTextTests.cs:7}` | `Assets/UI/MyriadCreation.UI.Presentation/`，命名空间 `MyriadCreation.UI.Presentation`（UI asmdef 已引用 Simulation/World/Core） |
| B-Sim | `PlaceNameGenerator.cs`（`WorldState/GameWorld.cs` 使用）；`LawComponentNames.cs`（`State/LawComposition.cs:4`、`Systems/LawReform.cs:1` 使用） | 2 | 同左 | 分别并入 `Scripts/Simulation/MyriadCreation.Simulation.Generation/`（或 `…Settlement/`）与 `…Simulation.Laws/`，命名空间随之改为对应层 |
| B-Dead | `AppearancePresenterRegistry.cs`、`IAppearancePresenter.cs`、`NullAppearancePresenter.cs` | 3 | **全仓 0 引用**（含非 .cs 资产 0 命中；三者仅互相引用） | 删除（死代码，见 Batch 3） |

> 注：`ClassNames.cs` 仅被同目录 `ClassPanelText.cs` 使用（随 UI 组一起移动）；`PopulationCompositionRow` 仅被 `PopulationCompositionPanelModel` 使用（随 UI 组一起移动），二者不算死代码。

### 2.3 C 类：灰区——`Scripts/Core/MyriadCreation.Core.Simulation`（11 文件，编入 Core）

- 文件：`IRuntimeDefinition / ISimulationScheduler / SimulationScheduler / SimulationBudgetGovernor / SimulationCadence / SimulationDirtySet / SimulationEventDirtyBridge / SimulationLane / SimulationResult / SimulationTickContext / SimulationTimeSchedule`
- 命名空间 `MyriadCreation.Core.Simulation`：按第 2 段算 **合规**（Core），且 `using MyriadCreation.Core.Simulation` 出现在 **24 个文件**（19 个 `Simulation/…Systems`、2 个 `…WorldState`、3 个 Tests）——**Core 自身 0 消费**。
- 判定：**灰区**。两条路线（需 Lead 决策）：
  - C-1（推荐，与"命名即层级"一致）：整体迁到 `Scripts/Simulation/MyriadCreation.Simulation.Scheduling/` + 命名空间改 `MyriadCreation.Simulation.Scheduling`，更新上述 24 个 using（机械改动，无 asmdef 变更：Simulation 已引用 Core/World）。
  - C-2（保守）：留在 Core，但命名空间改 `MyriadCreation.Core.Runtime`，同样需改 24 个 using；语义上把这套调度原语定位为"Core 运行时基础设施"。

### 2.4 D 类：平铺 / 未镜像目录（42 文件）

| 程序集根 | 平铺文件数 | 明细 | 建议 |
|---|---|---|---|
| `Scripts/Infrastructure` | 23 | `Audio/`(2)、`Save/`(21)，命名空间分别是 `MyriadCreation.Infrastructure.Audio/.Save` | 移动为 `Infrastructure/MyriadCreation.Infrastructure.Audio/`、`…Infrastructure.Save/` |
| `Scripts/Rendering` | 9 | 根 6（`MapRenderer*.cs`×4 partial、`MapLayerRegistry.cs`、`IMapEditController.cs`）+ `Overlays/MapLayerConfig.cs`(1，命名空间却是 `MyriadCreation.Rendering`)+ `Terrain/{CyowariMapStyle,SphericalMapRenderer}.cs`(2，命名空间也是 `MyriadCreation.Rendering`) | 全部归入 `Rendering/MyriadCreation.Rendering/`（命名空间不变）；`Overlays/`、`Terrain/` 空目录连同 `.meta` 删除 |
| `Assets/Editor` | 7 | 2 个 `MyriadCreation.EditorTools`（`AutoOpenMainScene.cs`、`GraphicsApiFix.cs`）+ 5 个 `MyriadCreation.Editor`（`BatchCompileCheck`、`ResaveScene`、`TmpSettingsCreator`、`UiCleanup`、`UiDiagnostics`） | 分别移入 `Editor/MyriadCreation.EditorTools/`、`Editor/MyriadCreation.Editor/` |
| `Assets/UI` | 3 | `Bootstrap.cs`、`GameManager.cs`、`SceneFlowController.cs`（命名空间 `MyriadCreation.UI`） | 可选：移入 `UI/MyriadCreation.UI/`（`UI/Common`、`UI/Panels` 已镜像） |

> 其余程序集根（Core/World/Simulation）无平铺 `.cs`；`Rendering/Editing/RuntimeMapEditingController.cs` 目录=命名空间，正确。

### 2.5 E 类：程序集外代码（记录，不在本卡范围）

`Assets/TextMesh Pro/Examples & Extras/Scripts/` 下 **34 个 `.cs`** 无 asmdef 祖先 → 编入默认 `Assembly-CSharp`。属第三方示例，建议后续整体移除或加 asmdef 隔离（不影响解耦，但会污染默认程序集）。

## 3. 依赖影响与跨层违规

### 3.1 跨层违规清单（全仓 grep 结果）

| # | 违规 | 证据（文件:行） | 当前为什么能编译 | 修复批次 |
|---|---|---|---|---|
| V1 | Core → Simulation | `Scripts/Core/MyriadCreation.Simulation.Economy/SettlementMarket.cs:3` `using MyriadCreation.Simulation.Definitions;` | 被 using 的 `DerivedEconomicProperties` 也被错编进 Core 程序集（A1） | Batch 2 |
| V2 | World → Simulation | `Scripts/World/MyriadCreation.World.Settlement/SettlementData.cs:4` `using MyriadCreation.Simulation.Economy;` | `SettlementMarket` 实际编进 Core（A2），World 引用了 Core | Batch 2 |
| V3 | World → Simulation | `Scripts/World/MyriadCreation.World.Settlement/SettlementData.cs:5` `using MyriadCreation.Simulation.Systems;` | `SettlementEconomy` 实际编进 World（A3） | Batch 2 |
| V4 | Infrastructure → Rendering | `Scripts/Infrastructure/Save/MapSaveSystem.cs:13` `using MyriadCreation.Rendering;`（`MapRenderer` 字段 :45、构造 :50、导出 :529） | Infrastructure asmdef 显式引用 Rendering（允许但非必要） | Batch 3 |

> 除以上 4 处外，全仓没有其它 `Core/World` 引用更高层命名空间的文本（含全限定写法）；`using MyriadCreation.World` in Core = 0。

### 3.2 各错位簇"移动后是否可行"

| 簇 | 目标程序集（建议） | 目标 asmdef 现有 references 是否足够 | 原程序集是否仍使用（需同时处理） |
|---|---|---|---|
| A1+A2+A3 结算经济簇 | **World**（`MyriadCreation.World.Settlement`） | ✅ World 引用 Core（结算簇只用 `System` + Core 枚举/数据） | Core/World 的引用迁走后归零；Infrastructure（引用 World ✅）、Simulation（引用 World ✅）、Rendering 无需改 |
| B-UI 表现层 (12) | **UI**（`MyriadCreation.UI.Presentation`） | ✅ UI 引用 Core/World/Simulation | UI 侧 2 文件改 using；Tests 改 using；Simulation 不再引用这些类型 |
| B-Sim (2) | **Simulation**（Laws / Generation） | ✅ 同程序集内移动 | 无 asmdef 变更；3 个 Simulation 文件改 using（Laws×2、WorldState×1） |
| B-Dead (3) | 删除 | — | 0 引用，无编译影响 |
| C 灰区 (11) | Simulation（C-1）或改名留 Core（C-2） | ✅ C-1: Simulation 引用 Core/World | 24 个文件改 using（19 Systems、2 WorldState、3 Tests） |

**结算经济簇为什么选 World 而不是 Simulation**：`SettlementData`（World，序列化数据，43 文件同命名空间）持有 `SettlementMarket market` 字段（:35）并调用 `SettlementEconomy.GetWarehouseCapacity`（:343）；若把 `SettlementMarket/SettlementEconomy` 迁去 Simulation，World 必须引用 Simulation（World→Simulation 违例，且 Simulation 是上层）→ 只能连带把 `SettlementData` 及整个 World.Settlement 上移，代价远大于收益。放 World（最低公共层）后，Infrastructure、Simulation、Rendering 都天然可引用，无任何跨层违例。

## 4. Infrastructure → Rendering 解耦结论：**可以，且是净收益**

- Rendering 被 Infrastructure 使用的**唯一**入口是 `Scripts/Infrastructure/Save/MapSaveSystem.cs`（`using MyriadCreation.Rendering;` + `MapRenderer` 字段/构造/PNG 导出）。
- `MapSaveSystem`（607 行，普通类非 MonoBehaviour）**全仓 0 引用**：无 `.cs` 引用、其 `.meta` GUID `b3abbcde…` 不被任何场景/资产引用。`SaveLoadPanel.cs:116` 只有一行注释提到"已废弃的 MapSaveSystem 格式"。
- 方案 A（推荐）：删除 `MapSaveSystem.cs`(+`.meta`)，从 `MyriadCreation.Infrastructure.asmdef` 移除 `"MyriadCreation.Rendering"`，Infrastructure 依赖收敛为 `Core/World/Simulation`。
- 方案 B（若仍需要 PNG 导出）：把 `ExportMapPNG`/`MapRenderer` 相关部分拆到 Rendering（或 UI）的新类，Infrastructure 只保留 JSON 存档；同样移除 Rendering 引用。

## 5. 批次执行计划（每批独立编译验证）

> 所有移动必须 **`.cs` 与 `.cs.meta` 同步移动**（保持 GUID，否则场景/资产引用丢失）；目录移动连带目录 `.meta`；删除空目录时连带其目录 `.meta`。

| 批次 | 内容 | 文件数 | 命名空间改动 | 引用更新 | asmdef 改动 | 风险 | 验证 |
|---|---|---|---|---|---|---|---|
| **B0**（本卡已做） | 删除 3 个 `*.csproj` + 3 个 `*.csproj.meta` | 6 删除 | 无 | 无 | 无 | 极低 | 已确认 0 跟踪 |
| **B1** | 纯物理移动：Rendering 根 6 + Overlays/Terrain 3 → `Rendering/MyriadCreation.Rendering/`；Infrastructure Audio 2 → `…Infrastructure.Audio/`、Save 21 → `…Infrastructure.Save/`；Editor 7 → 两个命名空间子目录；UI 根 3 → `UI/MyriadCreation.UI/`（可选） | 42 | **无**（全部保持原命名空间） | 无 | 无 | 低（GUID 保持，编译不变） | 编译 + 场景 Main.unity 加载 |
| **B2** | 结算经济簇：A1+A2+A3 共 4 文件 → `World/MyriadCreation.World.Settlement/`，命名空间改 `MyriadCreation.World.Settlement`；删除 2 个空目录及 meta | 4 移动 + 8 消费方改 using | `Simulation.Definitions`→`World.Settlement`；`Simulation.Economy`→`World.Settlement`；`Simulation.Systems`→`World.Settlement` | `SettlementData.cs`(删 2 using)、`SettlementMarketSaveData.cs`、`SettlementProduction.cs`、5 个测试 | 无 | 中低 | 编译 + EditMode 结算/经济/存档测试 |
| **B3** | 死代码：删 `MapSaveSystem.cs`+meta；删 B-Dead 3 文件+meta | 8 删除 | 无 | 无 | **Infrastructure 移除 Rendering 引用** | 低 | 编译；`git status` 净删除 |
| **B4** | 表现层拆分：B-UI 12 文件 → `UI/MyriadCreation.UI.Presentation/`；B-Sim 2 文件 → `Simulation/…Laws/`、`…Generation/`；删空目录 | 14 移动 + 10 文件改 using | `Simulation.Presentation`→`UI.Presentation`（12）/`Simulation.Laws`（1）/`Simulation.Generation`（1） | `UIManager.Panels.cs`、`SocietyPanel.cs`、`LawComposition.cs`、`LawReform.cs`、`GameWorld.cs`、5 个测试 | 无 | 中（命名空间迁移面广但机械） | 编译 + UI/文本相关 EditMode 测试 |
| **B5**（灰区，需 Lead 决策） | C-1：`Core/MyriadCreation.Core.Simulation` 11 文件 → `Simulation/MyriadCreation.Simulation.Scheduling/` + 命名空间改名；或 C-2 留 Core 改名 | 11 移动 + 24 文件改 using | C-1: →`MyriadCreation.Simulation.Scheduling` | 19 Systems/2 WorldState/3 Tests | C-1: 无；C-2: 无 | 中 | 编译 + EditMode 全量 |
| **B6**（可选） | TMP 示例 34 文件移出/加 asmdef；Editor 剩余 polish | — | — | — | — | 低 | 编译 |

**依赖顺序**：B0 → B1（无风险先做，缩小平铺面）→ B2（跨层违规归零）→ B3（Infra 解耦）→ B4（表现层）→ B5（灰区，最后做）→ B6（可选）。
**每批完成后期望状态**：B2 后 `Core→Simulation`/`World→Simulation` 违规 = 0；B3 后 `Infrastructure→Rendering` = 0；B4 后 `Simulation` 程序集不再含表现层命名空间；B5 后除灰区外所有命名空间第 2 段 = 所在程序集层。

## 6. 构建残留清理（本卡已执行 + 遗留候选）

**已删除（6 文件，均确认未被 git 跟踪）**
- `Assets/Scripts/Core/MyriadCreation.Core.csproj` + `.meta`
- `Assets/Scripts/Simulation/MyriadCreation.Simulation.csproj` + `.meta`
- `Assets/Scripts/World/MyriadCreation.World.csproj` + `.meta`
- 说明：`*.csproj` 被项目 `.gitignore`（第 35 行 `*.csproj`）忽略，故 `git status` 只看得到其 `.meta`；已用 `git ls-files` 逐个确认为空后删除。

> 补充：项目根 `MyriadCreation/Assembly-CSharp.csproj`、`MyriadCreation.{Core,World,Simulation,Infrastructure,Rendering,Editor,Tests}.csproj` 等 10 个文件是 **Unity 自动生成、被 `.gitignore` 忽略的正常文件**，不属于残留，不要删；本卡只清 `Assets/` 内的非法副本。

**遗留候选（未删，仅报告）**
| 项 | 规模 | 状态 | 建议 |
|---|---|---|---|
| `MyriadCreation/TEMPdecompiled-sim/` | 556 文件 / 2.7 MB（未跟踪） | 反编译期源码转储（非构建输出） | 参考价值已兑现（其内容已导入并做过差异核对）；建议下次清理卡整体删除或归档 |
| `MyriadCreation/.reverse/` | 9.1 MB（被根 `.gitignore` 第 30 行忽略） | 构建/导入日志 | 可删除释放空间；若排障需要可保留 |
| `MyriadCreation/*.slnx`（2 个） | 已跟踪且当前 M | 解决方案文件，不是残留 | 保留（不在清理范围） |
| `obj/`、`bin/`、`*.user`、`*.suo` | 未发现 | — | — |

## 7. 未验证项 / 执行注意事项

- **未验证**：B4 每个 Presentation 文件的完整依赖（已按类型消费方列表；逐文件 using 依赖需实现卡先核对）；B2 的 5 个测试是否还引用其他被移动命名空间中的类型（当前 grep 显示仅引用这 4 个类型，但实现卡应逐文件确认）。
- **GUID 纪律**：移动文件必须连同 `.meta` 移动；`MapRenderer`（MonoBehaviour）、`RuntimeMapEditingController` 等场景引用依赖 GUID 不变。
- **程序集身份**：若整目录移入另一 asmdef 根，Unity 会重新归属程序集；本计划 B1 全部在各自 asmdef 根内移动，程序集不变。
- 本卡未运行 Unity/编译/测试，结论以静态扫描 + 引用证据为准；各批次以 Lead 统一编译/测试验证。

## 8. 附：目录 → 程序集/命名空间全量映射（工程 840 文件）

| 目录 | 文件数 | asmdef | 命名空间 | 判定 |
|---|---|---|---|---|
| Scripts/Core/MyriadCreation.Core | 1 | Core | MyriadCreation.Core | OK |
| Scripts/Core/MyriadCreation.Core.Constants | 1 | Core | …Core.Constants | OK |
| Scripts/Core/MyriadCreation.Core.Contracts | 16 | Core | …Core.Contracts | OK |
| Scripts/Core/MyriadCreation.Core.Data | 14 | Core | …Core.Data | OK |
| Scripts/Core/MyriadCreation.Core.Dto | 2 | Core | …Core.Dto | OK |
| Scripts/Core/MyriadCreation.Core.Enums | 12 | Core | …Core.Enums | OK |
| Scripts/Core/MyriadCreation.Core.Events | 7 | Core | …Core.Events | OK |
| Scripts/Core/MyriadCreation.Core.Simulation | 11 | Core | …Core.Simulation | **灰区 C** |
| Scripts/Core/MyriadCreation.Simulation.Definitions | 1 | **Core** | …Simulation.Definitions | **错位 A1** |
| Scripts/Core/MyriadCreation.Simulation.Economy | 1 | **Core** | …Simulation.Economy | **错位 A2** |
| Scripts/World/MyriadCreation.Simulation.Systems | 2 | **World** | …Simulation.Systems | **错位 A3** |
| Scripts/World/MyriadCreation.World.* (Anchor/Biome/Climate/Editing/Generation/Hydrology/Settlement/Terrain/World) | 2~43 | World | …World.* | OK |
| Scripts/Simulation/MyriadCreation.Simulation.* (17 个域目录) | 555 | Simulation | …Simulation.* | OK |
| Scripts/Simulation/MyriadCreation.Simulation.Presentation | 17 | Simulation | …Simulation.Presentation | **语义错层 B** |
| Scripts/Infrastructure/{Audio,Save} | 23 | Infrastructure | …Infrastructure.* | 平铺 D |
| Scripts/Rendering 根 + Overlays + Terrain + Editing | 10 | Rendering | …Rendering(.*) | 平铺 D（Editing 正确） |
| UI 根 / Common / Panels | 16 | UI | …UI(.*) | 根 3 平铺（可选） |
| Editor 根 / ContentAuthoring / TerrainPainter | 10 | Editor | Editor / EditorTools 混放 | 平铺 D |
| Tests/EditMode、Tests/PlayMode | 72 | Tests / Tests.PlayMode | Tests.EditMode / Tests.PlayMode | OK |
| Assets/TextMesh Pro/Examples & Extras/Scripts | 34 | **Assembly-CSharp** | TMPro 示例 | E 类（第三方） |
