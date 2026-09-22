# Architecture 2.0 — Domain Audit 13

## 本轮重点：Settlement Control 与 README 继续同步

### 1. SettlementControl 的核心语义已经非常明确

现有 SettlementControlSystem 的注释明确描述一种“城市节点控制”模式：

- 早期文明不是通过连续、明确的边界控制所有土地；
- 高等级城市节点可以控制周围较低等级聚落；
- 控制通过节点关系、控制进度等机制逐渐形成；
- 税收与贸易优先等“虹吸效应”作为控制结果的一部分。

因此 SettlementControl 不是 MapCell.ownerRealmId 的简单替代。

更准确地说，它描述：

Settlement / Anchor → 对其他 Settlement / 区域产生控制关系

而不是：

Realm → 直接拥有某个 MapCell

这进一步支持此前的边界：

Political Ownership
≠ Settlement Control
≠ Regional Influence

### 2. 当前 SettlementControl 仍然是旧数据模型驱动

SettlementControlSystem 的现有接口仍然接收：

- Burgs
- Tiles
- MapWidth
- MapHeight
- Armies

并由 SettlementSimulationSystem 每日调用。

这说明它虽然已经被放到 scheduler-facing domain boundary 后面，但内部仍然直接依赖旧 BurgData / TileData 世界模型。

所以目前状态应标记为：

架构边界已开始整理，但 Domain Authority 尚未完成迁移。

不能因为有 SettlementSimulationSystem 就认为 Settlement Domain 已经完成 2.0 化。

### 3. SettlementControl 的下一步不是“搬到 MapCell”

不能采取：

SettlementControl → MapCell.controlRealmId

这种迁移。

应该保留关系语义，例如：

ControlRelation
- controller
- controlled target
- control strength/progress
- scope
- source
- legitimacy / enforcement（未来如需要）
- effective period（未来历史系统需要）

而 MapCell 只通过 Query 获得：

- 当前实际控制者；
- 是否受某 Settlement 辐射；
- 通行是否受控制影响；
- 区域控制强度。

### 4. MilitaryMovement 的定位进一步确认

MilitaryMovementSystem 当前明确把以下因素组合起来：

- 地形与坡度；
- 极难通行地形；
- Barrier；
- Fort influence；
- roadLevel；
- 控制/所有权相关因素。

因此它不是“军事单位移动状态”的持有者。

它应该最终成为类似：

MovementQuery / MovementResolver

输入：

- Unit / Army state
- From / To MapCell
- Geography
- Structure / Network
- Political / Control
- Supply context

输出：

- Passability
- MovementCost
- SpeedModifier
- Attrition
- SupplyModifier
- CombatSupport

这样既能服务 Army，也能服务 Caravan、Refugee、Nomad、CivilianMigration 等其他 Map Unit。

### 5. TradeCenter 的边界

当前 TradeCenter 至少同时承担：

- regionId
- centerName
- centerTileIndex
- inventory
- inventoryCapacity
- localDemand
- localSupply
- tradeRoutes

其中真正属于 TradeCenter 自身经济状态的是：

- inventory
- capacity
- demand
- supply

而：

- centerTileIndex
- tradeRoutes

更像空间/网络关系。

因此未来迁移不应该简单复制 TradeCenter 类，而应拆成：

TradeCenter
→ EconomicMarket / regional economic center state

Spatial relation
→ Anchor / Settlement / Region

Network relation
→ TradeRoute / TradeNetwork

### 6. TradeRoute 的边界

当前 TradeRoute 的核心字段：

- fromRegionId
- toRegionId
- nodeTileIndices
- baseEfficiency
- currentEfficiency
- isBlocked

其中：

fromRegionId / toRegionId
属于端点关系；

nodeTileIndices
属于旧空间路径表达；

baseEfficiency
属于定义/基础属性；

currentEfficiency
属于 Derived State；

isBlocked
不应长期作为无来源的第二真相。

未来更合理的模型是：

TradeRoute
→ endpoints + path/links + base properties

TradeRouteState
→ derived efficiency / blockage / flow

Blockage 应由战争、控制、基础设施损坏、关隘状态、地理条件、禁运/限制等事实经过 Query 计算，而不是长期依赖手工写入的 bool。

### 7. Road 的迁移优先级进一步提高

现在 Road 同时存在于：

- BuildingCategory.Road
- TileData.roadLevel
- MilitaryMovementSystem 的移动成本计算

因此 Road 是一个典型的跨域旧耦合点：

Building
→ Road
→ MapCell
→ MilitaryMovement

2.0 不应该继续扩大这个耦合。

目标：

RoadSegment / RoadNetwork
→ Structure / Network

Movement Query
→ 查询道路质量

Economy / Trade
→ 查询道路连接能力

Settlement
→ 作为道路端点或节点

### 8. Fort / Barrier / Gate 的最终边界越来越清楚

当前代码已经明确表达：

Barrier/Gate：
- 直接控制狭窄通道；
- 可以阻挡敌对单位。

Fort：
- 区域影响；
- 不直接等价于通行阻挡；
- 可以改变敌对单位成本/损耗；
- 可以为己方提供补给/支援。

因此不能建立：

Fort → Barrier 的继承关系。

更接近：

Settlement
└─ MilitaryRole / Fortification

Structure
└─ Gate / Barrier / WallSegment

Network
└─ FrontierDefenseNetwork / GreatWall

Movement Query
└─ 综合上述影响

### 9. README 状态规则继续保持

README 中的 AI、模组工具等属于开发路线时，必须标记为：

Future Plan

不能因为目录或少量类已经存在，就把 README 的整段愿景判断为“已实现”。

同理：

README 中已经描述的系统，如果当前代码存在但模型已发生升级，应标记：

Historical Design → Upgraded → Current Model

而不是直接回填旧 README 模型。

### 10. Innovation 同步规则

本轮没有改变 Innovation 的判断：

Innovation 必须继续按照：

旧 README 模型
→ 后续明确升级
→ 当前 InnovationDef / Field / Domain / Tree / Progress / Knowledge
→ CharacterResearch
→ ResearchPlan 边界

进行审计。

特别是：

不能因为 README 中存在旧的 Innovation 分类，就把当前 Innovation 恢复成旧的线性/二级分类模型。

Innovation 仍然是本次 Architecture 2.0 审计中的独立主域。

## 当前阶段结论

目前还不进入最终 Field Migration Matrix。

原因是还有三个证据面需要继续收齐：

1. SettlementControl 的完整控制算法；
2. MilitaryMovement 的完整输入/输出；
3. README 剩余章节逐段分类。

完成后再把字段正式归类为：

- Map-native
- Settlement
- Structure
- Network
- Political relation
- Economy
- Population
- Derived
- Query input
- Compatibility / legacy

之后才进入实际迁移。
