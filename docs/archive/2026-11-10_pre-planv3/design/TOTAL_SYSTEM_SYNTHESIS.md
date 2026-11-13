# 大系统综合规划：政治 / 头衔 / 关系 / 法律 / 信仰 / 历法

> 2026-11-05。合并前面所有结论，不再把信仰单独硬分层。
> 原则：**结构可选、组件可组合、权威可挂载、认同可挂可不挂、个人与公开分离、按历史阶段解锁。**

---

## 1. 统一设计原则

1. 不设普适硬层级；
2. 需要层级的地方，层级是**可选配置**，不是实体本质；
3. 内容用**组件/传统**表达，可互斥、升格、固化；
4. 权威用**能力包**表达，不靠“头衔名字”；
5. 组织、认同、共同体都可挂可不挂；
6. 公开身份与个人采纳分开；
7. 早期不能现代，靠革新/制度解锁；
8. 不用单一抽象满意度，用渠道/阀门/执行/派系机制。

---

## 2. 核心本体

| 本体 | 作用 | 关键字段 |
|---|---|---|
| Polity | 政治实体 | name, titles, memberRelations, contracts, legalOrder |
| Title | 法律地位 | referents[], appellations[], bindings[], rights/claims |
| SettlementControlNetwork | 聚落控制网/权利束 | settlementIds, controllerAnchorId, controlledAnchorIds, rights, contracts |
| Contract | 契约/条约 | parties, obligations, duration, breach |
| Organization | 组织/实体 | members, offices, property, rules |
| LegalOrder | 法律秩序 | norms, scope, enforcement |
| Calendar | 历法/纪年 | eras, months, festivals, day length |
| FaithTradition | 信仰思想/传统 | category, tenets, doctrines, practices |
| FaithCommunity | 信仰组织/网络（可选） | members, clergy, property |
| FaithIdentity | 信仰归属/边界（可选） | nestedParent, reachByRole, channels, valves |

---

## 3. 关系机制

按端点分类：

- P↔P：君合、复合成员、封臣、保护国、朝贡、同盟、战争、共管、租借
- S↔S：空间从属、道路/贸易、文化/宗教、军事支援
- U↔U：指挥/从属、联军、补给
- S↔P：直接统治、行政隶属、驻军、特许、征税
- U↔S：驻军、驻扎、征兵、补给
- U↔P：委任/雇佣、指挥、军事通行
- 另：P↔Character（官职）、Title↔Character（持有）、Title↔Settlement/控制网（法理/领有）

君合是 P↔P 关系，不是合并；复合政治体是 P，不是组织。

---

## 4. 头衔系统

- 无硬 `TitleKind`；类别由绑定/权利推导；
- 显示只显示“指代 + 称谓”；
- 指代可多目标：族群/文化/聚落/地域/政治实体/行政区/官职/人物；
- 绑定：holder/polity/territory/office/liege/contract/time；
- 称谓：开放 `segments[] + grammarId`，可组合、升格；
- 荣誉头衔 = 无绑定/无权利的头衔；
- 尊号、绰号、谥号、庙号、年号各自独立系统；
- 地图只显示政治实体名，头衔只在角色页。

---

## 5. 法律/法令系统

三层：

```text
根本法/政体结构（可选根本化）
普通法律（Known→Pending→Active→Consolidated→Fundamentalized）
法令（临时/逐级执行）
横切：特许/特权
```

- 法律有互斥、前置、已知/学习、兜底；
- 法令逐级下推，有执行/拒绝/谋利；
- 特许是例外规范，不是普通法律组；
- 官僚因子：capacity/reach/integration/compliance/complexity；
- 不满通过官员/地方/派系/执行机制表达。

---

## 6. 信仰系统

### 6.1 三层分离

```text
FaithTradition（思想内容）
PersonalFaith（个人采纳）
FaithCommunity / Organization（组织，可选）
FaithIdentity（社会边界，可选）
```

### 6.2 公开信仰 vs 个人信仰

```text
PublicFaith = 我对他人展示的信仰
  = 具体传统/流派/礼仪（罗马礼、禅宗、日莲宗……）
  = 社会识别

PersonalFaith = 我个人采纳的信仰
  ├─ personalTenetIds[]   // 从 Known 中采纳
  ├─ personalPractices[]
  ├─ privateTraditionId   // 秘密信仰
  └─ observance
```

### 6.3 层级：可选，不普适

对高度组织化宗教，可配置：

```text
Family? → Religion? → Faith? → Rite?
```

