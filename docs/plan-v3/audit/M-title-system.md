# 头衔（Title）系统专项审计（2026-11-09）

> 只读审计。锚点均相对 `MyriadCreation/`。与 `D-polity-law-title.md` 有重叠：D 覆盖"政体/法律/头衔"全景，本篇只深挖头衔全链（内容→模板→实例→继承→称谓→官职→存档→UI）。两者冲突时以本篇的逐行证据为准（本篇为后写、行号已复核）。

## 0. 范围

**读过（全量）**：`Assets/StreamingAssets/Base/Title/Titles.json`（139 行，15 条）；`Content/MyriadCreation.Content.Modding/TitleDef.cs`（23）；`Simulation/MyriadCreation.Simulation.Definitions/TitleDefinition.cs`（96）、`TitleCatalog.cs`（76）、`TitleEligibility.cs`；`Simulation/MyriadCreation.Simulation.Systems/TitleManager.cs`（139）、`RealmTitleEvolution.cs`（61）、`TitleAppellationContext.cs`（15）、`SuccessionSchedule.cs`（26）；`Simulation/MyriadCreation.Simulation.Politics/TitleInstance.cs`（76）、`TitleHolding.cs`（37）、`TitleInstanceRegistry.cs`（36）、`TitleHolderRole.cs`；`Simulation/MyriadCreation.Simulation.WorldState/GameWorld.Succession.cs`（378）、`GameWorld.Simulation.cs`（1225，读 140–360/520–560/790–890）；`Simulation/MyriadCreation.Simulation.State/OfficeTitleCatalog.cs`（34）、`RealmData.cs`、`AdminDivisionSaveData.cs`；`Simulation/MyriadCreation.Simulation.Systems/AdminDivision.cs`、`AdminDivisionSystem.cs`（143–164）；`Content/MyriadCreation.Content.Definitions/{TitleModules,TitleInheritanceMode,TitleHoldingMode,TitleAppointmentMethod,TitleResidence,InheritanceLawCatalog,EpithetCatalog}.cs`；`Infrastructure.Save/{GameSaveData,GameSaveSystem,SaveData,SaveJsonMigration}.cs`；`UI/Panels/{GovernmentPanel,SocietyPanel}.cs`、`UI/Common/UIManager.Panels.cs`、`UI/MyriadCreation.UI.Presentation/RealmOverviewText.cs`；`Tests/EditMode/Title*Tests.cs`（8 文件）。

**未覆盖**：`SuccessionSystem.cs`(431 行) 只做 `Title|title` grep 计数=0，未逐行读；`IWorldReadModel`/`RealmSnapshotDto` 全字段；Localization 回退语义；`.prefab/.unity` 引用（未扫场景）。

## 1. 系统清单（三态）

