# 裁决记录：M1 头衔/官职本体 · M2 行动门禁 · M3 角色域基线

> 2026-11-07。依据：`TOTAL_SYSTEM_SYNTHESIS.md`、`RELATION_TAXONOMY_AND_TITLE.md`、`POLITY_TITLE_CONTRACT_MODEL.md`、`SUPREME_AUTHORITY_MODEL.md`、`ARCHITECTURE_2_0_LAW_OFFICE_NAME_REGION_MODEL.md`、`UNIFIED_MASTER_PLAN.md`、`docs/角色系统整体设计.docx`、8.1 新稿、`docs/research/character-2026-11-07/01-character-code-audit.md`、`03-plan-docs-inventory.md`。
> 性质：**已裁决**。后续设计与代码以本文为准；与旧文档冲突时，旧文档让位（并在旧文档标注"由 DECISIONS_M1_M2_M3 取代"）。

## 0. 摘要

| 编号 | 裁决 |
|---|---|
| M1 | 头衔 = **权利束载体**，数据层不存 `kind`；五类只作**派生视图**；官职 = 带 office 模块的头衔；共有头衔用 `holders` 列表 + role/share |
| M2 | `ActionGate` 四层（能力/职权/法理/资源）为**唯一门禁契约**；最高权力采**四轴**；任职资格 9 维**不加能力硬约束**，能力门槛单列为 `CapabilityRequirement` |
| M3 | `角色系统整体设计.docx` = 现状基线；8.1 新稿 = 目标设计；`HONORIFIC` 覆盖称号；三层属性以 8.1 为准；信仰三形态收敛为两字段 |

---

## 1. M1：头衔 / 官职本体

### 1.1 裁决

**采 `TOTAL_SYSTEM_SYNTHESIS` 的"无硬 kind、由绑定/权利推导"原则；`LAW_OFFICE` 的 `title_kind`（君主/官僚/爵位）与 `RELATION` 的 5 值 kind 都不落库。** 头衔是"法律地位 = 权利束 + 绑定"，官职只是带 office 模块/权利的头衔，不另立系统。

### 1.2 数据规则

```
TitleDef              # 定义层（模板）
  ├─ title_id / tier / superior_title
  ├─ modules: { head_of_state, personnel_authority, general, central_office,
  │             local_ruler, religious_office, treasury_office, judicial_office, ... }
  ├─ rights: { 征税 / 征召 / 司法 / 任命 / 立法 / 礼仪 / 外交 / 军事指挥 }
  ├─ bindings: { territory?, polity?, office?, liege?, contract?, time? }
  ├─ appointment_methods[] / eligibility / inheritable / seats / term_years
  └─ （无 title_kind 字段）

TitleInstance          # 运行时
  ├─ holders: List<TitleHolding>      # 支持共有
  ├─ liegeTitleId / contractIds / legalOrderId
  └─ settlementIds[] / controlNetworkId

TitleHolding
  ├─ characterId（或 institutionId，机构持有）
  ├─ role: sole / senior / co-ruler / elector / regent
  ├─ share（0–1）
  ├─ sinceDay / endDay / legitimacy
  └─ 权利覆盖（可选：局部权利束）
```

### 1.3 派生视图（只查询、不落库）

`kind = derive(bindings, rights, modules)`：

| 派生 kind | 判据 |
|---|---|
| Landed | 有 settlement/control 绑定 |
| Office | 有 office 模块，且权利含职责/职权（任命、行政、司法等） |
| Honor | 只有礼仪权利，无领地、无管辖权 |
| NobleRank | 世袭、有层级、无领地绑定、无管辖权 |
| Claim | 未被持有，但存在 claim 记录 |

### 1.4 被否决的方案

| 方案 | 否决理由 |
|---|---|
| 存 `title_kind` 三值（君主/官僚/爵位） | 无法表达共治、多头政治、跨政权头衔、荣誉头衔；与派生视图职责重叠 |
| 独立官职系统（`OfficialOffice` 六值继续用） | 与"官职统一归口头衔管理器"（`LAW_OFFICE:1025` 已定口径）冲突；任命/继承/资格逻辑会分叉两套 |
| 5 值 kind 落库（Landed/Office/Honor/NobleRank/Claim） | 同样会变成硬分类；共治、摄政、机构持有都会撑破枚举 |

### 1.5 冲突收口

| 冲突 | 裁决 |
|---|---|
| X1 四套 kind | 统一为 §1.3 派生 5 类视图 |
| X2 官职是否独立 | 不独立；office 模块 + 职权权利即官职 |
| X3 荣誉头衔归属 | 有礼仪权利的 → 轻头衔（Honor）；**完全无权利**的 → 称号系统（尊号/绰号/谥号），不进头衔 |
| X6 共有头衔 | `holders` 列表 + `role/share`；国家继承轨只处理 senior/sole，共治者按契约保留 |
| X4/X5 称号 | 尊号/绰号/谥号按 `HONORIFIC_NICKNAME_SYSTEMS` 独立，不塞回头衔 |

### 1.6 影响面

- 文档：`LAW_OFFICE` §5.2 删除 `title_kind`，补 `holders/role/share`；`RELATION` 的 kind 改注"派生视图"；`POLITY_TITLE_CONTRACT_MODEL` 的 `TitleHolding` 采纳。
- 代码：新建 `TitleDef` / `TitleInstance` / `TitleHolding`（现均不存在）；`OfficialOffice` 六值迁移为头衔定义或兼容视图。
- 验收：同一头衔可查派生 kind；共治头衔可存两个 holder；机构可持有头衔。

