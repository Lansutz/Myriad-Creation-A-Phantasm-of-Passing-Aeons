# 05 · B2.0–M3.6 批次只读审计（头衔归一 / 共持 / 门禁接线 / 角色属性与存档）

> 日期：2026-11-07。性质：**只读审计**（唯一写入 = 本文件；未改任何代码/资产/文档）。
> 范围：B2.0 头衔归一、B2.1 删 legacy、B2.2 共持、B3(含 B3.3/B3.4) 门禁接线、M3.1–M3.6 角色属性与存档修复。
> 口径：`docs/design/DECISIONS_M1_M2_M3.md`（§7–§8.12、§9）+ `docs/architecture/ARCHITECTURE_RULES.md`。
> 路径约定：仓库根下 Unity 工程在 `MyriadCreation/`，故代码路径均写 `MyriadCreation/Assets/...`（任务清单省略了该前缀）。行号以本次读取为准。
> 方法限制：与 04-b1-audit 一致，**未运行** Unity 编译与 EditMode/PlayMode（`E:\TEMP\mc-*.ps1`）；记录中的 `COMPILE_OK` / 694 全绿按 Lead 记录采信，未独立复验。

## 0. 结论

| 维度 | 判定 | 一句话 |
|---|---|---|
| 架构规则（落位/命名空间/程序集方向/纯数据/一文件一主类型） | ✅ 通过 | 程序集方向未破；新文件零孤儿 `.meta`；纯数据层无 Unity |
| 死代码 / 重复模型（§2） | ❌ 不通过 | 头衔"归一"只归了 5 个字段；holders、9 个查询 API、整个命名链 0 消费者 |
| 正确性风险（§3） | ❌ 不通过 | Sociability 读取恒 0（存档丢失）；文化称谓退化；门禁有旁路 |
| 文档一致性（§4） | ⚠️ 需修 | §7.1/§8.10/§8.11/§9 与实现互相对不上；`CHARACTER_PLAN_SEAM` 多项过时 |
| 测试缺口（§5） | ⚠️ 需修 | LoadFromContent、4 参 DeclareWar、GrantSignatureTraits、存档新字段均无测试 |

**总判：不通过（架构合规，但"唯一真源"与"门禁唯一入口"两个核心承诺未兑现，且有 1 处存档级数据丢失）。**

## 1. 批次落盘清单（mtime 重建）

| 文件 | mtime | 与记录一致？ |
|---|---|---|
| `Core/MyriadCreation.Core.Contracts/ActionGateContracts.cs`、`Core/MyriadCreation.Core.Enums/TitleDerivedCategory.cs` | 11-07 02:08 | ✅ §8.5/§8.1 |
| `Simulation/MyriadCreation.Simulation.Modding/ContentRegistry.cs` | 02:11 | ✅ §8.7（新增 `:111 LoadFromContent()`） |
| `Content/MyriadCreation.Content.Modding/TitleDef.cs`、`Simulation.Definitions/TitleCatalog.cs`、`Systems/RealmTitleEvolution.cs` | 02:15 | ✅ §8.8 |
| `Systems/TitleManager.cs`、`Tests/EditMode/TitleCatalogTests.cs`、`TitleLanguageDataTests.cs` | 02:16 | ✅ §8.7/§8.8 |
| `Simulation.Definitions/TitleDefinition.cs`、`Politics/TitleHolderRole.cs`、`Politics/TitleHolding.cs`、`Tests/EditMode/TitleHoldingTests.cs` | 02:19 | ✅ §8.9 |
| `ActionGate/ActionIds.cs`、`RealmAuthorityResolver.cs`、`ActionGate.cs`、`GameWorld.Core.cs`、`Diplomacy/DeclareWarCommand*.cs`、`AI/AIIntent*.cs` | 02:28–03:28 | ✅ §8.10 + §9(B3.3) |
| `ActionGate/DiplomacyLegalityProvider.cs`、`RealmTreasuryProvider.cs`、`Tests/EditMode/ActionGateTests.cs` | 03:39 | ✅ §9(B3.4) |
| `Content.Definitions/SpecialtyDerivation.cs`、`Tests/EditMode/SpecialtyDerivationTests.cs` | 04:31 | ✅ §9(M3.2) |
| `Actors/MapActor.cs`、`LawHost/DecreePropagationSystem.cs`、`Tests/EditMode/EffectiveStatsTests.cs` | 11-07 10:10–10:11 | ✅ §9(M3.3/M3.6) |
| `Infrastructure.Save/CharacterSaveData.cs`、`State/CharacterData.cs`、`WorldState/GameWorld.Succession.cs` | 10:40 | ✅ §9(M3.4) |
| `Systems/CharacterManager.cs`、`FactionManager.cs`、`ResearchPlanSystem.cs`、`WorldState/GameWorld.Simulation.cs` | 19:35 | ⚠️ 记录仅写 M3.5/M3.6，未列 `GameWorld.Simulation.cs` 的门禁放大改动 |

