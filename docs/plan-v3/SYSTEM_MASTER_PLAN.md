# MASTER PLAN V2 —— 全系统合成（替换 `SYSTEM_MASTER_PLAN.md` 的候选件）

> 2026-11-10。**本文件是 `docs/plan-v3/SYSTEM_MASTER_PLAN.md` 的替代候选件**，读完 3 份权威件 + 12 份子计划 + `audit/` A–M 13 份 + `research/` 3 份 + `docs/README.md` 后合成。
> 记号：状态 `在跑 / 半跑 / 空转 / 不存在 / 重构中`（口径见 `SYSTEM_MASTER_PLAN.md` §0.2 + `DECISIONS.md` D2）；接线 `✅已接 / ⚠️半接 / ❌未接`。
> 纪律：结论必须能指到 `文件:行`；取不到证据写「无」；不凭空定数值，未标定一律写 `TODO_M05`。
> **覆盖度诚实声明**：本机无 Unity 编辑器，本轮**未运行编译与 706 个测试**。所有「在跑/空转」= 静态调用链 + grep + 审计 `file:line`；引用 `706/706 绿` 时一律注明为 2026-11-08 的既有结果文件，非本轮执行。

---

## §0 一句话 + 五条最重要的判断

**一句话**：这座工程不是「系统不够」，而是**六条单点断链让二十余个系统同时停摆**——所以计划的骨架不是「排系统」，而是「排断链」。

| # | 判断 | 证据（可指到行） |
|---|---|---|
| **J1** | **三件事不做完，其余全白做**：① **存档 + 确定性闭合** ② **物资生产入流** ③ **人口—聚落生命周期闭合**。三者分别是「可复现」「有物质载体」「会长大」的唯一前提，缺任一条，下游所有里程碑都在空数上运算 | ① `SaveData.cs:22-77` 无 plans / 外交四表 / 头衔 / 编年史；`settlementLevel` 只在 Unity 序列化回调同步（`SettlementPersistence.cs:269`，B §2.4）；② `TradeCenter.inventory` 写入者仅三处：播种 `GameWorld.Core.cs:842-843`、商队到货 `TradeCenter.cs:64`、读档 `TradeCenterDTO.cs:72`；③ `UpdateConstructionProgress/CanLevelUp/PerformLevelUp` 外部调用 0（`SettlementTypologySystem.cs:52/221/297`，B §4-1） |
| **J2** | **两处计划互相矛盾（用户已知）**，另发现 4 处，全部进 §5。已知两处：**D32 删掉 `Military/Trade/Diplomacy/Migration` 后 M7 的施工口径必须改**；**教理性别/婚姻规则与 M5「人格效力」是同一件事** | D32（`DECISIONS.md:177-179`）vs `SYSTEM_MASTER_PLAN.md:558`「凡需时间+人+物资一律用 Plan」；`INTEGRATION_PLAN.md:131-132` 自认 |
| **J3** | **ROI 最高的一批不是系统，是 6 个单点开关**：`monarchId` 开局不赋值 ⇒ 继承/摄政/谥号全不触发；`stateReligionId` 无生产写入 ⇒ 传教/热忱/圣战/圣地/面板五链同死；`LawState→LawComposition` 无上行 ⇒ 改法不影响治理；`CreateArmy` 零调用 ⇒ 战争在空集合上跑；`highPriestCharacterId` 恒 −1 ⇒ 封圣 + 大圣战双死；`mastery` 恒 0 ⇒ 167/186 革新不可达。**每一项 ≤1 个调用点，各点亮一域** | C §4-2（`monarchId`）、E §4-1（`stateReligionId`）、D §4-2、G §4-1、E §4-4、H §2.2 |
| **J4** | **「玩家」这个概念在代码里不存在**：`PlayerRealmId` 全仓从未赋值，`GovernmentPanel` 回退 `realms[0]`；29 项调度中 22 项无任何界面可触发或观察。所以"补入口"必须先立身份，否则只是给旁观者开窗 | `GovernmentPanel.cs:246-256` + D13；H §2.6 |
| **J5** | **成本要按断链数估，不按系统数估**：**M7 / M9 被高估**（各只差 1–2 条链：军队生成、`stateReligionId`）；**M3 被低估**（生产 + 人口单源 + 内容 + 存档四处同时动，其中 D6 的人口单源要迁 45 个读点/23 文件）；**M5 最贵**（唯一深交互 + 要并教理 WP3 + 八态生命周期） | L §6、B §2.1、`INTEGRATION_PLAN.md:1-5` |

---

## §1 系统全图（S1–S18：现状 / 依赖谁 / 谁依赖它 / 归属）

