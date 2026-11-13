# 有组织宗教的结构原型（研究底稿 v1）

> 2026-11-09 04:00。用途：为 `BELIEF_SYSTEM_MODEL.md`（S18）提供**跨宗教的结构样本**，检验模型字段是否够用、是否把亚伯拉罕结构硬套到所有宗教上。
> 方法：以"**权威怎么产生、怎么继承、怎么分财、怎么处理异见**"四个问题切，而不是以教义内容切。每个原型给案例、结构特征、对模型的检验点。
> 引用纪律：本文第一版基于通行宗教史常识；标注 ⚠ 的条目需引用核实（已派子任务核）。

---

## 0. 提取什么（比较用 schema）

对每个案例抽 10 项，缺项写"无"而不是猜：

| 项 | 问法 |
|---|---|
| `authoritySource` | 权威来自**职位**、**血统**、**学识**、**法脉**、**神启**、**选举**、**灵验**，还是**会众契约**？ |
| `succession` | 上一任怎么产生下一任：任命 / 选举 / 血缘 / 法脉印可 / 转世认定 / 共遴选 / 买官？ |
| `officeLayers` | 有几级教职？是否跨政权？ |
| `magisterium` | 谁有权判"什么是正统"？是职位、议会、学者共识，还是无？ |
| `canon` | 正典是封闭文本、开放文本，还是"活的权威"（如经典人格化）？ |
| `liturgyBase` | 礼仪是**地方基底**（多礼并存）还是**统一礼**？ |
| `initiation` | 入教/入团是洗礼式一次，还是**分级秘密**？ |
| `precept` | 戒律是伦理劝诫、还是可裁判的法（教法/哈拉卡）？ |
| `property` | 教会财产归**教座 / 修会 / 会众 / 国家 / 个人**？ |
| `stateRelation` | 国教 / 自治 / 从属 / 竞争 / 被禁？ |
| `sacredCenter` | 圣地/朝圣/圣徒/圣物是否构成独立权威来源？ |

---

## 1. 十种结构原型 × 案例

### 原型 A：礼仪基底 + 追认型（liturgical-ratification）

**案例**：罗马天主教、东正教（君士坦丁堡与各自主教会）、东方正统教会（科普特、亚美尼亚、叙利亚）、东方教会（"景教"）、圣公会。

- 结构：**先有多套地方礼仪**（罗马礼、拜占庭礼、亚历山大礼、安条克礼、加色丁礼…），教会/教座在礼仪之上做**追认与管辖**。
- `authoritySource` = 职位（主教/牧首）+ 使徒统绪（法脉合法性）。
- `succession` = 任命 + 统绪传递；`magisterium` = 主教团/教座（天主教后期强化为教宗首席权）。
- `liturgyBase` = **多礼并存**（这正是"自下而上"的证据）。
- `stateRelation` = 历史上从国教到自治到被压制都有。
- **对模型检验**：`assemblyMode=liturgical` 成立；`denomination` 应能表达"共融但分立"（如东方教会与东正教互不共融），而不只是"上下级"。

### 原型 B：法脉传承型（lineage-transmission）

**案例**：禅宗（法脉印可）、藏传佛教（法脉 + 转世）、上座部（僧团戒律 + 传承）。

- 结构：**传承链先定**，宗派与学派从链上分出；戒律（vinaya）是共同基底。
- `succession` = 印可/传法（禅）、**转世认定**（藏传）、戒腊资历（上座部）。
- `magisterium` = 无中央；由**戒律共同体**（僧团）与**师承**共同裁定。
- `canon` = 封闭（三藏），但解释传统开放。
- **对模型检验**：`assemblyMode=lineage` 成立；需要新字段**`successionByReincarnation`**（转世认定是"继承"的一种，模型没有）；宗派分立应以"法脉是否同源"判定，而非"教义差异"。

### 原型 C：学者—法理型（jurist-scholarly）

**案例**：逊尼派（乌莱玛/教法学家）、什叶派十二伊玛目（马尔贾/效仿源）、犹太教拉比。

- 结构：**无中央教阶**；权威来自**学识与共识**（ijmāʿ），不是职位。
- `authoritySource` = 学识声誉；`magisterium` = 学者共识（不是职位）。
- `succession` = 师承 + 学派认可；没有"任命"。
- `precept` = **可裁判的法**（fiqh / halakha），不是劝诫。
- **对模型检验**：模型若把 `magisterium` 建成 `polity` 子类（职位），就**表达不了"无职位的训导权"**。→ 需把 `authoritySource` 独立成字段。

