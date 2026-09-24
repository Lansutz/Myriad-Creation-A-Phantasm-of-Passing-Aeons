# Architecture 2.0 — Domain Audit 06

## BurgData / Building / Structure 边界审计

本轮继续基于现有代码，不创建最终运行时框架。

## 1. BurgData 是目前最明显的“聚居点超级实体”

现有 BurgData 同时包含：

### 空间定位
- burgId
- provinceId
- tileIndex
- x / y

### 聚居点人口与经济状态
- population
- development
- wealth
- tradePower
- economicComposition
- primarySector

### 军事状态
- fortification
- garrison
- garrisonQuality
- garrisonQuantity

### 社会/功能状态
- isCapital
- isPort
- isCoastal
- hasMarket
- hasTemple
- hasUniversity
- primaryFunction
- secondaryFunctions
- cityFocus
- cityForm
- fortSubtype
- portTier
- bottleneckType
- upgradePath

### 聚居点类型与演化
- settlementCategory
- settlementType
- settlementLevel
- evolutionStage
- settlementEvolution
- settlementStability
- evolutionTarget
- ticksSinceLastTransition
- foundingTick
- parentBurgId

### 聚居点关系
- controllerBurgId
- controlledBurgIds
- controlProgress
- influenceRadius
- bottleneckTileIndex

### 防御构筑物
- wallLevel

### 废墟/摧毁
- ruinLevel
- ruinedDay
- preRuinLevel
- preRuinType
- recoveryProgress
- remnantCultureId
- remnantFaithId

结论：

**BurgData 不能直接改名为 Settlement。**

它必须拆成至少：
- Anchor / spatial location
- Settlement state
- Settlement definition / typology
- Settlement relations/control
- Fortification/Structure relation
- Ruin/recovery state

## 2. 三种 SettlementCategory 应保留，但不能与 SettlementType 混成一层

现有代码已经明确：

SettlementCategory:
- Burg = 定居点
- Outpost = 据点
- Camp = 营地

同时 SettlementType:
- Village
- City
- Fort

以及 BurgType:
- Village
- Town
- City
- Port
- Capital
- Fortress

所以当前存在三个分类轴。

规划上应暂时保留这些概念，先不全局重命名：

1. **SettlementCategory**：根本存在形式，正是三类“定居点/据点/营地”；
2. **SettlementTypology / SettlementDefinition**：具体聚居形态、功能和能力；
3. **SettlementLevel**：规模/发展等级。

后续再决定 BurgType 是应该删除、并入 Definition，还是转成 Definition 的内容 ID。

## 3. Anchor 应只吸收 BurgData 的空间身份

BurgData 的：
- tileIndex
- x
- y

是最明确的 Anchor 候选。

provinceId 不应直接进入 Anchor，因为 Province 是世界空间/政治结构，具体语义还需单独审计。

Anchor 最小目标状态应接近：

- anchorId
- mapCellId
- localPosition / map position
- anchor existence/lifecycle

Anchor 不应该保存：
- population
- settlementType
- settlementCategory
- wealth
- wallLevel
- building list
- realm ownership

## 4. Settlement 应吸收 BurgData 的社会状态

Settlement 应承担：

- settlementCategory
- settlementLevel
- population
- development
- wealth
- tradePower
- economicComposition
- primarySector
- functions
- typology/evolution state
- founding / parent relation
- ruin/recovery state

但不应直接承担 MapCell 坐标。

因此目标关系：

Anchor → Settlement

不是：

Settlement → 自己保存 tileIndex 作为空间真相。

## 5. 城墙必须从 Settlement State 中拆出

当前 BurgData.wallLevel 是典型混合字段。

它实际上表达的是：
“这个聚居点相关的防御构筑体系目前达到什么等级”。

因此不能简单删除。

目标应变成：

Settlement
→ associated fortification structure / fortification state

而 Structure 才保存：
- structure identity
- geometry / covered MapCells
- construction state
- strength / level
- owner/controller relation

单个聚居点城墙只是 Structure 的一个空间实例。

长城则可以完全独立存在。

## 6. Building 当前存在两个问题

### 问题 A：建筑直接挂 Tile

ActiveBuilding：
- buildingId
- tileIndex
- realmId
- constructionDays
- remainingDays
- completion state
- construction plan metadata

BuildingSystem：
- Dictionary<int, List<ActiveBuilding>> _tileBuildings

因此当前建筑的空间真相是 Tile。

但目标模型是：

Settlement
→ Buildings

所以迁移时应该把：
- tileIndex → settlement/anchor association

作为主要变化。

### 问题 B：BuildingSystem 把 Road / Defense 当普通建筑