| 系统 | 三态 | 驱动证据 | 入口证据 | 存档证据 | 测试 | 一句话现状 |
|---|---|---|---|---|---|---|
| 内容加载 `Titles.json → TitleDef → TitleManager` | **在跑** | `ContentRegistry.cs:111`→`TitleManager.LoadFromContent:23-43` | 无玩家入口 | 内容层，不入档 | `TitleCatalogTests`(4)、`TitleLanguageDataTests`(2) | 15 条全部加载，但只映射 7/36 字段（`TitleManager.cs:32-41`） |
| 模板查询 `TitleCatalog/TitleManager` | **半跑（缺读侧）** | 在用：`TitleCatalog.Highest:30`←`GameWorld.Succession.cs:62`、`RealmTitleEvolution.cs:35`；`Get:11`、`All:19,33` | — | — | 同上 | `ByTier/ChildrenOf/Roots/GetChain/CountByTier/FilterEligible/ResolveAppellation/PickByWeight/IsHigher` 生产 0 调用（仅测试），见 §4-Q1 |
| 头衔实例 `TitleInstance`/`TitleInstanceRegistry` | **半跑（缺存档、缺消费者）** | 写：`GameWorld.Succession.cs:56-77`（每日 `succession.daily`→`CheckRulerSuccessions:108-161`，`SuccessionSchedule.cs:20` 优先级 26）、`GameWorld.Simulation.cs:293,331-342` | **无**（无头衔面板） | **活档无**：唯一序列化在 `GameSaveSystem.cs:292-311/636-646`，该类 `:39` 标 `[Obsolete("Legacy save path; use SaveSystem. Not wired into the game loop.")]`；活路径 `SaveData.cs:22-45` 无该字段 | `TitleInstanceTests`(2)、`TitleInstanceRegistryTests`(2)、`TitleSaveRoundTripTests`(1，自建 DTO，不经 SaveSystem) | 只在"继位"和"战末补官职"两个时刻写，玩家一生看不到也存不下 |
| 继承三模式 `TitleSuccessionMode` | **半跑（实为单分支）** | 唯一读点 `GameWorld.Succession.cs:69-77` | — | 存模板侧 | `TitleSuccessionModeTests`(4，只测默认值与目录) | 三值只有"None/其它"两条路；`None` 分支实际不可达，见 §4-Q3 |
| 位阶 `rank`(float) | **在跑** | `TitleCatalog.cs:26,35,71` | — | 内容层 | `TitleCatalogTests:40-41` | 全局浮点单值仍是唯一排序依据；§11 的"组合序+绑定"零实现 |
| `tier`/`superiorTitle` | **空转** | 字段在 `TitleDefinition.cs:38-40`，JSON 与该 2 键**不存在**（Titles.json 仅 7 键）→ 恒为 `4`/`""` | — | — | — | `ByTier:77`/`ChildrenOf:82`/`Roots:87`/`GetChain:92`/`CountByTier:111` 零调用者，且即便调用也只返回扁平集 |
| 称谓 `appellationId`/`TitleAppellationContext`/`ResolveAppellation` | **空转** | `appellationId` 无生产者（JSON 无该键、`LoadFromContent` 不映射）；`ResolveAppellation:134-137` 全仓 0 调用 | — | — | 无 | 名词只有签名没有内容 |
| 绰号 `EpithetCatalog`(81 条) | **空转** | grep `EpithetCatalog` 于 `Assets/` 命中 **1** 处（自身定义 `:5`） | — | 绰号本身经 `CharacterSaveData.cs:94` 入档 | 无 | 81 条目录与本系统无连接；实际绰号由 `EpithetSystem` 独立判定（`GameWorld.Succession.cs:317-318`） |
| 官职 6 席 `officeHolders`+`OfficeTitleCatalog` | **半跑（补缺时机错）** | `EnsureOfficeHolders:270-346`（唯一调用 `:880`，仅战争结束） | 局内：`CharacterDetailQueryAdapter.cs:52`→人物详情；`RealmOverviewText.cs:34` 官职段**生产调用不传参**（`UIManager.Panels.cs:679`） | `officeHolders` 入活档：`WorldEntitySaveData.cs:404,417-433` | 无 | 开局到首次战争结束前，6 席全空 |
| 行政区划授职 `AdminDivision.holderCharacterId`/`titleId` | **半跑（懒生成）** | `AssignAdminHolders:157-212`←`EnsureAdminDivisions:131`，仅由 `:543`(颁布法令) 与 `:881`(战末) 触发 | 地图 UI：`RealmOverviewText.cs:60` 显示 `item.titleId` **原串** | `AdminDivisionSaveData.cs:33,48` | `AdminDivisionUiTextTests` | 与头衔实例**完全无关**：`titleId` 是硬编码字符串标签（`AdminDivisionSystem.cs:143-164`），不建 `TitleInstance` |
| 头衔 UI | **不存在** | — | 无授予/罢免/查看头衔按钮：`GovernmentPanel.cs` 仅 5 区块（`:298-331`），`AddSection` 无头衔 | — | — | 玩家无法处置头衔，也无法看到"谁持有什么" |

