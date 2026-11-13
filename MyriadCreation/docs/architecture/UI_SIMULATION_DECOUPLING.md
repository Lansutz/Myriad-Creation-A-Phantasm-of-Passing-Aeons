# UI 去 Simulation 依赖方案（UI_SIMULATION_DECOUPLING）

> 卡片：**task-91（B8-last 方案：UI 去 Simulation 依赖的可行性与分步）**
> 性质：**只读审计 + 方案**（本卡仅写本文件，未修改/移动/删除任何 `.cs`/`.asmdef`，未跑 Unity/编译/测试）。
> 快照：2026-11-01 18:2x（+08:00）；基线：`docs/architecture/SYSTEM_OWNERSHIP.md`、`APP_EXTRACTION_PLAN.md`、`AsmdefArchitectureGateTests.cs`。
> 结论先行：可行，但**不能先删 UI.asmdef 的 Simulation 引用**——必须先把"世界/面板数据/命令"三类出口做成 Core 可表达的契约，最后一步才删引用与门禁白名单。

---

## 0. 结论摘要（给 Lead）

1. **规模**：UI 共 **28 个 `.cs` / 5,793 行**；其中 **13 个文件、69 行 `using MyriadCreation.Simulation*`**（占 UI 文件 46%）；另有 3 个文件只 import 但无实际 Simulation 类型/成员使用（`UIManager.Feedback`/`UIManager.MapMode`/`UiFactory`），可先零风险清理。
2. **核心耦合点**：`UIManager.cs` 的 `[SerializeField] private GameWorld world;`、`IGameUiHost.BindWorld(GameWorld)`、`IGameSession.World`（App.Contracts 仍引用 Simulation）是"UI 必须引用 Simulation"的根；其余面板基本是**读 GameWorld/RealmData/SettlementData/CharacterData 后自行格式化**。
3. **可行路径**：先加 **Core.Contracts 只读查询/DTO**（Core 不引 Simulation），Simulation 侧写 host 适配器；UI 面板改拿 DTO + 查询接口；写操作（政府面板法令/政令）走 Core 的 `ILawCommand`/新增 `IDecreeCommand`，由 App 注入。最后才删 UI.asmdef 的**模拟程序集引用（Simulation + Laws + Faith + Culture 四个）**与门禁 `UI→Simulation` 白名单。
4. **推荐顺序**：`4a 契约脚手架 + 零风险清理` → `4b 只读查询/DTO（按面板分批）` → `4c 命令接口（GovernmentPanel）` → `4d 删引用/门禁白名单/测试收口`。
5. **最小首步 3 项**（建议下一张实现卡直接做）：
   - 删除 `UIManager.Feedback.cs`/`UIManager.MapMode.cs`/`UiFactory.cs` 的 Simulation using（无实际类型引用）；
   - 新增 `IWorldReadModel` + `IMapStatsView`（Core.Contracts）+ Simulation host 适配器，替换 `UIManager.Panels.cs:52-89` 的地图统计/当前年份显示；
   - `GovernmentPanel` 的 `GameWorld` 依赖改为注入 `ILawQuery`/`ILawCommand`（现有 Core 契约），`IssueDecreeOrders` 暂时留在 wrapper，待 4c 收口。

---

## 1. 扫描方法与快照

- 扫描范围：`Assets/UI/**/*.cs`（28 文件）。
- 统计 `using MyriadCreation.Simulation*` 行数；用 `Assets/Scripts/Simulation/**/*.cs` 的类型声明表做"类型 token 命中"，再人工/逐行核对成员访问（`world.`/`realm.`/`settlement.`/`character.`/`faith.`/`society.`）。
- 列出的"成员"只列 UI 实际访问到的（不追求完整 GameWorld API）。
- 注意：`SettlementData`/`CampData` 中 **`SettlementData` 在 World.Settlement 程序集**（不是 Simulation）；本卡目标是 Simulation，`SettlementData`/`WorldSetupOptions` 等属于 UI→World 的另一条依赖，需另卡处理（见 §7 范围边界）。
- 当前架构门禁：`MyriadCreation.UI.asmdef` 仍引用 `MyriadCreation.Simulation`（且同时引用 `MyriadCreation.Laws/Faith/Culture`，见下），门禁唯一白名单 `UI→Simulation`（TODO B7）。本方案完成 4d 后才能删该白名单。
- **范围修正（重要）**：simulation 侧已拆成 **4 个 asmdef**：`MyriadCreation.Simulation` + `MyriadCreation.Laws` + `MyriadCreation.Faith` + `MyriadCreation.Culture`；UI asmdef 同时引用这 4 个。因此"UI 去 Simulation"必须按"**去 Simulation 家族（4 个程序集）**"执行：只删 `MyriadCreation.Simulation` 引用会因 Laws/Faith/Culture 类型仍被使用而编译失败；4d 要同时删 4 个引用并在门禁/测试中固化。<br>其中 Law/Faith/Culture 类型（`LawConstraints`、`FaithSystem`、`CultureData` 等）来自各自程序集，但其契约（`ILawQuery/IFaithQuery/ICultureQuery`）已在 Core.Contracts。

