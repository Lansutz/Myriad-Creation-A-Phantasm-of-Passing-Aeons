# 头衔称谓：跨文明组合类型与开放语料

> 2026-11-05。回应“世界上所有已知历史政权头衔”的要求。
> 先说明：不可能在一份文档里穷尽所有政权；但可以建立**开放语料 + 组合类型**，让新文明只加数据、不改 schema。
> 重点：**指代 ≠ 称谓**；称谓内部是开放组合，不是固定字段。

---

## 1. 先定边界

“所有已知政权”在方法上做不到“穷举”，原因：

- 很多政权没有留下自称；
- 很多头衔只有他称/后世追述；
- 同一政权在不同语言/时代有不同称谓；
- 很多“头衔”是短语、句子、图像铭文，不是单词。

可行做法：

```text
开放语料库
  ├─ 每个政权/文明一条或多条 Title 记录
  ├─ 每条 Title 有 referents[] + appellations[]
  ├─ 每种语言一套 AppellationGrammar
  └─ 新例子只加数据，不改固定字段
```

不能再用“只有这些组合方式”的硬 schema。

---

## 2. 称谓组合的跨文明类型

以下不是固定字段，而是**可观察的组合机制**；同一个称谓可以同时用多种机制。

### A. 限定词（qualifier）

- `mahā-`（大）+ `rāja` → `Mahārāja`；
- `parama-`（最高）+ `īśvara` → `Parameśvara`；
- 拉丁 `major`/`maximus`；
- 中文“大/太/高”；
- 语义标签：greatness/supremacy/holiness。

### B. 领属/地域短语（domain phrase）

- `rex Francorum`（法兰克人之王）；
- `rex Anglorum`（英格兰人之王）；
- `king of England`；
- `šar mātāti`（众地之王，若适用）；
- 语法：属格/介词短语/前置定语。

### C. 关系式（relational hierarchy）

- 亚述 `šar šarrāni`（王中之王）；
- 阿契美尼德 `xšāyaθiya xšāyaθiyānām`（王中之王）；
- 埃塞俄比亚 `negusa nagast`（王中之王）；
- 印度 `mahārājādhirāja`（大君/众王之王）；
- 语法：X-of-X 结构、属格、复合。

### D. 复合/连写（compound / sandhi）

- 梵语 `mahārājādhirāja` = `mahā` + `rāja` + `adhirāja`；
- `paramabhaṭṭāraka` = `parama` + `bhaṭṭāraka`；
- 希腊语 `Basileus Megas` 是短语，不是连写；
- 语法：前缀、后缀、sandhi、连字符。

### E. 宗教/德性修饰（epithet）

- 拉丁 `gratia Dei`（蒙神恩）；
- 希腊化 `Soter`（救主）、`Nikator`（胜利者）、`Euergetes`（恩主）；
- 伊斯兰 `al-Muʾayyad`（受助者）等；
- 中文 `尊号` 中的美称；
- 语法：形容词/分词/宗教公式。

### F. 累积/称号链（accumulation / titulature chain）

- 罗马 `Imperator Caesar Augustus`：多个名字/头衔累积；
- 中国 `尊号`：一串美称；
- 朱罗 `birudas`：一串称号；
- 埃及五重头衔：多个名字/称谓；
- **注意**：这是“多个称谓/名字的显示链”，不是单词内部组合。

### G. 敬称/语用层（honorific address）

- `Majesty`、`Highness`、`Dominus`、`Despotes`；
- 中文“陛下/殿下”；
- 不进入正式头衔词干，属于称谓的语用层。

### H. 名号/王号（regnal / dynastic name）

- 登基名、王号、庙号、年号；
- 与头衔词干可组合，也可独立显示；
- 不是“称谓组合”本身，但会进入 `TitleDisplayStack`。

---

## 3. 跨文明例证表

