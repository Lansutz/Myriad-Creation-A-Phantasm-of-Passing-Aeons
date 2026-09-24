# Architecture 2.0 — Domain Audit 12

## 本轮范围

继续同步两条线：

1. main/README 后续章节的逐段状态核对；
2. TradeCenter → TradeRoute → Road → Fort → Gate → SettlementControl → MilitaryMovement 的代码事实核对。

本轮仍不创建最终 Domain 类。

## 1. TradeCenter：不是单纯的网络节点

当前代码存在独立 TradeCenter，并且：
- 以 regionId 为核心标识；
- 有 centerTileIndex；
- 有 inventory / inventoryCapacity；
- 有 localDemand / localSupply；
- 持有 TradeRoute 列表；
- EconomyManager 通过 TradeCenter 进行地区贸易与库存查询；
- CarryingCapacitySystem 使用 TradeCenter 粮食库存修正地区人口承载；
- SaveData 有 TradeCenterDTO，说明它已经是持久化世界状态。

因此 TradeCenter 当前属于经济实体 / 地区经济中心状态，而不是 Network 本身。

它可以作为 Network 的节点。

## 2. TradeRoute：已有真实 Network-like 实现，但没有证明存在通用 Network 抽象

当前 TradeRoute 至少包含：
- fromRegionId
- toRegionId
- nodeTileIndices
- baseEfficiency
- currentEfficiency
- isBlocked

Caravan 直接沿 TradeRoute 移动，Map Overlay 也有 TradeRoutes。

因此 TradeRoute 是当前已经存在的具体 Network-like domain object。

但当前代码没有证据证明已经存在完整、通用的 Network 基类/接口。

Architecture 2.0 应先承认 TradeRoute 是既有事实，再确定不同 Network 的共同语义，而不是立即制造 GenericNetwork。

## 3. TradeRoute 当前存在明显的旧 MapCell 耦合

TradeRoute 使用 nodeTileIndices，TradeCenter 使用 centerTileIndex。

这说明现有贸易系统仍以 TileData 为主要空间索引。

Architecture 2.0 目标应该逐渐变成：

TradeCenter → Anchor / Settlement / Region relation

TradeRoute → Network path / nodes / links

MapCell → 空间定位与地理查询

而不是让 MapCell 继续成为经济系统的万能空间容器。

## 4. Road：当前实现明确仍属于 BuildingSystem 的旧模型

当前 BuildingSystem 对 BuildingCategory.Road 直接修改 TileData.roadLevel。

这说明旧设计里的“道路作为建筑/建设项目”仍然存在。

但这与当前 2.0 设计不一致。

道路更适合：

Structure:
- RoadSegment

Network:
- RoadNetwork

Settlement / Anchor:
- Road connection endpoint

MapCell:
- 几何/空间穿越关系

因此现有 BuildingCategory.Road → TileData.roadLevel 应记录为旧实现事实 / 待迁移，而不是 Architecture 2.0 目标。

## 5. Fort：代码中已经出现 Settlement + Fort influence 双层模型

这是关键证据。

BurgData/BurgType/SettlementType 中存在：
- BurgType.Fortress
- SettlementType.Fort

而 MilitaryMovementSystem / BarrierSystem 又明确区分：

Barrier / Gate：
- 狭窄通道；
- 直接阻挡敌对通行。

Fort：
- 不直接阻挡；
- 对周边区域产生影响；
- 敌对时增加损耗/成本；
- 己方提供补给/支援。

所以当前代码实际上已经意识到 Fort ≠ Barrier。

Architecture 2.0 应继续保留这一语义区分。

但是 Fort 当前仍被编码成 SettlementType / BurgType，因此 Fort 的军事功能/区域影响需要进一步从 Settlement Typology 中拆出来。

## 6. Gate / Barrier：当前仍直接写入 MapCell

当前 TileData 有：
- isGate
- hasBarrier
- barrierStrength
- barrierOwnerRealmId

BarrierSystem 会直接写这些字段，MilitaryMovementSystem 再读取。

现状：

BarrierSystem
→ TileData 状态

MilitaryMovement
→ TileData query

Architecture 2.0 目标：

Gate / Barrier / WallSegment
→ Structure / Network node/link

Control / Ownership
→ Political relation

MilitaryMovement
→ Query

MapCell
→ 空间位置与地理条件

其中 isGate 尤其不能继续作为纯 MapCell 地理属性。

## 7. SettlementControl：必须与 MapCell ownership 继续拆开

现有体系同时存在：
- ownerRealmId
- occupyingRealmId
- settlement control / fort influence / regional influence

因此“谁拥有这个格子”和“谁控制一个 Settlement / 通道 / 区域”不是同一个概念。

Architecture 2.0 至少保持：

Political Ownership
≠ Military Occupation
≠ Settlement Control
≠ Passage Control
≠ Regional Influence

这些可以最终通过 Distribution / Relation / Query 表达。

## 8. MilitaryMovement：已经是综合查询逻辑

当前系统综合：
- 地形；
- 坡度；
- 海拔；
- Barrier；
- Fort influence；
- roadLevel；
- ownership/control 等条件。

因此它本身不是世界状态实体。

目标：

MovementContext
+
Map/Geography Query
+
Structure/Network Query
+
Political/Control Query
+
Unit State
→ MovementResult

MovementResult 可以包含：
- 是否可通行；
- 移动成本；
- 速度修正；
- 损耗；
- 补给；
- 战斗支援等。

## 9. README 后续章节状态

本轮继续确认 README 中：
- 音频明确写为“比较基础”，属于当前已有但未完成的系统；
- AI 系统完善、模组工具等被放在后续开发路线，不能当作当前实现缺口直接要求架构完成；
- README 后半部分仍需按照“当前设计 / 已实现 / 未来计划”拆分。

## 10. 当前边界草图

Map
└─ MapCell
   └─ Geography / Hydrology / Terrain / Climate

MapCell
└─ Anchor
   └─ Settlement
      ├─ Buildings
      └─ Manors

MapCell
└─ Structure / spatial segments

Anchor / Settlement / Gate / Outpost
└─ Network participation
   ├─ TradeRoute
   ├─ RoadNetwork
   └─ FrontierDefenseNetwork / GreatWall

TradeCenter
└─ Economy entity / regional market state

Fort
└─ Settlement military role + Fortification / Influence state

Political / Control
├─ Ownership
├─ Occupation
├─ SettlementControl
├─ PassageControl
└─ RegionalInfluence

MilitaryMovement
└─ Query / Result

## 11. 下一步

继续逐文件检查：
- TradeCenter / TradeRoute 的完整字段；
- Road/Building 旧迁移路径；
- Fortification / Barrier / Gate；
- SettlementControl 的实际控制算法；
- MilitaryMovement 的全部输入字段；
- README 剩余章节。

在这些证据完整后，再开始正式 Field Migration Matrix。
