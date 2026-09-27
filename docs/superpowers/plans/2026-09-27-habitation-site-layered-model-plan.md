# Implementation Plan: 栖居点分层模型迁移

**Date**: 2026-09-27 | **Spec**: [2026-09-27-habitation-site-layered-model-design.md](../specs/2026-09-27-habitation-site-layered-model-design.md)

## Summary

将现有 `SettlementData`、`CampData` 和存档中的混合术语迁移到以 `HabitationSite` 为业务入口的分层模型。Anchor 保留为空间定位/索引基础设施；栖居点以 `site_mode` 区分定居点、据点和营地；设施独立拥有施工状态、损毁状态、材料和形制，城墙形状放入 `facility_form.wall_shape`。迁移分阶段进行，先建立类型契约和校验，再接入转换、营地消散、设施损毁及存档兼容，避免一次性破坏当前未提交的其他改动。

## Technical Context

**Language/Version**: C# / Unity（版本由现有 Unity 工程配置决定）
**Primary Dependencies**: Unity serialization、现有 `GameWorld`、Anchor、Settlement、Camp、Save 系统
**Storage**: Unity 序列化运行时对象与 `SettlementSaveData`/`GameSaveData`/`MapSaveData` 存档 DTO
**Testing**: 现有 Unity 测试/编译验证；至少增加纯 C# 规则测试或可执行验证入口
**Target Platform**: Unity 游戏运行时与编辑器
**Performance Goals**: 保持现有按地块查询和世界 Tick 的复杂度；模式查询不引入全量扫描以外的新热点
**Constraints**: 不覆盖现有工作树改动；不再扩大 `Settlement`、`Camp`、`Anchor` 的旧语义；模式转换保留 `site_id`，营地消散才删除实体

## Constitution Check

*GATE: Must pass. Re-check after design review.*

- 单一业务入口：PASS。栖居点业务统一通过 `HabitationSite`，Anchor 仅负责定位/索引。
- 明确职责边界：PASS。位点模式、位点分类、定居发展、设施形制和损毁状态分层。
- 数据契约优先：PASS。先建立枚举、DTO、校验和迁移映射，再迁移调用方。
- 保持可验证性：PASS。每个转换规则、非法路径、营地消散和设施形制均安排针对性验证。
- 兼容性可控：PASS。旧存档通过显式映射读取，禁止用模糊字段自动猜测语义。

## Applied Guidelines

- **Guideline**: `guidelines`
  - **Rules Used**: 为新增/迁移术语同时定义中文正式名、英文正式名和键名；按领域边界拆分数据对象；对旧字段使用显式映射而非语义重载。
  - **Reference**: `skills/guidelines/SKILL.md`
- 未发现适用于 Unity C# 本地领域迁移的其他顶层技术迁移 guideline。

## Implementation Steps

### Step 1: [Cross-cutting] 建立栖居点与设施类型契约
- **Requirements**: REQ-001, REQ-002, REQ-003, REQ-004
- **Design inputs**: 设计规格第 2、3、4 节
- **Description**: 新增 `HabitationSite`、`SiteMode` 和分层数据对象；新增 `Facility` 及 `FacilityForm`，明确 `wall_shape` 只能位于设施形制；为旧 `SettlementData` 保留显式的过渡映射，不继续使用 `SettlementCategory` 作为新业务契约。

### Step 2: [Cross-cutting] 迁移世界状态与查询入口
- **Requirements**: REQ-001, REQ-005
- **Design inputs**: 设计规格第 3、4 节
- **Description**: 将 `GameWorld` 和相关系统的栖居点引用改为 `HabitationSite`/`site_id`；保留 Anchor 的空间属性和索引能力；建立按 ID、地块和模式查询的统一入口，避免业务代码直接从 Anchor 推导位点模式。

### Step 3: [Cross-cutting] 实现模式转换规则与营地消散
- **Requirements**: REQ-006, REQ-007
- **Design inputs**: 设计规格第 5 节
- **Description**: 实现允许/禁止的模式转换图、`prev_site_mode` 和 `pending_site_mode`；确保转换保留同一 `site_id`；将旧 CampManager 的废弃/摧毁路径迁移为 `temporary_encampment` 的实体消散，并同步清理地块索引。

### Step 4: [Cross-cutting] 拆分设施损毁与位点模式转化
- **Requirements**: REQ-008
- **Design inputs**: 设计规格第 3.4、6 节
- **Description**: 将设施施工、局部损毁、修复和移除从全局 Settlement 损毁字段中拆出；行动进度不直接等同于损毁结果；只有明确的转化规则可以修改 `site_mode`，设施 `facility_form.wall_shape` 在损毁时保持语义独立。

