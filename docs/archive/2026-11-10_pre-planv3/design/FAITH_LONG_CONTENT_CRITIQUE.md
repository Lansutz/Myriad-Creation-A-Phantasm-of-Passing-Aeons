# 对“五层信仰体系长文”的逐项分析

> 2026-11-05。对象：你贴的那一大段“区分层级/单元/标志 + 五层 + 语言/教职/伦理子类 + 单词术语表 + belief-system 四型”的内容。
> 结论：里面有可用的分析工具，但**不能直接当游戏数据模型**；多处层级、标志、词性不平行，且基督教/新教中心。

---

## 1. 元层：level / unit / marker

| 项 | 判断 | 理由 |
|---|---|---|
| differentiation level | 可作分析工具 | 提醒不要跨层比较 |
| differentiation unit | 可作分析工具 | 提醒“比较单位”不是标志 |
| differential marker | 可作分析工具 | 提醒标志不是实体 |

问题：

- 这是**研究方法**，不是游戏实体；
- 不能把 level/unit/marker 直接做成三张表；
- 游戏需要的是“实体 + 组件 + 关系”，不是“每层必须一个标志”。

---

## 2. 五层单位逐项

### 2.1 religious family

长文定义：共享终极观与正典结构的宗教群。

问题：

- “终极观”是现代宗教学的抽象词，不是历史行动者的自称呼；
- 很多宗教家族没有共享正典；
- 家族边界是学者建构，不是固定实体；
- 可作可选分组，不能强制。

### 2.2 religion

长文定义：信念—实践—共同体—制度的复合体。

问题：

- 这是现代学术定义，不是历史单位；
- 在游戏里更像“分组”，不是可玩单位；
- 与 family/faith 的边界不稳定。

### 2.3 denomination

长文定义：同一宗教内因告白、礼仪、治理、入教、戒律而分立的组织体。

问题：

- `denomination` 是基督教/教会学词；
- 伊斯兰 `madhhab/firqa/ṭarīqa`、佛教 `nikāya`、印度教 `sampradāya`、犹太教 movement 都不是 denomination；
- “因告白/礼仪/治理/入教/戒律分立”是基督教教派模型；
- 不能当普适层。

### 2.4 tradition-stream

长文定义：宗派内跨代传承的修行与解释流派。

问题：

- 与 `school` 定义几乎重合；
- `tradition` 是传承链，不是 bounded unit；
- “stream” 是网络，不是组织；
- 不能同时当层级和流派。

### 2.5 local custom

长文定义：地方性、未必有规范权威的惯行。

问题：

- 地方习俗不是“层级”；
- 它可以横跨 family/religion/faith/community；
- 更适合做 practice/modifier，不是 unit。

---

## 3. 五层标志逐项

| 层 | 长文标志 | 问题 |
|---|---|---|
| family | ultimate reality conception | 现代抽象，不适用所有传统 |
| family | canonical structure | 很多家族无正典 |
| family | core narrative/revelation paradigm | “revelation” 亚伯拉罕中心 |
| religion | creed/dogma | creed 是基督教式公开宣认 |
| religion | canon | 不是所有宗教都有正典 |
| religion | basic liturgy | 不是所有宗教都有礼仪框架 |
| religion | polity | 治理不是“信仰标志”，是组织维度 |
| denomination | confession | 基督教词 |
| denomination | liturgical form | 与 religion 的 basic liturgy 重叠 |
| denomination | denominational polity | 与 religion 的 polity 重叠 |
| denomination | initiation | 入教程序不是所有宗教都有 |
| denomination | precept | 戒律不是宗派专属 |
| tradition-stream | praxis method | 与 ritual detail 重叠 |
| tradition-stream | interpretive tradition | 与 school 重叠 |
| tradition-stream | ritual detail | 与 liturgy 重叠 |
| tradition-stream | rule | 修会规约是组织，不是流派标志 |
| local custom | custom | 同义反复 |
| local custom | taboo | 禁忌不是地方专属 |
| local custom | local language usage | 语言不是信仰层标志 |

核心问题：

