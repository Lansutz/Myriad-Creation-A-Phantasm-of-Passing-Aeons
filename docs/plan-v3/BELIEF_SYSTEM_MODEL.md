# 信仰大系统：术语、层级与数据模型（v1 草案）

> ## ⚠ 口径优先（2026-11-11 用户重申，读本文前必看）
>
> **1. 主运行单位是 `faith`（信仰）**，不是 `religion`。玩家、人口、传教、改宗、分裂，全都发生在 faith 上；religion 是它的必挂归属，family 在最上。
> **2. `lineage`（传承）= faith 指向父信仰的引用**，是"我这个信仰从哪个信仰传下来"；**不叫传统**，也不是文化传统（`TraditionDef`）、不是文化遗产（`HeritageDef`）。
> **3. 设计口径 > 代码现状。** 代码里现在长什么样（例如 44 条内容里只有 22 条建了 `FaithSystem`、`ReligionNodeType` 把宗派写成了 `Tradition`）是**迁就现状的欠账**，不是模型的定义。**判断一个设计对不对，看本文与 D29/D31，不看去 grep 代码。**
> 4. 代码现状只在一处使用：`RELIGION_TERMS_MATRIX.md` 的"现状"列，用于排迁移工作量。
>
> **4. 内容里的 44 条宗教/信仰是「预制模板」，不是这个世界的信仰表。** 这个世界的 faith 与 religion 由**演化过程中文化自发产生**（`culture → faith/religion` 的生成），预制件的作用是给生成器提供种子/样例/风格，以及给"历史开局"提供初始局面。**不要把预制条目当成世界的全部**——这是 G1–G3 之外的第五件事。
> **5. `religion` 必挂，`family` 不必挂。**（更正此前把 family 也当作必挂层的写法。）一个 faith 必须有 religion；它上面有没有 family 由宗教形态决定。
> **6. 加载方式：流式 + 预加载。** 宗派/宗教/信仰的初始化**不是开局全量建表**，而是"用到才加载"（流式）；同时按既定预加载规则，把**会被立刻用到的子集**（国教、人口多数信仰、地图上正在发生的宗教事件相关条目）预加载，避免首次访问时卡顿。
>
> ---

> 2026-11-09 03:45。依据：用户给的术语体系（faith/religion/tradition；教理 8 词；制度 4 支；五层区分；各层区分标志）+ 学派类型学（宗教/政治/哲学/学术）+ 现有代码与内容（`ReligionCatalog` 44 条、`Doctrines` 46 条、`Tradition` 13 条、`Ethos` 8 条、`FaithSystem`、`FAITH_DOCTRINE_CLASSIFICATION` 两轴表）。
> 状态：**草案**。信仰系统正在重构（宪章 §7），本文只定**术语与数据形状**，不锁实现。

---

## 1. 三个总概念（排他定义，直接当字段名）

| 词 | 中文 | 排他边界 | 游戏里是什么 |
|---|---|---|---|
| `faith` | 信仰 | 排除纯命题 belief、制度化 religion | **个体/群体的委身状态**（谁信、多深、是否动摇） |
| `religion` | 宗教 | 排除个人 faith | **复合实体**：信条—实践—共同体—制度 |
| `lineage` | **传承** | 排除文化传统（`TraditionDef`）、保守主义传统、遗产（`HeritageDef`） | **信仰系统的传承引用**：faith → 父信仰（见 §4，已定稿） |

> **关键约定**：`lineage`（传承）是 faith 指向**父信仰**的引用，不是内容层、不是文化传统、不是遗产。三个词各归各家：传承（信仰）/ 遗产（文化·族群 `HeritageDef`）/ 文化传统（文化 `TraditionDef`）。

---

## 2. 教理词族（8 词，一个词一个字段）

| 词 | 中文 | 排他边界 | 数据位置 |
|---|---|---|---|
| `doctrine` | 教义 | 整套教导体系；排除单条 tenet、公开宣认 creed、权威定断 dogma | `religion.doctrineIds[]`（体系） |
| `creed` | 信条 | 被群体**公开宣认**的核心摘要；排除宗派 confession、教学手册 catechism | `religion.creedId`（唯一） |
| `tenet` | 单条原则 | 群体奉为真理的单条核心原则；不必然有制度权威 | `doctrine.tenets[]` |
| `dogma` | 教理定断 | 权威定断、不可争辩；排除一般 doctrine | `tenet.authority = dogma`（属性，不是新实体） |
| `confession` | 宗派告白 | 宗派性信仰告白文本；排除普世 creed | `denomination.confessionId` |
| `catechism` | 教义问答 | 问答式教学手册；排除体系性 doctrine | `doctrine.catechismId`（可选，教学用） |
| `orthodoxy` | 正统 | 被认定为正统的**边界**；不等于全部 doctrine | `religion.orthodoxy = { tenetIds[], exclusions[] }` |
| `canon` | 正典 | 被列为权威的**文本清单**；只指文本，不指法规 | `religion.canon = { textIds[], closed }` |

> 法规不进 `canon`；法规归制度层（`polity` / `precept` / `penalty`）。

---

## 3. 制度层：四支并列 + 子类

| 支 | 中文 | 管什么 | 排除 | 子类 |
|---|---|---|---|---|
| `polity` | 治理 | 权威、职位、会议、任命、职权 | 教理、礼仪、伦理 | `office`（教职）/ `magisterium`（训导权）/ `ordination`（授任）/ `council`（会议） |
| `liturgy` | 礼仪 | 仪式程序、节期、圣事、礼仪语言 | 治理、教理 | `ritual` / `calendar` / `language` |
| `precept` | 戒律 | 有约束力的伦理戒条 | 禁忌、处分 | `matrimony`（婚姻治理）/ `taboo`（禁忌）/ `commandment`（成文戒条） |
| `penalty` | 处分 | 违规惩罚与纪律程序 | 戒律本身 | `penaltyKind`（绝罚/停职/除名/苦修）+ `procedure`（谁判/怎么审/怎么上诉） |

**两处需修正用户原稿**：

1. `penalty` 不要在自己下面再放一个同名 `penalty`——应拆成 **`penaltyKind`（种类）** 与 **`procedure`（程序）**，两者并列。
2. `language` 归 `liturgy`（正确），但要标明它管的是**礼仪与经典诵读语言 + 哪个译本有权威**；日常 idiom 归 `custom.idiom`。

**层级断言**：`creed` 与 `polity` 可在"宗教构成维度"并列；**教职系统不是与 creed 并列，而是 `polity` 的子类**（用户原文已如此，此处固定为规则）。

