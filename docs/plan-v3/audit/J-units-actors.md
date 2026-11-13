# J · 单位与行为体（Units / Actors）审计（2026-11-09）

## 0. 范围与路径缩写（路径均相对 `MyriadCreation/`）

- `U/` = `Assets/Scripts/Simulation/MyriadCreation.Simulation.Units/`（30 个 .cs，2257 行）；`A/` = `.../MyriadCreation.Simulation.Actors/`（17 个 .cs，1765 行）。
- `S/` = `...Simulation.Systems/`，`W/` = `...Simulation.WorldState/`，`St/` = `...Simulation.State/`，`Svg/` = `Assets/Scripts/Infrastructure/MyriadCreation.Infrastructure.Save/`，`R/` = `Assets/Scripts/Rendering/`。
- 精读：`A/MapActor.cs`(401)、`A/MapActorManager.cs`(363)、`A/UnitAIFactory.cs`(90)、`A/CampManager.cs`(302)、`A/CampData.cs`(126)、`U/TransportUnit.cs`(689)、`U/TreasuryLedger.cs`(341)、`U/UnitProvisioning.cs`(129)、`U/MapUnit.cs`(153)、`U/MapUnitRegistry.cs`(142)、`U/TransportCatalog.cs`(150)、`U/UnitProvisioningCost.cs`(44)、`U/TerrainConditions.cs`(1-60)、`U/UnitPathfinder.cs`(71)；外域 `S/SettlementSchedule.cs`、`S/SettlementSimulationSystem.cs`、`W/GameWorld.Simulation.cs:490-843/1140-1225`、`W/GameWorld.Core.cs:745-850`、`W/GameWorld.Generation.cs:443-495`、`St/Army.cs`、`Svg/SaveSystem.cs:500-640`、`Svg/WorldEntitySaveData.cs:180-332`、`Svg/SaveEntityBridge.cs:94-124`、`Svg/ArmyPersistence.cs`。
- **未覆盖**：`StreamingAssets/Base/` 下无 `Unit*`/`Army*` 目录（`Get-ChildItem Assets/StreamingAssets -Recurse -Directory` 实得 Base/_generated、Biome、CharacterTemplate、Crafting、Culture、Dna、Equipment、EthnicGroup、Ethos、FamilyTradition、Goods、Innovation、Language、Laws、Localization、MentalHealth、Properties、Race、Religion、Title、Tradition、Mods/Dna）→ 本域**无内容数据层**可审。`U/` 未逐行：UnitTypeRules(34)、UnitCategoryRules(45)、UnitSubcategoryRules(27)、UnitInteraction(14)、PackAnimalGoods(40)、TreasuryVault(122，仅读 12/128 附近)、TreasuryGoodsStack、MemberSlot、CargoEntry、CartProfile、UnitFunding、UnitActivity、UnitCategory、UnitSubcategory、UnitSourceKind、UnitRole、UnitFundingSource、TransportUnitType、CartKind、PackAnimalKind —— 这些只做 grep 计数判定（见 §5–§6）。
- 行数口径：`TransportUnit.cs` 用 `read` 计 689 行、`Measure-Object -Line` 计 638 行（空行差异），下文行号以 `read` 为准。

## 1. 系统清单

