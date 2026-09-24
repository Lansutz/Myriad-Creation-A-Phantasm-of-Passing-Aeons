# Architecture 2.0 — Domain Audit 04

## 本轮主题：现有 MapActor / Army / TileData 的真实职责

本轮不创建新类，直接根据仓库现有代码确认此前讨论的概念边界。

## 1. 现有 MapActor 实际上已经是“地图单位”雏形

现有路径：
`CivilizationEvolution/Assets/Scripts/Simulation/Actors/MapActor.cs`

代码中已经明确存在：
- `actorId`
- `MapActorType`
- `currentTile`
- `targetTile`
- `movementProgress`
- `moveSpeed`
- `population`
- `health`
- `morale`
- `supplies`
- `ownerRealmId`
- `factionId`
- `isHostile`
- AI
- `SetTarget()`

`MapActorManager` 还承担：
- Actor 注册表
- Spawn
- Remove
- Tick
- 按地块查询
- 按类型统计

因此它与我们现在定义的 Map Unit 高度重合。

但是它目前明显**过度承载职责**：
- 地图单位状态
- 单位 AI
- 人口规模
- 健康
- 士气
- 补给
- 特定单位类型规则

所以不能简单把现有 MapActor 原封不动改名为 MapUnit。

## 2. MapActor 与用户提出的 Map Unit 概念基本一致，但 Army 已经形成另一套并行模型

现有 `Army` 位于：

`CivilizationEvolution/Assets/Scripts/Simulation/Warfare/Army.cs`

已确认 Army 同时具有：
- `armyId`
- `armyName`
- `ownerRealmId`
- `commanderId`
- `currentTileIndex`
- `unitCounts`
- CombatState
- organization
- morale
- supply
- training
- 军队移动/寻路相关逻辑

尤其 `unitCounts : Dictionary<int,float>` 明确属于军事构成，而不是 Map Unit 通用状态。

因此当前工程存在两套“地图上移动的东西”模型：

`MapActor.currentTile`

以及：

`Army.currentTileIndex`

同时两者都存在归属概念。

这正是后续迁移必须解决的结构重复，而不是再增加第三套 MapUnit 数据。

## 3. MapActor 的类型系统已经证明“地图单位不是只有军队”

当前 `MapActorType` 已经支持至少：
- Refugee
- Bandit
- Caravan
- Nomad

代码中存在：
- SettlementDestroyed → Spawn Refugee
- LandAbandonment → Spawn Refugee / Bandit
- AI 根据 MapActorType 行动
- MapActor 可独立于 Realm 存在

因此 Map Unit 作为独立概念是有真实代码依据的。

尤其 README 明确把 MapActor 描述为：
> 可以在地图上自主行动的单位，不一定隶属于任何政权。

这与当前架构讨论中的“Map Unit 可以有归属，也可以无归属”完全一致。

## 4. 但现有 MapActor 不应直接作为最终 Map Unit 模型

原因：
- Refugee 的 population 合理；
- Caravan 的 population/supplies 可能合理；
- Bandit 的 morale/supplies 可能合理；
- 但这些不是所有 Map Unit 的共同事实；
- AI 也不是 Map Unit 本体；
- 具体单位能力应该由各单位类型/领域提供。

因此最终应更接近：

Map Unit
→ 地图存在、位置、移动、归属、控制、领导、生命周期

具体 Unit Domain
→ 该单位自身的特殊状态和规则

例如：

Map Unit
└── Army Unit
     └── Military / Army

Map Unit
└── Refugee Unit
     └── Population / Migration

Map Unit
└── Caravan Unit
     └── Trade / Transport

Map Unit
└── Band Unit
     └── Bandit / Warfare / Survival

不要把这些领域状态全部塞回 MapUnit。

## 5. TileData 当前确实混入了 Distribution / World State

现有 TileData 至少包含：
- `provinceId`
- `regionId`
- `ownerRealmId`
- `occupyingRealmId`
- `populationBlocks`
- 地理/地形/气候等数据

其中：
- elevation / slope / sea-land / river 等更接近 Map 本体；
- ownerRealmId / occupyingRealmId 属于政治空间状态；
- populationBlocks 属于人口在空间上的状态；
- provinceId / regionId 需要进一步区分静态空间分区与政治/行政分区。

