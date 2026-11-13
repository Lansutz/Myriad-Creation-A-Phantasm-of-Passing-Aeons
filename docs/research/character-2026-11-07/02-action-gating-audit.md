# 行动门禁审计：政权决策是否被绑在角色属性上

> 只读审计，未改动任何 project 代码。审计对象：`D:/Myriad-Creation-A-Phantasm-of-Passing-Aeons`。
> 路径前缀约定：`S/` = `MyriadCreation/Assets/Scripts/`，`UI/` = `MyriadCreation/Assets/UI/`。
> 结论基于当前代码事实（grep + 逐文件阅读），不基于 docs 计划。

## 0. 结论速览

- 现有唯一「活」的宣战链路是：AI 意图 → `DeclareWarCommand` → `GameWorld.DeclareWar` → `DiplomacyManager.DeclareWar`。它只查 3 件事：关系存在、当前不在战争、停战期未满。**没有任何职权 / 法理 / 资源检查**。
- **玩家侧根本没有宣战入口**：`Assets/UI` 下只有政府面板（法律/法令），全仓 `DeclareWar` 调用点只有 AI 一处。
- 真正驱动 AI 宣战倾向的是 **统治者个人六维性格**（boldness/greed/honor/vengefulness/compassion/rationality），每 tick 同步成「国家人格」。这就是用户最担心的层级混乱，且它是唯一在生效的耦合。
- 法律/法令系统内部反而是模板级分层（提案权 / 法理前置 / 资源成本分开），但**法令绕过了提案权**（无 proposer 参数）。
- 大量"门禁"字段是装饰品：`Subordination.foreignPolicyControl`（附庸外交权）、`WarRules.allowVassalObligation`、`DeclareWarWithJustification`（唯一完整的法理宣战实现）、`CharacterData.basicAbilities`（零消费者）、`CharacterData.skills`（零消费者）。

---

## 1. 行动入口清单

### 1.1 外交 / 战争 / 军事

| # | 入口 | 触发链（谁调用 → 谁执行） | 实际检查的条件 | 角色属性 | 政权属性 |
|---|---|---|---|---|---|
| 1 | `DeclareWarCommand` | 仅 `AIIntentExecutor.cs:39` 发命令 → `DeclareWarCommandHandler.cs:15` → `GameWorld.DeclareWar`（`GameWorld.Core.cs:335` 注册、`GameWorld.Simulation.cs:1021`）→ `DiplomacyManager.DeclareWar`（`S/Simulation/.../Systems/DiplomacyManager.cs:345`） | 关系存在、未处于战争（348）、停战期未满（352）、取攻击方 realm 名（可能 NRE，354/361） | 无 | 仅 `relation.truceUntilDay` / `isAtWar`；不查合法性/权威/国库/兵源 |
| 2 | 战争法理版 | `DiplomacyManager.DeclareWarWithJustification`（`DiplomacyManager.cs:388`） | 停战期、CB 有效性（399）、按 CB 强度算威望/稳定惩罚（403） | 无 | 政权 prestige/stability 被扣（429-430） |
| 3 | `RaidSettlementCommand` | `AIIntentExecutor.cs:36` → `RaidSettlementCommandHandler.cs:27` → `DiplomacyManager.RaidSettlement`（`DiplomacyManager.cs:902`） | 关系存在、未处于战争（905）；劫掠后 60% 概率**反向自动宣战**（951-964） | `achievements.massacres++`（`RaidSettlementCommandHandler.cs:32-39`） | 无国库/兵源/职权检查；对受害者无阻止机制 |
| 4 | 大圣战 | `GameWorld.DeclareGreatHolyWar`（`GameWorld.Simulation.cs:1043`） | `faithSystem.CanDeclareGreatHolyWar()`（1050）、realm id 合法（1054-1061） | 无 | 信仰层 fervor；无政权资源/合法性 |
| 5 | `ProposeAllianceCommand` | `AIIntentExecutor.cs:45` → `ProposeAllianceCommandHandler.cs:18` → `DiplomacyManager.ProposeAlliance` | 由 DiplomacyManager 内部判定（返回 null 即拒） | 无 | 关系值；无职权（附庸可自由结盟） |
| 6 | `SendGiftCommand` | `AIIntentExecutor.cs:48` → `SendGiftCommandHandler.cs:18` → `DiplomacyManager.SendGift`（`DiplomacyManager.cs:53`） | 关系存在（55）、发起方国库够（59） | 无 | treasury |
| 7 | `MilitaryBuildUpCommand` | `AIIntentExecutor.cs:54` → `MilitaryBuildUpCommandHandler.cs:17` | realm 存在（19）、预算>0 且 treasury 足够（23） | 无 | treasury（仅扣钱，不产兵） |
| 8 | 建军/建舰队 | `GameWorld.CreateArmy/CreateNavy`（`GameWorld.Simulation.cs:1094/1078`） | 无检查 | 无 | 无；**全仓无调用者**（死口） |

