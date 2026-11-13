# 系统职责与兼容层规范（SYSTEM_OWNERSHIP）

> 卡片：**task-64（Arch 10：系统职责审计与兼容层规范）**
> 性质：**只读审计 + 规范文档**；本次唯一写入就是本文件，未移动/删除/重命名任何代码或资源。
> 审计快照：2026-11-01 11:53–12:10（+08:00）。仓库根：`MyriadCreation/`。
> ⚠️ **并发提示**：审计期间 task-63/65 正在移动代码。已观察到 10 个法律文件于 11:56–11:57 从 `Simulation.State / Simulation.Systems / Simulation.Definitions` 移入 `Simulation.Laws`（namespace 与引用已同步）。因此：
> - 文中"现状"路径、文件数、行号都是**快照**（序列：审计开始 State 83 / Systems 127 / Definitions 131 / Laws 21 → 快照末 State 80 / Systems 123 / Definitions 128 / Laws 31 个 `.cs`）。
> - 执行 §7 任一批次前，先用 §9 的命令重新扫描，不要照抄本文的"现状"清单直接批量移动。
> - 本文件与 task-63/65 的关系：**不替它们执行移动**；只给目标、接口反转与批次边界，避免三方同时移动同一目录。

---

## 0A. 当前状态（task-92 文档同步，2026-11-01）

> 本文件 §0–§9 为 **task-64 审计快照**（规则 R1–R8、违反点 V1–V16、旧批次 B0–B6、数字/路径/行号均按该快照时间阅读）。
> 当前程序集/依赖/owner/门禁/批次状态已同步到仓库根（本文件保留历史原文）：
> - `docs/architecture/SYSTEM_OWNERSHIP.md` —— 当前版 owner/程序集/批次/门禁；
> - `docs/architecture/CURRENT_ARCHITECTURE.md` —— 速查（程序集图/依赖方向/目录职责/遗留）；
> - `docs/audit/ARCH-COMPLETION-PLAN.md` —— 顶部执行状态摘要（旧 B0–B6 已完成；新 B1–B8 见下）。
>
> 实测快照要点（15 个 asmdef）：`Core`(61)/`Content`(117)/`Laws`(32)/`Faith`(2)/`Culture`(7)/`World`(90)/`Simulation`(421)/
> `Rendering`(12)/`Infrastructure`(23)/`App.Contracts`(8)/`App`(7)/`UI`(28) + Editor/Tests/PlayMode 3 个工具程序集。
> Laws/Faith/Culture 已独立（refs=Core+Content）；App/App.Contracts 已抽出（App 不引 UI）；`Rendering.refs={Core,World,Content}`
> （源码已无 `using MyriadCreation.World`/`Simulation`，仅 asmdef 的 World 引用待删 3d）。
> 门禁：`AsmdefArchitectureGateTests` 3 条，硬违规 0，**唯一白名单 `UI → Simulation`**。
> 批次（新编号）：B1–B7 ✅ 已完成；**B8 ⏳ 部分完成**（门禁已落地；待收口：UI→Simulation 白名单、Rendering→World asmdef、契约/事件推广）。

---

## 0. 结论摘要（给 Lead）

**目录层面（相对健康）**
- `Assets/Art` 干净：`0` 个 `.cs`、`0` 个 `.asmdef`，只有 `Shaders/CyowariHistoricalMap.shader` + 4 个空占位目录（Animations/Materials/Models/Textures）。shader 通过 `CyowariMapStyle.cs:232 Shader.Find("Custom/CyowariHistoricalMap")` 使用，**不是死资源**。
- `Assets/UI` 没有预制体/贴图，全部代码构建（32 个 `.cs`、约 7.1k 行）；但里面掺了 **App 流程** 和 **设置服务**（见 §3.4）。
- `Assets/Scripts` 下**所有 namespace 都是 `MyriadCreation.*`**；没有混入第三方命名空间目录。目录命名例外只有 `Rendering/Editing`（叶子目录不带 `MyriadCreation` 前缀）与 `Assets/Editor` 的 4 个子目录命名不统一。
- 程序集依赖是**无环 DAG 且无反向引用**：`Core` 无引用；`World→Core`；`Simulation→Core+World`；`Infrastructure/Rendering→Core+World+Simulation`；`UI→所有人`；Tests/Editor 在顶层。**当前没有物理反向依赖（asmdef 层面零违反）**。

**结构性问题（真正需要拆的）**
1. **`GameWorld` 是约 3244 行 God object（快照；并发修改中，行数以重扫为准）**：组合根 + 全局容器 + 生成器 + 运行时接线 + 事件派发都在一个类，import 了 18 个 `Simulation.*` + 7 个 `World.*` namespace（`GameWorld.cs:7-35`，`InitializeWorld:509`、`InitializeSubsystems:571`、`GenerateTerrain:1769/1787/2096`、`RecalculateDirty:2219`）。
2. **`Simulation` 单程序集内职责混装**（555 文件）：`Simulation.Systems` 123 文件把系统逻辑、数据模型、调度粘合放一起；`Simulation.State` 80 文件把各系统状态（法律/文化/信仰/军事/经济/政治）混在一起；`Simulation.Definitions` 128 文件把内容定义、运行时 DTO、存档数据混在一起。
3. **UI 程序集承载 App 层**：`Bootstrap`(203)/`GameManager`(354)/`SceneFlowController`(252) 是启动与场景流，不是 UI；`SettingsModel`(522)/`SettingsService`(196) 是设置服务，不是 UI 定义；UI 32 个文件里 **17 个 import `MyriadCreation.Simulation.*`**、10 个 import `Infrastructure.Save`、4 个 import `Rendering`。
4. **Rendering 的视图层直接改模型**：7/11 个 Rendering 文件 import Simulation；`RuntimeMapEditingController.cs:139,148,156,164` 执行编辑命令、`:183-184` 直接写 `world.tiles[i].isTerrainDirty` 并调用 `world.RecalculateDirty()`。
5. **全局可变静态注册表**是隐藏耦合：`Core.Data/ContractRegistry.cs:6`、`Core.Data/RelationRegistry.cs:7`、`Simulation.Economy/DynamicGoodsRegistry.cs:8`、`Simulation.Systems/CharacterKnowledgeRegistry.cs:6`、`Simulation.Units/MapUnitRegistry.cs:9`、`Simulation.Modding/ContentRegistry.cs:12`。
6. **法律/法令已基本合并到 `Simulation.Laws`（task-63/65 的成果，31 文件）**；剩余问题是状态归属（`RealmData.cs:74,76` 持有 lawState/decreeState）、UI 直接调用静态系统（`GovernmentPanel.cs:311/323/332/345`）、存档适配（`Infrastructure.Save`）与 `LawConstraints` 仍被 UI/Presentation 直接读（`UIManager.cs:464`、`RealmOverviewText.cs:29-30`、`SettlementPeopleText.cs:80`）。
7. **信仰/文化未独立**：Faith 约 20 个文件散在 6 个目录；Culture 6 个文件散在 2 个目录并牵连 Modding/Save/UI/Economy。二者是最适合先独立 asmdef 的系统（用户原则），但需要 §7.2/§7.3 的接口反转。

**建议批次（详见 §7）**：`B1 低风险归位` → `B2 Content 程序集` → `B3 App/组合根` → `B4 Faith asmdef` → `B5 Culture asmdef` → `B6 Laws asmdef` → `B7 World/Rendering 视图契约` → `B8 契约/事件推广 + 架构门禁`。
**只读审计结论**：真正"可安全删除"的只有 §8.1（4 个 0 引用字体 + 空占位目录）；其余全部是"归位/拆分"，不要删。

---

## 1. 用户原则 → 可执行规则

用户原意：**解耦 = 各系统各写各的，用少量兼容代码串起来；改一个系统不应牵连大量其它系统；文件夹与程序集体现"谁负责什么"；Art 只放图/材质/纹理/效果；UI 只放 UI 定义；法律和法令放一起；信仰、文化各自独立大文件夹。**

落到本仓库的可执行判据：

| 编号 | 规则 | 判定方法 |
|---|---|---|
| R1 | **目录即 owner**：一个系统的代码只放在自己的 owner 文件夹；文件归属唯一 | 文件路径前缀 |
| R2 | **程序集即边界**：owner 文件夹尽量对应 asmdef；跨 asmdef 引用即硬边界 | asmdef `references` JSON |
| R3 | **跨系统通道白名单**：只允许 Core.Events / Core.Contracts / Infrastructure（适配/组合根）/ Core.Data 只读 DTO | `using` 扫描 + code review |
| R4 | **Core 纯净**：Core 只放契约/DTO/枚举，不依赖 UnityEngine、不做 IO、不放可变全局状态 | `Core/*.cs` 的 using + `core.asmdef` |
| R5 | **UI 只做定义与输入**：UI 不直接调用 Simulation 静态系统、不改 world；只消费 Core.Contracts / ViewModel / 命令接口 | UI 目录 import 与调用点 |
| R6 | **兼容层唯一**：同一跨系统交互只允许一个适配实现；禁止系统 A、B 各写一份互相调用 | 搜索重复入口 |
| R7 | **事件/命令/查询语义**：事件=已发生通知；命令=请求变更；查询=读；不要用事件做 RPC，不要用直接写字段代替命令 | API/review |
| R8 | **改动波及指标**：改一个系统时，允许"本系统文件 + 至多 1 个适配文件"；超过即说明边界没切干净 | 统计跨文件夹 using 边（§9） |

**"改一个系统不应牵连大量其它系统"的量化基线**（审计扫描，using 级近似）：当前最大的跨文件夹边是
`Systems→State 62`、`State→Definitions 45`、`Systems→Definitions 44`、`Systems→Core.Data 39`、`Systems→Core.Enums 33`、`State→Enums 21`、`Systems→WorldState 21`、`Systems→Scheduling 19`、`Systems→World.Settlement 16`、`Actors→WorldState 14`、`Definitions→State 11`（这条还是"内容定义反向依赖运行状态"的坏信号）。
目标：每个系统 owner 文件夹的跨系统 using 边 ≤ 3 个系统目录，Core/World 边保持 0 反向。