---

## 4. 严格层级（11:50 定稿：`lineage` 传承；`denomination` 必挂）

**定稿结构**：

```
family（宗教家族）
  └ religion（宗教）                        ← 必挂，不可空
      └ denomination（宗派）                 ← **必挂**：承载"主流 / 分歧度"
          └ faith（信仰）                    ← 传播与分裂的单位（可玩单位）
              ├ lineage（传承）= faith.lineageFaithId   ← 把自己的【父信仰】认作传承
              │     · 传播中分裂产生子信仰 ⇒ childFaithIds（照抄文化机制的 parentCultureId/childCultureIds）
              ├ school（学派）                ← 可选
              └ custom（地方习俗）            ← 最外层
```

**三条铁规则**：

1. **`lineage`（传承）不是内容层，是 faith 指向"父信仰"的引用**（`lineageFaithId`）。子信仰把父信仰认作传承——与文化的"父文化/子文化"同构。
2. **`denomination` 必挂、不可空**：没有分裂的宗教也要有一个**默认宗派**（`isDefault=true`）。理由：**"主流"与"分歧度"只能挂在宗派上**——没有宗派这个层，就无法表达"哪个是正统主流、某个信仰偏离多远"。
3. **`religion` 必挂、`family` 最上**；分裂发生在 **faith** 层（产生子 faith），**宗派分裂**则是另一次事件（`parentDenominationId`），两者不混。

| 层 | 区分标志（必答） | 可否空 | 分裂产生什么 |
|---|---|---|---|
| `family` | `ultimacy` + `canon` 结构 + `narrative` | 不可 | 一般不分裂 |
| `religion` | `creed` + `canon` + 基本 `liturgy` + `polity` | **不可空** | 一般不分裂 |
| `denomination` | `confession` + 礼仪形式 + 治理 + `initiation` + 戒律细节 + **`mainstream` + `divergence`** | **不可空**（可只有一个默认宗派） | 新宗派（`parentDenominationId`） |
| `faith` | `faithCreed` + `lineageFaithId` + `denominationId`（必填）+ `schoolId?` | 不可空 | **子 faith**（`parentFaithId`） |
| `school` | `praxis` + `exegesis` + 礼仪细节 + `rule` | 可空 | 解释/修行分立 |
| `custom` | `custom` + `taboo` + `idiom` | 可空 | 地方变体 |

### 4.1 主流与分歧度（为什么宗派必须存在）

```
Religion {
  mainstreamDenominationId        // 当前主流宗派（随政治与人数变化）
  orthodoxy { tenetIds, exclusions }   // 正统边界
}
Denomination {
  id, religionId, parentDenominationId?,
  isDefault,                      // 未分裂宗教的默认宗派
  status ∈ { mainstream, tolerated, heterodox, condemned, extinct },
  divergence,                     // 0..1，与 religion.orthodoxy 的偏离度（可算，不是手填）
  sinceDay,                       // 何时成为主流 / 何时被定为异端
  headTitleId?,                   // 教会组织（可空：无管理机构的宗派）
  adherence                       // 信众占比（聚合自 faith 的 adherence）
}
```

**`divergence` 的算法（不是自由数值）**：

```
divergence = w1 × (1 − tenetOverlap(denomination.tenets, religion.orthodoxy))
           + w2 × liturgyDifference(liturgy, religion.baseLiturgy)
           + w3 × preceptDifference(precept, religion.precept)
```

权重进内容表。**禁止**直接给"分歧度 +10%"这类效果。

**主流是状态，不是标签**：`status` 与 `mainstreamDenominationId` 随时间变化（阿里乌派曾为主流后被定异端、东西教会互判、新教从 tolerated 到 mainstream）。判定输入是：`adherence` 占比 + 统治者支持 + 宗教领袖承认 + 正统边界。

**由此得到的可表达情形**：

| 情形 | 数据表现 |
|---|---|
| 未分裂的宗教 | 一个 `isDefault` 宗派，`status=mainstream`，`divergence=0` |
| 分裂但都合法 | 多个宗派，`status` 分别为 mainstream / tolerated，`divergence` 不同 |
| 被判异端 | `status=heterodox` 或 `condemned`，`sinceDay` 记录判定时间 |
| 主流更替 | `mainstreamDenominationId` 改写 + 编年史条目 |
| 宗派灭绝 | `status=extinct`，其下 faith 或改属或一并 extinct |

> 没有 `denomination` 这一层，"主流"就只能写成 faith 上的一个布尔，"分歧度"只能写成自由浮点——两者都会退化成无原因的数值。这就是它必须存在的原因。

### 4.2 术语禁令（严格用词，不许混用）

**三个容易混的词，各归各家**：

| 词 | 中文 | 属于 | 含义 | 禁止 |
|---|---|---|---|---|
| `lineage` | **传承** | 信仰系统 | faith 指向的父信仰（法脉/统绪/传承链） | ❌ 不叫"传统"；❌ 不是文化习俗 |
| `heritage` | **遗产** | 文化/族群系统 | `HeritageDef`，文化传承下来的身份性遗产 | ❌ 不用于信仰系统 |
| `cultural tradition` | 文化传统 | 文化系统 | 代码 `TraditionDef`（Production/Political 类目、`solidification`） | ❌ 不用于信仰系统 |

| 词 | 只许指 | 禁止用来指 |
|---|---|---|
| `faith` | 传播与分裂的单位、可玩单位 | ❌ 不要说"信仰这个教派"、"faith 层级的宗教" |
| `religion` | faith 的必挂归属（如基督教） | ❌ 不要用来指某个具体共同体（那是 faith） |
| `denomination` | **宗派**：必挂层，承载主流/分歧度/正统判定 | ❌ 当分裂单位（分裂的是 faith）；❌ 当可选层 |
| `school` | 解释/修行流派 | ❌ 与 denomination 混用 |
| `custom` | 地方惯行 | ❌ 与 lineage / heritage 混用 |
| `rite` | 仅指 CK3 的礼仪实体（借词时标注来源） | ❌ 当我们的传承 |

> ⚠ **代码命名冲突（三处）**：① `TraditionDef` = 文化传统（不是传承）；② `HeritageDef` = 遗产（已有，属文化/族群）；③ 信仰侧**没有** `lineage`/`denomination`，需新增 `Faith.lineageFaithId` 与 `Denomination` 实体。



---

## 5.5 挂靠与分裂（借族群/文化机制，但改规则）