---

## 2. M2：行动门禁

### 2.1 裁决

**`ActionGate` 四层为唯一门禁契约**：

```
ActionGate.Check(actor, action, target)
  = Capability ∧ Authority ∧ Legality ∧ Resource
```

| 层 | 数据来源 |
|---|---|
| Capability | 角色属性/专精/技能/健康；机构能力（行政力/军力） |
| Authority | 头衔/官职/机构席位/摄政/议会授权（`AuthoritySource`） |
| Legality | 法律域/CB/契约/停战/宗教法 |
| Resource | 国库/兵员/威望/合法性/时间 |

### 2.2 授权解析

`AuthoritySource` 枚举：`HeadOfState / CoRuler / Council / Office / CharterHolder / HouseholdHead / Consensus`。

解析链：`action → required authority → 从头衔 holders（modules/seats）与法律构成（assembly composition）解析持有者 → 无持有者则不可执行`。

### 2.3 最高权力

采 `SUPREME_AUTHORITY_MODEL` 四轴：**权力结构 / 法理化程度 / 共治关系 / 继承产生**。`SupremeSovereignty` 二元字段降为兼容显示；`ProposalAuthority` 三值废弃，改用 `AuthoritySource`。

### 2.4 资格与能力分离

- `TitleEligibility` 9 维（性别/范围/王族/机构/文化/信仰/语言/区域/阶段规模）保持**硬约束**，不加能力维。
- 能力门槛单列 `CapabilityRequirement`（考试、技艺官、军职等明确需要能力时使用），不进硬约束池。
- 硬约束与权重分离原则（`LAW_OFFICE:969-973`）不变。

### 2.5 宣战的验收标准

| 场景 | 预期 |
|---|---|
| 无授权角色（非元首/摄政/议会授权）宣战 | 拒绝，返回具体原因 |
| 元首/摄政/议会授权宣战 | 通过 |
| 元首 Martial 高 / 低 | **不影响**能否宣战；只影响战役执行（伤亡、士气、战术） |
| 无 CB 宣战 | 可执行但付威望/合法性代价（沿用 `WarJustification`） |
| 国库不足 | 拒绝或大幅提高代价 |

现状必须改：`DiplomacyManager.DeclareWar` 只有 relation/truce 检查，**没有 actor 解析**；AI 只看 `treasury > 200`。需新增 `WarDeclarationGate`，先解析 authority holder 再走四层。

### 2.6 被否决的方案

| 方案 | 理由 |
|---|---|
| 把宣战/立法/征税/任命写进个人技能树 | 权限与能力混层；且角色死亡/换人会带走政权能力 |
| 每个动作各写 `CanXxx` | 现有 7 个片段已经分散，必须统一 provider |
| 资格硬约束里加角色能力 | 会让"考试/任命"变成属性检定，硬约束与权重分离失效 |

### 2.7 影响面

- 新增 `IActionGate` + 4 个 provider 接口；收编 `TitleEligibility`、`ProposalAuthority`、`OfficeFactor`、`DecreeOrder`、组织阶段等片段。
- 先接三个动作：宣战、立法/法令、任命；其余动作后续接入。

---

## 3. M3：角色域基线

### 3.1 三份文档的权威分配

| 文档 | 管什么 | 不管什么 |
|---|---|---|
| `角色系统整体设计.docx`（2026-09-24） | 现状基线：六维、人格、特质、关系、家族、健康、DNA、称号现状 | 技能、专精、教育、成长、门禁（全无） |
| 8.1 新稿（2026-11-07） | 目标设计：三层属性、专精=能力+训练、技能树、双维健康 | 家族/继承细则（未写） |
| `HONORIFIC_NICKNAME_SYSTEMS.md` | 称号拆分：尊号/绰号/谥号独立；可追尊/加尊/夺号 | 头衔本体（归 M1） |

### 3.2 冲突收口

| 冲突 | 裁决 |
|---|---|
| X16 属性 vs 专精 | 三层：基本能力 → 专精 → 技能树；docx 六维改称"专精"；专精由公式派生（见下） |
| X17 信仰三形态（社会/私人/秘密） | 收敛为 `faithId`（公开归属）+ `privateFaithId`/`isSecretBeliever`（私下）+ `personalTenets`；不新增第三实体 |
| X4/X5 称号 | `HONORIFIC` 覆盖 docx §11.7；`DeterminePosthumousTitle` 迁入 `HonorificSystem` |
| X18 家族/继承 | docx 家族资产/家徽/传统作基线；继承三轨（国家/家族/地方）按 `LAW_OFFICE` §6；`Family` 补进 `TOTAL_SYSTEM_SYNTHESIS` 本体表 |
| X13/X14/X15 法律 | 不在本裁决范围，另行处理；但 ActionGate 的 Legality provider 以法律域为唯一入口 |

**专精公式（目标设计）**：

```
basic_norm   = 映射子属性加权均值 / 100
training_norm = 教育/实践/任职积累 / 100
specialty     = 100 × √(basic_norm × training_norm) + 特质/疾病修正
```

基本能力决定成长速率与上限；无训练时专精近 0。

### 3.3 一页权威表（角色字段 → 唯一权威）

