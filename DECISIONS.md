# DECISIONS — 计划 v3（系统总纲）

> 记录本轮的方案取舍与被否方案，避免走回头路。每条：候选 → 取舍 → 依据。

## D1 方案级抉择（开工前，一次给 3 案）

| 案 | 做法 | 优点 | 缺点 | 裁决 |
|---|---|---|---|---|
| A 保守：增量补丁式 | 沿用 `docs/plan-v3` 思路，逐系统补"缺口清单"，总纲只做目录 | 改动小，几天可见效 | 继续养 47 份冲突文档；读者仍需自己裁决冲突 | ✗ 否 |
| B 折中：先代码摸底再写计划 | 只读审计八大域 → 用代码事实合成新总纲 → 旧文档处置表 | 事实扎实、可执行；不写代码，风险低 | 审计成本高（8 域并行） | ✓ **采纳** |
| C 激进：推倒重来 | 删掉 docs/design 全部计划，从零写新设计书 | 文档债一次性清零 | 现有内容/决策大量丢失；无法验证；用户未授权删档 | ✗ 否（但保留"归档"作为软化版） |

- 采纳 B，理由：项目已有 1025 个 .cs 与 21 个内容域，真正的信息在代码里；文档债的根因是"计划先于事实"，解法只能是"先事实后计划"。
- C 的合理内核（文档债清零）被吸收为 M3 的**处置表 + 已定案归档**，不物理删除。

## D2 判定口径（防"过度宣称"）

- 一个系统算"**在跑**"必须同时满足三件：① 在主循环有 tick（`GameWorld.Simulation.cs` 注册）或被真实调用链驱动；② 有玩家入口（UI 面板/命令/按钮）**或**有 AI/规则自动驱动；③ 状态入档（`SaveSystem` 往返）。
- 只满足 ① 而缺 ② ③ → 记"**半跑（缺入口/缺存档）**"；只有数据结构无调用者 → 记"**空转**"；连数据都没有 → 记"**不存在**"。
- 依据：法律/革新审计已证明"有数据结构 + 有测试"≠ 可用；测试甚至能全绿而功能恒被 `null` 短路。

## D3 交付物形态

- 主产物是**一份**总纲 `docs/plan-v3/SYSTEM_MASTER_PLAN.md`，不是"N 份系统计划"。系统细节按需再裂变，但入口只有一个。
- 依据：`docs/PLAN_INDEX.md` 的初衷就是"唯一入口"，此前失败原因是计划分散在 47 份文件中。

## D4 旧文档处置策略

- 只做**处置表**（现行 / 取代 / 部分有效 / 仅语料），只有在 `PLAN_INDEX §3` 已定案的才物理移入 archive。
- 理由：批量移动不可逆风险高，且用户未授权删除；处置表已能消除"以哪份为准"的歧义。

## D5 本轮不改代码

- 理由：用户诉求是"重新规划"，改码会把"规划"与"实现"混在一轮，既无法验证也无法审查。
- 代价：总纲中每条缺口都只到"施工指令"级，不落地。下一轮以里程碑为单位施工。

## D6 人口权威单源（因 B 域审计新增）

- 事实：现存两套人口——`TileData.populationBlocks`（每日复利增长，入档）与 `SettlementData.population`（只在生成/踩踏时变，**不入档的等级**决定控制半径）。两者永不收敛（`B-settlement-population.md` §2.1/§2.4）。
- 决策：**人口块为权威**，聚落人口 = 其覆盖地块人口块的聚合派生量（只读缓存，允许重算）。理由：① 与既有决策"疾病以人口级为权威"（`TITLE_INHERITANCE_PLAN.md` §9）一致；② 地块级才承载地理差异；③ 聚合可重算，不需要新增存档字段。
- 影响：M3 必须先做这一步，否则"聚落成长"接不上真实人口；`SettlementData.population` 降级为派生属性。

## D7 法律双源合一的方向（因 D 域审计新增）

- 事实：`RealmData.laws`（`LawComposition`，被 AI/区划/继承消费）与 `RealmData.lawState`（`RealmLawState`，被玩家面板写）是两套真源，**只有下行同步**（D 域 P0-2）。
- 候选：A 以 `LawState` 为唯一真源、`LawComposition` 改为派生；B 以 `LawComposition` 为唯一真源、面板改写它；C 保持双源但补上行同步。
- 决策：**A**。理由：玩家可操作的层必须是可以被"改"的那一层；`LawComposition` 的维度/选项是"政体形态"，应由法律条目推导而非平行维护；C 只会让两个真源继续漂移。
- 影响：M5 的第一件事是"写一个 `LawState → LawComposition` 的派生器 + 对拍测试"，然后再谈新增法律。

## D8 信仰模型：文档口径 vs 代码实现（**待用户裁决，本总纲不擅自定**）

- 文档口径（`docs/PLAN_INDEX.md` §4 + `FAITH_AS_CULTURE_MODEL.md`，被列为"现行权威"）：**传统（Tradition）为主运行单位**；宗教层固定不消失；中间层可选；只借文化机制的"分裂 + 挂靠"。
- 代码实现（`E-culture-faith.md`）：**CK3 式两级** Religion(44 节点)→Succession(22)，只给 Succession 建运行时对象；8 个 Tradition 节点无运行时对象；`MiddleLayerDef` 全仓 0 命中；`ReligionCatalog` 的分裂/挂靠 7 个 API 全 0 调用。
- 两个口径不可同时成立。可选：
  - **E1 以代码为准**：承认现行实现是"宗教两级"，把 `FAITH_AS_CULTURE_MODEL.md` 降级为"未实现的设计"，M9 只做补齐（工作量小，但放弃"传统为主"的设计意图）。
  - **E2 以文档为准**：按"传统为主 + 中间层可选"重做信仰层（工作量大，需重定义 44+22 条内容与全部落点）。
  - **E3 折中**：保留 Religion→Succession 两级作为"信仰"，新增 Tradition 作为**并行**的文化运行单位（不做挂靠树），先让传统有运行时对象与分裂，再谈中间层。
- **暂定**：本总纲按 E3 排期（M9），并把选择权交回用户；在裁决前 M9 不开工。
- 影响：`FAITH_*` 相关 6 份文档的处置、M9 的工作量与 44 条宗教内容的去留都挂在这条决策上。

## D9 制度性缺口优先于新增内容（排序原则）

- 事实：内容层最厚的是革新（186 条）与群系（92 条），最薄的是种族（0）/族群（1）/语言（1）；而 43 条配方、120 条 `unlockRefs`、29 条建筑、49 条法律中的多数下游消费者不存在。
- 决策：在 M3–M6 交付前**冻结内容新增**（只修不增），把预算投在"让已有内容产生效果"上。
- 理由：现在每加一条内容，就多一条"无消费者的数据"；先修管道比灌内容更省。

## D10 反辉格史观：否决两项"缺口"（因设计宪章新增）

- 背景：`reports/innovation-audit.md` §12.2 把"Era 不构成解锁门槛"列为缺口 #10；另有"阶层应随时代推进自动升级"的隐含期待。
- 事实：`InnovationEra` 仅用于 AI 排序偏好（`AIController.cs:141-156`）；阶层由**具体革新 id** 门槛决定（`SocialClassAvailability`），与时代无关。
- 决策：**两项均不采纳**。Era 不作硬门槛、阶层不随时代自动升级——这是设计（反辉格史观 + 渐进分层），不是缺陷。
- 影响：M6 不做"时代门槛"；M9 不做"时代推进阶层"。若将来有人加，必须先推翻本条。

## D11 设计宪章作为横向验收门禁（因设计宪章新增）

- 决策：每个里程碑除工程门禁（编译/测试/五道静态门禁）外，必须至少满足其所承载设计原则的**一条可断言信号**（`DESIGN_CHARTER.md` §2）。
- 理由：本轮已证明"706 个测试全绿"与"世界会演化"无关；只有行为断言能防止"半跑"再次被当成完成。
- 影响：新增 M0.5「历史行为验收台」；四条原则的信号必须先能跑起来（哪怕断言当前失败）。

## D12 建国路径的接线位置（**2026-11-09 已撤回，被 D16 取代**）

- 原写法：接线放 M7，且"玩家在候选路径内选"（E3 折中）。
- **撤回理由（用户口径）**：国家形成是**世界生成之后自然涌现**的结果，不是菜单；`StateFormationPath` 是**事后分类**，用于描述"这个国家怎么来的"，不是给玩家/UI 的选择。
- 保留部分：接线仍必要（`FormRealmFromActor` 零调用者 ⇒ 世界永远不会出现新的国家），但触发源必须是**条件**（人口/剩余/组织能力/地理），不是玩家选择。
- 依据：`DESIGN_CHARTER.md` §0.5 规则 4。

## D16 反抽象数值驱动（宪章原则 5，最高优先级）

- 用户定义："尽可能不通过抽象的数值加成什么东西的去推动历史。"
- 决策：**数值只能作度量，不能作原因**。任何驱动力必须能回答三问：①有物质载体？②经过既有环节传导？③删掉会断链？三问不过的加成一律不得入库。
- 影响：`SYSTEM_MASTER_PLAN.md` §4.3 列出 7 处已定位的典型（建国加成 / 稳定度 Lerp / 法律乘子 / 种族常数 / 聚落财富 / 承载力 / unlockRefs），每个里程碑交付前按同一判据复审。
- 与既有决策的关系：`TITLE_INHERITANCE_PLAN.md` §10 早已写过"机制优先，分数只作度量"——本条把它提升为**全项目门禁**，不再只是头衔域的口径。

## D17 视角不是模式（撤回"三支柱"框架）

- 决策：**一个模拟、一套存档**；个人/家族/政权/世界是**观察与干预的缩放层级**，不是玩法模式，也没有"进入上帝模式"这种状态切换。
- 撤回：此前把「大战略/上帝模拟/RPG」当作三个设计支柱的写法（它会导致三套 UI 与三套状态）。
- RPG 的正确定位：**让人可指认**（属性、亲属、寿命、野心），从而让模拟更真实；人物是所有系统的共同主体。

## D18 可模组化是硬约束，不是加分项

- 决策：**规则活在数据里**（现有底座：21 域 JSON、`ContentSource` 带 priority 分层、`Mods/` 同构覆盖）；新增文明/物产/革新/法律/建筑/聚落类型**不得要求改 `.cs`**。
- 影响：`ApplyPathBonuses` 的 5-case、聚落类型/等级/升级门槛/破坏损失表（B 域 §3）都要外提为数据；`StreamingAssets/Base` 缺 Settlement/Province/Population/Building 四个目录，属于结构性缺口。

## D19 性能是设计约束（已有底座，立为门禁）

- 事实：`SimulationBudgetGovernor`（4–12ms 自适应，`MinBudgetMs=4/MaxBudgetMs=12/TargetFrameMs=16.7`）、`TickBudgeted` 超预算延后、`SimulationDirtySet` 脏键、`PopulationTick` 的 `Parallel.For` —— 说明性能优化是既定工程方向。
- 决策：任何新系统必须声明成本阶（全量 O(N)/增量 O(dirty)/事件 O(1)）与降级策略；禁止每帧全表重算。
- 现存违反：影响力叠加每帧全图 Dijkstra 且**查询路径写模拟状态**（`MapRenderer.Texture.cs:309-318` → `influenceTier`）；每季全表重写气候/群系。

## D20 系统耦合、架构解耦（用户口径，立为门禁）

- 原话："系统要耦合，但是代码和文件架构上要解耦，方便维护和优化，不至于我改一个地方，其他的地方都要大幅地改。"
- 决策：**因果链必须耦合，代码依赖必须解耦**。五条可测信号：①单一真源 ②内容改动不改代码 ③新增系统不修改既有系统（除组合根与调度注册）④替换实现只改接口引用 ⑤不跨层直接写可变字段。
- 现状判读：**程序集层做完了，内容层与状态层没做完**（用户怀疑正确）。证据与处置见总纲 §4.4（7 类耦合点：13 个 GameWorld partial、5 张硬编码内容表、5 组双真源、跨层写字段、查询写状态、场景与代码双源、静态类穿透）。
- 影响：每个里程碑交付前过五条；不合并不算完成。

## D21 生物学基底分两层：DNA（已在跑）+ 种族（群体基线，空）

- **更正**：D21 初版写"种族为空 ⇒ 所有人差异恒等于零"，**错误**。个体差异由 DNA 产生且真在跑：6 基因座（`DnaSystem.cs:24-31`）、`Inherit` 取父母等位基因 + 重组/突变（0.0075/0.015，`:226-258`）、`ComputeExpression` 表达为寿命/智/武/抗性偏移（`:263-278`）、`GenomeInstance` 入档、`DnaDefs.json` 8 条（4 才能 + 4 缺陷）。
- **种族层的实际作用**：提供**群体基线**——每个基因座的显性频率（`RaceData.GetLocusAFrequency`，缺省 0.5）、基因组/物种 id（`genome_human`/`species_human`）、智/武/抗基准（50）、寿命 75±15、冷/热/旱/湿/海拔耐受（`CalculateEnvironmentFitness`）、军事与物产偏好、变革性。
- **RaceDefs 为空的真实代价**：不是"没有个体差异"，而是**没有群体分化**——所有族群共用同一基因组定义与同一组等位频率，于是"不同物种/亚种在不同地理上分化"无法发生。
- **决策**：① 种族内容补到 ≥3 条（差异体现在等位频率/基因组/基准/耐受）；② **优先修"差异进入决策"**（`GetEffectiveStats` 只有 4 个下游，选将/立法/继承零调用）；③ 表型 `RefreshAppearance` 接线并入库；④ 修读档漂移（`DnaData` 置 null、`genome` 重随机）；⑤ `productionModifiers/consumptionModifiers` 去 `[NonSerialized]`。
- 主线仍是 M3（群体，影响人口与扩张）与 M8（个体，影响决策）。

## D22 撤回"能动性上限"提问

- 撤回理由：这不是设计参数。能否逆地理由**具体个体的构成**（种族生理 + 基因表达 + 特质 + 健康 + 处境）在具体地理下的实际结果决定。
- 影响：宪章原则 2 改为"只验因果、不设阈值"（换人换结果 / 换地理换结果 / 极端个体使分布尾部变厚）。

## D23 内容外提清单（解耦的具体施工）

| # | 现在在 C# 里的内容 | 证据 | 应外提为 |
|---|---|---|---|
| 1 | **29 个建筑定义** | `BuildingSystem.AddBuildingDef(...)` 30 处调用（id/名/类别/tier/成本/材料） | `Base/Building/Buildings.json` |
| 2 | **单位定义** | `GameWorld.Core.cs:775-786` `AddUnitDef(302, ...)` + `SetUnitRecruitCosts` | `Base/Units/Units.json` |
| 3 | **政体组件的革新门槛** | `PolityComponentInnovations`：`A1Hereditary={500}`、`B1Elected={980,500}`、`B2MilitaryCouncil={302}`、`C1Hereditary={501,952}`、`DFederal={501}`… | `Base/Laws/PolityComponents.json`（或并入 Laws.json） |
| 4 | **阶层/子类的革新门槛** | `SocialClassAvailability`：Freeholder{100,916,917,918}、Serf{952}、Merchant{701,700}、Scholar{600}+{503,504}… | `Base/Society/SocialClasses.json` |
| 5 | **革新→阶层涌现映射** | `ClassEmergenceEvents`：952→Serf、501→Tenant、701→Merchant、600→Scholar、505→DebtSlave… | 同上（与 4 同表） |
| 6 | **商品 57 条** | `GameWorld.Core.cs:601-659`（另有 `GoodsProperties.json` 29 条 → 两套来源） | `Base/Goods/Goods.json`（与现有属性表合并） |
| 7 | **灾害 12 条** | `DisasterSystem.cs:34-144` | `Base/Disaster/Disasters.json` |
| 8 | **建筑选址规则 30 条** | `BuildingAvailabilitySystem.cs:22-70` | 并入 1（建筑表） |
| 9 | **法律→革新映射** | `LawEffects.cs:390-435`（**string 键**，与 `InnovationTree` 的 int id 两套体系） | 法律条目内的 `requiredInnovationIds`（int），删 string 键 |
| 10 | **绰号 81 条** | `EpithetCatalog`（内建） | `Base/Epithet/Epithets.json` |
| 11 | **经济原型 11 条** | 代码内建 | `Base/Economy/Archetypes.json` |
| 12 | **资源-群系排除** | `GameWorld.Core.cs:733-742` | 资源定义内的 `excludeBiomes` |
| 13 | **加载体注入革新/能力缺省** | `ContentDefinitionIndex.cs:440-509`（1/1025 条革新由代码注入） | 移入内容 |

**模组可行性实测**（I 域）：新革新=纯 JSON 零代码；新种族=数据可入但须改 4 处 `raceId=0`；新建筑=必改 `BuildingSystem.cs` + `BuildingAvailabilitySystem.cs` + 枚举；新法律=**无 Mods 通路**（包白名单不含 Laws）且效果硬编码。**"加内容不改代码"目前只对革新成立。**

> 另有两个通路硬伤：① `Mods/Dna/DnaDefs.json` 被判定为包目录、实际找 `Mods/Dna/Dna/DnaDefs.json` ⇒ **现有 mod 样例永不加载**；② 包校验器 `ContentPackageValidator` 零运行时调用，14 个包域无 schema/引用校验 ⇒ 内容写错只会在运行时静默缺失。




## D13 玩家身份：`PlayerRealmId` 必须真实存在（因类型定位新增）

- 事实：`GameWorld.PlayerRealmId` 默认 −1，**全仓无赋值点**；活存档 `SaveData` 无该字段（只有 Obsolete 的 `GameSaveSystem` 存/读）；`GovernmentPanel` 用 `targetRealmId=0` 兜底到 `realms[0]`（`GovernmentPanel.cs:246-256`）；`FaithContracts.cs:103` 用 `PlayerRealmId>=0` 判国教，故该分支恒假。
- 决策：① 开局确定玩家政权（新档选项：指定/随机/上帝模式=−1）并写入 `PlayerRealmId`；② 进 M1 的存档字段；③ **移除** UI 侧"回退第一个政权"的兜底——它把"我是谁"变成了"列表第一项"；④ 上帝模式显式 `PlayerRealmId=-1`，所有"我的政权"栏位置灰。
- 理由：没有玩家身份，"大战略"只是"看着一个国家"；而且 `PlayerRealmId` 已被其它系统（国教判定、地图着色）依赖，恒 −1 会让它们静默走错分支。

## D14 世界尺度干预的边界：只改条件，不设代价层（已按原则 10 修订）

- 白名单 = 地形/高程、河道与海岸、气候带（低频）、灾害触发、人群播种与迁徙、事件注入。**禁止**：直接给金库、直接改稳定/好感、直接改战争胜负、指定某国存亡。
- **撤回**"必须带冷却或世界反噬"：反噬是凭空发明的机制，模拟里没有对应物质环节（违反原则 5），且会把干预做成"游戏技能"。
- 保留两条边界（不是代价）：① 只改条件；② 改动必须匹配时间尺度（地形/气候属长时段，天然稀少；灾害属短时段）；③ 每次干预记入编年史，改动可追溯。
- 理由：后果由已有因果链承担（抬山→降水→肥力→产出→人口→税基），改坏了世界，历史自己会惩罚。

## D34 四概念与挂载条件：信仰是定义主体，宗教/宗派/传承**按需挂**

- 用户口径（2026-11-11）："我们有信仰、宗教、宗派、传承 4 个概念。甚至**信仰本身也不一定是必挂宗教**——那些泛灵信仰总不能算是宗教吧，有些东西它没有一个统一性的组织。但是我们基本上**所有的定义都是在信仰这个层面**的，然后它其中还包括**教理部分、治理部分**等等层级。具体挂什么、要不要挂，**都取决于这个部分**。"
- 更正：此前 D29/G1 写的"faith 必挂 religion"**作废**。正确模型：
  - **`faith` 是定义主体**：教理（doctrine）、治理（polity）、礼仪（liturgy）、戒律（precept）、处分（penalty）这些**作为 faith 内部的组成部分**存在，不是挂在 religion 上；
  - **`religion` 是可选的上一层**：只有当这个信仰**形成了统一组织/制度**时才挂（基督教、佛教、道教有；泛灵、家神、地方崇拜没有）；
  - **`denomination` 是 religion 之下的宗派**：有 religion 才谈宗派；没有组织的 faith 不存在宗派；
  - **`lineage`（传承）是 faith→faith 的谱系**：独立于组织层，泛灵信仰之间也可以有传承关系；
  - **`family` 可选且最松**：只在需要"更大的宗教文明圈"时用。
- 与 CK3 的对应与超越：CK3 里 `faith` 是玩法单位、`religion` 必挂、`family` 再上一层；**它假设"信仰=有组织的宗教"**，所以泛灵只能被塞进某个 religion 里当一个 faith。我们的做法是把**组织性做成可选项**：`faith` 可以没有任何上层，照样拥有完整教理与治理（小型共同体/家户/部落层）。
- 挂载规则（替代"必挂"）：
  | 条件 | 挂什么 |
  |---|---|
  | faith 有统一组织、有跨地域的教阶/经典/礼仪体系 | 挂 `religion`（+ 可选的 `denomination`） |
  | faith 只在地方/家户/族群内部流传，无跨地域组织 | **不挂 religion**，`denomination` 也不存在；教理与治理仍在 faith 内部 |
  | 任何 faith | 可以有 `lineageFaithId`（它从哪个信仰传下来） |
- 影响：G1 的验收从"faith 必挂 religion"改为**"挂载合法"**——声明了父就必须能解析到（否则报错），未声明是合法状态（无组织信仰）；G3 的"必挂宗派"改为**"有 religion 的 faith 必须能落到一个宗派（含默认宗派），无 religion 的 faith 合法地没有宗派"**。


