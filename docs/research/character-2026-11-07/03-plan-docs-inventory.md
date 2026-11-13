# 综合大规划 × 角色系统：现有设计文档盘点（只读）

> 2026-11-07。性质：只读勘察，不改代码/文档（本文件除外）。
> 范围：`docs/design/`、`docs/architecture/` 必读清单 18 份 + 4 份增补（`UNIFIED_MASTER_PLAN`、`README_MAIN_DETAILED_REVIEW_01`、`GEOLOGICAL_CLIMATE_DYNAMICS_PLAN`、`docs/角色系统整体设计.docx`）。
> 方法：全文/定段阅读 + 关键句 `grep -n` 复核行号 + 少量代码存在性核对（`ProposalAuthority`、`TitleHolding`、`ActionResolver`）。状态判定以文档自述与关键文件存在性为准，未逐行读实现。

## 0. 一页结论

1. **大规划已有一份执行口径，但它是 2026-11-03 的，之后 11-05/06 的设计层没有再回收**：`UNIFIED_MASTER_PLAN.md:3` 自称"当前唯一执行口径"，而它的 WBS（W1–W20）**没有任何角色工作包**，且全文 **0 次**出现"角色"（`grep -c` 复核）；"角色面板契约补齐中"只出现在 `SYSTEM_OWNERSHIP.md` 的遗留项。
2. **大规划的本体框架里没有 Character，也没有 Family**：`TOTAL_SYSTEM_SYNTHESIS.md:25-34` 的核心本体表只有 Polity / Title / SettlementControlNetwork / Contract / Organization / LegalOrder / Calendar / Faith*；角色只以关系端点出现（`:48` "P↔Character（官职）、Title↔Character（持有）"）。
3. **角色系统唯一的全量设计是 `docs/角色系统整体设计.docx`（2026-09-24）**，它不在任何 md 清单里，且全文 0 次"技能""专精""成长""门禁""权限""行动"——与 README 口径（`AUDIT_11:78` "基本能力→专精→技能树"）脱节。
4. 因此"角色并入综合大规划"的实际缺口不是缺想法，而是**缺一个把角色放进本体—权威—门禁链的接缝**；先解决三个矛盾即可动工（§5.2）。

---

## 1. 文档清单（一句话：覆盖 / 结论 / 状态）