| 字段 | 唯一权威 | 备注 |
|---|---|---|
| 基本能力 12 项 | 8.1 新稿 | 代码当前恒 50，需生成 |
| 专精六维 | 8.1 新稿 + 公式 | 替换 `Random.Range(20,80)` |
| 技能树/点数 | 8.1 新稿（待细化） | 代码不存在 |
| 健康/压力/精神疾病 | docx §7 + 代码 | 代码已实现阈值 |
| 人格 7/8 维 | docx + 代码 | `Sociability` 需补字段或删 |
| 特质 108 条 | `PersonalityTraitDatabase` | 需接 `AddTrait` |
| 关系/纽带 | docx §5 + `CharacterRelation` | 需接生产端 |
| 家族 | `FamilyNode` + docx §6 | 需接分支/传统/声望 |
| 继承 | `LAW_OFFICE` 三轨 + `InheritanceLaw` | 头衔/领地/财产未实现 |
| 称号 | `HONORIFIC` | 覆盖 docx §11.7 |
| 信仰 | `FAITH_DOCTRINE_CLASSIFICATION` + 代码 | 两字段方案 |

### 3.4 代码基线修复顺序（P0）

1. `BasicAbilities` 生成（DNA/种族 → 12 项）。
2. 专精派生公式（替换随机数）。
3. `GetEffectiveStats` 接线（特质/疾病/精神修正生效）。
4. 疾病系统二选一（角色级 vs 人口级），禁止双轨。
5. 存档补 `achievements/accessionDay/personalTenets/bodyMarks`。
6. 修 `Sociability` 与 `reignYears` 公式。
7. 接 `AddTrait`/`ModifyRelation`/`CreateBond` 生产端。

### 3.5 被否决的方案

| 方案 | 理由 |
|---|---|
| 以 docx 为准写技能/教育 | docx 全文 0 次"技能/专精/成长/门禁" |
| 三层属性只建字段不建公式 | 就会重复当前"恒 50 + 随机数"的空转 |
| 信仰三形态各建实体 | 与前文信仰结论冲突（公开/私下/个人信条是字段，不是三张表） |

---

## 4. 执行顺序

1. **代码骨架**：`TitleDef/TitleInstance/TitleHolding` + `ActionGate` 接口与 provider 壳（不接内容）。
2. **文档同步**：`TOTAL_SYSTEM_SYNTHESIS` 本体表补 `Character/Family`；`LAW_OFFICE` 按 M1 改；`UNIFIED_MASTER_PLAN` WBS 加 W23（角色系统）。
3. **角色 P0 修复**（§3.4）。
4. **专精公式与教育/成长**。
5. **技能树内容**（最后，依赖 §1.2 效果分类）。

## 5. 风险与回滚

- 派生 kind 查询频繁 → 加缓存/物化视图，避免每次遍历 bindings。
- 四轴最高权力与 `SupremeSovereignty` 并存期 → 兼容字段只读，不参与门禁。
- ActionGate 接入面大 → 先只接宣战/立法/任命三个动作，验收后再扩。
- 头衔本体改动会牵动法律/继承/派系 → 先在设计与代码骨架层落地，不批量迁移旧数据。

## 6. 验收（可复验）

- M1：同一头衔可查派生 kind；共治头衔可存两个 holder；机构可持有头衔；`OfficialOffice` 不再被新代码引用。
- M2：宣战三分支测试通过（越权拒绝 / 授权通过 / 技能不影响）；`ActionGate` 单点可加日志解释拒绝原因。
- M3：角色页能显示三层属性；专精可由公式复算；称号按 `HONORIFIC` 拆分；存档往返后 `achievements/accessionDay` 不丢。

---

## 7. 架构落位与批次（按 ARCHITECTURE_RULES / SYSTEM_OWNERSHIP）

> 原则：**目录即 owner（R1）**；程序集方向 `Core ← Content ← {Laws, Faith, Culture} ← World ← Simulation ← {Rendering, Infrastructure} ← {App.Contracts, App} ← UI`；UI **禁止**引用 Simulation 家族；定义/状态/DTO/契约分家；不新增平行系统；小步补丁。

### 7.1 新增物落位表

| 新增物 | 类型 | 程序集 | 目录 | 命名空间 | 存档 |
|---|---|---|---|---|---|
| `TitleDef`、`TitleRight`、`TitleModule`、`TitleBinding`、`TitleEligibility` | 内容定义（纯数据 + 校验） | `MyriadCreation.Content` | `Content/MyriadCreation.Content.Definitions/`（扁平，与 `OfficialOffice.cs` 同层） | `MyriadCreation.Content.Definitions` | 否（JSON 内容） |
| `TitleInstance`、`TitleHolding`、`TitleRegistry`、`TitleKindClassifier` | 运行时状态 + 派生视图 | `MyriadCreation.Simulation` | `Simulation/MyriadCreation.Simulation.Politics/` | `MyriadCreation.Simulation.Politics` | 是（**第二批**，需 SaveVersion 批准） |
| `IActionGate`、`ActionContext`、`GateResult`、`AuthoritySource` | 跨系统契约/DTO/枚举 | `MyriadCreation.Core` | `Core/MyriadCreation.Core.Contracts/ActionGateContracts.cs` | `MyriadCreation.Core.Contracts` | 否 |
| `ActionGate` + `CapabilityProvider`、`AuthorityProvider`、`LegalityProvider`、`ResourceProvider` | 模拟宿主逻辑 | `MyriadCreation.Simulation` | `Simulation/MyriadCreation.Simulation.ActionGate/` | `MyriadCreation.Simulation.ActionGate` | 否（无状态） |
| 宣战接线 | 改造现有入口 | `MyriadCreation.Simulation` | `Simulation/MyriadCreation.Simulation.Diplomacy/DeclareWarCommandHandler.cs` + `WorldState` 注入 | 现有命名空间 | 否 |
| 只读门禁查询（给 UI/App） | 契约 + 适配 | `Core`（DTO）/`App.Contracts`（适配） | `Core/MyriadCreation.Core.Contracts/` / `App/MyriadCreation.App.Contracts/` | 现有 | 否 |
| 门禁测试 | EditMode | Tests | `Assets/Tests/EditMode/` | 测试程序集 | 否 |

