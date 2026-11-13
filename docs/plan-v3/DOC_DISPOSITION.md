# 文档处置表（DOC DISPOSITION）

> 2026-11-09。**本文只做处置，不新增计划**。判断"谁是现行、谁被取代、谁只能当参考/证据"，不写方案、不改任何文件。
> 范围：`docs/` 下全部非归档 markdown。归档目录（`docs/archive/**`）不在表内。
> 依据清单：`_fc_verify/docs-inventory.txt`（129 份）。**该清单已过期**：行号与 mtime 会漂移（例：`DECISIONS_M1_M2_M3.md` 清单记 404 行，实读 541 行；`UNIFIED_MASTER_PLAN.md` 记 203 行，实读 264 行），且清单之后新增了 9 份文件（见 §5）。
> 日期列 = 清单里的文件 mtime（EXTRA 段用文档自述日期）。**mtime ≠ 内容有效期**。
> 纪律：凡涉及"某系统已完成/未完成"，本表一律标 `文档自述，未核代码`，或只写文档职责不下判断。五个处置值：`现行（canonical）` / `部分有效` / `已取代` / `仅语料/参考` / `审计证据`。
> 统计：现行 9（清单内）/ 已取代 3 / 部分有效 21 / 仅语料·参考 36 / 审计证据 60 / 清单外新增 9。

---

## 1. 项目级口径（总纲）

| 路径 | 类别 | 日期 | 一句话职责 | 处置 | 依据 |
|---|---|---|---|---|---|
| `docs/PLAN_INDEX.md` | 总纲 | 2026-11-09 | 计划索引：§1 权威 7 份、§2 参考、§3 已取代、§4 矛盾裁决、§5 法律/革新实测 | 部分有效 | "唯一入口"已被 `plan-v3/SYSTEM_MASTER_PLAN.md` §0.1 取代；§1 权威名单、§3 取代结论、§4 裁决口径仍有效 |
| `docs/plan-v3/SYSTEM_MASTER_PLAN.md`【EXTRA】 | 总纲 | 2026-11-09 | 系统总纲 v3：现状—顺序—验收，唯一系统级入口 | 现行（canonical） | 2026-11-09 生效；其 §0 结论与八域审计一致；引 `DOC_DISPOSITION.md` 为本表 |
| `docs/architecture/ARCHITECTURE_RULES.md` | 总纲 | 2026-11-03 | 文件与架构硬规则：程序集方向、owner 决策树、Def/State/DTO 分家、门禁 | 现行（canonical） | PLAN_INDEX §1 权威；内容为规则而非状态，未被任何后续文档推翻 |
| `docs/architecture/CURRENT_ARCHITECTURE.md` | 总纲 | 2026-11-03 | 15 程序集图、L0–L7 依赖序、R8 门禁规则、目录 owner、遗留清单 | 部分有效 | 结构与边界（15 asmdef 图/门禁 R1–R8/owner 表）仍成立；"Round 35 基线 552/552、9/9"已被 2026-11-09 实测覆盖（EditMode 706/706；PlayMode 仍为 7 个测试文件） |
| `docs/architecture/SYSTEM_OWNERSHIP.md` | 总纲 | 2026-11-02 | 系统 owner/程序集/门禁/批次口径（Round 35 版） | 部分有效 | 同 `CURRENT_ARCHITECTURE`：owner 表与遗留 1–6 仍成立；"552/552、9/9"基线过期（2026-11-09 实测 706/706） |
| `docs/architecture/UNIFIED_MASTER_PLAN.md` | 总纲 | 2026-11-05 | 统一总计划 V2：§1 基线、§2 DoD、§4 决策、§5 WBS、§6 Phase 0–5 | 已取代 | 被 `docs/plan-v3/SYSTEM_MASTER_PLAN.md` 取代；**§4 决策有效**（革新区块 ID 分配、城防 0–15、物产/器物分流、法律缩窄），§1/§5 状态表已过期 |
| `docs/architecture/MASTER_COMPLETION_PLAN.md` | 总纲 | 2026-11-03 | W1–W12 收口记录 + DoD + 边界项 | 已取代 | 被 `docs/plan-v3/SYSTEM_MASTER_PLAN.md` 取代；本文件自述"仅用于追溯"，保留作 W1–W12 历史 |
| `docs/plan-v3/audit/_FORMAT.md` | 工具说明 | 2026-11-09 | 域审计输出格式：只读、三态判定、`路径:行号` 证据、禁写方案 | 现行（canonical） | plan-v3 八域审计的共用契约；"代码是唯一真源/不得把文档自述当证据"即本表纪律来源 |

## 2. `docs/architecture/` 其余（2.0 重构系列 + 状态类）