---

## 2. 逐文件清单（13 个 import 文件）

| # | UI 文件 | Simulation import 行数 | 实际使用的 Simulation 类型（token 命中） | UI 实际访问的成员（代表） |
|---|---|---|---|---|
| 1 | `Assets/UI/Common/UIManager.cs` | 9 | `GameWorld`, `RealmData`, `AdminDivision`, `OfficeTitleCatalog`, `LawConstraints`, `SupremeSovereignty`, `Alliance` | `world.PlayerRealmId/tiles/cultures/GetCharacterManager`；`realm.officeHolders/laws`；`OfficeTitleCatalog.GetDefaultTitleKey/GetTitle`；`LawConstraints.SupremeSovereignty`；`Alliance`（地图模式枚举） |
| 2 | `Assets/UI/Common/UIManager.Panels.cs` | 10 | `RealmData`, `FaithSystem`, `CanonizationSystem`, `MentalHealthSystem`, `DnaSystem`, `AdminDivision`, `CharacterKnowledgeRegistry`, `PopulationStats`, `ResourceAccess` | `world.PlayerRealmId/currentYear/currentDay/startYear/startDay/realms/tiles/provinces/mapWidth/mapHeight/cultures/goodsDefs`；`world.GetLandTileCount/GetSeaTileCount/GetInnovationTree/GetCharacterManager/GetFaithSystem/FaithQuery/GetRealmSociety/RegimeDynamics/Factions`；`realm.realmId/statePatronSaintId/adminDivisions`；`CanonizationSystem.GetSaints`；`PopulationStats.GetDominantCulture`；`ResourceAccess.DescribeStatus`；`MentalHealthSystem/DnaSystem`（角色知识视图） |
| 3 | `Assets/UI/Common/UIManager.Feedback.cs` | 9 | **无**（仅 partial 共享 `world` 字段） | `world.currentYear/currentDay`（字段声明在 `UIManager.cs`） |
| 4 | `Assets/UI/Common/UIManager.MapMode.cs` | 9 | **无** | 无 Simulation 成员；纯 UI 地图模式/按钮逻辑 |
| 5 | `Assets/UI/Common/UiFactory.cs` | 1 | **无** | 无；import 可删 |
| 6 | `Assets/UI/MyriadCreation.UI.Presentation/ClassPanelText.cs` | 1 | `RealmSociety`, `ClassProfile`, `ClassNeedReport` | `society.classes/unrestScore/totalPopulation`；`ClassProfile.populationShare/...`；`ClassNeedReport`/`ClassNeedDimension`（阶级需求条） |
| 7 | `Assets/UI/MyriadCreation.UI.Presentation/RealmDivisionText.cs` | 3 | `AdminDivision`, `TitleCatalog`, `TitleDef` | `division.name/level/tiles/titleId/divisionId/parentDivisionId/holderCharacterId`；`TitleCatalog.Get` |
| 8 | `Assets/UI/MyriadCreation.UI.Presentation/RealmOverviewText.cs` | 4 | `RealmData`, `RealmSociety`, `LawComposition`, `LawConstraints`, `LawDimension`, `AdminDivision` | `realm.realmName/treasury/stability/centralization/laws`；`society.totalPopulation`；`LawComposition.SupremeSovereignty/SupremeSuccession`；`AdminDivision` 树 |
| 9 | `Assets/UI/MyriadCreation.UI.Presentation/ReligionPanelText.cs` | 4 | `FaithSystem`, `CanonizationSystem` | `faith.fervor/hierarchyLevel/faithId/CanDeclareGreatHolyWar`；`CanonizationSystem.GetSaints`；`SaintDef`（Content） |
| 10 | `Assets/UI/MyriadCreation.UI.Presentation/SettlementPeopleText.cs` | 6 | `GameWorld`, `CharacterData`, `CharacterLocationService`, `RealmData`, `CultureData`, `AdminDivision`, `CampData`, `LawConstraints`, `OfficeTitleCatalog`, `SupremeSovereignty` | `world.realms/cultures/settlements/Camps`；`CharacterLocationService.GetKnownAtSettlement`；`character.characterId/fullName/role/cultureId/realmId`；`realm.monarchId/laws/officeHolders`；`CultureData`；`OfficeTitleCatalog.GetTitle`；`CampData.anchorId/tileIndex` |
| 11 | `Assets/UI/Panels/GovernmentPanel.cs` | 2 | `GameWorld`, `RealmData` | `world.LawQuery/LawCommand/currentDay/realms/PlayerRealmId/IssueDecreeOrders`；`realm.realmId/GetSupremeRulerId`；`ILawQuery/ILawCommand` 用法已存在但入口仍是 GameWorld |
| 12 | `Assets/UI/Panels/SettlementDetailPanel.cs` | 2 | `GameWorld`, `RealmData` | `world.settlements/realms/tiles`；`settlement.anchorId/tileIndex/settlementName/siteMode/settlementLevel/SettlementFormName/population/wealth/garrison/garrisonQuantity/garrisonQuality/constructionTier/constructionProgress/districts/facilities/market/hasMarket/hasTemple/hasUniversity/isCapital/isPort/IsRuined/controlFaction`；`realm.realmName` |
| 13 | `Assets/UI/Panels/SocietyPanel.cs` | 9 | `RealmData`, `RealmSociety`, `FactionManager`, `RegimeChangeDynamics` | `realm.realmName/realmId/stability/centralization/treasury`；`society.classes/dominantClass/mostRestlessClass/totalPopulation/unrestScore`；`factions.GetFactions`；`regime.GetState` |

