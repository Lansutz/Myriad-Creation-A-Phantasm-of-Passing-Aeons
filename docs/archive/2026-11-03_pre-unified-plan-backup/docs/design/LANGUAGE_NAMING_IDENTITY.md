# 语言 / 命名 / 族群分化：模型设计

> 需求来源：用户指出（1）音节/命名必须接语言学系统，涉及音系与语义；（2）需要设计"一个族群在什么时候意识到自己的语言与另一个语言不同，并据此命名"；
> （3）civ-atlas 的族群分离机制不行，我们要做得更好。
> 本文基于本仓库现有 `LanguageDef`、`PlaceNameGenerator`、`CultureData`、`EthnicGroupDef`、`LanguageFamilyNode`、
> `CultureLineage`、`ContentRegistry.GetRandomName` 的代码事实，给出可落地的模型与字段联系；不改代码。

> **术语修正（以用户澄清为准）**：
> 1. 我们的认同载体是 **族群（ethnos/people）和大起源（heritage/macro-origin）**；文化**嫁接**在认同上（`CultureData.ethnicGroupId/heritageId`），并会随原因改变。
> 2. 我们一般说"什么人"（族群），**不称"民族/某某族"**。"民族（nation）"是近代概念，对应革新"民族主义"→"民族国家"。
> 3. 族群机制到 **氏族→族群** 为止；**"语族"是语言学概念，不是认同层，不得引入身份模型**。语言谱系（`LanguageFamilyNode`）只服务语言学/命名。
> 4. 文化分离与认同分离是**两个相关但不同的机制**：文化分离来自距离/阻隔+地理→物产/生活方式→传统变化；认同分离主要来自政治整合/政权分裂（见 §4.3）。

---

## 1. 现有代码事实（起点）

| 已有 | 内容 | 缺口 |
|---|---|---|
| `LanguageDef` | `languageId/glottocode/familyId/parentLanguageId`、`sampleWords`、`male/female/family/cityNames`、`placeSuffixes`、`terrainWords`；名字是**固定词表** | 无音系、无音节规则、无构词/语义、无音变、无方言/接触 |
| `PlaceNameGenerator` | `terrainWords`（按语义）+ `placeSuffixes` + `AllowedSuffixes` 组合（如 山+城/关） | 语义组合有了，但词形来自词表；无形态变化、无转写、无多语言变体 |
| `CultureData` | `languageId/languageFamilyId/ethnicGroupId/heritageId/ethosId/traditionIds` | **已有嫁接点**：`ethnicGroupId`（族群）、`heritageId`（大起源）；但无语言感知/无认同分离历史；`Heritage` 内容目录/`Heritages.json` 目前缺失（加载器已支持） |
| `EthnicGroupDef` | `groupId/cultureId/languageId/traditionIds/innovationIds` | 族名/描述来自本地化；无自称/他称、无分化历史 |
| `LanguageFamilyNode` | `id/parent/name/level/macroarea` | 只是分类树，无音变/年代/接触 |
| `ContentRegistry.GetRandomName` | 从 `LanguageDef` 的名字表随机取 | 名字与语言音系/语义无关，无法生成新词/方言名 |
| `CultureLineage` | 用 `LanguageDef.parentLanguageId` 建立文化谱系 | 只做初始化，无运行时分化 |

结论：**命名目前是"查表"，不是"语言生成"**；族群是"文化+族名"，没有"认知边界"和"自称/他称"。这正是要补的。

---

## 2. 语言学模型（音节/词/语义）

### 2.1 音系（Phonology）

```csharp
LanguagePhonology {
    string languageId;
    List<Phoneme> consonants;   // place/manner/voicing/aspirated…
    List<Phoneme> vowels;       // height/backness/rounding/length/tone
    List<string> tones;         // 声调（可选）
    List<SyllableTemplate> syllableTemplates; // 权重：CV, CVC, CCV, V, CVV…
    List<PhonotacticRule> constraints;        // 禁音组合、响度序列、元音和谐
    StressRule stress;          // 重音位置/强弱
    List<SoundChangeRule> historicalChanges;  // 有序音变（从祖语到本语）
}
```

