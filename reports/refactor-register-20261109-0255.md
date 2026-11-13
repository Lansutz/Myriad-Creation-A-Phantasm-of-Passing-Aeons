# 重构中系统登记 + 参照系接入

> 2026-11-09 02:55（Asia/Shanghai）。输入：用户说明"法律和信仰宗教在大改；社会已拆成 4 个维度，需求相关要调整；有些计划里提到借鉴的游戏和想要实现的功能"。

---

## 1. 处理原则（已写进宪章 §7 与总纲 §7）

1. **重构中的系统只登"方向 + 接口契约"，不登"完成度"**。此前审计给的"半跑/空转"是**快照**，随重构失效。
2. **里程碑对它们只做两件事**：固定接口契约（谁能读、谁能命令、语义是什么）、接下游消费者。**不改内部形态**。
3. **不计入完成度统计**，避免拿快照当排期依据。

## 2. 三块重构的登记

| 系统 | 正在改什么 | 代码快照 | 里程碑动作 |
|---|---|---|---|
| **法律** | 域/组/项三层、位阶与冲突裁决、根本化 | 主线在跑（唯一深交互）；双源单向下行、司法链空转、死字段 | 固定 `ILawQuery/ILawCommand/IDecreeCommand` + 把 `GovernmentPanel` 的改动回写到 `LawComposition` |
| **信仰 / 宗教** | 传统为主 vs 宗教两级（D8 待裁决） | CK3 式两级在跑；传统无运行时对象；**8 条革新已被 `InnovationReviewPolicy.IsFrozen` 冻结**（602/604/601/809/810/958/982/1017） | 固定"宗教身份必填 + 查询契约"；内容重构不在本计划内推进 |
| **社会四维 + 需求** | 把社会拆成 4 个维度；需求要重新选轴 | 四轴并存：`SocialClass`(5)/`SocialSubclass`(12)/`EconomicClass`(8)/`SocialStratum`(5)；**`EconomicClass` 与 `SocialSubclass` 取值重叠**（Freeholder/Tenant/Serf/HiredLaborer/Slave 同名）；`ClassNeedsSystem` 仍只按 5 大类评 8 个需求维 | 固定"阶层查询 + 需求报告"的读取契约；**不锁需求的轴** |

### 2.1 社会四维的正确对位（用户澄清后更正）

用户给的四个词是 **阶级 / 阶层 / 身份 / 职业**。代码里这四维已经存在，就是 `IdentityProfile` 的四个字段：

| 维 | 枚举 | 取值 |
|---|---|---|
| **阶级** | `EconomicClass` | 8：Freeholder/Tenant/Serf/HiredLaborer/Slave/Lord/SelfEmployed/Vagrant |
| **阶层** | `SocialStratum` | 5：Upper/UpperMiddle/Middle/LowerMiddle/Lower |
| **身份** | `SocialIdentityFlags`（位标志） | Royalty/Noble/Clergy/Citizen/Freedman/Foreigner/Outlaw |
| **职业** | `OccupationFlags`（位标志） | 14：Farmer/Artisan/Merchant/Scholar/Soldier/Official/Priest/Miner/Mariner/Pastoralist/Physician/Teacher/Laborer |

legacy `SocialClass`(5) 与 `SocialSubclass`(12) 是**映射源**（`LegacyIdentityMapping.FromLegacy`），不是第五、第六维。

**现状（数据层已建好，决策层没用）**：

- `PopulationIdentity.Resolve` 在四轴全为默认值时回退到 legacy 映射；`SocietyManager.cs:153` 已按**阶级/阶层**聚合进 `RealmSociety`（但两个字典都 `[NonSerialized]`，不入档）。
- **身份/职业没有任何判定在读**：`PopulationComposition.ByOccupation()` 只被 `PopulationCompositionPanelModel` 用，而该模型零接线（K 域）；`SettlementData.composition` 零写零读。
- **决策层仍只用 legacy 大类**：需求 `ClassNeedsSystem.EvaluateClass(SocialClass)` + `ClassNeedWeightTable`（按 5 大类）；分化 `SocialDifferentiation` 也按大类。
- ⇒ 重构的核心问题就是**需求按哪一轴（或哪几轴交叉）来评**，以及四轴的聚合/入档/查询契约。这个我不代定。

## 3. 参照系（`REFERENCE_GAMES_UNIFIED_PLAN`）接入宪章 §6

已把该文档的**借鉴边界**收进宪章：Earth Simulator / FantasyMapSimulator / Things That Happen on the Map / civ-atlas / CK3 / Victoria 3 / EU4 / Stellaris 各自"借什么、不借什么"，并抽出 6 条落进代码的硬约束：

1. 数据驱动（JSON 内容，代码只解析/求值）
2. 资源三层（露天/坑采/深井，不白送深井）
3. 硬前置只表达"不会就不能"；软前置是能力阈值，不退化成材料阶梯
4. 确定性（seed 派生 RNG + 长跑 harness）
5. 性能（环境 tick 分条 + dirty + 跨帧预算）
6. 冻结域（宗教/信仰/思想，8 条革新冻结）

> 这份文档与总纲的关系：它是**设计参照**（方向、边界、借什么），不是状态表；总纲引用它的原则，不用它的完成度。

## 4. 对总纲的改动

- §3.0 系统清单改为 **16 组**，并把"重构中"块置于最前（警示性质）。
- 新增 **§3.0.1 重构中的系统**（三行登记表 + 规则）。
- §7 横向门禁新增**重构例外**：三块不计完成度，只验接口与下游。
- 宪章新增 **§6 参照系与借鉴边界**、**§7 重构中系统的处理约定**。

## 5. 未验证

- 未看到重构后的社会四维设计稿（若已有文档请指明路径，我接进去）。
- 未核实 `SocialStratum` 是否在重构中要并入 `EconomicClass`。
- 未核实信仰重构的目标模型（D8 仍待裁决）。

## 6. 产物

| 路径 | 说明 |
|---|---|
| `docs/plan-v3/DESIGN_CHARTER.md` | 新增 §6 参照系、§7 重构约定 |
| `docs/plan-v3/SYSTEM_MASTER_PLAN.md` | §3.0 改 16 组、新增 §3.0.1、§7 重构例外 |
| 本报告 | 登记与依据 |