## 2. 数据模型现状（生产者/消费者）

- **模板三件套**：`TitleDef`(7 字段, `TitleDef.cs:9-21`) → `TitleDefinition`(36 字段, `TitleDefinition.cs:12-82`) → `TitleManager._registry`。映射只 7 个：`titleId/category/successionMode/ownLawPreset/rank/weight/semantic`（`TitleManager.cs:32-41`）。
- **承诺未兑现字段**（有定义、0 生产者、0 消费者）：`designId`、`cultureId/ethnicGroupId/regionId/settlementId`、`appellationId`、`tier`、`superiorTitle`、`modules`、`appointmentOrder`、`eligibility`（唯一消费者 `FilterEligible` 本身 0 调用）、`holders`（生产 0 写，仅 `TitleHoldingTests:22`）、`inheritable`、`enabled`、`seats`、`maxSeatsPerFamily`、`termYears`、`salary`、`annualPrestige`、`familyPowerWeight`、`personnelWeight`、`canPromote`、`dismissalResistance`、`baseLoyalty`、`residence`、`nameColor`、`validPosition`、`onGetTitle`、`onFired`。`TotalWeight:84`、`IsModuleOn:86` 0 调用。
- **实例层**：`TitleInstance{titleId, definitionId, holders[]}`。`definitionId` 只被 `GameWorld.Succession.cs:60-67` 写一次（见 Q4）；官职实例（`realm_{id}_office_{k}`）**从不写 definitionId** → 无模板、无继承法语义。
- **`TitleHolding`**：`characterId/institutionId/role/share/legitimacy` 中，生产侧只写 `characterId/role=Sole/share=1/legitimacy=50`（`TitleInstance.cs:61-74` 默认值）；`institutionId` 仅测试写入（`TitleHoldingTests:34`）→ **机构持有从未发生**；`role` 生产侧只出现 `Sole`（`:67`），`Senior/CoRuler/Elector/Regent` 仅测试；`share`/`legitimacy` 无差异化写入者（永远 1 / 50）。
- **另一条"头衔"**：`CharacterData.ownedTitles: List<int>`（`CharacterData.cs:200`）有存档往返（`CharacterSaveData.cs:106,249,531`）但**生产 0 写 0 读**，是纯死字段。

## 3. 内容数据现状

`Titles.json` 15 条（`"titleId"` 计数=15），字段全集 7 个：`titleId/category/rank/weight/semantic/successionMode/ownLawPreset`。
`category` 分布（`Select-String '"category"'` 计数）：`0`=Landed **6**（emperor 3.0 / king 2.0 / vassal_king 1.8 / khagan 2.6 / khan 2.0 / basileus 3.1）、`1`=Office **3**（prefect 2.4 / magistrate 1.4 / gov_general 2.8）、`3`=NobleRank **6**（duke/marquis/count/baron/zhuhou/qingdafu，1.2–1.6）、`Honor(2)`=0、`Claim(4)`=0。
`successionMode` 分布：`0`(None)=3（即全部 Office）、`1`(RealmLaw)=6（全部 Landed）、`2`(OwnLaw)=6（全部 NobleRank，`ownLawPreset` 全为 `"primogeniture"`）——与 `DECISIONS_M1_M2_M3.md:526` 声明的映射一致。
**被代码引用比例**：`Titles.json` 的 `titleId` 有运行期消费者的仅 `title_basileus`（经 `Highest(Landed)` 间接绑定，见 Q4）与 `RealmTitleEvolution.TitleWord:52-57` 的 5 个 case（king/emperor/khan/khagan/vassal_king）；其余头衔只作为 `AdminDivision.titleId` 字面串出现（`AdminDivisionSystem.cs:149-162` 硬编码 6 个 id）。
**无本地化条目**：`StreamingAssets/` 全量 JSON 搜 `title_emperor|title_king|title_duke` 只命中 `Titles.json` 自身 → 头衔没有任何可显示名称，`semantic` 字段（15 条全有值）**0 消费者**。