| 系统 | 三态 | 驱动证据（file:line） | 入口证据 | 存档证据 | 测试 | 一句话现状 |
|---|---|---|---|---|---|---|
| 行为体日 tick | 在跑 | `S/SettlementSchedule.cs:31`（`settlement.map-actors.daily`，Daily，序 17）→ `S/SettlementSimulationSystem.cs:63-66` → `A/MapActorManager.cs:97-114`；调度器由 `W/GameWorld.Simulation.cs:768` `TickBudgeted` 驱动 | 仅"传令官"一条玩家链：`Assets/UI/Panels/GovernmentPanel.cs:385` | `Svg/WorldEntitySaveData.cs:206-273`、`Svg/SaveSystem.cs:528-530/598-604` | 无 tick 级测试（Tests 内 `MapActors` 5 命中全是读写档/视图断言） | 在跑，但玩家看不见，只由随机刷新与政令产生 |
| 营寨 CampManager/CampData | 在跑 | `S/SettlementSchedule.cs:32`（序 18）→ `S/SettlementSimulationSystem.cs:68-71` → `A/CampManager.cs:241-252` → `A/CampData.cs:92-124` | 无玩家入口；`S/SandboxSpawnSystem.cs:107` 月度自动建 | `Svg/GameSaveSystem.cs:231-232/529`、`Svg/SettlementPersistence.cs:247` | `SettlementPersistenceTests.cs:198-228` | "建→补给耗尽→废弃"闭环，无补给来源 |
| 传令官链（政令实体化） | 在跑 | `W/GameWorld.Simulation.cs:840-843`（`decrees.propagation.daily`，序 37）→ `:705-762` | `GovernmentPanel.cs:385` → `S/DecreeCommandAdapter.cs:18-25` → `W/GameWorld.Simulation.cs:536-554` | `St/RealmDTO.cs:22`（`decreeState`）+ `...Laws/DecreeOrder.cs:5`（`[Serializable]`，含 `courierActorId`） | `DecreePropagationTests`、`UiCommandContractsTests.cs:100-145` | 本域唯一"钱→货→人→移动→回执"贯通的功能 |
| UnitProvisioning + TreasuryLedger | 在跑 | 唯一业务调用 `W/GameWorld.Simulation.cs:672/674/686` | 无独立入口 | `UnitFunding` 随 transport 入档（`Svg/WorldEntitySaveData.cs:204`） | `ProvisioningLedgerTests`(19)、`UnitProvisioningTests`(8) | 唯一真实财政出口，只服务传令官 |
| TransportUnit | 半跑 | 移速 `U/TransportUnit.cs:551-602` ← `A/MapActor.cs:116`、`St/Army.cs:309`；吃粮 `:122-152` ← `A/MapActor.cs:145` | 无 | `TransportUnitSaveData`（`Svg/WorldEntitySaveData.cs` 5 命中） | `TransportUnitTests`(22) | 运力/角色/分遣/建寨模型基本只在测试生效 |
| TransportCatalog + TerrainConditions | 在跑 | 读者只有 `U/TransportUnit.cs:351/361/372/558/582/588` | — | 不入档（静态码表） | 无直接测试 | 9 畜种 + 3 车硬编码，只影响移速/运力 |
| UnitPathfinder | 在跑 | `A/MapActor.cs:216`（`EnsureMovePath`） | — | 不入档 | 无直接测试 | 复用 `PathfindingSystem`，参数写死（`U/UnitPathfinder.cs:48-56`） |
| MapUnit / MapUnitRegistry | 半跑 | 写 `A/MapActorManager.cs:275`、`U/MapUnitRegistry.cs:78-98`；读**只有** `Svg/SaveSystem.cs:488/532` | **无渲染、无 UI** | `Svg/SaveSystem.cs:606-622` | `MapUnitTests`(34) | 唯一消费者是存档 |
| UnitDef 兵种表 | 半跑 | 硬编码 `W/GameWorld.Core.cs:761-776`（14 条） | 无征募入口 | 不入档（`Svg/SaveSystem.cs:417` 注明保留默认集） | 间接（Combat 测试） | `terrainModifiers` 恒空、`recruitCost` 零读者 |
| Army.DailyTick | 空转 | 定义 `St/Army.cs:123-157`；全仓 `Grep "\.DailyTick\("` 无一条指向它 | — | `Svg/ArmyPersistence.cs` | `ArmyAndGarrisonSafetyTests`（不调 DailyTick） | 军队补给/行军/训练永不结算 |
| CreateArmy / CreateNavy | 空转 | `W/GameWorld.Simulation.cs:1167`（调用者仅 `WorldGenerationSmokeTests.cs:97/141`、`SaveSystemRoundTripTests.cs:110`）；`CreateNavy :1151` **零调用（含测试）** | — | `Svg/ArmyPersistence.cs:21-59` | 同上 | 生产环境 `world.armies` 恒空（`new Army(` 只在 Tests 出现） |
| UnitFounding（队伍建寨） | 空转 | `U/TransportUnit.cs:638` `AdvanceFounding` 0 调用；`S/UnitFounding.cs:17` 仅 `UnitFoundingTests.cs:51` | — | — | `UnitFoundingTests`(5) | 队伍侧建聚落未实现 |
| UnitCategoryRules / UnitSubcategoryRules / UnitInteraction | 空转 | `Grep UnitCategoryRules`=1（仅自身）、`UnitSubcategoryRules`=1（仅自身）；`UnitInteraction` 15 命中全在 `UnitSubcategoryRules` 内 | — | — | 无 | 死规则表，连带 `UnitInteraction` 一起死 |
| 行为体自然刷新 | 在跑 | `A/MapActorManager.cs:116-142`（概率 0.1/0.05/0.2/0.04/0.04，`MaxActors=500` 于 `:30`，判定 `:99-102`） | 无（`W/GameWorld.Core.cs:915-916` 从 SetupOptions 接线） | 随 actor 入档 | 无 | 每 tick 掷一次骰，上限 500 |

## 2. Q1 行为体生命周期