### 7.2 依赖方向校验

- `Content.TitleDef` 只引 Core → 合法（L1）。
- `Simulation.Politics` 在 Simulation 内，可引 Core/Content/World/Laws/Faith/Culture → 合法（L4）。
- `Simulation.ActionGate` 同上；`LegalityProvider` 经 `LawContracts`/`ILawContext` 访问法律域，**不反向引** Simulation 到 Laws。
- `Core.ActionGateContracts` 零引用 → 合法（L0）；UI 只能经 App.Contracts 读，不碰 Simulation。
- 不新增程序集、不改 asmdef；`AsmdefArchitectureGateTests` 必须保持全绿。

### 7.3 集成点（不新增平行系统）

| 新功能 | 接哪个现有入口 | 不做什么 |
|---|---|---|
| 头衔运行时 | 现有 `RealmData.officeHolders` / `OfficeTitleCatalog` / `OfficialOffice` 六值作为兼容视图；`TitleRegistry` 先只读兼容，再迁移 | 不新建 `TitleSystem` 调度器、不复制一套官职逻辑 |
| ActionGate | `DeclareWarCommandHandler`、`LawAndDecreeSystem`、任命/交互入口 | 不新建 `WarDeclarationSystem` 第二套；不写 `CanXxx` 家族 |
| 只读查询 | `GameWorld.Contracts` / `App.Contracts` 现有查询注入 | 不让 UI 直接读 Simulation 状态 |

### 7.4 文件与命名规则

- 一个文件一个主类型；文件名为类型名；命名空间 = 目录路径。
- 命名后缀：定义 `*Def`、运行时 `*Instance`/`*Holding`、注册表 `*Registry`、系统 `*System`、命令 `*Command`、处理器 `*CommandHandler`、契约 `I*`、provider `*Provider`。
- 不新增 `Manager` 类承载纯数据；不把运行时对象塞进 Content；不把 Save DTO 放进 Simulation。
- 新文件放 `Assets/**` 内由 Unity 编译（`mc-compile.ps1` 走 batchmode），不手改 `.csproj`。

### 7.5 批次（小步，每批过门禁）

| 批次 | 内容 | 是否改行为 | 门禁 |
|---|---|---|---|
| B1 | Content 的 TitleDef 家族 + Core 的 ActionGate 契约 + 纯数据测试 | 否 | 编译 + EditMode |
| B2 | Simulation.Politics 的 TitleInstance/Holding/Registry/Classifier；`officeHolders` 兼容读 | 否（兼容层） | 编译 + EditMode + 架构门禁 |
| B3 | ActionGate + 4 providers；接 `DeclareWarCommandHandler`；三分支测试（越权拒绝/授权通过/技能不影响） | **是** | 编译 + EditMode + PlayMode×2 + 审计 |
| B4 | 存档接入（`TitleInstance` 入 DTO，SaveVersion 升版需单独批准） | 是 | 存档往返测试 |
| B5 | M3 角色 P0 修复（独立轨道） | 是 | 按 WORKFLOW 全门禁 |

### 7.6 禁止事项（本批）

- 不移动/重命名现有文件（除明确迁移批次）。
- 不改 `SaveVersion`（B4 前不落盘）。
- 不新增程序集/目录层级超过必要。
- 不在 UI 里写门禁逻辑。
- 不把 `ActionGate` 做成每动作一套条件；统一 provider。
- 不新增 `TitleSystem` 之类的平行调度系统。

---

## 8. B1 执行记录与发现（2026-11-07）

### 8.1 已落地

| 文件 | 内容 | 状态 |
|---|---|---|
| `Core/MyriadCreation.Core.Contracts/ActionGateContracts.cs` | `AuthoritySource`、`ActionGateLayer`、`ActionContext`、`GateResult`、`IActionGate`、`IActionGateQuery` | 新增，零引用（契约） |
| `Content/MyriadCreation.Content.Definitions/DerivedTitleKind.cs` | Landed/Office/Honor/NobleRank/Claim 派生枚举 | 新增，零引用 |
| `Assets/Tests/EditMode/TitleDomainContractTests.cs` | 6 条契约测试 | 新增 |

- 编译：`COMPILE_OK`（`mc-compile.ps1 -LogName b1-title-contracts-3`）。
- EditMode：**669/669 passed，0 failed**。
- 未接行为、未落盘、未改现有文件、未新增程序集。

### 8.2 关键发现：头衔模型已存在，B2 必须"扩展"而不是"新建"