| # | 文档 | 覆盖系统 | 核心结论（一句话） | 状态 |
|---|---|---|---|---|
| 1 | `docs/architecture/UNIFIED_MASTER_PLAN.md` | 全项目执行口径、程序集、革新、城防、法律、头衔 | DOF+WBS 定 Phase 0–5，革新/城防已落地；但 WBS 无角色包，形态标签用 `PolityForm`（`:68`） | **已定稿（执行口径）**，已被 11-05/06 设计层部分超越 |
| 2 | `docs/design/TOTAL_SYSTEM_SYNTHESIS.md` | 政治/头衔/关系/法律/信仰/历法 | 结构可选、组件可组合、权威可挂载，不设普适硬层级（`:10-17`） | 已定稿（框架层），未落代码 |
| 3 | `docs/design/UNIFIED_DESIGN_V2.md` | 认同/语言/文化/头衔/物产/革新 | 术语严格定义 + 革新精确化（185→350–500）+ 头衔与 `IPolityRef` 分离 | 部分落地（W17 I0 已实现，其余未做） |
| 4 | `docs/design/WORLD_SANDBOX_AND_SYSTEMS_PLAN.md` | 沙盒、物种 DNA、聚落影响、法律组/法令 | 设计稿 + Round 1–46 滚动实施日志；"组只是组、每组一法、法令有前置" | 混合（日志部分已实施；Round 46 设计完成未改代码 `:1399`） |
| 5 | `docs/architecture/ARCHITECTURE_2_0_DOMAIN_MODEL.md` | 全领域语义分层 | Entity/State/Query/Action/Effect 分工，"Character 不应拥有 DailyTick"（`:69`） | 研究设计稿，未实施 |
| 6 | `docs/architecture/ARCHITECTURE_2_0_DOMAIN_AUDIT_11.md` | README 与代码对照 | 角色系统"远超过简单 CK3 clone…必须独立领域审计"（`:78`,`:94`） | 只读审计（缺口公告） |
| 7 | `docs/architecture/ARCHITECTURE_2_0_LAW_OFFICE_NAME_REGION_MODEL.md` | 法律域族组、头衔、继承三轨、键名、法理区域 | 5 域/10 族/30 组、官职统一归口头衔、硬约束与权重分离 | 研究设计稿，代码未改（`:3`） |
| 8 | `docs/design/LEGAL_SYSTEM_UNIFICATION.md` | 多层法律秩序、特许、冲突裁决 | LegalOrder 树 + 特许是规范而非提案权；"不是每个领主都重新写一整套法律"（`:50`） | 部分落地（`LegalNormKind/Scope/precedence` 已加 `:61-69`） |
| 9 | `docs/design/LEGAL_SYSTEM_FULL_AUDIT_AND_PLAN.md` | 法律三层、根本化、政体结构 | 三层规范 + 根本化阶段；后段自我修订"政体结构 ≠ 根本法"（`:425`） | 部分落地（`LegalNormStage` 已加 `:412`），文档内部前后修订 |
| 10 | `docs/design/LAW_DECREE_AUDIT_AND_REDESIGN.md` | 法律审计、术语精确化、革新接线 | 公民权/民主/法源必须拆；实测 9 组 32 法、`requiredInnovationIds` 全空（`:21`,`:184`） | 草案（基线过时：后续文档已 49–50 条） |
| 11 | `docs/design/SEVEN_DIMENSIONS_LAW_STUDY.md` | 七维政体法律 | 七维"太现代、太扁平、互相重叠"，应作 `ConstitutionalOrder` 主题域（`:173-183`） | 草案（结论已并入 #9 §13） |
| 12 | `docs/design/MILITARY_LAW_TAXONOMY_AUDIT.md` | 军事法域族组、边境管制 | 三组不够、常备军不是兵源、边境≠关隘；实测 50 条法 31 无革新（`:224`） | 草案 + 数据审计 |
| 13 | `docs/design/POLITY_TITLE_CONTRACT_MODEL.md` | 政治实体/头衔/土地/契约/组织 | 用"头衔—持有者—土地—契约—法律秩序"的图替代 `ownerRealmId`；不设 `PolityKind`（`:241`） | 草案 |
| 14 | `docs/design/SUPREME_AUTHORITY_MODEL.md` | 最高权力归属 | 用多头政治/多中心主权替代"多君主"，四轴替代 Monarch/Republic | 部分落地（删 `Divine` 已执行 `:290-297`） |
| 15 | `docs/design/RELATION_TAXONOMY_AND_TITLE.md` | 关系分类、头衔构成 | 六类端点关系 + 头衔三段（指代/称谓/绑定）；君合是关系不是合并 | 部分落地（删"国种"已执行 `:221-229`） |
| 16 | `docs/design/FAITH_DOCTRINE_CLASSIFICATION.md` | 信仰内容结构（CK3 对照） | 两轴归类、2+3 槽位、组织 5 档替代 reformed、VeneratedDead/Patronage | **与代码脱节**：Faith 侧 `Create*`/`GetDivergence`/`DoctrinePool` 无调用者（`:220`） |
| 17 | `docs/design/CIV_COGNITION_TITLE_CURRENCY.md` | 命名/头衔/官员因子/革新小绑定/货币/灾害 | `TitleInstance`+`IPolityRef`+`OfficeFactor`+`UnlockRule` 是四个缺口 | 草案（C1–C9 未做 `:252-264`） |
| 18 | `docs/design/HONORIFIC_NICKNAME_SYSTEMS.md` | 头衔/尊号/绰号 | 三套独立系统；现有 `EpithetSystem` 把绰号与谥号混在一起，要拆 | 草案 |
| 19 | `docs/design/VOCABULARY_TRACEABILITY_MATRIX.md` | 术语—计划对照 | 逐词映射落点；五层单位不硬套、`school` 不普适 | 草案 |
| 20 | `docs/README_MAIN_DETAILED_REVIEW_01.md` | README 逐段复核 | "角色是多个系统的连接节点"（`:61`）、个人革新是角色行为（`:62`） | 审计记录（判断口径） |
| 21 | `docs/design/GEOLOGICAL_CLIMATE_DYNAMICS_PLAN.md` | 全项目分模块完成度 | 角色/家族 78%，"属性、特质、关系、家族、继承、DNA"已有（`:45`） | 审计结论（唯一量化角色完成度的文档） |
| 22 | `docs/角色系统整体设计.docx` | 角色域全量（属性/人格/特质/心理/DNA/关系/家族/称号） | 四层分离（七维/特质/原型/称号）、六维属性、`GetEffectiveStats` 唯一出口、家族树、绰号谥号一体 | 现状级设计（2026-09-24），**无技能/专精/教育/成长/门禁** |

