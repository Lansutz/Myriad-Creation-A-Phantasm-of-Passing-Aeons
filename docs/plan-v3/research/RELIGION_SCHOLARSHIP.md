# 宗教系统：学术辨析（前沿文献对照）

> 2026-11-09 16:50。用途：拿近二十年的宗教研究去体检我们的 S18 模型，标出**哪些是实证支持、哪些是基督教语法的投影、缺哪几个轴**。
> 来源纪律：只用学术来源（专著/期刊/SEP/BMCR/JSTOR/出版社），不用百度百科。

## 0. 六个前提性批评（先看这个，再看字段）


### ① "宗教"本身是近代西方范畴

- Nongbri, *Before Religion: A History of a Modern Concept*（Yale, 2013）：把 religio 译成"宗教"是近代建构；古代没有"诸宗教并列"的边界。
- Fitzgerald, "A critique of 'religion' as a cross-cultural category"（*MTSR* 9/2, 1997）；Asad, *Genealogies of Religion*（1993）。
- **对我们的意义**：`family → religion → denomination → faith` 这条链是**基督教语法**。把它当"客观分类"会重演"给中国民间信仰硬找一个 creed"的错误。
- **不是要废掉分类**，而是：分类是 **etic（研究者标签）**，不能让它直接驱动机制；并且必须允许"没有 religion 这一层的宗教"（见 ④）。

### ② 信念中心主义（creed-centrism）

- "Lived Ancient Religion" 路径（Rüpke 等，"Religion in the making", *Religion* 2018）：古代宗教的常态是**实践、场所、时机、身体**，不是信条；个人是在具体情境里"做宗教"，不是"信一套教义"。
- **对我们的意义**：我们把 `creed` 设成 religion 必答，是把新教/天主教的问题意识当成了普适结构。`liturgy / custom / patronage / cultSites` 应当与 creed 同权，而不是附属。

### ③ 传播模式：教义型 vs 意象型（Whitehouse）

- Whitehouse 的 modes of religiosity：**doctrinal**（高频低唤起、文字、等级、跨地域正统）vs **imagistic**（低频高唤起、仪式创伤/狂欢、地方凝聚、教义弱）。
- 实证：*The cultural morphospace of ritual form*（*Evolution & Human Behavior*, 2011）用跨文化样本检验，结论更支持**频率与唤起度两条连续维度**，而非二元对立。
- **对我们的意义**：这是比我们 `assemblyMode`（liturgical/lineage）**更能解释组织形态**的轴。建议做两个 0–1 浮点（`ritualFrequency`, `ritualArousal`），不要做枚举。可断言：高 doctrinal → 有 canon、有 magisterium、跨地域；高 imagistic → 地方教派、弱 canon、强节庆。

### ④ 弥散型宗教（diffused religion）

- C.K. Yang, *Religion in Chinese Society*（1961）：中国宗教弥散在家族、国家、经济之中，没有独立制度领域。
- **对我们的意义**：我们的 `religion` 被设成必挂且有 creed/canon/polity，处理不了"宗教不在制度里"的形态。需要 `differentiation ∈ {differentiated, embedded}`：embedded 型的 creed/canon/polity 由家族/国家**借用**，自身不持有。

### ⑤ 排他性（Assmann 的"摩西区分"）

- Assmann：一神教的断裂不在"只信一个神"，而在**真/假宗教的区分**——别的神不是"另一个神"，而是"假神"。这解释了为什么"妖魔化/剥离神性"是一神教的选项，而多神教通常走"等同/吸纳"。
- **对我们的意义**：我们此前把"一神教→妖魔化"写成条件判断，**应该由字段推导**：`exclusivity ∈ {inclusive, henotheistic, exclusive}`。
  - `inclusive`：可等同（interpretatio）、可吸纳为主保；
  - `exclusive`：可妖魔化、可剥离神性、不可等同。

### ⑥ 个体化不是现代产物

- Rüpke 等（*Religious Individualisation*, 2019）：古代已有个人选择崇拜对象、个人誓愿、个人主保。
- **对我们的意义**：支持 `personal_tenet`/`patron`；但提醒**不要用"私人信念 vs 公共宗教"的新教框架**去建模——古代的"个人化"多是**实践选择**（拜哪个神、守哪个节、许什么愿），不是"内心信什么"。所以个人层应以 `vows / patron / practice` 为主，`belief` 为辅。

---

## 1. 逐条体检：我们的模型哪里站得住

