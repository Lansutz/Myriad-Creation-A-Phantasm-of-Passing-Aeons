# 宗教术语覆盖矩阵（全术语落位）

> ## ⚠ 本文件只做一件事：现状 → 迁移量
>
> **设计口径不看代码**：主运行单位是 **`faith`（信仰）**；`lineage` 是**传承**（faith → 父信仰的引用）。依据：`BELIEF_SYSTEM_MODEL.md` 顶部"口径优先"块 + D29/D31。
> 本文中"只有 22 条建了 FaithSystem""宗派在代码里叫 `Tradition`""`parentReligionId` 一 int 三义"等，都是**代码欠账清单**，不是模型定义。读法：左列＝要改掉的现状，右列＝要迁到的目标。


> 2026-11-10。`RELIGION_V2.md` 只深挖了**教理**一条线；本文补**其余全部术语**的落位：每个术语归哪一层、挂哪个字段、谁写、谁读、是否入档、现在有没有、没有是"计划建"还是"决定删"。不重复 V2 的教义组／档／参数设计。
> 口径：`BELIEF_SYSTEM_MODEL.md`（六层＋个人层、§4.2 术语禁令）＋ `research/RELIGION_{STRUCTURES,FACTS_VERIFIED,SCHOLARSHIP}.md`。**代码事实全部 grep 复核**，凭印象的一句没写。
> 路径前缀（均在 `MyriadCreation/Assets/Scripts/` 下）：`DEF/`=`Content/…Content.Definitions/Faith/`｜`CD/`=`Content/…Content.Definitions/`｜`MOD/`=`Content/…Content.Modding/`｜`SIM/`=`Simulation/…Simulation.Faith/`｜`HOST/`=`Simulation/…Simulation.FaithHost/`｜`WS/`=`Simulation/…Simulation.WorldState/`｜`ST/`=`Simulation/…Simulation.State/`｜`LAWS/`=`Simulation/…Simulation.Laws/`＋`…Simulation.LawHost/`｜`CC/`=`Core/…Core.Data/`｜`SAV/`=`Infrastructure/…Infrastructure.Save/`｜`JSON/`=`Assets/StreamingAssets/Base/`

**内容与运行基线（本轮实测）**：`JSON/Religion/Religions.json` 44 条、`Doctrines.json` 46 条、`Tradition/Traditions.json` 13 条、`Ethos/Ethos.json` 8 条；44 条的 nodeType 分布＝ Religion 7 ／ Succession 22 ／ **Tradition 8** ／ School 6 ／ ReligiousSchool 1。`InitializeFaithSystems` 只为 nodeType=Succession 建运行态，**所以"在跑的 faith"只有 22 个**（`WS/GameWorld.Core.cs:972`，日志写作「… 个教统」）。宗派级的 8 条（法华宗/禅宗/唯识宗/日莲宗/灭喜禅/正一道/全真道/帕西派）恰恰**不在**运行集里。

## 主表