> 增补说明：#1 不在委托清单内，但它是全仓唯一的"执行口径"文档，不读会误判"大规划"是哪一份；#22 不在清单内，但它是角色域唯一全量设计，不读会把"有设计"误判成"空白"。

---

## 2. 已定的跨系统约定（接口/原则 + 原文短句）

| # | 约定 | 原文短句 | 出处 |
|---|---|---|---|
| C1 | **权威 = 头衔 + 能力包**，不是层级字段 | "领袖是**头衔持有者 + 能力包**，不是层级自带字段" | `docs/design/TOTAL_SYSTEM_SYNTHESIS.md:142` |
| C2 | 无普适硬层级，结构可选 | "不设普适硬层级"；"不采用 CK3 的硬四层作为普适模型" | `TOTAL_SYSTEM_SYNTHESIS.md:10`,`:239` |
| C3 | **官职统一归口头衔管理器**，不单列官职系统 | "官职统一归口头衔管理器，不单列官职系统" | `ARCHITECTURE_2_0_LAW_OFFICE_NAME_REGION_MODEL.md:1025`（另见 `:375`） |
| C4 | 头衔 ≠ 地理，可绑定政治实体 | "头衔 ≠ 地理：头衔可以没有领地…地理只是**绑定字段之一**" | `docs/design/CIV_COGNITION_TITLE_CURRENCY.md:117`；`UNIFIED_DESIGN_V2.md:111` |
| C5 | 政权"是什么"由头衔/关系/制度推导，不设形态枚举 | "形态由 title/member/liege/institution 推导"；"不新增 `PolityKind`" | `POLITY_TITLE_CONTRACT_MODEL.md:173`,`:241`；`RELATION_TAXONOMY_AND_TITLE.md:170` |
| C6 | **法律域是对外唯一粗粒度接口**，其他系统只引用域 | "秘密系统、罪名定义、派系诉求、资格约束等不挂具体题、组或族，只引用域" | `ARCHITECTURE_2_0_LAW_OFFICE_NAME_REGION_MODEL.md:131` |
| C7 | **硬约束与权重分离**，是跨系统通用原则 | "硬约束…不通过直接排除，不受任何权重影响"；"与 5.5 头衔资格、4.9 罪名定义引用、派系候选保持同一套口径" | `ARCHITECTURE_2_0_LAW_OFFICE_NAME_REGION_MODEL.md:455`,`:969-973` |
| C8 | 法律秩序挂在 polity/title/contract，不只挂 realm | "`LegalOrder` 挂在 polity/title/contract，不只挂 realm" | `POLITY_TITLE_CONTRACT_MODEL.md:249` |
| C9 | **单一可写权威**（同一数据只有一个写者） | "同一条数据只能有一个可写权威"；"法律：新 `RealmLawState.activeLawByGroup` 是权威" | `WORLD_SANDBOX_AND_SYSTEMS_PLAN.md:57-60` |
| C10 | 信仰"规则"并入法律/司法，信仰只做 override | "规则；优先并入法律/司法，信仰只做 override"；"婚姻/性别/罪行 → 法律 + 司法；教职任命 → 头衔" | `FAITH_DOCTRINE_CLASSIFICATION.md:30`,`:219` |
| C11 | 认同机制（嵌套/渠道/阀门/下沉度）可直接复用给信仰 | "信仰认同复用同一套" | `VOCABULARY_TRACEABILITY_MATRIX.md:151` |
| C12 | 头衔三段（指代/称谓/绑定）+ 尊号绰号独立 | "指代回答…称谓回答…绑定字段回答…"；"头衔、尊号、绰号是三个独立系统" | `RELATION_TAXONOMY_AND_TITLE.md:282-305`；`HONORIFIC_NICKNAME_SYSTEMS.md:10-14` |
| C13 | 角色身份必须与人口阶层对齐 | "角色创建、继位、封官等身份变更时调用 `SyncClassFromRole()` 自动同步主/亚阶层" | `docs/角色系统整体设计.docx` §2.2 |
| C14 | 角色是连接节点，个人革新是角色行为 | "角色是多个系统的连接节点"；"个人革新是角色行为" | `docs/README_MAIN_DETAILED_REVIEW_01.md:61-62` |
| C15 | 头衔产生方式含考试，但只说"方式"不说"考什么" | "`examination`（考试，我方扩展）…候选先过 5.5 硬约束资格" | `ARCHITECTURE_2_0_LAW_OFFICE_NAME_REGION_MODEL.md:439` |