**汇总**：
- UI 文件总数 **28**；有 Simulation import 的文件 **13**；无 import 的 **15**。
- `using MyriadCreation.Simulation*` 行 **69**；实际出现 Simulation 类型 token 的文件 **10**；只 import 的 **3**（#3/#4/#5）。
- 除 `using` 外仅 1 处全限定引用：`UIManager.Panels.cs:177` 的 `MyriadCreation.Simulation.Systems.ResourceAccess.DescribeStatus(...)`。
- `UIManager` 是 **4 个 partial 文件**（`.cs`/`.Panels.cs`/`.Feedback.cs`/`.MapMode.cs`），共享 `private GameWorld world` 字段；`UIManager.cs:45/182/211` 是 UI→Simulation 的主入口。

### 2.1 使用类型频次 Top 20（UI 全量 token 计数）

| 类型 | 次数 | 主要文件 |
|---|---:|---|
| `GameWorld` | 27 | UIManager/Panels/SettlementPeople/Government/SettlementDetail |
| `AdminDivision` | 13 | UIManager/RealmDivision/RealmOverview/SettlementPeople |
| `CharacterData` | 8 | SettlementPeople/UIManager.Panels |
| `RealmData` | 7 | UIManager/Panels/Government/RealmOverview/Society/SettlementDetail |
| `ClassProfile` | 6 | ClassPanelText |
| `LawConstraints` | 5 | UIManager/RealmOverview/SettlementPeople |
| `OfficeTitleCatalog` | 4 | UIManager/SettlementPeople |
| `RealmSociety` | 4 | ClassPanel/RealmOverview/Society |
| `CanonizationSystem` | 3 | ReligionPanel/UIManager.Panels |
| `FaithSystem` | 3 | ReligionPanel/UIManager.Panels |
| `SupremeSovereignty` | 3 | UIManager/RealmOverview/SettlementPeople |
| `CampData` | 2 | SettlementPeople |
| `DnaSystem` | 2 | UIManager.Panels |
| 其余各 1 | - | `Alliance`, `CharacterKnowledgeRegistry`, `CharacterLocationService`, `ClassNeedReport`, `CultureData`, `FactionManager`, `LawComposition`, `LawDimension`, `MentalHealthSystem`, `PopulationStats`, `RegimeChangeDynamics`, `ResourceAccess` |

### 2.2 import 命名空间分布（Top）

| namespace | 行数 |
|---|---:|
| `MyriadCreation.Simulation.State` | 11 |
| `MyriadCreation.Simulation.WorldState` | 9 |
| `MyriadCreation.Simulation.Systems` | 9 |
| `MyriadCreation.Simulation.Modding` | 6 |
| `MyriadCreation.Simulation.Economy` | 5 |
| `MyriadCreation.Simulation.Events` | 5 |
| `MyriadCreation.Simulation.Generation` | 5 |
| `MyriadCreation.Simulation.Politics` | 5 |
| `MyriadCreation.Simulation.Definitions` | 4 |
| `MyriadCreation.Simulation.Faith` | 3 |
| `MyriadCreation.Simulation.Laws` | 3 |
| `MyriadCreation.Simulation.FaithHost` | 2 |
| `MyriadCreation.Simulation.Actors` | 1 |
| `MyriadCreation.Simulation.Culture` | 1 |

### 2.3 `GameWorld` 成员访问汇总（UI 实际用到的 26 个）

