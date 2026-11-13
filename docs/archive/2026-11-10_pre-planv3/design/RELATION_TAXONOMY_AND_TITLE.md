# 关系机制与头衔系统：分类模型

> 2026-11-05。先读现有关系/头衔代码，再给分类；不另起平行系统。
> 现有代码：`EntityKind`、`EntityRelation`、`RelationType`、`RelationRules`、`RelationRegistry`、`Subordination`、`DiplomaticRelation`、`Treaty`、`TitleDefinition`、`TitleManager`、`RealmData`。

---

## 1. 现有代码事实

| 概念 | 代码 | 真实行为 |
|---|---|---|
| 实体种类 | `EntityKind` | `Realm / Unit / Anchor / Province / Character` |
| 关系 | `EntityRelation` | `from/to/type/legitimacy/stability/isPrimary/obligations` |
| 关系类型 | `RelationType` | 17 种：DirectRule、Vassalage、PersonalUnion、Protectorate、Lease、Condominium、Tributary、Command、Garrison 等 |
| 端点规则 | `RelationRules` | 按 `EntityKind` 硬编码允许矩阵 |
| 关系存储 | `RelationRegistry` | 按 `to` 索引；`isPrimary` 单主关系 |
| 从属 | `Subordination` | 政权→政权，suzerain/vassal |
| 外交 | `DiplomaticRelation` | 政权对政权；`specialBond` 有 `PersonalUnion` |
| 条约 | `Treaty` | signer 是 realm id |
| 头衔 | `TitleDefinition` | `superiorTitle`、`seats`、`appointmentOrder`、`inheritable`；偏官职 |
| 头衔运行 | **没有 `TitleHolding` 类** | `TitleHoldingMode` 枚举存在但未使用 |
| 附庸 | `RealmData.suzerainId / vassalIds` | **把附庸当政权** |

---

## 2. 关系分六类（按端点）

```text
P = 政治实体 Polity
S = 聚落 Settlement
U = 单位实体 Unit
```

| 类 | 端点 | 主要关系 |
|---|---|---|
| R1 | P ↔ P | 君合/联统、复合成员、封臣、保护国、朝贡、同盟、战争、停战、贸易、共管、租借、直接统治 |
| R2 | S ↔ S | 空间从属、道路/贸易联系、文化/宗教联系、军事支援、边境相邻、供给 |
| R3 | U ↔ U | 指挥/从属、联军、补给、合并/拆分 |
| R4 | S ↔ P | 直接统治、行政隶属、驻军、特许、征税、军事通行、领有/归属 |
| R5 | U ↔ S | 驻军、驻扎、征兵、补给、占领 |
| R6 | U ↔ P | 委任/雇佣、指挥、军事通行、归属 |

另外必须有：

- **P ↔ Character**：官职、委任、效忠；
- **Title ↔ Character**：头衔持有；
- **Title ↔ Settlement/控制网**：法理/领有；
- 这些不属于“六类表关系”，但头衔系统必须单独有。

---

## 3. 关系 vs 契约 vs 头衔

| 概念 | 是什么 | 不是什么 |
|---|---|---|
| **Relation** | 持续结构边：谁从属谁、谁和谁联统、谁驻哪里 | 不是一次性事件 |
| **Contract** | 契约/条约：双方义务、期限、违约 | 不是归属 |
| **Title** | 法律地位/权利束：王冠、公国、官职、爵位、荣誉 | 不是土地本身 |

- 关系可以引用一个或多个契约；
- 契约可以创建/终止关系；
- 头衔可以产生关系（如封臣关系来自头衔持有）；
- 不要把三者塞进同一个 `RelationType` 枚举。

---

## 4. 君合/联统（Personal Union）

君合是**两个实际政治实体由一人统治**，不是合并、不是复合政治体。

```text
Polity A (Kingdom A)
Polity B (Kingdom B)
  ├─ PersonalUnionRelation:
  │    ├─ sharedRulerCharacterId
  │    ├─ seniorPolityId
  │    ├─ titleIds: [TitleA, TitleB]
  │    ├─ successionRule
  │    ├─ canSeparate
  │    └─ contractIds
  └─ 两实体仍各自有法律、财政、军队、议会
```

要点：

- 联统是**关系**，不是新政治实体；
- 两实体仍分别存在；
- 共享统治者死亡/退位/继承不同 → 联统可能分裂；
- 战争、外交、税收不自动合并；
- 只有特定制度（real union）才可能合并机构。

现有代码：