---

## 3. 冲突表（同一概念，不同文档说法不一致）

| # | 概念 | A 说法 | B 说法 | 后果 |
|---|---|---|---|---|
| X1 | **头衔种类是否硬枚举** | "无硬 `TitleKind`；类别由绑定/权利推导"（`TOTAL_SYSTEM_SYNTHESIS.md:56`） | `title_kind` 三值 君主/官僚/爵位（`LAW_OFFICE:407`）；`kind: Landed/Office/Honor/NobleRank/Claim`（`RELATION:137`）；蓝图 `kind` 王/公爵/总督…（`CIV:82-84`） | 四套分类，角色"持有什么"无法统一查询 |
| X2 | **官职系统是否独立** | 官职归口头衔（`LAW_OFFICE:1025`） | 现存 `OfficialOffice` 六值仍在用；`VOCABULARY:72` 仍写 `Office + TitleHolding`；`RELATION:20` 说现有 `TitleDefinition` "偏官职" | 角色任官接口分裂为两套 |
| X3 | **荣誉头衔归属** | "荣誉头衔 = 无绑定/无权利的头衔"（`TOTAL:61`，仍是 Title） | 尊号"无（或仅有礼仪地位）"且独立于头衔（`HONORIFIC:13`） | 荣誉到底是 Title 子类还是另一实体 |
| X4 | **尊号授予时机** | "尊号/谥号仅死后授予（华夏式）"（`角色系统整体设计.docx` §1.2） | "可以追尊、加尊、改尊、夺号"（`HONORIFIC:91`） | 生前/死后两套规则，UI 与判定冲突 |
| X5 | **谥号归哪套系统** | 谥号在 `EpithetSystem`（`角色系统整体设计.docx` §11.7） | "`DeterminePosthumousTitle` 从 `EpithetSystem` 移到 `HonorificSystem`"（`HONORIFIC:157`） | 迁移方向相反，需拍板 |
| X6 | **共有头衔（共治）** | `TitleHolding` 可多人共有，带 `role: sole/senior/co-king`、`share`（`POLITY:186-195`；`RELATION:150-155`） | 头衔定义只有 `tier/seats/personnel_weight`，无共治席位（`LAW_OFFICE:410-422`） | 法兰克共治/双王在头衔管理器里无处安放 |
| X7 | **canon（正典）是什么** | `canon → CanonTextList` 文本清单（`VOCABULARY:98`） | "canon = 状态/锁定标记（不是实体、不是文本清单）"（`FAITH:194`） | 信仰与文本/法律引用接口相反 |
| X8 | **tradition 落点** | `FaithTradition` 是内容本体（`TOTAL:32`；`VOCABULARY:90`） | "1.20 的对应物是 **rite**（教派/学派）+ tenet"；宗教学派落 `rite`（`FAITH:191`,`:209`） | 信仰内容层无法与已有的 `FaithTradition` 命名对齐 |
| X9 | **圣人/主保本体** | 圣人 ≠ 主保；`Saint` 是 cultus/封圣记录（`TOTAL:146-152`） | 建议同一 `NuminousEntity` 基类承载 神/圣/祖/魔/精怪，`Deity` 与 `SaintDef` 作子类型（`FAITH:342`） | 一张表 vs 两张表，影响角色"个人信仰/主保"外键 |
| X10 | **政权形态** | 不设 `PolityKind`（`RELATION:170`；`POLITY:241`） | 政体标签 → `PolityForm` 描述维度（`UNIFIED_MASTER_PLAN.md:68`）；`1018` 在 V2 中作 `PolityForm` 保留（`UNIFIED_DESIGN_V2.md:191`），在 Master 中"已移除"（`:193`） | 角色能进入哪种政体/官位无稳定口径 |
| X11 | **最高权力表达** | 四轴（权力结构/法理化程度/共治关系/继承产生）替代二元（`SUPREME:225-332`） | 仍保留归属题 君主/共和（`LAW_OFFICE:211-212` 的 `SupremeSovereigntyGroup`） | 谁批准/谁任命没有唯一选项集 |
| X12 | **`LawDomain` 枚举** | 五值 Institutional/Military/Faith/Governance/LegalTradition（`LAW_OFFICE:181-190`） | 九值 Constitutional/Civil/Criminal/…/Procedural（`LAW_DECREE:93`） | 同名枚举两套，跨系统引用必错 |
| X13 | **法律分层模型** | 三层：根本法(政体结构)/普通法/法令 + 特许（`TOTAL:69-76`） | LegalOrder 树（按 owner 分层，`LEGAL_SYSTEM_UNIFICATION:33-41`）；后稿又说"政体结构 ≠ 根本法"，政体结构是主题视图（`LEGAL_SYSTEM_FULL_AUDIT:425-451`） | 法律系统的顶层容器未定 |
| X14 | **法律状态机** | "Known→Pending→Active→Consolidated→Fundamentalized"（`TOTAL:73`） | 八态 + `LegalNormStage` 已加代码（`LEGAL_SYSTEM_FULL_AUDIT:296-305`,`:412`） | 文档与代码阶段数不一致 |
| X15 | **法律内容基线** | 9 组/32 法（`LAW_DECREE:21`） | 30 组、50 条、31 条无革新（`MILITARY:12`,`:224`）；删 `succ_divine` 后 49 条（`SUPREME:293`） | 任何"还缺几条"的判断都不可比 |
| X16 | **角色属性维度** | 六维属性 武力/外交/军事经略/管理/谋略/学识（`角色系统整体设计.docx` §2.3） | README 口径"基本能力→**专精**→技能树；六维专精"（`AUDIT_11:78`）；docx 全文 0 次"技能/专精" | 属性 vs 专精是两层还是一层未定 |
| X17 | **角色信仰形态** | 社会/私人/秘密 三形态（`角色系统整体设计.docx` §13.1） | `PublicFaith`/`PersonalFaith` 两层 + `privateTraditionId`（`TOTAL:100-108`）；`faith`=实体 `FaithDef`、`PersonalFaith` 是角色层附加（`FAITH:189`） | 角色页字段与信仰实体边界不一致 |
| X18 | **家族/继承归属** | 家族继承轨"挂家族系统，不被政权法律直接覆盖"（`LAW_OFFICE:894`） | docx 家族只有资产/家徽/传统，无继承规则（全文"继承"仅 2 次） | 家族轨实现方缺设计，法律侧缺运行时对象 |