| 系统 | 现状 | 依赖谁 | 谁依赖它 | WP / 里程碑 |
|---|---|---|---|---|
| **S1 世界与时间** | **半跑**（A 域 23 项：在跑 6 / 半跑 10 / 空转 5 / 不存在 2）；地形编辑破坏性重算、河网五字段零生产者、双气候双群系互写 | Content（群系 92 条） | S2 S3 S7 S8（`movementCost` 给行军） | M2 |
| **S2 空间与聚落** | **半跑**：营地→聚落唯一活路径；等级/建设度**永不推进**、毁灭/废弃/重定居**零触发源** | S1、S13 | S3 S7 S8（税基/控制/征兵） | M2/M3、WP1 |
| **S3 人口** | **在跑**（增长 × 承载力 × 满意度 → 饥荒/叛乱），但**两套人口永不收敛**、成长率恒正、无迁移规则 | S1 S2 | S5（税基）S6（合规）S7（劳动力）S8（兵源） | M3、WP4 |
| **S4 角色** | **半跑**：日结/遗传/心理在跑；`Marry` 零调用、`monarchId` 不赋值、角色面板序列化为 null | S5（官职/继承）、S14 | S6（法令传播）S8（选将）S10（研究）S16 | M8、WP1/WP5 |
| **S5 政权** | **在跑**（`politics.daily`：收税/稳定度回归/派系/政体变迁）；税率不可写、无收支表 | S3 S6 S7 | S4 S8 S9 S16 S17 | M4/M8 |
| **S6 法律** | **重构中 + 半跑**：唯一深交互；八态只 3 个转移在跑、双源单向下行、司法链整段空转 | S5、S10、Content | S7（税/人力乘子）S3（合规度）S8（兵役） | M5、WP3 |
| **S7 经济** | **半跑**：日结（库存/供需/发车/消费/货币）在跑，**生产不存在**；43 配方执行器仅测试调用（本轮亲验：`TryCraft` 5 命中全在 `SettlementProduction.cs` 自身） | S1 S2 S3 | S5（税基）S8（军需）S3（口粮） | M3/M4/M6、WP1 |
| **S8 军事** | **空转**：`CreateArmy` 生产 0 调用（本轮 grep：非测试命中 1 = 定义）、`Army.DailyTick` 0 调用、宣战后**永不结束**（白和 730 日 vs 日域上限 365） | S5 S7 S3 | S9 S5（战功/疆域） | M7 |
| **S9 外交** | **半跑**：`relation` 有真实消费者（AI 结盟/选目标 + 地图着色），但**全表不入档** ⇒ 读档随机重置，与已入档 `wars` 矛盾 | S5 | S7（贸易）S8（盟约/从属）S17 | M7 |
| **S10 革新** | **半跑**：AI 选题 + 研究点在跑；`mastery` 恒 0、`unlockRefs` 120 条零消费者（本轮亲验：`unlockRefs` 在 `Assets/Scripts` 全库仅 1 命中 = 定义）、玩家入口 0 | Content（186 条） | S6（法律门槛）S7（配方/建筑）S5（政体组件）S3（阶层涌现） | M6 |
| **S11 文化信仰** | **半跑 + 重构中**：文化阶段演化是唯一活链；相似/分离/融合/传播全 0 调用；`Traditions.json` 13/13 条 `factors` 为空 | S3 S5 | S18（信仰挂载）S6（习惯法源）S3（Artisan 门槛） | M9、WP2/WP3 |
| **S12 表现与工具** | **半跑**：真面板 5 个（+1 个静态文本类）；22/29 项调度无观测面；本地化 en/zh 135 条、de/es/fr/ja/ko 各 0 | 全部读模型契约 | 玩家全部动词 | M10 |
| **S13 地基** | **在跑**：SaveVersion 17、29 项调度、预算 4–12ms；F1 确定性 RNG **已改完**（`GameWorld.Core.cs:126-128` 已委托 `Core.DeterministicRandom`），但**未编译验证** | 无 | **全部** | M1、WP6 |
| **S14 生物学基底** | **两层不对称**：DNA 在跑（6 基因座/重组突变/入档）；**种族层空**（`RaceDefs.json` 0 条，`raceId` 恒 0 共 4 处）⇒ 无群体分化 | Content（`Race`，0 条） | S4（个体）、S3（群体基线/生育率） | M3（群体）/ M8（个体→决策） |
| **S15 计划与执行** | **半跑**：框架完整，10 类里 **1 类在转**（Research）；Construction 有执行器无创建者；**D32 已裁到 3–4 类** | S4（发起者）S7（资源）S13（入档） | S5 S6 S8 WP1 WP5 | 横切（D32）；WP1/WP5 |
| **S16 头衔与官职** | **半跑，设计的 5 条只落地 1 条**：三模式实为单分支（恒绑 `Highest(Landed)=basileus`）、29/36 字段无生产者、实例只在 Obsolete 存档器里 | S6 S5 | S4（继承）S6（立法主体）S17（持有） | M5/M8、WP3 |
| **S17 关系层** | **空转**：17 类型 + 投影 + 校验 + 存档策略全在，**生产零写入、消费者 0**；已是第 6 套关系存储 | S5 S16 S4 | S6（义务）S7（朝贡）S8（附庸） | 横切（D28）；WP1/WP5 |
| **S18 信仰大系统** | **重构中**：`Religions.json` 44 条 + 每 id 一个 `FaithSystem` **正是 faith 层**；上缺 `religion/family`、下缺 `denomination/lineage`；教义 46 条平铺、零接线 | S3 S5 | S6（特许权/教理）S7（教产）S4（个人层） | M9、WP2/WP3 |

> 读法：**S13 是唯一所有人都依赖的**（所以 M1 先行）；**S15（过程载体）/ S17（关系容器）是横切件**——它们不新增世界状态，只提供"过程"与"关系"两类容器，因此可按需接入、不必独占里程碑；**S8 现状是空转而非半跑**（军队实体零生成），这是 M7 工作量被低估的主要来源。

---

## §2 联系矩阵（系统 × 系统，边上写**具体传递物**）

> 图例：`✅已接` = 有生产写入 + 有下游消费者；`⚠️半接` = 只通一半（缺入口/缺消费者/缺入档之一）；`❌未接` = 有字段无链路，或有链路无生产者。