## D33 玩家侧入口走"授权制"（A 案）：不做微操面板，M10 口径随之改

- 冲突（总纲 V2 §5 第 3 条）：`CHARACTER_COUPLING`「保持无入口、税率不可调」 vs 旧 M10「11 项全部有入口」——方向性互相否决。用户裁决：**A 案**。
- 决策：**玩家不直接微操建造/税率/征税**，而是通过**授权**（官职/头衔/特许权）把事交给人物，再由人物按自己的 AI 执行；玩家看到的是"谁在办、办成没办成"，需要细节时**问人**（信息层）。
- 依据：项目定位是"多视角历史模拟"，不是欧陆/维多利亚式的国家意志；`CHARACTER_COUPLING` 已给出机制链（授权→代理→执行→结果→追责）与三处缺环：
  1. `RealmAuthorityResolver.cs:20-46` 只解析 HeadOfState/Regent，`AuthoritySource` 的 Office/CharterHolder/Council 未接；
  2. `ActionIds` 4 条常量只有 `DeclareWar` 被门禁使用，`AppointTitle` 零使用者 ⇒ 授予/撤职入口不存在；
  3. 人物缺 `Think/Act` 接口（只有 `IMapActorAI`），委托无法按人推进。
- 连带修改：**M10 由"11 项全部有入口"改为"授权 + 查询"**（每个可授权事项：谁能授权、谁执行、玩家看到什么、失败如何反馈）；税率保持不可直接编辑，由税吏代征 + L1 问数。
- 不做：新建 6 个操作面板。已有 22/29 个调度项无入口是**资产**（留给 NPC 执行），不是债务。


## D32 PlanType 从 10 缩到 3–4，并改成「结构语义 + 内容定义」

- 用户质疑：有些 PlanType 不该存在，别的系统就能干；要判断哪个合理。
- 判据（三问）：① 是不是**过程**（有起止/可中断/要时间）？② 有没有**自己的状态机**？③ 去掉它，别的系统能否完整承担？
- 判决：
  - **保留**：`Research`、`Construction`、`Intrigue`（须补"保密/暴露"维度）。
  - **合并**：`Engineering` → `Construction` + `scale`（同形，只是规模与中断规则不同）。
  - **暂留**：`Exploration`（依赖地图知识 S19，现在做等于空转）。
  - **删除枚举**：`Trade`（商队 + 路线在跑）、`Diplomacy`（`DiplomacyManager` 已管关系/条约/停战）、`Migration`（人口/行为体系统）、`Military`（`Warfare` 有 War/army/combat）——**这四个都不是"项目"**。
  - **仅模组**：`Custom`（本体禁用，避免把"没想清楚"塞进枚举）。
- 更彻底（推荐）：`PlanType` 改为 `Plan.kind ∈ { Process, Works, Expedition, Scheme }` + `planDefId : string`（内容决定具体活动）。**新增活动 = 加 JSON，不改枚举**（符合 D18 可模组化硬约束）。
- 影响：`PlanType` 的删除要连同创建/查询点一起清；`Engineering` 的内容迁成 `Construction` 的 `scale=large` 条目。


## D31 区块/城墙命名接**人物系统**，不接文化命名表

- 用户校正："为什么要接命名表啊，不应该是接人物系统吗"。
- 判断：对。文化命名表（`CultureRegistry.GetRandomName` + `CityNames.csv` 等）产出的是**人名/地名**的词；而"狄奥多西城墙"的语义是**"某人的墙"**——归属来自人物，不来自词表。接命名表只会得到一个"随机好听的名字"，与建造者无关，反而更假。
- 决策：`SettlementBlock.displayName` 由**建造者或当时统治者**的**本名（个人名）**生成（`SettlementBlockNaming.For(kind, ordinal, builderName)`）：
  - `builderCharacterId` 是锚点（存 id 不存名字，人物改名/身故后可重建）；
  - `builderName` = 该人物的**本名**（不是尊号、不是王朝名、不是头衔）；
  - 无建造者（自然形成/旧档）时退化为"第 N 道城墙/第 N 区"。
- **现状（重要）**：`AddBlock(...)` 的调用点目前**全在 World 内部与测试里**，Simulation 侧**没有任何生产调用**——也就是说"城墙/区块由谁建造"这条链**尚未存在**。命名规则已就绪，等建造系统落地时在调用点传 `builderName = 本名` 即可。
- 优先级（未实现，登记）：建造者本人 → 在建时的统治者 → 庇护者/机构（行会、教会）→ 序号兜底。
- 边界：`World` 层不能引用 `Simulation`，所以**解析人物名发生在调用点（Simulation）**，World 只接收字符串与 id。


## D30 存档架构对齐 CK3：扁平表 + 字符串定义键 + 加载期迁移（取代"DTO 镜像 + 枚举编码"）

- 用户口径："我们的架构是学的 CK3"。
- 对照 CK3（Jomini）四条：① 定义与实例分离（存档只存实例）；② 定义引用用**字符串 key**，不用数组下标；③ **扁平实体表 + 父指针**（层级靠 `parent=`，不是嵌套子数组）；④ 加载期迁移 + 版本号，不维护旧形状。
- 我们现状与后果：用 `int` id（宗教 100 起、群系 0 起，已踩坑）；用嵌套 `children[]`（撞 Unity 序列化深度 10，深层静默截断）；维护 `[FormerlySerializedAs]` + 双套 DTO（`SaveData` vs `GameSaveData`）。
- 决策：`Save.Contracts` **不是运行时对象的影子，而是存档文件自身的 schema**——扁平表、字符串键、父指针、显式字段清单。
- 影响：① §7 的 B 方案（枚举编码 int）**撤销**；② 区块树改扁平（`parentBlockId`），一次解决深度 10 与递归映射；③ 定义引用逐步串化（`biomeId`/`buildingDefId`/`unitTypeId` → string，老 int 只读兼容）；④ 枚举可继续留在 Simulation——DTO 用原语承载即可，`Save.Contracts` 不需要枚举。


## D29 信仰大系统：faith 为可玩单位、religion 必挂、denomination 必挂、tradition≈rite

- 用户术语体系已落 `docs/plan-v3/BELIEF_SYSTEM_MODEL.md`（三总概念 / 教理 8 词 / 制度四支 / 学派类型学 / 四问判据）。
- **关键裁决（用户口径，2026-11-09）**：
  1. **最小可玩单位是 `faith`**——玩家创立/改宗/分裂的对象是信仰共同体，不是"宗教"这个抽象体。
  2. **`religion` 必挂、不可空**（借族群"挂靠"逻辑，但宗教层**不能**可有可无）。
  3. **`denomination`（宗派）可选**——它有"管理/追认"意味，可以不存在。
  4. **`tradition` 用来替代 CK3 的 `rite`（礼仪）**，是**礼仪—传承基底**，不是"文化传统"，也不是层级容器。
- **两种装配模式（`religion.assemblyMode`，本轮最重要的新字段）**：
  - `liturgical`（自下而上，基督教型）：**地方礼仪传统先在**（罗马/高卢/莫扎拉比）→ 宗派在其上**追认/管理**；分裂多表现为"礼不改、治理分"。
  - `lineage`（自上而下，佛教型）：**传承链先定**（法脉/统绪）→ 宗派/学派从中分出；分裂多表现为"治理不改、法脉分"。
  - 若不建模这个方向，基督教与佛教会长成一模一样。
- **代码对位的重大发现**：代码里的 `Religions.json` 44 条 + 每 id 一个 `FaithSystem`，**正是 `faith` 层**（`religionId` 被直接当 `faithId` 用，`GameWorld.Core.cs:966`）。所以不是重造，而是**在 faith 上下各补一层**：上补 `religion`/`family`，下补 `denomination`/`tradition`。
- **命名冲突**：代码的 `TraditionDef` 是**文化传统**（Production/Political 类目、factors、solidification），正是用户排除的 cultural tradition；本模型的 `tradition`（≈rite）与之不同 ⇒ 代码侧改名 `CulturalTradition`，否则两套"传统"互撞。
- 修正用户原稿两处：`penalty` 拆 `penaltyKind`+`procedure`；`language` 归 `liturgy` 且只管礼仪/经典语言。
- **个人化信仰层（用户提醒，2026-11-09）**：制度层描述"群体怎么组织"，个人层描述"这个人到底信什么"。代码已有四件：`faithId`（名义归属，但创建时硬编码 0）、`piety`（**性格轴**，不是对某 faith 的虔诚）、`spiritualFulfillment`（0–100，`UpdatePiety` 按"特质 vs 该 faith 的 virtues/sins"每 tick 调）、`isSecretBeliever`（秘密信徒）。**缺四件**：私人主保 `patron`（接圣徒/圣地/神祇）、私誓 `vows[]`（带期限与违约后果）、改宗史 `conversionHistory[]`、**个体→群体聚合**（`fervor` 现在是独立浮点，不从成员 fulfillment 聚合）。
- 个人层规则：归属≠委身；只由行为更新（捐产/朝圣/守戒/庇护教士；禁止"每年 +1 虔诚"）；个体 fulfillment 分布聚合为群体热忱；高 fulfillment → 封圣/教职/动员，低 + 秘密 → 异端嫌疑/司法/继承争议；无国教时个人仍有 patron/vows/私祭。
- **CK3 1.20 实测（本机 game 目录）**：CK3 已多出 **`rite` 层**，层级是 `religion_family → religion → faith → rite`；`rite` 持有核心 **tenets** + 礼仪专属 doctrines，且有 `founder` 与 `head_of_rite`；doctrine 冲突时 **rite > faith > religion**；官方注释明确"**所有信仰机制挂在 tenet 上，不硬编码在 religion 上**"。
  ⚠ **更正（同一轮内自查）**：我此前说"个人化信条 = 创立/改革信仰时用虔诚买信条"，**错**。CK3 里这是**两套独立系统**：
  ① **`personal_tenet`（个人信条）**：角色级、可选、可传播。证据 `any_personal_tenet`（`ce3_achievements.txt:155`）、`has_personal_tenet_flag = tenet_vows_of_poverty_humble_host`（`feast.txt:916`/`coronation.txt:622`/`debate.txt:512`）、`spread_personal_tenet_intent`（`feast.txt:753/758`）。
  ② **`spiritual_fulfillment`（灵性满足）**：角色级 0–100 状态量，`MAX_SPIRITUAL_FULFILLMENT=100`；来源是**仪式性行为**（朝圣 `pilgrimage.txt:2588`、徒步 `hike.txt:110`、托钵传教、传播信条、神庙 `00_temple_buildings.txt:832`），按宗教家族分型（`has_spiritual_fulfillment_type=christian_fulfillment`），出口是**圣战 CB 档位**（`pam_wars.txt:84-119`）与朝圣成本（`pilgrimage.txt:1086`）。
  ③ 而 **faith 创建/改革**（选 tenets 付 `piety_cost`）是**第三套**，与上面两套分开。
- **我们自己的代码对位**：`spiritualFulfillment` 存在（默认 50、clamp 0–100、入档），**唯一写入** `UpdatePiety`（特质 vs 该 faith 的 virtues/sins，`CharacterManager.cs:102-118`），**唯一后果**是封圣门槛 ≥80（`CanonizationSystem.cs:42`）；**没有来源（无朝圣/仪式/传教加成）**、**没有第二出口（如圣战合法性）**；**`personal_tenet` 完全不存在**。
- 结构修正（仍然成立）：① 礼仪层挂在 **faith 之下**，不是 religion 与 denomination 之间；② `denomination` **是层级、且必挂**（11:50 更正，见下条）。
- **圣俗政治（17:10 用户口径，补第四轴）**：这里指**神圣政治 vs 世俗政治**的关系，不是 Durkheim 的 sacred/profane 分类。**"政教合一"不是布尔，是六种形态**：`sacral-kingship`（神圣王权：法老/天子/天皇）≠ `caesaropapist`（王权统教：拜占庭和谐论/沙皇/亨利八世）≠ `priestly`（祭司统治：达赖政权/伊朗/教皇国）≠ `dualist`（两权分立：Gelasius 双剑/叙任权之争）≠ `autonomous`（教权自主：逊尼乌莱马/美国式分离）≠ `embedded`（无分离：神道村落）。另需 `clergyPosition ∈ {subordinate, exempt, superior}`——同一宗教内不同集团可持不同立场（"沙门不敬王者"就是 dualist 诉求在 sacral-kingship 环境里的出现）。后果可断言：sacral-kingship 下叛乱即渎神；caesaropapist 下教产成财政工具、可催生"皇帝支持的异端"；dualist 下绝罚可削弱合法性并触发解誓/叛乱；priestly 下教法即国法。代码现状：`RealmData.stateReligionId` 无写入，五条后果一条都不成立。
- **学术辨析（16:50，见 `research/RELIGION_SCHOLARSHIP.md`）**：用前沿文献体检 S18。骨架站得住（faith 作传播/分裂单位、`lineage` 作传承〔Hervieu-Léger"记忆之链"〕、denomination 承载主流/分歧、`Patronage` 统一神祇与圣人〔P. Brown〕），但三处是**基督教语法投影**，必须补轴：① `differentiation ∈ {differentiated, embedded}`（中国式弥散宗教，C.K. Yang）——embedded 型 creed/canon 由家族/国家借用；② `exclusivity ∈ {inclusive, henotheistic, exclusive}`（Assmann 摩西区分）——把"一神教才妖魔化"从硬编码改成推导；③ `ritualFrequency`/`ritualArousal` 两浮点（Whitehouse 传播模式，跨文化实证支持连续量而非二元）——替代"多神教没有教统"的刻板。纪律：分类是 etic 标签、不驱动机制；宗教市场论与"圣人取代神"只当假设。
- **神祇与主保（15:30 用户 §4.2.2/§4.2.3；16:00 已按"先思考对不对"修正）**：① 神祇有**起源阶梯**（部落多神并存 → 部族/城邦守护神 → 万神殿；"一族一神"已否）；② 接触/征服/融合**六种结局**（消亡 / 降为下位 / 等同 interpretatio / 吸纳为主保 / 妖魔化 / 剥离神性），判定输入是制度条件，**禁止概率掷骰**；③ **主保（Patronage）是同一结构**：多神教主保神祇、一神教主保圣人、原始宗教祖先精灵；**共用的是主保授予，封圣只是一神教的程序**（术语不许越界）；④ 封圣是**四步流程**（地方崇拜→灵验→社群请愿→权威批准），代码只做了第四步阈值（`spiritualFulfillment ≥ 80`）；⑤ 神格状态与崇拜状态必须分开；⑥ 复杂度分级用同一套字段（`pantheonId=null` = 原始，有万神殿 = 多神，`denomination/magisterium` 复杂化 = 一神）。
- **11:50 更正：`denomination`（宗派）必挂，不是可选**（用户口径：不然"主流/分歧度"挂不上）。层级改为 `family → religion（必挂）→ denomination（必挂）→ faith（传播/分裂单位）`；未分裂的宗教也必须有**默认宗派**（`isDefault=true`）。宗派承载三件事：① `status ∈ {mainstream, tolerated, heterodox, condemned, extinct}`；② `divergence`（与 religion.orthodoxy 的偏离度，**可算不可填**：tenet 重叠 + 礼仪差 + 戒律差，权重入内容表）；③ `mainstreamDenominationId`（跟随人数占比/统治者支持/宗教领袖承认变化）。分裂分两种、不许混：**信分裂**产子 faith（`parentFaithId`），**宗派分裂**产子宗派（`parentDenominationId`）。
- **组织层不新建系统（05:05 用户定稿）**：所有**非独立组织**（修会/隐修会/军事修会/教团、商会/行会、兄弟会/善会、学会、秘密会社、特许公司）共用一套模型 = **实体 + 首领头衔 + 章程 + 财产 + 特许权 + dependency**；差别只在 `orgType` 与 `purposes`，不在机制。宗教四型用 `traits{cloistered,armed,apostolic,mendicant}` + 自洽规则区分；托钵不是第五类而是财产制度。
- **特许权（Privilege）= 统一对象，豁免只是其中一种**：`{grantor, grantee, kind, scope, sinceDay, expiresDay, revocable}`，`kind ∈ {exemption, monopoly, taxation, jurisdiction, arms, mint, selfElection, property, passage, toll}`。现有碎片收编：`RealmLawState.AddCharterHolder`、`ProposalAuthority.CharterHolders`、`LegalNormKind.Charter`、`LocalSuccession.CityCharter`、`TreatyClauseType.TradePrivileges`、`ContractType.Charter`、`BondType.SwornBrotherhood`。特许权是"法律的例外"，与 `LAW_LIFECYCLE.md` 共用位阶/冲突/撤销规则；被侵犯可成诉讼或战争理由。**非独立 = 不持政权头衔**（军事修会成领地政权是唯一例外，走 S16）。

- **04:50 术语定稿（覆盖所有旧写法）**：信仰侧的"谁传自谁"叫 **`lineage` 传承**，**不叫传统**。三词分家：
  - **传承 `lineage`**（信仰系统）：`faith.lineageFaithId` → 父信仰；传播分裂产子 faith（照抄文化的 `parentCultureId/childCultureIds`）。
  - **遗产 `heritage`**（文化/族群系统）：代码已有 `HeritageDef`，与信仰无关。
  - **文化传统**（文化系统）：代码 `TraditionDef`（Production/Political、`solidification`），也不是信仰的。
  - 三者代码里必须分开命名，否则互撞（现状：信仰侧**没有** `lineageFaithId`，需新增）。




- **核证修正（8/8 有来源，`research/RELIGION_FACTS_VERIFIED.md`）**：
：① 431 与 451 是**两条不同裂线**，东方教会未参加两次公议（神学定型于 484/612）⇒ `schisms[]` 需支持多条独立分裂且各带成因；② 逊尼无按立但有 **ijāza 授教许可** ⇒ `ordination.kind ∈ {sacramental, license, appointment, none}`；③ 马尔贾**非正式承认、可多位并存** ⇒ `magisterium{scope, plural}`；④ 锡克经书为"永恒古鲁" ⇒ `canon.living`；⑤ 神道由官办转民间法人 ⇒ `stateRelation` 必须是**时间序列**。


- **跨宗教结构研究（第一版）**：`docs/plan-v3/research/RELIGION_STRUCTURES.md` 用 10 种结构原型（礼仪追认 / 法脉传承 / 学者法理 / 会众契约 / 教阶修会 / 公职祭祀 / 秘密入教 / 军事教团 / 灵媒附体 / 种姓法型）× 案例，检验模型。结论：现有模型能覆盖 A–E 五型，对 F–J 需补 6 项：`authoritySource`、`succession`（含转世）、`property`、`stateRelation`、`initiation.grades[]`+`canon.esoteric`、`looseAggregate`（印度教/神道/民间信仰这类无单一信条的宗教）。
- 三条"不要硬套"：① 不假定宗教必有单一 creed；② 训导权不必依附职位（逊尼乌莱玛/犹太拉比）；③ 分裂归因按类型不同（基督教在治理、佛教在法脉、伊斯兰在继承—权威）。




## D28 关系层：物质关系是投影，契约关系是真源；先决定"读模型还是真源"

- 事实：`EntityRelation` 17 类型 + `RelationRegistry` + `RelationProjection` + `RelationEndpointValidator` + `RelationPersistencePolicy` 全在，但**生产零写入者**；`RelationProjection.Build` 唯一调用点是测试；存档捕获的是空表；没有任何系统按关系查询。
- 代码已暗示的规则（从 `isProjection` + `Capture()` 跳过投影反推）：**物质关系（领属/占领/委任/雇佣/附庸字段）= 投影，不入档可重算；契约关系（附庸契约/共治/租借/保护/朝贡/义务）= 真源，入档**。
- 决策：采纳这条切分；并**先决定关系层是"统一读模型"还是"真源"**——现存的 5 处关系存储（`suzerainId/vassalIds`、`_subordinations`、`DiplomaticRelation`、`TitleHolding`、`CharacterRelation`）意味着它已是第 6 套；若当读模型，各系统保留自己的字段，关系层只做投影 + 统一查询；若当真源，则要迁移那 5 处。
- 倾向：**读模型 + 统一查询**（改动小、可逆），并把"契约关系"作为唯一新增真源（附庸契约/共治/租借/保护/朝贡）。
- 缺口（迁入 `RELATION_LAYER.md` §5）：`obligations` 是裸字符串（无法被法律/条约引用）、无时间演化（legitimacy/stability 无 tick）、无冲突消解（同目标多 from 只靠后写覆盖）、`expiresDay` 无人读、无人际关系对位（联姻→同盟）、无 UI。
- 影响：M1 存档闭合时一并落实"投影不入档 / 契约入档"；M5（法律义务）、M7（附庸/条约）、M8（头衔/联姻）都从这里读。


## D26 法律与习惯的生命周期（用户口径 + 代码骨架）

