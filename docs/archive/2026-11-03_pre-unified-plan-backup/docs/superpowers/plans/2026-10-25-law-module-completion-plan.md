# Implementation Plan: 法律模块收尾与接线（阶段一收尾）

**Date**: 2026-10-25 | **Spec**: [2026-10-25-law-module-completion-design.md](../specs/2026-10-25-law-module-completion-design.md) | **Design source**: [ARCHITECTURE_2_0_LAW_OFFICE_NAME_REGION_MODEL.md](../../architecture/ARCHITECTURE_2_0_LAW_OFFICE_NAME_REGION_MODEL.md)

**Baseline**: 分支 `architecture-2-0-refactor`，HEAD `f1ca764`（政体系统重命名为法律系统，加领域划分）+ 未提交工作树（三层枚举、罪行管理器、头衔/文化骨架、设计稿本身）。行号以 2026-10-25 工作树为准，执行时以符号为准。

## Summary

设计稿的三层法律结构与罪行管理器已经写进工作树，但改名只做了一半：多点引用了已删除的 `Government*` 类型与 `realm.composition` 字段，四个程序集静态不可编译；条文仍挂域不挂组；组级查询、组级效果、缓存指纹、审判管线全部零调用；存档字段 `RealmDTO.laws` 没有旧名映射。本计划先把工作树收敛回可编译、可验证的基线，再把"枚举"变成"可运行契约"（条文挂组、组→族→域单一真源、效果与指纹覆盖全部组、法律变更唯一入口），最后补上罪名引用与审判消费、可见名与存档兼容，并把阶段二的接口与依赖登记成闸门。

## Technical Context

**Language/Version**: C# 9 / Unity 6000.6.3f1（工程 `ProjectSettings/ProjectVersion.txt`），`netstandard2.1`
**Primary Dependencies**: `MyriadCreation.Core` / `.Simulation` / `.World` / `.UI` / `.Editor` / `.Tests` 程序集；JsonUtility 序列化；Localization（`StreamingAssets/Localization/*.json`）
**Storage**: `SaveSystem` 主档（`SaveData` v4，`RealmDTO` 内嵌 `LawComposition`）；`GameSaveSystem` 为未接线死代码，本轮决定去向
**Testing**: Unity EditMode Test Runner（`Assets/Tests/EditMode`）；批处理 Unity 作为可选门禁（此前因 headless license 返回 198，不可依赖）
**Target Platform**: Unity 桌面运行时与编辑器
**Performance Goals**: 法律效果按政权缓存、指纹失效（不做每帧重算）；罪名引用在条文变更时重算一次并缓存；司法审判只读缓存
**Constraints**: 枚举按 `int` 入档，成员只追加不重排；`Definitions` 不反向依赖 `Systems`；内部域/族/组名不进玩家可见文本；不顺手实现阶段二功能

## Constitution Check

*GATE: Must pass. Re-check after Phase 0 and Phase 2.*

- 单一入口：PASS。条文入口 `Law`，法律变更入口统一为 `LawSystem.ChangeLaws`（经 `LawReform`），效果出口统一为 `GetRealmEffects`。
- 职责边界：PASS。域=对外引用接口，组=条文挂载点，族=内部归集；罪名归 `CrimeRegistry`，审判执行归 `JudicialSystem`。
- 数据契约优先：PASS。先定枚举真源、映射断言、条文挂组，再接效果与审判。
- 保持可验证性：PASS。映射完整性、指纹失效、罪名受理、旧档映射各有 EditMode 断言。
- 兼容性可控：PASS。旧档走显式字段映射与版本递增，不用"猜字段"兜底。
- 不扩大范围：PASS。13 个预留组只保留枚举位与中性效果；头衔/文化/姓名只登记接口。

## Applied Guidelines

- **Guideline**: 项目架构不变量（`docs/../MyriadCreation/Assets/Scripts/Simulation/Architecture/ARCHITECTURE.md` 第 1–9 条）：跨域只走 Event / Query / Command；定义与运行态分离；昂贵计算走"快照→计算→提交→事件"。
- **Guideline**: 拆分尺度（`docs/ARCHITECTURE_2_0_SPLIT_PRINCIPLES.md`）：新增抽象必须解决状态所有权、依赖方向、性能边界或可测试性之一；不做纯转发层。
- **Guideline**: 设计稿 §4.0：内部命名与可见名分离；硬约束与权重分离；归集顺序从组到族到域。
- **Reference**: 已有的栖居点迁移是本仓库的格式先例（`docs/superpowers/plans/2026-09-27-habitation-site-layered-model-plan.md` 与同目录 `checkpoints/`）。