- 音节生成：按模板加权 + 响度序列/禁音检查；同一语言内保持稳定，不同语言不同（不随机拼字母）。
- 声调/重音影响转写与名字节奏；诗歌/宗教用语可有独立语域（register）。
- `SoundChangeRule`：如 `p > f / _V`、词尾清化、元音高化、辅音弱化；按年代排序，用于**祖语→ daughter languages** 的分化。

### 2.2 词库与语义（Lexicon / Semantics）

```csharp
Lexeme {
    string id;
    string languageId;
    string form;              // 音系生成/继承
    List<string> glosses;     // 语义：water/river/mountain/forest/stone/home/fort/market/people/speech…
    string pos;               // root/noun/verb/adjective/affix
    List<string> tags;        // 神圣/禁忌/古语/借词
    string etymology;         // 来源（祖语/借词/拟声/复合）
}
Morpheme / Affix {
    string semantic;          // place-of / people-of / diminutive / augmentative / honorific
    string form;              // 语言特定
    MorphologyType type;      // prefix/suffix/infix/reduplication
}
```

- 地名生成 = **语义骨架 + 语言形态**：
  - 语义骨架：`[地貌/资源/方位/人物/事件] + [通名：城/关/渡/山/水/乡/国]`
  - 形态：前缀/后缀/复合/所有格/复数/敬语；按语言规则变形。
  - 例：`水` root + `城` suffix；`山` root + `关` suffix；`渡口` 专用词；`新`+旧名（殖民/重建）。
- `PlaceSuffixDef`/`terrainWords` 升级为带 `semantic`、`form`、`languageId`、`register` 的词条；可内容化 JSON。
- 语义不是装饰：地名会反映"那里有什么"（河/山/盐/铁/森林）和"谁命名的"（族群/事件/宗教），与物产/环境系统联动。

### 2.3 命名生成算法

```
Name(kind, language, context):
  pattern = weightedPick(language.namePatterns[kind])   // city/region/river/mountain/person/…
  slots   = pattern.slots                                // modifier, generic, suffix, honorific
  for slot:
      semantic = context.semantic(slot)                  // 地形/资源/历史/方位/人物
      lexeme   = language.Lexicon.Find(semantic, register) // 没有就派生/借词/拟声
      form     = language.Morphology.Inflect(lexeme, slot, context)
  apply phonotactic repair (禁音/响度/重复)
  apply sound changes for current language variety
  transliterate to target script
```

- 生成器要**可复现**：seed + languageId + 语义/坐标派生，跨平台一致（可借 civ-atlas 的 `round24`/`subSeed` 思路）。
- 词库不足时：派生（root+affix）、复合、借词（从接触语言）、拟声/描述；记录 etymology。
- 名字类型：人名/族名/地名/国名/语言名/神名/头衔名，各有 pattern。
- 语言名：**自称（autonym）**与**他称（exonym）**分开：
  - autonym：本语言中"我们的语言/话"（如 `*tir`“舌头/说”），由自己命名；
  - exonym：邻族按自己语言音系转写/意译，或按"他们说 X"起名；
  - 通用名：地图/学界/通用语采用的名（可以是 exonym 或标准名）。

### 2.4 命名的合法组合与语义字段（语言学负责）

- 我们生成的是**词（lexeme）**，不是词根；词本身可由多个语素/词复合而成。语素分类（含词根分类）由语言学系统提供。
- 语义字段分组（便于组合合法性）：
  - 抽象概念：自由、正义、荣耀、命运、王权…
  - 物体/资源：水、石、铁、盐、木、马、鱼…
  - 地点/地貌：山、河、湖、海、谷、原、岛…
  - 方位/关系：东、西、南、北、上、下、内、外…
  - 人物/群体：人、民、父、母、王、祭司…
  - 动作/事件：建、渡、战、居、守…
- 每种命名类型有**合法组合规则**（由语言学定义，不是硬编码 if）：
  - 人名：名+姓/父名/族名/头衔，性别/格/敬语一致；
  - 地名（地域/聚落）：[修饰语/地貌/资源/人物/事件]+[通名：城/关/渡/山/水/乡]；
  - 省份名：常由聚落/地貌/方位派生（"某州""某郡"），需符合形态与历史；
  - 头衔名：官职/军职/祭司/荣誉，受政体/宗教/语言限制；
  - 族群名：自称（autonym，来自"人/说话/自由"等语义）或他称（exonym，来自地名/特征/语言名）。
