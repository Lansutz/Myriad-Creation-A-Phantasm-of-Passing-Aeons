# Architecture 2.0 Domain Audit 14

## 本轮范围

本轮继续对 SettlementControl、MilitaryMovement、TradeCenter/TradeRoute、Road/Building 以及 README 尾段进行证据审计。目标仍然是先确定领域边界和字段归属，不提前创建最终 2.0 领域类。

## 1. SettlementControl：代码证明了“控制关系”而不是 MapCell 属性

当前 SettlementControlSystem 的核心数据仍挂在 BurgData：

- controllerBurgId
- controlledBurgIds
- controlProgress
- isControlStable
- garrisonQuantity / garrisonQuality
- influenceRadius（由 settlementLevel 派生）

算法流程为：

1. 根据聚落等级排序；
2. 高等级、未被控制的主要聚落扫描影响半径；
3. 只在同一 ownerRealmId 下建立候选控制关系；
4. 以驻军数量、质量、等级差推进 controlProgress；
5. 无驻军时进度衰退；
6. 达到阈值后写入 target.controllerBurgId 与 isControlStable；
7. 控制关系还会通过 GetTaxSiphonModifier / GetTradeSiphonModifier 影响经济。

这进一步确认：SettlementControl 不是 MapCell 的 ownership/control 字段。

它是 Settlement ↔ Settlement 的政治/行政/影响关系，并且有 source/controller、target/controlled settlement、progress、stability、military enforcement、spatial influence query、economic consequence。

MapCell 只应提供空间与地理查询能力；政治系统通过控制关系查询得到某地的有效控制状态。

### 当前实现中的技术债

SettlementControlSystem 目前仍直接依赖 BurgData、TileData、tileIndex、ownerRealmId、Army，因此它目前是“正确领域语义 + 旧数据边界”的典型过渡层，而不是 2.0 最终实现。

另外，代码中的 suzerainOccupied 当前被固定为 false，说明“宗主被占领导致控制衰减”的设计已有模型意图，但当前实现并未真正完成该输入。

## 2. MilitaryMovement：应定位为 Movement Query / Resolver

当前 MilitaryMovementSystem 没有成为地图状态拥有者，而是在计算：

- 是否可通行
- 实际移动成本
- 关隘封锁
- 敌对堡垒影响
- 己方堡垒影响
- 补给/士气等军事结果

当前输入主要来自 TileData：exists / passable / movementCost / hasBarrier / barrierOwnerRealmId / fortInfluenceLevel / roadLevel，并额外接受 armyRealmId、战争关系、堡垒所有者等上下文。

因此 2.0 应将它定义为 MovementQuery / MovementResolver，而不是 MapCell、Army 或 Structure 的状态容器。

目标输入边界：Unit/Army + From/To MapCell + Geography + Structure/Network + Political/Control + Supply。

目标输出至少应拆成可组合结果：Passability、MovementCost、SpeedModifier、Attrition、SupplyModifier、CombatSupport、PassageRestriction。

其中 PassageRestriction 应解释 Barrier/Gate/政治通行权，而不是让 MapCell 保存“最终能不能走”。

## 3. TradeCenter：经济实体，空间位置只是关系

TradeCenter 已有独立运行时对象与 TradeCenterDTO，并进入 SaveData：regionId、centerName、centerTileIndex、inventory、inventoryCapacity、localDemand、localSupply、tradeRoutes。

因此 TradeCenter 是真正的持久化经济实体，而不是 Map/Network 的别名。

字段分类：

| 字段 | 2.0 归属 |
|---|---|
| regionId | Economy ↔ Region relation |
| centerName | Economy identity/content |
| centerTileIndex | Settlement/Anchor/Map spatial relation，过渡字段 |
| inventory | Economy state |
| inventoryCapacity | Economy state/rule |
| localDemand | Economy derived/state |
| localSupply | Economy derived/state |
| tradeRoutes | Economy ↔ Network relation |

目标不是删除 TradeCenter，而是解除它对 tileIndex 的空间硬编码。

## 4. TradeRoute：已有具体 Network-like 实体，但暂不创建泛型 Network

当前 TradeRoute 有 fromRegionId、toRegionId、nodeTileIndices、baseEfficiency、currentEfficiency、isBlocked。

这足以证明存在具体的路线/网络对象，但不足以证明需要一个泛型 Network<T>。

2.0 先保留 TradeRoute，并把它理解为具体的经济网络关系。