- 用户口径：已知 → 正在推行 → 正在生效 → 已固化 → 宪法化（后者需相关革新）；无成文法时默认只有**习惯**，习惯在文化传统里；**习惯会变成法，法也会变成习惯**。
- 代码对位：`LegalNormStage` **八态已齐**（`Unknown/Known/Proposed/Pending/Active/Consolidated/Fundamentalized/Repealed`，注释写明"根本化需革新+程序+合法性"）；习惯槽位也留了——`LegalNormKind.Custom`、`LawGroupDef.fallbackLawId`、`LawDef.isFallback`、`TraditionDef.solidification/RoleGateFulfilled/upgradesFrom`。**但只有 3 个转移在跑**，其余全零读写。
- 决策（写进 `docs/plan-v3/LAW_LIFECYCLE.md`）：
  1. **四阶段语义分离**：Pending=行政（推行期/传递）、Active=执行（效果结算）、Consolidated=社会（反抗成本上升）、Fundamentalized=主权（同组普通法不可覆盖）。判据：年限 + 合规度 + 未被挑战 / 革新 + 制宪主体 + 合法性阈值。
  2. **习惯→法**三件齐备：`solidification` 达阈值 + 提出者位阶（`TraditionElevation`）+ 立法能力；生成 `normKind=Statute` 且 `source=custom`/`upgradesFrom=traditionId`。
  3. **法→习惯**：连续 Active ≥N 年且合规度 ≥X → 写回传统（solidification↑ + 编年史）；**撤销后习惯仍在**（法死习惯活）。
  4. **宪法化三前置**：革新 + **制宪主体**（君主/议会/教团）+ 合法性阈值；强行宪法化应触发合法性反噬，而不是静默成功。
- 未决：N/X 的具体数值（等数值标定）；制宪主体如何建模（可挂头衔/官职）。

## D27 法律的失效与效力（补用户模型之外的缺口）

- 失效不止"废除"：`Active → Replaced`（同组新法替代）/ `Suspended`（悬置）/ **纸面法**（合规度低于阈值 → 效果按比例衰减，而非 100% 生效）。
- 效力四轴：**层级**（上位/下位、特别法优先、后法优于前法）、**地域**（`LegalScope`：地方习惯/特许状/边疆特权）、**人格**（接 `SocialIdentityFlags`：贵族/教士/外国人豁免或另适用）、**时间**（生效延迟已有 `enactmentDays`；可加试行期/日落条款）。
- 依据：`LawDef.normKind/scope/precedence/overrides` 字段全在但 **JSON 0 次使用、无读取点**；`LegalScope` 无消费者。
- 影响：M5 施工新增 ⑤–⑧；`fundamentalizedLawIds`/`normStage`/传统固化度必须在 M1 入档，否则读档后"法又活了/习惯又没了"。


## D25 社会四维 = 阶级 / 阶层 / 身份 / 职业；需求**四轴各出**

- 口径（用户）：社会拆成 **阶级、阶层、身份、职业** 四个维度；**需求按四轴各出一套**（不选单轴、不做交叉表）。
- 代码对位（Lead 实读）：四维即 `IdentityProfile` 的四个字段——阶级 `EconomicClass`(8) / 阶层 `SocialStratum`(5) / 身份 `SocialIdentityFlags`(7 位标志) / 职业 `OccupationFlags`(14 位标志)；legacy `SocialClass`(5)、`SocialSubclass`(12) 只是映射源（`LegacyIdentityMapping`）。
- 现状：数据层已建好（`PopulationIdentity.Resolve` 四轴默认时回退 legacy；`SocietyManager.cs:153` 按阶级/阶层聚合，但 `[NonSerialized]` 不入档）；**决策层仍只用 legacy 大类**——需求按 5 大类评 8 维、分化按大类，身份/职业零判定。

### 落地形状（按"四轴各出"推导，待确认两处细节）

```
NeedAxis { Class, Stratum, Identity, Occupation }
NeedReport { axis; key; dimensions[8]; worstDimension; score }
INeedQuery.GetReports(NeedAxis) -> IReadOnlyList<NeedReport>
```

1. **归桶**：每条人口块按四轴各归一次——阶级/阶层是单值，各计 1 次；身份/职业是位标志，**按位展开并把该块人口按位数均分**（`count / bitCount`），以保持人口守恒、避免重复计。
2. **评分**：8 个需求维的公式沿用，但**权重表按轴取**（`ClassNeedWeightTable` 需要扩成四套，或改成 `(axis,key) → weights`）。
3. **政权级 unrest**：四轴各得一个"不满度"（=1−score 或 worstDimension 的缺口），再按该轴覆盖人口加权合成；轴级权重进权重表，不写死在代码里。
4. **派系**：`FactionManager` 现在直接读 `needReport.dimensions[PoliticalAccess]`，改为读"该派系所代表轴"的报告。
5. **存档**：报告是每 tick 派生量，不入档（与现状一致）。

**已确认（2026-11-09 用户"可以"）**：
1. **位标志轴的人口按位均分**（`count / bitCount`），保持人口守恒；不把 flags 组合当 key。
2. **四轴在政权 unrest 里的权重可配**，进内容表（不写死代码）。

- 影响：`ClassNeedsSystem.EvaluateClass(SocialClass)` 与 `ClassNeedWeightTable` 的签名必然改；M4（财政-阶层-稳定）与 M9 的"阶层"部分不得按 5 大类写死；M4 施工新增"四轴需求报告 + 聚合"一项。
- 验收（可断言）：① 四轴各返回一组报告，键数 = 该轴出现的取值数；② 四轴人口合计与人口块总量相等（守恒断言）；③ 改动任一轴的权重记录 → 政权 unrest 变化可复现。


## D24 偶然性不是预算（原则 9）

- 撤回"黑天鹅概率/偶然性预算"的提法：偶然来自**三层机制内部的随机源**——自然层（气候年际波动、13 类灾害，按季节修正概率）、个体层（出生等位基因组合/突变/疾病/死亡年龄）、接触层（谁遇到谁，由地理与事件决定）。
- 禁止**结果层掷骰**（直接决定战争胜负/国家存亡）；随机只发生在**条件层**。
- 与"可复现"不冲突：随机源全部由 world seed 派生。现在的反例是 `PopulationTick` 用 `Environment.TickCount` 播种（H 域），须在 M1 改掉。
- 由此确定"偶然性"的验收方式：**同一 seed 换参数 → 结果可复现地改变**；不同 seed → 分布不同但机制相同（不是靠百分比配额）。


## D15 RPG 的承载：人物为体，不新增系统（措辞已按 D17 校正）

- 决策：不建"RPG 模块"。RPG 内容 = 让人物属性/关系/心境进入**已有判定**：立法（`ActionGate`）、选将（`FindBestGeneral` 等零调用者）、研究（`GetEffectiveStats` 已有 4 下游）、继承（`SuccessionSystem`）、法令传播（`DecreePropagationSystem`）。
- 人物是**观察与干预的单位**，不是玩法模式：玩家可以停在任一人物视角看世界；该人物死亡只是视角失效，不是"游戏结束"。
- 主战场是 **M8**；验收按"一个角色 40 年生命周期全程可玩 + 属性影响 ≥3 个真实决策"。
- 理由：项目已经有 616 行遗传引擎、990 行个体疾病、685 行谥号、108 条特质——再加一套"RPG 系统"只会制造第 N 个半跑模块；缺的是**消费者**，不是新代码。





## D35 V3 优先，但以现有代码为事实

- 仓库当前没有独立 V3 总纲文件；`docs/plan-v3/` 是本轮 V3 计划集，`SYSTEM_MASTER_PLAN.md` 正文仍是 V2 内容。两者冲突时不默认“V3 全对”。
- 裁决顺序：**现有代码事实 > V3 方向 > V2 证据**。V3 更新时按 V3 排期；V2 有更实的 `file:line` 审计时保留为证据；代码与两者都不符时登记欠账，不在计划里假装已实现。
- 影响：后续施工项必须同时标注“V3 意图 / 代码现状 / 迁移动作”，不能只引用计划文字。

## D36 建筑/设施的“容纳人物”落在 SettlementBlock，不落在 FacilityData

- 事实：`FacilityData` 只有 `facilityType/constructionState/material/repairProgress`（`World/Settlement/FacilityData.cs:6-21`），`FacilityType` 只有墙/门/壕/栅/土垒/其他（`FacilityType.cs:3-11`）；`SettlementBlock` 已有 `buildingDefId/kind/subtype/level`（`SettlementBlock.cs:19-60`）；人物只有 `currentSettlementAnchorId/currentTile`（`CharacterLocationService.cs:9-17`）。
- 裁决：**容纳人物的是建筑实例 `SettlementBlock`，不是墙体 `FacilityData`**。新增通用 `Containment`：`capacity`、`occupantCharacterIds[]`、`accessPolicy`、`containmentKind`；人物补 `currentBlockId`。领主地牢是建筑的 `kind/subtype` 或 `containmentKind`，不是新的 `FacilityType`。
- 不做什么：不把“地牢”硬编码成 `FacilityType.Dungeon`；不把 `FacilityData` 变成人物容器；不新建平行“监狱系统”。

## D37 接敌先做探测/接触层，战斗只在接触后结算

- 事实：`CombatManager.DailyTick` 只在 `value2.currentTileIndex == value.currentTileIndex` 时开战（`CombatManager.cs:209`）；`Army` 无视野/探测字段（`Army.cs:14-77`）；`UnitRole.Scout` 已有枚举但未参与地图探测。
- 裁决：先有 **Detection/Contact**，后有 Battle。探测以**斥候可达范围**为主（地形/天气/道路/兵力修正），不是全知视野；双方接触后才进入 `Encounter`，再由现有 `CombatManager.ResolveBattle` 结算。遭遇/会战不强制硬类型，用 `EncounterKind` 标签 + 变量：地形、天气、士气、组织、补给、训练、指挥官、工事、侦察/突袭。
- 影响：`Army`/`MapActor` 补探测字段；`CombatManager` 的“同地块即战”改为“接触后同地块或追击战”；AI 用接触结果选目标，不做全图敌情全知。

## D38 战争分三层：战略 WarState / 战役 Campaign-Front / 战斗 Battle；CB、目标、条约三分

- 事实：`WarState` 只有分数/胜负（`WarState.cs:3-53`），不存 `CasusBelli`/`WarGoal`；CB 与目标在 `DiplomaticRelation.casusBelliList/activeWarGoals`；`DeclareWarWithJustification` 存在于 `DiplomacyManager.cs:388`，但 `GameWorld` 的生产宣战只走 `DeclareWarCore`（`GameWorld.Simulation.cs:1044-1051`），后者创建 `WarState`；正式战争与“敌对行为/劫掠”是两条链。
- 裁决：**战略层** `WarState/WarPlan` 承载参战方、宣战理由、战争目标、时间；**战役层** `Campaign/Front` 承载方向、兵力、补给线、目标地块；**战斗层** `Battle/Encounter` 只负责一次接触的结算。`CasusBelli`=为什么可打，`WarGoal`=打什么，`Treaty`=怎么结束；三者不得混写。
- 规则：正式宣战（含无 CB 的突袭）也必须创建 `WarState`，并把 CB/目标挂上；`DeclareWarWithJustification` 不得再有“改了外交关系但没有 WarState”的死路径。不新增 `PlanType.Military`（D32）。
- **理由固定、目标可变但受约束**：`CasusBelli` 在宣战时锁定，战中不可更换；`WarGoal` 可中途调整，但必须仍在原 CB 授权范围内（`WarJustificationSystem.GetSupportedWarGoals`），两次调整至少间隔 `WarRules.warGoalRevisionCooldownDays`（默认 90 天），无 CB 的突袭不允许改目标。`WarState.lastWarGoalChangeDay/warGoalRevisionCount` 记录变更史，外交投影同步刷新。
- **目标选择受接壤与类型约束**：`ConquerTerritory/BorderAdjustment/ConvertReligion` 必须指向守方地块且与攻方接壤；`ConquerRegion` 的目标区域必须与攻方接壤；不同目标有不同完成条件（战争分阈值只是其中一项，领土目标最终看条约割让）。
- **威望是等级，不是货币**：新增 `PrestigeTier`（Unknown/Local/Regional/Realm/Eminent），各目标要求最低等级（边境/赔款/羞辱→Local，夺地/传教→Regional，夺区/附庸/独立→Realm，灭国→Eminent）。**不扣威望点数**；等级不足时，只有在政权稳定度与中央权威仍足够时才能强推，代价落在**稳定度、不安、廷臣对统治者的好感**上（`WarRules.warGoalForce*`）。
- **两层目标能动性**：统治者层决定“打什么”，受 CB、接壤、威望等级、政治代价约束，不是随手改；执行层由带兵将领根据性格、忠诚、补给与战况自行偏移（后续接入 `CommanderAutonomy`，不得用抽象加成替代）。任何改动都必须留下政治后果与编年史记录。
- 优化：战争相关查询按参战方/战区索引，禁止每军队×每战争×全军队笛卡尔积。

## D39 预加载/AI 预热作为性能工作流，不新建玩法系统

- 事实：全库无 `Preload/Warmup` 生产代码；`ContentRegistry.Initialize` 同步加载，`EnsureAIControllers` 按政权懒创建，`PrepareLoadedWorld` 读档补齐。
- 裁决：新增 `PreloadService`（或等价组合根步骤）预热内容表、AI 控制器/决策上下文、常用查询索引；重计算放后台/分帧，主线程只做 Apply；带预算与取消，不阻塞首帧。AI 预热“上下文”，不预生成不可复现的决策。
- 验收：首年可玩时间、首次 AI tick 延迟、长跑中重复构建缓存次数三项可观测。

## D40 人口单源：地块人口块为权威，聚落人口是派生快照

- 事实：`TileData.populationBlocks` 随 `TileData` 入档（`SaveData.tiles`），`PopulationTick` 每日增长/满意度/饥荒都作用在块上；`HabitationSite.population` 是独立标量，`SettlementData` 继承它，运行期还有 `SettlementDestructionSystem`、`StampedeSystem` 直接增减该标量，形成第二套人口。
- 裁决：**人口唯一权威是 `TileData.populationBlocks`**。`SettlementData.population` 改为派生快照（`derivedPopulation`），由 `SettlementPopulationSystem` 每日聚合：自身地块 + `anchor.controlledAnchorIds` 对应聚落的地块，按地块索引去重；不递归展开从属聚落的受控锚点，避免重复计数。
- 写入口：运行期伤亡/毁灭必须改人口块（按聚落聚合人口比例分摊），不得只改快照；快照只允许**持久化恢复**与**生成期初始值**写入。读点一次性切换（计划 A），不留第三套人口。
- 失效条件：聚落聚合范围内没有人口块时快照为 0，不回落旧标量；等级推进/毁灭/废弃判据统一读派生快照。

## D41 战争议事：宣战与改目标先协商，绕过协商有政治代价

- 机制：`CouncilConsultationSystem` 在宣战/改目标前召集议事成员——优先在职官员；早期没有官职时由同政权有身份人物充任。成员立场由**性格（胆量/贪婪/同情/荣誉）+ 对统治者的好感 + 目标激进程度 + 政权稳定度/不安**决定，输出支持/反对/弃权与批准度。
- 决策模式：`ConsultAndAbide`（默认，议事多数反对就作罢）、`ConsultAndOverride`（强推）、`SkipConsultation`（不协商）。协商通过不额外收费；强推或不协商的代价落在**议事成员对统治者的好感、稳定度、不安**，不是扣威望点数。
- 记录：`WarState` 保存 `councilConsulted/councilSkipped/councilOverridden/councilSupporters/councilOpponents/councilApproval` 与改目标时的支持/反对数；编年史留痕。
- 与 D40 一致：所有代价必须能解释为人的反应或政权的政治状态，不用抽象货币替代协商。

## D42 财政税率单源与税基口径（P1.2 第一批）

- 税率唯一真源是 `RealmData.taxSystem`；`EconomyManager` 的全局 `_taxSystem` 只作旧测试/缺省兜底，生产结算不得再统一读全局税率。
- 税基 = 土地生产力（fertility × development × 10）× 人口因子 × 秩序因子；人口取 `TileData.populationBlocks` 聚合，秩序用 `tile.order` 作控制度代理。人口/控制度不再是税基外的抽象乘数。
- 不做：暂不引入 `vault` 与 `treasury` 二选一、不另建收支表；四轴需求报告留到 P1.2 后续。

## D43 国库单源：realm.treasury 是唯一货币余额，vault 是货物储备

- 事实：`TreasuryLedger.TryPayNational` 曾在有货币物产时花 `currencySystem.vault` 并回写 `realm.treasury`，无货币物产时又花 `realm.treasury`，形成"货币两本账"；UI/AI 直接读 treasury，付款却可能扣 vault。
- 裁决：**货币唯一可支付余额是 `realm.treasury`**；`currencySystem.vault` 只存货物（粮草、货币物产作为储备），不是第二本可支付余额。`TryPayNational` 只扣 treasury 的 money 与 vault 的 food；付款失败回滚 treasury。`ReconcileNationalCurrency` 保留作旧档迁移，不再在每次付款时镜像。
- 影响：vault 富裕不能覆盖国库赤字；储备与预算分离，后续收支表分别展示"货币余额/货物储备"。

## D44 聚落表就地替换：子系统持有引用，生成期不得重新赋值

- 事实：`GameWorld.InitializeSubsystems` 在 `InitializeWorld` 阶段构造 `SettlementSimulationSystem` 并持有 `settlements` 字典引用；`GenerateSettlements` 之后用 `settlements = item2` 重新赋值，导致聚落模拟/生产/生命周期一直在操作旧空表（长跑看似正常，实际这些系统空转）。
- 裁决：生成期一律 **就地 Clear + 填充** 现有字典（`AdoptSettlements`），不得替换字段引用；同理适用于任何在子系统构造后才生成、且被子系统持有引用的容器。
- 附带修正：营地降级必须走 `TryDowngradeDueToDestruction`，`TryTransition` 对 Sedentary→TemporaryEncampment 是禁止转移。

## D45 收支报告只汇总真实国库流水

- 机制：`TreasuryLedger` 增加运行时流水（`Record/GetHistory/ClearHistory`，上限 2000 条，不入档）；`GameWorld.GetRealmBudgetReport(realmId, windowDays)` 只读聚合窗口内的收入/支出/净额与分项。
- 已接：税收入（`PoliticsSimulationSystem` 每日入账时记录）。支出随命令/军费/建筑维护逐步接入，不预造费率；报告不得写任何模拟状态。
- 不做：不新建第二套账本；`realm.treasury` 仍是唯一货币余额（D43）。

## D46 四轴人口报告先落地，需求评分后接

- 轴：阶级（`EconomicClass`）、阶层（`SocialStratum`）、身份（由 legacy `socialClass` 映射的 `SocialIdentityFlags`）、职业（`OccupationFlags`）。
- 归桶：阶级/阶层各计 1 次；身份/职业是位标志，按位展开并把该块人口按位数均分，保证四轴人口合计与人口块总量相等（守恒断言）。
- 已做：`SocialAxisReportSystem` + `GameWorld.GetSocialAxisReports` 只读报告。未做：四轴需求评分与政权 unrest 权重（D25 后半），不假装已接。

## D47 四轴需求评分与 unrest 权重（D25 后半）

- 评分沿用现有 `ClassNeedsSystem` 的阶级满足度作为底分（8 维需求已实现），再叠加位置修正：阶层的上下位置、身份（Royalty/Noble/Clergy/Citizen/Freedman/Foreigner/Outlaw）、职业（农/工/商/学/兵/官/祭/矿/水手/牧/医/师/力役）。修正解释为社会位置与职业风险，不是抽象货币。
- 身份/职业仍按位均分人口；每桶记录 `needsSatisfaction`，每轴记录按人口加权的 `axisSatisfaction`；四轴合计仍与人口块总量守恒。
- `SocialAxisWeights` 提供四轴权重（默认均分，可配）；`GetSocialAxisUnrest` = 100 − 加权满足度。改权重 → unrest 改变且同权重可复现，满足 D25 验收③。

## D48 法律生命周期写入方（P1.3 第一批）

- 阶段：`Active → Consolidated（连续 20 年）→ Fundamentalized（连续 50 年 + legitimacy ≥ 70 + 该组无 pending）`；换法时 `SetActiveLaw` 把旧法记入 `repealedLawIds`、年限清零、阶段回 Active。
- 写入方：`LawRealmHost.BeginEnactLaw` 标 Pending；`LawLifecycleSystem.YearlyTick` 由年度法律效果调度调用；`Fundamentalized` 同时写入 `fundamentalizedLawIds`（普通法律/法令不能覆盖）。
- 不做：暂不接革新门槛（LawDef 无 requiredInnovations 字段）；教理合并（WP3）与八接线点仍属 P1.3 剩余。

## D49 革新 unlockRefs 第一消费者：建筑门禁

- `InnovationTree` 新增 `IsUnlockReferenced/IsUnlockGranted/GrantInnovation`：只有**已完成的革新**（`_realmInnovations`）才算解锁；未被任何革新引用的内容不设门槛，避免把全部既有建筑一刀切锁死。
- 消费者：`BuildingSystem.StartBuildingInternal` 在扣款前校验 `InnovationUnlockKind.Building`；`GameWorld` 在构造 `InnovationTree` 后注入 `BuildingSystem.Innovations`。`SettlementProduction.TryCraft` 校验 `InnovationUnlockKind.Recipe`（输出物 id），未引用配方不设门槛。`LawContracts.BeginEnactLaw` 校验 `InnovationUnlockKind.Law`（法律 key），未引用法律不设门槛。`InnovationTree.AreItemInnovationsMet` 校验 `InnovationUnlockKind.Equipment`；Unit 消费者要等 P1.5 征兵路径落地后再接。
- 其余消费者（Recipe/Law/Unit/Equipment）按同一 API 后续接入，不另造映射表。

## D50 军队日结与军事占领（P1.5 第一批）

- `Army.DailyTick` 之前从未注册进调度（军队不行军、不耗补）；新增 `ArmySchedule`（`army.daily`，order 15，在战斗日结之前）统一推进补给/组织/训练/行军。
- 行军后 `GameWorld.TryOccupyByArmy`：进入交战方领土时调用 `PoliticalManager.OccupyTile` 标记 `occupyingRealmId`；**所有权仍由和约决定**，占领只是军事状态，避免与外交割让两套写入口。
- 未做：征兵路径（依赖人力/物资/财政）、海军写侧、军队标记绘制；这些仍是 P1.5 剩余。

