# Rendering 去 World 依赖方案（B7 后续 / B8 前置）

> 类型：只读方案（B7-next）。依据：当前 worktree 静态扫描（2026-11-01）。
> 相关：`docs/architecture/ASMDEF_SPLIT_FAITH_CULTURE_LAWS.md`、`SYSTEM_OWNERSHIP.md`、`APP_EXTRACTION_PLAN.md`、`B-arch-recovery.md`。
> 目标：`MyriadCreation.Rendering.asmdef` 最终 **references = { MyriadCreation.Core, MyriadCreation.Content }**（App.Contracts 将由 B8-final 删除）。

---

## 0. 结论摘要

1. **实际 World 类型依赖只有 3 个、集中在 2 个文件**（外加 1 个接口文件）：
   - `SiteMode` / `SettlementForm`（World.Settlement 枚举）→ `MapRenderer.Texture.cs` 5+2 处；
   - `MapSelection`（World.Editing 类）→ `Editing/RuntimeMapEditingController.cs`（字段/属性/new）与 `Editing/IMapEditCommandService.cs`（2 个方法签名）。
2. Rendering 里 **16 行 `using MyriadCreation.World.*` 中大部分是 B7 拆分后的残留**：`Generation/Hydrology/Terrain/Anchor/World` 命名空间在 Rendering **0 类型引用**；World 内 `this` 扩展方法 **0 个**（已核对，排除扩展方法隐式依赖）。
3. 因此不需要大改：**3a 枚举下沉、3b 清残留/DTO 确认、3c MapSelection 下沉、3d 删 World 引用** 四步即可；其中 3b 基本是删除/文档工作（B7-part2 已把水/地形/虚控制计算全部移到 Core.Contracts DTO）。
4. **门禁**：每步 Unity 编译 `COMPILE_OK` + 定向测试；最后全量 EditMode/PlayMode。**回滚点**：每步只动“移动文件 + using + asmdef”，按步回滚目录/文件即可。
5. 预计改动：约 **27-30 个文件**（3a 为主要量：移动 3 个枚举定义文件 + 23 个消费方补 using + Rendering 删 1 行 using；union 40 个引用文件中 14 个已自带 Core using）；Rendering 本身仅需删 16 行 using + 2 个编辑类型 using + 1 个 asmdef ref 列表。

---

## 1. 现状

### 1.1 程序集与文件规模

- `Scripts/Rendering/MyriadCreation.Rendering.asmdef` references：
  `MyriadCreation.Core`、`MyriadCreation.World`、`MyriadCreation.Content`、`MyriadCreation.App.Contracts`。
- Rendering 共 **11 个 .cs / 3329 行**（B7 后已完全不依赖 Simulation；`App.Contracts` 无代码符号使用，见 §2-C/§3）。
- 文件：`Editing/{RuntimeMapEditingController, IMapEditCommandService}`、`MyriadCreation.Rendering/{CyowariMapStyle, IMapEditController, MapLayerConfig, MapLayerRegistry, MapRenderer, MapRenderer.Colors, MapRenderer.Nomadic, MapRenderer.Texture, SettlementIconLayer, SphericalMapRenderer}`。

### 1.2 逐文件 World 依赖矩阵

| 文件（相对 `Scripts/Rendering/`） | World `using` 行 | 实际使用的 World 类型（行号） | 结论 |
|---|---|---|---|
| `MyriadCreation.Rendering/MapRenderer.Texture.cs` | 10 Generation / 11 Hydrology / 12 Settlement / 13 Terrain / 14 Anchor | `SiteMode`：189、191、192、208、210；`SettlementForm`：189、210 | 仅 `Settlement` 两个枚举真实依赖；其余 4 个 using 残留（0 引用） |
| `Editing/RuntimeMapEditingController.cs` | 2 Editing | `MapSelection`：25（字段）、29（属性）、228（`new MapSelection(width,height)`）；变量使用 141 等 | 仅 `MapSelection` 真实依赖 |
| `Editing/IMapEditCommandService.cs` | 2 Editing | `MapSelection`：36（`MoveSelection` 参数）、39（`RaiseSelection` 参数） | 仅 `MapSelection` 真实依赖 |
| `MyriadCreation.Rendering/MapRenderer.cs` | 11 Generation / 12 Hydrology / 13 Settlement / 14 Terrain / 21 `World`（根） | **0 处** World 类型引用 | 5 行 using 全残留 |
| `MyriadCreation.Rendering/MapRenderer.Colors.cs` | 10 Generation / 11 Hydrology / 12 Settlement / 13 Terrain | **0 处** World 类型引用 | 4 行 using 全残留 |
| `MyriadCreation.Rendering/CyowariMapStyle.cs` | — | — | 无 World 依赖 |
| `MyriadCreation.Rendering/MapLayerConfig.cs` | — | — | 无 World 依赖（用 Content/Core） |
| `MyriadCreation.Rendering/MapLayerRegistry.cs` | — | — | 无 World 依赖 |
| `MyriadCreation.Rendering/MapRenderer.Nomadic.cs` | — | — | 无 World 依赖 |
| `MyriadCreation.Rendering/SettlementIconLayer.cs` | — | — | 无 World 依赖（Core.Contracts/Content） |
| `MyriadCreation.Rendering/SphericalMapRenderer.cs` | — | — | 无 World 依赖（纯 UnityEngine） |