## Implementation Steps

### Step 0.1: [Cross-cutting] 收敛半迁移的改名（P0，阻塞全部后续）
- **Requirements**: REQ-001
- **Design inputs**: 设计稿 §3 现状、本轮审计结果
- **Description**: 把 `Government*`→`Law*` 改名收尾到四个程序集都可编译：修正指向已删字段/类型的引用，统一命名空间引用，测试与编辑器工具同步改名。此步不改变任何运行行为，只恢复基线。

### Step 0.2: [Cross-cutting] 基线验证与登记
- **Requirements**: REQ-001
- **Description**: 用 Editor 确认四程序集编译通过，记录剩余警告；以 grep 断言 `Government` 与 `composition.` 旧引用归零；把当前未提交改动按主题登记（法律/罪行/头衔骨架/文化骨架/文档），避免后续提交混在一起。

### Step 1.1: [Cross-cutting] 条文挂组与三层单一真源
- **Requirements**: REQ-002, REQ-003
- **Design inputs**: 设计稿 4.0（归集方向）、4.2（枚举）、4.5（建制域明细）
- **Description**: `Law` 条目改挂 `LawGroup`，域由组派生（`LawConstraints.GetDomain(group)`）而不是另存字段；把散在 `LawConstraints` 各处的"维度↔组""组↔族""族↔域"映射合并为一张唯一真源表，`GetDomain` 的 default 分支不再兜底吞错，改由完整性测试保证 29 组全覆盖。

### Step 1.2: [Cross-cutting] 组合校验与映射测试
- **Requirements**: REQ-003
- **Design inputs**: 设计稿 4.2–4.5、4.6–4.9 预留组
- **Description**: 给 29 组建立"成员数、所属族、所属域、是否预留"的断言表；`LawComposition` 的组字段与题内子选项一致性校验集中一处；13 个预留组按 D4 决定是否留在组合里，其默认值与中性语义写明。

### Step 1.3: [Cross-cutting] 组级效果
- **Requirements**: REQ-004
- **Design inputs**: 设计稿 §4、§9 任务 #5
- **Description**: `LawEffects` 从"九维 + 法源"扩到"按组取值"：九维与法源沿用现有计算，信仰域四组按组语义计算，其余预留组返回中性值且不叠加；模板的 `recommended*` 字段与组 id 对齐，消除"字段存在但无人消费"的状态。

### Step 2.1: [Cross-cutting] 缓存指纹与唯一变更入口
- **Requirements**: REQ-005
- **Design inputs**: 设计稿 §9 任务 #5、拆分原则的性能条
- **Description**: `LawSystem` 指纹覆盖全部组字段（含 `ComponentChoice.secondary`），按 `realmId` 失效；`ChangeLaws` 成为唯一变更入口，`LawReform` 与其他调用方统一走它；`LawSystem` 在 `GameWorld` 初始化时真正被实例化并进入 Tick。

### Step 2.2: [Cross-cutting] 效果消费收敛
- **Requirements**: REQ-005
- **Description**: 把散落的 `CalculateEffects` 直接调用（AI 决策、事件、UI）收敛到 `GetRealmEffects`；确保一次变更只触发一次重算，并在重算后发事件（沿用 `SimulationEventBus` 既有模式）。

### Step 3.1: [Cross-cutting] 条文实例化与罪名引用
- **Requirements**: REQ-006
- **Design inputs**: 设计稿 4.9、4.10
- **Description**: 按 D3 决定条文来源（模板实例化或内容包 JSON），首次初始化生成条文列表；按 `CrimeRegistry` 的域/族/组归属反查填充 `Law.citedCrimes`；条文为空时"默认受理/默认不受理"的口径写死并测试。罪名匹配入口接 `CrimeRegistry.MatchByBehavior`。

