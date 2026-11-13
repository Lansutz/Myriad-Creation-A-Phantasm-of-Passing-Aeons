# 军事 / 战争 / 单位 / 行军 / 寻路 / 海军 / 外交 / 关系 / 战争法理 审计（2026-11-09）

## 0. 范围

**读取的文件/目录**（行数为 `Get-Content | Measure-Object -Line` 计数；锚点相对 `MyriadCreation/`）

- `Assets/Scripts/Simulation/MyriadCreation.Simulation.Units/**`：30 个 `.cs`。逐行读：`UnitPathfinder.cs`(68)、`MapUnit.cs`(135，前 70 行)、`MapUnitRegistry.cs`(128)。仅按调用面抽查：`TransportUnit.cs`(638)、`UnitProvisioning.cs`(122)、`TreasuryLedger.cs`(332)、`TerrainConditions.cs`(171)、`TransportCatalog.cs`(148)。
- `Assets/Scripts/Simulation/MyriadCreation.Simulation.Warfare/**`：2 个 `.cs`（`MilitaryBuildUpCommand` 9、`MilitaryBuildUpCommandHandler` 28），全读。
- `Assets/Scripts/Simulation/MyriadCreation.Simulation.Diplomacy/**`：9 个 `.cs`，全读（命令/Handler 共 8 个 + `DiplomaticEvent`）。
- `Simulation/.../Systems/`：`CombatManager`(258)、`DiplomacyManager`(1017 行含空行，工具报 963)、`DiplomacySchedule`(29)、`DiplomacyRelationBridge`(142)、`PathfindingSystem`(343)、`MovementControlSystem`(258)、`MilitaryMovementSystem`(284)、`PeaceNegotiationSystem`(277)、`WarJustificationSystem`(404 仅按调用面)、`RetinueSystem`(45)、`RetinueBand`(18)、`UnitFounding`(53)、`NomadicRealmSystem`(148 仅按调用面)、`SettlementControlSystem`(242)、`SettlementControlRuntime`(44)、`AIController`(相关段)、`AIIntentExecutor`(60 全读)、`AISchedule`(62 全读)、`ManpowerSystem`(83)、`SandboxSpawnSystem`(140)、`SettlementSimulationSystem`(88)。
- `Simulation/.../State/`：`Army.cs`(421)、`Navy.cs`(81)、`WarState.cs`(54)、`WarRules.cs`(60)、`DiplomaticRelation.cs`(149)、`RealmData.cs`(204)。`Alliance/Subordination/Treaty/TreatyClause/WarGoal/CasusBelli/Negotiation*` 按枚举与消费者 grep 覆盖。
- `Simulation/.../FaithHost/`：`WarfareReligionSchedule.cs`(60 全读)、`ReligionSimulationEventHandler.cs`(51 全读)。
- `Core/MyriadCreation.Core.Events/`：`WarDeclaredEvent.cs`(10)、`WarEndedEvent.cs`(12)。
- `Infrastructure.Save/`：`ArmySaveData.cs`(111)、`ArmyPersistence.cs`(69)、`NavySaveData.cs`(132)、`SaveSystem.cs`(相关段 330–630)、`SaveData.cs`(78 全读)、`GameSaveSystem.cs`（仅确认 `[Obsolete]`）。
- `WorldState/GameWorld.*`：`Simulation.cs`(1225 全读)、`Core.cs`(相关段)、`MapWorldViewAdapter.cs`(96–155)。
- 测试：`Assets/Tests/EditMode/` 中 `ArmyPersistenceTests`(137)、`ArmyAndGarrisonSafetyTests`(60)、`NavyEntityTests`(29)、`NavyPersistenceTests`(39)、`UnitFoundingTests`(63)、`MapUnitTests`(111)、`ActionGateTests`（DeclareWar 用例）。
- 玩家入口：`Assets/UI` 全目录（`Panels/` 恰 6 个：`SocietyPanel`、`SettlementDetailPanel`、`SaveLoadPanel`、`MainMenuUI`、`GovernmentPanel`、`SettingsPanel`）。
- `Assets/StreamingAssets/Base/`：全量文件清单 + 全部 JSON 的军事关键字扫描。

**未覆盖 / 只到调用面**

- `TransportUnit.cs`(638) 未逐行读：只确认 `MoveSpeedMultiplier`/`TryAddMember`/`IsFoundingReady` 的调用者。
- `WarJustificationSystem.cs` 只核到"谁调它"，未逐条验算 CB 权重公式。
- `Simulation.Actors`（`MapActor`/`MapActorManager`）只读移动与土匪片段，未读全。
- `Assets/Tests/PlayMode/` 未读；**未运行 Unity、未执行 EditMode 测试**，故"测试是否全绿"未验证，本报告只做源码调用链判定。
- 存档实测未做：没有任何真实 `.sav` 被打开验证 `armies` 段是否曾非空。