族群机制原样：**挂靠（attach）+ 分裂（split）**。宗教侧改造如下：

| 层 | 挂靠 | 可空？ | 分裂产生什么 |
|---|---|---|---|
| `religion` | 挂到 `family` | **不可空**（一直挂靠） | 一般不做；宗教层变化罕见 |
| `faith` | 挂到 `religion`（弱挂 `denomination`/`school`） | 不可空（必须有所属 religion） | **分裂出新的 `faith`**（`parentFaithId`，同 religion） |
| `denomination` | 由 `faith` 群"**追认/管理**"而成 | **可空** | 分裂出新的 `denomination`（分立的理由必须落在告白/礼仪/治理/入教/戒律之一） |
| `lineage`（传承） | 挂到父 faith（`lineageFaithId`） | 可空（有默认：继承创建者的信仰） | 不分裂——分裂的是 faith 本身，产子 faith |
| `school` | 挂到 `faith`/`denomination` | 可空 | 分裂出新的 `school`（praxis/exegesis 之争） |

**两种装配模式（`religion.assemblyMode`）**——这是本轮最重要的新字段：

| 模式 | 顺序 | 典型 | 机制后果 |
|---|---|---|---|
| `liturgical`（自下而上） | **传统（地方礼仪）先在** → 宗派在上追认/管理 | 基督教：罗马礼/高卢礼/莫扎拉比礼 → 宗座教会、主教团 | 宗派的合法性来自"追认既有礼仪"；分裂常表现为"礼不改、治理分" |
| `lineage`（自上而下） | **传承链先定** → 宗派/学派从中分出 | 佛教：法脉/统绪 → 宗派、学派 | 宗派的合法性来自"传承纯正"；分裂常表现为"治理不改、法脉分" |

> 一句话：**基督教的分裂常发生在治理层（谁管），佛教的分裂常发生在传承层（谁正宗）。** 这个差别如果不建模，两种宗教会长成一模一样。

---


---

## 4.5 个人化信仰（个人层，与制度层并行）

制度层（family/religion/faith/denomination/school）描述"群体怎么组织"；**个人层描述"这个人到底信什么、信多深、信得对不对"**。二者可以背离——名义归属某 faith，私下拜家神、守私誓、甚至暗自不信。

**代码已有的四个部件**（不是从零）：

| 部件 | 位置 | 现在做什么 |
|---|---|---|
| `faithId` | `CharacterData.cs:53` | 名义归属；但创建时被硬编码为 `0`（与宗教 id 从 100 起冲突） |
| `piety` | `CharacterData.cs:176` | **性格轴**（-100..100，对应 `zealous`/`cynical` 特质），不是"对某个 faith 的虔诚" |
| `spiritualFulfillment` | `UpdatePiety`（`CharacterManager.cs:102-118`） | **0–100**，按"个人特质 vs 该 faith 的 virtues/sins"每 tick 调：德行 +0.01×分数，罪行 −0.02×分数 |
| `isSecretBeliever` | `CharacterData` | 秘密信徒标记，`UpdatePiety` 里额外 −0.01/tick |

**缺的四件**：

1. **私人主保**（`patron`）：个人选定的神祇/圣人/祖先。现有圣徒与圣地系统（`SaintDef`/`canonizationPiety`/holy sites）只服务群体，未接个人。
2. **私誓**（`vows[]`）：带期限与违约后果的个人承诺（朝圣、守斋、捐产、独身、圣战）。这是"个人信仰如何改变历史"的主要通道。
3. **改宗史**（`conversionHistory[]`）：何时、因何、被谁促成改宗；也是"秘密信徒"的出口（公开化/被揭发）。
4. **聚合链**：群体 `FaithSystem` 的热忱目前是独立浮点（`fervor`），**没有从成员 `spiritualFulfillment`/`devotion` 聚合而来**——所以"这个信仰有多少人真心"这件事不存在。

**设计规则（防止又变成数值加成）**：

| 规则 | 说明 |
|---|---|
| 归属 ≠ 委身 | `faithId` 是名义；`spiritualFulfillment` + `patron` + `vows` 才是实际信仰 |
| 只由行为更新 | 捐产/朝圣/守戒/庇护教士 → 提升；破戒/伪誓 → 下降；**禁止"每年 +1 虔诚"** |
| 个体→群体聚合 | faith 的热忱 = 成员 fulfillment 的分布（如中位数 + 高分段占比），不是独立数值 |
| 后果可指认 | 高 fulfillment → 封圣候选/教职资格/动员；低 fulfillment + 秘密信徒 → 异端嫌疑/司法风险/继承争议 |
| 无制度也能有信仰 | 国教缺失时，个人仍有 `patron`/`vows`/私祭——对应"没有成文法时只有习惯" |

> 一句话：**制度层决定"有哪些信仰"，个人层决定"谁真的信、信成什么样"**。缺了个人层，信仰系统就只剩组织架构图。

---

## 5. 学派类型学（用户新增的核心需求）

`belief-system.type ∈ { religious, political, philosophical, secular }`，`school.schoolType` 必须与所属体系的 type 相容。

| 判定问题 | 宗教学派 | 政治学派 | 哲学学派 | 学术学派 |
|---|---|---|---|---|
| 信仰对象是超自然吗？ | 是 | 否 | 否 | 否 |
| 权威来自启示/经典吗？ | 是 | 否（纲领/领袖/历史叙事） | 部分（经典/论证） | 否（方法/证据） |
| 有礼仪与圣事吗？ | 有 | 无（有仪式性集会） | 通常无 | 无 |
| 有教职与处分吗？ | 有 | 有党纪 | 通常无 | 有学术规范 |

**可执行判据**（避免把政治派系误标成宗教学派）：

```
schoolType =
  if supernaturalObject && revelationalAuthority            -> religious-school
  else if ideologicalObject && (programAuthority || leaderAuthority) -> political-school
  else if rationalObject && argumentativeAuthority          -> philosophical-school
  else if methodicalObject && evidenceAuthority             -> academic-school
```

> 四个问题是**硬判据**，不是标签：一旦 `schoolType = political-school`，就**不能**挂 `liturgy`/`ordination`/`sacrament` 这类宗教子类。

---

## 5.6 CK3 1.20 实测对照（本机 `E:/SteamLibrary/.../Crusader Kings III/game/common/religion`）

CK3 1.20 的结构**已经不是老的四层**，而是多了一个 `rite` 层，且**机制挂在信条上**：