### Step 3.2: [Cross-cutting] 审判管线与司法归属
- **Requirements**: REQ-007
- **Design inputs**: 设计稿 4.10、审计 §2
- **Description**: 按 D2 决定 `JudicialSystem` 归属（`ThoughtManager` 或政权）与是否入档；`TrialCrime` 接上命令/事件入口；`JudicialSystem.source` 与 `LawComposition.legalSource` 合一，消除双来源。

### Step 3.3: [Cross-cutting] 罪名本地化
- **Requirements**: REQ-006, REQ-008
- **Description**: 15 条罪名的中文字面量改为 locale 键，补 en / zh-Hans / zh-Hant 词条；刑罚与判决类型同样处理；测试断言不出现硬编码中文键。

### Step 4.1: [Cross-cutting] 法律界面与可见名
- **Requirements**: REQ-008
- **Design inputs**: 设计稿 4.0（内部/可见分离）、D1、D6
- **Description**: 按 D1 处理 `GovernmentPanel`（改写为组视图或删除）；界面只显示可见名，不出现"域/族/组"与内部枚举名；`LawComponentNames`、`RealmOverviewText`、改革编年史文本改走可见名与 locale；资产目录若随之改名，需与 `Assets/UI` 现有组织保持一致。

### Step 5.1: [Cross-cutting] 存档兼容
- **Requirements**: REQ-009
- **Design inputs**: D4、D5
- **Description**: `RealmDTO.laws` 增加旧名映射（`[FormerlySerializedAs("composition")]` 或显式迁移），`SaveVersion` 按约定递增；13 个预留组字段按 D4 决定保留或迁移；加旧档样本回归，验证枚举未错位、法律组可用。

### Step 5.2: [Cross-cutting] 清理与技术债
- **Requirements**: REQ-010
- **Description**: 删 `LawCategory`（已 `[Obsolete]` 零引用）与其 meta；处理未接线死代码（`GameSaveSystem`、`ThoughtManager` 司法路径、`GetDomainName` 等无调用 API）并给出保留/删除理由；清理"七维/旧五域/8 罪名"等漂移注释。

### Step 6.1: [Cross-cutting] 文档校正与阶段二闸门
- **Requirements**: REQ-011, REQ-012
- **Description**: 按本轮结果校正设计稿 §3 现状表、§9 阶段表、附录源码对照；把阶段二（头衔/资格/继承/文化认同/姓名/键名/法理）的最小契约、依赖顺序、阻塞决策写成一页接口清单，作为下一份规格的输入。

## Project Structure

```text
MyriadCreation/Assets/Scripts/Simulation/
  Systems/LawConstraints.cs              # 三层枚举、唯一真源映射、完整性断言
  Systems/LawConstraints.SubOptions.cs   # 题内子选项
  Systems/LawConstraints.Templates.cs    # 模板与 recommended* 对齐
  Systems/LawSystem.cs                   # 效果缓存、指纹、唯一变更入口、JudicialSystem
  Systems/LawReform.cs                   # 改革 → ChangeLaws
  Systems/CrimeRegistry.cs               # 15 条罪名与引用（消费方）
  State/Law.cs                           # 条文条目：改挂 LawGroup
  State/LawComposition.cs                # 组字段、组合校验、预留组默认值
  State/LawEffects.cs                    # 组级效果
  State/RealmData.cs                     # laws 字段与继承法入口（旧名残留）
  Definitions/CrimeDefinition.cs         # 罪名定义（已是终态，少量消费）
  Definitions/LawCategory.cs             # 待删
  State/RealmDTO.cs                      # laws 旧名映射（存档 DTO）
  Core/Constants/GameConstants.cs        # SaveVersion
MyriadCreation/Assets/UI/
  Panels/GovernmentPanel.cs              # 按 D1 改写或删除
  Common/UIManager.cs                    # 官职/称号面板引用
MyriadCreation/Assets/Editor/ComponentChoiceDrawer.cs
MyriadCreation/Assets/Tests/EditMode/    # 映射/指纹/罪名/存档测试
docs/superpowers/{specs,plans}/2026-10-25-law-module-completion-*.md
docs/architecture/ARCHITECTURE_2_0_LAW_OFFICE_NAME_REGION_MODEL.md  # §3/§9 校正
```

