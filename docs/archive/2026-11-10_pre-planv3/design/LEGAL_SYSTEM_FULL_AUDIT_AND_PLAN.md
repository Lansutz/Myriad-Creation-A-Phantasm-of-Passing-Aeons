# 法律/法令系统全面审计与统一规划

> 2026-11-05。先读代码，再看学术研究，最后给统一规划；不先拍脑袋加法律。
> 覆盖：`Simulation/Laws` 与 `Simulation/LawHost` 全部核心文件、`Laws.json`、`LawComposition`、`DecreePropagationSystem`、`LawEffectRuntime`、`PolityComponentInnovations`。

---

## 1. 现有代码实际做了什么

### 1.1 法律生命周期

```text
已知 knownLawIds
  → 提案 CanPropose（必须已知 + 有提案权）
  → 推行 PendingLaw（同组唯一；旧法继续有效）
  → 到期 SetActiveLaw（LawGroupState.activeLawId）
  → 生效（LawEffectRuntime 合并效果）
```

- `RealmLawState.knownLawIds`：已知/学习到的法律；
- `PendingLaw`：正在推行，`startDay + daysToEnact` 到期才替换；
- `LawGroupState.activeLawId`：已生效法律，一组一条；
- `LawRealmHost.ForceSetLaw`：政变/派系/改革路径绕过正常流程，直接固化并补进 known；
- 没有单独的“已固化”状态字段；`ForceSetLaw`/`LawComposition` 更接近“已固化/结构性法律”。

### 1.2 法令生命周期

```text
DecreeDef
  → CanEnactDecree（成本/权威/合法性/前置法律/革新）
  → ActiveDecree（startDay/endDay）
  → 到期 TickDecreeExpiry 移除
  → 冷却 DecreeCooldown
```

- `DecreeDef` 是临时规范，有持续天数/冷却；
- `DecreeOrder` 是**逐级往下推**的执行单：
  - 从父行政区到子行政区，或从上级聚落到下级聚落；
  - 有 `officialCharacterId`、`courierActorId`、`targetTile`、`totalTravelDays`；
  - `Resolve` 根据官员管理/阴谋、腐败、地方 unrest、合法性计算 `compliance/executionStrength`；
  - 结果：`Enforced / PartiallyEnforced / Delayed / Refused / Profiteering / Failed`。
- 这就是“不是抽象不满，而是通过官员、信使、地方执行来模拟”的机制。

### 1.3 两套法律系统并存

| 系统 | 代码 | 作用 |
|---|---|---|
| `LawComposition` | `LawComposition`、`LawEffects`、`LawSystem` | 政体结构/九维选择，计算 centralization/expansionism 等 |
| `LawDef` catalog | `RealmLawState`、`LawAndDecreeSystem`、`LawEffectRuntime` | 具体法律组、已知/推行/生效、税收/人力/稳定效果 |

问题：

- `LawSystem` 只把 `LawComposition` 的 `centralization` 写回 `realm.centralization`；
- `LawEffectRuntime` 用 `LawDef` 目录合并效果；
- `LawComposition` 与 `RealmLawState` 没有单一来源，可能互相矛盾；
- `LawReform`/`PolityComponentInnovations` 直接把改革映射到 `LawDef`，又改 `realm.laws`，存在双写。

### 1.4 现有“不满”怎么表达

- `LawEffect`：`stabilityPerYear / legitimacyPerYear / unrestPerYear`；
- `LawEffectRuntime` 把年度效果加到 `realm.stability/unrest/lawState.legitimacy`；
- `DecreeOrder.Resolve` 用官员能力、腐败、地方 unrest、合法性决定执行；
- 没有单一“满意度”抽象值，符合“尽量机制化”的方向；
- 但“法律生效后谁不满、为什么不满”还没有按阶层/派系/地区表达，只有全局 stability/unrest。

---

## 2. 学术研究结论

### 2.1 中世纪法律不是单一等级体系