| # | 连接（从 → 到） | 传递物（字段 / 契约 / 方法） | 状态 | 证据 |
|---|---|---|---|---|
| 1 | S1 地形 → S8 行军 | `TileData.movementCost`（地形→行军成本） | ❌ | 唯一写者 `GameWorld.Simulation.cs:497` 在死路径；读档硬写 1f（A §2） |
| 2 | S1 气候/肥力 → S3 承载力 | `TileData.fertility` | ⚠️ | 在跑，但 `SwarmAI.cs:62` 对 0–1 量纲 **减 2f** ⇒ 一次接触永久归零（L §3） |
| 3 | S1 河网 → S7 河运 / S8 渡口 | `riverStreamOrder/Width/Depth/Current/Flow` | ❌ | 五字段**零生产者**，3 处消费者读恒 0（A §2） |
| 4 | S1 灾害 → S3/S7 | `annualTemp / fertility / soilHumidityPct` | ✅ | `DisasterSystem.cs:282-337` 按季概率改场，随 tile 入档 |
| 5 | S2 聚落 → S5 控制半径/法令 | `settlementLevel` | ⚠️ | 永不推进 + 不入档（读档回落 LevelI，B §2.4） |
| 6 | S2 宗主控制 → S6 法令逐级下达 | `controllerAnchorId / controlledAnchorIds` | ✅ | `DecreePropagationSystem.cs:68-74` 真读（但控制状态不入档） |
| 7 | S2 宗主控制 → S7 税收/贸易抽成 | `GetTaxSiphonModifier(0.9) / GetTradeSiphonModifier(0.8)` | ❌ | 两函数**零调用者** ⇒ 控制一个村不产生收益（B §4-6） |
| 8 | S3 人口块 → S4 阶层/职业分化 | `PopulationBlock.socialClass → IdentityProfile` | ✅ | `SocialDifferentiation.cs:226-247` 每日重配 |
| 9 | S3 人口 → S5 税基 | 人口总量（无传递物） | ❌ | 税基 = `fertility × development × 10`，**不含人口、不含控制度**（F §2） |
| 10 | S3 人口 → 饥荒事件 | `库存 < 人口×0.01` | ✅ | `GameWorld.Simulation.cs:88-101` |
| 11 | S4 角色属性 → S10/S5/S6/S8 | `GetEffectiveStats(...)` 六维 | ✅ | 4 个真下游：突破概率、派系力量、法令传播、行军速度（C §2） |
| 12 | S4 君主指针 → S5 继承 / S16 头衔转交 | `RealmData.monarchId` → `GetSupremeRulerId()` | ❌ | 开局**从不赋值** ⇒ `ExecuteSuccession` 早退（C §4-2） |
| 13 | S4 婚姻 → S4 生育 / S17 人际 | `CharacterData.spouseId` → `LinkSpouses` | ❌ | `Marry` 全仓 1 命中（=定义，本轮亲验） |
| 14 | S5 政权 → S7 建造/征兵支付 | `realm.treasury -=` | ⚠️ | `BuildingSystem.cs:162` 会扣，但 `BuildBuilding` 零调用者 |
| 15 | S5 政体组件 → S5 区划层数 / S12 AI 扩张 | `LawComposition.expansionism / administrativeCapacity` | ✅ | `GameWorld.Simulation.cs:150`、`AIDecisionSystem.cs:37` |
| 16 | **S6 法律 → S7 经济（税率乘子）** | `LawEffectRuntime.GetTaxMultiplier` | ✅ | `PoliticsSimulationSystem.cs:86` 每日结算 |
| 17 | S6 法律 → S8 兵源 | `LawEffectRuntime.GetManpowerMultiplier`（49 法中 **27 法**带非 1 值） | ❌ | 唯一调用者 `ManpowerSystem.cs:83`，该重载仅测试调用（D §4-7） |
| 18 | S6 法律 → S5 稳定/合法/动乱 | 年度 `stability / legitimacy / unrest` | ✅ | `laws.effects.yearly`（`GameWorld.Simulation.cs:807` → `:373`） |
| 19 | **S6 法律 ← 玩家推行（回写）** | `RealmLawState → LawComposition` 派生 | ❌ | 只有下行（`LawReform.cs:32`→`ForceReformSync`），**无上行** ⇒ 改法不影响治理（D §4-2） |
| 20 | S6 法令 → S2/S5 逐级合规 | `DecreePropagationSystem.Resolve` 五档合规度 | ✅ | 传令官链 + 失联判 Failed（J §3） |
| 21 | S7 库存 → S3 口粮/饥荒 | `TradeCenter.inventory` | ⚠️ | 消费侧在跑（`EconomyManager.cs:419`）；**生产侧不存在** ⇒ 1000 单调归零（F §4-1） |
| 22 | S7 库存/国库 → S8 军需 | `TreasuryLedger.TryPay` 扣物产 + 货币 | ✅ | 唯一贯通的"钱→货→人"链：传令官（`GameWorld.Simulation.cs:671-686`） |
| 23 | S7 税率 ← S5 政权 | 全局 `TaxSystem` vs `realm.taxSystem` | ⚠️ | 算法用**全局**实例（`EconomyManager.cs:485`），玩家/法律/AI 都改不了（F §2） |
| 24 | S7 价格 → S7 贸易/套利 | `SettlementMarket.GetPrice` | ❌ | 无调用链；`TradeRoute` 生产期从不创建（F §4-2） |
| 25 | **S10 革新 → S6 法律门槛** | `requiredInnovationIds` | ✅ | 2026-11-09 修复：`LawContracts.cs:65,229,258` 传真集合 |
| 26 | S10 革新 → S7 配方/建筑/单位 | `unlockRefs`（120 条引用 / 89 条非空） | ❌ | 运行时消费者 0（本轮亲验：`Assets/Scripts` 仅 1 命中 = 定义） |
| 27 | S10 革新 → S3 阶层涌现 | `SocialClassAvailability.IsClassAvailable` + `ClassEmergenceEvents` | ⚠️ | 机制在跑，但 `mastery` 恒 0 ⇒ 167/186 前置不可达 ⇒ 锁死 19 条根革新 |
| 28 | **S14 种族基线 → S4 DNA 表达** | `locusFrequencies / genomeId / 三项基准` | ❌ | `RaceDefs.json` 0 条；全族群共用 `genome_human` + 缺省频率 0.5 |
| 29 | S14 DNA → S3 疫病伤害 | `dnaExpression.resistanceOffset → individualResistance` | ✅ | `DiseaseSystem.cs:203-209` |
| 30 | S15 计划 → S10 研究 / S2 建造 | `PlanType.Research` / `Construction` | ⚠️ | 研究在转；`CreateConstructionPlan` 零调用（H §2.3） |
| 31 | S16 头衔 → S5 继承法 | `definitionId → Highest(Landed)` | ❌ | 恒绑 `title_basileus` ⇒ 三模式退化为单分支（M §5-1） |
| 32 | S16 官职 → S5 法令传递 / 摄政候选 | `RealmData.officeHolders`（6 席） | ⚠️ | 在跑，但补缺唯一调用在**战末**（`GameWorld.Simulation.cs:880`）⇒ 开局 0 官职 |
| 33 | S17 关系 → S6 义务 / S7 朝贡 / S8 附庸 | `EntityRelation{type, obligations}` | ❌ | 生产写入 0、消费者 0（RELATION_LAYER §2） |
| 34 | **S18 信仰 → S3 人口信仰构成** | `faithId / adherence` | ❌ | `stateReligionId` 无生产写入 ⇒ 传教恒早退（`GameWorld.Simulation.cs:234-237`） |
| 35 | S18 教义 → S4 婚姻/性别、S6 继承/司法 | `DoctrineOption.bindsTo[]`（八接线点） | ❌ | `DoctrineOptionDef` 只有 5 字段：无分组/无档位/无参数/无效果（RELIGION_V2 §0-5） |
| 36 | S18 灵性满足 → 封圣 | `spiritualFulfillment ≥ 80` | ❌ | 唯一后果，但 `highPriestCharacterId` 恒 −1 ⇒ 双保险恒 null（E §4-4） |
| 37 | S11 文化传统 → S3 阶层 | `trad_craft_guild → Artisan 副类` | ✅ | **全库唯一**"传统内容改变可观察量"的活链（E §1） |
| 38 | S11 文化 → S5 政体形态 | `mobilityType → RealmForm` | ⚠️ | 在跑，但 `realmForm` 不入档 ⇒ 读档回落定居（D §4-5） |
| 39 | S9 外交关系 → S12 AI 宣战 / 地图着色 | `relation ≤ 30` | ✅ | `AIController.cs:337`、`MapRenderer.Colors.cs:230` |
| 40 | S9 外交四表 → S13 存档 | `relations/alliances/treaties/casusBelli/subordinations` | ❌ | **全部无 DTO** ⇒ 读档随机重置（G §4-3） |

> 40 条中：**✅ 14 条 / ⚠️ 9 条 / ❌ 17 条**。**❌ 里至少 9 条是"上游已做、下游没接"**（#17 #19 #26 #28 #31 #33 #34 #35 #36），另有 3 条是反向（下游消费者真实存在、上游从未生产：#1 #3 #7）——这两类正是"半跑是默认状态"的机制来源。

---

## §3 优先级（P0 / P1 / P2）

**判据（按权重）**：① **是否让世界通电**（产生可观察的世界变化）> ② **是否不可逆**（不做则后续成果蒸发）> ③ **是否被别人依赖** > ④ **成本**（越低越前）。

**必须明确回答的三问**：
1. **第一件：P0.1 存档 + 确定性闭合**。判据 ②+③ 双重命中：不闭合，后面每片成果在读档时蒸发（已丢 20+ 字段，含 `settlementLevel` / 外交四表 / 头衔 / 编年史 / `primaryCultureId`），长跑验收不可复现；且 F1 已改完（`GameWorld.Core.cs:126-128` 已委托 `Core.DeterministicRandom`），这是最短路径。
2. **第二件：P0.2 六个单点开关**。判据 ④ 最强（每项 ≤1 调用点）+ ① 强（各自点亮一整域），且它是 P0.3/P1 的**观察面**——没有继承、国教、法律回写，就看不出生产与财政改动的效果。
3. **第三件：P0.3 物资生产入流**。判据 ① 最强：税基、阶层、价格、军需、建筑全部以「产出」为输入，现在这个输入是空的。