## 4. 必须回答的九个问题

**Q1 内容结构**：见 §2/§3。`TitleDefinition` 36 字段 vs `TitleDef` 7 字段 vs JSON 7 键，`LoadFromContent` 映射 7 个（`TitleManager.cs:32-41`）；差额 29 字段全部停在 C# 默认值。

**Q2 运行时真源**：模板 → `TitleManager`（静态字典，`Clear` 见 `:17`）→ 实例 `TitleInstanceRegistry`（静态字典，`GetOrCreate:12`）→ 持有 `TitleHolding`。`TitleInstanceRegistry` 的写方只有 3 处：`GameWorld.Succession.cs:56`（政权主头衔）、`GameWorld.Simulation.cs:293/331/342`（官职 6 席）、`GameSaveSystem.cs:641`（Obsolete 读档）；读方只有 `GameWorld.Simulation.cs:332`（`GetSeniorHolder` 比较，用于避免重复 Transfer）与 Obsolete 存档。**没有任何玩法消费者读"谁持有什么"**（法律门禁读 `monarchId/regent`，不读头衔：`RealmAuthorityResolver.cs:33-43`）。

**Q3 继承/转交**：`GameWorld.Succession.cs:48-106 NotifyRulerTransition` 是唯一转移点，由 `CheckRulerSuccessions:159` 与 `HandleInterregnum:182` 调用（每日）。
- 分支实况：`mode = titleDef?.successionMode ?? RealmLaw`（`:69`）；`None → EndActiveHoldings:72`；`else → TransferSenior:76`。**三模式在代码里只有 2 个分支**，`OwnLaw` 与 `RealmLaw` 完全同路（都直接用政权侧选出的 `newRulerCharId`），从不按 `ownLawPreset` 重选人。
- `ownLawPreset` 消费者：**0**。grep 全仓 5 命中 = `TitleDef.cs:13`(定义)、`TitleDefinition.cs:20`(定义)、`TitleManager.cs:37`(透传)、`Titles.json` 6 条值。`InheritanceLawCatalog` 只有测试引用（grep 5 命中 / 2 文件）。
- **`None` 分支实际不可达**：`definitionId` 只会被绑成 `Highest(Landed).titleId`（`:62-66`），而 `Highest(Landed)` 恒等于 `title_basileus`（rank 3.1，见 Q4），其 `successionMode`=1 → 永远走 `else`。
- **绝嗣写脏数据**：`hasNewRuler=false` 时传 `newRulerCharId = -1`（`:159`），`else` 分支照样 `TransferSenior(-1, day)` → 产生一条 `characterId=-1, role=Sole` 的"在任"持有，`IsActive` 恒真（`TitleHolding.cs:32-35`）。头衔没有"空缺"状态。
- `SuccessionSystem.cs`（431 行）grep `Title|title` = **0 命中**：继承系统完全不知道头衔存在，头衔转移是继位结算后的单向补记副作用。

**Q4 位阶语义**：`rank` 仍是 float 单值，且是唯一排序键（`TitleCatalog.cs:26` 排序、`:35` Highest、`:71` IsHigher）。`tier`/`superiorTitle`：代码有字段（`TitleDefinition.cs:38-40`），**JSON 无此键**，且查询方法零调用（§1）。
`Highest(Landed)` 实际结果 = **`title_basileus`（3.1）**，大于 `title_emperor`(3.0)。后果两条，均已成立：
1. 所有政权的主头衔实例都被绑成 `title_basileus` → 继承法恒 `RealmLaw`（Q3）。
2. `RealmTitleEvolution.GetRulerTitleDisplay:34-45` 用同一 `Highest`，再经 `TitleWord:48-59`——`basileus` 不在 switch 里 → 落 `_ => "王"`。**因此所有政权的君主一律显示为"王"**，皇帝/可汗/大汗的语义全部丢失，编年史即位条目（`GameWorld.Succession.cs:94-95`）写的是"某某即位为王"。`RealmTitleEvolutionTests:66-68` 断言"拉希斯人之王"通过纯属巧合，测试没有钉住"应为王/皇帝/可汗"。