---

## 4. 空白表

### 4.1 角色八域在"大规划"里缺什么

| 域 | 现有结论（含出处） | 缺什么 | 大规划里谁消费它 |
|---|---|---|---|
| 属性 | 六维 0–100 + `GetEffectiveStats()` 唯一出口（`角色系统整体设计.docx` §2.3） | 属性→系统消费清单（法律/官职/革新/军事/外交分别读哪几维）、数值校准、与人口阶层解耦规则 | **无**：大规划 0 处声明消费角色属性 |
| 技能/专精 | 仅 README 一句话"基本能力→专精→技能树"（`AUDIT_11:78`） | 定义、前置、成长曲线、上限、与属性关系、用途（门禁/产出/战斗） | 无（docx 0 次"技能"） |
| 教育 | 只有革新"知识与健康/教育"域（`INNOVATION_CATALOG_BLUEPRINT.md:124`）与信仰教育渠道（`FAITH_IDENTITY_MODEL.md:17`） | 师承/学院/入仕考试/留学；与 `examination` 任命方式（`LAW_OFFICE:439`）的输入接口 | 革新侧只有"解锁"，没有"谁被教育" |
| 成长 | docx 仅 `DailyTick`：年龄+1、压力计时（§2.10） | 生命阶段、成长曲线、监护/教养、事件驱动成长、代际差异 | 无 |
| 健康 | 心理健康状态机 + 疾病表（docx §7）；Disease 属角色健康、Plague/Disaster 属群体（`AUDIT_11:72-74`） | 与人口/灾害/医疗革新的接口、属性衰减规则、身体部位模型落地 | 灾害/人口系统已就绪，角色侧未接线 |
| 关系 | docx 三层：opinion / `RelationshipType` / `BondType`；`RELATION` 管 P/S/U + `P↔Character` | 角色↔角色关系进入政治/法律（婚姻已是法律组 `MarriageStatusGroup`，`LAW_OFFICE:243`，但无对角色/继承的效果接口） | 法律侧有组，角色侧无效果出口 |
| 家族 | `FamilyNode` 递归树 + 家族资产/传统/家徽（docx §6.5） | 家族作为法律/政治主体：`TOTAL` 本体表**无 Family**；头衔侧只有 `family_power` 权重字段（`LAW_OFFICE:421`）；家族席位上限（`:459`）无实体 | 派系/继承/头衔都引用家族，但无家族实体 |
| 继承 | 三轨最完整：国家/家族/地方（`LAW_OFFICE:887-902`）；枚举去重、`EldestLine/CognaticKin/Seniority` 唯一名 | 运行时对象缺失（`TitleHolding` 在代码中不存在，已 grep 确认）；国家交接与家族轨的合成顺序只在文中有（`:902`）；地方继承参照 TTHTOM 未实现 | 法律/头衔已定义，角色/家族侧未接线 |
| 语言能力 | 头衔与称谓按语言过滤（`LAW_OFFICE:105`,`:451`） | 角色"会说什么语言/读写水平"字段；docx 无 `language`；考试/任官/外交的读写门槛无输入 | 语言系统只服务命名/称谓，不服务角色 |
| 身份/阶层 | docx 十类 `CharacterRole` + `SyncClassFromRole()` | 与法律 `ClassCitizenshipGroup`（`LAW_OFFICE:244`）、5 阶层、8 维需求（`UNIFIED_MASTER_PLAN.md:111`）的映射表 | 法律/经济已定义阶层，角色侧只有一个枚举 |