## D51 选将接入军队创建（P1.6 第一批）

- `GameWorld.CreateArmy(ownerRealmId, -1, tile)` 现在调用 `CharacterManager.FindBestGeneral` 选统帅能力最高者，并把 `commandedArmyId` 写回人物；显式传指挥官时保持原行为。
- 不做：不新建"将军池/任命系统"；指挥官仍是人物 id，军队仍是唯一军事实体。

## D52 征兵路径：人口→军队，兵种受革新门禁

- `RecruitmentSystem.TryRecruit`：校验政权/兵种/数量；兵种 `requiredInnovations` 全部已掌握且 `InnovationUnlockKind.Unit` 未被未完成革新锁住；按 `count × manpowerCost` 从政权核心/所属地块人口块按每块最多 10% 分摊扣除；按 `UnitDef.recruitCost` 从本区市场扣物资；已有同地块同政权军队则补编，否则新建（自动选将）。
- 不做：不新增“人力储备”第二本账——人口块就是人力池；财政工资/装备折旧暂未接入，避免预造费率。
- 影响：P1.4 的 Unit 消费者由征兵入口正式消费，不再是空接。

## D53 海军日结写侧（P1.5）

- 新增 `NavySchedule`（`navy.daily`，order 16）：每舰队按 `0.05/舰/日` 消耗补给；断补后士气/组织下降，驻港训练缓慢恢复。
- 未做：海军寻路/海战结算/舰船建造消耗；这些仍属 P1.5 剩余，不硬接空逻辑。

## D54 婚姻入口与人口流程（P1.6 第一批）

- `GameWorld.TryArrangeMarriage` 成为婚姻唯一世界层入口：条件由 `CharacterManager.Marry` 判定；跨政权联姻改善外交关系；编年史留痕。
- `AutoProcreate` 的未婚配对改为“先收集候选 → 统一 Marry → Procreate”，避免枚举人口列表时改关系/家族集合；Marry 真正进入人口流程。
- 顺带修复 `CharacterData.ModifyRelation` 对 `relations/history` 为 null 的 NRE（旧档/手工构造关系时会出现）。

## D55 摄政评分消费治理能力与威望等级（P1.6）

- `SuccessionSystem.RegentScore` = 威望等级 × 1000 + `CalculateRuleAbility()` × 10 + 年龄；威望只按等级计权，不再用裸点数压过治理能力。
- 作用面：同家族、行政区、官职、同政权候选四条摄政推举路径统一使用该评分；测试改为跨等级比较（150 vs 10），避免两个同为 Unknown 时把随机能力当成威望差异。

## D56 教义 schema 第一批：optionId → DoctrineBindings（P1.3 WP3）

- 内容层已有 `Doctrines.json` 的 optionId（doctrine_/ethics/ritual/institution/material 前缀），但运行时只有 `Doctrine{name,category,strictness}`，没有可消费的绑定。
- 新增 `DoctrineBindings`：把选中 optionId 解析成伦理立场（同性/婚外情/巫术：罪行/忌讳/接纳）、离婚（禁止/许可/允许）、葬礼（土/火/天/木乃伊）、神职性别与婚配、和平主义、君权神授。`DoctrineBindingHost.Resolve(ReligionDef)` 是唯一解析入口。
- 未做：八接线点消费与参数级 schema（group→option→parameter）；本批只落“可被系统读取的绑定快照”，不假装已改行为。

## D57 教义绑定第一个消费者：同性婚姻是否允许（P1.3 WP3）

- `CharacterManager.Marry(a,b,allowSameSex=false)` 保留原默认；`GameWorld.TryArrangeMarriage` 从政权国教解析 `DoctrineBindings.homosexuality`，只有 `Accepted` 才放行同性婚姻。
- 这是 WP3 的第一条真实消费链：内容选项 → FaithHost 绑定 → 世界层婚姻入口；没有 Accepted 内容时行为保持旧口径（拒绝），不会凭空放开。
- 其余绑定点（伦理入罪、神职婚配/性别、葬礼、和平主义）仍待接。

## D58 教义伦理 → 司法入罪（P1.3 WP3 第二点）

- `CrimeBehavior`/`CrimeType` 增加 `SexualMisconduct`、`Witchcraft`；`CrimeRegistry` 注册对应罪行（性伦理 40 / 巫术 50），仍走既有司法系统，不新建审判流程。
- `GameWorld.IsDoctrineCriminal(realmId, behavior)` 按国教伦理绑定判定：只有 `Crime` 入罪；`Shunned`/`Accepted`/未指定都不入罪。`TryTrialDoctrineCrime` 非罪行直接驳回，入罪才调用既有 `TryTrialCrime`。
- 不做：`Shunned` 的社会排斥后果（关系/声望）暂未接，留给后续 WP3 批次。

## D59 教义和平主义 → 宗教战争门禁（P1.3 WP3 第三点）

- `GameWorld.IsStateFaithPacifist(realmId)` 读国教 `DoctrineBindings.pacifism`。
- 若为真：`Crusade/ReligiousConflict` 宣战直接拒绝；`DeclareGreatHolyWar` 直接拒绝。普通世俗战争不受影响，避免把和平主义误写成“不能打仗”。
- 测试：临时给国教加 `doctrine_pacifism`，断言宗教战争被拒，`finally` 还原内容。

## D60 Shunned 的社会后果（P1.3 WP3 第四点）

- `IsDoctrineShunned` 与 `ApplyShunnedStigma`：当伦理绑定为 `Shunned` 时，同政权、同信仰、在世的成年人（最多 12 人）对该行为人的好感下降 5，并写编年史；不入罪、不走司法。
- `TryTrialDoctrineCrime` 在非罪行分支调用它：Crime 走司法，Shunned 走社会排斥，Accepted/未指定无反应。三条口径互斥，避免“忌讳”只是空标签。

## D61 删除 legacy GameSaveSystem（P2.5 双源清理第一步）

- 事实：`GameSaveSystem` 已标 `[Obsolete]`、未接游戏循环，全库无引用；唯一残留是 `SaveJsonMigration` 仍用 `GameSaveData` 读旧档。
- 裁决：删除 `GameSaveSystem.cs`（约 700 行的第二套写/读路径），保留 `GameSaveData` 仅作旧档迁移 DTO；新档一律走 `SaveSystem + SaveData`。
- 下一步：把 `GameSaveData` 的字段并进 `SaveData` 后删除迁移 DTO；本轮不硬删，避免旧档不可读。

## D62 军事占领压力 → 聚落毁灭（P1.1 毁灭端口接线）

- `SettlementData.occupationPressureDays` 随存档往返；军队进入交战方聚落地块后，由 `ApplyOccupationPressure` 每日累加。
- 连续被同一占领方占领 90 天 → `SettlementDestructionSystem.DestroySettlement(..., Dismantle)`，进入废墟；占领方变化/退出则清零。
- 军事占领仍只写 `occupyingRealmId`，所有权与割让仍归和约；毁灭是占领的长期后果，不是第二套领土系统。

## D63 海军寻路/行军（P1.5）

- 新增 `NavyMovementSystem`：用 `PathfindingSystem(isNavy=true)` 规划海路，每日推进一格；路径含起点时剥掉，终点清空回 Idle。
- `NavySchedule` 在补给日结后调用 `MoveTick`；不写陆军路径、不共享 Army.MovePath。
- 未做：海战结算、舰船建造消耗；海军仍不是陆军子类。

## D64 教职接线：性别/独身/君权神授的单一判定（P1.3 WP3 第五点）

- 新增 `DoctrineIdentityRules`（FaithHost）：`CanHoldReligiousOffice(bindings, isMale)`、`CanMarry(bindings, SocialIdentityFlags)`、`RequiresCelibacy`、`IsSacredKingship`。教职性别、教职婚姻、君权神授只在这一处判定，供任官/婚配/宗教领袖遴选共用；这就是计划里「教理性别/婚姻与 M5 人格效力合并为一个 handler」的落点。
- `GameWorld.FaithLeadership`（`faith.leadership.monthly`，order 38）：
  - 君权神授 → 宗教领袖恒为该信仰覆盖政权的君主（结构性规则，不是加成）。
  - 其余信仰 → 从信奉该信仰的政权里按虔信遴选，学者优先、贵族次之；教职性别为男性专属时跳过女性；独身档跳过已婚者。
  - 只有现任领袖死亡/违规才换人，月结一次，不做每日全量扫描。
- `TryArrangeMarriage` 增加独身门禁：在任宗教领袖所属信仰要求独身时，婚配直接拒绝并给出理由；平民不受教职婚姻教义影响。
- 未接：教职任免的玩家入口（官职 0–5 仍无语义定义）；`Shunned` 之外的人格效力（法律适用按身份的差异）仍属 M5 剩余。
- 葬礼：`DoctrineBindings.funeral` 已可解析，但当前没有遗体/仪式/纪念物系统，唯一可接的只有抽象加成，违反宪章原则 5（机制优先于数值）。裁决：**内容就绪、消费者暂缓**，待仪式/节期系统落地后再接线；不假装已完成。


## D65 军队地图标记（P1.5 最后一个代码项）

- 契约：`Core.Contracts.MapArmyInfo` + `IMapArmyView`（只带 Core 类型，`combatState` 用 int 传值）；`MapWorldViewAdapter` 实现，投影 `GameWorld.armies`，跳过 `CombatState.Dead`。
- 渲染：`ArmyMarkerLayer` 与 `SettlementIconLayer` 同构——代码创建、程序化箭头 Sprite、按政权 id 确定性配色（交战红/围城橙/溃退灰）、每帧轻量跟随 `currentTileIndex`、相机 billboard。
- 开关：受 `MapLayerConfig.showArmyMarkers` 控制（默认关，属可选叠加层，不改变既有画面）；`MapRenderer.Start`/`SetWorldView`/尺寸重同步三处接上。
- Rendering 仍只依赖 `Core.Contracts`，没有把 `GameWorld/Army` 带进表现层。
- 未做：舰队标记（`Navy` 还没有地图视图契约）；海战结算；舰船建造消耗。

## D66 文化传播/同化首个执行点（P2.1 第一批）

- 事实：`CultureTransition.GetGroupResistance/CanCultureMerge` 与 `CultureSimilarity` 早已存在，但全库 0 调用；文化此前只有阶段演化与地图着色，没有可观察的人口变化。
- 新增 `CultureTransmissionSystem.MonthlyTick(realms, cultures, tiles)`：逐地块读政权 `primaryCultureId`，对非主文化人口块计算既有群体阻力；阻力低于 `AssimilationResistanceCeiling=0.45` 时累加 `PopulationBlock.culturePenetration`（既有字段，已是满意度输入），满 100 才换文化。
- 换文化时优先并入「种族/信仰/阶级/职业一致」的同文化人口块，人口守恒；不做碎片人口。
- 阻力高于上限时进度每月回落 3（接触减少），不产生反向同化。
- 调度：`culture.transmission.monthly`（order 39），月结一次，不逐日扫描。
- 未做：文化**分裂**（需要运行时新建文化 id/命名/子文化登记）与**融合**（两个文化合并为一个新文化）；本批只开「传播/同化」一条可验证链。

## D67 一个 UI 一个文件，注册进管理器（P1.6 UI 口径）

- 用户裁决：**每个 UI 面板只由一个文件负责**（构建/数据/交互都在该文件），管理器不再为每个面板持有一大组 `[SerializeField]` 字段与私有方法；面板构建后注册到管理器。
- 新增 `IUserInterfacePanel`（PanelId/IsOpen/Build/Show/Hide）与 `UiPanelRegistry`（Register/TryGet/Get/HideAll）；`UIManager.Panels` 是唯一注册表。
- 第一个落地：`CharacterPanelView`（新文件）——自建标题栏/正文/翻页/关闭，只读 `ICharacterQuery + ICharacterDetailQuery`（不引用 Simulation 家族），支持窄契约注入以便测试；`UIManager` 启动时创建并注册，`OpenCharacterPanel`/翻页/关闭优先转调它。
- 旧场景字段 `characterPanel` 保留为回退（当前场景值为 `fileID: 0`，即面板对象已缺失）；EditMode 里未构建视图时仍走旧路径，既有测试不变。
- 后续：家族树、革新面板按同一模式各建一个文件并注册；完成后再删除旧字段/旧方法。

## D68 家族树/革新面板同模式落地（P1.6 / P1.4 UI）

- `FamilyTreePanelView`：单文件自建面板，只读 `ICharacterQuery + ICharacterDetailQuery`，展示王朝/家族/父母/配偶/兄弟姐妹/子女与生死状态，支持翻页；注册为 `family_tree`。
- `InnovationPanelView`：单文件自建面板，只读 `IInnovationQuery`，展示已研究数与名称、可研究项与前置名称；注册为 `innovation`；只展示，不改研究状态。
- `UIManager` 启动时构建并注册两者；`SetWorldReadModel` 统一传播。场景里 `familyTreeOpenButton` 为 `fileID: 0`，革新面板原本无入口，因此补 `C/F/I` 三个键盘入口（输入框聚焦与主菜单时不响应），不新造顶栏按钮。
- 共用构建骨架抽到 `UiPanelBuilder`（Rect/Text/Button 重复劳动），面板内容与交互仍各自一个文件负责。

## D69 P1 施工面收口

- P1.1–P1.6 的代码项均已落地；葬礼按 D64 明确暂缓（无仪式系统），海军海战/舰船建造不属于 P1.5 计划的「海军写侧」，留到 P2 统一裁决。
- 进入 P2：文化分裂/融合、信仰六层与教义参数 schema、信息传播/Intrigue、内容外提与 Mods、架构 A–F 与双源清理、Compute/Apply 两段式。

## D70 Mods 布局：一级子目录即一个 mod（P2.4 F5 第一刀）

- 事实：`StreamingAssets/Mods/` 下已有 `Dna/DnaDefs.json`（标准 mod 布局：mod 根内再放 `Dna/` 等目录），但 `ContentSourceCatalog` 把整个 `Mods/` 当成一个源，按 `Mods/Dna/DnaDefs.json` 之外的标准路径查找会落空 ⇒ **该 mod 实际从未被加载**。
- 裁决：`Mods/` 的一级子目录各算一个 mod 源，按目录名稳定排序后给 100、101… 优先级；没有子目录时才退回“整个 Mods 根 = 单个平铺 mod”兼容旧布局。
- 覆盖仍走既有 provider 链（Base→mods 优先级）：同 id 后者覆盖前者，不新增平行内容库。
- 未做：启动期包校验器（F5 后半）、`mod.json` 清单/依赖/启用开关；当前布局不读清单。

## D71 启动期 mod 清单校验（P2.4 F5 第二刀）

- `ContentPackageManifest` 增 `enabled`（默认 true）与 `dependencies`（暂只记录，不做自动排序/拉取）。
- `ContentSourceCatalog` 对每个 mod 目录读 `mod.json`：存在则走 `ContentPackageValidator`，清单非法或 `enabled=false` 时整包跳过并留日志；缺清单时按兼容模式加载（当前 `Mods/Dna` 即无清单）。
- 校验只做结构（formatVersion/packageId/contentTypes/路径逃逸），不做跨 mod 依赖解析；依赖字段先落数据，排期留给后续。
- 测试覆盖：合法加载、禁用跳过、非法跳过、缺清单兼容。

## D72 文化融合（P2.1 第二批）

- 新增 `CultureFusionSystem.MonthlyTick`：政权内某文化人口占比 ≤15%、且不是任何政权的主文化、平均整合度 ≥60、`CultureTransition.CanCultureMerge` 通过 ⇒ 被主文化吸收。
- 融合的实际后果有两层：① 该政权内该文化的人口块并入主文化（复用同化入口，人口守恒、同属性合并）；② 主文化从被吸收文化吸收最多 2 条可兼容传统（`CultureTransitionHost.ListTraditionCandidates`，含角色门禁与互斥检查）。
- `CultureData.mergedIntoCultureId` 记录吸收结果；被吸收文化保留实体（人物仍引用其 cultureId），但不再参与传播/融合。
- 调度 `culture.fusion.monthly`（order 40），编年史留痕；同化与融合共用 `culturePenetration`，不新增"融合点"。
- 未做：文化**分裂**（需要运行时新建文化 id 与命名）；融合后主文化颜色的重算。

## D73 文化分裂（P2.1 第三批，P2.1 收口）

- 新增 `CultureSplitSystem.MonthlyTick`：某文化在「非故土」政权内人口占比 ≥25%、平均整合度 <25、母文化在别的政权仍是主文化、`allowsBranching`、冷却 10 年、子文化数 <4 ⇒ 就地分出一个子文化。
- 子文化继承母文化的语言/语言族/族群/传承/传统/阶段/成熟度，`parentCultureId` 指向母文化、`allowsBranching=false`（防止无限增殖）；命名 `母文化名·宿主政权名支`，颜色在母色上偏 0.06 色相。
- 宿主政权内该文化的人口块改挂子文化（复用同化入口，人口守恒）；母文化记 `childCultureIds` 与 `lastSplitDay`。
- 调度 `culture.split.monthly`（order 41），编年史留痕。分裂、同化、融合三者互斥：整合度高→同化/融合，整合度低且有故土→分裂。
- **P2.1 收口**：传播、融合、分裂三条链均有执行点与测试；未做：融合后主文化颜色重算（表现层，不影响模拟）。

## D74 教义 schema：组→档、域/序、参数注册表（P2.2 第一刀）

- `DoctrineOptionDef` 增 `groupId/kind/level/parameters`；`DoctrineParameterDef{key,value,source}`。
- `DoctrineSchema`：组由 optionId 前缀派生（ethics/marriage/funeral/clerical_*/ritual_devotion/sacred_material/theology），每组带 domain 与 kind（sequence/category）；参数键走全局注册表（`marriage.consanguinity_degree`、`marriage.spouse_count`），内容不得自造 key，值必须带 source 或 `TODO_M05`。
- `DoctrinePool.Load` 回填组与组内序、校验每条选项；未知组/未注册参数/无来源参数记录 `Debug.LogError`，不静默吞掉。`GetGroupIds/GetGroupOptions` 成为消费入口。
- 测试覆盖：前缀映射、域/序、未注册参数拒绝、已注册+来源通过、无来源拒绝、真实 46 条内容全部可归组。
- 未做：组→档的 UI 选择、`divergence` 计算按组权重、参数消费点（目前只有声明与校验）。

## D75 宗派运行时创建与挂靠（P2.2 六层第二刀）

- `GameWorld.CreateDenomination(faithId, name, parentDenominationId)`：从一条 faith 分出宗派，宗派必挂同一 religion、父宗派必须同教；写 `sinceDay`、`status=Tolerated`、`isDefault=false`，把 faith 挂过去并重算成员关系，编年史留痕。
- `GameWorld.AttachFaithToDenomination(faithId, denominationId)`：分裂后邀请同教 faith 挂靠的唯一入口；跨教拒绝。
- 宗派 id 从现有正 id 递增；默认宗派的负 id 段不受影响。成员关系沿用 `RebuildDenominationMembership`，不维护第二份列表。
- 未做：分歧度触发的自动分裂/合一/妥协判定（需要 divergence 计算与正统边界），本批只给「分裂/挂靠」的写入口。

## D76 教义分歧度按「组」计算（P2.2 第三刀）

- 新增 `DoctrineDivergence.Calculate(a, b, options)`：按组比较；`sequence` 组取组内档位距离（分母为该组全局最大 level，不是本次选中项的最大值），`category` 组取集合距离（1 − 交集/并集）；两边都缺的组不计入分母；结果 0–100。
- `ReligionCatalog.GetDivergence` 改为委托该函数，删除旧的「每项差 30 × pillar 权重 × 非 doctrine 0.8」口径——旧口径把偏离度算成选项计数的加权和，无法表达「两档相距多远」。
- 权重目前统一（TODO_M05）；等内容给出按域权重后再替换，不在此处硬编码。
- 测试覆盖：同集=0、两组中一组不同=50、sequence 档距、category 部分重合按并集、单边缺组=100。
- 未做：正统边界（orthodoxy.tenetIds/exclusions）与异端阈值判定；本批只统一「分歧度怎么算」。

## D77 计划类型收口 + 绰号表外提（§5.1 冲突 1 / P2.4）

- `PlanType` 的 `Trade/Diplomacy/Migration/Military` 加 `[Obsolete]` 标注：保留枚举值只为存档 int 序号稳定（Unity 枚举序连锁），明确「不得使用」；全库已无引用。§5.1 冲突 1 的「两种口径并存」由此消除。
- 绰号表 81 条从 `EpithetCatalog` 代码表外提到 `StreamingAssets/Base/Epithet/Epithets.json`；`EpithetDef` 标 `[Serializable]`，`ContentDefinitionIndex` 新增 `Epithet` provider 与加载器，Base→Mods 同 id 覆盖。
- `EpithetCatalog.LoadFromContent/ResetContent`：内容加载后覆盖；无内容时仍走代码兜底，避免早期测试/空内容世界拿不到数据。
- 测试：81 条齐全且 id 唯一、枚举合法；代码兜底仍可用；内容覆盖优先生效。
- 未做：其余 17 张表的外提（逐张排期，机制同上）。

## D78 建筑表外提（P2.4 第二张表）

