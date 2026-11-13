# 术语—计划对照矩阵

> 2026-11-05。目的：把你给的那一大串词逐项落到现有代码/现有计划里，避免“计划里根本没有这些词”。
> 已读：`ReligionDef`、`FaithSystem`、`ReligionNodeType`、`DoctrinePool`、`DoctrineCategory`、`TitleDef`、`TitleManager`、`PopulationIdentity`、`EthnicGroupDef`、`CultureData`、`TraditionDef`。
> 已读计划：`UNIFIED_DESIGN_V2.md`、`LANGUAGE_NAMING_IDENTITY.md`、`CIV_COGNITION_TITLE_CURRENCY.md`、`ARCHITECTURE_2_0_LAW_OFFICE_NAME_REGION_MODEL.md` 5A/5A.5A、`WORLD_SANDBOX_AND_SYSTEMS_PLAN.md`。

---

## 1. 先承认

前面的 `TOTAL_SYSTEM_SYNTHESIS.md` 只是大框架，**没有逐项覆盖你给的术语**。这份矩阵补上。

---

## 2. 元层术语

| 你的词 | 类型 | 处理 | 落点 |
|---|---|---|---|
| differentiation level | 分析工具 | 不进运行时 | 设计文档/审查清单 |
| differentiation unit | 分析工具 | 不进运行时 | 设计文档/审查清单 |
| differential marker | 分析工具 | 不进运行时 | 设计文档/审查清单 |

理由：这是比较方法，不是游戏实体。游戏运行时只存实体、组件、关系。

---

## 3. 五层单位

| 你的词 | 决定 | 落点 | 现有代码/计划 |
|---|---|---|---|
| religious family | 可选标签 | `FaithFamilyTag` | `ReligionNodeType` 有 Religion/Tradition；无 family |
| religion | 可选分组 | `ReligionTag` | `ReligionDef.parentReligionId` |
| denomination | **不当普适层**；只作可选显示类型 | `BranchType` | `ReligionNodeType.School/ReligiousSchool` |
| tradition-stream | **不当层**；改为解释网络 | `School/Lineage` | `ReligionNodeType.Tradition` 混用 |
| local custom | **不当层**；改为实践修正 | `Practice` | 无 |

---

## 4. 各层区分标志

| 你的词 | 决定 | 落点 |
|---|---|---|
| ultimate reality conception | 描述性标签，不强制 | `TraditionTag` / `FaithTraditionDef` |
| canonical structure | 描述性标签，不强制 | `CanonStructure` 值 |
| core narrative / revelation paradigm | 描述性标签，不强制 | `NarrativeTag` |
| creed / dogma | 组件 | `Doctrine`（机制）+ `Creed`（公开宣认） |
| canon | 文本清单 | `CanonTextList` |
| basic liturgy | 组件 | `Liturgy` |
| polity | 组织/治理组件 | `Organization` + `Office` |
| confession | 分支告白文本 | `BranchStatement` |
| liturgical form | 礼仪形式 | `LiturgyForm` |
| denominational polity | 分支治理 | `Organization`（不新层） |
| initiation | 入教程序 | `InitiationRite` |
| precept | 戒律 | `Precept` |
| praxis method | 修行方法 | `PracticeMethod` |
| interpretive tradition | 解释传承 | `Exegesis` |
| ritual detail | 礼仪细节 | `RitualDetail` |
| rule | 修会/流派规约 | `OrganizationRule` |
| custom | 地方惯行 | `Practice` |
| taboo | 禁忌 | `Taboo` |
| local language usage | 地方语言惯行 | `LanguageUse`（scope=local） |

---

## 5. 语言/教职/伦理子类

| 你的词 | 决定 | 落点 |
|---|---|---|
| liturgical language regulation | 合并 | `LanguageUse(scope=liturgy)` |
| ritual language regulation | 合并 | `LanguageUse(scope=ritual)` |
| scriptural language regulation | 合并 | `LanguageUse(scope=scripture)` |
| ecclesiastical office system | 组织/职位 | `Office` + `TitleHolding` |
| magisterium | 能力包 | `AuthorityFlag.Magisterium` |
| ordination | 程序 | `OrdinationRite` |
| sexual precept | 戒律子类 | `Precept(domain=sexual)` |
| matrimonial law | 法律 | `MarriageLaw` |
| sexual taboo | 禁忌 | `Taboo(domain=sexual)` |
| penal discipline | 处分 | `Penalty` |

理由：language 不需要三个 regulation 实体；office/magisterium/ordination 不是同层；precept/matrimonial/taboo/penalty 词性不同。

---

## 6. 单词术语表