| 路径 | 类别 | 日期 | 一句话职责 | 处置 | 依据 |
|---|---|---|---|---|---|
| `ARCHITECTURE_2_0_DOMAIN_AUDIT.md`（01） | 审计证据 | 2026-09-24 | Entity 承担规则职责、Definition/Runtime 混合的首轮判断 | 审计证据 | 研究阶段记录；结论属"结构判断"，非计划 |
| `ARCHITECTURE_2_0_DOMAIN_AUDIT_02.md` | 审计证据 | 2026-09-24 | Query/Rule/Process 雏形未被统一建模 | 审计证据 | 同上 |
| `ARCHITECTURE_2_0_DOMAIN_AUDIT_03.md` | 审计证据 | 2026-09-24 | Map/Distribution/History/Settlement/Unit/Army/Title 边界 | 审计证据 | 同上 |
| `ARCHITECTURE_2_0_DOMAIN_AUDIT_04.md` | 审计证据 | 2026-09-24 | MapActor/Army/TileData 真实职责 | 审计证据 | 同上 |
| `ARCHITECTURE_2_0_DOMAIN_AUDIT_05.md` | 审计证据 | 2026-09-24 | Map/MapCell/Anchor/Settlement/Building/Structure 边界 | 审计证据 | 同上 |
| `ARCHITECTURE_2_0_DOMAIN_AUDIT_06.md` | 审计证据 | 2026-09-24 | BurgData 超级实体与 Building/Structure 拆分 | 审计证据 | 同上 |
| `ARCHITECTURE_2_0_DOMAIN_AUDIT_07.md` | 审计证据 | 2026-09-24 | Camp/Settlement 生命周期与 Anchor 边界 | 审计证据 | 同上 |
| `ARCHITECTURE_2_0_DOMAIN_AUDIT_08.md` | 审计证据 | 2026-09-24 | Great Wall 不是普通 Structure；存档边界 | 审计证据 | 同上 |
| `ARCHITECTURE_2_0_DOMAIN_AUDIT_09.md` | 审计证据 | 2026-09-24 | 大型工程（Great Project）与 Construction/Barrier/Route | 审计证据 | 同上 |
| `ARCHITECTURE_2_0_DOMAIN_AUDIT_10.md` | 审计证据 | 2026-09-24 | Plan 不是国家级工程容器；Construction/Network 边界 | 审计证据 | 同上 |
| `ARCHITECTURE_2_0_DOMAIN_AUDIT_11.md` | 审计证据 | 2026-09-24 | main/README 逐段复核：革新已发生过规模升级 | 审计证据 | 同上；README 口径本身已由 `README_ALIGNMENT` 降级 |
| `ARCHITECTURE_2_0_DOMAIN_AUDIT_12.md` | 审计证据 | 2026-09-24 | TradeCenter→Route→Road→Fort→Gate→SettlementControl 事实核对 | 审计证据 | 同上 |
| `ARCHITECTURE_2_0_DOMAIN_AUDIT_13.md` | 审计证据 | 2026-09-24 | SettlementControl 是"控制关系"不是地块属性 | 审计证据 | 同上 |
| `ARCHITECTURE_2_0_DOMAIN_AUDIT_14.md` | 审计证据 | 2026-09-24 | SettlementControl/MilitaryMovement/Road/README 尾段 | 审计证据 | 同上 |
| `ARCHITECTURE_2_0_DOMAIN_AUDIT_15.md` | 审计证据 | 2026-09-24 | TradeRoute/Caravan/Innovation"设计已升级、代码残留旧模型"并存 | 审计证据 | 同上 |
| `ARCHITECTURE_2_0_DOMAIN_AUDIT_16.md` | 审计证据 | 2026-09-24 | InnovationTree 是跨域聚合对象（字段级迁移证据） | 审计证据 | 同上 |
| `ARCHITECTURE_2_0_DOMAIN_AUDIT_17.md` | 审计证据 | 2026-09-24 | InnovationProgress/CharacterResearch/ResearchPlan 三层状态 | 审计证据 | 同上 |
| `ARCHITECTURE_2_0_DOMAIN_AUDIT_18.md` | 审计证据 | 2026-09-24 | InnovationTree 内含两套生命周期 | 审计证据 | 同上 |
| `ARCHITECTURE_2_0_DOMAIN_AUDIT_19.md` | 审计证据 | 2026-09-24 | 革新/研究无独立存档 DTO（Save Schema 证据） | 审计证据 | 同上 |
| `ARCHITECTURE_2_0_DOMAIN_AUDIT_20.md` | 审计证据 | 2026-09-24 | SaveSystem 与 GameSaveSystem 两套存档入口 | 审计证据 | 同上 |
| `ARCHITECTURE_2_0_DOMAIN_AUDIT_21.md` | 审计证据 | 2026-09-24 | GameSaveSystem 独立管线与 Realm/Burg/Province 覆盖 | 审计证据 | 同上 |
| `ARCHITECTURE_2_0_DOMAIN_AUDIT_22.md` | 审计证据 | 2026-09-24 | 个人知识层 InnovationKnowledgeSystem 已独立 | 审计证据 | 同上 |
| `ARCHITECTURE_2_0_DOMAIN_AUDIT_23.md` | 审计证据 | 2026-09-24 | mastery/practice 双状态与 ResearchPlan 生命周期 | 审计证据 | 同上 |
| `ARCHITECTURE_2_0_DOMAIN_AUDIT_24.md` | 审计证据 | 2026-09-24 | 个人知识公开接口闭合 | 审计证据 | 同上 |
| `ARCHITECTURE_2_0_DOMAIN_AUDIT_25.md` | 审计证据 | 2026-09-24 | CharacterResearch/Knowledge/ResearchPlan 存档 Adapter 缺口 | 审计证据 | 同上 |
| `ARCHITECTURE_2_0_DOMAIN_AUDIT_26.md` | 审计证据 | 2026-09-24 | GameSaveData 不是完整 Domain Save | 审计证据 | 同上 |
| `ARCHITECTURE_2_0_DOMAIN_AUDIT_27.md` | 审计证据 | 2026-09-24 | RealmBasicSaveData 字段精确映射 | 审计证据 | 同上 |
| `ARCHITECTURE_2_0_DOMAIN_AUDIT_28.md` | 审计证据 | 2026-09-24 | InnovationTree 同时维护三种粒度状态 | 审计证据 | 同上 |
| `ARCHITECTURE_2_0_DOMAIN_AUDIT_29.md` | 审计证据 | 2026-09-24 | 实践驱动进度：方法已实现但**未接进主循环**（commit cf5486） | 审计证据 | 针对特定 commit 的调用图证据；不可当当前事实 |
| `ARCHITECTURE_2_0_DOMAIN_AUDIT_30.md` | 审计证据 | 2026-09-24 | 把革新审计接回 2.0 迁移目标（authority 出 GameWorld） | 审计证据 | 同上 |
| `ARCHITECTURE_2_0_DOMAIN_BOUNDARY_MATRIX.md` | 审计证据 | 2026-09-24 | 权威/关系/派生/查询/兼容/存档六分与核心链 | 审计证据 | 由 Audit 04–19 归纳；自述"不等于已完成迁移" |
| `ARCHITECTURE_2_0_DOMAIN_MODEL.md` | 域设计 | 2026-09-24 | 2.0 领域模型与重构计划（ENTITY→…→DERIVED 链） | 仅语料/参考 | 研究设计稿、未迁移；现行结构与边界以 `ARCHITECTURE_RULES`/`CURRENT_ARCHITECTURE` 为准 |
| `ARCHITECTURE_2_0_FIELD_MIGRATION_MATRIX.md` | 审计证据 | 2026-09-24 | 字段级 KEEP/SPLIT/MOVE/DERIVE/QUERY 迁移矩阵 | 审计证据 | 自述"只描述方向，不执行迁移" |
| `ARCHITECTURE_2_0_INNOVATION_SAVE_SCHEMA.md` | 域设计 | 2026-09-24 | 革新/研究四类状态的存档 DTO 草案 | 仅语料/参考 | 草案（Draft 01）；无现行文档引用它作为口径 |
| `ARCHITECTURE_2_0_LAW_OFFICE_NAME_REGION_MODEL.md` | 域设计 | 2026-11-04 | 法律域/族/组三层、官职并入头衔、继承三轨、键名命名空间、法理区域 | 部分有效 | 具体被推翻处：①自述"代码层尚未改动"→`LawDomain/LawFamily/LawGroup` 已在 `Simulation.Laws/LawConstraints.cs:45/54/68`；②头衔 `title_kind`/官职本体被 `DECISIONS_M1_M2_M3` M1 取代；三是继承三轨已由 `TITLE_INHERITANCE_PLAN` 收为三模式 |
| `ARCHITECTURE_2_0_README_ALIGNMENT.md` | 工具说明 | 2026-09-24 | README 对照的证据优先级规则（6 级） | 已取代 | 被 `docs/plan-v3/audit/_FORMAT.md` §2 取代（"代码是唯一真源、不得把文档自述当证据"表述更硬） |
| `Architecture-2.0-GitHub-Sync-Tool-Issue.md` | 工具说明 | 2026-09-24 | 双项目根导致的"搜索命中旧树、取文件 404"误判 | 仅语料/参考 | 工具坑记录；`MyriadCreation/` 已成唯一工程根 |
| `Architecture-2.0-Phase0-Audit.md` | 审计证据 | 2026-09-24 | 2.0 Phase 0 仓库审计：四层成熟度、目标依赖方向 | 审计证据 | 只读基线，非计划 |
| `Architecture-2.0-Tooling-Sync.md` | 工具说明 | 2026-09-24 | 声明 `MyriadCreation/` 为 canonical 工程根 | 仅语料/参考 | 与上条同源；已成常识，无裁决力 |
| `ASSET_LAYOUT_PLAN.md` | 计划 | 2026-11-02 | Assets 全盘盘点 + P0–P8 目录整改执行状态 | 部分有效 | 具体被推翻处：§顶部"EditMode 568/568"基线过期（2026-11-09 实测 706/706）；P6/P7 顶层形态若有变以实测为准 |
| `CONTENT_ASSEMBLY_MIGRATION.md` | 计划 | 2026-11-02 | Modding/ContentRegistry 下沉 Content 的拆分方案（P0–P4） | 部分有效 | 具体被推翻处：`CURRENT_ARCHITECTURE` §5-4 仍记"Modding 仍在 Simulation"→ P2–P4 未落地；§1 文件/行数规模数字过期 |
| `REFERENCE_GAMES_UNIFIED_PLAN.md` | 计划 | 2026-11-04 | 参考游戏借鉴边界 + 革新本轮范围与冻结域 | 部分有效 | 具体被推翻处：§2-8/§3.1"宗教/信仰/思想域冻结"已过期——信仰改由 `FAITH_AS_CULTURE_MODEL.md` 重定；借鉴/许可证边界仍可用 |