- 29 条建筑从 `BuildingSystem.InitializeBuildingDefs` 外提到 `StreamingAssets/Base/Building/Buildings.json`；`BuildingContentDef` 用 `materialCost[{goodsId,amount}]` 表达字典（`BuildingDef.materialCost` 是 `[NonSerialized]` 字典，JsonUtility 不能直读）。
- `BuildingCatalog`：内容加载后由 `BuildingSystem` 构造时复制；未加载时仍走原代码表，保证早期测试/空内容世界可用。`ContentDefinitionIndex` 新增 Building provider，Base→Mods 同 id 覆盖。
- 测试：29 条齐全且 id 唯一、材料合法；内容转换（类别/工期/材料字典）；内容优先与代码兜底两条路径。
- 未做：其余 15 张表（单位/商品/灾害/政体门槛/阶层门槛等）逐张外提。

## D79 兵种表外提（P2.4 第三张表）

- 14 条兵种从 `GameWorld.InitializeUnitDefs` 外提到 `StreamingAssets/Base/Unit/Units.json`；`UnitContentDef` 用 `recruitCost[{goodsId,amount}]` 表达字典（`UnitDef.recruitCost/terrainModifiers` 是 `[NonSerialized]`）。
- 原代码里 `AddUnitDef` 的默认招募成本与 `SetUnitRecruitCosts` 的覆盖在生成 JSON 时合并为最终值，避免读档/加载后出现两套成本。
- `UnitCatalog` + `ContentDefinitionIndex.Unit` provider：内容优先，未加载时 `GameWorld` 仍走原代码表；Base→Mods 同 id 覆盖。
- 测试：14 条齐全/唯一/成本合法；类别、成本字典、革新前置转换。
- 未做：其余 14 张表（商品 57 / 灾害 12 / 政体门槛 35 / 阶层门槛等）。

## D80 灾害表外提（P2.4 第四张表）

- 12 条灾害从 `DisasterSystem.InitializeDisasterDefs` 外提到 `StreamingAssets/Base/Disaster/Disasters.json`；`DisasterContentDef` 用 int 传枚举，`DisasterCatalog` 转回 `DisasterDef`。
- 时长钳制：`min ≥ 1`、`max ≥ min`（内容写 0 时不会产生负时长或反向区间）。
- `DisasterCatalog` + `ContentDefinitionIndex.Disaster` provider（该表合法使用 0 作为 Drought 键，store 显式允许默认键）；内容优先、代码兜底。
- 测试：12 条齐全/类型唯一/区间合法；转换与钳制。
- 未做：其余 13 张表。

## D81 商品表外提（P2.4 第五张表）

- 57 条商品从 `GameWorld.InitializeGoodsDefs` 外提到 `StreamingAssets/Base/Goods/Goods.json`；生成时合并了 `MarkPerishable/MarkLiving/MarkNaturalResource` 的后置标记，避免代码与内容两套状态。
- `GoodsCatalog` 转回 `GoodsDef`：自然资源的默认生物群系在目录层补全（与代码 `DefaultNaturalBiomes` 同口径），`spawnCondition` 用 `Unrestricted`；属性仍由既有 `GoodsPropertyCatalog` 合并，`ApplyGoodsProperties` 成为唯一合并点。
- `ContentDefinitionIndex.Goods` provider 允许 0 键（小麦）；内容优先、代码兜底；Base→Mods 覆盖。
- 测试：57 条齐全/唯一/自然丰度合法；自然标记与默认群系转换。
- 未做：其余 12 张表。

## D82 正统边界与宗派状态（P2.2 第四刀）

- `ReligionDef` 增 `orthodoxyTenets/orthodoxyExclusions`（可选；空=未声明边界）。
- `OrthodoxyEvaluator`：候选 faith 的教义必须包含全部必备项、不得包含排除项；未声明边界返回 `Unspecified`，不自动判异端（failMode=block）。
- `DenominationStatusRules`：声明性越界 → `Heterodox`（优先于分歧度）；未越界但分歧度 ≥30 → `Tolerated`；否则 `Mainstream`。阈值标 `TODO_M05`。
- `GameWorld.TickDenominationStatuses`（`faith.orthodoxy.monthly`，order 42）：对每个非默认宗派，取成员 faith 与其上位 religion 的分歧度/越界数，写回 `Denomination.status` 并留编年史。
- 测试：未声明边界、缺必备、含排除、合规；状态三档判定。
- 未做：合一/妥协/敌意的局势阶段与自动分裂（需要宗教局势状态机与玩家/机构裁决入口）。

## D83 宗教局势阶段（P2.2 第五刀）

- `ReligionStage`：合一/脆弱的合一/妥协/敌意/分裂；`ReligionStageRules.StageFor(divergence)` 按 20/40/60/80 分档（TODO_M05）。
- `GameWorld.TickReligionStages`（`faith.stage.monthly`，order 43）：对每个有 ≥2 条 faith 的 religion，取「faith 自身教义 vs 上位 religion 教义」的最大分歧度定阶段；达到分裂阈值时，只把分歧最大的 faith 分出宗派（若它仍挂默认宗派），避免每月重复分裂。
- 阶段本身是**派生值**，不入档；只有分裂动作写实体（Denomination/faith.denominationId），与既有存档字段一致。
- 未做：合一（消除分歧）与妥协（保留分歧但维持共融）的裁决入口；当前阶段用于观测与分裂触发，不假装已实现谈判。

## D84 合一/妥协入口与教义覆盖入档（P2.2 收口）

- `FaithSystem` 增 `doctrineIds`（运行时教义覆盖，空=沿用内容表）与 `communionUntilDay`（共融期限）；`FaithDoctrine.GetIds/SetIds` 是唯一读写口径。
- `ReligionCatalog.GetDivergence(doctrineIdsA, doctrineIdsB)` 重载；分歧度/正统判定改用有效教义集合（覆盖优先），不再直接读内容定义。
- `GameWorld.TryUnifyReligion`：同教各 faith 采用最常见的教义集合（平票取上位 religion 正统集合），清共融期限；`TryCompromiseReligion`：保留各自教义，设定共融期限；`TrySchismReligion`：显式分出分歧最大的 faith。
- 局势月结：若同教全部 faith 都在共融期内，阶段最高只到「妥协」，不会升级为敌意/分裂。
- 入档：`FaithSaveData` 增 `doctrineIds/communionUntilDay`，读档 overlay；旧档默认空/-1，不冲内容口径。
- 测试：合一后教义一致、妥协写共融期限、DTO 往返。

## D85 政权货币余额单一入口（P2.5 C 第一步）

- 新增 `RealmTreasury`（Get/TrySpend/Spend/Add/Set）：`RealmData.treasury` 仍是存档字段，运行时运行代码经此增减；`TrySpend` 先预检后整笔扣款，不产生部分扣款。
- 已迁移：建筑扣款、`TreasuryLedger` 国家付款/退款、军事预算、经济改善投资。`LawAndDecreeSystem` 的 `ref float treasury` 纯函数保持原样（它是算法层，不持有 RealmData）。
- 未迁移：外交赔款等必须允许负余额的路径仍可用 `Spend`；`DiplomacyManager` 的内部转账下一批再收口。
- 测试：余额不足不扣款、足额扣款、`Spend` 允许负余额、Add/Set。
- 未做：`stability`/`population` 的同类入口；字段仍未私有化（存档直接序列化，收紧需 DTO 配合）。

## D86 特质表外提（P2.4 第六张表）

- 108 条人格特质从 `PersonalityTraitDatabase` 的外提为 `StreamingAssets/Base/Trait/Traits.json`；`TraitContentLoader` 懒加载并转回 `PersonalityTrait`（枚举越界回落到 0，坏档不抛）。
- `PersonalityTraitDatabase.All` 改为属性：内容表可用时返回内容表，否则回退代码兜底数组；所有既有消费者（按数组用）无需改调用点。
- 测试：108 条齐全/唯一/枚举合法；加载器字段与修正值（`brave_1` 武力 +1、冲突 `craven_1`）。
- 未做：其余内容表；代码兜底表暂保留（删表需先确认真实内容加载在全部入口都发生）。

## D87 政权稳定度单一入口（P2.5 C 第二步）

- 新增 `RealmStability`（Get/Adjust/Set/Drift）：统一钳制 0–100；`Drift` 向基准值按比例靠拢，替代裸 `Mathf.Lerp`。
- 迁移帝国级写点：AI 决策、法律年度效果、政体改革、政权更替、外交赔款、战争议事代价、政治系统回归。
- 修正旧口径不一致：有的只 `Max(0)`、有的只 `Min(100)`，现在两端都钳。
- `LawEffects` 的局部 `e.stability` 是算法中间量（非 RealmData），保持原样。
- 测试：两端钳制、Set、Drift。
- 未做：population 入口（人口块是单一真源，写点分散在迁移/生育/征兵，需按场景逐个收口）。

## D88 信息滞后：已知人物的过期遗忘（P2.3 第一刀）

- `KnownCharacterInfo` 增 `characterRealmId`（Learn 时写当前所属政权；旧档 -1 保守保留）；显式参数less构造器避免结构体字段初始化报错。
- `CharacterKnowledgeRegistry.PruneStale(currentDay, maxAgeDays)`：只清「非本政权且超过期限无接触」的条目；本政权人物永远在视野内，不因时间被遗忘。
- `GameWorld` 月结 `knowledge.decay.monthly`（order 44），期限 3 年（1095 天，TODO_M05）。
- 存档：`KnownCharacterSaveData` 增 `characterRealmId`；读档经 `Learn` 按当前人物归属重建，不依赖旧值。
- 测试：外邦过期被遗忘、本政权保留、近期外邦保留、旧档未知归属保守保留。
- 未做：Intrigue（WP5）——计划本身要求先有 S19 地图知识；当前只有角色知识，不硬造密谋系统。

## D89 聚落形态建造门槛外提（P2.4 第七张表）

- 30 条 `BuildableType` 建造门槛从 `BuildingAvailabilitySystem.RegisterAllRequirements` 外提到 `StreamingAssets/Base/Buildable/BuildableTypes.json`；`BuildableRequirementLoader` 懒加载并转回 `BuildingRequirement`（生物群系/海洋层级/聚落等级用 int 数组，越界丢弃）。
- `BuildingAvailabilitySystem.Initialize` 内容优先、代码兜底；`allowedBiomes` 等数组字段在加载时重建，避免 JsonUtility 对枚举数组的兼容问题。
- 测试：30 条齐全/唯一/区间合法；关隘形态（瓶颈、6 个群系、坡度 5–40）转换正确。
- 长跑回归：`longrun.cmd` 2 seed × 100 年通过（1/1，260s），新加的月结系统未造成超时。
- 未做：其余零散常量表（多为逐类 switch/权重，不是可外提的平表）。

## D90 影响力场 Compute/Apply 分离（P2.6 第一刀）

- 事实：`SettlementInfluenceSystem.Calculate` 原先在计算过程中直接写 `anchor.influenceTier`；`MapWorldViewAdapter.TryGetInfluenceOverlay`（查询路径）每次调用都**整场重算**，既昂贵又写模拟状态。
- 拆分：`Calculate` 只产 `InfluenceField`（纯读）；新增 `Apply(settlements, field)` 只写 anchor 等级；`UpdateControlTiers` 日结按「Calculate → Apply → 缓存」锁步执行。
- 查询路径改为读 `SettlementInfluenceSystem.LastField`：未到日结/未结算时返回 false，不再重算、不再写状态。
- 测试：Apply 写 anchor、空输入不抛、Calculate 不写 anchor。
- 未做：调度器全局两相 API（当前只有该重量级系统需要，先落在系统内部；无消费者的通用抽象不提前建）。

## D91 mod 依赖排序（P2.4 F5 收口）

- `ContentSourceCatalog` 先读各 mod 的 `mod.json`，按 `dependencies[].packageId` 做稳定拓扑排序：被依赖者先加载、优先级更低。
- 缺依赖只告警并忽略该边（不让缺件 mod 静默改变加载顺序）；依赖成环告警后按目录名顺序兜底，不中断启动。
- 无清单/平铺布局兼容路径保持原样；`enabled=false`/非法清单仍在排序前剔除。
- 测试：A 依赖 B → B 先加载；既有布局/校验测试不变。
- 未做：跨 mod 内容冲突的细粒度诊断（同 id 覆盖仍是“后者胜”）。

## D92 经济原型文案外提（P2.4 第八张表）

- 11 条 `EconomicalArchetypeInfo`（名称/描述/五轴偏好）从 `EconomicalArchetypes` 外提到 `StreamingAssets/Base/Archetype/EconomicalArchetypes.json`。
- `EconomicalArchetypeLoader` 懒加载；`EconomicalArchetypes.All` 改为属性：内容优先、代码兜底；`Get()` 语义不变。
- 测试：11 条齐全/唯一/枚举合法；好战原型名称与 war/build 偏好转换正确。
- 未做：`PersonalityDimensions`/`DnaLocus`/`GenerationStage` 等属于枚举或流程顺序，不是可编辑平表，不外提。

## D93 设置程序集独立（P2.5 B 第一步）

- 事实：`*.app` 忽略规则误吞了 `Scripts/App/MyriadCreation.App/` 整个目录，App 程序集源码此前从未入库（GameManager/SceneFlow/Bootstrap/Settings 全部只在本地）。已在 `.gitignore` 加目录级例外并入库。
- 新增 `MyriadCreation.Settings` 程序集（引用 Core + Infrastructure），把 `SettingsModel/SettingsService` 从 App 移出；App、UI、两个测试程序集都显式引用它。
- 目的：UI 对 App 的依赖只剩 `Bootstrap/SceneFlowController/GameManager`（下一步改用 `AppServices` 契约），设置不再需要 App。
- 未做：UI 真正去掉 `MyriadCreation.App` 引用；UI 仍引用 Infrastructure（Save/Audio）与 World（SettlementData 标签），这两项留给 B 的后续步骤。

## D94 UI 去 App/World 依赖（P2.5 B 主刀）

- 方向反转：UI 不再引用 `MyriadCreation.App` 或 `MyriadCreation.World`；App 引用 UI（App 装配 UI）。架构门禁同步改为「App 必引 UI、UI 不得引 App」，层序 UI(rank6) < App(rank7)。
- `MainMenuUI` 去掉 `Bootstrap/SceneFlowController/GameManager/WorldSetupOptions`：开新档/自定义世界改走 `AppServices.AppFlow.StartNewGame(NewGameRequestDto)`；读档走 `AppServices.GameSession.LoadGame`；自定义模式用 `NewGameRequestDto.ContentModeCustom` 常量。
- `UIManager`/`UIManager.Panels` 的 GameManager 访问改 `AppServices.GameSession`。
- `SiteTypeLabels` 移到 World 程序集（`MyriadCreation.World.Presentation`），UI 不再因一个标签文件依赖 World；SettlementDetailPanel 删除未使用的 World using。
- 未做：UI 仍引用 Infrastructure（SaveSystem/SfxPlayer）；Save/Audio 需要契约化后才能去掉。B 剩余这一项。

## D95 UI 去 Infrastructure 依赖（P2.5 B 收口）

- 音频独立：`MusicPlayerSystem/SfxPlayer` 从 Infrastructure 移到新 `MyriadCreation.Audio` 程序集（仅引 Core）；Settings、UI、Infrastructure（旧档 DTO）、测试显式引用。
- 存档槽契约：`Save.Contracts.ISaveSlotService`（列目录/列存档/删除）；Infrastructure 提供 `SaveSlotService`，Bootstrap 在 Awake 注册到 `AppServices.SaveSlots`；UI 的 SaveLoad/Settings 面板只依赖契约。
- UI asmdef 最终 references：Core / Content / Rendering / App.Contracts / Save.Contracts / Settings / Audio / UI / TextMeshPro——不再引用 App、World、Infrastructure。
- 架构门禁保持：UI 不得引用 Simulation 家族；App 装配 UI。
- 未做：`SaveSystem` 的读/写仍由 App 层（Bootstrap/GameManager）直接调用；若以后玩家侧也要读写入口，再扩契约。

## D96 存档运行时独立，删除 Infrastructure 程序集（P2.5 A 收口）

- 19 个 `Infrastructure.Save` 文件整体迁入新 `MyriadCreation.Save.Runtime`（引用 Core/Save.Contracts/Content/World/Simulation/Faith/Culture/Audio）；命名空间保持 `MyriadCreation.Infrastructure.Save`，调用方无需改 using。
- `MyriadCreation.Infrastructure` 程序集随之删除（音频已拆到 Audio、设置已拆到 Settings、存档运行时已拆到 Save.Runtime）；Editor/App/两个测试程序集把引用换成 `Save.Runtime`。
- 判据达成：UI 不引 Infrastructure；App/Editor/Tests 只通过 Save.Runtime 访问存档实现；Core/Save.Contracts 保持为契约层。
- 架构门禁新增 `Save.Runtime` 分层位（与 Rendering/Infrastructure 同层）。
- 未做：`SaveSystem` 仍是静态入口；若要多平台后端，再在 `Save.Contracts` 上加读写契约。

## D97 P2.5 F（Host 适配层移出 sim.asmdef）本轮作废，理由与替代

- 现状：`LawHost/FaithHost/CultureHost/ContentHost` 都是**适配器**，直接持有 `GameWorld/RealmData/CharacterManager` 等 Simulation.State/WorldState 类型；把它们移到独立程序集的前提是先把这些类型抽成契约（约 30 个文件、跨 4 个子系统），否则只是把循环依赖换个 asmdef。
- 裁决：F 本轮**作废**，维持同一程序集内的 Host 分层；已完成的是更外层的解耦（A：Infrastructure 删除、B：UI 去 App/World/Infrastructure、Save.Contracts/Audio/Settings 独立）。F 的触发条件写死为「当 C 的状态封装把 RealmData/GameWorld 收成查询+命令后，再按 `IFaithContext` 的模式抽契约」。
- 这不是“没做”，而是拒绝在没有契约消费者时新增一层空抽象（宪章原则 5/单一真源）。

## D98 P2.3 Intrigue 本轮作废（前置缺失）

- 计划自己写明：WP5 Intrigue 须先有 **S19 地图知识**（谁知道哪块地/哪条路）。当前只有角色知识（`CharacterKnowledgeRegistry`），没有地图知识层。
- 已交付的信息传播部分：角色知识的过期遗忘（D88）。Intrigue 的密谋链、命令延迟、消息伪造在 S19 落地前不硬造。
- 触发条件：S19 地图知识入档并有查询/写入入口后，再开 WP5。

## D99 P2.6 调度器全局两相作废；P2.4 剩余表清点

- 调度器级 `Compute → Apply` 两相：当前唯一需要「先读场、再整批写」的重量级系统是影响力（已按系统内部拆分并缓存，D90）；其余系统是单相增量写。无第二个消费者时引入全局两相 API 只会增加锁步复杂度。
- 数值纪律不变：新增阈值均标 `TODO_M05`（教派状态 30/局势 20-40-60-80/信息遗忘 1095 天/教义参数）。
- P2.4 清点：计划点名的平表已外提 8 张（绰号 81、建筑 29、单位 14、商品 57、灾害 12、建造门槛 30、特质 108、经济原型 11）；法律表原已是 JSON；其余“表”是逐类 switch/枚举映射/流程顺序（如 PersonalityDimensions、DnaLocus、GenerationStage），不是可编辑内容，不外提。

## D100 遗传双轨收口：GenomeInstance 为真源（P2.5 E）

- `CharacterManager.CreateCharacter` 有父母时先走 `GeneticsEngine.Reproduce`（父/母 GenomeInstance + 指定子代性别，确定性 System.Random 种子）生成子代基因组，再用 `LegacyDnaAdapter.ToLegacyData` 产出 `DnaData` 兼容快照；子代对象同时写 `genome` 与 `dna`。
- 内容缺基因组/父母基因组不可得时才回退旧 `DnaSystem.Inherit`，保证不会生成无基因人物；`GetGenome` 仍能把旧档 DnaData 懒转成 GenomeInstance，旧档可读。
- 表型/魅力/疾病等既有消费者继续读 DnaData 快照，行为不变；新代码可只读 genome。
- 测试：双亲子代同时有 genome 与 6 座 DnaData 位点。
- 未做：彻底删除 DnaData 字段（它仍是存档 DTO 与大量消费者的输入；删除需同批改存档与表型链，风险大于收益）。

## D101 法律人格效力（M5 身份轴）落地

- `LawDef` 增 `appliesTo/exemptIdentities`（`SocialIdentityFlags` 掩码）；`LawApplicability.AppliesTo`：豁免优先，`appliesTo=None` 表示不限身份。
- `CharacterIdentity.Of(character, realmId)`：按角色给出身份旗标（Clergy 只给教士，不再把 Noble 也算作教士），外政权人物补 `Foreigner`。
- `GameWorld.HasLegalExemption(realmId, characterId)`：查现行 `activeLawId` 对应定义，命中不适用/豁免即返回 true；`TryTrialCrime` 与教义罪行审判入口据此不启动司法程序。
- 内容：`privilege` 组新增 `clergy_forum`（教士法庭，`exemptIdentities=Clergy`）；默认未设为现行法，需推行后生效，既有行为不变。
- `DefaultLawCatalog.CloneLaw` 补齐两个新字段（否则内容加载后被克隆丢掉）；测试基准表同步新增该法。
- 测试：掩码豁免/限定范围；世界里推行教士法庭后教士豁免、平民不豁免；JSON 计数与字段对拍。
- §5.1 冲突 2 的“法律人格效力”欠账由此关闭。

## D102 删除 legacy GameSaveData，法律索引随内容重载失效

- `GameSaveData` 已无生产读写路径，仅作为旧 JSON 形态被测试引用；删除该 DTO，`RealmBasicSaveData` 一并消失；`TitleHoldingSaveData/TitleInstanceSaveData/ChronicleEntrySaveData` 移入 `SaveData.cs` 继续被活存档使用。
- `SaveJsonMigration` 只保留地图旧键迁移（`FromMapSaveJson`），删除 `FromGameSaveJson`；测试改用 `MapSaveData`。活存档唯一路径仍是 `SaveSystem + SaveData`。
- `DefaultLawCatalog.ResetLawIndex` 在内容重载/重置时清空 lawId 索引，避免用旧定义做人格效力/豁免判定。
- 验证：`EXIT=0`、`876/878`、零 `error CS`。