| 我们的设计 | 学术判定 | 处理 |
|---|---|---|
| `family → religion → denomination → faith` | **etic 分类，有基督教语法风险** | 保留作分类，但标注 etic；加 `differentiation=embedded` 允许"无独立宗教领域" |
| `religion` 必挂 | 便于机制，但**不是普适事实** | 保留（游戏需要），但 embedded 型 religion 不持 creed/canon |
| `denomination` 必挂 + `mainstream`/`divergence` | **有支持**：宗教竞争与正统化确实需要中层单位 | 保留；`divergence` 由 tenet/liturgy/precept 差算，符合"可算不可填" |
| `lineage` 传承 | **强支持**：Hervieu-Léger 的"宗教是记忆之链"（*La religion pour mémoire*, 1993） | 保留，并可提升为"传承即合法性来源"的通用机制 |
| `faith` 为传播/分裂单位 | 与"宗教变迁发生在共同体层"一致 | 保留 |
| `personal_tenet` / `spiritual_fulfillment` | 有支持（Rüpke 个体化），但**要防信念中心** | 个人层以 practice/vow/patron 为主 |
| `Patronage`（神祇/圣人/祖先） | **强支持**：Peter Brown, *The Cult of Saints*（1981）——圣人是晚古社会里的 patronus | 保留；并把它接成**庇护关系（patron-client）的特化**，不是宗教专属 |
| 神庙→教堂（同圣地实体） | 与考古/制度史一致（圣地连续性常见，人格连续性少见） | 保留"结构延续≠人格延续"的限定 |

---

## 2. 应补的三个轴（缺了就会退化成硬编码）

| 轴 | 取值 | 替代掉什么硬编码 | 可断言 |
|---|---|---|---|
| `exclusivity` | `inclusive` / `henotheistic` / `exclusive` | "一神教才妖魔化"的条件判断 | exclusive 不能 `identified`；inclusive 不能 `de-deified` |
| `ritualFrequency` / `ritualArousal`（0–1 两浮点） | 连续量 | "多神教没有教统"的刻板 | 高 frequency+低 arousal → 易生 canon/magisterium；高 arousal → 地方凝聚强、跨地域弱 |
| `differentiation` | `differentiated` / `embedded` | "每个宗教都有 creed" | embedded 型 religion 的 creedId 可空 |

---

## 3. 两个理论要谨慎用

1. **宗教市场论**（Stark/Iannaccone；"Why Strict Churches Are Strong", *AJS* 1994）：竞争提升参与度、严格性提升承诺——可用作 `divergence`/主流更替的**输入之一**，但该理论在世俗化辩论中被反复质疑，且不适用于国教经济。**只当假设，不当定律**。
2. **"圣人取代了神"**：流行但过度简化。Brown 强调的是**庇护结构**的延续与**圣徒崇拜的新制度**（殉道者、圣髑、主教）——不是"旧神改名"。写作时必须限定在功能层。

---

## 4. 文献清单（可复核）

| 文献 | 用途 |
|---|---|
| Nongbri, *Before Religion*（Yale 2013）<https://sites.lsa.umich.edu/paul-in-athens/wp-content/uploads/sites/1400/2025/03/Nongbri-2013-Before-Religion-compressed.pdf> | ① 范畴问题 |
| Fitzgerald, *MTSR* 9/2 (1997) <https://brill.com/view/journals/mtsr/9/2/article-p91_1.xml> | ① 跨文化范畴批评 |
| Whitehouse 等, *The cultural morphospace of ritual form*, EHBE (2011) <https://www.sciencedirect.com/science/article/abs/pii/S1090513810001029> | ③ 仪式频率/唤起两维 |
| Whitehouse, *Rethinking ritual*, JRAI (2024) <https://rai.onlinelibrary.wiley.com/doi/10.1111/1467-9655.14048> | ③ 最新综述 |
| Rüpke 等, "Religion in the making: Lived Ancient Religion", *Religion* (2018) <https://www.tandfonline.com/doi/pdf/10.1080/0048721X.2018.1450305> | ② 实践优先 |
| Rüpke, *Religious Individualisation*（2019）<https://publikationen.uni-tuebingen.de/xmlui/bitstream/handle/10900/164505/R%c3%bcpke_354.pdf> | ⑥ 个体化 |
| Assmann, 摩西区分（研究综述）<https://hrcak.srce.hr/file/413570> | ⑤ 排他性 |
| Brown, *The Cult of Saints*（1981）；BMCR 书评 <https://repository.brynmawr.edu/cgi/viewcontent.cgi?article=1086&context=bmrcl> | 主保/圣徒 |
| Stark & Iannaccone, "Why Strict Churches Are Strong", *AJS* 99/5 (1994) <https://www.journals.uchicago.edu/doi/abs/10.1086/230409> | 宗教市场（谨慎） |
| Yang, *Religion in Chinese Society*（1961）；"Gift and Market in the Chinese Religious Economy" <https://hub.hku.hk/bitstream/10722/172360/1/Content.pdf> | ④ 弥散型 |

---

## 5. 结论（三句话）