**grep 口径（本报告所有"命中 N"均按此执行）**

```powershell
# 统计式（排除测试）
Get-ChildItem -Recurse -Filter *.cs -Path Assets |
  Select-String -Pattern '<模式>' -AllMatches |
  Where-Object { $_.Path -notmatch '\\Tests\\' }
# 列点式（含测试，用于确认"只有测试在调"）
Get-ChildItem -Recurse -Filter *.cs -Path Assets | Select-String -Pattern '<模式>'
```

注意：计数含定义行本身，所以"命中 1"等于"只有定义、无调用者"。

## 1. 系统清单

| 系统 | 三态 | 驱动证据（file:line） | 入口证据 | 存档证据 | 测试 | 一句话现状 |
|---|---|---|---|---|---|---|
| 军队实体创建/编制（`CreateArmy` / `Army.unitCounts`） | 空转 | `WorldState/GameWorld.Simulation.cs:1167`（定义）。`grep 'CreateArmy\('` 非测试命中 **1**（即该定义），生产无调用者 | 无 UI；无 AI 意图 | `SaveSystem.cs:394` 捕获、`:472` 还原，但字典恒空 | `ArmyPersistenceTests`、`WorldGenerationSmokeTests` 只用测试代码手造 | 全链第一环就断：`armies` 永远是空字典（`GameWorld.Core.cs:109`） |
| 军队行军（`SetMoveTarget`/`MoveTick`/`Army.DailyTick`） | 空转 | `State/Army.cs:323`（`SetMoveTarget` 唯一入口）、`:286` `MoveTick`、`:123` `Army.DailyTick`。`grep 'SetMoveTarget'` 非测试命中 **1**；`grep 'Army.DailyTick'` 命中 **0** | 无 | `movePath/moveTargetTile/moveProgress` 入档（`ArmySaveData.cs:35-37`） | 无 | `Army.DailyTick` 无任何调用者，行军代码永远不可达 |
| 战斗结算（`CombatManager.ResolveBattle`/`DailyTick`） | 空转 | 已注册：`GameWorld.Simulation.cs:814` → `FaithHost/WarfareReligionSchedule.cs:53` → `Systems/CombatManager.cs:185` 每日跑；但 `:193` 遍历 `armies` 恒空 → 无一场战斗 | 无 | 无 | **零**（全 `Tests/EditMode` 无 `CombatManager`/`ResolveBattle` 命中） | 注册有效但内部恒早退；`ResolveBattle`（`:32`）唯一调用点 `:211` 不可达 |
| 战斗规则细节（地形/士气/组织/补给/追击） | 半跑 | `State/Army.cs:97-121` 战力公式在；`:114-119` 士气/组织/训练/补给乘子；`CombatManager.cs:47-93` 五轮伤亡。**缺**：任何调用者，且 `UnitDef.terrainModifiers` 全表为空（`GameWorld.Core.cs:821`）→ `Army.cs:110` 恒取默认 1.0 | — | — | 无 | 规则写得比链子多，链子不通 |
| 战争状态与终局（`WarState`/`UpdateWarOutcomes`） | 半跑 | `CombatManager.cs:232-267` 每日调用（经 `GameWorld.Simulation.cs:864`）。**缺**：分数只由战斗产生（`CombatManager.cs:218/223`）→ 恒 0；白和条件 `day - war.startDay >= peaceMinYears*365` = 730，而 `currentDay` 上限 365（`GameWorld.Simulation.cs:350-355`）→ **永不成立** | 宣战由 AI 触发（见下行） | `wars` 入档：`SaveSystem.cs:387`/`:447-453` | 无 | 一旦宣战就永久 `isAtWar`，没有任何终局路径 |
| 战争结束处理（`ProcessWarOutcomes`/`ForcePeace`） | 空转 | `GameWorld.Simulation.cs:862` 由 `warOutcomeTick` 每日调；`ForcePeace` 仅 `:879` 调。因上一行"永不结束"，`UpdateWarOutcomes` 永远返回空表 → 整段不可达 | — | — | 无 | `AddWarAchievement`/`AddDefensiveWin`（`GameWorld.Succession.cs:260/276`）随之永不执行 |
| 宣战入口（ActionGate + 命令总线） | 在跑 | `GameWorld.Core.cs:335` 注册 `DeclareWarCommandHandler`；`Simulation/AI/AIIntentExecutor.cs:39` 发命令；`GameWorld.Simulation.cs:1064-1077` 过 `ActionGate`（职权 `:1093`、法理 `ActionGate/DiplomacyLegalityProvider.cs:17`、国库 ≥200 `:1090`）→ `:1041` 建 `WarState` + 发 `WarDeclaredEvent` | AI 目标 `AIController.cs:349`（要求 `treasury>200`、目标 `coreTiles` 更少、`relation<=30`）；**无玩家 UI** | `wars` 入档，但 `DiplomaticRelation.isAtWar` **不入档** | `ActionGateTests` 只测门禁 | 宣战真的会发生（AI 驱动），但打完不了 |
| `DiplomaticRelation` 关系值/信任/威胁 | 半跑 | `Systems/DiplomacyManager.cs:872-900` 读写；`DailyDecay`（`State/DiplomaticRelation.cs:142-147`）由 `DiplomacySchedule.cs:24-28` 每日跑 | 关系值消费者：`AIController.cs:247/292/296/337`（结盟/送礼/选目标）+ 地图着色 `Rendering/MapRenderer.Colors.cs:218-230` | **不入档**（`SaveData.cs` 无外交段） | 无 | `relation` 有真实消费者；`trust` 唯一读取点 `DiplomacyManager.cs:591` 在不可达分支；`threat` 只写不读 |
| 盟约（`Alliance`/`ProposeAlliance`） | 半跑 | `DiplomacyManager.cs:112-168`；AI 意图 `AIController.cs:294` → `AIIntentExecutor.cs:45` → Handler → `DiplomacyManager.cs:112`；每日条件校验 `:992-999` | 地图模式 `Alliance` 仅着色（`UIManager.cs:497`、`MapRenderer.Colors.cs:193`），无操作入口 | 不入档 | 无 | 5 种盟约（非 8）；能被签订、能被每日清退，但读档即丢 |
| 不平等从属（`Subordination` 5 型） | 空转 | `DiplomacyManager.cs:215`（`EstablishSubordination`）`grep` 非测试命中 **1**＝定义，无调用者 → `_subordinations` 恒空；`:1001-1009` 朝贡每日循环也就恒空 | 无 | `RealmData.suzerainId/vassalIds` 入档（`RealmDTO.cs:89/116`），`_subordinations` 不入档 | 无 | 数据模型在，产生器在，没人调用 |
| 和约/议和（`PeaceNegotiationSystem`/`OfferPeace`） | 空转 | `PeaceNegotiationSystem.cs:8` 静态类，`grep 'PeaceNegotiationSystem\.'` 非测试命中 **0**；`DiplomacyManager.OfferPeace`（`:729`）`grep` 命中 **1**＝定义 | 无 | `Treaty` 只在 `:754` 写入 `activeTreaties`（该函数无调用者）→ 条约表恒空 | 无 | 26 种条款类型全部只有定义与死代码生产者 |
| 战争法理（`WarJustificationSystem`） | 空转 | 5 个调用点全在死路径：`DiplomacyManager.cs:403/407`（在 `DeclareWarWithJustification`，后者 `grep` 命中 1＝定义）、`:461/470`（`GetValidCasusBelli/GetSupportedWarGoals`，外部零调用）、`:926`（在 `RaidSettlement`，见下行劫掠） | 无 | `casusBelliList`/`activeWarGoals` 只在死路径写入 | 无 | 15 CB 类型 + 15 战争目标类型无实际消费者 |
| 劫掠（`RaidSettlement` 路径） | 半跑 | `AIController.cs:69-123` 生成 `RaidSettlement` 意图 → `AIIntentExecutor.cs:36` → `DiplomacyManager.cs:902`。**缺**：门槛 `:92` 要求 `IsHostile && !isAtWar`，而 `hostilityLevel` 只有劫掠/边境摩擦能抬高，二者互为先决 → 冷启动不可达 | 无 UI | 不入档 | 无 | 逻辑闭环，起点被自己锁住 |
| 宗教 × 战争（`WarfareReligionSchedule`） | 半跑 | 每日在跑：`GameWorld.Simulation.cs:814-820` → `WarfareReligionSchedule.cs:51-57`（战力 tick → `UpdateFaithFervor` → 大圣战结算 → 战争终局）。真实生效的宗教部分：宣战异教 +25 热忱（`FaithHost/ReligionSimulationEventHandler.cs:19-31`，实例化于 `GameWorld.Core.cs:978`）、圣地失陷 +50（`GameWorld.Simulation.cs:387-408`）、每 360 日 −10（`:379-382`） | 无 | `faiths` 入档（`FaithSaveData.cs:58/128`） | 无 | 战时宗教影响**单向**：战争 → 热忱；热忱 → 战力/士气**没有任何乘子** |
| 大圣战（`GreatHolyWar*`） | 空转 | `GameWorld.Simulation.cs:1110`（`DeclareGreatHolyWar`）生产调用者 **0**（`grep 'DeclareGreatHolyWar'` 9 命中全为 `CanDeclare...` 与大圣战内部）；`:410-434` 的结算循环遍历 `GreatHolyWarSystem.ActiveWars` 恒空 | 无 | 不入档 | 仅 `FaithQueryExtensionTests` 测 `CanDeclare` | 且 `:1129` 先 `Declare` 后过 `ActionGate`（`:1134`），门禁拒绝会留下未绑战争的残状态 |
| 寻路（`PathfindingSystem`） | 半跑 | `Units/UnitPathfinder.cs:48` 构造 `PathfindingParams` 的唯一生产入口，来自 `Actors/MapActor.cs:216`（商队/难民/传令）。**缺**：`IsAtWar` 硬编码 `return false`（`PathfindingSystem.cs:365-368`）、`GetBuildingsAtTile` 恒空（`:370-373`）；`armyRealmId` 恒 −1（`UnitPathfinder.cs:50`）→ 通行管制分支全不生效；`isNavy` 恒 false（`:51`） | 玩家不能指路 | — | **零**（无 Pathfinding 测试） | 给商人用的寻路是活的；给军队用的一半是空壳 |
| 行军管制（`MovementControlSystem`） | 空转 | 唯一实际副作用：宣战时撤销军事通行权（`DiplomacyManager.cs:363/367/431/435/558/562` → `MovementControlSystem.cs:138-141`）。`grep 'GrantMilitaryAccess\('` 非测试命中 **2**，实为 `GrantMilitaryAccess`（`:116`）与 `ShouldGrantMilitaryAccess`（`:216`）两条定义（正则子串），二者均零调用者 | 无 | `militaryAccessGranted` 是 `[NonSerialized]`（`RealmData.cs:56-57`） | 无 | 4 级管制只有"撤权"活着，加权的入口根本不存在 |
| 军事行军（`MilitaryMovementSystem`） | 空转 | 284 行纯函数库，`grep 'MilitaryMovementSystem\.'` 非测试命中 **0** | 无 | — | 无 | 堡垒/关隘/损耗/补给恢复全部无消费者 |
| 海军（`Navy`/`CreateNavy`/`NavyPersistence`） | 空转 | `GameWorld.Simulation.cs:1151`（`CreateNavy`）`grep` 命中 **1**＝定义；`Navy.DailyTick`（`Navy.cs:67`）无生产调用者；`NavyPersistence.CaptureNavies`（`NavySaveData.cs:99`）`grep` 命中 **0** | 无 | `SaveSystem.cs:473` 调 `RestoreNavies(data.navies)`，但 `ToSaveData` 从不给 `data.navies` 赋值（`:359-404` 无该行）→ 读档恒空 | `NavyEntityTests`、`NavyPersistenceTests`（只测函数本身） | SaveVersion 17 只做了"读"的一半：**写侧缺失** |
| 驻军/聚落控制（`SettlementControlSystem`） | 在跑 | `SettlementSimulationSystem.cs:53/60` → `SettlementControlRuntime.cs:37`，由 `SettlementSchedule` 每日跑；驻军量来自 `armies`（`SettlementControlSystem.cs:46-60`） | 无 UI | `SettlementData.garrison/garrisonQuantity` 走 `SettlementPersistence` | `ArmyAndGarrisonSafetyTests`（只测不抛 NRE） | 系统在跑，但"驻军"输入恒为 0（armies 空） |
| 占领（`TileData.occupyingRealmId`） | 不存在 | 唯一写入口 `PoliticalManager.cs:20`（`OccupyTile`）`grep` 命中 **1**＝定义；其余全是置 −1（`GameWorld.Editing.cs:80/108`、`LandAbandonmentSystem.cs:46`）。读取者：渲染条纹（`MapRenderer.Colors.cs:341`）、`RealmSituationBuilder.cs:53`、`RelationProjection.cs:63`、`GameWorld.Simulation.cs:457` | 无 | `occupyingRealmId` 入档（`TileSaveData.cs:21`） | 无 | 有字段、有渲染、无占领行为；`CombatState.Besieging`（`GameEnums.cs:335`）零引用 |
| 亲兵（`RetinueSystem`/`RetinueBand`） | 空转 | `grep 'new RetinueSystem'` 非测试命中 **0**——类从未实例化 | 无 | 不入档 | 无 | 纯数据持有类 |
| 建城（`UnitFounding`） | 空转 | `grep 'UnitFounding\.'` 非测试命中 **0**（唯一命中在 `UnitFoundingTests.cs:51`） | 无 | — | 1 个负面用例 | `TryFoundSettlement` 生产零调用 |
| 游牧政体（`NomadicRealmSystem`） | 半跑 | 全类 148 行，唯一被调用的成员是 `FormFromMobility`（`CultureHost/CultureStageEvolutionHost.cs:48`）。`GetNomadicMilitaryBonus`（`:125`）零调用者 | — | `realmForm` 随 `RealmDTO` 入档 | 无 | 军事加成接口没有任何军事消费者 |
| 军队/地图单位视图（`MapUnitRegistry`） | 半跑 | `RegisterArmy` 生产调用点 2：`GameWorld.Simulation.cs:1182`（创建时）、`SaveSystem.cs:493`（读档后重登记）。**缺**：渲染层零消费——`Rendering/`+`UI/` 对 `MapUnit|UnitRegistry` 的 grep 只有 `MapLayerConfig.cs:22` 的 `ArmyMarkers` 位标志（`showArmyMarkers` 默认 false，`:69`），无绘制代码 | 无 | 视图本身不入档（`SaveSystem.cs:613` 跳过 Army 条目） | `MapUnitTests`、`ArmyPersistenceTests` | 视图派生链写完，但没有渲染器读它；军队即使存在也不会上图 |
| 军费（`MilitaryBuildUpCommand`） | 半跑 | `AIController.cs:321` → `AIIntentExecutor.cs:54` → `MilitaryBuildUpCommandHandler.cs:27-28`：**只扣 `treasury -= Budget`（默认 50），不产出任何单位** | 无 | 不涉及 | 无 | 唯一"军事 AI 动作"的净效果是烧钱 |
| `WarDeclaredEvent`/`WarEndedEvent` | 半跑 | 发布点 `GameWorld.Simulation.cs:1049/876`；唯一订阅者 `SimulationStatisticsRecorder.cs:29/33`，而该类只在测试里被 `new`（`SimulationEventBusTests.cs:14/44`），生产未实例化 | — | — | 2 个事件总线测试 | 战斗事件在生产里没人听 |
| 兵役/人力（`ManpowerSystem` + 法律乘子链） | 空转 | `ManpowerSystem.GetRealmTotalManpower(...)`（`:63/:78`）**零调用者**（`grep` 3 命中＝2 定义 + 1 内部自调）；`LawEffectRuntime.GetManpowerMultiplier`（`:75`）只被这个死方法消费；`RealmData.CalculateManpowerLimit`（`:166`）零调用 | 无 | — | 仅 `LawEffectRuntimeTests` 直接测 `Army.ReinforceUnit` | 法律→人力→补员三段的中间段整段断掉 |