- 全 Assets 孤儿 `.meta` 扫描：**0 个**（`TitleKinds.cs`/`SemanticTags.cs` 已连同 `.meta` 删除干净）。新增 16 个 `.cs.meta` 均有对应文件。

## 2. 架构规则核对（ARCHITECTURE_RULES）

| # | 规则 | 证据 | 判定 |
|---|---|---|---|
| 1 | 程序集方向 L0–L7（`ARCHITECTURE_RULES.md:11-34`） | `Simulation/sim.asmdef` 仍 refs=Core/World/Content/Laws/Faith/Culture；新增 ActionGate/Politics/Definitions 均在 sim 程序集内，未加跨层引用 | ✅ |
| 2 | Content 只引 Core（`:27`） | `Content.Modding/TitleDef.cs:1-2` 仅 `System` + `Core.Enums`；`Content.Definitions/SpecialtyDerivation.cs:1` 仅 `System` | ✅ |
| 3 | 纯数据层无 Unity（`:74`） | ActionGate 契约、TitleDefinition、TitleCatalog、TitleManager、SpecialtyDerivation 的 `using` 均无 UnityEngine | ✅ |
| 4 | namespace = 目录（`DECISIONS:269`） | `Simulation.ActionGate`/`Simulation.Politics`/`Simulation.Definitions`/`Core.Enums` 全部对齐目录 | ✅ |
| 5 | 不新增程序集/目录层级（`DECISIONS:257,288`） | 无新 asmdef；新目录均为既有程序集子目录 | ✅ |
| 6 | 一文件一主类型（`DECISIONS:269`） | `ActionGate.cs` 6 类型、`ActionGateContracts.cs` 6 类型——沿 B1 已认可的契约聚合惯例 | ✅（可接受） |
| 7 | 不新建平行系统（`:60`「禁止」/`DECISIONS:263`） | 未新建 `WarDeclarationSystem`；门禁接在 `DeclareWarCommandHandler` 与 GameWorld | ✅ |
| 8 | 不新增 `Manager` 承载纯数据（`DECISIONS:271`） | **`TitleManager` 被提升为唯一真源**（`TitleManager.cs:11-135`），与 §7.4 命名规则正面冲突 | ⚠️ 违反（见 §4-D2） |
| 9 | 定义层与运行时状态分家（`ARCHITECTURE_RULES.md`§3 / `DECISIONS:22-45`） | `TitleDefinition.holders`（`TitleDefinition.cs:44`）把运行时持有塞进"定义"对象；M1 §1.2 要求 `TitleDef`(模板)/`TitleInstance`(运行时) 分开 | ⚠️ 偏离 M1 设计 |
| 10 | 不得反向依赖（Definitions→Systems/Politics） | `TitleCatalog.cs:4,13`（Definitions 目录调 Systems）；`TitleDefinition.cs:4,44`（Definitions 依赖 Politics）；`TitleManager.cs:3`（Systems 依赖 Definitions）→ **命名空间成环**（同程序集可编译，但破坏分层意图） | ⚠️ 需修 |

## 3. 死代码与重复（审计项 2）

### 3.1 头衔：仍是"两套查询 + 一套半接线"

