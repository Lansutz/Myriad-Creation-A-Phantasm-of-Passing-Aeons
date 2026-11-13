# 宗教系统 v2：教义结构、下游接线与数值纪律

> 2026-11-10。依据：`BELIEF_SYSTEM_MODEL.md`（六层 + 个人层已定）、`FAITH_DOCTRINE_CLASSIFICATION.md`（49 组两轴重排 / 126 档位）、`research/RELIGION_STRUCTURES.md`（10 原型）、`RELIGION_FACTS_VERIFIED.md`（8 条已核）、`RELIGION_SCHOLARSHIP.md`（四轴）、`INTEGRATION_PLAN.md`（四轴 + 六问 + 什一税更正）、`PERF_LESSONS_CK3.md`；CK3 1.20 官方 schema 逐行核对（`E:\SteamLibrary\steamapps\common\Crusader Kings III\game\common\religion`）。
> 状态：**设计稿**。只锁结构、门禁与数值纪律，不锁内容量。**所有宗教词汇都是拆 CK3 用的分析工具，不是最终数据模型**；判据是"能不能比 CK3 表达更多、且不爆复杂度"。
> 轴标注：`真`＝模拟真实性｜`性`＝性能｜`维`＝可维护性｜`模`＝可模组化（四轴口径见 `INTEGRATION_PLAN.md:16-25`）。每条设计标它落在哪几轴；只落一两轴而伤到另一轴的，§6 明确否决。

## 0. 结论（先看这五条）

1. CK3 的宗教是**一张平表 + 一个覆盖链 + 一堆手填数字**；我们要的是**组→档→参数→接线点**四段式，数字带来源标签（§3、§5）。
2. 比 CK3 更好的可检验含义只有四条：**参数有单位与来源**、**教义声明在哪生效与失败怎么办**、**改教义的权力可表达**、**政教与教产是实体字段不是头衔指针**（§1）。
3. 层级沿用已定稿的六层 + 个人层，**不新增层**；制度四支（polity/liturgy/precept/penalty）是**挂在层上的组件**，不是第七层（§2）。
4. 教义解析**只在写入期做一次**并缓存快照，读路径永不仲裁 —— 这是性能与可维护性的同一个解（§2.3）。
5. 我们自己的现状：`DoctrineOptionDef.cs:9-19` 只有 `optionId/pillar/optionName/description/exclusiveReligionIds/enhancedFrom`——**无分组、无档位、无参数、无效果**；`FaithSystem.cs:34-42` 的 `churchWealth/churchInfluence/missionaryZeal=0.5f/tolerance=0.5f` 是无来源默认值且无消费者。这不是要重造，是要补结构。

## 0.4 四概念与挂载条件（D34，2026-11-11 更正，优先于 §0.5）

| 概念 | 是什么 | 是否必挂 |
|---|---|---|
| **faith 信仰** | **定义主体**：教理（doctrine）/治理（polity）/礼仪（liturgy）/戒律（precept）/处分（penalty）都是 faith **内部的组成部分** | — |
| **religion 宗教** | 有**统一组织/制度**时的上一层（教阶、经典、跨地域礼仪） | **按需**：泛灵、家神、地方崇拜**不挂** |
| **denomination 宗派** | religion 之下的分支（主流/分歧度） | 有 religion 才有；无 religion 的 faith **没有宗派** |
| **lineage 传承** | faith → faith 的谱系（它从哪个信仰传下来） | 任何 faith 都可以有 |

⇒ **"faith 必挂 religion" 作废**（D29/G1 原口径错）。正确判据是：
- 有跨地域组织 → 挂 religion（+ 可宗派）；
- 只在地方/家户/族群内流传 → **不挂**，但教理与治理照样在 faith 内部完整运作；
- 任何 faith → 可以有传承。

**与 CK3 的差异（这是我们"更好"的核心）**：CK3 假设"信仰 = 有组织的宗教"，所以泛灵只能被塞进某个 religion 当 faith；我们把**组织性做成可选项**，让无组织信仰拥有同等完整的教理/治理定义。

**对 G1/G3 的影响**：
- G1 验收从"必挂"改为"**挂载合法**"——声明了父必须能解析（否则报错），**未声明是合法状态**；
- G3 验收从"必挂宗派"改为"**有 religion 的 faith 必须落到一个宗派（含默认宗派）；无 religion 的 faith 合法地没有宗派**"。

