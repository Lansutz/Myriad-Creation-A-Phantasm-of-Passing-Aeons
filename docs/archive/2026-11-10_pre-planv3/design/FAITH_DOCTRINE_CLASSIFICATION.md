# 信仰内容结构：两轴归类 + 槽位 + 组织阶段与注册表（CK3 对照）

> 2026-11-06。依据：本机 CK3 1.20.1 原版文件（`E:/SteamLibrary/steamapps/common/Crusader Kings III/game`）与已安装工作坊模组 Arcana Caeli（3813191652）/ Shadows of the Cross（3827420629）。
> 用途：作为信仰系统设计稿的附录——把 CK3 的 49 个教义组按「类型 × 领域」重排，决定每条归哪个系统、由谁消费。
> 路径缩写：`CD/` = `MyriadCreation/Assets/Scripts/Content/MyriadCreation.Content.Definitions/`，`SF/` = `.../Simulation/MyriadCreation.Simulation.Faith/`，`LW/` = `.../Simulation/MyriadCreation.Simulation.Laws/`，`SY/` = `.../Simulation/MyriadCreation.Simulation.Systems/`。

## 0. 结论

1. CK3 的 `doctrine` 是**实现桶**，不是概念层：官方定义说它是"决定什么行为可接受的宗教法"（`localization/english/game_concepts_l_english.yml:330`），但桶里同时装着神观陈述、婚姻法、刑法、组织形态、教职资格、礼仪实践、身份标记、正典类型、宗教状态。
2. 用两轴重排即可消除混杂：**轴 A＝它是什么（类型）**，**轴 B＝它管什么（领域）**。
3. 126 个选项里只有约 44% 是"规则"；其余是身份标记、组织形态、礼仪实践、宗教关系、状态，还有 5 条是纯陈述（神观/正典类型）。
4. 一神/多神属于**神观/终极观**（陈述型必答档位），不是教规、不是限槽信条。
5. 信条槽位按 Arcana Caeli 的实证结构：**2 条正典（锁定）+ 3 条核心（可改）**，槽位数是 define 可配置项，不是引擎法；"正典"是锁定状态，不是文本实体。
6. **取消 reformed / unreformed 二分**：组织化用 5 档阶段（家户 → 地方 → 教派/社团 → 教团/寺院网络 → 国教/教会 → 普世）作为状态与门禁；组织本身由教职/继承/修道/神权等教规表达，阶段只做过滤与解锁。
7. **圣人 = 全局注册表 + 每信仰承认集合**；封圣是带条件的流程（本地崇拜、灵验、社群请愿、权威批准），不是"已死 + 同王朝 + 1 美德 + 0 罪"的判定。
8. **神祇 = 全局注册表（DeityDef）**：带领域、位阶、万神殿归属；CK3 只有 12 个"神名槽"，没有神的数据。
9. **教统与思想是两张图**：组织连续性（教统：使徒统绪/法脉/官祭）与思想谱系（学派/解经传统）正交；聂斯托里派与亚述教会是两图交叉的教科书案例。
10. **主保统一为一个 `Patronage` 关系**：神祇、圣人、祖先共用同一套"主体 → 主保 → 领域 → 崇拜义务"；封圣只是其中一条生成路径。
11. **神祇结局分档**：降格（降位/等同化/从属）→ 污名化 → 移除（名号与祭祀被清除，罕见）。污名化有三种去向：**恶神**（仍是神，性质恶）、**伪神**（不再是神，偶像）、**非神存在**（魔鬼/恶灵/妖怪/精怪/巨人/仙人，类别变化）；污名化与移除波动最大，需要局部 `shock` 模型。

---

## 1. 两轴定义

### 轴 A：类型（它是什么）

| 类型 | 判定 | 落地形态 |
|---|---|---|
| 陈述 | 关于"是什么"的主张，不直接规范行为 | 描述档位（faith profile 字段） |
| 规则 | 可被违反、可被惩罚/覆盖的行为规范 | 教规；优先并入法律/司法，信仰只做 override |
| 组织 | 谁掌权、怎么任命、怎么继承、有什么机构 | 头衔/政体/任命规则/组织实体 |
| 礼仪 | 仪式、节期、丧葬、加冕、朝圣 | 活动 + 礼仪组件 |
| 标记 | 身份/派别/阶段标签，不描述行为 | 状态标记（flag/枚举） |
| 关系 | 对其它信仰的立场 | 宗教关系表（敌对/宽容/传教） |
| 状态 | 随时间变化的发展阶段 | 运行时状态（unreformed、异端、偏离度） |
| 文本 | 只给人看 | 本地化键，不进任何判定 |

### 轴 B：领域（它管什么）