## 2. 数据模型现状

**有生产者也有消费者的字段**

- `DiplomaticRelation.relation`：写 `DiplomacyManager.cs:872-882` 等；读 `AIController.cs:247/292/296/337` + `MapRenderer.Colors.cs:230`（`MapDiplomacyView.relation`）。
- `DiplomaticRelation.hostilityLevel`：写 `:478-531`；读 `IsHostile`（`DiplomaticRelation.cs:67-77`）→ `AIController.cs:92`、`MapWorldViewAdapter.cs:130`。
- `WarState.attackerScore/defenderScore`：写 `WarState.cs:42-52`（仅战斗计分）；读 `CombatManager.cs:243-262`。
- `RealmData.treasury`：宣战门槛读（`GameWorld.Simulation.cs:1090`）、军费扣（`MilitaryBuildUpCommandHandler.cs:27`）。

**"承诺未兑现"字段（有定义、无有效消费者）**

| 字段 | 声明 | 现状 |
|---|---|---|
| `Army.unitCounts` | `Army.cs:30` | 生产写入只有存档还原（`ArmySaveData.cs:105`）与测试；无征兵/编队入口 → 军队永远 0 兵 |
| `Army.transport`(移动乘子) | `Army.cs:25` | `MoveSpeedMultiplier` 唯一调用点 `Army.cs:309` 在不可达的 `MoveTick` 内 |
| `Army.movePath/moveTargetTile/moveProgress` | `Army.cs:73-77` | 写入唯一入口 `SetMoveTarget`（`:323`）零调用；只由存档读写 |
| `Army.supply/morale/organization/training` | `Army.cs:50-56` | 只在战斗/行军里演化，两者皆不可达 → 永远停在初值 100/100/100/50 |
| `UnitDef.recruitCost` | `UnitDef.cs:31` | `grep 'recruitCost\.'` 非测试命中 **0**：只写（`GameWorld.Core.cs:794-799`）不读，征兵成本从未结算 |
| `UnitDef.terrainModifiers` | `UnitDef.cs`（构造于 `Core.cs:821`） | 唯一构造点写**空字典**；`Army.cs:110` 取默认 1.0 → 地形战力修正结构性为空 |
| `UnitDef.requiredInnovations` | `Core.cs:819` | 读点仅 `CombatManager.cs:147-166`（不可达） |
| `DiplomaticRelation.trust` | `DiplomaticRelation.cs:21` | 5 处写、1 处读（`:591`，在不可达的 `SurpriseAttack` 惩罚里）→ 对玩法零影响 |
| `DiplomaticRelation.threat` | `:24` | **只写不读**（写 `:370/418/553/898`） |
| `DiplomaticRelation.eventHistory` | `:57` | 只写（`AddEvent` `:133`），零读取者 → 外交事件日志不上 UI |
| `DiplomaticRelation.activeTreaties` | `:61` | 仅 `OfferPeace`（`:754`）写，该方法零调用 → 恒空 |
| `DiplomaticRelation.casusBelliList/activeWarGoals` | `:53/55` | 写入方 `:425/927` 均在死路径；读取方 `:461/470` 外部零调用 |
| `DiplomaticRelation.subordination` + `_subordinations` | `DiplomaticRelation.cs:63`、`DiplomacyManager.cs:17` | 唯一写入口 `EstablishSubordination` 零调用 |
| `Subordination.tributeRatio/militaryObligation/foreignPolicyControl/successionControl/autonomy` | `Subordination.cs` | 随上一行一起恒不生效（`DiplomacyManager.cs:1001-1009` 朝贡循环恒空） |
| `Navy.shipCounts` | `Navy.cs:27` | 唯一生产入口 `AddShips`（`:51`）零调用 |
| `TileData.occupyingRealmId` | `TileData.cs:18` | 唯一写入口 `OccupyTile` 零调用 → 恒 −1 |
| `RealmData.movementControl/tileMovementControlOverrides` | `RealmData.cs:51-54` | 读取方存在（`MovementControlSystem.cs:29-40`、`PathfindingSystem.cs:215`），但 `SetTileControlOverride` 零调用、`movementControl` 恒为默认 `Loose` |
| `RealmData.militaryAccessGranted` | `:57` | 只有 `Remove` 路径被调用（宣战撤权）；`Add` 侧 `GrantMilitaryAccess` 零调用 → 恒空 |
| `WarRules.scoreCapital/scoreCity/scoreProvince/scoreOccupation*/peaceForceScore/annualDrift` | `WarRules.cs:32-48` | 只有 `scoreBattle/allowWhitePeace/peaceWhiteScore/peaceMinYears/truceYears/allowAllianceIntervention` 被读；其余 8 个计分字段零读取 |