> 军事命令系统基本不存在：无 `MoveArmy/IssueOrder/OrderArmy` 一类入口（grep 命中 0）。军团战力在 `S/Simulation/.../Actors/MapActor.cs:334-336` 读 `leader.military + leader.prowess`。

### 1.2 决议 / 法令（grep Decision / Decree / Law 的活入口）

| # | 入口 | 触发链 | 实际检查的条件 | 角色属性 | 政权属性 |
|---|---|---|---|---|---|
| 9 | 推行法律 `BeginEnactLaw` | `UI/Panels/GovernmentPanel.cs:341` → `S/Simulation/.../LawHost/LawContracts.cs:201` → `LawRealmHost.BeginEnactLaw`（`LawRealmHost.cs:82`）→ `CanPropose`（473）+ `CanEnactLaw`（10） | 提案人 >= 0（501）、`state.IsLawKnown`（496）、提案权 `HeadOfState/CentralInstitution/CharterHolders`（506-537）、前置法/互斥法/前置革新（54-95）、treasury/prestige/legitimacy/centralization（29-53） | 仅**身份**（是否首脑/中央官员/特许持有者，`LawRealmContext.cs:22-61`）；**不读任何能力值** | treasury、prestige、`lawState.legitimacy`、`realm.centralization`、lawState 前置法 |
| 10 | 颁布法令 `EnactDecree` | `UI/Panels/GovernmentPanel.cs:381` → `S/Simulation/.../LawHost/LawContracts.cs:229` → `LawAndDecreeSystem.EnactDecree`（206） | 未生效（124）、冷却（129）、treasury（134）、`lawState.authority`（139）、legitimacy（145）、前置法/法令/互斥/革新（150-202） | **无 proposer 参数 → 零职权检查** | treasury、authority、legitimacy、前置法/法令 |
| 11 | 撤销法令 `RevokeDecree` | `GovernmentPanel.cs:370` → `S/Simulation/.../LawHost/LawContracts.cs:249` | 仅"确有生效中的该法令"（258） | 无 | 无（任何人/任何路径都能撤） |
| 12 | 下发政令 `IssueDecreeOrders` | `GovernmentPanel.cs:385` → `DecreeCommandAdapter.cs:18` → `GameWorld.IssueDecreeOrders` → 日推进 `TickDecreeOrders`（`GameWorld.Simulation.cs:695`） | 传令官存活（712）、目标聚落存在（718）、送到（727） | 官员 `management` 决定执行度、`conspiracy` 决定贪墨（`DecreePropagationSystem.cs:196-215`） | lawState 合法性、realm 稳定度（734-736） |
| 13 | 提案取消 `CancelPendingLaw` | `GovernmentPanel.cs:360` → `LawRealmHost.cs:144` | 发起人须是原提案人或首脑（158） | 身份 | 无 |
| 14 | `ForceSetLaw`（政变/派系直改） | `S/Simulation/.../LawHost/LawContracts.cs:266` 起 | 注释明示"绕过已知/授权/推行"（`S/Core/.../Contracts/LawContracts.cs:92-93`） | 无 | 无 |

