> **范围边界（按用户定调）**：法律不做民事/刑法那么细（角色/系统有限；组织/国际组织等是新系统，暂不引入）。
> 只细化"对现有模拟有实际作用"的条目：政权制度/继承/君权/中央地方/军事/税收/宗教/身份/土地/贸易/度量衡/边境/少量刑法与司法（已有 Crime/JudicialSystem）。
> 历史公民权类型（城邦/罗马/城市/臣民）是**内容变体/身份制度预设**，不是革新条目；革新侧只保留一般概念（公民身份/有限公民权/普遍公民权/归化/国籍登记）。
>
> **本轮大头是革新**，法律审计仅作范围参考。

# 法律 / 法令系统：审计、精确化与重设计

> 背景：用户指出"普遍公民权与希腊罗马时代的公民权不是一个概念，当时民主和现在民主也不同"，
> 并要求检查现有法律/法令系统"涉及的所有东西、还缺什么"。
> 本文基于代码与内容实测（`Laws.json`、`LawDef`、`LawComposition`、`LawEffect`、`LawRequirement`、
> `JudicialSystem`、`LawCatalog`、`ThoughtManager`、`GovernmentPanel`），先审计再给精确化方案。
> 本轮只改设计，不改代码。

---

## 1. 现有资产盘点（实测）

| 项 | 现状 |
|---|---|
| `Laws.json` | **9 组 / 32 法 / 2 法令**；20/32 法无 `description`，9/9 组无描述 |
| 法律组 | 赋税、军制、特权、继承、君权范围、中央继承、地方继承、地方权限、领土结构 |
| 法令 | 仅 2 条：蠲免赋税、加征 |
| `LawEffect` | **只有 5 个字段**：taxMultiplier / stabilityPerYear / legitimacyPerYear / manpowerMultiplier / unrestPerYear |
| `LawRequirement` | **只有 5 个条件**：minTreasury/minLegitimacy/minCentralization/requiredLawIds/requiredInnovationIds |
| 革新门槛 | **32 条法律的 `requiredInnovationIds` 全为空**——法律完全没有接革新 |
| `LawComposition` | 29 个 `LawGroup` 维度（户籍/土地/婚姻/阶层身份与公民权/姓名登记/度量衡/贸易管制/国家税/地方税/关隘/边市/信仰/法律传统…），但 catalog 只覆盖 9 组 |
| `LawRealmHost.TryMapReformToCatalogLaw` | 只映射 **7 个** 政体维度（至尊继承/君权范围/中央继承/中央机构/地方继承/地方权限/领土结构）；其余 22 组无 catalog 法律可同步 |
| `JudicialSystem` | 15 个 `CrimeDefinition`、5 种刑罚概率；有 judicialEfficiency/corruption/lawEnforcement/severity；但**没有法院层级/管辖/程序/证据/上诉/律师/法官**；`LawSource` 仅在 `LawComposition`/`JudicialSystem` 存字段，没有与 catalog 法律连接 |
| 两套"法" | `LawDef`（政治制度法，`DefaultLawCatalog`）+ `Law`/`LawCatalog`（刑法引用，`JudicialSystem`）——**同名不同物**，没有统一 |
| UI | GovernmentPanel 只列出/推行/取消法律；没有法律类型/领域/权利/法院/司法状态展示 |

**结论**：当前法律系统是"9 组制度开关 + 2 个临时法令 + 一层很薄的刑法判定"，距离一个"法律体系"还很远。

---

## 2. 先精确概念：公民权、民主、法源

### 2.1 公民权的历史类型（不能混）

| 类型 | 时代/语境 | 含义 | 不是什么 |
|---|---|---|---|
| **城邦公民权（polis citizenship）** | 希腊城邦 | 城邦成员身份，参与公民大会/陪审/官职；排除妇女、奴隶、外邦人 | 不是普遍权利 |
| **罗马公民权（civitas Romana）** | 罗马 | 法律身份与特权（通婚权/贸易权/上诉权等），可授予行省居民（212 卡拉卡拉敕令） | 不是民主平等 |
| **城市市民权（urban citizenship / burgher right）** | 中世纪城市 | 城市特许状带来的市民身份/行会/市场特权 | 不是国家公民权 |
| **臣民身份（subjecthood）** | 王朝国家 | 对君主的臣属/效忠，无政治权利 | 不是公民权 |
| **国民身份/国籍（nationality）** | 近代国家 | 与国家的法律联系（登记/护照/国籍法） | 不等于公民权 |
| **普遍公民权（universal citizenship）** | 近代以来 | 共同体全体成员的法律成员身份 + 民事/政治/社会权利（Marshall） | 不是古代城邦/罗马公民权 |
| **普选权（universal suffrage）** | 近代 | 选举权普及（可与普遍公民权分开） | 不是公民权本身 |

