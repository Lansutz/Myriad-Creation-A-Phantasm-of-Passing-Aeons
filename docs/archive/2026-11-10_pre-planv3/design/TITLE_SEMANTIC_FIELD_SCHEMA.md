# 完整头衔的结构：指代 + 称谓（语义字段整理 · 修正版 2）

> 2026-11-08 修正。上一版的两个根本错误：
> ① 把「称谓的组合」当成「完整头衔」——完整头衔 = **指代 + 称谓**；
> ② 把「指代可选」当通则——**指代是否可省略，取决于它指代的是什么**：
>   `法兰克人之王`（统治一群人）和 `执政官`（一个官职）不是同一种结构，不能同一套字段处理。
> 本版只整理字段，不写代码。

## 0. 术语表（换词后）

| 英文/旧词 | 我们的词 |
|---|---|
| referent | 指代 |
| referentType | 指代类型 |
| appellation | 称谓 |
| compositionType | 组合类型 |
| stem | 词干 |
| modifier | 修饰成分 |
| distribution | 分布（独立/依附/两可） |
| semanticFeatures | 语义特征 |
| literalMeaning | 本义 |
| etymon | 同源词根 |
| diffusionType | 传播方式 |
| bindingType | 绑定类型 |
| authority | 权威来源 |
| currentAuthority | 现行权威 |
| register | 语域 |
| grammar | 语法 |
| template | 模板 |
| religion / faith / tradition | 宗教 / 信仰 / 传统 |
| ethnicGroup | 族群 |
| adminDivision | 行政区划（建制） |
| settlement | 聚落 |
| polity | 政治实体 |
| office/institution | 官职/机构 |
| dynasty/house | 宗族/家族 |
| person | 人物 |

## 1. 结构总图

```text
完整头衔 = 指代（referent）
         + 称谓（appellation：组合 + 语法/语言）
         + 绑定（binding，不显示）

指代：这个头衔"关于什么/管辖什么/属于哪个机构"
      → 多字段（类型/目标/主次/权重/是否必需）
称谓：在某个语言/文化里"怎么叫"
      → 组合结构（词干/修饰/关系式/复合/称号链）
      + 语言学规则（语序/格/虚词/敬称/模板）
```

**语义特征挂在称谓的词元上**（词干、修饰词、关系标记），不是挂在整个头衔上；**指代不参与"称谓组合"**，但它决定头衔的类型（统治型/官职型/亲属型/荣誉型）。

## 2. 指代字段组（不是"可有可无"一个开关）

| 字段 | 说明 | 例 |
|---|---|---|
| `指代类型` | 指代类型（用项目术语） | **族群**、文化、地域、**行政区划（建制）**、聚落、政治实体、官职/机构、信仰、宗族/家族、人物、none |
| `指代目标` | 目标 id/key | 法兰克人、罗马、雅典、执政官署、某宗族 |
| `指代权重` | 多指代时的主次 | 共治/联统时多指代 |
| `主指代` | 是否主指代 | 法兰克与洛泰尔之王：两个都是主 |
| `显示形态` | 该语言下的指代表达 | 拉丁属格 Francorum；汉语"法兰克人"；英语 of the Franks |
| `必需性` | 是否必需 | 由 `指代类型` 决定，不是全局可选 |

### 2.1 指代类型 → 是否必需（关键规则）

| 指代类型 | 头衔类型 | 是否必需 | 例 | 说明 |
|---|---|---|---|---|
| people / ethnicGroup | 统治型 | **必需** | 法兰克人之王、英格兰人之王 | 统治"一群人" |
| territory / region | 统治型 | **必需** | 罗马皇帝、某地领主 | 统治"一块地" |
| polity | 统治型/官职型 | **必需** | 雅典将军、罗马执政官 | 属于/代表某政治体 |
| office / institution | 官职型 | **必需** | 执政官、宫廷伯、宰相 | 指代是"职位/机构"本身 |
| dynasty / clan | 亲属型 | **必需** | 宗长、族长、某氏之王 | 指代是"宗族/家族" |
| faith | 宗教型 | 必需（若为教职） | 教皇、主教 | 指代是"教会/信仰体" |
| person | 荣誉型/个人头衔 | **可选** | 奥古斯都（对个人的尊号） | 可挂人，也可作纯尊号 |
| none | 少数独立称号 | 允许 | 王、后、皇帝 | 语义上仍隐含政权，但不显式指代 |

结论：**"指代可有可无"只对少数独立称号成立**；统治型、官职型、亲属型、宗教型的指代都是必需的，且类型不同不能混用。

## 2.2 官职的指代不是一种（关键修正）

"官职型 → office 指代"是错的。官职按**管不管地方**分几类，指代类型不同：