| 档 | 项 | ① 通电 | ② 不可逆 | ③ 被依赖 | ④ 成本 | 归属 |
|---|---|---|---|---|---|---|
| **P0.1** | **存档 + 确定性闭合**：全部 RNG 走 seed 派生；`PlayerRealmId` 赋值并入档；`Chronicle` 入档；外交四表入档；头衔实例与 `primaryTitleKey` 迁入 `SaveSystem`；`settlementLevel`/`realmForm`/`heirId`/`primaryCultureId` 入档；补 5 类 DTO（plans/AI/mastery/司法/调度）；SaveVersion→18（**需单独批准**） | 中 | **极高** | **全部** | 中高 | M1 + F2 + D13 + WP6 |
| **P0.2** | **六个单点开关**：`monarchId` 开局赋值；`stateReligionId` 写入点（建国/改宗）；`LawComposition` 派生器（`LawState→Composition`，D7 方案 A）；`CreateArmy` 生产调用；`highPriestCharacterId` 生产者；`mastery` 的 `Learn/SetMastery` 接上练习与突破 | **高** | 低 | 高 | **极低** | M8-① / M9-① / M5-① / M7-① / M6-① |
| **P0.3** | **物资生产入流**：地块资源产出 → 产能 → 市场库存；配方消耗/产出闭环；贸易路线按供需差创建；保质期用 `shelfLifeDays` 而非统一 ×0.999 | **极高** | 中 | 高（S5/S8/S3） | 中 | M3a + F + `ResourceGenerator`（A-20） |
| **P0.4** | **M0.5 历史行为验收台**（与 P0.1 **并行**）：N seed × 100 年，三张表（日耗时/内存/函数热点）+ 指标导出 + 断言脚本 + 基线快照 | 低 | 低 | **每个里程碑的验收** | 中 | M0.5 + D19 |
| **P1.1** | **人口单源 + 聚落生命周期**：D6 人口块为权威、聚落人口改派生聚合（要迁 45 读点/23 文件）；等级推进/毁灭/废弃三端口接触发源 | 高 | 中 | 高 | 高 | M3b + D6 |
| **P1.2** | **财政真源 + 税率可写 + 收支表**：`realm.taxSystem` 成唯一税率源；税基接人口与控制度；`treasury` vs `vault` 二选一；四轴需求报告（D25） | 高 | 中 | 高 | 中高 | M4 |
| **P1.3** | **法律回写 + 生命周期 + 教理合并**：`LawState→Composition` 落地为派生器 + 对拍测试；八态补 `Consolidated/Fundamentalized` 写入方；两座桥（习惯↔法）；**WP3 教理八接线点与 M5 人格效力合并为一个施工项** | 高 | 中 | 高 | **最高** | M5 + WP2/WP3 + LAW_LIFECYCLE |
| **P1.4** | **革新解锁**：mastery 链（P0.2 已点）+ `unlockRefs` 接 3 类消费者（配方/建筑/法律）+ 革新面板 + 研究计划入档 | 中 | 中 | 中（S7/S6/S5） | 中 | M6 |
| **P1.5** | **军队 + 战争终局**：征兵（人力+物资+财政）→ 实体 → 行军 → 接战 → 和约 → 占领；修白和阈值与日域上限的对齐；海军写侧；军队上图 | 高 | 低 | 中（S9/S5） | 中高 | M7 |
| **P1.6** | **角色消费者**：`Marry` 接线 → 关系/纽带/生育；`FindBestGeneral/CalculateRuleAbility` 接选将/立法门禁；角色+家族面板；威望/恶名生产端 | 中 | 中 | 中 | 中 | M8 |
| **P2.1** | 文化分裂/融合/传播（E 域 P0-2）+ `Traditions.json` 的 `factors`（内容，非代码） | 中 | 低 | 低 | 中 | M9 |
| **P2.2** | 信仰六层（`religion/denomination/lineage`）+ 教义 schema（group→option→parameter→binding） | 中 | 低 | 低 | 高 | WP2 + S18 |
| **P2.3** | WP4 信息传播（谁知道什么/命令延迟）+ WP5 Intrigue（须先有 S19 地图知识） | 中 | 低 | 中 | 高 | WP4/WP5 |
| **P2.4** | 内容外提（F4，18 张表）+ Mods 通路（F5）+ 包校验器接启动 | 低 | 低 | 中（可模组化） | 中 | M10 前置 + D18/D23 |
| **P2.5** | 架构剩余 A–F（见 §4.4 末表）+ 双源清理（`GameSaveSystem` / legacy `DnaData`） | 低 | 低 | 中 | 中高 | WP6 |
| **P2.6** | 性能：Compute/Apply 两段式 tick、函数级插桩、**查询路径不写模拟状态**（`influenceTier`） | 低 | 低 | 中 | 中 | D19 + PERF_LESSONS |

> **反排序说明**：M9（文化信仰）、M10（表现）在旧总纲里靠后，本表仍靠后——但理由变了：**不是"它们不重要"，而是它们依赖的前置（S19 知识层 / 生产链）不存在，先做等于空转**（同 D32 对 `Exploration` 的判决口径）。

---

## §4 与架构同步的施工表

### 4.1 先更正一个事实：不是 15 个 asmdef，是 **16 个**（本轮 glob 亲验）

| 层 | asmdef | 现引用（实测） |
|---|---|---|
| L0 | `Core`（`core.asmdef`） | 无 |
| L0 | `Save.Contracts`（`Assets/Scripts/Save/Save.Contracts.asmdef`） | Core |
| L1 | `Content` | Core |
| L1 | `World` | Core、**Save.Contracts** |
| L2 | `Laws` / `Faith` / `Culture` | Core、Content（三片叶子，✅ 干净） |
| L3 | `Simulation`（`sim.asmdef`） | Core、World、Content、Laws、Faith、Culture、**Save.Contracts** |
| L4 | `Infrastructure` | Core、Save.Contracts、World、**Simulation**、Content、**Faith、Culture** ← ⚠ 反向 |
| L4 | `Rendering` | Core、Content ← ✅ 干净 |
| L5 | `App.Contracts` | Core |
| L6 | `App` | Core、App.Contracts、Infrastructure、World、Simulation、Rendering |
| L6 | `UI` | Core、Rendering、**Infrastructure、World、Content、App**、App.Contracts ← ⚠ 反向+越层 |
| — | `Editor` / `Tests` / `Tests.PlayMode` | 全部（可接受） |

> 后果：**`CURRENT_ARCHITECTURE.md` 与 `SYSTEM_MASTER_PLAN.md` §2 的"15 asmdef"已过期**（`Save.Contracts` 在 A0 建立，且 `World/Simulation/Infrastructure` 已引用它）⇒ 进 §5 冲突 5。

### 4.2 P0 施工（每项 → 程序集 → 文件级动作 → 对齐 A–F）