## 3. `docs/audit/`（审计与复核证据）

| 路径 | 类别 | 日期 | 一句话职责 | 处置 | 依据 |
|---|---|---|---|---|---|
| `ARCH-COMPLETION-PLAN.md` | 计划 | 2026-11-02 | 程序集解耦审计原文（B0–B8）+ 收尾批次 | 部分有效 | 具体被推翻处：§1–§9 的"待办/批次"状态已被 `CURRENT_ARCHITECTURE` 的 B0–B8 ✅ 覆盖；不得再作排期 |
| `A-save-persistence.md` | 审计证据 | 2026-10-31 | 存档/读档完整性与静态注册表生命周期（P0-1…P0-5） | 审计证据 | 只读审计；所述缺陷是否已修需看代码，不以本报告为现状 |
| `B-arch-recovery.md` | 审计证据 | 2026-10-31 | 反编译恢复残留、重复类型、程序集依赖（935 删/2704 增快照） | 审计证据 | 针对特定工作区快照 |
| `C-economy-provisioning.md` | 审计证据 | 2026-10-31 | 经济/补给/金库链路一致性（国库付粮 vs 背包无粮） | 审计证据 | 只读静态审计 |
| `D-test-quality.md` | 审计证据 | 2026-10-31 | 测试有效性与回归风险（298/298 的来源可疑性） | 审计证据 | 298 条基线早已过期，但"绿≠可复现"的方法论仍有效 |
| `PROJECT_COMPLETION_ASSESSMENT.md` | 审计证据 | 2026-11-02 | 全项目完成度 + 地形/气候/灾害/植被测试覆盖 | 审计证据 | 594/594 基线过期；完成度百分比为当时口径 |
| `TERRAIN_DYNAMICS_INTEGRATION.md` | 审计证据 | 2026-11-02 | 地形动态字段（植被/盐碱/侵蚀/板块）接入点与风险 | 审计证据 | 只读评估 |
| `TERRAIN_ECONOMY_COUPLING_AUDIT.md` | 审计证据 | 2026-11-02 | 地形/气候→食物/人口/经济耦合断点（食物产出环节缺失） | 审计证据 | 只读审计，含 `文件:行` 证据 |
| `VERIFY-round1.md` | 审计证据 | 2026-10-31 | Round1 8 项 P0 修复的独立复核（305/305） | 审计证据 | 一次性复核记录 |
| `VERIFY-round2.md` | 审计证据 | 2026-10-31 | Round2 修复有界复核 + PlayMode 真绿（306/306） | 审计证据 | 同上 |
| `VERIFY-team-round.md` | 审计证据 | 2026-10-31 | 复核原子写/经济守恒/全链路测试（324/324） | 审计证据 | 同上 |
| `VERIFY-team-round2.md` | 审计证据 | 2026-10-31 | 复核 .bak 恢复与商队滞留退役（331/331） | 审计证据 | 同上 |
| `VERIFY-team-round3.md` | 审计证据 | 2026-10-31 | 复核 P2 收口 + 角色入档（343/343） | 审计证据 | 同上 |
| `VERIFY-team-round4.md` | 审计证据 | 2026-10-31 | 复核 asmdef 去旧依赖 / bodyParts+bonds / 信仰入档（357/357） | 审计证据 | 同上 |
| `VERIFY-team-round5.md` | 审计证据 | 2026-10-31 | 复核场景迁移/UI 可点击/确定性重算/旧程序集删除 | 审计证据 | 同上 |
| `VERIFY-team-round6.md` | 审计证据 | 2026-10-31 | 复核新系统收口/Succession 端到端/seed 接线/读档重绑 | 审计证据 | 同上 |
| `VERIFY-team-round7.md` | 审计证据 | 2026-11-01 | 终局复核（破碎化/seed16/摄政/架构 B1–B5/TMP 清理） | 审计证据 | 同上 |