---

## 2. Assets 顶层目录职责表（快照）

| 目录 | 文件数 / .cs | 应然职责 | 现状问题 / 目标 |
|---|---|---|---|
| `Art/` | 11 / **0** | 图片、材质、纹理、着色器、特效 | ✅ 无代码、无 asmdef。仅 1 个 shader + 4 个空占位目录；shader 被 `CyowariMapStyle` 使用。目标：以后新增 UI 皮肤/贴图也进 `Art/Interface`（Lead 已清理旧 `Art/Interface`）；空目录可留可删。 |
| `Audio/` | 4 / 0 | 音频资产（音乐/音效） | ⚠️ 名存实亡：只有 `Music/`、`SFX/` 空目录；实际音频在 `Resources/Audio/UI/*.wav`（UI 音效）与 `StreamingAssets/Music`（MusicPlayer 扫描）；`MusicPlayerSystem.LoadFromResources` 还期待 `Resources/Music`（当前不存在）。目标：统一音频资产目录约定（建议 `Resources/Audio/{UI,Music,SFX}` 或 `StreamingAssets/Audio`），本目录归位或删除。 |
| `Data/` | 1 / 0 | 数据文件 | 仅 `.gitkeep`；内容数据实际在 `StreamingAssets/Base|Mods`。目标：删除或并入 `StreamingAssets`。 |
| `Editor/` | 22 / 7 | 编辑器工具 | 4 个子目录命名不统一：`ContentAuthoring/`、`MyriadCreation.Editor/`、`MyriadCreation.EditorTools/`、`TerrainPainter/`；`MapEditing/` 为空目录。目标：统一为 `MyriadCreation.Editor.<职责>` 或 `EditorTools/{ContentAuthoring,TerrainPainter,Diagnostics}`。 |
| `Fonts/` | 11 / 0 | 字体资产 | 5 个字体：`simhei.ttf` 被 `Main.unity` 引用（Legacy UI）；`Resources/Fonts/simhei.ttf` 是 TMP 运行时副本；4 个 SourceHan 字体 **0 GUID 引用**（见 §8.1）。目标：删 0 引用字体；在文档注明 simhei 两份副本的用途。 |
| `Prefabs/` | 1 / 0 | 预制体 | 仅 `.gitkeep`；本工程 UI 全部代码构建、地图也是代码生成 → 该目录可删除或保留为空占位。 |
| `Resources/` | 15 / 0 | `Resources.Load` 运行时资产 | `Audio/UI/*.wav`（UI 音效，task-62）、`Fonts/simhei.ttf`（TMP）。目标：作为"运行期必须随包加载的资产"唯一入口，新增音频/字体统一走这里。 |
| `Scenes/` | 4 / 0 | 场景 | 仅 `Main.unity`。目标：保持单场景；动态对象代码创建。 |
| `ScriptableObjects/` | 3 / 0 | SO 配置 | `DefaultWorldConfig.asset`（被 `Main.unity` 引用，保留）。目标：配置资产集中在此，或按系统分子目录。 |
| `Scripts/` | 1519 / **732** | 全部游戏代码 | 5 个程序集：Core(53)/World(90)/Simulation(555)/Infrastructure(23)/Rendering(11)。问题见 §3/§4。 |
| `Settings/` | 1 / 0 | 工程设置资产 | 仅 `.gitkeep`。可删除占位。 |
| `StreamingAssets/` | 99 / 0 | 随包明文数据 | `Base/`（内容数据、Localization）、`Mods/`（Localization）、`Music/`（空，MusicPlayer 扫描目录）。目标：内容数据由 Content 层拥有；音频/语言仍在此。 |
| `Tests/` | 178 / 86 | EditMode/PlayMode 测试 | 两个 asmdef 引用所有运行时程序集；目标：只引用被测系统 + Core，避免测试成为"全量引用"的第三处耦合。 |
| `TextMesh Pro/` | 80 / 0 | 第三方 TMP 资源 | 以 Assets 资源形式导入（非 Package）。目标：迁到 Package Manager / 第三方目录，减少顶层噪声。 |
| `UI/` | 72 / 32 | UI 定义与交互 | 无 prefab/贴图；但夹带 App shell、设置服务、View-Model（见 §3.4）。目标：UI 只保留 Panel/Widget/ViewModel；App shell 与设置服务迁出。 |

**Scripts 结构快照（owner 目录 vs 程序集）**：`Core/`（Core.Constants/Contracts/Data/Dto/Enums/Events + Localization）、`World/`（World/Anchor/Biome/Climate/Editing/Generation/Hydrology/Settlement/Terrain）、`Simulation/`（Actors/AI/Definitions/Diplomacy/Economy/Events/Generation/Genetics/Innovation/Laws/Modding/Planning/Politics/Scheduling/Settlement/State/Systems/Units/Warfare/WorldState）、`Infrastructure/`（Audio/Save）、`Rendering/`（MyriadCreation.Rendering + Editing）。**没有第三方/非 `MyriadCreation` 命名空间目录**；唯一例外是叶子目录 `Rendering/Editing` 不对应独立 asmdef（被 Rendering 程序集吸收）。

---

## 3. 程序集与依赖现状

### 3.1 asmdef DAG（快照，直接读 asmdef JSON）

| 程序集 | 路径 | references（简） | 文件/行 |
|---|---|---|---|
| `MyriadCreation.Core` | `Assets/Scripts/Core/core.asmdef` | **无** | 53 cs |
| `MyriadCreation.World` | `Assets/Scripts/World/world.asmdef` | Core | 90 cs |
| `MyriadCreation.Simulation` | `Assets/Scripts/Simulation/sim.asmdef` | Core, World | 555 cs |
| `MyriadCreation.Infrastructure` | `Assets/Scripts/Infrastructure/MyriadCreation.Infrastructure.asmdef` | Core, World, Simulation | 23 cs |
| `MyriadCreation.Rendering` | `Assets/Scripts/Rendering/MyriadCreation.Rendering.asmdef` | Core, World, Simulation | 11 cs |
| `MyriadCreation.UI` | `Assets/UI/MyriadCreation.UI.asmdef` | Core, Simulation, Rendering, Infrastructure, World | 32 cs |
| `MyriadCreation.Editor` | `Assets/Editor/MyriadCreation.Editor.asmdef` | 全部 + UI | 7 cs |
| `MyriadCreation.Tests` / `.PlayMode` | `Assets/Tests/**` | 全部运行时 | 86 cs |

**结论**：DAG 无环、无反向引用（`World` 不引 Simulation/UI；`Core` 不引任何程序集；`Infrastructure/Rendering` 不引 UI）。这是优点，B 批次拆分**不需要先修反向依赖**，主要是把大程序集切开 + 把 App/设置从 UI 挪走。

### 3.2 依赖基线（using 级跨文件夹边，审计扫描）

| 边 | 次数 | 解释 |
|---|---|---|
| `Simulation.Systems → Simulation.State` | 62 | 系统层大量直接读写集中式 State（最大耦合） |
| `Simulation.State → Simulation.Definitions` | 45 | 状态模型依赖内容定义（合理但说明 State 未分层） |
| `Simulation.Systems → Simulation.Definitions` | 44 | 同上 |
| `Simulation.Systems → Core.Data` / `Core.Enums` | 39 / 33 | 正常共享契约，但 Systems 夹着大量本应去 State/Definitions 的文件 |
| `Simulation.State → Core.Enums` | 21 | 正常 |
| `Simulation.Systems → Simulation.WorldState` | 21 | 系统直接抓 `GameWorld`（组合根/全局容器）→ 需要改为注入上下文/契约 |
| `Simulation.Systems → Simulation.Scheduling` | 19 | 调度粘合与系统逻辑混在同程序集 |
| `Simulation.Systems → World.Settlement` | 16 | 系统直接抓聚落模型（可接受但应收窄） |
| `Simulation.Actors → Simulation.WorldState` | 14 | 角色系统依赖世界容器 |
| `Simulation.Definitions → Simulation.State` | 11 | **反向坏信号**：内容定义依赖运行状态（B2 要反转） |
| `Infrastructure.Save → Simulation/World` | 10+8+7+5… | 存档适配层直接抓各系统状态（兼容层职责，见 §5） |
| `Rendering → Simulation.*` | 7 个文件 | 视图直接消费 Simulation 模型（B7 收敛） |
| `UI → Simulation.*` | **17/32 个 UI 文件** | UI 直接依赖 Simulation（B3/B7 收敛） |

> 口径：扫描每个 `.cs` 的 `using MyriadCreation.*`，映射到 owner 文件夹；同 namespace 的直接类型引用不计。数字为近似，用于趋势对比而非精确度量。

### 3.3 顶层结构问题清单（按"错位"排序）