| 优先级 | 程序集（asmdef） | 具体动作 | 对齐 |
|---|---|---|---|
| **P0.1** 存档闭合 | `Save.Contracts`（新，已有） | **新增** `DiplomacySaveData` / `TitleInstanceSaveData` / `ChronicleSaveData` / `PlanSaveData` / `KnowledgeSaveData`；`SettlementBlockSaveData.children[]` → 同表 `blocks[] + parentBlockId`（扁平化，解 Unity 序列化深度 10） | **A0 已完成**、A1 续做、**§7.5 扁平化** |
| | `Simulation` | **新增** `WorldSaveCapture : ISaveSource`（把 `SaveSystem.cs` 的捕获/恢复逻辑搬入）；`TitleInstanceRegistry.Clear()` 接到活路径；`PlanSystem`/`ResearchPlanSystem`/`AIManager` 各加 Capture/Restore | **A2（契约反转）**、D（删 `GameSaveSystem`） |
| | `World` | `SettlementPersistence` 等文件里的映射器（`From/Capture/Restore`）**迁回** `World.Settlement`，DTO 只留字段（`ARCHITECTURE_UNFINISHED` A1 拆分表逐项） | **A1 拆分表**（映射器与数据分离） |
| | `Infrastructure` | `SaveSystem` 瘦身为「文件读写 + JSON + 版本迁移 + 原子写」；**references 收窄到 `Core + Save.Contracts`** | **A3 收口判据** |
| | `Core` | `DeterministicRandom` **已建**（F1，本轮已核）；后续 RNG 一律走它 | F1 |
| **P0.2** 六开关 | `Simulation` | `GameWorld.Generation.cs` 开局写 `monarchId/consulId`；`CreateInitialRulers` 的 `faithId` 由文化推导（F8）；`highPriestCharacterId` 生产者；`CreateArmy` 调用点；`InnovationKnowledgeSystem.Learn` 接线 | **C**（状态写入需契约）、F7/F8 |
| | `Laws` | **新增** `LawCompositionDeriver`（`RealmLawState → LawComposition`，D7 方案 A）+ 对拍测试 | D20 ①（单一真源） |
| | `Faith` | 只出「国教写入 + 快照重建」两个契约；**不新增对 Laws 的引用**（保持叶子） | **F**（Host 与叶子同层） |
| **P0.3** 生产链 | `Simulation` | **新增** `SettlementProductionSchedule`（或挂进 `economy.daily`）；地块资源产出 → 产能 → `TradeCenter.AddGoods`；配方 `TryCraft` 生产调用；贸易路线按供需差创建 | 无 A–F 项（属功能补齐） |
| | `World` | `GetOrCreateMarket` 生产调用；`TradeCenter` 成为库存**唯一写入口**（`AddGoods` 单点） | F3（单源） |
| | `World.Terrain` | `ResourceGenerator.GenerateAll` 接回生成管线（现 A-20 空转） | 无（A 域 P0-4） |
| **P0.4** M0.5 | `Tests` / CLI | 新增 headless 长跑 runner + 三表导出 + 基线快照；`Simulation` 暴露 tick 计时与函数级插桩钩子 | 无（新增工具） |

### 4.3 P1 施工

| 优先级 | 程序集 | 具体动作 | 对齐 |
|---|---|---|---|
| **P1.1** | `World` | `SettlementData.population` 降为派生属性（聚合 `tile.populationBlocks`）；`SettlementTypologySystem` 三个推进函数接 daily | F3（人口单源）、D6 |
| | `Simulation` | `SettlementSimulationSystem` 补成长段 + `DestroySettlementCommand`/`AbandonTile` 派发者；`SettlementQueryAdapter` 收口 45 个读点 | 无 |
| **P1.2** | `Simulation` | 删全局 `TaxSystem` 的算法消费，`realm.taxSystem` 成唯一税率源；税基接人口×控制度；`TreasuryLedger` 与 `realm.treasury` 二选一 | D20 ⑤（不跨层写字段）、L §3 |
| | `UI` + `App.Contracts` | 新增收支表：契约放 `App.Contracts`（只读），面板放 `UI/Panels` | **B**（UI 只依赖 Contracts） |
| **P1.3** | `Laws` | 八态生命周期转移写入方；`precedence/overrides/normKind/scope` 接消费者；`fundamentalizedLawIds` 读写 | D20、LAW_LIFECYCLE §2 |
| | `Faith`（schema）+ `Simulation`（handler） | 教义 `DoctrineGroup/Option/ParameterSpec/BindingPoint` 入 `Faith`；**八接线点的 handler 落在 `Simulation` 的 `DoctrineBindingHost`**，Faith 只出不可变快照 | **F**（叶子不反向依赖） |
| | 合并项 | 婚姻/性别接线点与 M5「人格效力」共用同一 handler：读 `SocialIdentityFlags` × `precept.matrimony` ⇒ **一次实现，两处验收** | D25 + 冲突 2 |
| **P1.4** | `Simulation` + `UI` | `unlockRefs` 接 3 类消费者（`RecipeCatalog` 门禁 / 建筑解锁 / 法律门槛）；革新面板消费已存在的 `IInnovationQuery` | **B** |
| **P1.5** | `Simulation` | `Army.DailyTick` 注册进调度；战争终局判据与日域上限对齐；`OccupyTile` 接上；海军写侧 | 无 |
| | `Rendering` | 军队/行为体标记绘制（`showArmyMarkers` 现只有位标志、无绘制代码） | 无（J 域 P0-1） |
| **P1.6** | `Simulation` | `Marry` 接线 + `CharacterRelation.opinion` 进 `ActionGate`；`FindBestGeneral/CalculateRuleAbility` 接消费者 | D15（人物为体） |
| | `UI` | 角色面板/家族树**改为运行时构建**（`UIFactory`），删除场景 `{fileID:0}` 双源 | **B / §4.4-6（场景与代码双源）** |

### 4.4 P2 施工（架构主线）

| 项 | 动作 | 对齐 |
|---|---|---|
| **D** 存档双轨 | 删 `GameSaveSystem` + `GameSaveData`（642 行，`[Obsolete]`），字段并进 `SaveData`；**前置：P0.1 已迁出头衔/编年史** | ARCH D |
| **E** 遗传双轨 | `GenomeInstance` 为唯一真源，`LegacyDnaAdapter` 只用于读档迁移一次 | ARCH E |
| **B** UI 反向 | `MyriadCreation.UI.asmdef` 去掉 `App`/`Infrastructure`/`World`；`MyriadCreation.App.asmdef` 增 `UI` 引用（App 装配 UI） | ARCH B |
| **C** 状态无守卫 | `RealmData` 37 个 public 字段收成「查询 + 命令」，先做 `treasury`/`stability`/`population` 三项 | ARCH C、D20 ⑤ |
| **F** Host 适配层 | `LawHost/FaithHost/CultureHost/ContentHost` 移出 `sim.asmdef`，Simulation 只依赖 Host 契约 | ARCH F |
| **A** 收口 | `MyriadCreation.Infrastructure.asmdef` references = `Core + Save.Contracts`（判据：删掉该引用仍能编译） | ARCH A3 + §8 判据 |
| **F4/F5** 内容 | 18 张表逐张外提（建筑 29 / 单位 14 / 商品 57 / 灾害 12 / 政体门槛 35 / 阶层门槛 / 绰号 81 / …）；修 `Mods/` 布局判定（`ContentDefinitionIndex.cs:250`）+ 校验器接启动 | D18/D23、I 域 P0-1 |
| **性能** | Scheduler 引入 `Compute → Apply` 两相；`MapRenderer` 查询路径不再写 `influenceTier` | D19、PERF_LESSONS §2 |