| 术语（中／英） | 归属层／实体 | 承载字段 | 谁写 | 谁读 | 入档 | 现状 | 证据 | 处置 |
|---|---|---|---|---|---|---|---|---|
| 信仰／faith | 层3 faith（可玩单位） | `FaithSystem.faithId`；`PopulationGroup.faithId`；`CharacterData.faithId` | 内容表建运行态；`MissionarySystem` 改宗；`CreateCharacter` | `ByFaith`、地图配色、人口比重 | 是 | 在跑 | `WS/GameWorld.Core.cs:966-988`；`CC/PopulationGroup.cs:15`；`SAV/FaithSaveData.cs:23` | 保留；补 `religionId`/`denominationId` 归属 |
| 宗教／religion（总概念＋层1实体） | 层1 religion | `ReligionDef`（nodeType=0，7 条） | 内容 JSON | 仅作 `GetRoot` 父指针 | 否（静态） | 半跑 | `DEF/ReligionDef.cs:10`；`DEF/ReligionCatalog.cs:42` | 保留并升为实体（补 `familyId`/`creedId`/`canon`）；**与 faith 共用 id 空间，见冲突 C3** |
| 传承／tradition（＝`lineage`） | 层3 faith → 父 faith | `ReligionDef.parentReligionId`＋`hasSuccession`＋`ReligionNodeType.Succession` | 内容 JSON（22 条 nodeType=1） | `GetRoot`、`ResolveTraditionId` | 否 | 半跑 | `DEF/ReligionDef.cs:14,20`；`DEF/ReligionNodeType.cs:7`；`DEF/ReligionCatalog.cs:199` | 改名：**传承不叫传统**；字段拆为 `lineageFaithId`（见冲突 C3） |
| 教义／doctrine | 组件（挂层1/2/3） | `DoctrineOptionDef`（46 条）＋`ReligionDef.selectedDoctrines`（每条 0-9 个） | 内容 JSON | `GetDoctrines`（UI dto）、`GetReformPillars`、`GetDivergence` | 否 | 在跑（无数值后果） | `DEF/DoctrineOptionDef.cs:9-19`；`JSON/Religion/Doctrines.json` | 保留；按 `RELIGION_V2.md:85-98` 分组化，别在本表重复设计 |
| 信条／creed | 层1 religion（唯一） | 无（计划 `religion.creedId`） | 计划：内容+改革决议 | 计划：正统判定/传教 | 否 | 空 | 无（grep `creed` 仅命中 `decree`） | **新建**：最小版 = `creedTextId`＋`creedTenetIds[]` |
| 单条原则／tenet | 组件（`Doctrine` 的子项） | 无（`FaithSystem.doctrines` 是空表，见下） | 计划 | 计划：正统/异端 | 否 | 空 | `SIM/Doctrine.cs:7-16`（只有 name/description/category/strictness） | **新建** `tenet{id, textId, authority}` |
| 教理定断／dogma | tenet 的属性 | 无 | — | — | 否 | 空 | 无 | **合并**进 `tenet.authority ∈ {tenet,dogma}`，不建实体 |
| 宗派告白／confession | 层2 denomination | 无 | — | — | 否 | 空 | 无 | **合并**为 `denomination.confessionTextId?`；无文本管线时不阻塞（可空） |
| 教义问答／catechism | — | 无 | — | — | — | 空 | 无 | **删除**（理由见 §2-1） |
| 正统／orthodoxy | 层1 religion | `ReligionDef.orthodoxy : bool`（默认 true） | 内容 JSON | **无读者** | 否 | 空（有字段） | `DEF/ReligionDef.cs:26`（全仓仅此一处出现） | **重写**为 `orthodoxy{tenetIds[],exclusions[]}`；布尔语义不符（冲突 C4） |
| 正典／canon | 层1 religion | 无（旁支：`scripturalLanguage` 40 条有值） | 内容 JSON | 无 | 否 | 空 | `DEF/ReligionDef.cs:42`；`JSON/Religion/Religions.json`（`"scripturalLanguage": "拉丁语（武加大）/…"`） | **新建** `canon{textIds[],closed,living}`；本作无文本表 → 最小版 `textIds` 用 string |
| 治理／polity | 组件（挂层1/2） | 无（`polityKey` 是**政体**不是治理） | 计划 | 计划：任命/合法性 | 否 | 空 | `CD/OfficeTitleEntry.cs:10`；`CD/InnovationUnlock.cs:16`（`PolityForm`） | **新建**组件；复用官职／头衔系统，不另造教阶 |
| 礼仪／liturgy | 组件（挂层1/2） | `ReligionDef.rites[]`（18 条非空）＋`riteFamily`（12 条）＋`liturgicalLanguage`（40 条） | 内容 JSON | **只有地图配色**（`PrimaryRite` 参与色相哈希） | 否 | 半跑 | `DEF/ReligionDef.cs:40,48,50,58`；`DEF/ReligionCatalog.cs:270` | **合并**为 `liturgy{ritualIds,calendarId,languageId}`；`rites` 字符串保留原值 |
| 戒律／precept | 组件（挂层2/3） | 无（`FaithSystem.taboos` 恒空） | 计划 | 计划：司法/婚姻接线点 | 否 | 空 | `SIM/FaithSystem.cs:28`（无写者无读者） | **新建** `precept{matrimonyId?,tabooIds[],source}` |
| 处分／penalty | 组件（挂层2） | `PunishmentType`（9 值，世俗）＋`CrimeDefinition.defaultPunishment` | `CrimeRegistry` 静态表 | 世俗刑罚判定 | 否 | 半跑（无教会处分） | `CD/PunishmentType.cs:3-14`；`LAWS/CrimeRegistry.cs:97,110` | **合并**：复用 `PunishmentType`，补教会专属种类 |
| 教职／office | 组件 `polity.office` | `FaithSystem.hierarchyTitles[]`＋`hierarchyLevel`（0-4） | `AddHierarchyTitle`（**全仓无调用**） | `HierarchyName`（UI 文本） | 是 | 半跑（装饰） | `SIM/FaithSystem.cs:56,69`；`DEF/EcclesiasticalTitle.cs:3-10`；`HOST/FaithContracts.cs:88,231` | **合并**：教职＝`TitleInstance`＋`isEcclesiastical`；废弃平行结构 |
| 训导权／magisterium | 组件 `polity` | 无 | 计划 | 计划：正统/异端裁决 | 否 | 空 | 无 | **新建** `magisterium{scope,plural,authoritySource}`（依据 `FACTS_VERIFIED:29-31`） |
| 授任／ordination | 组件 `polity` | 无（最接近：`EcclesiasticalTitle.temporalAppointment : bool`） | 内容（计划） | 无 | 是（该 bool 随 `hierarchyTitles` 入档） | 空 | `DEF/EcclesiasticalTitle.cs:9` | **新建** `ordination.kind ∈ {sacramental,license,appointment,none}`，替换该布尔 |
| 会议／council | 组件 `polity`（暂用政体枚举） | `CentralInstitution.EldersCouncil/ReligiousCouncil`＋`TitleAppointmentMethod.CouncilNomination` | 内容/政体 | 政体判定 | 否 | 半跑（无会议实体、无决议） | `CD/CentralInstitution.cs:8,10`；`CD/TitleAppointmentMethod.cs:11` | **合并**：不建实体；会议只作 `polity.councilKinds[]` 取值 |
| 仪式程序／ritual | 组件 `liturgy` | `FaithSystem.rituals`（无写者无读者）＋`ReligionDef.rites` | 内容 JSON（只写了 `rites`） | 无 | 否 | 空 | `SIM/FaithSystem.cs:26`；`DEF/ReligionDef.cs:48` | **合并**进 `liturgy.ritualIds`；两个字段留一个 |
| 节期／calendar | 组件 `liturgy` | `Deity.festivals`（无读者、无内容） | 计划 | 计划：税/动员/朝圣窗口 | 否 | 空 | `DEF/Deity.cs:17` | **新建** `calendar.feastDays[]`（day-of-year 列表，最小版） |
| 礼仪语言／language | 组件 `liturgy` | `ReligionDef.liturgicalLanguage` | 内容 JSON | **无** | 否 | 半跑（有 40 条数据无消费者） | `DEF/ReligionDef.cs:40` | **保留**；接教育／传教接线点（`RELIGION_V2.md:108-111`） |
| 婚姻治理／matrimony | 组件 `precept` | 无实体；只有 `doctrine_divorce_forbidden/permitted/allowed` 3 档 | 内容 JSON | 无 | 否 | 半跑（有档位无接线点） | `JSON/Religion/Doctrines.json:24-26` | **新建接线点**，不建实体（V2 §3.2 婚姻点） |
| 禁忌／taboo | 组件 `precept` | `FaithSystem.taboos`（恒空） | 无写者 | 无读者 | 否 | 空 | `SIM/FaithSystem.cs:28` | **合并**进 `precept.tabooIds[]`；最小版＝`DoctrineOption` 的 taboo 组 |
| 成文戒条／commandment | — | 无 | — | — | — | 空 | 无 | **删除**（理由见 §2-2） |
| 处分种类／penaltyKind | 组件 `penalty` | `PunishmentType`（Death/Corporal/Fine/Imprisonment/Exile/Slavery/PublicHumiliation/Forfeiture/Pardon） | 静态枚举 | 刑罚判定 | 否 | 在跑（世俗） | `CD/PunishmentType.cs:3-14` | **保留**＋新增教会类（绝罚/停职/除名/苦修/禁罚） |
| 处分程序／procedure | 组件 `penalty` | 无 | — | — | 否 | 空 | `LAWS/` 只有 severity＋defaultPunishment | **新建** `procedure{judge,appeal,standard}`（谁判/怎么上诉） |
| 宗教家族／family | 层0 | 无（`FaithType` 是神观，不是家族） | 计划 | 计划：家族页/排他性 | 否 | 空 | `DEF/FaithType.cs:5-13` | **新建**（`familyId` 7 个根节点可先由 7 条 nodeType=0 充当） |
| 宗派／denomination | 层2 | **代码里就是 `ReligionNodeType.Tradition`（8 条）** | 内容 JSON | `ResolveTraditionId`→人口 `religionTraditionId` | 否 | 半跑（有内容无 status/divergence） | `DEF/ReligionNodeType.cs:8`；`CC/PopulationGroup.cs:17`；`DEF/ReligionCatalog.cs:199-226` | **改名** `Denomination`＋补 `status/divergence/isDefault`（冲突 C2） |
| 学派／school | 层4 | `ReligionNodeType.School`(6)＋`ReligiousSchool`(1)＋`ReligionDef.school : string` | 内容 JSON | 无（`schoolParentId` 写而不读） | 否 | 半跑（6 条 `parentReligionId=-1` 悬空） | `DEF/ReligionNodeType.cs:9,10`；`DEF/ReligionDef.cs:16,52` | **保留**；补挂靠（faith/denomination）＋`praxisId/exegesisId/ruleId` |
| 地方习俗／custom | 层5 | 无 | 计划 | 计划：地方抵抗/礼仪变体 | 否 | 空 | 无 | **新建**（最小版并用 `precept.tabooIds`＋`idiomId`） |
| 终极观／ultimacy | 层0 标志 | `FaithType`（Animistic/Polytheistic/Henotheistic/Monotheistic/Pantheistic/Atheistic/Cosmic，7 档）＋`ReligionDef.worldview : string` | `FaithSystem.type` 默认 `Polytheistic`；内容写 `worldview` | 无 | 否（`type` 不入档） | 半跑（三份真相，冲突 C6） | `DEF/FaithType.cs:5-13`；`SIM/FaithSystem.cs:18`；`DEF/ReligionDef.cs:22` | **保留** 7 档改名 `ultimacy`；废止 `worldview` 文本 |
| 核心叙事／narrative | 层0 标志 | `Ethos`（8 条，`ContentResolvers.Ethos` 可查） | 内容 JSON | **零消费者** | 否 | 空 | `MOD/EthosDef.cs:8`；`Simulation/…Simulation.Modding/ContentResolvers.cs:19`；`JSON/Ethos/Ethos.json` | **合并**：`family.narrativeId → ethosId`，别新建叙事表 |
| 入教／initiation | 层2 标志 | 无 | 计划 | 计划：改宗/成员资格 | 否 | 空 | 无 | **新建** `initiation{kind,grades[]}`（分级入教见 `RELIGION_STRUCTURES.md:95-97`） |
| 修行法门／praxis | 层4 标志 | 半：教义池 `pillar="experience"` 4 条（内在之旅/奉爱/秘传/朝圣） | 内容 JSON | 无 | 否 | 半跑 | `JSON/Religion/Doctrines.json:40-43` | **合并**：`school.praxisId` 指向 doctrine group，不建新表 |
| 解经传承／exegesis | 层4 标志 | 无 | 计划 | 计划：解释权威/教育 | 否 | 空 | 无 | **新建** `school.exegesisId`（最小版：`doctrineGroupId` 引用） |
| 规约／rule | 组织层 | 无（`LegalNormKind.Charter` 是世俗特许） | 计划 | 计划：修会/教团纪律 | 否 | 空 | 无（grep `RuleDef`／`class *Rule` 均 0） | **新建**（最小版 string id＋内容表，先不接线） |
| 地方语言／idiom | 层5 标志 | 有语言系统（`JSON/Language/Laethis/Language.json`），与宗教无连接 | 语言系统 | 语言系统 | 是 | 半跑（无 `custom.idiomId`） | `StreamingAssets/Base/Language/` | **新建引用**：`custom.idiomId → LanguageDef` |
| 传承引用／lineage（字段级） | 层3 faith | 计划 `Faith.lineageFaithId`；代码等价物＝`parentReligionId` | 内容/分裂事件 | 计划：合法性/正宗判定 | 否 | 空 | `DEF/ReligionDef.cs:14`（一字段三职，冲突 C3） | **新建字段**并分离职责；`childFaithIds` 照抄文化 `childCultureIds` |
| 主保／Patronage | 层1/层P 共用 | `RealmData.statePatronSaintId`（默认 -1）＋`Deity.importance/domain` | **无写者** | `GetPatronSaintName`（无 UI 调用者） | 是 | 半跑（有字段有入档，无写入无读者） | `ST/RealmData.cs:31`；`HOST/FaithContracts.cs:205-229` | **保留**并扩为 `Patronage{patronRef,domain,obligations}`（Deity｜Saint｜Ancestor 共用） |
| 神祇／Deity | 层1/0 内容 | `Deity{name,domain,importance,symbol,festivals}`；`FaithSystem.deities[]`＋`chiefDeity` | **无内容**（`deities` 恒空） | 无 | 否 | 空 | `DEF/Deity.cs:8-18`；`SIM/FaithSystem.cs:20,22` | **保留结构**，补 `rank/role/status/cult/parentDeityId`（入档） |
| 万神殿／Pantheon | 层1 内容 | 无（只有 `chiefDeity` 字符串） | 计划 | 计划：神谱/吸纳 | 否 | 空 | 无 | **新建** `pantheonId`＋`synthesisEvents[]`；`pantheonId=null` 即原始宗教 |
| 圣人／Saint | 层1 内容 | `SaintDef{id,characterId,name,domain,faithId,canonizationPiety=80}`＋静态 `_saintsByFaith` | `Canonize()`（**全仓无调用者，仅测试**） | `GetSaints`→UI dto | **否**（静态字典 Memory） | 空 | `DEF/SaintDef.cs:8-18`；`HOST/CanonizationSystem.cs:42,49`；`grep Canonize(` 仅测试 | **保留**＋接流程（地方崇拜→灵验→请愿→批准）＋`feastDay/patronage[]` |
| 特许权／Privilege | 跨系统 | 碎片：`LocalSuccession.CityCharter`、`ContractType.Charter`、`LawRealmHost.GrantCharter`、`TreatyClauseType.TradePrivileges`、`ActionGateContracts.CharterHolder` | 世俗法律系统 | 世俗法律系统 | 是（随契约/法律） | 半跑（世俗在跑，宗教侧无） | `CD/LocalSuccession.cs:9`；`Core/…Core.Enums/ContractType.cs:5`；`LAWS/LawRealmHost.cs:41`；`CD/TreatyClauseType.cs:20` | **合并**为统一 `Privilege{kind,scope,revocable}`；豁免＝kind 之一 |
| 个人信仰／PersonalFaith | 层P | 无实体（散在 `CharacterData`） | — | — | — | 空 | `ST/CharacterData.cs:53,85,87,89,91` | **新建**聚合对象，字段复用现有四件 |
| 个人信条／personal_tenet | 层P | `CharacterData.personalTenets : List<string>`（存 `optionId`） | `AddPersonalTenet/RemovePersonalTenet`（**无游戏入口调用**） | `HasFaithDivergence`（`Count>0` 即视为有偏离） | 是 | 半跑 | `ST/CharacterData.cs:89`；`Simulation/…Simulation.Systems/CharacterManager.cs:46-63,75`；`SAV/CharacterSaveData.cs:85` | **保留**；补来源（行为事件）与 `authority`；**注意 §5.7 记"grep 0"已过期** |
| 灵性满足／spiritualFulfillment | 层P | `float`，默认 50，clamp 0-100 | `UpdatePiety`（唯一写入：德行×0.01 − 罪行×0.02 − 秘密信徒 0.01） | 封圣阈值 ≥80（**调用链断**） | 是 | 在跑 | `ST/CharacterData.cs:85`；`Simulation/…Simulation.Systems/CharacterManager.cs:102-118`；`SAV/CharacterSaveData.cs:89` | **保留**刻度，改来源为行为事件；三个常数无来源→ `TODO_M05` |
| 私誓／vows | 层P | 无 | 计划 | 计划：违约后果/朝圣 | 否 | 空 | 无 | **新建** `vows[{kind,term,breachPenalty}]`（个人信仰影响历史的主通道） |