1. **`Simulation.WorldState` = 组合根 + 全局容器**：`GameWorld.cs` 约 3244 行（快照），同时做：世界数据容器、子系统工厂（`InitializeSubsystems:571`）、地形生成（`GenerateTerrain:1769`/`GenerateTerrainBase:1787`/`GenerateSettlements:2096`）、运行时全量重算（`RecalculateAll:2213`/`RecalculateDirty:2219`）、事件注册（`RegisterEventListener:458`）、20+ `GetXxx()` 服务出口。→ 拆：数据容器（WorldState）/ 子系统状态（各系统）/ 生成流程（World.Generation）/ 组合根（App）。
2. **`Simulation.Systems` 是"系统 + 数据 + 调度"杂物间**（123 文件）：例如 `AdminDivision`、`BuildingData`、`CharacterResearchData`、`ResearchPlanData`、`DiscoveryActors`、`PriorityQueue`、`RealmForm`、`PathTile`、`EligibilityRules`、`KnownCharacterInfo` 是数据/工具类；`*Schedule`/`*Runtime` 是调度与运行时胶水；`AISchedule`/`CultureSchedule`/`EconomySchedule`…与本系统逻辑同级。→ 每个系统一个文件夹（Logic/State/Data/Schedule 子目录），调度归 `Simulation.Scheduling` 或系统自身。
3. **`Simulation.State` 是各系统状态大杂烩**（80 文件）：`Law.cs`、`CultureData.cs`、`Doctrine`/`DoctrinePool`（信仰）、`WarState`/`WarRules`/`Army`/`BattleResult`（军事）、`TradeCenter`/`TradeRoute`（经济）、`RealmData`/`Faction`/`OfficeTitleEntry`（政治）、`MentalHealthSystem.cs`（系统逻辑放进 State）、`TaxSystemDTO`/`RealmDTO`/`RaceDTO`（DTO 放进 State）。→ 状态跟随系统 owner；DTO 进 Core.Dto；逻辑进对应 Systems。
4. **`Simulation.Definitions` 是"内容定义 + 运行时 + 存档"混合体**（128 文件）：`DynamicGoodsSaveData.cs:7` 是存档 DTO；`ReligionCatalog` 是运行期目录（不是定义）；`QualityManager`、`SpecialtyTypeExtensions` 是行为；另有 11 个文件依赖 `Simulation.State`（如 `ReligionCatalog.cs`、`PersonalityTraitDatabase.cs`）→ 内容层反向依赖运行状态。→ B2 拆 Content 程序集时反转。
5. **`Simulation.Modding` 是内容加载/校验流水线**（50 文件：`ContentRegistry:12`、`ContentResolvers/Runtime/Store/Validator/Provider*`），本质是数据基础设施，却放在 Simulation 程序集里；`Assets/StreamingAssets/Base|Mods` 才是它的数据。→ 目标 `MyriadCreation.Content` 程序集（或 Infrastructure.Content），Simulation 依赖 Content。
6. **`World.Settlement` 47 文件混"地图锚点模型"与"聚落仿真数据"**：`SettlementData.cs:11`（666 行）同时是地图/存档模型、市场/区划/设施/经济/演化状态容器；另有 `SettlementEvolutionSystem` 一类的逻辑副本（`HabitationSiteTransitionSystem`/`SettlementEvolutionSystem` 在 World 与 Simulation 两边都有语义）。→ B7 拆 `World.Settlement（模型）` vs `Simulation.Settlement（模拟）`。
7. **地图编辑职责分裂**：`World.Editing/`（`IMapEditCommand`/`MapEditHistory`/`MoveSelectionCommand`/`RaiseSelectionCommand`/`MapSelection`）是编辑命令模型；`Rendering/Editing/RuntimeMapEditingController.cs` 是输入+执行器，直接执行命令并写 `world.tiles`/`RecalculateDirty`（`:139,148,156,164,183-184`）。→ 执行器迁 App/Infrastructure；Rendering 只做输入适配。
8. **`Rendering` 视图层 fan-in 过大**：`MapRenderer.Colors.cs:4-21` import 十余个 Simulation/World namespace；`MapRenderer.Texture.cs:4-23` 同样；`CyowariMapStyle.cs:8-16`；`MapRenderer.Nomadic.cs`、`SettlementIconLayer.cs` 直接读世界/聚落/经济/政治模型。→ 定义 `Core.Contracts` 的 MapView 查询接口/展示 DTO（如 `ITileView`, `IRealmColorProvider`），Rendering 只依赖 Core+World；或引入 `World.Presentation` 视图模型程序集。
9. **UI 夹带 App 与设置服务**（§3.4）。
10. **全局静态注册表**（§5.4-V7）：6 个 mutable static registry（2 个在 Core）→ 目标：实例化 store（WorldState/App 持有），Core 只留接口。

### 3.4 UI 目录职责细分（32 文件 / 约 7.1k 行）

| 分类 | 文件（行数） | 现状职责 | 目标 |
|---|---|---|---|
| App/场景流 | `Bootstrap.cs`(203)、`GameManager.cs`(354)、`SceneFlowController.cs`(252) | 启动、Content/Localization 初始化、GameWorld 生命周期、主菜单↔游戏↔编辑器流转、存档加载重绑 | 迁 `MyriadCreation.App`（或 Infrastructure.App）程序集；UI 不依赖 |
| 设置服务 | `Settings/SettingsModel.cs`(522)、`SettingsService.cs`(196) | PlayerPrefs 模型、引擎分辨率/画质/音量应用、UI 缩放 | 模型/服务迁 Infrastructure/App；UI 只留 `SettingsPanel` |
| 面板 | `Panels/*`：`SettingsPanel`(685)、`GovernmentPanel`(450)、`MainMenuUI`(436)、`SettlementDetailPanel`(421)、`SaveLoadPanel`(343)、`SocietyPanel`(112)、`DiplomacyPanel`?（已删）等 | 视图 + 部分业务直连（法律/存档/世界生成） | 保留 Panel；业务调用改 Core.Contracts 命令/查询 |
| 通用 UI 框架 | `Common/UIManager*.cs`(519+978+209+148)、`UITheme`(93)、`TMPFontUtility`(57)、`UIButtonHover`(115)、`UiSfx`(99)、`UiScaleBinder`(76) | HUD/地块/面板调度、主题、字体、音效、缩放 | 保留；UIManager 瘦身为 panel router + ViewModel 绑定 |
| View-Model/Presentation | `MyriadCreation.UI.Presentation/*`(12 文件：`ReligionPanelText`、`RealmOverviewText`、`SettlementPeopleText`、`ClassPanelText`、`SiteTypeLabels` 等) | 文本/展示模型（正确方向） | 保留并扩展为 UI 与 Simulation 的唯一桥（B3/B7） |

**UI 依赖量化**：32 个 UI 文件中 **17 个 import 至少一个 `MyriadCreation.Simulation.*`**；import 次数 top：`Simulation.Definitions 15`、`Simulation.State 14`、`Simulation.Systems 12`、`WorldState 11`、`Economy 8`、`Politics 8`、`Laws` 4、`Infrastructure.Save` 10、`Rendering` 4。→ 目标：UI 直接 import 只允许 `Core.Contracts/Core.Data/Core.Events` + UI 自身；Simulation 只允许经 ViewModel/契约。

---

## 4. 系统清单（owner 现状 vs 目标）

> 数量为快照 `.cs` 数；"接口/事件"是现状（多为静态方法/管理器 + `GameWorld.GetXxx()`，少数命令/事件）。