## D103 货币/稳定度写点全量收口 + 门禁

- 剩余直接写点全部改走 `RealmTreasury`/`RealmStability`：AI 补贴、文化建国加成、法律推行扣款、外交赔款/赠款、游牧收入、政治收入、政体更替稳定度。
- 新增 `RealmMutationGatewayTests`：扫描 `Scripts/Simulation/**/*.cs`，禁止 `realm.treasury/stability` 的直接赋值（报告 DTO、TileData、LawEffect 中间量等已知非 RealmData 目标除外）。以后新增绕过会红。
- 字段本身仍为 public（存档 DTO/大量读点），但写入路径已单源；把字段私有化需要同时改 RealmDTO 映射与读点，留作 C 的下一步。
- 验证：`EXIT=0`、`877/879`、零 `error CS`。

## D104 删除 UIManager 旧角色/家族面板双源（一个 UI 一个文件）

- `UIManager` 删除 `characterPanel/familyTreePanel/charNameText...familyTreeText` 等场景绑定字段与 `_characterList/_charIndex`，删除 `UpdateCharacterPanel/RefreshCharacterList/RefreshFamilyTreePanel/CurrentCharacterId/FillCharacterTexts/FamilySummary/PersonalitySummary` 旧实现。
- 角色/家族面板唯一实现为 `CharacterPanelView`/`FamilyTreePanelView`（已注册到 `UiPanelRegistry`）；UIManager 只保留打开/关闭/翻页的薄转调与 `charOpenButton` 入口（顶栏按钮仍可用）。
- 测试改为只验证 UIManager 的地块面板；角色面板由 `CharacterPanelViewTests` 覆盖。场景 YAML 中的旧字段名变为未知键，Unity 忽略，不再有第二套渲染路径。
- 验证：`EXIT=0`、`877/879`、零 `error CS`。

## D105 删除 legacy 地图存档迁移路径

- `MapSaveData`/`SaveJsonMigration` 无生产调用者（`SaveSystem` 只读 `SaveData`），删除两者；`HabitationSiteSaveData` 的规范键测试改用测试内 DTO，保留字段契约断言。
- 旧 `settlements/burgs` JSON 迁移路径实际已不可达（活档入口不读 GameSaveData/MapSaveData），删除后不再维护第二套地图 DTO。
- 验证：`EXIT=0`、`876/878`、零 `error CS`。

## D106 PopulationLedger：人口块写点单源（P2.5 C）

- 新增 `PopulationLedger`（Core.Data）：Ensure/Total/AddCount/Add/Scale/RemoveFromIndex/SplitToNewBlock/ChangeCulture/ChangeFaith/Prune；同属性合并、人口守恒。
- 迁移：MapActor 增减人口、文化同化/融合/分裂、传教改宗（缩放与拆分）、疾病减员、饥荒减员、劫掠屠杀、人口增长；`CultureTransmissionSystem.ConvertBlock` 变薄包装。
- 阶梯再平衡 `SocialDifferentiation` 仍保留自身 deposit/withdraw（它是按阶层重分配的算法内部量），在门禁中显式豁免并注明。
- 新增 `PopulationMutationGatewayTests`：扫描 `.count *=/+=/-=`，除 `PopulationLedger` 与 `SocialDifferentiation` 外禁止直接改人口。
- 测试：合并/守恒、拆分、改文化合并、减员与碎片清理。
- 验证：`EXIT=0`、`882/884`、零 `error CS`。

## D107 GenomeInstance 成为运行时遗传主路径（P2.5 E）

- `CreateCharacter` 新增 `genomeOverride`；父母生育/新生直接使用 GenomeInstance，不再生成 DnaData；表型由 `DnaSystem.ComputeExpression(genome, race)` 推导（显性/杂合/隐性方向保留，幅度不再每次随机）。
- 魅力加成改由 `DnaSystem.IsAppearanceHomozygous(genome, race)` 判断，不再读 DnaData 位点。
- `Procreate` 直接把子代 genome 传回 `CreateCharacter`，去掉“genome→dna→genome”的往返。
- `LegacyDnaAdapter` 现在只出现在 `DnaData.GetGenome`（旧 DnaData 输入/旧档迁移）与显式 `dna` 参数兼容路径；正常新生/读档不写 DnaData（存档本来就只存 genome）。
- 测试更新：子代仅有 GenomeInstance，`dna == null`。
- 未做：彻底删除 `DnaData` 类型（旧测试/迁移入口仍用），以及 `DnaSystem` 旧随机表达式（保留作兼容）。
- 验证：`EXIT=0`、`882/884`、零 `error CS`。

## D108 调度器 Compute→Apply 两相 + 影响力迁移（P2.6）

- `ISimulationScheduler.RegisterPhased(id, cadence, order, compute, apply)`：同一 tick 内先执行全部到期系统的 Compute，再统一执行 Apply（锁步读后写，不交错）；`SimulationScheduler` 的 Apply 不进预算采样（写相轻量），预算只控 Compute。
- `settlement-influence.daily` 改为两相：Compute 只产 `InfluenceField`，Apply 写 anchor 并缓存 `LastField`；`SettlementControlSystem` 不再内部调用 `UpdateControlTiers`。
- 测试：两相条目的 Compute 全部先于 Apply；多个两相条目按注册序 Apply；既有影响力测试保留。
- 验证：`EXIT=0`、`884/886`、零 `error CS`。
- 未做：其余重量级系统尚未迁移到两相（先完成一个可测消费者，避免空抽象）。

## D109 教义快照移出 Simulation（P2.5 F 第一刀）

- `DoctrineBindings/DoctrineBindingHost/DoctrineCrime/DoctrineIdentityRules` 从 `Simulation.FaithHost` 移到 `Content.Definitions`：教义解析只依赖内容表与 Core，不再反向依赖 Simulation。
- 命名空间改为 `MyriadCreation.Content.Definitions`；调用方改用 Content 命名（11 处引用）。
- 效果：Faith 叶子只出不可变快照（RELIGION_V2 的 F 原则），Simulation 仍使用它但不构成循环依赖。
- 验证：`EXIT=0`、`884/886`、零 `error CS`。
- 未做：Faith/Culture/Law 的适配器与 Host 文件夹尚未迁到独立程序集；这是 F 的剩余主体。

## D110 Host 适配层独立 + Host 文件夹退出 Simulation（P2.5 F）

- 新增 `MyriadCreation.Simulation.Hosts` 程序集：承载 `FaithContracts`/`CultureContracts` 两个契约适配器；通过 `Core.Contracts.SimulationHostRegistry` 的工厂注册，Simulation 只引用契约，不反向引用 Hosts（无 asmdef 循环）。
- `HostRegistration.EnsureRegistered` 在运行时由 `RuntimeInitializeOnLoadMethod` 注册；EditMode 用 `[SetUpFixture]` 显式注册。
- `GameWorld.Contracts` 的 Faith/Culture 查询/命令改为接口 + 注册表工厂，查询与命令共享同一适配器实例（保持既有测试语义）。
- 其余 Host 逻辑归入 `Simulation/Systems/{FaithRuntime,CultureRuntime,LawRuntime}`：Simulation 不再有 `*Host` 文件夹；法律运行逻辑仍属仿真层（不是适配器），不入 Hosts 程序集。
- 空 `FaithContextAdapter`（无调用者）删除；教义快照上一刀已移入 Content。
- 验证：`EXIT=0`、`884/886`、零 `error CS`。

## D111 架构解耦目标收口（C/E/F/P2.6）

- C：`RealmTreasury/RealmStability` 为唯一写入口，源码门禁阻断直接赋值；`PopulationLedger` 覆盖人口块增删/转移/改身份，迁移增长/饥荒/疾病/传教/文化/劫掠写点，门禁阻断绕过。
- E：新生/生育/运行时表型以 GenomeInstance 为主，`DnaData` 不再由正常路径生成；`LegacyDnaAdapter` 只出现在 `DnaData` 旧输入转换（旧档/测试），存档本身只存 genome。
- F：新增 `MyriadCreation.Simulation.Hosts` 程序集承载 Faith/Culture 契约适配器，`SimulationHostRegistry` 反转依赖；Simulation 不再含 `*Host` 文件夹，法律/信仰/文化运行逻辑归入 `Systems/*Runtime`。
- P2.6：`RegisterPhased` 提供 Compute→Apply 两相（全部 Compute 后统一 Apply），影响力场完成迁移并有顺序测试。
- 最终验证：`build.cmd` `EXIT=0`、`884/886`（2 个 Explicit 跳过）、零 `error CS`；`longrun` 2 seed×100 年 `1/1` 通过、`EXIT=0`。

## D112 程序化家族纹章（借鉴纹章生成思路，不复制实现）

- 新增 `Heraldry`：由 `familyId + 名称 + 建国年` 确定性生成 `pattern`（bend/chevron/cross/saltire/paly/quarterly/fess/pall）与三色（field/charge/detail），只存字符串，不依赖美术资源。
- `CreateFamily` 时 `Ensure` 写入；旧档/缺省按需补齐且幂等；`FamilySaveData` 增 `coaPattern/coaColors` 并往返。
- 测试：确定性、幂等、存档往返。
- 为什么不用参考项目代码：纹章规则可抽象成“确定性符号组合”，参考实现的资源/代码许可不明；这里只借模型。
- 城市俯视图：当前 `SettlementBlock` 有区划/面积/城墙数据但没有布局坐标；要做得先定义“块→俯视布局”的确定性算法与渲染层，属于独立子项目，暂不硬塞。
- 验证：`EXIT=0`、`887/889`、零 `error CS`。

## D113 纹章模型对齐 Azgaar/Armoria（MIT）

- 参考 Azgaar FMG（MIT）的纹章数据模型：`division`（perPale/perFess/perBend/perChevron/perCross/perSaltire/perPile/gyronny）、`ordinary`（bend/chevron/cross/saltire/fess/pale/chief/pall/bordure/canton）、盾形与三色。
- `Heraldry.Generate` 输出 `division|ordinary|shield` + `field|charge|detail`；底场与主纹章遵循“金属不压金属、颜色不压颜色”，`Classify` 暴露金属/颜色/污色分类。
- 仍不复制其图标/渲染资源；只借符号模型，家族创建/存档字段不变（复用 coaPattern/coaColors）。
- 城市俯视图：Azgaar 的 `burgs-generator` 生成聚落位置/人口/经济与 SVG 图标，不是街道级俯视布局；因此不按它做“城市俯视图”，需要自研布局。
- 验证：`EXIT=0`、`887/889`、零 `error CS`。

## D114 NPP：Earth Simulator 生态模型移植

- 从本机 `E:\SteamLibrary\...\Earth Simulator\EarthSimulator\ecology.js` 读取 Miami NPP 公式（温度 sigmoid + 降水指数 + 45/55°C 叶片上限），在 `World.Terrain.EcologyModel` 重新实现（不复制代码）。
- `TileData.npp` 落盘、`PlanetTerrainGenerator` 生成时计算；`CarryingCapacitySystem` 以 `0.5+0.5*npp` 缩放承载力（`npp=0` 视为旧档/未计算保持旧口径）。
- 测试：温度/降水双限制、高温封顶、承载力随 NPP 变化。
- 未做：NPP 驱动植被演替/食物链/饥荒（Earth Simulator 的 fauna 需要独立子系统）；TTHTOM 的 polity/office/flag schema 仍待接入。
- 验证：`EXIT=0`、`889/891`、零 `error CS`。

## D115 纹章/旗帜的条件性（用户裁定）

- 家族纹章**不是普遍存在**：它是文化习俗，取决于内容/事件更新。`CultureData.heraldryEnabled` 默认 false；`CreateFamily` 只有该文化启用时才生成纹章，否则 `coaPattern/coaColors` 保持空。
- 政权旗帜同样不是普遍存在：早期大多数政权只有**军旗**，现代意义上的“国旗”出现很晚且意义不同。规划中的 `BannerKind` 分 `None / MilitaryBanner / HouseholdBanner / StateFlag`；StateFlag 需民族主义/印刷/革命类革新与相应政体，不能给所有政权发国旗。
- 参考项目的旗帜生成（TTHTOM `flag_profile`）只用于有旗帜的政体，且先做军旗/家旗层，不把现代国旗当默认。
- 验证：`EXIT=0`、`889/891`、零 `error CS`；纹章改为按文化开关。

## D116 农业—食物链（P3.0）

- 产出由**耕作者 × 肥力 × NPP × 开发度**推导（`AgricultureYield`），不读繁荣度等抽象字段：`farmers * 0.03 * (0.2+0.8*fertility) * (0.5+0.5*npp) * (0.7+0.3*development)`。
- 只有 `SocialClass.Peasant` 计入耕作者；土地贫瘠（`fertility<=0.02`）、非陆地、未存在地块产出为 0，不得凭空产粮。旧档 `npp=0` 回退中性值 0.5，保持旧口径可玩。
- 作物写入地区 `TradeCenter` 的 `goodsId=0`（粮食），与既有口粮消费/饥荒处罚共用一条库存链，不另开“农业产出”字段。
- `ProduceResources` 修正：自然资源与农业分开门禁。此前 `resources==null` 会跳过整块地，导致无矿无木的地块永远不产粮。
- 校准：100 农民、肥力 0.6、NPP 0.5、开发 0.1 → 约 1.12/日，扣 100 人口粮 1.0 后略有盈余；肥力降到 0.5（旱年）→ 0.81/日，库存见底并触发饥荒轻罚。系数是校准值，调整需同时更新 `AgricultureYieldTests` 的方向断言（不断言具体数值）。
- 已知未做：食物篮子仍只吃 `goodsId=0`（肉/鱼/稻谷不参与口粮），季节/灾害只通过肥力间接影响；留待 P3.2 生态与灾害联动。
- 测试：`AgricultureYieldTests` 7 例（纯函数方向、NPP 旧档回退、入账守恒、旱灾饥荒、容量约束）；`EconomyConservationTests` 的缺粮夹具改为贫瘠地（`fertility=0`），避免农业自给掩盖饥荒前提。
- 验证：`EXIT=0`、`896/898`（2 个 Explicit 跳过）、零 `error CS`。

## D117 官职内容表与运行（P3.1 第一批）

- 参考 TTHTOM `polity_profile.central_offices` 的字段形态（rank/modules/appointment_order/candidate_gender/candidate_royal/term_years/salary/dismissal_resistance），只借 schema 与“职务=模块集合”的思路，不复制其内容/代码。
- 内容表：`StreamingAssets/Base/Polity/Offices.json`，8 槽位。`slot 0-5` 与旧 `OfficialOffice` 枚举顺序严格对齐（Governor/DistrictGovernor/Chancellor/Steward/Marshal/WarCommander），旧档整数键无需迁移即可读；新增 `6 审判官`、`7 宫廷学士`。
- `OfficeCatalog`：内容优先，Base 打底、Mods 按 slot 覆盖（在全部来源合并后统一刷新）；缺内容时回退内置六职，保证测试/工具环境可运行。重复加载整表替换，不静默合并两套口径。
- `OfficeSystem` 只做四件事：按内容表补缺（出身/性别过滤、角色优先序、同分按 characterId 稳定决胜）、任期到期/死亡离任、俸禄按日折算从国库支出（国库不足不付、不透支）、按模块查询在职官员。
- 存档：`RealmData.officeAppointedDays` + `RealmOfficeSaveData.officeAppointedDays`。旧档缺省为空 = 终身任职，避免“读档后所有官员同时到期”。
- 调度：`offices.daily`（优先级 26）每日补缺 + 任期检查 + 俸禄；战争结束仍保留即时补缺。
- 已知未做（下一批）：模块尚未接到提案/审案/征税/任免；`appointmentOrder` 的 `council` 目前只作为候选顺序标记，尚未强制议事会通过；罢免阻力只入表、未接罢免动作。
- 验证：`EXIT=0`、`906/908`（2 个 Explicit 跳过）、零 `error CS`；新增 `OfficeSystemTests` 10 例。

## D118 官职职权接线（P3.1 第二批）

- **提案权**：`LawRealmContext.IsCentralOfficeHolder` 改为查内容表模块——只有持 `law_proposal` 的官职（默认宰相）能推中央机构法；一个角色可能同时挂多个槽位，必须看完全部槽位再判否。表中不存在的旧槽位保持“任一官职”旧行为。
- **审案权**：`JudicialSystem.TrialCrime` 增加 `hasJudge`；政权没有持 `judiciary` 的在职官员时案件 `Dismissed`（法庭不成立），不是判无罪。GameWorld 调用点传入模块查询结果。
- **征税没有做硬门禁**：世界生成只给每个政权君主+配偶，若把“无 taxation 官员=无税收”接上，前十几年国库收入为 0，会直接摧毁现有经济与长跑。现有 `TaxSystem` 的税率/税基链保持不变；`taxation` 模块先作为“征收者身份/任免资格”的内容声明，等宫廷人物供给（P3.3/P3.6）补上再收紧。这是有意保留的欠账，不是遗漏。
- **任免/罢免**：`dismissalResistance` 只入表，尚未接动作层；`personnel` 模块待与 `ActionIds.AppointTitle` 一起接。
- 测试适配：`LawDecreePipelineTests` 的“中央机构 holder 可提案”改用槽位 2（宰相）；新增“区长官无 law_proposal 不能提案”“无审判官时驳回案件”两例。
- 验证：`EXIT=0`、`908/910`（2 个 Explicit 跳过）、零 `error CS`。

## D119 生态演替与野生群落（P3.2 第一批）

- 参考 Earth Simulator 的 `ecology.js`/`fauna.js`：植被向 NPP 潜力趋近、破坏快于恢复；野生群落跟随植被与人类压力。只借模型，不搬运代码/数据。
- 聚合口径：`TileData.vegetation`（0–1）与 `TileData.wildlife`（0–1，草食动物密度），**不存逐格物种清单**——物种层需要独立表与更大预算，暂不做，避免半套系统。
- `EcosystemSystem`：每月 O(N)、无随机。目标植被 = NPP × (1 − 0.75 × 人口压力)；压力 = 人口 / (200 + 800×植被)。植被恢复 0.02/月、退化 0.065/月；野生群落向 `植被×0.8×(1−0.9×压力)` 响应 0.03/月。速率与 Earth Simulator 的 GROW/DECAY 同量级，是模型常量而非平衡加成。
- 旧档兼容：`vegetation/wildlife` 为 0 时按 `npp` 惰性补齐（植被=NPP，野生=0.4×植被）；世界生成时直接写入初值。
- 存档：`TileData` 随 `SaveData.tiles` 整体序列化，两个新字段自动往返；`npp` 同样随地块落盘。
- 已知未做：狩猎/渔业尚未接入食物链（`wildlife` 不产肉）、物种与灭绝、火灾/放牧反馈；这些需要与 P3.0 食物篮子一起做，避免一次跨太大。
- 测试：`EcosystemTests` 6 例（旧档补齐、向 NPP 恢复、人口压力压低植被与野生、海陆边界、确定性、存档往返）。
- 验证：`EXIT=0`、`914/916`（2 个 Explicit 跳过）、零 `error CS`。

## D120 食物篮子与狩猎（P3.2 第二批）

- `GoodsDef.edible/nutrition`：口粮资格进内容表（`Goods.json` 中 0 小麦、1 肉类、2 鱼类、10 稻谷标 `edible`），盐/矿物即使归在 Food 类也不充饥。旧内容/测试夹具缺字段时 `nutrition` 按 1 处理，`edible` 缺省 false。
- `ProcessConsumption` 改为按货号升序吃“可食库存”，不再只看 0 号。库存不足时先吃光全部可食库存再进饥荒分支（沿用原语义，避免卡在 0<stock<need）。
- 狩猎：`EcosystemSystem.Harvest` 按人口取肉（人均日粮的 10% 为上限），产出与野生密度扣减同源；`EconomyManager.ProduceResources` 把肉写入 1 号货，和农业共用库存/仓容/保质期链。
- `EcosystemSystem` 的 `TileData` 参数改为 `ref`（`TileData` 是 struct，按值传会把演替写丢）；`Tick` 使用 `ref` 局部变量原地更新。
- 种间/物种层、渔业、火灾与放牧反馈仍未做：需要独立物种表与更大预算，暂不半途插入。
- 测试：新增 `Salt_IsNotEdible`、`CityWithOnlyMeat_DoesNotStarve`、`WildlifeHarvest_ProducesMeatAndReducesHerd`、`Harvest_TakesMeatAndReducesWildlife`、`Harvest_WithoutPeopleOrWildlifeIsZero`。
- 验证：`EXIT=0`、`919/921`（2 个 Explicit 跳过）、零 `error CS`。

## D121 建造令的 AI 调用者（P3.3 第一批）

- 现状：`ConstructionPlanSystem.CreateConstructionPlan` 只有 API 与存档，没有任何生产调用者；P3.3 首先补调用者。
- `AIController.TryPlanConstruction`：每 180 日（按 realmId 错峰）根据“是否在战 / 财政余量”选建筑（战→石墙 401，财政 <800→农田 100，否则集市 500），在开发度最高、该建筑类别等级最低的己方地块上调用统一 `CreateConstructionPlan`。不新建建造系统、不直接写 `buildingLevels`。
- 计划自带 treasury/construction_days 需求，财政门禁由 `CanBuildBuilding` 统一判定；国库不足时不会立项。
- 战斗/贸易/外交/迁徙仍按 D32 不走 Plan；`PlanType.Military` 的 §9.4 设想与 D32 冲突，以 D32 为准（战争归 Warfare/ManpowerSystem）。
- 顺带修两个潜在崩溃：`GetAvailableInnovations` 列表可能含 null（排序解引用），以及 `AIPersonality.preferredDomains` 未初始化时比较器 NRE；两者都是内容不完整/测试环境的真实路径。
- 验证：`EXIT=0`、`926/928`（2 个 Explicit 跳过）、零 `error CS`；新增 `ConstructionPlanAITests` 2 例。

