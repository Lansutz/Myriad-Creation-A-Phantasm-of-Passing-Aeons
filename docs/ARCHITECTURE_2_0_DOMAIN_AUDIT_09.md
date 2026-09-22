# Architecture 2.0 — Domain Audit 09

## 大型工程（Great Project）与现有 Construction / Barrier / Route 边界

### 1. 长城应进入“大型工程”领域，但“大型工程”不是长城专属

本轮进一步确认：长城不是普通建筑，也不是普通 Structure。它属于一个由政权组织、长期投入、跨区域建设、完成后产生系统性效果的**大型工程（Great Project）**。

大型工程应是一个上位机制，而长城只是其中一种工程类型。

未来可能包括：长城/边疆防御体系；超大型运河或水利体系（若未来设计将其作为工程建设行为；运河完成后的水体仍属于 Geography/Hydrology）；大型道路/交通工程；跨区域灌溉体系；巨型港口/海防工程；其他需要政权级组织能力的工程。

注意：大型工程的“工程项目”与最终产生的空间实体/网络不能混为一谈。

### 2. 建筑 ConstructionPlan 与 GreatProject 应明确分层

现有 ConstructionPlanSystem 接受 realmId / initiatorId / tileIndex / buildingId，创建 PlanType.Construction，直接调用 BuildingSystem.StartPlannedBuilding，施工推进由 BuildingSystem 执行。

这实际上是**单个建筑施工计划**。它不适合直接扩展成“长城施工”。

目标应为：
ConstructionPlan → 单一/局部 Construction Target
GreatProject → 政权级、跨区域、长期、多阶段项目

例如 GreatProject: GreatWall 可包含 Project Governance、Funding、Labor / Manpower、Material Logistics、Engineering Capacity、Multiple Phases、Multiple Worksites、Spatial Network、Completed Effects。

### 3. 大型工程本身不是最终空间对象

应区分：
GreatProject = 正在组织/建设的工程项目及其生命周期。
GreatProjectResult / Network = 完成后形成的长期世界对象。

例如：GreatProject → 建设若干 WallSegment → 建立/升级 Outpost → 建立 Gate → 打通 Military Route → 形成 GreatWall Network。

因此 GreatProject ≠ GreatWall，而是 GreatProject → produces / transforms → GreatWall Network。

这也允许大型工程失败、中止、分期完成或改道。

### 4. Great Project 与 Wonder 不是同一个等级概念

用户明确要求：长城比普通 Wonder/奇观建筑还要大型。

因此不要设计成 Wonder → GreatWall。

更合理：Major Construction / GreatProject → Wonder-like Monument Project / GreatWall / FrontierDefenseNetwork Project / Mega Infrastructure Project / Other Realm-scale Projects。

“奇观”更像完成后的特殊建筑/文化对象；GreatProject 描述的是**政权组织的超大型工程机制**。二者甚至可以没有继承关系。

### 5. 当前代码已有 Plan 层，但还没有 GreatProject

仓库中存在 PlanSystem、Activity/Process、ConstructionPlanSystem，以及 PlanType.Engineering / PlanType.Construction，但没有专门的 GreatProject / MegaProject / Wonder 系统。

因此现在不要为了长城临时增加一个巨大类。应该先把工程项目层定义清楚，再接入现有 Process/Plan 基础设施。

### 6. Barrier 是未来迁移的样本

现有 BarrierSystem 直接写 tile.hasBarrier、tile.barrierOwnerRealmId、tile.barrierStrength、tile.isGate；MilitaryMovementSystem 直接读取这些字段。

这说明当前 Barrier = 空间对象 + 政治关系 + 军事效果，全部塞进 TileData。

未来至少应拆成：Barrier / Gate / WallSegment + Ownership / Control relation + Military Movement Query。

GreatWall Network 再把多个节点/连接组织起来。

### 7. Road 已证明 Network 与 Structure 要分开

TradeRoute 当前保存 fromRegionId、toRegionId、nodeTileIndices、baseEfficiency、currentEfficiency、isBlocked，已经是一个“节点序列 + 路线”的网络对象。

但它直接通过 nodeTileIndices 读取 tile.roadLevel。

未来应变成：RoadNetwork / Route → Path / Links → RoadSegment。
TradeRoute 查询道路条件得到 currentEfficiency；TradeRoute 不应该成为道路本身。

### 8. MilitaryMovement 已体现 Query 方向

MilitaryMovementSystem 当前读取 movementCost、roadLevel、barrier、fort influence、political movement control，并计算是否可通过、实际移动成本、损耗、补给恢复、士气恢复和战斗支援。

这些规则应逐渐成为 Movement Query + Military Effects，而不是继续把军事规则写进 MapCell。

### 9. 大型工程最小共同模型

暂定只定义概念，不创建代码：

GreatProject：projectId、projectType、sponsorRealmId、initiatorId、status、startDate、expectedCompletion、phases、worksites、funding、labor、materials、engineering requirements、progress、interruption/failure state、producedObjects / producedNetwork。

这些不是最终字段，只用于确认领域边界。

### 10. GreatProject 与政权

大型工程应该属于**政权级能力**，但不意味着只能由 Realm 作为唯一参与者。

Sponsor / Authority → Realm
Participants → Characters / Settlements / Populations / Contractors / Subjects / Allies
Funding → Realm treasury + contributions
Labor → Population / subjects / hired labor / military engineering units
Materials → Economy / logistics
Execution → Process / Activity
Result → World State + Network + Structures + Settlement changes

### 11. 下一阶段

继续审计：PlanType.Engineering / Construction 的现有语义；ConstructionPlanSystem 与 Activity/Process 的边界；TradeRoute 的节点/路径模型；Barrier / Gate / Fort 的关系；保存系统中工程/计划是否已有持久化入口。

然后形成：GreatProject / Network / Structure / Settlement / Anchor 的最终领域边界图 + 第一版字段迁移矩阵。

当前仍不创建 GreatProject 类。