**AI 数量纠正**：任务书称"12 种"，代码是 **9 个 `IMapActorAI` 实现**（`A/RefugeeAI.cs:7`、`NomadAI.cs:8`、`CaravanAI.cs:6`、`PredatorAI.cs:7`、`SwarmAI.cs:7`、`PilgrimAI.cs:6`、`BanditAI.cs:8`、`MercenaryAI.cs:7`、`CourierAI.cs:5`）+ 接口 `A/IMapActorAI.cs:5`（`Think`/`Act` 两段式，在 `A/MapActor.cs:140-141` 调用）+ 工厂 `A/UnitAIFactory.cs` + 枚举 `A/MapActorType.cs:3-14`（9 值，缺 5）。若"12"指 `St/CharacterAICategory.cs:5-16`（None+11），其映射在 `A/UnitAIFactory.cs:51-88`。

| AI | 谁 new（生产路径） | Think | Act | 可达 |
|---|---|---|---|---|
| RefugeeAI | `A/MapActorManager.cs:154`、`S/LandAbandonmentSystem.cs:147`、`S/SettlementDestructionSystem.cs:259` | 30 天冷却，41×41 内按 `order*系数+dev*系数-距离` 择优（`:29-60`） | 到达且 `order>50 && dev>0.3` → 人口并入地块（`:72-76`） | 是 |
| NomadAI | `A/MapActorManager.cs:171`，同时是工厂 default | 15-45 天冷却，随机 5-15 格找草原/肥力>30（`:22-43`） | 站 `fertility>40` → 补给+5、血+1（`:48-52`） | 是 |
| CaravanAI(origin,dest) | `A/MapActorManager.cs:200`、`A/UnitAIFactory.cs:70` | 往返两点（`:29-33`） | 到终点等 3-10 天 +greed 修正并 +50 补给；回起点若无领队 → `population=0`（`:45-56`） | 是 |
| PredatorAI | `A/MapActorManager.cs:222` | 20-50 天冷却，11×11 内找 `dev>0.2 && order<50`（`:21-44`） | 吃 `min(地块人口/10, 自己人口)`，`dev-0.02`（`:79-88`） | 是 |
| SwarmAI(寿命 60-180) | `A/MapActorManager.cs:232` | 寿命尽 → `population=0`；否则 5-15 天扩散到 `fertility>30`（`:22-55`） | 每 tick `fertility-2`、`dev-0.005`（`:62-64`） | 是 |
| BanditAI | `A/MapActorManager.cs:249`、`S/LandAbandonmentSystem.cs:189` | 先追 8 格内非敌对商队；否则 13×13 内找 `dev>0.15` 且 `order<40+boldness*0.2`，再不行游荡（`:20-89`） | 掳人（`min(地块/8, 自己/2)`，一半入自己）、`order-5`、+40 补给；`pop>50 && 无主 && order<30` → `attributes["hasCamp"]=1`（`:96-112`） | 是 |
| CourierAI(target) | `W/GameWorld.Simulation.cs:680`、`A/UnitAIFactory.cs:23/64/84` | 目标变则 `SetTarget`（`:16-19`） | 空（`:22-24`） | 是（政令） |
| MercenaryAI | 只 `A/UnitAIFactory.cs:74`（Soldier 领队） | 31×31 内找 dev 最高且有主地块（`:29-54`） | `dev>0.3` → 补给+2（`:59-62`） | **否**：`SetLeader` 唯一生产调用 `W/GameWorld.Simulation.cs:697`，该角色 `:695` 刚被设成 `Courier` → `CreateForLeader` 的 Merchant/Migrant/Soldier/Raider/Ruler/Official/Courtier 分支全不可达 |
| PilgrimAI(shrine) | **零处**（`Grep PilgrimAI`=3 全在自身；`MapActorType.Pilgrim` 只在 `U/MapUnit.cs:120/139` 映射出现） | 到圣地停留 10-30 天后 `population=0`（`:19-33`） | 记录到达（`:37-42`） | 否 |

**谁创建行为体**：外部只走 `A/MapActorManager.cs:279-284` `SpawnActor`，生产调用 5 处 —— `W/GameWorld.Generation.cs:484`、`W/GameWorld.Simulation.cs:680`、`S/LandAbandonmentSystem.cs:147/189`、`S/SettlementDestructionSystem.cs:259`。私有 `CreateActor`（`:257-277`）只被本类 5 个 `TrySpawnXxx`（`:152/169/198/220/230/247`）与 `SpawnActor` 调用，并负责 `MapUnitRegistry.RegisterActor`（`:275`）。