| 系统 | 现状 owner（路径 / 文件数） | 目标 owner | 对外接口/事件现状 | 依赖方向 / 备注 |
|---|---|---|---|---|
| **Core 契约/事件/DTO** | `Core/*`（53） | `Core`（仅契约/DTO/枚举；禁止引擎/IO/静态状态） | `ISimulationCommand/Query/Process/Activity` + `SimulationEventBus` + 7 个事件（`EconomyDayTicked`、`SeasonChanged`、`WarDeclared/Ended`、`FaithConflictTriggered`、`PracticeRecorded`）；`Core.Data` = `TileData/WorldConfig/Contract/Relation/PopulationBlock` 等 | 被所有层引用；现状 `Localization.cs:4` import UnityEngine、`TileGrid/WorldConfig` 用 UnityEngine、`core.asmdef noEngineReferences:false` → B3 清理 |
| **地图/地形/气候/水文生成** | `World/Generation`(5)、`Terrain`(14)、`Climate`(4)、`Hydrology`(4)、`Biome`(4)、`Anchor`(5)、`World`(2) | `World` 程序集；生成算法可留；`World.Editing` 合并进地图编辑 owner | `PlanetTerrainGenerator`、`SeaLandGenerator`、`AtmosphericCirculation`、`HydrologySystem`、`HoldridgeBiomeClassifier`；无事件 | `World→Core`（干净）；问题：`World.Settlement` 47 文件、`World.Editing` 与 Rendering 重复概念 |
| **世界容器/组合根/调度** | `Simulation.WorldState`（3：`GameWorld.cs` 约 3244、`WorldMapSchedule`、`WorldMapSimulationSystem`） | 数据容器 `WorldState` + 组合根 `App`（B3） | `GameWorld.GetEconomyManager/GetFaithSystem/…`（20+）；`RegisterEventListener`（`GameWorld.cs:458`）；`SimulationScheduler` | 依赖全部 Simulation 文件夹（import 列表 `GameWorld.cs:7-35`）；God object 首要拆分对象 |
| **法律/法令** | 快照：`Simulation.Laws`(31)；外部仍有 `RealmData.cs:74,76` lawState/decreeState、`Infrastructure.Save`、`UI/GovernmentPanel`、`UI/Common/UIManager.cs:464`、`UI.Presentation:29-30/80`、Tests | `Simulation.Laws`（独立 asmdef） | `LawAndDecreeSystem`（静态）、`LawDecreeViewBuilder`、`DefaultLawCatalog`、`LawConstraints`；事件：无（建议 `LawEnactedEvent`/`LawCancelledEvent`）；UI 直接调用 `LawAndDecreeSystem.*`（`GovernmentPanel.cs:233-235/311/323/332/345`） | 依赖 `State(RealmData/LawState)`、`Definitions`；被 UI/Save/Tests 直接引用；B6 反转 |
| **信仰** | 20 文件：`Simulation.Systems`(5)：`FaithSystem`、`CanonizationSystem`、`ReligionSimulationEventHandler`、`WarfareReligionSchedule`、`GreatHolyWarSystem`；`Definitions`(9)：`ReligionCatalog/Def`、`FaithType`、`FaithOrganizationType`、`ReligionMapLevel/NodeType`、`Doctrine*`、`SaintDef`；`State`(3)：`Doctrine`、`DoctrinePool`、`GreatHolyWarState`；`Core.Events`(1)；`Infrastructure.Save`(2) | `Simulation.Faith`（`Definitions/`, `State/`, `Runtime/`, `Persistence/`），独立 asmdef（B4） | `FaithSystem`（`Systems:10`）；`GameWorld.GetFaithSystem(faithId)`（`GameWorld.cs:2826`）；事件已有 `FaithConflictTriggeredEvent`、`PracticeRecordedEvent`；无 `IFaithQuery` | 依赖 `State`/`Definitions`/`Systems`；被 `CharacterManager/ThoughtManager/CanonizationSystem`、`Infrastructure.Save`、`UI/ReligionPanelText`、`Rendering/MapRenderer.Colors` 直接引用 → 需接口反转（§7.2） |
| **文化** | 6 文件：`State/CultureData.cs`(1)、`Systems/CultureLineage/Similarity/Transition/StageEvolutionSystem/Schedule`(5)；引用方：`GameWorld`、`Modding/ContentRegistry/Runtime`、`Economy/ClassEmergenceEvents`、`Infrastructure.Save`、`UI/SettlementPeopleText` | `Simulation.Culture`（独立 asmdef，B5） | 无契约；`CultureData` 状态 + 5 个静态系统；无文化事件 | `CultureStageEvolutionSystem` import Actors/Modding/WorldState/World.Settlement（`CultureStageEvolutionSystem.cs:2-8`）→ 需 `ICultureContext`/`ICultureQuery` 反转（§7.3） |
| **经济** | `Simulation.Economy`(4：`DynamicGoodsRegistry`、`ClassEmergenceEvents`、`ImproveRealmEconomyCommand(+Handler)`)；`Systems` 经济约 9（`EconomyManager`、`TaxSystem`、`CurrencySystem`、`Caravan`、`SettlementProduction`、`ResourceAccess`…）；`State`(TradeCenter/TradeRoute/TaxSystemDTO/TradeCenterDTO)、`Infrastructure.Save`(SettlementMarket/Persistence)、`Rendering/MapRenderer.Colors`、UI | `Simulation.Economy` | `EconomyManager`、`CurrencySystem`、`TaxSystem`、`SettlementMarket`（World）；事件 `EconomyDayTickedEvent` | 依赖 State/Systems/World；与聚落/法律/军事交叉多；先做事件与查询，不急于独立 asmdef |
| **军事/战争** | `Simulation.Warfare`(2 command)、`Simulation.Units`(29)、`Systems` 军事约 12（`CombatManager`、`ManpowerSystem`、`MilitaryMovementSystem`、`Retinue*`…）、`State` 军事 13（`Army`、`BattleResult`、`WarState/Rules`、`PeaceNegotiation`…）、`Infrastructure.Save/Army*` | `Simulation.Military` / `Simulation.Units` | `GameWorld.GetCombatManager()`（`GameWorld.cs:3027`）、`GetWars`；事件 `WarDeclared`/`WarEnded`、`MilitaryBuildUpCommand` | 依赖 Actors/State/Laws/Faith（`WarfareReligionSchedule`）→ 宗教战争是最先要解的交叉 |
| **政治/外交/继承/行政** | `Simulation.Politics`(2)、`Diplomacy`(9)、`Systems` 政治/行政约 14、`State` 政治 12 | 拆 `Simulation.Politics` / `Simulation.Diplomacy` | `DiplomacyManager`、`PoliticalManager`、`FactionManager`、`SuccessionSystem`、`AdminDivisionSystem`；命令：`ConsolidateRealm`、`DeclareWar`、`ProposeAlliance` 等 | `State/RealmData` 是跨系统共享根实体（法律/信仰/军事都挂它）→ 建议 RealmData 只留标量/ID，各系统状态各自持有（B4-B6 共同前提） |
| **聚落** | `World.Settlement`(47)、`Simulation.Settlement`(9)、`Systems` 聚落/建造约 22、`Infrastructure.Save`(SettlementPersistence/Block/Market)、`Rendering/SettlementIconLayer`、`UI/SettlementDetailPanel`、`UI/SettlementPeopleText` | `World.Settlement`（地图模型/生成）+ `Simulation.Settlement`（模拟） | `SettlementData`、`SettlementMarket`、`BuildingSystem`、`SettlementEvolutionSystem`、`SettlementControlSystem`；无事件 | 依赖 World/Simulation 两侧；`SettlementData.cs:11` 666 行混合模型/模拟 → B7 |
| **角色/基因/性格** | `Simulation.Actors`(17)、`Genetics`(26)、`State` 角色 16、`Systems` 角色约 18、`Infrastructure.Save/Character*`、UI 人物面板 | `Simulation.Actors` + `Simulation.Genetics` | `CharacterManager`、`GameWorld.GetCharacterManager()`（`:3134`）、`DnaSystem`；无事件 | 角色是"根实体"，被几乎所有系统读；目标暴露 `ICharacterQuery` 只读契约，禁止系统直接改角色字段（命令化） |
| **AI/规划/创新** | `Simulation.AI`(12)、`Planning`(10)、`Innovation`(2)、`Systems` AI/规划约 19 | `Simulation.AI`/`Planning`/`Innovation` | `AIManager`、`PlanSystem`、`ResearchPlanSystem`、`StartInnovationResearchCommand` | 依赖 State/Systems；可低优先级拆分 |
| **内容/Modding** | `Simulation.Definitions`(128) + `Simulation.Modding`(50) + `StreamingAssets/Base|Mods` + `Assets/Editor/ContentAuthoring` | `MyriadCreation.Content`（独立 asmdef，B2） | `ContentRegistry`（`ContentRegistry.cs:12`）、`ContentResolver/Runtime/Store/Validator`、`ReligionCatalog`、`GoodsDef` 等；`IContentProvider` 接口已有 | 反向依赖 State 11 文件（`ReligionCatalog` 等）要反转；Systems 直接 new/静态访问 content（如 `DefaultLawCatalog`）→ 通过 `IContentLookup` 契约 |
| **存档/兼容** | `Infrastructure.Save`(20) + `Infrastructure.Audio`(3) | `Infrastructure`（兼容层，保留并扩展 App/适配器） | `SaveSystem`、`GameSaveSystem`、各系统 `*Persistence`、`SaveEntityBridge`、`FaithPersistence`…；事件总线 `SfxPlayer` | 依赖 Core/World/Simulation（兼容层唯一允许"抓多系统"）；问题：直接改各系统字段（可接受但要按系统快照接口收敛） |
| **渲染/视图** | `Rendering`(11：`MapRenderer*`、`CyowariMapStyle`、`SettlementIconLayer`、`Editing/RuntimeMapEditingController`) | `Rendering`（只读视图）+ `App`（输入执行器） | `MapRenderer.BindWorld`、`SettlementIconLayer.SettlementClicked`（事件）、`CyowariMapStyle` | 7/11 文件 import Simulation；`RuntimeMapEditingController` 改 world（`:139,148,156,164,183-184`）→ B7 |
| **UI/App** | `UI`(32，约 7.1k 行) + `UI/MyriadCreation.UI` App shell + `UI/Settings` | `UI`（定义/交互）+ `MyriadCreation.App`（B3） | Panel `Build/Show/Hide`、`UIManager.Open*`、`SettingsModel/Service`；事件：`SettlementIconLayer.SettlementClicked` | UI 17/32 直接 import Simulation；App 3 文件在 UI 程序集里；B3 先拆 |
| **美术/音频资产** | `Art`(1 shader+空目录)、`Fonts`、`Resources/Audio/UI`、`StreamingAssets/Music` | `Art/` 仅美术；音频统一目录；字体统一 | 无代码 | §8 清理/归位 |

---

## 5. 兼容层规范（本次要落地的核心约定）

### 5.1 定义

**兼容层 = 少量代码：只做"数据转换/持久化/事件派发/跨系统事务编排"，不含业务规则；改系统 A 时只允许改 A 的 owner 文件 + 至多 1 个兼容层适配文件。**

本仓库的兼容层落点（只允许这三处）：

| 通道 | 现状实现 | 适用场景 | 使用现状 |
|---|---|---|---|
| `Core.Events` | `SimulationEventBus`（`Core.Events/SimulationEventBus.cs`）+ 7 个事件（`EconomyDayTickedEvent`、`SeasonChangedEvent`、`WarDeclaredEvent`、`WarEndedEvent`、`FaithConflictTriggeredEvent`、`PracticeRecordedEvent`） | 已发生事实的广播（弱耦合、可多订阅） | 仅 6 个文件使用 `SimulationEventBus`；事件类型偏少 |
| `Core.Contracts` | `ISimulationCommand/Query/Process/Activity` + `SimulationCommandBus`/`SimulationQueryBus`（16 文件） | 跨系统请求变更/读查询/流程编排 | 已有 11 个 `*CommandHandler`（Diplomacy 4、Settlement 3、Economy/Innovation/Politics/Warfare 各 1）；`ISimulationQuery`/`ISimulationProcess` 仍只有契约定义，无系统 Adopt |
| `Infrastructure` | `SaveSystem`/`GameSaveSystem` + `*Persistence`（20 文件）、`SfxPlayer`（3 文件） | 存档/读档、跨系统快照、引擎适配、组合根（未来 App） | 存档层已在做"跨系统适配"，是本仓库唯一合法的多系统引用点；但很多适配是直接改字段而非走系统快照接口 |
| `Core.Data`（只读 DTO） | `TileData`、`WorldConfig`、`Contract/Relation`、`PopulationBlock` 等 | 跨系统共享只读数据 | 使用广泛；需清理：`Localization.cs`/`TileGrid`/`WorldConfig` 的 UnityEngine 依赖（§5.4-V6） |
| `World`（共享模型） | 地形/气候/聚落模型 | 所有系统共享的空间模型 | 允许读；写只允许通过编辑命令（World.Editing） |

> 注意：`Simulation.Scheduling`（11 文件）是**系统调度运行时的胶水**，不是兼容层；目标归 `App`/`WorldState` 的调度器，或保留为 `Simulation.Runtime` 的一部分，但不要作为跨系统通信通道。

### 5.2 规范细则