- **合法性检查**：音系合法（音节/禁音）、形态合法（词类/格/数/敬语）、语义搭配合法（动作配对象、通名配地貌）、历史/禁忌（避讳、禁忌词）。
- 法兰克例子作为历史模型：**政治联盟（萨利安人等）→ "自由人"的民间词源/他称 → 族群认同 → "法兰克人之地"**。名字可以是政治/民间词源的产物，不必是"真实语义"；词源与认同形成要能分别记录。---

## 3. 语言差异的"意识"：什么时候发现"他们说的是另一种话"

### 3.1 认知层次

```csharp
LanguageAwareness {
    int observerGroupId;        // 观察者族群/政体/阶层
    string subjectLanguageId;   // 被观察的语言/方言
    AwarenessLevel level;       // Unaware/Heard/Recognized/Different/Distinct/Standardized
    float intelligibility;      // 0..1 互通度（观察者视角）
    int firstContactYear;
    float contactIntensity;     // 贸易/战争/婚姻/迁徙/传教累积
    string autonym;             // 对方自称（若知道）
    string exonym;              // 观察者给的名字
    string note;                // 事件/来源
}
```

### 3.2 判定规则（不是单一阈值）

一个族群把另一群人的话认成"另一种语言"需要同时具备：

1. **接触**：贸易/战争/通婚/迁徙/传教/使节等累积到一定强度；
2. **互通度低**：基于词库重叠（Swadesh 类核心词）+ 音系距离 + 形态差异计算 `intelligibility`；
3. **边界**：政治/宗教/文字/制度边界强化"我们 vs 他们"（同一语言也可因政治/宗教被说成两种语言；反之方言可被统一为标准语）；
4. **精英/标准语**：有文字、经典、国家教育/宗教仪式时，语言被"标准化"，边界更清晰；
5. **时间**：接触持续一代以上、跨代传承。

```text
P(recognize as distinct) = f(intelligibility, contactIntensity, boundaryStrength, standardization, time)
intelligibility = w1·lexicalOverlap + w2·phonologicalSimilarity + w3·morphologicalSimilarity
```

- 互通度是**不对称**的（强者/标准语使用者可能听懂方言，反向不一定）。
- 触发事件：`LanguageBoundaryRecognized`（记录名字、年份、双方、原因）；可被玩家/事件修改。
- 方言 vs 语言：同一 `LanguageVariety` 连续体；`LanguageAwareness` 可在不同观察者/时期给出不同结论（"方言"或"语言"），这本身是政治/文化选择。

### 3.3 与命名的关系

- **先有认知，才有名字**：族群不会给没接触过的区域/群体命名（Earth 的优点）。
- 接触后：
  1. 给"对方的话"起名（exonym），记录对方自称（若学到）；
  2. 给对方族群起名（ethnonym，常来自地名/语言名/特征）；
  3. 给对方区域起名（exonym），与自己的地名并存；通用名可从中选/另立；
  4. 语言名影响族名、地名、头衔名（如"某语之地""某族之王"）。
- 反向：被命名者也可能给观察者起名；名字进入双方历史/编年史。

### 3.4 字段联系（先接这些）

| 字段 | 所在 | 用途 |
|---|---|---|
| `CultureData.languageId` | Culture | 文化→语言（已有） |
| `EthnicGroupDef.languageId` | Content | 族群→语言（已有） |
| `LanguageDef.parentLanguageId/familyId` | Content | 谱系（已有） |
| 新增 `LanguageVariety` | Content/Simulation | 方言/变体、互通度 |
| 新增 `LanguageAwareness` | Simulation | 观察者→语言认知 |
| 新增 `RegionNameRecord` | Core/Simulation | 通用名+族群名（见 CIV_COGNITION 文档） |
| 新增 `PopGroup` | Simulation | 聚落/区域的族群/语言/宗教构成（见 §4） |
| `SettlementData` 名/`CharacterData` 名 | Simulation | 记录生成时的语言/词源 |
| `TitleDef.cultureId`/新 `languageId` | Content | 头衔名的语言风格 |

---

## 4. 族群分化：比 civ-atlas 更好的模型

### 4.1 civ-atlas 的做法与问题