| 文明/地区 | 头衔例 | 组合机制 | 备注 |
|---|---|---|---|
| 苏美尔 | `lugal` | 单词 | 王；`lugal-gal` 可作“大王” |
| 阿卡德/亚述 | `šar šarrāni`、`šarru rabû` | 关系式 + 限定 | 王中之王、大王 |
| 巴比伦 | `šar kiššati` | 领属 | 众地之王 |
| 埃及 | 五重头衔 | 累积 + 修饰 | 荷鲁斯名、两夫人名、金荷鲁斯名、即位名、本名 |
| 赫梯 | `LUGAL.GAL` | 限定 | 大王 |
| 阿契美尼德 | `xšāyaθiya xšāyaθiyānām` | 关系式 | 王中之王 |
| 萨珊 | `šāhān šāh` | 关系式 | 王中之王 |
| 希腊 | `basileus`、`hegemon`、`strategos autokrator` | 单词/短语 | 王、霸主、全权将军 |
| 希腊化 | `Basileus Megas`、`Soter` | 限定 + 修饰 | 大王、救主 |
| 罗马 | `imperator`、`caesar`、`augustus` | 累积 | 不是单词内部组合 |
| 拜占庭 | `basileus`、`autokrator`、`porphyrogennetos` | 单词/修饰 | 皇帝、独裁者、紫室所生 |
| 拉丁欧洲 | `rex Francorum`、`rex Anglorum`、`gratia Dei` | 领属 + 修饰 | 族名/地名 + 王 |
| 伊斯兰 | `khalīfa`、`amīr al-muʾminīn`、`sulṭān`、`malik`、`shāh` | 单词/领属 | 哈里发、信士长官、苏丹、王 |
| 印度 | `rāja`、`mahārāja`、`mahārājādhirāja`、`parameśvara`、`paramabhaṭṭāraka`、`cakravartin`、`samrāṭ` | 限定/复合/关系式 | 王、大王、众王之王、最高主、转轮王 |
| 中国 | `王`、`皇帝`、`天子`、`尊号` | 单词/复合/累积 | 皇帝=皇+帝；尊号是累积美称 |
| 草原 | `qaghan`、`khagan`、`khan`、`il-khan` | 单词/限定 | 可汗、汗、伊利汗 |
| 日本 | `tennō`、`shōgun` | 单词 | 天皇、将军 |
| 东南亚 | `devarāja`、`cakravartin`、`vrah kamrateṅ añ` | 复合/宗教修饰 | 神王、转轮王、最高主 |
| 埃塞俄比亚 | `negus`、`negusa nagast` | 关系式 | 王、王中之王 |
| 美洲 | `Sapa Inca`、`Huey Tlatoani` | 限定/领属 | 大印加、大演说者/大君 |
| 非洲 | `oba`、`mansa`、`mwami` | 单词 | 王/统治者 |
| 大洋洲 | `ariki`、`mo'i`、`tui` | 单词/限定 | 首领/王 |

> 表中例证用于说明组合机制，不代表该文明只有这些头衔。

---

## 4. 关键区分

```text
Title
  ├─ referent（指代）: 指谁/什么
  ├─ appellation（称谓）: 怎么叫
  ├─ bindings（绑定）: 谁持有、绑在哪
  └─ rights/claims

Appellation
  ├─ segments[]（开放）
  ├─ grammarId（语言组合语法）
  └─ tags[]（语义标签）

TitleDisplayStack
  = 一个人/政权同时持有/显示的多个头衔/名字
  ≠ Appellation composition
```

- `Mahārājādhirāja`：**一个称谓内部的组合**；
- `Imperator Caesar Augustus`：**多个名字/头衔的累积显示**；
- `King of Kings, Great King, King of Countries`：**称谓链/累积**，不是多个政治实体；
- “一人持有多头衔”：`TitleDisplayStack`，政治/法律事实，不是称谓组合。

---

## 5. 数据策略

不要建固定字段：

```text
❌ qualifier / relational / honorific 固定列
✅ segments[] + grammarId + tags[]
```

- 新文明只加 `AppellationSegment` 数据和 `AppellationGrammar`；
- 语言系统负责词形、语序、sandhi、格、敬称；
- 内容系统负责词根/词形/语义标签；
- UI 只取 `referent + appellation` 显示；
- `TitleDisplayStack` 单独处理多标题显示。

---

## 6. 参考

- [Kings – 'King of Kings'](https://verlag.oeaw.ac.at/api/download/content/9783700139621_kapitel_81_94.pdf)
- [Royal Titulature in Hellenistic Babylonia](https://www.degruyterbrill.com/de/document/doi/10.1515/zava.2002.92.2.241/html)
- [The Great Kings of Asia: Imperial Titulature](https://www.degruyterbrill.com/de/document/doi/10.1515/9783110283846-010/)
- [Antiochus III and his Title 'Great King'](http://core-prod.cambridgecore.org/core/journals/journal-of-hellenic-studies/article/abs/antiochus-iii-and-his-title-greatking/D41A64AFA47D894F7E1FF64DF2E4A506)
- [The Great Name: Ancient Egyptian Royal Titulary](https://search.worldcat.org/zh-cn/title/836261158)
- [Indian Epigraphy — Mahārājādhirāja](http://archive.org/download/indianepigraphic00sircuoft/indianepigraphic00sircuoft.pdf)
- ['God of Gods' — 'Foremost king of kings'](https://brill.com/view/journals/iij/67/3/article-p229_2.xml)
- [Praising the king in Tamil during the Pallava period](https://books.openedition.org/ifp/2915?format=embed)
- [Negus](https://en.wikipedia.org/wiki/Negus)
- [Implications for the spread of the title khan, khagan](https://www.kci.go.kr/kciportal/landing/article.kci?arti_id=ART002862059)
- [古代尊号制度起始年代考辨](http://data.gzdafzxx.cn/KCMS/detail/detail.aspx?filename=ZHWL201705016&dbcode=CJFQ&dbname=CJFD2017)
- [Of the Royal Names and Their Meanings — Inca](https://www.cambridge.org/core/books/abs/first-part-of-the-royal-commentaries-of-the-yncas/of-the-royal-names-and-their-meanings/0D196A78011EE840BF7665C5F8D329ED)
- [Kingship, Caliphate or Sultanate](https://core.ac.uk/download/198403222.pdf)
- [Thai kingship / devaraja](https://cuir.car.chula.ac.th/xmlui/bitstream/handle/123456789/75828/6288038122.pdf?sequence=1&isAllowed=y)