## Testing Strategy

- **appType**: Unity 桌面游戏 / 模拟运行时
- **Critical user journeys**:
  1. 新开局：法律组初始化完整，29 组都有默认值，域/族查询正确；
  2. 改革：改一个组 → 指纹变化 → 效果在下一个 Tick 重算一次；
  3. 罪名：行为事件命中罪名定义 → 依据条文引用决定受理 → 审判产出判决；
  4. 存档：旧档（`composition`）载入 → 法律组可用、枚举不错位；
  5. 界面：打开法律面板只看到可见名，无内部系统词。
- **primaryValidationStack**: Unity Editor 编译 + EditMode Test Runner（映射完整性、指纹、罪名受理、旧档映射、界面词断言）
- **fallbackMatrix**:
  - `runtime-tier`: `Unity.exe -batchmode -quit` → 若 license 返回 198（历史记录），改为 Editor 内编译 + 人工确认；
  - `static-tier`: 本机 `dotnet` SDK 缺 `Microsoft.NET.SDK.WorkloadAutoImportPropsLocator` 等 Sdk 目录（MSB4276），**不能**用 `dotnet build` 替代 Unity 编译；只允许用 grep 做旧名归零与符号存在性断言；
  - `save-tier`: 无法自动生成旧档时，用固定 JSON 样本（参照 `HabitationSiteTests` 的旧档样本写法）跑 DTO 反序列化断言。
- **Environment requirements**: Unity 6000.6.3f1（`D:\Unity Hub\6000.6.3f1`）、工程可打开、EditMode 测试可运行；无新增外部服务。
- **knownGaps**: 批处理 license 不可用时不具备自动化编译门禁；JsonUtility 的枚举 int 语义只能靠"只追加"约定与测试样本保证，没有编译期保护。
- **Test data strategy**: 固定 `realmId` 与固定的九维/组取值；每个测试独立构造 `LawComposition`，不共享静态状态；旧档样本放 `Assets/Tests/EditMode/TestData/`。
- **Acceptance criteria**: 规格第 7 节 7 条全部满足；映射测试覆盖 29 组；指纹测试覆盖"改一组刷新一次"与"改无关字段不刷新"；罪名测试覆盖"有引用受理/无引用默认"；旧档测试覆盖 `composition`→`laws`。
- **Validation review expectations**: 审核者确认没有把"域"塞进条文、没有把域/族/组名写进界面、没有在 `Definitions` 反向依赖 `Systems`、没有顺手实现阶段二功能。

## Tasks

### Phase 0: Baseline restore

- [ ] T001 [P] [Plan:0.1] 修正 Simulation 程序集旧名残留：`State/RealmData.cs:86-108`（`composition.successionLaw`→`laws.successionLaw`，同步注释）、`WorldState/GameWorld.Simulation.cs:164`（`Politics.LawEffects`→`State.LawEffects`）、`WorldState/GameWorld.Succession.cs:225`（`Politics.InheritanceLaw`→`Definitions.InheritanceLaw`）。补齐对应 `using`，不新增别名。
- [ ] T002 [P] [Plan:0.1] 修正 UI 程序集：`Assets/UI/Panels/GovernmentPanel.cs` 全篇按 D1 处理为可编译版本（`GovernmentComposition`→`LawComposition`、`GovernmentConstraints.GovernmentDimension`→`LawConstraints.LawDimension`、`GovernmentConstraints.Xxx`→`LawConstraints.Xxx`，或按 D1 删除）；`Assets/UI/Common/UIManager.cs:417` 的 `realm.composition` / `GovernmentConstraints.SupremeSovereignty` 改用 `realm.laws` / `LawConstraints.SupremeSovereignty`。
- [ ] T003 [P] [Plan:0.1] 修正 Editor 程序集：`Assets/Editor/ComponentChoiceDrawer.cs:39-45` 的 `GovernmentComponentNames` 改用 `LawComponentNames`。
- [ ] T004 [P] [Plan:0.1] 同步测试旧名：`GovernmentCompositionTests.cs`、`AdminDivisionTests.cs`、`RealmOverviewTextTests.cs`、`ReformEmergenceTests.cs`、`SuccessionSystemTests.cs`。
- [ ] T005 [Plan:0.2] 基线验证与登记：Editor 编译四程序集、记录警告、grep 断言 `Government` 与 `composition.` 归零；按主题整理未提交改动清单。