神观 / 经典 / 伦理·性 / 婚姻·亲属 / 教职 / 政教 / 礼仪·节期 / 宗教间 / 身份·派别 / 状态。

### 三条判据（每行必须过）

1. 谁创建它？2. 谁修改它？3. 谁读它？4. 删掉会坏什么？
后两问答不出来 → 不是实体，最多是组件或文本。

---

## 2. 49 组归类总表

档位数合计 126。`落点` 指项目里的承接系统。

### 2.1 主要教义 main_group（10 组 / 30 档）

| 组 | 档位 | 类型 | 领域 | 落点 | 备注 |
|---|---|---|---|---|---|
| doctrine_theism | 2 | 陈述 | 神观 | 描述档位 | 无 `parameters`，唯一机制是 `hostility_levels`（一神=evil/多神=hostile）+ 改革花费 |
| doctrine_gender | 3 | 规则 | 社会性别 | 法律/身份 | CK3 由继承法、婚姻交互、CB 读取（`00_succession_laws.txt:1952`） |
| doctrine_pluralism | 3 | 规则 | 宗教间 | 宗教关系 | 有参数：传教加成、派系入伙、易受异端、暴政；带 `character_modifier` |
| doctrine_theocracy | 2 | 组织 | 政教 | 政体/组织 | 神权政体、教会领地 |
| doctrine_head_of_faith | 3 | 组织 | 政教 | 头衔/组织 | 决定能否创建信仰领袖头衔（`00_religious_triggers.txt:1513`） |
| doctrine_pilgrimage | 5 | 礼仪 | 礼仪·节期 | 活动/教规 | 朝圣义务门槛（`00_religious_triggers.txt:1580`） |
| doctrine_muhammad_succession | 4 | 标记 | 身份·派别 | 身份/关系 | 伊斯兰派别起源标记 |
| doctrine_temple_authority | 4 | 组织 | 教职·权威 | 组织/教职 | 犹太系权威形态 |
| doctrine_zoroastrian_branches | 2 | 标记 | 身份·派别 | 身份/关系 | 琐罗亚斯德分支标记 |
| special_tolerance | 2 | 关系 | 宗教间 | 宗教关系 | 普世宽容，`doctrine_lock = faith` |

### 2.2 神职 clergy（8 组 / 28 档）

| 组 | 档位 | 类型 | 领域 | 落点 | 备注 |
|---|---|---|---|---|---|
| doctrine_clerical_function | 3 | 规则 | 教职·经济 | 组织/经济 | 征税/救济/招募 |
| doctrine_clerical_gender | 3 | 规则 | 教职资格 | 教职/头衔 | 谁能任神职 |
| doctrine_clerical_marriage | 2 | 规则 | 教职资格 | 教职/头衔 + 法律 | 神职婚姻 |
| doctrine_clerical_succession | 5 | 组织 | 教职任命 | 头衔/任命 | 任命/继承规则 |
| doctrine_funeral | 6 | 礼仪 | 礼仪·节期 | 活动/礼仪 | 丧葬形态 |
| doctrine_coronation | 3 | 礼仪 | 礼仪·节期 | 活动/礼仪 | 加冕/涂油 |
| monasticism_group | 4 | 组织 | 组织·修道 | 组织/头衔 | 修道制度 |
| sacraments_group | 2 | 礼仪 | 礼仪·节期 | 活动/礼仪 | 圣礼中心性 |

### 2.3 罪行 crimes（6 组 / 22 档）

| 组 | 档位 | 类型 | 领域 | 落点 | 备注 |
|---|---|---|---|---|---|
| doctrine_homosexuality | 3 | 规则 | 伦理·性 | 司法（罪行） | crime/shunned/accepted |
| doctrine_deviancy | 4 | 规则 | 伦理·行为 | 司法 | 多一个 virtuous |
| doctrine_adultery_men | 3 | 规则 | 伦理·性 | 司法 | |
| doctrine_adultery_women | 3 | 规则 | 伦理·性 | 司法 | 与男性分开，本身是性别史证据 |
| doctrine_kinslaying | 5 | 规则 | 伦理·亲属 | 司法 | 按亲属范围五档 |
| doctrine_witchcraft | 4 | 规则 | 伦理·巫术 | 司法 | crime/shunned/accepted/virtuous |

### 2.4 婚姻 marriage（4 组 / 14 档）

| 组 | 档位 | 类型 | 领域 | 落点 | 备注 |
|---|---|---|---|---|---|
| doctrine_marriage_type | 3 | 规则 | 婚姻法 | 法律 | 一夫一妻/多妻/侧室 |
| doctrine_divorce | 3 | 规则 | 婚姻法 | 法律 | |
| doctrine_bastardry | 3 | 规则 | 婚姻·亲属 | 法律 | 私生与合法化 |
| doctrine_consanguinity | 5 | 规则 | 婚姻·亲属 | 法律 | 五档近亲范围 |