| 既有类型 | 位置 | 已覆盖 |
|---|---|---|
| `TitleDefinition` | `Simulation/Definitions/TitleDefinition.cs` | titleId/designId/kind/tier/superior/modules(Dictionary)/appointment/eligibility/seats/term/salary/personnelWeight/residence/onGet/onFired |
| `TitleEligibility` | `Simulation/Definitions/TitleEligibility.cs` | 九维硬约束 + `IsHardPass(TitleCandidateContext)` |
| `TitleCandidateContext` | `Content/Definitions/TitleCandidateContext.cs` | 性别/王族/范围/机构/文化/信仰/语言/区域/阶段/规模 |
| `TitleModules` | `Content/Definitions/TitleModules.cs` | 15 个模块字符串键（head_of_state…military_council） |
| `TitleManager` | `Simulation/Systems/TitleManager.cs` | 注册/查询/ByKind/ByTier/ChildrenOf/GetChain/FilterEligible |
| `TitleCatalog` + `Content.Modding.TitleDef` | `Simulation/Definitions/TitleCatalog.cs`、`Content.Modding/TitleDef.cs` | 命名用内容 DTO（kind/rank/weight/semantic/authorities/modifiers） |
| `TitleHoldingMode` / `TitleResidence` / `TitleAppointmentMethod` / `TitleInheritanceMode` | `Content/Definitions/` | 持有模式/驻地/任命/继承 |

**结论（已按 04-b1-audit 修正）**：`TitleDefinition` + `TitleManager` 家族目前是**零消费者死代码**（`TitleManager.Register` 0 调用）；真正在跑的是 `Content.Modding.TitleDef` + `TitleCatalog` + `Titles.json`。B2 **不能**直接在死模型上加 holders（会造第三套没人读的头衔模型），必须先归一真源。M1 真正缺的是：运行时 holders（共有头衔）、typed rights、派生分类、Actor 门禁。B2 应：

- 扩展 `TitleDefinition`：加 `List<TitleHolding> holders`（或独立 `TitleRegistry` 键 titleId → holders）、加 `TitleRight` 权利束（或复用 `authorities`/`modules` 字符串键做映射）、加派生 kind 查询方法；
- 新增 `TitleHolding`（结构）：`characterId/institutionId`、`role: sole/senior/co-ruler/elector/regent`、`share`、`since/end`、`legitimacy`；
- 不新建 `TitleDef`/`TitleEligibility` 第二套（B1 曾误建，已删）；
- 不移动 `TitleDefinition`（避免牵动 `TitleManager`/`TitleCatalog`/JSON 加载）。

### 8.3 对 §7 落位表的修正

- 删除"`TitleDef`/`TitleRight`/`TitleModule`/`TitleEligibility` 新增到 Content.Definitions"一行；改为"扩展既有 `TitleDefinition` + 新增 `TitleHolding`"。
- `TitleHolding` 建议落 `Simulation/MyriadCreation.Simulation.Politics/`（polity/title 运行时的 owner 目录），namespace `MyriadCreation.Simulation.Politics`；`TitleDefinition` 保持原位。
- `ActionGate` 仍按 §7.1 落 `Simulation.ActionGate/`；`AuthorityProvider` 经 `TitleManager`/`TitleRegistry` 解析持有者。

### 8.4 B1 过程中的修正

- 首轮编译失败：`TitleDef` 与 `Content.Modding.TitleDef` 歧义（CS0104）；删除新建的 `Content.Definitions/TitleDef.cs` 等四个文件与孤儿 `.meta` 后恢复。
- `TitleKind.cs` 是未跟踪的既有文件（monarch/bureaucratic/noble/honorific 字符串常量），保持原样；派生枚举另起 `DerivedTitleKind`，避免语义冲突。
- `ActionContext` 增加 `IsValid`（`default` 结构 ActionId 为空，不能当有效上下文）。

### 8.5 B1 审计结论与修复（04-b1-audit）

审计判定"不通过（差距小）"，已按三条必修 + 中风险项修复：

| 项 | 修复 |
|---|---|
| C1 `AuthoritySource` 缺摄政 | 加 `Regent = 8`（共 9 值） |
| C2 授权无载体 | `GateResult` 加 `GrantedBy`（`AuthoritySource`） |
| C3 与 `ProposalAuthority` 双真源 | 契约注释写明映射表（HeadOfState→HeadOfState/CoRuler/Regent；CentralInstitution→Council/Office；CharterHolders→CharterHolder），B3 在 Simulation 侧实现，Core 不引 Laws |
| C4 `Allow()` 填假失败层 | `FailedLayer` 改为可空，`Allow()` 为 `null` |
| C5 无机器码 | `Deny(layer, reasonCode, message)`；`GateResult` 增 `ReasonCode`/`Message` |
| C6 双接口漂移 | `IActionGateQuery : IActionGate`，同一 `GateResult` |
| C7 target 不可表达立法/任命 | `ActionContext` 增 `TargetKind` + `TargetId`（Law/Decree/Title/Settlement） |
| C9 缺机构 actor | `ActionContext` 增 `ActorInstitutionId` |
| 2.7 派生枚举落位 | `DerivedTitleKind` 改名 `TitleDerivedCategory`，迁 `Core/MyriadCreation.Core.Enums/` |
| 3.2 kind 第 4 个来源 | 删除重复的 `Content.Definitions/TitleKind.cs`；保留 `Content.Modding/TitleKinds.cs`（唯一常量源） |

- 复验：`COMPILE_OK`（`mc-compile.ps1 -LogName b1-title-contracts-4`）；EditMode **672/672 passed，0 failed**。
- 审计报告：`docs/research/character-2026-11-07/04-b1-audit.md`。

### 8.6 B2 阻断项：头衔模型必须先归一

| 模型 | 位置 | 状态 |
|---|---|---|
| 活的命名模型 | `Content.Modding.TitleDef` + `TitleCatalog` + `Titles.json`（15 条） | 被 `RealmTitleEvolution` 使用 |
| 死的治理模型 | `TitleDefinition` + `TitleManager` + `TitleEligibility` + `TitleCandidateContext` | 0 生产/0 存档/0 UI/0 测试；`Register` 0 调用 |