## D122 S19 地图知识层（P3.5 第一刀）

- `MapKnowledge`：每政权一张位图（`byte[]`），只存“知道/不知道”，不存传闻强度；读取不写状态。
- 写入口三个：`SeedOwnership`（自己拥有的地块+一圈邻地，生成时播种）、`RevealRadius`（军队按 `scoutRange` 每月揭示，切比雪夫方形网格）、`Restore`（读档）。后续商队/探险只加写入口，不另开知识系统。
- 存档：`SaveData.realmKnowledge` 为 `RealmKnowledgeSaveData { realmId, bitsBase64 }`；坏 Base64 段只让该政权退回全未知，不让整档读失败。
- UI/密谋读取：`GameWorld.Knowledge`；P3.5 的 Intrigue 必须在“目标地块已知”条件下才允许立项（下一步接入）。
- 性能：MarkKnown O(1)，RevealRadius O(r²)，SeadOwnership O(N) 只在生成/月度跑；存储 = 每政权 ceil(N/8) 字节。
- 验证：`EXIT=0`、`926/928`（2 个 Explicit 跳过）、零 `error CS`；新增 `MapKnowledgeTests` 5 例。

## D123 政权旗帜与城市俯视布局（P3.4）

- 旗帜四态（`BannerKind`）：`None / MilitaryBanner / HouseholdBanner / StateFlag`，都是合法状态，不强制每个政权都有旗。
- 条件（中立表述，不绑定现实文明）：
  - 无君主/执政官 → `None`；
  - 已获得民族主义革新（内容 1017）且定居政体 → `StateFlag`；
  - 统治家族已有纹章（`CultureData.heraldryEnabled` 才会生成）→ `HouseholdBanner`；
  - 其余有君主的政体 → `MilitaryBanner`（早期默认，符合“先有军旗”）。
- 生成：`BannerSystem.Generate` 由 `realmId + 名称 + 类型` 确定性产出 `类型|布局` 与三色（field/charge/accent），遵循金属不压金属；不生成美术资源。类型变化才重算，否则保留。
- 存档：`RealmOfficeSaveData` 增 `bannerKind/bannerPattern/bannerColors`；旧档缺省 0/空 = 无旗。
- 城市俯视图：`CityLayoutGenerator` 只产布局数据（`CityLayoutPlan`：100×100 画布、按 `areaRatio` 的竖条街区、道路间隙、城墙外环），按 `blockId` 稳定排序，确定性、不存档、读档重算；渲染层后续把矩形光栅化成 Sprite。空聚落返回空布局，不抛异常。
- 未做：旗帜/城市布局的实际 Sprite 渲染与 UI 入口；`StateFlag` 的政体条件目前只有“定居”，共和/宪章政体细分等政体系统补齐后再收紧。
- 验证：`EXIT=0`、`937/939`（2 个 Explicit 跳过）、零 `error CS`（含 `BannerSystemTests` 6 例、`CityLayoutTests` 5 例）。

## D124 密谋计划复活（P3.5 第二刀）

- D98 的触发条件（S19 有写入/查询入口并入档）已满足；`PlanType.Intrigue` 重新启用，不是新建第二套情报系统。
- `IntriguePlanSystem`：立项要求目标人物在世、目标地块在发起政权的地图知识内；否则返回 null（信息不足不能凭空阴谋）。计划带 initiator 参与者，预计 30 日。
- 执行：目标死亡 → `target_dead` 失败；目标离开已知区域 → `target_lost` 失败；目标地块有军队 → `guarded` 失败；否则完成并写编年史。进度判定放在执行器内、先于 PlanSystem 的“progress≥1 自动完成”，避免有守卫也被自动判成功。
- 不引入“阴谋成功率”随机数或能力值加成：结果只由可观察条件（目标存活/知识/守卫）决定。
- AI 调用者：每 365 日（按 realmId 错峰）对“目标地块已知”的他国君主发起一次；没有已知目标就不行动。
- 未做：消息伪造/延迟传播、秘密与把柄、被察觉后的反向密谋；这些需要独立信息传播层，暂不硬造。
- 测试：`IntriguePlanTests` 4 例（未知目标不立项、目标死亡失败、无守卫完成、有守卫失败）。
- 验证：`EXIT=0`、`941/943`（2 个 Explicit 跳过）、零 `error CS`。

## D125 P3.6 内容/本地化/架构尾巴（本轮）

- 本地化：官职名新增 `office_Judge`、`office_CourtScholar`（en / zh-Hans / zh-Hant）；其他语言没有官职块时走 `Localization.Get(key, fallback)` 回退内容名，不硬填未翻译文案。
- 内容：官职表 `Polity/Offices.json`、群系资源 `Biome/BiomeResources.json`、商品口粮资格 `Goods.json` 已走内容层；徽章/旗帜条件当前在代码中（`BannerSystem`），因为它们是规则而不是可枚举内容，等政体/革新 schema 稳定后再外提为表。
- 存档：新增字段全部可选（`officeAppointedDays`、`bannerKind/bannerPattern/bannerColors`、`realmKnowledge`），旧档缺省值有明确语义（终身、无旗、全未知），不需要新 SaveVersion。
- 架构尾巴明确不做（有理由）：
  - `RealmData` 字段全面私有化：当前大量 Host/UI 只读访问，收益主要是封装，风险是跨 20+ 文件的机械改动；留给后续专门批次。
  - `DnaData` 删除：仍有旧档迁移输入，保留为兼容入口。
  - 法律 Host 再拆：P2.5 已按 Hosts 程序集反转，继续拆没有新消费者。
- `Exploration`（P3.3 剩余）：S19 已提供知识层，但还缺“远征实体/路线”这一过程载体；现在立项只能空转，继续按 D32/D99 暂不启用，等航海/远征系统。
- 验证：`EXIT=0`、`942/944`（2 个 Explicit 跳过）、零 `error CS`（本地化改动后复跑）。

## D126 基因组/物种内容化（D1）

- 纯定义（`GenomeDef/GenomeLocusDef/AlleleDef/AlleleTraitEffect/AppearanceStyleDef/AppearanceChannel/PigmentDef/SpeciesDef/SpeciesCategory/ReproductiveIsolation/SexChromosomeSystemDef/SexDeterminationSystem/GeneticInheritance/GeneticsContentPack`）从 `Simulation.Genetics` 迁入 `Content.Definitions/Genetics`：定义属内容层，引擎/目录留在 Simulation，符合既有程序集边界。
- `ContentDefinitionIndex` 新增 `Genome`/`Species` 两个 provider（`Base/Genetics/Genomes.json`、`Species.json`），Base 打底、Mods 按 id 覆盖；物种引用不存在基因组（且非内置 `genome_human`）时剔除并告警，不拖垮整包。
- `ContentRegistry` 只做桥接：把 `_definitions.Genomes/Species` 注册进 `GeneticsCatalog`，不另存一份定义。
- 修复 provider store 未接属性字典的 bug：`Offices/Genomes/Species` 之前写入临时字典，导致 `OfficeCatalog` 实际一直走内置兜底；现在内容表真正生效。
- `ContentPackageLayout` 支持 `Genetics` 目录；`DelegateContentProvider` 把 `Genetics/Genomes.json` 视为内容根标记（否则只有 Genetics 的 mod 会被当成“多包目录”拆错）。
- 复杂度：内容只在初始化读一次，运行期查字典 O(1)；无 tick 开销。
- 测试：`GeneticsContentIndexTests` 3 例（加载、Mod 覆盖、坏引用剔除）；既有 Offices/GOODS 内容测试保持全绿。
- 验证：`EXIT=0`、`945/947`（2 个 Explicit 跳过）、零 `error CS`。

## D127 身体构造/体表/代谢位点与表型（D2）

- `AlleleTraitEffect` 增加 `discreteTraitId/discreteValue`：离散性状由等位基因表达，不靠随机标签；加性通道（traitId/additiveEffect）继续用于隔热、护甲、体型、食性、耐温。
- 新增结构化表型 `BodyPlanPhenotype`（wingType/hornType/tailType/posture）、`IntegumentPhenotype`（skinType/insulation/naturalArmor）、`MetabolicPhenotype`（dietHerbivore/dietCarnivore/tempToleranceC/size），由 `PhenotypeResult` 暴露；`GetDiscrete` 供机制查询。
- `GeneticsEngine.ComputePhenotype`：离散性状按显性规则取等位基因值，缺位点回退中性默认（none/skin/biped、体型 1、食性 0.5、耐温 0）；加性位点折算为可解释生理量（体型 = 1 + 两份加性，耐温 = 两份加性 °C）。
- 人类默认基因组扩到 16 个位点：新增 body_plan（翼/角/尾/姿态）、integument（皮/隔热/护甲）、metabolism（体型/食性/耐温），人类等位基因都是中性值，不改变既有角色表现。
- 旧 `GenomeInstance` 数组短于基因组时，缺位点按 -1 处理并回退默认；D6 再做一次性补齐迁移与存档版本。
- 复杂度：表型计算 O(位点数)，纯函数、无随机；运行期只读。
- 测试：`BodyPhenotypeTests` 5 例（显性/隐性、加性生理量、缺位点回退、创始人与繁殖覆盖新位点）；旧迁移测试改为按默认基因组位点数断言，不再写死 6。
- 验证：`EXIT=0`、`950/952`（2 个 Explicit 跳过）、零 `error CS`。

## D128 表型→机制接线（D3）

- **角/爪/体型/天然护甲 → 个人战力**：`PhenotypeMechanics.ProwessBonus` 在 `CharacterManager.CreateCharacter` 中加到最后一次 `prowess` 推导之后；只读身体构造，不新建通用加成层。中性表型返回 0。
- **隔热/耐温 → 寒热健康损耗**：`CharacterData.DailyTick(day, year, ambientTempC)` 增加表型驱动的健康损耗；`CharacterManager.DailyTick` 接收 `ambientAtTile`，`CharacterSchedule` 由 GameWorld 注入 `tiles[tileIndex].annualTemp`。舒适区：`[-10 - 耐温 - 隔热×10, 30 + 耐温 - 隔热×5]`，损耗上限 0.5/日，避免极端气候一天致死。旧调用（不传温度）默认 20°C，行为不变。
- **翼型 → 斥候范围**：`DetectionContactService` 增加 `CommanderResolver`（GameWorld 注入人物查询），具翼指挥官的军队 `scoutRange` +2（膜翼）/+1（羽翼）；无翼 0。接入既有 `ComputeScoutRange`，不新建视野系统。
- **食性 → 口粮结构暂缓**：人口块没有物种字段（只有阶层），若按“统治种族食性”硬接会把全体人口当成一个物种。等人口块带物种归属后再做，先记欠账；本轮三条机制已满足验收。
- 测试：`PhenotypeMechanicsTests` 7 例（中性无加成、角爪体型加战力、隔热寒暑双向、翼型斥候、具翼指挥官实际扩大范围、酷暑实际掉血）。
- 验证：`EXIT=0`、`956/958`（2 个 Explicit 跳过）、零 `error CS`。

## D129 物种目录与纯内容驱动的瓦伊凡（D4/D5）

- `GeneticsCatalog.FindSpeciesForGenome`：由基因组反查物种（内容覆盖优先，`genome_human` 回退内置人类）；物种目录继续用 `GeneticsCatalog` 的注册表，不新建第二套。
- 生殖隔离沿用 `GeneticCompatibility.Evaluate`：同种可育 → 先判定；任一方 `reproductiveIsolation != None` 则跨种不可育；否则按父/母渗入率取较小值。内容表里的 `reproductiveIsolation` 是唯一开关。
- 新增真实内容：
  - `Base/Genetics/Genomes.json`：`genome_wyvern`（膜翼/角质角/长尾/四足/鳞皮/隔热/护甲/体型/肉食/耐温）与 `genome_drake`（无翼、角/鳞/长尾的近缘地龙），全部 10 个位点、固定等位基因频率。
  - `Base/Genetics/Species.json`：人类/瓦伊凡/地龙；瓦伊凡与地龙声明生殖隔离，跨种不可育。
- 代码没有任何 `wyvern`/`drake` 特判：翼、角、皮、尾、食性全部由基因组位点推导；`SpeciesWyvernTests` 用地龙反证“同属龙类但无翼位点优势等位基因就不会飞”。
- 复杂度：物种查询 O(物种数)（内容规模小），表型 O(位点)；无 tick 开销。
- 测试：`SpeciesWyvernTests` 5 例（内容加载、瓦伊凡表型、地龙反证、隔离矩阵、按基因组反查物种）。
- 验证：`EXIT=0`、`961/963`（2 个 Explicit 跳过）、零 `error CS`。

## D130 旧 GenomeInstance 结构迁移（D6）

- `GenomeInstance` 增 `genomeVersion`（旧档缺省 0）；`GenomeMigration.CurrentVersion = 2`。
- `GenomeMigration.Upgrade`：读档/取基因组时就地补齐到当前基因组位点数；旧位点原样保留，新位点优先取**中性等位基因**（加性 0，离散 none/skin/biped/short），没有中性项才取 `defaultFrequency` 最高者；并列取索引最小，保证确定性。
- 接线：`CharacterSaveData.ToCharacter()` 在还原后调用一次；`CharacterData.GetGenome` 对运行时旧实例也调用，避免绕过存档路径的旧数据漏迁移。
- 复杂度 O(位点数)，只在读档/首次取基因组时发生；已补齐实例直接返回，不重复分配。
- 测试：`GenomeMigrationTests` 6 例（补齐长度、旧位点保留、新位点中性、幂等、JSON 往返、迁移后可繁殖、GetGenome 路径）。
- 验证：`EXIT=0`、`967/969`（2 个 Explicit 跳过）、零 `error CS`。

## D131 DNA/物种目标收口（D7）

- 表型缓存：`CharacterData.GetCachedPhenotype` 在基因组不变时只推导一次，`DailyTick` 的寒热结算与机制查询共用；`SetPhenotypeCache` 在创建角色时复用已算表型，`GetGenome` 迁移/替换基因组时清空缓存。避免百年长跑里“每人每天重算 16 位点”的分配与 CPU 浪费。
- 最终验证：`build` 971 例、969 通过、0 失败、零 `error CS`（2 例 Explicit 跳过）；`longrun` 2 seed×100 年 `1/1`、`EXIT=0`。
- 交付范围对照 `DNA_SPECIES_PLAN.md §4`：
  1. 基因组/物种内容表可加载、Mod 覆盖、坏引用剔除 — D126/D129；
  2. 新位点可遗传/突变/推导表型 — D127 + 突变测试；
  3. 三条机制接线 — D128（战力/寒热/斥候）；
  4. 瓦伊凡纯 JSON、无物种特判 — D129；
  5. 旧档迁移、可繁殖、存档往返 — D130；
  6. 文档与最终验证 — 本条。
- 未做（明确欠账）：食性→口粮结构（人口块无物种字段）；逐格物种丰度/食物网；3D/DNA 直接建模。

## D132 宫廷人物供给与征税硬门禁（P4.0）

- 世界生成末段为每个政权创建 3 名宫廷人物（Noble/Scholar/Military，成年、同文化、非君主家族），由 `CreateInitialCourtiers` 负责。**必须在所有依赖全局 Random 的生成步骤之后调用**：`CreateCharacter` 会消耗全局随机数，早调用会改变地图/聚落/初始单位结果（本轮首次实现踩到，已修正）。
- 生成结束调用 `EnsureOfficeHolders`，开局官职即可被担任；`offices` 调度从月度改回每日（优先级 12，早于政治 15），已满时只遍历槽位，空槽才扫候选。
- 恢复 `taxation` 硬门禁：`PoliticsSimulationSystem` 查 `OfficeSystem.HasModuleHolder(..., taxation)`；无征税官时 `EconomyManager.SettleTaxes(..., hasCollector:false)` 返回 0，不是税率归零。
- 测试适配：战争/议事测试改用 `WarDecisionMode.SkipConsultation` 隔离议事变量；`CouncilConsultationTests` 在构造议事会前清空自动填充的官职，避免人数漂移。
- 验证：`EXIT=0`、`973/975`（2 个 Explicit 跳过）、零 `error CS`；`longrun` 2 seed×100 年 `1/1`、`EXIT=0`（税收硬门禁未摧毁经济）。

## D133 远征实体与探索计划（P4.1）

- 新增 `Expedition`（Simulation.State）：owner/leader/home/current/route/cursor/supplies/state/failReason；路线在立项时用八向 BFS 一次算好，运行期只推进 cursor，不每日重算寻路。
- `ExpeditionSystem`：每日扣 0.2 补给、移动一格、按 `RevealRadius=1` 揭示地图知识；补给耗尽 → `supplies` 失败；同格有他国军队 → `intercepted` 失败；抵达终点 → Completed；失败/完成都写编年史。
- `ExplorationPlanSystem`：立项要求领队在世、目标为陆地、BFS 有路；创建远征并把 `plan.targetId` 指向远征 id；进度由 `routeCursor/route.Count` 驱动，远征失败/完成映射为计划 Fail/Complete。
- 调度：`expeditions.daily`（优先级 29，早于计划 30）；`GameWorld.expeditions` 字典 + 自增 id。
- 存档：`ExpeditionSaveData`/`ExpeditionPersistence`，`SaveData.expeditions`；旧档为 null 时保持空表；读档恢复 id 计数。
- 复杂度：路线 BFS O(N) 仅在立项时；每日 O(远征数×1)；知识揭示 O(r²)。
- 测试：`ExpeditionTests` 4 例（立项+完成+知识揭示、补给失败、拦截失败、DTO 往返）。
- 验证：`EXIT=0`、`977/979`（2 个 Explicit 跳过）、零 `error CS`。

## D134 城市俯视 Sprite 与独立面板（P4.2）

- 布局类型从 `World.Settlement` 迁到 `Core.Data`，输入改为 `CityBlockSpec`（blockId/areaRatio/districtType/isWall/enabled）：World 不再被 Rendering/UI 反向依赖；`SettlementBlock` 映射留在 `SettlementQueryAdapter`。
- `ISettlementQuery.GetSettlementBlocks(anchorId)` 返回纯 DTO `SettlementBlockRowDto`；GameWorld/ReadModel/Adapter 三层透传，Fake 与测试同步。
- `CityLayoutRenderer`：布局→RGBA32 纹理（街区按区划上色、道路间隙为背景、城墙外环 2px）；`RenderCached` 按 key 做 LRU（默认 8），同布局同图；纹理按需生成、不参与主 tick。
- UI：新增 `CityViewPanel`（一个 UI 一个文件），实现 `IUserInterfacePanel`，由 `UIManager.RegisterRuntimePanels` 创建并注册到 `UiPanelRegistry`；`SettlementDetailPanel` 底部新增“城市视图”按钮，调用 `UIManager.OpenCityView`；无区块/无聚落显示合法空态。
- 复杂度：布局 O(B log B)，纹理 O(W×H)，缓存 O(1)；缓存淘汰时销毁旧纹理。
- 测试：`CityViewTests` 4 例（纹理确定性、缓存命中/淘汰、面板注册+空态、有区块标题/渲染）。
- 验证：`EXIT=0`、`981/983`（2 个 Explicit 跳过）、零 `error CS`。

## D135 渔业、火灾、放牧反馈（P4.3）

- `TileData.fishStock`（沿海 0–1）：生成时沿海给 0.8；`EcosystemSystem` 月度向 `0.8×(1−0.5×人口压力)` 恢复；`HarvestFish` 按人口需求捕捞，产量与鱼群扣减同源；`EconomyManager.ProduceResources` 写入 2 号货（鱼），与狩猎/农业共用库存链。
- `TileData.livestock`（0–1）：月度向 `min(1, 人口/800)` 趋近；放牧压力按 `MaxGrazingShare=0.3` 压低目标植被；无新阶层/新系统。
- 火灾：`ShouldBurn` 只对“陆地、植被≥0.7、年降水<500mm、年均温≥20°C”的地块按 `tileIndex×year` 确定性 hash 判定；`Burn` 烧掉植被/野生/牲畜；`EcosystemSystem.Tick` 在演替后处理，随后按非火灾年恢复。阈值是模型常量，不是平衡加成。
- 复杂度：捕鱼/火灾都在既有月度 O(N) 循环内；每个地块 O(1)。
- 旧档：新字段缺省 0，沿海旧档鱼群为 0（需一次生成/事件补种；不影响存档读取）。
- 测试：`EcologyExtensionTests` 5 例（沿海捕鱼、鱼群恢复、放牧压低植被、火灾确定性/恢复、字段往返）。
- 验证：`EXIT=0`、`986/988`（2 个 Explicit 跳过）、零 `error CS`。

## D136 生态物种表与物种池（P4.4）