---

> 旧提法（"救回 22 条没建 `FaithSystem` 的旧数据"）**方向错误**：那是迁就代码的欠账，不是目标。
> 正确目标：**让 faith 成为真正的运行单位**，lineage 承载传承，denomination 从 faith 聚合。

| # | 目标 | 具体做什么 | 验收（可断言） |
|---|---|---|---|
| **G1** | **faith 建入运行态且必挂** | 所有内容 faith 都建运行对象；每个 faith 必有 `religionId`；内容缺 `religionId` **加载失败**（不许默认挂到某个 religion） | 运行态 faith 数 = 内容 faith 数；随机抽 10 个，`religionId` 均非空且指向存在 religion |
| **G2** | **lineage 承载传承** | 从 `parentReligionId` **拆出** `lineageFaithId`（faith→父信仰）；父不存在则空并记"初代信仰" | 每个 faith 的 `lineageFaithId` 要么空、要么指向存在的 faith；子信仰链可追 3 代 |
| **G3** | **denomination 从 faith 聚合** | 建 `Denomination`（`status`/`divergence`/`mainstream`）；`faith.denominationId` 必填；未分裂宗教给 `isDefault=true` 默认宗派 | 每个 religion ≥1 宗派；`divergence` 由 tenet/礼仪/戒律差算出（不手填） |
| **G4** | **lineage 与宗派不再抢一个字段** | `parentReligionId` 只留"层级父指针"；宗派走 `denominationId`；传承走 `lineageFaithId` | `grep parentReligionId` 的读点只剩层级用途 |

**依赖**：G1 → G2 → G3 → G4（严格顺序）。**与 WP 的关系**：G1–G4 是 **WP2（教理 schema）的前置**——没有稳定的 faith 层，教义没有可挂的位置。

---

## 0.6 G1 施工单（可执行到函数级）

| 步 | 文件 / 位置 | 动作 | 验收 |
|---|---|---|---|
| 1 | `Simulation/WorldState/GameWorld.Core.cs:966-987` `InitializeFaithSystems()` | 现在只对 `nodeType == Succession` 建运行态。改为：**先按 nodeType 判定层**（Religion→religion 层；Succession→faith 层；Tradition(8 条实际是宗派)→denomination 层，留到 G3；School/ReligiousSchool→school 层），只对 **faith 层** 建运行态 | 运行态数量 = faith 层节点数，且日志打印"faith N / denomination M（待 G3）/ school K"三类计数 |
| 2 | `ReligionDef`（`Content.Definitions/Faith/`） | 校验 `parentReligionId` 非空且指向存在的 religion；**不满足则 `Debug.LogError` 并跳过**（不许默认归属） | 故意把某条 faith 的 parent 改成不存在 → 加载报错且该 faith 不进运行态 |
| 3 | 同函数 | 运行态对象补 `religionId`（父）、`denominationId = -1`（G3 填）、`lineageFaithId = -1`（G2 填） | 读档/新局后三字段可查；`denominationId/-1` 不被误当作有效值 |
| 4 | `Infrastructure.Save` | 三个新字段进 `FaithSaveData` + 迁移默认值 | 存档往返后三字段不变 |

**不做**：不动 22 条的内容数据（那是 G3 的宗派归属问题）；不改 `ReligionNodeType` 枚举（G4 才拆）。

---

## 1. CK3 的五个结构性缺陷，与我们的绕过

### 缺陷 1｜"参数"只是开关，程度无处安放，数字无来源　`真·模`

- **CK3 写法**：`parameters` 是"任意参数"，只能用 `has_doctrine_parameter` 查**存在性**（`doctrine_types/_doctrine_types.info:61-67`）；真正的强度被赶进引擎白名单字段 `special_parameters`（同文件 `:136-282`），而多来源冲突的规则是**取最高值**（`:312-330` Precedence）。内容侧于是只能手填魔数：`number_of_spouses = 1/4`、`number_of_consorts = 3`、`divergence = 0/30/20`（`doctrine_types/20_doctrines.txt:1-53`）。
- **我们写法**：参数是**全局注册的键**，带单位与来源：`ParameterSpec{ key, unit, scale, source | calibratedBy=TODO_M05 }`。内容里出现未注册的 key，或值既无 `source` 又无 `TODO_M05` 标签 → **加载失败**。
- **更好在**：CK3 的"3 个侧室"是不可追问的常量；我们的同一条要么指到教会法／法典条文，要么显式欠着 M0.5。冲突不再"取最高"，而是写入期报错，**没有人能悄悄把强度加进去**。

