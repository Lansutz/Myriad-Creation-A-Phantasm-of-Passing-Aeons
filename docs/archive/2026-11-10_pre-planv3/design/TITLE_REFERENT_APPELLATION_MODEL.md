# 头衔字段模型：种类 / 指代 / 称谓 / 绑定

> 2026-11-09 注：其中 `kind` 列表与 `rank` 部分已被 M1/§11 取代；现行字段见 `TITLE_SEMANTIC_FIELD_SCHEMA.md`。

> 2026-11-05。先读现有代码：`TitleDef`、`TitleDefinition`、`TitleManager`、`TitleCatalog`、`TitleAppellationContext`、`TitleEligibility`、`TitleCandidateContext`、`AdminDivision`、`Titles.json`。
> 目标：头衔种类必须有；显示只显示“指代 + 称谓”；指代是多字段范围；绑定字段与指代相近但不等同；称谓接语言系统，可升格、可组合。

---

## 1. 现有代码事实

| 概念 | 代码 | 真实行为 |
|---|---|---|
| 内容头衔 | `TitleDef` | `titleId/kind/rank/weight/semantic`；`kind` 只有 monarch/bureaucratic/noble/honorific |
| 运行头衔 | `TitleDefinition` | `titleId/appellationId/superiorTitle/modules/appointmentOrder/seats/inheritable/...`；**与 `TitleDef` 重复** |
| 头衔目录 | `TitleCatalog` | 读 `ContentResolvers.Titles`（`TitleDef`） |
| 头衔注册 | `TitleManager` | 另一个静态 registry（`TitleDefinition`）；**几乎无人注册/使用** |
| 称谓 | `TitleAppellationContext` | 有 culture/polityKey/independent/governor/institution；但 `ResolveAppellation` 只返回 `appellationId`，没真正用 |
| 头衔持有 | **无 `TitleInstance/TitleHolding`** | `TitleHoldingMode` 枚举存在但未使用 |
| 行政区头衔 | `AdminDivision.titleId` | 硬编码 `title_zhuhou/title_qingdafu/title_baron/title_gov_general/...` |
| 角色页 | `CharacterDetailQueryAdapter` | 只查 office title，没有完整头衔列表 |

结论：现在只有“头衔定义 + 官职名”，没有“头衔实例/持有/指代/称谓组合”。

---

## 2. 头衔种类（必须有）

`TitleKind` 保留并扩展：

```text
monarch        君主
bureaucratic   官僚/官职
noble          贵族/爵位
honorific      荣誉
landed         领地头衔（可与 noble/monarch 重叠，作为 flag）
office         具体衙门职位
ecclesiastical 教会头衔
claim          宣称/继承权
```

注意：

- `kind` 是**分类字段**，参与 UI 分组、排序、规则；
- `kind` **不参与显示**；
- `kind` 不是政治实体类型；
- `landed/office` 与 `noble/bureaucratic` 可有重叠，建议用 flags 而不是硬拆。

---

## 3. 指代字段（titleReferent）

指代回答：**这个头衔是关于什么/管辖什么**。不是单一枚举，可填多个：

```csharp
TitleReferent {
  string referentId;          // 主键/槽位
  ReferentType type;          // ethnic/culture/settlement/territory/polity/adminDivision/office/faith/dynasty/character
  int targetId;
  string targetKey;
  float weight;               // 多指代时的主/次
}
```

支持：

- 族群 `ethnicGroupId`
- 文化 `cultureId`
- 聚落 `settlementId`
- 地域 `regionId/territoryId`
- 政治实体 `polityId`
- 行政区划 `adminDivisionId`
- 官职/机构 `officeId/institution`
- 信仰 `faithId`
- 王朝/家族 `dynastyId/houseId`
- 人物（个人头衔/荣誉） `characterId`

特点：

- 不是单一字段，可以多个；
- 多个指代可以有主次/权重；
- 指代决定“法兰克人的王”“雅典将军”“罗马皇帝”“某城领主”；
- 指代可以和绑定字段重合，但语义不同。

---

## 4. 绑定字段（boundFields）

绑定回答：**谁在什么时候、通过什么关系持有它**：

```csharp
TitleBinding {
  int holderCharacterId;      // 持有者
  int polityId;               // 所属/代表政治实体
  int territoryId;            // 领地
  int adminDivisionId;        // 行政区
  int officeId;               // 官职
  int liegeTitleId;           // 上级头衔
  int contractId;             // 契约
  int startDay/endDay;
  TitleHoldingMode mode;      // single/concurrent/multi-claim/personal union
  float legitimacy;
  int precedence;
}
```