### 原型 D：会众契约型（congregational）

**案例**：新教各派（路德、加尔文、浸信、五旬节）、巴哈伊（选举制、无教士）。

- 结构：**会众拥有财产、选聘牧师**；教派之上无强制管辖。
- `authoritySource` = 会众契约 + 经典（sola scriptura）。
- `property` = **会众所有**（与 A/B/C 全不同）。
- `succession` = 选举/聘邀，可解聘。
- **对模型检验**：需要 `property` 字段与 `polity.congregational` 子类；`ordination` 仍存在但不构成等级。

### 原型 E：教阶—修会型（hierarchical-monastic）

**案例**：本笃会、方济各会、耶稣会、Cross 修会等。

- 结构：**修会叠加在教阶之上**，有自己的会规（rule）、会长、财产、豁免权。
- `authoritySource` = 会规 + 会长任命；与主教权有张力（豁免）。
- `property` = **修会所有**（不是教区）。
- **对模型检验**：`order` 不能只是 `school` 的同义词——它有**财产、成员、上级、豁免**。模型里 `order` 需要独立子结构。

### 原型 F：公职祭祀型（civic-cult）

**案例**：罗马国教、希腊城邦祭祀、神道（神社/氏神；近代国家神道）。

- 结构：**祭司是公职**（罗马的 augur/pontifex 是政治履历的一部分）；祭祀是**公共事务**。
- `authoritySource` = 政治职位；`polity` 与政权**同构**。
- `magisterium` = 元老院/国家（判"何为合法祭祀"）。
- **对模型检验**：模型必须允许 `religion` 与 `polity` **重合**（政教合一不是特例，是原型之一）；`office` 应当能引用政权官职（`TitleInstance`）。

### 原型 G：秘密入教型（initiatic）

**案例**：密特拉教、诺斯替各派、伊斯玛仪派（等级入教）、部分神秘教团。

- 结构：**分级入教**（如密特拉七阶），知识按级披露。
- `initiation` = **多级**，不是一次性。
- `canon` = 秘密口传 + 少量文本。
- **对模型检验**：`initiation` 需要 `grades[]`；`canon` 需要 `esoteric` 标记（不是所有人都能读）。

### 原型 H：军事教团 / 武装教派（military-order）

**案例**：锡克教（Khalsa 军事化共同体）、十字军修会（圣殿/医院骑士团）、日本僧兵。

- 结构：**宗教共同体同时是军事组织**；成员身份=战斗义务。
- `precept` 与 `polity` 强耦合（戒律含武装义务）；`property` 常为领地。
- **对模型检验**：`order` 需能挂军事属性（兵源、装备、领地）——这与 S8 军事域接。

### 原型 I：灵媒—附体型（mediumistic）

**案例**：西非/加勒比（Vodun、Candomblé、Santería）、东北亚萨满、部分民间信仰。

- 结构：**灵媒/附体**是权威与知识的主要通道；组织松散、以"家户—社群"为单位。
- `authoritySource` = **灵验**（有效即权威），不是职位或学识。
- `canon` = 口传为主；`initiation` = 附体/入门。
- **对模型检验**：需要 `authoritySource = efficacy`；且模型必须允许**低组织度**宗教（不强制 `polity`）。

### 原型 J：种姓—法型（caste-dharma）

**案例**：印度教（无单一 creed、无中央、多神/一神/非神并存；sampradaya 师承）。

- 结构：**没有单一"宗教"边界**；以 `dharma`（法）、种姓、师承（sampradaya）、地方神祇组织。
- `canon` = 多经典并存（吠陀、史诗、往世书），无封闭正典。
- `magisterium` = 无；由**种姓会议/地方权威**裁量。
- **对模型检验**：这是最硬的反例——**不能用"一个 religion = 一个 creed + 一个 canon"**。模型需要 `looseAggregate` 类型（宗教作为**家族级聚合**，无单一信条）。

---

## 2. 原型 → 模型字段的检验结果