## 4. `docs/design/`（域设计 / 语料 / 审计）

| 路径 | 类别 | 日期 | 一句话职责 | 处置 | 依据 |
|---|---|---|---|---|---|
| `DECISIONS_M1_M2_M3.md` | 域设计 | 2026-11-09 | 裁决总账：M1 头衔/官职本体、M2 四层 ActionGate、M3 角色域基线 + 批次自审 | 现行（canonical） | PLAN_INDEX §1；M1/M2/M3 为"唯一门禁契约/无硬 kind"等最终裁决 |
| `TITLE_INHERITANCE_PLAN.md` | 域设计 | 2026-11-08 | 头衔授予/继承三模式（None/RealmLaw/OwnLaw）、位阶 §11、疾病 C 方案、机制优先 §10 | 现行（canonical） | PLAN_INDEX §1 + §4（位阶/删 rank 顺序/疾病/数值四行裁决引用本文） |
| `TITLE_SEMANTIC_FIELD_SCHEMA.md` | 域设计 | 2026-11-09 | 完整头衔 = 指代 + 称谓：术语表、指代/称谓字段组、分布、语言学接口 | 现行（canonical） | PLAN_INDEX §1；`TITLE_REFERENT_APPELLATION_MODEL` 明示以本文为现行字段 |
| `FAITH_AS_CULTURE_MODEL.md` | 域设计 | 2026-11-09 | 信仰层级：宗教固定 / 中间层可选 / 传统为主运行单位；只借分裂+挂靠 | 现行（canonical） | PLAN_INDEX §1 + §4 全部信仰行 |
| `FAITH_DOCTRINE_CLASSIFICATION.md` | 域设计 | 2026-11-06 | 教义内容层：两轴归类、槽位、组织 5 阶段、圣人/神祇注册表、教统与思想两图 | 现行（canonical） | PLAN_INDEX §1（"内容层，仍有效"） |
| `CHARACTER_PLAN_SEAM.md` | 域设计 | 2026-11-07 | 角色域→大规划的接缝：四层门禁、八域接缝表、代码缺口、P0 清单 | 现行（canonical） | PLAN_INDEX §1（注明"需按 M3 进度复核"） |
| `TITLE_REFERENT_APPELLATION_MODEL.md` | 域设计 | 2026-11-09 | 头衔字段模型演进（kind→指代/称谓/绑定），含四次自我修正 | 部分有效 | 具体被推翻处：`kind` 列表与 `rank` 部分已被 M1/`TITLE_INHERITANCE_PLAN` §11 取代（文档首行自述 + PLAN_INDEX §2） |
| `SUPREME_AUTHORITY_MODEL.md` | 域设计 | 2026-11-05 | 最高权力不能压成 Monarchy/Republic；多头政治、东西分治、家产制、四轴 | 部分有效 | M2 §2.3 采其四轴（权力结构/法理化/共治/继承产生）；其"建议模型"里对 `SupremeSovereignty` 二元字段的处置已由 M2 定案取代 |
| `HONORIFIC_NICKNAME_SYSTEMS.md` | 域设计 | 2026-11-05 | 头衔/尊号/绰号三套系统不可合并，含代码问题与迁移 | 部分有效 | 尊号/绰号拆分被 M3 §3.1 授权沿用；"头衔"本体与 `TitleKind` 相关判断归 M1，本文该部分不作数 |
| `LANGUAGE_NAMING_IDENTITY.md` | 域设计 | 2026-11-02 | 语音/语义命名、语言差异意识、族群与文化/认同分离双机制 | 部分有效 | 具体被推翻处：§4.6b"`1018 民族国家` 在民族主义之后"→ `UNIFIED_MASTER_PLAN` §4.1 定民族国家是 `PolityForm`、1018 已移除 |
| `GOODS_CLASS_ENVIRONMENT_MODEL.md` | 域设计 | 2026-11-04 | 物产/阶层需求/环境修正：物产是具体 GoodsDef，需求 5×8 | 部分有效 | 具体被推翻处：§六"对原计划 E0–E7 的修订"所依赖的完成度数字已过期；修正本身仍与 `UNIFIED_MASTER_PLAN` §4.5 同口径 |
| `WORLD_SANDBOX_AND_SYSTEMS_PLAN.md` | 计划 | 2026-11-02 | 沙盒世界生成、物种 DNA、聚落影响力、法令系统现状审计 + 分阶段实施 | 部分有效 | 具体被推翻处：§3/§8 对 `LawComposition`（7 维/29 组）的"组只选一条法律+法令条件"方向，已被三层法律域/族/组（`LawConstraints.cs`）与 M2 门禁取代 |
| `GEOLOGICAL_CLIMATE_DYNAMICS_PLAN.md` | 计划 | 2026-11-02 | 项目完成度评估 + 动态地质/气候系统设计（E0–E7 分阶段） | 部分有效 | 具体被推翻处：§一 完成度表（594/594、分模块百分比）已过期；§四 设计未见落地证据（未核代码） |
| `GOODS_RECIPE_GRAPH.md` | 域设计 | 2026-11-04 | 配方表（当前 11+ 条）+ 与革新的连接 | 部分有效 | 具体被推翻处：§5 配方字段/§6 规则已被同日 `INNOVATION_RECIPE_SYSTEM.md` §1 schema 取代；配方内容本身为快照 |
| `INNOVATION_CATALOG_BLUEPRINT.md` | 计划 | 2026-11-05 | 革新目录蓝图 V3：五条硬规则、14 Domain 分类、词汇审计表、候选池、变体 | 部分有效 | 具体被推翻处：顶部"实现状态（187 条 schema=2、656/656）"为文档自述、未核代码，且与 `PLAN_INDEX` §5"186 条内容/四层空转"口径不一；§1–§7 规则仍为革新口径 |
| `CIV_COGNITION_TITLE_CURRENCY.md` | 语料研究 | 2026-11-02 | 认知驱动命名、头衔与地理分离、官员能力因子、地区化货币、灾害 | 仅语料/参考 | PLAN_INDEX §2 列为研究；头衔部分已被 TITLE_* 系列细化 |
| `REFERENCE_PROJECTS_ANALYSIS.md` | 语料研究 | 2026-11-02 | 参考项目（Earth/TTHTOM/FantasyMapSimulator/civ-atlas 等）实现参考与不照搬项 | 仅语料/参考 | 只读分析；借鉴边界已由 `architecture/REFERENCE_GAMES_UNIFIED_PLAN.md` 收束（该文 §2-8 已过期） |
| `CHINESE_TITLE_ETYMOLOGY.md` | 语料研究 | 2026-11-08 | 王/公/侯/伯/子/男/后 的字源与原始权力来源 | 仅语料/参考 | PLAN_INDEX §2（语料）；结论支持 `TITLE_ETYMOLOGY_AND_AUTHORITY` 的"位阶非数值轴" |
| `COMPARATIVE_TITLE_CORPUS.md` | 语料研究 | 2026-11-08 | 西欧/神罗/北欧/近东/印度/希腊/美洲 头衔样本集 + 传播四路径 | 仅语料/参考 | PLAN_INDEX §2（语料） |
| `TITLE_APPELLATION_TYPOLOGY.md` | 语料研究 | 2026-11-05 | 跨文明称谓组合类型与开放语料（指代≠称谓） | 仅语料/参考 | PLAN_INDEX §2（语料） |
| `TITLE_ETYMOLOGY_AND_AUTHORITY.md` | 语料研究 | 2026-11-08 | 词源=权力来源，故 rank 浮点应删；位阶用偏序 | 仅语料/参考 | PLAN_INDEX §2（语料）；其结论已被 `TITLE_INHERITANCE_PLAN` §11 吸收为裁决 |
| `FAITH_CK3_TERM_MAPPING.md` | 语料研究 | 2026-11-05 | CK3 宗教术语 → 本项目对应/改名/不做系统的判定 | 仅语料/参考 | PLAN_INDEX §2（研究） |
| `FAITH_LONG_CONTENT_CRITIQUE.md` | 语料研究 | 2026-11-05 | 对"五层信仰体系长文"的逐项批评与替代模型 | 仅语料/参考 | PLAN_INDEX §2（研究） |
| `DATA_FORGE_INTEGRATION.md` | 工具说明 | 2026-11-02 | 游戏侧如何消费 Data Forge 产物（已接/未接、门禁） | 仅语料/参考 | PLAN_INDEX §2（工具接入说明，非计划）；§1 接入状态为当日快照，未核代码 |
| `UNITY_MCP_SETUP.md` | 工具说明 | 2026-10-31 | Unity 编辑器接入方式（@opdsh/unity-plugin、Pipeline HTTP API） | 仅语料/参考 | PLAN_INDEX §2（工具接入说明） |
| `GATHERING_OUTPOST_VEHICLE_MODEL.md` | 域设计 | 2026-11-03 | 软前置物理阈值、资源三层、据点采集、载具层 | 仅语料/参考 | 设计说明，未被现行文档引用；软前置口径与 `UNIFIED_MASTER_PLAN` §4.2 一致但非其权威来源 |
| `NAVY_ENTITY.md` | 域设计 | 2026-11-04 | Navy 作为独立实体、舰船为基本构成 | 仅语料/参考 | 设计说明；实现状态未核 |
| `TECH_ARTIFACT_INNOVATION_MODEL.md` | 域设计 | 2026-11-03 | 工艺—型制—能力三层模型与技术史中性命名 | 仅语料/参考 | 分类方法参考；革新条目以 `INNOVATION_CATALOG_BLUEPRINT` 为准 |
| `TECH_HISTORY_NOTES.md` | 语料研究 | 2026-11-03 | 技术史笔记：发明/创新/扩散、工艺—型制—能力序列 | 仅语料/参考 | 依据材料 |
| `TECH_TERM_DISAMBIGUATION.md` | 语料研究 | 2026-11-04 | 船/攻城炮/火器/弩 的名称辨析 | 仅语料/参考 | 依据材料 |
| `INNOVATION_VS_ITEM_CLASSIFICATION.md` | 域设计 | 2026-11-04 | 革新 vs 物品/物产/单位的判定标签与分类表 | 仅语料/参考 | 判定方法参考；规则已并入 `UNIFIED_MASTER_PLAN` §4.2/§3 |
| `INNOVATION_REDESIGN_RESEARCH.md` | 语料研究 | 2026-11-02 | 革新术语研究与重设计 V2（术语表、改名/拆分规则） | 仅语料/参考 | 文档首行自述"保留为术语研究底稿，条目以蓝图为准" |
| `INNOVATION_RECIPE_SYSTEM.md` | 域设计 | 2026-11-04 | MC 式数据驱动配方 schema/API/模组化 | 仅语料/参考 | 配方 schema 参考实现说明（`Recipes.json`） |
| `INNOVATION_DEDUPE_AUDIT.md` | 审计证据 | 2026-11-03 | 现有 185 条革新与候选池的去重/粒度审计 | 审计证据 | 只做审计与细化，自述"不改代码" |
| `INNOVATION_AUDIT_ROUND2.md` | 审计证据 | 2026-11-04 | 非宗教/思想域的革新审计（186 条、84 条有具体引用） | 审计证据 | 计数字为快照 |
| `INNOVATION_UNLOCK_REPORT.md` | 审计证据 | 2026-11-04 | 按解锁目标系统拆分的革新统计 | 审计证据 | 同上 |
| `INNOVATION_COMPLETION_REPORT.md` | 审计证据 | 2026-11-04 | 革新/物资/工艺整改报告（187 条 schema=2、645/645） | 审计证据 | 文档自述，未核代码；不得当作"革新已完成" |
| `LEGAL_SYSTEM_FULL_AUDIT_AND_PLAN.md` | 计划 | 2026-11-05 | 法律/法令全面审计 + 统一规划 + 根本化 Fundamentalization | 仅语料/参考 | PLAN_INDEX §5 明列"保留原位、不作结论"；"根本化"未被现行口径采纳 |
| `LEGAL_SYSTEM_UNIFICATION.md` | 域设计 | 2026-11-04 | 多层法律秩序（政权/领主/特许城市）+ 冲突裁决 + 规范字段 | 仅语料/参考 | PLAN_INDEX §5 同列；口径已被三层域/族/组与 M2 门禁覆盖 |
| `LAW_DECREE_AUDIT_AND_REDESIGN.md` | 计划 | 2026-11-02 | 法律/法令审计 + 精确化（公民权/民主/法源）+ 缺口矩阵 | 仅语料/参考 | PLAN_INDEX §5 同列；顶部自述"本轮大头是革新，法律仅范围参考" |
| `SEVEN_DIMENSIONS_LAW_STUDY.md` | 语料研究 | 2026-11-05 | 现有七维法律结构（`LawComposition`）逐维研究 | 仅语料/参考 | PLAN_INDEX §5 同列 |
| `MILITARY_LAW_TAXONOMY_AUDIT.md` | 语料研究 | 2026-11-04 | 军事法域/族/组/项审计 + 维度分离、边境管制、兜底 | 仅语料/参考 | PLAN_INDEX §5 同列 |
| `POLITY_TITLE_CONTRACT_MODEL.md` | 域设计 | 2026-11-05 | 政治实体/头衔/土地/契约/组织/法律统一本体（Polity、复合政治体…） | 仅语料/参考 | PLAN_INDEX §2（参考）；头衔本体归 M1 |
| `RELATION_TAXONOMY_AND_TITLE.md` | 域设计 | 2026-11-05 | 关系六类（按端点）、君合/联统、复合政治体、头衔缺口 | 仅语料/参考 | PLAN_INDEX §2（参考） |
| `TOTAL_SYSTEM_SYNTHESIS.md` | 域设计 | 2026-11-05 | 大系统综合规划：原则、核心本体、关系/头衔/法律/信仰/历法 | 仅语料/参考 | PLAN_INDEX §5（未逐份核对、默认保留）；其"无硬 kind"原则已被 M1 采纳，但本体表未含 Character |
| `UNIFIED_DESIGN_V2.md` | 域设计 | 2026-11-02 | 统一设计 V2：认同/语言/文化/头衔/物产/革新 + 术语表 | 仅语料/参考 | PLAN_INDEX §5（未逐份核对） |
| `VOCABULARY_TRACEABILITY_MATRIX.md` | 域设计 | 2026-11-05 | 术语→代码/计划落点对照矩阵 | 仅语料/参考 | PLAN_INDEX §2（参考） |

