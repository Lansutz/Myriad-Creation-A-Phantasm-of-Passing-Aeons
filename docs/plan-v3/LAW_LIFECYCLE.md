# 法律与习惯的生命周期（LAW LIFECYCLE）

> 2026-11-09 03:20。依据：用户口径（已知→推行→生效→固化→宪法化；没有成文法时只有**习惯**，习惯在文化传统里；**习惯会变成法，法也会变成习惯**）+ 代码实测。
> 定位：`DESIGN_CHARTER` 管方向，本文管**法律域的生命周期与法源桥**；状态仍以总纲 §3 为准。

---

## 1. 代码里已经有的骨架（不是从零设计）

| 设计元素 | 代码位置 | 现状 |
|---|---|---|
| 八态生命周期 | `LegalNormStage`：`Unknown / Known / Proposed / Pending / Active / Consolidated / Fundamentalized / Repealed` | **枚举存在，但只有 3 个转移在跑**（Known→Pending→Active，加上撤销）；`Consolidated/Fundamentalized` 无写入方 |
| 根本化条件 | 枚举注释原文："根本化不是自动升级，需要革新 + 程序 + 合法性" | 与用户口径一致；**未实现** |
| 根本法清单 | `RealmLawState.fundamentalizedLawIds` | **无读无写**（D 域 P1） |
| 习惯法位阶 | `LegalNormKind.Custom = 0`（"习惯法/默认"） | 枚举在，无消费者 |
| 习惯作默认法 | `LawGroupDef.fallbackLawId` + `LawDef.isFallback` | JSON 中 **0 次使用**；无消费者 |
| 冲突与效力 | `LawDef.normKind / scope / precedence / issuerCharacterId / overrides` | JSON 中 **0 次使用**；无读取点 |
| 习惯的载体 | `TraditionDef`：`solidification`（固化度，默认 0.3）、`RoleGateFulfilled`（政治传统需角色位阶）、`upgradesFrom`、`linkedInnovationIds`、`incompatibleWith` | **全部零消费者**（`solidification` / `upgradesFrom` / `linkedInnovationIds` 全仓无读写） |
| 传统的政治门槛 | `TraditionElevation.Rank(role)`（平民 0 / 廷臣 1 / 商人军事教士学者 2 / 贵族 3 / 君主 4） | 只被 `CultureTransitionHost` 用，而该链零调用 |
| 现行运行链 | `LawRealmHost.EnsureKnownLaws` → `BeginEnactLaw`（`enactmentDays` 倒计时）→ `SetActiveLaw` → 撤销 | 这是唯一真跑的路径 |

**结论**：你说的生命周期，代码里**八态齐全、习惯槽位也留了**（`Custom` + `fallbackLawId` + `isFallback` + `TraditionDef.solidification`），缺的是**转移规则与两座桥**。

---

## 2. 完整状态机（建议形态）

```
                  ┌──────────── 习惯（Custom，存在 tradition 里，不在 law 表）
                  │                     │ 固化度↑ + 位阶够 + 有立法能力
                  │                     ▼
Unknown → Known ─┴→ Proposed → Pending（推行中，enactmentDays）
                                   │ 到期且未被撤销
                                   ▼
                                 Active（生效，效果结算）
                                   │ 连续生效 ≥N 年 + 合规度 ≥X
                                   ▼
                              Consolidated（固化：成为稳定制度）
                                   │ 相关革新 + 制宪主体 + 合法性阈值
                                   ▼
                            Fundamentalized（宪法化：不可被同组普通法覆盖）
                                   │ 反宪法化程序 / 政权灭亡
                                   ▼
                                Repealed（废除；若已固化，习惯可能留下）

另有旁路：Active → Replaced（被同组新法替代）/ Suspended（悬置）/ 纸面法（合规度极低，效果按比例衰减）
```

### 2.1 四个阶段在机制上的区别（不能混为一谈）

| 阶段 | 机制含义 | 谁决定 | 可观察 |
|---|---|---|---|
| 推行中 Pending | 已颁未行：行政系统在传递、在讨价还价 | `enactmentDays` + 合规度（法令传播已有五档） | 面板可见"推行中"、可取消 |
| 生效 Active | 在施行：效果结算（税/人力/稳定/合法性） | 已到期且未被撤销 | 效果数字变化 |
| 固化 Consolidated | 成为制度：不再靠某个人推行，**反抗成本上升** | 连续生效年限 + 合规度 + 未被反复挑战 | 撤销成本变高、编年史记"某法已成定制" |
| 宪法化 Fundamentalized | 成为根本法：**同组普通法不能覆盖它** | 革新（成文法/法典/根本法）+ 制宪主体 + 合法性阈值 | `precedence` 生效、修宪需要更高门槛 |

> 一句话：**Pending 是行政问题，Active 是执行问题，Consolidated 是社会问题，Fundamentalized 是主权问题。**