字段分类：fromRegionId / toRegionId → endpoint relation；nodeTileIndices → path/link spatial relation，未来转 Anchor/MapCell 路径；baseEfficiency → definition/base state；currentEfficiency → derived state；isBlocked → derived/query result，而不是永久权威布尔值。

阻塞最终应由战争、控制、损坏、关隘、地理变化、禁运/通行限制等事实推导。

## 5. Road：当前仍错误地落在 Building → TileData

当前 BuildingSystem 仍定义土路、石砌路、帝国大道、桥梁，并把它们放入 BuildingCategory.Road。

建成后直接修改 TileData.roadLevel，MilitaryMovement 又直接读取 roadLevel。

这形成 Building → MapCell state → Movement，而 2.0 目标应是 RoadSegment / Bridge → RoadNetwork → MovementQuery。

因此 Road 不能简单“换一个枚举名”。

尤其 Bridge 与普通道路并不完全等价：RoadSegment 是路线基础设施；Bridge 是跨越水体/峡谷等空间障碍的结构；RoadNetwork 是连接关系；MovementQuery 决定具体移动成本。

当前 BuildingSystem 应保留兼容层，直到 Settlement/Structure/Network 的正式迁移阶段再拆。

## 6. Fort / Barrier / Gate

本轮继续确认三者不能合并。

Barrier / Gate 属于 Structure / passage-control 层，核心语义是是否阻挡、谁控制、是否需要攻破、是否允许通行、防御加成。

Fort 更接近 Settlement 的军事角色 + Fortification/Influence。它不直接等价于 Barrier。当前代码中 Fort 仍被压缩进 SettlementType/BurgType 与 TileData.fortInfluenceLevel，因此需要后续拆出 Fortified settlement / Fort role、Fortification state、Regional influence、Movement effects。

Great Wall 仍保持此前结论：不是普通 Structure，也不是普通 Fort。应由大型工程项目产生 GreatProject → FrontierDefenseNetwork / GreatWall。

## 7. README 尾段的状态分类

当前 README 明确写了大量“已实现/已搭好”的表述，但这些不能自动作为 2.0 实现证据。

特别是：

- README 顶部的“27 个 C# 文件”明显属于旧版本项目描述，不能覆盖当前已扩展代码规模；
- README 的旧模块目录结构不能当作当前 asmdef/代码目录的精确事实；
- README 所列 Innovation 的“研究点/速率/完成效果”是旧设计描述，不能覆盖当前 InnovationKnowledge + CharacterResearch + ResearchPlan 架构；
- README 所列“每地块建筑上限 5”等旧规则仍需以当前代码为准；
- README 的待完善列表属于历史状态，不能直接解释当前架构缺失。

因此 README 的证据等级继续保持：历史设计/产品叙述 > 具体代码事实。两者冲突时，应记录冲突，而不是强行把 README 当成当前实现。

## 8. 本轮边界结论

目前可以进一步固定以下 2.0 边界：

- MapCell → 地理/水文/地形/气候/生物群系/空间索引
- Anchor → 空间存在点与生命周期
- Settlement → 定居实体及其人口、发展、功能、军事角色等状态
- Building → 聚落内部局部建筑
- Structure → 跨空间或独立于建筑的实体结构，如 RoadSegment / WallSegment / Gate / Bridge
- Network → 连接节点与链接的网络关系；但暂不创建泛型抽象，先以 TradeRoute / RoadNetwork / FrontierDefenseNetwork 等具体类型落地
- Political Relation → ownership / control / occupation / passage rights / influence 等关系，不回写成 MapCell 单字段
- Movement Query → 对地形、道路、关隘、堡垒、控制和补给进行综合计算
- Economy → TradeCenter / inventory / supply-demand / TradeRoute economic flow
- Innovation → 社会知识 + 个人掌握/实践 + ResearchPlan 的分层系统，不恢复旧线性研究点模型

## 9. 下一阶段

在正式 Field Migration Matrix 之前，还需要继续补齐：

1. MilitaryMovementSystem 全部方法的输入/输出及直接写状态情况；
2. TradeRoute/EconomySystem 的完整读写关系；
3. Road/Bridge/Barrier/Fort 的全部引用；
4. Innovation 当前所有权与 ResearchPlan/CharacterResearch 的边界；
5. README 设计与当前代码的最终冲突清单。

完成这些证据后，再建立 Domain Boundary Matrix + Field Migration Matrix。

此阶段仍不创建最终 SettlementControl 2.0、MovementResolver 2.0、Network 基类或 GreatProject 正式类。
