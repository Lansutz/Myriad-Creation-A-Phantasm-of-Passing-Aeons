# 当前架构速查（CURRENT_ARCHITECTURE）

> **Round 35 架构状态（task-114 文档同步，2026-11-02）**。只读 asmdef/测试事实，不依赖 Unity 运行。
> 事实来源：`MyriadCreation/Assets/**/*.asmdef`（15 个）+ `Assets/Tests/EditMode/AsmdefArchitectureGateTests.cs`（Round 35 门禁）。
> 历史长文：`MyriadCreation/docs/architecture/SYSTEM_OWNERSHIP.md`（task-64 审计快照）；方案/回滚：`MyriadCreation/docs/architecture/{APP_EXTRACTION_PLAN,ASMDEF_SPLIT_FAITH_CULTURE_LAWS,RENDERING_WORLD_CONTRACTS,CONTENT_PIPELINE_FAITH_CULTURE}.md`。

---

## 1. 程序集图（15 个 asmdef）

### 1.1 运行时程序集（12 个）

| 程序集 | asmdef 目录（相对 `Assets`） | 直接 references | 层 |
|---|---|---|---:|
| `MyriadCreation.Core` | `Scripts/Core` | 无 | L0 |
| `MyriadCreation.Content` | `Scripts/Content` | Core | L1 |
| `MyriadCreation.Laws` | `Scripts/Simulation/MyriadCreation.Simulation.Laws` | Core, Content | L2 |
| `MyriadCreation.Faith` | `Scripts/Simulation/MyriadCreation.Simulation.Faith` | Core, Content | L2 |
| `MyriadCreation.Culture` | `Scripts/Simulation/MyriadCreation.Simulation.Culture` | Core, Content | L2 |
| `MyriadCreation.World` | `Scripts/World` | Core | L3 |
| `MyriadCreation.Simulation` | `Scripts/Simulation`（不含上述 3 个子 asmdef） | Core, World, Content, Laws, Faith, Culture | L4 |
| `MyriadCreation.Rendering` | `Scripts/Rendering` | **Core, Content** | L5 |
| `MyriadCreation.Infrastructure` | `Scripts/Infrastructure` | Core, World, Simulation, Content, Faith, Culture | L5 |
| `MyriadCreation.App.Contracts` | `Scripts/App/MyriadCreation.App.Contracts` | Core, World, Simulation | L6 |
| `MyriadCreation.App` | `Scripts/App`（`App.Contracts/` 为子程序集） | Core, Infrastructure, UnityEngine.UI, App.Contracts, World, Simulation, Rendering | L6 |
| `MyriadCreation.UI` | `Assets/UI` | **Core、Rendering、UnityEngine.UI、Unity.TextMeshPro、Infrastructure、World、Content、App、App.Contracts** | L7 |

- **UI 不再引用 Simulation 家族**：UI refs 中无 `MyriadCreation.Simulation` / `Laws` / `Faith` / `Culture`。
- `App` 不引用 UI；UI 必引 App（反向依赖通过 `App.Contracts` 接口断开）。
- `Laws` / `Faith` / `Culture` refs 均为 `{Core, Content}`。
- `Rendering` refs 恰为 `{Core, Content}`（不再引 World/Simulation/App.Contracts）。

### 1.2 工具/测试程序集（3 个，门禁豁免）

| 程序集 | 目录 | 说明 |
|---|---|---|
| `MyriadCreation.Editor` | `Assets/Editor` | Editor-only；运行时全集 + UI/TMP |
| `MyriadCreation.Tests` | `Assets/Tests/EditMode` | EditMode；全量运行时 + 测试框架（可引 Simulation 家族） |
| `MyriadCreation.Tests.PlayMode` | `Assets/Tests/PlayMode` | PlayMode；运行时 + App + TestRunner/TMP |

### 1.3 依赖方向