## 1. 重复与冲突（必须改名或隔离）

| # | 冲突 | 事实 | 处置 |
|---|---|---|---|
| C1 | **"传统"一名三义** | ①`MOD/TraditionDef.cs:9-31`＝文化传统（13 条 `trad_*`，`solidification`/`exclusiveToCultureIds`）；②`DEF/ReligionNodeType.cs:8` 的 `Tradition`＝宗教树节点；③`FamilyTraditions.json`（`famtrad_*`）另立一套 traditionId 命名空间 | 信仰侧一律改称**传承**；`TraditionDef` 改名 `CulturalTraditionDef`；宗教树里的 `Tradition` 改 `Denomination`（见 C2） |
| C2 | **宗派被塞进 `Tradition`** | `ReligionNodeType.Tradition` 的 8 条内容名就叫"宗"（法华宗/禅宗/正一道/全真道/帕西派…），语义是宗派；而它们**不在** `InitializeFaithSystems` 的白名单（只收 Succession） | `nodeType=2` → `Denomination` 实体；补 `status/isDefault/divergence`；否则"主流/异端"无处挂 |
| C3 | **`parentReligionId` 一字段三职** | 同一字段同时充当：层级父指针（`GetRoot`）、传承引用（`hasSuccession`）、宗派归属 | 拆三字段：`familyId`（层级）、`lineageFaithId`（传承）、`denominationId`（宗派）；`parentReligionId` 只留 ancestry |
| C4 | **`orthodoxy: bool` ≠ 正统** | `DEF/ReligionDef.cs:26` 声明 `bool orthodoxy = true`，全仓零读者；规划要的是"正统边界（tenetIds+exclusions）" | 删布尔，改结构体；同批把 `isMainstreamTradition`（`ReligionDef.cs:38`，只被 `ResolveTraditionId` 读）迁为 `denomination.status` |
| C5 | **神观三份真相** | `FaithType`（7 档枚举，`FaithSystem.type` 默认 Polytheistic）／`ReligionDef.worldview`（自由文本"一神"）／`Doctrines.json` 的 `doctrine_monotheism|polytheism|dualism|animism`（教义选项）同时表达神观 | 只留 `ultimacy`（7 档）当权威；`worldview` 文本废止；教义池里 4 条神观移入 `ultimacy` 选项域，不再可"多选多个神观" |
| C6 | **两套教职** | `EcclesiasticalTitle`（titleName/level/temporalAppointment，入档但无 gameplay 读者）vs 世俗 `OfficialOffice`/`TitleInstance`（在跑） | 教职＝`TitleInstance`＋`isEcclesiastical` 标记；`EcclesiasticalTitle` 降级为存档兼容 |
| C7 | **`faithId` 默认 0 撞 id 空间** | `ST/CharacterData.cs:53` `public int faithId;` → 默认 0，而 religionId 从 100 起；`CreateCharacter` 有多处传 0 | 默认改 −1，并在加载期断言"无 religionId==0" |
| C8 | **`FaithSystem.doctrines` 是死表** | `SIM/Doctrine.cs:7-16` 的 struct 只在 `FaithContracts` 里当 fallback 读，全仓 `doctrines.Add` 命中 0，且不入档 | 删该字段；教义唯一来源＝`ReligionDef.selectedDoctrines`（+ 规划的分组快照） |
| C9 | **`HeritageDef` 空载** | 加载器存在（`MOD/ContentDefinitionIndex.cs:207-211` 找 `Heritage/Heritages.json`），但 `JSON/Heritage/` 目录不存在 | 与信仰无关，**保持隔离**；不要把它当"传承"用（§4.2 禁令） |
| C10 | **处分种类只有世俗九类** | `PunishmentType` 为 Death/Corporal/Fine/Imprisonment/Exile/Slavery/PublicHumiliation/Forfeiture/Pardon，无绝罚/停职/除名/苦修 | 追加教会类，不新开枚举（避免 `switch` 双表） |