### 2.2 民主也不能当一个条目

- **直接民主**（雅典公民大会、抽签、陶片放逐）≠ **共和制**（res publica、混合政体、元老院/执政官）≠ **代议制**（选举代表）≠ **自由民主**（宪法/权利/法治/多党）≠ **普选权**。
- 因此革新树应分开：`公民大会`、`抽签任职`、`元老院`、`执政官`、`陪审法庭`、`代表制`、`议会`、`宪法`、`权利法案`、`普选权`，而不是一个"民主"。

### 2.3 法源（LawSource）已有但没接

`LawSource`：Customary（习惯法）/ Statutory（成文法）/ Religious（宗教法）/ Common（普通法）/ Civil（大陆法）/ Mixed。
问题：它只存于 `LawComposition.legalSource`/`JudicialSystem.source`，**没有进入 `LawDef` 的内容、没有影响法律效果/司法判定/法律职业**。

---

## 3. 缺口矩阵（按法律领域）

| 领域 | 现有 | 缺什么 |
|---|---|---|
| **宪法/国家结构** | 领土结构（单一制/联邦制/邦联）、君权范围、继承 | 宪法/根本法、主权在民/君主主权、权利法案、紧急状态、修宪程序、中央地方权限划分（法律层面）、国籍法 |
| **公民/身份** | `LawComposition.classCitizenship` 仅组件；无 catalog 法律 | 城邦公民权、罗马公民权、城市市民权、臣民、国民/国籍、普遍公民权、归化、无国籍、外国人地位、选举权/被选举权 |
| **公法/行政** | 官僚制度（革新）、地方权限 | 行政法、行政法院、公务员法、地方自治法、公共财政法、审计/监察法、信息公开 |
| **刑法** | 15 个罪名 + 5 种刑罚概率 | 刑法总则（故意/过失/正当防卫/未遂/共犯）、罪刑法定、比例原则、刑罚种类细化、死刑/肉刑存废、监狱、少年犯、赦免 |
| **刑事程序** | `TrialCrime` 概率判定 | 侦查/起诉/审判/上诉、证据规则、无罪推定、辩护权、陪审/参审、公开审判、回避、冤案/复审 |
| **民法** | 无 | 法律人格、行为能力、财产权、契约、侵权、不当得利、婚姻/离婚/收养、继承（仅政治继承）、物权登记、时效 |
| **土地/水/资源法** | `landTenure` 组件（无 catalog） | 土地所有制/登记/租佃/公地/圈地、水权、灌溉、采矿权、林权、渔权、地役权 |
| **商法/经济法** | 铸币/银行/股份公司（革新），无法律 | 契约法、公司法、破产、票据、保险、海商、消费者保护、反垄断、度量衡法、关税/边市法 |
| **劳工/社会法** | 无 | 劳工法、工时/工资/安全、工会、罢工、童工、妇女财产权、废奴、济贫、公共卫生、教育法 |
| **宗教法** | `FaithStatus/FaithLawPriority/HeterodoxPolicy` 组件 | 宗教法/教会法、宗教法庭、婚姻/继承的宗教管辖、容忍/国教/政教分离、异端/亵渎、宗教财产 |
| **国际法** | 无 | 条约法、战争法、中立、外交豁免、边界条约、引渡、移民/难民、贸易协定、殖民/保护国法 |
| **法律职业/教育** | 无 | 法官/律师/检察官/公证人、法学院、法学、法律出版、法律援助、司法独立保障 |
| **法律多元/地方** | `LawSource` 有 Customary/Common | 习惯法与成文法并存、地方习惯、属人法/属地法、法律移植/汇编、法典化 |
| **执行/司法行政** | `JudicialSystem` 有概率 | 法院层级/管辖、执行/狱政、司法腐败/效率、法律普及、诉讼成本、ADR/调解 |
| **法令** | 2 条 | 法令类型（行政/军事/司法/宗教/经济/紧急）、适用范围（政权/区域/聚落/阶层）、合宪性、撤销、行政负担、阶层反应 |

---

## 4. 数据模型重设计

### 4.1 `LawDef` 扩展

```csharp
LawDef {
  string lawId, groupId, name, description;
  LawType type;              // Constitution/Statute/Code/Ordinance/Customary/Canon/CaseLaw
  LawDomain domain;          // Constitutional/Civil/Criminal/Commercial/Labor/Administrative/Religious/International/Procedural
  LawSource source;          // Customary/Statutory/Religious/Common/Civil/Mixed
  JurisdictionScope jurisdiction; // Central/Local/Religious/Estate/Personal/Territorial
  List<string> appliesTo;    // 阶层/族群/宗教/地区（空=全体）
  List<RightGrant> rights;   // 权利授予（财产/人身/政治/宗教/言论/集会…）
  List<Obligation> obligations;
  EnforcementMode enforcement; // Court/Bureaucracy/Community/ReligiousCourt
  int tier;                  // 组内层级
  LawRequirement requirements;
  LawEffect effects;         // 扩展后的效果
  float enactmentCostTreasury/prestige; int enactmentDays;
  ProposalAuthority proposalAuthority; CentralInstitution requiredInstitution;
  List<string> requiredInnovations (precise); List<string> requiredInstitutions;
  List<string> supportGroups/oppositionGroups;
  bool constitutional; bool amendable; string repealOf;
  List<string> regionalVariants;
}
```