## 5. 其余目录 + 清单外新增（EXTRA）

| 路径 | 类别 | 日期 | 一句话职责 | 处置 | 依据 |
|---|---|---|---|---|---|
| `docs/ARCHITECTURE_2_0_SPLIT_PRINCIPLES.md` | 工具说明 | 2026-09-24 | 拆分尺度八维度 + 拆分硬标准（不新增抽象除非解决真问题） | 仅语料/参考 | 原则性说明；未与现行规则冲突，但无裁决力 |
| `docs/README_MAIN_DETAILED_REVIEW_01.md` | 审计证据 | 2026-09-26 | main/README 逐段复核记录（不得整体视作当前设计） | 审计证据 | 复核记录 |
| `docs/research/2026-10-27-world-simulation-resource-index.md` | 语料研究 | 2026-10-27 | 外部数据集索引（版本/许可/可再分发/覆盖度） | 仅语料/参考 | 数据来源与许可依据；含 `UNVERIFIED` 项 |
| `docs/research/character-2026-11-07/01-character-code-audit.md` | 审计证据 | 2026-11-07 | 角色/家族代码现状（三层属性 0.5 层、专精随机、疾病空转） | 审计证据 | 只读审计，含 `文件:行` |
| `docs/research/character-2026-11-07/02-action-gating-audit.md` | 审计证据 | 2026-11-07 | 宣战/决策是否绑角色属性；"无政治主体门禁"结论 | 审计证据 | 只读审计；M2 据此立四层门禁 |
| `docs/research/character-2026-11-07/03-plan-docs-inventory.md` | 审计证据 | 2026-11-07 | 18+4 份设计文档盘点：大规划无角色包、本体无 Character | 审计证据 | 只读勘察；本表的先例 |
| `docs/research/character-2026-11-07/04-b1-audit.md` | 审计证据 | 2026-11-07 | B1 批次（头衔契约/ActionGate/纯数据测试）静态复核 | 审计证据 | 自述未跑编译与测试 |
| `docs/research/character-2026-11-07/05-batch-audit.md` | 审计证据 | 2026-11-07 | B2.0–M3.6 批次（头衔归一/共持/门禁接线/属性入档）静态复核 | 审计证据 | 同上；"694 全绿"按 Lead 记录采信 |
| `docs/research/ck3-religion-2026-11-06/01-doctrine-system.md` | 语料研究 | 2026-11-06 | CK3 1.20.1 教义系统端到端拆解（religion/faith/rite 三层装配） | 仅语料/参考 | 依据材料；`FAITH_DOCTRINE_CLASSIFICATION` 的数据来源 |
| `docs/research/ck3-religion-2026-11-06/02-project-faith-state.md` | 审计证据 | 2026-11-06 | 项目信仰系统现状：内容骨架 ~1900 行、无玩法回路、5 稿互相推翻 | 审计证据 | 只读勘察 |
| `docs/superpowers/specs/2026-09-27-habitation-site-layered-model-design.md` | 域设计 | 2026-09-27 | 栖居点分层模型（HabitationSite/SiteMode/设施形制/Anchor 边界） | 现行（canonical） | 代码已落地：`World.Settlement/HabitationSite.cs`、`SiteModeKeys.cs`、`SettlementPersistence.cs` 均存在并被调用（grep 命中）；无后继取代文档 |
| `docs/superpowers/plans/2026-09-27-habitation-site-layered-model-plan.md` | 计划 | 2026-09-27 | 上述设计的迁移实施计划与阶段验收 | 部分有效 | 具体被推翻处：阶段清单与"待迁移/风险"已由实现完成（`HabitationSiteMigration`/`SaveJsonMigration` 在库），不再待执行 |
| `docs/superpowers/specs/2026-10-25-law-module-completion-design.md` | 域设计 | 2026-10-25 | 法律模块阶段一收尾口径：组/族/域归集方向、枚举序列化纪律、不做什么 | 部分有效 | 具体被推翻处：§3"工作树静态不可编译"等基线已过期；"本轮只处理阶段一"的边界已被后续法律/M1 批次越过 |
| `docs/superpowers/plans/2026-10-25-law-module-completion-plan.md` | 计划 | 2026-10-25 | 法律模块收尾与接线实施计划（挂组、组级效果、指纹、审判消费） | 部分有效 | 具体被推翻处：基线 `HEAD f1ca764` 与"四程序集不可编译"过期；其目标被 `LawDomain/LawFamily/LawGroup` 落地部分实现（未核完成度） |
| `docs/superpowers/plans/2026-10-27-dataset-integration-plan.md` | 计划 | 2026-10-27 | 外部数据集四层接入（在线/设计期/构建期/运行时）+ 许可闸门 | 部分有效 | 具体被推翻处：基线"EditMode 341 条"过期；许可闸门与 lock 已另有 `DATA_FORGE_INTEGRATION` 描述（文档自述，未核代码） |
| `docs/ui/POPULATION_COMPOSITION_PANEL.md` | UI规格 | 2026-10-29 | 人口构成面板：显示对象、关系、数据来源、结构 | 仅语料/参考 | UI 规格；实现状态未核，无现行文档引用 |
| `docs/plan-v3/audit/A-world-foundation.md`【EXTRA】 | 审计证据 | 2026-11-09 | A 域：世界地基审计 | 现行（canonical） | plan-v3 八域审计链，`SYSTEM_MASTER_PLAN` §9 证据来源；本表未逐行复核其内容 |
| `docs/plan-v3/audit/B-settlement-population.md`【EXTRA】 | 审计证据 | 2026-11-09 | B 域：聚落/行省/人口审计 | 现行（canonical） | 同上 |
| `docs/plan-v3/audit/C-character-genetics.md`【EXTRA】 | 审计证据 | 2026-11-09 | C 域：角色/家族/遗传/心理/人际关系审计 | 现行（canonical） | 同上 |
| `docs/plan-v3/audit/D-polity-law-title.md`【EXTRA】 | 审计证据 | 2026-11-09 | D 域：政体/法律/头衔审计 | 现行（canonical） | 同上 |
| `docs/plan-v3/audit/E-culture-faith.md`【EXTRA】 | 审计证据 | 2026-11-09 | E 域：文化/信仰/宗教/传统/语言/思潮/族群审计 | 现行（canonical） | 同上 |
| `docs/plan-v3/audit/F-economy-goods.md`【EXTRA】 | 审计证据 | 2026-11-09 | F 域：经济/物资/生产/贸易/货币/税收/工艺/建筑/装备/阶层需求审计 | 现行（canonical） | 同上 |
| `docs/plan-v3/audit/G-military-diplomacy.md`【EXTRA】 | 审计证据 | 2026-11-09 | G 域：军事/战争/单位/行军/寻路/海军/外交/关系/战争法理审计 | 现行（canonical） | 同上 |
| `docs/plan-v3/audit/H-innovation-spine.md`【EXTRA】 | 审计证据 | 2026-11-09 | H 域：革新/科研/计划/AI/事件/调度/存档主干/UI 入口审计 | 现行（canonical） | 同上 |