## 2. 决定删除的术语（学 CK3 但不要的）

| # | 术语 | 理由 |
|---|---|---|
| 1 | `catechism` 教义问答 | 是**教学手册体裁**，不是模拟实体。本作无文本阅读系统，建了也只有空字段；它要表达的"教义可被教授"由"教育接线点读 `canon`+`magisterium`"承担 |
| 2 | `commandment` 成文戒条 | 与 `precept`（有约束力的伦理戒条）重名不同物。「是否成文」由 `precept.source ∈ {code,commentary,custom}` 一个取值表达，不值得单开一层（CK3 亦无此层） |
| 3 | `dogma` 作为独立实体 | 它只是 `tenet` 的**权威等级**。单开实体必然与 tenet 重复，且"某条原则被定为定断"是可变事件，属性位更合适 |
| 4 | `rite` 作为独立层 | 承 `RELIGION_V2.md:119` 第 1 条：一个槽不能同时装"礼仪变体"和"宗派"。礼仪降为 `liturgy` 组件的 `ritualIds` |
| 5 | `unreformed` 式布尔 | 承 `RELIGION_V2.md:120`：改革＝新建 faith，布尔无信息量；本表同批删 `ReligionDef.orthodoxy:bool`、`ReligionDef.hasSuccession`（后者由 `lineageFaithId == -1` 表达） |
| 6 | `ReligionDef.riteHeadName` / `communionName` / `headName` | 三者**写而无读**（仅 `CreateSuccession` 内部拷贝与内容表填写，grep 无 gameplay 读者）。教会首脑归 `polity.offices`，共融关系归 `denomination` 间关系，字符串字段删 |
| 7 | `schoolParentId` | 写而不读（全仓仅赋值 −1），且 6 条学派 `parentReligionId` 本身就是 −1 → 学派整层悬空。删字段，改由 `school.ownerRef` 单一挂靠 |
| 8 | `ReligionCatalog.GetDivergence` 现有实现 | 死代码（无调用者），且内含 `30f × pillarWeight`（0.3/0.2/0.15…，`ReligionCatalog.cs:152-172`）**全是无来源魔数**，违反数值纪律。删实现，按 V2 §3.1 的"档位距离＋内容权重"重写 |
| 9 | `ReligionNodeType.Religion` 作为"内容节点" | 与 `faith` 共用 `ReligionDef` 表、共用 id 空间，导致"22 个 faith 之外还有 7 个宗教、8 个宗派、7 个学派"混在一张表里。删掉"类型混装"的做法，落成五张表（family/religion/denomination/faith/school） |