```
religion_family → religion → faith → rite
```

| 层 | 字段（摘自官方 `.info`） | 备注 |
|---|---|---|
| `faith` | `religion`（必填）、`color`/`icon`、**`religious_head`（title）**、**`head_of_rite`（title）**、`graphical_faith`、`theocracy_government_type`、`origin`（派生自哪个 faith）、`holy_sites`/`eminent_holy_sites`、`tenets`、`doctrines`（**附加式**）、`tenet_selection_pair`、`reserved_male/female_names`、`cultures`、`clerical_elector_titles`、`historical`、`main_rite`、`holy_order_names` | `main_rite` 指向脚本化的主礼仪；**主礼仪拥有核心信条**，faith 自带信条只作"动态创建 rite"的种子 |
| `rite` | `name`/`desc`/`icon`/`color`、**`founder`（title）**、`faith`（父）、`cultures`、`convert`/`create`/`history`、**`tenets`**、**`doctrines`**、`tenet_selection_pair` | **特权：同一 doctrine group 内，rite 覆盖 faith 覆盖 religion** |
| `tenet` | `icon`、`name`/`desc`、`visible`、`requires_dlc_flag`、`parameters`、**`piety_cost`**、`is_shown` | 官方注释原话：**"All unique faith mechanics are tied to tenets, rather than involving hardcoding on the religion itself."** |

**三条可直接借的结论**：

1. **`rite` 是 CK3 的礼仪层，不是我们的"传承"**：它挂在 **faith 之下**，持有核心信条 + 礼仪专属教义，有 `founder` 与 `head_of_rite`。我们的 `lineage`（传承）是 faith→父信仰的引用，两者不是一回事。
2. **`denomination`（宗派）不是层级，而是可选的组织覆盖层**：CK3 用 `religious_head`/`head_of_rite` 两个 title 指针表达"有没有管理机构"——这正好对上你说的"宗派有管理/追认意味、可有可无"。
3. **"个人化的信条"= 创立/改革信仰时选信条**：`tenet` 带 `piety_cost` 与 `is_shown`（何时可被选），玩家用**虔诚**支付来挑选信条——不是"每个角色有私人信条"。角色层只有 `piety`、`zealous/cynical`、virtues/sins、`devout` 类特质。

**修正后的层级**：

```
family → religion → faith → { tradition(rite) → school? } → custom
                          └ denomination = 可选组织覆盖（religious_head / head_of_rite）
```

---


## 5.7 CK3 个人层实测：`personal_tenet` 与 `spiritual_fulfillment` 是**两套独立系统**

> 更正我此前的错误说法："个人化信条 = 创立/改革信仰时买信条"。那句把**两套系统**混成一套。CK3 1.20 里它们分开：

### A. `personal_tenet`（个人信条，独立于创立信仰）

| 证据 | 含义 |
|---|---|
| `any_personal_tenet`（`ce3_achievements.txt:155`） | 存在"个人信条"这一**角色级集合**，可断言 |
| `has_personal_tenet_flag = tenet_vows_of_poverty_humble_host`（`chariot_race.txt:267`、`coronation.txt:622`、`debate.txt:512`、`feast.txt:916/1038/1104`） | 个人信条带**行为标志**，活动/仪式按它分支（如守贫者在宴会上不同） |
| `spread_personal_tenet_intent`（`feast.txt:753/758`） | 宴会里可**传播个人信条**——这是个人信条影响他人的通道 |
| `tenet_transubstantiation_feast_discount` | 个人信条也能给具体折扣（化体论→宴会花费） |

**要点**：个人信条是**角色级、可选、可传播**的东西，和"创立/改革一个 faith"完全不是一回事。

### B. `spiritual_fulfillment`（灵性满足，角色级 0–100 状态量）

| 证据 | 含义 |
|---|---|
| `MAX_SPIRITUAL_FULFILLMENT`（`ce3_achievements.txt:158` 断言 `>= 100`） | 有上限，是**刻度**不是布尔 |
| `change_spiritual_fulfillment = {miniscule/minor/major}_spiritual_fulfillment_value` | 由**活动与建筑**改变：朝圣（`pilgrimage.txt:2588`）、徒步（`hike.txt:110`）、托钵传教（`pam_mendicant_...:42`）、传播信条（`spread_tenet_apa.txt:63`）、神庙（`00_temple_buildings.txt:832`） |
| `has_spiritual_fulfillment_type = christian_fulfillment`（`pam_buildings.txt:104/186/268`） | 满足度**按宗教家族分型**（基督教型、其他型），不是全局一个数 |
| `spiritual_fulfillment <= spiritual_fulfillment_minus_3` …（`pam_wars.txt:84-119`） | **直接影响圣战 CB 的档位**（0–3 档），即灵性满足低的人打不了高合法性圣战 |
| `spiritual_fulfillment_pilgrimage_cost_mult`（`pilgrimage.txt:1086/1326`） | 反向也成立：低满足度让朝圣更贵 |

**要点**：灵性满足是**可积累、可消耗、有档位后果**的状态量，来源是**仪式性行为**，出口是**战争合法性/成就/成本**。

### C. 我们自己的代码：有这个量，但只通了一条线

| 现状 | 证据 |
|---|---|
| 字段存在，默认 50，clamp 0–100，入档 | `CharacterData.cs:85`；`CharacterSaveData.cs:89/231/511` |
| **唯一写入**：`UpdatePiety`（按"特质 vs 该 faith 的 virtues/sins"，德行 +0.01×分、罪行 −0.02×分、秘密信徒 −0.01/tick） | `CharacterManager.cs:102-118` |
| **唯一后果**：封圣要求 ≥80 | `CanonizationSystem.cs:42` |
| **没有来源**：无朝圣、无仪式活动、无传教加成——只有被动漂移 | 全仓无 `add/change spiritual` 类入口 |
| **没有个人信条**：`personal_tenet` 概念完全不存在 | grep 0 |

⇒ 我们做了一半：**有刻度、有一个出口（封圣）、没有来源、没有第二出口（如圣战合法性）**，而且**缺整个个人信条系统**。

---

## 6. 数据形状（可直接编码）