### Step 5: [Cross-cutting] 更新存档 DTO、读写和术语
- **Requirements**: REQ-009, REQ-010
- **Design inputs**: 设计规格第 2、3、7、8 节
- **Description**: 更新 `SettlementSaveData`、`GameSaveData`、`MapSaveData` 和读写系统，采用 `habitation_sites`/`site_mode`/`facilities` 的明确字段；提供旧存档字段到新字段的显式版本映射；清理新增代码和玩家可见文本中的 `burg`、核心 `holding`、底层 `camp` 及把栖居点简写为 `settlement` 的用法。

### Step 6: [Cross-cutting] 增加规则验证并完成迁移验收
- **Requirements**: REQ-001, REQ-006, REQ-007, REQ-008, REQ-009, REQ-010
- **Design inputs**: 设计规格第 5、6、7 节
- **Description**: 增加类型/转换/存档映射验证；覆盖合法转换、非法直达、ID 保留、营地删除、设施形制归属和旧字段映射；运行 Unity 编译与现有测试，检查未提交的无关改动不被覆盖。

## Project Structure

```text
MyriadCreation/Assets/Scripts/World/Settlement/
  HabitationSite.cs                 # 栖居点主数据与模式专属数据
  HabitationSiteEnums.cs            # SiteMode、分类和设施相关枚举
  FacilityData.cs                   # 设施及设施形制
  HabitationSiteTransitionSystem.cs # 模式转换和消散规则
  SettlementData.cs                 # 迁移期兼容映射，最终逐步收缩
MyriadCreation/Assets/Scripts/Simulation/WorldState/
  GameWorld*.cs                     # 栖居点索引、Anchor 关联和查询
MyriadCreation/Assets/Scripts/Simulation/Actors/
  CampSystem.cs                     # 营地业务迁移/兼容入口
MyriadCreation/Assets/Scripts/Infrastructure/Save/
  SettlementSaveData.cs             # 新旧存档显式映射
  GameSaveData.cs
  GameSaveSystem.cs
  MapSaveData.cs
  MapSaveSystem.cs
```

## Testing Strategy

- **appType**: Unity desktop game / simulation runtime
- **Critical user journeys**:
  1. 创建栖居点并按 `site_mode` 查询；
  2. 定居点、据点、营地之间执行允许的转换；
  3. 拒绝营地直达定居点和定居点直接删除；
  4. 营地消散后删除位点并清理地块关联；
  5. 城墙设施保存 `facility_form.wall_shape`，局部损毁不改变位点模式；
  6. 旧存档映射到新术语和分层字段。
- **primaryValidationStack**: Unity C# 编译 + 现有 Unity Test Runner；规则对象优先使用无 Unity 依赖的单元测试
- **fallbackMatrix**:
  - `runtime-tier`: Unity batchmode test/build → C# 静态编译和最小规则验证；前提是本机可用 Unity 编辑器
  - `save-tier`: 游戏存档回放测试 → DTO 序列化/反序列化测试；前提是可生成固定测试存档
- **Environment requirements**: Unity 工程、目标 Unity 编辑器版本、现有依赖包；不新增外部服务
- **knownGaps**: 若 Unity Test Runner 不可用，只能验证纯 C# 规则和编译，无法证明完整场景索引与序列化运行时行为
- **Test data strategy**: 使用固定 `site_id` 和 tile/anchor ID；每个测试独立创建世界状态，结束后清除实体和索引
- **Acceptance criteria**: 所有合法/非法转换结果符合规格；营地消散无残留地块引用；设施形制不出现在位点模式字段；旧存档可显式转换且无 `burg` 键；Unity 编译通过
- **Validation review expectations**: 审核者确认业务代码不以 Anchor 推导模式、不以全局损毁字段替代设施状态、不将 `Settlement` 作为 HabitationSite 的同义词

## Tasks

### Phase 1: Foundational

- [ ] T001 [P] [Plan:1.1] 新增 `MyriadCreation/Assets/Scripts/World/Settlement/HabitationSiteEnums.cs`，定义 `SiteMode` 的 `sedentary_settlement`、`fortified_outpost`、`temporary_encampment` 对应枚举及设施形制枚举。
- [ ] T002 [P] [Plan:1.1] 新增 `MyriadCreation/Assets/Scripts/World/Settlement/FacilityData.cs`，实现设施身份、类型、施工状态、损毁状态、材料和 `FacilityForm.wall_shape`。
- [ ] T003 [Plan:1.1] 新增 `MyriadCreation/Assets/Scripts/World/Settlement/HabitationSite.cs`，承载 `site_id`、`site_mode`、位置引用、模式专属数据和 `facilities`，并提供模式数据有效性校验。
- [ ] T004 [Plan:1.1] 为 `SettlementData` 增加到 `HabitationSite` 的显式迁移映射，禁止以 `SettlementCategory` 的名称直接作为新存档键或新模式契约。

