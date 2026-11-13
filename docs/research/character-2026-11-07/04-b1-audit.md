# B1 批次只读审计（头衔契约 + ActionGate 契约 + 纯数据测试）

> 日期：2026-11-07。性质：**只读审计**（唯一写入 = 本文件，未改任何代码/资产/文档）。
> 口径：`ARCHITECTURE_RULES.md`（2026-11-03 生效）+ `UNIFIED_MASTER_PLAN.md` + `SYSTEM_OWNERSHIP.md` + `DECISIONS_M1_M2_M3.md`。
> 说明：本审计做静态核对（文件/asmdef/引用/时间戳/git），**未运行** Unity 编译与 EditMode（`E:\TEMP\mc-*.ps1` 属行为验证，不在只读范围）；B1 记录里的 `COMPILE_OK` / 669 条全绿无法由本次审计独立复验。

## 0. 结论

| 维度 | 判定 | 一句话 |
|---|---|---|
| 程序集方向（Core 零引 / Content 仅引 Core / Tests 可引全量） | ✅ 通过 | 三个声明文件均无越层引用 |
| 纯数据层引入 Unity 类型 | ✅ 通过 | 两文件 0 个 `using`，无 UnityEngine |
| 命名空间 = 目录 | ✅ 通过 | Core.Contracts / Content.Definitions / Tests.EditMode 全部对齐 |
| 新增 asmdef / 移动既有文件 | ✅ 通过 | B1 窗口内无 asmdef 变更，无既有 title 文件被改写 |
| 与既有头衔模型重复/冲突 | ⚠️ 需修 | 分类现成 4 个来源；派生枚举与 `TitleKind.cs` 同目录仅差前缀 |
| 契约设计完整性 | ❌ 不通过 | 缺 `Regent`、`AuthoritySource` 无载体、`Allow()` 填假失败层、无 reasonCode |
| 测试质量 | ⚠️ 需修 | 6 条中 3 条纯反射计数；无拒绝分支；1 条仅走 Allow |
| 残留（孤儿 `.meta` / 悬空引用） | ✅ 通过 | 全 Assets 0 个孤儿 `.meta`；删除的 4 文件无任何代码残留 |

**总判：不通过（差距小、约 30 分钟可修完，不需重做；修正后可进 B2/B3）。** 三个声明文件本身"能编译、不违规"，但契约与 M2 验收标准不一致，且批次清单漏报第 4 个文件。

## 1. B1 实际落盘清单（按 git + mtime 重建）

| 文件 | git 状态 | mtime | 与记录（`DECISIONS_M1_M2_M3.md:295-303`）一致？ |
|---|---|---|---|
| `MyriadCreation/Assets/Scripts/Core/MyriadCreation.Core.Contracts/ActionGateContracts.cs` + `.meta` | `??` 未跟踪 | 01:58:01 | ✅ 记录为新增 |
| `MyriadCreation/Assets/Scripts/Content/MyriadCreation.Content.Definitions/DerivedTitleKind.cs` + `.meta` | `??` 未跟踪 | 01:55:39 | ✅ 记录为新增 |
| `MyriadCreation/Assets/Tests/EditMode/TitleDomainContractTests.cs` + `.meta` | `??` 未跟踪 | 01:58:01 | ✅ 记录为新增 |
| `MyriadCreation/Assets/Scripts/Content/MyriadCreation.Content.Definitions/TitleKind.cs` + `.meta` | `??` 未跟踪 | **01:55:39** | ❌ 记录称"既有未跟踪文件、保持原样"（`:331` §8.4），但 mtime 落在 B1 窗口内 |
| 记录中"曾建后删"的 `TitleDef.cs`/`TitleEligibility.cs`/`TitleModule.cs`/`TitleRight.cs` | 无任何痕迹 | — | ✅ 删除干净（见 §6） |

- 判定依据：仓库 `Content/Core/Tests` 整树未跟踪（`UNIFIED_MASTER_PLAN.md:20` 已注明"大量未提交改动"），**"未跟踪"不能证明新增**；只能用 mtime。B1 窗口（01:50–02:20）内 `Assets/**` 仅有上述 8 个文件被写，`TitleKind.cs` 是其中之一。
- 结论：批次实际新增 **4 个** `.cs`（非 3 个）；`TitleKind.cs:3-11` 的 4 个常量与 `Content.Modding/TitleKinds.cs:5-11` **逐字重复**，且全仓 0 引用（`grep "TitleKind\."` 无命中）。违反 `ARCHITECTURE_RULES.md:90`「同一概念只允许一个定义源」。