### 2.5 特殊 special（14 组 / 17 档）

| 组 | 档位 | 类型 | 领域 | 落点 | 备注 |
|---|---|---|---|---|---|
| is_christian_faith | 1 | 标记 | 身份 | 身份标记 | BOM/决议定向 |
| is_islamic_faith | 1 | 标记 | 身份 | 身份标记 | |
| is_jewish_faith | 1 | 标记 | 身份 | 身份标记 | |
| is_eastern_faith | 1 | 标记 | 身份 | 身份标记 | |
| is_sinitic_faith | 1 | 标记 | 身份 | 身份标记 | |
| is_gnostic_faith | 1 | 标记 | 身份 | 身份标记 | 附带少量特质/修正 |
| adoptionist_school | 3 | 标记 | 派别·神学 | 身份/关系 | 嗣子论三派 |
| divine_destiny | 1 | 标记 | 政治神学 | 身份/合法性 | 大圣战开关，南亚决议授予 |
| full_tolerance | 1 | 状态 | 宗教间 | 宗教关系 | 仅 game rule 注入 |
| has_jizya_doctrine | 1 | 规则 | 政教·财政 | 法律/契约 | 解锁吉兹亚契约 |
| immaterial_harmony | 1 | 标记 | 政治神学 | 身份/关系 | 无为和谐 |
| not_allowed_to_hof | 2 | 组织限制 | 政教 | 组织/头衔 | 禁止创建信仰领袖 |
| nudity_doctrine | 1 | 礼仪 | 礼仪·实践 | 礼仪/立绘 | 裸体祭司 |
| pope_blessing | 1 | 组织 | 政教·外交 | 组织 | 教皇祝福 |

### 2.6 不可创建 not_creatable（7 组 / 15 档）

| 组 | 档位 | 类型 | 领域 | 落点 | 备注 |
|---|---|---|---|---|---|
| hostility_group | 4 | 关系 | 宗教间 | 宗教关系 | 四大宗教家族敌对底色 |
| is_hostile_to_maitreya | 2 | 关系/标记 | 宗教间 | 宗教关系 | 弥勒敌对 |
| christian_tradition_classification | 2 | 标记 | 身份 | 身份/立绘 | 东西教会外观区分 |
| preservation | 3 | 陈述 | 经典 | 描述档位 | 经卷/神话/口传，只给 `faith_coherence_high/normal/low` |
| has_clerical_electors | 1 | 组织 | 教职 | 组织/头衔 | 枢机选举团 |
| heresy_hostility | 1 | 关系 | 宗教间 | 宗教关系 | 同宗教内按 hostile |
| unreformed_faith | 2 | 状态 | 状态 | 宗教状态 | 原始信仰，改革后移除 |

---

## 3. 分布统计

| 类型 | 档位数 | 占比 | 说明 |
|---|---|---|---|
| 规则 | 56 | 44% | 婚姻 14 + 罪行 22 + 教职资格/职能 13 + 性别 3 + 多元主义 3 + 吉兹亚 1 |
| 标记 | 19 | 15% | 五系身份 + 派别 + 政治神学 + 东西教会 |
| 组织 | 17 | 13% | 神权、领袖、圣殿权威、修道、选举团、教皇祝福 |
| 礼仪 | 17 | 13% | 葬礼 6 + 加冕 3 + 圣礼 2 + 朝圣 5 + 裸体祭司 1 |
| 关系 | 10 | 8% | 多元主义相关 + 敌对组 + 异端 + 弥勒 + 全宽容 |
| 陈述 | 5 | 4% | 神观 2 + 正典类型 3 |
| 状态 | 2 | 2% | 未改革 |

**判读**：近一半是"规则"，规则里婚姻+罪行+性别共 39 条恰好落在项目的法律/司法系统；组织与礼仪合计 34 条落在头衔/活动；身份/关系/状态 31 条落在标记与关系表；纯陈述只有 5 条。按这套归属，信仰系统自己只需要"教规 override + 描述档位 + 关系表"。

---

## 4. 信条槽位模型（Arcana Caeli 实证）

### 4.1 原版与模组

| 项 | 原版 | Arcana Caeli / SOTC |
|---|---|---|
| 上限 | `FAITH_CORE_TENETS_CAP = 3`（`common/defines/00_defines.txt:908`） | 覆盖为 5（`common/defines/ac_foundation_defines.txt:1`） |
| 结构 | 3 条同级核心信条 | **2 条正典（canon，锁定）+ 3 条核心（可改）** |
| 正典实现 | 无 | 礼上的变量 `ac_foundation_1/2`；`ac_rite_foundational_tenet_trigger` 判定；`ac_rite_core_slots_full_trigger` 要求非正典恰好 3 |
| 改动路径 | 建礼/改礼时整体重选 | 礼编辑器只改 3 条核心；正典只能由大公会议谴责（有抵抗与成本） |
| UI | 单行三格（`wrap_count = 3`） | 两行：Canon Tenets 标题 + 2 格，Core Tenets 标题 + 3 格（`gui/window_faith.gui:1180-1260`） |
| 相关机制 | — | 会议/教务会议投票、禁信条、教皇诏书、创建时确认两个正典 |