B2 顺序（不满足就停）：

1. **归一真源**：决定 `TitleDefinition` 是否接管 `Content.Modding.TitleDef` 的命名数据（kind/rank/weight/semantic/authorities），或反向把治理字段并入 `TitleDef`；只留一套。
2. **接线**：`TitleManager.Register` 必须在内容加载时被调用（`ContentResolvers.Titles` → `TitleManager`）；否则加任何字段都无人读。
3. **直接替换 kind 模型（封闭开发，无 legacy）**：删除 `kind` 字符串/常量类，内容与运行时统一用 `TitleDerivedCategory`；不保留迁移映射。
4. **扩展 holders**：`TitleHolding` 落 `Simulation/MyriadCreation.Simulation.Politics/`，`TitleDefinition` 加 `List<TitleHolding> holders`（或独立 `TitleRegistry`）。
5. 然后才做 `ActionGate` 的 `AuthorityProvider`（B3）。

### 8.7 B2.0 头衔模型归一（2026-11-07）

| 改动 | 文件 | 说明 |
|---|---|---|
| `TitleDefinition` 补命名字段 | `Simulation/Definitions/TitleDefinition.cs` | 新增 `rank`/`weight`/`semantic`/`cultureNumericId`，接收 Titles.json 的命名数据 |
| 内容 → 运行时登记 | `Simulation/Systems/TitleManager.cs` | 新增 `LoadFromContent()`/`Clear()`：遍历 `ContentResolvers.Titles`，映射后 `Register` |
| 加载接线 | `Simulation/Modding/ContentRegistry.cs` | `Titles = _definitions.Titles;` 后调用 `TitleManager.LoadFromContent()`（`Register` 首次拥有调用者） |
| 目录改单一真源 | `Simulation/Definitions/TitleCatalog.cs` | 从 `ContentResolvers.Titles` 改为读 `TitleManager`；签名 `TitleDef → TitleDefinition` |
| 调用方适配 | `Simulation/Systems/RealmTitleEvolution.cs` | `TitleWord(TitleDefinition)` |
| 测试适配 | `Assets/Tests/EditMode/TitleCatalogTests.cs` | 文化专属用例改为 `TitleManager.Register(new TitleDefinition{...})` |

- 复验：`COMPILE_OK`；EditMode **672/672 passed**。
- 结果：`TitleManager` 成为运行时唯一头衔真源；`Content.Modding.TitleDef` 保留为 JSON 解析 DTO（符合分层）。
- 未做（B2.1）：`TitleHolding`（共有头衔）、`kind → TitleDerivedCategory` 映射、旧 `kind` 标 legacy、`TitleHolding.gender/role/share`。
- 已知风险：`TitleManager` 是静态注册表；`ContentRegistry.Reset()` 后重新 Initialize 会清空并重建（测试内注册的自定义头衔需在断言前注册）。

### 8.8 封闭开发：删除 legacy 模型（2026-11-07）

> 原则确认：**封闭开发，不需要旧内容/旧档/兼容层**。能删就删，不标 legacy、不写迁移。

| 删除/替换 | 文件 | 说明 |
|---|---|---|
| 删除 `TitleKinds` 常量类 | `Content.Modding/TitleKinds.cs`（删） | 4 个字符串 kind 的唯一常量源，0 引用 |
| 删除 `SemanticTags` | `Content.Modding/SemanticTags.cs`（删） | 三个校验方法全仓 0 引用 |
| 内容 DTO 换 schema | `Content.Modding/TitleDef.cs` | `titleId/category(TitleDerivedCategory)/rank/weight/semantic`；删 `kind/cultureId/authorities/modifiers` |
| 内容数据换 schema | `StreamingAssets/Base/Title/Titles.json` | `kind` 字符串 → `category` 整数（monarch→Landed、bureaucratic→Office、noble→NobleRank） |
| 运行时字段替换 | `Simulation.Definitions/TitleDefinition.cs` | `string kind` → `TitleDerivedCategory category`；删 `cultureNumericId` |
| 查询 API 替换 | `TitleManager`/`TitleCatalog` | `ByKind(string)` → `ByCategory(TitleDerivedCategory)`；`Highest(category)` 去掉 culture 参数 |
| 调用方适配 | `RealmTitleEvolution` | `Highest(TitleDerivedCategory.Landed)` |
| 测试适配 | `TitleCatalogTests`、`TitleLanguageDataTests` | 全部改 category；文化专属用例改为运行时注册 |

- 复验：`COMPILE_OK`；EditMode **672/672 passed**。
- 结果：头衔分类只剩一个真源（`TitleDerivedCategory` + `Titles.json.category`），旧 kind 字符串模型彻底移除。

### 8.9 B2.2 头衔共持（2026-11-07）

| 文件 | 内容 |
|---|---|
| `Simulation/Politics/TitleHolderRole.cs` | Sole / Senior / CoRuler / Elector / Regent |
| `Simulation/Politics/TitleHolding.cs` | characterId / institutionId / role / share / sinceDay / endDay / legitimacy / IsActive / IsInstitutionHolding |
| `Simulation/Definitions/TitleDefinition.cs` | 新增 `List<TitleHolding> holders` |
| `Tests/EditMode/TitleHoldingTests.cs` | 4 条测试 |

- 复验：`COMPILE_OK`；EditMode **676/676 passed**。
- 未入档（封闭开发，SaveVersion 未改）。

### 8.10 B3 门禁骨架与接线（2026-11-07）