## 3. 缺口清单（按依赖排序，标最小可跑版本）

| 序 | 缺口 | 依赖 | 最小可跑版本（能通电即可） |
|---|---|---|---|
| G1 | `Religion` 层实体 | — | `ReligionDef` 加 `familyId`＋`religionOfFaithId`；7 条 nodeType=0 直接当 family，不新建表 |
| G2 | `lineageFaithId`（传承） | G1 | 复用 `parentReligionId` 值，改名＋分离；`childFaithIds` 由反查生成，不落盘 |
| G3 | `Denomination` 实体 | G1 G2 | 把 nodeType=2 的 8 条改类型；只补 `status ∈ {mainstream,tolerated,heterodox,condemned,extinct}`＋`isDefault`；`divergence` 先置 0 并标 `TODO_M05` |
| G4 | 全量 faith 归属 | G1-G3 | `InitializeFaithSystems` 放开到 44 条中所有非 School 节点，补 `religionId/denominationId`；**这是"宗教系统通电"的第一块砖** |
| G5 | `ultimacy` 归位 | G4 | `FaithType` 7 档改名＋`ReligionDef.ultimacy` 字段；`worldview` 文本转档位映射表 |
| G6 | 制度四组件最小字段 | G4 | `polity/liturgy/precept/penalty` 各只落 3-4 字段；`liturgy` 直接复用已有的 `rites/riteFamily/liturgicalLanguage`，先做到"有读者"（教育接线点读 `languageId`） |
| G7 | `penalty.procedure`＋教会处分种类 | G6 | 接 `LAWS/CrimeRegistry`：异端/亵渎两条罪行（已存在，`LAWS/CrimeType.cs:10`、`LAWS/CrimeRegistry.cs:92-117`）配"谁判/能否上诉"两字段 |
| G8 | `magisterium`＋`ordination.kind` | G6 | 只做 `{scope,plural,authoritySource}` 与 `kind` 四值；不建教阶实体（教职已由头衔承担） |
| G9 | `Patronage` 写入端 | G4 | `statePatronSaintId` 补一个写入口（立国教时选主保）；读端已有 `GetPatronSaintName` |
| G10 | `Saint` 流程接线 | G9 | 先接"权威批准"以外的三步最小版：`Canonize()` 挂到一个年度事件；`feastDay/patronage[]` 补字段 |
| G11 | `Deity`/`Pantheon` 内容与入档 | G4 | `Deity` 补 `rank/status/cult/parentDeityId`；`deities` 入档（现在完全不存）；`pantheonId=null` 即原始宗教 |
| G12 | `PersonalFaith` 聚合＋`vows` | G4 G6 | 只做 `patron`＋`vows[]`＋`conversionHistory[]` 三件；`spiritualFulfillment` 已有刻度，只需换来源 |
| G13 | `custom`＋`idiom` | G4 G6 | `custom{placeRef,tabooIds,idiomId}`；`idiomId → LanguageDef` 只读不写 |
| G14 | `Privilege` 统一对象 | G6 | 把已有五处碎片（见主表）收成一个 `Privilege` 视图，**不新建存档表** |
| G15 | `initiation`／`praxis`／`exegesis`／`rule` | G3 G4 | 四者都先做成"字符串 id＋内容表"，只求字段存在与可校验，暂不接线 |