1. **模型的骨架站得住**：faith 作传播/分裂单位、lineage 作传承、denomination 承载主流/分歧、Patronage 统一神祇与圣人——这四点是文献支持的。
2. **最大的风险是基督教语法**：把 creed 设成必答、把 religion 设成有独立制度领域、把"多神教无教统"当成事实。这三处必须用 `differentiation`、`exclusivity`、`ritualFrequency/Arousal` 三个轴来解。
3. **不能拿理论当定律**：宗教市场论、"圣人取代神"都只能当假设，写进模型时要有可证伪的断言，而不是当默认规则。

---

## 6. 圣俗政治：合一还是二分（用户 17:10 口径）

> 先更正：这里说的**不是** Durkheim 的 sacred/profane 分类（那是"世界怎么分神圣与凡俗"），而是**神圣政治与世俗政治的关系**——即政治权威本身是否神圣、以及祭司权与王权是否分离。

### 6.1 六种形态（不是二元）

| 形态 | 含义 | 历史锚点 | 数据 |
|---|---|---|---|
| **神圣王权** `sacral-kingship` | 统治者本人即神圣（神、神子、天子），无需祭司授权 | 埃及法老、中国天子/郊祀、日本天皇（祭政一致） | `rulerIsSacred=true`；`clergyAboveRuler=false` |
| **王权统教** `caesaropapist` | 统治者是**教会的首脑**，可任命、可定教义 | 拜占庭"和谐论"（Justinian）、俄国沙皇、亨利八世 | `rulerHeadsChurch=true`；可 `appointClergy` |
| **两权分立** `dualist` | 祭司权与王权**两个权威**，各有领域，互相可争 | Gelasius I 的"双剑"（494）、主教叙任权之争 | `twoAuthorities=true`；可 `excommunicateRuler` |
| **祭司统治** `priestly` | 祭司直接执政，教法即国法 | 达赖政权、伊朗（法基赫监国）、教皇国 | `rulerIsClergy=true`；`religiousLawIsStateLaw=true` |
| **教权自主** `autonomous` | 有独立教士阶层，但不谋求政权；国家亦不强管教义 | 逊尼乌莱马（学者自治）、美国式分离 | `clergyAutonomous=true`；`rulerHeadsChurch=false` |
| **无分离** `embedded` | 没有独立的宗教权威，祭祀即公职 | 神道村落、民间信仰 | 接"公职祭祀"原型 |

> 注意：**"政教合一"不是一个值**。神圣王权、王权统教、祭司统治是三件不同的事——把它们混成一个布尔，就解释不了"为什么拜占庭皇帝能定教义却不能自称神"。

### 6.2 一个关键的中间案例

中国佛教的**"沙门不敬王者"**之争（慧远）：在一个"宇宙论一体、皇权神圣"的系统里，僧团主张自己不受王者礼制管辖——这正是 `dualist` 诉求在 `sacral-kingship` 环境里的出现。模型必须允许**同一宗教内不同集团持不同立场**（`clergyPosition ∈ {subordinate, exempt, superior}`），而不是给整个宗教一个值。

### 6.3 机制后果（可断言）

| 若 | 则 |
|---|---|
| `sacral-kingship` | 合法性主要来自神圣血统/天命；**叛乱即渎神**；祭司无法废黜君主 |
| `caesaropapist` | 统治者可任命教职 → 教产成为财政工具；教义争议由宫廷裁决（可催生"皇帝支持的异端"） |
| `dualist` | **绝罚可削弱合法性 → 可触发臣民解誓/叛乱**；主教叙任权可成战争理由；教产享有豁免（`Privilege.kind=exemption`） |
| `priestly` | 教法=国法；司法与宗教法庭合一；异端即叛国 |
| `autonomous` | 教士不干政但垄断教育/法律解释；国家改教义会遭遇消极抵抗（非叛乱） |
| `embedded` | 祭祀与行政同批人；"改宗"不是个人选择而是整村整族的事 |

### 6.4 与现有系统的接口

- **合法性**：`sacral-kingship` 的合法性来自血统/天命；`dualist` 的合法性可被教会**收回**（这是"绝罚"能被做成机制的根据）。
- **教产/豁免**：`dualist` 与 `autonomous` 下，`Privilege{kind=exemption}` 是核心制度（免税、免世俗司法、教产不可侵）。
- **法律**：`priestly` 下 `religiousLawIsStateLaw=true`，`precept` 直接进国法；`dualist` 下有两套法庭（教会法 vs 世俗法）。
- **头衔**：`religious_head`/`head_of_rite` 是 `caesaropapist`（皇帝兼）与 `dualist`（教宗独立）的分野所在。
- **代码现状**：`RealmData.stateReligionId` 默认 −1 且**无写入**——国教这条线整个没接，所以上面五条后果目前一条都不成立。