### 4.2 设计的三个要点

1. **槽位数是配置，不是引擎法**：改上限只需一行 define；真正的成本在 UI 布局、创建流程、改动权限、平衡。
2. **槽位必须结构化**：全可改的 5 格只是数值堆；2 固定 + 3 可改才同时提供"身份稳定"与"改革弹性"。
3. **正典 = 锁定状态**：它约束"谁能改、走什么流程、付出什么代价"，不是一份文本清单，也不是新实体。

### 4.3 落到本项目

| 槽 | 内容 | 改动权限 |
|---|---|---|
| 固定槽 1（正典） | 神观/终极观 | 只能由宗教会议/改革决议改 |
| 固定槽 2（正典） | 一条核心宣认（如经典类型或救赎立场） | 同上 |
| 活动槽 1 | 伦理/性规范 | 改革、会议 |
| 活动槽 2 | 教职/组织形态 | 改革、会议 |
| 活动槽 3 | 礼仪/节期实践 | 改革、会议 |

槽位数做成数据配置（`faithConfig.coreTenetCap` 之类），默认 2+3；每格必须有消费者，且必须有改槽机制。

---

## 5. 术语三桶对照（修正版）

每个词必须只落进一桶：**实体 / 组件 / 文本**。以下是原词表中最容易错的行。

| 词 | 原表落点 | 判断 | 落点 |
|---|---|---|---|
| faith | PersonalFaith | ✗ 方向反了 | 实体 FaithDef（共同体层）；PersonalFaith 是角色层附加 |
| religion | ReligionTag | △ | 实体 ReligionDef（容器 + 默认值） |
| tradition | FaithTradition 组件 | ✗ | 1.20 的对应物是 **rite**（教派/学派）+ tenet |
| doctrine | DoctrineOptionDef | ✓ | 实体（选项须带 group/parameters/modifiers） |
| tenet | Tenet（Core/Permitted/Prohibited/Known） | △ | 实体；状态 5 档 unknown/known/permitted/prohibited/core；mainline 不是状态 |
| canon | CanonTextList | ✗ | 状态/锁定标记（不是实体、不是文本清单） |
| dogma | Dogma | ✗ | 权威状态（加在 doctrine 上），非实体 |
| creed / confession / catechism | Creed 文本 / BranchStatement / Catechism | ✗ | 文本 |
| orthodoxy | OrthodoxyStatus | △ | 派生状态（敌对 + 信条状态 + 领袖权威） |
| polity | Organization / Organization + Office | △ 且前后不一致 | 头衔（religious_head）+ 政体 + 继承规则 |
| liturgy / liturgical form / basic liturgy | Liturgy / LiturgyForm / Liturgy | ✗ | 同一个 **rite**；语言/历法/仪式是它的组件 |
| precept / commandment | Precept / Precept 子类 | △ | doctrine/tenet 的内容 |
| penalty / procedure | Penalty / PenaltyProcedure | ✓ 但落点错 | 司法系统 |
| matrimony / matrimonial law | MarriageLaw | ✓ 但落点错 | 法律系统（信仰做 override） |
| taboo / sexual taboo | Taboo / Taboo(domain) | △ | 罪行轴 + 特质 |
| office / ordination | Office / OrdinationRite | ✓ / ✗ | 头衔与宫廷职 / 任命 doctrine + 活动 |
| magisterium | AuthorityFlag | △ | 派生标记 |
| council / ritual | Council / Ritual | ✓ / △ | 活动 / 活动 + doctrine |
| calendar / language | Calendar / LanguageUse | △ / ✓ | 组件（有消费才做） |
| ultimate reality conception / canonical structure / core narrative | 描述标签 | ✓ | 文本 |
| 宗教学派 | Lineage/School | ✗ 落点错 | **rite**（hanafi/theravada/经学/道学/全真） |
| 修会 | Organization | ✓ 但要真做 | 组织实体（成员/会院/财产/契约） |

---

## 6. 对项目 Faith 模型的最小落点