**开局四类 actor 全是游牧脑**：`W/GameWorld.Generation.cs:484` 调 `SpawnActor(type, tile, pop)` 不传 ai、不传 subcategory（默认 `UnitSubcategory.None`）→ `A/MapActorManager.cs:282` 落到 `UnitAIFactory.Create(None, -1)` → `A/UnitAIFactory.cs:37-38` default = `NomadAI`（`:477-483` 生成的 Refugee/Wildlife/Bandit 行为与标签不符）。默认 `Standard` 预设 `generateInitialUnits=false`（`World/MyriadCreation.World.Generation/WorldSetupOptions.cs:93-96`），仅 `Minimal` 生成 2 个（`:83-86`）。

**tick 落点**：唯一注册点 `settlement.map-actors.daily`（`S/SettlementSchedule.cs:16/31`，Daily，序 17；camps 18、evolution 19）→ `S/SettlementSimulationSystem.cs:63-66` → `A/MapActorManager.cs:97-114`（先尝试刷新；逆序 `Tick`；死则列表/字典/`MapUnitRegistry` 三处移除 `:107-112`）。`A/MapActor.cs:107-179` 内部顺序：领队解析与 AI 重绑 `:109-110` → 移动 `:111-139` → `Think:140` → `Act:141` → `supplies-=0.5*days:142` → 运输队吃粮 `:143-151` → 断粮扣血 `:152-156` → 血<30 减员 `:157-168` → 随队角色 `:174`（`TickNamedMembers:369-399`）→ 死亡 `:175-178`。

## 3. Q2 运输与货物（真实职责 + 调用链）

- **TransportUnit**（689 行）：队伍容器（成员/驮畜/车/船/货物/资助 `:16-41`）+ 派生量（`DailyFoodDemand:43`、`BaseCapacity:45`、`FoundingDaysRequired:47`）+ 能力面：货物增删 `:62-120`、吃粮 `:122-152`、分遣 `:184-245`、成员角色 `:247-303`、运力 `:346-422`、管理负担 `:501-527`、移速 `:551-602`、建档/建寨 `:638-687`。**运行期写入点只有两处**：`A/MapActorManager.cs:267-271`（空壳）、`U/UnitProvisioning.cs:110-117`（`Equip` 发货）；读取点 `A/MapActor.cs:116/145`、`St/Army.cs:309`、`S/UnitFounding.cs`。零调用方法（`Grep` 计数=1，只在定义处）：`DetachSubunit`、`DefaultRoles`、`FoodDays`、`AdvanceFounding`、`TryConvertCargoToAnimal`、`AssignRole`、`HasRole`、`RemoveCargo`。→ **除传令官外所有 actor 的 `cargo`/`members` 恒空**。
- **TransportCatalog**（150 行）：纯码表，9 畜种 `:7-120` + 3 车 `:122-148`，读者只有 `TransportUnit`，无内容文件、不入档。
- **TreasuryLedger**（341 行）：**唯一真正扣钱扣粮处**。选币/选粮 `:30-88`；legacy `realm.treasury` 单向迁入 `CurrencySystem.vault`（`:90-119`，靠 `U/TreasuryVault.cs:12` 的 `legacyCurrencyMigrated`）；`TryPay:134-221` 分派（个人/家户未接入 → 降级国库再降级区域库并回写 `funding.source` `:170-193`；城/省库 → 区域库再国库 `:194-217`）；扣款落地 `SpendRegional:262-280`（`TradeCenter.inventory`）与 `TryPayNational:282-339`（`TreasuryVault.TrySpend` 或 `realm.treasury-=`，粮草失败回滚 `:316-331`）。消费者：`U/UnitProvisioning.cs:75/79/81`、`S/EconomyManager.cs:537`（`NationalVault`）、`W/GameWorld.Core.cs:832`（选币播种区域库）。
- **UnitProvisioning**（129 行）：`ResolveFundingSource:12-46` 按法律组合（`localSuccession.primary` ∈{1,4}=城库、=3 且无国税=省库、`nationalTax|militaryOrganization` 已选=国库、`localTax`=省库、否则个人）决定付款方；`TryFund:64-82` 补齐缺省物产后转 `TreasuryLedger.TryPay`；`Equip:84-127` 以 `funding` 为唯一事实源发货（`:90-95` 注释：不能反向覆盖 cost，否则"钱扣了、货没发"），并发 1 马/若干手车。
- **"传令官"链在做什么**（`W/GameWorld.Simulation.cs:648-703`）：把"政令送达"实体化为一个会饿、会被劫、会失踪的 actor。逐条 `DecreeOrder`：①查目标聚落取 `targetTile`（`:656-662`）②算两聚落欧氏距离 `:668` ③`UnitProvisioningCost.ForCourier` 折口粮/钱（`U/UnitProvisioningCost.cs:28-42`：2 口/天 ×（距离+3 天）×1.15；钱 4+d·0.04；1 马）④`TryFund`（`:674`，失败即 `Failed`+原因）⑤`SpawnActor(Courier, 起点格, 人口=1, new CourierAI(目标格))`（`:680`）⑥写 `ownerRealmId/category/subcategory/名字/transport.subcategory` 并 `Equip`（`:681-686`）⑦`FindCourierLeader` → `TryAddMember(Courier)` → `SetLeader` → `SetTargetSettlement` → `DecreePropagationSystem.AttachCourier`（`:687-701`）。回执在 `TickDecreeOrders:705-762`：到达判定 `mapActor.currentSettlementAnchorId == targetAnchorId`（`:737`）→ `Delivered` → 按官员阴谋值/正统性/动乱 `DecreePropagationSystem.Resolve:752` → 未下发过则给下级聚落再发一批并**再生成传令官**（`:753-759`）；actor 消失 → `Failed`"传令官失联"（`:721-726`）。
- **链上漏水（P1）**：`TryFund`（`:674`）早于 `FindCourierLeader`（`:687`）；无可用领队时只 `RemoveActor`+标 Failed（`:688-694`），已扣的钱粮与已装船货物不回收。
- 入口唯一：`IssueDecreeOrders` ← UI（`GovernmentPanel.cs:385`，经 `S/DecreeCommandAdapter.cs:18-25`）。玩家不下诏 → 该链 0 实例。