### 1.3 内政 / 聚落

| # | 入口 | 触发链 | 检查 | 角色属性 | 政权属性 |
|---|---|---|---|---|---|
| 15 | `ImproveRealmEconomyCommand` | `AIIntentExecutor.cs:42` → handler（`ImproveRealmEconomyCommandHandler.cs:22`） | treasury > 50（31） | 无 | treasury |
| 16 | `ConsolidateRealmCommand` | `AIIntentExecutor.cs:51` → handler（`ConsolidateRealmCommandHandler.cs:22`） | 至少有 1 个 coreTile（38） | 无 | coreTiles |
| 17 | `StartInnovationResearchCommand` | `AIIntentExecutor.cs:33` → handler（`StartInnovationResearchCommandHandler.cs:20`） | 交给 `InnovationTree.StartResearch` 内部 | 无（研究速率另由 `scholarship` 决定，见 §3） | 革新前置 |
| 18 | 弃地/迁民/毁城 | `AIIntentExecutor` 无此意图；命令由 handler 包装（`AbandonSettlementTileCommandHandler.cs:19`、`ResettleTileCommandHandler.cs:19`、`DestroySettlementCommandHandler.cs:19`） | 仅"该地块能不能弃/迁/毁" | 无 | 无（无国库/民心/职权） |

### 1.4 交互（grep Interaction）

- 全仓仅 1 处 `Interaction` 语义：`S/Simulation/.../Units/UnitInteraction.cs:3` 是**单位行为枚举**（Travel/Graze/Raid…），与"角色对角色发起的交互"无关。
- **不存在 CK3 式 Interaction 系统**（无 `InteractionDef`、无目标筛选、无接受度判定）。角色层行动入口为零。

### 1.5 玩家侧行动面

- 玩家可触达的命令接口只有：`ILawCommand` / `ILawQuery`（`S/Core/.../Contracts/LawContracts.cs:73`）、`IDecreeCommand`（`UiCommandContracts.cs:7`）、`IWorldReadModel` 等只读视图（`IGameUiHost.cs:22-26`）。
- **宣战 / 劫掠 / 同盟 / 赠礼 / 建军都没有玩家命令接口**；`DeclareWarCommand` 只有 AI 一个发送者。
- UI 假定"玩家 = 首脑"：`GovernmentPanel.ResolveRulerId`（`UI/Panels/GovernmentPanel.cs:259`）直接取 `GetRuler(realmId)` 当提案人；`LawDecreeView.cs:157` 同样用 `realm.GetSupremeRulerId()` 预筛可提案列表。

---

## 2. 门禁条件分类盘点

分类口径：**角色属性 / 角色技能 / 职位或头衔 / 政权制度或法律 / 资源 / 关系 / 其他**。

| 入口 | 角色属性 | 角色技能 | 职位·头衔 | 政权制度·法律 | 资源 | 关系 | 其他 |
|---|---|---|---|---|---|---|---|
| 宣战 | — | — | — | — | — | ✅ 停战期(352) | ✅ 未交战中(348) |
| 宣战(带法理) | — | — | — | ✅ CB 有效性(399) | ✅ 威望/稳定惩罚(429) | ✅ 停战期 | 死代码，无调用者 |
| 劫掠 | 无检查 | — | — | — | — | ✅ 未交战中 | 自动反向宣战(953-961) |
| 大圣战 | — | — | — | ✅ 信仰可宣战(1050) | — | — | realm id 合法 |
| 同盟 | — | — | — | — | — | ✅ 关系值 | — |
| 赠礼 | — | — | — | — | ✅ treasury(59) | ✅ 关系存在 | — |
| 军费 | — | — | — | — | ✅ treasury(23) | — | — |
| 推行法律 | — | — | ✅ 提案权(506-537) | ✅ 已知/前置/互斥/革新(54-95) | ✅ treasury/prestige(29-38) | — | ✅ legitimacy/centralization |
| 颁布法令 | — | — | ❌ **缺失** | ✅ 前置法/法令(150-182) | ✅ treasury/authority(134-143) | — | ✅ legitimacy |
| 撤销法令 | — | — | ❌ 缺失 | — | — | — | — |
| 下发政令 | ✅ 官员 management/conspiracy（只影响执行度） | — | 隐含（有官员才下发） | — | — | — | 传令官存活 |
| 经济/巩固 | — | — | — | — | ✅ treasury(31) | — | — |
| 科研 | — | — | — | ✅ 革新前置 | — | — | — |
| 弃地/迁民/毁城 | — | — | — | — | ❌ 缺失 | — | 地块状态 |