| 文件 | 内容 |
|---|---|
| `Simulation/ActionGate/ActionGate.cs` | `ICapabilityProvider` / `IAuthorityResolver` / `ActionGate`（能力∧职权，fail-closed）/ `NoCapabilityGate` |
| `Simulation/ActionGate/ActionIds.cs` | `declare_war` / `enact_law` / `enact_decree` / `appoint_title` |
| `Simulation/ActionGate/RealmAuthorityResolver.cs` | 元首/执政官 → HeadOfState；空位期摄政 → Regent；未知政权 fail-closed |
| `Simulation/WorldState/GameWorld.Simulation.cs` | 新增 4 参 `DeclareWar(..., actorCharacterId)`：先过 `ActionGate`，再走原流程；3 参旧路径保持不变 |
| 测试 | `ActionGateTests`（4 条：越权/授权/能力不影响/无主体）、`RealmAuthorityResolverTests`（4 条：元首/摄政/他人/未知政权） |

- 复验：`COMPILE_OK`；EditMode **684/684 passed**。
- 注意：命名空间 `Simulation.ActionGate` 与类 `ActionGate` 同名，`GameWorld` 侧用 `GateService` 别名。

### 8.11 待办（按依赖）

1. AI 意图 / 玩家命令切到 4 参 `DeclareWar`（命令携带 actor）
2. Legality provider（CB/停战/条约）、Resource provider（国库/兵员）
3. M3 P0：BasicAbilities 生成、专精公式、`GetEffectiveStats` 接线、疾病单轨、存档补字段、`Sociability`/`reignYears`
4. 旧 `kind` 相关文档同步（`LEGAL/MASTER_PLAN` 等）与审计

### 8.12 待办：AddTrait 与教育/事件系统（2026-11-07 记录）

**决定**：`AddTrait` 的正式 gameplay 调用者**等教育/事件系统落地后再接**，不在本轮造临时规则。

现状（已完成，不重复做）：

- `CharacterManager.AddTrait(characterId, traitId)`：查 `PersonalityTraitDatabase`、`HasTrait` 去重、调用 `CharacterData.AddTrait`（公开生产端，事件/教育可直接调）。
- 创角时 `GrantSignatureTraits`：性格八轴按阈值授予签名特质（临时来源，不是教育/事件）。
- 特质修正已经通过 `GetEffectiveStats` 生效（战斗/法令/派系/腐败/研究）。

教育/事件落地后的接入点（按优先级）：

| 系统 | 接入点 | 授予内容 |
|---|---|---|
| 教育 | 童年教育完成、师承出师、学院毕业 | 对应能力/专精相关特质 |
| 事件 | 事件选项 effect（勇气/荣誉/残暴类抉择） | 单条特质，带冷却与去重 |
| 任命 | 就任特定头衔（军职/教职/法官） | 职业特质（如 marshal/zealous） |
| 婚姻/家族 | 联姻、继承、家族传统 | 家族性特质 |

验收：新增调用点后跑 `AddTrait` 去重测试；特质授予不得绕过 `HasTrait`；`GetEffectiveStats` 已有测试覆盖。

---

## 9. 批次总览与收口（2026-11-07）

| 批次 | 内容 | 验证 |
|---|---|---|
| B1 | ActionGate 契约、TitleDerivedCategory、纯数据测试 | Compile OK |
| B2.0 | 头衔归一：TitleManager 唯一真源、LoadFromContent 接线 | Compile OK |
| B2.1 | 封闭开发：删 kind 字符串模型，Titles.json 换 category 整数 | Compile OK |
| B2.2 | TitleHolding / TitleHolderRole / TitleDefinition.holders | Compile OK |
| B3 | ActionGate 骨架、RealmAuthorityResolver、4 参 DeclareWar | Compile OK |
| B3.3 | AI/命令携带 actor；Legality/Resource provider 接口 | Compile OK |
| B3.4 | 真实 DiplomacyManager 法理层 + 国库资源层接线 | Compile OK |
| M3.1 | BasicAbilities 生成（DNA 偏移） | Compile OK |
| M3.2 | 专精派生公式（Content 纯函数） | Compile OK |
| M3.3 | 公式接进 CreateCharacter；GetEffectiveStats 接战斗/法令 | Compile OK |
| M3.4 | 存档补 accessionDay/achievements/personalTenets/bodyMarks；Sociability；reignYears | Compile OK |
| M3.5 | AddTrait 生产端（创角签名特质 + 婚姻关系/纽带） | Compile OK |
| M3.6 | 派系/腐败/研究改有效值 | Compile OK；EditMode **694/694** |

### 9.1 收口状态

- 已收口：M1/M2/M3 的代码骨架、门禁、头衔归一、角色属性链、存档字段。
- 待办：疾病系统二选一；教育/事件落地后接 `AddTrait`（§8.12）；旧文档（`CHARACTER_PLAN_SEAM.md`、`LAW_OFFICE` 头衔字段说明）同步。
- 验证：每批均 `COMPILE_OK` + EditMode 全绿（最新 694/694）；PlayMode 未跑（本批无场景/渲染/UI 改动）。
- 风险：`TitleManager`/`ContentRegistry` 静态注册表生命周期；门禁 provider 与 `DiplomacyManager` 检查语义重复；存档新字段的旧档兼容（封闭开发可声明不兼容）。

### 9.2 批次审计结果（05-batch-audit，2026-11-07）

审计判定：**不通过**（架构合规；三项必修）。报告：`docs/research/character-2026-11-07/05-batch-audit.md`。