`PlayerRealmId`, `currentDay`, `currentYear`, `startDay`, `startYear`, `mapWidth`, `mapHeight`, `tiles`, `provinces`, `realms`, `cultures`, `settlements`, `Camps`, `goodsDefs`, `Factions`, `RegimeDynamics`, `FaithQuery`, `LawQuery`, `LawCommand`, `GetLandTileCount()`, `GetSeaTileCount()`, `GetInnovationTree()`, `GetCharacterManager()`, `GetFaithSystem(id)`, `GetRealmSociety(realmId)`, `IssueDecreeOrders(decreeId, realmId)`。

**结论**：`GameWorld` 在 UI 里主要承担"世界级查询入口"（26 个成员中只有 `IssueDecreeOrders`/`LawCommand.*` 是写命令），因此设计 `IWorldReadModel` + 查询接口后，UI 可不持 GameWorld。

---

## 3. 现有契约能力评估（分类 A）

| 现有契约（位置） | 覆盖内容 | UI 现状/缺口 |
|---|---|---|
| `ILawQuery` / `ILawCommand`（Core.Contracts） | `GetGovernmentView`、`GetActiveLaw`、`BeginEnactLaw`、`CancelPendingLaw`、`EnactDecree`、`RevokeDecree` 等 | `GovernmentPanel` 已用，但通过 `world.LawQuery/LawCommand`；`world.IssueDecreeOrders` 未进契约；`realm.GetSupremeRulerId` 未进契约 |
| `IFaithQuery`（Core.Contracts） | `GetStateReligionId/HasFaith/GetFaithName/GetFervor/GetHolySiteTiles/TryGetFaithSnapshot` | `UIManager.Panels` 已用 `world.FaithQuery`；**ReligionPanelText 需要 fervor/hierarchyLevel/CanDeclareGreatHolyWar/圣徒列表**，超出 FaithSnapshotDto |
| `ICultureQuery` / `ICultureCommand`（Core.Contracts） | 主体文化 id/名称/阶段/社会分层等 | UI **尚未使用**（0 文件），可复用但不覆盖阶级/人口展示 |
| `IMapWorldView` / `IMapRealmView` / `IMapSettlementView` / `IMapOverlayView`（Core.Contracts） | 地图宽高/tile/文化/外交/聚落/游牧/叠加层只读视图 | Rendering 用，**UI 0 引用**；缺地图统计（陆地/海洋/省份数）与 UI 专用聚合 |
| `IAppFlow`（App.Contracts） | 主菜单→生成→游戏→编辑流程 | UI **0 直接引用**（Still `SceneFlowController`/`Bootstrap` concrete） |
| `IGameSession`（App.Contracts） | 状态/速度/开新档/读档/存档 | **`GameWorld World {get;}`、`Action<GameWorld> WorldChanged` 直接暴露 Simulation 类型**，UI 无法在不引 Simulation 的情况下使用；且含 `WorldSetupOptions`（World 程序集） |
| `IGameUiHost`（App.Contracts） | `BindWorld(GameWorld)`/`NotifyWorldChanged(GameWorld)`/loading/panel 开关 | **同样暴露 GameWorld**；`UIManager` 实现该接口 → 无法删 Simulation 引用 |
| `ISettingsService`/`IUiFactory`/`IMainMenuUi`/`ISettlementDetailUi` | UI/设置反向契约 | 与 Simulation 无关，可继续使用 |

**A 类（可立即用现有契约/仅清理）**
- **注**：UI asmdef 实际引用 4 个模拟程序集（Simulation/Laws/Faith/Culture）；表中 `ILawQuery/IFaithQuery/ICultureQuery` 在 Core，但 UI 仍因使用 `LawConstraints`/`FaithSystem`/`CultureData` 等类型而保留 Laws/Faith/Culture 引用，4d 必须四者一起删。
- **A1 零风险 import 清理**：`UIManager.Feedback.cs`、`UIManager.MapMode.cs`、`UiFactory.cs` 删除各自 Simulation using（无类型/成员引用；删除后编译不受影响，因为 `world` 字段类型声明在 `UIManager.cs`，partial 成员可解析）。
- **A2 GovernmentPanel 读**：`GetGovernmentView/GetPendingLaws` 已是 `ILawQuery`；只要把查询对象从 `world.LawQuery` 改为注入的 `ILawQuery`。
- **A3 GovernmentPanel 写**：`BeginEnactLaw/CancelPendingLaw/EnactDecree/RevokeDecree` 已是 `ILawCommand`；`IssueDecreeOrders` 需补进命令契约/`IDecreeCommand`。
- **A4 UIManager faith**：`IFaithQuery` 已存在（`world.FaithQuery`），可先注入接口；但 ReligionPanel 扩展字段属于 B。
- **A5 删除 3 个 import-only 文件后**，UI 仍因其它 10 个文件/`IGameSession`/`IGameUiHost` 保留 Simulation 引用，不能删 asmdef。