因此此前“TileData 是地图本体 + 全部世界状态”的现状确实需要在 Architecture 2.0 中逐步拆开。

## 6. 当前政治分布已经明确存在 Owner 与 Occupation 两层

`PoliticalSystem` 使用：
- `ownerRealmId`
- `occupyingRealmId`

并在控制变化时分别修改。

这说明未来的 Political Distribution 至少不能简单命名成一个 Owner 字段。

需要保留类似：

Political Ownership
Political Occupation / Control

二者不能混为一谈。

## 7. Population / Culture / Faith 的空间分布目前主要通过 PopulationBlock 表达

`PopulationBlock` 当前至少保存：
- count
- raceId
- cultureId
- faithId
- socialClass
- profession
- satisfaction

因此当前 Culture/Faith 的空间分布并不是独立的 CultureMap/FaithMap，而是人口块中的组成事实。

这意味着未来的 Distribution 层不能机械建立：

CultureDistribution
FaithDistribution

两个独立的权威数据源，否则会与 PopulationBlock 产生双重 Authority。

更合理的方向是：

PopulationBlock = 空间人口事实

Culture / Faith Distribution Query
= 从 PopulationBlock 派生

只有在未来证明某些文化/信仰存在“不依附于人口块”的独立空间状态时，才建立额外 Authority State。

## 8. Settlement 当前也是独立于 MapActor 的实体

现有 CampData 至少具有：
- tileIndex
- ownerRealmId
- ownerActorId
- population
- defense
- supplies
- morale
- establishedDay
- abandoned 状态

因此 Camp/Settlement 与 MapActor 存在关联，但不是 MapActor 本身。

这符合：

Settlement ≠ Map Unit

同时 `ownerActorId` 说明现有工程已经存在：

Settlement ← MapActor

这种归属关系，需要后续明确为 Owner / Founder / Controller / AssociatedUnit 中的哪一种，不能直接假设它就是政治所有权。

## 9. Title 的领域边界已经被代码直接确认

现有：
- `TitleDef`
- `TitleCatalog`
- `titleId`
- `Title/TitleCatalog.cs`
- `AdminDivision.titleId`
- `RealmTitleEvolution`

并且代码明确把 Title 用作：
- 官僚头衔
- 贵族头衔
- 君主头衔
- realmSuffix 等称号定义

因此 Title 必须严格保留给“头衔/名位”语义。

不能用于：
- Territory
- Political Distribution
- Map Distribution
- 地块归属

## 10. History 目前没有发现一个统一的 WorldHistory 数据模型

本轮搜索没有发现明确的 `WorldHistory` / `HistoryData` / `history.json` 统一模型。

现有 `history` 多为：
- CharacterRelation.history
- RegimeChangeState.history
- DiplomaticRelation.eventHistory
- UI historyText

另有 Historical Map 作为渲染概念。

因此当前不能直接断言“历史文件已经负责所有地图分布”。

更准确的结论是：

**目前工程尚未形成统一的 World History / Historical Distribution 权威模型。**

后续如果设计要求“历史文件负责初始世界分布与历史状态”，需要单独建立持久化/历史语义，而不是把现有零散的 `history` 字段统称为 WorldHistory。

## 11. 本轮最重要的架构结论

当前代码已经验证：

Map
≠ Map Unit
≠ Settlement
≠ Army
≠ Title
≠ Distribution

同时：

TileData
目前是一个混合容器，里面同时放了：
- Map 本体属性
- Political Distribution
- Population State
- 空间分区信息

MapActor
目前是最接近 Map Unit 的既有实现，但职责过载。

Army
目前同时拥有军事状态与 Map Unit 状态，需要后续拆分。

因此下一步不应该创建一个全新的抽象体系，而应该做：

**MapActor / Army / TileData 的职责迁移设计。**

优先顺序建议：

1. 明确 Map Unit 最小共同状态；
2. 从 MapActor 提取这些共同状态；
3. 从 Army 提取其地图存在部分；
4. 保留 Army 的军事构成/战斗/补给等军事状态；
5. 确认 Settlement 与 Map Unit 的 Owner/Controller/Association 语义；
6. 最后才决定正式目录结构。

本轮仍然不进行大规模代码迁移。
