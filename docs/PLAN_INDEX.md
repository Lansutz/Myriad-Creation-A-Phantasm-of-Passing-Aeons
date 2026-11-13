# 计划索引（唯一入口）

> **2026-11-09 更新**：系统级计划已重做为 [`plan-v3/SYSTEM_MASTER_PLAN.md`](plan-v3/SYSTEM_MASTER_PLAN.md)（v3 总纲）。
> 分工：**系统现状/依赖/里程碑/验收**一律看 v3 总纲；**域内设计裁决**（头衔、信仰、角色接缝等）仍按下表 §1 的权威文档。
> `architecture/UNIFIED_MASTER_PLAN.md`（V2）的**状态表与排期作废**（其 §4 决策条目仍有效），见 v3 总纲开头。
>
> 2026-11-09。规则：
> 1. 本索引是计划状态的唯一入口；与本索引冲突的旧文档，**以本索引和其列出的权威文档为准**。
> 2. 已取代的计划从 `docs/design/` 移入 `docs/archive/2026-11-09-superseded-plans/`（可恢复；确认后可直接删）。
> 3. 研究/语料/审计不是"计划"，保留在原位；但其中与最新口径冲突的结论不作数。

## 1. 权威计划（canonical）

| 文档 | 范围 | 说明 |
|---|---|---|
| `DECISIONS_M1_M2_M3.md` | 裁决总账 | M1 头衔/官职、M2 行动门禁、M3 角色；批次记录与自审 |
| `../architecture/ARCHITECTURE_RULES.md` | 架构规则 | 程序集方向、目录即 owner、Def/State/DTO 分家、门禁 |
| `TITLE_INHERITANCE_PLAN.md` | 头衔继承/收口 | 三模式（None/RealmLaw/OwnLaw）、位阶 §11、自审 §12 |
| `TITLE_SEMANTIC_FIELD_SCHEMA.md` | 头衔语义字段 | 指代 + 称谓、语义特征、分布、术语表 |
| `FAITH_AS_CULTURE_MODEL.md` | 信仰系统 | 宗教固定/中间层可选/**传统为主运行单位**；只借分裂+挂靠 |
| `FAITH_DOCTRINE_CLASSIFICATION.md` | 教义内容 | 两轴分类、槽位、组织阶段（内容层，仍有效） |
| `CHARACTER_PLAN_SEAM.md` | 角色接缝 | 四层门禁、八域接缝（需按 M3 进度复核） |

## 2. 字段模型与语料（参考，非计划）

| 文档 | 用法 |
|---|---|
| `TITLE_REFERENT_APPELLATION_MODEL.md` | 指代/称谓/绑定字段细节；其中 `kind` 列表与 `rank` 部分**已被 M1/§11 取代** |
| `TITLE_APPELLATION_TYPOLOGY.md` | 跨文明称谓组合类型（语料） |
| `TITLE_ETYMOLOGY_AND_AUTHORITY.md` | 词源=权力来源（语料） |
| `CHINESE_TITLE_ETYMOLOGY.md` | 王/公/侯/伯/子/男/后 字源（语料） |
| `COMPARATIVE_TITLE_CORPUS.md` | 西欧/神罗/北欧/近东/印度/希腊/美洲样本（语料） |
| `HONORIFIC_NICKNAME_SYSTEMS.md` | 头衔/尊号/绰号三套系统（研究） |
| `CIV_COGNITION_TITLE_CURRENCY.md` | 认知/命名/头衔/货币/灾害（研究） |
| `FAITH_CK3_TERM_MAPPING.md` | CK3 宗教术语对照（研究） |
| `FAITH_LONG_CONTENT_CRITIQUE.md` | 对旧五层长文的批评（研究） |
| `RELATION_TAXONOMY_AND_TITLE.md` | 关系分类（参考） |
| `POLITY_TITLE_CONTRACT_MODEL.md` | 政治实体/头衔/土地/契约（参考） |
| `VOCABULARY_TRACEABILITY_MATRIX.md` | 术语—计划对照（参考） |
| `DATA_FORGE_INTEGRATION.md`、`UNITY_MCP_SETUP.md` | 工具接入说明（非计划） |

## 3. 已取代（移出 design）

| 文档 | 取代原因 |
|---|---|
| `FAITH_CULTURE_MODEL_BORROW.md` | 以 faith/community 为核心，与"**传统**为主运行单位"冲突 |
| `FAITH_IDENTITY_MODEL.md` | 借族群认同的"可弱可无"嵌套，与"**宗教身份不可无**"冲突 |
| `FAITH_SYSTEM_ANALYSIS.md` | faith 核心方案（family/religion 可选），被 `FAITH_AS_CULTURE_MODEL.md` 取代 |

## 4. 矛盾裁决（最新口径，冲突一律按此）

| 主题 | 旧说法 | 现行口径 | 依据 |
|---|---|---|---|
| 信仰主运行单位 | faith / community | **传统（Tradition）** | `FAITH_AS_CULTURE_MODEL.md` |
| 宗教层 | 可选分组 | **固定、不消失** | 同上 |
| 中间层 | 无 | **可选**（名称待定：信仰/教派/宗门） | 同上 |
| 挂靠 | 可弱可无 | **宗教身份必填**；中间层/传统可选 | 同上 |
| 分裂 | 文化式产生新文化 | **宗教层不变**；分裂发生在中间层/传统 | 同上 |
| 借用范围 | 整体借文化机制 | **只借分裂 + 挂靠** | 同上 |
| 头衔位阶 | `rank` 浮点 | **组合序 + 绑定**，跨域不可比 | `TITLE_INHERITANCE_PLAN.md` §11 |
| 头衔 `kind` | 保留字段 | **派生视图，不存** | `DECISIONS` M1 |
| 删 `rank` 顺序 | 直接删 | **先实现组合序/绑定优先级，再删** | 同上 §11.4 |
| 疾病 | A 删角色级 / B 角色级接管 | **C：人口级权威**（意义是减人口），角色级=有名角色病程层 | `TITLE_INHERITANCE_PLAN.md` §9 |
| 数值 | 抽象分数作决策源 | **机制优先**：分数只作度量，决策由规则/程序/关系 | `TITLE_INHERITANCE_PLAN.md` §10 |
| 宗教术语 | rite | **传统**（religion=宗教、faith=信仰） | 用户口径 |

## 5. 旧系统计划（按代码实测状态，不再是"状态未核"）

> 2026-11-09 补测。以下状态**以代码与内容数据为准**，不看文档自述；证据见
> `reports/law-audit.md`、`reports/innovation-audit.md`、
> `reports/law-innovation-code-status-20261109-0047.md`。
> 结论：法律与革新**都是"部分已跑通、部分空转"的在建系统**，不是"未开工的旧计划"。

| 系统 | 实测状态 | 真的在跑 | 空转 / 缺口 |
|---|---|---|---|
| 法律（`Simulation/Laws` + `Simulation/LawHost`） | 约 55%（子审计判定），主线可玩 | 已知→提案（首脑/中央机构/特许持有者）→pending→到期固化；法令颁布/冷却/到期/撤销；法令逐级下达（传令官 + 合规度五档）；年度效果结算（税收/人力乘子被生产消费）；GovernmentPanel 可操作 | 罪行—审判—判决整链 0% 通电（无案件实体、`TryTrialCrime`/`MatchCrimes` 零调用者、`justice.daily` 空转）；`LegalNormStage`/`fundamentalizedLawIds` 无写入方（根本化未实现）；`normKind/scope/precedence/overrides` 无消费者（无冲突裁决）；`LawComposition` 与 `LawDef` 双源未合并；`upkeepPerYear` 不扣钱 |
| 革新（`Content/…/Innovation*` + `Simulation/…/Innovation*`） | 未给百分比：内容/前置/单条推进链可跑，四层空转 | 186 条内容、前置图无环无悬空；AI 选题 + 每日研究点；角色练习→突破计划→验证固化；硬前置/能力/软前置求值；政体组件、阶层、头衔、家族传统等 `HasInnovation` 硬字段门槛真生效；政权级状态入档 | 玩家零入口（`Assets/UI` 无一处引用）；`unlockRefs/unlocks` 84 条无消费者；`MonthlyTickProgress`/发现层/`CharacterResearchManager` 零调用；角色熟练永不提升（突破链只能吃 19 条根革新）；研究计划与熟练不入档；配方/物品/资源门槛无游戏侧调用方 |

其余文档仍按"未逐份核对"处理（默认不删）：`UNIFIED_DESIGN_V2.md`、`TOTAL_SYSTEM_SYNTHESIS.md`、`WORLD_SANDBOX_AND_SYSTEMS_PLAN.md`、`SUPREME_AUTHORITY_MODEL.md`、`SEVEN_DIMENSIONS_LAW_STUDY.md`、`MILITARY_LAW_TAXONOMY_AUDIT.md`、`GOODS_*`、`GEOLOGICAL_CLIMATE_DYNAMICS_PLAN.md`、`GATHERING_OUTPOST_VEHICLE_MODEL.md`、`NAVY_ENTITY.md`、`LANGUAGE_NAMING_IDENTITY.md`。
法律/革新的**审计与历史**文档保留原位、不作结论：`LEGAL_SYSTEM_FULL_AUDIT_AND_PLAN.md`、`LEGAL_SYSTEM_UNIFICATION.md`、`LAW_DECREE_AUDIT_AND_REDESIGN.md`、`INNOVATION_*`、`TECH_*`、`../architecture/ARCHITECTURE_2_0_*`。

## 6. 说明

- 本索引不新增计划内容，只定"谁是现行、谁被取代、冲突按谁"。
- 已移出文件在 `docs/archive/2026-11-09-superseded-plans/`；确认后可直接删除。