- **Cross-owner 引用白名单**：`Core.Data/Core.Enums/Core.Contracts/Core.Events`（只读/接口）+ 本系统 + 明确的适配器（Infrastructure/App）。
- **禁止**：UI→Simulation 具体静态系统（命令接口例外）；Rendering→Simulation 写操作；系统 A→系统 B 的 manager/状态字段直连；Core→任何程序集/UnityEngine/IO。
- **命令/查询/事件命名**：`XxxCommand`（请求，可失败）、`XxxQuery`（读，无副作用）、`XxxEvent`（已发生，不可拒绝）。
- **状态归属**：`RealmData` 只保留跨系统根实体标量与 ID；各系统状态（`RealmLawState`/Faith/Culture/Military 状态）由各自系统持有，通过接口暴露只读查询/快照。
- **兼容层单一实现**：同一交互只能有一个适配器；如"推进法律"只允许 `ILawCommand` 一个入口（UI、事件、AI 都走它），不允许 `GovernmentPanel` 与 `LawSystem` 各写一套。
- **Core 纯净**：`core.asmdef` 目标 `noEngineReferences: true`；`Localization`、`TileGrid`（UnityEngine.Vector2Int?）、`WorldConfig`（ScriptableObject）需拆：`WorldConfig` 保留为 Core 纯数据 + Unity 侧 SO 适配（World/Infrastructure），`Localization` 迁 Infrastructure。

### 5.3 现有兼容层资产（可直接复用，不要重写）

`Infrastructure.Save`（20）：`SaveSystem`、`GameSaveSystem`、`SaveData`、`GameSaveData`、`MapSaveData`、`TileSaveData`、`*SaveData`（Character/Faith/HabitationSite/Province/SettlementBlock/SettlementMarket/WorldEntity）、`*Persistence`（Army/Character/Faith/Settlement）、`SaveEntityBridge`、`SaveJsonMigration`、`RelationPersistencePolicy`。
`Infrastructure.Audio`（3）：`SfxPlayer`、`MusicPlayer`、`MusicPlayerSystem`。
`Core.Events/Contracts`：事件总线 + 命令/查询/流程接口（已具备"兼容层"骨架，缺 adapter 与使用）。
`World.Editing`（5）：编辑命令模型（`IMapEditCommand`/`MapEditHistory`/`MoveSelectionCommand`/`RaiseSelectionCommand`/`MapSelection`）。

### 5.4 当前违反点（文件:行 + 建议）

| # | 违反点（文件:行） | 问题 | 建议走向 | 批次 |
|---|---|---|---|---|
| V1 | `Assets/UI/Panels/GovernmentPanel.cs:233-235`（TickPending/TickDecree/EnsureKnown）、`:311`（BeginEnactLaw）、`:323`（CancelPendingLaw）、`:332`（RevokeDecree）、`:345`（EnactDecree） | UI 直接驱动法律系统全套变更（不是只发命令） | `Core.Contracts` 增 `ILawQuery`/`ILawCommand`；UI 只构造命令并显示结果；`LawAndDecreeSystem` 实现并注册到组合根 | B6/B8 |
| V2 | `Assets/UI/Common/UIManager.Panels.cs:103,113-120,420-426,425,506,528`；`Assets/UI/Common/UIManager.cs:371-379,464` | UI 直接读 `world.tiles/world.realms/FaithSystem/ReligionCatalog/LawConstraints`（读多写少） | 抽 `UI.Presentation` ViewModel 只用 DTO/Query；`IPoliticsQuery`/`IFaithQuery`/`IMapViewQuery` 返回展示 DTO；`LawConstraints` 移 Core.Contracts 只读规则或由 `ILawRules` 暴露 | B3/B7 |
| V3 | `Assets/Scripts/Rendering/Editing/RuntimeMapEditingController.cs:139,148,156,164`（执行编辑命令）、`:183-184`（写 `isTerrainDirty` + `world.RecalculateDirty`） | 视图层直接改世界模型 | 控制器迁 `App/Infrastructure`；Rendering 只把输入转成 `IMapEditCommand`；World 内部执行 | B7 |
| V4 | `Assets/Scripts/Rendering/MyriadCreation.Rendering/MapRenderer.Colors.cs:4-21`、`MapRenderer.Texture.cs:4-23`、`CyowariMapStyle.cs:8-16`、`MapRenderer.Nomadic.cs`、`SettlementIconLayer.cs` | 视图 import 十余个 Simulation/World namespace，直接算业务展示（颜色/关系/经济） | 定义 Core.Contracts 视图查询（`ITileViewQuery`/`IRealmViewQuery`/`ISettlementViewQuery`）与 DTO；Rendering 只依赖 Core DTO + World | B7 |
| V5 | `Assets/Scripts/Simulation/WorldState/GameWorld.cs:7-35`（18 Simulation + 7 World using）、`:509,571,1769,1787,2096,2219`，约 3244 行（快照） | 组合根 + 数据容器 + 生成 + 调度 + 事件全在一个类；改任何系统都要动它 | 拆四块：`WorldState`（数据/状态持有）、`WorldCompositionRoot`（App：创建系统/接线）、`World.Generation`（`GenerateTerrain*`/`GenerateSettlements`）、`SimulationScheduler`（`WorldMapSchedule` 已有） | B3 |
| V6 | `Assets/Scripts/Core/Localization.cs:4`（using UnityEngine）、`Core.Data/TileGrid.cs`、`Core.Data/WorldConfig.cs`；`Assets/Scripts/Core/core.asmdef`（`noEngineReferences:false`） | Core 不是纯契约层：引擎/IO 依赖 | `Localization` 迁 Infrastructure/App；`WorldConfig` 保留纯数据 + Unity SO 适配；Core 目标 `noEngineReferences:true` | B2/B3 |
| V7 | `Core.Data/ContractRegistry.cs:6`、`Core.Data/RelationRegistry.cs:7`、`Simulation.Economy/DynamicGoodsRegistry.cs:8`、`Simulation.Systems/CharacterKnowledgeRegistry.cs:6`、`Simulation.Units/MapUnitRegistry.cs:9`、`Simulation.Modding/ContentRegistry.cs:12` | 6 个 mutable static 全局注册表（Core 里 2 个）；隐式跨系统共享状态，生命周期靠 `InitializeWorld` 清扫（`GameWorld.InitializeWorld:509` 内 `Clear`） | 改为 App/WorldState 持有的实例 store；Core 只留接口（`IRegistry`/查询）；`ContentRegistry` 归 Content 程序集 | B3/B8 |
| V8 | `Assets/Scripts/Simulation/Definitions/DynamicGoodsSaveData.cs:7` | 存档 DTO 放在内容定义层 | 迁 `Infrastructure.Save`；`Definitions` 保持纯内容定义 | B1/B2 |
| V9 | `Simulation.State`（80 文件）：`Law.cs`、`LawComposition.cs`、`LawEffects.cs`、`CultureData.cs`、`Doctrine/DoctrinePool`、`WarState/WarRules/Army/BattleResult`、`TradeCenter/TradeRoute/TaxSystemDTO`、`MentalHealthSystem.cs:7`、`RealmDTO.cs:11`、`RaceDTO.cs:10`、`TaxSystemDTO.cs:10` | State 变成"所有系统的状态+逻辑+DTO 大杂烩" | 状态归各系统 owner（`Simulation.Laws/State` 或 Faith/Culture/…），DTO 迁 `Core.Dto`，逻辑迁对应 Systems | B1 |
| V10 | `Simulation.Systems`（123 文件）含数据/工具类：`AdminDivision`、`BuildingData`、`CharacterResearchData`、`ResearchPlanData`、`DiscoveryActors`、`PriorityQueue`、`RealmForm`、`PathTile`、`EligibilityRules`、`KnownCharacterInfo`；`*Schedule`/`*Runtime` 混在系统文件中 | 系统逻辑/数据/调度/胶水混装 | 每系统拆 `Logic/`（系统）+ `State/`（模型）+ 调度归 `Simulation.Scheduling` 或系统内部；数据类下沉 | B1 |
| V11 | `Simulation.Definitions → Simulation.State` 11 文件（`ReligionCatalog.cs`、`PersonalityTraitDatabase.cs`、`ImpairmentRegistry.cs` 等） | 内容定义反向依赖运行状态（最坏的方向） | B2：定义只依赖 Core；运行时通过 `IContentLookup` 查询；必要时内容 DTO 放 Core.Data | B2 |
| V12 | `Simulation.Modding`（50 文件，`ContentRegistry.cs:12`）在 Simulation 程序集；`StreamingAssets/Base|Mods` 是它的数据 | 内容加载流水线放在模拟程序集，导致 Simulation 依赖 IO/JSON | 新 `MyriadCreation.Content` asmdef（只依赖 Core；由 Content 加载 JSON/校验/解析）；Simulation 依赖 Content | B2 |
| V13 | `Assets/UI/MyriadCreation.UI/Bootstrap.cs:47-109`（初始化设置/Content/Localization/GameManager/SceneFlow）、`:141-150`（世界生成）；`GameManager.cs:110-165,209-235,280-341`（世界生命周期/存档重绑/动态面板接线）；`SceneFlowController.cs:44-247`（菜单↔游戏↔编辑器流）；`MainMenuUI.cs:208-226,318-346`（世界生成编排） | App/场景流/存档重绑逻辑在 UI 程序集 | 新 `MyriadCreation.App`：Bootstrap/GameManager/SceneFlow/SettingsService/RuntimeMapEditingController；UI 只保留 Panel/ViewModel | B3 |
| V14 | `Assets/UI/Settings/SettingsModel.cs:15`、`SettingsService.cs:16` | 设置模型/引擎应用服务放在 UI | 迁 Infrastructure/App（UI 只留 SettingsPanel）；`SettingsService.ApplyAudio` 等属 App/Infra | B3 |
| V15 | `Assets/UI/Common/UIManager*.cs`（4 文件 1854 行）直连 `MapRenderer`（`:45,184,371-379`）与 world | UIManager 成为 UI 内的第二组合根 | 拆：`UIHudRouter`（面板开关/ViewModel 绑定） + `IUiInput`/`IMapViewQuery`；MapRenderer 通过 App/事件输入 | B3/B7 |
| V16 | `Rendering` 7/11 文件 import `Simulation.*`；`UI` 17/32 文件 import `Simulation.*` | 视图/UI 直接依赖模拟程序集（asmdef 合法但语义错位） | 逐步改为依赖 Core.Contracts 查询/DTO；UI 只依赖 App+Core | B7/B8 |