## 2. 架构合规核对

| # | 检查项 | 规则出处 | 证据 | 判定 |
|---|---|---|---|---|
| 2.1 | Core 不引上层（L0） | `ARCHITECTURE_RULES.md:24,26`；`core.asmdef` `references: []` | `ActionGateContracts.cs` 0 个 `using`，只引用 `System` 概念外的自建类型 | ✅ |
| 2.2 | Content 仅引 Core（L1） | `ARCHITECTURE_RULES.md:27`；`MyriadCreation.Content.asmdef:5-7` refs=`[Core]` | `DerivedTitleKind.cs` 0 个 `using`，未触 Simulation/World | ✅ |
| 2.3 | Tests 可引全量 | `ARCHITECTURE_RULES.md:63-64`；`MyriadCreation.Tests.asmdef:4-21` 含 Core/Content | 测试 `:2-3` 同时 `using Content.Definitions` + `Core.Contracts` | ✅ |
| 2.4 | 纯数据层无 Unity 类型 | `ARCHITECTURE_RULES.md:74`；`SYSTEM_OWNERSHIP` R4 | 两文件无 `using UnityEngine` | ✅ |
| 2.5 | namespace = 目录 | `DECISIONS_M1_M2_M3.md:291`（§7.4） | `ActionGateContracts.cs:1` ≡ `Core/…Core.Contracts/`；`DerivedTitleKind.cs:1` ≡ `Content/…Content.Definitions/`；测试 `:5` ≡ `Tests/EditMode` | ✅ |
| 2.6 | 一文件一主类型、文件名=类型名 | `DECISIONS_M1_M2_M3.md:292` | `ActionGateContracts.cs` 6 个类型（契约聚合文件，与 `CommandResult.cs`/`LawContracts.cs` 同惯例） | ✅（可接受） |
| 2.7 | 纯枚举落位 | `ARCHITECTURE_RULES.md:44-45`（纯契约/DTO/枚举 → **Core**） | `DerivedTitleKind` 是"运行时绑定/权利推导出的视图"，**不是** Content 的 JSON 内容定义 → 按决策树应在 `Core/MyriadCreation.Core.Enums/` | ⚠️ 定位偏 Content（可运行、不影响程序集方向，但规则上应在 Core） |
| 2.8 | 未新增 asmdef / 未移文件 | `DECISIONS_M1_M2_M3.md:289`；`ARCHITECTURE_RULES.md:66` | B1 窗口无 asmdef 变化；无 title 文件被 M | ✅ |
| 2.9 | 门禁不会被新文件打破 | `ARCHITECTURE_RULES.md:117` | `CompletionGateTests.cs:23-25` 只扫 Content 的 Simulation/World using；`AsmdefArchitectureGateTests.cs:410` 只扫 asmdef | ✅ |
| 2.10 | **B1 编号唯一性** | `MyriadCreation/docs/architecture/SYSTEM_OWNERSHIP.md:19,26,47`；`docs/architecture/SYSTEM_OWNERSHIP.md:73-81`；`docs/audit/ARCH-COMPLETION-PLAN.md:143` | 仓内已有两套 B1（旧：平铺目录归位；新 B1–B8：owner 归位），本批次是第三套"B1"（`DECISIONS_M1_M2_M3.md:278`） | ⚠️ 文档追踪风险（建议改 C1–C5 或 W23.x） |

## 3. 与既有头衔模型的关系（本次最关键发现）

### 3.1 现有两套头衔模型：一套在跑，一套是死代码