> 扫描同时确认：World 命名空间内 `this` 扩展方法 = **0**，因此不存在“using 删了但扩展方法调用失联”的隐式依赖。
> 实际 World 符号引用合计：**3 类型 / 10 行代码**；World `using` 行合计 **16 行**（其中 13 行是残留）。

### 1.3 World 枚举定义（3a 对象）

| 枚举 | 定义 | 值 | 当前引用面（代码级） |
|---|---|---|---|
| `SiteMode` | `Scripts/World/MyriadCreation.World.Settlement/SiteMode.cs` | 0 SedentarySettlement / 1 FortifiedOutpost / 2 TemporaryEncampment | 28 文件 |
| `SettlementForm` | `.../SettlementForm.cs` | 0 Village / 1 City | 16 文件 |
| `SettlementLevel` | `.../SettlementLevel.cs` | 0 LevelI … 4 LevelV | 17 文件 |

三者代码级引用 **union = 40 文件**（含 3 个定义文件）；其中 **26 文件当前没有 `using MyriadCreation.Core.Enums;`**（需在 3a 增行）。`Core.Contracts/MapSettlementView.cs` 只在**注释**里提到这些枚举名（字段用 int），无代码/using 改动。无 FQN 引用（`MyriadCreation.World.Settlement.SiteMode` 等 0 处）。

### 1.4 其余 World 命名空间的零依赖证据

`Generation` / `Hydrology` / `Terrain` / `Anchor` / `MyriadCreation.World`（根）在 Rendering **0 类型引用**；`using` 仅为 B7 前遗留（见 §1.2）。水/地形/虚控制的“计算”已不在 Rendering（见 §2-B）。

---

## 2. 分类与方案

### A. 纯枚举（SiteMode / SettlementForm / SettlementLevel）

**推荐：下沉 `MyriadCreation.Core.Enums`（命名空间改为 `MyriadCreation.Core.Enums`，枚举名/成员名/数值/顺序完全不变）。**

- 兼容性：JsonUtility 对枚举字段按数值序列化；类型命名空间变化不影响存档字段名/值；枚举不是 ScriptableObject/Asset，场景/资产无类型引用；`HabitationSiteSaveData` 等 DTO 字段类型仅改 using。
- 改动量：移动 3 个 `.cs` + `.meta`；**23 个消费方文件**加 `using MyriadCreation.Core.Enums;`（3 个定义文件本身随迁移改 namespace；union 40 中其余 14 个已自带 Core using；完整清单见 §5）；Rendering 的 `MapRenderer.Texture.cs` 本来已有 `using MyriadCreation.Core.Enums;`（:8），只需删 `using MyriadCreation.World.Settlement;`（:12），`(int)SiteMode.*` / `(int)SettlementForm.City` 代码不变。
- 明确不做：Rendering 自建镜像枚举（重复定义、序列化/比较漂移，不推荐）。
- 备选（若想进一步压缩本批 diff）：**A2 = DTO 标志位**——`Core.Contracts/MapSettlementInfo` 增加 `bool isCity / isFortifiedOutpost / isTemporaryCamp`，由 `Simulation.WorldState/MapWorldViewAdapter` 填充，`MapRenderer.Texture` 改为读 bool（3 文件：DTO、adapter、纹理）；枚举留在 World（World/Simulation 内部继续用）。代价是 DTO 长大后仍属“视图契约”，可接受；但任务 A 的主线仍建议枚举下沉（语义共享、消除 World 根依赖更彻底）。

### B. 计算逻辑（VirtualControlCalculator / 水文 / 地形分类）

**现状：B7-part2 已完成 host 化，Rendering 无直接计算依赖。**