| 官职类 | 指代 | 是否必需 | 例 | 说明 |
|---|---|---|---|---|
| 中央官/事务官 | 官职/机构（+可选政治实体） | 必需 | 执政官、宰相、尚书令 | **无地方指代** |
| 地方官 | **行政区划/地域（建制名）** | **必需** | 地方总督、刺史、郡守 | 建制名必须保留 |
| 军事统帅（中央） | 官职/机构 + 军事范围（中外） | 必需 | 都督中外诸军事 | 范围是"中外"，不是具体地名 |
| 军事统帅（区域） | **行政区划/地域（建制名）** + 官职 | **必需** | 都督荆州诸军事 | 建制名"荆州"必须保留 |
| 宫廷官 | 机构/宫廷 | 必需 | 宫廷伯、内侍 | 无地方指代 |
| 使职/差遣 | 辖区（道/镇/路） | 必需 | 节度使（某道）、观察使 | 辖区名保留 |

结论：

- **执政官 ≠ 地方总督**：前者是中央官（无地方指代），后者是地方官（必须有行政区划指代）。
- **都督中外诸军事 ≠ 都督荆州诸军事**：前者是中央军事范围，后者是区域军事；**建制名（荆州）必须保留**，否则两者会被压成同一个"都督"。
- 因此 `指代类型` 必须包含 **行政区划（建制）**；地方官/区域统帅的 `requiredness = 必需`，且 `显示形态` 要带建制名（"都督荆州诸军事"、"荆州刺史"）。

## 2.3 术语对齐（用项目词）

| 之前写法 | 项目术语 |
|---|---|
| people | **族群**（`ethnicGroupId`） |
| territory | **地域**（`regionId`） |
| administrative division | **行政区划/建制**（`AdminDivision`） |
| settlement | 聚落（`settlementId`） |
| polity | 政治实体 |
| office/institution | 官职/机构 |
| dynasty/house | 宗族/家族 |
| person | 人物 |

## 2.4 信仰指代要拆细（宗教系统大改后）

"信仰"不能是一个 `指代类型`。宗教系统要改成**类似族群/文化机制**（见 `FAITH_AS_CULTURE_MODEL.md`）：religion 像族群层、faith 像文化层、tradition 是内容层；神祇/主保/修会/教职是 faith 内部实体。因此信仰类指代要带 **subtype + 对应 id**：

| 子类型 | target | 例 |
|---|---|---|
| religion（宗教，类比族群层） | religionId | 跨信仰的宗教上层称号 |
| faith（信仰，类比文化层，运行单位） | faithId | 某信仰的领袖、教会/教团 |
| tradition（传统，类比文化传统/教义） | traditionId | 某传统的主持、教派 |
| deity（神祇） | deityId | 宙斯祭司、某神庙祭司 |
| patron/venerated（主保/圣人） | veneratedDeadId | 主保圣人、某圣人的守墓人 |
| cult（崇拜/教团） | cultId | 某教团的首领 |
| holyOrder（修会） | holyOrderId | 修会大团长 |
| religiousOffice（教职机构） | officeId | 主教座、教区 |


- 字段：`religiousSubtype` + `religionId/faithId/traditionId/deityId/veneratedDeadId/holyOrderId/officeId`（按 subtype 取用）。
- 规则：宗教系统改到哪一层，就填哪一层；**不要**再用一个笼统的"信仰"字段。

> 信仰像文化一样有**地区/人口构成**（多信仰 shares），头衔指代应指向 `FaithData`（信仰运行单位），不是笼统的"信仰"；教职头衔 = faithId + 官职/机构。

## 2.5 亲属指代（"某统治者的亲属"）

借鉴 CK3 的亲属/家族机制，但换成我们的字段：

CK3 可借鉴的点：

- `dynasty` / `house`（王朝/家族）+ `house head` / `dynasty head`（家主/族长）；
- `close family`（近亲）与 `is_close_family_of` 判定；
- `prince/princess` 是"统治者子女"的描述性身份，不是独立爵位；
- `queen mother`（太后）、`regent`（摄政）是角色/职位，不是头衔等级；
- 通过亲属关系产生的 **claim（继承权）**；
- cadet house（分支家族）。

适配我们的字段：

```text
kinReferent {
  anchorCharacterId     // 以谁为基准（通常是统治者）
  relationKind          // father/mother/son/daughter/sibling/uncle/aunt/
                        // cousin/nephew/niece/grandparent/grandchild/spouse
  direction             // 上行/下行/旁系
  legitimacy            // 婚生/私生（影响能否承袭称号）
  seniority             // 长幼（伯/仲/叔/季）
  houseId/familyId      // 所属宗族/家族
  derivedFrom           // 若为间接关系（如"叔"=父之弟），记录推导链
}
```

用途与规则：

| 场景 | 例 | 说明 |
|---|---|---|
| 亲属尊称 | 王叔、王弟、王子、太后 | 指代 = kin（锚定统治者 + 关系）；称谓用亲属词/复合 |
| 摄政 | 叔父摄政、太后摄政 | 摄政是**角色/职权**，不是头衔；头衔可另有 |
| 继承权 | 某王之侄的继承主张 | 亲属关系产生 claim，进入继承/宣称系统 |
| 家族头衔 | 某氏之主、家主 | 指代 = 宗族/家族 + 家主身份 |