---

## 6. 会误导人的文档 Top 10（按危害排序）

1. `docs/architecture/UNIFIED_MASTER_PLAN.md` — 自称"当前唯一执行口径"，还带整张 ✅ WBS 与 Phase 0–5；最像总纲，实际排期与状态表整块过期（真正入口已是 `SYSTEM_MASTER_PLAN`）。
2. `docs/architecture/CURRENT_ARCHITECTURE.md` — 名字就叫"当前架构"，第 82/123 行把 **552/552、9/9** 写成"Round 35 基线"，会被直接抄成测试基线（实测已是 EditMode 706/706）。
3. `docs/architecture/SYSTEM_OWNERSHIP.md` — 自称"仓库根最新口径"，同一组过期基线；且"遗留 1–6"读起来像当前缺陷清单。
4. `docs/PLAN_INDEX.md` — 自称"唯一入口"，其 §5 末句"其余文档仍按未逐份核对处理、默认不删"最容易被读成"那些文档还都算数"。
5. `docs/architecture/MASTER_COMPLETION_PLAN.md` — 整表 ✅ + 明确 DoD，孤立看会得出"项目已按 W1–W12 完成"的结论。
6. `docs/design/INNOVATION_COMPLETION_REPORT.md` — 标题即"整改报告"，正文"187 条 schema=2、645/645"极易被当成革新已就绪（文档自述，未核代码；`PLAN_INDEX` §5 记四层空转）。
7. `docs/architecture/ARCHITECTURE_2_0_LAW_OFFICE_NAME_REGION_MODEL.md` — 1044 行、层级最完整，开头"代码层尚未改动"已经过期，头衔部分又被 M1 推翻；照它写代码会踩两处。
8. `docs/design/LEGAL_SYSTEM_FULL_AUDIT_AND_PLAN.md` — 537 行、含"统一架构建议 + 根本化 + 重构方案"，最像法律现行设计，实际 `PLAN_INDEX` §5 只当历史保留。
9. `docs/design/WORLD_SANDBOX_AND_SYSTEMS_PLAN.md` — 1407 行、带"实施状态（2026-10-30）""建议的下一步"，读起来像在跑的路线图；其法律方向已被三层域/族/组取代。
10. `docs/architecture/ARCHITECTURE_2_0_DOMAIN_AUDIT_29.md` / `_30.md` — 英文、带 commit 与调用图，权威感最强，却是**特定 commit `cf5486` 的快照**，其"未接线"结论不能当当前事实。