- 契约（已在 `Core.Contracts/`）：
  - `IMapWorldView`（宽高/地块只读/`IsProvinceBorder` 等纯查询）、`IMapSettlementView` + `MapSettlementInfo`（聚落：anchorId/tileIndex/name/isMajor/isCapital/isPort/isRuined/**siteMode:int/settlementForm:int/settlementLevel:int**/population）；
  - `IMapOverlayView` + `MapVirtualControlOverlay`（`realmByTile`/`connectionFrom/To`）、`MapAdminDivisionOverlay`、`MapInfluenceOverlay`（虚控制/行政区/影响力数组）。
  - host 实现：`Simulation.WorldState/MapWorldViewAdapter`（同一适配器实现上述接口）；Rendering 通过 `MapRenderer.WorldView`/`SetWorldView`/`TryGet*Overlay` 只读消费。
- Rendering 中 `VirtualControlCalculator` / 水文 / 地形分类类型引用 = **0**；`MapRenderer.Texture` 的 `DrawVirtualControlOverlay` 只读 `MapVirtualControlOverlay`（并带 `_cachedVirtualControlFrame` 帧缓存）；`MapRenderer.cs:692-694` 的省界判断也只走 `IMapWorldView.IsProvinceBorder`；河流/地形颜色来自 `Core.Data.TileData`（Core 层）。
- 因此 B 不需要新增大改；3b 仅做契约收尾（可选 A2 标志位、注释/文档、删除残留 using）。设计原则写在此，供后续扩展：
  1. host 侧在 `BindWorld`/世界切换/读档后一次性计算或按需缓存，Rendering 不每帧回调 host 计算；
  2. DTO 只用 Core/Content 类型（数组/字典/int/bool/float），不暴露 World/Simulation 类型；
  3. Rendering 只读，不持有 World 实例（`RuntimeMapEditingController` 的 `world` 仅为 Unity 兼容壳，见 §2-C）。

### C. 编辑历史 / 命令（MapSelection / MapEditHistory）

**现状（未移到 App，仍在 `World.Editing`）：**

| 类型 | 位置 | 使用方 |
|---|---|---|
| `MapSelection` | `World.Editing/MapSelection.cs`（纯 C#：width/height/bool[]/SelectRect/Clear/Count） | Rendering（`IMapEditCommandService` 参数、`RuntimeMapEditingController` 字段）；App `WorldEditService`（120/130）；`World.Editing/{MoveSelection,RaiseSelection}Command`；Tests 2 文件 |
| `MapEditHistory` | `World.Editing/MapEditHistory.cs` | App `WorldEditService`（20/171-175）；Tests 2 文件；Rendering 仅注释提及（`IMapEditCommandService.cs:35`），**无类型引用** |
| `IMapEditCommand` / `MoveSelectionCommand` / `RaiseSelectionCommand` | `World.Editing/` | App `WorldEditService`（125/135）；Tests；Rendering 无类型引用 |

- **Rendering 只需要 `MapSelection`**；`MapEditHistory`/命令类型不进入 Rendering 代码。
- **推荐**：`MapSelection` → `Core.Contracts`（零依赖纯类，命名空间 `MyriadCreation.Core.Contracts`）。更新 7 处使用方：`App/Editing/WorldEditService.cs`、`Rendering/Editing/IMapEditCommandService.cs`、`Rendering/Editing/RuntimeMapEditingController.cs`、`World.Editing/MoveSelectionCommand.cs`、`World.Editing/RaiseSelectionCommand.cs`、`Tests/EditMode/MapEditCommandServiceTests.cs`、`Tests/EditMode/MapEditCommandTests.cs`（加 `using MyriadCreation.Core.Contracts;`）。
- `MapEditHistory`/`IMapEditCommand`/命令类**本批不迁**（App 仍可合法引用 World；不属于 Rendering 依赖）。后续若 App 去 World，再迁 `App/Editing/`（已有 `WorldEditService` 同目录）；Rendering 届时无需改。
- Rendering 的 `using MyriadCreation.World.Editing;`（2 文件）在此步删除；`App.Contracts` 在 Rendering **无代码符号**（仅 `RuntimeMapEditingController.cs:11` 注释），3c/3d 可删 asmdef 引用（B8-final 会删除整个 App.Contracts，不影响 Rendering）。

---

## 3. 目标 asmdef 与分步顺序

**最终目标**：`MyriadCreation.Rendering.asmdef` references =
`["MyriadCreation.Core", "MyriadCreation.Content"]`（App.Contracts 由 B8-final 删除；当前它已无代码引用，可提前移除）。

### 3.1 分步表