### Phase 2: Core migration

- [ ] T005 [P] [Plan:2.1] 更新 `GameWorld` partial 文件中的栖居点集合和索引，使用 `site_id` 关联 Anchor，并保留 Anchor 的空间查询职责。
- [ ] T006 [P] [Plan:2.1] 更新 `SettlementControlSystem.cs`、`SettlementAISystem.cs`、`SettlementCommands.cs` 和生成流程，改用 `HabitationSite.site_mode` 与统一查询入口。
- [ ] T007 [Plan:3.1] 新增 `HabitationSiteTransitionSystem.cs`，实现允许转换、禁止转换、历史模式和延迟模式字段。
- [ ] T008 [Plan:3.1] 将 `CampManager` 的建立、废弃和摧毁路径接入 `temporary_encampment`，在实体消散时同步移除位点集合和 tile camp 引用。
- [ ] T009 [Plan:4.1] 重构 `SettlementDestructionSystem.cs`，按设施记录局部损毁和修复，禁止用全局 `ruinLevel` 单独决定位点模式。

### Phase 3: Persistence and cleanup

- [ ] T010 [P] [Plan:5.1] 更新 `SettlementSaveData.cs` 及相关 DTO，增加 `habitation_sites`、`site_mode`、模式专属数据和 `facilities` 的显式结构。
- [ ] T011 [Plan:5.1] 更新 `GameSaveSystem.cs`、`MapSaveSystem.cs` 和对应数据装配逻辑，实现旧存档字段的显式版本映射与新存档写出。
- [ ] T012 [P] [Plan:5.1] 清理 `README.md`、玩家可见文本和新增代码中的旧术语，确保正式中文为“栖居点、定居点、据点、营地”，英文和键名与规格一致。

### Phase 4: Validation

- [ ] T013 [P] [Plan:6.1] 增加模式转换规则测试，覆盖 ID 保留、合法转换、非法直达和定居点不可删除。
- [ ] T014 [P] [Plan:6.1] 增加设施形制与损毁测试，验证 `wall_shape` 只存在于设施且局部损毁不自动改变 `site_mode`。
- [ ] T015 [P] [Plan:6.1] 增加存档映射测试，验证旧数据显式转换、新键写出、营地消散无残留引用。
- [ ] T016 [Plan:6.1] 运行 Unity 编译/测试和工作树检查，记录与本次迁移无关的既有改动，不回滚或覆盖它们。

## Requirement Mapping

| REQ ID | Description | Plan Items | Implementation Evidence |
|--------|-------------|------------|--------------------------|
| REQ-001 | 栖居点使用 HabitationSite 作为唯一业务父概念 | 1.1, 2.1 | `HabitationSite.cs`, `GameWorld*.cs` |
| REQ-002 | 三种模式使用严格英文和枚举键 | 1.1, 5.1 | `HabitationSiteEnums.cs`, save DTO |
| REQ-003 | 位点分类、定居发展和设施形制分层 | 1.1 | `HabitationSite.cs`, `FacilityData.cs` |
| REQ-004 | 城墙形制位于设施 `facility_form.wall_shape` | 1.1, 4.1 | `FacilityData.cs`, destruction rules |
| REQ-005 | Anchor 仅保留定位/索引职责 | 2.1 | `GameWorld*.cs`, Anchor references |
| REQ-006 | 合法模式转换保留 `site_id` | 3.1 | `HabitationSiteTransitionSystem.cs` |
| REQ-007 | 营地可消散，非法路径被拒绝 | 3.1 | `HabitationSiteTransitionSystem.cs`, `CampSystem.cs` |
| REQ-008 | 设施损毁与位点模式转换分离 | 4.1 | `SettlementDestructionSystem.cs`, `FacilityData.cs` |
| REQ-009 | 新旧存档字段显式映射 | 5.1 | `SettlementSaveData.cs`, save systems |
| REQ-010 | 清理 burg/holding/底层 camp 等旧语义 | 5.1 | `README.md`, migrated source files |

## Complexity Tracking

无需要豁免的宪法违规。
