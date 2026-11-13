# 宗教术语 × CK3 实际实现：对应、缺陷、必要性

> 2026-11-05。依据：`game/common/religion` 实际文件（faith_types/rite_types/tenet_types/doctrine_*）与 `game/common/decisions/dlc_decisions/pam`。
> 结论：CK3 可直接对应的只有 family/religion/faith/rite、doctrine/tenet、holy site、head、fervor、hostility、personal tenet；很多词只该做文本/标签，不该做系统。

---

## 1. 直接对应，建议采用

| 我们的词 | CK3 对应 | 判断 |
|---|---|---|
| religious family | `religion_family_types` | 对，保留为可选分组 |
| religion | `religion_types` | 对，保留为分组 |
| faith | `faith_types` | 对，保留 |
| rite | `rite_types` | 对，但是**具体传统/礼仪实例**，不是普适层名 |
| doctrine | `doctrine_types` + `doctrine_group_types` + `doctrine_category_types` | 对，数据驱动，比我们 7 类细 |
| tenet | `tenet_types` | 对，采用 |
| tenet status | Core / Permitted / Prohibited / Known | 对，采用 |
| personal tenet | `can_pick_as_personal_tenet` + `personal_tenet_modifier/parameters` | 对，采用 |
| holy site | `holy_site_types` | 对，采用 |
| head of faith | `religious_head = title_key` | 对，但领袖是头衔 + 能力包 |
| head of rite | `head_of_rite = title_key` | 对，可选 |
| fervor | faith fervor | 对，采用 |
| hostility | Righteous/Astray/Hostile/Evil | 对，采用 |
| venerated dead | `saint` trait 的 fallback `trait_venerated_ancestor` | 对，CK3 比我们宽 |

---

## 2. 可映射，但要改名/降级

| 我们的词 | CK3 实际 | 建议 |
|---|---|---|
| denomination | 无独立层；faith/rite 覆盖 | 不做普适层，只做 `BranchType` 显示 |
| tradition-stream | 无独立层 | 改 `School/Lineage` 网络 |
| local custom | 无独立层；rite/doctrine 覆盖 | 改 `Practice` 修正 |
| church/community | CK3 无“信仰共同体”实体 | 组织另立，信仰不等于共同体 |
| school | CK3 无 | 宗教学派归 Faith；政治/哲学/学术另立 |
| polity | CK3 用 title/doctrine | 归 Organization + Office |
| liturgy | CK3 用 rite/tenet/doctrine | 归 `Liturgy` 组件 |
| precept | CK3 用 doctrine | 归 `Precept` 组件，不单独成层 |
| penalty | CK3 用 doctrine/crime | 归法律/司法，不单独成信仰系统 |
| office | CK3 用 title | 接 `TitleHolding` |
| magisterium | CK3 用 doctrine/head | 归 `AuthorityFlag` |
| ordination | CK3 用 doctrine/rite | 归 `OrdinationRite` |
| language | CK3 用 `doctrine_scriptural` + triggers | 用一个 `LanguageUse` + scope，不拆三个实体 |

---

## 3. CK3 做得不够好，我们可以改进

| 领域 | CK3 问题 | 我们的做法 |
|---|---|---|
| Rite 的普适性 | Rite 词/结构偏基督教；泛灵/多神/家神/城邦/部落守护神不适用 | Rite 只作可选具体传统；泛灵/多神用 Pantheon/Shrine/Cult/Patron |
| 主保 | `pam_determine_patron_saint_decision` 只限基督教，只选六使徒，给 modifier | `Patron` 独立关系：神圣/世俗；可挂个人/家族/聚落/政治实体/职业 |
| 圣人 | `saint` trait 是广受崇敬的死者，但只有单 trait，没有 cultus/圣地/节庆深度 | `VeneratedDead`：category（saint/wali/divus/siddha/kami/祖先）、cultus、shrine、feastDay、patronEligible |
| 个人信仰 | CK3 有 personal tenet/secret faith，但没有完整个人信仰层 | `PersonalFaith`：publicTraditionId/privateTraditionId/personalTenetIds/observance |
| 领袖 | 只有 faith/rite head；地方/泛灵/多神无领袖模型；权力散在 doctrine | `ReligiousLeader` = 可选头衔 + 能力包；地方祭司/萨满/家长各自模型 |
| 学派 | CK3 无 denomination/school 区分 | `Lineage/School` 网络 + `IdeologySchool` 分离 |
| 地方实践 | CK3 无 local custom 层 | `Practice` 修正 + `LanguageUse(scope=local)` |
| 泛灵/多神 | CK3 把它们塞进 faith/rite，缺家神/城邦神/部落神 | `Pantheon` + `householdGods/localGods/ancestors/spirits` + `Shrine` |
| 组织 | CK3 教会/修会/圣地分散 | `FaithCommunity/Organization` 独立，可挂可不挂 |

---

## 4. 模拟上没必要，别做成系统

| 词 | 原因 | 处理 |
|---|---|---|
| differentiation level/unit/marker | 分析方法 | 只写文档 |
| creed | 公开宣认文本 | 可做 `Creed` 文本/metadata，不做独立机制 |
| dogma | 权威定断 | 可做 doctrine 的 authority 标签 |
| confession | 分支告白 | 可做 branch 文本 |
| catechism | 教学手册 | 文本/localization，不做系统 |
| orthodoxy | 正统状态 | 可用 doctrine/tag 表示，不做实体 |
| canon | 正典清单 | 除非用于合法性/权威，否则 metadata |
| 三个 language regulation | 高度重叠 | 一个 `LanguageUse` + scope |
| sexual precept / matrimonial law / sexual taboo / penal discipline | 词性不同、互相重叠 | 拆到 `Precept` / `MarriageLaw` / `Taboo` / `Penalty`，不塞进信仰层 |
| 政治/哲学/学术 school | 不属于 Faith | 归 `Ideology`/`Knowledge` |
| denomination / tradition-stream / local custom 作为层 | 不普适 | 降为标签/网络/修正 |

---

## 5. 建议的最小实现集

必须：

```text
FaithTradition（组件）
Faith（分组/可选）
Rite（具体传统/可选）
Doctrine / Tenet（Core/Permitted/Prohibited/Known）
PersonalTenet
VeneratedDead
Patron（神圣/世俗）
ReligiousLeader（title + authority flags）
Organization（可选）
```

可选：

```text
ReligionFamily / ReligionTag
Liturgy / LanguageUse / Calendar
Shrine / Pantheon / Cult
HolySite
```

不做：

```text
Denomination 硬层
TraditionStream 硬层
LocalCustom 硬层
Creed/Dogma/Confession/Catechism/Orthodoxy 独立系统
三个 LanguageRegulation
SexualPrecept/MatrimonialLaw/SexualTaboo/PenalDiscipline 作为信仰子层
政治/哲学/学术 School 塞进 Faith
```

---

## 6. 结论

- CK3 对的：family/religion/faith/rite、doctrine/tenet、personal tenet、holy site、head、fervor、hostility、venerated ancestor；
- CK3 可对应的：language/office/ordination 等降为组件；
- CK3 不够好的：rite 普适性、主保、圣人深度、个人信仰、领袖、泛灵/多神、地方实践；
- 没必要做系统的：creed/dogma/confession/catechism/orthodoxy/canon 独立实体、三个 language regulation、政治/哲学/学术 school 塞进 faith；
- 我们应做“组件 + 可选层 + 头衔/能力 + 个人信仰 + VeneratedDead/Patron/泛灵多神”，而不是照抄 CK3。
