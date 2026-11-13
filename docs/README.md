# 文档架构（唯一入口）

> 2026-11-11。这份文件回答一个问题：**docs 里每个文件是干什么的、谁是权威、旧的在哪。**
> 规则：**新增/修改计划只能改"权威件"（3 份）；子计划必须先挂到 `SYSTEM_MASTER_PLAN` 的 WP 下**，否则不许新建文件。

## 1. 权威件（3 份，其余都不是"计划"）

| 文件 | 职责 | 谁能改 |
|---|---|---|
| `plan-v3/SYSTEM_MASTER_PLAN.md` | **唯一总纲**：系统清单 S1–S18、状态矩阵、里程碑 M0.5–M10、DAG、DoD、证据索引 | 只有里程碑/WP 变化时改 |
| `plan-v3/DESIGN_CHARTER.md` | 设计宪章：11 原则 + 4 条质量轴 + 术语禁令 + 重构约定 | 原则变化时改（很少） |
| `DECISIONS.md`（仓库根） | 决策台账 D1–D32：每条决定 + 理由 + 被否决方案 | 每次做决定追加 |

> 判定：**如果你想知道"我们到底在做什么"，只读这三份。** 其他都是材料。

## 2. 子计划（挂 WP，不是独立计划）

这些文件是某个 WP 的**施工细节**，必须能被总纲的 WP 编号索引到；**不允许自成体系**。

| 文件 | 挂在哪 | 一句话 |
|---|---|---|
| `INTEGRATION_PLAN.md` | WP1–WP6 | 跨系统耦合矩阵 + 六问门禁 + 四轴 + 命名事件 |
| `RELIGION_V2.md` | WP2/WP3 | 宗教 v2：拆 CK3 五缺陷、教义组→档→参数→八接线点、数值三桶 |
| `CHARACTER_COUPLING.md` | WP1/WP4 | 人物互动收敛耦合：授权→代理→执行→结果→追责 |
| `BELIEF_SYSTEM_MODEL.md` | WP2/WP3 | 信仰实体分层（family/religion/denomination/faith/lineage 等） |
| `LAW_LIFECYCLE.md` | WP3 | 法律八态、习惯↔法两座桥、失效与效力四轴 |
| `RELATION_LAYER.md` | 横切 | `EntityRelation` 17 类、投影 vs 契约、六点缺口 |
| `PLAN_COVERAGE.md` | WP1 | PlanType 10→3–4 判决（D32） |
| `ARCHITECTURE_UNFINISHED.md` | WP6 | 架构六项未完成 + CK3 存档模型对照 |
| `FOUNDATION_FIXES.md` | 地基 | F1–F8 修复清单（含已完成的 F1） |
| `COVERAGE_GAP_AUDIT.md` | 待排期 | 覆盖缺口（信息/后勤/价格 P0） |
| `PERF_LESSONS_CK3.md` | 门禁 | 性能基准口径（100 年日耗时/内存/函数热点） |
| `DOC_DISPOSITION.md` | 历史 | 129 份旧文档的去向裁定 |

## 3. 证据与材料（只读，不进计划）

| 目录 | 内容 | 用途 |
|---|---|---|
| `plan-v3/audit/` | A–M 共 13 份域审计（逐条 `file:line`） | 状态矩阵的证据来源 |
| `plan-v3/research/` | 宗教结构与核证（10 原型、8 条已核事实、19 条 URL、学术辨析） | RELIGION_V2 的证据来源 |
| `architecture/REFERENCE_GAMES_UNIFIED_PLAN.md` | 借鉴边界（Earth Simulator/civ-atlas/CK3/V3/EU4…） | 宪章 §6 引用 |

## 4. 归档（只读，**不要再引用**）

| 目录 | 内容 | 为什么归档 |
|---|---|---|
| `archive/2026-11-03_pre-unified-plan-backup/` | 上一轮全量备份（architecture/audit/design） | 历史留痕 |
| `archive/2026-11-09-superseded-plans/` | 被本轮取代的计划（law/innovation/faith 等旧稿） | 已被 plan-v3 覆盖 |
| `archive/2026-11-10_pre-planv3/` | **本轮归档**：`architecture/`（ARCHITECTURE_2_0_* 30+ 份、MASTER_COMPLETION_PLAN、UNIFIED_MASTER_PLAN 等）、`design/`（旧域设计稿）、`audit/`（旧审计） | 与 plan-v3 重复或陈旧；保留只为追溯 |

## 5. 三条维护规则

1. **不新增"计划"文件**：要写新计划 → 先加到总纲 WP 表，再在已有子计划里加章节。确实要新文件，必须在本表登记一行。
2. **删≻改**：子计划被取代时**删除**（移动进 `archive/<日期>_superseded/`），不要留"半新半旧"的副本。
3. **证据与结论分离**：audit/research 只放证据，不下结论；结论只能出现在三份权威件里。

## 6. 当前工具性入口

- 构建/测试：`_fc_verify/build.cmd`（脱离 shell，写 `RUNNING` → `EXIT=<code>`），日志 `_fc_verify/test-run-last.log`，结果 `_fc_verify/test-editmode-last.xml`
- 项目状态：仓库根 `PROJECT.md`（每个动作后更新「当前状态」）