| 事实 | 证据 | 影响 |
|---|---|---|
| `LoadFromContent` 只映射 5 个字段 | `TitleManager.cs:32-39`（titleId/category/rank/weight/semantic） | `TitleDefinition` 其余 30+ 字段（`cultureId:24`、`modules:38`、`appointmentOrder:40`、`eligibility:42`、`seats:50`、`salary:56`、`inheritable:46`、`onGetTitle:76` …）在运行时**永远为默认值**，无法被内容填充 |
| `TitleManager` 9 个查询 API 全仓 0 引用 | `TitleManager.cs:75-135`（ByTier/ChildrenOf/Roots/GetChain/CountByTier/FilterEligible/ResolveAppellation；仅 `Get/All/Register/ByCategory` 被用） | `FilterEligible` 因 eligibility 恒默认，实际等于"全部通过"；`ResolveAppellation` 恒返回 null（appellationId 恒空） |
| `TitleCatalog` 与 `TitleManager` 两套查询语义 | `TitleManager.cs:70-73`（IEnumerable、不排序）vs `TitleCatalog.cs:16-41`（`List` 副本、按 rank 降序、独有 `Highest`） | 同一问题两种答案/两种复杂度；调用方无从判断该用哪套 |
| `RealmTitleEvolution` 全静态 0 调用 | `RealmTitleEvolution.cs:16-53`（`IsTerritorial`/`GetRealmDisplayName`/`GetRulerTitleDisplay`）全仓无外部调用 | **整条命名链（15 条 Titles.json → TitleManager → TitleCatalog → RealmTitleEvolution）没有玩家可见消费者**，B2.0「接线」只接到一个死出口 |
| 文化称谓被删掉 | `TitleCatalog.cs:30-41` 的 `Highest(category)` 无 culture 参数；`TitleDefinition.cultureId:24` 无任何生产者；`RealmTitleEvolution.cs:42-51` 用 titleId 硬编码中文且**没有 `title_basileus` 分支** | 最高 Landed = `title_basileus`(3.1) → 命中 `_ => "王"`：**所有政权统治者显示"王"**，皇帝/可汗/大汗永不显示；`semantic` 字段虽加载但全仓 0 读取 |
| B2.2 `holders` 是纯死数据 | `TitleDefinition.cs:44` 全仓唯一出现；`TitleHolding.cs:10-36` 仅被 4 条测试构造；`TitleHoldingTests.cs:14-45` 只测数据结构 | M1 验收「共治头衔可存两个 holder / 机构可持有」只到"能 new 出来"，无生产写入、无消费读取 |
| `TitleHolderRole` 与既有 `TitleHoldingMode` 未收口 | `Politics/TitleHolderRole.cs:4-11`（Sole/Senior/CoRuler/Elector/Regent）vs `Content.Definitions/TitleHoldingMode.cs:3-9`（Single/ConcurrentHolding/MultiPeopleClaim/PersonalUnion），后者同样 0 引用 | 两套"持有模式"枚举语义重叠（Sole↔Single、CoRuler↔Concurrent），B1 审计已提醒"别再建第三套 role" |
| 陈旧注释指向已删模型 | `TitleDerivedCategory.cs:5-6` 仍写"与 `TitleKinds` 字符串常量不同""B2 需要 kind→枚举映射、旧字段标 legacy" | B2.1 已删该模型，注释未同步，属悬空引用 |

### 3.2 Content DTO 与运行时类型字段重复

| 层 | 类型 | 字段 | 判定 |
|---|---|---|---|
| 内容（JSON 解析 DTO） | `Content.Modding/TitleDef.cs:7-18` | titleId/category/rank/weight/semantic | ✅ 该 5 字段与运行时逐字重复，但方向是**单向映射**（Content→Simulation），符合 §8.7「保留为 JSON DTO」 |
| 运行时 | `Simulation.Definitions/TitleDefinition.cs:12-21` | 同上 5 字段 + 30 个治理字段 | ⚠️ 5 字段有生产者；治理字段无生产者（见 3.1） |

- `Titles.json` schema 已完全换为 `category` 整数，15 条全覆盖（0=6、1=3、3=6，无 2/4），**未发现残留 `kind`**；`AdminDivisionSystem.cs:145-165` 引用的 6 个 titleId 在 JSON 中全部存在，无悬空 ID。✅

### 3.3 `GetEffectiveStats` 仍用原始字段的剩余点

| 位置 | 用途 | 判定 |
|---|---|---|
| `WorldState/CharacterDetailQueryAdapter.cs:76-81` | 角色详情 UI 六维直接用 `data.prowess/social/...` | ❌ UI 看不到特质/精神修正（§6 验收"角色页显示三层属性"未达） |
| `WorldState/GameWorld.Succession.cs:63` | 继任"强改革者"评分用原始六维 | ❌ 应走有效值 |
| `Systems/ResearchPlanExecutor.cs:29` | 研究速度用原始 `character.scholarship` | ❌ M3.6 声称"研究改有效值"，只改了 `ResearchPlanSystem.cs:269,277`，执行器漏改 |
| `Systems/CharacterManager.cs:641` | 年迈失智判定用原始 `scholarship` | ⚠️ 低影响 |
| `State/MentalHealthSystem.cs:106-125` | `ApplyDisorderMods` 与 `GetEffectiveStats` 的精神病修正逐行重复，且全仓 0 调用 | ❌ 重复死代码，应与 `GetEffectiveStats` 合流或删除 |