---

## 4. 需要的新契约（分类 B）——最小设计

> 放置原则：DTO/接口放 **Core.Contracts**（Core 不引 Simulation；只使用 int/string/bool/float/枚举/`Core.Data` 类型）；适配器实现放 **Simulation 家族**（`Simulation`/`Laws`/`Faith`/`Culture` 各自 host，均引 Core），由 App/GameManager 注册为 `IWorldReadModel`；UI 只引 Core.Contracts（+App.Contracts 的流程/设置接口）。禁止把 GameWorld/RealmData/CharacterData/SettlementData 放进 Core 接口签名。

### 4.1 世界只读聚合（替代 UIManager 的 GameWorld 字段）

```csharp
// Core.Contracts/IWorldReadModel.cs
public interface IWorldReadModel
{
    int CurrentDay { get; }
    int CurrentYear { get; }
    int PlayerRealmId { get; }
    IMapWorldView Map { get; }          // 已有
    IMapStatsView MapStats { get; }     // 新增：地块/陆海/省份数
    IRealmQuery Realms { get; }
    ISettlementQuery Settlements { get; }
    ICharacterQuery Characters { get; }
    IFaithQuery Faith { get; }
    ICultureQuery Cultures { get; }
    ILawQuery Laws { get; }
    ILawCommand LawCommands { get; }
    ISocietyQuery Society { get; }
    IInnovationQuery Innovations { get; }
    bool TryGetWorldEventLog(...);       // 可选：AddEventLog 需要的 day 已可由 CurrentDay 提供
}

public interface IMapStatsView
{
    int TotalTiles { get; }
    int LandTiles { get; }
    int SeaTiles { get; }
    int ProvinceCount { get; }
}
```

`IGameSession`（App.Contracts）改造：
```csharp
IWorldReadModel WorldView { get; }
event Action<IWorldReadModel> WorldViewChanged;
// WorldSetupOptions -> NewGameRequestDto（App.Contracts/Core），
// 避免 UI 因签名再引 World 程序集（本卡只处理 Simulation；World 依赖另卡）
```
`IGameUiHost.BindWorld(GameWorld)` → `BindWorld(IWorldReadModel)`；`UIManager` 只保存 `IWorldReadModel`（非 Unity 序列化字段，改由事件注入）。

### 4.2 面板只读 DTO

| DTO | 最小字段（UI 实际需要） | 查询接口 |
|---|---|---|
| `AdminDivisionNodeDto` | divisionId, parentDivisionId, level, name, titleId, holderCharacterId, tiles(int[]) | `IAdminDivisionQuery.GetAdminDivisions(realmId)` / `GetDivision(divisionId)` |
| `RealmSnapshotDto` | realmId, realmName, treasury, stability, centralization, isMonarchy, supremeRulerCharacterId, statePatronSaintId, governmentLabel, successionLabel, adminDivisions, officeDisplay | `IRealmQuery.GetRealm(realmId)`；`GetOfficeDisplay(realmId)`（把 `OfficeTitleCatalog` 逻辑留在 Simulation） |
| `CharacterRowDto` | characterId, fullName, roleLabel, officeLabel, cultureName, realmId, isLeader | `ICharacterQuery.GetKnownAtSettlement(anchorId, observerRealmId)` / `GetCharacter(id)` / `IsLeader(id)` |
| `SettlementSnapshotDto` | anchorId, tileIndex, settlementName, siteTypeLabel, settlementFormName, settlementLevel, siteMode, population, wealth, constructionTier, constructionProgress, garrison/garrisonQuality/garrisonQuantity, marketStock, marketCapacity, hasMarket, hasTemple, hasUniversity, isCapital, isPort, isRuined, districtNames, facilityNames, controlFactionName | `ISettlementQuery.TryGetSettlement(anchorId, out dto)`；`ICharacterQuery.GetKnownAtSettlement` |
| `SocietySnapshotDto` | realmId, realmName, totalPopulation, unrestScore, classes (`ClassSnapshotDto`: name, populationShare, unrest, needs), dominantClassName, mostRestlessClassName | `ISocietyQuery.GetRealmSociety(realmId)` / `GetClassDetail(...)` |
| `FaithPanelDto` / `SaintRowDto` | faithId, faithName, fervor, hierarchyLevel, canDeclareGreatHolyWar, patronSaintName, saints | 扩展 `IFaithQuery.TryGetFaithPanel(realmId)`（或 `IReligionQuery`） |
| `FactionRowDto` / `RegimeRowDto` | name, strength, unrest, state text | `ISocietyQuery.GetFactions(realmId)` / `GetRegimeState(realmId)`（把 `FactionManager/RegimeChangeDynamics` 格式化留在 Simulation） |
| `TileResourceRowDto` | goodsName, statusLabel | `IResourceQuery.DescribeTileResources(tile, realmId)`（替代 `ResourceAccess.DescribeStatus` 直接调用） |
| `InnovationRowDto`（可选） | id/name/status | `IInnovationQuery`（替代 `GetInnovationTree()` 传参） |