### 4.2 `LawEffect` 扩展（不要只有 5 个数值）

按域分字段：

```csharp
LawEffect {
  // 经济/财政
  float taxMultiplier, tradeMultiplier, productionMultiplier, upkeepMultiplier;
  // 政治/合法性
  float stabilityPerYear, legitimacyPerYear, unrestPerYear, centralizationDelta, localAutonomyDelta;
  // 社会/阶层
  float[] classSatisfaction;   // 按阶层
  float mobility, inequality;
  // 司法/法律
  float judicialEfficiency, corruption, crimeRate, dueProcess, legalCapacity;
  // 权利/身份
  RightsDelta rights;          // 财产/人身/政治/宗教/言论/集会/平等等
  float citizenshipInclusion;
  // 宗教/文化
  float religiousAuthority, tolerance, conversion;
  // 军事
  float manpowerMultiplier, discipline, militaryJustice;
  // 环境/资源
  float landUseRegulation, waterRights, resourceAccess;
  // 革新/知识
  float researchMultiplier;
}
```

### 4.3 `DecreeDef` 扩展

```csharp
DecreeDef {
  string decreeId, name, description;
  DecreeType type;             // Administrative/Military/Judicial/Religious/Economic/Emergency
  TargetScope scope;           // Realm/Region/Settlement/Class/Group
  int targetId;                // -1=全政权
  bool constitutional;         // 是否受宪法/法律约束
  float costTreasury/authority; int durationDays/cooldownDays;
  LawRequirement requirements;
  LawEffect effects;
  List<string> support/opposition;
  bool revocable; int revocationCost;
}
```

### 4.4 `JudicialSystem` / `CourtDef`

- `CourtDef` 内容：层级（基层/上诉/最高）、管辖（民事/刑事/宗教/行政/军事）、法官来源、程序类型、证据规则、上诉链、公开性、陪审/参审。
- `JudicialSystem`：法院列表、法官/律师/检察官、司法独立度、腐败、效率、法律职业规模、判例/法典状态。
- 现有 `TrialCrime` 的概率判定保留，但增加：立案/证据/辩护/上诉/量刑/赦免；与 `LawDef` 的刑法/程序法联动。

### 4.5 统一两套"法"

- `LawDef`（制度法）与 `Law`/`LawCatalog`（刑法引用）改名为：
  - `InstitutionalLawDef` / `InstitutionalLawCatalog`（政治制度法）
  - `PenalLawDef` / `PenalCode`（刑法/罪名引用）
- `JudicialSystem.IsCrimeCited` 从 `PenalCode` 查；`LawEffect` 的司法效果来自制度法/程序法。
- 两套通过 `LawDomain.Criminal`/`Procedural` 关联，不再同名混淆。

---

## 5. 缺失内容清单（P0/P1/P2）

### P0（先补，否则"法律体系"不成立）

1. **宪法/权利**：`宪法`、`权利法案`、`人身保护令`、`法律面前平等`、`财产权`、`宗教自由`、`言论/集会`、`紧急状态`、`修宪`.
2. **公民/国籍（一般概念 + 内容变体）**：`公民身份`、`有限公民权`、`普遍公民权`、`归化`、`国籍登记`、`普选权`；历史类型（城邦/罗马/城市/臣民）作为 `CitizenshipRegime` 内容变体，不是革新条目。
3. **法院/程序**：`法院层级`、`管辖`、`证据规则`、`辩护`、`上诉`、`陪审/参审`、`司法独立`、`无罪推定`.
4. **民法基础**：`法律人格`、`财产权`、`契约`、`侵权`、`婚姻`、`离婚`、`收养`、`继承`（政治继承之外）.
5. **刑法总则**：`罪刑法定`、`故意/过失`、`正当防卫`、`未遂/共犯`、`比例原则`、`死刑/肉刑存废`.
6. **法令体系**：类型/范围/合宪性/撤销/行政负担；至少 10–20 条不同领域法令.
7. **革新门槛**：所有法律/法令接精确 `requiredInnovations`（现有 32 条全空）.

### P1（让法律有"社会效果"）