### 4.2 "行动门禁（谁有权做什么）"是否已有统一模型

**结论：没有。** 现有是 7 个分散片段，分别回答"谁能任某头衔""谁能提案""谁能执行"，但没有任何一层把"角色能力"作为输入，也没有 `actor × action × target × 权威来源` 的统一契约。

| 已有片段 | 出处 | 覆盖范围 | 缺什么 |
|---|---|---|---|
| `TitleEligibility` 硬约束（9 维） | `LAW_OFFICE:441-455` | 谁能任某头衔 | 9 维全是身份/机构/文化/信仰/语言/区域/阶段/规模，**无属性/技能/教育/健康** |
| 硬约束与权重分离原则 | `LAW_OFFICE:969-973` | 通用原则 | 只是原则，无统一执行点 |
| `AppointmentMethod` 7 种 | `LAW_OFFICE:439` | 头衔产生方式 | `examination` 无角色能力输入 |
| `ProposalAuthority` 3 值（HeadOfState/CentralInstitution/CharterHolders） | `WORLD_SANDBOX_AND_SYSTEMS_PLAN.md:954`；`SUPREME:180` | 谁能提案 | 制度位置而非角色资格；缺共治者/主权机构（`SUPREME:180-188` 已提） |
| `OfficeFactor` capabilities/constraints | `CIV_COGNITION_TITLE_CURRENCY.md:134-144` | 头衔能发什么命令 | 未实现；无与角色的绑定 |
| `DecreeOrder`（`officialCharacterId` + 官员管理/阴谋/腐败） | `LEGAL_SYSTEM_FULL_AUDIT:37-41`,`:208` | 谁能执行、执行到什么程度 | **唯一**把角色能力接进执行的钩子，且只管法令 |
| 四段过滤 can_create/can_have/can_pass/can_keep | `LAW_OFFICE:954-959` | 名号/头衔生命周期 | 不是行动授权 |
| 信仰组织阶段门禁 | `FAITH_DOCTRINE_CLASSIFICATION.md:245-249` | 哪些教规/行动可选 | 按组织阶段，不按角色 |
| Architecture 2.0 Action="是否允许" | `ARCHITECTURE_2_0_DOMAIN_MODEL.md:158-160` | 机制位 | 无授权主体模型 |

