# Architecture 2.0 Domain Audit 15

## 本轮范围

本轮继续核实 TradeRoute / TradeCenter / Caravan 与 Innovation 当前实际实现，重点识别“设计上已经升级”与“代码中仍残留旧模型”的并存状态。

## 1. TradeRoute：currentEfficiency 实际是派生值，但 isBlocked 仍是权威布尔状态

当前 TradeRoute 字段：

- fromRegionId
- toRegionId
- nodeTileIndices
- baseEfficiency
- currentEfficiency
- isBlocked

CalculateEfficiency(TileData[]) 会读取每个节点的：

- roadLevel
- stability

并根据路线长度计算 currentEfficiency。

因此 currentEfficiency 明确属于 Derived State，不应成为第二份权威经济状态。

但 isBlocked 目前直接参与 CalculateEfficiency，并且 Caravan.MoveTick 直接用它阻止移动。这意味着当前代码把“路线是否被阻塞”作为独立事实保存，而没有完整证明其来源。

2.0 应逐步改为：

`Blockage Facts → TradeRoute Blockage Query → Effective Efficiency`

Blockage Facts 可以来自战争、关隘、破坏、政治通行权、地理变化等。

## 2. TradeCenter：经济状态与空间/人口来源仍高度耦合

TradeCenter 本身已经是独立经济实体，并有 DTO 持久化边界。

但 UpdateSupplyDemand(TileData[]) 直接扫描 tile：

- fertility
- development
- populationBlocks
- buildingLevels

由此说明当前 Economy 仍把 MapCell 当作人口、建筑、生产的直接权威来源。

这不是 TradeCenter 领域错误，而是跨域读取尚未迁移完成。

2.0 目标应为：

`Economy Query → Population / Production / Settlement / Geography`

TradeCenter 只消费这些查询结果，不直接遍历 MapCell 内部数据。

## 3. Caravan：它进一步证明 MapActor / MapUnit 迁移的重要性

Caravan 有自己的：

- caravanId
- fromRegionId / toRegionId
- currentNodeIndex
- cargo
- capacity
- speed
- isMoving
- moveProgress

但 MoveTick 接受 TradeRoute + TileData[]，并把 currentNodeIndex 作为路线位置。

这与此前 MapActor/MapUnit 审计形成一致证据：商队属于地图上的移动实体，但其经济货物状态属于 Economy，位置/移动属于 MapUnit/Movement 层。

因此不能把 Caravan 直接塞进 TradeRoute；应保持：

`Caravan(MapUnit + Economy cargo) → TradeRoute/Network → Movement Query`

## 4. Innovation：代码中确实存在“旧 Realm Research + 新 Practice Progress”并存

本轮读取 InnovationTypes 与 InnovationTree 后，发现一个非常关键的事实：

### 新模型证据

InnovationDef 已经远超简单线性科技树：

- InnovationDomain：Technology / Thought / Institution / Tradition
- InnovationField 多领域分类
- prerequisites + prerequisitesAny
- requiredAnyResources / requiredAllResources
- allowTradeResource
- requiredResourceAmount
- requiredCapabilities
- practiceTags
- affinityTags

InnovationTree 又拥有 InnovationProgress，并实现：

- cumulativeOutput
- averageQuality
- oldMethodPracticeCount
- researcherCount
- facility support
- base observation
- diminishing returns
- resource cumulative output
- practice-based monthly progress

这与 README 旧的“研究点/速率/完成效果”已经不是同一个完整模型。

### 旧模型残留证据

同一个 InnovationTree 仍然保留：

- _realmResearchPoints
- _realmCurrentResearch
- DailyTick(realmId, researchRate)
- StartResearch(realmId, innovationId)
- CompleteResearch(realmId, innovationId)
- GetResearchProgress(realmId)
- GetCurrentResearch(realmId)

也就是说当前实现不是“旧模型已完全删除”，而是：

`Realm research-point model + Practice-driven InnovationProgress`

两套机制共存于 InnovationTree。

因此此前“创新系统已经完成升级”的结论应精确表述为：

**升级后的模型已经存在并且规模明显扩大，但旧 Realm Research 状态仍然残留，尚未完成所有权与流程收敛。**

## 5. Innovation 的领域边界现在可以进一步明确

InnovationDef = 革新内容定义。

InnovationTree = 当前仍混合 Definition Registry、Realm ownership、Research Selection、Research Progress、Practice Progress、Resource prerequisite query。

这意味着 InnovationTree 当前实际上是一个过大的聚合对象。

2.0 后续应拆分职责，但暂时不直接重构：

- InnovationCatalog / Definition Query
- InnovationKnowledge / Social Knowledge
- InnovationProgress / Progress State
- CharacterResearch / Personal Practice
- ResearchPlan / Personal Research Intent
- InnovationEffect / completion consequence

其中 CultureData.innovationAffinities 只提供文化侧 Modifier/Input，不应成为 Innovation owner。

## 6. Innovation 与 Culture 的边界证据

CultureData 只有 innovationAffinities，并提供 HasInnovationAffinity(fieldName)。

InnovationTree 读取这一值并计算有效研究速率，当前为亲和时 ×1.25。

因此正确关系是：

`Culture → Innovation Modifier`

而不是：

`Culture → Innovation State`

这也支持之前的判断：Culture 不应因为革新系统升级而膨胀成 Innovation mega-entity。

## 7. 需要特别注意的当前矛盾

InnovationTree 中存在两个进度体系：

A. Realm research points：按 realmId 累加 researchRate，达到 innovation.researchCost 即完成；

B. InnovationProgress：按生产实践、品质、角色研究、设施、基础观察、边际递减累加到 100。

这两个体系的完成入口都可能调用 CompleteResearch。

因此后续迁移必须先决定“谁是最终权威进度”，而不是简单把字段搬家。

在没有完成所有调用方审计前，不应删除其中任何一套字段。

## 8. 当前阶段新增结论

可以把目前架构状态准确描述为：

- Map / Settlement / Economy / Innovation 等领域的目标边界已经逐渐明确；
- 大量新模型已经存在；
- 但旧数据结构仍被多个系统直接读取；
- 部分领域同时存在新旧两套机制；
- 当前阶段最重要的不是“继续造新类”，而是建立字段级权威矩阵与迁移顺序。

因此下一步进入 Field Migration Matrix 的条件已经接近满足，但仍需先补齐 InnovationTree 的所有调用方以及 Road/Barrier/Fort 的引用图。

## 9. 下一阶段

优先继续两条线：

1. InnovationTree 全调用方：确定 RealmResearch / InnovationProgress / CharacterResearch / ResearchPlan 的实际调用关系；
2. Road / Barrier / Fort 全引用：确定 BuildingSystem、MilitaryMovement、SettlementControl、TradeRoute、SaveData 中哪些字段仍把 TileData 当权威。

完成后建立第一版：

`Domain Boundary Matrix`
+
`Field Authority / Migration Matrix`

仍不直接删除旧字段，也不创建最终 Network 基类或 GreatProject 类。