- **法律多元**：王室法、领主法、城市法、教会法、习惯法并存；不是一条“最高法→普通法”的直线。
- **地方习惯与普通法互动**：地方习惯可被吸收/记录/改造，不是简单被中央法覆盖。
- **封建法**：封君-封臣关系本身产生司法权/管辖权，领主有自己法庭。
- **特许状/特权**：城市、行会、领主、个人可获豁免/自治/司法权，形成“法内例外”。

参考：

- [Cosmopolitan Local Law in the Medieval Latin West (2025)](https://www.tandfonline.com/doi/full/10.1080/01440365.2025.2456266)
- [Legal Pluralism Explained](https://books.google.com.sg/books?id=dPUTEAAAQBAJ)
- [Legal Pluralism as a Category of Analysis — Law and History Review](https://www.cambridge.org/core/journals/law-and-history-review/article/legal-pluralism-as-a-category-of-analysis/EDFD7E065C7EA438131508CD4EE9A3CA)
- [Feudal Law: A Partially Neglected Theme (2025)](https://www.tandfonline.com/doi/full/10.1080/01916599.2025.2494408)

### 2.2 法律/法令/敕令的边界

- 秦漢出土文献研究明确区分 **律（code/statute）** 与 **令（decree/ordinance）**：律相对稳定、令因时因事而发；令可转化为律。
- 中世纪欧洲：**lex/ius**（法/法理）与 **ordinatio/statutum**（条例/法令）不同；城市/行会/领主条例是局部规范。
- 所以边界不是“永久 vs 临时”这么简单，而是：
  - **法**：一般、持久、需公布/编纂/权威；
  - **令**：具体、行政、可临时、可逐级下达；
  - **特许**：例外/特权，有授予者和范围；
  - **习惯**：默认/兜底，未必成文。

参考：

- [Differences between "Decree" and "Law" during Qin and Han Dynasties](https://journal.kci.go.kr/kucsi/archive/articleView?artiId=ART002180944)
- [Cosmopolitan Local Law in the Medieval Latin West](https://www.tandfonline.com/doi/full/10.1080/01440365.2025.2456266)

### 2.3 法律执行依赖行政能力

- 前现代国家“法律能力/行政能力”有限；中央法能否到地方，取决于官员、文书、道路、财政、驻军。
- 地方官/领主有执行偏差：拖延、部分执行、拒绝、借机谋利。
- 所以“法律生效”不等于“全国执行”；`DecreeOrder` 的方向是对的，应扩展到法律推行。

参考：

- [Seeing Like an Official](https://www.hup.harvard.edu/books/9780674308237)
- [Economic Consequences of State Failure — Legal Capacity, Regulatory Activity, and Market Integration in Poland, 1505–1772](https://homsy-staging.cambridgecore.org/core/journals/journal-of-economic-history/article/economic-consequences-of-state-failurelegal-capacity-regulatory-activity-and-market-integration-in-poland-15051772/5DEDC9569125F734554ED85C5E41C341)

---

## 3. 统一架构建议

### 3.1 三层规范，不是一堆平行法律

```text
Layer 1 根本法/政体结构（Entrenched）
  = 现有 LawComposition（继承、范围、中央/地方结构、军事组织等）
  = 改动成本高，需改革/政变/高合法性

Layer 2 普通法律（LawDef catalog）
  = 已知 → 推行中 → 生效
  = 一组一题，组内选项互斥/链式
  = 按域/族/组/项归类

Layer 3 法令（DecreeDef）
  = 临时、逐级下达
  = ActiveDecree + DecreeOrder
  = 到期/冷却；可部分执行/拒绝/谋利

横切 特许/特权（Charter）
  = 例外规范，挂在持有人/地域/事项
  = 可覆盖普通法（在授予范围内），不能覆盖根本法
```

这样：

- `LawComposition` 不再和 `LawDef` 抢“法律”身份，它是**根本法/政体结构**；
- `LawDef` 是普通法律；
- `DecreeDef` 是临时行政令；
- `Charter` 是例外。

### 3.2 状态机

```text
Unknown
  → Known（学习/接触/初始）
  → Proposed（提案）
  → Pending（推行中，旧法仍生效）
  → Active（生效）
  → Entrenched（可选：长期生效/高合法性后固化，改动成本更高）
  → Repealed
```

- 目前代码只有 Known/Pending/Active；`Entrenched` 是建议新增，不是现有字段；
- “已固化”可以映射到 `LawComposition` 或 `Entrenched` 状态；
- 法令没有 Entrenched，只有 Active/Expired/Cooldown。

### 3.3 每个领主/管理者都有法令吗

现有 `DecreeOrder` 已经逐级往下推：

```text
中央/上级 → 行政区 → 子行政区 → 聚落
```

每个节点有官员、信使、执行强度、合规度、结果状态。

建议扩展：

- 不只是“中央法令下达”，**地方领主的命令也走同一套 `DecreeOrder`**；
- 地方命令与上级法律冲突时，先走冲突裁决，再决定是否执行；
- 领主不是复制一套国家法律，而是：
  - 继承上级法律；
  - 在 autonomy 内发布地方条例/命令；
  - 通过 `DecreeOrder` 向下执行；
  - `compliance` 由官员/地方利益/合法性决定。

### 3.4 法律 vs 法令的边界

| 维度 | 法律 LawDef | 法令 DecreeDef |
|---|---|---|
| 时间 | 持久 | 临时/有期限 |
| 范围 | 一般/组内选项 | 具体事项/逐级下达 |
| 生效 | 推行期后替换旧法 | 立即 active，到期失效 |
| 执行 | 全局效果 + 官僚/地方 compliance | `DecreeOrder` 逐级执行，可部分/拒绝 |
| 变更 | 再推行/废除 | 冷却/撤销 |
| 固化 | 可 Entrenched/根本法 | 不固化 |

“法令转法律”可以作为机制：某法令长期重复/高合法性 → 提案转为 LawDef；不是自动。

### 3.5 冲突裁决

```text
1. 适用范围（地域/人群/事项）
2. 规范层级：Fundamental > Constitutional > Statute > Charter > Custom > Decree
3. 同层：特别法 > 一般法；后法 > 前法（同层）
4. Charter 在授予范围内可覆盖 Statute；不能覆盖 Fundamental
5. 仍冲突：legalConflict 标记，按 enforcementReach/compliance 实际执行
```

现有代码已有 `excludesLawIds`、`requiredDecreeIds`、`excludesDecreeIds`、`LegalNormKind/Scope`；需要真正接冲突裁决器。

### 3.6 不满/阻力怎么模拟

不要做“满意度 −5”，而是：

- **官员执行偏差**：`DecreeOrder.Resolve` 已有管理/阴谋/腐败/unrest/合法性；
- **地方利益**：地方 autonomy、领主/城市/行会利益受损 → compliance 降低；
- **阶层/派系**：法律效果影响特定阶层 → 派系诉求/叛乱风险；
- **合法性/稳定**：`LawEffectRuntime` 年度效果；
- **执行失败**：部分执行/拒绝/谋利，而不是全局数值。

生效法律仍可能有人不满；固化法律也可能被挑战——通过派系、地方拒绝、政变/改革路径表达，而不是单一数值。

---

## 4. 和官僚系统/官僚因子的连接

| 因子 | 来源 | 作用 |
|---|---|---|
| `administrativeCapacity` | 官员、识字、道路、财政、中央机构 | 登记/执法/统计上限 |
| `enforcementReach` | capacity × centralization × 距离 | 法律/法令实际覆盖 |
| `legalIntegration` | 统一法源、语言、官僚训练 | 冲突裁决、同法程度 |
| `compliance` | 合法性、驻军、自治、利益 | 地方执行 |
| `legalComplexity` | 法律组/例外/特许数量 | 行政负荷；超过 capacity 产生腐败/延迟 |

法律效果 ↔ 官僚能力：

- 户籍/税册/统一法提高 capacity，但增加 complexity；
- 特许/地方自治降低 central reach，提高 compliance/稳定；
- 军管/戒严提高 enforcement，降低 legitimacy。

---

## 5. 建议落地顺序

1. **承认三层**：`LawComposition`=根本法，`LawDef`=普通法，`DecreeDef`=法令；先停止双写。
2. 单一来源：`realm.laws` 由 active 根本法组派生；`LawSystem` 不再独立计算。
3. 法律状态机：Known → Pending → Active →（可选 Entrenched）；UI 分栏。
4. 法令 `DecreeOrder` 扩展到地方领主/城市命令，并接冲突裁决。
5. `Charter` 作为例外规范，挂 holder/scope/overrides。
6. 官僚因子接 `administrativeCapacity/enforcementReach/legalComplexity`。
7. 不满/阻力接派系/阶层/地方 compliance，不引入单一抽象满意度。
8. 旧档迁移：现有 `LawComposition` → 根本法；`LawDef` active → 普通法；`DecreeOrder` 保留。

---

## 6. 结论

- 你记得的“已知→推行→生效→固化”方向是对的；代码里已有 Known/Pending/Active，但没有 Entrenched 字段，`ForceSetLaw` 才是“固化”路径。
- “法令逐级往下推”也已经在 `DecreePropagationSystem` 实现，且有官员/信使/合规度/拒绝/谋利，不是纯数值。
- 现有最大矛盾是 **`LawComposition` 和 `LawDef` 两套法律系统并存**，以及 **特许权只被当成提案权**。
- 统一方案不是把所有东西塞进一个组，而是分三层 + 特许例外 + 多层执行；不满通过官员/地方/派系/执行机制表达，不做抽象满意度。
- 下一步应先做“三层归一 + 特许规范 + 冲突裁决”，再谈更多法律内容。


---

# 追加：历史演化阶段与“根本化”

## 7. 这是历史演化游戏，不能开局就现代

法律系统必须按时代/革新逐层解锁，不能一开始就有成文法、宪法、司法审查。

### 7.1 阶段模型

| 阶段 | 法律形态 | 关键革新 | 代码落点 |
|---|---|---|---|
| 0 习俗/禁忌 | 无成文；长老/首领裁决；习惯即法 | 语言、用火、口传 | `Custom`/`Command` 规范；无 law/decree 区分 |
| 1 首领/王命 | 口头命令、神谕、誓约 | 文字前/早期文字 | `Command`/`Decree`；执行靠个人权威 |
| 2 早期成文 | 律/令开始区分；书记、档案 | 文字、文书行政、档案 | `Statute`/`Decree` 解锁 |
| 3 法律学习 | 习惯法记录、法学家、ius commune、注释 | 大学、法律专家、印刷 | `Statute`/`Charter`/`Custom` 互动 |
| 4 等级/特许 | 城市/行会/领主特许；代表会议 | 城市特许状、代表机构 | `Charter` 作为例外规范 |
| 5 早期现代 | 主权立法、法典化、常备官僚 | 印刷、常备军、官僚制 | 统一法典/ordinance |
| 6 现代/未来 | 根本法/宪法化、司法审查 | 根本化革新、政治思想 | `Fundamental` + 根本化程序 |

注意：

- 不是所有文明都线性走完；有的停在习俗/王命；
- 法律多元在中世纪是常态，不是“落后”；
- 早期没有“法律/法令/特许”的现代区分，区分本身要革新解锁。

参考：

- [Customary Law and the Influence of the Ius Commune in High and Late Medieval East Central Europe](http://oxfordjournals.org/edited-volume/27972/chapter-abstract/211611515)
- [Consuetudine, Coutume, Gewohnheit and Ius Commune](https://rg.lhlt.mpg.de/index.php/rg/article/view/668)
- [Law and Revolution, I](https://www.hup.harvard.edu/books/9780674020856)
- [Religion, Law and the Growth of Constitutional Thought 1150–1650](https://openyls.law.yale.edu/bitstream/handle/20.500.13051/7523/Carl_Landauer.pdf?isAllowed=y&sequence=2)
- [Hobbes on treason and fundamental law](https://www.tandfonline.com/doi/full/10.1080/17496977.2021.1947597)

## 8. 生命周期加一层：根本化 Fundamentalization

你提的“已固化之上再加根本化”成立，而且适合作为**高成本、需革新、可被挑战**的阶段。

```text
Unknown
  → Known（学习/接触）
  → Proposed（提案）
  → Pending（推行中；旧法仍生效）
  → Active（生效）
  → Consolidated（固化：长期执行/稳定，普通程序仍可改）
  → Fundamentalized（根本化：受更高保护，普通法律/法令不能覆盖）
  → Repealed（废除；根本法只能走根本程序/革命）
```

### 8.1 固化 Consolidated

- 触发：法律生效满 N 年 + 合法性/执行稳定 + 无重大冲突；
- 效果：改动成本提高；不再是“临时政策”；
- 仍需普通立法程序；
- 对应现有 `ForceSetLaw`/长期 active 的语义，但要有独立状态，不混用。

### 8.2 根本化 Fundamentalized

触发条件（全部满足）：

1. 已 `Consolidated`；
2. 掌握根本化革新（如成文法、法学家/大学、印刷、根本法思想）；
3. 合法性/权威达到阈值；
4. 无更高规范冲突；
5. 通过根本化程序（代表会议/元老院/宗教会议/独裁者制宪等，按政体）。

效果：

- 普通 `LawDef` 不能覆盖；
- `DecreeDef` 不能覆盖；
- 只有 `Fundamental` 级规范/根本化程序/革命/政变能改；
- 与特许状冲突时，根本法优先；强行冲突 → 合法性/派系/地方抵抗机制，不是静默覆盖；
- 根本化不是“永远正确”，可以腐化/过时，但改动成本高。

### 8.3 根本化需要革新解锁

建议字段：

```csharp
LawRequirement.requiredInnovationIds; // 已有
LawGroupState.stage;                  // 新增 LegalNormStage
RealmLawState.fundamentalizedLawIds;  // 新增
```

根本化革新示例（按历史阶段，不一次全上）：

- 成文法/文书行政（505/822）；
- 法学家/大学（603）；
- 印刷/传播（1110）；
- 根本法/宪制思想（未来条目）；
- 代表机构/等级会议（现有法律组）。

没有这些革新，只能停在 Active/Consolidated，不能根本化。

## 9. 法律/法令/根本法的边界（更新）

| 层 | 规范 | 状态 | 谁能改 |
|---|---|---|---|
| 根本法 | `Fundamental`/`Constitutional` | Fundamentalized | 根本程序/革命 |
| 普通法 | `Statute` | Known→Pending→Active→Consolidated | 立法程序 |
| 法令 | `Decree` | Active/Expired/Cooldown | 行政/首脑 |
| 特许 | `Charter` | Active/Revoked | 授予者/上级 |
| 习惯 | `Custom` | 默认/兜底 | 长期实践/司法 |

- 法令可以转普通法（提案），普通法可以固化，固化可以根本化；
- 根本化不是自动升级，必须有革新 + 程序 + 合法性。

## 10. 重构方案（先规划，不急着全改）

### Phase A：模型归一

1. 新增 `LegalNormStage`：Unknown/Known/Proposed/Pending/Active/Consolidated/Fundamentalized/Repealed；
2. `LawGroupState` 增加 `stage`；
3. `RealmLawState` 增加 `fundamentalizedLawIds`；
4. 明确 `LawComposition` = 根本法/政体结构，不再和普通 `LawDef` 双写。

### Phase B：生命周期

1. Known → Pending → Active 已有；
2. 新增 `Consolidate`（时间/稳定/合法性）；
3. 新增 `TryFundamentalize`（革新/合法性/程序/无冲突）；
4. `CanEnactLaw` 检查：根本法不能被普通法覆盖；
5. `CanEnactDecree` 检查：根本法不能被法令覆盖。

### Phase C：冲突与特许

1. 接 `LegalNormKind/Scope/precedence` 冲突裁决；
2. `Charter` 作为例外规范，带 scope/overrides；
3. 特许与根本法冲突 → 根本法优先 + 抵抗机制。

### Phase D：官僚与执行

1. `administrativeCapacity/enforcementReach/legalIntegration/compliance/legalComplexity`；
2. `DecreeOrder` 扩展到地方领主/城市；
3. 不满通过官员/地方/派系机制，不做抽象满意度。

### Phase E：UI/存档/迁移

1. UI 分“根本法/普通法/推行中/法令/特许”；
2. 旧档：`LawComposition` → 根本法；active LawDef → 普通法；pending/decree 保留；
3. 测试门禁。

## 11. 结论

- 你的 Known→Pending→Active→固化 流程对；**再加“根本化”合理**；
- 根本化必须是革新解锁 + 程序 + 合法性，不是自动升级；
- 早期不能现代：习俗/王命 → 成文 → 法律学习 → 特许/等级 → 法典 → 根本法；
- 重构先做模型归一（三层 + 阶段 + 特许），再做冲突/官僚/UI；
- 法律内容按时代/革新逐步解锁，不要开局全开。

---

## 12. 代码现状（本轮）

- 已加 `LegalNormStage`（Unknown/Known/Proposed/Pending/Active/Consolidated/Fundamentalized/Repealed）；
- `LawGroupState.stage`、`RealmLawState.fundamentalizedLawIds`；
- 尚未接根本化行为/UI/旧档迁移；先做模型归一。


---

# 追加：政体结构不是“根本法”

## 13. 核心判断

你的方向成立：**政体结构应该收进法律/法令系统**，不能再让 `LawComposition` 和 `LawDef` 两套并行。

但**政体结构 ≠ 根本法**。

- **政体结构**是“管什么”：谁统治、怎么交接、中央/地方怎么分权、有哪些机关、军事怎么统辖；
- **根本法**是“效力/保护级别”：一条规范被根本化、普通程序不能改；
- 政体结构可以是：
  - 未成文习惯（部落/早期王权）；
  - 王室法令/诏令；
  - 成文律；
  - 特许状/等级会议协定；
  - 后来才被“根本化”。

所以不要把“政体结构”直接命名为“根本法”，也不要把 `Fundamentalized` 当成政体结构的默认状态。

## 14. 建议的模型

```text
ConstitutionalOrder（政体结构/宪制秩序）
  = 一个“主题域/视图”，不是一种规范等级
  ├─ 主权归属
  ├─ 最高交接
  ├─ 最高范围
  ├─ 中央有无/交接/机构
  ├─ 地方交接/职能
  ├─ 央地结构
  ├─ 军事统辖
  └─ 身份/公民权边界（按时代）
```

每项本身是 `LegalNorm`：

```text
LegalNorm
  ├─ kind: Custom / Command / Decree / Statute / Charter / Constitutional
  ├─ scope: Realm / Province / Lordship / Locality / Personal
  ├─ stage: Known..Active..Consolidated..Fundamentalized..
  ├─ source/issuer
  ├─ requirements（革新/法律/法令/政体条件）
  └─ effects
```

关键：

- `ConstitutionalOrder` 是**主题视图**；
- `Fundamentalized` 是**阶段/效力**；
- 两者不能合并。

## 15. 早期不能现代

| 阶段 | 政体结构怎么存在 | 是否有“根本法” |
|---|---|---|
| 部落/习俗 | 习惯、长老/首领裁决 | 无 |
| 早期王权 | 王命、神谕、誓约、王室习惯 | 通常没有 |
| 早期成文 | 律/令、王室法令 | 可能有“祖制”但不叫根本法 |
| 中世纪 | 封建习惯、特许状、等级会议、教会法 | 有“基本法/王国法”雏形，但不是现代宪法 |
| 早期现代 | 主权立法、王国基本法、法典 | 根本法话语兴起 |
| 现代/未来 | 成文宪法、根本化、司法审查 | 根本法/宪法 |

所以：

- 早期政体结构可能是 `Custom/Command`，不是 `Statute`；
- “根本化”是后期革新解锁的能力；
- 不要在开局给玩家一套现代宪法式政体法。

## 16. 与现有代码的对应

当前：

- `LawComposition` = 9 个维度/组件（继承、范围、中央/地方、结构等）；
- `LawDef` = 具体法律组；
- `LawSystem` 从 `LawComposition` 算 `centralization`；
- `LawEffectRuntime` 从 `LawDef` 算效果；
- `LawReform`/`PolityComponentInnovations` 双写。

重构：

1. 删除 `LawComposition` 作为独立规则源；
2. 建立 `ConstitutionalOrder` 视图，由 `RealmLawState` 中政体相关组派生；
3. `LawSystem` 的 `centralization/expansionism` 从 active `LegalNorm` 效果计算；
4. `LawReform` 改为“提出/推行政体法律组”，不再直接改 `realm.laws`；
5. `PolityComponentInnovations` 改为“解锁政体法律组选项/革新前置”；
6. `Realm.laws` 迁移为 `ConstitutionalOrder` 快照或删除；
7. `ForceSetLaw` 仍作为政变/革命快速路径，但要写事件/合法性后果。

## 17. 根本化怎么放

- 不是所有政体结构都自动根本化；
- 政体结构先 `Active`；
- 满足条件后可 `Consolidated`；
- 再满足革新/程序/合法性才 `Fundamentalized`；
- 根本化后普通法律/法令不能覆盖，但政体结构本身仍可通过根本程序/革命改变。

## 18. 学术依据

- 中世纪“基本法/王国法”不是现代宪法；见 Tierney, *Religion, Law and the Growth of Constitutional Thought 1150–1650*；
- “fundamental law” 概念有早期现代起源，不等于成文宪法；见 [Hobbes on treason and fundamental law](https://www.tandfonline.com/doi/full/10.1080/17496977.2021.1947597)；
- 习惯法/ius commune/法律多元：政体结构存在于多种法源，不是单一最高法；
- 秦漢律/令区分说明“法/令”边界本身是历史产物。

参考：

- [Religion, Law and the Growth of Constitutional Thought 1150–1650](https://openyls.law.yale.edu/bitstream/handle/20.500.13051/7523/Carl_Landauer.pdf?isAllowed=y&sequence=2)
- [Hobbes on treason and fundamental law](https://www.tandfonline.com/doi/full/10.1080/17496977.2021.1947597)
- [Customary Law and the Influence of the Ius Commune](http://oxfordjournals.org/edited-volume/27972/chapter-abstract/211611515)
- [Consuetudine, Coutume, Gewohnheit and Ius Commune](https://rg.lhlt.mpg.de/index.php/rg/article/view/668)
- [Differences between "Decree" and "Law" during Qin and Han Dynasties](https://journal.kci.go.kr/kucsi/archive/articleView?artiId=ART002180944)

## 19. 结论

- **同意**把政体结构收进法律/法令系统；
- **不同意**把政体结构等同于根本法；
- 正确关系是：政体结构 = 主题域；根本化 = 规范阶段/效力；
- 早期政体结构可以是习惯/王命，不必成文；
- 先做 `ConstitutionalOrder` 派生视图 + 删除 `LawComposition` 双写，再做根本化服务。