## 4. Q3 单位定义

- `UnitDef` 是 `struct`（`Content/MyriadCreation.Content.Definitions/UnitDef.cs:8-39`，13 字段）。**来源纯硬编码**：`W/GameWorld.Core.cs:761-776` 的 14 条 `AddUnitDef`（100/101/102/110/111/200/201/202/203/210/300/301/302/303），无 JSON/StreamingAssets 管线，`Svg/SaveSystem.cs:417` 注明"unitDefs 不入档，保留默认集"。
- `terrainModifiers`：写方 `W/GameWorld.Core.cs:821` **恒空字典**；唯一读者 `St/Army.cs:110` `GetValueOrDefault(tactic, 1f)` → 空字典恒 1f → **零效果**（读者所在 `CalculateCombatPower` 由 `S/CombatManager.cs:37-38` 调用）。
- `recruitCost`：写方 `W/GameWorld.Core.cs:777-787`（11 条）+ `:820`；`Grep recruitCost` 17 命中全在定义/写入端 → **零读者**（无征募流程）。
- 有读者的字段：`speed`→`St/Army.cs:297`；`supplyConsumption`→`St/Army.cs:134`；`manpowerCost`→`St/Army.cs:179-191`；`requiredInnovations`→`S/CombatManager.cs:149-160`；战斗力四项→`St/Army.cs:97-120`。即**除 `requiredInnovations` 外全部挂在 Army 上，而 Army 无 tick、无生产实例**。
- `MapUnit`/`MapUnitRegistry` 语义：`MapUnit` 是**快照视图**（`U/MapUnit.cs:56-80` / `:82-104`），但 `transport` 存的是**同一引用**（`:75-78`、`:99-102`），不是深拷贝，且视图无回写路径。`MapUnitRegistry` 为静态全局（`U/MapUnitRegistry.cs:11-13`），`Register*` 按 `source+sourceId` 去重（`:85/96/100-116`）。**消费者只有存档**：`Svg/SaveSystem.cs:488/532`（写盘）、`:606-622`（读盘重建并跳过 Army 条目）、`:218/292`（事务回滚）。**渲染与 UI 零引用**：`R/` 12 个文件 `Grep "actor|unit"` 无命中（`R/MapRenderer.Nomadic.cs:25` 画的是游牧政权范围，读 `GetNomadicRealms`，非行为体）；`Assets/UI/Panels` 仅 6 个面板（Government/MainMenu/SaveLoad/Settings/SettlementDetail/Society），无任何单位面板 → **行为体在客户端不可见**。

## 5. Q4 行为体 ↔ 角色/聚落/经济 的跨域接口