| 层 | 类型 | 位置 | 实际消费者 | 判定 |
|---|---|---|---|---|
| **活（命名/称谓）** | `Content.Modding.TitleDef`（kind/rank/weight/semantic/authorities/modifiers/cultureId） | `Content/MyriadCreation.Content.Modding/TitleDef.cs:7-24`（`:11` kind、`:19` authorities） | `ContentRegistry.cs:54`（`Dictionary<string,TitleDef>`）、`ContentResolvers.cs:37` 从 `StreamingAssets/Base/Title/Titles.json`（15 条）加载 → `TitleCatalog.cs:10,23,37,62` → `RealmTitleEvolution.cs:34` + `TitleCatalogTests.cs` | ✅ 真正在跑 |
| **死（治理/任职模板）** | `TitleDefinition` + `TitleEligibility` + `TitleManager` + `TitleCandidateContext` | `Simulation/MyriadCreation.Simulation.Definitions/TitleDefinition.cs:8-81`、`TitleEligibility.cs:8-85`、`Simulation/…Systems/TitleManager.cs:8-107`、`Content/…Definitions/TitleCandidateContext.cs:6-27` | `TitleManager` 全仓仅 1 处出现（自身声明 `:8`）；`TitleDefinition` 仅被 `TitleManager` 引用；`TitleEligibility` 仅被 `TitleDefinition.cs:34` 引用；`TitleCandidateContext` 只被 `TitleEligibility.cs:30`/`TitleManager.cs:86` 引用 → **0 生产、0 存档、0 UI、0 测试** | ⚠️ 零消费者 |
| **活（官职兼容）** | `OfficialOffice` 6 值 + `OfficeTitleEntry` | `Content/…Definitions/OfficialOffice.cs:3-11`、`OfficeTitleEntry.cs:6-13` | `OfficeTitleCatalog.cs:14,29` → `CharacterDetailQueryAdapter.cs:52`；`RealmData.cs:24 officeHolders` 经 `WorldEntitySaveData.cs:393-433` 落盘 | ✅ 在跑（M1 计划改为兼容视图） |

- 影响 B2 的硬事实：B1 记录 `DECISIONS_M1_M2_M3.md:309-327`（§8.2）把"既有头衔模型"认定为 `TitleDefinition` 家族，并据此定"B2 扩展 `TitleDefinition`"。但该家族**没有任何消费者**，B2 若直接给它加 `holders`，产出的是**第三套没人读的模型**，违反 `ARCHITECTURE_RULES.md:66`「不新建平行系统」与 `MyriadCreation/docs/architecture/SYSTEM_OWNERSHIP.md:65` R6「兼容层唯一」。
- `TitleManager.Register`（`:14`）全仓 0 调用 → 即使加字段也不会被 JSON/生产端填值；`FilterEligible`（`:86`）同样 0 调用。

### 3.2 `kind` 分类现在有 4 个来源（B1 加了第 4 个）

| # | 来源 | 位置 | 值 | 消费者 | 判定 |
|---|---|---|---|---|---|
| 1 | JSON 字面量 | `StreamingAssets/Base/Title/Titles.json` | monarch 6 / bureaucratic 3 / noble 6 / honorific 0 | `TitleCatalog.cs:28,43` 字符串比较；`RealmTitleEvolution.cs:34` 硬编码 `"monarch"` | 活的唯一真源（但无常量约束） |
| 2 | `TitleKind` 常量类 | `Content/…Content.Definitions/TitleKind.cs:3-11` | 4 字符串 | **0 引用**（死） | ❌ 新增/重复 |
| 3 | `TitleKinds` 常量类 + `All` | `Content/…Content.Modding/TitleKinds.cs:3-14`（`:14` `All`） | 4 字符串 | 仅 `SemanticTags.cs:29 IsValidKind` ← 该方法本身 0 引用 | ❌ 死链 |
| 4 | `DerivedTitleKind` 枚举 | `Content/…Content.Definitions/DerivedTitleKind.cs:7-14` | Landed/Office/Honor/NobleRank/Claim | **0 引用** | ⚠️ 计划中的派生视图（M1 `:49-57`） |
| 5 | `TitleDefinition.kind` 字符串字段 | `Simulation/…Definitions/TitleDefinition.cs:14` | 未赋值（无人写） | `TitleManager.cs:41 ByKind` ← 0 调用 | ⚠️ 死字段 |