| 原型 | 模型已能表达 | 模型表达不了（需补字段） |
|---|---|---|
| A 礼仪追认 | `assemblyMode=liturgical`、`denomination`、`tradition` 基底 | "共融但分立"的关系（communion 不是隶属） |
| B 法脉传承 | `assemblyMode=lineage`、`tradition` 作为传承基底 | **转世认定**（`successionByReincarnation`）；法脉是否同源的判定 |
| C 学者法理 | `magisterium`、`doctrine` | **无职位的训导权**（`authoritySource=learning`）；`precept` 作为可裁判法 |
| D 会众契约 | `polity`、`initiation` | `property=congregation`；可解聘的任职 |
| E 教阶修会 | `order`、`polity` | 修会的**独立财产/上级/豁免**（`order` 要有自己的 polity 与 property） |
| F 公职祭祀 | `polity` | **政教重合**（religion 与政权同构）；祭司=官职 |
| G 秘密入教 | `initiation` | **分级入教**（`grades[]`）；正典的 `esoteric` 标记 |
| H 军事教团 | `order`、`precept` | 军事属性（兵源/装备/领地），需接 S8 |
| I 灵媒附体 | `authoritySource` 若存在 | **灵验型权威**（`efficacy`）；允许低组织度（无 polity） |
| J 种姓法型 | — | **`looseAggregate`**（无单一 creed/canon 的宗教）；`dharma`/种姓作为法源 |

**净结论**：现有模型（faith/religion/denomination/tradition/school + 四支制度）**能覆盖 A/B/C/D/E 五型**，对 F/G/H/I/J 需要补 6 个字段或类型：

1. `authoritySource ∈ {office, charisma, learning, lineage, revelation, election, efficacy, congregational}`
2. `succession ∈ {appointment, election, hereditary, lineage, reincarnation, co-option, purchase}`
3. `property ∈ {see, order, congregation, state, individual, none}`
4. `stateRelation ∈ {state, autonomous, subordinate, rival, suppressed}`
5. `initiation.grades[]` + `canon.esoteric`
6. `looseAggregate`（宗教作为无单一信条的聚合，印度教/民间信仰/神道）

---

## 3. 三条"不要硬套"的警告

1. **不要把"宗教"当成有单一信条的实体**：印度教、神道、民间信仰都是聚合。模型必须有 `looseAggregate`，否则会逼着内容作者编造一个它们没有的 creed。
2. **不要把"训导权"等同于"职位"**：逊尼乌莱玛、犹太拉比都没有教阶，却有强训导权。`magisterium` 必须允许**无职位**。
3. **不要把"分裂"都归因于教义**：基督教分裂多在治理（A 型），佛教分裂多在法脉（B 型），伊斯兰分裂在继承—权威（C 型）。分裂的**判定字段应当不同**。

---

## 4. 已核事实（8 条，见 `RELIGION_FACTS_VERIFIED.md`，19 条来源 URL）

| # | 结论 | 对模型的影响 |
|---|---|---|
| 1 | **431 与 451 是两条不同的裂线**：东方教会（景教）**没有参加**两次公议，其神学定型于 484 Beth Lapat 与 612 Babai 会议；451 迦克墩造成的是**东方正统教会**（埃及/叙利亚/亚美尼亚）分离 | **必须支持"多条独立分裂"，且每条的成因不同**；不能用一个 parent→child 树表达 |
| 2 | 转世认定：13 世纪噶玛噶举起系统化；程序 = 寻访/占卜/神谕/湖观 → 预兆与前生记忆 → 辨认遗物 → （必要时）抽签 → 坐床 | `succession=reincarnation` 成立，且是多步程序（可挂 Plan） |
| 3 | 逊尼**无按立、无圣事**；乌莱玛是**学者阶层**（madrasa + 师承 + 教师授予的 ijāza），国家任命只是行政职位 | `ordination.kind = license`（不是授圣职）；`authoritySource=learning` |
| 4 | 马尔贾承认**非正式**、可多位并存（2023 在世逾 50）、无选举无任命；taqlīd 只在**教法事务**上成立，"信念之事"禁止 taqlīd | `magisterium.plural=true` + `magisterium.scope=legal`（不覆盖教义） |
| 5 | 锡克 1708-10-20 Nanded 宣言"Guru Maneyo Granth"，**人格式古鲁传承终结** | `canon.living=true` 且 `succession` 在 1708 后改为经书权威 |
| 6 | 神道：1871 社格制度编入行政 → 1945 SCAPIN 448 切断国家赞助 → 1946 神社本厅转民间法人、社格废止 | `stateRelation` 必须是**时间序列**，不是静态字段 |
| 7 | 印度教无中央权威、无单一正典；sampradaya 是师承结构 | `looseAggregate` + `tradition` 作为师承基底 |
| 8 | 巴哈伊无教士、选举治理 | `authoritySource=election` + `ordination=none` |

> 因此模型新增 5 个字段/属性：`schisms[]`（多条独立分裂，各带成因）、`ordination.kind ∈ {sacramental, license, appointment, none}`、`magisterium {scope, plural}`、`canon.living`、`stateRelationHistory[]`。