- **实体**：religion、faith、rite、doctrine_group、doctrine_option、tenet、holy_site、religious_head（头衔）。
- **组件**：language(scope)、calendar、practice、罪行轴参数、virtues/sins、authority flags。
- **文本**：creed、canon 清单、catechism、confession 描述、终极观/核心叙事描述。
- **现有系统承接**：婚姻/性别/罪行 → 法律（`LW/LawComposition.cs`）+ 司法；教职任命 → 头衔；礼仪 → 活动；修会 → 组织实体。
- **接线顺序**：先给 `ReligionDef`/`FaithSystem` 接线（世界生成赋值 `faithId`/`stateReligionId`、教义参数有消费者、玩家有改教入口），再重切 `Doctrines.json`，最后才重构模型与页面。当前 `ReligionCatalog.Create*`、`GetDivergence`、`DoctrinePool.GetOptions`、`FaithSystem.DailyTick` 均无调用者（见 `docs/research/ck3-religion-2026-11-06/02-project-faith-state.md`）。

---

## 7. 组织化阶段（替代 reformed / unreformed）

### 7.1 CK3 现状与问题

- 原版：`unreformed_faith` 是隐藏组里的教义（`special_parameters.unreformed = yes`），"改革"＝新建信仰，旧信仰被改名加 `_old`（`common/religion/doctrine_types/_doctrine_types.info` 原文）。
- 问题：① 只有两档，没有中间态；② 改革是一次性按钮，不是组织演化；③ 组织程度实际散落在教职/神权/修道各教规里，枚举本身不承载任何东西；④ 不能倒退（被迫害、教产被夺、教会崩解）。

### 7.2 阶段表（5 档；按需可裁到 4 档）

| 阶段 | 崇拜单位 | 教职 | 教产 | 层级 | 覆盖 | 经典 | 法权 |
|---|---|---|---|---|---|---|---|
| 0 家户 | 家祠/祖龛 | 家主或元老 | 无 | 0 | 家户 | 口传 | 家法 |
| 1 地方 | 神龛/村庙/圣地 | 兼职（长老、巫、庙祝） | 小庙产 | 1 | 聚落 | 口传/图像 | 地方习俗 |
| 2 教派/社团 | 会堂/社团 | 专职或巡回 | 社团公产 | 2 | 多聚落网络 | 经文形成 | 社团规约 |
| 3 教团/寺院网络 | 寺院/修道院/学院 | 受训教团 | 大宗地产 | 3 | 区域网络 | 正典 | 寺院规约 + 部分法权 |
| 4 国教/教会 | 教区教堂 | 等级教阶 | 教会领地 | 4 | 全境 | 正典 + 教法 | 教会法庭/豁免 |
| 5 普世/帝国教会（可选） | 中央教廷 | 官僚教阶 | 跨政权教产 | 5 | 跨政权 | 普世正典 | 普世法权 |

### 7.3 判据与门禁

- 判据（可测，取"已满足条件的最高档"）：教产规模、专职教职、层级深度、覆盖范围、法权自主、经典形态、领袖层级、财政来源（献仪/什一/国帑）、教育设施。
- 门禁（阶段决定"哪些教规组和行动可选"）：
  - ≥2：神职继承/任命、授任；
  - ≥3：修道制度、修会、封圣、朝圣网络；
  - ≥4：神权政体、信仰领袖、什一税、大公会议、宗教裁判、教会法；
  - ≥5：普世领袖主张、封圣垄断、跨政权权威。
- 变迁方式：决议/事件（建寺院、置教产、立教区、编教法、加冕/国立教会），有成本与条件，**可升可降**；不是"改革按钮"。

### 7.4 项目落点

- `FaithOrganizationType`（Decentralized / Congregational / Episcopal / Papal / Theocratic / StateChurch，`CD/Faith/FaithOrganizationType.cs:4`）目前是死枚举（永远默认 0）。
- 它是**形态**（congregational/episcopal/papal），不是**程度**：保留为形态标签，另加 `organizationStage`（0–5）作为真实状态。
- 状态由判据推导或决议推进；教规、行动、事件池、AI、UI 过滤都读这个阶段。
- 组织本身仍由第 2 节的教规表达（教职职能/资格/继承、修道、神权、领袖），阶段只负责"何时解锁"。

---

## 8. 圣人：全局注册表 + 封圣流程

### 8.1 CK3 现状（实证）

- 候选条件：已死、未被封圣、与封圣者同信仰、**同王朝**、按本礼口径 ≥1 美德且 0 罪（`common/scripted_triggers/pam_saint_triggers.txt:63-80`）。
- 还需：有空墓地/大教堂（一省最多一位圣人）、付虔诚、可能需要信仰领袖批准（`pam_can_canonize_saint_trigger`、`pam_needs_hof_approval_for_canonization_trigger`）。
- 缺：本地崇拜/ cultus、灵验与奇迹、死后年限、社群请愿、殉道/苦修、政治与教义语境；"同王朝"是设计怪癖（变成王朝面子工程）。主保系统另有多套决议/头衔，碎片化。