## 3. 内容数据现状

- **没有军事内容 JSON。** `Assets/StreamingAssets/Base/` 全量 44 个文件里不存在 `Units*.json`/`Armies*.json`/`Warfare*.json`/`Naval*.json`（目录清单实测）。对全部 JSON 做军事关键字扫描，命中仅来自非军事域：`Innovation/Innovations.json`（`military_colony` 等革新 id）、`Laws/Laws.json`（`military_*` 法条 id）、`Tradition/Traditions.json`、`Ethos/Ethos.json`（`"warfare"` 特质字段）。
- **单位定义硬编码**：14 个 `UnitDef` 写在 `Simulation/WorldState/GameWorld.Core.cs:763-776`（步兵 100–102、弓弩 110–111、骑兵 200–210、海军 300–303），征兵成本硬编码 `:777-787`。
- **外交枚举即"内容"**（全部定义在 `Assets/Scripts/Content/MyriadCreation.Content.Definitions/`，无数据表）：`AllianceType` **5**（`NonAggressionPact/DefensiveAlliance/OffensiveAlliance/TotalAlliance/Faction`）、`SubordinationType` **5**（`Tributary/Vassal/Associate/Protectorate/Puppet`）、`TreatyClauseType` **26**、`CasusBelliType` **15**（含 `None`）、`WarGoalType` **15**（含 `None`）、`DiplomaticEventType` **19**、`RaidType` **6**、`MovementControlLevel` **4**、`SpecialBondType` **3**。
- **"8 平等盟约 + 8 不平等从属 + 12 条款"在任何 `docs/**` 里都查不到对应表述**（对 `docs` 全量 grep `平等盟约|不平等从属|盟约|从属|条款` 无此条），代码实测值为 5 / 5 / 26。若该数字来自口头/别处规格，则与代码不一致；本报告按代码口径记录。
- 条款的**可达**生产者：`0` 种（`PeaceNegotiationSystem` 与 `OfferPeace` 均零调用）。CB 的可达生产者：`0` 种（`GenerateRaidReprisalCB` 在 `RaidSettlement` 内，而后者冷启动不可达）。
- 关联内容（非本域但被军事消费）：军事法律组在 `Laws/Laws.json`，但 `LawEffects.manpowerRecruitment`（`Laws/LawEffects.cs:28`）只写不读。