| 接口/字段 | 写方 | 读方 | 可观察后果 |
|---|---|---|---|
| `actor.population` ↔ `tile.populationBlocks` | `A/BanditAI.cs:98-103`、`A/PredatorAI.cs:81-84`、`A/RefugeeAI.cs:74`、`...CultureHost/CultureStageEvolutionHost.cs:71`、`S/LandAbandonmentSystem.cs:98-171` | `A/MapActorManager.cs:295` 等 8 处（`W/GameWorld.Queries.cs:163`、`W/RealmQueryAdapter.cs:140`、`W/TileInfoQueryAdapter.cs:50`） | 土匪/野兽真减聚落人口，流民真并入 —— 本域唯一双向实质耦合 |
| `leaderCharacterId`/`leaderAICategory` ← `CharacterData.aiCategory` | `A/MapActor.cs:276/306-310`（每 tick 同步）、`S/CharacterManager.cs:798/989`、`W/GameWorld.Simulation.cs:195/337/695` | `SyncLeaderAI:304-326` 决定重绑（`:319-325` 调 `CreateForActor`；Courier 换目标也会重绑 `:316`） | 角色 AI 分类直接换行为体 AI 实现 |
| 领队属性 → 行军 | — | `LeaderMarchMultiplier:328-345`：`(martial+…)/200 → 1+0.25x`，再乘 `health/100` 的 0.75-1 系数 | 乘进 `A/MapActor.cs:117`，改变每日移动进度 |
| 领队性格 → AI 决策 | — | `A/BanditAI.cs:37`(boldness)、`A/RefugeeAI.cs:26`(compassion)、`A/CaravanAI.cs:40`(greed) | 同一 AI 因领队不同而目标/停留不同 |
| 队伍成员 ↔ `CharacterData` | `A/MapActor.cs:347-367`（写 `currentTile`/`currentSettlementAnchorId`） | 角色域/UI 按该字段定位 | 角色地图坐标被队伍每格覆盖 |
| 随队伤病 | `A/MapActor.cs:369-399`（血<30 扣血，`Die(...,"随队伤病") :394`；死/失踪即 `RemoveMember`） | 角色死亡流程 | 角色可在队伍里被慢慢耗死 |
| `actor.ownerRealmId` | `W/GameWorld.Simulation.cs:681`；`CreateActor` 默认 -1 | `S/RelationProjection.cs:110-125`（Realm→Unit `Employment`）、`S/RelationEndpointValidator.cs:68-70` | 只有传令官与政权建立关系投影 |
| 财政 | `U/TreasuryLedger.cs:262-280/282-339`（扣 `TradeCenter.inventory`、`TreasuryVault`、`realm.treasury`） | 经济域读同一库 | 传令官直接消耗国库/区域库实物 |
| 地块被改写 | `A/BanditAI.cs:102`(order-5)、`A/PredatorAI.cs:83`(dev-0.02)、`A/SwarmAI.cs:62-64`(fertility-2/dev-0.005) | 地块渲染、承载、聚落演化 | 蝗群/鼠患/土匪让地块数值可见退化 |
| 营寨 ↔ 地块/锚点 | `A/CampManager.cs:127-141`（`anchors[id]`、`tile.campId`）、`:254-268` 清理 | `W/GameWorld.Core.cs:255`（`ActiveHabitationSites`）、`W/GameWorld.Queries.cs:52-54`（`TryGetActiveSiteByAnchor`） | 营寨作为 `HabitationSite` 参与查询；锚点 `x=y=0.5` 固定（`:132-133`） |
| `actor.factionId` | 无生产写入（默认 -1） | **零读者**（`W/FactionQueryAdapter.cs:51/64` 读的是 `faction.factionId`） | 无 |
| `actor.morale` | `A/MapActorManager.cs:155`、`S/LandAbandonmentSystem.cs:149`、`S/SettlementDestructionSystem.cs:261`、`A/MapActor.cs:149/166` | **零读者** | 无（士气不参与任何判定） |
| `actor.attributes` | `A/BanditAI.cs:110`（`hasCamp`） | **零读者**（仅存档搬运 `Svg/WorldEntitySaveData.cs:234-235`） | 无 |

## 6. Q5 停摆点

**有类/有方法无实例**
1. `A/PilgrimAI.cs:6`：`new PilgrimAI` 零处；`MapActorType.Pilgrim`（`A/MapActorType.cs:10`）不在任何生成路径。
2. `A/MercenaryAI.cs:7`：唯一构造 `A/UnitAIFactory.cs:74` 需 Soldier 领队，而 `SetLeader` 唯一生产调用传 `Courier`（`W/GameWorld.Simulation.cs:695-697`）。
3. `S/UnitFounding.cs:17` + `U/TransportUnit.cs:638` `AdvanceFounding`：队伍建聚落整链 0 运行期调用（仅 `UnitFoundingTests.cs:51`）。
4. `U/UnitCategoryRules`(45 行)、`U/UnitSubcategoryRules`(27 行)、`U/UnitInteraction`(14 行)：外部引用 0；`UnitInteraction` 的 15 处引用全在被判死的 `UnitSubcategoryRules` 内。
5. `U/UnitActivity`(13 行)：只有 `U/TransportUnit.cs:19` 的字段默认值，无写无读。
6. `U/TransportUnit.cs` 8 个零调用方法（§3 清单）；`A/CampManager.cs:210` `DestroyCamp`、`:79` `GetCampsOfRealm`、`A/MapActorManager.cs:65` `GetActorsAtTile`、`:45` `CountByType`。
7. `Navy`：`CreateNavy`（`W/GameWorld.Simulation.cs:1151`）零调用（含测试）。