### 8.2 设计

- **注册表 `VeneratedDead`**：全局、只增（允许撤销/降级）；字段：id、人物引用、类别（saint / walī / divus / siddha / kami / ancestor / hero）、主保领域、崇拜层级（家户/地方/区域/普遍）、圣地与墓、节庆、圣物、封圣记录（谁、何时、依据、流程）、灵验记录、跨信仰对应。
- **每信仰承认集合**：venerated / recognized / contested / heretical（同一圣人可被不同信仰以不同状态承认，解决"全局注册"与"信仰边界"的冲突）。
- **封圣流程**：候选（本地崇拜 + 声望）→ 列真福/承认 → 封圣（权威批准）。条件：死后年限、本地崇拜存在、墓/圣物、灵验/奇迹、社群请愿、权威、教义契合、政治支持、花费。不同传统不同路径（无中央封圣/共识承认/神化/寺院认定）。
- **主保收束为一个关系**：`Patronage`（人/家族/聚落/政权/行会/职业 → 圣人或神祇），按领域匹配给效果；不再另立多套决议。

### 8.3 项目落点

- `SaintDef`（`CD/Faith/SaintDef.cs:6`）现只有 6 个字段；`CanonizationSystem`（`FH/CanonizationSystem.cs`）用"死亡 + 灵性满足 ≥80 + 美德 ≥2"作条件——方向对但过窄。
- 需要：注册表化（全局 + 每信仰承认）、封圣流程与记录、主保关系；把 `RealmData.statePatronSaintId` 并入 `Patronage` 关系。

---

## 9. 神祇注册表

### 9.1 CK3 现状

- CK3 只有 **12 个"神名槽"**：High / Fertility / Wealth / Household / Fate / Knowledge / War / Water / Night / Trickster / Health / Witch（`common/customizable_localization/`，仅本地化占位）。
- 没有神的数据、没有领域、没有万神殿、没有注册表；"神"只是文本变量。

### 9.2 设计

- **`DeityDef` 全局注册表**：id、名与别名、领域（多值带权重）、位阶（至上 / 守护 / 家户 / 祖先 / 英雄 / 精灵）、性别与亲缘、图像、节庆、庙宇与圣地、神话文本、跨信仰对应、崇拜范围、来源（原生 / 神化 / 传入 / 融合）。
- **万神殿**：每信仰一组有序神祇引用 + 角色（至上神/守护神/家神/…）。一神教＝1 至上 + 中介（天使/圣人）；单一神崇拜＝1 至上 + 从属；多神教＝位阶万神殿；泛灵＝精灵类别而非具名神。
- **神会增长**：神化（统治者/英雄崇拜）、融合（interpretatio）、传入、改革 → 与圣人一样全局累积，随时间变多。
- **领域表**：自然（天/日/月/雨/雷/海/河/山/火/大地/丰产/收成）、社会（战争/王权/正义/誓约/财富/商旅/法）、生命（生育/健康/疾病/死亡/冥界）、技艺（冶炼/农事/织造）、抽象（命运/智慧/爱/复仇）、地点（城/河/港/山）、家户（灶/仓/祖先）。
- **领域驱动**：事件池、赐福/降祸、主保匹配、节庆历法、圣地关联、融合匹配、占卜。

### 9.3 项目落点

- `Deity`（`CD/Faith/Deity.cs:7`，name/domain/importance/symbol/festivals）已存在但从不填充（0 外部引用）→ 升级为 `DeityDef` 注册表，加领域表、位阶、万神殿、来源、跨信仰对应。
- 信仰侧挂 `pantheonDeityIds`（有序 + 角色）；地方/家户可挂子集。

### 9.4 起源与演化

- 起源：家户/部落守护神 → 城邦守护神 → 万神殿（征服与融合时并入）；来源分类：原生、神化（统治者/英雄）、传入（interpretatio）、融合（syncretism）。
- 状态机：emergent → recognized → **降格**（降位/等同化/从属/别名）→ **污名化**（恶神/魔鬼/伪神）→ **移除**（名号与祭祀被清除）→ extinct（无祭祀、无记忆）。
- 降格是最常见的一类结局（上面列的"降位并入、等同化"都属此）；污名化是最狠的降格；直接移除罕见。灭绝通常是长期结果，不一定是当场手段。

### 9.4.1 降格阶梯