与指代的区别：

| | 指代 | 绑定 |
|---|---|---|
| 回答 | 头衔“关于什么” | 头衔“谁持有、绑在哪” |
| 例子 | 法兰克人、罗马、雅典 | 某人为法兰克人之王、持有某地 |
| 变化 | 可长期不变 | 随持有/继承/征服变化 |
| 显示 | **显示** | **不显示**（除非指代包含） |

---

## 5. 称谓字段（titleAppellation）

称谓回答：**在某个语言/文化/场合怎么叫**：

```csharp
TitleAppellation {
  string appellationId;
  string languageId;
  string cultureId;
  string polityKey;
  string institution;
  TitleRegister register;     // 正式/简称/诗歌/外交
  string template;            // 例如 "{referent}之王"
  int tier;                   // 称谓等级（可升格）
  string upgradeFrom;         // 升格来源
  List<AppellationPart> parts; // 组合称谓
}
```

- 接语言系统：词形、语序、格、冠词、敬称；
- 可升格：`king → emperor`、`chief → king`，由法律/革新/加冕触发；
- 可组合：`King of A and B`、`Augustus` + `Imperator`、个人联统组合称谓；
- 同一头衔在不同语言有不同称谓；
- 不把中文“王/皇帝/可汗”硬编码进逻辑。

---

## 6. 显示规则

只显示：

```text
Display = Appellation(referent) 
```

更准确：

```text
Display = 语言模板( 称谓词 + 指代 )
  ├─ 例：法兰克人 + 之王 → 法兰克人之王
  ├─ 例：雅典 + 将军 → 雅典将军
  ├─ 例：罗马 + 皇帝 → 罗马皇帝
  └─ 多指代：法兰克与洛泰尔之王（按语言模板连接）
```

不显示：

- `kind`（只做分类）；
- `rank/weight`（只做排序/权重）；
- `authorities/modules`（只做规则）；
- `binding`（谁持有、哪块地、契约）；
- `legitimacy/precedence`；
- `eligibility/appointment`。

除非：

- 指代本身包含人物/官职（如“某人的监护人”），才在指代里体现；
- UI 详情页可另开“绑定/权利”栏，不在地图/名单主显示。

---

## 7. 包含关系

```text
TitleDef
  ├─ kind（必须）
  ├─ referents[]（可多）
  ├─ appellations[]（按语言/文化）
  └─ rules（rank/precedence/eligibility/appointment/rights）

TitleInstance
  ├─ titleId
  ├─ selectedReferentIds[]（显示用）
  ├─ selectedAppellationId（显示用）
  ├─ bindings[]（不显示）
  └─ state（active/claim/dormant）
```

- 一个 `TitleDef` 可以有多个 `TitleInstance`（不同持有/不同指代）；
- 一个 `TitleInstance` 可以有多个指代（共治、联统、复合）；
- 显示只用 `selectedReferentIds + selectedAppellationId`；
- 其他字段只进规则/详情页。

---

## 8. 与语言系统的接口

```text
TitleAppellationContext
  ├─ languageId
  ├─ cultureId
  ├─ polityKey
  ├─ institution
  ├─ register
  └─ referent forms
```

`TitleAppellationResolver`：

1. 选语言/文化；
2. 选 register；
3. 取称谓词；
4. 取指代词形（族名/地名/政体名）；
5. 按语言模板组合；
6. 输出显示串。

现有 `TitleAppellationContext` 已有字段，但 `ResolveAppellation` 只返回 `appellationId`，需要真正接语言系统。

---

## 9. 迁移/落地

1. 合并 `TitleDef` 与 `TitleDefinition`，只留一个内容模型 + 一个运行实例；
2. 加 `TitleReferent`（多字段）；
3. 加 `TitleBinding` / `TitleHolding`；
4. 加 `TitleAppellation`（语言/文化/升格/组合）；
5. 显示层只取 `referent + appellation`；
6. 角色页显示头衔列表；地图不显示；
7. 行政区/官职/贵族/荣誉都挂到头衔实例；
8. 旧 `AdminDivision.titleId` 改为 `titleInstanceId`。

---

## 10. 结论