- 一个州一个 `Culture` 归属（硬分区，文化=地块颜色）；`assimilation.ts` 用概率改州归属；`diverge` 在"地理隔断/不同国家"时按面积/定居年限分裂，新族名=母族+方位（"北某某"）。
- 问题：
  1. **硬分区**：没有混合人口/多族群共存，边界是格子；
  2. **单一维度**：族群=文化，缺少语言/宗教/阶层/血缘的交叉；
  3. **无认知**：没有"我们/他们"的语言感知、自称/他称；
  4. **分化原因单薄**：只看地理+国家，缺少经济网络、通婚、制度、精英策略、宗教；
  5. **名字后置**：族名由命名风格统一生成，不是群体自我命名/他称的历史结果；
  6. **不可嵌套**：没有"某族内部的分支/氏族/教派"。

### 4.2 我们的认同/文化模型（两个载体：族群 + 大起源）

```csharp
// 认同载体（已有字段）：CultureData.ethnicGroupId / heritageId
IdentityCarrier {
    string id;
    CarrierKind kind;         // EthnicGroup(族群) / Heritage(大起源) —— 不引入语族
    string parentId;          // 氏族→族群；大起源可嵌套/分支（历史来源）
    List<string> markers;     // 语言/习俗/宗教/神话/服饰/饮食…
    List<Boundary> boundaries;// 与谁有边界、强度、原因
}
CultureData {
    int cultureId;
    string languageId;
    string ethnicGroupId;     // 族群认同
    string heritageId;        // 大起源认同
    List<string> traditionIds;// 文化=可嫁接的传统组合
}
```

- **文化是嫁接在认同上的属性/传统组合**，不是认同本身；同一族群/大起源下可有多个文化，文化也可因通婚/迁徙/政治/经济改变嫁接对象。
- 人口不是"一格一个文化"：一个聚落可同时有多个族群/文化/语言/宗教/阶层的**份额**（`PopGroup`，允许混居）。
- 认同的嵌套只说 **氏族→族群**；大起源是另一条历史来源（可跨族群共享），二者交叉但不互为层级。
- 语言是文化/认同的标记之一，不是认同层；语言谱系只用于语言学与命名。

### 4.2b 文化分离 vs 认同分离（两种机制，不可混成抽象数值）

**文化分离（cultural divergence）**：由距离/阻隔 + 地理地貌差异 → 物产与生活方式不同 → **传统发生变化**（不是"文化相似度下降"这种抽象数）。我们的传统已有分类 `TraditionCategory`（Habitation/Production/Political/Ritual/Societal）与因子（SettlementDegree/SeparationTendency/FusionTendency/ResourcePreference），分离应发生在**传统项**上：
- 新的生产/居住/仪式传统出现、旧传统被替换/组合；
- `CultureSimilarity` 只做派生度量（已有），不是分离的驱动力；
- 地理只是权重：同样的距离+地貌，若物产/生计不变，文化未必分离。

**认同分离（identity separation）**：主要与**政治整合**有关：
- 一个认同下可以挂多个文化；即使长期同属一个政权/认同，一旦分裂成两个或多个政权，且**分离持续时间超过此前统一时间**（或用更真实的判定：政治整合度跌破阈值、精英/制度/法律分道扬镳），认同就可能分离；
- 认同融合则来自政治整合（共同政权/法律/公民权/精英联姻/共同叙事）；
- 文化分离不必然导致认同分离（同一认同可容纳多种文化）；认同分离也不必然产生全新文化（可保留语言/传统，只换政治认同）。

代码现状：`CultureTransition`（融合/分离倾向、相似度）、`CultureData.parentCultureId/allowsBranching/maturity`、`CultureStage` 已有；**缺少认同层的历史**（何时统一/何时分裂/统一时长），需要新增 `IdentityHistory`（统一区间、政权归属、分离事件）来判定认同分离。### 4.3 分化/融合机制（多因，且有行动者）

身份边界强度 `B(dim, a, b)` 由多项加权：

| 因素 | 作用 |
|---|---|
| 语言互通度 | 低互通 → 边界强（沟通成本） |
| 通婚/亲属网络 | 高互通 → 边界弱（血缘） |
| 市场/贸易依赖 | 高依赖 → 边界弱（经济利益） |
| 法律/公民身份 | 同权利 → 边界弱；歧视性法律 → 边界强 |
| 宗教/仪式 | 不同教派/禁忌 → 边界强 |
| 政治/精英策略 | 精英强调"我们" → 边界强（族群 entrepreneurs） |
| 地理/交通 | 山/海/距离 → 边界强（但只是权重，不是决定） |
| 历史创伤/战争 | 边界强（记忆） |
| 文字/标准语 | 标准化 → 语言边界清晰，可能压平方言 |