**建议的横切契约（角色并入大规划唯一必须新增的一层）**：
`ActionGate(actor, action, target, authoritySource)`，四类 provider：① 头衔/官职资格（`TitleEligibility` + `OfficeFactor`）② 法律/身份资格（法律域 + 公民权组）③ 组织阶段（信仰/机构）④ 角色能力（属性/技能/教育/健康）。现有 7 个片段全部收编为该接口的 provider，而不是各写一套 `CanXxx`。

---

## 5. 给 Lead 的建议

### 5.1 以哪几份为基座

| 角色 | 文档 | 理由 | 并入前必须补的 |
|---|---|---|---|
| 第一基座（执行口径） | `docs/architecture/UNIFIED_MASTER_PLAN.md` | 全仓唯一执行口径，DOF/门禁/SaveVersion/批次流程都以它为准 | WBS 增角色工作包；`PolityForm` 与 `1018` 归属拍板 |
| 第二基座（本体框架） | `docs/design/TOTAL_SYSTEM_SYNTHESIS.md` | 权威=能力包、组件可组合、无硬层级，是唯一能容纳角色的"可挂载"框架 | 本体表补 `Character`/`Family`；关系端点补 Character↔Character |
| 第三基座（可落地接口） | `docs/architecture/ARCHITECTURE_2_0_LAW_OFFICE_NAME_REGION_MODEL.md` | 头衔/继承/资格/键名最细，是角色与法律、官职、继承三系统的实际接缝 | 补共有头衔；补 ActionGate；头衔持有运行时 |
| 角色侧第四基座 | `docs/角色系统整体设计.docx` | 角色域唯一全量设计（属性/人格/特质/关系/家族/健康/DNA/称号），与代码现状对应 | 转 md；按 `AUDIT_11:78` 补专精/技能树；按 `HONORIFIC` 拆谥号 |
| 必读参考 | `AUDIT_11`（缺口清单）、`HONORIFIC`（称号拆分）、`CIV_COGNITION`（头衔运行时/官员因子）、`SUPREME`（权威四轴） | 提供已裁决/待裁决边界，避免重开讨论 | — |