- 每层都可空；
- `Rite` 是具体传统/礼仪实例，不是所有宗教必需；
- 泛灵论/多神教/家神/城邦守护神/部落守护神不套这套层级；
- 用 `Cult/Shrine/Patron/Pantheon` 表达。

### 6.4 信条

- 状态：Core / Permitted / Prohibited / Known；
- 个人可从 Known 采纳个人信条；
- 个人信条有独立 modifier/parameter；
- 公开信仰的 Core 是集体核心，不等于个人采纳。

### 6.5 每一层的领袖

| 层 | 领袖 | 能做什么 | 是否必需 |
|---|---|---|---|
| Family | 通常无 | 无统一领袖 | 否 |
| Religion | 通常无，或跨教派会议 | 召集会议、仲裁 | 否 |
| Faith | Head of Faith | 宣布圣战、绝罚、授职、征税、裁决、封圣、定义教义、召集会议 | 否 |
| Rite | Head of Rite | 管理礼仪/神职/财产/教区/朝圣，服从 Faith Head | 否 |
| 地方共同体 | 祭司/住持/长老 | 主持仪式、管理场所、调解 | 可选 |
| 泛灵/多神 | 祭司/萨满/家长/神庙看护 | 仪式、占卜、祭祀、调解；不是教义权威 | 可选 |

领袖是**头衔持有者 + 能力包**，不是层级自带字段。

### 6.6 主保与圣人

- `Saint`（圣人/圣徒）和 `Patron`（主保）分开；
- `Patron` 分神圣/世俗：
  - 神圣：神祇、圣人、天使、祖灵；
  - 世俗：祖先、建国者、王朝始祖、城市英雄；
- `Patron` 可挂：个人/家族/聚落/政治实体/职业/团体；
- `Saint` 是 cultus/封圣记录，不等于主保；
- CK3 的主保机制不完善，不照抄。

### 6.7 泛灵/多神

```text
Pantheon
  ├─ deities[]
  ├─ householdGods[]
  ├─ localGods[]
  ├─ ancestors[]
  └─ spirits[]

Shrine / Temple / SacredPlace
  ├─ patronId
  ├─ cultId
  ├─ rituals[]
  └─ clergy/ritualSpecialist
```

- 没有单一教会也可以有信仰；
- 家神/城邦守护神/部落守护神是 `Patron`，不是 `Faith` 层级；
- 公共祭祀与个人供奉分开；
- 祭司/萨满/家长是角色/头衔，不是“教皇”。

---

## 7. 文化/认同机制

- 文化 = 传统集合 + 因子 + 语言/ethos；
- 传统可组合、互斥、升格、固化；
- 族群认同 = 嵌套 + 渠道 + 阀门 + 下沉度 + 边界；
- 信仰认同可借同一套机制，但**可挂可不挂**；
- 文化内容与认同边界分开；
- 不默认全民同层。

---

## 8. 历法/时间

```text
Calendar
  ├─ calendarId
  ├─ eraId / epoch
  ├─ months[] / days[]
  ├─ festivals[]
  ├─ intercalation
  └─ dayLength
```

- 各政权/信仰/礼仪可有自己的历法；
- 纪年法可编辑、可模组化；
- UI 顶部显示；
- 游戏内一日时长可配置；
- 历法不是信仰层级，是独立系统。

---

## 9. 历史阶段

| 阶段 | 政治 | 法律 | 信仰 |
|---|---|---|---|
| 部落/习俗 | 首领/长老/双王 | 习惯/命令 | 泛灵/祖灵/家神 |
| 早期王权 | 王权/城邦 | 王命/早期成文 | 城邦/部落守护神 |
| 中世纪 | 封建/复合/教会 | 封建法/教会法/特许 | 组织化宗教/修会/圣人 |
| 早期现代 | 主权/官僚 | 法典/根本法话语 | 教派/国家教会 |
| 现代/未来 | 民族国家/联邦 | 宪法/根本化 | 个人信仰/多元/世俗意识形态 |

不是所有文明线性走完。

---

## 10. 实施顺序

1. 本体：Polity/Title/SettlementControlNetwork/Contract/Organization/LegalOrder；
2. 关系：六类端点 + 君合/复合；
3. 头衔：指代/称谓/绑定 + 尊号/绰号；
4. 法律：三层 + 互斥/前置/已知/兜底；
5. 信仰：Tradition/PersonalFaith/Community（可选）/Identity（可选）；
6. 领袖/主保/圣人/泛灵多神；
7. 历法；
8. 官僚/财政/军事接口；
9. UI/存档/迁移。

---

## 11. 结论