**关键原则**：DTO 只带 `label/name/值`；所有涉及 `OfficeTitleCatalog/LawConstraints/SettlementFormName/PopulationStats/ResourceAccess` 的格式化逻辑放 Simulation 适配器（与 `LawItemDto`/`MapSettlementInfo` 既有模式一致），避免 DTO 爆炸，也不让 UI 重新 import Simulation。

### 4.3 命令接口（分类 C）

| UI 写操作 | 现状 | 目标 |
|---|---|---|
| `GovernmentPanel` 法令/政令：`BeginEnactLaw/CancelPendingLaw/EnactDecree/RevokeDecree` | `world.LawCommand`（Core `ILawCommand`） | 注入 `ILawCommand`（已有） |
| `GovernmentPanel.IssueDecreeOrders(decreeId, realmId)` | `world.IssueDecreeOrders` 直接调用（返回订单列表） | 新增 `IDecreeCommand.IssueOrders(...)`（Core.Contracts，返回 DTO/数量） |
| `realm.GetSupremeRulerId()`（读） | `RealmData` 方法 | `IRealmQuery.GetSupremeRulerId(realmId)` |
| 其余面板（SettlementDetail/Society/Presentation） | 只读 | 无写命令 |

**注入方式**：App/GameManager 持有 `IWorldReadModel` + `ILawCommand`/`IDecreeCommand`；通过 `IGameSession`（或 `IUiFactory` 类似的定位器）暴露给 UI；**不允许** UI 自己 `FindAnyObjectByType<GameWorld>()`。

---

## 5. 规模统计与改造文件数

| 指标 | 数值 |
|---|---:|
| UI `.cs` 文件 | 28 |
| UI 总行数 | 5,793 |
| 有 Simulation `using` 的文件 | 13（46%） |
| Simulation `using` 行 | 69 |
| 仅 import、无实际类型/成员使用 | 3（Feedback/MapMode/UiFactory；19 行 using 可优先清） |
| 出现 Simulation 类型 token 的文件 | 10 |
| UI 中 `GameWorld` 引用次数 | 27 |
| UI 中 `GameWorld` 成员种类 | 26 |
| UI asmdef 引用的模拟程序集 | **4**：`Simulation`/`Laws`/`Faith`/`Culture`（`SettlementData` 属 World，另计） |
| 门禁白名单 | 1 条：`UI→Simulation`（Laws/Faith/Culture 未被白名单单独限制） |

**按契约/步骤预计改造 UI 文件数（去重前）**：

| 改造 | 预计文件 |
|---|---|
| 4a import-only 清理 | 3（Feedback/MapMode/UiFactory） |
| `IWorldReadModel` + `IMapStatsView`（UIManager 顶栏/事件日志/地图统计） | 4（UIManager.cs / Panels / Feedback / MapMode） |
| `IRealmQuery`/`AdminDivision` | 6（GovernmentPanel、RealmOverviewText、SocietyPanel、UIManager.cs、SettlementDetailPanel、SettlementPeopleText） |
| `ISettlementQuery` + `ICharacterQuery` | 3（SettlementDetailPanel、SettlementPeopleText、UIManager.Panels） |
| `ISocietyQuery` + Class DTO | 4（ClassPanelText、SocietyPanel、RealmOverviewText、UIManager.Panels） |
| `IFaithQuery` 扩展 | 2（ReligionPanelText、UIManager.Panels） |
| `IResourceQuery`/`IInnovationQuery` | 1（UIManager.Panels） |
| `ILawQuery/ILawCommand` 深化 + `IDecreeCommand` | 2（GovernmentPanel、RealmOverviewText 的 laws 读取） |
| 契约/适配器新增（非 UI） | Core.Contracts 4-6 文件、Simulation host 2-4 文件、App.Contracts 1-2 文件 |

> 实际总 UI 改造文件约为 **13**（有 import 的文件）＋可能 4 个 partial 合并；4d 才改 asmdef/门禁。

---

## 6. 分步计划（4a→4d，每步可独立提交/回滚）

> 前置：每步编译 + EditMode 全量 + 相关 PlayMode（GovernmentPanel/MainMenu/MapVisibility/SettlementIcon/设置）作为门禁；保持旧路径可用，**新契约与旧 GameWorld 路径并行**，逐文件切换。