```text
L0  Core（无引用）
      ↑
L1  Content（Core）
      ↑
L2  Laws / Faith / Culture（Core + Content）
      ↑
L3  World（Core）
      ↑
L4  Simulation（Core + World + Content + Laws + Faith + Culture）
      ↑
L5  Rendering（Core + Content）        Infrastructure（Core + World + Simulation + Content + Faith + Culture）
      ↑                                  ↑
L6  App.Contracts（Core + World + Simulation） ← App（Core + Infrastructure + World + Simulation + Rendering + App.Contracts + UGUI）
      ↑
L7  UI（Core + Rendering + Infrastructure + World + Content + App + App.Contracts + UGUI/TMP；无 Simulation 家族）
```

- asmdef DAG **无环**；除测试/Editor 豁免项外无向上引用。
- UI→App 方向保持；App→UI 禁止。

---

## 2. 架构门禁（Round 35）

实现：`Assets/Tests/EditMode/AsmdefArchitectureGateTests.cs`（4 条 Test：主门禁、UI 硬边界、Rendering 两条边界）。

| 规则 | 内容 | 状态 |
|---|---|---|
| R1 无环 | 解析全部 asmdef，DFS 检测环 | ✅ 0 违规 |
| R2 Content 边界 | Content refs 禁 Simulation/World/UI/Rendering/Infrastructure/App | ✅ |
| R3 子系统边界 | Laws/Faith/Culture refs ⊆ `{Core, Content}`；禁上层依赖 | ✅ |
| R4 App/UI 方向 | App 禁引 UI；UI 必引 App | ✅ |
| R5 分层序 | Core < Content < {Laws,Faith,Culture} < World < Simulation < {Rendering,Infrastructure} < {App.Contracts,App} < UI；越级即失败 | ✅ 无白名单、无越级 |
| R6 Rendering | Rendering refs 恰为 `{Core, Content}`；禁 App.Contracts/World/Simulation | ✅ |
| R7 UI 边界 | **UI 不得引用 Simulation/Laws/Faith/Culture**（硬断言） | ✅ |
| R8 豁免 | Editor / Tests / Tests.PlayMode 仅豁免分层序，不被业务 asmdef 引用 | — |

- `KnownTransitionEdges` 白名单已删除；不再存在 UI→Simulation 过渡例外。
- 当前门禁静态模拟（15 asmdef）**0 违规**；Round 35 基线：EditMode **552/552**、PlayMode **9/9**（Lead 执行）。

---

## 3. 目录职责（摘要）

| 程序集 | owner 目录 | 职责/边界 |
|---|---|---|
| Core | `Scripts/Core/{MyriadCreation.Core, .Constants, .Contracts, .Data, .Dto, .Enums, .Events}` | 纯契约/DTO/枚举/事件/跨系统只读数据；不反向依赖任何层 |
| Content | `Scripts/Content/MyriadCreation.Content.Definitions` | 内容定义/目录/JSON 内容；只引 Core；Modding 流水线仍在 Simulation |
| Laws/Faith/Culture | `Scripts/Simulation/MyriadCreation.Simulation.{Laws,Faith,Culture}` | 域模型；只引 Core+Content；host glue 在 Simulation |
| World | `Scripts/World/**` | 地图/地形/气候/水文/锚点/聚落数据模型；只引 Core |
| Simulation | `Scripts/Simulation/**` | 模拟系统 + host glue + 调度；`GameWorld` partial 组合根/数据容器仍在收口 |
| Rendering | `Scripts/Rendering/{MyriadCreation.Rendering, Editing}` | 视图 + 编辑输入/命令执行；经 Core.Contracts 视图契约读世界；只引 Core+Content |
| Infrastructure | `Scripts/Infrastructure/{Save,Audio}` | 存档/读档、音频兼容层；不引 UI/Rendering |
| App.Contracts | `Scripts/App/MyriadCreation.App.Contracts` | UI↔App 反向接口（`IGameUiHost`/`IGameSession`/`ISettlementDetailUi` 等） |
| App | `Scripts/App`（Bootstrap/GameManager/SceneFlow/Settings/Editing） | 组合根/启动/场景流/地图编辑执行；不引 UI |
| UI | `Assets/UI/{Common,Panels,MyriadCreation.UI.Presentation}` | 面板/控件/展示模型；只消费 Core/App/App.Contracts + 非 Simulation 运行时程序集 |
| Editor/Tests | `Assets/Editor`、`Assets/Tests/**` | 工具与测试；门禁豁免 |