- 已正确接入有效值：`Actors/MapActor.cs:341`、`LawHost/DecreePropagationSystem.cs:203`、`Systems/FactionManager.cs:235,257`、`Systems/ResearchPlanSystem.cs:269,277`、`WorldState/GameWorld.Simulation.cs:741`。✅

## 4. 正确性风险（审计项 3）

| ID | 级别 | 问题 | 证据 | 说明 |
|---|---|---|---|---|
| A1 | 🔴 高 | **Sociability 只有 setter 没有 getter** | `CharacterData.cs:397-407` 的 switch **缺 `Sociability` 分支**（落到 `_ => 0f`）；`:413` 起 `SetPersonalityValue` 有 `:438-440` 分支 | 后果链：①`GrantSignatureTraits` 的 gregarious/shy 轴恒读到 0 → `CharacterManager.cs:256-266` 8 轴中该轴**永不下发**（`GrantTraitByAxis` 阈值 40）；②`CharacterSaveData.cs:256-258` 以 `GetPersonalityValue` 捕获 → **personality[8] 里 sociability 恒写 0，存盘丢失**；③`CharacterData.DailyTick` 用 `AddPersonality`→`Get` 恒 0 → 每日漂移被重置为 ±0.02；④`InitializePersonality` 用 `Set` 写入的初值被后续读取路径全部视为 0。M3.4/M3.5 声称已修 Sociability，实际只补了字段与 setter |
| A2 | 🔴 高 | **门禁有旁路** | `GameWorld.Simulation.cs:1029-1050` 的 3 参 `DeclareWar` 仍是 public 且无门禁（仅被 4 参内部调用）；`:1084-1110 DeclareGreatHolyWar` 直接调 `_diplomacyManager.DeclareWar`（`:1108`）**完全绕过 ActionGate** | 任何人新增一个 3 参调用/大圣战即绕开 M2 门禁；`DeclareWarWithJustification`（`DiplomacyManager.cs:388-398`）也是无门禁的第三套宣战入口 |
| A3 | 🟡 中 | 4 参 `DeclareWar` 顺序/递归已核对 | `:1052-1065`：先 gate，再调 3 参基础实现——**不是递归**，顺序正确；但每次调用都 new 一套 gate+providers（`:1058`），无缓存 | 可接受，建议 provider 复用 |
| A4 | 🟡 中 | **法理层与 `DiplomacyManager.DeclareWar` 逐条重复** | `DiplomacyLegalityProvider.cs:24-45`（relation null / isAtWar / truceUntilDay）与 `DiplomacyManager.cs:345-356` 三条判断完全相同 | 当前一致（不冲突），但两处维护必然漂移；且 provider 拒绝时**不写外交事件**（DM 会 `AddEventTo`，`:354`），玩家/日志看不到"停战期拒绝"原因 |
| A5 | 🟡 中 | 国库 200 门槛与 AI 条件不完全一致 | `GameWorld.Simulation.cs:1058` 传 `200f`；`RealmTreasuryProvider.cs:30` 判定 `< 200` 即拒（即 **200 允许**）；`AIController.cs:347` 是 `treasury > 200f`（**200 不允许**） | 边界差 1 个数值；AI 同时受两层检查，属冗余不会放宽，但语义不一致 |
| A6 | ✅ | `GrantSignatureTraits` 调用时机正确 | `CharacterManager.cs:840` 先 `_characters[id]=...`，`:841` 再 `GrantSignatureTraits` | 满足"加入 `_characters` 后调用"；且 `CharacterData.AddTrait`（`:652-658`）自身按 traitId 去重 |
| A7 | 🟡 中 | `GrantSignatureTraits` 绕过新的 `AddTrait` 生产端 | `CharacterManager.cs:280` 直接 `c.AddTrait(trait)`，不走 `:285 AddTrait(characterId,traitId)`（后者含 GetCharacter 校验） | 有 `HasTrait` 级去重，无功能错误；但与"统一生产端"表述不符 |
| A8 | 🟡 中 | 存档新字段：捕获/还原**真有** | `CharacterSaveData.cs:81 accessionDay`、`:83 achievements`（struct，`AchievementRecord.cs:6 [Serializable]` 纯值字段，JsonUtility 可序列化）、`:85 personalTenets`、`:87 bodyMarks`；捕获 `:244-251`，还原 `:524-533`；`personality` 数组 `:177-178`（8 维）捕获 `:256-258`/还原 `:526-534` | 结构完整，但 **A1 使 sociability 入档恒 0**；且 `basicAbilities`/`genome` 同段已入档 |
| A9 | 🟡 中 | 旧档兼容未声明 + SaveVersion 未升 | `Core.Constants/GameConstants.cs:13 SaveVersion = 17`（mtime 11-04，本批未动）；`CharacterSaveData.cs:14-26` 类注释仍写"personalTenets/bodyMarks/成就计数**暂未覆盖**"（与 `:244-251` 代码矛盾）；`:18,177` 注释写"人格**七维**"，实际 8 维 | 封闭开发可不兼容，但 §9.1 只写"可声明"，仓内**没有声明文件**；同一 v17 存档新旧字段无法区分 |
| A10 | ✅ | `reignYears` 已修正且唯一 | `GameWorld.Succession.cs:273-277`：`(deathDay - accessionDay)/365`；全仓无第二处赋 `reignYears`（`EpithetSystem` 只读） | 但 `accessionDay` 仅 `:95-97,133-135` 两处置位；世界初始君主若未经这两条路径则 `accessionDay=-1` → `reignYears` 恒 0（历史遗留，未在本批修） |
| A11 | 🟡 中 | 专精公式"固定训练 0.6"的影响面 | `CharacterManager.cs:816-822`：`trainingBaseline = 0.6f` 对全部角色恒定；`SpecialtyDerivation.cs:33-39` 公式 `100×√(basic×training)`；`talentBonus` 参数全仓 0 调用 | 训练从"变量"退化为常量：①"无训练→近 0"只在单测存在，运行时永不发生；②专精区间被压到约 47–61（basic≈50±12），**方差远小于旧 `Random(20,80)`**；③天赋/缺陷走 `ApplyDef`（`:908-930`）在派生**之后**直接加减，等于公式外修正，`talentBonus` 成死参数；④设计里的"训练成长速率/上限"未实现 |
| A12 | 🟡 中 | 基本能力生成只覆盖 6/12 项 | `CharacterManager.cs:808-814`：`BasicAbilities.Random(50,12)` + 仅 strength/coordination/endurance（martialOffset）与 memory/logic/inspiration（intelligenceOffset） | `eloquence/insight/charisma/observation/intuition/reaction` 纯随机；`DnaExpression`（`State/DnaExpression.cs:10-12`）也只有两个能力偏移——M3.1"DNA/种族→12 项"只兑现一半 |
| A13 | 🟡 中 | 婚姻/纽带"生产端"无调用者 | `CharacterManager.cs:199 Marry` 全仓 0 调用（仅 `:236-243 LinkSpouses` 内部用）；`CreateBond:511-524` 生产端只被测试调用 | M3.5 记为"婚姻关系/纽带生产端"，实际是"公开了 API、无触发源"；且 `CreateBond` 无同对角色去重、`establishedDay` 恒 0 |