```
BeliefSystem { id, type(religious|political|philosophical|secular), name, parentId? }
ReligiousFamily { id, ultimacy, canonStructure, narrativeId }          ← 新增
Religion { id, familyId, assemblyMode(liturgical|lineage), creedId, doctrineIds[], canon{textIds,closed}, baseLiturgyId, polityId, orthodoxy{tenetIds,exclusions} }   ← 新增
Faith { id, religionId(必填), **lineageFaithId**(= 父信仰·传承), childFaithIds[], denominationId?, schoolId?, faithCreed, fervor, adherence }   ← **传播与分裂的单位**
Denomination { id, religionId, faithIds[], confessionId, liturgyVariantId, polityVariantId, initiationId, preceptIds[] }   ← **faith 自建的可选管理层**
School { id, ownerId(faith|denomination|beliefSystem), schoolType, praxisId, exegesisId, ritualVariantId, ruleId }
Order { id, religionId, ruleId, vows[] }
Custom { id, placeRef, customId, tabooIds[], idiomId }
Doctrine { id, tenets[{id, authority: tenet|dogma}], catechismId? }
Polity { officeIds[], magisteriumId?, ordinationId?, councilIds[] }
Liturgy { ritualId, calendarId, languageId }
Precept { matrimonyId?, tabooIds[], commandmentIds[] }
Penalty { penaltyKindIds[], procedureId }
PersonalFaith { faithId, devotion(0..100 = spiritualFulfillment), patron{kind,id}?, vows[], heterodoxy(0..1), isSecret, conversionHistory[] }   ← 个人层
```

**经核证补充的 5 个属性**（见 `research/RELIGION_FACTS_VERIFIED.md`）：

| 属性 | 取值 | 依据 |
|---|---|---|
| `schisms[]` | 多条**独立**分裂，各带 `cause ∈ {christology, governance, lineage, inheritance, authority, politics}` | 431 与 451 是两条不同裂线；东方教会未参加两次公议 |
| `ordination.kind` | `sacramental` / `license` / `appointment` / `none` | 逊尼无按立但有 **ijāza 授教许可**；巴哈伊完全没有 |
| `magisterium.scope` | `legal` / `doctrinal` / `both` | 什叶 taqlīd 只管教法，"信念之事"禁止效仿 |
| `magisterium.plural` | `bool` | 马尔贾可多位并存（2023 在世逾 50），无选举无任命 |
| `canon.living` | `bool` | 锡克《古鲁·格兰特·萨希卜》即"永恒古鲁"，人格式传承 1708 终止 |
| `stateRelationHistory[]` | 时间序列 `{fromDay, relation}` | 神道 1871 编入行政 → 1945 切断国家赞助 → 1946 转民间法人 |

**必答/可选**：`faith.religionId` 必填（宗教必挂）；`faith.lineageFaithId` 一般必填（默认指向创建者所承的父信仰）；`religion.assemblyMode` 必填（决定分裂长在哪一层）；`denominationId`/`schoolId` 可空。

---

## 7. 与现有代码/内容的对位

| 现有 | 内容量 | 对位到新模型 | 缺口 |
|---|---|---|---|
| 现有 | 量 | 对位到新模型 | 缺口 |
|---|---|---|---|
| `Religions.json` | **44** | **→ 用户口径的 `faith`**（可玩单位）——代码把 `religionId` 直接当 `faithId` 用（`GameWorld.Core.cs:966`），并给每个 id 建一个 `FaithSystem` | **它上面的 `religion`/`family` 两层完全不存在**；44 条里缺 `religionId`/`lineageFaithId`/`denominationId` 归属字段 |
| `Doctrines.json` | **46** | → `Doctrine` 的 tenets（部分） | 与"类型×领域"两轴表对不上，需重排 |
| `Tradition`（`TraditionDef`） | **13**（`factors` **全空**） | ⚠ **是"文化传统"，不是信仰系统的传承** | 命名冲突：代码侧改 `CulturalTradition` |
| `Heritage`（`HeritageDef`） | 存在 | ⚠ **是"遗产"（文化/族群），与信仰无关** | 不要挪进信仰系统 |
| `Ethos` | **8** | → `family.narrative` / `ultimacy` 候选 | 零消费者 |
| `ReligionCatalog` | — | 加载 44 → `FaithSystem`，**无层级** | 无 religion/family 聚合 |
| `FaithSystem` | 每 faith 一个 | `churchWealth/missionaryZeal/tolerance/faithRelations` | 四字段**全无消费者**（E 域 P2） |
| `CultureData.faithId` | 仅 Laethis（值 `-1`） | → faith 附着点 | 悬空引用 |
| `RealmData.stateReligionId` | 默认 −1，**无写入** | → 政权国教 | 五条链同死（E 域 P0） |
| `School` / `Denomination` / `Order` / `Deity` / **`religion` 层** | **不存在** | 需新建 | 五张新表 |

**结论（最重要的一条）**：**代码已经把 `faith` 当可玩单位了**——44 条 `Religions.json` + 每 id 一个 `FaithSystem`，这正是用户说的"最低可玩单位"。缺的是**它上面的 `religion`/`family`（必挂层）**与**`denomination`（可选管理层）**，以及 `lineageFaithId`（传承）字段。所以不是"重造信仰系统"，而是**在已有的 faith 上补归属与传承**。

---

## 8. 与 D8（信仰模型待裁决）的关系

D8 原三选一（E1 代码为准 / E2 文档为准 / E3 折中）可以借本草案收口：

- **E3+（建议）**：**`faith` 为可玩单位（代码已有 44 条）→ 上补 `religion`（必挂）与 `family`，下补 `denomination`（可选）与 `tradition`（≈rite 基底）**。
- 这样 `FAITH_AS_CULTURE_MODEL` 的"传统有规范权威"落在 `tradition` 基底上，"宗教身份必填"落在 religion 必挂上，两者不再冲突。
- E1 会让 tradition/denomination 彻底退化；E2 需重造 44 条内容，代价最大。

---

## 9. 与设计原则的对账（防抽象数值）

信仰系统同样受宪章原则 5 约束：**不许"信教 +5% 稳定"这类加成**。信仰必须通过可指认环节起作用：

| 环节 | 具体机制 |
|---|---|
| 合法性 | 国教 → 统治者合法性来源；异端 → 合法性缺口 |
| 征收 | 教会财产（`churchWealth`）与什一税 → 实际转移支付 |
| 动员 | 圣战号召 → 征召能力（走 `PlanType.Military`） |
| 合规 | 戒律与法律冲突 → 合规度惩罚（走法令传播五档） |
| 组织 | 教职任命 → 官职与头衔（polity→office） |

---

## 10. 这套结构为什么能容纳"宗教之间的差别"

差别不靠给每个宗教写特例，而靠**差异落在哪个维度**。同一套 schema，不同宗教把重量放在不同格里：

