# 系统职责与兼容层规范（SYSTEM_OWNERSHIP，Round 35 当前状态版）

> **Round 35 架构状态（task-114 文档同步，2026-11-02）**。本文是仓库根最新“系统 owner / 程序集图 / 门禁 / 批次”口径；
> 历史长文保留在 `MyriadCreation/docs/architecture/SYSTEM_OWNERSHIP.md`（task-64 审计快照）。
> 事实来源：直接解析 `MyriadCreation/Assets/**/*.asmdef`（15 个）+ `AsmdefArchitectureGateTests`。

---

## 1. 程序集图与依赖（15 asmdef）

### 1.1 运行时 12 个

| 程序集 | owner 目录 | references |
|---|---|---|
| `MyriadCreation.Core` | `Scripts/Core` | 无 |
| `MyriadCreation.Content` | `Scripts/Content` | Core |
| `MyriadCreation.Laws` | `Scripts/Simulation/MyriadCreation.Simulation.Laws` | Core, Content |
| `MyriadCreation.Faith` | `Scripts/Simulation/MyriadCreation.Simulation.Faith` | Core, Content |
| `MyriadCreation.Culture` | `Scripts/Simulation/MyriadCreation.Simulation.Culture` | Core, Content |
| `MyriadCreation.World` | `Scripts/World` | Core |
| `MyriadCreation.Simulation` | `Scripts/Simulation` | Core, World, Content, Laws, Faith, Culture |
| `MyriadCreation.Rendering` | `Scripts/Rendering` | **Core, Content** |
| `MyriadCreation.Infrastructure` | `Scripts/Infrastructure` | Core, World, Simulation, Content, Faith, Culture |
| `MyriadCreation.App.Contracts` | `Scripts/App/MyriadCreation.App.Contracts` | Core, World, Simulation |
| `MyriadCreation.App` | `Scripts/App` | Core, Infrastructure, UnityEngine.UI, App.Contracts, World, Simulation, Rendering |
| `MyriadCreation.UI` | `Assets/UI` | **Core、Rendering、UnityEngine.UI、Unity.TextMeshPro、Infrastructure、World、Content、App、App.Contracts** |

### 1.2 工具/测试 3 个（门禁豁免）

`MyriadCreation.Editor`（`Assets/Editor`）、`MyriadCreation.Tests`（`Assets/Tests/EditMode`）、`MyriadCreation.Tests.PlayMode`（`Assets/Tests/PlayMode`）。

### 1.3 依赖方向

```text
Core
 ← Content
      ← Laws / Faith / Culture（均只引 Core + Content）
      ← World（只引 Core）
           ← Simulation（Core/World/Content/Laws/Faith/Culture）
                ← Rendering（Core + Content）
                ← Infrastructure（Core/World/Simulation/Content/Faith/Culture）
                     ← App.Contracts（Core/World/Simulation）
                     ← App（Core/Infrastructure/World/Simulation/Rendering/App.Contracts/UGUI）
                          ← UI（Core/Rendering/Infrastructure/World/Content/App/App.Contracts/UGUI/TMP；不含 Simulation 家族）
```

- App 不引用 UI；UI 必引 App。
- **UI refs 不含 `MyriadCreation.Simulation` / `Laws` / `Faith` / `Culture`**。
- Rendering refs 恰为 `{Core, Content}`；Laws/Faith/Culture refs 均为 `{Core, Content}`。
- asmdef DAG **无环**；无向上引用（Editor/Tests 豁免）。

---

## 2. 当前门禁状态

实现：`Assets/Tests/EditMode/AsmdefArchitectureGateTests.cs`（4 条 Test：主门禁、UI 硬边界、Rendering 两条）。

| 规则 | 结果 |
|---|---|
| 无环 | ✅ 0 违规 |
| Content 边界（禁 Simulation/World/UI/Rendering/Infrastructure/App） | ✅ |
| Laws/Faith/Culture refs ⊆ `{Core, Content}` 且禁上层依赖 | ✅ |
| App 不引 UI；UI 必引 App | ✅ |
| 分层序无向上引用（**无白名单**） | ✅ |
| Rendering refs 恰为 `{Core, Content}`，不引 App.Contracts | ✅ |
| **UI 禁 Simulation/Laws/Faith/Culture（硬断言）** | ✅ |

结论：`KnownTransitionEdges` 白名单已删除；门禁静态模拟 **0 违规**。
Round 35 基线：EditMode **552/552**、PlayMode **9/9**。

---

## 3. 批次执行状态

### 3.1 旧编号 B0–B6（全部 ✅）

B0 清非法 `.csproj`；B1 平铺目录归位；B2 结算经济簇→World.Settlement；B3 删 `MapSaveSystem`/Infrastructure 去 Rendering；B4 Presentation 拆分；B5 灰区调度原语归位；B6 TMP 示例/Editor 收尾。