> **最小可跑集（M1）＝ G1+G2+G3+G4**：不做这四项，其余全部术语没有归属层可挂——现状是 22 条内容在跑、22 条被静默丢弃，且"宗派/传承/宗教"三种语义挤在 `parentReligionId` 一个 int 上。G5-G8 是"通电"（有读者、有后果）；G9-G15 是"有内容"。

## 4. 待查（不编，给查法）

| # | 问题 | 查法 |
|---|---|---|
| 1 | `Ultimacy` 该几档（现 7 档：含 Cosmic/Atheistic，与 `FaithType.cs` 原注释不一致） | 对照 `BELIEF_SYSTEM_MODEL.md:633` 待确认第 1 条；先做 7 档映射表，再用现有 44 条内容试填，填不进去的档删 |
| 2 | `divergence` 三权重、`status` 迁移阈值 | 全部 `TODO_M05`；本表不预设数字（承 `RELIGION_V2.md:134`） |
| 3 | 教会处分种类清单（绝罚/停职/除名/苦修/禁罚）是否够覆盖东正/伊斯兰/佛教 | 查教会法汇编（Gratian C.11/C.24 相关）+ 佛教僧团 `pārājika/saṃghādisesa` 分类；本会话未取得可引来源 |
| 4 | `temporalAppointment`（叙任权在君/在教）现有 2 条教义档 `doctrine_clerical_appointment_*` 是否够表达 `caesaropapist/dualist` | 对照 `RELIGION_SCHOLARSHIP.md:107-118` 六形态，逐形态试填；填不进的形态即缺字段 |