## 4. 缺口清单（按影响排序）

| # | 缺口 | 影响 | 证据 | 严重度 |
|---|---|---|---|---|
| 1 | **军队永不创建**：`CreateArmy` 零生产调用者，"征召→编队"整段缺失 | 战斗系统、行军、驻军、战争计分、`MapUnit` 军队视图、`SettlementControlSystem` 驻军输入全部失去数据源 | `GameWorld.Simulation.cs:1167`（grep 非测试命中 1）；`GameWorld.Core.cs:109` | P0 |
| 2 | **战争永不结束**：白和阈值 730 日 > `currentDay` 上限 365；分数只来自不存在的战斗；`OfferPeace` 零调用 | `isAtWar` 一置永真；`truceUntilDay = currentDay+N*365` 同样超出日域 → 相关判断永久成立/永不成立 | `CombatManager.cs:257`、`GameWorld.Simulation.cs:350-355`、`DiplomacyManager.cs:729/757/774` | P0 |
| 3 | **外交状态不入档**，而 `wars` 入档 | 读档后 `_relations` 由 `InitializeAllRelations` 重新 `Random.Range` 生成（`DiplomacyManager.cs:823-844`）→ 战争标记/盟约/停战/朝贡全丢，`wars` 里的战争与关系表互相矛盾；且读档后关系值非确定 | `SaveData.cs:22-77`（无外交字段）、`SaveSystem.cs:387/447`、`WorldState/GameWorld.Core.cs:556`（读档重建） | P0 |
| 4 | 玩家零军事入口 | 无宣战/调兵/议和 UI；军事只能由 AI 跑（而 AI 的"军队建设"只扣钱） | `Assets/UI/Panels/` 恰 6 个且无一军事；`UIManager.cs:475-503` 仅地图着色模式 | P0 |
| 5 | 海军只写了一半存档 | `NavyPersistence.CaptureNavies` 零调用 → `data.navies` 恒 null → SaveVersion 17 的读侧 `RestoreNavies` 恒还原空表；即使造出舰队也会读档蒸发 | `NavySaveData.cs:99`、`SaveSystem.cs:473`、`SaveSystem.cs:359-404` | P1 |
| 6 | 议和/条约全链死：`PeaceNegotiationSystem` 与 `OfferPeace` 零调用 | 26 种条款类型无任何可达生产者；"打—占—和"里的"和"不存在 | `PeaceNegotiationSystem.cs:8`（grep 0）、`DiplomacyManager.cs:729`（grep 1=定义） | P1 |
| 7 | 战争法理死：`WarJustificationSystem` 5 个调用点全在零调用者方法内 | 15 CB + 15 战争目标无消费者；`DeclareWarCore` 走的 `DeclareWar` 完全绕过 CB/惩罚 | `DiplomacyManager.cs:345-386` vs `:388-452`；`DiplomacyManager.cs:403/461/926` | P1 |
| 8 | 占领不存在：`OccupyTile` 零调用、`Besieging` 零引用 | 围城/占领/割让无行为；`occupyingRealmId` 恒 −1，占领条纹渲染永不出现；`notifyWarDefeat` 的占领比例恒 0 | `PoliticalManager.cs:20`（grep 1=定义）、`GameEnums.cs:335`、`GameWorld.Simulation.cs:457` | P1 |
| 9 | 信任/威胁无消费者 | `threat` 只写不读；`trust` 唯一读取点在不可达分支 → 侮辱/禁运/礼物等动作产出的 trust/threat 对 AI 决策与 UI 展示均无效 | `DiplomaticRelation.cs:21/24`、`DiplomacyManager.cs:591` | P1 |
| 10 | 朝贡/从属链断在产生器 | `EstablishSubordination` 零调用；但 `RealmData.suzerainId/vassalIds` 仍入档 → 读档后"数据说有宗主、`GetSubordination` 说没有" | `DiplomacyManager.cs:215/270`、`RealmDTO.cs:89/116` | P1 |
| 11 | 军队行军不可达 + 地图视图无渲染 | `Army.DailyTick`/`SetMoveTarget` 零调用；`MapUnitRegistry` 军队条目无渲染消费者（`ArmyMarkers` 只是位标志） | `Army.cs:123/323`、`MapLayerConfig.cs:22/69` | P1 |
| 12 | 军事通行权单向 | `GrantMilitaryAccess` 零调用；`militaryAccessGranted` 只减不增 → 4 级管制里 `Strict` 永远等价于"禁止通行" | `MovementControlSystem.cs:116/138`、`PathfindingSystem.cs:217-221` | P1 |
| 13 | 劫掠冷启动自锁 | 门槛要求 `hostilityLevel>=50`，而抬高敌对度的 `BorderSkirmish`（`:646`）与 `CheckInvasionTriggerWar`（`:681`）都零调用 → 劫掠与"入侵触发战争"永不发生 | `AIController.cs:92`、`DiplomacyManager.cs:646/681` | P1 |
| 14 | 宗教→战力无乘子 | 热忱只被大圣战门槛（死）与 UI 读取；宣战抬热忱是真的，但热忱不改变任何战斗数值 | `FaithHost/ReligionSimulationEventHandler.cs:28`、`FaithSystem.cs:88-90` | P2 |
| 15 | 战力公式里地形恒 1.0 | `terrainModifiers` 空表 + `GetValueOrDefault(t,1f)` → `TerrainTacticType` 只用于取键，不产生差异 | `GameWorld.Core.cs:821`、`Army.cs:110` | P2 |
| 16 | 补员/人力乘子链断开 | `GetRealmTotalManpower` 零调用、`CalculateManpowerLimit` 零调用、`ReinforceUnit` 仅测试调用 → 法律/法令的人力加成无落点 | `ManpowerSystem.cs:63/78`、`RealmData.cs:166`、`Army.cs:163` | P2 |
| 17 | 日期域溢出类缺陷（同一根因） | `currentDay ∈ [1,365]` 却与"日数累加"比较：除 #2 外，`UpdateFaithFervor` 的 `currentDay - _faithFervorDay < 30` 在跨年后 diff 变负 → `_faithFervorDay` 一旦 >335 就永久锁死不再更新 | `GameWorld.Simulation.cs:350-355`、`:370-376` | P2 |
| 18 | 测试覆盖为零的子系统 | `CombatManager`、`WarState` 计分、`DiplomacyManager`、`PathfindingSystem`、`MovementControlSystem`、`MilitaryMovementSystem`、`PeaceNegotiationSystem`、`RetinueSystem` 均无 EditMode 测试；军队相关测试全部绕开真实调用链（测试里手工 `new Army` + 手填 `unitCounts`） | `grep 'CombatManager\|DiplomacyManager\|WarState\|ResolveBattle'` 在 `Tests/EditMode` 仅命中 `ActionGateTests`/`ArchitectureContractsTests` 的字符串与构造 | P2 |
| 19 | `MilitaryBuildUpCommandHandler` 只扣钱 | 唯一军事 AI 动作的净效果是扣默认 50 金库，无任何单位产出、无回滚 | `MilitaryBuildUpCommandHandler.cs:23-28`、`AIController.cs:321` | P2 |