**错层 / 缺层标注（按四层口径）**

| 四层 | 有权重的入口 | 完全缺失的入口 |
|---|---|---|
| 能力（会不会） | 无（法律/法令/宣战都不读能力；只有政令"执行度"读官员 management） | 法案起草质量、外交谈判、统兵资格 |
| 职权（准不准） | 法律提案权（唯一实现到位的职权门禁） | **宣战 / 劫掠 / 同盟 / 赠礼 / 法令颁布 / 撤销法令 / 政令下发 / 建军** |
| 法理（合不合法） | 宣战(带法理版，未接线)、法令前置 | **实际宣战链路**、劫掠、大圣战之外的战争 |
| 资源（付不付得起） | 法令、法案、赠礼、军费、经济 | **宣战（无战争经费/兵源/权威消耗）**、劫掠（无成本）、建军 |

---

## 3. 角色属性 / 专精的读取点（属性 → 消费者）

`CharacterData` 的属性声明集中在 `S/Simulation/.../State/CharacterData.cs:113-152`。

| 属性 | 声明 | 消费者（读取点） | 判定 |
|---|---|---|---|
| `prowess` | `CharacterData.cs:116` | `MapActor.cs:334`（军团战力：`military+prowess`）；`CharacterManager.cs:747/807/819/862`（生成/成长/特质）；`GameWorld.Succession.cs:63`（六维均值→StrongReformer 判定） | 基本健康：只影响战斗与成长 |
| `social` | `:119` | `FactionManager.cs:235/256`（派系影响力）；`GameWorld.Succession.cs:63` | 无"社交/说服"消费点，偏窄 |
| `military` | `:122` | `MapActor.cs:334`（战力）；`GameWorld.Succession.cs:63` | 只影响战斗，**不影响宣战/统兵资格** |
| `management` | `:125` | `FactionManager.cs:235/256`；`DecreePropagationSystem.cs:196-198`（政令执行度）；`GameWorld.Succession.cs:63` | 健康：只影响执行效果 |
| `conspiracy` | `:128` | `FactionManager.cs:235/256`；`DecreePropagationSystem.cs:197-215`（贪墨）；`GameWorld.Simulation.cs:735`（=腐败系数）；`GameWorld.Succession.cs:63` | 健康 |
| `scholarship` | `:131` | `ResearchPlanSystem.cs:269/276`、`ResearchPlanExecutor.cs:29`（研究速率）；`CharacterManager.cs:576`（老年失智触发）；`GameWorld.Succession.cs:63` | 健康 |
| `health` | `:140` | `MapActor.cs:335/380-384`（战力折扣、减员）；`CharacterDiseaseSystem.cs:727-792`；`DiseaseSystem.cs:209` | 健康 |
| `stress` | `:146` | `CharacterManager.cs:550/592/636`（精神障碍发作/康复）；无任何行动门禁 | 无决策消费者 |
| `charm` | `:155` | **仅**存档（`CharacterSaveData.cs:214/490`）+ `MentalHealthSystem.cs:114`（而该函数无调用者） | ❌ **零消费者** |
| `dread` | `:149` | `CharacterManager.cs:571`（偏执触发）、`:645`（增长） | 极窄 |
| `basicAbilities` | `:113` | **无任何读取点**（只有声明、存档 `CharacterSaveData.cs:173/512`、DSO 映射） | ❌ **零消费者（死字段）** |
| `skills`（字典） | `:194`（`[NonSerialized]`） | **无任何读取点**；仅存档读写（`CharacterSaveData.cs:297-303/593`） | ❌ **零消费者（技能树预留字段）** |
| 六维人格 | `:158-176` | `AIController.SyncPersonality`（`AIController.cs:363-383`）→ AI 国家人格；`CharacterData.cs:584-590` 内部（绰号/倾向） | ⚠️ **唯一的"角色→政权"耦合**，见 §4 |