| 宗教类型 | 主要差异维度 | 次要维度 | 数据表现 |
|---|---|---|---|
| 基督教（自下而上） | `liturgy` 多礼并存 + `denomination` 追认 | `polity`（教座/主教团） | `assemblyMode=liturgical`；多 denomination 共享 religion；分裂多在治理 |
| 佛教（自上而下） | `lineage`（法脉/统绪） | `school`（praxis/exegesis） | `assemblyMode=lineage`；分裂在传承；school 承载解释之争 |
| 逊尼伊斯兰 | `magisterium` 无职位的学者权威 | `precept`（可裁判的法） | `ordination.kind=none`；`authoritySource=learning` |
| 什叶十二伊玛目 | `magisterium.plural`（多马尔贾并存） | `magisterium.scope=legal` | 训导权多中心、只管教法 |
| 锡克 | `canon.living`（经书即古鲁） | `polity`（Khalsa 武装共同体） | 人格式传承 1708 终止，权威转向文本 |
| 神道 | `stateRelationHistory`（官办→民间法人） | `polity`=公职 | 同一信仰跨越政教关系的制度切换 |
| 印度教 | `looseAggregate`（无单一 creed/canon） | `school`/sampradaya | 不强制填 creed；以师承与地方神祇组织 |
| 民间/灵媒型 | `authoritySource=efficacy` | 低组织度（无 polity） | 允许无教职、无正典 |

**判据**：写内容时先问"这个宗教的差别在哪一格"，只要落进上表任一行，就不需要新代码；落不进去，才是模型缺维度（例如 `succession=reincarnation` 就是补进去的）。

---

## 11. 宗教组织：修会 / 隐修会 / 军事修会 / 教团

组织层**不新建一套组织系统**——交给**头衔 + 实体**：每一座院/会院是一个实体（可持地、可持头衔），会长/院长是**官职头衔**，会规是**内容（rule）**。这里只固定"四种组织怎么区分"。

| 类型 | 会规 | 隐居 | 武装 | 在俗事工 | 财产 | 上级 | 典型 |
|---|---|---|---|---|---|---|---|
| **修会** `monastic` | 有（日课/清规） | 部分 | 无 | 有限 | 修院共有 | 院长 → 总会长 | 本笃会、戒律院 |
| **隐修会** `contemplative` | 有（更严：静默/断食/闭院） | **是**（闭院） | 无 | **无** | 修院共有（常极简） | 院长 | 嘉尔笃会、贫穷修女会 |
| **军事修会** `military` | 有（含战斗义务） | 无 | **是** | 有（战争/医务/护路） | **修会领地**（可成政权） | 大团长 | 圣殿、医院、条顿 |
| **教团** `congregation` | 有（使命导向） | 无 | 无 | **是**（传教/教育/医疗/学术） | 团体或会省所有 | 总会长 → 会长 | 耶稣会、道明会 |

**四类不是四个类，而是一个实体 + 四个布尔特征**（现实里会混：军事修会也守修道规则，教团也可托钵）：

```
ReligiousInstitute {
  id, name,
  instituteType ∈ { monastic, contemplative, military, congregation }
  traits { cloistered, armed, apostolic, mendicant }   ← 与 type 必须自洽
  ruleId              // 会规（内容）
  faithId / religionId // 服务哪个信仰/宗教
  superiorTitleId     // 院长/总会长/大团长 → 官职头衔
  houses[]            // 各院：实体 + 可持地头衔
  propertyOwner ∈ { institute, house, individual, none }
  exemption           // 是否豁免于主教管辖（真实制度关键）
  filiation           // 母院 → 子院（传承链，借用 lineage 结构）
  apostolate[]        // 传教/教育/医疗/学术（教团才有）
  armedForces[]       // 军事修会才有的兵源/要塞
}
```

**自洽规则（写内容时必须过）**：

| 若 `instituteType` = | 则必须 | 则必须不为 |
|---|---|---|
| `monastic` | `ruleId` 非空 | `armed` |
| `contemplative` | `cloistered = true` | `armed`、`apostolic` |
| `military` | `armed = true`、`superiorTitleId` 为军事头衔 | `cloistered` |
| `congregation` | `apostolic = true` | `cloistered` |

**托钵修会**（方济各、道明）不是第五类：它是 `traits.mendicant = true` 的 `monastic` 或 `congregation`——托钵是**财产制度**（不置产、靠施舍），不是组织形态。

**与头衔/实体的接法**：
- 院长/总会长/大团长 = `TitleInstance`（office 模块），与官职三套并行的清理同批做；
- 每一座院 = 一个实体（`HabitationSite`/`Anchor` 级）+ 可持 `TitleInstance`；
- 军事修会的领地 = 政权级头衔（条顿骑士团国就是用这个表达的）；
- 会规 = 内容 `rule`，与 `precept`（戒律）分开：会规是**组织内部纪律**，戒律是**信仰伦理**。

---

## 12. 非独立组织与特许权（商会/兄弟会/会社，一把解决）

宗教组织不特殊——**所有"非独立组织"共用一套模型**：实体 + 首领头衔 + 章程 + 财产 + **特许权**。差别在 `orgType` 与 `purposes`，不在机制。

### 12.1 组织类型（一套字段，多型取值）

| `orgType` | 中文 | 成员基础 | 目的 | 典型 |
|---|---|---|---|---|
| `religious-institute` | 修会/隐修会/军事修会/教团 | 誓愿 | 修行/传教/医务/战斗 | 本笃会、圣殿、耶稣会 |
| `guild` | 商会/行会 | 行业准入 | 垄断、价格、学徒制、互助 | 汉萨、布商行会 |
| `confraternity` | 兄弟会/善会 | 自愿入会 | 互助、丧葬、慈善、共同祭祀 | 各城善会 |
| `academy` | 学会/书院 | 学识 | 研究、教育、出版 | 科学院、书院 |
| `society` | 秘密会社 | 秘密引入 | 政治/宗教/互助（常非法） | 各类密社 |
| `company` | 特许公司 | 股份 | 贸易、殖民、武装 | 东印度公司 |

共同字段：

```
Organization {
  id, name, orgType,
  charterId            // 章程（内部规则；由 rule 内容承载）
  headTitleId          // 会长/院长/大团长/社长 = 官职头衔
  branches[]           // 分会/分号/各院 = 实体
  propertyOwner ∈ { org, branch, individual, none }
  privileges[]         // 特许权 id
  dependency ∈ { autonomous, semi, dependent }   // 自治程度（非独立组织 = 不持主权）
  purposes[]           // 宗教/商业/慈善/学术/军事/政治
  membership           // 准入（血统/行业/誓愿/选举/秘密引入）
}
```