### 4a 契约脚手架 + 零风险清理（首步）
1. 删除 3 个 import-only 文件的 Simulation using（Feedback/MapMode/UiFactory）。
2. Core.Contracts 新增 `IWorldReadModel`/`IMapStatsView`（仅骨架）与 Simulation host 适配器（包装 `GameWorld`）；App/GameManager 实例化但不强制 UI 使用。
3. 改 `IGameSession.World`/`WorldChanged` 与 `IGameUiHost.BindWorld` 为 `IWorldReadModel`（UI/GameManager 同步适配），`UIManager.Panels` 的地图统计/当前年显示先切 `IWorldReadModel`。
- **最小首步 3 项**：①删 3 个 import；②`IWorldReadModel`+MapStats 适配器；③GovernmentPanel 注 `ILawQuery/ILawCommand`（去掉 `world.LawQuery/Command`）。
- 门禁：编译；EditMode；PlayMode GovernmentPanel/MainMenu；UI 地图统计与事件日志时间显示回归。
- 风险：`IGameSession`/`IGameUiHost` 改签名会牵动 GameManager/UI/tests（一次性适配，保持重载兜底可选）；回滚：契约文件+引用全量还原（不改数据）。

### 4b 只读查询/DTO（按面板分批）
1. `IRealmQuery`+`RealmSnapshotDto`/`AdminDivisionNodeDto`：迁移 `GovernmentPanel`、`RealmOverviewText`、`SocietyPanel`、`RealmDivisionText`、`UIManager` 官职显示。
2. `ISettlementQuery`+`SettlementSnapshotDto`：迁移 `SettlementDetailPanel`；`ICharacterQuery`+`CharacterRowDto`：迁移 `SettlementPeopleText`、`UIManager.Panels` 的"聚落已知人物"。
3. `ISocietyQuery`+`ClassSnapshotDto`、`IFaithQuery` 扩展（信仰面板）、`IResourceQuery`/`IInnovationQuery`：迁移 `ClassPanelText`、`ReligionPanelText`、`UIManager.Panels` 资源配置。
- 门禁：编译；EditMode 全量；PlayMode SettlementIcon/Government/Society（如存在）；UI 逐面板截图/手测对照（文本内容需逐字一致）；DTO 空值/未加载世界降级。
- 风险：格式化逻辑从 UI 迁 Simulation 容易漏字段/换行；DTO 字段膨胀。缓解：DTO 字段按 UI 实际使用最小集；格式化逻辑集中在适配器并加单测（MapSettlementInfo/LawItemDto 模式）。
- 回滚：每面板一个小步骤，保留旧分支；DTO 文件可删。

### 4c 命令接口收口
1. `ILawCommand`/新增 `IDecreeCommand` 放进 Core.Contracts，GovernmentPanel 不再持 `GameWorld`；`IssueDecreeOrders` 改为返回 DTO/订单数。
2. `IRealmQuery.GetSupremeRulerId` 替代 `RealmData.GetSupremeRulerId`；所有 `world.` 写调用清零（静态 grep）。
3. 命令由 App/GameManager 注入 UI（通过 `IGameSession` 或 `IAppCommands`），不通过 GameWorld。
- 门禁：编译；EditMode；PlayMode GovernmentPanel（法令/政令按钮流程）；grep `world.` 写调用=0。
- 风险：命令语义（授权/冷却/推行中）在适配器内必须逐项对齐 LawAndDecreeSystem；回滚：UI 回到 `world.LawCommand` 旧路径（旧代码保留到 4c 完成）。

### 4d 删除 UI→Simulation 引用与门禁白名单
1. 全仓 grep/CI 确认 UI 无 `using MyriadCreation.Simulation*`/`MyriadCreation.Simulation.`/`GameWorld`/`RealmData` 等类型；删除 UI 文件遗留字段（`UIManager.world` 改 `IWorldReadModel`）。
2. `MyriadCreation.UI.asmdef` 删除 **4 个模拟程序集引用**：`MyriadCreation.Simulation`、`MyriadCreation.Laws`、`MyriadCreation.Faith`、`MyriadCreation.Culture`；门禁 `KnownTransitionEdges` 删除 `MyriadCreation.UI->MyriadCreation.Simulation`，并新增/更新断言：UI refs 不含上述 4 个模拟程序集（当前 `CheckSubsystemForbiddenRefs` 只约束 Laws/Faith/Culture 自身，不约束 UI；需在 `AsmdefArchitectureGateTests` 加 UI 侧断言）。`CheckUiDependencyDirection` 保留 UI→App。
3. 清理注释/文档；跑全量 EditMode/PlayMode + 主场景冒烟（开始游戏/读档/政府/聚落/社会/宗教/阶级面板）。
- 门禁：编译 + 全量测试 + `AsmdefArchitectureGateTests` 0 违规；`grep -rn "MyriadCreation.Simulation" Assets/UI` = 0；UI asmdef refs 不含 Simulation/Laws/Faith/Culture 四个模拟程序集。
- 风险：最后删引用才能发现所有隐式依赖（反射、`var`、扩展方法）；用 `grep`+编译双保险。回滚：恢复 asmdef 引用与门禁白名单（代码层已解耦，可快速回退）。