### 5.2 先解决的三个矛盾（按阻塞度排序）

| 顺序 | 矛盾 | 为什么必须先解决 | 建议裁决 |
|---|---|---|---|
| M1 | **头衔/官职本体未收敛**：四套 `kind` 分类（X1）+ 官职是否独立（X2）+ 共有头衔缺失（X6） | 角色的"身份/权力/继承"全部挂在这个本体上；本体不定，属性/技能就没有消费点，角色面板也无处显示 | 采用 TOTAL 的"无硬 kind、由绑定+权利推导"作**本体原则**；RELATION 的 5 值只作**派生分类视图**（可查询、不落库）；头衔持有层必须支持多人共有（`TitleHolding{characterId, role, share}`）；官职仅作官僚头衔的 `kind` 标记，不重建独立系统 |
| M2 | **"谁有权做什么"没有统一门禁**：最高权力二元 vs 四轴（X11）；资格字段 0 维角色能力（§4.2）；Action 层无授权主体 | 角色系统一旦并入，属性/技能/教育/健康必须有出口；否则仍是一堆"没人读"的数值——这正是 docx 现状的问题 | 先定 `ActionGate` 契约（§4.2），把 `ProposalAuthority`/`TitleEligibility`/`OfficeFactor`/组织阶段收编为 4 个 provider；最高权力采 SUPREME 四轴，`SupremeSovereignty` 降为兼容字段 |
| M3 | **角色域自身基线不唯一**：docx 六维+绰号谥号一体（X16/X17）vs README"专精→技能树"vs HONORIFIC 拆称号（X4/X5） | 三份文档对"角色是什么"回答不同，字段无法与其他系统对齐；且 docx 早于 10–11 月所有系统结论 | docx 作**现状基线**；`AUDIT_11:78` 作**待补设计输入**（专精/技能树另立一节）；`HONORIFIC` 已定项（绰号/谥号分离、尊号可追改）**覆盖** docx §11.7；先出"角色域现状+待定"一页表，再谈并入 |

> 紧随其后（不阻塞并入，但拖久了必返工）：**X12 `LawDomain` 双枚举**（5 值 vs 9 值）与 **X10 政权形态**（禁止 `PolityKind` vs `PolityForm`）。

### 5.3 建议路径（三步，先出接口再写角色内容）

1. **定本体与门禁**（1 份短文 + 3 张表）：头衔本体裁决（M1）、`ActionGate` 契约（M2）、角色域基线表（M3）。
2. **写接缝表**（不写新机制）：把角色八域逐项映射为"角色 → 法律/官职/继承/信仰/革新/军事"的消费关系，每行必须有"谁读、读哪个字段、失败会怎样"。
3. **再写角色系统设计**（专精/技能树/教育/成长），并把它作为 `UNIFIED_MASTER_PLAN` 的一个 WBS 工作包（如 W23），而不是独立文档。

---

## 6. 勘察边界与风险

1. 本报告只读文档 + 关键符号存在性核对，**未系统读实现代码**；"与代码脱节"的判断依据是文档自述（如 `LAW_OFFICE:3` "代码层尚未改动、未编译验证"）与 `grep` 结果（`TitleHolding`/`TitleInstance` 无定义、无通用 `ActionResolver`/`CanExecute`、`ProposalAuthority` 仅存在于法律目录）。
2. 行号基于 2026-11-07 当前工作区；工作区有大量未提交改动（`UNIFIED_MASTER_PLAN.md:20`），引用前建议再 `grep` 复核。
3. `docs/角色系统整体设计.docx` 是唯一未纳入 md 清单的角色全量设计；若它已被后续讨论实质修订，本报告 §4.1/§5 的"空白"结论需要相应收紧。
4. 未覆盖 `docs/design/LANGUAGE_NAMING_IDENTITY.md`、`FAITH_IDENTITY_MODEL.md`、`INNOVATION_*` 全量，只在需要交叉引用时取单句；它们与角色的交集（语言能力、信仰个人层、革新解锁身份）已记入 §4.1。