| 步骤 | 内容 | 改动文件数（估） | 门禁 | 回滚点 |
|---|---|---|---|---|
| **3a 枚举下沉** | `SiteMode/SettlementForm/SettlementLevel` → `Core.Enums`（3 文件+meta；23 个消费方加 using；`MapRenderer.Texture` 删 World.Settlement using） | ~27 | 编译 + EditMode 全量（重点 Settlement*/WallDefense/SiteTypeLabels）+ PlayMode SettlementIconLayer | 移回 3 文件 + 还原 24 行 using |
| **3b 地形/水文 DTO 收尾** | 删 `MapRenderer.cs`/`Colors.cs`/`Texture.cs` 的残留 World using（Generation/Hydrology/Terrain/Anchor/World 根）；确认 B 契约覆盖；可选 A2 标志位 | 3 文件（+可选 DTO/adapter 2） | 编译；`grep Rendering World 类型=0` | 恢复 3 个 using 行 |
| **3c MapSelection 下沉** | `MapSelection` → `Core.Contracts`；更新 7 使用方；删 Rendering `using World.Editing`；删 Rendering asmdef 的 App.Contracts | 1 移动 + 7 使用 + 2 行 using + 1 asmdef | 编译 + `MapEditCommand*`/`MapEditCommandServiceTests` | 移回 MapSelection + 还原 using/asmdef |
| **3d 删 World 引用** | Rendering asmdef 删 `MyriadCreation.World`；全仓确认 Rendering 无 `using MyriadCreation.World`/World 类型 | 1 asmdef | 编译 + 全量 EditMode/PlayMode + 存读档 smoke | 把 World 加回 references |

> 3a/3b 可合并一次提交验证；3c 独立（涉及 App/Tests）；3d 最后（删依赖）。

### 3.2 每步“最小首步 3 项”可执行清单

**3a（枚举下沉）**
1. `git mv`/move `SiteMode.cs`、`SettlementForm.cs`、`SettlementLevel.cs`（+meta）到 `Scripts/Core/MyriadCreation.Core.Enums/`，namespace 改 `MyriadCreation.Core.Enums`（成员/数值不动）。
2. 给 23 个消费方文件补 `using MyriadCreation.Core.Enums;`（World/Settlement 同命名空间文件也必须补；3 个定义文件随迁移改 namespace）；删除 `MapRenderer.Texture.cs` 的 `using MyriadCreation.World.Settlement;`。
3. 编译 + 跑 Settlement 相关 EditMode/PlayMode 定向测试；核对 `HabitationSiteSaveData`/`SettlementData` 存档字段与旧 JSON 一致。

**3b（DTO/残留清理）**
1. 删除 `MapRenderer.cs`（11-14、21）、`MapRenderer.Colors.cs`（10-13）、`MapRenderer.Texture.cs`（10/11/13/14，Settlement 在 3a 删）的残留 World using。
2. `grep -rn 'MyriadCreation.World' Scripts/Rendering` 结果仅剩 Editing 的 2 行（3c 处理），类型引用 0。
3. （可选）若不做 3a 而选 A2：给 `MapSettlementInfo` 加 3 个 bool、`MapWorldViewAdapter` 填值、`MapRenderer.Texture` 改用 bool；否则本步只文档确认 B 契约（`IMapOverlayView`/`IMapSettlementView` 已覆盖虚控制/地形颜色所需数据）。

**3c（MapSelection 下沉）**
1. 移动 `MapSelection.cs`+meta 到 `Scripts/Core/MyriadCreation.Core.Contracts/`，namespace 改 `MyriadCreation.Core.Contracts`（类体不动）。
2. 更新 7 个使用方 using（App/Rendering/World.Editing 两个命令/Tests×2）；Rendering 删 `using MyriadCreation.World.Editing;`（2 文件）。
3. 编译 + `MapEditCommandTests`/`MapEditCommandServiceTests`；Rendering asmdef 删除 `MyriadCreation.App.Contracts` 引用（当前 0 代码符号）。

**3d（删 World 引用）**
1. `MyriadCreation.Rendering.asmdef` references 改为 `Core, Content`。
2. `grep -rn 'using MyriadCreation.World' Scripts/Rendering` = 0；`grep -rn 'MyriadCreation.World' Scripts/Rendering` 仅允许注释。
3. 全量 EditMode + PlayMode（MapVisibility/SettlementIconLayer）+ 存档回归；失败恢复 asmdef 引用。

---

## 4. 风险与兼容