事件：
- `BoundaryFormation`（边界形成：语言/宗教/法律/战争/精英叙事）；
- `Assimilation`（通婚/市场/教育/迁入 → 身份份额变化，不是瞬间换色）；
- `Ethnogenesis`（新族群从多群体融合/分裂，产生新自称/他称）；
- `LanguageShift`（语言转用，可能保留祖语宗教/地名）；
- `Diaspora`/`Migration`（群体迁出/迁入，保持身份）；
- `Syncretism`（宗教/文化融合）；
- 所有事件有**发起者/受益者/反对者**（精英、阶层、政体），不是纯随机概率。

### 4.4 与命名/头衔/政治的连接

- 族群分化 → 新自称/他称 → `RegionNameRecord` 的族群名增加 → 地图标签/编年史变化；
- 语言分化 → 新语言变体/语言名 → 命名风格变化（daughter language 音变）；
- 头衔：头衔名可按持有者语言/文化生成；头衔的法理宣称可绑定"族群/宗教"合法性；
- 政体：`IPolityRef` 有主导族群/语言/宗教构成，政策（同化/宽容/隔离）影响边界强度；
- 战争：不是"文化 vs 文化"，而是政体/精英为资源/合法性/边界而战；族群叙事可被动员。

### 4.5 反例测试（避免 civ-atlas 的问题）

1. 两个族群共居一城（份额 55/45），边界不应硬切成两色；
2. 同一语言因宗教/政治分裂成两个"语言名"（方言被标准化为语言）；
3. 两族群语言互通但长期通婚 → 边界减弱，出现混合身份/新自称；
4. 地理隔绝但通过海路/贸易保持联系 → 语言不分化；
5. 精英推行标准语 → 方言被压制但民间仍用，出现双言制（diglossia）；
6. 迁徙群体保持语言/宗教，但在经济上同化 → 身份维度不同步变化。

---

### 4.6b 现有内容数据对照（代码事实）

- `CultureStage`：`Band(游群)→Tribe(部落)→Chiefdom(酋邦)→EthnicGroup(族群)→HighCivilization(高级文明)`——我们的"氏族→族群"已体现在阶段上，不再引入"语族"。
- `CultureData.ethnicGroupId`（族群）与 `heritageId`（大起源）就是**文化嫁接点**；`CultureTransition` 已有融合/分离倾向、`CultureSimilarity` 从传统/因子派生，`CultureStageCatalog` 决定阶段门槛。
- **缺口 1**：`Heritage` 内容目录/`Heritages.json` 缺失（`ContentDefinitionIndex` 加载器已支持 `Base/Heritage/Heritages.json`，但 `StreamingAssets/Base` 无 Heritage 目录）——大起源目前没有内容数据。
- **缺口 2**：民族主义门槛与用户定义不一致。当前 `Innovations.json`：
  - `1017 民族主义` 前置 `[1016 报纸, 600 文字]`；
  - `1018 民族国家` 前置 `[1017, 503 官僚制度]`。
  但用户定义为**边界固定 + 先进印刷 + 公民权，三者缺一不可**。需要新增/指定：
  - `边界概念`（领土/主权边界可被想象与主张）——内容里没有；
  - `公民权`（法律成员身份，超越血缘/地方）——内容里没有（只有 `980 雅典公民议事` 是具体制度，不等于普遍公民权）；
  - 印刷已有（`206/979`，报纸 `1016`）。
  建议新增 2 个革新（如 1019 边界概念、1020 公民权），把 `1017` 前置改为 `[1016, 600, 1019, 1020]`（全部必要），`1018` 再加 `503`；并给现有测试补门槛断言。
- **缺口 3**：认同层没有"统一/分裂历史"字段，无法判定"分裂持续时间 > 此前统一时间"；需 `IdentityHistory`（统一区间/政权归属/分离事件）。

### 4.6 民族主义 / 民族国家（术语与革新门槛）