**找不到消费者的（直接列出）**：`basicAbilities`（0）、`skills`（0）、`charm`（0 个游戏逻辑消费者）、`Subordination.foreignPolicyControl`（只写不读）、`WarRules.allowVassalObligation`（只写不读）、`MentalHealthSystem.ApplyDisorderMods`（0 调用者，意味着精神障碍对六维的减益**从未生效**）、`DeclareWarWithJustification`（0 调用者）。

> 附带事实：官职分配不看能力。`GameWorld.EnsureOfficeHolders`（`GameWorld.Simulation.cs:267-336`）只按 `role==Noble/Military/Scholar` 顺序填空，6 个 `OfficialOffice` 无论 Chancellor 还是 WarCommander 都用同一套顺序。若把"职位"当职权门禁，先修这里的"职得其人"。

---

## 4. AI 决策：角色驱动还是政权驱动？

**答案：当前是"统治者人格驱动政权行为"，且这是唯一活着的 AI 决策路径。**

链路（每游戏日执行）：

```
AISchedule.cs:53-59  scheduler "ai.daily"
  → ai.SyncRulers(characters)                    // AISchedule.cs:56
      → AIManager.SyncRulers                    // AIManager.cs:59-69
          → characters.FindRulerOfRealm(id)     // 取统治者
          → AIController.SyncPersonality(ruler) // AIController.cs:363-383
              boldness/greed/honor/vengefulness/compassion/rationality
              → expansionBias / economicBias / diplomaticBias / militaryBias / aggression / riskTolerance
  → ai.DailyTick(...) → TryRaid + MakeMajorDecision(AIGoal 效用打分)
      → FindWeakNeighborAndDeclareWar           // AIController.cs:326-351
          → AIIntent(DeclareWar)                // :349
  → ai.ExecutePendingIntents(executor) → AIIntentExecutor.cs:39 → DeclareWarCommand
```

| 维度 | 事实 | 位置 |
|---|---|---|
| AI 读哪些属性 | 只读统治者六维人格（→ `AIPersonality`）与 `fixedArchetype` 开关；**不读六维能力/技能** | `AIController.cs:363-383` |
| 宣战门槛 | `coreTiles.Count` 比较（谁更弱）+ `realm.treasury > 200` | `AIController.cs:339-350` |
| 劫掠门槛 | `personality.aggression >= 0.45` 或 `expansionBias >= 0.55`，45 天冷却，`aggression>0.6` 时 12% 屠城 | `AIController.cs:69-122` |
| 结盟/赠礼 | 关系值 + treasury（`DoDiplomacy`） | `AIController.cs:281-302` |
| 政权制度输入 | **无**（不读法律/政体/中央机构/合法性） | — |
| `AIDecisionSystem` | 读 `realm.laws` 的 `LawEffects` 初始化 AI 倾向（`AIDecisionSystem.cs:34-42`），但**全仓无 `new AIDecisionSystem`，是死代码** | `AIDecisionSystem.cs:10-23` |

**耦合判定**：

- ⚠️ **P0 级层级混乱**：统治者个人性格 → 国家扩张/侵略倾向 → 宣战。一个残暴但无实权的君主会让整个政权对外侵略；反之国家的地缘、财政、法理、政体、派系利益全不参与"要不要打"。
- 修法方向不是删掉性格，而是**降权重、换主语**：把 `AIPersonality` 改成"政权人格"（政体+主流文化+国教+统治集团+派系），统治者性格只作为"决策噪音/执行风格/战争中的冒险度"的**有限修正项**（例如 ±15% 权重上限），不能独占 `aggression`。