- 新增 `Content.Definitions.EcologySpeciesDef`/`EcologySpeciesCatalog`：生态物种只描述承载/捕猎有意义的性状（role/habitat/bodySize/fecundity/diet），与遗传 `SpeciesDef` 通过 `speciesId` 对齐，不复制遗传定义。
- `Base/Ecology/Species.json`：森林（鹿/狼）、草原（羚羊/野牛）、荒漠（骆驼）、湿地（野猪）、苔原（驯鹿）、海岸（鱼群）、通用（野兔）；`ContentDefinitionIndex` 新增 `EcologySpecies` provider，Base 打底、Mods 按 id 覆盖。
- `EcosystemSystem`：`HabitatOf` 把群系归为 forest/grass/arid/wet/tundra/coast/generic；野生承载 × `CarryingFactor(habitat, herbivore)`（含体型/繁殖/食性），捕猎产出 × `HuntYieldFactor`（最大体型）；鱼群承载/捕捞 × `CarryingFactor("coast","fish")`。
- 不存逐格物种数组：物种池按栖息地查表，承载/捕猎是池内性状的纯函数；空表返回 1，保持旧行为。
- 复杂度：内容只在初始化读一次；运行期每组地块 O(池大小)（池很小），无 tick 额外分配。
- 测试：`EcologySpeciesTests` 4 例（内容加载/栖息地分类、承载与捕猎差异、Tick 下森林>荒漠、Mods 覆盖）。
- 验证：`EXIT=0`、`990/992`（2 个 Explicit 跳过）、零 `error CS`。

## D137 P4 欠账收口验证

- `build`：994 例、992 通过、0 失败、零 `error CS`（2 个 Explicit 跳过）。
- `longrun`：2 seed × 100 年 `1/1`、`EXIT=0`。
- 对照 `P4_DEFERRED_PLAN.md §4`：
  1. 宫廷/官职/征税：开局 3 名宫廷人物、生成末段填官职、`taxation` 硬门禁、移除后 0 税收与补缺恢复（D132 + `CourtAndTaxGateTests`）。
  2. 远征：BFS 路线、知识揭示、补给/拦截失败、计划完成、DTO 与 Persistence 往返（D133 + `ExpeditionTests`）。
  3. 城市视图：Core 布局、确定性纹理、LRU、`CityViewPanel` 注册与空态、聚落面板入口（D134 + `CityViewTests`）。
  4. 渔业/火灾/放牧：`fishStock` 产 2 号货、火灾确定性/恢复、放牧压植被（D135 + `EcologyExtensionTests`）。
  5. 生态物种表：Base/Mods 加载、承载/捕猎受体型/繁殖/食性影响（D136 + `EcologySpeciesTests`）。
  6. 最终验证与文档同步：本条。
- 明确未做（不在 P4 范围）：逐格物种个体/完整食物网、3D 城市模型、远征 UI 入口、旧档沿海鱼群补种。

## D138 默认世界自然基线（P5.0–P5.1）

- **撤回 P4.0 的“开局政权 + 3 名宫廷”前提**：世界生成不再预置政权、君主、配偶、宫廷、官职、国教、高祭司、军队或玩家政权。`P4.0` 的“无征税官则无税”方向保留，但“谁有征收权”改由行政区划与仓储机制决定。
- `WorldSetupOptions.CreateDefault()` 改为自然基线：`generatePolities=false`、`initialPolityCount=0`、`generateSettlements=true`、`initialSettlementCount=-1`、`generateInitialUnits=false`。聚落是人群，不是国家。
- `CreateStandard()` 明确为**沙盒预置**：3 个政权、君主+配偶、国教、高祭司=君主、一支军队、`PlayerRealmId=0`；仅供测试/调试，不是默认新游戏。
- 新增 `StateFormationSystem`（月 tick，优先级 37，规则判定）+ `StateFormationHost`（写入）：无主聚落存在 ≥8 年、人口 ≥200、开发 ≥6、地区仓储 ≥人口 90 天口粮时，自然形成一个政权；每月最多一个，按 `anchorId` 稳定排序。
- 建国时：领土=聚落地块+最多 2 块同省无主邻地；首领由当地人口产生（`CharacterManager.CreateFoundingLeader`，含家族/配偶）；地方显贵按人口比例产生（1 人/约 120 人，封顶 5，`SeedFoundingNotables`）；`treasury=0`、`stateReligionId=-1`、`stability` 取聚落稳定度、`centralization` 由文化阶段推导。**删除 `ApplyPathBonuses` 的 stability=55/treasury=2000 等硬加成**。
- 关键前置 bug：`AnchorGenerator.CreatePair` 未把 `SettlementData.anchor` 指向刚创建的 `AnchorData`，导致生成期 `settlement.tileIndex == -1`，初始人口被写进 -1 号地块；城市人口快照为 0，180 天后被当成“人口流失”废弃。现已在 `CreatePair` 中回填 `anchor`，并在 `SeedPopulation` 用聚落开发度把地块开发度抬到 0–1，避免城市按未开发土地产粮。
- 测试：`NaturalEmergenceTests`（默认世界 0 政权/0 角色/有聚落；够格建国；无剩余/太年轻不建国；官职门槛；配偶/平民不得任官）。验证：`EXIT=0`、`1005/1001`、零 `error CS`。

## D139 官职随制度与人口出现（P5.2）

- 删除世界生成与旧档兜底的 `CreateInitialCourtiers` 调用；`CreateInitialCourtiers` 降为**测试/沙盒显式播种**工具，生产路径不得调用。
- `OfficeDef` 新增内容门槛：`minTiles/minPopulation/minCentralization/minCultureStage`（0=不限，旧内容/Mods 兼容）；`Offices.json` 为 8 个官职补齐门槛（地方长官 2 地/150 人，宰相 6 地/600 人/中枢 0.25/文化阶段 2 等）。
- `OfficeSystem.FillVacancies` 增加 `OfficeRealmContext`（实际控制地块、人口、中枢、文化阶段）；`GameWorld.EnsureOfficeHolders` 只在存在空槽时做一次 O(N) 地块扫描构造 context，满编时不扫全图。
- 候选资格排除 `Ruler/Spouse/Heir/Commoner/Courier`；配偶不再因“有一个活人”自动成为宰相。
- `InitializeDefaultRealms` 生成后只建立行政区划，不填官职；官职由每日 `offices.daily` 在政权出现后按门槛补缺。
- 测试：`CourtAndTaxGateTests`（生成后 0 宫廷/0 官职；显式播种后才补缺）、`NaturalEmergenceTests.OfficeThresholds_RequireTerritoryAndPopulation`、`SpouseAndCommoner_AreNotOfficeCandidates`。

## D140 机制化税收（P5.3）

- 删除 `EconomyManager.SettleTaxes` 与 `TaxSystem.CalculateTileTax` 的 `fertility×development×10×人口×秩序×税率` 抽象税基；不再直接往 `realm.treasury` 加“算出来的税额”。
- 新增 `TaxCollectionSystem`：
  1. **征收资格**：地区属于某行政区划，且该区划有在世持有人（首都级区划允许君主直领，但只限首都所在地区）；没有行政区划/持有人就没有征收。
  2. **征实物**：只从地区 `TradeCenter.inventory` 取真实货物；按 `TaxSystem` 类别税率（农业/畜牧/矿业/工艺/奢侈/盐专营）；先扣人口 90 天口粮 `reserveDays`，只征剩余；食用货与非食用货分别处理。
  3. **运输损耗**：按地区中心到首都的曼哈顿距离 × `transportLossPerTile`（上限 50%），实际入库=征收量×(1−损耗)。
  4. **货币化**：只有 `CurrencyStage >= MintedCoin` 且当地市场有真实需求时，才把**非食用**货物按 `TradeCenter.GetGoodsPrice` 卖出，销售量受 `localDemand` 限制；所得计入 `RealmTreasury` 并记 `tax` 流水。物物交换阶段不产生货币税。
  5. **实物流水**：`TaxReceiptLedger` 记录政权/日期/地区/货物/入库量，供 UI 与长跑断言。
- `PoliticsSimulationSystem` 每日调用 `EconomyManager.CollectTaxes`，法律/法令的征收强度乘子在该结算点消费；不再直接 `RealmTreasury.Add` 抽象税额。
- 沙盒预置政权从 `MintedCoin` 开始（便于测试/调试货币税）；自然政权仍从 `Barter` 开始。
- 测试：`TaxSingleSourceTests`（税率单源、高税率多征、无行政区划不征、90 天口粮保护、物物交换无货币/铸币+市场才有货币）、`CourtAndTaxGateTests`（实物从地方仓储进国家仓、移除持有人停征/恢复）、`BudgetReportTests`（真实售货产生 tax 流水）、`LawEffectRuntimeTests`（征收强度乘子按实物量缩放）。
- 验证：`EXIT=0`、`1005/1001`、零 `error CS`。

## D141 P5 收口验证（2026-11-14）

- **全量 EditMode**：`EXIT=0`、`1004` 例、`1001` 通过、`0` 失败、零 `error CS`（3 个 Explicit 按需跳过）。
- **自然世界 100 年长跑**：`NaturalEmergenceTests.NaturalLongRun_FormsRealmOfficesAndTaxReceipts`（1 seed × 100 年，48×24，`StateFormationSystem.MaxRealms=8`）通过，耗时 187.8s；长跑过程中出现过政权、官职与 `TaxReceiptLedger` 实物税流水。
- **施工中修复的前置 bug**：
  1. `AnchorGenerator.CreatePair` 未回填 `SettlementData.anchor`，初始人口写进 -1 号地块，城市人口快照为 0、180 天后被废弃；
  2. 聚落地块开发度为 0，城市按未开发土地产粮，产出低于口粮；
  3. `currentDay` 每年回绕，建国年龄与 `foundingTick` 必须用 `currentYear*365+currentDay`；
  4. Unity/Mono 的 `Dictionary` 在改写已有键时也递增 version，边枚举边 `RemoveGoods`/同键赋值会抛 "Collection was modified"；
  5. 税收按政权全图扫描是 O(政权×地块×天)，改为只扫行政区划所在地区的 16 格。
- **自然政权上限**：小地图默认最多 8 个自然政权（`MaxRealms=8`），是实体规模/性能上限，不是历史规律；大地图可另行配置。
- **仍留欠账**：营地补给无来源、`SandboxSpawnSystem` 随机地块造人、稳定度回归/种族常数/聚落财富只减不增、铸币发行与在途运输队列。

## D142 聚落生命周期闭合（P6）

- **营地补给有来源**：`CampData.Tick` 不再只消耗；每日从所在地块获得农业劳动产出（`AgricultureYield.ComputeForLabor`）+ 狩猎（`EcosystemSystem.HarvestForPeople`）+ 捕鱼（`HarvestFishForPeople`），产出从地块植被/野生/鱼群真实扣减。补给消耗统一为 `0.01/人/日`，与人口口粮同单位（原 0.1 是 10 倍口径错误）。营地能在有产出的地块活过 5 年并把 `permanence` 推到 100，进而可演化为聚落；无产出地块仍会废弃。
- **人口迁移替代随机造人**：`SandboxSpawnSystem.TrySeedPopulation` 改为从人口 > 承载力 1.1 倍的源地块，向邻近可承载地块移出约 10% 人口；源地块按 `PopulationLedger.RemoveFromIndex` 扣减、目标地块按 `PopulationLedger.Add` 增加，总人口守恒；没有过载源/合适目标就不新增人口。原“随机地块凭空加 4–16 人”删除。
- **聚落财富来源**：商队到货时按实际入库货物的 `baseValue` 计成交价值，其中 5% 留在目的地聚落 `wealth`（仓储/卸货/交易服务）；没有聚落/没有成交就不加。
- **聚落开发来源**：`SettlementSimulationSystem` 每日按人口规模产生的建设劳动推进 `development`（`gain * deltaDays * 0.01`），不再只减不增。
- 测试：`SettlementLifecycleTests` 4 例（营地正反向、迁移守恒、无过载源不造人）。验证：`EXIT=0`、`1006/1003`、零 `error CS`（3 个 Explicit 跳过）。
- 仍留欠账：完整市场价格/货币流通、迁徙路线寻路、营地仓储上限、聚落财富的消费端。
- **长跑回归**：P6 改动后 100 年自然长跑通过（189.0s），P5 自然建国/官职/实物税流水仍成立。

## D143 P7.0 时间尺度与 200 年建国周期（2026-11-14）

- **五档倍速单源**：新增 `GameSpeedScale`（App.Contracts）`{1,2,5,10,30}`、暂停 0、上限 30，`Clamp/IndexOf/Next`；`GameManager.SetGameSpeed` 与 TopBar 循环按钮共用，旧 1x/2x/3x 按钮运行时隐藏。测试 `GameSpeedScaleTests`。
- **建国门槛**：聚落年龄 ≥200 年、人口 ≥800、开发 ≥15、地区仓储 ≥180 天口粮；`MaxRealms=8`。`NaturalHundredYears_NoRealmBeforeStateCycle` 与 `NaturalLongRun_FormsRealmOfficesAndTaxReceipts` 通过。
- **世界气温公式修正**：`ComputeBaseAnnualTemperature` 原按全表归一化高程 ×65℃ 罚海拔，默认世界均温 -28.7℃、半图为冰盖；改为相对海平面高程 ×45℃，破碎化补算同口径。修复后均温约 0℃、冰盖约 23%，肥力 p90 0.5。
- **生成期肥力一致性**：气候重算/破碎化后按当前群系把肥力夹到潜在值（`ClampFertilityToCurrentClimate`），并在气候模拟改群系时同步夹取；不再出现“沙漠地块肥力 0.8、潜力 0.02”。第二套气候模型（`PlanetClimateSimulator`）不再重判群系，气候模型统一列为 P7.5 欠账。
- **承载力机制化**：`2400 × 地形 × NPP × 开发 × 肥力`；农业 NPP 对河流/海岸设灌溉下限（0.65/0.45）；人口超载由“增长率归零”改为马尔萨斯净减少。
- **人口与秩序**：修复 `PopulationTick` 先缩放再写回旧块导致增长被覆盖的 bug；自然增长从 0.002/日（≈73%/年）降到 0.00003/日（≈1%/年）；稳定/秩序每月向 50 恢复；未初始化的文化渗透按中性 50。
- **食物链**：可食货列表惰性重建 + 兜底商品表显式 `MarkEdible`；饥荒与消费统一按经济系统的可食货清单与营养折算；饥荒线改为“全区不足 7 天且缺口 >50%”，死亡率随缺口（≤2%）；出口保留本地 180 天口粮；主粮储存期 1800→7300 天；**口粮独立仓容**，非食用货最多占共享仓容 60%，避免矿石/木材把粮食挤到无法入库。
- **聚落选址**：省治落在省域内“肥力×NPP×水”最高地块，额外村落从地力前 1/3 抽取；初始人口按选址得分平方给（40 + max×score²），不再把两千人塞进干旱海岸。
- **其他长跑 bug**：蝗群改为“到达新地块才结算一次”（原 -2.0/日直接清零）；营地演化聚落时 `fortifiedOutpostData` 空引用；迁移只搬“超过承载 1.1 倍的部分”，每次 ≤2%。
- 测试：`GameSpeedScaleTests`、`SoilRecoveryTests`、`SwarmDamageTests`、`AgricultureYieldTests`（旱年场景改用肥力 0.1）、`EconomyConservationTests.InitiateTrades_KeepsLocalFoodReserve`、`SettlementLifecycleTests`（过载源 1200 人）。
- 验证：100 年无政权通过（101.4s）；220 年自然长跑出现政权/官职/实物税流水通过（202.6s）；全量 EditMode：`EXIT=0`、1021 例、1013 通过、0 失败、零 `error CS`（8 个 Explicit 按需跳过）。
- 欠账（P7.1/P7.6）：革新数量与研究速度、仓储设施化（粮仓/盐仓/金库/货栈分级与升级配方）、第二套气候模型合并、30x 下的事件队列压力测试。

## D144 P8.0 历法与时间尺度（2026-11-14）

- **GameCalendar 成为日/年/季节唯一来源**：`hoursPerDay`（自转，默认 24，仅显示与昼夜推导）、`daysPerYear`（公转，默认 365）、`seasons`（默认 4，边界按年长/季数计算）、`ticksPerDay`（默认 1，世界模拟以“日”为原子步长）、`daysPerTick`（史前快进/高倍速批处理）。提供 `SeasonIndex/AbsoluteDay/YearOfAbsoluteDay/DayOfYearOfAbsoluteDay/HourOfDay`。
- **五档速度语义**：`GameSpeedScale` 的 1/2/5/10/30 表示“游戏日/现实秒”（`tickInterval=1s`、`daysPerTick=1` 时）；30x 上限不变。史前跨度不靠继续抬高 `Time.timeScale`，而靠 `daysPerTick` 批处理。
- **自转/公转是两个世界参数**：`WorldSetupOptions.calendar` 提供 `hoursPerDay`（自转）与 `daysPerYear`（公转），新局克隆到 `GameWorld.calendar`；`PlanetClimateSimulator` 读 `DiurnalRangeFactor = sqrt(24/hoursPerDay)` 调整昼夜温差（转得快温差小、转得慢温差大），公转通过季节长度/年长影响所有按年系统。完整气候耦合仍留 P8.3。
- **调度器 cadence 改为区间累计**：Entry 记录 `elapsedDays`，首次按 `(day-1) % interval` 补齐相位，之后按 `deltaDays` 累加；`daysPerTick=30/90` 时月度/季节/年度系统不会因跳日漏触发。`LongTerm` 间隔读 `SimulationScheduler.DaysPerYear`。
- **修复“时间双推进”**：`GameTick` 与 `RunDiagnosticDays` 原来先直接调用 `AdvanceTime()`，调度器里的 `simulation.time.daily` 又推进一次，长跑实际以两倍速度老化（这也是 P7 诊断里“150 年就出现政权”的来源）。现在只由调度器推进一次。
- **替换运行时裸 365/91**：`OfficeSystem`、`LawEffectRuntime`、`WarRules`、`WarJustificationSystem`、`DiplomacyManager`、`CombatManager`、`SuccessionSystem`、`PoliticsSimulationSystem`、`StateFormationSystem`、`SettlementEvolutionSystem`、`CharacterManager`、`GameWorld.Budget/Simulation`、`DisasterSystem` 等改用 `calendar.AbsoluteDay/daysPerYear` 或 `GameCalendar.Default*`；运行时 grep `365/91` 只剩日历自身。
- 测试：`GameCalendarTests`（4）、`SimulationSchedulerCadenceTests`（4，含 30/90/365 跳日）、`CalendarIntegrationTests`（2 + 100 年 Explicit：365 与 360 天两套各 100 年）。
- 验证：全量 EditMode `EXIT=0`、1035 例、1026 通过、0 失败、零 `error CS`（9 个 Explicit 跳过）；100 年双历法回归通过（81.1s）。
- 说明：5000–7000 年不作为时间门禁；P8.1 的城市/政权只由机制涌现，该区间是文献校准带（Zeder 2011、Çatalhöyük 7400–5600 cal BC、Uruk 晚第五至第四千年、Yoffee 2005、Trigger 2003 等，见 P8 计划 §6）。

## D145 P8.1 城市与政权的机制涌现（2026-11-14）

- **取消年份门禁**：`StateFormationSystem` 删除 `MinSettlementAgeYears` 与 `ageDays` 检查，不再读世界/聚落年龄；门槛只剩可指认的物质条件：城市形态、人口 ≥800、开发 ≥15、地区仓储 ≥180 天口粮；`MaxRealms=8` 仍是实体规模上限。
- **城市形态在既有系统内完善**：不新建系统，`SettlementEvolutionSystem.TryUrbanize` 负责村镇→城市；条件为定居、人口 ≥1200、开发 ≥20、有市场、营建就绪、地区可食库存 ≥人口×180 天。成立时写编年史 `settlement_urbanized`，并记录 `FirstCityYear` 观测值。
- **世界生成不再预置城市**：`AnchorGenerator` 的省中心与港口由 `SettlementForm.City` 改回默认村镇；城市必须由机制在运行时出现（此前 P7.0 让每个省治开局即城市，等于绕过整条演化链）。
- **政权形成也去年龄化**：`StateFormationHost` 记录 `FirstRealmYear`；建国仍由 `StateFormationHost` 创建开国首领与 Household（既有 P5 设计），首领不是额外数值门槛。
- **批处理等价性**：`EconomyManager.DailyTick(int days)` 按实际跨天数缩放产粮/资源产出、口粮与盐消费、腐损（`(1-日损)^days`）；`EconomySchedule` 传 `context.deltaDays`。贸易发车与商队移动仍按 tick，标为已知近似。
- **诊断推进语义修正**：`RunDiagnosticDays(n)` 现在推进恰好 n 天（内部 `ceil(n/daysPerTick)` 个 tick），此前 `daysPerTick>1` 时会把一次调用当成一天、实际推进 n×daysPerTick 天。
- **观测而非配置**：新增 Explicit 测试 `NaturalLongRun_ObservesFirstCityAndRealmYears`，48×24、seed 4242、`daysPerTick=30`、窗口 7000 年，输出 `firstCity/firstRealm` 年份。
- **观测结果**：`firstCity=109`、`firstRealm=109`（worldYear=109，58 聚落、2 城、1 政权）。原因可指认：默认世界开局即为有市场的定居村镇（人口数百、开发 20–50），距 1200 人城市只差约一世纪的人口增长；5000–7000 年是“从采集基线到城市”的文献校准带，本作当前开局已跳过采集阶段。
- 测试：`SettlementUrbanizationTests` 4 例；`NaturalEmergenceTests.TooYoungSettlement_DoesNotFormRealm` 改为 `ZeroAgeCity_CanFormRealm`；旧的“100 年内无政权”断言随年份门一并删除。
- 验证：全量 EditMode `EXIT=0`、1039 例、1030 通过、0 失败、零 `error CS`（9 个 Explicit 跳过）；7000 年观测窗口在 109 年即满足城市与政权，长跑 4.3s。
- 欠账：市场目前只在生成期置位、运行时没有“市场涌现”机制；开局村镇的初始开发度仍是生成常量。把开局改为营地/游群基线、让市场随人口与交换涌现，是逼近文献校准带的正路。