- 头衔种类必须有，但只做分类；
- 显示只显示“指代 + 称谓”；
- 指代是多字段范围，不是单一枚举；
- 绑定字段与指代相近但语义不同，不参与显示；
- 称谓接语言系统，可升格、可组合；
- 现有代码 `TitleDef` 与 `TitleDefinition` 重复、`TitleManager` 未用、没有 `TitleHolding`、`ResolveAppellation` 未接上下文，需要按此模型收口。


---

# 修订：取消硬种类，指代不是组合，称谓才是组合

## 11. 取消 `TitleKind` 硬分类

不再把 `monarch / bureaucratic / noble / honorific` 作为必须字段。

`TitleDef` 只保留：

```text
TitleDef
  ├─ titleId
  ├─ rank / precedence
  ├─ referents[]        // 指代：关于什么
  ├─ bindings[]         // 绑定：谁持有、绑在哪
  ├─ rights[]           // 权利/义务
  ├─ claims[]           // 宣称
  ├─ appellations[]     // 称谓（按语言/文化/升格）
  └─ rules              // 资格/继承/任命/任期
```

**类别由字段推导**，不硬编码：

```text
无 binding、无 rights/claims        → 荣誉头衔 Honorific
binding.officeId 有值               → 官职头衔 Office
binding.territoryId + jurisdiction  → 领地头衔 Landed
binding.polityId + sovereign rights → 统治头衔 Rulership
只有 claims、无 binding             → 宣称头衔 Claim
```

- 荣誉头衔不是“另一种实体”，而是**字段不足/只保留指代与称谓**的头衔；
- 不硬分君主/官僚/贵族；
- UI 可按派生类别筛选，但数据不存 `kind`；
- `rank` 仍可用于排序/礼仪，不参与显示。

## 12. 指代不是“组合”

指代是**范围字段**，可以是多个目标：

```text
法兰克人之王
  referents: [ethnicGroup: Franks]
  bindings: [polity: Frankish kingdom; holder: X]

罗马皇帝
  referents: [polity: Rome / people: Romans]
  bindings: [polity: Roman Empire; holder: Y]

雅典将军
  referents: [settlement: Athens]
  bindings: [office: strategos; holder: Z]
```

- 指代不是把多个头衔拼起来；
- 指代是“这个头衔覆盖谁/什么”；
- 绑定是“谁持有、绑在哪”；
- 两者可以重合，但不是同一字段。

## 13. 真正的“组合”：称谓组合

称谓组合是**语言学层面的内部结构**，不是“一个人持有多个头衔”的显示拼接。

```text
Appellation
  ├─ base            // 头衔词：王、皇帝、可汗
  ├─ referent        // 指代词形：法兰克人、罗马、雅典
  ├─ qualifier       // 语义限定：伟大的、神圣的、胜利的
  ├─ relational      // 关系结构：王中之王、众王之王
  ├─ honorific       // 敬称/前缀：陛下、殿下
  └─ register        // 正式/简称/诗歌/外交
```

### 13.1 限定词（qualifier）

- “伟大的王”“大王”；
- 语义字段：`greatness/holiness/victory/legitimacy`；
- 不是新头衔种类；
- 由语言系统选择词形和位置（前置/后置）。

### 13.2 关系式（relational）

- “王中之王”（King of Kings）；
- 指代不是普通族群/地域，而是**另一个王权/头衔**；
- 历史上：
  - 亚述 `šar šarrāni`（王中之王）；
  - 阿契美尼德 `xšāyaθiya xšāyaθiyānām`；
  - 希腊化 `Basileus Megas`（大王）；
  - 罗马 `Imperator Caesar Augustus` 的累积式头衔；
  - 拜占庭 `Basileus/Autokrator`；
  - 埃及五重头衔；
  - 中国 `尊号` 累积美称。

### 13.3 累积式（titulature chain）

“王中之王、众王之王、伟大的王、万邦之王”不是多个政治实体，而是**同一统治者的称谓链**。

```text
AppellationChain
  ├─ part 1: King of Kings
  ├─ part 2: Great King
  ├─ part 3: King of Countries
  └─ order/语言模板
```

- 与“一个人持有多个头衔”的 `TitleDisplayStack` 不同：
  - `TitleDisplayStack` 是政治/法律事实（多个头衔）；
  - `AppellationChain` 是语言/礼仪表达（一个头衔的多个称谓部分）。

## 14. 升格

```text
AppellationTier
  chief → king → great king → king of kings
```

- 升格由法律/革新/加冕/征服/承认触发；
- 升格不是简单字符串替换，而是称谓层级 + 词形 + 礼仪；
- `upgradeFrom` 记录来源；
- 升格可能创建新 `Appellation`，也可能改 `TitleDef` 的 rank。

