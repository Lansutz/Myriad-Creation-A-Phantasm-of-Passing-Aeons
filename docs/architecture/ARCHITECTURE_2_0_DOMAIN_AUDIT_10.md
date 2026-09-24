# ARCHITECTURE 2.0 DOMAIN AUDIT 10 — Plan / Construction / Network Boundary

> 分支：`architecture-2-0-refactor`  
> 本审计继续以当前重构分支的实际文件为准；本轮只审计，不创建最终领域类。

## 1. 本轮关键修正：Plan 不是国家级工程容器

当前 `PlanSystem` 的语义是：

- `Plan` = 持续存在的意图 / 工作过程；
- `initiatorId`、`participants`、`purpose`、`targetId` 等字段明显允许以个人/角色为中心；
- `PlanType` 当前包含 Research、Intrigue、Engineering、Construction、Exploration、Trade、Diplomacy、Migration、Military、Custom；
- `PlanSystem` 本身只负责生命周期和调度，不理解具体领域规则。

这套抽象更接近**个人/主体发起的计划与行动意图**，而不是国家级公共工程管理器。

因此，后续不要为了 Great Project 把 `PlanSystem` 无限扩大。

## 2. ConstructionPlanSystem 当前存在的语义错位

当前：

`CreateConstructionPlan(realmId, initiatorId, tileIndex, buildingId, ...)`

流程是：

1. 检查 Realm 是否拥有 Tile；
2. 创建 `PlanType.Construction`；
3. 把 treasury / construction_days 写入 PlanRequirement；
4. 可选加入 builderCharacterId；
5. 调用 `BuildingSystem.StartPlannedBuilding`；
6. PlanSystem 驱动 `AdvancePlannedConstruction`；
7. 建筑完成后修改 TileData。

这说明它实际管理的是：

**“一个主体发起的一次单体建筑施工意图”**

而不是“Construction Plan”作为普适国家工程系统。

### 结论

保留 PlanSystem 的情况下，ConstructionPlan 可以存在，但它应被理解为：

> **主体对某个局部施工活动的计划/意图。**

不能让它成为 GreatProject 的父类，也不能让 GreatProject 依赖“一个 Plan = 一个大型工程”。

## 3. Engineering 与 Construction 目前没有真正形成两个领域

`PlanType.Engineering` 当前只是枚举值；代码中实际建造流程使用的是：

`PlanType.Construction`

因此目前不能把 Engineering 理解成已经存在的“大型工程系统”。

后续如果需要 Engineering：

- Engineering 可以表示工程设计、勘测、技术准备等主体活动；
- Construction 可以表示具体施工计划；
- GreatProject 则是独立的长期公共工程 / 国家级工程机制。

三者不应通过枚举层级强行继承。

## 4. BuildingSystem 的边界问题

当前 BuildingSystem 仍以：

`Dictionary<int, List<ActiveBuilding>> _tileBuildings`

作为建筑容器，建筑通过 `tileIndex` 挂在 MapCell/Tile 上。

而目标架构已经确定：

**Building → Settlement**

因此 ConstructionPlan 当前的：

`tileIndex + buildingId`

只是旧空间定位方式。

未来应该变为：

`ConstructionIntent → Settlement/BuildingTarget`

再由 Anchor/MapCell 提供空间解析。

这里不要把“施工计划”直接等同于“建筑实体”。

## 5. 计划、活动、过程三层应保持分离

当前 Plan 已经拥有：

- state
- phase
- progress
- activity
- waitCondition
- resultCode

这容易再次把 Plan 变成万能 Process。

本轮审计后的边界：

### Plan
回答：

> 谁想做什么？

典型对象：

- 角色研究某项革新；
- 角色执行阴谋；
- 角色主持一项局部建设；
- 主体安排一次迁移/探索。

### Activity
回答：

> 当前正在进行哪一个具体活动？

例如：

- 勘测；
- 设计；
- 施工；
- 等待材料；
- 验收。

### Process
回答：

> 如何让持续过程随模拟时间推进？

Process 是运行机制，不是世界对象。

### Project / GreatProject
回答：

> 一个跨时期、跨地点、具有治理和资源组织能力的大型工程本身是什么？

因此 GreatProject 不应塞入 Plan 的生命周期字段。

## 6. GreatProject 的正确位置

目标关系应更接近：

`Political/Institutional主体`
→ `GreatProject`
→ `Activities / Processes`
→ `Structures / Networks / Settlement Effects`

而不是：

`Plan`
→ `GreatProject`

GreatProject 至少需要独立表达：

- sponsor / governing authority；
- project definition；
- spatial scope；
- phases；
- worksites；
- funding；
- labour；
- materials；
- logistics；
- engineering capacity；
- progress；
- interruption；
- abandonment；
- partial completion；
- rerouting / redesign；
- final world-state result。

这也符合长城的实际模型。

## 7. Great Wall 的工程生命周期

长城应形成：

`GreatProject`
→ 施工阶段
→ 若干 Structure / Route / Gate / Outpost 节点
→ `FrontierDefenseNetwork`

注意：

**GreatProject 是生命周期对象；Great Wall 是完成后持续存在的世界系统。**

例如：

- 项目暂停：GreatProject 仍存在，网络可能部分完成；
- 工程改线：项目修改未完成区段；
- 一段建成：产生新的 WallSegment / Gate / Route；
- 项目取消：已经完成的部分不能简单从世界状态抹除；
- 后续维护：应进入 Network/Structure maintenance，而不是重新变成原 Project。