### Phase 1: Law three-layer contract

- [ ] T006 [Plan:1.1] `State/Law.cs`：`domain` 改为 `group`（`LawGroup`），提供 `Family`/`Domain` 只读派生属性；保留 `citedCrimes`，写明"条文只挂组、跨系统只引用域"。
- [ ] T007 [Plan:1.1] `Systems/LawConstraints.cs`：合并 `MapDimensionToGroup`、`GetGroup`、`GetFamily`、`GetDomain` 的映射为唯一真源表；移除重复映射与包裹式 default 兜底；`GetGroupName/GetFamilyName/GetDomainName` 保留但标注只供开发日志。
- [ ] T008 [P] [Plan:1.2] EditMode 新增 `LawGroupMappingTests`：29 组全部有族与域、每族至少一组、域族组数量与设计稿一致、非法组值拒绝。
- [ ] T009 [Plan:1.2] `State/LawComposition.cs`：组字段与题内子选项一致性校验集中；13 个预留组默认值集中声明（按 D4 决定是否保留在组合中）。
- [ ] T010 [P] [Plan:1.3] `State/LawEffects.cs`：把效果计算从"直接读九维字段"改为"按组取值"（九维映射到 11 个建制域组，信仰族四组沿用现有计算，法源组沿用 `legalSource`）；13 个预留组返回中性值且不参与叠加；`LawConstraints.Templates.cs` 的 `recommended*` 与组 id 对齐并补消费点。

### Phase 2: Effect pipeline

- [ ] T011 [Plan:2.1] `Systems/LawSystem.cs`：指纹覆盖全部组字段（含 `ComponentChoice.secondary`），`ChangeLaws` 成为唯一变更入口；`Systems/LawReform.cs` 改走该入口。
- [ ] T012 [Plan:2.1] 接线：`WorldState/GameWorld.*` 初始化与月度 Tick 中实例化 `LawSystem`；`AI/AIDecisionSystem.cs:48-56` 等直接 `CalculateEffects` 调用改读 `GetRealmEffects`；变更后发事件。
- [ ] T013 [P] [Plan:2.2] EditMode 新增 `LawEffectFingerprintTests`：改单组→指纹变→重算一次；改无关字段→不重算；缓存按 `realmId` 隔离。

### Phase 3: Crime and judicature

- [ ] T014 [Plan:3.1] 条文实例化（按 D3）：初始化生成条文列表并按 `CrimeRegistry` 归属填充 `citedCrimes`；无条文时的受理默认口径写死并注释。
- [ ] T015 [Plan:3.1] 受理改读引用：`JudicialSystem.IsCrimeCited` 走条文引用关系，`CrimeRegistry.MatchByBehavior` 接入行为匹配入口；删除"全部登记罪名一律受理"的兜底。
- [ ] T016 [Plan:3.2] `JudicialSystem` 归属与实例化（按 D2），`TrialCrime` 接命令/事件；`source` 与 `LawComposition.legalSource` 合一。
- [ ] T017 [P] [Plan:3.3] 罪名/刑罚/判决类型名称改 locale 键，补三种语言词条；EditMode 新增 `CrimeCitationTests`（有引用受理、无引用默认、行为匹配）。

### Phase 4: UI and visible names

- [ ] T018 [Plan:4.1] 法律界面（按 D1）：新 `LawPanel` 或改写后的组视图，只展示可见名；`UIManager` 接线；内部词不进界面。
- [ ] T019 [Plan:4.1] `Presentation/LawComponentNames.cs`、`RealmOverviewText.cs`、`LawReform` 编年史文本改可见名与 locale（按 D6 的键命名规范）；补 `StreamingAssets/Localization/*.json`。
- [ ] T020 [P] [Plan:4.1] EditMode 新增 `LawVisibleTextTests`：界面/日志文本不含"域/族/组/Institutional/Internal/Governance"等内部词。

### Phase 5: Persistence and cleanup