---

## §5 冲突与待决

### 5.1 计划之间的矛盾（6 条，每条给处置）

| # | 冲突 | 位置 | 处置（本文裁定） |
|---|---|---|---|
| **1** | **D32 删 `Military/Trade/Diplomacy/Migration`，但 M7 与两处子计划仍按 Plan 口径写** | `DECISIONS.md:177-179` / D32 vs `SYSTEM_MASTER_PLAN.md:558`「凡需时间+人+物资一律用 Plan」/ `BELIEF_SYSTEM_MODEL.md:381`「动员→征召能力（走 PlanType.Military）」/ `INTEGRATION_PLAN.md:131` 自认 | **M7 全部走 `Warfare` + `DiplomacyManager` + `ManpowerSystem`，不进 Plan**；`SYSTEM_MASTER_PLAN.md:558` 的"一律"改为「除战争/贸易/外交/迁徙四类（D32）」；`BELIEF_SYSTEM_MODEL` §9 的动员出口改为 `ManpowerSystem`。Plan 保留 `Research / Works(Construction) / Scheme(Intrigue) / Expedition(暂不启用)` + `planDefId` |
| **2** | **教理性别/婚姻规则 与 M5「人格效力」是同一件事**，会做两遍 | `INTEGRATION_PLAN.md:132` 自认；M5 施工 ⑦「人格效力接身份轴」 | **合并为一个施工项**（P1.3 第三行）：一个 `BindingPoint(group=marriage|gender) × SocialIdentityFlags` handler；M5 该项改为"读教义快照"，不再独立实现身份轴判定 |
| **3**（新，**已裁决**） | **入口口径互相否决**：`CHARACTER_COUPLING.md` 主张"建造/征税/征兵/审判/传教/外交**保持无入口**、税率**保持不能调**"；旧 M10 施工要求"按动词表补面板"、DoD 要求"11 项全部有入口" | `CHARACTER_COUPLING.md:129-132` vs 旧总纲 `:665-666` | **用户 2026-11-11 裁决 A 案（D33）**：走**授权→代理→执行→结果→追责**，细节靠"问人"；**M10 改为"授权 + 查询"口径**，税率保持不可直接编辑；不新建 6 个面板 |
| **4**（新） | **`denomination` 必挂 vs 可选**：D29 与 `BELIEF_SYSTEM_MODEL` §4（11:50 定稿）判"必挂"；同文件 §5.6 又写"denomination 不是层级，而是可选的组织覆盖层"、§8 写"下补 denomination（可选）" | `DECISIONS.md:232`（D29 11:50 更正）vs `BELIEF_SYSTEM_MODEL.md:254,261,367` | **按 D29 = 必挂**（未分裂宗教给 `isDefault=true` 的默认宗派）；§5.6/§8 的两处标为**旧口径已作废**，进 §7 迁移 |
| **5**（新） | **"15 个 asmdef" 已过期**：实测 16（`Save.Contracts` 已建，且 `World/Simulation/Infrastructure` 已引用它） | `CURRENT_ARCHITECTURE.md`、`SYSTEM_MASTER_PLAN.md` §2 vs 本轮 glob 实测 | 全库口径改 **16**（13 运行时 + 3 工具/测试），§4.1 表为准 |
| **6**（新） | **M9 前置"M9 开工前必须裁决 D8"已过期**：D29 已把 D8 收口（faith 为可玩单位 + 上补 religion/family + 下补 denomination/tradition） | `SYSTEM_MASTER_PLAN.md:748,751`、`DECISIONS.md:50-59` vs `DECISIONS.md:205-232`（D29） | **D8 标记 superseded by D29**；M9 的阻塞项从"待裁决"改为"待 WP2 schema（P2.2）" |

### 5.2 需要用户拍板的 5 条

| # | 问题 | 选项（代价） | 默认（若不答） |
|---|---|---|---|
| ① | **入口口径**：玩家微操 or 授权代理？ | A 授权+问人（少 UI、多人物 AI，WP4/WP5 前置）／B 面板化（多 6 个面板 + 契约 + 测试）／C 折中（税率只读收支表、其余授权） | **已定 A（D33，2026-11-11 用户裁决）**；M10 口径同步改为"授权 + 查询" |
| ② | **存档路线**：本轮是否做 `ISaveSource` 反转（A1–A3）？ | A 全做（干净，估 P0.1 的大头）／B 只补字段、年后再反转（快，但 Infrastructure 反向依赖继续留 + 新字段继续散三层） | 按 **B 先补字段，A2/A3 与 P2.5 合并** |
| ③ | **数值纪律**：是否接受"无来源即 `TODO_M05`、加载期直接拒绝硬编码数字"作为门禁？ | A 接受（`tithe.rate=0.1` 加载失败，`TODO_M05` 通过且默认 0 + `failMode=block`）／B 先允许区间占位 | 按 **A**（RELIGION_V2 §5 已提出，与宪章原则 5 一致） |
| ④ | **D6 人口单源的迁移方式**：一次性切换 or 双写过渡？ | A 一次切换（45 读点/23 文件同批改，风险集中）／B 双写过渡（引入第 3 套人口，违反单一真源） | 按 **A** |
| ⑤ | **`SaveVersion` 17 → 18 是否批准** | 需单独批准（M1 已写明）；影响旧档迁移测试与备份策略 | 不批准则 P0.1 只能做"可重算字段"，头衔/编年史/外交仍丢 |

---

## §6 验收：什么条件下可以说"计划统一了"

1. **单一入口成立**：排期权只存在于本文件 + `DESIGN_CHARTER.md` + `DECISIONS.md` 三处；12 份子计划**全部**能被 §3/§4 的项编号索引到，**零孤儿计划**（规则见 `docs/README.md` §5.1）。
2. **每条 P0/P1 都有四件套**：目标程序集 + 文件级动作（新增/移动/改引用/新契约）+ 可失败断言 + 证据锚点。缺任一件不进主干。
3. **冲突清零**：§5.1 六条全部有处置（采纳/作废/待决且指定拍板项），无"两种口径并存"。
4. **接线表可审计**：§2 每条 ✅/⚠️/❌ 都能被一次 grep 或一次读档往返证伪；不允许"看着像通了"。
5. **数值无凭空值**：所有阈值/比率/权重带 `source` 或 `TODO_M05`；CI 拒绝二者皆无的参数。
6. **架构判据可执行**：`§8 判据（删掉一个程序集引用还能编译吗）` 对 `Rendering/Laws/Faith/Culture/Save.Contracts` 为「能」，对 `Infrastructure/UI` 在 P2.5 后也为「能」。
7. **迁移已落地**：§7 的合并/归档清单执行完毕，旧 `SYSTEM_MASTER_PLAN.md` 不再被任何文件引用为"现行"。