> 说明：V1–V16 中 V1–V3 是"真违反"（写模型/直接改状态）；V4/V15/V16 是"耦合过重"；V6/V7 是"Core 纯净性"；V9–V12 是"归属错位"。**当前 asmdef 依赖没有反向依赖，所以拆分不需要先修"循环引用"，这降低了 B2–B7 的风险。**

---

## 6. Faith / Culture / Laws 散落清单（快照）

### 6.1 法律/法令（task-63/65 已合并，快照 31 文件）

- 已归位：`Assets/Scripts/Simulation/MyriadCreation.Simulation.Laws/` 31 个 `.cs`，namespace `MyriadCreation.Simulation.Laws`（含 `Law`、`LawComposition`、`LawEffects`、`LawCatalog`、`LawConstraints`、`LawReform`、`LawSystem`、`InheritanceLaw`、`LawCategory`、`LawSource`、`Decree*`、`PendingLaw`、`RealmLawState/RealmDecreeState` 等）。
- 仍在文件夹外的**类型引用**（不是文件错位，是接口耦合，B6 处理）：
  - `RealmData.cs:74,76` 持有 `RealmLawState lawState`/`RealmDecreeState decreeState`（跨系统根实体挂法律状态）
  - `UI/Common/UIManager.cs:464` 读 `LawConstraints.SupremeSovereignty`；`UI/Presentation/RealmOverviewText.cs:29-30`、`SettlementPeopleText.cs:80`
  - `UI/Panels/GovernmentPanel.cs:233-235,311,323,332,345` 直接调用 `LawAndDecreeSystem`
  - `Infrastructure.Save` 的 `GameSaveData/SaveData` 引用 `RealmLawState`（存档快照）
  - `Tests/EditMode/LawModuleTests.cs`、`RealmOverviewTextTests.cs`
- 目标：`Simulation.Laws` 独立 asmdef；`LawConstraints` 只读规则留 `Core.Contracts/Data` 或由 `ILawRules` 暴露；`RealmData` 只留 ID/标量，lawState 由 Laws 系统持有。

### 6.2 信仰（20 文件 / 6 个目录）

| 现状路径 | 文件 |
|---|---|
| `Simulation.Definitions`(9) | `DoctrineCategory`、`DoctrineOptionDef`、`FaithOrganizationType`、`FaithType`、`ReligionCatalog`、`ReligionDef`、`ReligionMapLevel`、`ReligionNodeType`、`SaintDef` |
| `Simulation.State`(3) | `Doctrine`、`DoctrinePool`、`GreatHolyWarState` |
| `Simulation.Systems`(5) | `FaithSystem`、`CanonizationSystem`、`ReligionSimulationEventHandler`、`WarfareReligionSchedule`、`GreatHolyWarSystem` |
| `Core.Events`(1) | `FaithConflictTriggeredEvent` |
| `Infrastructure.Save`(2) | `FaithPersistence`、`FaithSaveData` |
| 外部引用方 | `CharacterManager`、`ThoughtManager`、`CanonizationSystem`、`GameWorld`（`GetFaithSystem:2826`）、`UI/ReligionPanelText`、`UI/UIManager.Panels.cs`、`Rendering/MapRenderer.Colors`、`Infrastructure.Save/SaveSystem` |

### 6.3 文化（6 文件 / 2 个目录 + 多处引用）

| 现状路径 | 文件 / 引用 |
|---|---|
| `Simulation.State`(1) | `CultureData`（状态模型） |
| `Simulation.Systems`(5) | `CultureLineage`、`CultureSimilarity`、`CultureTransition`、`CultureStageEvolutionSystem`、`CultureSchedule` |
| 引用方 | `GameWorld`（调度/容器）、`Simulation.Modding/ContentRegistry/ContentRuntime`、`Simulation.Economy/ClassEmergenceEvents`、`Infrastructure.Save/SaveData/SaveSystem`、`UI/SettlementPeopleText` |
| 依赖方向 | `CultureStageEvolutionSystem.cs:2-8` import `Actors`、`Modding`、`WorldState`、`World.Settlement`（重依赖，需反转） |

> 目标结构（用户要求"各自独立大文件夹，后续再细分"）：
> ```
> Assets/Scripts/Simulation/
>   MyriadCreation.Simulation.Faith/
>     Definitions/ State/ Runtime/ Persistence/   （或 Definitions 留 Content）
>   MyriadCreation.Simulation.Culture/
>     State/ Runtime/ Persistence/
>   MyriadCreation.Simulation.Laws/                （现有 31 文件，补 Contracts/ 薄接口）
> ```

---

## 7. 后续拆分批次（asmdef 级解耦路线）

> 每批的验收：Unity 编译通过 + EditMode/PlayMode 全绿 + §9 的 `systems→systems` 边数下降且无新增反向依赖。批次建议按风险从低到高，**B1 只移动同程序集文件，不改接口**；B2 起才拆程序集。

| 批次 | 目标 | 动作 | 风险 | 依赖/备注 |
|---|---|---|---|---|
| **B0** | 规范落地 | 本文件；新增架构审计脚本（§9 命令固化为 `tools/arch-audit`，可选） | 低 | 本卡只出文档 |
| **B1** | 同 asmdef 内归位（不改 asmdef、不改公开 API 签名） | ① Faith 20 文件 → `Simulation.Faith/*`；② Culture 6 文件 → `Simulation.Culture/*`；③ `Simulation.Systems` 数据/调度类下沉到各系统 `State/` + `Simulation.Scheduling`；④ `State` 的 DTO（TaxSystemDTO/RealmDTO/RaceDTO）→ `Core.Dto`；⑤ `DynamicGoodsSaveData` → `Infrastructure.Save`；⑥ 命令-only 文件夹（Planning/Politics/Warfare/Economy/Innovation）并入对应系统文件夹的 `Commands/` | 中低（namespace 改动需全仓 using 更新；但同程序集不会引入 asmdef 问题） | 与 task-63/65 的移动**不要并行**：Lead 需指定本批唯一执行者；先重扫 §9 |
| **B2** | 抽 `MyriadCreation.Content` asmdef | ① `Simulation.Modding`(50) + `Simulation.Definitions`(128) + 内容定义 → `Assets/Scripts/Content/MyriadCreation.Content/`；② 反转 `Definitions→State` 11 文件：`ReligionCatalog` 等改为只读 `IContentLookup` / DTO；③ `ContentRegistry` 静态改实例（Content 层内）；④ 系统引用内容统一走 `IContentResolver`（Content 暴露 interface，引用方向 Content←Systems） | 高（128+50 文件、JSON 加载路径、内容包/Mod 校验） | 前置：B1 完成；需要 `IContentLookup`（Core.Contracts）先落地 |
| **B3** | 抽 `MyriadCreation.App`（组合根/场景流/设置） | ① 移 `UI/MyriadCreation.UI/{Bootstrap,GameManager,SceneFlowController}`、`UI/Settings/{SettingsModel,SettingsService}`、`Rendering/Editing/RuntimeMapEditingController` → App；② `GameWorld` 拆：数据容器留 WorldState，`InitializeSubsystems`/系统工厂/事件订阅 → `WorldCompositionRoot`（App）；③ 静态注册表改实例 store（App 持有、WorldState 生命周期）；④ UI asmdef 收缩为 Core+App+UI 自身依赖 | 高（App 是启动入口，牵动场景/设置/存档/编辑） | 前置：B2（Content 初始化移到 App）；与 UI 卡（政府/聚落/设置）协调，避免同文件冲突 |
| **B4** | `Simulation.Faith` 独立 asmdef | 见 §7.2：移动 20 文件 + 反转 6 类接口（`IFaithQuery/IFaithCommand/IFaithContext`、`IFaithSnapshot`、`IContentLookup`、`ICharacterQuery/IRealmQuery`） | 中高 | 前置：B1（文件已归位）+ B2（内容定义在 Content，Faith 只依赖 Content 接口）；允许 Faith asmdef 依赖 Core+Content+World（只读） |
| **B5** | `Simulation.Culture` 独立 asmdef | 见 §7.3 | 中高 | 前置：B4 同一套 `ICharacterQuery/IRealmQuery/IContentLookup` 可复用；还需 `ISettlementQuery` |
| **B6** | `Simulation.Laws` 独立 asmdef | 见 §7.4：`ILawQuery/ILawCommand/ILawRules` + 快照接口；UI/GovernmentPanel 改命令；`RealmData` 去 lawState 字段（或 partial 状态由 Laws 持有） | 中 | 前置：B1（Laws 已合并）+ B3（组合根可注册 ILawCommand 实现） |
| **B7** | World/Rendering 视图契约 | ① `Core.Contracts` 增 Map/Settlement/Realm 展示查询接口 + DTO；② `Rendering` 去掉 Simulation import（只 Core+World+Contracts）；③ `RuntimeMapEditingController` 实现输入→`IMapEditCommand`（App 执行）；④ `World.Settlement` 拆分：`SettlementData` 模型 vs `SettlementSimulationState`（Simulation.Settlement） | 中高 | 前置：B3；渲染/地图是玩家主界面，需要视觉回归 |
| **B8** | 契约/事件推广 + 架构门禁 | ① 事件补：`LawEnactedEvent`/`LawCancelledEvent`、`CultureChangedEvent`、`FaithChangedEvent`、`SettlementChangedEvent`、`MapEditedEvent`；② 系统间 manager 直连改 `IXxxQuery`/`IXxxCommand`（先 Faith/Culture/Laws，再 Economy/Military）；③ CI/本地 lint：asmdef 反向引用=0、`UI` 禁止 import `Simulation.*`（白名单除外）、每 owner 跨文件夹边 ≤3、`Core` 禁 UnityEngine/IO；④ `docs/architecture` 增 adr 记录 | 持续 | 依赖 B2–B7；建议每批完成后更新 §9 基线数字 |

