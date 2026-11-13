# 设计宪章接入报告（学术基础 → 可执行约束）

> 2026-11-09 01:22（Asia/Shanghai）。里程碑：Plan v3 / M2.5「宪章接入」。
> 输入：用户口述的 8 条学术基础（唯物史观、布罗代尔三时段、反辉格史观、历史政治学、人类学、地理历史学、技术史、年鉴学派）与 4 条核心设计原则（多路径、地理×能动性、渐进分层、革新为核心）。

---

## 1. 目标与验收标准

**目标**：把"理念"转成"约束"——每条学术立场必须落到**机制含义 + 禁止做法 + 代码锚点**；每条设计原则必须落到**可在模拟里断言的信号**。否则理念只能停留在文档，而本项目最大的病就是"文档写得比代码跑得多"。

**验收标准**：

| # | 标准 | 结果 |
|---|---|---|
| 1 | 8 条学术基础各给出机制含义/禁止做法/代码锚点/现状偏差 | ✅ `DESIGN_CHARTER.md` §1 表 |
| 2 | 4 条原则各给出可断言信号（能写成测试或批量模拟断言） | ✅ §2 四组信号 |
| 3 | 对现状做对账：哪些已兑现、哪些被违背（带审计证据） | ✅ §4：6 条已兑现 / 7 条被违背 |
| 4 | 总纲据此修订（里程碑、DoD、否决项） | ✅ 新增 M0.5；DoD 增横向门禁；M6 mastery 提前；M7 增建国路径；否决 2 项 |

---

## 2. 输入与前提

- 用户口述的八条理论立场与四条原则，逐字保留在 `DESIGN_CHARTER.md` 的标题体系里（唯物史观/布罗代尔三时段/反辉格/历史政治学/人类学/地理历史学/技术史/年鉴学派；多路径/地理与能动性/渐进分层/革新为核心）。
- 前提：**理念不能推翻代码事实**。冲突时，宪章改的是"计划与验收"，不是"把现状写成功"。
- 只读：本轮未改任何运行时代码。

---

## 3. 动作清单

1. **为每条原则找代码锚点**（Lead 亲自 grep/read）：
   - `StateFormationPath` / `CultureStageEvolutionHost.FormRealmFromActor` / `ApplyPathBonuses`
   - `SocialClassAvailability.IsClassAvailable` / `SocialSubclass` 门槛
   - `ClassEmergenceEvents` + 调用点 `GameWorld.Simulation.cs:266`
   - `InnovationEra.GetPreferenceWeight` 的唯一使用点 `AIController.cs:141-156`
   - `CarryingCapacitySystem`、`HoldridgeBiomeClassifier`
2. **写宪章**：`docs/plan-v3/DESIGN_CHARTER.md`（8 条理论 + 4 条原则 + 系统映射 + 对账 + 修订 + 待议）。
3. **改总纲**：`SYSTEM_MASTER_PLAN.md` —— §0 增第 7 条结论、§1 增"方向由宪章约束"、§5 增 M0.5、§5 改 M6/M7、§7 增横向门禁、§8 增待议指针、§9 增证据、§10 增变更。
4. **改决策**：`DECISIONS.md` 增 D10（反辉格：否决 Era 门槛与时代自动分层）、D11（宪章作为横向验收门禁）、D12（建国路径接线位置与"必须带代价"）。

---

## 4. 证据

### 4.1 已兑现（原则在代码里真的成立）

| 原则 | 代码证据 |
|---|---|
| 渐进分层 + 革新驱动 | `SocialClassAvailability.cs:11-79`：12 个子类全部挂**具体革新 id**（Freeholder←100/916/917/918、Tenant←501/952、Serf←952、Merchant←701/700、Scholar←600+503/504、三类奴隶各有门槛），并按 `culture.livelihoodType` 过滤（`:20`）；`ClassEmergenceEvents.cs:12-32` 建"革新→阶层"映射，`GameWorld.Simulation.cs:266` 在革新完成时写编年史（如"庄园制兴，农奴阶层出现"） |
| 革新 → 制度 | `LawContracts.cs:65,229,258`（2026-11-09 修复，Lead 亲验）；`PolityComponentInnovations` 政体组件硬门槛 |
| 反辉格 | `InnovationEra.GetPreferenceWeight` 只出现在 `AIController.cs:154-155`（排序偏好），前置判定只看前置图 |
| 制度有成本 | 法律 `enactmentDays`、法令冷却 + 逐级下达 + 合规度五档 |
| 总体史耦合 | `RealmSituationBuilder.Build` 每 tick 汇总态势，被 AI/阶层/法令/继承共享 |

### 4.2 被违背（原则与实现的关键落差）