### 12.2 特许权（Privilege）——统一对象

**豁免就是特许权的一种**，不单做机制：

```
Privilege {
  id, grantor(EntityRef), grantee(EntityRef),
  kind ∈ { exemption(豁免管辖), monopoly(独占), taxation(征税),
           jurisdiction(司法), arms(武装), mint(铸币),
           selfElection(自选首领), property(置产), passage(通行), toll(收费) },
  scope,              // 范围：领地/行业/人群
  sinceDay, expiresDay?, revocable
}
```

**已有代码碎片，正好收进来**：

| 现有 | 位置 | 收编为 |
|---|---|---|
| `RealmLawState.AddCharterHolder/IsCharterHolder` | `RealmLawState.cs:108/114/130` | `Privilege{kind=selfElection 或 jurisdiction}` 的一种 |
| `ProposalAuthority.CharterHolders` | `ProposalAuthority.cs:13` | 特许权持有者的立法权 |
| `LegalNormKind.Charter`（"特许/特权，有明确范围与覆盖声明"） | `LegalNormKind.cs:9` | 特许权的**规范类型** |
| `LocalSuccession.CityCharter` | `LocalSuccession.cs:9` | 城市特许状（继承/自治） |
| `TreatyClauseType.TradePrivileges` | `TreatyClauseType.cs:20` | 条约授予的贸易特许 |
| `ContractType.Charter` | `ContractType.cs:5` | 契约层面的特许 |
| `BondType.SwornBrotherhood` | `BondType.cs:6` | 兄弟会的誓约关系 |

### 12.3 三条规则

1. **特许权是"法律的例外"**：它挂在 `LegalNormKind.Charter` 上，有授予者、范围、期限、可撤销性 → 与法律生命周期（`LAW_LIFECYCLE.md`）同一套位阶与冲突规则；被撤销/被侵犯可成诉讼或战争理由。
2. **非独立 = 不持主权**：组织可持地、可持头衔、可持特许权，但**不持政权头衔**；唯一例外是军事修会成领地政权（条顿骑士团国）——那时它同时持政权头衔，走 S16。
3. **组织不发明新系统**：它 = 实体（院/分会）+ 头衔（首领）+ 内容（章程）+ 特许权（豁免/垄断/司法）+ 关系（母会→分会，复用 lineage 结构）。

---

## 13. 神祇、万神殿与主保（Deity / Pantheon / Patronage）

> 依据：用户 §4.2.2 / §4.2.3。核心判断：**神祇不是凭空出现的，也不是靠数值加成的**——它有起源、有谱系、有降格与消亡，并且与"主保"是同一套结构在不同宗教复杂度下的两种表现。

### 13.1 神祇的起源阶梯（**修正**：不是"一族一神"）

| 阶段 | 形态 | 数据表现 |
|---|---|---|
| 部落 | **精灵/祖先/地方神并存**；守护神只是其中一位，不一定唯一 | `origin=tribal`；`pantheonId=null`；`rank=guardian`；常有多个 deity 共存 |
| 部族/城邦 | 部落融合 → **大部族/城邦守护神**（城邦各有主神）；融合常带来多神并存而非一神 | `origin=city`；出现 `pantheonId`；`rank ∈ {patron, major}` |
| 万神殿 | 诸神被编入**谱系**，有上下位、职司、亲属关系 | `rank ∈ {supreme, major, minor, subordinate}`；`parentDeityId` 成树 |

> 修正理由：**"最初一个部落就有一个神祇"是 19 世纪进化论的整齐化说法**。民族志里更常见的是祖先崇拜、地方精灵、图腾与守护神混杂；"一神"只在特定条件下出现（如迁徙/战争中的单一部族神上升为国神）。模型不能假定"一族一神"，否则原始宗教会被迫长成小号一神教。

### 13.2 神祇的**五种**结局（原稿四种，缺"吸纳"）

| 结局 | 含义 | 数据 | 触发条件 |
|---|---|---|---|
| **消亡** | 崇拜断绝、名号不传 | `cult=extinct` | 征服彻底 + 无祭司阶层承接 + 无圣地留存 |
| **降为下位** | 被编进征服者神谱，成下属/眷属/职能神 | `status=subordinate`，`parentDeityId` 指向新主神 | 双方都有万神殿结构（可容纳） |
| **等同/同化** | 被认定为征服者某神的地方化身（`interpretatio`） | `status=identified`，`identifiedWith=deityId` | 两神职司相近、有祭司阶层愿承接 |
| **吸纳为主保** | **神庙变教堂、神祇变主保圣人**——一神教传播的文化适应 | `status=sanctified`，生成 `Saint{patronage=旧神职司}` | 强一神论 + 需要保留地方崇拜土壤 |
| **妖魔化** | 仍是"神"，但性质转恶（恶神/魔） | `status=demonized` | 一神教或强二元宗教征服，且**不愿**保留其崇拜 |
| **剥离神性** | 不再是神：伪神/偶像/精怪/巨人/仙人 | `status=de-deified` | 强一神论 + 需要否定其神格 |

> **这里原稿自相矛盾**：§4.2.2 只给"消亡/降下位/妖魔化/剥离神性"四种，但 §4.2.3 说"神庙神祇→城市主保圣人"。若没有"吸纳为主保"这一条，§4.2.3 的机制没有入口。补上后两条线才接得上。
> **神格与崇拜要分开**：一位神可以"神格仍在、崇拜已衰"（`status=active` + `cult=decayed`），也可以"神格被否定、崇拜转为民间"（`status=de-deified` + `cult=folk`）。合成一个 `status` 会丢掉这类真实情形。

### 13.3 万神殿（Pantheon）

```
Pantheon { id, religionId, deities[], ordering, synthesisEvents[] }
Deity {
  id, name, origin ∈ {tribal, city, imported, apotheosized},
  rank ∈ {supreme, major, minor, subordinate},
  role ∈ {guardian, patron, dynastic, occupational},   // role 与 rank 分开
  domains[], parentDeityId?, pantheonId?,
  status ∈ {active, subordinate, identified, sanctified, demonized, de-deified},
  cult   ∈ {active, decayed, forbidden, extinct, folk},   // 崇拜状态独立
  patronage[], cultSites[], feastDays[],
  sinceDay, endedDay?
}
```