现有 BuildingCategory 包含：
- Agriculture
- Craft
- Road
- Defense
- Market
- Admin

其中 Road 和部分 Defense 不符合 Building 的概念边界。

后续应从 Building Definition 中拆出：
- Road → Structure
- Wall → Structure
- GreatWall → Structure
- Bridge → 后续根据是否作为独立跨格构筑物确定，当前倾向 Structure

## 7. Road 的现有实现尤其需要重构

现有：
- TileData.roadLevel
- BuildingCategory.Road
- TradeRoute 按 TileData.roadLevel 计算效率
- MilitaryMovementSystem 使用 TileData.roadLevel
- BarrierSystem 使用 TileData.roadLevel
- Save 系统保存 roadLevel

这说明 roadLevel 已成为跨系统共享的事实。

但是它表达的是“某地块有道路等级”，而不是“道路实体”。

最终应区别：

**Road Structure**
- 道路实体；
- 有空间路径；
- 可以跨多个 MapCell；
- 可以连接多个 Anchor；
- 有类型、等级、维护、建设状态。

**Movement / Trade Derived Query**
- 查询某条移动路径经过的道路质量；
- 不把 roadLevel 再写成第二个独立权威。

迁移期间可以保留 TileData.roadLevel 作为兼容缓存/Derived State，但不应继续把它当最终权威。

## 8. Barrier 也不应该继续作为 Tile 的原始属性

TileData 当前：
- hasBarrier
- barrierOwnerRealmId
- barrierStrength
- nearbyFortId
- fortInfluenceLevel

其中：
- hasBarrier / barrierStrength = 构筑物/军事空间对象状态；
- barrierOwnerRealmId = 政治/控制关系；
- nearbyFortId / fortInfluenceLevel = 派生空间影响。

因此至少应拆成：

Structure / Fortification
+
Political Relation
+
Derived Fort Influence

不能让 MapCell 同时承担三者。

## 9. TileData 当前真正适合保留的核心

在现阶段，TileData 中最接近 MapCell + Geography 的字段是：

### 空间
- tileIndex
- exists

### 地形
- elevation01
- slopeDegree
- terrainShade

### 海陆/水文
- isLand
- isCoast
- oceanTier
- oceanDepth01
- seaConnectId
- waterAdjacentWeight
- isRiver

### 气候/生态
- annualTemp
- diurnalTempRange
- annualPrecipMm
- airHumidityPct
- soilHumidityPct
- accumulatedTemp
- frostFreeDays
- climateZone
- biome

### 通行的地理派生
- passable
- movementCost

但 movementCost 最终更像 Geography/Movement Derived，而不是原始地理。

## 10. TileData 中明显应该迁出的内容

### Politics
- ownerRealmId
- occupyingRealmId

### Population
- populationBlocks

### Settlement / Anchor association
- campId

### Economy / Society
- fertility（需确认是自然地理生产力还是经济状态；目前倾向 Geography-derived）
- development
- stability
- order
- buildingLevels

### Infrastructure / Structure
- roadLevel
- hasBarrier
- barrierOwnerRealmId
- barrierStrength
- nearbyFortId
- fortInfluenceLevel

### Region structure
- provinceId
- regionId

这几个需要单独决定究竟属于 Map 的静态空间分区，还是世界状态中的空间分布。

## 11. 地理动态变化的正式边界

现有代码已经有：
- WorldConfig.seaLevel
- SeaLandGenerator
- HydrologySystem

因此未来海侵/海退应该作为 Geography Change。

目标：

Geography Change
→ MapCell geographic state mutation
→ recompute Geography Derived
→ emit Domain Event
→ affected Anchor / Settlement / Structure / MapUnit / Population / Politics react

例如：

Sea Transgression：
Land → Ocean

Sea Regression：
Ocean → Land

不要让：
SettlementSystem
BuildingSystem
StructureSystem
PoliticsSystem

直接修改 isLand。

## 12. 当前规划

第一阶段：不改类名，只建立字段迁移表。

第二阶段：
- 将 BurgData 拆成 Anchor + Settlement；
- 将 ActiveBuilding 从 tile-centric 转为 settlement/anchor-aware；
- 将 Road / Wall / Barrier 从 Building / TileData 中抽离；
- 将 TileData 逐步收缩为 MapCell + Geography + 明确的 Derived Geography。

第三阶段：
- 引入真正的 Structure；
- 引入道路几何/路径；
- 引入可跨 MapCell 的 Wall / GreatWall；
- 处理 Structure 与 Settlement 的关联，而不是父子关系。

第四阶段：
- Geography Change；
- 海侵/海退；
- 地理重算与事件响应。

**当前不创建最终 Anchor / Settlement / Structure 类。**