## 15. 显示

```text
Display = AppellationRealization(referents, appellation, language, register)
```

- 指代参与词形；
- 称谓决定头衔词/限定/关系式；
- 绑定不显示；
- 多个头衔的显示用 `TitleDisplayStack`，不是称谓组合。

## 16. 学术参考

- [Kings – 'King of Kings'](https://verlag.oeaw.ac.at/api/download/content/9783700139621_kapitel_81_94.pdf)
- [Royal Titulature in Hellenistic Babylonia](https://www.degruyterbrill.com/de/document/doi/10.1515/zava.2002.92.2.241/html)
- [The Great Kings of Asia: Imperial Titulature in the Seleukid and Post-Seleukid Middle East](https://www.degruyterbrill.com/de/document/doi/10.1515/9783110283846-010/)
- [Antiochus III and his Title 'Great King'](http://core-prod.cambridgecore.org/core/journals/journal-of-hellenic-studies/article/abs/antiochus-iii-and-his-title-greatking/D41A64AFA47D894F7E1FF64DF2E4A506)
- [The Great Name: Ancient Egyptian Royal Titulary](https://search.worldcat.org/zh-cn/title/836261158)
- [Basileus/autokrator, Byzantine](https://novel-coronavirus.onlinelibrary.wiley.com/doi/abs/10.1002/9781444338386.wbeah03022)
- [古代尊号制度起始年代考辨](http://data.gzdafzxx.cn/KCMS/detail/detail.aspx?filename=ZHWL201705016&dbcode=CJFQ&dbname=CJFD2017)


---

# 修正：指代与称谓严格分开；称谓是开放组合

## 17. 先认错

前面把“指代”和“称谓”混着讲，是错的。

- **指代 referent**：这个头衔指谁/指什么（族群、文化、聚落、地域、政治实体、行政区、官职、人物……）。
- **称谓 appellation**：这个头衔在某种语言/文化里怎么被称呼。

这是**同一个头衔的两个组成部分**，不是同一个东西。

“称谓的组成”只谈称谓内部，不再跳回指代。

## 18. 称谓不是固定字段，是开放组合

不能预设“base + qualifier + relational + honorific”这种硬槽位。不同语言、不同文明的头衔组合方式不同。

正确模型：

```text
Appellation {
  appellationId;
  languageId;
  scriptId;
  grammarId;                 // 该语言的组合语法
  List<AppellationSegment> segments; // 开放、可扩展
  List<SemanticTag> tags;    // 语义标签，不占固定槽位
  int tier;                  // 称谓层级/升格
}

AppellationSegment {
  string lexicalKey;         // 词/词根：maha, raja, adhi, parama, isvara...
  string surfaceForm;        // 该语言实际写法
  SegmentMorphology morphology; // prefix/suffix/infix/compound/particle
  int order;                 // 语序
  List<SemanticTag> semanticTags; // greatness/supremacy/lordship/...
}
```

- `segments` 是开放列表，不是固定几列；
- 组合规则由 `AppellationGrammar` 提供；
- 语义标签只描述，不硬编码位置；
- 可以一个词、复合词、短语、关系式、整串称谓；
- 语言系统决定连写、分写、变格、sandhi、敬称。

## 19. 印度例子

- `Mahārāja` = `mahā`（大）+ `rāja`（王）；
- `Mahārājādhirāja` = `mahārāja` + `adhirāja`（overking）→ “众王之王/大君”；
- `Rājādhirāja` = `rāja` + `adhirāja`；
- `Parameśvara` = `parama`（最高）+ `īśvara`（主）；
- `Paramabhaṭṭāraka` = `parama` + `bhaṭṭāraka`（尊师/尊主）；
- `Cakravartin`（转轮王）是称谓词，不是组合；
- 朱罗王朝的 `birudas` 是一串称号/美称，属于称谓链/称号列表，不是简单固定槽位。

参考：

- [Indian Epigraphy — Mahārājādhirāja](http://archive.org/download/indianepigraphic00sircuoft/indianepigraphic00sircuoft.pdf)
- ['God of Gods' — 'Foremost king of kings'](https://brill.com/view/journals/iij/67/3/article-p229_2.xml)
- [Royal Titulature in Hellenistic Babylonia](https://www.degruyterbrill.com/de/document/doi/10.1515/zava.2002.92.2.241/html)
- [Praising the king in Tamil during the Pallava period](https://books.openedition.org/ifp/2915?format=embed)
- [Prashasti](https://en.m.wikipedia.org/w/index.php?title=Prashasti)

## 20. 罗马那个不是同一种东西

`Imperator Caesar Augustus` 不是“称谓组合”的同一类：

- 它是**多个名字/头衔的累积**（个人名、家族名、荣誉名、头衔）；
- 不是 `Mahārājādhirāja` 这种单一名词内部的复合；
- 更不是 `King of Kings` 这种关系式称谓。

所以：

```text
称谓内部组合（appellation composition）
  = Mahārājādhirāja / King of Kings / Parameśvara

多个头衔/名字的显示（display stack）
  = Imperator + Caesar + Augustus
  = 一个人同时持有/宣称多个头衔或名号
```

`TitleDisplayStack` 是政治/法律事实；`Appellation` 是语言形式；`AppellationChain` 是同一头衔的多个称谓部分。三者不能混。

## 21. 修正后的头衔结构

```text
Title
  ├─ referents[]      // 指代：指谁/什么（可多个）
  ├─ appellations[]   // 称谓：怎么叫（每种语言/文化/语境）
  ├─ bindings[]       // 绑定：谁持有、绑在哪
  └─ rights/claims    // 权利/宣称
```

- 指代和称谓是并列的两个组成部分；
- 称谓内部是开放的 `segments` 组合；
- 不预设固定槽位；
- 不为“只有这些情况”造枚举；
- 语言系统提供组合语法，内容提供词根/词形。

## 22. 结论

- 指代 ≠ 称谓；
- 指代不是组合；
- 称谓是开放组合，不是固定字段；
- 印度、波斯、希腊化、埃及、中国等都有各自的称谓组合方式，不能用一个硬 schema 套完；
- 罗马累积式是“多个头衔/名字的显示”，不是称谓组合；
- 先改文档，再按 `AppellationSegment + AppellationGrammar` 设计数据。


---

# 追加：尊号不等于头衔

## 23. 尊号是称号/美称，不是头衔

“尊号”不能硬套进 `Title`。很多时候它只是一个**称号/美称**，与头衔有联系，但不是同一种东西。

必须分开：

```text
Title（头衔）
  ├─ referents[]       // 指代
  ├─ appellations[]    // 称谓
  ├─ bindings[]        // 绑定
  └─ rights/claims     // 权利/宣称

HonorificName（尊号/徽号/美称）
  ├─ referent          // 指谁（常为君主/政权）
  ├─ nameForm          // 美称本身
  ├─ category          // 尊号/徽号/称号/谥号/庙号/年号...
  ├─ issuer            // 谁上尊号
  ├─ date              // 何时上
  ├─ languageId
  └─ displayOrder      // 与头衔并排时的顺序
```

关键：

- `Title` 有法律指代/绑定/权利；
- `HonorificName` 可以没有 binding、没有 rights，只是称谓；
- 尊号可以累积、可以改、可以追尊；
- 尊号不等于官职、不等于爵位、不等于主权头衔；
- 不要把尊号塞进 `TitleDisplayStack` 当成“多个头衔”。

## 24. 中国例子

- `皇帝` 是正式称号/头衔性称号；
- `尊号` 是尊崇性美称，如“大圣大明”等；
- `谥号` 是死后评定；
- `庙号` 是宗庙祭祀名号；
- `年号` 是纪年名号；
- 这些都不是同一个字段，不能统一叫 `title`。

## 25. 显示

```text
角色页
  ├─ 头衔 Title（指代 + 称谓）
  ├─ 尊号/称号 HonorificName（独立栏）
  ├─ 谥号/庙号（独立栏）
  └─ 年号（纪年，不是头衔）
```

- 头衔显示只用 `referent + appellation`；
- 尊号单独显示，不参与头衔称谓组合；
- 尊号可以排在头衔旁边，但不是头衔的一部分；
- `TitleDisplayStack` 只放法律头衔，不放尊号。

## 26. 参考

- [古代尊号制度起始年代考辨](http://data.gzdafzxx.cn/KCMS/detail/detail.aspx?filename=ZHWL201705016&dbcode=CJFQ&dbname=CJFD2017)
- [Titles and designations in a study of the Qin and Han dynasties](https://find.library.upenn.edu/catalog/9979032332303681?hld_id=22744785290003681)
- [Chinese temple name](https://en.m.wikipedia.org/wiki/Chinese_temple_name)