### 缺陷 2｜教义只声明效果，不声明在哪生效、缺了怎么办　`真·维`

- **CK3 写法**：doctrine 直接挂 `character_modifier`/`clergy_modifier`（`_doctrine_types.info:115-128`），注释说"所有该礼仪的角色都得到此修正"；但**哪个系统在什么时候读它、没有这条教义时算什么**，schema 里一个字都没有。缺失行为全靠脚本各自 `if has_doctrine`，写漏就是静默失效。
- **我们写法**：每条 `DoctrineOption` 声明 `bindsTo[]`（接线点 id），每个 `BindingPoint` 声明 `reads[]` 与 **`failMode ∈ {deny, allow, inherit, block}`**。八个接线点见 §3.2。
- **更好在**：缺教会产生**可枚举的后果**而不是"没反应"：无教义不许结婚（deny）、无教义不构成罪行（allow）、判不出异端就维持现状并留痕（block）。CI 能同时拦住"写了没人读"和"读了不存在"（§7 断言 3）。

### 缺陷 3｜层级靠覆盖与锁定，不靠职权：谁有权改教义不可表达　`真·维`

- **CK3 写法**：同组教义按 **rite 覆盖 faith、faith 覆盖 religion** 解析（`rite_types/_rite_types.info:74-82`、`faith_types/_faith_types.info:36-39`）；`doctrine_lock = rite|faith|religion` 的全部语义是"**失去时被移除**"（`doctrine_group_types/_doctrine_group_types.info:26-37`），不是"只能由谁改"。权限只剩 `can_pick` 的 `actor`（"通常是信仰领袖/礼仪领袖"，`_doctrine_types.info:100-113`）——那只是"能不能选"。
- **我们写法**：每条教义带 `setBy ∈ {family, religion, denomination, faith, school, custom}` + 所属层的 `amendmentAuthority ∈ {seat, council, ruler, congregation, none}`。下层另有声明时**不静默覆盖**，产生 `DoctrineConflict` 并记录，由 `amendmentAuthority` 判定一方胜出。
- **更好在**：这正是 `RELIGION_SCHOLARSHIP.md:107-133` 六种政教形态的落点——`caesaropapist` 下统治者能改、`dualist` 下教座能否决、`embedded` 下没人能单独改。CK3 用覆盖表达层级，**结构上就说不出"皇帝支持的异端"为什么能成立**（圣上能任命、却不能说这是异端）。

### 缺陷 4｜宗教实体没有"组织体"：政教、教产、任命被压成头衔指针 + 政体枚举　`真`

- **CK3 写法**：`religion_details` 只有 family / graphical_faith / piety_icon_group / theocracy_government_type / theocracy_lease_contract_type / 背景图标（`religion_types/_religion_types.info:6-16`）；"有没有教会"= 一个 `religious_head = title_key`、`head_of_rite = title_key`（`faith_types/_faith_types.info:7-11`）。教产归属、任命权、豁免，schema 里没有位置。
- **我们写法**：`Denomination` 上直接放 `propertyHolder ∈ {see, order, congregation, state, individual, none}`、`appointmentAuthority`、`exemptions[]`（复用 `Privilege{kind=exemption}`，`BELIEF_SYSTEM_MODEL.md:486-515`）。
- **更好在**：`RELIGION_STRUCTURES.md:129-140` 里 C/D/E/F 四型的差别**就是这几格**（学者无职位训导权、会众所有制、修会独立财产、祭司即公职）。CK3 只能靠头衔指针暗示，我们要能直接查。

### 缺陷 5｜个人层与群体层断开，偏离度与异端阈值是手填数　`真·性`