**编合（synthesis）是事件，不是自动合并**：`synthesisEvents[]` 记录"某神在某年被编入某谱系/降为下位/被等同/被吸纳为主保"，可被编年史引用、可被后世翻案。

### 13.4 主保（Patronage）是三教共用的**结构**；封圣只是其中一条**程序**

```
Patronage { patronRef(Deity|Saint|Ancestor|Spirit), domain ∈ {city, guild, group, place, person}, obligations[] }
```

- **多神教**：主保 = 神祇护佑城邦/行业（`Deity.patronage`）；**"纳入万神殿"是主保授予的一种结果**。
- **一神教**：主保 = 圣人护佑城市/行业/群体（`Saint.patronage`）；**"封圣"是主保授予的一条程序**。
- **原始宗教**：主保 = 祖先/地方精灵。

> **术语修正（原稿越界）**：不要说"封圣也适用于多神教"。共用的是**主保授予**这一结构；`封圣`（canonization）是一神教里由权威主持的**特定程序**，多神教没有"封圣"，只有"纳入万神殿/立为主保"。术语严格，机制才不混。

**结构延续 ≠ 人格延续**：一神教继承的是**功能位**（主保、节庆日、圣地位置、庇护关系），多数圣人并非旧神的改名。所以写"主保圣人制度是万神殿传统的衍生"时必须限定为**功能与制度层面的延续**，不能写成"旧神换了个名字"。

### 13.5 封圣（Canonization）——流程，不是判定

```
local cult（地方崇拜）→ 灵验（miracle/灵迹记录）→ 社群请愿 → 权威批准（宗教领袖/主教会议/教座）
```

数据：`Saint { id, personId, feastDay, patronage[], recognizedByFaithIds[], canonizedDay, cultSiteId? }`

**代码现状**：`SaintDef.canonizationPiety = 80`、`CanonizationSystem.cs:42` 要求 `spiritualFulfillment ≥ 80`——只做了"权威批准"这一道阈值，**缺前三步**（地方崇拜、灵验、请愿），也缺 `patronage` 与 `feastDay`。所以现在的"封圣"是"虔诚够 80 就自动成圣"，不是流程。

### 13.6 复杂度分级（同一框架，三种实现度）

| 类型 | 神祇 | 神谱 | 教统/教职 | 圣地 | 传教 |
|---|---|---|---|---|---|
| 原始宗教 | 有（精灵/祖先/守护神并存） | **无或不全** | 无专职或极简 | 自然场所 | 弱 |
| 多神教 | 有（万神殿） | **有** | **有教职等级，但无跨地域教义权威**（埃及神庙祭司、罗马祭司团、神道社格） | 神庙 | 中 |
| 一神教 | 有（唯一神）+ 圣人 | 无（否定多神谱系） | **有跨地域教阶与教义权威** | 圣所/朝圣 | 强 |

> 修正：原稿说"多神教通常没有教统"过强。多神教**有教职与等级**（神庙经济、祭司团、社格制度），缺的是**跨地域的教义裁决权**——这正是 `magisterium` 与 `office` 的区别。

**同一套字段**：`pantheonId=null` 即原始宗教；有万神殿即多神教；`denomination`/`magisterium` 复杂化即一神教——不需要三套系统。

### 13.7 对原稿的五点修正（逐条）

| # | 原稿 | 问题 | 改为 |
|---|---|---|---|
| 1 | "最初一个部落就有一个神祇" | 19 世纪进化论式整齐化；民族志里多为精灵/祖先/地方神并存 | 部落阶段**多神并存**，守护神只是其中一位 |
| 2 | 征服只有四种结局 | 与 §4.2.3"神庙→主保圣人"矛盾，缺入口 | 补 **等同（interpretatio）** 与 **吸纳为主保（sanctified）**，共六种 |
| 3 | 用单一 `status` 表达 | "神格仍在但崇拜已衰"这类真实情形会被吞掉 | **神格状态** 与 **崇拜状态** 分开 |
| 4 | "封圣也适用于多神教" | 术语越界：封圣是一神教的权威程序 | 共用的是**主保授予**；封圣是其中一条程序 |
| 5 | "多神教没有教统" | 过强：埃及/罗马/神道都有教职等级 | 多神教缺的是**跨地域教义裁决权**，不是教职 |

---

---

## 14. 学术辨析与四轴修正（见 `research/RELIGION_SCHOLARSHIP.md`）

前沿文献体检后，模型**骨架站得住**（faith 作传播/分裂单位、lineage 作传承、denomination 承载主流/分歧、Patronage 统一神祇与圣人），但要解掉**基督教语法**，补三个轴：

| 轴 | 取值 | 解掉什么 |
|---|---|---|
| `differentiation` | `differentiated` / `embedded` | "每个宗教都有独立制度领域与必答 creed"——embedded 型（如中国民间信仰）的 creed/canon 由家族/国家借用 |
| `exclusivity` | `inclusive` / `henotheistic` / `exclusive` | 把"一神教才妖魔化"从硬编码变成推导：exclusive 不能 `identified`，inclusive 不能 `de-deified` |
| `ritualFrequency` / `ritualArousal` | 两个 0–1 浮点 | 把"多神教没有教统"的刻板换成可算的传播模式（高频率低唤起 → canon/magisterium；高唤起 → 地方凝聚强、跨地域弱） |
| `churchState` | `sacral-kingship` / `caesaropapist` / `dualist` / `priestly` / `autonomous` / `embedded` | 把"政教合一"这个粗布尔拆成六种：神圣王权≠王权统教≠祭司统治。后果：合法性来源、能否任命教职、绝罚能否废黜君主、教法是否即国法、教产豁免是否成立 |

**另两条纪律**：① 分类是 **etic**（研究者标签），不能让它直接驱动机制；② 宗教市场论与"圣人取代神"只能当**假设**，要有可证伪断言，不当默认规则。

---

## 15. 待确认（少量，且是真问题）
1. **神观档位**：一神/多神/泛神/万有在神/非神/不可知/无神——要几档？（现有 CK3 对照表只有"一神/多神"两档，太粗）
2. **正典是否可开合**：`canon.closed` 一旦 true 是否永久（历史上有"正典封闭"事件）？
3. **学派的政治性**：政治学派是否允许"有党纪与处分"（这会让它更像宗派）？还是政治体系一律无 `penalty`？
4. **学术师承是"规范"还是"影响"**：若只是影响，非宗教轨不需要 `lineage.normativeAuthority`。