| 你的词 | 决定 | 落点 |
|---|---|---|
| faith | 个人采纳/归属 | `PersonalFaith` |
| religion | 可选分组 | `ReligionTag` |
| tradition | 不单独当层 | `FaithTradition` 组件 |
| doctrine | 机制教义 | `DoctrineOptionDef`（现有） |
| creed | 公开宣认 | `Creed` 文本 |
| tenet | 身份信条 | `Tenet`（Core/Permitted/Prohibited/Known） |
| dogma | 权威定断 | `Dogma` 文本/状态 |
| confession | 分支告白 | `BranchStatement` |
| catechism | 教学文本 | `Catechism` 文本 |
| orthodoxy | 正统状态 | `OrthodoxyStatus` |
| canon | 正典清单 | `CanonTextList` |
| polity | 组织/治理 | `Organization` |
| liturgy | 礼仪 | `Liturgy` |
| precept | 戒律 | `Precept` |
| penalty | 处分 | `Penalty` |
| office | 职位 | `Office` |
| magisterium | 训导权 | `AuthorityFlag` |
| ordination | 授任 | `OrdinationRite` |
| council | 会议 | `Council` |
| ritual | 仪式 | `Ritual` |
| calendar | 历法 | `Calendar` |
| language | 语言使用 | `LanguageUse` |
| matrimony | 婚姻治理 | `MarriageLaw` |
| taboo | 禁忌 | `Taboo` |
| commandment | 成文戒条 | `Precept` 子类 |
| procedure | 处分程序 | `PenaltyProcedure` |

---

## 7. 学派类型

| 你的词 | 决定 | 落点 |
|---|---|---|
| 宗教学派 | 归 Faith 系统 | `Lineage/School`（可选） |
| 政治学派 | 归 Ideology 系统 | `IdeologySchool` |
| 哲学学派 | 归 Ideology 系统 | `IdeologySchool` |
| 学术学派 | 归 Knowledge/Scholarship | `AcademicSchool` |
| 修会/规约团体 | 组织 | `Organization`（不是 school） |

`school` 不能当一个普适字段；必须按系统分开。

---

## 8. belief-system 四型

| 你的词 | 决定 |
|---|---|
| religious | `Faith` 系统 |
| political | `Ideology` 系统 |
| philosophical | `Ideology`/`Philosophy` |
| secular | 现代概念，不建独立层；用 `Ideology`/`Worldview` |

不把政治派系塞进 Faith。

---

## 9. 与现有计划对齐

| 现有计划 | 已有内容 | 本矩阵怎么接 |
|---|---|---|
| `UNIFIED_DESIGN_V2` | 文化/宗教/语言分开；伞形词警告 | 本矩阵把 faith/religion/tradition 拆成组件 |
| `LANGUAGE_NAMING_IDENTITY` | PopGroup、语言/宗教/族群份额、边界 | 信仰认同接 PopGroup 份额 + 边界 |
| `CIV_COGNITION_TITLE_CURRENCY` | 头衔 authorities（ControlReligion 等） | 宗教领袖能力包接 `authorities` |
| `ARCHITECTURE_2_0...` 5A/5A.5A | 族群认同嵌套/渠道/阀门/下沉度 | 信仰认同复用同一套 |
| `WORLD_SANDBOX...` | 法律组只是组、法令需条件 | 信仰组件同样“组只是组” |

---

## 10. 实施计划（可执行）

### Phase 1：本体与适配

- 新 `FaithTraditionDef`（组件）：category、effects、incompatibleWith、requiredInnovations、upgradesFrom、solidification；
- 新 `PersonalFaith`：publicTraditionId/privateTraditionId/personalTenetIds/observance；
- 新 `VeneratedDead`：category（saint/wali/divus/siddha/kami/ancestor…）、cultus、shrine、feastDay；
- 新 `Patron`：type（deity/saint/ancestor/founder/hero）、scope、target、modifier；
- 新 `ReligiousOffice`/`AuthorityFlag`：接 `TitleHolding`；
- 保留 `ReligionDef` 作旧档适配，逐步拆。

### Phase 2：现有代码收口

- `ReligionNodeType`：移除 `Tradition/School` 硬层，改为标签；
- `FaithSystem`：拆成 `FaithCommunity`（组织）+ `FaithTradition`（内容）+ `PersonalFaith`（角色）；
- `DoctrineCategory/DoctrinePool`：保留，加 `canPickAsPersonalTenet`；
- `SaintDef/CanonizationSystem`：改为 `VeneratedDead`，支持多宗教词；
- `EpithetSystem`：绰号/谥号分开。

### Phase 3：认同/人口

- 接 `LANGUAGE_NAMING_IDENTITY` 的 `PopGroup`：信仰份额、语言、族群份额；
- 接 5A.5A：`reachByRole`、`accessChannels`、`boundaryValves`、`nestedParentId`；
- 不默认全民同信仰。

### Phase 4：领袖/主保

- `ReligiousLeader` = title + authority flags；
- `Patron` 可选，分神圣/世俗；
- 泛灵/多神用 `Pantheon/Shrine/Cult/ritualSpecialist`；
- 不做“每层必有领袖”。

### Phase 5：历法/UI/存档

- `Calendar` 独立，顶部 UI；
- 角色页分：公开信仰/个人信仰/个人信条/主保/圣人；
- 地图只显示政治实体名，不显示头衔/信仰层级；
- 存档迁移。

---

## 11. 结论

- 你给的词不是“没进计划”，是之前没有被逐项映射；这份矩阵补上；
- 现有代码 `ReligionDef/FaithSystem/DoctrinePool` 可保留作底层，但需要拆；
- 现有计划 `LANGUAGE_NAMING_IDENTITY`/5A/5A.5A 已经给了认同/人口/边界骨架，应复用；
- 不硬套 CK3 四层，不硬套 denomination/tradition-stream/local custom；
- 下一步按 Phase 1–5 执行，不再先写内容。
