# 信仰系统方案分析：对五层体系的批评与 CK3 借鉴

> 2026-11-05。对象：前述“religious family → religion → denomination → tradition-stream → local custom”五层体系，以及“belief-system 四型 + school 四类”方案。
> 结论：**分析框架可以借鉴，但不能直接当游戏数据模型**；应借鉴 CK3 的“扁平 faith + doctrines/tenets”结构，而不是硬造五层。

---

## 1. 先看现有代码

| 概念 | 代码 | 真实行为 |
|---|---|---|
| 宗教内容 | `ReligionDef` | `parentReligionId`、`schoolParentId`、`nodeType`、`school`、`selectedDoctrines`、`rites`、`liturgicalLanguage` |
| 节点类型 | `ReligionNodeType` | `Religion / Succession / Tradition / School / ReligiousSchool` |
| 地图层级 | `ReligionMapLevel` | 只有 `Religion / Succession / Tradition` |
| 信仰类型 | `FaithType` | Animistic/Polytheistic/Henotheistic/Monotheistic/Pantheistic/Atheistic/Cosmic |
| 组织类型 | `FaithOrganizationType` | Decentralized/Congregational/Episcopal/Papal/Theocratic/StateChurch |
| 教义类别 | `DoctrineCategory` | Cosmology/Soteriology/Ethics/Ritual/Ecclesiology/Eschatology/Political |
| 教义选项 | `DoctrineOptionDef` | `pillar`、`exclusiveReligionIds`、`enhancedFrom` |
| 运行时 | `FaithSystem` | deities、doctrines、rituals、taboos、orgType、highPriest、fervor、hierarchy |
| 圣人 | `SaintDef/CanonizationSystem` | 封圣、主保圣人 |

问题：

- `ReligionDef` 同时承担“宗教/继承/传统/学派”多层，`nodeType` 和 `ReligionMapLevel` 不一致；
- `FaithType` 是一套本质化的宗教类型学，和实际机制（doctrine/organization）脱节；
- `schoolParentId`/`school` 是字符串，和 `nodeType` 重复；
- `FaithSystem` 与 `ReligionDef` 两套状态/内容并行；
- 没有“faith 作为可改革单位”的清晰模型；
- 教义/信条/礼仪/组织/教职混在不同层。

---

## 2. 五层体系的问题

### 2.1 把分析层级当游戏层级

`family → religion → denomination → tradition-stream → local custom` 是一个**分析框架**，不是每个宗教都必须有的数据结构。

强行五层会导致：

- 很多宗教没有 denomination；
- 很多宗教没有 school；
- 很多地方没有独立 custom；
- 为了填层而造空节点；
- UI/逻辑要处理大量空层。

### 2.2 `denomination` 不是普适词

`denomination` 来自基督教/教会学语境。其他传统有不同单位：

- 伊斯兰：`madhhab`（教法学派）、`firqa`（教派）、`ṭarīqa`（苏菲教团）；
- 佛教：`nikāya`（部派）、`school`（宗）；
- 印度教：`sampradāya`（传承/派别）；
- 犹太教：`movement`（运动）；
- 中国：宗派、法脉、宗门。

所以不能把 `denomination` 当所有宗教的通用层；最多做**可选组织分支** `branch`。

### 2.3 `tradition-stream` 和 `school` 重叠

- `tradition-stream` 说是“宗派内跨代传承的修行/解释流派”；
- `school` 又说“宗派内流派”；
- 两者定义几乎重合。

要么合并为 `school/lineage`，要么把 `tradition` 限定为“传承链”，不作为一个层级。

### 2.4 `local custom` 不是层级

地方习俗是**实践变体/修正项**，不是和 religion/denomination 并列的实体层级。它更适合作为：

- `practice` 修正；
- `localRite`；
- `custom` 标签；
- 事件/法律的地方差异。

### 2.5 `order` 被误放进 school

修会（方济各会、耶稣会）是**组织**，不是教义流派。应归：

- `ReligiousOrganization`（有规约、成员、财产、会院）；
- 不是 `denomination`/`school`。

### 2.6 教义词族被当成层级

`creed/dogma/canon/confession/catechism/orthodoxy` 是**文本/话语/规范类型**，不是同一层级：

- `canon` 是正典文本清单；
- `creed` 是公开宣认；
- `dogma` 是权威定断；
- `confession` 是宗派告白；
- `catechism` 是教学手册；
- `orthodoxy` 是正统边界。