## 5. 我核实不了的事

1. **测试是否全绿**：本审计只读源码，未运行 Unity/EditMode。所有"无测试"结论来自 `grep` 命中数，而非执行结果。
2. **真实存档内容**：没有打开任何 `.sav` 验证 `armies` 段是否曾出现非空记录，也无法确认历史存档里 `wars` 与关系表是否已经出现矛盾态。
3. **调度器是否会长期饿死军事相关 tick**：`SimulationScheduler.TickBudgeted`（`:115-166`）在预算不足时把剩余条目留在 `_pending` 顺延；我无法实测 `warfare-religion.daily`(order 24)/`diplomacy.daily`(23)/`ai.daily`(28) 在真实帧预算下是否稳定执行。
4. **AI 目标分布**：`ExpandTerritory` 被选中的实际频率取决于 `AIDecisionSystem` 权重与性格随机，我未跑长周期观测，因此"宣战真的会频繁发生"只是静态可达性判断。
5. **`ReligionSimulationEventHandler` 是否重复订阅**：`InitializeFaithSystems`（`GameWorld.Core.cs:956-979`）每次调用都 `Dispose` 后再 `new`，但我未验证该方法在读档/重建路径上被调用的次数。
6. **`RetinueLevel`/`RetinueBand` 的字段语义**（等级门槛、编制）只读了声明，未读其全部字段与既有设计意图。
7. **`docs/**` 中"M7 战争与外交"里程碑的验收口径**未逐条比对：`docs/plan-v3/SYSTEM_MASTER_PLAN.md:135` 已自述"宣战/调兵/议和无入口"，与本次代码核查一致，但里程碑清单本身未逐项验收。