---

## 3. 两座桥（习惯 ↔ 法）

### 3.1 习惯 → 法（法制化）

**触发条件**（三件齐备，缺一不成）：

1. 该传统 `solidification` 达阈值（长期被实践）；
2. 提出者位阶够（`TraditionElevation.Rank(role)` ≥ 该传统 `minRole`；政治类必须有门槛）；
3. 政权有立法能力（现有 `proposalAuthority` + `minCentralization/minTreasury/minLegitimacy`）。

**结果**：生成一条 `normKind = Statute` 的 `LawDef`（内容侧以 `source = custom` / `upgradesFrom = traditionId` 标注），推行期照常；成功后习惯仍在（它不是被"消灭"，而是被"成文化"）。

**反例（禁止）**：把"习惯"直接变成立法效果，绕过推行期——那就是数值驱动。

### 3.2 法 → 习惯（惯例化）

**触发条件**：某法连续 `Active` ≥ N 年 **且** 合规度均值 ≥ X **且** 未被反复撤销。

**结果**：写回文化传统——新增或强化对应 `TraditionDef`（`solidification += Δ`），编年史记"某法已成习俗"。

**关键后果**：**即使该法日后被 Repealed，习惯仍在**（法死习惯活）。这才是真实的：很多制度"法律上废了，生活里还在"。

### 3.3 宪法化的三个前置（不只是革新）

1. **革新**：成文法/法典/根本法概念（对应 `requiredInnovationIds`）；
2. **制宪主体**：君主诏令 / 议会决议 / 教团裁定——谁宣布它是根本法，决定它日后谁能改；
3. **合法性阈值**：`legitimacy` 与 `centralization` 不够时强行宪法化 → 应触发合法性反噬/分裂（而不是静默成功）。

---

## 4. 你模型里我认为还缺的八点

| # | 缺口 | 为什么重要 | 代码现状 |
|---|---|---|---|
| 1 | **失效不止"废除"** | 真实历史里多是"未废除但已死"（纸面法）、被替代（replaced）、悬置（suspended） | 只有撤销；`DecreeOrderState.Profiteering` 之类零散枚举无对应 |
| 2 | **法的层级与冲突规则** | 宪法化只是最高位阶；还需上位/下位、特别法优先、后法优于前法、法不溯及既往 | `normKind/scope/precedence/overrides` 字段全在、**JSON 0 次使用、无读取点** |
| 3 | **地域效力** | 一法不必行天下：地方习惯、城市特许状、边疆特权 | `LegalScope` 枚举在，无消费者 |
| 4 | **人格效力**（与刚定的"身份"轴直接相连） | 贵族/教士/平民/外国人适用不同法——这正是 `SocialIdentityFlags` 的用处 | 身份轴目前零判定 |
| 5 | **判例作为第三法源** | 司法链（案件→判决）本该产生判例 | 司法链整段空转 |
| 6 | **习惯的层级与冲突** | 家族习惯 / 地方习惯 / 族群传统 / 宗教法；冲突时谁优先 | 传统无层级、无冲突规则 |
| 7 | **宪法化的"谁"** | 没有制宪主体，宪法化会退化成"科技解锁" | 无 |
| 8 | **政权断裂时的法** | 政权灭亡后：继承国沿用？征服者废除？习惯残留？ | 无 |

另有一个小点：**时效**（试行期、日落条款 sunset）——法典化早期的常见做法，可用 `enactmentDays` 的兄弟字段实现。

---

## 5. 验收信号（可断言）

1. **习惯→法**：给某传统足够 `solidification` + 位阶满足 → 出现可推行的 law 条目；位阶不够时**不出现**。
2. **法→习惯**：某法连续 Active N 年且合规 → 对应传统出现/固化度上升，编年史有条目；之后撤销该法 → 传统**仍在**。
3. **宪法化**：无相关革新时不可达；有革新 + 制宪主体 + 合法性时可达；此后同组普通法**无法覆盖**它。
4. **纸面法**：合规度低于阈值 → 效果按比例衰减（不是 100% 生效）。
5. **层级冲突**：给出两条冲突法（普通法 vs 根本法）→ 判定结果由 `precedence` 决定且有测试。
6. **人格效力**：同一条法对不同 `SocialIdentityFlags` 给出不同适用/豁免结果（至少贵族与外国人两条）。

---

## 6. 与现有计划的关系

- 归 **M5**（玩家干预：法律/法令/官职）与 **S16**（头衔：立法主体与制宪主体同源）。
- 法律仍在**重构中**（宪章 §7）：本文只固定**生命周期语义与两座桥的接口**，不锁 `LegalNormStage` 的内部实现。
- 依赖 **M1**：`fundamentalizedLawIds`、`normStage`、传统固化度都要入档，否则读档后"法又活了/习惯又没了"。