> 反例提示：`docs/audit/VERIFY-*.md`、`docs/audit/A–D` 不在此列——它们自报"只读、快照时间、基线"，误用风险低，但**同样不能当现状**。

## 7. 重复 / 近重复（同主题竞争文档）

| 主题 | 竞争文档 | 建议保留 |
|---|---|---|
| 项目总纲/排期 | `SYSTEM_MASTER_PLAN.md`（v3）、`UNIFIED_MASTER_PLAN.md`（V2）、`MASTER_COMPLETION_PLAN.md`（W1–W12）、`PLAN_INDEX.md` | 只留 `SYSTEM_MASTER_PLAN.md`；`PLAN_INDEX` 降为索引；另两份留档不引 |
| 架构现状 | `CURRENT_ARCHITECTURE.md`、`SYSTEM_OWNERSHIP.md`、`audit/ARCH-COMPLETION-PLAN.md` | 留 `CURRENT_ARCHITECTURE.md`（并重测基线）；另两份只作历史 |
| 头衔字段模型 | `TITLE_SEMANTIC_FIELD_SCHEMA.md`、`TITLE_REFERENT_APPELLATION_MODEL.md` | 留 `TITLE_SEMANTIC_FIELD_SCHEMA.md`（后者 `kind`/`rank` 已被 M1 推翻） |
| 头衔语料 | `COMPARATIVE_TITLE_CORPUS.md`、`CHINESE_TITLE_ETYMOLOGY.md`、`TITLE_APPELLATION_TYPOLOGY.md`、`TITLE_ETYMOLOGY_AND_AUTHORITY.md` | 留 `COMPARATIVE_TITLE_CORPUS.md`（样本）+ `TITLE_ETYMOLOGY_AND_AUTHORITY.md`（结论）；另两份并入 |
| 信仰/宗教 | `FAITH_AS_CULTURE_MODEL.md`、`FAITH_DOCTRINE_CLASSIFICATION.md`、`FAITH_CK3_TERM_MAPPING.md`、`FAITH_LONG_CONTENT_CRITIQUE.md`、`research/ck3-religion-*/01,02` | 留前两份（现行层级 + 内容层）；其余当语料/证据 |
| 法律体系 | `LEGAL_SYSTEM_FULL_AUDIT_AND_PLAN.md`、`LEGAL_SYSTEM_UNIFICATION.md`、`LAW_DECREE_AUDIT_AND_REDESIGN.md`、`SEVEN_DIMENSIONS_LAW_STUDY.md`、`MILITARY_LAW_TAXONOMY_AUDIT.md`、`ARCHITECTURE_2_0_LAW_OFFICE…§4` | 无一可直接留作口径；结构以代码 `LawConstraints.cs` + M2 门禁为准，文本只留 `LAW_OFFICE…§4` 作设计溯源 |
| 革新 | `INNOVATION_CATALOG_BLUEPRINT.md`、`INNOVATION_REDESIGN_RESEARCH.md`、`INNOVATION_DEDUPE_AUDIT.md`、`INNOVATION_AUDIT_ROUND2.md`、`INNOVATION_UNLOCK_REPORT.md`、`INNOVATION_COMPLETION_REPORT.md`、`INNOVATION_RECIPE_SYSTEM.md`、`INNOVATION_VS_ITEM_CLASSIFICATION.md`、`TECH_ARTIFACT_INNOVATION_MODEL.md`、`TECH_HISTORY_NOTES.md`、`TECH_TERM_DISAMBIGUATION.md` | 口径留 `INNOVATION_CATALOG_BLUEPRINT.md`；分类留 `TECH_ARTIFACT_INNOVATION_MODEL.md` + `INNOVATION_VS_ITEM_CLASSIFICATION.md`；其余作证据/语料 |
| 政体/头衔本体 | `DECISIONS_M1_M2_M3.md`、`TOTAL_SYSTEM_SYNTHESIS.md`、`POLITY_TITLE_CONTRACT_MODEL.md`、`RELATION_TAXONOMY_AND_TITLE.md`、`SUPREME_AUTHORITY_MODEL.md` | 只留 `DECISIONS_M1_M2_M3.md`（唯一裁决）；`SUPREME_AUTHORITY_MODEL` 仅保留其被 M2 采纳的四轴 |
| 角色 | `CHARACTER_PLAN_SEAM.md`、`research/character-2026-11-07/01–05` | 留 `CHARACTER_PLAN_SEAM.md`；research 五份作证据 |
| Architecture 2.0 领域边界 | `ARCHITECTURE_2_0_DOMAIN_AUDIT_01–30`、`DOMAIN_BOUNDARY_MATRIX`、`FIELD_MIGRATION_MATRIX`、`DOMAIN_MODEL` | 若日后重启 2.0，只读 `DOMAIN_BOUNDARY_MATRIX.md` 一张；其余按证据留档 |
| 数据集接入 | `research/2026-10-27-world-simulation-resource-index.md`、`design/DATA_FORGE_INTEGRATION.md`、`superpowers/plans/2026-10-27-dataset-integration-plan.md` | 留 `DATA_FORGE_INTEGRATION.md`（游戏侧）+ `resource-index`（许可/来源） |

## 8. 处置数量核对

| 处置 | 数量 | 说明 |
|---|---:|---|
| 现行（canonical） | 9 | PLAN_INDEX §1 的 7 份 + `plan-v3/audit/_FORMAT.md` + `superpowers/specs/2026-09-27-habitation-site-layered-model-design.md`（代码已落地，见 §5） |
| 已取代 | 3 | `UNIFIED_MASTER_PLAN`、`MASTER_COMPLETION_PLAN`、`README_ALIGNMENT` |
| 部分有效 | 21 | 每条在"依据"列写明具体被推翻的条目 |
| 仅语料/参考 | 36 | 语料、技术史、法律/革新历史设计、工具说明、2.0 提案 |
| 审计证据 | 60 | A–D、VERIFY×9、2.0 Audit 01–30、革新四份报告、research 六份等 |

> 合计 129，与清单一致。`docs/archive/**` 未计；清单外另见 §5 的 9 份新增（`SYSTEM_MASTER_PLAN` + plan-v3 八域审计）。