**有实例无 tick**
1. `St/Army.cs:123-157` `DailyTick`、`:323-329` `SetMoveTarget` 全仓零调用 → 补给不减、`Marching` 不动；`CreateArmy` 仅测试调用，`new Army(` 只出现在 Tests → **生产环境 `world.armies` 恒空**，`S/SettlementControlSystem.cs:56` 驻军汇总恒 0。
2. `U/MapUnitRegistry.cs:78-87` `RegisterArmy` 只在 `W/GameWorld.Simulation.cs:1182`（零生产调用）与读档 `Svg/ArmyPersistence.cs:62-67` 触发 → 生产期写入 0 条。
3. 非传令官 `MapActor.transport`：`A/MapActorManager.cs:267-271` 建出实例但 `cargo`/`members` 恒空 → 运力/粮耗/移速全走默认分支。
4. `BanditAI` 的 `attributes["hasCamp"]`（`A/BanditAI.cs:110`）：写后无读者、无后续行为。

**有 tick 无数据源**
1. `A/MapActor.cs:143-151` 的"吃粮/缺粮扣血"：自然生成 actor 的 `DailyFoodDemand=0`（`U/TransportUnit.cs:43`）→ 分支永假；寿命实际由常量 `supplies-=0.5/天`（`:142`）决定（100→200 天），与地块产能/掠夺无关。
2. `St/Army.cs:110` 的 `terrainModifiers`：数据源 `W/GameWorld.Core.cs:821` 恒空 → 恒 1f。
3. `A/CampData.cs:92-124` 的补给：建营给 100（`A/CampManager.cs:189`）、演化时用财富覆盖（`S/SettlementEvolutionSystem.cs:199`），**无任何再补给来源**；按 `population*0.1/天` 消耗，80 人营约 12 天、20 人营约 50 天即断粮 → 士气崩 → 自动废弃。
4. `decrees.propagation.daily`（`W/GameWorld.Simulation.cs:840`）：`orders` 仅 UI 产出（`GovernmentPanel.cs:385`），无 AI 自动下诏 → 不操作即空转。
5. `BanditAI` 的劫掠对象：找 `MapActorType.Caravan`（`A/BanditAI.cs:22-33`），但该 actor 无货物（§3）→ 劫掠只剩 `+40 补给 / order-5`，经济后果≈0。
6. `actor.morale`/`attributes`/`factionId`：只写不读（§5）。

## 7. Q6 Army（State）与 MapActor（Actors）是两套还是同一套

**两套，并行且互不引用。** `St/Army.cs:14` 与 `A/MapActor.cs:13` 无继承、无字段互指；数据容器分离（`world.armies : Dictionary<int,Army>` vs `A/MapActorManager.cs:14-16` 的 `List<MapActor>`）。共享仅三样：类型 `TransportUnit`（`St/Army.cs:25` / `A/MapActor.cs:71`）、移速乘子 + `TerrainConditions`（`St/Army.cs:309` / `A/MapActor.cs:116`）、`MapUnit` 视图（以 `UnitSourceKind.Army|MapActor` 区分来源，`U/MapUnit.cs:82-104/56-80`）。**无任何交叉逻辑**：`S/CombatManager.cs` 不引用 `MapActor`；AI 不引用 `Army`；土匪/野兽不会与军队交战，军队也不携带行为体。

`MapUnitRegistry.RegisterArmy` 之后**谁读过它**：只有存档 —— `Svg/SaveSystem.cs:488`（遍历 `All` 取最大 unitId）、`:532`（`MapUnitSaveData.FromUnit` 写盘）、`:606-622`（读盘重建，`:613` 显式跳过 Army 条目避免指向不存在的 army），以及 `Svg/ArmyPersistence.cs:62-67` 在读档后重建视图。渲染/UI/gameplay 消费者为 0；且 Army 恒空（§6），这个"统一视图"实际上只承载行为体。

**第三套要注意**：真正在跑的商队是 `St/Caravan.cs` + `S/EconomyManager.cs`（发车 `:199-208`、`MoveCaravans:229-258`、卡货退役 `:264-283`，由 `S/EconomySchedule.cs:11` 注册），与 `A/CaravanAI` / `MapActorType.Caravan` **完全无关**（后者只漫游 + 补给自己）。