---

## 4. 批次状态（Round 35）

### 4.1 旧编号（B0–B6，全部 ✅）

B0 清理非法 `.csproj`；B1 平铺目录归位；B2 结算经济簇→World.Settlement；B3 删 `MapSaveSystem`/Infrastructure 去 Rendering；B4 Presentation 拆分；B5 灰区调度原语归位；B6 TMP 示例/Editor 收尾。

### 4.2 新编号（B1–B8，全部 ✅）

| 批次 | 内容 | 状态 |
|---|---|---|
| B1 | 同 asmdef owner 归位 | ✅ |
| B2 | 抽 `MyriadCreation.Content` | ✅ |
| B3 | App/App.Contracts 抽取；**Step 1–4（服务层 + 迁移）✅** | ✅ |
| B4 | Faith 独立（Core+Content） | ✅ |
| B5 | Culture 独立（Core+Content） | ✅ |
| B6 | Laws 独立（Core+Content） | ✅ |
| B7 | World/Rendering 视图契约；Rendering 去 Simulation/App.Contracts/World | ✅ |
| B8 | 契约/事件推广 + UI 4d + 架构门禁；UI 去 Simulation 家族 | ✅ |

Round 35 基线：EditMode **552/552**，PlayMode **9/9**。

---

## 5. 已知遗留

1. **GameWorld 组合根 hooks 进行中**：`GameWorld` 仍以 partial 形式位于 `MyriadCreation.Simulation`，组合根/数据容器纯化与 hook 注入收尾未完成。
2. **UI 地块/角色面板契约补齐中**：UI 已去 Simulation 家族，但地块详情/角色详情/家族树等无对应聚合契约，当前为安全空/最小实现，后续补 `IWorldReadModel` 查询与面板。
3. **App.Contracts 仍引用 Simulation/World**：`IGameSession`/`IGameUiHost` 等契约的 World 类型仍在 App.Contracts 签名中；需继续下沉/精简。
4. **Modding 仍在 Simulation**：`Simulation.Modding`/`ContentRegistry` 内容加载/校验流水线尚未迁入 Content。
5. **Editor/Tests 豁免**：Editor/Tests/Tests.PlayMode 全量引用运行时程序集，是门禁豁免的第三处耦合。
6. 其他既有遗留（法律最小回退、旧 Simulation.Systems/State 归位、文档位置分散）保持原状，详见历史审计计划。

---

## 6. 维护规则

- 新功能/新内容的放置规则见 [ARCHITECTURE_RULES.md](ARCHITECTURE_RULES.md)；当前总计划见 [UNIFIED_MASTER_PLAN.md](UNIFIED_MASTER_PLAN.md)。所有 asmdef/owner 变化必须同步本文件与门禁。


- asmdef/`references`/owner 变化 → 同步本文件、`docs/architecture/SYSTEM_OWNERSHIP.md`、`docs/audit/ARCH-COMPLETION-PLAN.md` 顶部摘要与门禁测试。
- 依赖方向以 asmdef `references` 为唯一事实来源；`using` 扫描只作趋势辅助。
- UI 边界为硬门禁：新增 UI 代码不得引入 `MyriadCreation.Simulation/Laws/Faith/Culture`；需要数据时扩展 Core 契约并在 App/Simulation 侧实现注入。