1. **枚举下沉**：仅命名空间变化；枚举数值/名称/顺序不变 → JsonUtility/存档字段不变；无 ScriptableObject/场景类型引用。风险主要是 26 处 using 漏改（编译即暴露）；`World.Settlement` 内文件移动后必须自带 Core using。
2. **MapSelection 移动**：纯 C# 无 UnityEngine 依赖；App/Rendering/World.Editing/Tests 编译期暴露漏改；`IMapEditCommandService` 签名不变。
3. **App.Contracts**：Rendering 仅 1 行注释提到 `AppServices`，无代码依赖；删引用安全；B8-final 删除程序集后 Rendering 无影响。
4. **性能/刷新**：B 契约是只读 DTO + `_cachedVirtualControlFrame` 帧缓存；3a/3c 不改刷新路径，无性能风险。
5. **测试**：3a 定向 Settlement*/WallDefense/SiteTypeLabels；3c MapEditCommand*；3d 全量 + PlayMode 地图渲染。调试入口：`Logs/editmode-results.xml`、`Logs/playmode-gfx-results.xml`。
6. **并发**：当前 worktree 有其它卡的改动（MapWorldView/AsmdefArchitectureGateTests 等），执行 3a-3d 前先 `git status` 确认无同文件冲突；本方案只读产出，未改任何文件。

---

## 5. 附：3a 涉及的文件清单

**A) 3 个被移动的定义文件**（移入 `Scripts/Core/MyriadCreation.Core.Enums/`，namespace 改 `MyriadCreation.Core.Enums`）：

```
Scripts/World/MyriadCreation.World.Settlement/SiteMode.cs
Scripts/World/MyriadCreation.World.Settlement/SettlementForm.cs
Scripts/World/MyriadCreation.World.Settlement/SettlementLevel.cs
```

**B) 23 个需补 `using MyriadCreation.Core.Enums;` 的消费方文件**（其余 14 个引用方已自带 Core using，无需增行；`Core.Contracts/MapSettlementView.cs` 仅注释、无代码改动）：

```
Scripts/Infrastructure/MyriadCreation.Infrastructure.Save/HabitationSiteSaveData.cs
Scripts/Infrastructure/MyriadCreation.Infrastructure.Save/SettlementPersistence.cs
Scripts/Simulation/MyriadCreation.Simulation.Actors/CampData.cs
Scripts/Simulation/MyriadCreation.Simulation.Actors/CampManager.cs
Scripts/Simulation/MyriadCreation.Simulation.Systems/SettlementDestructionSystem.cs
Scripts/Simulation/MyriadCreation.Simulation.Systems/SettlementEvolutionSystem.cs
Scripts/World/MyriadCreation.World.Anchor/AnchorGenerator.cs
Scripts/World/MyriadCreation.World.Settlement/CityDistrictSystem.cs
Scripts/World/MyriadCreation.World.Settlement/EconomicCompositionSystem.cs
Scripts/World/MyriadCreation.World.Settlement/HabitationSite.cs
Scripts/World/MyriadCreation.World.Settlement/HabitationSiteMigration.cs
Scripts/World/MyriadCreation.World.Settlement/HabitationSiteTransitionSystem.cs
Scripts/World/MyriadCreation.World.Settlement/SettlementData.cs
Scripts/World/MyriadCreation.World.Settlement/SettlementEvolutionTree.cs
Scripts/World/MyriadCreation.World.Settlement/SettlementTypologySystem.cs
Scripts/World/MyriadCreation.World.Settlement/SiteModeKeys.cs
Scripts/World/MyriadCreation.World.Settlement/WallDefense.cs
Tests/EditMode/HabitationSiteTests.cs
Tests/EditMode/SettlementPersistenceTests.cs
Tests/EditMode/SiteTypeLabelsTests.cs
Tests/EditMode/WallDefenseTests.cs
Tests/EditMode/WorldGenerationSmokeTests.cs
UI/MyriadCreation.UI.Presentation/SiteTypeLabels.cs
UI/Panels/SettlementDetailPanel.cs
```

---

## 6. 结论（给实现卡的取舍）

- **推荐走完整 3a→3d**：3a 是唯一“面广但机械”的步骤（26 行 using + 3 文件移动），3b/3c/3d 都是小步；做完后 Rendering 只依赖 Core+Content，B8-final 删 App.Contracts 无感。
- 若排期紧：可先做 **3b+3c+3d-lite**（用 A2 DTO 标志位替代 3a 枚举迁移），4-6 个文件即可让 Rendering 不再引用 World；但 `SiteMode/SettlementForm/SettlementLevel` 会继续留在 World，供 World/Simulation/Infra/UI/Tests 使用（不影响 Rendering 目标），后续再择机下沉。两条路线都不改存档字段与枚举值。