## 5. 文档一致性（审计项 4）

| ID | 文档条目 | 实际代码 | 判定 |
|---|---|---|---|
| D1 | `DECISIONS:239-249`（§7.1 落位表）要求新建 `TitleInstance/TitleRegistry/TitleKindClassifier`、`TitleRight/TitleModule/TitleBinding` | 全仓 0 处这些类型；实际是 `TitleManager` + `TitleDefinition.holders` | ❌ 表格未随 §8.3/§8.7/§8.9 更新 |
| D2 | `DECISIONS:271`「不新增 `Manager` 类承载纯数据」 | `TitleManager` 被确认为"运行时唯一头衔真源"（§8.7 末条） | ❌ 自相矛盾 |
| D3 | `DECISIONS:421-432`（§8.10）称 4 参 `DeclareWar`「先过 ActionGate（能力∧职权）」 | 实为四层（`GameWorld.Simulation.cs:1058` 同时注入 Legality/Resource） | ⚠️ §8.10 未同步 §9-B3.3/B3.4 |
| D4 | `DECISIONS:434-439`（§8.11 待办 1/3：AI 切 4 参、M3 P0） | 均已完成（`AIIntentExecutor.cs:39`、`AIController.cs:349`、M3 各批） | ⚠️ 待办未勾销，读者会重复劳动 |
| D5 | `DECISIONS:280`（§7.5）B3「改行为 → 编译+EditMode+**PlayMode×2**+审计」 | `:486` §9.1 自认"PlayMode 未跑" | ❌ 门禁未闭合（B3 明确写了"是否改行为=是"） |
| D6 | `DECISIONS:139-142`（§2.7）「先接三个动作：宣战、立法/法令、任命」 | 仅宣战接了门禁；`ProposalAuthority`（`Laws/ProposalAuthority.cs:6`）仍是法律域唯一入口，`LawAndDecreeSystem.cs:508-534` 未接 ActionGate，`AuthoritySource` 在 ActionGate 之外 0 引用 | ⚠️ 与 §9.1「已收口 M2 门禁」口径不符 |
| D7 | `DECISIONS:409-419`（§8.9）说 B2.2 完成 `holders` | 未提"holders 无生产/消费"，也未提其与 §1.2「TitleInstance 持 holders」的设计冲突 | ⚠️ 记录不完整 |
| D8 | `DECISIONS:482-487`（§9.1 风险）已提到静态注册表、provider 与 DM 检查重复、旧档兼容 | 与实际一致（本审计 A1/A4/A9 进一步定量） | ✅ 风险表可信，但低估了 A1 |
| D9 | `CHARACTER_PLAN_SEAM.md` | **过时条目**：§0 结论 1（能力恒 50/专精随机）→ 已改；§2 P0 行「`BasicAbilities.cs:55`、`CharacterManager.cs:747-761`、`CharacterData.cs:436-438`、`CharacterSaveData` 漏字段」→ 行号与结论均已失效；§7「立即修复清单」1/2/3/5/6 已完成、4（疾病双轨）未做、7 部分；§6 表「M2：9 维资格**补能力/健康/教育维**」与 `DECISIONS:113-117`（**不加能力维**）正面冲突；§7 第 4 条疾病"二选一"仍未落地（`DECISIONS:485` 也承认） | ⚠️ 需按 §9.1 待办同步 |