- **CK3 写法**：德性/罪行的 `weight` **直接变成** "base Spiritual Fulfillment"（`_religion_types.info:37-40`），倍率是全局 define（`00_defines.txt:894-901`）；群体 `fervor` 是另一套独立浮点。异端阈值 `RITE_DIVERGENCE_HERETICAL_THRESHOLD = 65` 一个数管所有宗教（`00_defines.txt:834`）；`divergence` 由档位 `index` 的求和或绝对差算（`_doctrine_group_types.info:18-24`）——**偏离度=档位距离×手填权重**。
- **我们写法**：群体 `fervor` **由成员分布推导**（中位数 + 高分段占比，`BELIEF_SYSTEM_MODEL.md:204`）；异端判定读三个可指认输入（正统边界差 + 信众占比 + 统治者立场），阈值是**内容参数**不是全局 define。
- **更好在**："这个信仰有多少人真心"在 CK3 里不存在，在我们这儿是可算的；异端不是"分歧度 > 65"这种跨文化常量，而是可解释的政治—教义合成判断。

## 2. 分层、实体与六问门禁

### 2.1 七层（`真·维`）

| # | 层/实体 | 关键字段（只列有读者的） | 分裂产生什么 |
|---|---|---|---|
| 0 | `ReligionFamily` | `ultimacy`、`canonStructure`、`exclusivity ∈ {inclusive,henotheistic,exclusive}`、`narrativeId` | 一般不分 |
| 1 | `Religion` | `familyId`、`assemblyMode ∈ {liturgical,lineage}`、`differentiation ∈ {differentiated,embedded}`、`creedId?`（embedded 可空）、`canon{textIds,closed,living}`、`baseLiturgyId`、`orthodoxy{tenetIds,exclusions}` | 罕见 |
| 2 | `Denomination` | `religionId`、`parentDenominationId?`、`isDefault`、`status`、`sinceDay`、`divergence`（算）、`headTitleId?`、**`propertyHolder`、`appointmentAuthority`、`exemptions[]`、`amendmentAuthority`** | 新宗派 |
| 3 | `Faith` | `religionId`（必）、`lineageFaithId`（传承）、`childFaithIds[]`、`denominationId`（必）、`schoolId?`、`adherence`、`fervor`（聚合） | 子 faith |
| 4 | `School` | `praxisId`、`exegesisId`、`ruleId` | 新学派 |
| 5 | `Custom` | `placeRef`、`tabooIds[]`、`idiomId` | 地方变体 |
| P | `PersonalFaith` | `faithId`、`patron{kind,id}?`、`vows[]`、`practice[]`、`isSecret`、`conversionHistory[]`、`spiritualFulfillment` | 不移 |

**两条硬规则**：① 制度四支（`polity/liturgy/precept/penalty`）是**组件**，挂在 religion/denomination 上，**不自成层、不各自入档**——只有被接线点读到的字段才入档；② `rite` **不建层**（理由见 §4 第 1 条）。

### 2.2 六问门禁（`维·模`）

> 门禁口径取自 `INTEGRATION_PLAN.md:49-53`。任一行答不出，不算完成。

| 层/实体 | 谁写（唯一入口） | 谁读 | 入档 | UI | AI | 留痕 |
|---|---|---|---|---|---|---|
| family | 内容 JSON | religion 解析、家族页 | 否（静态） | 是 | 否 | — |
| religion | 世界生成 / 改革决议 / 立国教 | faith 解析、正统判定、政教接线 | 是 | 是 | 是 | 改革、分裂 |
| denomination | 分裂事件、会议决议、主流更替 | 异端判定、传教、合法性、司法管辖 | 是 | 是 | 是 | **是**（判异端/主流更替） |
| faith | `ConvertSystem`（改宗/传教/分裂） | 人口构成 + 全部八个接线点 | 是 | 是 | 是 | **是**（改宗/分裂） |
| school | 内容 + 师承事件 | 解释权威、教育接线 | 是 | 是 | 是 | 学派分立 |
| custom | 地方事件 | 礼仪/禁忌、地方抵抗 | 是 | 是 | 否 | 地方志 |
| personal | **只由行为事件写**（捐产/朝圣/守戒/破誓） | fervor 聚合、封圣、异端嫌疑 | 是 | 是 | 是 | 是 |
| doctrine 内容（group/option/spec） | 内容 JSON | BindingPoint | 否（静态） | 是 | — | — |
| `EffectiveDoctrineSet`（派生） | 写入期重建 | 全部接线点 | **否（可重算）** | 只读快照 | 是 | 否 |

### 2.3 三条解析规则（`性·维`）