- 商法/海商/破产/票据/保险/公司；劳工/工会/罢工/童工；土地登记/租佃/公地/水权/采矿；行政法/行政法院/公务员法；宗教法/教会法庭/容忍/政教分离；国际法/条约/战争法/引渡/移民；法律职业/法学院/法学/法律出版/法律援助；法律多元/习惯法/法典化.

### P2（细化/区域变体）

- 环境法、公共卫生法、教育法、社会福利、消费者保护、反垄断、数据/知识产权（如适用）、过渡司法、地方习惯法汇编、判例/衡平.

---

## 6. 法律 ↔ 革新映射（示例）

| 法律/法令 | 需要的精确革新 |
|---|---|
| `charter_free_city` 自由市特许 | `城市特许状`（法律/制度）、`城市市民权`、`市场法` |
| `charter_guild` 行会特许 | `行会`、`城市市民权`、`商法` |
| `standing_army` 常备军 | `常备军`（军事制度）、`军饷/后勤` |
| `succ_elective` 选举继承 | `选举`/`代表制`（非直接民主） |
| `scope_legally_bound` 法理受限 | `成文法`、`宪法`/`根本法`、`司法独立` |
| `spatial_unitary` 单一制 | `单一制`、`领土主权`、`郡县制/行省制` |
| `spatial_federal` 联邦制 | `联邦制`、`成文宪法`、`地方自治` |
| `citizenship_universal` 普遍公民权 | `公民身份` + `法律面前平等` + `政治权利` |
| `trial_by_jury` 陪审 | `陪审法庭`、`证据规则`、`公开审判` |
| `habeas_corpus` 人身保护令 | `普通法`/`成文法`、`法院`、`权利法案` |
| `abolition_slavery` 废奴 | `废奴运动`、`普遍公民权`/`自然权利`、`劳工法` |
| `universal_suffrage` 普选 | `普选权`、`代表制`、`选举管理` |

> 现有 32 条法律的 `requiredInnovationIds` 全空，这是**最大的接线缺口**：革新不影响法律，法律不影响革新。

---

## 7. 法令系统

- 现有 2 条：蠲免赋税、加征；都只影响 5 个数值，没有类型/范围/合宪性/撤销/阶层反应。
- 法令应分：
  - 行政（任免/考核/整饬吏治）、军事（征发/戒严/进军）、司法（大赦/严打/复审）、宗教（祭祀/封圣/宗教会议）、经济（铸币/限价/专卖/关税）、紧急（赈灾/防疫/戒严）。
- 法令与法律的关系：法令不得违反宪法/法律（`constitutional` 字段）；违反可被法院/反对派挑战，产生合法性/稳定度后果。
- 法令应有行政负担/执行能力要求，避免"无限法令"。

---

## 8. 实施顺序

| 步 | 内容 | 验收 |
|---|---|---|
| L0 | **统一术语/结构**：`LawDef` 扩展、`LawEffect` 扩展、`DecreeDef` 扩展、两套法改名；旧档迁移默认值 | 编译/旧档加载；现有 Law 测试绿 |
| L1 | **P0 内容**：宪法/权利/公民/法院/民法基础/刑法总则；30–50 条法律 + 15 条法令；每条有 `definition/type/domain/source` | 内容校验；UI 可显示类型/领域 |
| L2 | **革新接线**：所有法律 `requiredInnovations` 精确；`NationalismGateTests` 同源；法律→阶层/身份/职业解锁 | 正反向测试 |
| L3 | **司法系统**：CourtDef/管辖/程序/证据/上诉/律师；`TrialCrime` 接程序法 | 行为测试 |
| L4 | **法律多元/法典化/法律职业**；区域变体；法令合宪性/撤销 | 测试 |
| L5 | UI：法律类型/领域/权利/法院/司法状态/法令范围 | PlayMode |

---

## 9. 待拍板

1. 两套"法"是否按本文改名为 `InstitutionalLaw`（制度法）与 `PenalCode`（刑法）？还是保留 `LawDef` 只加 `domain`？
2. 法律类型/领域枚举是否按本文（Constitutional/Civil/Criminal/Commercial/Labor/Administrative/Religious/International/Procedural）？
3. 公民权是否按"城邦/罗马/城市/臣民/国民/普遍"六类建法律组？
4. 法令是否先扩到 15–20 条（行政/军事/司法/宗教/经济/紧急）？
5. 是否接受"所有法律必须挂精确革新门槛"（含现有 32 条迁移）？

---

## 10. 参考

- Weber《Economy and Society》（科层制/家产官僚制）
- Anderson《Imagined Communities》（印刷资本主义）
- T.H. Marshall《Citizenship and Social Class》（公民身份三要素）
- Eisenstein《The Printing Press as an Agent of Change》
- 比较政治学：单一制/联邦制/邦联、中央集权化 vs 分权
- 国际法：boundary delimitation/demarcation（划界/标界）
- 中国制度史：郡县制、行省制、科举、考课、上计