| 档 | 状态 | 含义 | 典型手段 |
|---|---|---|---|
| 0 | 至上/高位 | 万神殿主神 | — |
| 1 | 万神殿成员 | 有明确位阶 | 保留祭祀 |
| 2 | 地方/家户守护 | 降为地方神、家神 | 降位、改配神庙 |
| 3 | 从属/别名/配偶 | 并入他神谱系 | 等同化、改称、编入神谱 |
| 4 | **污名化** | 由神变"恶神/魔鬼/伪神"（仍被"承认"，但性质反转） | 神学贬斥、经典改写、禁祀、圣像改造 |
| 5 | **移除** | 名号与祭祀被清除（不被承认） | 毁庙、杀/逐祭司、damnatio memoriae、焚经 |
| — | 灭绝 | 无祭祀、无记忆 | 长期失祀 |

- 污名化与移除都不删除注册表条目：改 `status`（demonized / erased / extinct），保留 `memory`，供后世复兴、民俗、考古、异端引用（与"全局注册表只增"一致）。
- 生存条件：庙宇/圣地、祭司团连续性、精英供养、民间崇拜、神话/经典、节庆、朝圣。
- 接触/征服结果：**降格**（降位并入、等同化 interpretatio、从属/别名）、**污名化**、**移除**、**并存**、**替代**（一神教把地方神转成主保圣人）。
- 一神教＝降维万神殿：1 至上 + 中介者（天使/圣人/先知，非神）；单一神崇拜＝1 至上 + 从属神；多神教＝位阶万神殿；泛灵＝精灵类别。
- 机制：神祇对关系（等同/吸收/压制/未知）、圣地转换、祭司改属、节庆重新解释。

### 9.4.2 污名化的三种去向（恶神 / 伪神 / 非神存在）

污名化不只是"降等级"，它可能改变实体的**本体类别**：

| 去向 | 类别变化 | 崇拜状态 | 残留形态 | 波动 |
|---|---|---|---|---|
| 恶神 | 仍是神，性质转为恶/混沌 | 仍有祭祀或安抚 | 邪神崇拜、黑仪式 | 高 |
| 伪神 | 不再是神（偶像/虚妄/骗局） | 禁祀、嘲讽 | 偶像、讽刺文学、护身符 | 中高 |
| 妖魔 | 神 → 魔鬼/恶灵 | 禁祀，转驱邪 | 驱邪仪式、咒语、护符、附身叙事 | 最高 |
| 精怪/怪物 | 神 → 妖怪/精怪/巨人/仙人/鬼 | 无正式祭祀，转民俗 | 传说、忌讳、安抚祭、bogeyman | 高（长尾） |

关键点：

- **"恼人的怪物"是一种存活方式**：它保留记忆但失去神格，转为"安抚/驱邪"的残余仪礼（给怪物献祭让它别作祟——仪式关系仍在，只是从崇拜变成避忌）。所以 `cultStatus` 要区分正式崇拜与安抚仪礼，不能一律记为"无祭祀"。
- **与"移除"的区别**：移除＝记忆被清空/极低（复辟潜力低）；妖魔化/精怪化＝记忆保留（复辟潜力高，可被后世重新解释、融合或复兴）。
- **统一实体注册表**：建议用同一个 `NuminousEntity` 基类承载 神/圣人/祖先/魔鬼/精怪/怪物，用 `class` + `valence` + `cultStatus` + `memory` + `revivalPotential` 描述状态；神→魔→精怪只是同一实体的状态迁移，不是换表。`Deity` 与 `SaintDef` 应作为它的两个子类型，而不是两张互不相干的表。
- **同样适用于圣人/祖先**：圣人可被降为异端/伪圣，祖先可变成怨灵/厉鬼；机制复用同一套类别迁移。

历史参照：巴力→别西卜（恶神→魔鬼）；奥古斯丁等把异教神祇当作 demons；凯尔特 Tuatha Dé Danann 在中世纪爱尔兰传统中变成 sídhe（神族→仙人/精灵）；赛特在埃及后期被污名化（内部污名化）；日本旧神在佛教体系下成为权现或被降为荒神/妖怪；中国淫祀禁毁与"邪神/妖怪"化；西班牙征服者把阿兹特克神像斥为魔鬼。

### 9.5 主保统一层（神祇 / 圣人 / 祖先）

- 一个 `Patronage` 关系：主体（人/家族/聚落/政权/行会/职业/职官）→ 主保（神/圣人/祖先/英雄），带领域匹配、崇拜义务（节庆/献祭/朝圣）、效果（护佑/丰产/合法性）、排他性（一神教只允许中介者）。
- 三种本体同一套关系：神（多神教主保）、圣人（一神教封圣）、祖先（家户/宗族）。
- 生成路径：封圣（流程）、神化（统治者/英雄）、祖先崇拜（家族记忆）、融合（外来神本地化）。
- 历史提醒："主保圣人是万神殿残留"是常见模式（罗马神庙改奉圣人、地方神转主保），但不是普遍规律；殉道者崇拜也有独立生成路径。模型要同时允许"替换"与"独立生成"两条来源。

### 9.6 污名化、移除与波动