## 6. 测试缺口（审计项 5）

| 新行为 | 现有测试 | 缺口 |
|---|---|---|
| `TitleManager.LoadFromContent` | 无专测；仅 `TitleCatalogTests.cs:15-57` 间接依赖（Get/Highest/ByCategory） | 未断言"JSON→TitleManager 的 5 字段映射"、未覆盖 `Clear()` 幂等、未覆盖空 titleId 跳过 |
| TitleManager 静态生命周期 | `TitleCatalogTests.cs:66-76` 运行时 `Register` 后**无 TearDown/清理** | 测试间污染：该测试写入 `title_test_high_king(rank 3.5)` 后，同程序集后续 `Highest(Landed)` 会返回测试数据；任何 `ContentRegistry.Reset()` 又会静默清空它 → 用例顺序敏感（§8.7 已列为风险但未加护栏） |
| 4 参 `DeclareWar` 端到端 | `ActionGateTests.cs` 只测 gate 单件；`RealmAuthorityResolverTests.cs` 只测 resolver 委托 | **无一条测试从 `GameWorld`/命令处理器驱动**：越权角色被拒、元首通过、摄政通过、provider 接线是否正确，全部未验证（`DECISIONS:230` 验收 3 分支未落测） |
| `DiplomacyLegalityProvider` / `RealmTreasuryProvider` | `ActionGateTests.cs:27-37` 只测桩；`:118-133` 测 treasury 单件 | 与真实 `DiplomacyManager` 的行为等价性无测试（重复实现无回归保护） |
| `GrantSignatureTraits` / `AddTrait` | 无 | 性格轴→特质映射、8 轴阈值、去重均无测试（正是 A1 能漏掉的原因） |
| `Marry`/`CreateBond` 生产端 | `CharacterPersistenceTests.cs:522-575` 只测存档往返 | 婚姻前置条件、关系/纽带写入无测试 |
| 存档新字段往返 | `SuccessionEndToEndTests.cs:472-474` 断言 `accessionDay/achievements` 在继任内存态；**无一条 round-trip 测试** | `accessionDay/achievements/personalTenets/bodyMarks/personality[8]` 存→读往返全缺；补一条 personality 往返即可直接暴露 A1 |
| 专精派生 | `SpecialtyDerivationTests.cs:35-68` 纯函数 4 条 | 未测 `CreateCharacter` 的 `trainingBaseline=0.6` 与 `ApplyDef` 后置修正的实际分布（A11） |
| `GetEffectiveStats` | `EffectiveStatsTests.cs:10-26` 2 条 | 未测剩余原始字段消费者（§3.3 表） |