**Q5 称谓/尊号/绰号**：`appellationId` 无生产者（JSON 无键、无映射），`ResolveAppellation` 0 调用；`semantic`（15 条文本）只进内存不显示；`TitleAppellationContext` 仅作 `ResolveAppellation` 参数类型（5 字段：`cultureId/polityKey/isIndependentRuler/isGovernor/institution`，无 §11.4 要求的 `authority/register`）。`EpithetCatalog` 81 条（`Add("` 计数=81）**零引用**；玩家能看到的是 `EpithetSystem.EvaluateAndGrant` 自算的绰号 + `DeterminePosthumousTitle` 谥号（`GameWorld.Succession.cs:317-318`），与头衔系统无接口。

**Q6 官职关系**：`officeHolders` 是 `RealmData` 的 `Dictionary<int,int>`（`RealmData.cs:27`，键 0..5 = `OfficialOffice` 枚举值），是**官职的唯一真源**；`TitleInstanceRegistry` 里的 `realm_{id}_office_{k}` 只是它的影子镜像（`GameWorld.Simulation.cs:331-335` 由 officeHolders 驱动写、`:293/342` 由它驱动清），方向单向、无 `definitionId`。`OfficeTitleCatalog` 只做"官职 → 本地化 key"（`GetTitleKey:14-27`，按 `culture.officialTitles` 匹配，缺省 `"office_"+office`），消费点仅 `CharacterDetailQueryAdapter.cs:52`。`AdminDivision.holderCharacterId` 是**第三套**官职（区划长官，`AdminDivisionSystem.cs:131` 生成 + `AssignAdminHolders:157-212` 配人），与头衔实例互不引用，只在 `LawRealmContext.cs:54`/`DecreePropagationSystem.cs:45`/`SuccessionSystem.cs:269-286` 被读。
**补缺时机**：`EnsureOfficeHolders` 唯一调用点是 `GameWorld.Simulation.cs:880`（`ProcessWarOutcomes` 胜利/白和之后）→ 开局无官职、非战争年份官职空缺不再补；`AssignAdminHolders` 只随 `EnsureAdminDivisions`（`:543` 颁布法令、`:881` 战末）触发 → 区划长官同样懒生成。

**Q7 存档与 UI**：活档（`SaveSystem`/`SaveData`）**不含任何头衔实例**（`SaveData.cs:22-45` 字段表无 title；`RealmDTO` 无 `primaryTitleKey`）；`officeHolders` 与 `AdminDivision.holderCharacterId` 入活档（`WorldEntitySaveData.cs:404`、`AdminDivisionSaveData.cs:33`）。`primaryTitleKey` 与 `TitleInstanceSaveData` 只存在于 Obsolete 的 `GameSaveData.cs:55,66`。**另有一处活档缺口**：`TitleInstanceRegistry.Clear()` 全仓唯一生产调用在 `GameSaveSystem.cs:636`（Obsolete），`ContentRegistry.Reset` 不清理它 → 读档/开新世界后该静态表保留上一局持有者。
UI：无头衔面板、无授予入口（`GovernmentPanel` 5 区块全是法律/法令，`ActionIds.AppointTitle`(`ActionIds.cs:9`) 零使用者）；可见面只有 3 处——人物详情官职名（`CharacterDetailQueryAdapter.cs:52`）、政权总览"官职体系"段（`RealmOverviewText.cs:34-42`，但生产调用 `UIManager.Panels.cs:679` 不传 `officeDisplay` → **永不渲染**）、行政区划树把 `AdminDivision.titleId` **原串**打印给玩家（`RealmOverviewText.cs:60` → 玩家看到"（title_zhuhou）"）。