1. **快照制**：`EffectiveDoctrineSet(faithId)` 是**不可变快照**，只在写入期（改宗/改革/会议/建国教/分裂）重建；读路径 O(1)，**不做每 tick 解析**。
2. **写入期仲裁**：冲突在写入那一刻解决并记录 `DoctrineConflict`；**读路径永不仲裁**。因此读耗时与冲突数无关。
3. **按组索引订阅**：接线点注册为 `groupId → handlers[]`，**不遍历教义全集**，也不做"每角色×每月×全部教义"的笛卡尔积（`PERF_LESSONS_CK3.md:47-49`）。

## 3. 教义不是一张平表：组 → 档 → 参数 → 判定

### 3.1 结构（`真·模`）

```
DoctrineGroup  { id, domain ∈ 11 域, kind ∈ {sequence, category}, options[] }
DoctrineOption { id, groupId, level(组内有序), parameters{paramKey: value}, requires{}, bindsTo[], setBy }
ParameterSpec  { key, unit, scale, source | calibratedBy=TODO_M05 }     ← 全局注册表，内容不得自造 key
BindingPoint   { id, reads[{group, param?}], effect, failMode, trace }
```

11 域（承 `FAITH_DOCTRINE_CLASSIFICATION.md:38-40`，裁到有读者的）：神观 / 经典 / 婚姻·亲属 / 性别 / 教职 / 政教 / 礼仪·节期 / 司法·罪行 / 经济·教产 / 教育 / 宗教间。**序（kind）必须显式声明**：`sequence`＝一条光谱上的档（神观、禁婚亲等），`category`＝互不相容的并行选项（丧葬形态、教职继承）；这直接决定 `divergence` 是"绝对差"还是"求和"（同 CK3 `_doctrine_group_types.info:18-24`），但**权重是内容参数，不是档位 index**。

**"信条槽"只保留三格，不是五格**（承 `FAITH_DOCTRINE_CLASSIFICATION.md` §4.3 但收窄）：`神观`（正典·锁定）、`救赎/经典立场`（正典·锁定）、`伦理`（可改）。CK3 的 `FAITH_CORE_TENETS_CAP = 3`（`00_defines.txt:908`）是设计选择不是史实，**我们不引用它当依据**，改用"两个锁定 + 一个可改"作为**改革成本的设计**并写明这一点。

### 3.2 八个下游接线点（`真·维·模`）

| 接线点 | 读什么 | 影响什么 | 失败时（failMode） |
|---|---|---|---|
| **婚姻** | 婚姻形态、禁婚亲等、离婚、私生认定 | 婚配提案合法性、离婚许可、禁婚范围、私生标签 | `deny`（沿用上层；上层也无 → 最严档） |
| **继承** | 私生认定、性别·继承、教士婚姻（子嗣）、异端状态 | 继承资格与顺位、私生子能否继承 | `inherit`（借 religion → 再借世俗法） |
| **性别** | 性别·任职、性别·继承、性别·礼仪 | 任官/继承/摄政/礼仪角色资格 | `deny` |
| **司法** | 六组罪行 + 处分种类 | 罪行标签、处分种类、教会法庭管辖 | `allow`（**无教义不等于有罪**） |
| **税** | `tithe.rate`【TODO_M05】、教产归属、豁免特许 | 税率与征收者、教产豁免实际效果 | `block` + 记 `MissingParameter`；**默认 0 不征** |
| **教育** | 正典文本、礼仪语言、训导权范围 | 识字来源、经典解释权、学校归属 | `inherit` |
| **传教** | 弥散/分化、排他性、仪式频率与唤起、宗教间敌对 | 改宗速率、传教许可、抵抗强度 | `deny`（无许可不传） |
| **异端判定** | 正统边界、divergence 三权重、异端阈值 | `denomination.status` 迁移、迫害、合法性缺口 | `block`（判不出就维持现状，**不自动判异端**） |

> 每条接线点在代码里**只有一个实现**（`DoctrineBindingRegistry`）。新增档位＝加 JSON；新增接线点＝加一条 `BindingPoint` 声明 + 注册一个 handler，**不改散落 switch**（D20 五信号），也**不改 `.cs` 内容表**（D18）。

## 4. 应当删掉的九个概念（CK3 有、我们不要）