## 7. 总评

**不通过。** 架构层（落位/命名空间/程序集/纯数据/孤儿 meta）是本批最干净的部分，全部通过；但三个"承诺"没兑现：①"TitleManager 唯一真源"只归了 5 个字段，其余仍是死字段、查询 API 0 引用、命名链 0 消费者，且文化称谓功能实际退化；②B2.2 的共持完全是死数据；③"门禁唯一入口"存在 3 条旁路，B3 的 PlayMode 门禁未跑。叠加 A1 的 Sociability 读取断链（存档级数据丢失），本批不能按 §9 宣称"已收口"。

### 必须修（前 3 条）

1. **修 Sociability 读取断链**：`CharacterData.cs:397-407` 补 `case PersonalityDimension.Sociability => sociability`（或在 `GetPersonalityValue` 统一走 `PersonalityDimensions.All` 索引）；随后补一条 save round-trip 断言（`personality[1]` 非 0）。同时清理 `CharacterSaveData.cs:18,177` 的"七维"注释。
2. **把"归一"做到底或明确降级**：二选一——(a) 让内容真的填满 `TitleDefinition`（`LoadFromContent` 映射 cultureId/modules/seats/eligibility 等），并把 `TitleCatalog` 合并进 `TitleManager`（删除重复查询语义，消除 Definitions↔Systems 环）；(b) 或声明本轮只做"命名目录"，删掉 `TitleManager.cs:75-135` 的死 API 与 `TitleDefinition` 的不可达治理字段、恢复 `Highest` 的 culture 参数与 `RealmTitleEvolution` 的真实消费者。无论哪条，都须修 `TitleDerivedCategory.cs:5-6` 的过期注释。
3. **关门禁旁路并补验收测试**：`GameWorld.Simulation.cs:1029` 3 参 `DeclareWar` 降为 private（或强制带 actor）；`DeclareGreatHolyWar:1108` 过同一 gate；把 `DiplomacyLegalityProvider` 与 `DiplomacyManager.DeclareWar` 的重复判断收敛到一处（provider 调 DM 的只读 `CanDeclareWar`）；补"GameWorld 级"三分支测试，并跑 PlayMode×2（§7.5 门禁）。

### 可延后（3 条）

1. **存档版本与旧档声明**：`SaveVersion` 仍 17、字段语义已变；补一页"封闭开发不同档 v≤17"声明 + 待 B4 统一升版（`ARCHITECTURE_RULES.md`§5 要求批准，封闭开发可先声明）。
2. **专精/训练建模**：`trainingBaseline=0.6` 令训练成常量、专精方差压到 47–61；等教育/实践系统落地后改为真实训练累计，届时删除 `talentBonus` 死参数或把它接上天赋修正（A11/A12）。
3. **文档同步**：`DECISIONS` §7.1/§8.10/§8.11/§9 与 `CHARACTER_PLAN_SEAM` 按 §5 表修正；并把"仅宣战接入门禁、立法/任命待 B4"写进 §9.1，避免"已收口 M2"被误读。

## 8. 方法与限制

1. 只读：未编译、未跑 EditMode/PlayMode、未改任何文件（唯一写入 = 本文件）；`COMPILE_OK`/694 全绿按 Lead 记录采信。
2. "新增/改动"以 mtime 窗口（11-07 02:08–19:35）+ 全仓 grep 双证据重建；仓库整树未跟踪，git 不能区分新增与既有。
3. "死代码"判定为符号级 grep（`Assets/Scripts`+`Assets/UI`+`Assets/Tests`，含 `--include=*.cs`），未含反射/字符串拼装；仓内无对上述类型的反射调用。
4. 未审计：SaveVersion 迁移路径实际执行、PlayMode 场景接线、`B4` 头衔入档（未开工）。