### 3.2 新编号 B1–B8（全部 ✅）

- B1 owner 归位；B2 Content 独立；**B3 App/App.Contracts 抽取，Step 1–4（服务层+迁移）✅**；
- B4 Faith、B5 Culture、B6 Laws 独立（均 Core+Content）；
- B7 World/Rendering 视图契约，Rendering 去 Simulation/App.Contracts/World；
- B8 契约/事件推广 + UI 4d（去 Simulation 家族/删 Legacy）+ 架构门禁。

---

## 4. 系统 owner 清单

| 系统 | owner | 程序集 | 职责与边界 |
|---|---|---|---|
| Core 契约/DTO/事件 | `Scripts/Core/*` | Core | 纯契约/DTO/枚举/事件/跨系统只读数据；不被反向依赖 |
| 内容/定义 | `Scripts/Content/MyriadCreation.Content.Definitions` | Content | 内容定义/目录/JSON；只引 Core；Modding 加载流水线仍在 Simulation |
| 法律/法令 | `...Simulation.Laws` + `LawHost` | Laws + Simulation(host) | 法律/法令域模型；JSON 唯一完整来源，缺失时最小回退 |
| 信仰 | `...Simulation.Faith` + `FaithHost` | Faith + Simulation(host) | 信仰域模型 + host 接线 |
| 文化 | `...Simulation.Culture` + `CultureHost` | Culture + Simulation(host) | 文化域模型 + host 接线 |
| 地图/地形/气候/水文/锚点/聚落 | `Scripts/World/**` | World | 世界生成算法与数据模型；只引 Core |
| 模拟系统 | `Scripts/Simulation/**` | Simulation | 模拟逻辑 + host glue + 调度；`GameWorld` 组合根/数据容器仍在收口 |
| 渲染/视图 | `Scripts/Rendering/{MyriadCreation.Rendering,Editing}` | Rendering | 只读视图 + 地图编辑输入/命令；经 Core.Contracts 视图契约读世界 |
| 存档/音频 | `Scripts/Infrastructure/{Save,Audio}` | Infrastructure | Save/Load、Persistence、音频总线；不引 UI/Rendering |
| App 契约 | `Scripts/App/MyriadCreation.App.Contracts` | App.Contracts | UI↔App 反向接口：`IGameUiHost`/`IGameSession`/`ISettlementDetailUi`/`IUiFactory` 等 |
| App 组合根 | `Scripts/App`（Bootstrap/GameManager/SceneFlow/Settings/Editing） | App | 启动/场景流/设置/世界编辑执行；不引 UI |
| UI | `Assets/UI/{Common,Panels,MyriadCreation.UI.Presentation}` | UI | 面板/控件/展示模型；只消费 Core/App/App.Contracts 与非 Simulation 运行时 |
| Editor/Tests | `Assets/Editor`、`Assets/Tests/**` | Editor/Tests | 工具与测试；门禁豁免 |

---

## 5. 已知遗留

1. **GameWorld 组合根 hooks 进行中**：仍以 partial 形式编在 `MyriadCreation.Simulation`，组合根/数据容器纯化未收尾。
2. **UI 地块/角色面板契约补齐中**：地块详情/角色/家族树暂无聚合查询契约，当前安全空/最小实现，需后续扩展 `IWorldReadModel` 查询与面板。
3. **App.Contracts 仍引 Simulation/World**：`IGameSession`/`IGameUiHost` 等签名中的 World 类型仍在，待继续下沉/精简。
4. **Modding 在 Simulation**：`Simulation.Modding`/`ContentRegistry` 加载/校验流水线未迁 Content。
5. **Editor/Tests 豁免**：全量引用运行时程序集，是门禁豁免的第三处耦合。
6. 历史遗留（法律最小回退、Simulation.Systems/State 归位、文档分散）保持原状，详见 `docs/audit/ARCH-COMPLETION-PLAN.md`。

---

## 6. 维护约定

- asmdef / references / owner 迁移 → 同步本文、`docs/architecture/CURRENT_ARCHITECTURE.md`、`docs/audit/ARCH-COMPLETION-PLAN.md` 顶部摘要与门禁测试。
- 依赖方向以 asmdef `references` 为唯一事实来源；using 扫描只作趋势辅助。
- UI 边界为硬门禁：新增 UI 代码不得引入 Simulation/Laws/Faith/Culture using；数据需求通过 Core.Contracts + App/Simulation 注入解决。
- 历史快照：`MyriadCreation/docs/architecture/SYSTEM_OWNERSHIP.md`（task-64）仅作历史阅读，当前口径以本文为准。