- 同一现象在不同层重复出现（liturgy/ritual、polity/denominational polity、creed/confession）；
- 标志不是互斥的；
- “每层一个标志”是伪精度；
- 很多标志是**文本类型/组织类型/实践类型**，不是层级标志。

---

## 4. 语言/教职/伦理子类

### 4.1 语言

长文拆成：

- liturgical language regulation；
- ritual language regulation；
- scriptural language regulation。

问题：

- 三者高度重叠；
- 语言是 rite/community 的属性，不是三层；
- 游戏里一个 `language` 字段 + `scope` 就够；
- 不需要三个 regulation 实体。

### 4.2 教职

长文拆成：

- ecclesiastical office system；
- magisterium；
- ordination。

问题：

- office 是组织/职位；
- magisterium 是训导权；
- ordination 是授任程序；
- 三者不是同一层；
- 应分别归 organization/office/ritual，不是 polity 子类并列。

### 4.3 伦理/纪律

长文拆成：

- sexual precept；
- matrimonial law；
- sexual taboo；
- penal discipline。

问题：

- precept 是戒律；
- matrimonial law 是法律；
- taboo 是禁忌；
- penal discipline 是处分；
- 四者词性不同，不能并列成“precept 子类”；
- 性伦理、婚姻法、禁忌、处分应各自归位。

---

## 5. 单单词术语表的问题

长文最后把所有东西压成单单词：

```text
faith / religion / tradition
doctrine / creed / tenet / dogma / confession / catechism / orthodoxy / canon
polity / liturgy / precept / penalty
office / magisterium / ordination / council
ritual / calendar / language
matrimony / taboo / commandment
penalty / procedure
family / religion / denomination / school / custom
```

问题：

- 单单词牺牲精度；
- `tradition` 仍然歧义；
- `school` 被用于宗教/政治/哲学/学术，四种不同东西；
- `office/magisterium/ordination` 不是并列层级；
- `penalty` 出现两次；
- `religion` 在总概念和层级单位重复；
- `faith` 在总概念和层级中缺失；
- `custom` 同义反复。

---

## 6. belief-system 四型的问题

长文提出：

```text
religious / political / philosophical / secular
```

问题：

- “secular” 是现代范畴，前现代不适用；
- 政治意识形态不是 religion；
- 哲学学派不必然有共同体/礼仪/教职；
- 把政治派系塞进 faith 会变成“政党=教会”；
- 四型混合了“信仰对象”“权威来源”“组织形态”三个维度。

---

## 7. 哪些能留

可留作分析工具：

- level/unit/marker 的区分；
- family/religion/faith 作为可选分组；
- doctrine/tenet/canon 作为组件；
- polity/liturgy/precept/penalty 作为制度维度；
- school/lineage 作为解释网络；
- custom 作为地方实践修正。

不能直接留：

- 五层强制；
- denomination 普适；
- tradition-stream 与 school 并用；
- local custom 当层级；
- 每层一个标志；
- 单单词硬压；
- belief-system 四型。

---

## 8. 建议的替代模型

```text
ReligiousFamily（可选）
  └─ Religion（可选）
       └─ Faith（可选/通常有）
            └─ FaithCommunity（必须，可玩）
                 ├─ communityTypeId  // rite/madhhab/nikaya/sampradaya/zong/...
                 ├─ doctrines[]
                 ├─ tenets[]
                 ├─ liturgy（可选）
                 ├─ language
                 ├─ calendar
                 ├─ organization
                 ├─ clergy
                 └─ practices[]
```

- `FaithCommunity` 是可玩单位；
- `communityTypeId` 数据驱动，按文化显示“礼仪/宗派/教法学派/部派/法脉/宗”；
- `Rite` 只是 `communityType` 的一种；
- `denomination` 不普适；
- `tradition-stream` 改成 `lineage/school` 网络；
- `local custom` 改成 `practice` 修正；
- `Ideology` 独立于 faith。

---

## 9. 结论

- 长文有分析价值，但不是可实施的数据模型；
- 最大问题是**层级、标志、词性不平行**；
- 第二问题是**基督教/新教中心**；
- 第三问题是**单单词压缩丢失精度**；
- 游戏应围绕 `FaithCommunity` 可玩单位 + 数据驱动组件，而不是五层强制。