> 反例（不构成"统一"）：706 个测试全绿但世界第一年就饿死（F 域 P0-1）；法律面板能改法但治理参数不变（D §4-2）；里程碑表列得很齐而 §2 仍有 17 条边是 ❌。

---

## §7 迁移说明（旧文档 → 本文件）

| 旧件 | 处置 | 去向 |
|---|---|---|
| `SYSTEM_MASTER_PLAN.md` §0（六结论）/ §1（产品定义）/ §3.0–3.1（状态矩阵）/ §3.5（S15–S17）/ §4.4（耦合审计）/ §5（M0.5–M10）/ §6（DAG）/ §7（DoD）/ §8（待议） | **合并** | 分别并入本文件 §0 / §1 / §1 / §1+§2 / §4 / §3+§4 / §2 / §6 / §5 |
| `SYSTEM_MASTER_PLAN.md` §4.1（玩家动词表）+ §4.2（29 项调度语义：order/cadence/预算/延迟队列）+ §9（证据索引） | **保留为事实参考，不合并**（它们是"代码现状"而非"计划"） | 随旧件整份移入 `docs/archive/2026-11-10_superseded_master_plan/`，由本文件 §1/§2 引用 |
| `SYSTEM_MASTER_PLAN.md` 全文 | **整份归档** | `docs/archive/2026-11-10_superseded_master_plan/SYSTEM_MASTER_PLAN.md`（保留 §4.1/§4.2 作附录 A） |
| `PLAN_COVERAGE.md` | **归档**（其 10→3–4 判决已被 D32 完整吸收，无独立排期价值） | `docs/archive/2026-11-10_superseded_master_plan/` |
| `FOUNDATION_FIXES.md` | **拆分**：F1 标"已完成（`GameWorld.Core.cs:126-128`，待编译验证）"留档；F2–F8 并入 §4 P0.1/P0.2 | 原文留档，不再作施工口径 |
| `COVERAGE_GAP_AUDIT.md` | **部分并入**：信息/后勤/价格三条 P0 → §3 P2.3 + P0.3；其余留作待排期 | 原文留档 |
| `BELIEF_SYSTEM_MODEL.md` §5.6（"denomination 可选"）与 §8（"下补 denomination 可选"） | **标作废**（与 D29 冲突，见 §5.1-4） | 原文加作废标记，不删 |
| `INTEGRATION_PLAN.md` §5（冲突自认段） | **并入** §5.1-1 / §5.1-2（已给处置） | 原文留档 |
| `DOC_DISPOSITION.md` 判"已取代"的 `UNIFIED_MASTER_PLAN.md` / `MASTER_COMPLETION_PLAN.md` / `PLAN_INDEX.md` | **不进计划层**，按该表处置 | 已在 `docs/archive/2026-11-10_pre-planv3/` 或标"仅索引" |
| 其余 7 份子计划（`RELIGION_V2` / `CHARACTER_COUPLING` / `LAW_LIFECYCLE` / `RELATION_LAYER` / `ARCHITECTURE_UNFINISHED` / `PERF_LESSONS_CK3` / `DOC_DISPOSITION`） | **全部保留**，降级为「WP 施工细节」（`docs/README.md` §2 已如此登记） | 原位；本文件只引用不复制 |
| `docs/README.md` §1（权威件 3 份） | **需同步改**：`SYSTEM_MASTER_PLAN.md` → `MASTER_PLAN_V2.md`（该改动不属本轮授权范围，列为交接项） | 交接项，非本轮动作 |

---

## 附：本文相对旧总纲的净增量（便于评审）

1. **系统全图改为"依赖双向"**（旧总纲只写"归属里程碑"，不写谁依赖谁）⇒ 让顺序可推、不必靠记忆。
2. **联系矩阵 40 条带状态**（旧总纲的 DAG 只有 10 个节点、无传递物、无接线状态）。
3. **优先级判据显式化**（通电 > 不可逆 > 被依赖 > 成本），并对旧总纲的 M 顺序给出**维持/调整**理由（M7/M9 成本下调，M3/M5 上调）。
4. **架构动作落到 asmdef + 文件级**，并更正 15→16 个程序集（旧总纲 §2 与本轮实测不符）。
5. **冲突从 2 条扩到 6 条**（新增入口口径、denomination 必挂、asmdef 计数、D8 过期四条），每条给处置或指定拍板项。
6. **§7 迁移说明**（旧总纲无；导致"哪份还算数"每轮都要重议）。

---

## §9 代码复核与统一规划（2026-11-13）

> 本节是在读完当前代码与内容后写的**统一规划**，不替代 §1–§8 的架构判断；两者冲突时，以本节 §9.2 的设计原则与 §8 的宪章判据为准，并记入 `DECISIONS.md`。

### 9.1 复核事实（本机实测）

- **规模**：`Assets/**/*.cs` 共 1154 个；`StreamingAssets/Base` 40 张 JSON；19 个 asmdef；EditMode 891 条测试（889 通过，2 条 Explicit 默认跳过）。
- **程序集层次**：Core → Content → {Laws, Faith, Culture} → World → Simulation → Rendering → UI → App；契约层为 `Save.Contracts` / `App.Contracts`；`Save.Runtime` 承载存档实现；`Simulation.Hosts` 承载 Faith/Culture 适配器；`Settings` 与 `Audio` 独立；`Infrastructure` 已删除。
- **代码分布**：Simulation 486（State 78 / Systems 147 / Laws 36 / Units 30 / Genetics 26 / WorldState 41 / 其余 128），Content 188，World 98，Core 83，UI 33，Rendering 13，SaveRuntime 16，App 14。
- **已跑通的链**：存档单源（`SaveSystem+SaveData`）、人口块单源（`PopulationLedger`）、货币/稳定度单源（`RealmTreasury/RealmStability`）、文化传播/融合/分裂、教义/宗派/正统/局势、战争契约/探测/占领、革新解锁五类消费者、调度预算与两相、Mods 布局/校验/依赖。
- **验证基线**：`build.cmd` `EXIT=0`、`889/891`、零 `error CS`；`longrun` 2 seed×100 年 `1/1`。

### 9.2 本规划必须遵守的原则（从宪章 §2 落到本轮）

1. **机制优先于数值**（原则 5）：只改容量/成本/延迟/占有/信息，不新增“+X% 产出/+N 稳定”的驱动字段。
2. **单一真源**（原则 8）：人口、货币、稳定度、法律、遗传、存档、影响力各只有一条权威链；不新建平行库存/关系/战争/人口。
3. **数据驱动**（原则 6）：新增/修改内容不改 `.cs`；新表必须可被 `Mods/` 覆盖。
4. **过程优先**（原则 11）：长周期变化用 Plan/过程承载，允许中断与失败，不点一下立即生效。
5. **确定性**（原则 9）：随机只发生在条件层（气候/灾害/出生/疾病），由 world seed 派生。
6. **性能是约束**（原则 7）：新系统必须声明 O(N)/O(dirty)/O(1)，并挂预算或增量；查询路径不写模拟状态。
7. **干预只改条件**（原则 10）：不设代价层，不直接改结果。
8. **可断言**：每项必须有 EditMode 测试或长跑断言，不能只靠“看起来通了”。