- 我们不说"民族"，说**族群**；"民族（nation）"是近代政治概念，对应革新"民族主义"与政体"民族国家"。
- 民族主义需要**三者同时具备**，缺一不可：
  1. **边界概念的固定**（领土/主权边界可被想象与主张）；
  2. **先进的印刷技术**（印刷/普及文字，形成共同想象共同体）；
  3. **公民权**（法律上的成员身份，超越血缘/地方）。
- 只有民族主义之后，才可能形成**民族国家**（nation-state）这一政体/合法性形态；在此之前是王朝/城邦/帝国/部落联盟等。
- 实现：`UnlockRule` 的 `target = PolityForm.NationState`，`requiredInnovations = {边界概念, 印刷术, 公民权}`（全部必要，不是加权）；民族主义本身也可作为 `CultureTrait`/`Ideology` 解锁；`RealmForm`/`GovernmentForm` 增加 NationState，需合法性来源变化。
- 不要把"族群"直接当"民族"；族群可以没有民族主义/民族国家，也可以跨政权分布。

## 5. 落地阶段（建议）

| 阶段 | 内容 | 验收 |
|---|---|---|
| L1 字段接线 | `LanguageVariety`/`Lexeme`/`Phonology` 基础类型；`CultureData`/`EthnicGroupDef`/`PopGroup`/`RegionNameRecord` 的 `languageId` 贯通；内容 JSON schema | 现有测试全绿；所有命名调用可拿到 `languageId` |
| L2 语言生成 | 音系/音节模板/词库/形态/命名 pattern；`PlaceNameGenerator` 改为语言生成；内容可加载 | 同 seed 可复现；不同语言名字风格可区分；单测 |
| L1.5 Data Forge | 接入 `conlang-inventories.json`（音段库存）、`core-concept-lists.json`（核心概念/语义组）、Forge 的命名合法性协议；`_generated` header/lock/license 门禁 | 见 [DATA_FORGE_INTEGRATION.md](DATA_FORGE_INTEGRATION.md)；`DatasetIntegrityTests` 全绿 |
| L3 认知 | `LanguageAwareness`、互通度、接触/边界判定；语言名（自称/他称）事件 | "接触→认知→命名"测试；未接触不命名 |
| L4 族群多层 | `PopGroup` 构成、身份 salience、边界强度；同化/分化/民族生成事件（多因） | 混居/分化/融合反例测试 |
| L5 联动 | 命名/头衔/政体/宗教/编年史接入；UI 显示自称/他称/语言名 | PlayMode/UI 测试 |

**先做 L1（字段接线）**：不加字段，后面的生成器和事件都无处落。L1 具体：
- `LanguageDef` 扩展 `phonologyId`、`lexiconId`、`namePatternIds`（引用 Content JSON，不破坏旧字段）；
- `CultureData` 已有 `languageId`，补 `languageVarietyId`（方言）；
- `EthnicGroupDef` 已有 `languageId`，补 `autonymKey`/`exonymKey`（本地化/语言生成）；
- 新增 `LanguageVariety`（languageId + parentVarietyId + region/group + intelligibility 参数）；
- `PopGroup` 先挂到 Settlement/Region（可以先用现有 `PopulationBlock` 扩展）；
- `RegionNameRecord` 与 `LanguageAwareness` 放 Core.Contracts/Simulation。

---

## 6. 结论

- 音节不是"随机拼字母"，而是**音系 + 词库 + 形态 + 语义**的生成；先接 `languageId` 字段，再把 `PlaceNameGenerator` 从查表升级为语言生成。
- "语言差异意识"是**接触 + 互通度 + 边界 + 标准化 + 时间**的多因判定；它决定何时出现语言名、族群名、地名 exonym 和通用名。
- 族群分离不能像 civ-atlas 那样"一格一文化、按地理/国家概率分裂"；我们采用**多层身份 + 人口构成 + 边界强度 + 有行动者的事件**，允许混居、嵌套、双言、多重认同。
- 与之前的命名/头衔文档串联：`RegionNameRecord`（通用名+族群名）→ `LanguageAwareness`（谁在什么时候命名）→ `PopGroup`/`IdentityProfile`（谁是我者/他者）→ `TitleInstance`（头衔绑定的政治实体有主导语言/族群）。

> 相关文档：[CIV_COGNITION_TITLE_CURRENCY.md](CIV_COGNITION_TITLE_CURRENCY.md)、[REFERENCE_PROJECTS_ANALYSIS.md](REFERENCE_PROJECTS_ANALYSIS.md)。