| # | 概念 | CK3 证据 | 处置与理由 |
|---|---|---|---|
| 1 | `rite` 作为**独立层** | `_rite_types.info:30-93`：`founder`/`faith`/`tenets`/`doctrines`/`cultures`——它同时在装"礼仪变体"和"宗派" | **不建层**。宗派归 `denomination`，礼仪降为 `liturgyVariantId` 组件。一个槽装两义，必然让"治理分裂"和"礼仪差异"不可区分 |
| 2 | `unreformed` 布尔 | `_doctrine_types.info:250-258`：改革＝新建信仰、旧信仰改名加 `_old`，"只能改革一次是你自己的责任，代码不管" | 删。改 `organizationStage`（判据推导、**可升可降**，`FAITH_DOCTRINE_CLASSIFICATION.md:231-257`） |
| 3 | `doctrine` 作为实现桶 | 同桶里装神观、婚姻法、刑法、组织、礼仪、身份、状态（`FAITH_DOCTRINE_CLASSIFICATION.md:9`，"实现桶"三字即出自该行所引官方概念页） | 删"桶"。按两轴拆七类型，**只有"规则"进教义组**，其余归各自系统 |
| 4 | 五系身份标记 | `is_christian_faith`/`is_islamic_faith`/… 各 1 档（`FAITH_DOCTRINE_CLASSIFICATION.md:105-110`） | 删。由 `familyId` + tag 派生；布尔组会随新增宗教无限膨胀 |
| 5 | `doctrine_lock` | `_doctrine_group_types.info:26-37`：语义只是"失去时移除" | 删。改 `amendmentAuthority`（谁有权改，§1 缺陷 3） |
| 6 | `tenet_selection_pair` | `_faith_types.info:45-49`、`_rite_types.info:84-88` | 删。DLC 商业机制，不是历史机制 |
| 7 | 表现字段混入宗教 schema | `graphical_faith`、`piety_icon_group`、`tenet_*_background_icon`（`_religion_types.info:8-16`） | 移出。这是 UI 主题，不是宗教概念；留在 schema 里会让内容作者以为它有意义 |
| 8 | 数量硬上限 | `eminent_holy_sites_max = 3`、`holy_sites_max = 9`（`_religion_types.info:19-22`；define `921/923`）；`BASE_PERSONAL_TENETS_CAP = 1`（`904`） | 删上限。圣地不设数，用 `eminence ∈ {local, regional, universal}` 排序；个人誓言不限，教义允许什么就许什么 |
| 9 | "特质 → 灵性满足"直给 | `_religion_types.info:37-40` weight → base Spiritual Fulfillment；全局倍率 `00_defines.txt:894-901` | 删这个写入口（我们代码里是 `CharacterManager.cs:102-118`：`GetVirtueScore × 0.01f` − `GetSinScore × 0.02f` − 秘密信徒 `0.01f`，**三个常数全无来源**）。保留 0–100 刻度，**改为只由行为事件写**（`BELIEF_SYSTEM_MODEL.md:200-206`） |

## 5. 数值策略：三桶 + 什一税专条

| 桶 | 内容 | 规则 |
|---|---|---|
| **A 可内容化**（有可引用来源） | 禁婚亲等的代数、斋期与节期日数、礼仪语言、一夫多妻/侧室的**法源**、处分的种类与程序 | 必须填 `source`；填不出就降到 B，不许"大致记得" |
| **B 必须 M0.5 标定** | `divergence` 三权重、异端判定的三个输入阈值、传教/改宗速率、`fervor` 漂移、`organizationStage` 判据阈值、仪式频率×唤起对传播的影响 | 标 `calibratedBy=TODO_M05` + 给**初始区间与单调性预期**（例如"三权重均 ≥0 且和为 1"），M0.5 调 |
| **C 暂不引入** | 一切"信仰 → +X% 稳定/税收/战力"乘子；每 tick 常数漂移；跨宗教统一的异端阈值（CK3 的 65）；教义→战斗修正 | 进来了就是宪章原则 5 的违规，直接拒 |

**什一税专条**（吸收用户更正：此前"11 岁"是转写错误，原意是**什一税**）：
名义额"十分之一"有词源与教令依据，但**实际征收率、征收物（实物/折钱）、折算（commutation）、豁免与侵占（impropriation）的史料区间，本轮未取得可引用来源**（本机联网检索在本会话被拦截，见 §8），因此：

- 字段 `economy.tithe.rate` 存在，值 = **`TODO_M05`**，默认 0（不征），`failMode=block` + 记 `MissingParameter`；
- **禁止**填 `0.1`。理由：0.1 会让"教会财富"变成一条凭空乘子，而历史上真正决定教会收入的是**征收能力与豁免范围**，不是名义比例；
- 取得区间后，`rate` 仍应是**区间 + 征收能力系数**两参数，不是单值。