- **是否与 `TitleKind` 混淆**：编译层不冲突（不同类型名）；但同目录、仅差 `Derived` 前缀，且语义都与 "kind" 相关，grep/IntelliSense/后续 `TitleKindClassifier` 极易误用；`DerivedTitleKind.cs:4-5` 的注释本身就在解释"与旧 TitleKind 不同"，说明作者已感到歧义。
- **改名建议**：`DerivedTitleKind` → `TitleDerivedCategory`（或 `TitleClass`），并迁 `Core/MyriadCreation.Core.Enums/`（`ARCHITECTURE_RULES.md:44-45`）；`TitleKind.cs` 删除，`TitleKinds.All` 保留为唯一常量源并供 B2 的 `kind → DerivedTitleKind` 映射表使用。
- **M1 冲突提醒**：M1 裁决「数据层不存 kind、5 值只作派生视图」（`DECISIONS_M1_M2_M3.md:20,30-32,65`），而现状 `TitleDefinition.cs:14` 与 `Titles.json` 的 `kind` 都是硬分类。B2 必须给"kind → 派生类"显式映射并把旧字段标 legacy，否则"派生视图"会被当成第二套硬分类。

## 4. 契约设计问题清单

| ID | 级别 | 问题 | 证据 | 影响 / 建议 |
|---|---|---|---|---|
| C1 | 🔴 高 | `AuthoritySource` **缺 `Regent`（摄政）** | 枚举 `ActionGateContracts.cs:6-16`（8 值）；M2 验收明确"元首/**摄政**/议会授权宣战通过"（`DECISIONS_M1_M2_M3.md:123-124`）；摄政态已存在（`RealmData.cs:94 regentCharacterId`、`:121-123`） | B3 的"三分支"验收无法按原文实现；建议 `Regent = 8`（可同时保留 `HouseholdHead` 供家族摄政/家督区分） |
| C2 | 🔴 高 | `AuthoritySource` **无任何载体** | 该枚举全仓 0 引用；`ActionContext.cs:30-40` 无 authority 字段；`GateResult.cs:49-55` 无"授予来源"字段 | AuthorityProvider 无法输出"凭什么授权"，门禁日志/UI 只能显示层，不能显示"以元首/摄政/议会身份"；建议 `GateResult` 增 `GrantedBy`（`ActionGateLayer` 之外再加"授权来源"），或 `IActionGate` 增 `out AuthoritySource` 查询方法 |
| C3 | 🔴 高 | 与 `ProposalAuthority` 形成**双授权真源且无映射** | `ProposalAuthority.cs:6-14`（HeadOfState/CentralInstitution/CharterHolders）仍在使用：`LawDef.cs:58`、`LawAndDecreeSystem.cs:506-534`（`:508/:515/:527`）；M2 宣布废弃（`DECISIONS_M1_M2_M3.md:111`）但无迁移表 | B3 的 LegalityProvider 会同时面对两套枚举；建议立即补映射：`HeadOfState → HeadOfState/CoRuler/Regent`、`CentralInstitution → Council/Office`、`CharterHolders → CharterHolder`，并在 B3 让法律入口只读映射结果 |
| C4 | 🟡 中 | `GateResult.Allow()` 填**假失败层** | `ActionGateContracts.cs:57-60`：成功结果 `FailedLayer = Capability` | 成功仍带"能力层"语义，UI/日志误读为"能力层被绕过"；建议 `ActionGateLayer.None = 0` 或 `FailedLayer` 可空，`Allow()` 用 None |
| C5 | 🟡 中 | 拒绝原因**只有中文自由文本，无机器码** | `ActionGateContracts.cs:55,62-65`；对比既有约定 `CommandResult.cs:3`（`code`+`message`）、`SimulationProcessResult.cs:5`（`resultCode`/`resultSummary`）、`SimulationActivityTransition.cs:3`（`reasonCode`） | 不可本地化（`ARCHITECTURE_RULES.md:75-76` 内容/i18n 分离）、UI 无法按 code 分支；建议 `Deny(layer, reasonCode, message = null)`，测试断言 code |
| C6 | 🟡 中 | `IActionGate` 与 `IActionGateQuery` **双接口重复** | `ActionGateContracts.cs:69-72`（返回 `GateResult`）vs `:75-78`（`bool CanExecute(out string reason)`） | 查询版本丢掉 `FailedLayer`、无 code，两个实现必然漂移；建议 `IActionGateQuery` 直接返回 `GateResult`（等价 `bool` 由调用方取 `.Allowed`），或 `IActionGateQuery : IActionGate` |
| C7 | 🟡 中 | **target 模型不可表达 B3 三个动作** | `ActionContext.cs:30` 只有 `targetRealmId`/`targetCharacterId` | 立法 target 是 `lawId`（string，`Core.Contracts/LawContracts.cs:84 BeginEnactLaw`）、任命 target 是"头衔+候选人"、法令是 `decreeId`；建议 `TargetKind` 枚举 + `targetId`（string/int），或按动作携带 payload |
| C8 | 🟡 中 | 契约未规定"非法上下文必须拒绝" | `ActionContext.cs:43 IsValid`（只查 ActionId 非空）、`:45 HasActor` | 实现方可能对 `!HasActor` 放行（fail-open）；建议接口注释强制"`!HasActor` → `Deny(Capability/Legality)`"，并加测试 |
| C9 | 🟢 低 | 缺机构 actor | M1 允许机构持头衔（`DECISIONS_M1_M2_M3.md:40-41` `institutionId`） | `ActionContext` 无 `institutionId`，议会/教会以机构名义行动无法表达；B3 前补或标注二期 |
| C10 | 🟢 低 | `actionId` 是裸字符串、无常量/注册表 | 全仓 0 个 action id 常量（`Core.Constants` 目录已存在）；`ActionContext.cs:36` | 拼写错 → provider 静默不匹配（B3 可能出现"谁都能做"）；建议 `Core.Constants` 增 `ActionIds` 并与之对齐 |