### 7.1 度量目标（用于判断"解耦完成"）

| 指标 | 现状（快照） | 目标 |
|---|---|---|
| `Core` 反向/引擎依赖 | `Localization.cs`、`TileGrid`、`WorldConfig` 用 UnityEngine；2 个静态 registry | 0 引擎/IO/静态状态 |
| `UI` 直接 import `Simulation.*` | 17/32 文件 | ≤5（仅 ViewModel 层允许；Panel 只 import UI/Core/App） |
| `Rendering` import `Simulation.*` | 7/11 文件 | 0（只 Core/World/Contracts） |
| `GameWorld` 行数 | 约 3244（快照） | ≤800（容器+查询入口），生成/组合根分离 |
| `Systems→State` using 边 | 62 | ≤20（状态按系统下沉后） |
| `Definitions→State` using 边 | 11 | 0 |
| 系统 owner 文件夹跨系统边数 | 多（Systems 最重） | 每个 ≤3 |

### 7.2 Faith 独立 asmdef 的接口反转清单（B4）

**目标**：`MyriadCreation.Simulation.Faith` 只依赖 `Core`（+ 内容接口/World 只读），不被 Simulation 主程序集引用具体类型。

需要新增/迁移的契约（建议放 `Core.Contracts`/`Core.Events`；Faith 实现，其它系统只依赖接口）：

| 契约/事件 | 提供方 | 消费方（现状直接引用） | 反转动作 |
|---|---|---|---|
| `IFaithQuery`：`GetFaith(faithId)`、`GetRealmFaith(realmId)`、`GetDoctrine(realmId)`、`GetPatronSaint(realmId)`、`CanCanonize(...)` | Faith 实现；App 组合根注册 | `UI/UIManager.Panels.cs:420-426`、`UI/Presentation/ReligionPanelText.cs`、`Rendering/MapRenderer.Colors.cs`、`Infrastructure.Save/SaveSystem` | UI/Rendering/Save 改查接口/快照；Rendering 只拿展示 DTO |
| `IFaithCommand`：`RecordPractice`、`Canonize`、`DeclareHolyWar`（或由 `IHolyWarCommand`） | Faith | `Systems/CanonizationSystem`、`ThoughtManager`、`ReligionSimulationEventHandler`、`GreatHolyWarSystem`/`WarfareReligionSchedule` | 改为命令/事件（`PracticeRecordedEvent` 已有；补 `FaithChangedEvent`） |
| `IFaithContext`：`ICharacterQuery`/`IRealmQuery`/`IContentLookup`/事件总线 | App/Core | `FaithSystem` 当前 import `State/Definitions`（`FaithSystem.cs:3-4`） | Faith 内部只依赖这些接口，由组合根注入（构造参数或 Init） |
| `IFaithSnapshot`/`IFaithPersistence` | Faith 实现；Infrastructure 调用 | `FaithPersistence.cs`、`FaithSaveData.cs`、`SaveSystem.cs` | Faith 提供 `Capture()/Restore(snapshot)`；Save 层只编排，不直接改 `FaithSystem` 内部字典 |
| `IFaithCatalog`（或直接用 Content 的 `IContentLookup`） | Content | `ReligionCatalog` 被 Faith/Modding/UI/Rendering/Save 直接静态调用 | 内容定义放 Content；Faith 通过 `IContentLookup` 读取；UI 通过查询/ViewModel |
| `FaithConflictTriggeredEvent`/`PracticeRecordedEvent` | **已有**（Core.Events） | `ReligionSimulationEventHandler`、`CombatManager`/`BuildingSystem`/`ResearchPlanSystem` | 保留；补订阅方通过接口而非直接抓 FaithSystem |

**关键文件反转**：`FaithSystem.cs`（去掉 `using Simulation.State/Definitions`，改 `IFaithContext`）、`GreatHolyWarSystem.cs`/`WarfareReligionSchedule.cs`（依赖政治/军事→改事件/命令）、`CanonizationSystem.cs`、`CharacterManager.cs`（去 `GetFaithSystem` 直连）、`Infrastructure.Save/FaithPersistence.cs`（改快照）、`GameWorld.GetFaithSystem:2826`（改为内部字段 + 接口注册，外部不再暴露实现）。

### 7.3 Culture 独立 asmdef 的接口反转（B5）

| 契约/事件 | 说明 | 现状引用 |
|---|---|---|
| `ICultureQuery`：`GetCulture(cultureId)`、`GetRealmCulture(realmId)`、`GetDominantCulture(tile)`、`GetCultureName(id)` | Culture 实现；UI/Rendering/Economy 使用 | `UI/SettlementPeopleText`（`CultureData` 直接读）、`Systems/PopulationStats`、`Economy/ClassEmergenceEvents`、`Rendering/MapRenderer.Colors`（文化地图） |
| `ICultureCommand`：`RecordPractice`、`AdvanceStage`（或 Culture 内部调度） | Culture | `CultureStageEvolutionSystem` 由 `GameWorld` 调度（`CultureSchedule`） |
| `ICultureContext`：`ICharacterQuery`（人群/身份）、`ISettlementQuery`（聚落）、`IContentLookup`（语言/传统定义） | App 注入 | `CultureStageEvolutionSystem.cs:2-8` 当前 import `Actors/Modding/WorldState/World.Settlement` → 改为接口 |
| `CultureData` 归属 | 移入 `Simulation.Culture/State/`；`RealmData`/`SaveData` 只持 ID 或快照 | `State/CultureData.cs`、`Simulation.Modding/ContentRuntime`、`SaveData` |
| `CultureChangedEvent`/`CultureDiffusedEvent` | 新增（B8 可先加） | 目前无文化事件，Modding/Economy/UI 都直接读状态 |
| `ICultureSnapshot` | 存档适配 | `Infrastructure.Save/SaveData/SaveSystem` 直接成员访问 |

### 7.4 Laws 独立 asmdef（B6，法律/法令已合并）

- 新增 `ILawQuery`（已固化制度/现行法令/待推行/可推行候选）、`ILawCommand`（BeginEnact/Cancel/Revoke/EnactDecree）、`ILawRules`（`LawConstraints` 只读规则，替代 UI/Presentation 直读）、`ILawSnapshot`（Save 层）。
- `GovernmentPanel.cs:233-235,311,323,332,345` 改为：读取 View（`ILawQuery.GetView(realmId)`）→ 点击构造 `ILawCommand` → 组合根分发；UI 不直接 tick 法律系统（tick 由调度器/App 做）。
- `RealmData.cs:74,76` 的 `lawState/decreeState`：短期保留（`[NonSerialized]` 状态可由 Laws 系统持有）；目标 `RealmData` 只留 `realmId` 等标量，Laws 的 `RealmLawState` 由 `ILawQuery/ILawSnapshot` 暴露。
- 前置：B1（31 文件已合并、namespace 统一）+ B3（组合根注册 `ILawCommand` 实现）+ B8 事件。

---

## 8. 可安全删除 / 归位 / 待确认清单（只报告，未执行）

### 8.1 可安全归档/删除（有 0 引用证据，低风险）

| 项 | 证据 | 建议 |
|---|---|---|
| `Assets/Fonts/SourceHanSansSC-Medium.otf` | GUID `07898661...` 在 `Assets/**/*.unity/prefab/asset/mat/cs` 中 0 引用（脚本扫描） | 归档/删除（先确认无美术后续使用） |
| `Assets/Fonts/SourceHanSansSC-Regular.otf` | GUID `145eb2e5...`：0 引用 | 同上 |
| `Assets/Fonts/SourceHanSerifSC-Regular.otf` | GUID `de65e0f9...`：0 引用 | 同上 |
| `Assets/Fonts/SourceHanSerifSC-SemiBold.otf` | GUID `1eb18f0c...`：0 引用 | 同上 |
| `Assets/Audio/Music/`、`Assets/Audio/SFX/` | 仅 `.gitkeep`，实际音频在 `Resources/Audio/UI`、`StreamingAssets/Music` | 删除占位或改为统一音频 owner（音频移动需 Task 决定，避免 UI 音效 Resources 路径变化） |
| `Assets/Data/`、`Assets/Prefabs/`、`Assets/Settings/` | 仅 `.gitkeep`（无内容） | 可删除占位（若团队约定用目录占位则保留） |
| `Assets/Editor/MapEditing/`、`Assets/Fonts/Fonts/`、`Assets/TextMesh Pro/Resources/Shaders/` | 空目录 | 可删（Unity 会忽略空目录；保留也无害） |

**不删**：`Assets/Fonts/simhei.ttf`（`Main.unity` 引用，Legacy UI）、`Assets/Resources/Fonts/simhei.ttf`（`TMPFontUtility` 运行时加载）、`Assets/Art/Shaders/CyowariHistoricalMap.shader`（`CyowariMapStyle.cs:232 Shader.Find`）、`Assets/ScriptableObjects/DefaultWorldConfig.asset`（`Main.unity` 引用）。

### 8.2 归位清单（move，按批次；本卡不执行）