同样的纪律适用于任何税率、比例、阈值：**没有来源就只能是 `TODO_M05`，不能是数字**。

## 6. 明确否决的写法（会破坏性能 / 可模组化）

| 写法 | 否决理由 | 轴 |
|---|---|---|
| 每 tick 遍历所有 faith 算正统/异端 | 与 `PERF_LESSONS_CK3.md:47-49` 直接冲突；CK3 性能增长最大来源正是"每个 AI 每月多算一件事" | 性 |
| 每角色 × 每月 × 每条教义求值 | 同上，且教义根本不需要角色粒度——角色只读所在 faith 的快照 | 性 |
| 读路径上做教义仲裁 | 每次读都要解释一遍，且结果不可缓存 | 性·维 |
| `switch (doctrineId)` 或 C# 枚举写档位 | 加一个档位就要改代码，直接违反 D18 | 模·维 |
| 新增宗教概念就新增一张存档表 | 应复用 `Faith`/`Denomination` 字段 + 迁移；表越多，六问里"入档"越容易漏 | 维 |
| 用"信教 +5% 稳定"表达后果 | 宪章原则 5（D16）；后果必须落在 §3.2 的某个接线点上 | 真 |
| 把八接线点做成八个独立系统 | 会重复实现"读教义"，且新增接线点要改八处 | 维 |

## 7. 验收（五条可执行断言）

1. **无硬编码档位**：新增一个教义档位只改 JSON。CI 断言 `DoctrineOption` 的 `groupId/level/parameters/bindsTo` 全部来自 JSON，且 C# 中不存在 `doctrineId` 字面量比对。
2. **参数来源守卫**：加载器对任何 `parameters` 值，若既无 `source` 又无 `calibratedBy=TODO_M05` → **加载失败并报出条目与字段**；测试用例必须包含 `tithe.rate = 0.1`（期望被拒）与 `tithe.rate = TODO_M05`（期望通过、默认 0、`failMode=block`）。
3. **接线闭环**：CI 双向校验——每个 `BindingPoint.reads` 的 group 至少要有一个 option 能解析出值；每个已声明的 group 至少被一个 `BindingPoint` 读。**"写了没人读"与"读了不存在"都拦得住**（对应六问的第 2 问）。
4. **写入期冲突可复现**：构造"faith 允许离婚 / religion 禁止离婚"，断言产出 `DoctrineConflict` 记录、由 `amendmentAuthority` 判定的胜出方明确，且该次解析的读路径耗时与冲突条数无关（快照未被重建时不重算）。
5. **性能可归因**：M0.5 长跑 100 年，`doctrine.eval` 与 `EffectiveDoctrineSet.rebuild` 两个计数器可读；断言 **`rebuild` 次数 = 写入次数**，且日耗时增量能归因到具体接线点。**增量阈值留给 M0.5 定**，本文件不预设数字。

## 8. 待查（不编，给查法）

| # | 问题 | 查法 |
|---|---|---|
| 1 | 什一税的实际税率、征收物、折算与豁免区间 | 教会法汇编（Gratian《Decretum》C.13 相关问答）+ 教区 tithe accounts 研究；关键词 `tithe commutation`、`impropriation`、`great and small tithes`。本会话联网检索被拦截（`web-search-safe` 报 DNS 拦截），下轮补 |
| 2 | 禁婚亲等的教法改动（是否 1215 第四次拉特兰会议由七代缩至四代、canon 编号） | 查会议法令原文：Tanner, *Decrees of the Ecumenical Councils* vol.1；本文件不把它当 A 桶事实用，只当"有文本可查"的示例 |
| 3 | CK3 `has_doctrine_parameter` 是否支持非布尔参数 | 已记 `FAITH_DOCTRINE_CLASSIFICATION.md:411-416` 第 2 条；需实测（构造参数后查非布尔用法） |
| 4 | 现有 `Doctrines.json` 46 条 → 11 域的映射，哪些域是空的 | 一次内容盘点，产物另出；本文只定结构 |
| 5 | `EffectiveDoctrineSet` 的失效触发点是否覆盖全部写入者 | 需与 `ConvertSystem`/会议/改革决议逐一对齐，D20 五信号 |