---

## 5. 不合理清单（按严重度）

| # | 问题 | 证据 | 层级错位 |
|---|---|---|---|
| P0-1 | **宣战无职权门禁**：任意势力/附庸/空位期都能宣战；无"宣战权"概念落地 | `DiplomacyManager.cs:345-386` 全程无 actor/office 参数 | 职权层缺失 |
| P0-2 | **统治者性格直接决定国家宣战倾向** | `AISchedule.cs:56` → `AIController.cs:374-379` → `:347-350` → `AIIntentExecutor.cs:39` | 角色层越权到政权层 |
| P0-3 | **法理层完全未接线**：`DeclareWarWithJustification` + `CasusBelli` + `WarJustificationSystem.CalculateDeclarationPenalties` 全部实现完整、零调用；实际宣战用无 CB 的 `DeclareWar` | `DiplomacyManager.cs:388`（0 调用者）；`:955/961` 劫掠自动宣战 | 法理层虽存在但失效 |
| P0-4 | **法令颁布无提案权**：`EnactDecree(realmId, decreeId, day)` 没有 proposer，任何人/任何调用路径都能颁布 | `S/Simulation/.../LawHost/LawContracts.cs:229`、`LawAndDecreeSystem.cs:111`（无 proposer 参数） | 职权层半缺失（法律有、法令无） |
| P1-1 | **附庸外交权是装饰**：`foreignPolicyControl` 被写入 12 个地方，全仓无一处读取用于门禁 | `Subordination.cs:25`；写入 `DiplomacyManager.cs:238/242/247/251`；唯一读取是展示 `DiplomacyRelationBridge.cs:131` | 职权层字段存在但未启用 |
| P1-2 | **空位期/摄政期不封战争权**：`inInterregnum` 只用于继承结算，宣战不查 | `RealmData.cs:91`、`GameWorld.Succession.cs:77-81` | 职权层缺失 |
| P1-3 | **撤销法令零门禁** | `LawContracts.cs:249-264` | 职权层缺失 |
| P1-4 | **宣战无资源成本**：无战争经费、无兵源、无权威消耗；只有带法理版扣威望/稳定（且未用） | `DiplomacyManager.cs:345-386` vs `:429-430` | 资源层缺失 |
| P1-5 | **`basicAbilities` / `skills` / `charm` 零消费者**：数值层是空壳，技能树接上去也无处生效 | `CharacterData.cs:113/155/194` | 能力层未接 |
| P1-6 | **精神障碍减益从未生效**（`ApplyDisorderMods` 零调用） | `MentalHealthSystem.cs:106` | 能力层半失效 |
| P2-1 | **官职不看能力**：6 个官职按角色类型顺序填空，WarCommander 可能由学者担任 | `GameWorld.Simulation.cs:297-334` | 能力层与职权层未衔接 |
| P2-2 | **劫掠无代价、可无限触发自动宣战**（60% / 40%） | `DiplomacyManager.cs:951-964` | 法理/资源层缺失 |
| P2-3 | **`WarRules.allowVassalObligation` 未使用**：附庸参战义务无实现 | `WarRules.cs:22` | 关系层字段空转 |
| P2-4 | **建军/建舰队入口无调用者**，军事命令层事实上不存在 | `GameWorld.Simulation.cs:1078/1094` | 门禁对象缺失 |
| P2-5 | 大圣战绕过政权资源/合法性，只看信仰 fervor 与"有领袖"硬编码 `hasLeader: true` | `GameWorld.Simulation.cs:1050-1062` | 法理/资源层 |

---

## 6. 建议：四层行动门禁模型

### 6.1 分层定义（一句话各一层）