## 8. 缺口清单（按影响排序）

| # | 缺口 | 影响 | 证据 | 严重度 |
|---|---|---|---|---|
| 1 | 行为体在客户端完全不可见（无渲染、无 UI、无查询供 UI） | 劫掠、蝗灾、流民并入、传令官行走全在后台发生，玩家看不到也干预不了；`MaxActors=500` 的规模纯属空耗 | `R/**` 对 actor/unit 零命中；UI 仅 6 面板；`MapActors` 的 21 处引用无一处指向 UI/渲染 | P0 |
| 2 | 军队整链无生产入口（`CreateArmy`/`Army.DailyTick`/`SetMoveTarget` 零生产调用） | `world.armies` 恒空 → 战斗/驻军/行军无数据；`UnitDef` 的 14 条兵种表只被测试与死代码读 | `W/GameWorld.Simulation.cs:1167/1151`、`St/Army.cs:123/323`、`Grep new Army(` 仅 Tests | P0 |
| 3 | 传令官付款早于领队校验且失败不回滚 | 每次"没有可用传令官"都白扣国库/区域库钱粮并吞掉一队货物 | `W/GameWorld.Simulation.cs:674` vs `:687-694` | P1 |
| 4 | 开局 actor 不传 subcategory/ai，全部退化为 `NomadAI` | 开局"土匪/野兽/流民"标签与行为不符（不劫掠、不吃人口、不并入） | `W/GameWorld.Generation.cs:484` + `A/MapActorManager.cs:282` + `A/UnitAIFactory.cs:37-38` | P1 |
| 5 | 营寨无补给来源，必然自我废弃 | `settlement.camps.daily` 在跑，但营寨是"建了必死"的消耗品；`CanEvolveToSettlement`（`A/CampData.cs:74`，需 `permanence>=100 && pop>=50`）几乎不可达 | `A/CampManager.cs:189`、`A/CampData.cs:98-124` | P1 |
| 6 | `UnitDef.terrainModifiers` 恒空、`recruitCost` 零读者 | 兵种"地形战术修正""征募成本"两条设计承诺未兑现 | `W/GameWorld.Core.cs:820-821`、`Grep recruitCost` 17 命中无读者 | P1 |
| 7 | `PilgrimAI`/`MercenaryAI` 生产不可达；三张规则表死代码 | 9 个 AI 里 2 个永不运行；86 行规则表无消费者 | 见 §6 | P2 |
| 8 | `TransportUnit` 运力/角色/分遣/建寨模型基本只在测试生效 | 22 个 `TransportUnitTests` 覆盖的能力在生产只有"吃粮/移速/发货"三条被用到 | §3 零调用清单 | P2 |
| 9 | `actor.morale`/`attributes`/`factionId` 只写不读（却入档） | 字段承诺为空；`factionId` 让人误以为行为体有派系归属 | §5 表 | P2 |
| 10 | 营寨锚点坐标恒 `x=y=0.5` 且 `anchorId` 取"当前最大+1" | 与聚落锚点共用 id 空间，长期运行有 id 冲突/渲染重叠风险 | `A/CampManager.cs:119-137` | P2 |

## 9. 我核实不了的事

- 玩家是否**本应**看见行为体：仓内无"地图单位"UI 契约或未接线面板，只有 `S/RelationProjection.cs:110-125` 与 `U/MapUnitRegistry.cs:26-52` 的"给视图用"注释，无法区分"未实现"还是"已废弃"。
- `A/MapActor.cs` 的 `health=100/supplies=100/morale=80` 与各 `XxxSpawnChance` 是否有设计稿：无内容文件、无调参入口（`W/GameWorld.Core.cs:915-916` 只接线 `NaturalSpawning`/`SpawnRateMultiplier`）。
- `SimulationTickContext.deltaDays>1`（daysPerTick）时 `movementProgress`（`A/MapActor.cs:117`）与 `supplies-=deltaDays*0.5`（`:142`）的实际天数标定：未跑测。
- `decreeState` 的入档完整性：`St/RealmDTO.cs:22` 与 `...Laws/DecreeOrder.cs:5` 具备 `[Serializable]`，但未跑存档往返，不能确认 `JsonUtility` 对 `RealmDecreeState.orders` 的逐字段落地。
- `A/CampManager.cs:119-137` 的锚点 id 分配是否已被 `World.Anchor` 域约定规避：未审该域。
- `S/EconomyManager.cs` 只在 grep 级别确认其 `Caravan`/`TreasuryLedger` 用法，未读全文；未核其与营寨补给是否有耦合（疑似无）。