| # | 违背 | 证据 |
|---|---|---|
| 1 | **多路径建国永不发生** | `FormRealmFromActor` 全仓 grep 仅定义处 + 内部调用，**零外部调用者**；`ApplyPathBonuses` 只给一次性属性（`stability=55` / `treasury=2000` …），无后续约束 |
| 2 | 唯物史观少一半（生产力不存在） | `inventory` 写入者只有开局播种（`GameWorld.Core.cs:842-843`）、商队到货（`TradeCenter.cs:64`）、读档（`TradeCenterDTO.cs:72`）；`SettlementProduction.TryCraft` 零产品调用者 |
| 3 | 长时段被中时段覆盖 | 每季 tick 全表重写陆地气候与群系（`GameWorld.Simulation.cs:360`，A 域 P0-3） |
| 4 | 能动性缺失 | `FindBestGeneral`/`CalculateRuleAbility`/`CalculateOverallAbility` 零调用；`ModifyPrestige` 零调用且 prestige 每 tick −0.05（C 域）⇒ 人物属性不改变决策，威望恒 0 |
| 5 | 革新核心对玩家不可见 | `Assets/UI` 对 `Innovation\|Research` grep 命中 0；mastery 恒 0；`unlockRefs` 120 条零运行时消费者（H 域） |
| 6 | 人类学层空转 | `FamilyNode.AddFamilyTradition` 零调用、字段 `[NonSerialized]`；`CharacterRelation.opinion/trust/fear` 只写不读（C 域） |
| 7 | 分层渐进只有上半段 | 阶层涌现接在革新上，但 167/186 条革新因 mastery 恒 0 不可达 ⇒ 涌现被锁在 19 条根革新 |

### 4.3 由此产生的计划改动（不是建议，已写入总纲）

1. 新增 **M0.5 历史行为验收台**（批量 seed × 300 年 + 指标导出 + 四组断言）。
2. **DoD 增横向门禁**：每个里程碑必须满足其承载原则的一条可断言信号。
3. **M6 的 mastery 修复提前**为 M3 前置（否则"文明自己长成"的实验会被锁死在 19 条革新）。
4. **M7 增建国路径接线**，并要求每条路径带代价（`DECISIONS.md` D12）。
5. **否决两项伪缺口**：Era 硬门槛、阶层随时代自动升级（`DECISIONS.md` D10）。

---

## 5. 结论

### 已验证
1. 八条理论立场在代码里**有六处真实锚点**（阶层门槛/革新→制度/时代仅偏好/制度成本/态势汇总/受生计类型过滤），宪章不是空转的口号，它描述的是"这套系统本来的设计"。
2. 四条原则中，**渐进分层与革新驱动已有半套实现**，多路径与能动性是**整段零调用**——这是本轮最有价值的定位。
3. 计划已据此修订，且新增了"没有行为断言就不算完成"的横向门禁。

### 未验证（不得当成事实）
1. 未跑批量模拟：宪章的四个断言信号**目前都还没有实际数据**（因为没有 M0.5）。"300 年出现 ≥3 种路径"现在必然不成立（建国路径零调用）。
2. 未实测阶层涌现的实际节奏：受 AI 研究速度与 19 条根革新限制，100 年内能涌现几个子类未测。
3. "能动性"的量化阈值（属性差 ≥1σ 导致 100 年显著分叉）是**待标定的假设**，不是我算出来的数。

---

## 6. 遗留与风险

1. **宪章的四条待议需用户裁决**（`DESIGN_CHARTER.md` §6）：建国路径是"开局选"还是"条件逼出来"；人物能动性上限（天才能否逆地理）；是否给"文明阶段"总指标；偶然性预算。
2. **风险：宪章变成新的"纸面权威"**。缓解：宪章每条原则都钉了可断言信号，且 D11 把它设为门禁；没有信号的原则按口号处理。
3. **风险：M0.5 被跳过**。若直接开工 M1–M10，则"完成"仍只能靠工程门禁判定——这正是本项目过去失败的方式。
4. **术语漂移**：用户口径用"文明演进/国家形成"，代码用 `RealmForm`/`StateFormationPath`/`CultureStage`；宪章保留用户术语，并在括号里标代码名，避免第二套词汇。

---

## 7. 产物清单

| 路径 | 说明 |
|---|---|
| `docs/plan-v3/DESIGN_CHARTER.md` | **新增**：设计宪章（8 条理论 + 4 条原则 + 系统映射 + 对账 + 修订 + 待议） |
| `docs/plan-v3/SYSTEM_MASTER_PLAN.md` | 修订：§0 第 7 条结论、§1 宪章约束、§5 新增 M0.5 与 M6/M7 改动、§7 横向门禁、§8/§9/§10 |
| `DECISIONS.md` | 新增 D10（反辉格否决两项）、D11（宪章门禁）、D12（建国路径位置与代价） |
| `reports/charter-integration-20261109-0122.md` | 本报告 |