### 9.3 现状缺口（按因果链，不按文件）

| # | 缺口 | 证据 | 影响 |
|---|---|---|---|
| G1 | **农业产出链缺失** | 自然资源产出走 `ResourceGenerator → TradeCenter`；小麦/稻谷等作物不是 `isNaturalResource`，肥力/NPP 不进入产出；食物需求只在 `ClassNeedsSystem` 读库存 | 旱灾→减产→饥荒的经典因果链是断的（原则 5/9） |
| G2 | **官职无语义** | `RealmData.officeHolders` 0–5 只有槽位；`OfficeTitleCatalog` 仅命名；无法表达 rank/modules/任命/薪酬 | 治理、法律提案、行政效率没有人物载体 |
| G3 | **生态只到 NPP** | `EcologyModel` 已算 NPP，但植被演替/动物/过牧/侵蚀未接；`fauna.js` 的动物链未实现 | 生态反馈只有承载力一个出口 |
| G4 | **Plan 只有 2/10 在转** | `ResearchPlanExecutor`、`ConstructionPlanExecutor`；其余 8 类无执行器/创建者 | 远航、围城、变法、迁徙等仍是瞬时或缺失（原则 11） |
| G5 | **政权标识只有家族纹章** | `Heraldry` 已生成家族纹章；政权旗帜/徽记未接 | 地图/面板缺少政权可识别符号 |
| G6 | **城市只有块数据** | `SettlementBlock` 有区划/面积/城墙；无布局坐标/俯视图 | 无法做城市俯视或街道级可视化 |
| G7 | **信息/密谋缺失** | 只有 `CharacterKnowledgeRegistry` 的过期遗忘；无 S19 地图知识/密谋 | P2.3 的 WP5 未开始 |
| G8 | **本地化与内容尾巴** | UI 文案大量硬编码；少量历史常量无 `source/TODO_M05`；`DnaData` 仍作旧输入兼容 | 可模组化与验收 §6.5 不完全 |

### 9.4 统一施工序列（P3.0–P3.6）

#### P3.0 农业—食物链（最高优先，G1）

- **机制**：耕地/牧场/渔场是“生产场所”，产出 = 面积 × 肥力/NPP × 劳力 × 工具/革新 × 运输可达性；作物进入市场，人口块按需求消费；短缺→价格/配给→饥荒/不满。
- **不新增**：不建第二套人口、第二套库存；产出走既有 `TradeCenter`，消费走既有 `ClassNeedsSystem` 的需求输入。
- **验收**：同一 seed 下，旱灾年作物产出下降、库存下降、食物不满上升、饥荒事件可复现；关掉 NPP/肥力后链条断开（反向断言）。
- **依赖**：`TileData.fertility/npp`、`TradeCenter`、`SettlementProduction`。

#### P3.1 官职与治理（G2，借 TTHTOM `polity_profile`）

- **内容**：`Offices.json`：6 个槽位的 id/名称/rank/modules（central_office/general/personnel_authority/treasury/judicial/religious）/salary/appointment_order/candidate_gender/royal/scope/family_power/dismissal_resistance。
- **机制**：任命按候选约束与能力/关系；官职提供“职权”而非数值加成：谁能提案、谁审案、谁征税、谁能任免；`officeHolders` 仍是唯一真源。
- **验收**：任职改变法律提案权/司法管辖/税收执行者；读档往返；无“官职 +X%”字段。
- **依赖**：`OfficeTitleCatalog`、`LawRealmContext`、`JudicialSystem`、`TaxSystem`。

#### P3.2 生态演替与动物（G3，借 Earth Simulator `ecology.js/fauna.js`）

- **机制**：NPP→植被量（慢增快减）→过牧/侵蚀→肥力下降；动物种按 NPP 承载，捕食/驯化由具体物种参数驱动。
- **验收**：过度放牧 20 年后 NPP/肥力下降；移除草食动物后捕食者数量下降；全部 seed 派生。
- **依赖**：P3.0 的产出链、`MapActor`/`TileData`。

#### P3.3 Plan 执行器扩展（G4，原则 11）

- **首批**：Construction 生产调用者（AI/玩家下建造令）、Military 战役计划（行军/围城/撤退，接 `ArmySchedule`）、Expedition（远航）。
- **验收**：计划可被中断/失败并留编年史；参与者与需求可查；不新建第二套军队/建造系统。

#### P3.4 政权旗帜与城市俯视图（G5/G6，借 TTHTOM `flag_profile`）

- **旗帜（条件化）**：`BannerKind = None / MilitaryBanner / HouseholdBanner / StateFlag`；早期多数政权只有军旗，StateFlag 需民族主义/印刷/革命类革新与相应政体；只给有旗帜的政体生成，确定性 layout/bands/colors/saltire/canton。
- **城市俯视**：块数据→派生布局（城墙外环/道路骨架/区划按面积切分/建筑落位）→程序化 Sprite；布局不存档、读档重算。
- **纹章条件**：家族纹章不是普遍习俗，只有 `CultureData.heraldryEnabled`（内容/事件开启）的文化才生成；默认无。
- **验收**：同 `settlementId/blockId` 同图；旗帜与纹章可区分政权/家族；无旗/无纹章是合法状态。

#### P3.5 信息与密谋（G7，前置 S19）

- **先做**：地图知识层（谁知道哪块地/哪条路，由军队/商队/斥候写入，入档）。
- **后做**：密谋计划（PlanType.Intrigue）+ 信息延迟/伪造；不硬造无知识层的密谋。

#### P3.6 内容/本地化/架构尾巴（G8）

- 继续外提可编辑表（政体/阶层门槛等）；UI 文案走 Localization JSON；`RealmData` 可变字段逐步私有化；`DnaData` 仅保留旧档迁移；法律 Host 运行逻辑按需再拆。
- 所有新增阈值带 `source/TODO_M05`。

### 9.5 顺序与并行

- **串行**：P3.0 → P3.2（生态反馈依赖产出链）；P3.1 可与 P3.0 并行（不同文件面）。
- **并行**：P3.3 与 P3.4 可在 P3.0 稳定后并行；P3.5 等 S19；P3.6 持续穿插。
- **每批**：先读现有代码 → 写测试 → 实现 → `build.cmd` 全绿 → `DECISIONS.md/PROJECT.md` → 提交推送；不达全绿不开下一批。

### 9.6 本轮不做

- 不复制任何参考项目的代码/美术/数据格式（Azgaar MIT 仅借模型并注明；Earth Simulator/TTHTOM 只读分析）。
- 不为“全量”硬填空内容；不新增平行系统；不用抽象加成替代因果链。
- 不在没有消费者时引入新抽象层（F 的剩余部分按触发条件再开）。