## 8. TradeRoute 当前已经暴露了 Network 的雏形

当前 TradeRoute：

- fromRegionId
- toRegionId
- nodeTileIndices
- baseEfficiency
- currentEfficiency
- isBlocked

并且 Caravan 通过 currentNodeIndex 沿 nodeTileIndices 移动。

这说明项目已经存在：

> **节点 + 路径 + 通行状态 + 流量效率**

的网络模型雏形。

问题是节点仍然直接使用 TileIndex。

目标应逐步转为：

`Network`
→ Node references
→ Edge/Segment references
→ Path/route state
→ derived efficiency

其中 Node 可以是：

- Anchor；
- Settlement；
- TradeCenter；
- Gate；
- Port；
- Outpost；

而不是只能是 MapCell。

MapCell 可以作为空间底层，但不应该成为所有 Network 的唯一节点。

## 9. TradeRoute 的 currentEfficiency 应视为 Derived State

当前 `CalculateEfficiency(TileData[] tiles)` 根据：

- roadLevel；
- stability；
- node 数量；

计算 currentEfficiency。

因此：

- `baseEfficiency` 可以是路线定义/基础状态；
- `currentEfficiency` 更接近 Derived State；
- `isBlocked` 是路线当前状态，但其原因应该来自阻断关系/结构/政治/灾害等领域事实。

未来不要让 TradeRoute 自己成为道路基础设施的唯一权威。

## 10. Barrier / Gate / Fort 必须进一步拆开

当前 BarrierSystem 把多个概念集中在一起：

- 地形可通行性；
- 基础 movementCost；
- Barrier；
- Gate；
- Fort influence；
- Fort ownership。

尤其当前 TileData 有：

- hasBarrier
- barrierOwnerRealmId
- barrierStrength
- isGate
- nearbyFortId
- fortInfluenceLevel

这些不应继续作为 MapCell 的最终权威。

目标：

### Structure
表达物理构筑物：

- WallSegment
- Gate
- Bridge
- RoadSegment
- Fortification element

### Settlement / Fort
表达堡垒作为聚居点/军事据点本身。

### Network
表达：

- Great Wall / FrontierDefenseNetwork；
- RoadNetwork；
- TradeRoute；
- 其他跨节点连接体系。

### Political / Control Relation
表达：

- 谁控制；
- 谁允许通过；
- 谁拥有通行权；
- 谁被阻挡。

### Movement Query
最终根据上述事实计算：

- 是否可通行；
- movement cost；
- attrition；
- supply；
- military support。

## 11. 一个特别重要的边界：人工设施 ≠ Structure

本轮再次确认：

“人工制造”不能直接成为 Structure 的定义。

例如：

- Canal：如果它作为持续存在的水文网络参与地理系统，可以属于 Hydrology/Geography；
- Road：作为交通设施属于 Structure / Network；
- Great Wall：作为跨区域防御体系属于 Network/System；
- Great Project：是建设生命周期；
- Wonder-like monument：是大型建设成果/项目，而不是 Network。

所以分类标准应是：

> **它在世界中承担什么本体角色，而不是它是不是人工制造。**

## 12. Save / Persistence 风险

当前 SaveData 仍直接保存：

- TileData[]
- TradeCenterDTO
- TradeRoute
- WarState

而 ConstructionPlan / Plan 没有进入 SaveData 的明确持久化边界。

这暴露一个后续问题：

### 短期主体 Plan
如果 Plan 只是短生命周期个人意图，可以选择不作为长期世界核心数据保存，或单独持久化其运行状态。

### GreatProject
必须可持久化，因为：

- 可能持续多年；
- 可能跨多个模拟阶段；
- 可能暂停；
- 可能部分完成；
- 可能改变路线；
- 可能留下已经完成的世界实体。

因此 GreatProject 必须进入独立 Save DTO，而不能依赖 PlanSystem 的内部 Dictionary。

## 13. 当前领域边界暂定图

```
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

Anchor / Settlement / Gate / Port / Outpost
 └─ Network
     ├─ RoadNetwork
     ├─ TradeRoute
     └─ FrontierDefenseNetwork / GreatWall

主体 / 政府 / Realm
 └─ GreatProject
     ├─ Activities
     ├─ Worksites
     ├─ Funding / Labour / Materials
     └─ produces / modifies → Structure / Network / Settlement

Character /主体
 └─ Plan
     └─ Activity / Process
```

## 14. 本轮结论

最重要的不是新增一个 GreatProject 类，而是先确定：

**PlanSystem 不能吞掉 ProjectSystem。**

当前 PlanSystem 继续作为“主体意图 / 个人计划 / 局部持续行动”的统一生命周期层是合理的。

而：

- Local Construction → ConstructionPlan / Activity；
- Realm-scale Great Project → 独立 GreatProject；
- Physical works → Building / Structure；
- Cross-location connection → Network；
- Great Wall → GreatProject 产生的 FrontierDefenseNetwork。

下一轮继续审计 **TradeCenter / Road / Fort / Gate / SettlementControl / MilitaryMovement** 的真实关系，并开始形成正式的 **Domain Boundary + Field Migration Matrix**；仍不直接创建最终领域类。