- `RelationType.PersonalUnion` 存在；
- `SpecialBondType.PersonalUnion` 存在；
- 但 `DiplomaticRelation` 只存 realmA/B 的 `specialBond`，**没有 sharedRulerId/titleId**；
- `EstablishPersonalUnion` 不检查是否同一统治者，也不创建头衔关系。

需要补：

```csharp
EntityRelation {
  RelationType type;
  int contractId;
  int sharedRulerCharacterId; // 君合/共治
  int titleId;
  int seniorPolityId;
}
```

---

## 5. 复合政治体

复合政治体也是**政治实体**，不是组织：

```text
CompositePolity: HRE
  ├─ member polities (principalities, cities, bishoprics)
  ├─ central institutions (diet, courts)
  └─ membership relation: Member → Composite
```

- `CompositeMembership` 是 P→P 关系，不是 `Vassalage`；
- 成员保留自己的头衔/法律/军队；
- 复合体有共同中央制度；
- 与君合不同：君合是同一人统治两个实体；复合体是一个实体含多个成员。

---

## 6. 头衔系统

前现代很多东西围绕头衔：

```text
TitleDef
  ├─ kind: Landed / Office / Honor / NobleRank / Claim
  ├─ rank/tier
  ├─ customary succession
  ├─ liegeTitleId
  └─ rights bundle

TitleInstance
  ├─ settlementIds[] / controlNetworkId
  ├─ holders: List<TitleHolding>
  ├─ liegeTitleId
  ├─ contractIds
  └─ legalOrderId

TitleHolding
  ├─ characterId
  ├─ role: sole/senior/co-king/elector
  ├─ share
  ├─ sinceDay/endDay
  └─ legitimacy
```

- 头衔不是人属性；人可以同时持有多头衔；
- 头衔可以共有（法兰克式）；
- 头衔可以跨政治实体（英王在法国）；
- 官僚/爵位/荣誉都是头衔或头衔派生；
- 头衔决定官职、征召、税收、司法、礼仪。

---

## 7. 关系端点与现有代码的缺口

| 缺口 | 现状 | 需要 |
|---|---|---|
| 政治实体形态 | 只有 `Realm` | **不新增 `PolityKind`**；形态由头衔/成员关系/中央制度推导 |
| 聚落 | `Anchor` | 保留，但语义明确为 Settlement |
| 单位 | `Unit` | 保留 |
| 头衔运行 | 无 `TitleHolding` | 新增 |
| 君合数据 | 只有 `specialBond` | sharedRuler/titleId/senior |
| 契约绑定 | `EntityRelation` 无 contractId | 新增 |
| 头衔-聚落/控制网 | 无 | `TitleInstance.settlementIds/controlNetworkId` |
| 附庸 | `RealmData.suzerainId` | 改为 relation/contract；旧字段迁移 |
| 多主关系 | `isPrimary` 单主 | 允许 `isPrimary` + 多重义务 |

---

## 8. 模拟必要性

| 关系 | 必要 | 作用 |
|---|---|---|
| 君合/联统 | 高 | 共享统治者、继承分裂、外交协调 |
| 复合成员 | 高 | 神罗、复合君主制 |
| 封臣/保护国/朝贡 | 高 | 义务、税收、军队 |
| 同盟/战争/停战 | 高 | 外交 |
| 聚落-政治实体 | 高 | 统治、驻军、征税 |
| 单位-聚落/政治实体 | 高 | 补给、驻扎、指挥 |
| 单位-单位 | 中高 | 指挥链、联军 |
| 头衔持有 | 高 | 官僚、爵位、荣誉、统治合法性 |

---

## 9. 落地顺序

1. `EntityRelation` 加 `contractId/sharedRulerCharacterId/titleId/seniorPolityId`；
2. 新增 `TitleInstance/TitleHolding`；
3. `PersonalUnion` 关系接共享统治者/头衔；
4. `CompositeMembership` 关系；
5. 关系端点仍用 `Realm`/`Polity`；复合/霸权/邦联形态由关系与头衔推导，不设 `PolityKind`；
6. 头衔→官僚/法律/税收/征召接线；
7. 旧 `suzerainId/vassalIds` 迁移为关系/契约；
8. UI 分：政治实体/聚落/单位/头衔/契约/组织。

---

## 10. 结论