| 现状 | 目标 | 批次 |
|---|---|---|
| `Simulation.State/{Law,LawComposition,LawEffects}`（已移完，仅核对） | `Simulation.Laws/`（快照已完成） | 已由 task-63/65 处理 |
| `Simulation.State/{CultureData}` | `Simulation.Culture/State/` | B1 |
| `Simulation.State/{Doctrine,DoctrinePool,GreatHolyWarState}` | `Simulation.Faith/State/` | B1 |
| `Simulation.Systems/{FaithSystem,CanonizationSystem,ReligionSimulationEventHandler,WarfareReligionSchedule,GreatHolyWarSystem}` | `Simulation.Faith/Runtime/` | B1 |
| `Simulation.Definitions/{Religion*,Doctrine*,FaithType,FaithOrganizationType,SaintDef}` | `Content`（或 `Simulation.Faith/Definitions`，取决于 B2 取舍） | B1/B2 |
| `Simulation.Systems/Culture*`（5） | `Simulation.Culture/Runtime/` | B1 |
| `Simulation.Definitions/DynamicGoodsSaveData.cs` | `Infrastructure.Save/` | B1 |
| `Simulation.State/{TaxSystemDTO,RealmDTO,RaceDTO}.cs` | `Core.Dto/` | B1 |
| `Simulation.Systems/{AdminDivision,BuildingData,CharacterResearchData,ResearchPlanData,DiscoveryActors,PriorityQueue,RealmForm,PathTile,EligibilityRules,KnownCharacterInfo,...}` | 对应系统 `State/`/`Definitions/` | B1 |
| `Simulation.{Planning,Politics,Warfare,Economy,Innovation}` 的 command 文件 | 对应系统文件夹 `Commands/` | B1 |
| `Assets/UI/MyriadCreation.UI/{Bootstrap,GameManager,SceneFlowController}` | `Assets/Scripts/App/MyriadCreation.App/`（新） | B3 |
| `Assets/UI/Settings/{SettingsModel,SettingsService}` | App/Infrastructure（设置服务） | B3 |
| `Assets/Scripts/Rendering/Editing/RuntimeMapEditingController.cs` | App（输入执行器）；Rendering 只留输入适配 | B3/B7 |
| `World.Settlement` 的模拟逻辑文件 | `Simulation.Settlement` | B7 |
| `Simulation.WorldState/GameWorld.cs` 的生成/组合根部分 | `World.Generation` + `App/WorldCompositionRoot` | B3 |

### 8.3 待确认（不删除）

- `Assets/TextMesh Pro/`（80 文件，第三方 TMP 资源）：建议迁 Package Manager，但会改资源路径/GUID；待 Lead 决定。
- `Assets/Resources/Music/` 不存在，但 `MusicPlayerSystem.LoadFromResources` 期待它；`MusicPlayer` 扫描 `StreamingAssets/Music`（当前空）。音频目录约定需产品决定。
- `Assets/Scripts/Core/MyriadCreation.Core.Contracts/{ISimulationProcess,ISimulationQuery,...}.cs`：契约已定义但几乎没有系统 Adopt（查询/流程），不是死代码，是"未落地扩展"；B8 推广或删除。
- `Assets/Simulation/.../*Schedule.cs`（19 处调度）与 `Simulation.Scheduling` 的重叠：需 B3 设计调度归属后再合并。

### 8.4 审计发现的"健康项"（保持）

- 无重复 basename 的 `.cs`（脚本扫描 `Scripts` 下同名文件 = 0）→ 无重复类文件。
- `Art` 无代码/asmdef；`UI` 无 prefab/贴图（纯代码策略一致）。
- asmdef 无循环引用；`World/Core/Infrastructure/Rendering` 没有反向依赖 UI。
- 法律/法令文件已合并（task-63/65），目录命名与 namespace 一致。

---

## 9. 审计复现命令（只读；建议固化为 `tools/arch-audit`）

```bash
# 0) 目录/文件数
find Assets -name "*.cs" | wc -l
# 1) asmdef 依赖图
python - <<'PY'
import json,glob,os
for p in sorted(glob.glob('Assets/**/*.asmdef', recursive=True)):
    d=json.load(open(p,encoding='utf-8-sig'))
    print(os.path.dirname(p), '=>', d.get('name'), '| refs=', ','.join(d.get('references') or []))
PY
# 2) 跨文件夹 using 边（近似）
python - <<'PY'
import os,re,collections
root='Assets/Scripts'; edges=collections.Counter(); nsf={}
files=[(os.path.join(dp,f),os.path.basename(dp)) for dp,dn,fn in os.walk(root) for f in fn if f.endswith('.cs')]
for p,o in files:
    nsf.setdefault(o,o)
for p,o in files:
    txt=open(p,encoding='utf-8-sig',errors='ignore').read()
    for m in re.finditer(r'^using\s+(MyriadCreation\.[A-Za-z0-9_.]+)\s*;',txt,re.M):
        for k in sorted(nsf,key=len,reverse=True):
            if m.group(1)==k or m.group(1).startswith(k+'.'):
                if nsf[k]!=o: edges[(o,nsf[k])]+=1
                break
for (a,b),c in edges.most_common(30): print(a,'->',b,c)
PY
# 3) namespace 是否都在 MyriadCreation 前缀
grep -rhoE '^namespace [A-Za-z0-9_.]+' Assets/Scripts --include='*.cs' | sed 's/namespace //' | grep -v '^MyriadCreation\.' | sort -u
# 4) 0 引用资产 GUID
for f in Assets/Fonts/*.meta; do g=$(grep -o 'guid: [a-f0-9]*' "$f" | cut -d' ' -f2); echo "$f $(grep -rl "$g" Assets --include='*.unity' --include='*.prefab' --include='*.asset' --include='*.mat' --include='*.cs' | wc -l)"; done
# 5) 重复 basename .cs（重复类风险）
python - <<'PY'
import os,collections
root='Assets/Scripts'; by=collections.defaultdict(list)
for dp,dn,fn in os.walk(root):
    for f in fn:
        if f.endswith('.cs'): by[f].append(dp)
for f,ps in by.items():
    if len(ps)>1: print(f, ps)
PY
```

**口径**：using 级扫描只反映"显式依赖"，同 namespace 的类型引用不计；用于趋势/回归对比，不替代人工 review。GUID 扫描覆盖 `*.unity/*.prefab/*.asset/*.mat/*.cs`，不含 StreamingAssets。

---

## 10. 与其它卡/批次的协作约定

- 本卡**不移动、不删除**任何文件；§8 清单只是候选，由 Lead 排期。
- **与 task-63/65**：法律文件已由它们并发移动；B1 的"降级 move"必须等它们完成并重扫，否则会冲突。建议 Lead 把 B1 指派给单一执行者，其它人只读。
- **与 UI 卡（政府/聚落/设置）**：UI 目录在持续增长；建议 B3（App 拆分）**先于**新增 UI 功能卡，否则 UI 程序集继续膨胀、B3 的冲突面扩大。
- **与 Content/Modding 卡**：B2 是 Faith/Culture 独立化的前置（内容定义要先有归属）；否则 Faith asmdef 会反向依赖 Simulation。
- **每批完成后**：更新本文件 §3.2/§7.1 的基线数字，并跑一次 §9 的扫描；禁止用"感觉"判断解耦完成。
- 本文不是 ADR 替代品：每个批次建议在 `docs/architecture/adr/` 记一条 ADR（范围、接口、回滚）。

---

### 附：本次审计的"错位速查"（给 Lead 的 20 秒版）

- **UI 里不该有的**：`Bootstrap`、`GameManager`、`SceneFlowController`、`SettingsModel/Service`（App/Infra）。
- **Rendering 里不该写的**：`RuntimeMapEditingController` 直接写 `world.tiles`/`RecalculateDirty`。
- **Simulation 里不该混的**：`WorldState/GameWorld` 当组合根；`Systems` 当杂物间；`State` 当所有系统状态；`Definitions` 当运行时/存档。
- **Core 里不该有的**：`Localization` 的 UnityEngine/IO、2 个静态 registry、`TileGrid/WorldConfig` 引擎类型。
- **已经对的**：Art 无代码；程序集 DAG 干净无环；法律/法令已合并到 Laws；Infrastructure.Save 是唯一合法的跨系统适配层。
- **下一步只做**：B1 归位 → B2 Content → B3 App → B4/B5/B6 Faith/Culture/Laws → B7 视图契约 → B8 契约/事件门禁。

## 附录 A：CK3 对齐（定义是定义、脚本是脚本）

参考：[CK3 Modding Wiki — Mod structure](https://github.com/jesec/ck3-modding-wiki/blob/master/wiki_pages/Mod_structure.md)（CK3 目录约定：`common/` 放内容定义，`events/`、`gui/`、`gfx/`、`localization/`、`history/`、`map_data/`、`sound/` 放脚本与资产）。

| CK3 目录 | 职责 | 本项目对应 | 现状/目标 |
|---|---|---|---|
| `common/*`（religion/laws/culture/…） | **内容定义**（数据，不写逻辑） | `Assets/StreamingAssets/Base/**/*.json` + `ContentRegistry`/`Modding` | Religion/Ethos/Biome/Innovation 等已 JSON 化；**法律目录（task-66）正在 JSON 化**；Culture 定义后续迁移 |
| `events/` | 脚本（事件效果/触发） | `Simulation.Events` + 各系统 `*EventHandler`/`*Schedule` | 已分离 |
| `gui/` | UI 定义（布局/绑定） | `Assets/UI/**`（代码生成 UI 定义） | 纯 UI；UI 不直接写模拟状态（后续 B3/B6 反转） |
| `gfx/`、`sound/` | 美术/音频资源 | `Assets/Art`、`Assets/Resources/Audio` | Art 0 代码；音频有 SfxPlayer 总线 |
| `localization/` | 本地化 | `Assets/StreamingAssets/Base/Localization/*.json` | 已 JSON |
| `history/` | 历史/初始状态 | `Simulation.Generation`/内容覆盖 | 后续可视需要独立 JSON |
| `map_data/` | 地图数据 | `World.Generation`/`World.Terrain` + 内容表 | 地形由 seed+内容驱动 |

**本项目比 CK3 更进一步的约定**：
1. 每个大系统一个 owner 文件夹 + namespace（`Simulation.Laws`、`Simulation.Faith`、`Simulation.Culture`），内部再分 `Definitions/`（数据 schema，不含逻辑）、`State/`（运行时状态/DTO）、`Runtime/`（逻辑/调度）。
2. 跨系统只经 **Core 事件/契约** 或 **Infrastructure 兼容层**（存档/音频/命令总线）；Rendering/UI 不得直接写模拟模型（按 B3/B6 反转接口）。
3. 内容定义优先 JSON（可 mod 覆盖/热改），C# 只保留 schema + 默认回退；启动由 ContentRegistry 统一加载校验。