- **不成立的疑点（已核对）**：`-1` 哨兵与既有约定一致（`TileCharacterContracts.cs:54,109`、`UiQueryContracts.cs:108`；Simulation 侧同样用 `realmId >= 0`，如 `LawSystem.cs:100`），`ActorRealmId >= 0` **不是** bug；`in` 参数对 24 字节只读 struct 合理，与仓内 `SimulationProcessContext.cs:3` 等风格一致。
- `ActionContext.cs:68` 注释"UI 不得直接引用"表述易误读为"UI 不能用契约"；按既有惯例 UI 直接消费 `Core.Contracts`（`UIManager.cs:30 _lawQuery`），应为"UI 不得引用 Simulation 实现"。

## 5. 测试质量（6 条 / 16 个断言）

| # | 测试 | 覆盖 | 问题 | 建议 |
|---|---|---|---|---|
| 1 | `GateResult_AllowAndDeny`（`:19-29`） | Allow 的 `Allowed`/`Reason`；Deny 的层与原因 | 未断言 `Allow().FailedLayer`（恰好回避了 C4）；`Reason` 断言中文串 `"无授权"`（`:28`），改 reasonCode 会连带失败 | 改为断码；补 `Allow().FailedLayer == None` |
| 2 | `ActionContext_DefaultsAndHasActor`（`:32-44`） | 默认 struct 无效、单 actor 有效、target 默认 -1 | 缺边界：`ActionId` 有值但 actor 双 -1（`IsValid=true, HasActor=false`）；缺 target 校验 | 补 2 条断言 |
| 3 | `ActionGate_ContractIsImplementable`（`:47-53`） | 接口可被实现 | 只走 Allow 分支，近同义反复 | 用 `DenyGate` 桩测拒绝分支（层/原因透传） |
| 4 | `AuthoritySource_HasSevenSourcesPlusNone`（`:56-59`） | 反射计数 = 8 | 纯反射计数，违反 `ARCHITECTURE_RULES.md:118`「行为测试优先于纯反射测试」；且无法发现"缺 Regent"（数量对、语义错） | 改成显式值/映射断言（如 `(int)Regent` 稳定、与 ProposalAuthority 映射表） |
| 5 | `ActionGateLayer_HasFourLayers`（`:62-65`） | 反射计数 = 4 | 同上，无行为价值 | 改为"层序 = 短路顺序"的行为断言（B3） |
| 6 | `DerivedTitleKind_HasFiveDerivedValues`（`:68-71`） | 反射计数 = 5 | 锁死数量：B2 加 `Unknown`/新值就必须改测试，却不测派生逻辑 | 删除，改测 `kind → 派生类` 映射（B2 分类器落地时） |
| — | **缺** | — | 无拒绝分支、无多 provider 组合/短路约定、无非法上下文 | 契约层至少补 1 条"Deny 透传层+码"；多 provider 组合可留 B3，但契约应写明"按 Capability→Authority→Legality→Resource 顺序短路，返回**第一个**失败层" |