- 关系按端点分六类：P-P、S-S、U-U、S-P、U-S、U-P；另加 P-Character、Title-Character、Title-Settlement/控制网；
- 君合是 P-P 关系，不是合并；需要 sharedRuler/title；
- 复合政治体是 P，不是组织；成员关系是 `CompositeMembership`；
- 头衔系统是核心：头衔持有、共有、跨实体、官僚/爵位/荣誉都挂在上面；
- 现有代码的 `TitleDefinition` 偏官职、`suzerainId/vassalIds` 偏政权、`PersonalUnion` 缺共享统治者，需要按上述模型迁移。


---

## 11. 追加：去掉“国种”描述

已删除：

- `LawEffects.RealmNameSuffix` 类、`_suffixTable`、`GetDefaultSuffix/GetAllSuffixes/AddCustomSuffix`；
- `LawEffectData.realmNameSuffix/realmNamePrefix`；
- “王国/公国/选侯国/僭主国/共和国/议事会共和国/联合政体/盟约政体/自由市”自动后缀；
- `RealmTitleEvolution.SuffixWord` 与领土化“XX王国/帝国”显示；
- `LawSystem` 相关日志。

保留：

- `realm.realmName` 本体，不再自动拼“国种”；
- 统治者头衔（王/皇帝/可汗）仍由 `TitleCatalog` 的头衔决定；
- `RealmForm`（定居/半游牧/游牧）是生计形态，不是国种，保留。

原则：

- 政治实体“是什么”由头衔/关系/制度/法律推导；
- 不自动生成“XX王国/公国/帝国”；
- 历史内容里的专名（如“领土王国”革新）需要另行内容审计，不和系统国种混为一谈。


---

## 12. 专名 / 通名 / 头衔构成

### 12.1 专名与通名

```text
PolityName
  ├─ properName      // 专名：法兰克、亚述、雅典
  ├─ commonName      // 通名：王国、公国、帝国、汗国……可为空
  ├─ languageId/cultureId
  └─ nameSource: city / people / dynasty / title / custom
```

显示规则：

```text
commonName 存在 → properName + commonName（例：法兰克王国）
commonName 为空 → 只显示 properName（例：法兰克、亚述、雅典）
```

要点：

- 通名不是政治实体类型，是**语言/文化/制度**的称谓层；
- 早期常常没有通名，地图只显示专名；
- 通名可由革新/文化/法律/接触产生；
- 同一政治实体在不同语言里可有不同通名；
- 不自动用 `PolityKind`/`RealmSuffix` 生成通名。

历史依据：

- 希腊古风/古典：`polis`/`ethnos` 认同，雅典人、拉栖第梦人；“雅典帝国”是后世分析词；
- 罗马：`res publica`、`imperium Romanum`、`populus Romanus`；
- 中世纪：`regnum Francorum`/`rex Francorum`，先有人民/头衔，后有领土王国名；
- 中国：`国号`（汉、唐、明）不是“王国/帝国”类型。

### 12.2 头衔构成

```text
TitleDefinition
  ├─ titleReferent（头衔指代）
  │    ├─ titleId / kind / rank
  │    ├─ jurisdiction: polity / territory / office
  │    ├─ authorities / rights
  │    └─ superiorTitle
  ├─ titleAppellation（称谓）
  │    ├─ cultureId / languageId
  │    ├─ polityKey / institution
  │    └─ display form（王/皇帝/可汗/执政官……）
  └─ boundFields（绑定字段）
       ├─ holderCharacterId
       ├─ polityId / settlementIds[] / controlNetworkId
       ├─ officeId / bureaucraticRole
       ├─ liegeTitleId
       ├─ startDay/endDay
       ├─ legitimacy
       └─ contractIds
```

- **指代**回答“这个头衔是什么、管什么”；
- **称谓**回答“在这个文化/语言/场合怎么叫”；
- **绑定字段**回答“谁持有、绑哪块地/哪个政权/哪个官职”。

现有代码：

- `TitleDef`：`titleId/kind/rank/semantic/authorities/modifiers`；
- `TitleDefinition`：`titleId/appellationId/superiorTitle/modules/appointmentOrder/seats/inheritable`；
- `TitleAppellationContext`：`cultureId/polityKey/isIndependentRuler/isGovernor/institution`；
- 缺 `TitleInstance/TitleHolding`，`ResolveAppellation` 尚未真正用上下文。

### 12.3 已删

- `TitleKinds.RealmSuffix` / `TitleKind.RealmSuffix`；
- `Titles.json` 里 `realm_kingdom/realm_empire/realm_khaganate`；
- `RealmNameSuffix` 表；
- `RealmTitleEvolution` 的“XX王国/帝国”显示。

地图不再自动生成通名；头衔只在角色/头衔页。