**Q8 文档裁决对账**（仅对账，不作事实）：
| 文档裁决 | 判定 | 证据 |
|---|---|---|
| M1：头衔=权利束载体，不存 `kind`，五类只作派生视图（`DECISIONS:10,20`） | **已实现（仅数据层）** | `TitleDerivedCategory.cs:8-15`；`Titles.json.category` 无 honor 值 |
| M1：共有头衔 `holders`+`role/share`，机构可持有（`:10,74,81`） | **未实现（生产侧）** | `TitleInstance.cs:61-74` 只写 Sole/1.0；`institutionId` 仅 `TitleHoldingTests:34` |
| M1：X3 完全无权利的头衔 → 称号系统（`:73`） | **未实现** | 头衔与 `EpithetSystem` 无接口 |
| M1：官职 = 带 office 模块的头衔，不另立系统（`:20,64`） | **被违反** | 并行三套：`officeHolders`(真源)、`office_*` 影子实例、`AdminDivision.holderCharacterId`；`TitleModules` 15 键 0 消费者 |
| M1 §8.13 头衔实例入档（`:512`） | **未实现（活档）** | 仅 `GameSaveSystem`（`:39` Obsolete） |
| M2：`appoint_title` 门禁 + 从头衔 holders 解析职权（`:107,426`） | **未实现** | `ActionIds.cs:9` 0 使用；`RealmAuthorityResolver.cs:33-43` 只读 monarch/regent |
| §8 三模式（`TITLE_INHERITANCE_PLAN:119-123`） | **被违反（退化为二分支）** | `GameWorld.Succession.cs:69-77`；`None` 不可达、`OwnLaw` 无行为 |
| §8.1 `InheritanceLawCatalog.Resolve` 驱动 `OwnLaw` | **未实现（仅测试）** | grep 5 命中 / 2 文件 |
| §11 位阶改为组合序+绑定，先替代再删 `rank` | **未实现** | `TitleCatalog.cs:26,35,71` 仍按 float rank；无 `CompositionOrder/BindingPriority/AuthorityDomain`（grep 0） |
| §8.14 已知未完成：`OwnLaw` 不重选人（`:533`） | **文档自认，与代码一致** | 同上 |
| §12 已知不一致：`rank` 仍在、`ByTier/ChildrenOf/GetChain/FilterEligible` 无消费者（`:338,340`） | **文档自认，与代码一致** | §1/§2 |

**Q9 未核清单**：见 §6。

## 5. 缺口清单（按影响排序）