- 不采用 CK3 的硬四层作为普适模型；
- 对高度组织化宗教，可选配置四层；
- 泛灵/多神用 Pantheon/Shrine/Patron，不硬套教会层级；
- 信仰是思想/采纳，组织/共同体另算，可挂可不挂；
- 个人信仰/个人信条/秘密信仰独立于公开信仰；
- 主保与圣人分开，主保分神圣/世俗；
- 每一层领袖都是可选头衔 + 能力包，不是层级自带；
- 所有系统统一到“本体 + 关系 + 组件 + 权威 + 认同”框架。


---

## 12. 实读 CK3 修正：Venerated Ancestor / Patron / Leader

### 12.1 CK3 的“圣人”不是单指基督教圣人

实际文件：

- `common/traits/00_traits.txt`：`saint` trait 的 fallback 名称是 `trait_venerated_ancestor`；
- 按宗教标签分支：`trait_saint`（基督教）、`trait_wali/waliyya`（伊斯兰）、`trait_divus/diva`（希腊化）、`trait_siddha`（耆那教）、`trait_kami`（神道）、`trait_xianru`（儒）、`trait_arya`（佛教）、`trait_zhenren`（道）、`trait_tzadik/tzadeket`（犹太）；
- `common/customizable_localization/pam_saint_custom_loc.txt` 明确：基督教用 Saint/canonize；有本地词的用 Wali/Divus/Siddha/Kami；其他 fallback 为“Venerated Ancestor”/“venerate”。

所以：

```text
CK3 的 saint 系统 = 广受崇敬的死者 / venerated ancestor
不是单纯基督教“圣人”
```

### 12.2 圣人与主保的关系

- `saint` 是“被崇敬的死者”这一**身份/状态**；
- `patron saint` 是**选择某位圣人作为主保**的关系/角色；
- `pam_determine_patron_saint_decision` 是基督教专属决策，选彼得/保罗/约翰/马太/马可/路加，给角色 modifier；
- 主保决策依赖 `tenet_dulia` 或 `personal_tenet_flag`；
- 所以：
  - 圣人 ≠ 主保（身份 vs 关系）；
  - 主保圣人是“被选为主保的圣人”；
  - 不是所有圣人都是主保；
  - CK3 的主保机制只覆盖基督教，且很薄，不能照抄。

### 12.3 每一层领袖

CK3 实际：

- `faith_types`：`religious_head = title_key`；
- `rite_types`：`head_of_rite = title_key`（或 faith 上 `head_of_rite`）；
- 领袖是**头衔持有者**，不是层级字段；
- 没有 `religious_head` 的 faith 就没有统一领袖；
- 没有 `head_of_rite` 的 rite 就没有 rite 领袖；
- 领袖能力来自 doctrine 参数/脚本，不来自头衔名。

实际能力参数/脚本：

- `spiritual_head_of_faith`
- `temporal_head_of_faith`
- `spiritual_head_of_faith_claims`
- `spiritual_head_of_faith_gold`
- `temporal_head_of_faith_directed_holy_wars`
- `clerical_appointment_head_of_faith`
- `set_religious_head_title` / `remove_religious_head_title`
- `make_temporal_head_of_faith_title`
- 绝罚、圣战、授职、征税、裁决、封圣、召集会议等

结论：

```text
领袖 = 可选头衔 + 能力包
不是“每个层级必然有领袖”
```

### 12.4 我们的模型修正

```text
VeneratedDead（广受崇敬的死者）
  ├─ category: saint/wali/divus/siddha/kami/xianru/arya/zhenren/tzadik/ancestor...
  ├─ cultus
  ├─ shrine/holySite
  ├─ feastDay
  └─ patronEligible

Patron（主保关系，可选）
  ├─ patronType: deity/saint/ancestor/founder/hero
  ├─ targetCharacterId / deityId / saintId
  ├─ scope: person/family/settlement/polity/occupation/guild
  ├─ chosenDay
  └─ modifier/obligation

ReligiousLeader（可选）
  ├─ titleId
  ├─ authorityFlags[]
  ├─ scope: faith/rite/local
  └─ powers[]  // 圣战/绝罚/授职/征税/裁决/封圣/会议
```

- `VeneratedDead` 是身份，不是主保；
- `Patron` 是关系，可以指向 VeneratedDead/Deity/Ancestor；
- `ReligiousLeader` 是头衔+能力，不是层级；
- 泛灵/多神没有统一领袖，用 Shrine/Cult/Patron/ritualSpecialist；
- 不把 CK3 的 Christian patron saint 决策照抄。