- **为什么这两种波动最大**：降格只是改等级，污名化与移除攻击的是**身份边界**——把"我们的神"改写成"恶神/伪神"或彻底抹掉，会同时触发殉道、地下教团、融合伪装、流亡与复辟运动。降格通常只引起局部不满，污名化/移除会引起跨代记忆与反弹。
- **波动变量**：`cultStrength`（崇拜基础：庙宇/祭司/信众/节庆）、`coerciveCapacity`（执行能力：国家/教会）、`eliteCohesion`（精英是否配合）、`replacementAvailability`（有无替代崇拜）、`severity`（降格/污名化/移除）。
- **波动效果**：地方 unrest、合法性下降、热情变化、分裂/殉道风险、地下化（秘密信仰）、后世复兴事件；殉道者还可进入 §8 的圣人/主保联动。
- **设计原则**：波动必须是**局部的、可积累、可触发**的（省份/人口组），不是全局一次性事件；严重度排序：移除 ≈ 污名化 > 降格 > 并存。
- **历史参照**：巴力→别西卜（污名化）；阿肯那顿的阿吞崇拜被清除并遭记忆抹除（移除 + damnatio memoriae，随后复辟）；罗马 damnatio memoriae；基督教化时期神庙改奉与毁像；西班牙毁墨西哥神庙；三武一宗灭佛与后世复兴。

---

## 10. 教统与思想谱系（两张图）

### 10.1 教统是什么

教统＝组织连续性/合法性的传承链，不是思想本身。类型：

| 类型 | 例子 | 合法性来源 |
|---|---|---|
| 制度性教统 | 使徒统绪、主教座、宗主教座 | 按手/座次传承 |
| 传承性法脉 | 佛教法脉/衣钵、苏菲 silsila | 师承 |
| 官祭性连续性 | 罗马祭司团、国家祭祀、职官 | 职位/国家 |
| 会众性连续性 | 会众制、选举制 | 选举/契约 |
| 无 | 自发性地方崇拜 | 无（或神启） |

### 10.2 图结构

- `Succession` 节点：座/法脉；带 `transmissionType`、`seat`、`founder`、`validity`、`recognizedBy`；传承边（人→人 / 座→座）带日期与有效性。
- **validity ≠ recognition**：有效但未共融（东方教会与罗马 431/1054 后即此状态）。
- 分裂：同一教统分裂为竞争 claimant（schism）；也可合并/共融/灭绝。
- `Tradition/School` 节点：parent、解经立场、经典、师承；学派可跨宗派共享，一个宗派内也可多学派。

### 10.3 规则

- **宗派**＝独立组织：教统 + 教产 + 教职 + 边界（入教/绝罚/经典）。
- **学派**＝思想/解经 + 师承，不必然有独立组织；要独立化，需取得连续性（教统/职位/会众）+ 教产 + 边界，否则只能"思想并入"。
- 例外：不以教统为合法性来源的组织——神启/新经典、选举、王朝任命、会众契约（否则无法表达国家祭祀、会众制、复兴运动）。

### 10.4 案例

- 聂斯托里派：组织＝塞琉西亚-泰西封宗主教座（使徒统绪），思想＝安提阿学派（狄奥多若—聂斯托里一线）→ 两图交叉。
- 天主教：罗马座（制度性教统）+ 拉丁学派（思想）。
- 东方教会与罗马：教统有效、共融破裂。

### 10.5 项目落点

- `ReligionNodeType` 已有 Religion / Succession / Tradition / School / ReligiousSchool（`CD/Faith/ReligionNodeType.cs:4`）——正可读成两张图：组织图（Religion → Succession）与思想图（Religion → Tradition → School）。
- 新增 `SuccessionDef`（transmissionType/seat/validity/recognition）、`SchoolDef`（parent/doctrines/texts）、`LineageEdge`（from/to/type/date/valid）。
- 组织阶段（§7）与连续性类型正交：帝国祭祀可以是 4 档 + 官祭性；寺院网络可以是 3 档 + 法脉。

---

## 11. 直接照搬项（无争议）

圣地（eminent/普通、持有/未持有、数量上限）、埋葬（一省一圣、圣物）、朝圣、加冕、葬礼、圣礼——作为"地理 + 活动 + 礼仪"层保留。

---

## 12. 未确定 / 需实测

1. 组完全为空时引擎是否回退 index 0：无脚本证据（原版核心组均被 religion 覆盖）。
2. `rite_has_parameter` 未见于官方文档，作用域靠 1606 次用法反推。
3. 部分参数由引擎硬编码消费（如 `women_cannot_be_granted_titles`），完整清单不可枚举。
4. Arcana Caeli 的 2+3 是模组脚本约定，不是引擎规则；若上游改动礼/信条接口，需要重测。