把它们塞进一个“区分标志”列表可以，但不能当组织层级。

---

## 3. CK3 信仰系统可借鉴什么

CK3 的结构大致是：

```text
ReligiousFamily（宗教家族）
  └─ Religion（宗教）
       └─ Faith（信仰/可玩单位）
            ├─ Doctrines（教义组）
            ├─ Tenets（信条，限量）
            ├─ Holy Sites
            ├─ Religious Head
            ├─ Fervor
            └─ Organization / Clergy
```

关键：

- **Faith 是核心可玩单位**，不是 denomination；
- **Family/Religion 是分组**，不是必须层级；
- **Doctrines/Tenets 是组件**，不是层级；
- **Reformation/Creation** 通过 piety/fervor/legitimacy 改变 faith；
- **Hostility** 由 family/religion/doctrines 决定（Righteous/Astray/Hostile/Evil）；
- **Religious Head / Fervor** 是组织与动态；
- **Holy Sites** 是地理/威望/改革条件；
- **Tenets 有限槽位**，Doctrines 分组选择；
- **Rites/Liturgy/Language** 可作为文化变体，不是层级。

参考：

- [CK3 Wiki — Faith](https://ck3.paradoxwikis.com/Faith)
- [CK3 Wiki — Religion](https://ck3.paradoxwikis.com/Religion)
- [CK3 Wiki — Tenet](https://ck3.paradoxwikis.com/Tenet)
- [CK3 开发日志：信条 & 灵性满足](https://store.steampowered.com/news/app/1158310/view/526500154372522963)

---

## 4. 建议的模型

### 4.1 核心单位：Faith

```text
Faith
  ├─ faithId
  ├─ name
  ├─ familyId        // 可选
  ├─ religionId      // 可选
  ├─ parentFaithId   // 分裂来源
  ├─ doctrines[]     // 机制教义
  ├─ tenets[]        // 身份信条（限量）
  ├─ rites[]         // 礼仪
  ├─ liturgy         // 礼仪框架
  ├─ language        // 礼仪/经典语言
  ├─ organization    // 组织形态
  ├─ head            // 宗教领袖
  ├─ holySites[]     // 圣地
  ├─ fervor          // 热情
  ├─ legitimacy
  └─ hostilityRules
```

### 4.2 可选分组

```text
ReligiousFamily（可选）
  └─ Religion（可选）
       └─ Faith（必须）
```

- 没有 family/religion 的信仰也可以存在；
- 不强制 denomination/school/custom 层；
- `branch`/`school`/`lineage` 作为**可选网络**，不是层级：
  - `InterpretiveSchool`（解经/修行流派）；
  - `MonasticOrder`（组织）；
  - `PoliticalSchool`（意识形态，归政治系统）。

### 4.3 教义/信条/礼仪/组织分离

| 维度 | 数据 | 作用 |
|---|---|---|
| 教义 | `DoctrineGroup/DoctrineOption` | 机制规则：婚姻、性别、圣战、奴隶制、巫术、朝圣、税收、神职 |
| 信条 | `Tenet` | 身份标识，限量槽位 |
| 礼仪 | `Rite/Liturgy` | 仪式、节期、语言 |
| 组织 | `ReligiousOrganization` | 教会/教团/修会/教派治理 |
| 教职 | `EcclesiasticalOffice` | 职位、任命、职权（接头衔系统） |
| 圣地 | `HolySite` | 地理、朝圣、改革条件 |
| 敌对 | `Hostility` | 由 family/religion/doctrines 决定 |

### 4.4 不要硬造五层

```text
❌ family → religion → denomination → tradition-stream → local custom（强制）
✅ family? → religion? → faith（必须） + 可选 school/order/practice 网络
```

---

## 5. 对“四型 belief-system”的批评

“religious / political / philosophical / secular”四型太现代：

- “secular”在前现代不是独立信仰体系；
- 政治意识形态不是 religion；
- 哲学学派不必然有共同体/礼仪/教职；
- 把政治派系塞进 faith 会导致“政党=教会”的荒谬。

建议：

- **Faith 系统只处理宗教/类宗教共同体**；
- **Ideology 系统**单独处理政治/哲学/世俗意识形态；
- 两者共享 `doctrine`、`legitimacy`、`faction` 等底层，但不共享 `religion` 层级；
- “school”在 Ideology 中叫 `school/faction`，在 Faith 中叫 `lineage/order`。

---

## 6. 对现有代码的改造建议

1. `ReligionDef` 拆成 `FaithDef`（可玩单位）+ 可选 `ReligionDef`（分组）+ 可选 `FamilyDef`；
2. `ReligionNodeType` 移除 `Tradition/School`，改为 `family/religion/faith`；
3. `FaithType` 保留为描述标签，不决定机制；
4. `DoctrineCategory` 扩展为数据驱动 `DoctrineGroup`；
5. `DoctrineOptionDef.pillar` 改为 `groupId`，加 `effects/requirements/exclusions`；
6. `FaithSystem` 成为 `FaithDef` 的运行时状态；
7. `FaithOrganizationType` 保留为组织形态，和 `ReligiousOrganization` 实例分离；
8. `CanonizationSystem` 独立，主保圣人分神圣/世俗；
9. 语言/礼仪/历法归 `liturgy`，不单列层级；
10. 教职系统接 `TitleHolding`，不另造一套。

---

## 7. 结论

- 五层体系作为分析工具可以，但不能直接当游戏数据模型；
- `denomination` 不是普适词，`tradition-stream` 与 `school` 重叠，`local custom` 不是层级；
- CK3 的“family → religion → faith + doctrines/tenets”更扁平、更适合游戏；
- 应借鉴：Faith 为可玩单位、doctrines/tenets 组件、fervor/hostility/head/holy sites、改革机制；
- 不应借鉴：把一切都压成 faith，政治/哲学/世俗意识形态应另立 `Ideology` 系统；
- 先改 `ReligionDef`/`ReligionNodeType`/`FaithSystem`，再谈内容。


---

# 修正：CK3 是四层，Rite 才是最低可玩单位

## 8. 先更正

之前按“family → religion → faith”三层分析，是错的。

按你说的 CK3 现机制：

```text
ReligiousFamily（宗教家族）
  └─ Religion（宗教）
       └─ Faith（信仰）
            └─ Rite（礼仪/仪式传统，最低可玩单位）
```

**Rite 才是最低可玩单位**，不是 Faith。

这意味着：

- 玩家角色属于某个 Rite；
- 改革/创建/控制的对象是 Rite；
- Faith 是共享教义/信条的上级单位；
- Religion 是更大的分类；
- Family 是更宽的分类。

参考：

- [CK3 开发日志 #2：教会制 & 礼仪](https://store.steampowered.com/news/app/1158310/view/690882175138006168)
- [CK3 Wiki — Religion](https://ck3.paradoxwikis.com/Religion)
- [CK3 Rite Guide](https://tposegaming.com/ck3-rite/)

## 9. Rite 是什么

Rite 不是“地方习俗”，也不是“教派”：

```text
Rite
  ├─ riteId
  ├─ faithId
  ├─ name
  ├─ liturgy          // 礼仪框架
  ├─ language         // 礼仪/经典语言
  ├─ calendar         // 礼仪历法
  ├─ clergy           // 神职/教职
  ├─ practices[]      // 实践变体
  ├─ doctrineOverrides[] // 对 Faith 教义的局部覆盖
  ├─ legitimacy
  └─ fervor
```

- 同一 Faith 下可以有多个 Rite；
- Rite 共享 Faith 的核心教义/信条；
- Rite 可以有不同礼仪、语言、历法、神职、地方实践；
- Rite 是玩家实际“玩”的单位；
- Rite 不是 `denomination` 的同义词，也不是 `local custom`。

## 10. 四层模型的边界

```text
Family
  = 最宽分类，共享终极观/正典结构（可选）
Religion
  = 宗教群，共享核心叙事/正典（可选）
Faith
  = 共享教义/信条/圣地的信仰共同体
Rite
  = 可玩单位：礼仪/语言/历法/神职/实践变体
```

- 不是每个传统都必须四层齐全；
- 没有 Rite 的可以只有 Faith；
- 没有 Faith 的可以只有 Religion/Rite；
- 但**可玩单位必须是 Rite**，不是抽象 Faith。

## 11. 不要再只分析宗教

信仰系统只是一个大系统，不能孤立设计。需要一起收口：

| 大系统 | 核心单位 | 与其他系统的接口 |
|---|---|---|
| 政治实体/头衔/契约 | Polity / Title / Contract | Rite 的教会组织、教职头衔 |
| 法律/法令/根本化 | LegalNorm / LegalOrder | Rite 的宗教法、教法庭、特许 |
| 信仰/礼仪 | Family / Religion / Faith / Rite | 主保、圣地、朝圣、宗教权威 |
| 意识形态/学派 | Ideology / School / Faction | 政治学派、哲学学派，不塞进 Faith |
| 官僚/教职 | Office / TitleHolding | 教会官僚、教职任命 |
| 历法/时间 | Calendar / Era | 各 Rite/政权的礼仪历法、纪年 |
| 聚落/单位/关系 | Settlement / Unit / Relation | 宗教场所、朝圣路线、圣战 |

## 12. 意识形态不要塞进信仰

政治学派、哲学学派、世俗意识形态不是 Rite。

```text
Faith/Rite 系统：宗教/类宗教共同体
Ideology 系统：政治/哲学/世俗意识形态
```

两者可以共享：

- `Doctrine`（机制化主张）；
- `School`（解释/路线网络）；
- `Faction`（组织化派系）；
- `Legitimacy`（正当性）。

但不共享：

- `Rite`（礼仪）；
- `Clergy`（神职）；
- `HolySite`（圣地）；
- `ReligiousHead`（宗教领袖）。

## 13. 修正后的重构顺序

1. 先定本体：Polity/Title/Territory/Contract/Relation；
2. 再定信仰：Family/Religion/Faith/Rite；
3. 定意识形态：Ideology/School/Faction；
4. 定法律/官僚/教职：LegalOrder/Office/TitleHolding；
5. 定历法：Calendar/Era；
6. 最后内容/UI。

不要先写宗教内容，再发现本体接不上。


---

# 追加：Rite 太基督教中心，改中性可玩单位

## 14. 问题

`Rite`（礼仪）作为最低可玩单位，确实偏基督教/礼仪宗教：

- 基督教：Latin/Greek/Mozarabic rites 很自然；
- 佛教：有仪式，但“宗/部派/法脉”不叫 rite；
- 伊斯兰：`madhhab`/`firqa`/`ṭarīqa`，不是 rite；
- 印度教：`sampradāya`，不是 rite；
- 犹太教：movement；
- 中国：宗、派、法脉、宗门；
- 部落/民俗宗教：可能只有地方共同体，没有成文礼仪。

所以不能把 `Rite` 当所有宗教的最低可玩单位。

## 15. 中性可玩单位：FaithCommunity

内部用一个中性词：

```text
FaithCommunity（信仰共同体 / 可玩单位）
  ├─ communityId
  ├─ faithId
  ├─ communityTypeId   // 数据驱动，不是硬枚举
  ├─ name
  ├─ organization
  ├─ practice
  ├─ liturgy           // 可选
  ├─ law               // 可选
  ├─ language
  ├─ lineage
  ├─ legitimacy
  └─ fervor
```

`communityTypeId` 对应不同文化的实际单位：

| 文化/宗教 | 类型 id | 中文显示 |
|---|---|---|
| 基督教 | `rite` | 礼仪/教会 |
| 基督教 | `denomination` | 宗派 |
| 伊斯兰 | `madhhab` | 教法学派 |
| 伊斯兰 | `firqa` | 教派 |
| 伊斯兰 | `tariqa` | 苏菲教团 |
| 佛教 | `nikaya` | 部派 |
| 佛教 | `school` | 宗/学派 |
| 佛教 | `lineage` | 法脉 |
| 印度教 | `sampradaya` | 传承/派别 |
| 犹太教 | `movement` | 运动 |
| 中国 | `zong` | 宗 |
| 中国 | `lineage` | 法脉 |
| 部落 | `community` | 共同体 |
| 通用 | `branch` | 分支 |

- `communityTypeId` 是数据，不是 enum；
- UI 按文化/语言显示对应词；
- `Rite` 只是 `communityType` 的一种，不是通用层名；
- 玩家玩的是 `FaithCommunity`，不是抽象 Faith。

## 16. 四层改为可选

```text
ReligiousFamily（可选）
  └─ Religion（可选）
       └─ Faith（可选/通常有）
            └─ FaithCommunity（必须，可玩）
```

不是每个宗教都要四层：

- 有 family/religion/faith/community 的：基督教、伊斯兰、佛教等；
- 只有 faith/community 的：地方宗教；
- 只有 community 的：部落/民俗；
- `Rite` 只是基督教型 community 的显示词。

## 17. CK3 能借什么、不能借什么

能借：

- 分组层级（family/religion/faith）作为可选；
- doctrines/tenets 作为组件；
- fervor/hostility/head/holy sites；
- 改革/创建机制；
- 可玩单位是具体的，不是抽象层。

不能直接借：

- `Rite` 作为普适最低单位；
- `denomination` 作为普适层；
- 把基督教礼仪结构套到伊斯兰/印度/中国/部落宗教。

## 18. 与意识形态分开

```text
FaithCommunity：宗教/类宗教共同体
Ideology/School/Faction：政治/哲学/世俗意识形态
```

- `communityTypeId` 只用于宗教；
- 政治学派用 `Ideology` 的 `schoolTypeId`；
- 两者可共享 `Doctrine` 数据，但不共享 `Rite/Clergy/HolySite`。

## 19. 结论

- `Rite` 太基督教中心，不能当普适最低可玩单位；
- 中性内部单位用 `FaithCommunity`；
- `communityTypeId` 数据驱动，按文化显示“礼仪/宗派/教法学派/部族/法脉/宗”；
- 四层可选，不强制；
- CK3 的分组、doctrines/tenets、fervor/hostility 可借，Rite 不能普适化。


---

# 实读 CK3 文件：Rite/Personal Tenet 的真实结构

> 路径：`E:\SteamLibrary\steamapps\common\Crusader Kings III\game\commoneligion`

## 20. 实际目录

```text
common/religion/
  ├─ religion_family_types/
  ├─ religion_types/
  ├─ faith_types/
  ├─ rite_types/
  ├─ tenet_types/
  ├─ doctrine_types/
  ├─ doctrine_group_types/
  ├─ doctrine_category_types/
  ├─ holy_site_types/
  └─ rite_names/
```

## 21. 实际层级

- `religion_family_types`：宗教家族
- `religion_types`：宗教
- `faith_types`：信仰
- `rite_types`：礼仪/具体传统
- `doctrine_*` / `tenet_types`：组件

`_faith_types.info` 明确：

- `main_rite = rite_key`：每个 Faith 有一个主 Rite；
- `tenets` / `doctrines` 在 Faith 上是“该 Faith 所有 Rite 共有”的部分；
- Rite 上的 tenets/doctrines 覆盖同组 Faith/Religion 项；
- 没有脚本化 Rite 的 Faith 会动态生成同名主 Rite。

`_rite_types.info` 明确：

- Rite 有 `faith`、`cultures`、`tenets`、`doctrines`；
- Rite 的 doctrines 覆盖父 Religion/Faith 同组 doctrine；
- Rite 是具体可转换/可创建对象。

实例：

```text
roman_rite.faith = christian_faith
theravada.faith = buddhist_faith
mahayana.faith = buddhist_faith
vajrayana.faith = buddhist_faith
```

所以 CK3 的 Rite **不是基督教专属机制**；佛教也有 theravada/mahayana/vajrayana 三个 Rite。术语本身有基督教色彩，但机制是“Faith 的具体礼仪/传统实例”。

## 22. Personal Tenets 实际存在

`_tenet_types.info` 有：

```text
can_pick_as_personal_tenet = { ... }   # root = character
personal_tenet_modifier = { ... }
personal_tenet_parameters = { ... }
```

说明：

- 角色可以从已知/允许的信条中选个人信条；
- 个人信条有独立 modifier/parameter；
- 公开 Faith/Rite 的 tenets 与个人 tenets 分开；
- 这正是“公开信仰 + 个人化信条”的机制。

## 23. 修正结论

- 四层真实存在：Family → Religion → Faith → Rite；
- Rite 是具体可玩/可转换单位，Faith 是共享教义分组；
- Rite 有 cultures/tenets/doctrines，可覆盖 Faith/Religion 同组；
- 每个 Faith 有 `main_rite`；没有脚本 Rite 会动态生成；
- 个人信条是角色层，不是 Faith 层；
- 所以我们的模型应是：
  - Faith/Rite 层：公开信仰；
  - Character 层：个人信仰/个人信条/秘密信仰；
  - Organization 层：教会/教团/修会（可选）；
  - Identity 层：社会边界（可选）。
- `Rite` 作为内部术语可用，但显示要本地化：礼仪/传统/流派/宗/法脉，不能硬套所有文化。