| 项 | 状态 | 说明 |
|---|---|---|
| ① `Sociability` 读取恒 0 | **已修** | `GetPersonalityValue` 缺分支（上次 replace 锚点尾随空格未命中）→ 已补；`Set` 分支已在；Compile OK / 694 全绿 |
| ② 头衔"归一"只归 5 字段 | 待修 | `LoadFromContent` 只映射 titleId/category/rank/weight/semantic；`TitleManager` 9 个查询 API 与 `holders` 0 消费者；`RealmTitleEvolution` 0 调用且文化称谓退化 |
| ③ 门禁旁路 | 待修 | 3 参 `DeclareWar` 无门禁；`DeclareGreatHolyWar` 直调 `DiplomacyManager`；法理层与 DM 检查重复 |
| ④ 行为变更未跑 PlayMode | 待跑 | B3 改行为后按 WORKFLOW 需 PlayMode×2 |
| ⑤ 命名空间成环 | 待修 | Definitions↔Systems/Politics 同程序集可编译，但破坏分层意图 |

后续顺序：② 先让头衔模型真正有消费者（RealmTitleEvolution 走 TitleManager、holders 接继承/存档），再 ③ 收口门禁入口，最后 PlayMode 与文档同步。

### 8.13 头衔实例、存档与任命接线（2026-11-08）

| 改动 | 文件 | 说明 |
|---|---|---|
| 运行时实例 | `Simulation/Politics/TitleInstance.cs` | titleId + holders；EndActiveHoldings / GetSeniorHolder / TransferSenior（旧记录保留结束日） |
| 实例注册表 | `Simulation/Politics/TitleInstanceRegistry.cs` | 按 titleKey 取/建、TryGet、Clear、All |
| 政权主头衔 | `Simulation/State/RealmData.cs` | 新增 `primaryTitleKey`（运行时） |
| 继位接线 | `GameWorld.Succession.cs` | `NotifyRulerTransition` → 主头衔 `TransferSenior(newRuler, day)` |
| 任命接线 | `GameWorld.Simulation.cs` | 六类官职：任命 → TransferSenior；死亡/空缺 → EndActiveHoldings |
| 存档 | `GameSaveData.cs` / `GameSaveSystem.cs` | `TitleInstanceSaveData`/`TitleHoldingSaveData` + `realms.primaryTitleKey`；SaveRealms 捕获、LoadRealms 清空重建 |
| 测试 | `TitleInstanceTests`、`TitleInstanceRegistryTests`、`TitleSaveRoundTripTests` | 共持/继位/注册表/JsonUtility 往返 |

- 复验：`COMPILE_OK`；EditMode **699/699 passed**。
- 未升 SaveVersion（纯增量字段，旧档缺省；封闭开发不做迁移）。
- 审计②剩余：`TitleManager` 的 `ByTier/ChildrenOf/GetChain/FilterEligible` 仍 0 调用（等授予/继承/派系玩法）；命名空间环 Definitions↔Systems/Politics 未拆。

### 8.14 头衔继承三模式（2026-11-08）

| 改动 | 文件 | 说明 |
|---|---|---|
| 枚举 | `Core/Enums/TitleSuccessionMode.cs` | None / RealmLaw / OwnLaw |
| 预设目录 | `Content.Definitions/InheritanceLawCatalog.cs` | 6 个预设 id → `InheritanceLaw`，未知回退长子继承 |
| 模板字段 | `TitleDefinition` / `Content.Modding.TitleDef` | `successionMode`、`ownLawPreset` |
| 内容数据 | `StreamingAssets/Base/Title/Titles.json` | 15 条补 `successionMode`（Landed→RealmLaw、NobleRank→OwnLaw+primogeniture、Office→None）与 `ownLawPreset` |
| 映射 | `TitleManager.LoadFromContent` | 透传两字段 |
| 实例 | `Politics/TitleInstance.cs` | `definitionId`（默认空） |
| 存档 | `TitleInstanceSaveData` / `GameSaveSystem` | `definitionId` 捕获/还原 |
| 继位 | `GameWorld.Succession.NotifyRulerTransition` | 自动绑定模板（`TitleCatalog.Highest(Landed)`）；None→EndActiveHoldings；RealmLaw/OwnLaw→TransferSenior |

- 复验：Compile OK；EditMode **703/703**；PlayMode **9/9**、gfx **9/9**。
- 未完成（P2）：`OwnLaw` 目前只表示"法源"，**没有按该法重新选人**——继承人选仍来自政权侧已选出的 `newRulerCharId`；按头衔法重选需要按头衔构建候选池，留 P2。
- 未做：`FamilyShared` 共治（D1 按推荐先不做）。

### 13. 计划整合（2026-11-09）

- 新建 `docs/PLAN_INDEX.md` 作为唯一计划入口（权威计划、研究语料、已取代、矛盾裁决）。
- 移出三份被取代的信仰计划到 `docs/archive/2026-11-09-superseded-plans/`：`FAITH_CULTURE_MODEL_BORROW`（faith 核心）、`FAITH_IDENTITY_MODEL`（挂靠可弱）、`FAITH_SYSTEM_ANALYSIS`（faith 核心）——均与"传统为主运行单位、宗教身份必填、宗教层固定"冲突。
- 矛盾裁决表见 `PLAN_INDEX §4`：信仰主单位=传统；宗教层固定、中间层可选；只借分裂+挂靠；位阶=组合序+绑定；kind 派生不存；删 rank 先替代；疾病=C；机制优先；rite=传统。
- 待核旧系统计划（综合/法律/革新/地理/语言等）列于 `PLAN_INDEX §5`，默认不删。