| 层 | 回答的问题 | 数据归属 | 谁读 | 失败表现 |
|---|---|---|---|---|
| **能力 Capability** | 这个人自己做得成吗？ | 角色：六维/技能/健康/精神 | 角色系统 | 办不好、办得慢、办砸（**不是禁止**） |
| **职权 Authority** | 这件事谁有权启动？ | 政权：最高权力归属、官职、特许、附庸契约、空位期 | 政权系统 | 直接拒绝（必须是硬门禁） |
| **法理 Legality** | 以什么名义做？合不合法？ | 政权：法律、CB、条约、习惯法 | 政权系统 | 硬拒 or 高额合法性/威望惩罚（政体决定） |
| **资源 Affordability** | 付不付得起？ | 政权：国库、人力、权威、行动力 | 政权系统 | 直接拒绝 |

核心原则：**能力层永不直接禁止政权级行动，只决定质量与代价；政权级行动的第一道门永远是职权。**

### 6.2 统一入口与门禁接口（伪代码）

```csharp
// 所有行动（玩家/AI/叛乱者）都走同一条管线；actor 不可为 null
public readonly struct ActionActor {
    public int CharacterId;   // 发起人（可为 -1 = 政权本身/制度惯性）
    public int RealmId;
    public ActorKind Kind;    // Player | AI | Rebel | Institution
}

public enum GateLayer { Capability, Authority, Legality, Resource }

public readonly struct GateDenial {
    public GateLayer Layer; public string Code; public string Reason;
}

// 四层顺序固定：职权 → 法理 → 资源 → 能力（能力不拒绝，只回传 modifier）
public static bool TryGate<TRequest>(TRequest req, ActionActor actor, out GateDenial deny)
```

### 6.3 宣战门禁应该检查什么

```csharp
public static bool CanDeclareWar(RealmData atk, RealmData def, ActionActor actor, int day, out GateDenial deny)
{
    // ---- L2 职权（硬门禁）----
    var sov = atk.laws.supremeAuthority;                 // 设计见 docs/design/SUPREME_AUTHORITY_MODEL.md:114
    if (atk.inInterregnum && atk.regentCharacterId < 0) return Deny(Authority, "no_regent");
    if (!WarAuthority.HasRight(sov, actor))              // 单君主=首脑；同僚制=全体/轮值；机构主权=机构表决
        return Deny(Authority, "no_war_power");
    if (atk.suzerainId >= 0 && atk.IsForeignPolicyControlled())   // 复用 foreignPolicyControl
        return Deny(Authority, "suzerain_controls_foreign_policy");

    // ---- L3 法理（按政体决定硬/软）----
    var cb = WarJustificationSystem.GetStrongestCB(atk.cbList, atk.realmId, def.realmId, day);
    if (cb == null) {
        if (atk.laws.ForbidsWarWithoutCause()) return Deny(Legality, "no_casus_belli");
        // 否则允许，但记合法性/威望代价（现有 CalculateDeclarationPenalties 可直接用，:103）
    }

    // ---- L4 资源（硬门禁）----
    if (atk.treasury < WarCosts.WarChest(atk))        return Deny(Resource, "insufficient_treasury");
    if (atk.manpower < WarCosts.MinLevy(atk))         return Deny(Resource, "insufficient_manpower");
    if (atk.lawState.authority < WarCosts.Authority)  return Deny(Resource, "insufficient_authority");

    return true;   // 能力层不参与"能不能宣战"
}
```

### 6.4 角色 Martial / Prowess **应该**只影响什么

| 该影响 | 不该影响 |
|---|---|
| 亲征时的战斗修正（已有：`MapActor.cs:334`） | 政权是否宣战（应由职权+利益+法理决定） |
| 单挑/突围/镇暴等"本人下场"的行动成功率 | 同盟是否接受、和约条款、CB 是否成立 |
| 军队统率上限、练兵/军改类行动的质量 | 国库能否承担、附庸能否独立开战 |
| 战争中的冒险倾向（作为 AI **修正项**，±15% 上限） | 国家扩张倾向 `expansionBias` 的**主体** |

即：`Martial` 是**执行能力**，不是**开战权**。它可以决定"这一仗打得好不好"，不能决定"这一仗打不打"。

### 6.5 落地顺序建议（最小改动优先）