- 无空断言（16 个 `Assert.That` 均有实质比较）；失败信息可读；`AllowAllGate` 桩实现正确（`in` 参数一致）。测试放在 `Tests/EditMode` 符合 `ARCHITECTURE_RULES.md:118`。

## 6. 残留与落盘检查

| 项 | 方法 | 结果 |
|---|---|---|
| 孤儿 `.meta`（全 Assets） | 逐 `.meta` 检查同名资产是否存在 | **0 个** |
| `TitleDef`/`TitleModule`/`TitleRight`/`TitleEligibility` 残留 | `Content.Definitions/` 目录枚举 + 全仓 `grep` | 无同名 `.cs`/`.meta`；`TitleDef` 仅存在于 `Content.Modding/`（既有）、`TitleEligibility` 仅存在于 `Simulation.Definitions/`（既有） |
| 悬空代码引用 | `grep TitleRight\|TitleModule\|TitleBinding\|TitleInstance\|TitleHolding --include=*.cs` | 0 处代码引用；仅 `docs/design/*` 设计文档提及未来类型（`CIV_COGNITION_TITLE_CURRENCY.md:94-121` 等），属计划而非悬空 |
| 新 `.meta` GUID | 全局 `guid` 去重 | 4 个新 GUID 均唯一，无与既有冲突 |
| B1 未跟踪清单 | `git status --porcelain` | 仅 §1 的 8 个文件（4 `.cs` + 4 `.meta`）；记录中"曾建后删"的 4 文件在 git 中**没有任何痕迹**（从未 `add`），删除干净 |
| 既有 title 文件是否被改 | `git status` 过滤 `Title` | 无 ` M` 的 title 文件（` D` 均为早前目录迁移，非本批次） |

## 7. B2 建议：扩展而非新建（含唯一真源归一）

| 目标 | 现状证据 | B2 做法 | 落位 |
|---|---|---|---|
| **唯一真源（最关键）** | `TitleManager.cs:14 Register` 0 调用；`TitleDefinition` 0 消费者；活的只有 `TitleDef`→`TitleCatalog` | **先归一**：明确"`Content.Modding.TitleDef` = 命名/称谓内容行；`Simulation.Definitions.TitleDefinition` = 治理/任职模板（modules/appointment/eligibility/seats）"，并在 ContentHost 加载 `Titles.json` 后构造 `TitleDefinition` 并调 `TitleManager.Register`（先只读兼容，不改行为）。**不要**在未归一前给 `TitleDefinition` 加 `holders` | Simulation.Definitions 保持原位（`DECISIONS_M1_M2_M3.md:326`） |
| modules | `TitleDefinition.cs:30 modules`（`Dictionary<string,bool>`）+ `TitleModules.cs:5-33`（15 键） | **复用**；不新建 `TitleModule` 类型 | 原位 |
| rights（管辖权权利束） | 现无；`TitleDef.cs:19 authorities` 是**命名语义**标签（`SemanticAuthority.cs:5-25`：military/elder_council/…），不等于征税/司法/任命权利 | **新建** `TitleRight`（flags/枚举）；挂在 `TitleDefinition`；**不要**复用 `authorities` | `Simulation.Definitions`（与模板同层） |
| holders / 共有头衔 | 无；`RealmData.cs:24 officeHolders` 是"官职→角色"单值映射，非头衔持有 | **新建** `TitleHolding`（character/institution、role、share、since/end、legitimacy）+ `TitleInstance`（holders 列表）；不塞进 definition（definition 是模板）。复用 `TitleHoldingMode.cs:3-9`（Single/ConcurrentHolding/MultiPeopleClaim/PersonalUnion）与 `TitleInheritanceMode.cs:3-7`（SoleHeir/FamilyShared）作为**定义层策略**，角色/份额放 holding，别再建第三套 role 枚举 | `Simulation/MyriadCreation.Simulation.Politics/`，namespace `MyriadCreation.Simulation.Politics`（`DECISIONS_M1_M2_M3.md:331`；该目录需新建，`sim.asmdef` 在 `Simulation/` 根覆盖子目录，**无需新 asmdef**） |
| 派生 kind | `DerivedTitleKind.cs`（0 引用）；`TitleDefinition.cs:14 kind` 死字段；JSON kind 4 值 | **新建** `TitleKindClassifier.Classify(TitleInstance)`（只查询不落库，M1 `:47-57`）；同时补 `kind → DerivedTitleKind` 映射表覆盖现有 15 条 JSON（monarch/bureaucratic/noble；honorific 0 条需补内容或标空） | 分类器 `Simulation.Politics`；枚举建议迁 `Core.Enums` 并改名 |
| 官职兼容 | `OfficialOffice.cs:3-11` + `OfficeTitleCatalog.cs:14` + `RealmData.cs:24` | **只读兼容视图**，不新建第二套官职逻辑（M1 `:63-64` 已否决独立官职系统） | 原位 |
| 资格 | `TitleEligibility.cs:8-85`（9 维硬约束）+ `TitleCandidateContext.cs:6-27` | 保持硬约束**不加能力维**（M2 `:115-117`）；`CapabilityRequirement` 单列 | 原位 |
| 存档 | B4 才落盘（`DECISIONS_M1_M2_M3.md:307`） | B2 `TitleHolding/TitleInstance` **不入 DTO**，避免动 `SaveVersion` | — |
| 测试驱动 | `FilterEligible`/`Register` 0 调用 | B2 每条扩展都要从真实入口驱动（加载 `Titles.json` → 查询 → 派生类），否则仍是"没人读"的字段（`ARCHITECTURE_RULES.md:119`） | — |