- [ ] T021 [Plan:5.1] `Simulation/State/RealmDTO.cs`：`laws` 旧名映射（按 D5）；`Core/Constants/GameConstants.cs:19` 递增 `SaveVersion` 并补迁移分支（`SaveSystem` 走 `SaveData.v4`，`GameSaveSystem` 走自己的 version，两者需分别确认）。
- [ ] T022 [P] [Plan:5.1] EditMode 新增 `LawSaveCompatTests`：旧档 `composition` 载入映射、枚举 int 稳定性、预留组字段保留/迁移（按 D4）。
- [ ] T023 [Plan:5.2] 删除 `Definitions/LawCategory.cs` 与 `.meta`；清理未接线死代码与漂移注释（`GameSaveSystem`、`ThoughtManager` 司法路径、`GetDomainName` 等），逐项记录保留理由。
- [ ] T024 [Plan:6.1] 文档校正：更新设计稿 §3 现状表、§9 阶段表、附录源码对照；同步 `ARCHITECTURE.md` 的 Current migration slice。

### Phase 6: Stage-two gate

- [ ] T025 [P] [Plan:6.1] 输出阶段二接口清单（头衔注册与称谓链、资格填充器、继承三轨落地、传统数据迁移、认同层、姓名双轨、键名、法理区域），标依赖顺序与阻塞决策，作为下一份规格输入。

## Requirement Mapping

| REQ ID | Description | Plan Items | Implementation Evidence |
|---|---|---|---|
| REQ-001 | 四程序集恢复可编译 | 0.1, 0.2 | T001–T005，grep 归零 |
| REQ-002 | 条文挂组、域派生 | 1.1 | T006 |
| REQ-003 | 组→族→域唯一真源与完整性 | 1.1, 1.2 | T007–T009，`LawGroupMappingTests` |
| REQ-004 | 组级效果覆盖全部组 | 1.3 | T010 |
| REQ-005 | 指纹全字段、唯一变更入口、实例化 | 2.1, 2.2 | T011–T013 |
| REQ-006 | 罪名被条文引用并被审判消费 | 3.1, 3.3 | T014, T015, T017 |
| REQ-007 | 司法执行层归属、实例化、入档决策 | 3.2 | T016 |
| REQ-008 | UI 只显示可见名 | 3.3, 4.1 | T017–T020 |
| REQ-009 | 存档兼容与版本递增 | 5.1 | T021, T022 |
| REQ-010 | 删 LawCategory、清死代码与漂移注释 | 5.2 | T023 |
| REQ-011 | 设计稿与代码对齐 | 6.1 | T024 |
| REQ-012 | 阶段二接口闸门 | 6.1 | T025 |

## Complexity Tracking

无需要豁免的架构违规。唯一需要记录的取舍：`Definitions` 层引用法律域时继续用 `int` 存值 + `Systems` 侧访问器（现有 `CrimeDefinition.citedDomainIds` 先例），不为了强类型在 `Definitions` 反向依赖 `Systems`。

## 风险与依赖

| 风险 | 影响 | 处置 |
|---|---|---|
| 枚举按 int 入档 | 重排/复用值会静默破坏旧档 | 只追加成员；T022 断言顺序 |
| 13 个预留组字段已进档 | 删字段或重排静默丢数据 | D4 先决策；T009/T021 统一处理 |
| 批处理 license 不可用 | 无自动编译门禁 | 以 Editor 编译为主门禁；T005/T016 记入验证记录 |
| 阶段二骨架已在工作树 | 改动交叉导致冲突 | 本轮只动法律链路；T025 登记骨架状态，不接阶段二 |
| 改名收尾涉及 UI/Editor/Tests | 编译基线恢复可能牵连面大 | Phase 0 单独成批、单独验证，不与契约改混提交 |

## 执行建议

1. 先只做 Phase 0 并单独提交，让工作树回到可编译、可回滚的状态；这是后续所有验证的前提。
2. Phase 1–2 作为一批提交（契约 + 效果），Phase 3 单独提交（审判是行为变化），Phase 5 单独提交（存档有兼容风险）。
3. T018 与 D6 需要先定界面可见名规范；若一时定不下来，可先删旧面板（D1 的删除分支）并留占位，不让它阻塞 Phase 0 编译。