**每步回滚点**：以 git commit/临时分支为单位；4a/4b 都是"新增契约+逐文件切换"，回滚只需还原 UI 文件；4c 旧 LawCommand 路径保留到完成；4d 引用/门禁单点可回滚。

---

## 7. 风险、边界与待决策

### 7.1 风险
- **UI 格式化逻辑外移**：`SettlementPeopleText`/`RealmOverviewText`/`SocietyPanel` 含大量中文格式化/排序/筛选逻辑；迁 Simulation 适配器可能改变文案细节 → 需要逐面板文本对照测试。
- **接口签名破坏**：`IGameSession.World`/`IGameUiHost.BindWorld` 从 `GameWorld` 改 `IWorldReadModel` 会牵动 App/UI/tests（`WorldRebindOnLoadTests` 反射、PlayMode 测试找 GameManager/Bootstrap）→ 一次 PR 内完成或保留过渡重载。
- **世界为空/未加载**：所有新查询必须 null 安全（现有 UI 有大量 `world == null` 判空）。
- **性能**：UI 每帧/节流刷新若每次 new DTO/查字典，可能 GC；建议查询接口内部复用缓存/DTO 池，或按 `_uiUpdateTimer` 节流后调用（沿用现状）。
- **Scene 序列化**：`UIManager.world` 是 `[SerializeField]` GameWorld；改为非序列化视图后需确认场景无引用丢失（改动前先查 Main.unity）。
- **测试依赖**：PlayMode 测试通过 `FindAnyObjectByType<GameManager/Bootstrap>`/反射 `RuntimeMapEditingController.world`；契约改造会影响，需要同步测试。
- **范围边界**：UI 仍依赖 `SettlementData`（World）、`WorldSetupOptions`（World）、`MapRenderer`（Rendering）、`ReligionDef`（Content）；本卡只解决 **Simulation 家族（Simulation/Laws/Faith/Culture 四个程序集）**，UI 最终 asmdef 目标需另卡决定是否也去 World/Rendering。另：Laws/Faith/Culture 已是独立 asmdef，若只删 Simulation 引用会编译失败，必须按 4 个程序集一起收口。

### 7.2 待 Lead 决策
1. `IWorldReadModel` 是否放 **Core.Contracts**（推荐；UI 只依赖 Core）？还是放 App.Contracts（则 UI→App.Contracts，仍不引 Simulation）？
2. 面板 DTO 是否允许 **预格式化 label 字段**（推荐，沿用 LawItemDto/MapSettlementInfo 模式），避免 UI 再 import Simulation 计算逻辑？
3. `IGameSession` 的 `WorldSetupOptions`（World 程序集）是否同卡换成 `NewGameRequestDto`，还是留到"UI 去 World"卡？
4. 是否接受 UI 仍依赖 World/Rendering（本卡只去 Simulation）；最终 UI asmdef 目标 refs 需要确认。
5. 4a 是否允许先改 `IGameSession`/`IGameUiHost` 签名（影响 GameManager/UI/tests），还是先加兼容属性过渡？

---

## 8. 附录：扫描命令（只读，可复现）

```bash
# 文件/行数
find Assets/UI -name '*.cs' | wc -l
grep -rn '^using MyriadCreation.Simulation' Assets/UI --include='*.cs' | wc -l
# 每个文件 import + 实际类型
grep -rl '^using MyriadCreation.Simulation' Assets/UI --include='*.cs'
# 全限定引用
grep -rn 'MyriadCreation\.Simulation\.' Assets/UI --include='*.cs' | grep -v 'using '
# UI 中 GameWorld 成员访问
grep -rhoE '\bworld\.[A-Za-z_][A-Za-z0-9_]*' Assets/UI --include='*.cs' | sort -u
# 门禁白名单/UI asmdef
grep -n 'KnownTransitionEdges\|UI->Simulation' Assets/Tests/EditMode/AsmdefArchitectureGateTests.cs
cat Assets/UI/MyriadCreation.UI.asmdef
```

> 本文件为只读方案的唯一写入；未修改任何 `.cs`/`.asmdef`，未运行 Unity/编译/测试。