**一条最重要建议**：B2 的胜负手不是加字段，而是**先把"活的 `TitleDef`/`TitleCatalog`"与"死的 `TitleDefinition`/`TitleManager`"归一成一个真源并接线（Register 目前 0 调用）**；在归一之前给 `TitleDefinition` 加 `holders`，等于造第三套没人读的头衔模型。

## 8. 必改三条（B3 开工前）

1. **修 ActionGate 契约与 M2 验收不一致**：`AuthoritySource` 补 `Regent`（`ActionGateContracts.cs:6-16` vs `DECISIONS_M1_M2_M3.md:123-124`）；给 `AuthoritySource` 一个载体（`GateResult.GrantedBy` 或 `IActionGate` 查询出参），并补 `ProposalAuthority`（`ProposalAuthority.cs:6-14`）→ `AuthoritySource` 的映射表，避免双权限真源。
2. **修 `GateResult`/`IActionGateQuery` 形状**：`Allow()` 不再填 `Capability`（`ActionGateContracts.cs:59`）；`Deny` 增机器可读 `reasonCode`（对齐 `CommandResult.cs:3`）；`IActionGateQuery` 改为返回 `GateResult`，与 `IActionGate` 统一；补"非法上下文必须 Deny"的契约约定 + 拒绝分支测试。
3. **收掉第 4 个文件与分类歧义**：删除 `TitleKind.cs`（与 `TitleKinds.cs:5-11` 逐字重复、0 引用、本批次写入），把 `DerivedTitleKind` 改名（如 `TitleDerivedCategory`）并迁 `Core.Enums`，同时在 B1 记录中更正"3 文件"为实际 4 文件并说明原因。

## 9. 审计方法与限制

1. 只读：未编译、未跑 EditMode/PlayMode、未改任何文件；`COMPILE_OK`/669 全绿按 Lead 记录采信，未独立复验。
2. "新增/未改"判定用 mtime + git 双证据；仓库整树未跟踪，git 无法区分新增与既有，故以 B1 时间窗（01:50–02:20）唯一写入集为准。
3. "死代码/0 引用"判定为符号级 grep（含 `--include=*.cs` 全 Assets + UI），未含反射/字符串拼装调用；仓内测试无对 `TitleManager`/`TitleDefinition` 的反射使用。
4. 本批次未涉及存档 schema、未改行为，故未触发 `SaveVersion`/迁移路径审计。