| # | 缺口 | 影响 | 证据 | 严重度 |
|---|---|---|---|---|
| 1 | 继位分支实为单分支：`definitionId` 恒绑 `Highest(Landed)=title_basileus` | `None`/`OwnLaw` 永不生效；"三模式"是文档口径不是运行时行为 | `GameWorld.Succession.cs:62-77` + `TitleCatalog.cs:30-41` | P0 |
| 2 | 全部君主称号退化为"王" | 皇帝/可汗/大汗语义丢失，编年史与显示全错 | `RealmTitleEvolution.cs:34-45,48-59`（`basileus`→`_`） | P0 |
| 3 | 头衔实例不入活档，且静态表不在活档路径清理 | 读档后持有沿革全丢 + 上一局持有者残留；当前君主读档后不持有头衔 | `SaveData.cs:22-45`；`TitleInstanceRegistry.Clear` 仅 `GameSaveSystem.cs:636`；写入仅 `Succession.cs:56-77` | P0 |
| 4 | 绝嗣/无新君时写入 `characterId=-1` 的在任 Sole 持有 | 头衔无"空缺"语义，沿革出现幽灵持有者 | `GameWorld.Succession.cs:159` + `TitleInstance.cs:61-74` | P1 |
| 5 | 开局/新生世界从不把主头衔给人 | 世界首日至第一次政权更替，`TitleInstanceRegistry` 无主头衔持有者 | `NotifyRulerTransition` 仅被 `:159,182` 调用（均为继位） | P1 |
| 6 | 29/36 模板字段无生产者 | 层级树（tier/superior）、资格（eligibility）、席位（seats）、任期/薪俸/模块全部不可用 | `TitleManager.cs:32-41` vs `TitleDefinition.cs:12-82`；`Titles.json` 7 键 | P1 |
| 7 | `officeHolders` 补缺只在战争结束触发 | 开局 0 官职；和平年代死亡空缺永不补 | `EnsureOfficeHolders` 唯一调用 `GameWorld.Simulation.cs:880` | P1 |
| 8 | 官职三套并行且互不引用 | "官职=头衔"裁决失效；任一处改动不会同步另两处 | `RealmData.cs:27` / `GameWorld.Simulation.cs:331` / `AdminDivisionSystem.cs:143-164` | P1 |
| 9 | `EpithetCatalog` 81 条零引用 | 内建绰号库完全不生效（玩家看到的是 `EpithetSystem` 另一套） | grep `EpithetCatalog`=`1` 命中（自身定义） | P1 |
| 10 | 头衔无任何可显示名称/本地化 | UI 只能显示 id 原串或硬编码"王" | `StreamingAssets` 搜 `title_emperor` 仅命中 `Titles.json`；`RealmOverviewText.cs:60` | P1 |
| 11 | `appellationId`/`semantic`/`TitleAppellationContext`/`ResolveAppellation` 全链空转 | "指代+称谓"结构零落地 | `TitleDefinition.cs:25,36`；`TitleManager.cs:134-137`（0 调用） | P2 |
| 12 | `CharacterData.ownedTitles` 死字段（入档但 0 写 0 读） | 存档里存在一列永远为空的"头衔" | `CharacterData.cs:200`；`CharacterSaveData.cs:106,249,531` | P2 |
| 13 | 玩家无任何头衔 UI（无授予/罢免/查看） | 头衔不可被玩家处置，与 M1/M2 承诺不符 | `GovernmentPanel.cs:298-331`；`RealmOverviewText.cs:34` 生产未传参 | P2 |

## 6. 我核实不了的事

1. 运行时 `Highest(Landed)` 是否真的恒为 `title_basileus` —— 逻辑上成立（`rank 3.1` 唯一最大、严格 `>` 比较），但**未实跑**验证；若 `Titles.json` 在构建产物中被改写则结论需复核。
2. `Localization.Get("office_N")` 缺 key 时的回退行为（显示原串还是空）未读实现。
3. `TitleInstanceRegistry` 残留对玩法的**实际**影响面：未找到任何消费者读取主头衔持有者，因此可能只是数据脏、无可见后果——需在接线后再评估。
4. `SuccessionSystem.cs`(431 行) 未逐行读，仅确认 `Title|title` 零命中；若其通过别名/间接结构接触头衔则本结论失效。
5. 场景/prefab 是否引用了 `GameSaveSystem`（D 域称已实测无引用，本篇未独立复验）。
6. `EpithetSystem` 内部是否**间接**使用 `EpithetCatalog` 的字符串 id（别名/反射）未核；grep 层面为 0。
7. 未跑任何测试（只读审计），8 个 `Title*Tests.cs` 共 28 个 `[Test]` 的实际通过状态未验证。
8. **版本口径**：本篇审计对象是工作区（未提交）版本。`git diff HEAD -- .../Titles.json` 显示 138 插入/129 删除——HEAD 版仍是旧 schema（`kind` 字符串，无 `successionMode/ownLawPreset`）。若以 HEAD 为准，Q1 的"字段全集 7 个"与 Q3 的三模式数据分布均不成立（运行时加载的是工作区文件，故本篇按工作区写）。