结论：

- 亲属指代是独立的 `referentType = kin`，不是 `person`；
- 亲属称谓不等于头衔等级（王叔不是"王的下一级"）；
- 婚生/私生、长幼、宗族归属必须进字段，否则"叔/伯/弟/私生子"无法区分；
- CK3 的 prince/princess 只作描述性身份，不照搬成爵位。

## 3. 称谓字段组

| 字段 | 说明 | 取值/例 |
|---|---|---|
| `组合类型` | 组合结构 | standalone / modifier_core / relational / compound / accumulation / domain_phrase / honorific_chain |
| `词干` | 核心词干 | 王、皇帝、宗长、将军、执政官、公爵、演说者 |
| `修饰成分` | 修饰成分（bound） | 大、至高、副、首席、神/圣（视分布） |
| `关系标记` | 关系标记 | 之、of、属格、construct state |
| `敬称` | 敬称 | 陛下、殿下、Majesty |
| `语域` | 语域 | formal / short / poetic / diplomatic |
| `语法` | 语言学规则 | 语序、格、虚词、冠词、性数一致 |
| `模板` | 显示模板 | `{referent}之{stem}`、`{stem} of {referent}`、`{referent}.{stem}` |

### 3.1 词元的分布（决定它是不是"修饰"）

| 词元 | 组合角色 | distribution | 例 |
|---|---|---|---|
| 大 | modifier | **bound** | 大王；不能单称"大" |
| 至高 | modifier | **bound** | 至高王 |
| 副/藩 | modifier | **bound** | 副王、藩王 |
| 王 | stem | **standalone** | 王 |
| 神 | stem/修饰两可 | **both** | 神（独立）、神王（修饰） |
| 圣 | stem/修饰两可 | **both** | 圣人（独立）、圣王（修饰） |
| 王中之王 | 关系式整体 | **standalone** | 完整称谓，不是修饰 |

## 4. 答辩：法兰克人之王 ≠ 执政官

| | 法兰克人之王 | 执政官 |
|---|---|---|
| 头衔类型 | 统治型 | 官职型 |
| 指代 | **people（法兰克人）**，必需 | **office（执政官署）**，必需 |
| 称谓组合 | `domain_phrase`：{指代} + 之 + 王 | `standalone`：执政官 |
| 语言学 | 汉语"之"+前置定语；拉丁属格 Francorum；英语 of the Franks | 官职名词本身；可加城邦形容词（雅典执政官） |
| 权力来源 | 对人群的统治 | 城邦授予的职位 |
| 可替换性 | 换成"英格兰人之王"只是换指代 | 换成"将军"是换官职，不是换指代 |

**它们不是同一种东西**：一个是"统治某人群"，一个是"担任某职位"；指代的类型不同，称谓的模板也不同。把它们塞进同一个"指代可选"字段就是把结构压平了。

## 5. 更多例子（举一反三）

| 完整头衔 | 指代类型 | 称谓组合 | 语言学要点 |
|---|---|---|---|
| 法兰克人之王 | people | domain_phrase（X 之王） | 汉语"之"；拉丁属格；英语 of |
| 罗马皇帝 | territory/polity | compound（皇+帝），带地名形容词 | 拉丁 Romanus；汉语"罗马"+皇帝 |
| 雅典将军 | polity + office | standalone（将军） | 城邦形容词/属格 |
| 执政官 | office | standalone | 官职名；无人群指代 |
| 宗长 | clan/dynasty | standalone（亲属词） | 亲属称谓系统 |
| 后 | person/宫廷 | standalone | 本义君主→后妃（语义漂移） |
| Herzog | territory/部族 | standalone | 德语复合/仿译 dux |
| knyaz | clan/people | standalone | 斯拉夫；与 king 同源 |
| 奥古斯都 | person（可选） | standalone（尊号） | 形容词→头衔 |
| 王中之王 | polity（隐含） | relational（X-of-X） | 属格/关系式；称号公式 |

## 6. 语言学接口（由语言系统提供，不在语义字段里硬编码）

- 语序：汉语前置定语 + "之/的"；英语 of-短语或形容词；拉丁属格；阿拉伯 construct state；希腊属格。
- 格/数/性：指代在称谓中的形态由语言系统给出（Francorum 是属格复数）。
- 敬称/语域：陛下/殿下/Majesty 属 honorific 层，不进词干。
- 头衔升格/组合：`king → emperor`、`A 与 B 之王`（多指代）、`尊号链`（累积）都由语言模板 + 组合类型处理。

## 7. 待确认

1. 指代类型表（§2）是否完整；每类的 `必需性` 规则是否同意；
2. 词元分布（bound/standalone/both）是否足够表达"神/圣"这类两可词；
3. 语言学模板字段是否由现有语言系统提供（本字段只存模板 id/槽位，不硬编码语序）。