1. 把 `EnactDecree` 加 `proposerCharacterId`，复用 `LawAndDecreeSystem.CanPropose`（照抄法律链路）。
2. 抽一个 `AuthorityRules`（把 `ProposalAuthority` 泛化），供宣战/同盟/劫掠/大圣战/法令共用。
3. 新建 `IDiplomacyCommand`/`IWarCommand` 玩家侧契约（当前玩家无宣战入口），所有入口一律携带 `ActionActor`。
4. 接线 `DeclareWarWithJustification`（或把 CB 检查并入 `DeclareWar`），删掉裸 `DeclareWar` 的公开入口。
5. `AIPersonality` 改为政权人格：`国家利益分数`为主项，`ruler 性格`仅作修饰项，权重设上限。
6. 修 `EnsureOfficeHolders`：按 `OfficialOffice` 要求的能力/资历选人，否则职权门禁建立在不称职的官职上。

---

## 7. 存档 / 读档是否覆盖门禁相关状态

| 状态 | 是否入档 | 证据 | 后果 |
|---|---|---|---|
| 角色六维 / health / stress / 人格 | ✅ | `CharacterSaveData.cs:208-217`、`:484-493` | 能力层状态可恢复 |
| `basicAbilities` / `skills` / `charm` | ✅ 存取但无消费者 | `CharacterSaveData.cs:173/512`、`:297-303/593` | 存了也没用 |
| 政权 `lawState`（legitimacy/authority/known/pending/charter） | ✅ | `RealmDTO.cs:20`；`RealmData.cs:75` | 法律的职权/法理/资源门禁可恢复 |
| 政权 `decreeState`（active/cooldown/orders） | ✅ | `RealmDTO.cs:22` | 法令门禁可恢复 |
| 官职 `officeHolders` | ✅ 单独落盘 | `WorldEntitySaveData.cs:393-433` | 职权身份可恢复 |
| `WarState` 列表 | ✅ | `SaveData.cs:49`；`SaveSystem.cs:446-453` | 战争实体可恢复 |
| **`DiplomaticRelation`**（isAtWar / truceUntilDay / hostility / CB 列表 / 同盟 / 条约） | ❌ **不入档** | `SaveData.cs:43-49` 只有 `relations`(投影) 与 `wars`；无 Diplomacy 段 | **读档后宣战门禁全部归零**：停战期消失、可对正在交战方重复宣战；AI 的 `TryRaid` 判定也失真 |
| `Subordination`（附庸外交权/军役义务） | ❌ 不入档（仅投影为 `EntityRelation`） | `DiplomacyRelationBridge.cs:22-52` | 附庸门禁状态丢失 |
| `_nextTreatyId` 等自增 id | ❌ 不回滚（除 war/army/navy） | `SaveSystem.cs:474-478` | 外交实体 id 撞号风险 |

读档时序问题：`SaveSystem.cs:446-453` 先恢复 `wars`，随后 `:460-461` `new WorldCompositionRoot(world).InitializeSubsystems()` 重建 `DiplomacyManager`（`DiplomacyManager.cs:808-811` 构造函数调用 `InitializeAllRelations()`，随机生成全部关系的 relation/trust/threat）。结果：**战争列表存在，但关系表认为双方没在打仗**。

结论：门禁状态（尤其是"谁有权/是否在停战/是否有 CB/是否是附庸"）当前**不可靠持久化**；在补职权门禁之前必须先补 `DiplomaticRelation` 入档（或至少在重建后用 `wars` + 编年史回灌关系）。

---

## 8. 附注：本次审计的边界

- 未审计 `Faith` / `Culture` / `Genetics` 的内部门禁（除大圣战、政令执行外）。
- 未审计战斗结算内部细则（`Combat/Warfare` 实际只有 `MilitaryBuildUpCommand` 两个文件）。
- `docs/design/SUPREME_AUTHORITY_MODEL.md:110-128` 已经写明"主权持有者类型 → 法律批准权、**宣战权**、继承危机、合法性来源"，说明"宣战是政权层"在**设计层**已有共识；本报告的问题全部是**实现层未接线**，不是设计分歧。
